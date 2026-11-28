// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
    internal partial record FieldValue
    {
        public FieldValue(IFieldSymbol fieldSymbol, AnalyzerCompilationState? compilationState = null)
            : this(fieldSymbol, fieldSymbol.Type, FlowAnnotations.GetFieldAnnotation(fieldSymbol, compilationState))
        {
        }

        public FieldValue(IPropertySymbol propertySymbol, AnalyzerCompilationState? compilationState = null)
            : this(propertySymbol, propertySymbol.Type, FlowAnnotations.GetBackingFieldAnnotation(propertySymbol, compilationState))
        {
        }

        private FieldValue(ISymbol fieldSymbol, ITypeSymbol fieldType, DynamicallyAccessedMemberTypes annotations)
        {
            FieldSymbol = fieldSymbol;
            StaticType = new(fieldType);
            DynamicallyAccessedMemberTypes = annotations;
        }

        public readonly ISymbol FieldSymbol;

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch()
            => new string[] { FieldSymbol.GetDisplayName() };

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(FieldSymbol, DynamicallyAccessedMemberTypes);
    }
}
