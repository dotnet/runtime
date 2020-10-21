using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal class Utf16StringMarshaller : IMarshallingGenerator
    {
        // [Compat] Equivalent of MAX_PATH on Windows to match built-in system
        // The assumption is file paths are the most common case for marshalling strings,
        // so the threshold for optimized allocation is based on that length.
        private const int StackAllocBytesThreshold = 260 * sizeof(ushort);

        private static readonly TypeSyntax InteropServicesMarshalType = ParseTypeName(TypeNames.System_Runtime_InteropServices_Marshal);
        private static readonly TypeSyntax NativeType = PointerType(PredefinedType(Token(SyntaxKind.UShortKeyword)));

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            string identifier = context.GetIdentifiers(info).native;
            if (info.IsByRef)
            {
                // &<nativeIdentifier>
                return Argument(
                    PrefixUnaryExpression(
                        SyntaxKind.AddressOfExpression,
                        IdentifierName(identifier)));
            }
            else if (context.PinningSupported)
            {
                // (ushort*)<nativeIdentifier>
                return Argument(
                    CastExpression(
                        AsNativeType(info),
                        IdentifierName(identifier)));
            }

            // <nativeIdentifier>
            return Argument(IdentifierName(identifier));
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            // ushort*
            return NativeType;
        }

        public ParameterSyntax AsParameter(TypePositionInfo info)
        {
            // ushort**
            // or
            // ushort*
            var type = info.IsByRef
                ? PointerType(AsNativeType(info))
                : AsNativeType(info);
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(type);
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            if (context.PinningSupported && !info.IsByRef && !info.IsManagedReturnPosition)
            {
                if (context.CurrentStage == StubCodeContext.Stage.Pin)
                {
                    // fixed (char* <nativeIdentifier> = <managedIdentifier>)
                    yield return FixedStatement(
                        VariableDeclaration(
                            PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier(nativeIdentifier))
                                    .WithInitializer(EqualsValueClause(IdentifierName(managedIdentifier))))),
                        EmptyStatement());
                }

                yield break;
            }

            string usedCoTaskMemIdentifier = $"{managedIdentifier}__usedCoTaskMem";
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    // ushort* <native>
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            AsNativeType(info),
                            SingletonSeparatedList(VariableDeclarator(nativeIdentifier))));
                    break;
                case StubCodeContext.Stage.Marshal:
                    if (info.RefKind != RefKind.Out)
                    {
                        // <nativeIdentifier> = (ushort*)Marshal.StringToCoTaskMemUni(<managedIdentifier>)
                        var coTaskMemAlloc = ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(nativeIdentifier),
                                CastExpression(
                                    AsNativeType(info),
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            InteropServicesMarshalType,
                                            IdentifierName("StringToCoTaskMemUni")),
                                        ArgumentList(
                                            SingletonSeparatedList<ArgumentSyntax>(
                                                Argument(IdentifierName(managedIdentifier))))))));
                        if (info.IsByRef && info.RefKind != RefKind.In)
                        {
                            yield return coTaskMemAlloc;
                        }
                        else
                        {
                            // <usedCoTaskMem> = false;
                            yield return LocalDeclarationStatement(
                                VariableDeclaration(
                                    PredefinedType(Token(SyntaxKind.BoolKeyword)),
                                    SingletonSeparatedList(
                                        VariableDeclarator(usedCoTaskMemIdentifier)
                                            .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.FalseLiteralExpression))))));

                            string stackAllocPtrIdentifier = $"{managedIdentifier}__stackalloc";
                            string byteLenIdentifier = $"{managedIdentifier}__byteLen";

                            // <managed>.Length + 1
                            ExpressionSyntax lengthWithNullTerminator = BinaryExpression(
                                SyntaxKind.AddExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(managedIdentifier),
                                    IdentifierName("Length")),
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)));

                            // Code block for stackalloc if string is below threshold size
                            var marshalOnStack = Block(
                                // ushort* <stackAllocPtr> = stackalloc ushort[<managed>.Length + 1];
                                LocalDeclarationStatement(
                                    VariableDeclaration(
                                        PointerType(PredefinedType(Token(SyntaxKind.UShortKeyword))),
                                        SingletonSeparatedList(
                                            VariableDeclarator(stackAllocPtrIdentifier)
                                                .WithInitializer(EqualsValueClause(
                                                    StackAllocArrayCreationExpression(
                                                        ArrayType(
                                                            PredefinedType(Token(SyntaxKind.UShortKeyword)),
                                                            SingletonList<ArrayRankSpecifierSyntax>(
                                                                ArrayRankSpecifier(SingletonSeparatedList(lengthWithNullTerminator)))))))))),
                                // ((ReadOnlySpan<char>)<managed>).CopyTo(new Span<char>(<stackAllocPtr>, <managed>.Length + 1));
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                         ParenthesizedExpression(
                                            CastExpression(
                                                GenericName(Identifier("System.ReadOnlySpan"),
                                                    TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                                        PredefinedType(Token(SyntaxKind.CharKeyword))))),
                                                IdentifierName(managedIdentifier))),
                                            IdentifierName("CopyTo")),
                                        ArgumentList(
                                            SeparatedList(new ArgumentSyntax[] {
                                                Argument(
                                                    ObjectCreationExpression(
                                                        GenericName(Identifier("System.Span"),
                                                            TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                                                PredefinedType(Token(SyntaxKind.CharKeyword))))),
                                                        ArgumentList(
                                                            SeparatedList(new ArgumentSyntax[]{
                                                                Argument(IdentifierName(stackAllocPtrIdentifier)),
                                                                Argument(lengthWithNullTerminator)})),
                                                        initializer: null))})))),
                                // <native> = <stackAllocPtr>;
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(nativeIdentifier),
                                        IdentifierName(stackAllocPtrIdentifier))));

                            // if (<managed> == null)
                            // {
                            //     <native> = null;
                            // }
                            // else
                            // {
                            //     int <byteLen> = (<managed>.Length + 1) * sizeof(ushort);
                            //     if (<byteLen> > <StackAllocBytesThreshold>)
                            //     {
                            //         <native> = (ushort*)Marshal.StringToCoTaskMemUni(<managed>);
                            //         <usedCoTaskMem> = true;
                            //     }
                            //     else
                            //     {
                            //         ushort* <stackAllocPtr> = stackalloc ushort[<managed>.Length + 1];
                            //         ((ReadOnlySpan<char>)<managed>).CopyTo(new Span<char>(<stackAllocPtr>, <managed>.Length + 1));
                            //         <native> = <stackAllocPtr>;
                            //     }
                            // }
                            yield return IfStatement(
                                BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    IdentifierName(managedIdentifier),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                Block(
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(nativeIdentifier),
                                            LiteralExpression(SyntaxKind.NullLiteralExpression)))),
                                ElseClause(
                                    Block(
                                        LocalDeclarationStatement(
                                            VariableDeclaration(
                                                PredefinedType(Token(SyntaxKind.IntKeyword)),
                                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                                    VariableDeclarator(Identifier(byteLenIdentifier))
                                                        .WithInitializer(EqualsValueClause(
                                                            BinaryExpression(
                                                                SyntaxKind.MultiplyExpression,
                                                                ParenthesizedExpression(lengthWithNullTerminator),
                                                                SizeOfExpression(PredefinedType(Token(SyntaxKind.UShortKeyword))))))))),
                                        IfStatement(
                                            BinaryExpression(
                                                SyntaxKind.GreaterThanExpression,
                                                IdentifierName(byteLenIdentifier),
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(StackAllocBytesThreshold))),
                                            Block(
                                                coTaskMemAlloc,
                                                ExpressionStatement(
                                                    AssignmentExpression(
                                                        SyntaxKind.SimpleAssignmentExpression,
                                                        IdentifierName(usedCoTaskMemIdentifier),
                                                        LiteralExpression(SyntaxKind.TrueLiteralExpression)))),
                                            ElseClause(marshalOnStack)))));
                        }
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
                    {
                        // <managed> = <native> == null ? null : new string((char*)<native>);
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.EqualsExpression,
                                        IdentifierName(nativeIdentifier),
                                        LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression),
                                    ObjectCreationExpression(
                                        PredefinedType(Token(SyntaxKind.StringKeyword)),
                                        ArgumentList(SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                CastExpression(
                                                    PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                                                    IdentifierName(nativeIdentifier))))),
                                        initializer: null))));
                    }
                    break;
                case StubCodeContext.Stage.Cleanup:
                    // Marshal.FreeCoTaskMem((IntPtr)<native>)
                    var freeCoTaskMem = ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                InteropServicesMarshalType,
                                IdentifierName("FreeCoTaskMem")),
                            ArgumentList(
                                SingletonSeparatedList<ArgumentSyntax>(
                                    Argument(
                                        CastExpression(
                                            ParseTypeName("System.IntPtr"),
                                            IdentifierName(nativeIdentifier)))))));

                    if (info.IsByRef && info.RefKind != RefKind.In)
                    {
                        yield return freeCoTaskMem;
                    }
                    else
                    {
                        // if (<usedCoTaskMem>)
                        // {
                        //     Marshal.FreeCoTaskMem((IntPtr)<native>)
                        // }
                        yield return IfStatement(
                            IdentifierName(usedCoTaskMemIdentifier),
                            Block(freeCoTaskMem));
                    }

                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
    }
}
