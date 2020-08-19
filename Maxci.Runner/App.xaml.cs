using System.Windows;

namespace Maxci.Runner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static NLog.Logger Log;

        static App()
        {
            Log = NLog.LogManager.GetCurrentClassLogger();
            Log.Info("Start program");
        }
    }
}
