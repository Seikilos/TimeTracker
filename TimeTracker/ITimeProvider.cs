using System;
using System.Windows.Controls;

namespace TimeTracker
{
    public interface ITimeProvider
    {
        DateTime GetCurrentTime();

        void SetCurrentStartTime(TextBox starttimeBox);
    }
}