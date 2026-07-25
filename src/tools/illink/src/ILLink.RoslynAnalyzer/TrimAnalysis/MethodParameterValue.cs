// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
    internal partial record MethodParameterValue
    {
        public MethodParameterValue(ParameterProxy parameter)
            : this(parameter, FlowAnnotations.GetMethodParameterAnnotation(parameter)) { }

        public MethodParameterValue(ParameterProxy parameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
        {
            Parameter = parameter;
            DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
            StaticType = parameter.ParameterType;
        }

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public IMethodSymbol MethodSymbol => Parameter.Method.Method;
    }
}
