﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public abstract class BoolMarshallerBase : IMarshallingGenerator
    {
        private readonly PredefinedTypeSyntax _nativeType;
        private readonly int _trueValue;
        private readonly int _falseValue;
        private readonly bool _compareToTrue;

        protected BoolMarshallerBase(PredefinedTypeSyntax nativeType, int trueValue, int falseValue, bool compareToTrue)
        {
            _nativeType = nativeType;
            _trueValue = trueValue;
            _falseValue = falseValue;
            _compareToTrue = compareToTrue;
        }

        public bool IsSupported(TargetFramework target, Version version) => true;

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            Debug.Assert(info.ManagedType is SpecialTypeInfo(_, _, SpecialType.System_Boolean));
            return _nativeType;
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            if (info.IsByRef)
            {
                return ValueBoundaryBehavior.AddressOfNativeIdentifier;
            }

            return ValueBoundaryBehavior.NativeIdentifier;
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    break;
                case StubCodeContext.Stage.Marshal:
                    // <nativeIdentifier> = (<nativeType>)(<managedIdentifier> ? _trueValue : _falseValue);
                    if (info.RefKind != RefKind.Out)
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(nativeIdentifier),
                                CastExpression(
                                    AsNativeType(info),
                                    ParenthesizedExpression(
                                        ConditionalExpression(IdentifierName(managedIdentifier),
                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(_trueValue)),
                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(_falseValue)))))));
                    }

                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
                    {
                        // <managedIdentifier> = <nativeIdentifier> == _trueValue;
                        //   or
                        // <managedIdentifier> = <nativeIdentifier> != _falseValue;
                        (SyntaxKind binaryOp, int comparand) = _compareToTrue ? (SyntaxKind.EqualsExpression, _trueValue) : (SyntaxKind.NotEqualsExpression, _falseValue);

                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                BinaryExpression(
                                    binaryOp,
                                    IdentifierName(nativeIdentifier),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(comparand)))));
                    }
                    break;
                default:
                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
    }

    /// <summary>
    /// Marshals a boolean value as 1 byte.
    /// </summary>
    /// <remarks>
    /// This boolean type is the natural size of a boolean in the CLR (<see href="https://www.ecma-international.org/publications/standards/Ecma-335.htm">ECMA-335 (III.1.1.2)</see>).
    ///
    /// This is typically compatible with <see href="https://en.cppreference.com/w/c/types/boolean">C99</see>
    /// and <see href="https://en.cppreference.com/w/cpp/language/types">C++</see>, but those is implementation defined.
    /// Consult your compiler specification.
    /// </remarks>
    public sealed class ByteBoolMarshaller : BoolMarshallerBase
    {
        /// <summary>
        /// Constructor a <see cref="ByteBoolMarshaller" instance.
        /// </summary>
        /// <param name="signed">True if the byte should be signed, otherwise false</param>
        public ByteBoolMarshaller(bool signed)
            : base(PredefinedType(Token(signed ? SyntaxKind.SByteKeyword : SyntaxKind.ByteKeyword)), trueValue: 1, falseValue: 0, compareToTrue: false)
        {
        }
    }

    /// <summary>
    /// Marshals a boolean value as a 4-byte integer.
    /// </summary>
    /// <remarks>
    /// Corresponds to the definition of <see href="https://docs.microsoft.com/windows/win32/winprog/windows-data-types">BOOL</see>.
    /// </remarks>
    public sealed class WinBoolMarshaller : BoolMarshallerBase
    {
        /// <summary>
        /// Constructor a <see cref="WinBoolMarshaller" instance.
        /// </summary>
        /// <param name="signed">True if the int should be signed, otherwise false</param>
        public WinBoolMarshaller(bool signed)
            : base(PredefinedType(Token(signed ? SyntaxKind.IntKeyword : SyntaxKind.UIntKeyword)), trueValue: 1, falseValue: 0, compareToTrue: false)
        {
        }
    }

    /// <summary>
    /// Marshal a boolean value as a VARIANT_BOOL (Windows OLE/Automation type).
    /// </summary>
    public sealed class VariantBoolMarshaller : BoolMarshallerBase
    {
        private const short VARIANT_TRUE = -1;
        private const short VARIANT_FALSE = 0;
        public VariantBoolMarshaller()
            : base(PredefinedType(Token(SyntaxKind.ShortKeyword)), trueValue: VARIANT_TRUE, falseValue: VARIANT_FALSE, compareToTrue: true)
        {
        }
    }
}
