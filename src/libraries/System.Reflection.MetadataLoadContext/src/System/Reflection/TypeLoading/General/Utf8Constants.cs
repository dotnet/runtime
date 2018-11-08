// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    internal static class Utf8Constants
    {
        public static readonly byte[] System = {83, 121, 115, 116, 101, 109};
        public static readonly byte[] SystemReflection = {83, 121, 115, 116, 101, 109, 46, 82, 101, 102, 108, 101, 99, 116, 105, 111, 110};
        public static readonly byte[] SystemCollectionsGeneric = {83, 121, 115, 116, 101, 109, 46, 67, 111, 108, 108, 101, 99, 116, 105, 111, 110, 115, 46, 71, 101, 110, 101, 114, 105, 99};
        public static readonly byte[] SystemRuntimeInteropServices = {83, 121, 115, 116, 101, 109, 46, 82, 117, 110, 116, 105, 109, 101, 46, 73, 110, 116, 101, 114, 111, 112, 83, 101, 114, 118, 105, 99, 101, 115};
        public static readonly byte[] SystemRuntimeCompilerServices = {83, 121, 115, 116, 101, 109, 46, 82, 117, 110, 116, 105, 109, 101, 46, 67, 111, 109, 112, 105, 108, 101, 114, 83, 101, 114, 118, 105, 99, 101, 115};

        public static readonly byte[] Array = {65, 114, 114, 97, 121};
        public static readonly byte[] Boolean = {66, 111, 111, 108, 101, 97, 110};
        public static readonly byte[] Byte = {66, 121, 116, 101};
        public static readonly byte[] Char = {67, 104, 97, 114};
        public static readonly byte[] Double = {68, 111, 117, 98, 108, 101};
        public static readonly byte[] Enum = {69, 110, 117, 109};
        public static readonly byte[] Int16 = {73, 110, 116, 49, 54};
        public static readonly byte[] Int32 = {73, 110, 116, 51, 50};
        public static readonly byte[] Int64 = {73, 110, 116, 54, 52};
        public static readonly byte[] IntPtr = {73, 110, 116, 80, 116, 114};
        public static readonly byte[] Object = {79, 98, 106, 101, 99, 116};
        public static readonly byte[] NullableT = { 78, 117, 108, 108, 97, 98, 108, 101, 96, 49 };
        public static readonly byte[] SByte = {83, 66, 121, 116, 101};
        public static readonly byte[] Single = {83, 105, 110, 103, 108, 101};
        public static readonly byte[] String = {83, 116, 114, 105, 110, 103};
        public static readonly byte[] TypedReference = {84, 121, 112, 101, 100, 82, 101, 102, 101, 114, 101, 110, 99, 101};
        public static readonly byte[] UInt16 = {85, 73, 110, 116, 49, 54};
        public static readonly byte[] UInt32 = {85, 73, 110, 116, 51, 50};
        public static readonly byte[] UInt64 = {85, 73, 110, 116, 54, 52};
        public static readonly byte[] UIntPtr = {85, 73, 110, 116, 80, 116, 114};
        public static readonly byte[] ValueType = {86, 97, 108, 117, 101, 84, 121, 112, 101};
        public static readonly byte[] Void = {86, 111, 105, 100};
        public static readonly byte[] MulticastDelegate = {77, 117, 108, 116, 105, 99, 97, 115, 116, 68, 101, 108, 101, 103, 97, 116, 101};
        public static readonly byte[] IEnumerableT = {73, 69, 110, 117, 109, 101, 114, 97, 98, 108, 101, 96, 49};
        public static readonly byte[] ICollectionT = {73, 67, 111, 108, 108, 101, 99, 116, 105, 111, 110, 96, 49};
        public static readonly byte[] IListT = {73, 76, 105, 115, 116, 96, 49};
        public static readonly byte[] IReadOnlyListT = {73, 82, 101, 97, 100, 79, 110, 108, 121, 76, 105, 115, 116, 96, 49};
        public static readonly byte[] Type = {84, 121, 112, 101};
        public static readonly byte[] DBNull = {68, 66, 78, 117, 108, 108};
        public static readonly byte[] Decimal = {68, 101, 99, 105, 109, 97, 108};
        public static readonly byte[] DateTime = {68, 97, 116, 101, 84, 105, 109, 101};
        public static readonly byte[] ComImportAttribute = {67, 111, 109, 73, 109, 112, 111, 114, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] DllImportAttribute = {68, 108, 108, 73, 109, 112, 111, 114, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] CallingConvention = {67, 97, 108, 108, 105, 110, 103, 67, 111, 110, 118, 101, 110, 116, 105, 111, 110};
        public static readonly byte[] CharSet = {67, 104, 97, 114, 83, 101, 116};
        public static readonly byte[] MarshalAsAttribute = {77, 97, 114, 115, 104, 97, 108, 65, 115, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] UnmanagedType = {85, 110, 109, 97, 110, 97, 103, 101, 100, 84, 121, 112, 101};
        public static readonly byte[] VarEnum = {86, 97, 114, 69, 110, 117, 109};
        public static readonly byte[] InAttribute = {73, 110, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] OutAttriubute = {79, 117, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] OptionalAttribute = {79, 112, 116, 105, 111, 110, 97, 108, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] PreserveSigAttribute = {80, 114, 101, 115, 101, 114, 118, 101, 83, 105, 103, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] FieldOffsetAttribute = {70, 105, 101, 108, 100, 79, 102, 102, 115, 101, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] IsByRefLikeAttribute = {73, 115, 66, 121, 82, 101, 102, 76, 105, 107, 101, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] DecimalConstantAttribute = {68, 101, 99, 105, 109, 97, 108, 67, 111, 110, 115, 116, 97, 110, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] CustomConstantAttribute = {67, 117, 115, 116, 111, 109, 67, 111, 110, 115, 116, 97, 110, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] GuidAttribute = {71, 117, 105, 100, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] DefaultMemberAttribute = {68, 101, 102, 97, 117, 108, 116, 77, 101, 109, 98, 101, 114, 65, 116, 116, 114, 105, 98, 117, 116, 101};
        public static readonly byte[] DateTimeConstantAttribute = { 68, 97, 116, 101, 84, 105, 109, 101, 67, 111, 110, 115, 116, 97, 110, 116, 65, 116, 116, 114, 105, 98, 117, 116, 101 };
    }
}
