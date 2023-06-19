// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using System.Threading;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record BinderInvocation
    {
        public IInvocationOperation Operation { get; private set; }
        public Location? Location { get; private set; }

        public static BinderInvocation? Create(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.Node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax } invocationSyntax ||
                context.SemanticModel.GetOperation(invocationSyntax, cancellationToken) is not IInvocationOperation operation)
            {
                return null;
            }

            return new BinderInvocation()
            {
                Operation = operation,
                Location = invocationSyntax.GetLocation()
            };
        }
    }
}
