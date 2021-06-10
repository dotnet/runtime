using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using static Microsoft.Interop.MarshallerHelpers;

namespace Microsoft.Interop
{
    internal sealed class AnsiStringMarshaller : ConditionalStackallocMarshallingGenerator
    {
        private static readonly TypeSyntax NativeType = PointerType(PredefinedType(Token(SyntaxKind.ByteKeyword)));

        private readonly Utf8StringMarshaller utf8StringMarshaller;

        public AnsiStringMarshaller(Utf8StringMarshaller utf8StringMarshaller)
        {
            this.utf8StringMarshaller = utf8StringMarshaller;
        }

        public override ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
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

            // <nativeIdentifier>
            return Argument(IdentifierName(identifier));
        }

        public override TypeSyntax AsNativeType(TypePositionInfo info)
        {
            // byte*
            return NativeType;
        }

        public override ParameterSyntax AsParameter(TypePositionInfo info)
        {
            // byte**
            // or
            // byte*
            var type = info.IsByRef
                ? PointerType(AsNativeType(info))
                : AsNativeType(info);
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(type);
        }

        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    if (TryGenerateSetupSyntax(info, context, out StatementSyntax conditionalAllocSetup))
                        yield return conditionalAllocSetup;

                    break;
                case StubCodeContext.Stage.Marshal:
                    if (info.RefKind != RefKind.Out)
                    {
                        // <native> = (byte*)Marshal.StringToCoTaskMemAnsi(<managed>);
                        var windowsBlock = Block(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(nativeIdentifier),
                                    CastExpression(
                                        AsNativeType(info),
                                        StringMarshaller.AllocationExpression(CharEncoding.Ansi, managedIdentifier)))));

                        // Set the allocation marker to true if it is being used
                        if (UsesConditionalStackAlloc(info, context))
                        {
                            // <allocationMarker> = true
                            windowsBlock = windowsBlock.AddStatements(
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(GetAllocationMarkerIdentifier(info, context)),
                                        LiteralExpression(SyntaxKind.TrueLiteralExpression))));
                        }

                        // [Compat] The generated source for ANSI string marshalling does not optimize for
                        // allocating on the stack based on the string length. It always uses AllocCoTaskMem.
                        // if (OperatingSystem.IsWindows())
                        // {
                        //     <native> = (byte*)Marshal.StringToCoTaskMemAnsi(<managed>);
                        // }
                        // else
                        // {
                        //     << marshal as UTF-8 >>
                        // }
                        yield return IfStatement(IsWindows,
                            windowsBlock,
                            ElseClause(
                                Block(this.utf8StringMarshaller.Generate(info, context))));
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
                    {
                        // if (OperatingSystem.IsWindows())
                        // {
                        //     <managed> = <native> == null ? null : new string((sbyte*)<native>);
                        // }
                        // else
                        // {
                        //     << unmarshal as UTF-8 >>
                        // }
                        yield return IfStatement(IsWindows,
                            Block(
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(managedIdentifier),
                                        ConditionalExpression(
                                            BinaryExpression(
                                                SyntaxKind.EqualsExpression,
                                                IdentifierName(nativeIdentifier),
                                                LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                            LiteralExpression(SyntaxKind.NullLiteralExpression),
                                            ObjectCreationExpression(
                                                PredefinedType(Token(SyntaxKind.StringKeyword)),
                                                ArgumentList(SingletonSeparatedList(
                                                    Argument(
                                                        CastExpression(
                                                            PointerType(PredefinedType(Token(SyntaxKind.SByteKeyword))),
                                                            IdentifierName(nativeIdentifier))))),
                                                initializer: null))))),
                            ElseClause(
                                Block(this.utf8StringMarshaller.Generate(info, context))));
                    }
                    break;
                case StubCodeContext.Stage.Cleanup:
                    yield return GenerateConditionalAllocationFreeSyntax(info, context);
                    break;
            }
        }

        public override bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        
        public override bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;

        // This marshaller only uses the conditional allocaction base for setup and cleanup.
        // It always allocates for ANSI (Windows) and relies on the UTF-8 (non-Windows) string marshaller for allocation/marshalling.
        protected override ExpressionSyntax GenerateAllocationExpression(TypePositionInfo info, StubCodeContext context, SyntaxToken byteLengthIdentifier, out bool allocationRequiresByteLength) => throw new NotImplementedException();
        protected override ExpressionSyntax GenerateByteLengthCalculationExpression(TypePositionInfo info, StubCodeContext context) => throw new NotImplementedException();
        protected override StatementSyntax GenerateStackallocOnlyValueMarshalling(TypePositionInfo info, StubCodeContext context, SyntaxToken byteLengthIdentifier, SyntaxToken stackAllocPtrIdentifier) => throw new NotImplementedException();

        protected override ExpressionSyntax GenerateFreeExpression(TypePositionInfo info, StubCodeContext context)
        {
            return StringMarshaller.FreeExpression(context.GetIdentifiers(info).native);
        }
    }
}
