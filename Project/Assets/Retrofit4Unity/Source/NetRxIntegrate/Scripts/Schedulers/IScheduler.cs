using System;
using System.Reactive.Disposables;

namespace NetRxIntegrate
{
    public interface ISchedulerQueueing
    {
        void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action);
    }
}