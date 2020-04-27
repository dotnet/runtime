using System.Diagnostics;
using System.Net.Sockets;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Helper class for managing timestamps.
    /// </summary>
    internal static class Timestamp
    {
        private static readonly long TicksPerMillisecond = Stopwatch.Frequency / 1000;
        public static long Now => Stopwatch.GetTimestamp();

        public static long FromMilliseconds(long milliseconds) => TicksPerMillisecond * milliseconds;
        public static long FromMicroseconds(long microseconds) => TicksPerMillisecond * microseconds / 1000;

        public static long GetMilliseconds(long ticks) => ticks / TicksPerMillisecond;
        public static long GetMicroseconds(long ticks) => ticks * 1000 / TicksPerMillisecond;
    }
}
