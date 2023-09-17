// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record InterceptorLocationInfo
    {
        public InterceptorLocationInfo(IInvocationOperation operation)
        {
            MemberAccessExpressionSyntax memberAccessExprSyntax = ((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)operation.Syntax).Expression);
            SyntaxTree operationSyntaxTree = operation.Syntax.SyntaxTree;
            TextSpan memberNameSpan = memberAccessExprSyntax.Name.Span;
            FileLinePositionSpan linePosSpan = operationSyntaxTree.GetLineSpan(memberNameSpan);

            LineNumber = linePosSpan.StartLinePosition.Line + 1;
            CharacterNumber = linePosSpan.StartLinePosition.Character + 1;
            FilePath = GetInterceptorFilePath();

            // Use the same logic used by the interceptors API for resolving the source mapped value of a path.
            // https://github.com/dotnet/roslyn/blob/f290437fcc75dad50a38c09e0977cce13a64f5ba/src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs#L1063-L1064
            string GetInterceptorFilePath()
            {
                SourceReferenceResolver? sourceReferenceResolver = operation.SemanticModel?.Compilation.Options.SourceReferenceResolver;
                return sourceReferenceResolver?.NormalizePath(operationSyntaxTree.FilePath, baseFilePath: null) ?? operationSyntaxTree.FilePath;
            }
        }

        public string FilePath { get; }
        public int LineNumber { get; }
        public int CharacterNumber { get; }
    }

    internal sealed record ConfigurationBinderInterceptorInfo
    {
        private OverloadInterceptorInfo? _bind_Instance;
        private OverloadInterceptorInfo? _bind_instance_BinderOptions;
        private OverloadInterceptorInfo? _bind_key_instance;

        public void RegisterOverloadInfo(MethodsToGen_ConfigurationBinder overload, TypeSpec type, IInvocationOperation operation)
        {
            OverloadInterceptorInfo overloadInfo = DetermineOverload(overload, initIfNull: true);
            overloadInfo.RegisterLocationInfo(type, operation);
        }

        public OverloadInterceptorInfo GetOverloadInfo(MethodsToGen_ConfigurationBinder overload) =>
            DetermineOverload(overload, initIfNull: false) ?? throw new ArgumentOutOfRangeException(nameof(overload));

        private OverloadInterceptorInfo? DetermineOverload(MethodsToGen_ConfigurationBinder overload, bool initIfNull)
        {
            return overload switch
            {
                MethodsToGen_ConfigurationBinder.Bind_instance => InitIfNull(ref _bind_Instance),
                MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions => InitIfNull(ref _bind_instance_BinderOptions),
                MethodsToGen_ConfigurationBinder.Bind_key_instance => InitIfNull(ref _bind_key_instance),
                _ => throw new InvalidOperationException(nameof(overload)),
            };

            OverloadInterceptorInfo InitIfNull(ref OverloadInterceptorInfo? info)
            {
                if (initIfNull)
                {
                    info ??= new OverloadInterceptorInfo();
                }

                return info;
            }
        }
    }

    internal sealed record OverloadInterceptorInfo : IEnumerable<KeyValuePair<TypeSpec, List<InterceptorLocationInfo>>>
    {
        private readonly Dictionary<TypeSpec, List<InterceptorLocationInfo>> _typeInterceptionInfo = new();

        public void RegisterLocationInfo(TypeSpec type, IInvocationOperation operation) =>
            _typeInterceptionInfo.RegisterCacheEntry(type, new InterceptorLocationInfo(operation));

        public IEnumerator<KeyValuePair<TypeSpec, List<InterceptorLocationInfo>>> GetEnumerator() => _typeInterceptionInfo.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
