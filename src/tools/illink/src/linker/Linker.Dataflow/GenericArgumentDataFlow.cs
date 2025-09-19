// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
    internal static class GenericArgumentDataFlow
    {
        public static void ProcessGenericArgumentDataFlow(in MessageOrigin origin, MarkStep markStep, LinkContext context, TypeReference type)
        {
            var diagnosticContext = new DiagnosticContext(origin, !context.Annotations.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode(origin.Provider, out _), context);
            var reflectionMarker = new ReflectionMarker(context, markStep, enabled: true);
            ProcessGenericArgumentDataFlow(in diagnosticContext, reflectionMarker, context, type);
        }

        public static void ProcessGenericArgumentDataFlow(in DiagnosticContext diagnosticContext, ReflectionMarker reflectionMarker, LinkContext context, TypeReference type)
        {
            if (type is GenericInstanceType genericInstanceType && context.TryResolve(type) is TypeDefinition typeDefinition)
            {
                ProcessGenericInstantiation(diagnosticContext, reflectionMarker, context, genericInstanceType, typeDefinition);
            }
        }

        public static void ProcessGenericArgumentDataFlow(in DiagnosticContext diagnosticContext, ReflectionMarker reflectionMarker, LinkContext context, MethodReference method)
        {
            if (method is GenericInstanceMethod genericInstanceMethod && context.TryResolve(method) is MethodDefinition methodDefinition)
            {
                ProcessGenericInstantiation(diagnosticContext, reflectionMarker, context, genericInstanceMethod, methodDefinition);
            }

            ProcessGenericArgumentDataFlow(diagnosticContext, reflectionMarker, context, method.DeclaringType);
        }

        public static void ProcessGenericArgumentDataFlow(in DiagnosticContext diagnosticContext, ReflectionMarker reflectionMarker, LinkContext context, FieldReference field)
        {
            ProcessGenericArgumentDataFlow(diagnosticContext, reflectionMarker, context, field.DeclaringType);
        }

        private static void ProcessGenericInstantiation(in DiagnosticContext diagnosticContext, ReflectionMarker reflectionMarker, LinkContext context, IGenericInstance genericInstance, IGenericParameterProvider genericParameterProvider)
        {
            var arguments = genericInstance.GenericArguments;
            var parameters = genericParameterProvider.GenericParameters;

            for (int i = 0; i < arguments.Count; i++)
            {
                var genericArgument = arguments[i];
                var genericParameter = parameters[i];

                var parameterRequirements = context.Annotations.FlowAnnotations.GetGenericParameterAnnotation(genericParameter);

                if (genericParameter.HasDefaultConstructorConstraint)
                {
                    reflectionMarker.MarkTypeForDynamicallyAccessedMembers(diagnosticContext.Origin, genericArgument, DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, DependencyKind.DefaultCtorForNewConstrainedGenericArgument);
                    // Avoid duplicate warnings for new() and DAMT.PublicParameterlessConstructor
                    parameterRequirements &= ~DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
                }

                var genericParameterValue = context.Annotations.FlowAnnotations.GetGenericParameterValue(genericParameter, parameterRequirements);
                if (genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                {
                    MultiValue genericArgumentValue = context.Annotations.FlowAnnotations.GetTypeValueFromGenericArgument(genericArgument);

                    var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(context, reflectionMarker, diagnosticContext);
                    requireDynamicallyAccessedMembersAction.Invoke(genericArgumentValue, genericParameterValue);
                }

                // Recursively process generic argument data flow on the generic argument if it itself is generic
                if (genericArgument.IsGenericInstance)
                {
                    ProcessGenericArgumentDataFlow(diagnosticContext, reflectionMarker, context, genericArgument);
                }
            }
        }

        internal static bool RequiresGenericArgumentDataFlow(FlowAnnotations flowAnnotations, MethodReference method)
        {
            // Method callsites can contain generic instantiations which may contain annotations inside nested generics
            // so we have to check all of the instantiations for that case.
            // For example:
            //   OuterGeneric<InnerGeneric<Annotated>>.Method<InnerGeneric<AnotherAnnotated>>();

            if (method is GenericInstanceMethod genericInstanceMethod)
            {
                if (flowAnnotations.HasGenericParameterAnnotation(method))
                    return true;

                if (flowAnnotations.HasGenericParameterNewConstraint(method))
                    return true;

                foreach (var genericArgument in genericInstanceMethod.GenericArguments)
                {
                    if (RequiresGenericArgumentDataFlow(flowAnnotations, genericArgument))
                        return true;
                }
            }

            return RequiresGenericArgumentDataFlow(flowAnnotations, method.DeclaringType);
        }

        internal static bool RequiresGenericArgumentDataFlow(FlowAnnotations flowAnnotations, FieldReference field)
        {
            return RequiresGenericArgumentDataFlow(flowAnnotations, field.DeclaringType);
        }

        internal static bool RequiresGenericArgumentDataFlow(FlowAnnotations flowAnnotations, TypeReference type)
        {
            if (flowAnnotations.HasGenericParameterAnnotation(type))
            {
                return true;
            }

            if (flowAnnotations.HasGenericParameterNewConstraint(type))
            {
                return true;
            }

            if (type is GenericInstanceType genericInstanceType)
            {
                foreach (var genericArgument in genericInstanceType.GenericArguments)
                {
                    if (RequiresGenericArgumentDataFlow(flowAnnotations, genericArgument))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
