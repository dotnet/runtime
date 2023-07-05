// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILCompiler;
using ILCompiler.Dataflow;
using ILLink.Shared.DataFlow;
using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{

    /// <summary>
    /// A representation of a field. Typically a result of ldfld.
    /// </summary>
    internal sealed partial record FieldValue : IValueWithStaticType
    {
        public FieldValue(FieldDesc field, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
        {
            StaticType = field.FieldType;
            Field = field;
            DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
        }

        public readonly FieldDesc Field;

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch()
            => new string[] { Field.GetDisplayName() };

        public TypeDesc? StaticType { get; }

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(Field, DynamicallyAccessedMemberTypes);
    }
}
