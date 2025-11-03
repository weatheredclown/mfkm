const CardColor = Object.freeze({
  NONE: "none",
  FRIEND: "friend",
  MONSTER: "monster"
});

const CardType = Object.freeze({
  POINT: "point",
  ACTION: "action",
  FRIEND_MONSTER: "friendMonster"
});

const CardAction = Object.freeze({
  NO_ACTION: "noAction",
  PICK_FROM_DISCARD: "pickFromDiscard",
  TAKE_TWO_ACTIONS: "takeTwoActions",
  DRAW_ONE_CARD: "drawOneCard",
  STEAL_EASY_FRIEND_MONSTER: "stealEasyFriendMonster",
  STEAL_CARD: "stealCard",
  TRADE_CARD: "tradeCard",
  BUY_WITH_BANK: "buyWithBank",
  BUY_WITH_DISCOUNT: "buyWithDiscount"
});

const typeRank = {
  [CardType.POINT]: 0,
  [CardType.ACTION]: 1,
  [CardType.FRIEND_MONSTER]: 2
};

function capitalize(value) {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function escapeHtml(value) {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

class Card {
  constructor(color, value, type, action, description) {
    this.color = color;
    this.value = value;
    this.type = type;
    this.action = action;
    this.description = description;
  }

  compareTo(other) {
    if (this.type === CardType.FRIEND_MONSTER && other.type !== CardType.FRIEND_MONSTER) {
      return -1;
    }
    if (this.type !== CardType.FRIEND_MONSTER && other.type === CardType.FRIEND_MONSTER) {
      return 1;
    }
    if (this.color === CardColor.FRIEND && other.color !== CardColor.FRIEND) {
      return -1;
    }
    if (this.color !== CardColor.FRIEND && other.color === CardColor.FRIEND) {
      return 1;
    }

    if (
      this.type === CardType.FRIEND_MONSTER &&
      other.type === CardType.FRIEND_MONSTER &&
      this.color === CardColor.FRIEND &&
      other.color === CardColor.FRIEND
    ) {
      return other.value - this.value;
    }

    if (
      this.type === CardType.FRIEND_MONSTER &&
      other.type === CardType.FRIEND_MONSTER &&
      this.color === CardColor.MONSTER &&
      other.color === CardColor.MONSTER
    ) {
      return other.value - this.value;
    }

    if (
      this.type === CardType.POINT &&
      other.type === CardType.POINT &&
      this.color === CardColor.FRIEND &&
      other.color === CardColor.FRIEND
    ) {
      return other.value - this.value;
    }

    if (
      this.type === CardType.POINT &&
      other.type === CardType.POINT &&
      this.color === CardColor.MONSTER &&
      other.color === CardColor.MONSTER
    ) {
      return other.value - this.value;
    }

    if (this.type === CardType.ACTION && other.type === CardType.ACTION) {
      return 0;
    }

    return typeRank[this.type] - typeRank[other.type];
  }

  toString() {
    const colorString = this.color === CardColor.NONE ? "" : `${capitalize(this.color)} `;
    switch (this.type) {
      case CardType.POINT: {
        const suffix = this.value === 1 ? "pt" : "pts";
        return `${colorString}${suffix}: ${this.value} – ${this.description}`;
      }
      case CardType.ACTION:
        return `${this.description}`;
      case CardType.FRIEND_MONSTER: {
        let tier = "";
        if (this.value === 1) tier = "Easy";
        else if (this.value === 3) tier = "Medium";
        else if (this.value === 5) tier = "Hard";
        return `${this.description} – ${tier} ${colorString}cost: ${this.value}`;
      }
      default:
        return this.description;
    }
  }

  get label() {
    switch (this.type) {
      case CardType.POINT:
        return "Points";
      case CardType.ACTION:
        return "Action";
      case CardType.FRIEND_MONSTER:
        return this.color === CardColor.FRIEND ? "Friend" : "Monster";
      default:
        return "Card";
    }
  }

  async handleCard(player) {
    switch (this.type) {
      case CardType.POINT:
        return this.#handlePointCard(player);
      case CardType.ACTION:
        return this.#handleActionCard(player);
      case CardType.FRIEND_MONSTER:
        return this.#handleFriendMonsterCard(player);
      default:
        return true;
    }
  }

  async #handlePointCard(player) {
    if (this.action === CardAction.DRAW_ONE_CARD) {
      return this.#handleActionCard(player);
    }

    let playAs = 0;
    if (this.action !== CardAction.NO_ACTION) {
      playAs = await player.getPlayerChoice(`Playing ${this.toString()} as:`, ["Points", "Action"], {
        canPass: false
      });
    }

    if (playAs === 0) {
      player.bankCard(this);
      player.game.ui.log(`[${player.name}] banked ${this.toString()}.`);
      player.stats.cardsBanked += 1;
      player.game.ui.update(player.game);
      return true;
    }

    return this.#handleActionCard(player);
  }

  async #handleActionCard(player) {
    const { deck } = player.game;
    player.playCard(this);
    deck.discardCard(this);
    player.stats.actionsPlayed += 1;
    let resolved = true;

    switch (this.action) {
      case CardAction.DRAW_ONE_CARD: {
        if (deck.hasDrawCards()) {
          const bonus = deck.drawCard();
          player.game.ui.log(`[${player.name}] draws a bonus card: ${bonus.toString()}`);
          player.handAdd(bonus);
          player.stats.cardsDrawn += 1;
        }
        break;
      }
      case CardAction.TAKE_TWO_ACTIONS: {
        player.game.ui.log("First bonus action:");
        await player.playACard();
        player.game.ui.log("Second bonus action:");
        await player.playACard();
        break;
      }
      case CardAction.PICK_FROM_DISCARD: {
        if (!deck.hasDiscardCards()) {
          player.game.ui.log("Discard pile is empty.");
          break;
        }
        const index = await player.printCardsAndCollectSelection("Discard pile:", deck.discardPile, {
          allowPass: true
        });
        if (index === -1) {
          player.stats.passes += 1;
          player.game.ui.log("Passed on discard.");
        } else {
          const picked = deck.discardPile.splice(index, 1)[0];
          player.handAdd(picked);
          player.stats.discardsDrawn += 1;
          player.game.ui.log(`[${player.name}] picked ${picked.toString()} from discard.`);
        }
        break;
      }
      case CardAction.STEAL_EASY_FRIEND_MONSTER: {
        const targets = player.opponents.filter((opp) =>
          opp.bleachers.some((card) => card.value === 1 && card.color === this.color)
        );
        if (!targets.length) {
          player.game.ui.log("No easy targets available.");
          resolved = false;
          break;
        }
        const choices = targets.map((opp) => opp.name);
        const oppIndex = await player.getPlayerChoice("Pick an opponent:", choices, { canPass: false });
        const opponent = targets[oppIndex];
        const stealable = opponent.bleachers.filter((card) => card.value === 1 && card.color === this.color);
        if (!stealable.length) {
          player.game.ui.log("Opponent has no easy cards.");
          resolved = false;
          break;
        }
        const stealIndex = await player.printCardsAndCollectSelection(`${opponent.name}'s board:`, stealable, {
          allowPass: true
        });
        if (stealIndex === -1) {
          player.game.ui.log("Passed on stealing, card returned to hand.");
          resolved = false;
        } else {
          const stolen = stealable[stealIndex];
          opponent.removeFromBleachers(stolen);
          player.bleachers.push(stolen);
          player.stats.steals += 1;
          player.game.ui.log(`[${player.name}] stole ${stolen.toString()} from ${opponent.name}.`);
        }
        break;
      }
      case CardAction.STEAL_CARD: {
        const targets = player.opponents.filter((opp) => opp.hand.length > 0);
        if (!targets.length) {
          player.game.ui.log("No opponents with cards to steal.");
          resolved = false;
          break;
        }
        const choices = targets.map((opp) => opp.name);
        const oppIndex = await player.getPlayerChoice("Pick an opponent:", choices, { canPass: false });
        const opponent = targets[oppIndex];
        const stealIndex = await player.printCardsAndCollectSelection(`${opponent.name}'s hand:`, opponent.hand, {
          allowPass: true
        });
        if (stealIndex === -1) {
          player.game.ui.log("No card selected.");
          resolved = false;
        } else {
          const stolen = opponent.hand.splice(stealIndex, 1)[0];
          player.handAdd(stolen);
          player.stats.steals += 1;
          player.game.ui.log(`[${player.name}] stole ${stolen.toString()} from ${opponent.name}.`);
        }
        break;
      }
      case CardAction.TRADE_CARD: {
        const targets = player.opponents.filter((opp) => opp.hand.length > 0);
        if (!targets.length || player.hand.length === 0) {
          player.game.ui.log("No trade possible.");
          resolved = false;
          break;
        }
        const choices = targets.map((opp) => opp.name);
        const oppIndex = await player.getPlayerChoice("Pick an opponent:", choices, { canPass: false });
        const opponent = targets[oppIndex];
        const takeIndex = await player.printCardsAndCollectSelection("Card to take:", opponent.hand, {
          allowPass: true
        });
        if (takeIndex === -1) {
          player.game.ui.log("Trade cancelled.");
          resolved = false;
          break;
        }
        const tradeIndex = await player.printCardsAndCollectSelection("Card to give:", player.hand, {
          allowPass: true
        });
        if (tradeIndex === -1) {
          player.game.ui.log("Trade cancelled.");
          resolved = false;
          break;
        }
        const taken = opponent.hand.splice(takeIndex, 1)[0];
        const given = player.hand.splice(tradeIndex, 1)[0];
        opponent.handAdd(given);
        player.handAdd(taken);
        player.stats.trades += 1;
        player.game.ui.log(`[${player.name}] traded ${given.toString()} for ${taken.toString()} with ${opponent.name}.`);
        break;
      }
      case CardAction.BUY_WITH_DISCOUNT: {
        const buyable = player.hand.filter((card) => card.type === CardType.FRIEND_MONSTER);
        if (!buyable.length) {
          player.game.ui.log("No friends or monsters in hand to buy.");
          resolved = false;
          break;
        }
        const index = await player.printCardsAndCollectSelection("Card to buy:", buyable, {
          allowPass: true
        });
        if (index === -1) {
          resolved = false;
          break;
        }
        const toBuy = buyable[index];
        const originalValue = toBuy.value;
        toBuy.value = Math.max(0, originalValue - 2);
        const success = await toBuy.handleCard(player);
        toBuy.value = originalValue;
        if (!success) {
          resolved = false;
        }
        break;
      }
      default:
        player.game.ui.log("Action resolved.");
    }

    if (!resolved) {
      deck.removeFromDiscard(this);
      player.handAdd(this);
    }

    player.game.ui.update(player.game);
    return resolved;
  }

  async #handleFriendMonsterCard(player) {
    const canUseDiscount = player.bankedPoints(this.color) >= this.value - player.role.discount;
    const requiredPoints = Math.max(0, this.value - (canUseDiscount ? player.role.discount : 0));

    if (player.usablePoints(this.color) < requiredPoints) {
      player.game.ui.log(`Not enough banked points to purchase ${this.toString()}.`);
      return false;
    }

    let usableCards = player.bankedCards.filter((card) => card.color === this.color);
    if (!canUseDiscount && player.role.canBuyWithHandPoints) {
      usableCards = usableCards.concat(
        player.hand.filter((card) => card.type === CardType.POINT && card.color === this.color)
      );
    }

    const payment = findOptimalCards(usableCards, requiredPoints);
    const paid = payment.reduce((sum, card) => sum + card.value, 0);

    if (paid < requiredPoints) {
      player.game.ui.log(`Not enough value to cover the cost of ${this.description}.`);
      return false;
    }

    payment.forEach((card) => {
      if (player.removeBankedCard(card)) {
        player.stats.spent += 1;
      } else {
        player.handRemove(card);
        player.stats.spentFromHand += 1;
      }
      player.game.ui.log(`Spent ${card.toString()}.`);
    });

    player.bleachers.push(this);
    player.playCard(this);
    player.stats.bought += 1;
    player.game.ui.log(`(${this.description}) purchased for ${paid}.`);
    player.game.ui.update(player.game);
    return true;
  }
}

