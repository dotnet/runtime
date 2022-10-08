// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class EmptyJSGenerator : IJSMarshallingGenerator
    {
        public TypeSyntax AsNativeType(TypePositionInfo info) => info.ManagedType.Syntax;
        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<ExpressionSyntax> GenerateBind(TypePositionInfo info, StubCodeContext context) => Array.Empty<ExpressionSyntax>();
        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.ManagedTypeAndAttributes;
        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
        public bool IsSupported(TargetFramework target, Version version) => false;
        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
    }
}
