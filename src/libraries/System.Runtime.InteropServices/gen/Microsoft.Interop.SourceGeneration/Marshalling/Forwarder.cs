// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public sealed class Forwarder : IUnboundMarshallingGenerator
    {
        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return info.ManagedType;
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return SignatureBehavior.ManagedTypeAndAttributes;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            return ValueBoundaryBehavior.ManagedIdentifier;
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
            => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);
    }
}
