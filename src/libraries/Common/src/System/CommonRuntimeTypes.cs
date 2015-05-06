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
        internal static Type Object { get { return s_object; } }
        internal static Type ValueType { get { return s_valuetype; } }
        internal static Type Attribute { get { return s_attribute; } }
        internal static Type String { get { return s_string; } }
        internal static Type Array { get { return s_array; } }
        internal static Type Enum { get { return s_enum; } }
        internal static Type Boolean { get { return s_boolean; } }
        internal static Type Char { get { return s_char; } }
        internal static Type Byte { get { return s_byte; } }
        internal static Type SByte { get { return s_sByte; } }
        internal static Type UInt16 { get { return s_uInt16; } }
        internal static Type Int16 { get { return s_int16; } }
        internal static Type UInt32 { get { return s_uInt32; } }
        internal static Type Int32 { get { return s_int32; } }
        internal static Type UInt64 { get { return s_uInt64; } }
        internal static Type Int64 { get { return s_int64; } }
        internal static Type UIntPtr { get { return s_uIntPtr; } }
        internal static Type IntPtr { get { return s_intPtr; } }
        internal static Type Single { get { return s_single; } }
        internal static Type Double { get { return s_double; } }
        internal static Type Decimal { get { return s_decimal; } }
        internal static Type DateTime { get { return s_datetime; } }
        internal static Type Nullable { get { return s_nullable; } }
        internal static Type Void { get { return s_void; } }

        private static Type s_object = typeof(Object);
        private static Type s_valuetype = typeof(ValueType);
        private static Type s_attribute = typeof(Attribute);
        private static Type s_string = typeof(String);
        private static Type s_array = typeof(Array);
        private static Type s_enum = typeof(Enum);
        private static Type s_boolean = typeof(Boolean);
        private static Type s_char = typeof(Char);
        private static Type s_byte = typeof(Byte);
        private static Type s_sByte = typeof(SByte);
        private static Type s_uInt16 = typeof(UInt16);
        private static Type s_int16 = typeof(Int16);
        private static Type s_uInt32 = typeof(UInt32);
        private static Type s_int32 = typeof(Int32);
        private static Type s_uInt64 = typeof(UInt64);
        private static Type s_int64 = typeof(Int64);
        private static Type s_uIntPtr = typeof(UIntPtr);
        private static Type s_intPtr = typeof(IntPtr);
        private static Type s_single = typeof(Single);
        private static Type s_double = typeof(Double);
        private static Type s_decimal = typeof(Decimal);
        private static Type s_datetime = typeof(DateTime);
        private static Type s_nullable = typeof(Nullable<>);
        private static Type s_void = typeof(void);
    }
}
