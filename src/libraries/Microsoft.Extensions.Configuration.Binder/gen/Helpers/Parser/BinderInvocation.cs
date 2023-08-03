// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record BinderInvocation
    {
        public IInvocationOperation Operation { get; private set; }
        public Location? Location { get; private set; }

        public static BinderInvocation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            if (!IsCandidateInvocationExpressionSyntax(context.Node, out InvocationExpressionSyntax? invocationSyntax) ||
                context.SemanticModel.GetOperation(invocationSyntax, cancellationToken) is not IInvocationOperation operation ||
                !IsCandidateInvocation(operation))
            {
                return null;
            }

            return new BinderInvocation()
            {
                Operation = operation,
                Location = invocationSyntax.GetLocation()
            };
        }

        private static bool IsCandidateInvocationExpressionSyntax(SyntaxNode node, out InvocationExpressionSyntax? invocationSyntax)
        {
            if (node is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name.Identifier.ValueText: string memberName
                    }
                } syntax && IsCandidateBindingMethodName(memberName))
            {
                invocationSyntax = syntax;
                return true;
            }

            invocationSyntax = null;
            return false;

            static bool IsCandidateBindingMethodName(string name) =>
                IsCandidateMethodName_ConfigurationBinder(name) ||
                IsCandidateMethodName_OptionsBuilderConfigurationExtensions(name) ||
                IsValidMethodName_OptionsConfigurationServiceCollectionExtensions(name);
        }

        private static bool IsCandidateInvocation(IInvocationOperation operation)
        {
            if (operation.TargetMethod is not IMethodSymbol
                {
                    IsExtensionMethod: true,
                    Name: string methodName,
                    ContainingType: ITypeSymbol
                    {
                        Name: string containingTypeName,
                        ContainingNamespace: INamespaceSymbol { } containingNamespace,
                    } containingType
                } method ||
                containingNamespace.ToDisplayString() is not string containingNamespaceName)
            {
                return false;
            }

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
