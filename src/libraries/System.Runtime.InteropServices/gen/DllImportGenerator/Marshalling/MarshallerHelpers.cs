using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal static class MarshallerHelpers
    {
        public static readonly ExpressionSyntax IsWindows = InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            ParseTypeName("System.OperatingSystem"),
                                                            IdentifierName("IsWindows")));

        public static readonly TypeSyntax InteropServicesMarshalType = ParseTypeName(TypeNames.System_Runtime_InteropServices_Marshal);

        public static readonly TypeSyntax SystemIntPtrType = ParseTypeName("System.IntPtr");

        public static ForStatementSyntax GetForLoop(string collectionIdentifier, string indexerIdentifier)
        {
            // for(int <indexerIdentifier> = 0; <indexerIdentifier> < <collectionIdentifier>.Length; ++<indexerIdentifier>)
            //      ;
            return ForStatement(EmptyStatement())
            .WithDeclaration(
                VariableDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.IntKeyword)))
                .WithVariables(
                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                        VariableDeclarator(
                            Identifier(indexerIdentifier))
                        .WithInitializer(
                            EqualsValueClause(
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0)))))))
            .WithCondition(
                BinaryExpression(
                    SyntaxKind.LessThanExpression,
                    IdentifierName(indexerIdentifier),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(collectionIdentifier),
                        IdentifierName("Length"))))
            .WithIncrementors(
                SingletonSeparatedList<ExpressionSyntax>(
                    PrefixUnaryExpression(
                        SyntaxKind.PreIncrementExpression,
                        IdentifierName(indexerIdentifier))));
        }

        public static LocalDeclarationStatementSyntax DeclareWithDefault(TypeSyntax typeSyntax, string identifier)
        {
            // <type> <identifier> = default;
            return LocalDeclarationStatement(
                VariableDeclaration(
                    typeSyntax,
                    SingletonSeparatedList(
                        VariableDeclarator(identifier)
                            .WithInitializer(
                                EqualsValueClause(
                                    LiteralExpression(SyntaxKind.DefaultLiteralExpression))))));
        }

        public static RefKind GetRefKindForByValueContentsKind(this ByValueContentsMarshalKind byValue)
        {
            return byValue switch
            {
                ByValueContentsMarshalKind.Default => RefKind.None,
                ByValueContentsMarshalKind.In => RefKind.In,
                ByValueContentsMarshalKind.InOut => RefKind.Ref,
                ByValueContentsMarshalKind.Out => RefKind.Out,
                _ => throw new System.ArgumentOutOfRangeException(nameof(byValue))
            };
        }

        public static TypeSyntax GetCompatibleGenericTypeParameterSyntax(this TypeSyntax type)
        {
            TypeSyntax spanElementTypeSyntax = type;
            if (spanElementTypeSyntax is PointerTypeSyntax)
            {
                // Pointers cannot be passed to generics, so use IntPtr for this case.
                spanElementTypeSyntax = SystemIntPtrType;
            }
            return spanElementTypeSyntax;
        }

        private const string MarshalerLocalSuffix = "marshaler";
        public static string GetMarshallerIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetAdditionalIdentifier(info, MarshalerLocalSuffix);
        }

        public static class StringMarshaller
        {
            public static ExpressionSyntax AllocationExpression(CharEncoding encoding, string managedIdentifier)
            {
                string methodName = encoding switch
                {
                    CharEncoding.Utf8 => "StringToCoTaskMemUTF8", // Not in .NET Standard 2.0, so we use the hard-coded name 
                    CharEncoding.Utf16 => nameof(System.Runtime.InteropServices.Marshal.StringToCoTaskMemUni),
                    CharEncoding.Ansi => nameof(System.Runtime.InteropServices.Marshal.StringToCoTaskMemAnsi),
                    _ => throw new System.ArgumentOutOfRangeException(nameof(encoding))
                };

                // Marshal.StringToCoTaskMemUTF8(<managed>)
                // or
                // Marshal.StringToCoTaskMemUni(<managed>)
                // or
                // Marshal.StringToCoTaskMemAnsi(<managed>)
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InteropServicesMarshalType,
                        IdentifierName(methodName)),
                    ArgumentList(
                        SingletonSeparatedList<ArgumentSyntax>(
                            Argument(IdentifierName(managedIdentifier)))));
            }

            public static ExpressionSyntax FreeExpression(string nativeIdentifier)
            {
                // Marshal.FreeCoTaskMem((IntPtr)<nativeIdentifier>)
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InteropServicesMarshalType,
                        IdentifierName(nameof(System.Runtime.InteropServices.Marshal.FreeCoTaskMem))),
                    ArgumentList(SingletonSeparatedList(
                        Argument(
                            CastExpression(
                                SystemIntPtrType,
                                IdentifierName(nativeIdentifier))))));
            }
        }
    }
}