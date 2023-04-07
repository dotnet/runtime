// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------
namespace System
{
    public abstract class TimeProvider
    {
        public static TimeProvider System { get; }
        protected TimeProvider(long timestampFrequency) { throw null; }
        public abstract System.DateTimeOffset UtcNow { get; }
        public System.DateTimeOffset LocalNow { get; }
        public abstract System.TimeZoneInfo LocalTimeZone { get; }
        public long TimestampFrequency { get; }
        public static TimeProvider FromLocalTimeZone(System.TimeZoneInfo timeZone) { throw null; }
        public abstract long GetTimestamp();
        public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) { throw null; }
        public abstract System.Threading.ITimer CreateTimer(System.Threading.TimerCallback callback, object? state, System.TimeSpan dueTime, System.TimeSpan period);
    }
}
namespace System.Threading
{
    public interface ITimer : System.IDisposable, System.IAsyncDisposable
    {
        bool Change(System.TimeSpan dueTime, System.TimeSpan period);
    }
}
