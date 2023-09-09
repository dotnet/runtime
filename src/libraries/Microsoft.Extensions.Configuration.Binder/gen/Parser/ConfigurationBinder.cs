﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Parser
        {
            private void ParseInvocation_ConfigurationBinder(BinderInvocation invocation)
            {
                switch (invocation.Operation.TargetMethod.Name)
                {
                    case nameof(MethodsToGen_ConfigurationBinder.Bind):
                        {
                            ParseBindInvocation_ConfigurationBinder(invocation);
                        }
                        break;
                    case nameof(MethodsToGen_ConfigurationBinder.Get):
                        {
                            ParseGetInvocation(invocation);
                        }
                        break;
                    case nameof(MethodsToGen_ConfigurationBinder.GetValue):
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

                MethodsToGen_ConfigurationBinder overload = MethodsToGen_ConfigurationBinder.None;

                if (paramCount is 2)
                {
                    overload = MethodsToGen_ConfigurationBinder.Bind_instance;
                }
                else if (paramCount is 3)
                {
                    if (@params[1].Type.SpecialType is SpecialType.System_String)
                    {
                        overload = MethodsToGen_ConfigurationBinder.Bind_key_instance;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(@params[2].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        overload = MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions;
                    }
                }

                if (overload is MethodsToGen_ConfigurationBinder.None)
                {
                    return;
                }

                int instanceIndex = overload switch
                {
                    MethodsToGen_ConfigurationBinder.Bind_instance => 1,
                    MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions => 1,
                    MethodsToGen_ConfigurationBinder.Bind_key_instance => 2,
                    _ => throw new InvalidOperationException()
                };

                IArgumentOperation instanceArg = operation.Arguments[instanceIndex];
                if (instanceArg.Parameter.Type.SpecialType != SpecialType.System_Object)
                {
                    return;
                }

                ITypeSymbol? type = ResolveType(instanceArg.Value)?.WithNullableAnnotation(NullableAnnotation.None);

                if (!IsValidRootConfigType(type))
                {
                    _context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CouldNotDetermineTypeInfo, invocation.Location));
                    return;
                }

                if (type!.IsValueType)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ValueTypesInvalidForBind, invocation.Location, type));
                    return;
                }

                if (GetTargetTypeForRootInvocationCore(type, invocation.Location) is TypeSpec typeSpec)
                {
                    RegisterInterceptor(overload, typeSpec, invocation.Operation);
                }

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

                MethodsToGen_ConfigurationBinder overload = MethodsToGen_ConfigurationBinder.None;
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
                        overload = MethodsToGen_ConfigurationBinder.Get_T;
                    }
                    else if (paramCount is 2 && SymbolEqualityComparer.Default.Equals(@params[1].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        overload = MethodsToGen_ConfigurationBinder.Get_T_BinderOptions;
                    }
                }
                else if (paramCount > 3)
                {
                    return;
                }
                else
                {
                    ITypeOfOperation? typeOfOperation = operation.Arguments[1].ChildOperations.FirstOrDefault() as ITypeOfOperation;
                    type = typeOfOperation?.TypeOperand;

                    if (paramCount is 2)
                    {
                        overload = MethodsToGen_ConfigurationBinder.Get_TypeOf;
                    }
                    else if (paramCount is 3 && SymbolEqualityComparer.Default.Equals(@params[2].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        overload = MethodsToGen_ConfigurationBinder.Get_TypeOf_BinderOptions;
                    }
                }

                if (GetTargetTypeForRootInvocation(type, invocation.Location) is TypeSpec typeSpec)
                {
                    RegisterInvocation(overload, invocation.Operation);
                    RegisterTypeForGetCoreGen(typeSpec);
                }

            }

            private void ParseGetValueInvocation(BinderInvocation invocation)
            {
                IInvocationOperation operation = invocation.Operation!;
                IMethodSymbol targetMethod = operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
                int paramCount = @params.Length;

                MethodsToGen_ConfigurationBinder overload = MethodsToGen_ConfigurationBinder.None;
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
                        overload = MethodsToGen_ConfigurationBinder.GetValue_T_key;
                    }
                    else if (paramCount is 3 && SymbolEqualityComparer.Default.Equals(@params[2].Type, type))
                    {
                        overload = MethodsToGen_ConfigurationBinder.GetValue_T_key_defaultValue;
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

                    ITypeOfOperation? typeOfOperation = operation.Arguments[1].ChildOperations.FirstOrDefault() as ITypeOfOperation;
                    type = typeOfOperation?.TypeOperand;

                    if (paramCount is 3)
                    {
                        overload = MethodsToGen_ConfigurationBinder.GetValue_TypeOf_key;
                    }
                    else if (paramCount is 4 && @params[3].Type.SpecialType is SpecialType.System_Object)
                    {
                        overload = MethodsToGen_ConfigurationBinder.GetValue_TypeOf_key_defaultValue;
                    }
                }

                ITypeSymbol effectiveType = (IsNullable(type, out ITypeSymbol? underlyingType) ? underlyingType : type)!;

                if (!IsValidRootConfigType(type))
                {
                    _context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CouldNotDetermineTypeInfo, invocation.Location));
                    return;
                }

                if (IsParsableFromString(effectiveType, out _) &&
                    GetTargetTypeForRootInvocationCore(type, invocation.Location) is TypeSpec typeSpec)
                {
                    RegisterInvocation(overload, invocation.Operation);
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetValueCore, typeSpec);
                }
            }

            private void RegisterInvocation(MethodsToGen_ConfigurationBinder overload, IInvocationOperation operation)
            {
                _sourceGenSpec.MethodsToGen_ConfigurationBinder |= overload;
                RegisterInterceptor(overload, operation);
            }

            /// <summary>
            /// Registers generated Bind methods as interceptors. This is done differently from other root
            /// methods <see cref="RegisterInterceptor(Enum, IInvocationOperation)"/> because we need to
            /// explicitly account for the type to bind, to avoid type-check issues for polymorphic objects.
            /// </summary>
            private void RegisterInterceptor(MethodsToGen_ConfigurationBinder overload, TypeSpec typeSpec, IInvocationOperation operation)
            {
                _sourceGenSpec.MethodsToGen_ConfigurationBinder |= overload;
                _sourceGenSpec.InterceptionInfo_ConfigBinder.RegisterOverloadInfo(overload, typeSpec, operation);
            }
        }
    }
}
