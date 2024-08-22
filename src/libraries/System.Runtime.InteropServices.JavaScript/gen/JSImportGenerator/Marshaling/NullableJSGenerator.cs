// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class NullableJSGenerator : PrimitiveJSGenerator
    {
        public NullableJSGenerator(TypePositionInfo info, StubCodeContext context, MarshalerType resultMarshalerType)
            : base(info, context, resultMarshalerType)
        {
        }

        public override IEnumerable<ExpressionSyntax> GenerateBind()
        {
            yield return InvocationExpression(MarshalerTypeName(MarshalerType.Nullable),
                    ArgumentList(SingletonSeparatedList(Argument(MarshalerTypeName(Type)))));
        }
    }
}
