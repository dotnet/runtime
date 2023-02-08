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
// namespace System.Runtime.Intrinsics.Arm
// {
    // public abstract partial class AdvSimd : ArmBase
    // {
        // public static System.Runtime.Intrinsics.Vector64<byte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>) table, System.Runtime.Intrinsics.Vector64<byte> byteIndexes) { throw null; }
        // public static System.Runtime.Intrinsics.Vector64<sbyte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>) table, System.Runtime.Intrinsics.Vector64<sbyte> byteIndexes) { throw null; }
        // public static System.Runtime.Intrinsics.Vector64<byte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>) table, System.Runtime.Intrinsics.Vector64<byte> byteIndexes) { throw null; }
        // public static System.Runtime.Intrinsics.Vector64<sbyte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>) table, System.Runtime.Intrinsics.Vector64<sbyte> byteIndexes) { throw null; }
        // public static System.Runtime.Intrinsics.Vector64<byte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>) table, System.Runtime.Intrinsics.Vector64<byte> byteIndexes) { throw null; }
        // public static System.Runtime.Intrinsics.Vector64<sbyte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>) table, System.Runtime.Intrinsics.Vector64<sbyte> byteIndexes) { throw null; }

        // public abstract partial class Arm64 : ArmBase.Arm64
        // {
            // public static System.Runtime.Intrinsics.Vector128<byte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>) table, System.Runtime.Intrinsics.Vector128<byte> byteIndexes) { throw null; }
            // public static System.Runtime.Intrinsics.Vector128<sbyte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>) table, System.Runtime.Intrinsics.Vector128<sbyte> byteIndexes) { throw null; }
            // public static System.Runtime.Intrinsics.Vector128<byte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>) table, System.Runtime.Intrinsics.Vector128<byte> byteIndexes) { throw null; }
            // public static System.Runtime.Intrinsics.Vector128<sbyte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>) table, System.Runtime.Intrinsics.Vector128<sbyte> byteIndexes) { throw null; }
            // public static System.Runtime.Intrinsics.Vector128<byte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>, System.Runtime.Intrinsics.Vector128<byte>) table, System.Runtime.Intrinsics.Vector128<byte> byteIndexes) { throw null; }
            // public static System.Runtime.Intrinsics.Vector128<sbyte> VectorTableLookup((System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>, System.Runtime.Intrinsics.Vector128<sbyte>) table, System.Runtime.Intrinsics.Vector128<sbyte> byteIndexes) { throw null; }
        // }
    // }
// }
