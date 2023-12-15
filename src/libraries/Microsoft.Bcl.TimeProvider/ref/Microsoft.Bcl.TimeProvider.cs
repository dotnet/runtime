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
        protected TimeProvider() { throw null; }
        public virtual System.DateTimeOffset GetUtcNow() { throw null; }
        public System.DateTimeOffset GetLocalNow() { throw null; }
        public virtual System.TimeZoneInfo LocalTimeZone { get; }
        public virtual long TimestampFrequency { get; }
        public virtual long GetTimestamp() { throw null; }
        public TimeSpan GetElapsedTime(long startingTimestamp) { throw null; }
        public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) { throw null; }
        public virtual System.Threading.ITimer CreateTimer(System.Threading.TimerCallback callback, object? state, System.TimeSpan dueTime, System.TimeSpan period) { throw null; }
    }
}
namespace System.Threading
{
    public interface ITimer : System.IDisposable, System.IAsyncDisposable
    {
        bool Change(System.TimeSpan dueTime, System.TimeSpan period);
    }
}
