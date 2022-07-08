// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class ArraySegmentJSGenerator : PrimitiveJSGenerator
    {
        private MarshalerType _elemenetMarshalerType;
        public ArraySegmentJSGenerator(MarshalerType elemenetMarshalerType)
            : base(MarshalerType.ArraySegment)
        {
            _elemenetMarshalerType = elemenetMarshalerType;
        }

        public override IEnumerable<ExpressionSyntax> GenerateBind(TypePositionInfo info, StubCodeContext context)
        {
            yield return InvocationExpression(MarshalerTypeName(Type),
                ArgumentList(SingletonSeparatedList(Argument(MarshalerTypeName(_elemenetMarshalerType)))));
        }
    }
}
