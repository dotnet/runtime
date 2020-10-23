using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Interface for generation of marshalling code for P/Invoke stubs
    /// </summary>
    internal interface IMarshallingGenerator
    {
        /// <summary>
        /// Get the native type syntax for <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <returns>Type syntax for the native type representing <paramref name="info"/></returns>
        TypeSyntax AsNativeType(TypePositionInfo info);

        /// <summary>
        /// Get the <paramref name="info"/> as a parameter of the P/Invoke declaration
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <returns>Parameter syntax for <paramref name="info"/></returns>
        ParameterSyntax AsParameter(TypePositionInfo info);

        /// <summary>
        /// Get the <paramref name="info"/> as an argument to be passed to the P/Invoke
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>Argument syntax for <paramref name="info"/></returns>
        ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Generate code for marshalling
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>List of statements to be added to the P/Invoke stub</returns>
        /// <remarks>
        /// The generator should return the appropriate statements based on the
        /// <see cref="StubCodeContext.CurrentStage" /> of <paramref name="context"/>.
        /// For <see cref="StubCodeContext.Stage.Pin"/>, any statements not of type
        /// <see cref="FixedStatementSyntax"/> will be ignored.
        /// </remarks>
        IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Returns whether or not this marshaller uses an identifier for the native value in addition
        /// to an identifer for the managed value.
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>If the marshaller uses an identifier for the native value, true; otherwise, false.</returns>
        /// <remarks>
        /// <see cref="StubCodeContext.CurrentStage" /> of <paramref name="context"/> may not be valid.
        /// </remarks>
        bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);
    }

    /// <summary>
    /// Exception used to indicate marshalling isn't supported.
    /// </summary>
    internal class MarshallingNotSupportedException : Exception
    {
        /// <summary>
        /// Construct a new <see cref="MarshallingNotSupportedException"/> instance.
        /// </summary>
        /// <param name="info"><see cref="Microsoft.Interop.TypePositionInfo"/> instance</param>
        /// <param name="context"><see cref="Microsoft.Interop.StubCodeContext"/> instance</param>
        public MarshallingNotSupportedException(TypePositionInfo info, StubCodeContext context)
        {
            this.TypePositionInfo = info;
            this.StubCodeContext = context;
        }

        /// <summary>
        /// Type that is being marshalled.
        /// </summary>
        public TypePositionInfo TypePositionInfo { get; private init; }

        /// <summary>
        /// Context the marshalling is taking place.
        /// </summary>
        public StubCodeContext StubCodeContext { get; private init; }

        /// <summary>
        /// [Optional] Specific reason marshalling of the supplied type isn't supported.
        /// </summary>
        public string? NotSupportedDetails { get; init; }
    }

    internal class MarshallingGenerators
    {
        public static readonly ByteBoolMarshaller ByteBool = new ByteBoolMarshaller();
        public static readonly WinBoolMarshaller WinBool = new WinBoolMarshaller();
        public static readonly VariantBoolMarshaller VariantBool = new VariantBoolMarshaller();
        public static readonly Utf16CharMarshaller Utf16Char = new Utf16CharMarshaller();
        public static readonly Utf16StringMarshaller Utf16String = new Utf16StringMarshaller();
        public static readonly Utf8StringMarshaller Utf8String = new Utf8StringMarshaller();
        public static readonly Forwarder Forwarder = new Forwarder();
        public static readonly BlittableMarshaller Blittable = new BlittableMarshaller();
        public static readonly DelegateMarshaller Delegate = new DelegateMarshaller();
        public static readonly SafeHandleMarshaller SafeHandle = new SafeHandleMarshaller();

        /// <summary>
        /// Create an <see cref="IMarshallingGenerator"/> instance to marshalling the supplied type.
        /// </summary>
        /// <param name="info">Type details</param>
        /// <param name="context">Metadata about the stub the type is associated with</param>
        /// <returns>A <see cref="IMarshallingGenerator"/> instance.</returns>
        public static IMarshallingGenerator Create(
            TypePositionInfo info,
            StubCodeContext context)
        {
#if GENERATE_FORWARDER
            return MarshallingGenerators.Forwarder;
#else
            if (info.IsNativeReturnPosition && !info.IsManagedReturnPosition)
            {
                // [TODO] Use marshaller for native HRESULT return / exception throwing
                // Debug.Assert(info.ManagedType.SpecialType == SpecialType.System_Int32)
            }

            switch (info)
            {
                // Blittable primitives with no marshalling info or with a compatible [MarshalAs] attribute.
                case { ManagedType: { SpecialType: SpecialType.System_SByte }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.I1 } }
                    or { ManagedType: { SpecialType: SpecialType.System_Byte }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.U1 } }
                    or { ManagedType: { SpecialType: SpecialType.System_Int16 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.I2 } }
                    or { ManagedType: { SpecialType: SpecialType.System_UInt16 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.U2 } }
                    or { ManagedType: { SpecialType: SpecialType.System_Int32 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.I4 } }
                    or { ManagedType: { SpecialType: SpecialType.System_UInt32 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.U4 } }
                    or { ManagedType: { SpecialType: SpecialType.System_Int64 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.I8 } }
                    or { ManagedType: { SpecialType: SpecialType.System_UInt64 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.U8 } }
                    or { ManagedType: { SpecialType: SpecialType.System_IntPtr }, MarshallingAttributeInfo: NoMarshallingInfo }
                    or { ManagedType: { SpecialType: SpecialType.System_UIntPtr }, MarshallingAttributeInfo: NoMarshallingInfo}
                    or { ManagedType: { SpecialType: SpecialType.System_Single }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.R4 } }
                    or { ManagedType: { SpecialType: SpecialType.System_Double }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.R8 } }:
                    return Blittable;

                case { ManagedType: { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: NoMarshallingInfo }:
                    return WinBool; // [Compat] Matching the default for the built-in runtime marshallers.
                case { ManagedType: { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo { UnmanagedType: UnmanagedType.I1 or UnmanagedType.U1 } }:
                    return ByteBool;
                case { ManagedType: { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo { UnmanagedType: UnmanagedType.I4 or UnmanagedType.U4 or UnmanagedType.Bool } }:
                    return WinBool;
                case { ManagedType: { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo { UnmanagedType: UnmanagedType.VariantBool } }:
                    return VariantBool;

                case { ManagedType: { SpecialType: SpecialType.System_Char } }:
                    return CreateCharMarshaller(info, context);

                case { ManagedType: { SpecialType: SpecialType.System_String } }:
                    return CreateStringMarshaller(info, context);

                case { ManagedType: { TypeKind: TypeKind.Delegate }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo { UnmanagedType: UnmanagedType.FunctionPtr } }:
                    return Delegate;

                case { MarshallingAttributeInfo: BlittableTypeAttributeInfo _ }:
                    return Blittable;

                // Marshalling in new model
                case { MarshallingAttributeInfo: NativeMarshallingAttributeInfo marshalInfo }:
                    return Forwarder;

                // Simple marshalling with new attribute model, only have type name.
                case { MarshallingAttributeInfo: GeneratedNativeMarshallingAttributeInfo { NativeMarshallingFullyQualifiedTypeName: string name } }:
                    return Forwarder;

                case { MarshallingAttributeInfo: SafeHandleMarshallingInfo _}:
                    return SafeHandle;

                case { ManagedType: { SpecialType: SpecialType.System_Void } }:
                    return Forwarder;

                default:
                    throw new MarshallingNotSupportedException(info, context);
            }
#endif
        }

        private static IMarshallingGenerator CreateCharMarshaller(TypePositionInfo info, StubCodeContext context)
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

        private static IMarshallingGenerator CreateStringMarshaller(TypePositionInfo info, StubCodeContext context)
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
                    case CharEncoding.Utf16:
                        return Utf16String;
                    case CharEncoding.Utf8:
                        return Utf8String;
                }
            }

            throw new MarshallingNotSupportedException(info, context);
        }
    }
}
