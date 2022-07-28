// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Enumerates all the system types that MetadataLoadContexts may need to fish out of the core assembly.
    /// Note that the enum values are often cast to "int" and used as indices into a table so the
    /// enum values should be left contiguous.
    ///
    /// If you add a member to this enum, you must also add a switch case for it in CoreTypeHelpers.GetFullName();
    /// </summary>
    internal enum CoreType
    {
        Array,
        Boolean,
        Byte,
        Char,
        Double,
        Enum,
        Int16,
        Int32,
        Int64,
        IntPtr,
        Object,
        NullableT,
        SByte,
        Single,
        String,
        TypedReference,
        UInt16,
        UInt32,
        UInt64,
        UIntPtr,
        ValueType,
        Void,

        MulticastDelegate,

        // "Implemented" by arrays
        IEnumerableT,
        ICollectionT,
        IListT,
        IReadOnlyListT,

        // Default values
        DBNull,
        Decimal,
        DateTime,

        // For custom attribute processing
        Type,

        // Pseudo Custom Attributes
        ComImportAttribute,
        DllImportAttribute,
        CallingConvention,
        CharSet,
        MarshalAsAttribute,
        UnmanagedType,
        VarEnum,
        InAttribute,
        OutAttribute,
        OptionalAttribute,
        PreserveSigAttribute,
        FieldOffsetAttribute,

        NumCoreTypes,
    }

    internal static class CoreTypeHelpers
    {
        public static void GetFullName(this CoreType coreType, out ReadOnlySpan<byte> ns, out ReadOnlySpan<byte> name)
        {
            switch (coreType)
            {
                case CoreType.Array: ns = Utf8Constants.System; name = Utf8Constants.Array; return;
                case CoreType.Boolean: ns = Utf8Constants.System; name = Utf8Constants.Boolean; return;
                case CoreType.Byte: ns = Utf8Constants.System; name = Utf8Constants.Byte; return;
                case CoreType.Char: ns = Utf8Constants.System; name = Utf8Constants.Char; return;
                case CoreType.Double: ns = Utf8Constants.System; name = Utf8Constants.Double; return;
                case CoreType.Enum: ns = Utf8Constants.System; name = Utf8Constants.Enum; return;
                case CoreType.Int16: ns = Utf8Constants.System; name = Utf8Constants.Int16; return;
                case CoreType.Int32: ns = Utf8Constants.System; name = Utf8Constants.Int32; return;
                case CoreType.Int64: ns = Utf8Constants.System; name = Utf8Constants.Int64; return;
                case CoreType.IntPtr: ns = Utf8Constants.System; name = Utf8Constants.IntPtr; return;
                case CoreType.NullableT: ns = Utf8Constants.System; name = Utf8Constants.NullableT; return;
                case CoreType.Object: ns = Utf8Constants.System; name = Utf8Constants.Object; return;
                case CoreType.SByte: ns = Utf8Constants.System; name = Utf8Constants.SByte; return;
                case CoreType.Single: ns = Utf8Constants.System; name = Utf8Constants.Single; return;
                case CoreType.String: ns = Utf8Constants.System; name = Utf8Constants.String; return;
                case CoreType.TypedReference: ns = Utf8Constants.System; name = Utf8Constants.TypedReference; return;
                case CoreType.UInt16: ns = Utf8Constants.System; name = Utf8Constants.UInt16; return;
                case CoreType.UInt32: ns = Utf8Constants.System; name = Utf8Constants.UInt32; return;
                case CoreType.UInt64: ns = Utf8Constants.System; name = Utf8Constants.UInt64; return;
                case CoreType.UIntPtr: ns = Utf8Constants.System; name = Utf8Constants.UIntPtr; return;
                case CoreType.ValueType: ns = Utf8Constants.System; name = Utf8Constants.ValueType; return;
                case CoreType.Void: ns = Utf8Constants.System; name = Utf8Constants.Void; return;
                case CoreType.MulticastDelegate: ns = Utf8Constants.System; name = Utf8Constants.MulticastDelegate; return;
                case CoreType.IEnumerableT: ns = Utf8Constants.SystemCollectionsGeneric; name = Utf8Constants.IEnumerableT; return;
                case CoreType.ICollectionT: ns = Utf8Constants.SystemCollectionsGeneric; name = Utf8Constants.ICollectionT; return;
                case CoreType.IListT: ns = Utf8Constants.SystemCollectionsGeneric; name = Utf8Constants.IListT; return;
                case CoreType.IReadOnlyListT: ns = Utf8Constants.SystemCollectionsGeneric; name = Utf8Constants.IReadOnlyListT; return;
                case CoreType.Type: ns = Utf8Constants.System; name = Utf8Constants.Type; return;
                case CoreType.DBNull: ns = Utf8Constants.System; name = Utf8Constants.DBNull; return;
                case CoreType.Decimal: ns = Utf8Constants.System; name = Utf8Constants.Decimal; return;
                case CoreType.DateTime: ns = Utf8Constants.System; name = Utf8Constants.DateTime; return;
                case CoreType.ComImportAttribute: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.ComImportAttribute; return;
                case CoreType.DllImportAttribute: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.DllImportAttribute; return;
                case CoreType.CallingConvention: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.CallingConvention; return;
                case CoreType.CharSet: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.CharSet; return;
                case CoreType.MarshalAsAttribute: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.MarshalAsAttribute; return;
                case CoreType.UnmanagedType: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.UnmanagedType; return;
                case CoreType.VarEnum: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.VarEnum; return;
                case CoreType.InAttribute: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.InAttribute; return;
                case CoreType.OutAttribute: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.OutAttriubute; return;
                case CoreType.OptionalAttribute: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.OptionalAttribute; return;
                case CoreType.PreserveSigAttribute: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.PreserveSigAttribute; return;
                case CoreType.FieldOffsetAttribute: ns = Utf8Constants.SystemRuntimeInteropServices; name = Utf8Constants.FieldOffsetAttribute; return;
                default:
                    Debug.Fail("Unexpected coreType passed to GetCoreTypeFullName: " + coreType);
                    ns = name = default;
                    return;
            }
        }
    }
}
