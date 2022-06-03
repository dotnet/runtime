// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILCompiler.Dataflow;
using ILLink.Shared.DataFlow;
using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// Return value from a method
    /// </summary>
    partial record MethodReturnValue : IValueWithStaticType
    {
        public MethodReturnValue(MethodDesc method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
        {
            StaticType = method.Signature.ReturnType;
            Method = method;
            DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
        }

        public readonly MethodDesc Method;

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch()
            => new string[] { DiagnosticUtilities.GetMethodSignatureDisplayName(Method) };

        public TypeDesc? StaticType { get; }

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(Method, DynamicallyAccessedMemberTypes);
    }
}
