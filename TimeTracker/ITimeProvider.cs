using System;

namespace TimeTracker
{
    public interface ITimeProvider
    {
        DateTime GetCurrentTime();
    }
}