function findOptimalCards(cards, targetPoints) {
  if (targetPoints <= 0) {
    return [];
  }
  const sorted = [...cards].sort((a, b) => a.value - b.value);
  const exact = sorted.find((card) => card.value === targetPoints);
  if (exact) {
    return [exact];
  }
  const selected = [];
  let total = 0;
  for (const card of sorted) {
    total += card.value;
    selected.push(card);
    if (card.value >= targetPoints) {
      return [card];
    }
    if (total >= targetPoints) {
      return selected;
    }
  }
  return selected;
}

function buildDeck() {
  const deck = [];
  const pointSet = [
    { color: CardColor.MONSTER, value: 1, action: CardAction.NO_ACTION, description: "No Action" },
    { color: CardColor.MONSTER, value: 2, action: CardAction.DRAW_ONE_CARD, description: "(plus draw again)" },
    { color: CardColor.MONSTER, value: 3, action: CardAction.TAKE_TWO_ACTIONS, description: "or take two actions" },
    { color: CardColor.MONSTER, value: 3, action: CardAction.PICK_FROM_DISCARD, description: "or pick from discard" },
    {
      color: CardColor.MONSTER,
      value: 5,
      action: CardAction.STEAL_EASY_FRIEND_MONSTER,
      description: "or steal an easy monster"
    },
    { color: CardColor.FRIEND, value: 1, action: CardAction.NO_ACTION, description: "No Action" },
    { color: CardColor.FRIEND, value: 2, action: CardAction.DRAW_ONE_CARD, description: "(plus draw again)" },
    { color: CardColor.FRIEND, value: 3, action: CardAction.PICK_FROM_DISCARD, description: "or pick from discard" },
    { color: CardColor.FRIEND, value: 3, action: CardAction.TAKE_TWO_ACTIONS, description: "or take two actions" },
    {
      color: CardColor.FRIEND,
      value: 5,
      action: CardAction.STEAL_EASY_FRIEND_MONSTER,
      description: "or steal an easy friend"
    }
  ];

  for (let i = 0; i < 4; i += 1) {
    pointSet.forEach((config) => {
      deck.push(new Card(config.color, config.value, CardType.POINT, config.action, config.description));
    });
  }

  const actionCards = [
    { action: CardAction.BUY_WITH_DISCOUNT, description: "Buy Action" },
    { action: CardAction.BUY_WITH_DISCOUNT, description: "Buy Action" },
    { action: CardAction.BUY_WITH_DISCOUNT, description: "Buy Action" },
    { action: CardAction.BUY_WITH_DISCOUNT, description: "Buy Action" },
    { action: CardAction.BUY_WITH_DISCOUNT, description: "Buy Action" },
    { action: CardAction.STEAL_CARD, description: "Forced Steal" },
    { action: CardAction.TRADE_CARD, description: "Forced Trade" },
    { action: CardAction.STEAL_CARD, description: "Forced Steal" },
    { action: CardAction.TRADE_CARD, description: "Forced Trade" }
  ];

  actionCards.forEach((config) => {
    deck.push(new Card(CardColor.NONE, 0, CardType.ACTION, config.action, config.description));
  });

  const friendMonsterCards = [
    { color: CardColor.FRIEND, value: 1, name: "Howman" },
    { color: CardColor.FRIEND, value: 3, name: "Kevin" },
    { color: CardColor.FRIEND, value: 5, name: "Kevin's Mom" },
    { color: CardColor.MONSTER, value: 1, name: "Gelatinous Cube" },
    { color: CardColor.MONSTER, value: 3, name: "Sasquatch" },
    { color: CardColor.MONSTER, value: 5, name: "Green Dragon" },
    { color: CardColor.FRIEND, value: 1, name: "Grace" },
    { color: CardColor.FRIEND, value: 3, name: "Roshni" },
    { color: CardColor.FRIEND, value: 5, name: "Kate" },
    { color: CardColor.MONSTER, value: 1, name: "Mimic" },
    { color: CardColor.MONSTER, value: 3, name: "Beholder" },
    { color: CardColor.MONSTER, value: 5, name: "Black Dragon" },
    { color: CardColor.FRIEND, value: 1, name: "Anthony" },
    { color: CardColor.FRIEND, value: 3, name: "Muhan" },
    { color: CardColor.FRIEND, value: 5, name: "Dez" },
    { color: CardColor.MONSTER, value: 1, name: "Kobold" },
    { color: CardColor.MONSTER, value: 3, name: "Mind Flayer" },
    { color: CardColor.MONSTER, value: 5, name: "Blue Dragon" }
  ];

  friendMonsterCards.forEach((config) => {
    deck.push(new Card(config.color, config.value, CardType.FRIEND_MONSTER, CardAction.BUY_WITH_BANK, config.name));
  });

  return deck;
}

