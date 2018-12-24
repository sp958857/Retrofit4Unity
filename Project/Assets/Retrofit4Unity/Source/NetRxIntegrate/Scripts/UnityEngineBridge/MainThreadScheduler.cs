using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using UnityEngine;

namespace NetRxIntegrate
{


    public static partial class SchedulerUnity
    {
     
        static IScheduler mainThread;

        /// <summary>
        /// Unity native MainThread Queue Scheduler. Run on mainthread and delayed on coroutine update loop, elapsed time is calculated based on Time.time.
        /// </summary>
        public static IScheduler MainThread
        {
            get
            {
                return mainThread ?? (mainThread = new MainThreadScheduler());
            }
        }

        static IScheduler mainThreadIgnoreTimeScale;

        /// <summary>
        /// Another MainThread scheduler, delay elapsed time is calculated based on Time.unscaledDeltaTime.
        /// </summary>
        public static IScheduler MainThreadIgnoreTimeScale
        {
            get
            {
                return mainThreadIgnoreTimeScale ?? (mainThreadIgnoreTimeScale = new IgnoreTimeScaleMainThreadScheduler());
            }
        }

        static IScheduler mainThreadFixedUpdate;

        /// <summary>
        /// Run on fixed update mainthread, delay elapsed time is calculated based on Time.fixedTime.
        /// </summary>
        public static IScheduler MainThreadFixedUpdate
        {
            get
            {
                return mainThreadFixedUpdate ?? (mainThreadFixedUpdate = new FixedUpdateMainThreadScheduler());
            }
        }

        static IScheduler mainThreadEndOfFrame;

        /// <summary>
        /// Run on end of frame mainthread, delay elapsed time is calculated based on Time.deltaTime.
        /// </summary>
        public static IScheduler MainThreadEndOfFrame
        {
            get
            {
                return mainThreadEndOfFrame ?? (mainThreadEndOfFrame = new EndOfFrameMainThreadScheduler());
            }
        }

        class MainThreadScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
        {
            readonly Action<object> scheduleAction;

            public MainThreadScheduler()
            {
                MainThreadDispatcher.Initialize();
                scheduleAction = new Action<object>(Schedule);
            }

            // delay action is run in StartCoroutine
            // Okay to action run synchronous and guaranteed run on MainThread
            IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
                // zero == every frame
                if (dueTime == TimeSpan.Zero)
                {
                    yield return null; // not immediately, run next frame
                }
                else
                {
                    yield return new WaitForSeconds((float)dueTime.TotalSeconds);
                }

                if (cancellation.IsDisposed) yield break;
                MainThreadDispatcher.UnsafeSend(action);
            }

            IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
            {
                // zero == every frame
                if (period == TimeSpan.Zero)
                {
                    while (true)
                    {
                        yield return null; // not immediately, run next frame
                        if (cancellation.IsDisposed) yield break;

                        MainThreadDispatcher.UnsafeSend(action);
                    }
                }
                else
                {
                    var seconds = (float)(period.TotalMilliseconds / 1000.0);
                    var yieldInstruction = new WaitForSeconds(seconds); // cache single instruction object

                    while (true)
                    {
                        yield return yieldInstruction;
                        if (cancellation.IsDisposed) yield break;

                        MainThreadDispatcher.UnsafeSend(action);
                    }
                }
            }

            public DateTimeOffset Now
            {
                get { return DateTimeOffset.UtcNow; }
            }

            void Schedule(object state)
            {
                var t = (Tuple<BooleanDisposable, Action>)state;
                if (!t.Item1.IsDisposed)
                {
                    t.Item2();
                }
            }
            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
            {
                if (action == null)
                    throw new ArgumentNullException("action");
                var d = new BooleanDisposable();
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.Post(scheduleAction, Tuple.Create(d, a));
                return d;
            }

            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                var d = new BooleanDisposable();
                var time = Utils.Normalize(dueTime);
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.SendStartCoroutine(DelayAction(time, a, d));

