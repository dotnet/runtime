// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    sealed partial record FieldValue : IValueWithStaticType
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
