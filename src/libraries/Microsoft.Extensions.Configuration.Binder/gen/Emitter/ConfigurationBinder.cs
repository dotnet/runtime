// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private void EmitBindingExtensions_IConfiguration()
            {
                if (!ShouldEmitMethods(MethodsToGen.ConfigBinder_Any))
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

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_Get_T))
                {
                    EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen.ConfigBinder_Get_T, documentation);
                    _writer.WriteLine($"public static T? {Identifier.Get}<T>(this {Identifier.IConfiguration} {Identifier.configuration}) => " +
                        $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureOptions}: null) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_Get_T_BinderOptions))
                {
                    EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen.ConfigBinder_Get_T_BinderOptions, documentation);
                    _writer.WriteLine($"public static T? {Identifier.Get}<T>(this {Identifier.IConfiguration} {Identifier.configuration}, {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}) => " +
                        $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureOptions}) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_Get_TypeOf))
                {
                    EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen.ConfigBinder_Get_TypeOf, documentation);
                    _writer.WriteLine($"public static object? {Identifier.Get}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}) => " +
                        $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureOptions}: null);");
                }

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_Get_TypeOf_BinderOptions))
                {
                    EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen.ConfigBinder_Get_TypeOf_BinderOptions, documentation);
                    _writer.WriteLine($"public static object? {Identifier.Get}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}) => " +
                        $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureOptions});");
                }
            }

            private void EmitGetValueMethods()
            {
                const string expressionForGetValueCore = $"{Identifier.BindingExtensions}.{nameof(MethodsToGen_CoreBindingHelper.GetValueCore)}";
                const string documentation = "Extracts the value with the specified key and converts it to the specified type.";

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_GetValue_T_key))
                {
                    EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen.ConfigBinder_GetValue_T_key, documentation);
                    _writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {Identifier.IConfiguration} {Identifier.configuration}, string {Identifier.key}) => " +
                        $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_GetValue_T_key_defaultValue))
                {
                    EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen.ConfigBinder_GetValue_T_key_defaultValue, documentation);
                    _writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {Identifier.IConfiguration} {Identifier.configuration}, string {Identifier.key}, T {Identifier.defaultValue}) => " +
                        $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? {Identifier.defaultValue});");
                }

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_GetValue_TypeOf_key))
                {
                    EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen.ConfigBinder_GetValue_TypeOf_key, documentation);
                    _writer.WriteLine($"public static object? {Identifier.GetValue}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key}) => " +
                        $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key});");
                }

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_GetValue_TypeOf_key_defaultValue))
                {
                    EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen.ConfigBinder_GetValue_TypeOf_key_defaultValue, documentation);
                    _writer.WriteLine($"public static object? {Identifier.GetValue}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key}, object? {Identifier.defaultValue}) => " +
                        $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key}) ?? {Identifier.defaultValue};");
                }
            }

            private void EmitBindMethods_ConfigurationBinder()
            {
                if (!ShouldEmitMethods(MethodsToGen.ConfigBinder_Bind))
                {
                    return;
                }

                string instanceParamExpr = $"object? {Identifier.instance}";

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_Bind_instance))
                {
                    EmitMethods(
                        _interceptorInfo.ConfigBinder_Bind_instance,
                        additionalParams: instanceParamExpr,
                        configExpression: Identifier.configuration,
                        configureOptions: false);
                }

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_Bind_instance_BinderOptions))
                {
                    EmitMethods(
                        _interceptorInfo.ConfigBinder_Bind_instance_BinderOptions,
                        additionalParams: $"{instanceParamExpr}, {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}",
                        configExpression: Identifier.configuration,
                        configureOptions: true);
                }

                if (ShouldEmitMethods(MethodsToGen.ConfigBinder_Bind_key_instance))
                {
                    EmitMethods(
                        _interceptorInfo.ConfigBinder_Bind_key_instance,
                        additionalParams: $"string {Identifier.key}, {instanceParamExpr}",
                        configExpression: $"{Expression.configurationGetSection}({Identifier.key})",
                        configureOptions: false);
                }

                void EmitMethods(ImmutableEquatableArray<TypedInterceptorInvocationInfo>? interceptorInfo, string additionalParams, string configExpression, bool configureOptions)
                {
                    Debug.Assert(interceptorInfo is not null);

                    foreach ((ComplexTypeSpec type, ImmutableEquatableArray<InvocationLocationInfo> locations) in interceptorInfo)
                    {
                        EmitBlankLineIfRequired();
                        _writer.WriteLine($"/// <summary>Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.</summary>");
                        EmitInterceptsLocationAnnotations(locations);
                        EmitStartBlock($"public static void {Identifier.Bind}_{type.IdentifierCompatibleSubstring}(this {Identifier.IConfiguration} {Identifier.configuration}, {additionalParams})");

                        if (_typeIndex.HasBindableMembers(type))
                        {
                            Debug.Assert(!type.IsValueType);
                            string binderOptionsArg = configureOptions ? $"{Identifier.GetBinderOptions}({Identifier.configureOptions})" : $"{Identifier.binderOptions}: null";

                            EmitCheckForNullArgument_WithBlankLine(Identifier.configuration, _emitThrowIfNullMethod);
                            EmitCheckForNullArgument_WithBlankLine(Identifier.instance, _emitThrowIfNullMethod, voidReturn: true);
                            _writer.WriteLine($$"""
                                var {{Identifier.typedObj}} = ({{type.TypeRef.FullyQualifiedName}}){{Identifier.instance}};
                                {{nameof(MethodsToGen_CoreBindingHelper.BindCore)}}({{configExpression}}, ref {{Identifier.typedObj}}, defaultValueIfNotFound: false, {{binderOptionsArg}});
                                """);
                        }

                        EmitEndBlock();
                    }
                }
            }

            private void EmitStartDefinition_Get_Or_GetValue_Overload(MethodsToGen overload, string documentation)
            {
                EmitBlankLineIfRequired();
                _writer.WriteLine($"/// <summary>{documentation}</summary>");
                EmitInterceptsLocationAnnotations(overload);
            }
        }
    }
}
