// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    // Shared helpers to go from MethodProxy to dataflow values.
    partial class FlowAnnotations
    {
        internal partial bool MethodRequiresDataFlowAnalysis(MethodProxy method);

        internal partial MethodReturnValue GetMethodReturnValue(MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

        internal partial MethodReturnValue GetMethodReturnValue(MethodProxy method);

        internal partial GenericParameterValue GetGenericParameterValue(GenericParameterProxy genericParameter);

        internal partial MethodThisParameterValue GetMethodThisParameterValue(MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

        internal partial MethodThisParameterValue GetMethodThisParameterValue(MethodProxy method);

        internal partial MethodParameterValue GetMethodParameterValue(MethodProxy method, SourceParameterIndex parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

        internal partial MethodParameterValue GetMethodParameterValue(MethodProxy method, SourceParameterIndex parameterIndex);
    }
}
