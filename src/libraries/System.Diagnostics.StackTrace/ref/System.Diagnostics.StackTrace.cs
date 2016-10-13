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
        public int GetFileColumnNumber() { throw null; }
        public int GetFileLineNumber() { throw null; }
        public string GetFileName() { throw null; }
        public int GetILOffset() { throw null; }
        public System.Reflection.MethodBase GetMethod() { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class StackFrameExtensions
    {
        public static System.IntPtr GetNativeImageBase(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static System.IntPtr GetNativeIP(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static bool HasILOffset(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static bool HasMethod(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static bool HasNativeImage(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static bool HasSource(this System.Diagnostics.StackFrame stackFrame) { throw null; }
    }
    public sealed partial class StackTrace
    {
        public StackTrace(System.Exception exception, bool needFileInfo) { }
        public System.Diagnostics.StackFrame[] GetFrames() { throw null; }
        public override string ToString() { throw null; }
    }
}