class Deck {
  constructor() {
    this.drawPile = buildDeck();
    this.discardPile = [];
    this.shuffle();
  }

  shuffle() {
    for (let i = this.drawPile.length - 1; i > 0; i -= 1) {
      const j = Math.floor(Math.random() * (i + 1));
      [this.drawPile[i], this.drawPile[j]] = [this.drawPile[j], this.drawPile[i]];
    }
  }

  drawCard() {
    if (!this.hasDrawCards()) {
      throw new Error("Draw pile is empty");
    }
    return this.drawPile.shift();
  }

  drawCardFromDiscard() {
    if (!this.hasDiscardCards()) {
      throw new Error("Discard pile is empty");
    }
    return this.discardPile.pop();
  }

  discardCard(card) {
    this.discardPile.push(card);
  }

  removeFromDiscard(card) {
    const index = this.discardPile.indexOf(card);
    if (index >= 0) {
      this.discardPile.splice(index, 1);
    }
  }

  hasDrawCards() {
    return this.drawPile.length > 0;
  }

  hasDiscardCards() {
    return this.discardPile.length > 0;
  }

  cardsLeft() {
    return this.drawPile.length;
  }

  topDiscard() {
    return this.discardPile.length ? this.discardPile[this.discardPile.length - 1] : null;
  }
}

