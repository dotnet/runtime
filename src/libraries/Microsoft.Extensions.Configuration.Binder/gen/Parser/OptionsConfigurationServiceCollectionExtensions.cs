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
            private void ParseInvocation_ServiceCollectionExt(BinderInvocation invocation)
            {
                IInvocationOperation operation = invocation.Operation!;
                IMethodSymbol targetMethod = operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
                int paramCount = @params.Length;

                if (!targetMethod.IsGenericMethod ||
                    !SymbolEqualityComparer.Default.Equals(_typeSymbols.IServiceCollection, @params[0].Type))
                {
                    return;
                }

                if (paramCount is < 2 or > 4)
                {
                    return;
                }

                MethodsToGen overload;

                if (paramCount is 2 && SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, @params[1].Type))
                {
                    overload = MethodsToGen.ServiceCollectionExt_Configure_T;
                }
                else if (paramCount is 3)
                {
                    ITypeSymbol? secondParamType = @params[1].Type;
                    ITypeSymbol? thirdParamType = @params[2].Type;

                    if (secondParamType.SpecialType is SpecialType.System_String &&
                        SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, thirdParamType))
                    {
                        overload = MethodsToGen.ServiceCollectionExt_Configure_T_name;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, secondParamType) &&
                        SymbolEqualityComparer.Default.Equals(_typeSymbols.ActionOfBinderOptions, thirdParamType))
                    {
                        overload = MethodsToGen.ServiceCollectionExt_Configure_T_BinderOptions;
                    }
                    else
                    {
                        return;
                    }
                }
                else if (paramCount is 4 &&
                    @params[1].Type.SpecialType is SpecialType.System_String &&
                    SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, @params[2].Type) &&
                    SymbolEqualityComparer.Default.Equals(_typeSymbols.ActionOfBinderOptions, @params[3].Type))
                {
                    overload = MethodsToGen.ServiceCollectionExt_Configure_T_name_BinderOptions;
                }
                else
                {
                    Debug.Assert(paramCount is 4);
                    return;
                }

                ITypeSymbol? typeSymbol = targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                // This would violate generic type constraint; any such invocation could not have been included in the initial parser.
                Debug.Assert(typeSymbol?.IsValueType is not true);

                EnqueueTargetTypeForRootInvocation(typeSymbol, overload, invocation);
            }

            private void RegisterInterceptor_ServiceCollectionExt(TypeParseInfo typeParseInfo, TypeSpec typeSpec)
            {
                MethodsToGen overload = typeParseInfo.BindingOverload;

                if (typeSpec is ComplexTypeSpec complexTypeSpec &&
                    TryRegisterTypeForOverloadGen_ServiceCollectionExt(overload, complexTypeSpec))
                {
                    _interceptorInfoBuilder.RegisterInterceptor(overload, typeParseInfo.BinderInvocation.Operation);
                }
            }

            private bool TryRegisterTypeForOverloadGen_ServiceCollectionExt(MethodsToGen overload, ComplexTypeSpec typeSpec)
            {
                Debug.Assert((MethodsToGen.ServiceCollectionExt_Any & overload) is not 0);

                if (!_helperInfoBuilder!.TryRegisterTypeForBindCoreMainGen(typeSpec))
                {
                    return false;
                }

                _interceptorInfoBuilder.MethodsToGen |= overload;
                _helperInfoBuilder!.RegisterNamespace("Microsoft.Extensions.DependencyInjection");
                // Emitting refs to IOptionsChangeTokenSource, ConfigurationChangeTokenSource, IConfigureOptions<>, ConfigureNamedOptions<>.
                _helperInfoBuilder!.RegisterNamespace("Microsoft.Extensions.Options");
                return true;
            }
        }
    }
}
