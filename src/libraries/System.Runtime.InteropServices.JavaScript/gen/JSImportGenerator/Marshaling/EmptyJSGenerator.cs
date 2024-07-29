// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class EmptyJSGenerator : IJSMarshallingGenerator
    {
        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<ExpressionSyntax> GenerateBind(TypePositionInfo info, StubCodeContext context) => Array.Empty<ExpressionSyntax>();
        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.ManagedTypeAndAttributes;
        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
            => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
    }
}
