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
            private void RegisterMethodInvocation_ServiceCollectionExt(BinderInvocation invocation)
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

                MethodsToGen_Extensions_ServiceCollection overload;

                if (paramCount is 2 && SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, @params[1].Type))
                {
                    overload = MethodsToGen_Extensions_ServiceCollection.Configure_T;
                }
                else if (paramCount is 3)
                {
                    ITypeSymbol? secondParamType = @params[1].Type;
                    ITypeSymbol? thirdParamType = @params[2].Type;

                    if (secondParamType.SpecialType is SpecialType.System_String &&
                        SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, thirdParamType))
                    {
                        overload = MethodsToGen_Extensions_ServiceCollection.Configure_T_name;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, secondParamType) &&
                        SymbolEqualityComparer.Default.Equals(_typeSymbols.ActionOfBinderOptions, thirdParamType))
                    {
                        overload = MethodsToGen_Extensions_ServiceCollection.Configure_T_BinderOptions;
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
                    overload = MethodsToGen_Extensions_ServiceCollection.Configure_T_name_BinderOptions;
                }
                else
                {
                    Debug.Assert(paramCount is 4);
                    return;
                }

                TypeSpec typeSpec = GetBindingConfigType(
                    type: targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None),
                    invocation.Location);

                if (typeSpec is null)
                {
                    return;
                }

                RegisterTypeForMethodGen(overload, typeSpec);
            }

            private void RegisterTypeForMethodGen(MethodsToGen_Extensions_ServiceCollection overload, TypeSpec typeSpec)
            {
                _sourceGenSpec.MethodsToGen_ServiceCollectionExt |= overload;
                RegisterTypeForBindCoreUntypedGen(typeSpec);
            }
        }
    }
}
