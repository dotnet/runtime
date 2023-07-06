// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public sealed class Forwarder : IMarshallingGenerator
    {
        public bool IsSupported(TargetFramework target, Version version) => true;

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

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, [NotNullWhen(false)] out GeneratorDiagnostic? diagnostic)
        {
            diagnostic = GeneratorDiagnostic.DefaultDiagnosticForByValueGeneratorSupport(ByValueMarshalKindSupport.NotSupported, info, context);
            return false;
        }
    }
}
