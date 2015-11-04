using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

using Path = System.IO.Path;

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

        private List< Tuple< string, DateTime > > _work;

        public DateTime StartDate;
        

        /// <summary>
        /// Keeps xml nodes which are transformed to date to ensure date time is always 
        /// the proper day so that the app can be used continuously
        /// </summary>
        private List< XElement > _breakElements; 

        public ICommand LogTimeCommand { get; set; }

        private FileStream _progressFile;
        private StreamWriter _progressFileStream;
        private FileStream _summaryFile;
        private StreamWriter _summaryFileStream;

        public MainWindow(ITimeProvider provider)
        {
            LogTimeCommand = new DelegateCommand( ActionLogTime );
            _provider = provider;
            DataContext = this;

            Directory.CreateDirectory( "Work" );

            var doc = XDocument.Load( "config.xml" );
            _breakElements = doc.XPathSelectElements("//Break").ToList();

         
            var catList = doc.XPathSelectElements("//Category").Select(e => e.Value).ToList();
            Cats = new ObservableCollection< string >(catList);
            
#if DEBUG

            for ( var i = 0; i < 10; ++i )
            {
                Cats.Add( "Project "+i );
            }
#endif



            InitializeComponent();


            _dumpBreaks();


            list.IsEnabled = false;


            // Disable updater for now, fake time
           /* {
                startTime.Text = "8:00";
                _doNotUpdate = true;
            }*/

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

        /// <summary>
        /// Performs a non-cached conversion from breaks to current date to avoid stalled dates for next days
        /// </summary>
        /// <returns></returns>
        private List<Tuple<DateTime, DateTime> > _getBreaks()
        {
            return _breakElements.Select( e => Tuple.Create(
              DateTime.Parse( e.Attribute( "Start" ).Value ),
              DateTime.Parse( e.Attribute( "End" ).Value ) ) ).ToList();

        }

        private void _dumpBreaks()
        {
            foreach ( var b in _getBreaks() )
            {
                output(string.Format("Break from {0} ended at {1}", b.Item1, b.Item2));
            }
        }

        private void output(string format, params object[] args)
        {
            _output.AppendNewLine(format, args);
            if ( _progressFileStream != null )
            {
                _progressFileStream.WriteLine( format, args );
                _progressFileStream.Flush();
            }

        }
        private void summary( string format, params object[] args )
        {
            _output.AppendNewLine( format, args );
            if ( _summaryFileStream != null )
            {
                _summaryFileStream.WriteLine( format, args );
                _summaryFileStream.Flush();
            }
        }

        private void summaryDivide(  )
        {

            _output.Divide();
            _summaryFileStream.WriteLine( "----------------------" );
        }

        private void outputDivide(  )
        {
            _output.Divide();
            if(_progressFileStream != null)
            { 
                _progressFileStream.WriteLine( "----------------------" );
            }
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

        private void Start_Button_Click( object sender, RoutedEventArgs e )
        {
            ( sender as Button ).IsEnabled = false;
            AddButton.IsEnabled = true;

            _progressFile = new FileStream( Path.Combine("Work", DateTime.Parse( startTime.Text ).ToString( "'Work'_yyyy_MM_dd_HH_mm_ss'.log'" )), FileMode.Create);
            _progressFileStream = new StreamWriter(_progressFile);
            _summaryFile = new FileStream( Path.Combine("Work", DateTime.Parse( startTime.Text ).ToString( "'Summary'_yyyy_MM_dd_HH_mm_ss'.log'" )), FileMode.Create );
            _summaryFileStream = new StreamWriter(_summaryFile);

            output( "Work logged to {0}", _progressFile.Name  );
            output( "Summary logged to {0}", _summaryFile.Name  );


            _work = new List< Tuple< string, DateTime > >();
            _work.Add( Tuple.Create( "Start",  DateTime.Parse( startTime.Text ) ) );
         

            output( "Starting at "  + _work[0].Item2);

            SummaryButton.IsEnabled = true;



            _doNotUpdate = true;
            startTime.IsEnabled = false;
            list.IsEnabled = true;
            StopButton.IsEnabled = true;
        }

        private void ActionLogTime( object workName )
        {
            _work.Add( Tuple.Create( workName.ToString(), _provider.GetCurrentTime()) );

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
            

            output( "{0} to {1}, total {2}{3}", last.Item1, last.Item2,ts.ToString(@"hh\:mm\:ss"), (hadBreak != TimeSpan.Zero? string.Format(", subtracted {0} break", hadBreak.ToString(@"hh\:mm\:ss")):"") );
        }

        private TimeSpan _hadBreak( DateTime startTime, DateTime endTime )
        {
            
            var t = new TimeSpan();

            

            foreach ( var bTuple in _getBreaks() )
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

        private void Button_Click_Summary( object sender, RoutedEventArgs e )
        {
            if ( _work.Any() == false )
            {
                summary( "You did not work yet" );
                return;
            }

            summaryDivide();
            summary( "Start at {0}", _work.First().Item2 );

            if ( _work.Count < 2 )
            {
                summary( "You did not log any work yet" );
            }

            var workTime = _hadBreak( _work.First().Item2, _work.Last().Item2 );

            summary( "Ended at {0}, worked {1}", _work.Last().Item2, (_work.Last().Item2-_work.First().Item2 ).Duration() - workTime);

            var jobs = new Dictionary< string, TimeSpan >();

            for ( var i = 1; i < _work.Count; ++i )
            {
                var before = _work[ i - 1 ];
                var current = _work[ i ];

                if ( jobs.ContainsKey( current.Item1 ) == false )
                {
                    jobs[current.Item1] = TimeSpan.Zero;
                }

                var breaks = _hadBreak( before.Item2, current.Item2 );

                var dur = ( current.Item2 - before.Item2 ).Duration() - breaks;

                jobs[ current.Item1 ] = jobs[ current.Item1 ].Add( dur );


            }

            foreach ( var job in jobs )
            {
                summary( "{0} for {1}", job.Key, job.Value );
            }


           summaryDivide();

        }

        private void Add_And_Bill_Click( object sender, RoutedEventArgs e )
        {
            Cats.Add( newJob.Text );
          
            ActionLogTime( newJob.Text );

            list.SelectedIndex = list.Items.Count - 1;
            list.ScrollIntoView( list.SelectedItem );
            newJob.Text = "";

        }

        private void Real_Exit( object sender, RoutedEventArgs e )
        {

            var res = MessageBox.Show( this, "Are you sure?", "Really exit?", MessageBoxButton.YesNo );
 
            if ( res == MessageBoxResult.No )
            {
                return;
            }
            Application.Current.Shutdown( 0 );


        }

        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            e.Cancel = true;
            Hide();
        }

        private void Open_Click( object sender, RoutedEventArgs e )
        {
           Show();
        }

        private void myNotifyIcon_TrayMouseDoubleClick( object sender, RoutedEventArgs e )
        {
            Show();
            Activate();
        }

        private void Stop_Click( object sender, RoutedEventArgs e )
        {

            Button_Click_Summary(this, e);
            _progressFileStream.Dispose();
            _summaryFile.Dispose();
            _progressFileStream = null;
            _summaryFile = null;


            output("Stopped");
            outputDivide();
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            SummaryButton.IsEnabled = false;

            _doNotUpdate = false;
            startTime.IsEnabled = true;
            list.IsEnabled = false;
            

        }

        private void Button_Click_Clear( object sender, RoutedEventArgs e )
        {
            _output.Clear();
        }
    }

    public static class Extensions
    {
        public static void AppendNewLine( this TextBoxBase tb, string text, params object[] args )
        {
            tb.AppendText( string.Format( text + Environment.NewLine, args ) );

            tb.ScrollToEnd();

            
        }

        public static void Divide( this TextBoxBase tb )
        {
            tb.AppendNewLine( "--------------------------" );
        }
    }
}