class Role {
  constructor(name, discount, leftoverMultiplier, canBuyWithHandPoints, canDrawFromDiscard, canBankInsteadOfDraw) {
    this.name = name;
    this.discount = discount;
    this.leftoverMultiplier = leftoverMultiplier;
    this.canBuyWithHandPoints = canBuyWithHandPoints;
    this.canDrawFromDiscard = canDrawFromDiscard;
    this.canBankInsteadOfDraw = canBankInsteadOfDraw;
  }

  toString() {
    return this.name;
  }
}

const Roles = {
  none: new Role("None", 0, 0.25, false, false, false),
  gravedigger: new Role("Gravedigger", 0, 1.0, false, false, false),
  artificer: new Role("Artificer", 1, 0.25, true, false, false),
  fateweaver: new Role("Fateweaver", 0, 0.25, false, true, true)
};

class Player {
  constructor(name, role, game, { autoPilot = false } = {}) {
    this.name = name;
    this.role = role;
    this.game = game;
    this.autoPilot = autoPilot;
    this.hand = [];
    this.bankedCards = [];
    this.bleachers = [];
    this.opponents = [];
    this.stats = {
      cardsBanked: 0,
      actionsPlayed: 0,
      cardsDrawn: 0,
      discardsDrawn: 0,
      steals: 0,
      bankInsteadOfDraw: 0,
      trades: 0,
      spent: 0,
      spentFromHand: 0,
      bought: 0,
      passes: 0,
      choices: 0
    };
  }

