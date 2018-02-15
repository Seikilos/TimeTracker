using System;
using System.Windows.Controls;

namespace TimeTracker
{
    public class TimeFromTextProvider : ITimeProvider
    {
        public TextBox TextBox { get; set; }

        public DateTime GetCurrentTime()
        {
            return DateTime.Parse(TextBox.Text);
        }

        public void SetCurrentStartTime( TextBox starttimeBox )
        {
            TextBox = starttimeBox;
        }
    }
}
