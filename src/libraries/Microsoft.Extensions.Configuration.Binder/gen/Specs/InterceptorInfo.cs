// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record InterceptorInfo
    {
        public required MethodsToGen MethodsToGen { get; init; }

        public required ImmutableEquatableArray<TypedInterceptorInvocationInfo>? ConfigBinder_Bind_instance { get; init; }
        public required ImmutableEquatableArray<TypedInterceptorInvocationInfo>? ConfigBinder_Bind_instance_BinderOptions { get; init; }
        public required ImmutableEquatableArray<TypedInterceptorInvocationInfo>? ConfigBinder_Bind_key_instance { get; init; }


        public required ImmutableEquatableArray<InvocationLocationInfo>? ConfigBinder { get; init; }
        public required ImmutableEquatableArray<InvocationLocationInfo>? OptionsBuilderExt { get; init; }
        public required ImmutableEquatableArray<InvocationLocationInfo>? ServiceCollectionExt { get; init; }

        public IEnumerable<InvocationLocationInfo>? GetInfo(MethodsToGen interceptor)
        {
            Debug.Assert((MethodsToGen.ConfigBinder_Bind & interceptor) is 0);

            ImmutableEquatableArray<InvocationLocationInfo>? infoList;
            if ((MethodsToGen.ConfigBinder_Any ^ MethodsToGen.ConfigBinder_Bind & interceptor) is not 0)
            {
                infoList = ConfigBinder;
            }
            else if ((MethodsToGen.OptionsBuilderExt_Any & interceptor) is not 0)
            {
                infoList = OptionsBuilderExt;
            }
            else
            {
                Debug.Assert((MethodsToGen.ServiceCollectionExt_Any & interceptor) is not 0);
                infoList = ServiceCollectionExt;
            }

            return infoList?.Where(i => i.Interceptor == interceptor);
        }

        internal sealed class Builder
        {
            private TypedInterceptorInfoBuildler? _configBinder_InfoBuilder_Bind_instance;
            private TypedInterceptorInfoBuildler? _configBinder_InfoBuilder_Bind_instance_BinderOptions;
            private TypedInterceptorInfoBuildler? _configBinder_InfoBuilder_Bind_key_instance;

            private List<InvocationLocationInfo>? _interceptors_configBinder;
            private List<InvocationLocationInfo>? _interceptors_OptionsBuilderExt;
            private List<InvocationLocationInfo>? _interceptors_serviceCollectionExt;

            public MethodsToGen MethodsToGen { get; set; }

            public void RegisterInterceptor_ConfigBinder_Bind(MethodsToGen overload, ComplexTypeSpec type, IInvocationOperation invocation)
            {
                Debug.Assert((MethodsToGen.ConfigBinder_Bind & overload) is not 0);

                switch (overload)
                {
                    case MethodsToGen.ConfigBinder_Bind_instance:
                        RegisterInterceptor(ref _configBinder_InfoBuilder_Bind_instance);
                        break;
                    case MethodsToGen.ConfigBinder_Bind_instance_BinderOptions:
                        RegisterInterceptor(ref _configBinder_InfoBuilder_Bind_instance_BinderOptions);
                        break;
                    case MethodsToGen.ConfigBinder_Bind_key_instance:
                        RegisterInterceptor(ref _configBinder_InfoBuilder_Bind_key_instance);
                        break;
                }

                MethodsToGen |= overload;

                void RegisterInterceptor(ref TypedInterceptorInfoBuildler? infoBuilder)
                {
                    infoBuilder ??= new TypedInterceptorInfoBuildler();
                    infoBuilder.RegisterInterceptor(overload, type, invocation);
                }
            }

            public void RegisterInterceptor(MethodsToGen overload, IInvocationOperation operation)
            {
                Debug.Assert((MethodsToGen.ConfigBinder_Bind & overload) is 0);

                if ((MethodsToGen.ConfigBinder_Any ^ MethodsToGen.ConfigBinder_Bind & overload) is not 0)
                {
                    RegisterInterceptor(ref _interceptors_configBinder);
                }
                else if ((MethodsToGen.OptionsBuilderExt_Any & overload) is not 0)
                {
                    RegisterInterceptor(ref _interceptors_OptionsBuilderExt);
                }
                else
                {
                    Debug.Assert((MethodsToGen.ServiceCollectionExt_Any & overload) is not 0);
                    RegisterInterceptor(ref _interceptors_serviceCollectionExt);
                }

                MethodsToGen |= overload;

                void RegisterInterceptor(ref List<InvocationLocationInfo>? infoList)
                {
                    infoList ??= new List<InvocationLocationInfo>();
                    infoList.Add(new InvocationLocationInfo(overload, operation));
                }
            }

            public InterceptorInfo ToIncrementalValue() =>
                new InterceptorInfo
                {
                    MethodsToGen = MethodsToGen,

                    ConfigBinder = _interceptors_configBinder?.ToImmutableEquatableArray(),
                    OptionsBuilderExt = _interceptors_OptionsBuilderExt?.ToImmutableEquatableArray(),
                    ServiceCollectionExt = _interceptors_serviceCollectionExt?.ToImmutableEquatableArray(),

                    ConfigBinder_Bind_instance = _configBinder_InfoBuilder_Bind_instance?.ToIncrementalValue(),
                    ConfigBinder_Bind_instance_BinderOptions = _configBinder_InfoBuilder_Bind_instance_BinderOptions?.ToIncrementalValue(),
                    ConfigBinder_Bind_key_instance = _configBinder_InfoBuilder_Bind_key_instance?.ToIncrementalValue(),
                };
        }
    }

    internal sealed class TypedInterceptorInfoBuildler
    {
        private readonly Dictionary<ComplexTypeSpec, TypedInterceptorInvocationInfo.Builder> _invocationInfoBuilderCache = new();

        public void RegisterInterceptor(MethodsToGen overload, ComplexTypeSpec type, IInvocationOperation invocation)
        {
            if (!_invocationInfoBuilderCache.TryGetValue(type, out TypedInterceptorInvocationInfo.Builder? invocationInfoBuilder))
            {
                _invocationInfoBuilderCache[type] = invocationInfoBuilder = new TypedInterceptorInvocationInfo.Builder(overload, type);
            }

            invocationInfoBuilder.RegisterInvocation(invocation);
        }

        public ImmutableEquatableArray<TypedInterceptorInvocationInfo>? ToIncrementalValue() =>
            _invocationInfoBuilderCache.Values
            .Select(b => b.ToIncrementalValue())
            .ToImmutableEquatableArray();
    }

    public sealed record TypedInterceptorInvocationInfo(ComplexTypeSpec TargetType, ImmutableEquatableArray<InvocationLocationInfo> Locations)
    {
        public sealed class Builder(MethodsToGen Overload, ComplexTypeSpec TargetType)
        {
            private readonly List<InvocationLocationInfo> _infoList = new();

            public void RegisterInvocation(IInvocationOperation invocation) =>
                _infoList.Add(new InvocationLocationInfo(Overload, invocation));

            public TypedInterceptorInvocationInfo ToIncrementalValue() => new(
                TargetType,
                Locations: _infoList.ToImmutableEquatableArray());
        }
    }

    public sealed record InvocationLocationInfo
    {
        public InvocationLocationInfo(MethodsToGen interceptor, IInvocationOperation invocation)
        {
            Debug.Assert(BinderInvocation.IsBindingOperation(invocation));

            if (invocation.Syntax is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccessExprSyntax })
            {
                const string InvalidInvocationErrMsg = "The invocation should have been validated upstream when selecting invocations to emit interceptors for.";
                throw new ArgumentException(InvalidInvocationErrMsg, nameof(invocation));
            }

            SyntaxTree operationSyntaxTree = invocation.Syntax.SyntaxTree;
            TextSpan memberNameSpan = memberAccessExprSyntax.Name.Span;
            FileLinePositionSpan linePosSpan = operationSyntaxTree.GetLineSpan(memberNameSpan);

            Interceptor = interceptor;
            LineNumber = linePosSpan.StartLinePosition.Line + 1;
            CharacterNumber = linePosSpan.StartLinePosition.Character + 1;
            FilePath = GetInterceptorFilePath();

            // Use the same logic used by the interceptors API for resolving the source mapped value of a path.
            // https://github.com/dotnet/roslyn/blob/f290437fcc75dad50a38c09e0977cce13a64f5ba/src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs#L1063-L1064
            string GetInterceptorFilePath()
            {
                SourceReferenceResolver? sourceReferenceResolver = invocation.SemanticModel?.Compilation.Options.SourceReferenceResolver;
                return sourceReferenceResolver?.NormalizePath(operationSyntaxTree.FilePath, baseFilePath: null) ?? operationSyntaxTree.FilePath;
            }
        }

        public MethodsToGen Interceptor { get; }
        public string FilePath { get; }
        public int LineNumber { get; }
        public int CharacterNumber { get; }
    }
}