  get greenPoints() {
    return this.bankedCards.filter((card) => card.color === CardColor.MONSTER).reduce((sum, card) => sum + card.value, 0);
  }

  get pinkPoints() {
    return this.bankedCards.filter((card) => card.color === CardColor.FRIEND).reduce((sum, card) => sum + card.value, 0);
  }

  get completeSets() {
    const colors = [CardColor.FRIEND, CardColor.MONSTER];
    return colors.reduce((total, color) => {
      const easy = this.bleachers.filter((card) => card.value === 1 && card.color === color).length;
      const medium = this.bleachers.filter((card) => card.value === 3 && card.color === color).length;
      const hard = this.bleachers.filter((card) => card.value === 5 && card.color === color).length;
      return total + Math.min(easy, medium, hard);
    }, 0);
  }

  get gamePoints() {
    let sum = this.bleachers.reduce((acc, card) => acc + card.value, 0);
    sum += this.completeSets * 3;
    const bonusGreenOnes =
      this.bankedCards.filter((card) => card.value === 1 && card.color === CardColor.MONSTER).length +
      this.hand.filter((card) => card.value === 1 && card.color === CardColor.MONSTER).length;
    const bonusPinkOnes =
      this.bankedCards.filter((card) => card.value === 1 && card.color === CardColor.FRIEND).length +
      this.hand.filter((card) => card.value === 1 && card.color === CardColor.FRIEND).length;
    const cardBonus = this.role.leftoverMultiplier;
    if (this.bankedCards.length) {
      sum += this.bankedCards.filter((card) => card.type === CardType.POINT).length * cardBonus;
    }
    if (this.hand.length) {
      sum += this.hand.filter((card) => card.type === CardType.POINT).length * cardBonus;
    }
    const bonusGreen = Math.floor(bonusGreenOnes / 3);
    const bonusPink = Math.floor(bonusPinkOnes / 3);
    sum -= (bonusGreen + bonusPink) * 3 * cardBonus;
    sum += bonusGreen + bonusPink;
    return sum;
  }

  bankedPoints(color) {
    return this.bankedCards.filter((card) => card.color === color).reduce((sum, card) => sum + card.value, 0);
  }

  usablePoints(color) {
    const bonus = this.role === Roles.artificer
      ? this.hand.filter((card) => card.type === CardType.POINT && card.color === color).reduce((sum, card) => sum + card.value, 0)
      : 0;
    return bonus + this.bankedPoints(color);
  }

  handAdd(card) {
    this.hand.push(card);
    this.hand.sort((a, b) => a.compareTo(b));
    this.game.ui.update(this.game);
  }

  handRemove(card) {
    const index = this.hand.indexOf(card);
    if (index >= 0) {
      this.hand.splice(index, 1);
      this.game.ui.update(this.game);
      return true;
    }
    return false;
  }

  removeBankedCard(card) {
    const index = this.bankedCards.indexOf(card);
    if (index >= 0) {
      this.bankedCards.splice(index, 1);
      this.game.ui.update(this.game);
      return true;
    }
    return false;
  }

  removeFromBleachers(card) {
    const index = this.bleachers.indexOf(card);
    if (index >= 0) {
      this.bleachers.splice(index, 1);
      this.game.ui.update(this.game);
    }
  }

  playCard(card) {
    this.handRemove(card);
  }

  dealCard(card) {
    this.hand.push(card);
    this.hand.sort((a, b) => a.compareTo(b));
  }

  bankCard(card) {
    this.playCard(card);
    this.bankedCards.push(card);
  }

  async takeTurn() {
    this.game.ui.log(`\u2014 Turn for ${this.name}`);
    if (!this.game.deck.hasDrawCards()) {
      this.game.ui.log(`${this.name} cannot draw a card. The deck is empty!`);
      return false;
    }
    await this.drawCard();
    await this.playACard();
    return true;
  }

