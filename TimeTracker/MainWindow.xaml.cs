using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml.XPath;
using NodaTime;
using NodaTime.Text;

namespace TimeTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Task _updater;
        private bool _doNotUpdate;

        public ObservableCollection<string> Cats { get; set; }

        private List<Tuple<string, DateTime>> _work = new List< Tuple< string, DateTime > >();

        public DateTime StartDate;

        public MainWindow()
        {
            DataContext = this;
            var doc = XDocument.Load( "config.xml" );
            var breaks = doc.XPathSelectElements( "//Break" ).Select( e => Tuple.Create(
                LocalTimePattern.CreateWithInvariantCulture( e.Attribute( "Start" ).Value ),
                LocalTimePattern.CreateWithInvariantCulture( e.Attribute( "End" ).Value ) ) ).ToList();
            Cats = new ObservableCollection< string >(doc.XPathSelectElements( "//Category" ).Select( e => e.Value ).ToList());

            for ( var i = 0; i < 10; ++i )
            {
                Cats.Add( "Project "+i );
            }

         


            InitializeComponent();

               foreach ( var b in breaks )
            {
                output.AppendNewLine( string.Format( "Break from {0} to {1}", b.Item1.PatternText, b.Item2.PatternText ) );
            }


            list.IsEnabled = false;

            _doNotUpdate = false;
            _updater = Task.Run( () =>
            {

                while ( true )
                {
                    if ( _doNotUpdate == false )
                    {
                        startTime.Dispatcher.Invoke( () => startTime.Text = DateTime.Now.ToString( "HH:mm:ss" ) );
                    }
                    //startTime.Dispatcher.Invoke(() =>  startTime.Text = SystemClock.Instance.Now.ToString("HH:mm", null));
                   
                    Thread.Sleep( 1000 );
                }
            } ).ContinueWith( ErrorFunc );

        }

        private void ErrorFunc( Task obj )
        {
            if ( obj.IsFaulted )
            {
                MessageBox.Show( obj.Exception.ToString() );
            }
        }

        private void startTime_GotFocus( object sender, RoutedEventArgs e )
        {
            _doNotUpdate = true;
        }

        private void Button_Click( object sender, RoutedEventArgs e )
        {
            ( sender as Button ).IsEnabled = false;


            _work.Add( Tuple.Create( "Start",  DateTime.Parse( startTime.Text ) ) );
         

            output.AppendNewLine( "Starting at "  + _work[0].Item2);



            _doNotUpdate = true;
            startTime.IsEnabled = false;
            list.IsEnabled = true;
        }

        private void ButtonBase_OnClick( object sender, RoutedEventArgs e )
        {
            _work.Add( Tuple.Create( (sender as Button).Content.ToString(), DateTime.Now) );

            _addLast();
            
        }

        private void _addLast()
        {
            // Get last and previous
            var oneBeforeLast = _work[ _work.Count - 2 ];
            var last = _work.Last();
            output.AppendNewLine( "{0} to {1}, total {2}", last.Item1, last.Item2, (last.Item2-oneBeforeLast.Item2).Duration().ToString(@"hh\:mm\:ss") );
        }
    }

    public static class Extensions
    {
        public static void AppendNewLine( this TextBoxBase tb, string text, params object[] args )
        {
            tb.AppendText( string.Format( text + Environment.NewLine, args ) );
        }
    }
}
