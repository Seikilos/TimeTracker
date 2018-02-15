using System;
using System.Windows.Controls;

namespace TimeTracker
{
    public class DummyCurrentTimeProvider : ITimeProvider
    {
        private DateTime _start;

        public DummyCurrentTimeProvider()
        {
            _start = DateTime.Today;
            _start = _start.AddHours( 9 );
        }

        public DateTime GetCurrentTime()
        {
            var copy = _start;

            _start = _start.AddHours( 1 );

            return copy;
        }

        public void SetCurrentStartTime( TextBox starttimeBox )
        {
            
        }
    }
}