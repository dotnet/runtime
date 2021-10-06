﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using static Microsoft.Interop.MarshallerHelpers;

namespace Microsoft.Interop
{
    public sealed class PlatformDefinedStringMarshaller : ConditionalStackallocMarshallingGenerator
    {
        private static readonly TypeSyntax s_nativeType = PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)));

        private readonly IMarshallingGenerator _windowsMarshaller;
        private readonly IMarshallingGenerator _nonWindowsMarshaller;

        public PlatformDefinedStringMarshaller(IMarshallingGenerator windowsMarshaller, IMarshallingGenerator nonWindowsMarshaller)
        {
            _windowsMarshaller = windowsMarshaller;
            _nonWindowsMarshaller = nonWindowsMarshaller;
        }

        public override ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            ExpressionSyntax windowsExpr = _windowsMarshaller.AsArgument(info, context).Expression;
            ExpressionSyntax nonWindowsExpr = _nonWindowsMarshaller.AsArgument(info, context).Expression;

            // If the Windows and non-Windows syntax are equivalent, just return one of them.
            if (windowsExpr.IsEquivalentTo(nonWindowsExpr))
                return Argument(windowsExpr);

            // OperatingSystem.IsWindows() ? << Windows code >> : << non-Windows code >>
            return Argument(
                ConditionalExpression(
                    IsWindows,
                    windowsExpr,
                    nonWindowsExpr));
        }

        public override TypeSyntax AsNativeType(TypePositionInfo info)
        {
            // void*
            return s_nativeType;
        }

        public override ParameterSyntax AsParameter(TypePositionInfo info)
        {
            // void**
            // or
            // void*
            TypeSyntax type = info.IsByRef
                ? PointerType(AsNativeType(info))
                : AsNativeType(info);
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(type);
        }

        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    if (TryGenerateSetupSyntax(info, context, out StatementSyntax conditionalAllocSetup))
                        yield return conditionalAllocSetup;

                    break;
                case StubCodeContext.Stage.Marshal:
                    if (info.RefKind != RefKind.Out)
                    {
                        if (TryGetConditionalBlockForStatements(
                                _windowsMarshaller.Generate(info, context),
                                _nonWindowsMarshaller.Generate(info, context),
                                out StatementSyntax marshal))
                        {
                            yield return marshal;
                        }
                    }
                    break;
                case StubCodeContext.Stage.Pin:
                    // [Compat] The built-in system could determine the platform at runtime and pin only on
                    // the platform on which is is needed. In the generated source, if pinning is needed for
                    // any platform, it is done on every platform.
                    foreach (StatementSyntax s in _windowsMarshaller.Generate(info, context))
                        yield return s;

                    foreach (StatementSyntax s in _nonWindowsMarshaller.Generate(info, context))
                        yield return s;

                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
                    {
                        if (TryGetConditionalBlockForStatements(
                                _windowsMarshaller.Generate(info, context),
                                _nonWindowsMarshaller.Generate(info, context),
                                out StatementSyntax unmarshal))
                        {
                            yield return unmarshal;
                        }
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
        // It relies on the UTF-16 (Windows) and UTF-8 (non-Windows) string marshallers for allocation/marshalling.
        protected override ExpressionSyntax GenerateAllocationExpression(TypePositionInfo info, StubCodeContext context, SyntaxToken byteLengthIdentifier, out bool allocationRequiresByteLength) => throw new NotImplementedException();
        protected override ExpressionSyntax GenerateByteLengthCalculationExpression(TypePositionInfo info, StubCodeContext context) => throw new NotImplementedException();
        protected override StatementSyntax GenerateStackallocOnlyValueMarshalling(TypePositionInfo info, StubCodeContext context, SyntaxToken byteLengthIdentifier, SyntaxToken stackAllocPtrIdentifier) => throw new NotImplementedException();

        protected override ExpressionSyntax GenerateFreeExpression(TypePositionInfo info, StubCodeContext context)
        {
            return StringMarshaller.FreeExpression(context.GetIdentifiers(info).native);
        }

        private bool TryGetConditionalBlockForStatements(
            IEnumerable<StatementSyntax> windowsStatements,
            IEnumerable<StatementSyntax> nonWindowsStatements,
            out StatementSyntax conditionalBlock)
        {
            conditionalBlock = EmptyStatement();

            bool hasWindowsStatements = windowsStatements.Any();
            bool hasNonWindowsStatements = nonWindowsStatements.Any();
            if (hasWindowsStatements)
            {
                IfStatementSyntax windowsIfBlock = IfStatement(IsWindows, Block(windowsStatements));
                if (hasNonWindowsStatements)
                {
                    // if (OperatingSystem.IsWindows())
                    // {
                    //     << Windows code >>
                    // }
                    // else
                    // {
                    //     << non-Windows code >>
                    // }
                    conditionalBlock = windowsIfBlock.WithElse(
                        ElseClause(Block(nonWindowsStatements)));
                }
                else
                {
                    // if (OperatingSystem.IsWindows())
                    // {
                    //     << Windows code >>
                    // }
                    conditionalBlock = windowsIfBlock;
                }

                return true;
            }
            else if (hasNonWindowsStatements)
            {
                // if (!OperatingSystem.IsWindows())
                // {
                //     << non-Windows code >>
                // }
                conditionalBlock = IfStatement(PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, IsWindows),
                    Block(nonWindowsStatements));

            }

            return hasWindowsStatements || hasNonWindowsStatements;
        }
    }
}
