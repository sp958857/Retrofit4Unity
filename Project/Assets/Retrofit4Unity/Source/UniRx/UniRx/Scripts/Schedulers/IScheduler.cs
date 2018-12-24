using System;
using System.Reactive.Disposables;

namespace UniRx
{
    public interface ISchedulerQueueing
    {
        void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action);
    }
}