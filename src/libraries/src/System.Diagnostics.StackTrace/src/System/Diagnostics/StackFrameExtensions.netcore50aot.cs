using System;

namespace System.Diagnostics
{
    public static partial class StackFrameExtensions
    {
        public static IntPtr GetNativeIP(this StackFrame stackFrame)
        {
            return stackFrame.GetNativeIP();
        }

        public static IntPtr GetNativeImageBase(this StackFrame stackFrame)
        {
            return stackFrame.GetNativeImageBase();
        }
    }
}
