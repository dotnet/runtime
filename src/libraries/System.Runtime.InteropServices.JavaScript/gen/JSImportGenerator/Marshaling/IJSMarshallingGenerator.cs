// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop.JavaScript
{
    internal interface IJSMarshallingGenerator : IMarshallingGenerator
    {
        IEnumerable<ExpressionSyntax> GenerateBind(TypePositionInfo info, StubCodeContext context);
    }
}
