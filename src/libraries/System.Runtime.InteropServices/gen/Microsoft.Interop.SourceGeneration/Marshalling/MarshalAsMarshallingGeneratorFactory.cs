// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class MarshalAsMarshallingGeneratorFactory : IMarshallingGeneratorFactory
    {
        private static readonly ByteBoolMarshaller s_byteBool = new();
        private static readonly WinBoolMarshaller s_winBool = new();
        private static readonly VariantBoolMarshaller s_variantBool = new();

        private static readonly Utf16CharMarshaller s_utf16Char = new();

        private static readonly Forwarder s_forwarder = new();
        private static readonly BlittableMarshaller s_blittable = new();
        private static readonly DelegateMarshaller s_delegate = new();
        private static readonly SafeHandleMarshaller s_safeHandle = new();
        private InteropGenerationOptions Options { get; }
        private IMarshallingGeneratorFactory InnerFactory { get; }

        public MarshalAsMarshallingGeneratorFactory(InteropGenerationOptions options, IMarshallingGeneratorFactory inner)
        {
            Options = options;
            InnerFactory = inner;
        }

        /// <summary>
        /// Create an <see cref="IMarshallingGenerator"/> instance for marshalling the supplied type in the given position.
        /// </summary>
        /// <param name="info">Type details</param>
        /// <param name="context">Metadata about the stub the type is associated with</param>
        /// <returns>A <see cref="IMarshallingGenerator"/> instance.</returns>
        public IMarshallingGenerator Create(
            TypePositionInfo info,
            StubCodeContext context)
        {
            switch (info)
            {
                // Blittable primitives with no marshalling info or with a compatible [MarshalAs] attribute.
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_SByte }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I1, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Byte }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U1, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Int16 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I2, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_UInt16 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U2, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Int32 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I4, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_UInt32 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U4, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Int64 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I8, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_UInt64 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U8, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_IntPtr }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.SysInt, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_UIntPtr }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.SysUInt, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Single }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.R4, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Double }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.R8, _) }:
                    return s_blittable;

                // Enum with no marshalling info
                case { ManagedType: EnumTypeInfo enumType, MarshallingAttributeInfo: NoMarshallingInfo }:
                    // Check that the underlying type is not bool or char. C# does not allow this, but ECMA-335 does.
                    SpecialType underlyingSpecialType = enumType.UnderlyingType;
                    if (underlyingSpecialType == SpecialType.System_Boolean || underlyingSpecialType == SpecialType.System_Char)
                    {
                        throw new MarshallingNotSupportedException(info, context);
                    }
                    return s_blittable;

                // Pointer with no marshalling info
                case { ManagedType: PointerTypeInfo(_, _, IsFunctionPointer: false), MarshallingAttributeInfo: NoMarshallingInfo }:
                    return s_blittable;

                // Function pointer with no marshalling info
                case { ManagedType: PointerTypeInfo(_, _, IsFunctionPointer: true), MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.FunctionPtr, _) }:
                    return s_blittable;

                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: NoMarshallingInfo }:
                    throw new MarshallingNotSupportedException(info, context)
                    {
                        NotSupportedDetails = SR.MarshallingBoolAsUndefinedNotSupported
                    };
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.I1 or UnmanagedType.U1, _) }:
                    return s_byteBool;
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.I4 or UnmanagedType.U4 or UnmanagedType.Bool, _) }:
                    return s_winBool;
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.VariantBool, _) }:
                    return s_variantBool;

                case { ManagedType: DelegateTypeInfo, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.FunctionPtr, _) }:
                    return s_delegate;

                case { MarshallingAttributeInfo: SafeHandleMarshallingInfo(_, bool isAbstract) }:
                    if (!context.AdditionalTemporaryStateLivesAcrossStages)
                    {
                        throw new MarshallingNotSupportedException(info, context);
                    }
                    if (info.IsByRef && isAbstract)
                    {
                        throw new MarshallingNotSupportedException(info, context)
                        {
                            NotSupportedDetails = SR.SafeHandleByRefMustBeConcrete
                        };
                    }
                    return s_safeHandle;

                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Char } }:
                    return CreateCharMarshaller(info, context);

                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_String } }:
                    return ReportStringMarshallingNotSupported(info, context);

                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Void } }:
                    return s_forwarder;

                default:
                    return InnerFactory.Create(info, context);
            }
        }

        private static IMarshallingGenerator CreateCharMarshaller(TypePositionInfo info, StubCodeContext context)
        {
            MarshallingInfo marshalInfo = info.MarshallingAttributeInfo;
            if (marshalInfo is NoMarshallingInfo)
            {
                // [Compat] Require explicit marshalling information.
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = SR.MarshallingStringOrCharAsUndefinedNotSupported
                };
            }

            // Explicit MarshalAs takes precedence over string encoding info
            if (marshalInfo is MarshalAsInfo marshalAsInfo)
            {
                switch (marshalAsInfo.UnmanagedType)
                {
                    case UnmanagedType.I2:
                    case UnmanagedType.U2:
                        return s_utf16Char;
                }
            }
            else if (marshalInfo is MarshallingInfoStringSupport marshalStringInfo)
            {
                switch (marshalStringInfo.CharEncoding)
                {
                    case CharEncoding.Utf16:
                        return s_utf16Char;
                    case CharEncoding.Ansi:
                        throw new MarshallingNotSupportedException(info, context) // [Compat] ANSI is not supported for char
                        {
                            NotSupportedDetails = string.Format(SR.MarshallingCharAsSpecifiedCharSetNotSupported, CharSet.Ansi)
                        };
                }
            }

            throw new MarshallingNotSupportedException(info, context);
        }

        private static IMarshallingGenerator ReportStringMarshallingNotSupported(TypePositionInfo info, StubCodeContext context)
        {
            MarshallingInfo marshalInfo = info.MarshallingAttributeInfo;
            if (marshalInfo is NoMarshallingInfo)
            {
                // [Compat] Require explicit marshalling information.
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = SR.MarshallingStringOrCharAsUndefinedNotSupported
                };
            }

            // Supported string marshalling should have gone through custom type marshallers.
            throw new MarshallingNotSupportedException(info, context);
        }
    }
}