                return d;
            }

            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                return Schedule(state,dueTime - Now, action);

            }

            public IDisposable SchedulePeriodic<TState>(TState state, TimeSpan period, Func<TState, TState> action)
            {
                var d = new BooleanDisposable();
                var time = Utils.Normalize(period);
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.SendStartCoroutine(PeriodicAction(time, a, d));

                return d;
            }

            void ScheduleQueueing<T>(object state)
            {
                var t = (Tuple<ICancelable, T, Action<T>>)state;
                if (!t.Item1.IsDisposed)
                {
                    t.Item3(t.Item2);
                }
            }

            public void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
            {
                MainThreadDispatcher.Post(QueuedAction<T>.Instance, Tuple.Create(cancel, state, action));
            }

            
            static class QueuedAction<T>
            {
                public static readonly Action<object> Instance = new Action<object>(Invoke);

                public static void Invoke(object state)
                {
                    var t = (Tuple<ICancelable, T, Action<T>>)state;

                    if (!t.Item1.IsDisposed)
                    {
                        t.Item3(t.Item2);
                    }
                }
            }
        }
     
        class IgnoreTimeScaleMainThreadScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
        {
            readonly Action<object> scheduleAction;

            public IgnoreTimeScaleMainThreadScheduler()
            {
                MainThreadDispatcher.Initialize();
                scheduleAction = new Action<object>(Schedule);
            }

            IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
                if (dueTime == TimeSpan.Zero)
                {
                    yield return null;
                    if (cancellation.IsDisposed) yield break;

                    MainThreadDispatcher.UnsafeSend(action);
                }
                else
                {
                    var elapsed = 0f;
                    var dt = (float)dueTime.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        elapsed += Time.unscaledDeltaTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            break;
                        }
                    }
                }
            }

            IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
            {
                // zero == every frame
                if (period == TimeSpan.Zero)
                {
                    while (true)
                    {
                        yield return null; // not immediately, run next frame
                        if (cancellation.IsDisposed) yield break;

                        MainThreadDispatcher.UnsafeSend(action);
                    }
                }
                else
                {
                    var elapsed = 0f;
                    var dt = (float)period.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        elapsed += Time.unscaledDeltaTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            elapsed = 0;
                        }
                    }
                }
            }

            public DateTimeOffset Now
            {
                get { return DateTimeOffset.UtcNow; }
            }
           
            void Schedule(object state)
            {
                var t = (Tuple<BooleanDisposable, Action>)state;
                if (!t.Item1.IsDisposed)
                {
                    t.Item2();
                }
            }
            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
            {
                if (action == null)
                    throw new ArgumentNullException("action");
                var d = new BooleanDisposable();
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.Post(scheduleAction, Tuple.Create(d, a));
                return d;
            }

            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                var d = new BooleanDisposable();
                var time = Utils.Normalize(dueTime);
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.SendStartCoroutine(DelayAction(time, a, d));

                return d;
            }

            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                return Schedule(state,dueTime - Now, action);
            }

            public IDisposable SchedulePeriodic<TState>(TState state, TimeSpan period, Func<TState, TState> action)
            {
                var d = new BooleanDisposable();
                var time = Utils.Normalize(period);
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.SendStartCoroutine(PeriodicAction(time, a, d));

                return d;
            }

            public void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
            {
                MainThreadDispatcher.Post(QueuedAction<T>.Instance, Tuple.Create(cancel, state, action));
            }

           

            static class QueuedAction<T>
            {
                public static readonly Action<object> Instance = new Action<object>(Invoke);

                public static void Invoke(object state)
                {
                    var t = (Tuple<ICancelable, T, Action<T>>)state;

                    if (!t.Item1.IsDisposed)
                    {
                        t.Item3(t.Item2);
                    }
                }
            }
        }

        class FixedUpdateMainThreadScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
        {
            public FixedUpdateMainThreadScheduler()
            {
                MainThreadDispatcher.Initialize();
            }

            IEnumerator ImmediateAction<T>(T state, Action<T> action, ICancelable cancellation)
            {
                yield return null;
                if (cancellation.IsDisposed) yield break;

                MainThreadDispatcher.UnsafeSend(action, state);
            }

            IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
                if (dueTime == TimeSpan.Zero)
                {
                    yield return null;
                    if (cancellation.IsDisposed) yield break;

                    MainThreadDispatcher.UnsafeSend(action);
                }
                else
                {
                    var startTime = Time.fixedTime;
                    var dt = (float)dueTime.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        var elapsed = Time.fixedTime - startTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            break;
                        }
                    }
                }
            }

            IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
            {
                // zero == every frame
                if (period == TimeSpan.Zero)
                {
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) yield break;

                        MainThreadDispatcher.UnsafeSend(action);
                    }
                }
                else
                {
                    var startTime = Time.fixedTime;
                    var dt = (float)period.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        var ft = Time.fixedTime;
                        var elapsed = ft - startTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            startTime = ft;
                        }
                    }
                }
            }

            public DateTimeOffset Now
            {
                get { return DateTimeOffset.UtcNow; }
            }
            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
            {
                return Schedule(state,TimeSpan.Zero, action);

            }

            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                var d = new BooleanDisposable();
                var time = Utils.Normalize(dueTime);
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.StartFixedUpdateMicroCoroutine(DelayAction(time, a, d));

                return d;
            }

            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                return Schedule(state,dueTime - Now, action);

            }

            public IDisposable SchedulePeriodic<TState>(TState state, TimeSpan period, Func<TState, TState> action)
            {
                var d = new BooleanDisposable();
                var time = Utils.Normalize(period);
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.StartFixedUpdateMicroCoroutine(PeriodicAction(time, a, d));

                return d;
            }

            public void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
            {
                MainThreadDispatcher.StartFixedUpdateMicroCoroutine(ImmediateAction(state, action, cancel));
            }

           
        }

        class EndOfFrameMainThreadScheduler : IScheduler, ISchedulerPeriodic, ISchedulerQueueing
        {
            public EndOfFrameMainThreadScheduler()
            {
                MainThreadDispatcher.Initialize();
            }

            IEnumerator ImmediateAction<T>(T state, Action<T> action, ICancelable cancellation)
            {
                yield return null;
                if (cancellation.IsDisposed) yield break;

                MainThreadDispatcher.UnsafeSend(action, state);
            }

            IEnumerator DelayAction(TimeSpan dueTime, Action action, ICancelable cancellation)
            {
                if (dueTime == TimeSpan.Zero)
                {
                    yield return null;
                    if (cancellation.IsDisposed) yield break;

                    MainThreadDispatcher.UnsafeSend(action);
                }
                else
                {
                    var elapsed = 0f;
                    var dt = (float)dueTime.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;

                        elapsed += Time.deltaTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            break;
                        }
                    }
                }
            }

            IEnumerator PeriodicAction(TimeSpan period, Action action, ICancelable cancellation)
            {
                // zero == every frame
                if (period == TimeSpan.Zero)
                {
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) yield break;

                        MainThreadDispatcher.UnsafeSend(action);
                    }
                }
                else
                {
                    var elapsed = 0f;
                    var dt = (float)period.TotalSeconds;
                    while (true)
                    {
                        yield return null;
                        if (cancellation.IsDisposed) break;
                        
                        elapsed += Time.deltaTime;
                        if (elapsed >= dt)
                        {
                            MainThreadDispatcher.UnsafeSend(action);
                            elapsed = 0;
                        }
                    }
                }
            }

            public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
            {
                return Schedule(state,TimeSpan.Zero, action);
            }

            public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                var d = new BooleanDisposable();
                var time = Utils.Normalize(dueTime);
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(DelayAction(time, a, d));

                return d;
            }

            public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
            {
                return Schedule(state,dueTime - Now, action);

            }
            public IDisposable SchedulePeriodic<TState>(TState state, TimeSpan period, Func<TState, TState> action)
            {
                var d = new BooleanDisposable();
                var time = Utils.Normalize(period);
                Action a = Utils.IgnoreResult(action, (IScheduler)this, state);
                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(PeriodicAction(time, a, d));

                return d;
            }
            public DateTimeOffset Now
            {
                get { return DateTimeOffset.UtcNow; }
            }

            public void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
            {
                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(ImmediateAction(state, action, cancel));
            }

        
        }
    }
}