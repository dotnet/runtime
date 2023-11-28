// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        internal sealed partial class Parser
        {
            private void ParseInvocation_ConfigurationBinder(BinderInvocation invocation)
            {
                switch (invocation.Operation.TargetMethod.Name)
                {
                    case "Bind":
                        {
                            ParseBindInvocation_ConfigurationBinder(invocation);
                        }
                        break;
                    case "Get":
                        {
                            ParseGetInvocation(invocation);
                        }
                        break;
                    case "GetValue":
                        {
                            ParseGetValueInvocation(invocation);
                        }
                        break;
                }
            }

            private void ParseBindInvocation_ConfigurationBinder(BinderInvocation invocation)
            {
                IInvocationOperation operation = invocation.Operation!;
                ImmutableArray<IParameterSymbol> @params = operation.TargetMethod.Parameters;
                int paramCount = @params.Length;

                if (!SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, @params[0].Type))
                {
                    return;
                }

                MethodsToGen overload = MethodsToGen.None;

                if (paramCount is 2)
                {
                    overload = MethodsToGen.ConfigBinder_Bind_instance;
                }
                else if (paramCount is 3)
                {
                    if (@params[1].Type.SpecialType is SpecialType.System_String)
                    {
                        overload = MethodsToGen.ConfigBinder_Bind_key_instance;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(@params[2].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        overload = MethodsToGen.ConfigBinder_Bind_instance_BinderOptions;
                    }
                }

                if (overload is MethodsToGen.None)
                {
                    return;
                }

                int instanceIndex = overload switch
                {
                    MethodsToGen.ConfigBinder_Bind_instance => 1,
                    MethodsToGen.ConfigBinder_Bind_instance_BinderOptions => 1,
                    MethodsToGen.ConfigBinder_Bind_key_instance => 2,
                    _ => throw new InvalidOperationException()
                };

                IArgumentOperation instanceArg = GetArgumentForParameterAtIndex(operation.Arguments, instanceIndex);
                if (instanceArg.Parameter?.Type.SpecialType is not SpecialType.System_Object)
                {
                    return;
                }

                ITypeSymbol? type = ResolveType(instanceArg.Value)?.WithNullableAnnotation(NullableAnnotation.None);

                if (!IsValidRootConfigType(type))
                {
                    RecordDiagnostic(DiagnosticDescriptors.CouldNotDetermineTypeInfo, invocation.Location);
                    return;
                }

                if (type.IsValueType)
                {
                    RecordDiagnostic(DiagnosticDescriptors.ValueTypesInvalidForBind, invocation.Location, messageArgs: new object[] { type });
                    return;
                }

                EnqueueTargetTypeForRootInvocation(type, overload, invocation);

                static ITypeSymbol? ResolveType(IOperation conversionOperation) =>
                    conversionOperation switch
                    {
                        IConversionOperation c => ResolveType(c.Operand),
                        IInstanceReferenceOperation i => i.Type,
                        ILocalReferenceOperation l => l.Local.Type,
                        IFieldReferenceOperation f => f.Field.Type,
                        IPropertyReferenceOperation o => o.Type,
                        IMethodReferenceOperation m when m.Method.MethodKind == MethodKind.Constructor => m.Method.ContainingType,
                        IMethodReferenceOperation m => m.Method.ReturnType,
                        IAnonymousFunctionOperation f => f.Symbol.ReturnType,
                        IParameterReferenceOperation p => p.Parameter.Type,
                        IObjectCreationOperation o => o.Type,
                        _ => null
                    };
            }

            private static IArgumentOperation GetArgumentForParameterAtIndex(ImmutableArray<IArgumentOperation> arguments, int parameterIndex)
            {
                foreach (var argument in arguments)
                {
                    if (argument.Parameter?.Ordinal == parameterIndex)
                    {
                        return argument;
                    }
                }

                throw new InvalidOperationException();
            }

            private void ParseGetInvocation(BinderInvocation invocation)
            {
                IInvocationOperation operation = invocation.Operation!;
                IMethodSymbol targetMethod = operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
                int paramCount = @params.Length;

                if (!SymbolEqualityComparer.Default.Equals(_typeSymbols.IConfiguration, @params[0].Type))
                {
                    return;
                }

                MethodsToGen overload = MethodsToGen.None;
                ITypeSymbol? type;

                if (targetMethod.IsGenericMethod)
                {
                    if (paramCount > 2)
                    {
                        return;
                    }

                    type = targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);

                    if (paramCount is 1)
                    {
                        overload = MethodsToGen.ConfigBinder_Get_T;
                    }
                    else if (paramCount is 2 && SymbolEqualityComparer.Default.Equals(@params[1].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        overload = MethodsToGen.ConfigBinder_Get_T_BinderOptions;
                    }
                }
                else if (paramCount > 3)
                {
                    return;
                }
                else
                {
                    ITypeOfOperation? typeOfOperation = GetArgumentForParameterAtIndex(operation.Arguments, 1).ChildOperations.FirstOrDefault() as ITypeOfOperation;
                    type = typeOfOperation?.TypeOperand;

                    if (paramCount is 2)
                    {
                        overload = MethodsToGen.ConfigBinder_Get_TypeOf;
                    }
                    else if (paramCount is 3 && SymbolEqualityComparer.Default.Equals(@params[2].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        overload = MethodsToGen.ConfigBinder_Get_TypeOf_BinderOptions;
                    }
                }

                EnqueueTargetTypeForRootInvocation(type, overload, invocation);
            }

            private void ParseGetValueInvocation(BinderInvocation invocation)
            {
                IInvocationOperation operation = invocation.Operation!;
                IMethodSymbol targetMethod = operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
                int paramCount = @params.Length;

                MethodsToGen overload = MethodsToGen.None;
                ITypeSymbol? type;

                if (targetMethod.IsGenericMethod)
                {
                    if (paramCount > 3 || @params[1].Type.SpecialType is not SpecialType.System_String)
                    {
                        return;
                    }

                    type = targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);

                    if (paramCount is 2)
                    {
                        overload = MethodsToGen.ConfigBinder_GetValue_T_key;
                    }
                    else if (paramCount is 3 && SymbolEqualityComparer.Default.Equals(@params[2].Type, type))
                    {
                        overload = MethodsToGen.ConfigBinder_GetValue_T_key_defaultValue;
                    }
                }
                else if (paramCount > 4)
                {
                    return;
                }
                else
                {
                    if (@params[2].Type.SpecialType is not SpecialType.System_String)
                    {
                        return;
                    }

                    ITypeOfOperation? typeOfOperation = GetArgumentForParameterAtIndex(operation.Arguments, 1).ChildOperations.FirstOrDefault() as ITypeOfOperation;
                    type = typeOfOperation?.TypeOperand;

                    if (paramCount is 3)
                    {
                        overload = MethodsToGen.ConfigBinder_GetValue_TypeOf_key;
                    }
                    else if (paramCount is 4 && @params[3].Type.SpecialType is SpecialType.System_Object)
                    {
                        overload = MethodsToGen.ConfigBinder_GetValue_TypeOf_key_defaultValue;
                    }
                }

                if (!IsValidRootConfigType(type))
                {
                    RecordDiagnostic(DiagnosticDescriptors.CouldNotDetermineTypeInfo, invocation.Location);
                    return;
                }

                ITypeSymbol effectiveType = IsNullable(type, out ITypeSymbol? underlyingType) ? underlyingType : type;

                if (IsParsableFromString(effectiveType, out _))
                {
                    EnqueueTargetTypeForRootInvocation(type, overload, invocation);
                }
            }

            private void RegisterInterceptor_ConfigurationBinder(TypeParseInfo typeParseInfo, TypeSpec typeSpec)
            {
                MethodsToGen overload = typeParseInfo.BindingOverload;
                IInvocationOperation invocationOperation = typeParseInfo.BinderInvocation!.Operation;
                Debug.Assert((MethodsToGen.ConfigBinder_Any & overload) is not 0);

                if ((MethodsToGen.ConfigBinder_Bind & overload) is not 0)
                {
                    if (typeSpec is ComplexTypeSpec complexTypeSpec &&
                        _helperInfoBuilder!.TryRegisterTransitiveTypesForMethodGen(complexTypeSpec.TypeRef))
                    {
                        _interceptorInfoBuilder.RegisterInterceptor_ConfigBinder_Bind(overload, complexTypeSpec, invocationOperation);
                    }
                }
                else
                {
                    Debug.Assert((MethodsToGen.ConfigBinder_Get & overload) is not 0 ||
                        (MethodsToGen.ConfigBinder_GetValue & overload) is not 0);

                    bool registered = (MethodsToGen.ConfigBinder_Get & overload) is not 0
                        ? _helperInfoBuilder!.TryRegisterTypeForGetGen(typeSpec)
                        : _helperInfoBuilder!.TryRegisterTypeForGetValueGen(typeSpec);

                    if (registered)
                    {
                        _interceptorInfoBuilder.RegisterInterceptor(overload, invocationOperation);
                    }
                }
            }
        }
    }
}
