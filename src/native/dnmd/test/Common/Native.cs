using System.Runtime.InteropServices;
using System.Text;

using Xunit;

namespace Common
{
    public static class Native
    {
        public static string Path => OperatingSystem.IsWindows() ? "regnative.dll"
            : OperatingSystem.IsMacOS() ? "libregnative.dylib"
            : "libregnative.so";
    }

    public enum TestState
    {
        Fail,
        Pass
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TestResult
    {
        public TestState State;
        public byte* FailureMessage;
        public delegate* unmanaged<void*, void> Free;

        public void Check()
        {
            if (State != TestState.Pass)
            {
                Assert.True(FailureMessage != null);
                Assert.True(Free != null);
                string msg = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(FailureMessage));
                Free(FailureMessage);
                Assert.Fail(msg);
            }
        }
    }
}