  async drawCard() {
    const deck = this.game.deck;
    const handWasEmpty = this.hand.length === 0;

    if (this.role.canDrawFromDiscard && deck.hasDiscardCards()) {
      const choice = await this.getPlayerChoice("Draw from:", ["Draw Pile", "Discard Pile"], { canPass: false });
      if (choice === 1) {
        const drawn = deck.drawCardFromDiscard();
        this.handAdd(drawn);
        this.stats.discardsDrawn += 1;
        this.game.ui.log(`${this.name} draws from discard: ${drawn.toString()}`);
        return;
      }
    }

    if (this.role.canBankInsteadOfDraw) {
      const pointCards = this.hand.filter((card) => card.type === CardType.POINT);
      if (pointCards.length) {
        const choice = await this.getPlayerChoice("Bank or Draw:", ["Draw", "Bank"], { canPass: false });
        if (choice === 1) {
          const index = await this.printCardsAndCollectSelection("Card to bank:", pointCards, { allowPass: false });
          if (index >= 0) {
            const selected = pointCards[index];
            const success = await selected.handleCard(this);
            if (success) {
              this.stats.bankInsteadOfDraw += 1;
              return;
            }
          }
        }
      }
    }

    const drawnCard = deck.drawCard();
    this.handAdd(drawnCard);
    this.stats.cardsDrawn += 1;
    this.game.ui.log(`${this.name} draws: ${drawnCard.toString()}`);
    if (handWasEmpty && deck.hasDrawCards()) {
      await this.drawCard();
    }
  }

  async playACard() {
    if (this.hand.length === 0) {
      this.game.ui.log(`${this.name} has no cards to play and passes.`);
      this.stats.passes += 1;
      return;
    }
    let done = false;
    while (!done) {
      const index = await this.printCardsAndCollectSelection(`${this.name}'s hand:`, this.hand, { allowPass: true });
      if (index === -1) {
        this.game.ui.log(`${this.name} passes.`);
        this.stats.passes += 1;
        done = true;
      } else {
        const card = this.hand[index];
        this.game.ui.log(`${this.name} plays ${card.toString()}`);
        done = await card.handleCard(this);
      }
    }
  }

  async printCardsAndCollectSelection(prompt, list, { allowPass = true } = {}) {
    const cards = Array.isArray(list) ? list : Array.from(list);
    if (!cards.length) {
      this.game.ui.log("Nothing to pick from.");
      return -1;
    }
    const options = cards.map((card) => ({
      label: card.toString(),
      card
    }));
    return this.getPlayerChoice(prompt, options, { canPass: allowPass });
  }

  async getPlayerChoice(prompt, options, { canPass = true } = {}) {
    this.stats.choices += 1;
    if (this.autoPilot) {
      const choice = Player.getAIInput(options.length, canPass);
      await this.game.ui.delay();
      return choice;
    }
    return this.game.ui.promptSelection(prompt, options, { canPass });
  }

  static getAIInput(maxCount, canPass) {
    if (maxCount === 0) {
      return -1;
    }
    if (canPass && Math.floor(Math.random() * 10) === 0) {
      return -1;
    }
    return Math.floor(Math.random() * maxCount);
  }
}

class Game {
  constructor(ui) {
    this.ui = ui;
    this.deck = null;
    this.players = [];
    this.currentPlayerIndex = 0;
    this.round = 0;
    this.running = false;
  }

  async start({ autoPlayer2 = false } = {}) {
    this.ui.clearLog();
    this.ui.hideSummary();
    this.deck = new Deck();
    this.players = [
      new Player("Player One", Roles.none, this, { autoPilot: false }),
      new Player("Player Two", Roles.none, this, { autoPilot: autoPlayer2 })
    ];
    this.players.forEach((player, index) => {
      player.opponents = this.players.filter((_, idx) => idx !== index);
    });
    this.round = 0;
    this.running = true;
    this.ui.log("Game initialised. Dealing opening hands...");
    this.dealOpeningHands();
    this.ui.update(this);
    await this.run();
  }

  dealOpeningHands() {
    for (let i = 0; i < 3; i += 1) {
      this.players.forEach((player) => {
        if (this.deck.hasDrawCards()) {
          const card = this.deck.drawCard();
          player.dealCard(card);
        }
      });
    }
  }

  async run() {
    while (this.running) {
      this.round += 1;
      this.ui.log(`### Round ${this.round}`);
      const continueGame = await this.playRound();
      if (!continueGame) {
        break;
      }
    }
    this.endGame();
  }

  async playRound() {
    for (let i = 0; i < this.players.length; i += 1) {
      this.currentPlayerIndex = i;
      this.ui.update(this);
      const player = this.players[i];
      const result = await player.takeTurn();
      if (!result) {
        return false;
      }
      if (!this.deck.hasDrawCards()) {
        return false;
      }
    }
    return true;
  }

