// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

using Parser = Microsoft.Extensions.Configuration.Binder.SourceGeneration.Parser;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed partial record OptionsBuilderMethodSpec : MethodSpec
    {
        private MethodSpecifier _methodsToGen;

        public OptionsBuilderMethodSpec(SourceGenSpec spec) : base(spec) { }

        public bool Any() =>
            ShouldEmitMethods(MethodSpecifier.Bind | MethodSpecifier.BindConfiguration_T_path_BinderOptions);

        public override void RegisterInvocation(Parser parser, BinderInvocation invocation)
        {
            IMethodSymbol targetMethod = invocation.Operation.TargetMethod;
            ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;

            if (!targetMethod.IsGenericMethod ||
                @params.Length < 2 ||
                @params[0].Type is not INamedTypeSymbol { IsGenericType: true } genericType ||
                !SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.OptionsBuilderOfT_Unbound, genericType.ConstructUnboundGenericType()))
            {
                return;
            }

            TypeSpec typeSpec = parser.GetBindingConfigType(
                type: targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None),
                invocation.Location);

            if (typeSpec is null)
            {
                return;
            }

            // We are going to emit calls to APIs on IServiceCollection.
            SourceGenSpec.CoreBindingHelperSpec.TypeNamespaces.Add("Microsoft.Extensions.DependencyInjection");

            if (targetMethod.Name is nameof(MethodSpecifier.Bind))
            {
                RegisterBindInvocation(parser, invocation, typeSpec);
            }
            else if (targetMethod.Name is "BindConfiguration")
            {
                ParseBindConfigurationInvocation(parser, invocation, typeSpec);
            }
        }

        private void RegisterBindInvocation(Parser parser, BinderInvocation invocation, TypeSpec typeSpec)
        {
            IInvocationOperation operation = invocation.Operation!;
            IMethodSymbol targetMethod = operation.TargetMethod;
            ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
            int paramCount = @params.Length;

            Debug.Assert(paramCount >= 2);

            if (!SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.IConfiguration, @params[1].Type))
            {
                return;
            }

            if (paramCount is 2)
            {
                _methodsToGen |= MethodSpecifier.Bind_T;
            }
            else if (paramCount is 3 && SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.ActionOfBinderOptions, @params[2].Type))
            {
                _methodsToGen |= MethodSpecifier.Bind_T_BinderOptions;
            }
            else
            {
                return;
            }

            SourceGenSpec.ServiceCollectionSpec.RegisterTypeForMethodGen(ServiceCollectionMethodSpec.MethodSpecifier.Configure_T_name_BinderOptions, typeSpec);
        }

        private void ParseBindConfigurationInvocation(Parser parser, BinderInvocation invocation, TypeSpec typeSpec)
        {
            IMethodSymbol targetMethod = invocation.Operation.TargetMethod;
            ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;

            int paramCount = @params.Length;
            Debug.Assert(paramCount >= 2);

            if (paramCount is 3 && @params[1].Type.SpecialType is SpecialType.System_String && SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.ActionOfBinderOptions, @params[2].Type))
            {
                _methodsToGen |= MethodSpecifier.BindConfiguration_T_path_BinderOptions;
                SourceGenSpec.CoreBindingHelperSpec.RegisterTypeForBindCoreUntypedGen(typeSpec);
            }
        }

        [Flags]
        private enum MethodSpecifier
        {
            None = 0x0,

            /// <summary>
            /// Bind<T>(OptionsBuilder<T>, IConfiguration).
            /// </summary>
            Bind_T = 0x1,

            /// <summary>
            /// Bind<T>(OptionsBuilder<T>, IConfiguration, Action<BinderOptions>?).
            /// </summary>
            Bind_T_BinderOptions = 0x2,

            /// <summary>
            /// BindConfiguration<T>(OptionsBuilder<T>, string, Action<BinderOptions>?).
            /// </summary>
            BindConfiguration_T_path_BinderOptions = 0x4,

            // Method group. BindConfiguration_T is its own method group.
            Bind = Bind_T | Bind_T_BinderOptions,
        }
    }
}
