// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private bool ShouldEmitMethods(MethodsToGen_ConfigurationBinder methods) => (_sourceGenSpec.MethodsToGen_ConfigurationBinder & methods) != 0;

            private void EmitBindingExtensions_IConfiguration()
            {
                if (!ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Any))
                {
                    return;
                }

                EmitBindingExtStartRegion(Identifier.IConfiguration);
                EmitGetMethods();
                EmitGetValueMethods();
                EmitBindMethods_ConfigurationBinder();
                EmitBindingExtEndRegion();
            }

            private void EmitGetMethods()
            {
                const string expressionForGetCore = nameof(MethodsToGen_CoreBindingHelper.GetCore);
                const string documentation = "Attempts to bind the configuration instance to a new instance of type T.";

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Get_T))
                {
                    StartMethodDefinition(MethodsToGen_ConfigurationBinder.Get_T, documentation);
                    _writer.WriteLine($"public static T? {Identifier.Get}<T>(this {Identifier.IConfiguration} {Identifier.configuration}) => " +
                        $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureOptions}: null) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Get_T_BinderOptions))
                {
                    StartMethodDefinition(MethodsToGen_ConfigurationBinder.Get_T_BinderOptions, documentation);
                    _writer.WriteLine($"public static T? {Identifier.Get}<T>(this {Identifier.IConfiguration} {Identifier.configuration}, {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}) => " +
                        $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureOptions}) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Get_TypeOf))
                {
                    StartMethodDefinition(MethodsToGen_ConfigurationBinder.Get_TypeOf, documentation);
                    _writer.WriteLine($"public static object? {Identifier.Get}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}) => " +
                        $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureOptions}: null);");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Get_TypeOf_BinderOptions))
                {
                    StartMethodDefinition(MethodsToGen_ConfigurationBinder.Get_TypeOf_BinderOptions, documentation);
                    _writer.WriteLine($"public static object? {Identifier.Get}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}) => " +
                        $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureOptions});");
                }
            }

            private void EmitGetValueMethods()
            {
                const string expressionForGetValueCore = $"{Identifier.BindingExtensions}.{nameof(MethodsToGen_CoreBindingHelper.GetValueCore)}";
                const string documentation = "Extracts the value with the specified key and converts it to the specified type.";

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.GetValue_T_key))
                {
                    StartMethodDefinition(MethodsToGen_ConfigurationBinder.GetValue_T_key, documentation);
                    _writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {Identifier.IConfiguration} {Identifier.configuration}, string {Identifier.key}) => " +
                        $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.GetValue_T_key_defaultValue))
                {
                    StartMethodDefinition(MethodsToGen_ConfigurationBinder.GetValue_T_key_defaultValue, documentation);
                    _writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {Identifier.IConfiguration} {Identifier.configuration}, string {Identifier.key}, T {Identifier.defaultValue}) => " +
                        $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? {Identifier.defaultValue});");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.GetValue_TypeOf_key))
                {
                    StartMethodDefinition(MethodsToGen_ConfigurationBinder.GetValue_TypeOf_key, documentation);
                    _writer.WriteLine($"public static object? {Identifier.GetValue}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key}) => " +
                        $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key});");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.GetValue_TypeOf_key_defaultValue))
                {
                    StartMethodDefinition(MethodsToGen_ConfigurationBinder.GetValue_TypeOf_key_defaultValue, documentation);
                    _writer.WriteLine($"public static object? {Identifier.GetValue}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key}, object? {Identifier.defaultValue}) => " +
                        $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key}) ?? {Identifier.defaultValue};");
                }
            }

            private void EmitBindMethods_ConfigurationBinder()
            {
                if (!ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Bind))
                {
                    return;
                }

                string instanceParamExpr = $"object? {Identifier.instance}";

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Bind_instance))
                {
                    EmitMethods(
                        MethodsToGen_ConfigurationBinder.Bind_instance,
                        additionalParams: instanceParamExpr,
                        configExpression: Identifier.configuration,
                        configureOptions: false);
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions))
                {
                    EmitMethods(
                        MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions,
                        additionalParams: $"{instanceParamExpr}, {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}",
                        configExpression: Identifier.configuration,
                        configureOptions: true);
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Bind_key_instance))
                {
                    EmitMethods(
                        MethodsToGen_ConfigurationBinder.Bind_key_instance,
                        additionalParams: $"string {Identifier.key}, {instanceParamExpr}",
                        configExpression: $"{Expression.configurationGetSection}({Identifier.key})",
                        configureOptions: false);
                }

                void EmitMethods(MethodsToGen_ConfigurationBinder method, string additionalParams, string configExpression, bool configureOptions)
                {
                    foreach (KeyValuePair<TypeSpec, List<InterceptorLocationInfo>> pair in _sourceGenSpec.InterceptionInfo_ConfigBinder.GetOverloadInfo(method))
                    {
                        (TypeSpec type, List<InterceptorLocationInfo> interceptorInfoList) = (pair.Key, pair.Value);

                        EmitBlankLineIfRequired();
                        _writer.WriteLine($"/// <summary>Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.</summary>");
                        EmitInterceptsLocationAnnotations(interceptorInfoList);
                        EmitStartBlock($"public static void {Identifier.Bind}_{type.DisplayString.ToIdentifierSubstring()}(this {Identifier.IConfiguration} {Identifier.configuration}, {additionalParams})");

                        if (type.NeedsMemberBinding)
                        {
                            string binderOptionsArg = configureOptions ? $"{Identifier.GetBinderOptions}({Identifier.configureOptions})" : $"{Identifier.binderOptions}: null";

                            EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);
                            if (!type.IsValueType)
                            {
                                EmitCheckForNullArgument_WithBlankLine(Identifier.instance, voidReturn: true);
                            }
                            _writer.WriteLine($$"""
                                var {{Identifier.typedObj}} = ({{type.EffectiveType.DisplayString}}){{Identifier.instance}};
                                {{nameof(MethodsToGen_CoreBindingHelper.BindCore)}}({{configExpression}}, ref {{Identifier.typedObj}}, {{binderOptionsArg}});
                                """);
                        }

                        EmitEndBlock();
                    }
                }
            }

            private void StartMethodDefinition(MethodsToGen_ConfigurationBinder method, string documentation)
            {
                EmitBlankLineIfRequired();
                _writer.WriteLine($"/// <summary>{documentation}</summary>");
                EmitInterceptsLocationAnnotations(method);
            }
        }
    }
}
