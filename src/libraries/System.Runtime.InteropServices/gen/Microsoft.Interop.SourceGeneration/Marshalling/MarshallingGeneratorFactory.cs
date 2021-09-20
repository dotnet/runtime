using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public interface IMarshallingGeneratorFactory
    {
        /// <summary>
        /// Create an <see cref="IMarshallingGenerator"/> instance for marshalling the supplied type in the given position.
        /// </summary>
        /// <param name="info">Type details</param>
        /// <param name="context">Metadata about the stub the type is associated with</param>
        /// <returns>A <see cref="IMarshallingGenerator"/> instance.</returns>
        public IMarshallingGenerator Create(
            TypePositionInfo info,
            StubCodeContext context);
    }

    public sealed class DefaultMarshallingGeneratorFactory : IMarshallingGeneratorFactory
    {
        private static readonly ByteBoolMarshaller ByteBool = new();
        private static readonly WinBoolMarshaller WinBool = new();
        private static readonly VariantBoolMarshaller VariantBool = new();

        private static readonly Utf16CharMarshaller Utf16Char = new();
        private static readonly Utf16StringMarshaller Utf16String = new();
        private static readonly Utf8StringMarshaller Utf8String = new();
        private static readonly AnsiStringMarshaller AnsiString = new AnsiStringMarshaller(Utf8String);
        private static readonly PlatformDefinedStringMarshaller PlatformDefinedString = new PlatformDefinedStringMarshaller(Utf16String, Utf8String);

        private static readonly Forwarder Forwarder = new();
        private static readonly BlittableMarshaller Blittable = new();
        private static readonly DelegateMarshaller Delegate = new();
        private static readonly SafeHandleMarshaller SafeHandle = new();
        private InteropGenerationOptions Options { get; }

        public DefaultMarshallingGeneratorFactory(InteropGenerationOptions options)
        {
            this.Options = options;
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
                    return Blittable;

                // Enum with no marshalling info
                case { ManagedType: EnumTypeInfo enumType, MarshallingAttributeInfo: NoMarshallingInfo }:
                    // Check that the underlying type is not bool or char. C# does not allow this, but ECMA-335 does.
                    var underlyingSpecialType = enumType.UnderlyingType; 
                    if (underlyingSpecialType == SpecialType.System_Boolean || underlyingSpecialType == SpecialType.System_Char)
                    {
                        throw new MarshallingNotSupportedException(info, context);
                    }
                    return Blittable;

                // Pointer with no marshalling info
                case { ManagedType: PointerTypeInfo(_, _, IsFunctionPointer: false), MarshallingAttributeInfo: NoMarshallingInfo }:
                    return Blittable;

                // Function pointer with no marshalling info
                case { ManagedType: PointerTypeInfo(_, _, IsFunctionPointer: true), MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.FunctionPtr, _) }:
                    return Blittable;

                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: NoMarshallingInfo }:
                    return WinBool; // [Compat] Matching the default for the built-in runtime marshallers.
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.I1 or UnmanagedType.U1, _) }:
                    return ByteBool;
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.I4 or UnmanagedType.U4 or UnmanagedType.Bool, _) }:
                    return WinBool;
                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.VariantBool, _) }:
                    return VariantBool;

                case { ManagedType: DelegateTypeInfo, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.FunctionPtr, _) }:
                    return Delegate;

                case { MarshallingAttributeInfo: SafeHandleMarshallingInfo(_, bool isAbstract) }:
                    if (!context.AdditionalTemporaryStateLivesAcrossStages)
                    {
                        throw new MarshallingNotSupportedException(info, context);
                    }
                    if (info.IsByRef && isAbstract)
                    {
                        throw new MarshallingNotSupportedException(info, context)
                        {
                            NotSupportedDetails = Resources.SafeHandleByRefMustBeConcrete
                        };
                    }
                    return new SafeHandleMarshaller();

                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Char } }:
                    return CreateCharMarshaller(info, context);

                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_String } }:
                    return CreateStringMarshaller(info, context);

                case { ManagedType: SpecialTypeInfo { SpecialType: SpecialType.System_Void } }:
                    return Forwarder;

                default:
                    throw new MarshallingNotSupportedException(info, context);
            }
        }

        private IMarshallingGenerator CreateCharMarshaller(TypePositionInfo info, StubCodeContext context)
        {
            MarshallingInfo marshalInfo = info.MarshallingAttributeInfo;
            if (marshalInfo is NoMarshallingInfo)
            {
                // [Compat] Require explicit marshalling information.
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = Resources.MarshallingStringOrCharAsUndefinedNotSupported
                };
            }

            // Explicit MarshalAs takes precedence over string encoding info
            if (marshalInfo is MarshalAsInfo marshalAsInfo)
            {
                switch (marshalAsInfo.UnmanagedType)
                {
                    case UnmanagedType.I2:
                    case UnmanagedType.U2:
                        return Utf16Char;
                }
            }
            else if (marshalInfo is MarshallingInfoStringSupport marshalStringInfo)
            {
                switch (marshalStringInfo.CharEncoding)
                {
                    case CharEncoding.Utf16:
                        return Utf16Char;
                    case CharEncoding.Ansi:
                        throw new MarshallingNotSupportedException(info, context) // [Compat] ANSI is not supported for char
                        {
                            NotSupportedDetails = string.Format(Resources.MarshallingCharAsSpecifiedCharSetNotSupported, CharSet.Ansi)
                        };
                    case CharEncoding.PlatformDefined:
                        throw new MarshallingNotSupportedException(info, context) // [Compat] See conversion of CharSet.Auto.
                        {
                            NotSupportedDetails = string.Format(Resources.MarshallingCharAsSpecifiedCharSetNotSupported, CharSet.Auto)
                        };
                }
            }

            throw new MarshallingNotSupportedException(info, context);
        }

        private IMarshallingGenerator CreateStringMarshaller(TypePositionInfo info, StubCodeContext context)
        {
            MarshallingInfo marshalInfo = info.MarshallingAttributeInfo;
            if (marshalInfo is NoMarshallingInfo)
            {
                // [Compat] Require explicit marshalling information.
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = Resources.MarshallingStringOrCharAsUndefinedNotSupported
                };
            }

            // Explicit MarshalAs takes precedence over string encoding info
            if (marshalInfo is MarshalAsInfo marshalAsInfo)
            {
                switch (marshalAsInfo.UnmanagedType)
                {
                    case UnmanagedType.LPStr:
                        return AnsiString;
                    case UnmanagedType.LPTStr:
                    case UnmanagedType.LPWStr:
                        return Utf16String;
                    case (UnmanagedType)0x30:// UnmanagedType.LPUTF8Str
                        return Utf8String;
                }
            }
            else if (marshalInfo is MarshallingInfoStringSupport marshalStringInfo)
            {
                switch (marshalStringInfo.CharEncoding)
                {
                    case CharEncoding.Ansi:
                        return AnsiString;
                    case CharEncoding.Utf16:
                        return Utf16String;
                    case CharEncoding.Utf8:
                        return Utf8String;
                    case CharEncoding.PlatformDefined:
                        return PlatformDefinedString;
                }
            }

            throw new MarshallingNotSupportedException(info, context);
        }
    }
}
