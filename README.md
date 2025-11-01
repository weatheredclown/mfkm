# MFKM

MFKM contains two .NET projects that simulate and visualize a two-player monster-versus-friend card game.

## Repository layout

- `mfkm/` – Console implementation that automates card play, simulating multiple rounds between computer-controlled players.
- `mfkmapp/` – WPF desktop client that hosts the same game logic with a UI for stepping through each turn.
- `mfkm.sln` – Solution file for the console project.
- `mfkmapp.sln` – Solution file for the WPF app.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download) or later.

## Running the console simulator

```bash
# Run automated games and print the summary statistics
dotnet run --project mfkm
```

The console entry point (`mfkm/mfkm.cs`) launches a `PlayContainer` that loops through games, prints round-by-round updates, and reports cumulative statistics when the simulation ends.

## Launching the WPF client

The WPF client shares the same game engine but surfaces turns through a graphical interface.

```bash
# Build the application
dotnet build mfkmapp

# On Windows, start the UI (requires a GUI-enabled environment)
dotnet run --project mfkmapp
```

`MainWindow.xaml.cs` initializes a two-player game, pushes turn prompts into the UI, and mirrors the console statistics such as bleacher counts, draw pile size, and per-color point totals.

## Development tips

- The console UI utilities in `mfkm/Program.cs` orchestrate menu navigation, AI decision making, and the overall game loop.
- The shared game model (cards, deck, players, and statistics) lives in the console project and is consumed directly by the WPF client via project references.

Happy hacking!
