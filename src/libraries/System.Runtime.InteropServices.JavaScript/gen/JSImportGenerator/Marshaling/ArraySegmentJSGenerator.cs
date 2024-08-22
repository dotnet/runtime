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
        private readonly MarshalerType _elementMarshalerType;

        public ArraySegmentJSGenerator(TypePositionInfo info, MarshalerType elementMarshalerType)
            : base(info, MarshalerType.ArraySegment)
        {
            _elementMarshalerType = elementMarshalerType;
        }

        public override IEnumerable<ExpressionSyntax> GenerateBind()
        {
            yield return InvocationExpression(MarshalerTypeName(Type),
                ArgumentList(SingletonSeparatedList(Argument(MarshalerTypeName(_elementMarshalerType)))));
        }

        public override IBoundMarshallingGenerator Rebind(TypePositionInfo info) => new ArraySegmentJSGenerator(info, _elementMarshalerType);
    }
}
