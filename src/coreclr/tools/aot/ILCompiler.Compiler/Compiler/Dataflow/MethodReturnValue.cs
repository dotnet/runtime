// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal partial record MethodReturnValue
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

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(Method, DynamicallyAccessedMemberTypes);
    }
}
