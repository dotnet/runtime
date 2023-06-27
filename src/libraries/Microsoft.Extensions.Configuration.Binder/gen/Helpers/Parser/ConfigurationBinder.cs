// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
            private void RegisterMethodInvocation_ConfigurationBinder(BinderInvocation invocation)
            {
                switch (invocation.Operation.TargetMethod.Name)
                {
                    case nameof(MethodsToGen_ConfigurationBinder.Bind):
                        {
                            RegisterBindInvocation(invocation);
                        }
                        break;
                    case nameof(MethodsToGen_ConfigurationBinder.Get):
                        {
                            RegisterGetInvocation(invocation);
                        }
                        break;
                    case nameof(MethodsToGen_ConfigurationBinder.GetValue):
                        {
                            RegisterGetValueInvocation(invocation);
                        }
                        break;
                    default:
                        return;
                }
            }

            private void RegisterBindInvocation(BinderInvocation invocation)
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

                int objectIndex = overload switch
                {
                    MethodsToGen_ConfigurationBinder.Bind_instance => 1,
                    MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions => 1,
                    MethodsToGen_ConfigurationBinder.Bind_key_instance => 2,
                    _ => throw new InvalidOperationException()
                };

                IArgumentOperation objectArg = operation.Arguments[objectIndex];
                if (objectArg.Parameter.Type.SpecialType != SpecialType.System_Object)
                {
                    return;
                }

                ITypeSymbol? type = ResolveType(objectArg.Value)?.WithNullableAnnotation(NullableAnnotation.None);

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

                if (GetOrCreateTypeSpec(type, invocation.Location) is TypeSpec typeSpec)
                {
                    Dictionary<MethodsToGen_ConfigurationBinder, HashSet<TypeSpec>> types = _sourceGenSpec.TypesForGen_ConfigurationBinder_BindMethods;

                    if (!types.TryGetValue(overload, out HashSet<TypeSpec>? typeSpecs))
                    {
                        types[overload] = typeSpecs = new HashSet<TypeSpec>();
                    }

                    _sourceGenSpec.MethodsToGen_ConfigurationBinder |= overload;
                    typeSpecs.Add(typeSpec);
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCore, typeSpec);
                }

                static ITypeSymbol? ResolveType(IOperation conversionOperation) =>
                    conversionOperation switch
                    {
                        IConversionOperation c => ResolveType(c.Operand),
                        IInstanceReferenceOperation i => i.Type,
                        ILocalReferenceOperation l => l.Local.Type,
                        IFieldReferenceOperation f => f.Field.Type,
                        IMethodReferenceOperation m when m.Method.MethodKind == MethodKind.Constructor => m.Method.ContainingType,
                        IMethodReferenceOperation m => m.Method.ReturnType,
                        IAnonymousFunctionOperation f => f.Symbol.ReturnType,
                        _ => null
                    };
            }

            private void RegisterGetInvocation(BinderInvocation invocation)
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

                if (GetBindingConfigType(type, invocation.Location) is TypeSpec typeSpec)
                {
                    _sourceGenSpec.MethodsToGen_ConfigurationBinder |= overload;
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetCore, typeSpec);
                }
            }

            private void RegisterGetValueInvocation(BinderInvocation invocation)
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
                    GetOrCreateTypeSpec(type, invocation.Location) is TypeSpec typeSpec)
                {
                    _sourceGenSpec.MethodsToGen_ConfigurationBinder |= overload;
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetValueCore, typeSpec);
                }
            }
        }
    }
}
