// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed partial record ServiceCollectionMethodSpec : MethodSpec
    {
        private MethodSpecifier _methodsToGen;

        public ServiceCollectionMethodSpec(SourceGenSpec spec) : base(spec) { }

        public override void RegisterInvocation(Parser parser, BinderInvocation invocation)
        {
            IInvocationOperation operation = invocation.Operation!;
            IMethodSymbol targetMethod = operation.TargetMethod;
            ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
            int paramCount = @params.Length;

            if (!targetMethod.IsGenericMethod ||
                !SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.IServiceCollection, @params[0].Type))
            {
                return;
            }

            if (paramCount is < 2 or > 4)
            {
                return;
            }

            MethodSpecifier overload;

            if (paramCount is 2 && SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.IConfiguration, @params[1].Type))
            {
                overload = MethodSpecifier.Configure_T;
            }
            else if (paramCount is 3)
            {
                ITypeSymbol? secondParamType = @params[1].Type;
                ITypeSymbol? thirdParamType = @params[2].Type;

                if (secondParamType.SpecialType is SpecialType.System_String &&
                    SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.IConfiguration, thirdParamType))
                {
                    overload = MethodSpecifier.Configure_T_name;
                }
                else if (SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.IConfiguration, secondParamType) &&
                    SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.ActionOfBinderOptions, thirdParamType))
                {
                    overload = MethodSpecifier.Configure_T_BinderOptions;
                }
                else
                {
                    return;
                }
            }
            else if (paramCount is 4 &&
                @params[1].Type.SpecialType is SpecialType.System_String &&
                SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.IConfiguration, @params[2].Type) &&
                SymbolEqualityComparer.Default.Equals(parser.TypeSymbols.ActionOfBinderOptions, @params[3].Type))
            {
                Debug.Assert(paramCount is 4);
                overload = MethodSpecifier.Configure_T_name_BinderOptions;
            }
            else
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

            RegisterTypeForMethodGen(overload, typeSpec);
        }

        public void RegisterTypeForMethodGen(MethodSpecifier overload, TypeSpec typeSpec)
        {
            _methodsToGen |= overload;
            SourceGenSpec.CoreBindingHelperSpec.RegisterTypeForBindCoreUntypedGen(typeSpec);
        }

        /// <summary>
        /// Methods on Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions
        /// </summary>
        [Flags]
        public enum MethodSpecifier
        {
            None = 0x0,

            /// <summary>
            /// Configure<T>(IServiceCollection, IConfiguration).
            /// </summary>
            Configure_T = 0x1,

            /// <summary>
            /// Configure<T>(IServiceCollection, string, IConfiguration).
            /// </summary>
            Configure_T_name = 0x2,

            /// <summary>
            /// Configure<T>(IServiceCollection, IConfiguration, Action<BinderOptions>?).
            /// </summary>
            Configure_T_BinderOptions = 0x4,

            /// <summary>
            /// Configure<T>(IServiceCollection, string, IConfiguration, Action<BinderOptions>?).
            /// </summary>
            Configure_T_name_BinderOptions = 0x8,

            Configure = Configure_T | Configure_T_name | Configure_T_BinderOptions | Configure_T_name_BinderOptions
        }
    }
}
