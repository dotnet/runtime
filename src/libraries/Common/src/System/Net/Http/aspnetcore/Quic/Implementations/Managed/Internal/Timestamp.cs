using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Helper class for managing timestamps.
    /// </summary>
    internal static class Timestamp
    {
        public static long Now => Stopwatch.GetTimestamp();

        public static long FromMilliseconds(long milliseconds) => TimeSpan.TicksPerMillisecond * milliseconds;
        public static long FromMicroseconds(long microseconds) => TimeSpan.TicksPerMillisecond * microseconds / 1000;

        public static long GetMilliseconds(long ticks) => ticks / TimeSpan.TicksPerMillisecond;
        public static long GetMicroseconds(long ticks) => ticks * 1000 / TimeSpan.TicksPerMillisecond;
    }
}
