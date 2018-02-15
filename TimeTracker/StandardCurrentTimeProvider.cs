using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TimeTracker
{
    public class StandardCurrentTimeProvider : ITimeProvider
    {
        public DateTime GetCurrentTime()
        {
            return DateTime.Now;
        }

        public void SetCurrentStartTime( TextBox starttimeBox )
        {
            
            throw new NotImplementedException();
        }
    }
}
