// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using System.Threading;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record BinderInvocationOperation()
    {
        public IInvocationOperation InvocationOperation { get; private set; }
        public BinderMethodSpecifier MethodGroup { get; private set; }
        public Location? Location { get; private set; }

        public static BinderInvocationOperation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            BinderMethodSpecifier kind;
            if (context.Node is not InvocationExpressionSyntax invocationSyntax ||
                invocationSyntax.Expression is not MemberAccessExpressionSyntax memberAccessSyntax ||
                (kind = GetBindingMethodKind(memberAccessSyntax.Name.Identifier.ValueText)) is BinderMethodSpecifier.None ||
                context.SemanticModel.GetOperation(invocationSyntax, cancellationToken) is not IInvocationOperation operation)
            {
                return null;
            }

            return new BinderInvocationOperation
            {
                InvocationOperation = operation,
                MethodGroup = kind,
                Location = invocationSyntax.GetLocation()
            };
        }

        public static BinderMethodSpecifier GetBindingMethodKind(string name) =>
            name switch
            {
                "Bind" => BinderMethodSpecifier.Bind,
                "Get" => BinderMethodSpecifier.Get,
                "GetValue" => BinderMethodSpecifier.GetValue,
                "Configure" => BinderMethodSpecifier.Configure,
                _ => BinderMethodSpecifier.None,

            };
    }
}
