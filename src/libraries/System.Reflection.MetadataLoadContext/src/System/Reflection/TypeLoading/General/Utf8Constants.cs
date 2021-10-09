// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.TypeLoading
{
    internal static class Utf8Constants
    {
        public static ReadOnlySpan<byte> System => new byte[] { 83, 121, 115, 116, 101, 109 };
        public static ReadOnlySpan<byte> SystemReflection => new byte[] { 83, 121, 115, 116, 101, 109, 46, 82, 101, 102, 108, 101, 99, 116, 105, 111, 110 };
        public static ReadOnlySpan<byte> SystemCollectionsGeneric => new byte[] { 83, 121, 115, 116, 101, 109, 46, 67, 111, 108, 108, 101, 99, 116, 105, 111, 110, 115, 46, 71, 101, 110, 101, 114, 105, 99 };
        public static ReadOnlySpan<byte> SystemRuntimeInteropServices => new byte[] { 83, 121, 115, 116, 101, 109, 46, 82, 117, 110, 116, 105, 109, 101, 46, 73, 110, 116, 101, 114, 111, 112, 83, 101, 114, 118, 105, 99, 101, 115 };
        public static ReadOnlySpan<byte> SystemRuntimeCompilerServices => new byte[] { 83, 121, 115, 116, 101, 109, 46, 82, 117, 110, 116, 105, 109, 101, 46, 67, 111, 109, 112, 105, 108, 101, 114, 83, 101, 114, 118, 105, 99, 101, 115 };

        public static ReadOnlySpan<byte> Array => new byte[] { 65, 114, 114, 97, 121 };
        public static ReadOnlySpan<byte> Boolean => new byte[] { 66, 111, 111, 108, 101, 97, 110 };
        public static ReadOnlySpan<byte> Byte => new byte[] { 66, 121, 116, 101 };
        public static ReadOnlySpan<byte> Char => new byte[] { 67, 104, 97, 114 };
        public static ReadOnlySpan<byte> Double => new byte[] { 68, 111, 117, 98, 108, 101 };
        public static ReadOnlySpan<byte> Enum => new byte[] { 69, 110, 117, 109 };
        public static ReadOnlySpan<byte> Int16 => new byte[] { 73, 110, 116, 49, 54 };
        public static ReadOnlySpan<byte> Int32 => new byte[] { 73, 110, 116, 51, 50 };
        public static ReadOnlySpan<byte> Int64 => new byte[] { 73, 110, 116, 54, 52 };
        public static ReadOnlySpan<byte> IntPtr => new byte[] { 73, 110, 116, 80, 116, 114 };
        public static ReadOnlySpan<byte> Object => new byte[] { 79, 98, 106, 101, 99, 116 };
        public static ReadOnlySpan<byte> NullableT => new byte[] { 78, 117, 108, 108, 97, 98, 108, 101, 96, 49 };
        public static ReadOnlySpan<byte> SByte => new byte[] { 83, 66, 121, 116, 101 };
        public static ReadOnlySpan<byte> Single => new byte[] { 83, 105, 110, 103, 108, 101 };
        public static ReadOnlySpan<byte> String => new byte[] { 83, 116, 114, 105, 110, 103 };
        public static ReadOnlySpan<byte> TypedReference => new byte[] { 84, 121, 112, 101, 100, 82, 101, 102, 101, 114, 101, 110, 99, 101 };
        public static ReadOnlySpan<byte> UInt16 => new byte[] { 85, 73, 110, 116, 49, 54 };
        public static ReadOnlySpan<byte> UInt32 => new byte[] { 85, 73, 110, 116, 51, 50 };
        public static ReadOnlySpan<byte> UInt64 => new byte[] { 85, 73, 110, 116, 54, 52 };
        public static ReadOnlySpan<byte> UIntPtr => new byte[] { 85, 73, 110, 116, 80, 116, 114 };
        public static ReadOnlySpan<byte> ValueType => new byte[] { 86, 97, 108, 117, 101, 84, 121, 112, 101 };
        public static ReadOnlySpan<byte> Void => new byte[] { 86, 111, 105, 100 };
        public static ReadOnlySpan<byte> MulticastDelegate => new byte[] { 77, 117, 108, 116, 105, 99, 97, 115, 116, 68, 101, 108, 101, 103, 97, 116, 101 };
        public static ReadOnlySpan<byte> IEnumerableT => new byte[] { 73, 69, 110, 117, 109, 101, 114, 97, 98, 108, 101, 96, 49 };
        public static ReadOnlySpan<byte> ICollectionT => new byte[] { 73, 67, 111, 108, 108, 101, 99, 116, 105, 111, 110, 96, 49 };
        public static ReadOnlySpan<byte> IListT => new byte[] { 73, 76, 105, 115, 116, 96, 49 };
        public static ReadOnlySpan<byte> IReadOnlyListT => new byte[] { 73, 82, 101, 97, 100, 79, 110, 108, 121, 76, 105, 115, 116, 96, 49 };
        public static ReadOnlySpan<byte> Type => new byte[] { 84, 121, 112, 101 };
        public static ReadOnlySpan<byte> DBNull => new byte[] { 68, 66, 78, 117, 108, 108 };
        public static ReadOnlySpan<byte> Decimal => new byte[] { 68, 101, 99, 105, 109, 97, 108 };
        public static ReadOnlySpan<byte> DateTime => new byte[] { 68, 97, 116, 101, 84, 105, 109, 101 };
        public static ReadOnlySpan<byte> ComImportAttribute => new byte[] { 67, 111, 109, 73, 109, 112, 111, 114, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> DllImportAttribute => new byte[] { 68, 108, 108, 73, 109, 112, 111, 114, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> CallingConvention => new byte[] { 67, 97, 108, 108, 105, 110, 103, 67, 111, 110, 118, 101, 110, 116, 105, 111, 110 };
        public static ReadOnlySpan<byte> CharSet => new byte[] { 67, 104, 97, 114, 83, 101, 116 };
        public static ReadOnlySpan<byte> MarshalAsAttribute => new byte[] { 77, 97, 114, 115, 104, 97, 108, 65, 115, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> UnmanagedType => new byte[] { 85, 110, 109, 97, 110, 97, 103, 101, 100, 84, 121, 112, 101 };
        public static ReadOnlySpan<byte> VarEnum => new byte[] { 86, 97, 114, 69, 110, 117, 109 };
        public static ReadOnlySpan<byte> InAttribute => new byte[] { 73, 110, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> OutAttriubute => new byte[] { 79, 117, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> OptionalAttribute => new byte[] { 79, 112, 116, 105, 111, 110, 97, 108, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> PreserveSigAttribute => new byte[] { 80, 114, 101, 115, 101, 114, 118, 101, 83, 105, 103, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> FieldOffsetAttribute => new byte[] { 70, 105, 101, 108, 100, 79, 102, 102, 115, 101, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> IsByRefLikeAttribute => new byte[] { 73, 115, 66, 121, 82, 101, 102, 76, 105, 107, 101, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> DecimalConstantAttribute => new byte[] { 68, 101, 99, 105, 109, 97, 108, 67, 111, 110, 115, 116, 97, 110, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> CustomConstantAttribute => new byte[] { 67, 117, 115, 116, 111, 109, 67, 111, 110, 115, 116, 97, 110, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> GuidAttribute => new byte[] { 71, 117, 105, 100, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> DefaultMemberAttribute => new byte[] { 68, 101, 102, 97, 117, 108, 116, 77, 101, 109, 98, 101, 114, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
        public static ReadOnlySpan<byte> DateTimeConstantAttribute => new byte[] { 68, 97, 116, 101, 84, 105, 109, 101, 67, 111, 110, 115, 116, 97, 110, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
    }
}
