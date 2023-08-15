// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal readonly record struct BinderInvocation
    {
        public IInvocationOperation? CandidateOperation { get; private init; }
        public Location? Location { get; private init; }

        public static BinderInvocation Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(IsCandidateSyntaxNode(context.Node));
            InvocationExpressionSyntax? invocationSyntax = (InvocationExpressionSyntax)context.Node;

            if (context.SemanticModel.GetOperation(invocationSyntax, cancellationToken) is IInvocationOperation operation &&
                IsCandidateInvocationOperation(operation))
            {
                return new BinderInvocation()
                {
                    CandidateOperation = operation,
                    Location = invocationSyntax.GetLocation()
                };
            }

            return default;
        }

        public static bool IsCandidateSyntaxNode(SyntaxNode node)
        {
            return node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: string memberName
                }
            } && IsCandidateBindingMethodName(memberName);

            static bool IsCandidateBindingMethodName(string name) =>
                IsCandidateMethodName_ConfigurationBinder(name) ||
                IsCandidateMethodName_OptionsBuilderConfigurationExtensions(name) ||
                IsValidMethodName_OptionsConfigurationServiceCollectionExtensions(name);
        }

        private static bool IsCandidateInvocationOperation(IInvocationOperation operation)
        {
            if (operation.TargetMethod is not IMethodSymbol
                {
                    IsExtensionMethod: true,
                    Name: string methodName,
                    ContainingType.Name: string containingTypeName,
                })
            {
                return false;
            }

            return (containingTypeName) switch
            {
                "ConfigurationBinder" =>
                    IsCandidateMethodName_ConfigurationBinder(methodName),
                "OptionsBuilderConfigurationExtensions" =>
                    IsCandidateMethodName_OptionsBuilderConfigurationExtensions(methodName),
                "OptionsConfigurationServiceCollectionExtensions" =>
                    IsValidMethodName_OptionsConfigurationServiceCollectionExtensions(methodName),
                _ => false,
            };
        }

        private static bool IsCandidateMethodName_ConfigurationBinder(string name) => name is
            nameof(MethodsToGen_ConfigurationBinder.Bind) or
            nameof(MethodsToGen_ConfigurationBinder.Get) or
            nameof(MethodsToGen_ConfigurationBinder.GetValue);

        private static bool IsCandidateMethodName_OptionsBuilderConfigurationExtensions(string name) => name is
            nameof(MethodsToGen_Extensions_OptionsBuilder.Bind) or
            nameof(MethodsToGen_Extensions_OptionsBuilder.BindConfiguration);

        private static bool IsValidMethodName_OptionsConfigurationServiceCollectionExtensions(string name) => name is
            nameof(MethodsToGen_Extensions_ServiceCollection.Configure);
    }
}