  endGame() {
    this.running = false;
    const scores = this.players.map((player) => ({
      name: player.name,
      score: player.gamePoints,
      green: player.greenPoints,
      pink: player.pinkPoints
    }));
    const maxScore = Math.max(...scores.map((s) => s.score));
    const winners = scores.filter((s) => Math.abs(s.score - maxScore) < 0.001);
    const summary = [
      `Game over after ${this.round} rounds.`,
      winners.length > 1
        ? `It's a tie between ${winners.map((w) => w.name).join(" and ")}!`
        : `${winners[0].name} wins!`
    ];
    scores.forEach((s) => {
      summary.push(
        `${s.name}: ${s.score.toFixed(2)} points (Friend ${s.pink} / Monster ${s.green})`
      );
    });
    this.ui.showSummary(summary.join("<br>"));
  }
}

class GameUI {
  constructor() {
    this.playersContainer = document.getElementById("players");
    this.scoreboard = document.getElementById("scoreboard");
    this.promptText = document.getElementById("prompt-text");
    this.promptOptions = document.getElementById("prompt-options");
    this.logEntries = document.getElementById("log-entries");
    this.summary = document.getElementById("game-summary");
    this.drawCount = document.getElementById("draw-count");
    this.discardCount = document.getElementById("discard-count");
    this.discardTop = document.getElementById("discard-top");
    this.newGameButton = document.getElementById("new-game");
    this.autoToggle = document.getElementById("auto-player-2");
    this.events = [];
    this.currentResolve = null;
    this.pendingOptions = null;
  }

  bind(game) {
    this.game = game;
    this.newGameButton.addEventListener("click", () => {
      game.start({ autoPlayer2: this.autoToggle.checked });
    });
    this.autoToggle.addEventListener("change", () => {
      if (game.players[1]) {
        game.players[1].autoPilot = this.autoToggle.checked;
        this.log(`Auto Pilot Player 2 ${this.autoToggle.checked ? "enabled" : "disabled"}.`);
        this.update(game);
      }
    });
  }

  update(game) {
    this.renderScoreboard(game);
    this.renderPlayers(game);
    this.renderPiles(game.deck);
  }

  renderScoreboard(game) {
    const html = game.players
      .map((player, index) => {
        const auto = player.autoPilot
          ? '<span class="stat-chip">AUTO</span>'
          : "";
        return `
          <article class="score-card">
            <header class="score-card__title">
              <span>${escapeHtml(player.name)}</span>
              ${auto}
            </header>
            <div class="score-card__points">${player.gamePoints.toFixed(2)}</div>
            <div class="score-card__meta">
              <span>Sets: ${player.completeSets}</span>
              <span>Hand: ${player.hand.length}</span>
              <span>Friend pts: ${player.pinkPoints}</span>
              <span>Monster pts: ${player.greenPoints}</span>
            </div>
          </article>
        `;
      })
      .join("");
    this.scoreboard.innerHTML = html;
  }

  renderPlayers(game) {
    const html = game.players
      .map((player, index) => {
        const isActive = index === game.currentPlayerIndex;
        const activeClass = isActive ? "active" : "";
        const handCount = player.hand.length;
        const bankCount = player.bankedCards.length;
        const bleacherCount = player.bleachers.length;
        const handNote = `${handCount} card${handCount === 1 ? "" : "s"}${isActive ? "" : " hidden"}`;
        const bankNote = `${bankCount} card${bankCount === 1 ? "" : "s"}`;
        const tableNote = `${bleacherCount} face-up card${bleacherCount === 1 ? "" : "s"}`;
        return `
          <section class="player-card ${activeClass}">
            <header class="player-card__header">
              <h2 class="player-card__title">
                <span>${escapeHtml(player.name)}</span>
                <span class="player-card__role">${escapeHtml(player.role.toString())}</span>
              </h2>
              <div class="player-card__totals">
                <span class="stat-chip">Sets ${player.completeSets}</span>
                <span class="stat-chip" style="color: var(--friend)">♡ ${player.pinkPoints}</span>
                <span class="stat-chip" style="color: var(--monster)">♢ ${player.greenPoints}</span>
              </div>
            </header>
            ${this.renderSection("Hand", player.hand, {
              hidden: !isActive,
              titleNote: handNote,
              listClasses: ["card-list--hand"]
            })}
            ${this.renderSection("Table – In Play", player.bleachers, {
              titleNote: tableNote,
              listClasses: ["card-list--table"],
              cardOptions: { classes: ["card--table"] }
            })}
            ${this.renderSection("Bank", player.bankedCards, {
              titleNote: bankNote,
              listClasses: ["card-list--bank"]
            })}
          </section>
        `;
      })
      .join("");
    this.playersContainer.innerHTML = html;
  }

