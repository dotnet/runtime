// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
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

            if (invocation.Syntax is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccessExprSyntax } invocationExpressionSyntax)
            {
                const string InvalidInvocationErrMsg = "The invocation should have been validated upstream when selecting invocations to emit interceptors for.";
                throw new ArgumentException(InvalidInvocationErrMsg, nameof(invocation));
            }

            InitializeInterceptableLocationFeature();
            Interceptor = interceptor;
            InterceptableLocation = s_getInterceptableLocationFunc(invocation.SemanticModel, invocationExpressionSyntax, default(CancellationToken));
        }

        internal static void InitializeInterceptableLocationFeature()
        {
            const string NewerRoslynRequired = "The 'InterceptsLocationAttribute' class was not found. A newer version of Roslyn is necessary.";

            if (!_hasInitializedInterceptableLocation)
            {
                MethodInfo? getInterceptableLocationMethod = typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions).GetMethod(
                    "GetInterceptableLocation",
                    BindingFlags.Static | BindingFlags.Public,
                    binder: null,
                    new Type[] { typeof(SemanticModel), typeof(InvocationExpressionSyntax), typeof(CancellationToken) },
                    modifiers: Array.Empty<ParameterModifier>());

                if (getInterceptableLocationMethod is null)
                {
                    throw new NotSupportedException(NewerRoslynRequired);
                }

                s_getInterceptableLocationFunc = (Func<SemanticModel, InvocationExpressionSyntax, CancellationToken, object>)
                    getInterceptableLocationMethod.CreateDelegate(typeof(Func<SemanticModel, InvocationExpressionSyntax, CancellationToken, object>), target: null);

                Type? interceptableLocationType = typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.InterceptableLocation");
                s_interceptableLocationVersionGetDisplayLocation = interceptableLocationType.GetMethod("GetDisplayLocation", BindingFlags.Instance | BindingFlags.Public);
                s_interceptableLocationVersionGetter = interceptableLocationType.GetProperty("Version", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
                s_interceptableLocationDataGetter = interceptableLocationType.GetProperty("Data", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();

                _hasInitializedInterceptableLocation = true;
            }
        }

        private static bool _hasInitializedInterceptableLocation;
        private static Func<SemanticModel, InvocationExpressionSyntax, CancellationToken, object>? s_getInterceptableLocationFunc;
        private static MethodInfo? s_interceptableLocationVersionGetDisplayLocation;
        private static MethodInfo? s_interceptableLocationDataGetter;
        private static MethodInfo? s_interceptableLocationVersionGetter;

        public MethodsToGen Interceptor { get; }
        private object? InterceptableLocation { get; }
        public string InterceptableLocationGetDisplayLocation() => (string)s_interceptableLocationVersionGetDisplayLocation.Invoke(InterceptableLocation, parameters: null);
        public string InterceptableLocationData => (string)s_interceptableLocationDataGetter.Invoke(InterceptableLocation, parameters: null);
        public int InterceptableLocationVersion => (int)s_interceptableLocationVersionGetter.Invoke(InterceptableLocation, parameters: null);
    }
}
