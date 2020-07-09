namespace JitBench
{
    public enum ProcessStream
    {
        StandardIn = 0,
        StandardOut = 1,
        StandardError = 2,
        MaxStreams = 3
    }

    public enum KillReason
    {
        TimedOut,
        Unknown
    }

    public interface IProcessLogger
    {
        void ProcessExited(ProcessRunner runner);
        void ProcessKilled(ProcessRunner runner, KillReason reason);
        void ProcessStarted(ProcessRunner runner);
        void Write(ProcessRunner runner, string data, ProcessStream stream);
        void WriteLine(ProcessRunner runner, string data, ProcessStream stream);
    }
}
