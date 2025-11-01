using System.Configuration;
using System.Data;
using System.Windows;

namespace mfkmapp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static void Main2()
        {
            mfkmapp.App app = new mfkmapp.App();
            app.InitializeComponent();
            app.Run();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
