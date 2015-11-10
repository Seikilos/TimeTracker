using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TimeTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );


#if DEBUG
            var provider = new DummyCurrentTimeProvider();
#else
            //var provider = new StandardCurrentTimeProvider();
            var provider = new TimeFromTextProvider();
#endif
            var m = new MainWindow(provider);

            provider.TextBox = m.startTime;

            m.Show();

        }
    }
}
