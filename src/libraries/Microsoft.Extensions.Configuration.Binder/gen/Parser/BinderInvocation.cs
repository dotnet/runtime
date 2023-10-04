// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record BinderInvocation(IInvocationOperation Operation, Location Location)
    {
        public static BinderInvocation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            Debug.Assert(IsCandidateSyntaxNode(context.Node));
            InvocationExpressionSyntax invocationSyntax = (InvocationExpressionSyntax)context.Node;

            return context.SemanticModel.GetOperation(invocationSyntax, cancellationToken) is IInvocationOperation operation &&
                IsBindingOperation(operation)
                ? new BinderInvocation(operation, invocationSyntax.GetLocation())
                : null;
        }

        public static bool IsCandidateSyntaxNode(SyntaxNode node)
        {
            return node is InvocationExpressionSyntax
            {
                // TODO: drill further into this evaluation for a declaring-type name check.
                // https://github.com/dotnet/runtime/issues/90687.
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: string memberName,
                }
            } && IsCandidateBindingMethodName(memberName);

            static bool IsCandidateBindingMethodName(string name) =>
                IsCandidateMethodName_ConfigurationBinder(name) ||
                IsCandidateMethodName_OptionsBuilderConfigurationExtensions(name) ||
                IsValidMethodName_OptionsConfigurationServiceCollectionExtensions(name);
        }

        public static bool IsBindingOperation(IInvocationOperation operation)
        {
            if (operation.TargetMethod is not IMethodSymbol
                {
                    IsExtensionMethod: true,
                    Name: string methodName,
                    ContainingType: INamedTypeSymbol
                    {
                        Name: string containingTypeName,
                        ContainingNamespace: INamespaceSymbol containingNamespace,
                    }
                })
            {
                return false;
            }

            string containingNamespaceName = containingNamespace.ToDisplayString();

            return (containingTypeName) switch
            {
                "ConfigurationBinder" =>
                    containingNamespaceName is "Microsoft.Extensions.Configuration" &&
                    IsCandidateMethodName_ConfigurationBinder(methodName),
                "OptionsBuilderConfigurationExtensions" =>
                    containingNamespaceName is "Microsoft.Extensions.DependencyInjection" &&
                    IsCandidateMethodName_OptionsBuilderConfigurationExtensions(methodName),
                "OptionsConfigurationServiceCollectionExtensions" =>
                    containingNamespaceName is "Microsoft.Extensions.DependencyInjection" &&
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
