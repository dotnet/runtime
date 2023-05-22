// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.Extensions.Configuration.Binder.SourceGeneration.Parser;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed partial record ConfigBinderMethodSpec : MethodSpec
    {
        private Dictionary<MethodSpecifier, HashSet<TypeSpec>> _typesForBindMethodGen { get; } = new();

        private MethodSpecifier _methodsToGen { get; set; }

        public ConfigBinderMethodSpec(SourceGenSpec spec) : base(spec) { }

        public override void RegisterInvocation(Parser parser, BinderInvocation invocation)
        {
            switch (invocation.Operation.TargetMethod.Name)
            {
                case nameof(MethodSpecifier.Bind):
                    {
                        RegisterBindInvocation(parser, invocation);
                    }
                    break;
                case nameof(MethodSpecifier.Get):
                    {
                        RegisterGetInvocation(parser, invocation);
                    }
                    break;
                case nameof(MethodSpecifier.GetValue):
                    {
                        RegisterGetValueInvocation(parser, invocation);
                    }
                    break;
                default:
                    return;
            }
        }

        private void RegisterBindInvocation(Parser parser, BinderInvocation invocation)
        {
            IInvocationOperation operation = invocation.Operation!;
            ImmutableArray<IParameterSymbol> @params = operation.TargetMethod.Parameters;
            int paramCount = @params.Length;

            if (!SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.IConfiguration, @params[0].Type))
            {
                return;
            }

            MethodSpecifier overload = MethodSpecifier.None;

            if (paramCount is 2)
            {
                overload = MethodSpecifier.Bind_instance;
            }
            else if (paramCount is 3)
            {
                if (@params[1].Type.SpecialType is SpecialType.System_String)
                {
                    overload = MethodSpecifier.Bind_key_instance;
                }
                else if (SymbolEqualityComparer.Default.Equals(@params[2].Type, parser.TypeSymbols.ActionOfBinderOptions))
                {
                    overload = MethodSpecifier.Bind_instance_BinderOptions;
                }
            }

            if (overload is MethodSpecifier.None)
            {
                return;
            }

            int objectIndex = overload switch
            {
                MethodSpecifier.Bind_instance => 1,
                MethodSpecifier.Bind_instance_BinderOptions => 1,
                MethodSpecifier.Bind_key_instance => 2,
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
                parser.Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CouldNotDetermineTypeInfo, invocation.Location));
                return;
            }

            if (type!.IsValueType)
            {
                parser.Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ValueTypesInvalidForBind, invocation.Location, type));
                return;
            }

            if (parser.GetOrCreateTypeSpec(type, invocation.Location) is TypeSpec typeSpec)
            {
                if (!_typesForBindMethodGen.TryGetValue(overload, out HashSet<TypeSpec>? typeSpecs))
                {
                    _typesForBindMethodGen[overload] = typeSpecs = new HashSet<TypeSpec>();
                }

                _methodsToGen |= overload;
                typeSpecs.Add(typeSpec);
                SourceGenSpec.CoreBindingHelperSpec.RegisterTypeForMethodGen(CoreBindingHelperMethodSpec.MethodSpecifier.BindCore, typeSpec);
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

        private void RegisterGetInvocation(Parser parser, BinderInvocation invocation)
        {
            IInvocationOperation operation = invocation.Operation!;
            IMethodSymbol targetMethod = operation.TargetMethod;
            ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
            int paramCount = @params.Length;

            if (!SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.IConfiguration, @params[0].Type))
            {
                return;
            }

            MethodSpecifier overload = MethodSpecifier.None;
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
                    overload = MethodSpecifier.Get_T;
                }
                else if (paramCount is 2 && SymbolEqualityComparer.Default.Equals(@params[1].Type, parser.TypeSymbols.ActionOfBinderOptions))
                {
                    overload = MethodSpecifier.Get_T_BinderOptions;
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
                    overload = MethodSpecifier.Get_TypeOf;
                }
                else if (paramCount is 3 && SymbolEqualityComparer.Default.Equals(@params[2].Type, parser.TypeSymbols.ActionOfBinderOptions))
                {
                    overload = MethodSpecifier.Get_TypeOf_BinderOptions;
                }
            }

            if (parser.GetBindingConfigType(type, invocation.Location) is TypeSpec typeSpec)
            {
                _methodsToGen |= overload;
                SourceGenSpec.CoreBindingHelperSpec.RegisterTypeForMethodGen(CoreBindingHelperMethodSpec.MethodSpecifier.GetCore, typeSpec);
            }
        }

        private void RegisterGetValueInvocation(Parser parser, BinderInvocation invocation)
        {
            IInvocationOperation operation = invocation.Operation!;
            IMethodSymbol targetMethod = operation.TargetMethod;
            ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
            int paramCount = @params.Length;

            MethodSpecifier overload = MethodSpecifier.None;
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
                    overload = MethodSpecifier.GetValue_T_key;
                }
                else if (paramCount is 3 && SymbolEqualityComparer.Default.Equals(@params[2].Type, type))
                {
                    overload = MethodSpecifier.GetValue_T_key_defaultValue;
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
                    overload = MethodSpecifier.GetValue_TypeOf_key;
                }
                else if (paramCount is 4 && @params[3].Type.SpecialType is SpecialType.System_Object)
                {
                    overload = MethodSpecifier.GetValue_TypeOf_key_defaultValue;
                }
            }

            ITypeSymbol effectiveType = (IsNullable(type, out ITypeSymbol? underlyingType) ? underlyingType : type)!;

            if (!IsValidRootConfigType(type))
            {
                parser.Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CouldNotDetermineTypeInfo, invocation.Location));
                return;
            }

            if (parser.IsParsableFromString(effectiveType, out _) &&
                parser.GetOrCreateTypeSpec(type, invocation.Location) is TypeSpec typeSpec)
            {
                _methodsToGen |= overload;
                SourceGenSpec.CoreBindingHelperSpec.RegisterTypeForMethodGen(CoreBindingHelperMethodSpec.MethodSpecifier.GetValueCore, typeSpec);
            }
        }

        /// <summary>
        /// Methods on Microsoft.Extensions.Configuration.ConfigurationBinder
        /// </summary>
        [Flags]
        internal enum MethodSpecifier
        {
            None = 0x0,

            /// <summary>
            /// Bind(IConfiguration, object).
            /// </summary>
            Bind_instance = 0x1,

            /// <summary>
            /// Bind(IConfiguration, object, Action<BinderOptions>).
            /// </summary>
            Bind_instance_BinderOptions = 0x2,

            /// <summary>
            /// Bind(IConfiguration, string, object).
            /// </summary>
            Bind_key_instance = 0x4,

            /// <summary>
            /// Get<T>(IConfiguration).
            /// </summary>
            Get_T = 0x8,

            /// <summary>
            /// Get<T>(IConfiguration, Action<BinderOptions>).
            /// </summary>
            Get_T_BinderOptions = 0x10,

            /// <summary>
            /// Get<T>(IConfiguration, Type).
            /// </summary>
            Get_TypeOf = 0x20,

            /// <summary>
            /// Get<T>(IConfiguration, Type, Action<BinderOptions>).
            /// </summary>
            Get_TypeOf_BinderOptions = 0x40,

            /// <summary>
            /// GetValue(IConfiguration, Type, string).
            /// </summary>
            GetValue_TypeOf_key = 0x80,

            /// <summary>
            /// GetValue(IConfiguration, Type, object).
            /// </summary>
            GetValue_TypeOf_key_defaultValue = 0x100,

            /// <summary>
            /// GetValue<T>(IConfiguration, string).
            /// </summary>
            GetValue_T_key = 0x200,

            /// <summary>
            /// GetValue<T>(IConfiguration, string, T).
            /// </summary>
            GetValue_T_key_defaultValue = 0x400,

            // Method groups
            Bind = Bind_instance | Bind_instance_BinderOptions | Bind_key_instance,
            Get = Get_T | Get_T_BinderOptions | Get_TypeOf | Get_TypeOf_BinderOptions,
            GetValue = GetValue_T_key | GetValue_T_key_defaultValue | GetValue_TypeOf_key | GetValue_TypeOf_key_defaultValue,
        }
    }
}
