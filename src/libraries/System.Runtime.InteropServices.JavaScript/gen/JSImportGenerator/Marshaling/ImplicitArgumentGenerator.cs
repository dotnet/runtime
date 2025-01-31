// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class ImplicitArgumentGenerator(TypePositionInfo info, StubCodeContext codeContext) : BaseJSGenerator(info, codeContext)
    {
        public override IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            if (context.CurrentStage == StubIdentifierContext.Stage.Setup)
            {
                var (_, js) = context.GetIdentifiers(TypeInfo);
                return [
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(js),
                                IdentifierName("Initialize")),
                            ArgumentList()))
                ];
            }

            return [];
        }
    }
}
