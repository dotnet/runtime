// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System
{
    //
    // typeof() is quite expensive so if you need to compare against a well-known system type,
    // try adding it to this class so it only gets allocated once.
    //
    internal static class CommonRuntimeTypes
    {
        internal static Type Object { get { return _object; } }
        internal static Type ValueType { get { return _valuetype; } }
        internal static Type Attribute { get { return _attribute; } }
        internal static Type String { get { return _string; } }
        internal static Type Array { get { return _array; } }
        internal static Type Enum { get { return _enum; } }
        internal static Type Boolean { get { return _boolean; } }
        internal static Type Char { get { return _char; } }
        internal static Type Byte { get { return _byte; } }
        internal static Type SByte { get { return _sByte; } }
        internal static Type UInt16 { get { return _uInt16; } }
        internal static Type Int16 { get { return _int16; } }
        internal static Type UInt32 { get { return _uInt32; } }
        internal static Type Int32 { get { return _int32; } }
        internal static Type UInt64 { get { return _uInt64; } }
        internal static Type Int64 { get { return _int64; } }
        internal static Type UIntPtr { get { return _uIntPtr; } }
        internal static Type IntPtr { get { return _intPtr; } }
        internal static Type Single { get { return _single; } }
        internal static Type Double { get { return _double; } }
        internal static Type Decimal { get { return _decimal; } }
        internal static Type DateTime { get { return _datetime; } }
        internal static Type Nullable { get { return _nullable; } }
        internal static Type Void { get { return _void; } }

        private static Type _object = typeof(Object);
        private static Type _valuetype = typeof(ValueType);
        private static Type _attribute = typeof(Attribute);
        private static Type _string = typeof(String);
        private static Type _array = typeof(Array);
        private static Type _enum = typeof(Enum);
        private static Type _boolean = typeof(Boolean);
        private static Type _char = typeof(Char);
        private static Type _byte = typeof(Byte);
        private static Type _sByte = typeof(SByte);
        private static Type _uInt16 = typeof(UInt16);
        private static Type _int16 = typeof(Int16);
        private static Type _uInt32 = typeof(UInt32);
        private static Type _int32 = typeof(Int32);
        private static Type _uInt64 = typeof(UInt64);
        private static Type _int64 = typeof(Int64);
        private static Type _uIntPtr = typeof(UIntPtr);
        private static Type _intPtr = typeof(IntPtr);
        private static Type _single = typeof(Single);
        private static Type _double = typeof(Double);
        private static Type _decimal = typeof(Decimal);
        private static Type _datetime = typeof(DateTime);
        private static Type _nullable = typeof(Nullable<>);
        private static Type _void = typeof(void);
    }
}
