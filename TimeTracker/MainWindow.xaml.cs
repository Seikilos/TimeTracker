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
        private readonly ITimeProvider _provider;
        private Task _updater;
        private bool _doNotUpdate;

        public ObservableCollection<string> Cats { get; set; }

        private List<Tuple<string, DateTime>> _work = new List< Tuple< string, DateTime > >();

        public DateTime StartDate;
        private List< Tuple< DateTime, DateTime > > _breaks;

        public MainWindow(ITimeProvider provider)
        {
            _provider = provider;
            DataContext = this;
            var doc = XDocument.Load( "config.xml" );
            _breaks = doc.XPathSelectElements( "//Break" ).Select( e => Tuple.Create(
                DateTime.Parse( e.Attribute( "Start" ).Value ),
                DateTime.Parse( e.Attribute( "End" ).Value ) ) ).ToList();
            Cats = new ObservableCollection< string >(doc.XPathSelectElements( "//Category" ).Select( e => e.Value ).ToList());

            for ( var i = 0; i < 10; ++i )
            {
                Cats.Add( "Project "+i );
            }

         


            InitializeComponent();

               foreach ( var b in _breaks )
            {
                output.AppendNewLine( string.Format( "Break from {0} ended at {1}", b.Item1, b.Item2 ) );
            }


            list.IsEnabled = false;


            // Disable updater for now, fake time
            {
                startTime.Text = "8:00";
                _doNotUpdate = true;
            }

            //_doNotUpdate = false;
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
            _work.Add( Tuple.Create( (sender as Button).Content.ToString(), _provider.GetCurrentTime()) );

            _addLast();
            
        }

        private void _addLast()
        {
            // Get last and previous
            var oneBeforeLast = _work[ _work.Count - 2 ];
            var last = _work.Last();

            var ts = ( last.Item2 - oneBeforeLast.Item2 ).Duration();

            var hadBreak = _hadBreak( oneBeforeLast.Item2, last.Item2 );

          
            ts = ts.Subtract( hadBreak );
            

            output.AppendNewLine( "{0} to {1}, total {2}{3}", last.Item1, last.Item2,ts.ToString(@"hh\:mm\:ss"), (hadBreak != TimeSpan.Zero? string.Format(", subtracted {0} break", hadBreak.ToString(@"hh\:mm\:ss")):"") );
        }

        private TimeSpan _hadBreak( DateTime startTime, DateTime endTime )
        {
            
            var t = new TimeSpan();

            

            foreach ( var bTuple in _breaks )
            {

                var breakSpan = ( bTuple.Item2 - bTuple.Item1 ).Duration();

                // Skip if endtime is before start or startTime is after end
                if ( endTime <= bTuple.Item1 || startTime >= bTuple.Item2 )
                {
                    continue;
                }

                // Conditions: Did startDate occur before or after the start of the break
                var startedBefore = startTime < bTuple.Item1;
                var endedAfter = endTime > bTuple.Item2;

                if ( startedBefore && endedAfter )
                {
                    // Subtract full timespan
                    t = t.Add( breakSpan );

                }
                else if( startedBefore) // implies && !endedAfter)
                {
                    t = t.Add( ( endTime - bTuple.Item1 ).Duration() );

                }
                else if ( endedAfter ) // implies !startedBefore
                {
                    t = t.Add( ( bTuple.Item2 - startTime ).Duration() );
                    
                }
                else // implies ( !startedBefore && !endedAfter )
                {
                    t = t.Add( ( endTime - startTime ).Duration() );
                }


               
            }

            return t;

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
