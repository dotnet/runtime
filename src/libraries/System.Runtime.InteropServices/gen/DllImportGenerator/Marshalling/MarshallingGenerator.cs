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

        /// <summary>
        /// Returns if the given ByValueContentsMarshalKind is supported in the current marshalling context.
        /// A supported marshal kind has a different behavior than the default behavior.
        /// </summary>
        /// <param name="marshalKind">The marshal kind.</param>
        /// <param name="context">The marshalling context.</param>
        /// <returns></returns>
        bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context);
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
        /// Context in which the marshalling is taking place.
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
        public static readonly AnsiStringMarshaller AnsiString = new AnsiStringMarshaller(Utf8String);
        public static readonly PlatformDefinedStringMarshaller PlatformDefinedString = new PlatformDefinedStringMarshaller(Utf16String, Utf8String);

        public static readonly Forwarder Forwarder = new Forwarder();
        public static readonly BlittableMarshaller Blittable = new BlittableMarshaller();
        public static readonly DelegateMarshaller Delegate = new DelegateMarshaller();
        public static readonly HResultExceptionMarshaller HResultException = new HResultExceptionMarshaller();

        /// <summary>
        /// Create an <see cref="IMarshallingGenerator"/> instance for marshalling the supplied type in the given position.
        /// </summary>
        /// <param name="info">Type details</param>
        /// <param name="context">Metadata about the stub the type is associated with</param>
        /// <returns>A <see cref="IMarshallingGenerator"/> instance.</returns>
        public static IMarshallingGenerator Create(
            TypePositionInfo info,
            StubCodeContext context,
            AnalyzerConfigOptions options)
        {
            return ValidateByValueMarshalKind(context, info, CreateCore(info, context, options));
        }

        private static IMarshallingGenerator ValidateByValueMarshalKind(StubCodeContext context, TypePositionInfo info, IMarshallingGenerator generator)
        {
            if (info.IsByRef && info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default)
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = Resources.InOutAttributeByRefNotSupported
                };
            }
            else if (info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.In)
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = Resources.InAttributeNotSupportedWithoutOut
                };
            }
            else if (info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default
                && !generator.SupportsByValueMarshalKind(info.ByValueContentsMarshalKind, context))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = Resources.InOutAttributeMarshalerNotSupported
                };
            }
            return generator;
        }

        /// <summary>
        /// Create an <see cref="IMarshallingGenerator"/> instance to marshalling the supplied type.
        /// </summary>
        /// <param name="info">Type details</param>
        /// <param name="context">Metadata about the stub the type is associated with</param>
        /// <returns>A <see cref="IMarshallingGenerator"/> instance.</returns>
        private static IMarshallingGenerator CreateCore(
            TypePositionInfo info,
            StubCodeContext context,
            AnalyzerConfigOptions options)
        {
            if (options.GenerateForwarders())
            {
                return MarshallingGenerators.Forwarder;
            }

            if (info.IsNativeReturnPosition && !info.IsManagedReturnPosition)
            {
                // Use marshaller for native HRESULT return / exception throwing
                System.Diagnostics.Debug.Assert(info.ManagedType.SpecialType == SpecialType.System_Int32);
                return HResultException;
            }

            switch (info)
            {
                // Blittable primitives with no marshalling info or with a compatible [MarshalAs] attribute.
                case { ManagedType: { SpecialType: SpecialType.System_SByte }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I1, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_Byte }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U1, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_Int16 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I2, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_UInt16 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U2, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_Int32 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I4, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_UInt32 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U4, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_Int64 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.I8, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_UInt64 }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.U8, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_IntPtr }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.SysInt, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_UIntPtr }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.SysUInt, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_Single }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.R4, _) }
                    or { ManagedType: { SpecialType: SpecialType.System_Double }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.R8, _) }:
                    return Blittable;

                // Enum with no marshalling info
                case { ManagedType: { TypeKind: TypeKind.Enum }, MarshallingAttributeInfo: NoMarshallingInfo }:
                    // Check that the underlying type is not bool or char. C# does not allow this, but ECMA-335 does.
                    var underlyingSpecialType = ((INamedTypeSymbol)info.ManagedType).EnumUnderlyingType!.SpecialType;
                    if (underlyingSpecialType == SpecialType.System_Boolean || underlyingSpecialType == SpecialType.System_Char)
                    {
                        throw new MarshallingNotSupportedException(info, context);
                    }
                    return Blittable;

                // Pointer with no marshalling info
                case { ManagedType: { TypeKind: TypeKind.Pointer }, MarshallingAttributeInfo: NoMarshallingInfo }:
                    return Blittable;

                // Function pointer with no marshalling info
                case { ManagedType: { TypeKind: TypeKind.FunctionPointer }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.FunctionPtr, _) }:
                    return Blittable;

                case { ManagedType: { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: NoMarshallingInfo }:
                    return WinBool; // [Compat] Matching the default for the built-in runtime marshallers.
                case { ManagedType: { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.I1 or UnmanagedType.U1, _) }:
                    return ByteBool;
                case { ManagedType: { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.I4 or UnmanagedType.U4 or UnmanagedType.Bool, _) }:
                    return WinBool;
                case { ManagedType: { SpecialType: SpecialType.System_Boolean }, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.VariantBool, _) }:
                    return VariantBool;

                case { ManagedType: { TypeKind: TypeKind.Delegate }, MarshallingAttributeInfo: NoMarshallingInfo or MarshalAsInfo(UnmanagedType.FunctionPtr, _) }:
                    return Delegate;

                case { MarshallingAttributeInfo: SafeHandleMarshallingInfo }:
                    if (!context.CanUseAdditionalTemporaryState)
                    {
                        throw new MarshallingNotSupportedException(info, context);
                    }
                    return new SafeHandleMarshaller(options);

                // Marshalling in new model.
                // Must go before the cases that do not explicitly check for marshalling info to support
                // the user overridding the default marshalling rules with a MarshalUsing attribute.
                case { MarshallingAttributeInfo: NativeMarshallingAttributeInfo marshalInfo }:
                    return CreateCustomNativeTypeMarshaller(info, context, marshalInfo);

                case { MarshallingAttributeInfo: BlittableTypeAttributeInfo }:
                    return Blittable;

                // Simple generated marshalling with new attribute model, only have type name.
                case { MarshallingAttributeInfo: GeneratedNativeMarshallingAttributeInfo(string nativeTypeName) }:
                    return Forwarder;

                // Cases that just match on type must come after the checks that match only on marshalling attribute info.
                // The checks below do not account for generic marshalling overrides like [MarshalUsing], so those checks must come first.
                case { ManagedType: { SpecialType: SpecialType.System_Char } }:
                    return CreateCharMarshaller(info, context);

                case { ManagedType: { SpecialType: SpecialType.System_String } }:
                    return CreateStringMarshaller(info, context);
                    
                case { ManagedType: IArrayTypeSymbol { IsSZArray: true, ElementType: ITypeSymbol elementType } }:
                    return CreateArrayMarshaller(info, context, options, elementType);

                case { ManagedType: { SpecialType: SpecialType.System_Void } }:
                    return Forwarder;

                default:
                    throw new MarshallingNotSupportedException(info, context);
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
        
        private static ExpressionSyntax GetNumElementsExpressionFromMarshallingInfo(TypePositionInfo info, StubCodeContext context, AnalyzerConfigOptions options)
        {
            ExpressionSyntax numElementsExpression;
            if (info.MarshallingAttributeInfo is not ArrayMarshalAsInfo marshalAsInfo)
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = Resources.ArraySizeMustBeSpecified
                };
            }

            LiteralExpressionSyntax? constSizeExpression = marshalAsInfo.ArraySizeConst != ArrayMarshalAsInfo.UnspecifiedData
                ? LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(marshalAsInfo.ArraySizeConst))
                : null;
            ExpressionSyntax? sizeParamIndexExpression = null;
            if (marshalAsInfo.ArraySizeParamIndex != ArrayMarshalAsInfo.UnspecifiedData)
            {
                TypePositionInfo? paramIndexInfo = context.GetTypePositionInfoForManagedIndex(marshalAsInfo.ArraySizeParamIndex);
                if (paramIndexInfo is null)
                {
                    throw new MarshallingNotSupportedException(info, context)
                    {
                        NotSupportedDetails = Resources.ArraySizeParamIndexOutOfRange
                    };
                }
                else if (!paramIndexInfo.ManagedType.IsIntegralType())
                {
                    throw new MarshallingNotSupportedException(info, context)
                    {
                        NotSupportedDetails = Resources.ArraySizeParamTypeMustBeIntegral
                    };
                }
                else
                {
                    var (managed, native) = context.GetIdentifiers(paramIndexInfo);
                    string identifier = Create(paramIndexInfo, context, options).UsesNativeIdentifier(paramIndexInfo, context) ? native : managed;
                    sizeParamIndexExpression = CastExpression(
                            PredefinedType(Token(SyntaxKind.IntKeyword)),
                            IdentifierName(identifier));
                }
            }
            numElementsExpression = (constSizeExpression, sizeParamIndexExpression) switch
            {
                (null, null) => throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = Resources.ArraySizeMustBeSpecified
                },
                (not null, null) => constSizeExpression!,
                (null, not null) => CheckedExpression(SyntaxKind.CheckedExpression, sizeParamIndexExpression!),
                (not null, not null) => CheckedExpression(SyntaxKind.CheckedExpression, BinaryExpression(SyntaxKind.AddExpression, constSizeExpression!, sizeParamIndexExpression!))
            };
            return numElementsExpression;
        }

        private static IMarshallingGenerator CreateArrayMarshaller(TypePositionInfo info, StubCodeContext context, AnalyzerConfigOptions options, ITypeSymbol elementType)
        {
            var elementMarshallingInfo = info.MarshallingAttributeInfo switch
            {
                ArrayMarshalAsInfo(UnmanagedType.LPArray, _) marshalAs => marshalAs.ElementMarshallingInfo,
                ArrayMarshallingInfo marshalInfo => marshalInfo.ElementMarshallingInfo,
                NoMarshallingInfo _ => NoMarshallingInfo.Instance,
                _ => throw new MarshallingNotSupportedException(info, context)
            };

            var elementMarshaller = Create(
                TypePositionInfo.CreateForType(elementType, elementMarshallingInfo),
                new ArrayMarshallingCodeContext(StubCodeContext.Stage.Setup, string.Empty, context, false),
                options);
            ExpressionSyntax numElementsExpression = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0));
            if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
            {
                // In this case, we need a numElementsExpression supplied from metadata, so we'll calculate it here.
                numElementsExpression = GetNumElementsExpressionFromMarshallingInfo(info, context, options);
            }
            
            return elementMarshaller == Blittable
                ? new BlittableArrayMarshaller(numElementsExpression)
                : new NonBlittableArrayMarshaller(elementMarshaller, numElementsExpression);
        }

        private static IMarshallingGenerator CreateCustomNativeTypeMarshaller(TypePositionInfo info, StubCodeContext context, NativeMarshallingAttributeInfo marshalInfo)
        {
            if (marshalInfo.ValuePropertyType is not null && !context.CanUseAdditionalTemporaryState)
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = Resources.ValuePropertyMarshallingRequiresAdditionalState
                };
            }

            // The marshalling method for this type doesn't support marshalling from native to managed,
            // but our scenario requires marshalling from native to managed.
            if ((info.RefKind == RefKind.Ref || info.RefKind == RefKind.Out || info.IsManagedReturnPosition) 
                && (marshalInfo.MarshallingMethods & SupportedMarshallingMethods.NativeToManaged) == 0)
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = string.Format(Resources.CustomTypeMarshallingNativeToManagedUnsupported, marshalInfo.NativeMarshallingType.ToDisplayString())
                };
            }
            // The marshalling method for this type doesn't support marshalling from managed to native by value,
            // but our scenario requires marshalling from managed to native by value.
            // Pinning is required for the stackalloc marshalling to enable users to safely pass the stackalloc Span's byref
            // to native if we ever start using a conditional stackalloc method and cannot guarantee that the Span we provide
            // the user with is backed by stack allocated memory.
            else if (!info.IsByRef 
                && (marshalInfo.MarshallingMethods & SupportedMarshallingMethods.ManagedToNative) == 0 
                && !(context.PinningSupported && (marshalInfo.MarshallingMethods & SupportedMarshallingMethods.Pinning) == 0) 
                && !(context.StackSpaceUsable && context.PinningSupported && (marshalInfo.MarshallingMethods & SupportedMarshallingMethods.ManagedToNativeStackalloc) == 0))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = string.Format(Resources.CustomTypeMarshallingManagedToNativeUnsupported, marshalInfo.NativeMarshallingType.ToDisplayString())
                };
            }
            // The marshalling method for this type doesn't support marshalling from managed to native by reference,
            // but our scenario requires marshalling from managed to native by reference.
            // "in" byref supports stack marshalling.
            else if (info.RefKind == RefKind.In 
                && (marshalInfo.MarshallingMethods & SupportedMarshallingMethods.ManagedToNative) == 0 
                && !(context.StackSpaceUsable && context.PinningSupported && (marshalInfo.MarshallingMethods & SupportedMarshallingMethods.ManagedToNativeStackalloc) != 0))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = string.Format(Resources.CustomTypeMarshallingManagedToNativeUnsupported, marshalInfo.NativeMarshallingType.ToDisplayString())
                };
            }
            // The marshalling method for this type doesn't support marshalling from managed to native by reference,
            // but our scenario requires marshalling from managed to native by reference.
            // "ref" byref marshalling doesn't support stack marshalling
            else if (info.RefKind == RefKind.Ref 
                && (marshalInfo.MarshallingMethods & SupportedMarshallingMethods.ManagedToNative) == 0)
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = string.Format(Resources.CustomTypeMarshallingManagedToNativeUnsupported, marshalInfo.NativeMarshallingType.ToDisplayString())
                };
            }
            
            return new CustomNativeTypeMarshaller(marshalInfo);
        }
    }
}