  renderSection(title, cards, options = {}) {
    const { hidden = false, titleNote = "", listClasses = [], cardOptions = {} } = options;
    const sectionClasses = ["section"];
    if (!cards.length) {
      sectionClasses.push("section--empty");
    }
    const listClassNames = ["card-list", ...listClasses];
    if (hidden) {
      listClassNames.push("card-list--hidden");
    }
    const noteMarkup = titleNote
      ? ` <span class="section-title__note">${escapeHtml(titleNote)}</span>`
      : "";
    let cardsHtml = "";
    if (hidden) {
      cardsHtml = cards
        .map((_, index) => this.renderCardBack({ label: `${title} card ${index + 1}` }))
        .join("");
    } else {
      cardsHtml = cards.map((card) => this.renderCard(card, cardOptions)).join("");
    }
    return `
      <section class="${sectionClasses.join(" ")}">
        <h3 class="section-title">${escapeHtml(title)}${noteMarkup}</h3>
        <div class="${listClassNames.join(" ")}">${cardsHtml}</div>
      </section>
    `;
  }

  renderCard(card, options = {}) {
    const { classes = [], compact = false } = options;
    const cardClasses = ["card", ...classes];
    if (compact) {
      cardClasses.push("card--compact");
    }
    if (card.type === CardType.ACTION) {
      cardClasses.push("card--action");
    } else if (card.color === CardColor.FRIEND) {
      cardClasses.push("card--friend");
    } else if (card.color === CardColor.MONSTER) {
      cardClasses.push("card--monster");
    }
    if (card.type === CardType.POINT) {
      cardClasses.push("card--point");
    }
    const value = card.value ? card.value : "—";
    return `
      <article class="${cardClasses.join(" ")}">
        <span class="card__label">${escapeHtml(card.label)}</span>
        <span class="card__value">${value}</span>
        <span class="card__meta">${escapeHtml(card.description)}</span>
      </article>
    `;
  }

  renderCardBack({ label = "Hidden card" } = {}) {
    return `
      <article class="card card--back" aria-label="${escapeHtml(label)}">
        <span class="card__label">Hidden</span>
        <span class="card__value">?</span>
        <span class="card__meta">${escapeHtml(label)}</span>
      </article>
    `;
  }

  renderPiles(deck) {
    if (!deck) {
      this.drawCount.textContent = "0";
      this.discardCount.textContent = "0";
      this.discardTop.textContent = "Empty";
      this.discardTop.classList.remove("pile__card--filled");
      return;
    }
    this.drawCount.textContent = deck.cardsLeft();
    this.discardCount.textContent = deck.discardPile.length;
    const top = deck.topDiscard();
    if (top) {
      this.discardTop.classList.add("pile__card--filled");
      this.discardTop.innerHTML = this.renderCard(top, {
        classes: ["card--pile"],
        compact: true
      });
    } else {
      this.discardTop.classList.remove("pile__card--filled");
      this.discardTop.textContent = "Empty";
    }
  }

  async promptSelection(prompt, options, { canPass }) {
    if (this.currentResolve) {
      this.currentResolve(-1);
    }
    this.promptText.textContent = prompt;
    this.promptOptions.innerHTML = "";
    const frag = document.createDocumentFragment();
    const normalized = options.map((option) =>
      typeof option === "string" ? { label: option } : option
    );
    normalized.forEach((option, index) => {
      const button = document.createElement("button");
      button.type = "button";
      button.classList.add("prompt-option");
      if (option.card) {
        button.classList.add("prompt-option--card");
        button.setAttribute("aria-label", option.label);
        button.innerHTML = this.renderCard(option.card, {
          classes: ["card--selectable"],
          compact: true
        });
      } else {
        button.textContent = option.label;
      }
      button.addEventListener("click", () => {
        this.resolveSelection(index);
      });
      frag.appendChild(button);
    });
    if (canPass) {
      const passButton = document.createElement("button");
      passButton.type = "button";
      passButton.textContent = "Pass";
      passButton.classList.add("pass");
      passButton.addEventListener("click", () => {
        this.resolveSelection(-1);
      });
      frag.appendChild(passButton);
    }
    this.promptOptions.appendChild(frag);
    return new Promise((resolve) => {
      this.currentResolve = resolve;
    });
  }

  resolveSelection(value) {
    if (this.currentResolve) {
      this.currentResolve(value);
      this.currentResolve = null;
    }
    this.promptOptions.innerHTML = "";
  }

  log(message) {
    this.events.unshift(message);
    if (this.events.length > 40) {
      this.events.pop();
    }
    this.renderLog();
  }

  clearLog() {
    this.events = [];
    this.renderLog();
  }

  renderLog() {
    const html = this.events.map((event) => `<li>${escapeHtml(event)}</li>`).join("");
    this.logEntries.innerHTML = html;
  }

  showSummary(content) {
    this.summary.innerHTML = content;
    this.summary.classList.add("visible");
  }

  hideSummary() {
    this.summary.classList.remove("visible");
    this.summary.innerHTML = "";
  }

  async delay(ms = 400) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}

const ui = new GameUI();
const game = new Game(ui);
ui.bind(game);
