using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    class CustomNativeTypeMarshaller : IMarshallingGenerator
    {
        private const string MarshalerLocalSuffix = "__marshaler";
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly TypeSyntax _nativeLocalTypeSyntax;
        private readonly SupportedMarshallingMethods _marshallingMethods;
        private readonly bool _hasFreeNative;
        private readonly bool _useValueProperty;
        private readonly bool _marshalerTypePinnable;

        public CustomNativeTypeMarshaller(NativeMarshallingAttributeInfo marshallingInfo)
        {
            ITypeSymbol nativeType = marshallingInfo.ValuePropertyType ?? marshallingInfo.NativeMarshallingType;
            _nativeTypeSyntax = ParseTypeName(nativeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            _nativeLocalTypeSyntax = ParseTypeName(marshallingInfo.NativeMarshallingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            _marshallingMethods = marshallingInfo.MarshallingMethods;
            _hasFreeNative = ManualTypeMarshallingHelper.HasFreeNativeMethod(marshallingInfo.NativeMarshallingType);
            _useValueProperty = marshallingInfo.ValuePropertyType != null;
            _marshalerTypePinnable = marshallingInfo.NativeTypePinnable;
        }

        public CustomNativeTypeMarshaller(GeneratedNativeMarshallingAttributeInfo marshallingInfo)
        {
            _nativeTypeSyntax = _nativeLocalTypeSyntax = ParseTypeName(marshallingInfo.NativeMarshallingFullyQualifiedTypeName);
            _marshallingMethods = SupportedMarshallingMethods.ManagedToNative | SupportedMarshallingMethods.NativeToManaged;
            _hasFreeNative = true;
            _useValueProperty = false;
            _marshalerTypePinnable = false;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeSyntax;
        }

        public ParameterSyntax AsParameter(TypePositionInfo info)
        {
            var type = info.IsByRef
                ? PointerType(AsNativeType(info))
                : AsNativeType(info);
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(type);
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            string identifier = context.GetIdentifiers(info).native;
            if (info.IsByRef)
            {
                return Argument(
                    PrefixUnaryExpression(
                        SyntaxKind.AddressOfExpression,
                        IdentifierName(identifier)));
            }

            if (context.PinningSupported && (_marshallingMethods & SupportedMarshallingMethods.Pinning) != 0)
            {
                return Argument(CastExpression(AsNativeType(info), IdentifierName(identifier)));
            }

            return Argument(IdentifierName(identifier));
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string marshalerIdentifier = _useValueProperty ? nativeIdentifier + MarshalerLocalSuffix  : nativeIdentifier;
            if (!info.IsManagedReturnPosition 
                && !info.IsByRef 
                && context.PinningSupported 
                && (_marshallingMethods & SupportedMarshallingMethods.Pinning) != 0)
            {
                if (context.CurrentStage == StubCodeContext.Stage.Pin)
                {
                    yield return FixedStatement(
                        VariableDeclaration(
                            PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier(nativeIdentifier))
                                    .WithInitializer(EqualsValueClause(
                                        IdentifierName(managedIdentifier)
                                    ))
                            )
                        ),
                        EmptyStatement()
                    );
                }
                yield break;
            }

            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:                    
                    if (_useValueProperty)
                    {
                        yield return LocalDeclarationStatement(
                            VariableDeclaration(
                                _nativeLocalTypeSyntax,
                                SingletonSeparatedList(
                                    VariableDeclarator(marshalerIdentifier)
                                        .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))));
                    }
                    break;
                case StubCodeContext.Stage.Marshal:
                    if (!info.IsManagedReturnPosition && info.RefKind != RefKind.Out)
                    {
                        // Stack space must be usable and the marshaler must support stackalloc to use stackalloc.
                        // We also require pinning to be supported to enable users to pass the stackalloc'd Span
                        // to native code by having the marshaler type return a byref to the Span's elements
                        // in its GetPinnableReference method.
                        bool scenarioSupportsStackalloc = context.StackSpaceUsable 
                            && (_marshallingMethods & SupportedMarshallingMethods.ManagedToNativeStackalloc) != 0 
                            && context.PinningSupported;

                        List<ArgumentSyntax> arguments = new List<ArgumentSyntax>
                        {
                            Argument(IdentifierName(managedIdentifier))
                        };

                        if (scenarioSupportsStackalloc && (!info.IsByRef || info.RefKind == RefKind.In))
                        {
                            string stackallocIdentifier = $"{managedIdentifier}__stackptr";
                            // byte* <managedIdentifier>__stackptr = stackalloc byte[<_nativeLocalType>.StackBufferSize];
                            yield return LocalDeclarationStatement(
                            VariableDeclaration(
                                PointerType(PredefinedType(Token(SyntaxKind.ByteKeyword))),
                                SingletonSeparatedList(
                                    VariableDeclarator(stackallocIdentifier)
                                        .WithInitializer(EqualsValueClause(
                                            StackAllocArrayCreationExpression(
                                                    ArrayType(
                                                        PredefinedType(Token(SyntaxKind.ByteKeyword)),
                                                        SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                _nativeLocalTypeSyntax,
                                                                IdentifierName(ManualTypeMarshallingHelper.StackBufferSizeFieldName))
                                                        ))))))))));

                            // new Span<byte>(<managedIdentifier>__stackptr, <_nativeLocalType>.StackBufferSize)
                            arguments.Add(Argument(
                                                ObjectCreationExpression(
                                                    GenericName(Identifier(TypeNames.System_Span),
                                                        TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                                            PredefinedType(Token(SyntaxKind.ByteKeyword))))))
                                                .WithArgumentList(
                                                    ArgumentList(SeparatedList(new ArgumentSyntax[]
                                                    {
                                                        Argument(IdentifierName(stackallocIdentifier)),
                                                        Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                _nativeLocalTypeSyntax,
                                                                IdentifierName(ManualTypeMarshallingHelper.StackBufferSizeFieldName)))
                                                    })))));
                        }

                        // <marshalerIdentifier> = new <_nativeLocalType>(<arguments>);
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(marshalerIdentifier),
                                ObjectCreationExpression(_nativeLocalTypeSyntax)
                                    .WithArgumentList(ArgumentList(SeparatedList(arguments)))));

                        bool skipValueProperty = _marshalerTypePinnable && (!info.IsByRef || info.RefKind == RefKind.In);

                        if (_useValueProperty && !skipValueProperty)
                        {
                            // <nativeIdentifier> = <marshalerIdentifier>.Value;
                            yield return ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(nativeIdentifier),
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(marshalerIdentifier),
                                        IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName))));
                        }
                    }
                    break;
                case StubCodeContext.Stage.Pin:
                    if (_marshalerTypePinnable && (!info.IsByRef || info.RefKind == RefKind.In))
                    {
                        // fixed (<_nativeTypeSyntax> <nativeIdentifier> = &<marshalerIdentifier>)
                        yield return FixedStatement(
                            VariableDeclaration(
                            _nativeTypeSyntax,
                            SingletonSeparatedList(
                                VariableDeclarator(nativeIdentifier)
                                    .WithInitializer(EqualsValueClause(
                                        PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                            InvocationExpression(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(marshalerIdentifier),
                                                    IdentifierName(ManualTypeMarshallingHelper.GetPinnableReferenceName)),
                                                ArgumentList())))))),
                            EmptyStatement());
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
                    {
                        if (_useValueProperty)
                        {
                            // <marshalerIdentifier>.Value = <nativeIdentifier>;
                            yield return ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(marshalerIdentifier),
                                        IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName)),
                                    IdentifierName(nativeIdentifier)));
                        }

                        // <managedIdentifier> = <marshalerIdentifier>.ToManaged();
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(marshalerIdentifier),
                                        IdentifierName(ManualTypeMarshallingHelper.ToManagedMethodName)))));
                    }
                    break;
                case StubCodeContext.Stage.Cleanup:
                    if (_hasFreeNative)
                    {
                        // <marshalerIdentifier>.FreeNative();
                        yield return ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(marshalerIdentifier),
                                        IdentifierName(ManualTypeMarshallingHelper.FreeNativeMethodName))));
                    }
                    break;
                // TODO: Determine how to keep alive delegates that are in struct fields.
                default:
                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            if (info.IsManagedReturnPosition || info.IsByRef && info.RefKind != RefKind.In)
            {
                return true;
            }
            if (context.PinningSupported)
            {
                if (!info.IsByRef && (_marshallingMethods & SupportedMarshallingMethods.Pinning) != 0)
                {
                    return false;
                }
                else if (_marshalerTypePinnable)
                {
                    return false;
                }
            }
            return true;
        }
        
        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
    }
}
