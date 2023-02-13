// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: Types/members which are not publicly exposed in System.Runtime.dll but still used internally by libraries.
//       Manually maintained, keep in sync with System.Private.CoreLib.ExtraApis.txt

namespace System.Runtime.Serialization
{
    public readonly partial struct DeserializationToken : System.IDisposable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        internal DeserializationToken(object tracker) { }
        public void Dispose() { }
    }
    public sealed partial class SerializationInfo
    {
        public static System.Runtime.Serialization.DeserializationToken StartDeserialization() { throw null; }
    }
}
namespace System.Diagnostics
{
    public partial class DebugProvider
    {
        public DebugProvider() { }
        [System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute]
        public virtual void Fail(string? message, string? detailMessage) { throw null; }
        public static void FailCore(string stackTrace, string? message, string? detailMessage, string errorSource) { }
        public virtual void OnIndentLevelChanged(int indentLevel) { }
        public virtual void OnIndentSizeChanged(int indentSize) { }
        public virtual void Write(string? message) { }
        public static void WriteCore(string message) { }
        public virtual void WriteLine(string? message) { }
    }
    public static partial class Debug
    {
        public static System.Diagnostics.DebugProvider SetProvider(System.Diagnostics.DebugProvider provider) { throw null; }
    }
}
namespace System.Runtime.Intrinsics.X86
{
    public abstract partial class X86Base
    {
        public abstract partial class X64
        {
            public static (ulong Quotient, ulong Remainder) DivRem(ulong lower, ulong upper, ulong divisor) { throw null; }
            public static (long Quotient, long Remainder) DivRem(ulong lower, long upper, long divisor) { throw null; }
        }

        public static (uint Quotient, uint Remainder) DivRem(uint lower, uint upper, uint divisor) { throw null; }
        public static (int Quotient, int Remainder) DivRem(uint lower, int upper, int divisor) { throw null; }
        public static (nuint Quotient, nuint Remainder) DivRem(nuint lower, nuint upper, nuint divisor) { throw null; }
        public static (nint Quotient, nint Remainder) DivRem(nuint lower, nint upper, nint divisor) { throw null; }
    }
}