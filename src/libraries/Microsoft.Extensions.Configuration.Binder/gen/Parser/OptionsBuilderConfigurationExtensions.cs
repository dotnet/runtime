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
        internal sealed partial class Parser
        {
            private void ParseInvocation_OptionsBuilderExt(BinderInvocation invocation)
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

                if (targetMethod.Name is "Bind")
                {
                    ParseBindInvocation_OptionsBuilderExt(invocation, typeSymbol);
                }
                else if (targetMethod.Name is "BindConfiguration")
                {
                    ParseBindConfigurationInvocation(invocation, typeSymbol);
                }
            }

            private void ParseBindInvocation_OptionsBuilderExt(BinderInvocation invocation, ITypeSymbol? type)
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

                MethodsToGen overload = paramCount switch
                {
                    2 => MethodsToGen.OptionsBuilderExt_Bind_T,
                    3 when SymbolEqualityComparer.Default.Equals(_typeSymbols.ActionOfBinderOptions, @params[2].Type) =>
                        MethodsToGen.OptionsBuilderExt_Bind_T_BinderOptions,
                    _ => MethodsToGen.None
                };

                if (overload is not MethodsToGen.None)
                {
                    EnqueueTargetTypeForRootInvocation(type, overload, invocation);
                }
            }

            private void ParseBindConfigurationInvocation(BinderInvocation invocation, ITypeSymbol? type)
            {
                IMethodSymbol targetMethod = invocation.Operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;

                int paramCount = @params.Length;
                Debug.Assert(paramCount >= 2);

                if (paramCount is 3 &&
                    @params[1].Type.SpecialType is SpecialType.System_String &&
                    SymbolEqualityComparer.Default.Equals(_typeSymbols.ActionOfBinderOptions, @params[2].Type))
                {
                    EnqueueTargetTypeForRootInvocation(type, MethodsToGen.OptionsBuilderExt_BindConfiguration_T_path_BinderOptions, invocation);
                }
            }

            private void RegisterInterceptor_OptionsBuilderExt(TypeParseInfo typeParseInfo, TypeSpec typeSpec)
            {
                MethodsToGen overload = typeParseInfo.BindingOverload;
                Debug.Assert((MethodsToGen.OptionsBuilderExt_Any & overload) is not 0);

                if (typeSpec is not ComplexTypeSpec complexTypeSpec)
                {
                    return;
                }

                if ((MethodsToGen.OptionsBuilderExt_Bind & overload) is not 0)
                {
                    if (!TryRegisterTypeForOverloadGen_ServiceCollectionExt(MethodsToGen.ServiceCollectionExt_Configure_T_name_BinderOptions, complexTypeSpec))
                    {
                        return;
                    }
                }
                else if (!_helperInfoBuilder!.TryRegisterTypeForBindCoreMainGen(complexTypeSpec))
                {
                    return;
                }

                _interceptorInfoBuilder.RegisterInterceptor(typeParseInfo.BindingOverload, typeParseInfo.BinderInvocation.Operation);

                // Emitting refs to IOptionsChangeTokenSource, ConfigurationChangeTokenSource.
                _helperInfoBuilder!.RegisterNamespace("Microsoft.Extensions.Options");

                // Emitting refs to OptionsBuilder<T>.
                _helperInfoBuilder!.RegisterNamespace("Microsoft.Extensions.DependencyInjection");
            }
        }
    }
}
