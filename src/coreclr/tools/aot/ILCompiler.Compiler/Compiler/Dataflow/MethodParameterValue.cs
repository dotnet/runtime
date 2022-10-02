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
    /// A value that came from a method parameter - such as the result of a ldarg.
    /// </summary>
    internal partial record MethodParameterValue : IValueWithStaticType
    {
        public MethodParameterValue(MethodDesc method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
        {
            StaticType = method.Signature[parameterIndex];
            Method = method;
            ParameterIndex = parameterIndex;
            DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
        }

        public readonly MethodDesc Method;

        /// <summary>
        /// This is the index of non-implicit parameter - so the index into MethodDesc.Signature array.
        /// It's NOT the IL parameter index which could be offset by 1 if the method has an implicit this.
        /// </summary>
        public readonly int ParameterIndex;

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch()
            => new string[] { DiagnosticUtilities.GetParameterNameForErrorMessage(Method, ParameterIndex), DiagnosticUtilities.GetMethodSignatureDisplayName(Method) };

        public TypeDesc? StaticType { get; }

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(Method, ParameterIndex, DynamicallyAccessedMemberTypes);

        internal ParameterOrigin ParameterOrigin
        {
            get
            {
                int index = ParameterIndex;
                if (!Method.Signature.IsStatic)
                    index++;

                return new ParameterOrigin(Method, index);
            }
        }
    }
}
