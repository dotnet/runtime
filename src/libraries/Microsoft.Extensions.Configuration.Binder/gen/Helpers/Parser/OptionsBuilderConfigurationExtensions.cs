// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Parser
        {
            private void RegisterMethodInvocation_OptionsBuilderExt(BinderInvocation invocation)
            {
                IMethodSymbol targetMethod = invocation.Operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;

                if (!targetMethod.IsGenericMethod ||
                    @params.Length < 2 ||
                    @params[0].Type is not INamedTypeSymbol { IsGenericType: true } genericType ||
                    !SymbolEqualityComparer.Default.Equals(_typeSymbols.OptionsBuilderOfT_Unbound, genericType.ConstructUnboundGenericType()))
                {
                    return;
                }


                ITypeSymbol? typeSymbol = targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                // This would violate generic type constraint; any such invocation could not have been included in the initial parser.
                Debug.Assert(typeSymbol?.IsValueType is not true);
                TypeSpec typeSpec = GetTargetTypeForRootInvocation(typeSymbol, invocation.Location);

                if (typeSpec is null)
                {
                    return;
                }

                if (targetMethod.Name is "Bind")
                {
                    RegisterBindInvocation(invocation, typeSpec);
                }
                else if (targetMethod.Name is "BindConfiguration")
                {
                    ParseBindConfigurationInvocation(invocation, typeSpec);
                }
            }

            private void RegisterBindInvocation(BinderInvocation invocation, TypeSpec typeSpec)
            {
                IInvocationOperation operation = invocation.Operation!;
                IMethodSymbol targetMethod = operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
                int paramCount = @params.Length;

                Debug.Assert(paramCount >= 2);

                if (!SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, @params[1].Type))
                {
                    return;
                }

                MethodsToGen_Extensions_OptionsBuilder overload = paramCount switch
                {
                    2 => MethodsToGen_Extensions_OptionsBuilder.Bind_T,
                    3 when SymbolEqualityComparer.Default.Equals(_typeSymbols.ActionOfBinderOptions, @params[2].Type) =>
                        MethodsToGen_Extensions_OptionsBuilder.Bind_T_BinderOptions,
                    _ => MethodsToGen_Extensions_OptionsBuilder.None
                };

                if (overload is not MethodsToGen_Extensions_OptionsBuilder.None)
                {
                    RegisterAsInterceptor_OptionsBuilder(overload, operation);
                    RegisterTypeForMethodGen(MethodsToGen_Extensions_ServiceCollection.Configure_T_name_BinderOptions, typeSpec);
                }
            }

            private void ParseBindConfigurationInvocation(BinderInvocation invocation, TypeSpec typeSpec)
            {
                IMethodSymbol targetMethod = invocation.Operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;

                int paramCount = @params.Length;
                Debug.Assert(paramCount >= 2);

                if (paramCount is 3 &&
                    @params[1].Type.SpecialType is SpecialType.System_String &&
                    SymbolEqualityComparer.Default.Equals(_typeSymbols.ActionOfBinderOptions, @params[2].Type))
                {
                    RegisterAsInterceptor_OptionsBuilder(MethodsToGen_Extensions_OptionsBuilder.BindConfiguration_T_path_BinderOptions, invocation.Operation);
                    RegisterTypeForBindCoreMainGen(typeSpec);
                }
            }

            private void RegisterAsInterceptor_OptionsBuilder(MethodsToGen_Extensions_OptionsBuilder overload, IInvocationOperation operation)
            {
                _sourceGenSpec.MethodsToGen_OptionsBuilderExt |= overload;
                RegisterAsInterceptor(overload, operation);

                // Emitting refs to IOptionsChangeTokenSource, ConfigurationChangeTokenSource.
                _sourceGenSpec.Namespaces.Add("Microsoft.Extensions.Options");

                // Emitting refs to OptionsBuilder<T>.
                _sourceGenSpec.Namespaces.Add("Microsoft.Extensions.DependencyInjection");
            }
        }
    }
}
