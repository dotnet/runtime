// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Linker;
using FieldReference = Mono.Cecil.FieldReference;

namespace ILLink.Shared.TrimAnalysis
{

    /// <summary>
    /// A representation of a field. Typically a result of ldfld.
    /// </summary>
    internal sealed partial record FieldValue
    {
        public FieldValue(FieldReference fieldToLoad, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, ITryResolveMetadata resolver)
        {
            StaticType = new(fieldToLoad.FieldType.InflateFrom(fieldToLoad.DeclaringType as IGenericInstance), resolver);
            Field = fieldToLoad;
            DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
        }

        public readonly FieldReference Field;

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch()
            => new string[] { Field.GetDisplayName() };

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(Field, DynamicallyAccessedMemberTypes);
    }

}
