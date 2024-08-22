﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class MarshalAsMarshallingGeneratorResolver : IMarshallingGeneratorResolver
    {
        private static readonly ByteBoolMarshaller s_byteBool = new(signed: false);
        private static readonly ByteBoolMarshaller s_signed_byteBool = new(signed: true);
        private static readonly WinBoolMarshaller s_winBool = new(signed: false);
        private static readonly WinBoolMarshaller s_signed_winBool = new(signed: true);
        private static readonly VariantBoolMarshaller s_variantBool = new();

        private static readonly Forwarder s_forwarder = new();
        private static readonly BlittableMarshaller s_blittable = new();
        private static readonly DelegateMarshaller s_delegate = new();
        private InteropGenerationOptions Options { get; }

        public MarshalAsMarshallingGeneratorResolver(InteropGenerationOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Create an <see cref="IUnboundMarshallingGenerator"/> instance for marshalling the supplied type in the given position.
        /// </summary>
        /// <param name="info">Type details</param>
        /// <param name="context">Metadata about the stub the type is associated with</param>
        /// <returns>A <see cref="IUnboundMarshallingGenerator"/> instance.</returns>
        public ResolvedGenerator Create(
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
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Int32 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I4 or UnmanagedType.Error, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_UInt32 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U4 or UnmanagedType.Error, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Int64 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I8, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_UInt64 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U8, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_IntPtr }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.SysInt, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_UIntPtr }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.SysUInt, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Single }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.R4, _) }
                    or { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Double }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.R8, _) }:
                    // TODO: Report the MarshalAs attribute as unnecessary
                    return ResolvedGenerator.Resolved(s_blittable.Bind(info, context));

                // Enum with no marshalling info
                case { ManagedType: EnumTypeInfo enumType, MarshallingAttributeInfo: NoMarshallingInfo }:
                    // Check that the underlying type is not bool or char. C# does not allow this, but ECMA-335 does.
                    SpecialType underlyingSpecialType = enumType.UnderlyingType;
                    if (underlyingSpecialType == SpecialType.System_Boolean || underlyingSpecialType == SpecialType.System_Char)
                    {
                        return ResolvedGenerator.NotSupported(info, context, new(info));
                    }
                    return ResolvedGenerator.Resolved(s_blittable.Bind(info, context));

                // Pointer with no marshalling info
                case { ManagedType: PointerTypeInfo{ IsFunctionPointer: false }, MarshallingAttributeInfo: NoMarshallingInfo }:
                    return ResolvedGenerator.Resolved(s_blittable.Bind(info, context));

                // Function pointer with no marshalling info
                case { ManagedType: PointerTypeInfo { IsFunctionPointer: true }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.FunctionPtr, _) }:
                    return ResolvedGenerator.Resolved(s_blittable.Bind(info, context));

                // Bool with marshalling info
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.U1, _) }:
                    return ResolvedGenerator.Resolved(s_byteBool.Bind(info, context));
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.I1, _) }:
                    return ResolvedGenerator.Resolved(s_signed_byteBool.Bind(info, context));
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.U4, _) }:
                    return ResolvedGenerator.Resolved(s_winBool.Bind(info, context));
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.I4 or UnmanagedType.Bool, _) }:
                    return ResolvedGenerator.Resolved(s_signed_winBool.Bind(info, context));
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.VariantBool, _) }:
                    return ResolvedGenerator.Resolved(s_variantBool.Bind(info, context));

                // Delegate types
                case { ManagedType: DelegateTypeInfo, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.FunctionPtr, _) }:
                    return ResolvedGenerator.Resolved(s_delegate.Bind(info, context));

                // void
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Void } }:
                    return ResolvedGenerator.Resolved(s_forwarder.Bind(info, context));

                default:
                    return ResolvedGenerator.UnresolvedGenerator;
            }
        }
    }
}
