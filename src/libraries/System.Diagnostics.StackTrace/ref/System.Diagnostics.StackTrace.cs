// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Diagnostics
{
    public sealed partial class StackFrame
    {
        internal StackFrame() { }
        public const int OFFSET_UNKNOWN = -1;
        public int GetFileColumnNumber() { return default(int); }
        public int GetFileLineNumber() { return default(int); }
        public string GetFileName() { return default(string); }
        public int GetILOffset() { return default(int); }
        public System.Reflection.MethodBase GetMethod() { return default(System.Reflection.MethodBase); }
        public override string ToString() { return default(string); }
    }
    public static partial class StackFrameExtensions
    {
        public static System.IntPtr GetNativeImageBase(this System.Diagnostics.StackFrame stackFrame) { return default(System.IntPtr); }
        public static System.IntPtr GetNativeIP(this System.Diagnostics.StackFrame stackFrame) { return default(System.IntPtr); }
        public static bool HasILOffset(this System.Diagnostics.StackFrame stackFrame) { return default(bool); }
        public static bool HasMethod(this System.Diagnostics.StackFrame stackFrame) { return default(bool); }
        public static bool HasNativeImage(this System.Diagnostics.StackFrame stackFrame) { return default(bool); }
        public static bool HasSource(this System.Diagnostics.StackFrame stackFrame) { return default(bool); }
    }
    public sealed partial class StackTrace
    {
        public StackTrace(System.Exception exception, bool needFileInfo) { }
        public System.Diagnostics.StackFrame[] GetFrames() { return default(System.Diagnostics.StackFrame[]); }
        public override string ToString() { return default(string); }
    }
}
