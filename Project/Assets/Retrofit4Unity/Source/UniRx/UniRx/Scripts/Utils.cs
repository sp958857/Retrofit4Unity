using System;

namespace UniRx
{
    internal static class Stubs<T>
    {
        public static readonly Action<T> Ignore = (T t) => { };
        public static readonly Func<T, T> Identity = (T t) => t;
        public static readonly Action<Exception, T> Throw = (ex, _) => { throw ex; };
    }

    internal static class Stubs<T1, T2>
    {
        public static readonly Action<T1, T2> Ignore = (x, y) => { };
        public static readonly Action<Exception, T1, T2> Throw = (ex, _, __) => { throw ex; };
    }


    internal static class Stubs<T1, T2, T3>
    {
        public static readonly Action<T1, T2, T3> Ignore = (x, y, z) => { };
        public static readonly Action<Exception, T1, T2, T3> Throw = (ex, _, __, ___) => { throw ex; };
    }
    public static class Utils
    {
        public static Action IgnoreResult(Delegate d, params object[] arguments)
        {
            return () => d.DynamicInvoke(arguments);
        }
        public static TimeSpan Normalize(TimeSpan timeSpan)
        {
            return timeSpan >= TimeSpan.Zero ? timeSpan : TimeSpan.Zero;
        }
    }
}