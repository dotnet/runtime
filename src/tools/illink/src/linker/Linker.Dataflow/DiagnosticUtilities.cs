// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
    static class DiagnosticUtilities
    {
        internal static string GetParameterNameForErrorMessage(ParameterDefinition parameterDefinition) =>
            string.IsNullOrEmpty(parameterDefinition.Name) ? $"#{parameterDefinition.Index}" : parameterDefinition.Name;

        internal static string GetGenericParameterDeclaringMemberDisplayName(GenericParameter genericParameter)
        {
            if (genericParameter.DeclaringMethod is MethodReference declaringMethod)
                return declaringMethod.GetDisplayName();

            if (genericParameter.DeclaringType is TypeReference declaringType)
                return declaringType.GetDisplayName();

            return genericParameter.Name;
        }

        internal static string GetMethodSignatureDisplayName(IMethodSignature methodSignature) =>
            (methodSignature is MethodReference method) ? method.GetDisplayName() : (methodSignature.ToString() ?? string.Empty);
    }
}
