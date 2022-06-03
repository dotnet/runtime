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
    /// A value that came from the implicit this parameter of a method
    /// </summary>
    partial record MethodThisParameterValue : IValueWithStaticType
    {
        public MethodThisParameterValue(MethodDesc method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
        {
            Method = method;
            DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
        }

        public readonly MethodDesc Method;

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch()
            => new string[] { Method.GetDisplayName() };

        public TypeDesc? StaticType => Method.OwningType;

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(Method, DynamicallyAccessedMemberTypes);
    }
}
