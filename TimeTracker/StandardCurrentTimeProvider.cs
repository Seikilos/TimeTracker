using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTracker
{
    public class StandardCurrentTimeProvider : ITimeProvider
    {
        public DateTime GetCurrentTime()
        {
            return DateTime.Now;
        }
    }
}
