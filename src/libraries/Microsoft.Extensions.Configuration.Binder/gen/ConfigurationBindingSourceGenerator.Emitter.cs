// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingSourceGenerator
    {
        private sealed partial class Emitter
        {
            private static class Expression
            {
                public const string sectionKey = "section.Key";
                public const string sectionPath = "section.Path";
                public const string sectionValue = "section.Value";

                public const string GetBinderOptions = $"{FullyQualifiedDisplayName.Helpers}.{Identifier.GetBinderOptions}";
            }

            private static class FullyQualifiedDisplayName
            {
                public const string ActionOfBinderOptions = $"global::System.Action<global::Microsoft.Extensions.Configuration.BinderOptions>";
                public const string Helpers = $"global::{GeneratorProjectName}.{Identifier.Helpers}";
                public const string IConfiguration = "global::Microsoft.Extensions.Configuration.IConfiguration";
                public const string IConfigurationSection = IConfiguration + "Section";
                public const string InvalidOperationException = "global::System.InvalidOperationException";
                public const string IServiceCollection = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
                public const string NotSupportedException = "global::System.NotSupportedException";
                public const string Type = $"global::System.Type";
            }

            public static class Identifier
            {
                public const string binderOptions = nameof(binderOptions);
                public const string configureActions = nameof(configureActions);
                public const string configuration = nameof(configuration);
                public const string defaultValue = nameof(defaultValue);
                public const string element = nameof(element);
                public const string enumValue = nameof(enumValue);
                public const string exception = nameof(exception);
                public const string getPath = nameof(getPath);
                public const string key = nameof(key);
                public const string obj = nameof(obj);
                public const string originalCount = nameof(originalCount);
                public const string path = nameof(path);
                public const string section = nameof(section);
                public const string services = nameof(services);
                public const string stringValue = nameof(stringValue);
                public const string temp = nameof(temp);
                public const string type = nameof(type);

                public const string Add = nameof(Add);
                public const string Any = nameof(Any);
                public const string Array = nameof(Array);
                public const string Bind = nameof(Bind);
                public const string BindCore = nameof(BindCore);
                public const string BinderOptions = nameof(BinderOptions);
                public const string Configure = nameof(Configure);
                public const string CopyTo = nameof(CopyTo);
                public const string ContainsKey = nameof(ContainsKey);
                public const string Count = nameof(Count);
                public const string CultureInfo = nameof(CultureInfo);
                public const string CultureNotFoundException = nameof(CultureNotFoundException);
                public const string Enum = nameof(Enum);
                public const string ErrorOnUnknownConfiguration = nameof(ErrorOnUnknownConfiguration);
                public const string GeneratedConfigurationBinder = nameof(GeneratedConfigurationBinder);
                public const string Get = nameof(Get);
                public const string GetBinderOptions = nameof(GetBinderOptions);
                public const string GetCore = nameof(GetCore);
                public const string GetChildren = nameof(GetChildren);
                public const string GetSection = nameof(GetSection);
                public const string GetValue = nameof(GetValue);
                public const string GetValueCore = nameof(GetValueCore);
                public const string HasChildren = nameof(HasChildren);
                public const string HasValueOrChildren = nameof(HasValueOrChildren);
                public const string HasValue = nameof(HasValue);
                public const string Helpers = nameof(Helpers);
                public const string IConfiguration = nameof(IConfiguration);
                public const string IConfigurationSection = nameof(IConfigurationSection);
                public const string Int32 = "int";
                public const string InvalidOperationException = nameof(InvalidOperationException);
                public const string InvariantCulture = nameof(InvariantCulture);
                public const string Length = nameof(Length);
                public const string Parse = nameof(Parse);
                public const string Path = nameof(Path);
                public const string Resize = nameof(Resize);
                public const string TryCreate = nameof(TryCreate);
                public const string TryGetValue = nameof(TryGetValue);
                public const string TryParse = nameof(TryParse);
                public const string Uri = nameof(Uri);
                public const string Value = nameof(Value);
            }

            private enum InitializationKind
            {
                None = 0,
                SimpleAssignment = 1,
                AssignmentWithNullCheck = 2,
                Declaration = 3,
            }

            private readonly SourceProductionContext _context;
            private readonly SourceGenerationSpec _generationSpec;

            // Postfix for stringValueX variables used to save config value indexer
            // results e.g. if (configuration["Key"] is string stringValue0) { ... }
            private int _parseValueCount;

            private bool _precedingBlockExists;

            private readonly SourceWriter _writer = new();

            private readonly Regex _arrayBracketsRegex = new(Regex.Escape("[]"));

            private bool _useFullyQualifiedNames = true;

            public Emitter(SourceProductionContext context, SourceGenerationSpec generationSpec)
            {
                _context = context;
                _generationSpec = generationSpec;
            }

            public void Emit()
            {
                if (!_generationSpec.HasRootMethods())
                {
                    return;
                }

                _writer.WriteLine(@"// <auto-generated/>
#nullable enable
");

                #region Generated binder for user consumption.
                _writer.WriteBlockStart($"internal static class {Identifier.GeneratedConfigurationBinder}");
                EmitConfigureMethod();

                // ConfigurationBinder ext. methods.
                EmitGetMethods();
                EmitGetValueMethods();
                EmitBindMethods();

                _writer.WriteBlockEnd();

                _writer.WriteBlankLine();
                #endregion

                #region Helper class in source-generation namespace.
                _useFullyQualifiedNames = false;
                _precedingBlockExists = false;

                _writer.WriteBlockStart($"namespace {GeneratorProjectName}");
                EmitHelperUsingStatements();

                _writer.WriteBlankLine();

                _writer.WriteBlockStart($"internal static class {Identifier.Helpers}");
                EmitGetCoreMethod();
                EmitGetValueCoreMethod();
                EmitBindCoreMethods();
                EmitHelperMethods();

                _writer.WriteBlockEnd(); // End helper class.
                _writer.WriteBlockEnd(); // End containing namespace.
                #endregion

                _context.AddSource($"{Identifier.GeneratedConfigurationBinder}.g.cs", _writer.ToSourceText());
            }

            #region Generated binder for user consumption.
            private void EmitConfigureMethod()
            {
                if (!_generationSpec.ShouldEmitMethods(MethodSpecifier.Configure))
                {
                    return;
                }

                _writer.WriteBlockStart($"public static {FullyQualifiedDisplayName.IServiceCollection} {Identifier.Configure}<T>(this {FullyQualifiedDisplayName.IServiceCollection} {Identifier.services}, {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration, useFullyQualifiedNames: true);

                foreach (TypeSpec type in _generationSpec.RootConfigTypes[MethodSpecifier.Configure])
                {
                    string typeDisplayString = type.FullyQualifiedDisplayString;

                    _writer.WriteBlockStart($"if (typeof(T) == typeof({typeDisplayString}))");

                    _writer.WriteBlockStart($@"return {Identifier.services}.{Identifier.Configure}<{typeDisplayString}>({Identifier.obj} =>");
                    EmitIConfigurationHasValueOrChildrenCheck(voidReturn: true);
                    EmitBindLogicFromRootMethod(type, Identifier.obj, InitializationKind.None);
                    _writer.WriteBlockEnd(");");

                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                Emit_NotSupportedException_TypeNotDetectedAsInput();
                _writer.WriteBlockEnd();
                _precedingBlockExists = true;
            }

            private void EmitGetMethods()
            {
                const string expressionForGetCore = $"{FullyQualifiedDisplayName.Helpers}.{Identifier.GetCore}";

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.Get_T))
                {
                    EmitBlankLineIfRequired();
                    _writer.WriteLine($"public static T? {Identifier.Get}<T>(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}) => " +
                        $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureActions}: null) ?? default(T));");
                }

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.Get_T_BinderOptions))
                {
                    EmitBlankLineIfRequired();
                    _writer.WriteLine($"public static T? {Identifier.Get}<T>(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayName.ActionOfBinderOptions}? {Identifier.configureActions}) => " +
                        $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureActions}) ?? default(T));");
                }

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.Get_TypeOf))
                {
                    EmitBlankLineIfRequired();
                    _writer.WriteLine($"public static object? {Identifier.Get}(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayName.Type} {Identifier.type}) => " +
                        $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureActions}: null);");
                }

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.Get_TypeOf_BinderOptions))
                {
                    EmitBlankLineIfRequired();
                    _writer.WriteLine($"public static object? {Identifier.Get}(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayName.Type} {Identifier.type}, {FullyQualifiedDisplayName.ActionOfBinderOptions}? {Identifier.configureActions}) => " +
                        $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureActions});");
                }
            }

            private void EmitGetValueMethods()
            {
                const string expressionForGetValueCore = $"{FullyQualifiedDisplayName.Helpers}.{Identifier.GetValueCore}";

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.GetValue_T_key))
                {
                    EmitBlankLineIfRequired();
                    _writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, string {Identifier.key}) => " +
                        $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? default(T));");
                }

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.GetValue_T_key_defaultValue))
                {
                    EmitBlankLineIfRequired();
                    _writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, string {Identifier.key}, T {Identifier.defaultValue}) => " +
                        $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? {Identifier.defaultValue});");
                }

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.GetValue_TypeOf_key))
                {
                    EmitBlankLineIfRequired();
                    _writer.WriteLine($"public static object? {Identifier.GetValue}(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayName.Type} {Identifier.type}, string {Identifier.key}) => " +
                        $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key});");
                }

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.GetValue_TypeOf_key_defaultValue))
                {
                    EmitBlankLineIfRequired();
                    _writer.WriteLine($"public static object? {Identifier.GetValue}(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayName.Type} {Identifier.type}, string {Identifier.key}, object? {Identifier.defaultValue}) =>" +
                        $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key}) ?? {Identifier.defaultValue};");
                }
            }

            private void EmitBindMethods()
            {
                if (!_generationSpec.ShouldEmitMethods(MethodSpecifier.BindMethods))
                {
                    return;
                }

                Dictionary<MethodSpecifier, HashSet<TypeSpec>> rootConfigTypes = _generationSpec.RootConfigTypes;

                if (rootConfigTypes.TryGetValue(MethodSpecifier.Bind_instance, out HashSet<TypeSpec>? typeSpecs))
                {
                    foreach (TypeSpec type in typeSpecs)
                    {
                        EmitBlankLineIfRequired();
                        _writer.WriteLine(
                            $"public static void {Identifier.Bind}(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, {type.FullyQualifiedDisplayString} {Identifier.obj}) => " +
                                $"{FullyQualifiedDisplayName.Helpers}.{Identifier.BindCore}({Identifier.configuration}, ref {Identifier.obj}, {Identifier.binderOptions}: null);");
                    }
                }

                if (rootConfigTypes.TryGetValue(MethodSpecifier.Bind_instance_BinderOptions, out typeSpecs))
                {
                    foreach (TypeSpec type in typeSpecs)
                    {
                        EmitBlankLineIfRequired();
                        _writer.WriteLine(
                            $"public static void {Identifier.Bind}(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, {type.FullyQualifiedDisplayString} {Identifier.obj}, {FullyQualifiedDisplayName.ActionOfBinderOptions}? {Identifier.configureActions}) => " +
                                $"{FullyQualifiedDisplayName.Helpers}.{Identifier.BindCore}({Identifier.configuration}, ref {Identifier.obj}, {Expression.GetBinderOptions}({Identifier.configureActions}));");
                    }
                }

                if (rootConfigTypes.TryGetValue(MethodSpecifier.Bind_key_instance, out typeSpecs))
                {
                    foreach (TypeSpec type in typeSpecs)
                    {
                        EmitBlankLineIfRequired();
                        _writer.WriteLine($"public static void {Identifier.Bind}(this {FullyQualifiedDisplayName.IConfiguration} {Identifier.configuration}, string {Identifier.key}, {type.FullyQualifiedDisplayString} {Identifier.obj}) => " +
                            $"{FullyQualifiedDisplayName.Helpers}.{Identifier.BindCore}({Identifier.configuration}.{Identifier.GetSection}({Identifier.key}), ref {Identifier.obj}, {Identifier.binderOptions}: null);");
                    }
                }
            }
            #endregion

            #region Helper class in source-generation namespace.
            private void EmitHelperUsingStatements()
            {
                foreach (string @namespace in _generationSpec.Namespaces)
                {
                    _writer.WriteLine($"using {@namespace};");
                }
            }

            private void EmitGetCoreMethod()
            {
                if (!_generationSpec.ShouldEmitMethods(MethodSpecifier.GetMethods))
                {
                    return;
                }

                _writer.WriteBlockStart($"public static object? {Identifier.GetCore}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, Action<{Identifier.BinderOptions}>? {Identifier.configureActions})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureActions});");
                _writer.WriteBlankLine();

                EmitIConfigurationHasValueOrChildrenCheck(voidReturn: false);

                if (_generationSpec.RootConfigTypes.TryGetValue(MethodSpecifier.GetMethods, out HashSet<TypeSpec>? types))
                {
                    foreach (TypeSpec type in types)
                    {
                        _writer.WriteBlockStart($"if (type == typeof({type.MinimalDisplayString}))");
                        EmitBindLogicFromRootMethod(type, Identifier.obj, InitializationKind.Declaration);
                        _writer.WriteLine($"return {Identifier.obj};");
                        _writer.WriteBlockEnd();
                        _writer.WriteBlankLine();
                    }
                }

                Emit_NotSupportedException_TypeNotDetectedAsInput();
                _writer.WriteBlockEnd();
                _precedingBlockExists = true;
            }

            private void EmitGetValueCoreMethod()
            {
                if (!_generationSpec.ShouldEmitMethods(MethodSpecifier.GetValueMethods))
                {
                    return;
                }

                EmitBlankLineIfRequired();

                _writer.WriteBlockStart($"public static object? {Identifier.GetValueCore}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteLine($"{Identifier.IConfigurationSection} {Identifier.section} = {Identifier.configuration}.{Identifier.GetSection}({Identifier.key});");
                _writer.WriteLine($"object? {Identifier.obj};");

                _writer.WriteBlankLine();

                foreach (TypeSpec type in _generationSpec.RootConfigTypes[MethodSpecifier.GetValueMethods])
                {
                    TypeSpec effectiveType = (type as NullableSpec)?.UnderlyingType ?? type;
                    _writer.WriteBlockStart($"if (type == typeof({type.MinimalDisplayString}))");
                    EmitBindLogicFromString(
                        (ParsableFromStringTypeSpec)effectiveType,
                        Identifier.obj,
                        Expression.sectionValue,
                        Expression.sectionPath,
                        writeOnSuccess: () => _writer.WriteLine($"return {Identifier.obj};"));
                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                _writer.WriteLine("return null;");
                _writer.WriteBlockEnd();
                _precedingBlockExists = true;
            }

            private void EmitBindCoreMethods()
            {
                if (!_generationSpec.ShouldEmitMethods(MethodSpecifier.BindCore))
                {
                    return;
                }

                foreach (TypeSpec type in _generationSpec.RootConfigTypes[MethodSpecifier.BindCore])
                {
                    if (type.SpecKind is TypeSpecKind.ParsableFromString)
                    {
                        continue;
                    }

                    EmitBlankLineIfRequired();
                    EmitBindCoreMethod(type);
                }
            }

            private void EmitBindCoreMethod(TypeSpec type)
            {
                string objParameterExpression = $"ref {type.MinimalDisplayString} {Identifier.obj}";
                _writer.WriteBlockStart(@$"public static void {Identifier.BindCore}({Identifier.IConfiguration} {Identifier.configuration}, {objParameterExpression}, {Identifier.BinderOptions}? {Identifier.binderOptions})");
                EmitBindCoreImpl(type);
                _writer.WriteBlockEnd();
            }

            private void EmitHelperMethods()
            {
                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.GetMethods | MethodSpecifier.Configure))
                {
                    _writer.WriteBlankLine();
                    EmitHasValueOrChildrenMethod();
                    _writer.WriteBlankLine();
                    EmitHasChildrenMethod();
                    _precedingBlockExists = true;
                }
                else if (_generationSpec.ShouldEmitMethods(MethodSpecifier.HasChildren))
                {
                    _writer.WriteBlankLine();
                    EmitHasChildrenMethod();
                    _precedingBlockExists = true;
                }

                if (_generationSpec.ShouldEmitMethods(MethodSpecifier.RootMethodsWithConfigOptions))
                {
                    _writer.WriteBlankLine();
                    EmitGetBinderOptionsHelper();
                    _precedingBlockExists = true;
                }

                if (_generationSpec.PrimitivesForHelperGen.Count > 0)
                {
                    foreach (ParsableFromStringTypeSpec type in _generationSpec.PrimitivesForHelperGen)
                    {
                        EmitBlankLineIfRequired();
                        EmitPrimitiveParseMethod(type);
                    }
                }
            }

            private void EmitHasValueOrChildrenMethod()
            {
                _writer.WriteBlock($$"""
                    public static bool {{Identifier.HasValueOrChildren}}({{Identifier.IConfiguration}} {{Identifier.configuration}})
                    {
                        if (({{Identifier.configuration}} as {{Identifier.IConfigurationSection}})?.{{Identifier.Value}} is not null)
                        {
                            return true;
                        }
                        return {{Identifier.HasChildren}}({{Identifier.configuration}});
                    }
                    """);
            }

            private void EmitHasChildrenMethod()
            {
                _writer.WriteBlock($$"""
                    public static bool {{Identifier.HasChildren}}({{Identifier.IConfiguration}} {{Identifier.configuration}})
                    {
                        foreach ({{Identifier.IConfigurationSection}} {{Identifier.section}} in {{Identifier.configuration}}.{{Identifier.GetChildren}}())
                        {
                            return true;
                        }
                        return false;
                    }
                    """);
            }

            private void EmitGetBinderOptionsHelper()
            {
                _writer.WriteBlock($$"""
                    public static {{Identifier.BinderOptions}}? {{Identifier.GetBinderOptions}}(System.Action<BinderOptions>? {{Identifier.configureActions}})
                    {
                        if ({{Identifier.configureActions}} is null)
                        {
                            return null;
                        }

                        {{Identifier.BinderOptions}} {{Identifier.binderOptions}} = new();
                        {{Identifier.configureActions}}({{Identifier.binderOptions}});

                        if ({{Identifier.binderOptions}}.BindNonPublicProperties)
                        {
                            throw new global::System.NotSupportedException($"{{string.Format(ExceptionMessages.CannotSpecifyBindNonPublicProperties)}}");
                        }

                        return {{Identifier.binderOptions}};
                    }
                    """);
            }

            private void EmitPrimitiveParseMethod(ParsableFromStringTypeSpec type)
            {
                string innerExceptionTypeDisplayString;
                string cultureInfoTypeDisplayString;
                string numberStylesTypeDisplayString;

                if (_useFullyQualifiedNames)
                {
                    innerExceptionTypeDisplayString = "global::System.Exception";
                    cultureInfoTypeDisplayString = "global::System.Globalization.CultureInfo";
                    numberStylesTypeDisplayString = "global::System.Globalization.NumberStyles";
                }
                else
                {
                    innerExceptionTypeDisplayString = "Exception";
                    cultureInfoTypeDisplayString = "CultureInfo";
                    numberStylesTypeDisplayString = "NumberStyles";
                }

                string invariantCultureExpression = $"{cultureInfoTypeDisplayString}.InvariantCulture";

                string expressionForParsedValue;
                StringParsableTypeKind typeKind = type.StringParsableTypeKind;
                string typeDisplayString = type.MinimalDisplayString;

                switch (typeKind)
                {
                    case StringParsableTypeKind.Enum:
                        {
                            expressionForParsedValue = $"({typeDisplayString}){Identifier.Enum}.{Identifier.Parse}(typeof({typeDisplayString}), {Identifier.stringValue}, ignoreCase: true)";
                        }
                        break;
                    case StringParsableTypeKind.ByteArray:
                        {
                            expressionForParsedValue = $"Convert.FromBase64String({Identifier.stringValue})";
                        }
                        break;
                    case StringParsableTypeKind.Integer:
                        {
                            expressionForParsedValue = $"{typeDisplayString}.{Identifier.Parse}({Identifier.stringValue}, {numberStylesTypeDisplayString}.Integer, {invariantCultureExpression})";
                        }
                        break;
                    case StringParsableTypeKind.Float:
                        {
                            expressionForParsedValue = $"{typeDisplayString}.{Identifier.Parse}({Identifier.stringValue}, {numberStylesTypeDisplayString}.Float, {invariantCultureExpression})";
                        }
                        break;
                    case StringParsableTypeKind.Parse:
                        {
                            expressionForParsedValue = $"{typeDisplayString}.{Identifier.Parse}({Identifier.stringValue})";
                        }
                        break;
                    case StringParsableTypeKind.ParseInvariant:
                        {
                            expressionForParsedValue = $"{typeDisplayString}.{Identifier.Parse}({Identifier.stringValue}, {invariantCultureExpression})"; ;
                        }
                        break;
                    case StringParsableTypeKind.CultureInfo:
                        {
                            expressionForParsedValue = $"{cultureInfoTypeDisplayString}.GetCultureInfo({Identifier.stringValue})";
                        }
                        break;
                    case StringParsableTypeKind.Uri:
                        {
                            expressionForParsedValue = $"new Uri({Identifier.stringValue}, UriKind.RelativeOrAbsolute)";
                        }
                        break;
                    default:
                        {
                            Debug.Fail($"Invalid string parsable kind: {typeKind}");
                            return;
                        }
                }

                string exceptionTypeDisplayString = _useFullyQualifiedNames ? FullyQualifiedDisplayName.InvalidOperationException : Identifier.InvalidOperationException;

                _writer.WriteBlock($$"""
                    public static {{typeDisplayString}} {{type.ParseMethodName}}(string {{Identifier.stringValue}}, Func<string?> {{Identifier.getPath}})
                    {
                        try
                        {
                            return {{expressionForParsedValue}};
                    """);

                string exceptionArg1 = string.Format(ExceptionMessages.FailedBinding, $"{{{Identifier.getPath}()}}", $"{{typeof({typeDisplayString})}}");

                _writer.WriteBlock($$"""
                        }
                        catch ({{innerExceptionTypeDisplayString}} {{Identifier.exception}})
                        {
                            throw new {{exceptionTypeDisplayString}}($"{{exceptionArg1}}", {{Identifier.exception}});
                        }
                    }
                    """);
            }
            #endregion

            #region Core binding logic.
            private void EmitBindCoreImpl(TypeSpec type)
            {
                switch (type.SpecKind)
                {
                    case TypeSpecKind.Array:
                        {
                            EmitBindCoreImplForArray((ArraySpec)type);
                        }
                        break;
                    case TypeSpecKind.Enumerable:
                        {
                            EmitBindCoreImplForEnumerable((EnumerableSpec)type);
                        }
                        break;
                    case TypeSpecKind.Dictionary:
                        {
                            EmitBindCoreImplForDictionary((DictionarySpec)type);
                        }
                        break;
                    case TypeSpecKind.IConfigurationSection:
                        {
                            EmitCastToIConfigurationSection();
                            EmitAssignment(Identifier.obj, Identifier.section);
                        }
                        break;
                    case TypeSpecKind.Object:
                        {
                            EmitBindCoreImplForObject((ObjectSpec)type);
                        }
                        break;
                    case TypeSpecKind.Nullable:
                        {
                            EmitBindCoreImpl(((NullableSpec)type).UnderlyingType);
                        }
                        break;
                    default:
                        Debug.Fail("Invalid type kind", type.SpecKind.ToString());
                        break;
                }
            }

            private void EmitBindCoreImplForArray(ArraySpec type)
            {
                EnumerableSpec concreteType = (EnumerableSpec)type.ConcreteType;

                EmitCheckForNullArgument_WithBlankLine_IfRequired(isValueType: false);

                // Create, bind, and add elements to temp list.
                string tempVarName = GetIncrementalVarName(Identifier.temp);
                EmitBindCoreCall(concreteType, tempVarName, Identifier.configuration, InitializationKind.Declaration);

                // Resize array and copy additional elements.
                _writer.WriteBlock($$"""
                    {{Identifier.Int32}} {{Identifier.originalCount}} = {{Identifier.obj}}.{{Identifier.Length}};
                    {{Identifier.Array}}.{{Identifier.Resize}}(ref {{Identifier.obj}}, {{Identifier.originalCount}} + {{tempVarName}}.{{Identifier.Count}});
                    {{tempVarName}}.{{Identifier.CopyTo}}({{Identifier.obj}}, {{Identifier.originalCount}});
                    """);
            }

            private void EmitBindCoreImplForEnumerable(EnumerableSpec type)
            {
                EmitCheckForNullArgument_WithBlankLine_IfRequired(type.IsValueType);

                TypeSpec elementType = type.ElementType;

                _writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");

                string addStatement = $"{Identifier.obj}.{Identifier.Add}({Identifier.element})";

                if (elementType.SpecKind is TypeSpecKind.ParsableFromString)
                {
                    ParsableFromStringTypeSpec stringParsableType = (ParsableFromStringTypeSpec)elementType;
                    if (stringParsableType.StringParsableTypeKind is StringParsableTypeKind.ConfigValue)
                    {
                        string tempVarName = GetIncrementalVarName(Identifier.stringValue);
                        _writer.WriteBlockStart($"if ({Expression.sectionValue} is string {tempVarName})");
                        _writer.WriteLine($"{Identifier.obj}.{Identifier.Add}({tempVarName});");
                        _writer.WriteBlockEnd();
                    }
                    else
                    {
                        EmitVarDeclaration(elementType, Identifier.element);
                        EmitBindLogicFromString(stringParsableType, Identifier.element, Expression.sectionValue, Expression.sectionPath, () => _writer.WriteLine($"{addStatement};"));
                    }
                }
                else
                {
                    EmitBindCoreCall(elementType, Identifier.element, Identifier.section, InitializationKind.Declaration);
                    _writer.WriteLine($"{addStatement};");
                }

                _writer.WriteBlockEnd();
            }

            private void EmitBindCoreImplForDictionary(DictionarySpec type)
            {
                EmitCheckForNullArgument_WithBlankLine_IfRequired(type.IsValueType);

                _writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");

                // Parse key
                ParsableFromStringTypeSpec keyType = type.KeyType;

                if (keyType.StringParsableTypeKind is StringParsableTypeKind.ConfigValue)
                {
                    _writer.WriteLine($"{keyType.MinimalDisplayString} {Identifier.key} = {Expression.sectionKey};");
                    Emit_BindAndAddLogic_ForElement();
                }
                else
                {
                    EmitVarDeclaration(keyType, Identifier.key);
                    EmitBindLogicFromString(
                        keyType,
                        Identifier.key,
                        expressionForConfigStringValue: Expression.sectionKey,
                        expressionForConfigValuePath: Expression.sectionValue,
                        writeOnSuccess: Emit_BindAndAddLogic_ForElement);
                }

                void Emit_BindAndAddLogic_ForElement()
                {
                    TypeSpec elementType = type.ElementType;

                    if (elementType.SpecKind == TypeSpecKind.ParsableFromString)
                    {
                        ParsableFromStringTypeSpec stringParsableType = (ParsableFromStringTypeSpec)elementType;
                        if (stringParsableType.StringParsableTypeKind is StringParsableTypeKind.ConfigValue)
                        {
                            string tempVarName = GetIncrementalVarName(Identifier.stringValue);
                            _writer.WriteBlockStart($"if ({Expression.sectionValue} is string {tempVarName})");
                            _writer.WriteLine($"{Identifier.obj}[{Identifier.key}] = {tempVarName};");
                            _writer.WriteBlockEnd();
                        }
                        else
                        {
                            EmitVarDeclaration(elementType, Identifier.element);
                            EmitBindLogicFromString(
                                stringParsableType,
                                Identifier.element,
                                Expression.sectionValue,
                                Expression.sectionPath,
                                () => _writer.WriteLine($"{Identifier.obj}[{Identifier.key}] = {Identifier.element};"));
                        }
                    }
                    else // For complex types:
                    {
                        string elementTypeDisplayString = elementType.MinimalDisplayString + (elementType.IsValueType ? string.Empty : "?");

                        // If key already exists, bind to value to existing element instance if not null (for ref types).
                        string conditionToUseExistingElement = $"{Identifier.obj}.{Identifier.TryGetValue}({Identifier.key}, out {elementTypeDisplayString} {Identifier.element})";
                        if (!elementType.IsValueType)
                        {
                            conditionToUseExistingElement += $" && {Identifier.element} is not null";
                        }
                        _writer.WriteBlockStart($"if (!({conditionToUseExistingElement}))");
                        EmitObjectInit(elementType, Identifier.element, InitializationKind.SimpleAssignment);
                        _writer.WriteBlockEnd();

                        EmitBindCoreCall(elementType, $"{Identifier.element}!", Identifier.section, InitializationKind.None);
                        _writer.WriteLine($"{Identifier.obj}[{Identifier.key}] = {Identifier.element};");
                    }
                }

                _writer.WriteBlockEnd();
            }

            private void EmitBindCoreImplForObject(ObjectSpec type)
            {
                Dictionary<string, PropertySpec> properties = type.Properties;
                if (properties.Count == 0)
                {
                    return;
                }

                EmitCheckForNullArgument_WithBlankLine_IfRequired(type.IsValueType);

                string listOfStringDisplayName = "List<string>";
                _writer.WriteLine($"{listOfStringDisplayName}? {Identifier.temp} = null;");

                _writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");
                _writer.WriteBlockStart($"switch ({Expression.sectionKey})");

                foreach (PropertySpec property in properties.Values)
                {
                    _writer.WriteBlockStart($@"case ""{property.ConfigurationKeyName}"":");

                    TypeSpec propertyType = property.Type;

                    EmitBindCoreImplForProperty(property, propertyType, parentType: type);
                    _writer.WriteBlockEnd();
                    _writer.WriteLine("break;");
                }

                _writer.WriteBlock($$"""
                    default:
                    {
                        if ({{Identifier.binderOptions}}?.ErrorOnUnknownConfiguration == true)
                        {
                            ({{Identifier.temp}} ??= new {{listOfStringDisplayName}}()).Add($"'{{{Expression.sectionKey}}}'");
                        }
                    }
                    break;
                    """);

                // End switch on config child key.
                _writer.WriteBlockEnd();

                // End foreach on config.GetChildren().
                _writer.WriteBlockEnd();

                _writer.WriteBlankLine();

                string exceptionMessage = string.Format(ExceptionMessages.MissingConfig, Identifier.ErrorOnUnknownConfiguration, Identifier.BinderOptions, $"{{typeof({type.MinimalDisplayString})}}", $@"{{string.Join("", "", {Identifier.temp})}}");
                _writer.WriteBlock($$"""
                    if ({{Identifier.temp}} is not null)
                    {
                        throw new InvalidOperationException($"{{exceptionMessage}}");
                    }
                    """);

            }

            private void EmitBindCoreImplForProperty(PropertySpec property, TypeSpec propertyType, TypeSpec parentType)
            {
                string configurationKeyName = property.ConfigurationKeyName;

                string propertyParentReference = property.IsStatic ? parentType.MinimalDisplayString : Identifier.obj;
                string expressionForPropertyAccess = $"{propertyParentReference}.{property.Name}";

                string expressionForConfigValueIndexer = $@"{Identifier.configuration}[""{configurationKeyName}""]";

                bool canSet = property.CanSet;

                switch (propertyType.SpecKind)
                {
                    case TypeSpecKind.ParsableFromString:
                        {
                            if (canSet)
                            {
                                EmitBindLogicFromString(
                                    (ParsableFromStringTypeSpec)propertyType,
                                    expressionForPropertyAccess,
                                    expressionForConfigValueIndexer,
                                    expressionForConfigValuePath: Expression.sectionPath);
                            }
                        }
                        break;
                    case TypeSpecKind.Array:
                        {
                            EmitBindCoreCallForProperty(
                                property,
                                propertyType,
                                expressionForPropertyAccess);
                        }
                        break;
                    case TypeSpecKind.IConfigurationSection:
                        {
                            EmitAssignment(expressionForPropertyAccess, Identifier.section);
                        }
                        break;
                    case TypeSpecKind.Nullable:
                        {
                            TypeSpec underlyingType = ((NullableSpec)propertyType).UnderlyingType;
                            EmitBindCoreImplForProperty(property, underlyingType, parentType);
                        }
                        break;
                    default:
                        {
                            EmitBindCoreCallForProperty(
                                property,
                                propertyType,
                                expressionForPropertyAccess);
                        }
                        break;
                }
            }

            private void EmitBindLogicFromRootMethod(TypeSpec type, string expressionForMemberAccess, InitializationKind initKind)
            {
                TypeSpecKind kind = type.SpecKind;

                if (kind is TypeSpecKind.Nullable)
                {
                    EmitBindLogicFromRootMethod(((NullableSpec)type).UnderlyingType, expressionForMemberAccess, initKind);
                }
                else if (kind is TypeSpecKind.ParsableFromString)
                {
                    if (initKind is InitializationKind.Declaration)
                    {
                        EmitCastToIConfigurationSection();
                        _writer.WriteLine($"{GetTypeDisplayString(type)} {expressionForMemberAccess} = default!;");
                    }
                    else
                    {
                        EmitCastToIConfigurationSection();
                    }
                    EmitBindLogicFromString((ParsableFromStringTypeSpec)type, expressionForMemberAccess, Expression.sectionValue, Expression.sectionPath);
                }
                else
                {
                    EmitBindCoreCall(type, expressionForMemberAccess, Identifier.configuration, initKind);
                }
            }

            private void EmitBindCoreCall(
                TypeSpec type,
                string expressionForMemberAccess,
                string expressionForConfigArg,
                InitializationKind initKind)
            {
                string tempVarName = GetIncrementalVarName(Identifier.temp);
                if (initKind is InitializationKind.AssignmentWithNullCheck)
                {
                    EmitAssignment($"{type.MinimalDisplayString} {tempVarName}", $"{expressionForMemberAccess}");
                    EmitObjectInit(type, tempVarName, InitializationKind.AssignmentWithNullCheck);
                    EmitBindCoreCall(tempVarName);
                }
                else if (initKind is InitializationKind.None && type.IsValueType)
                {
                    EmitObjectInit(type, tempVarName, InitializationKind.Declaration);
                    _writer.WriteLine($@"{Identifier.BindCore}({expressionForConfigArg}, ref {tempVarName}, {Identifier.binderOptions});");
                    EmitAssignment(expressionForMemberAccess, tempVarName);
                }
                else
                {
                    EmitObjectInit(type, expressionForMemberAccess, initKind);
                    EmitBindCoreCall(expressionForMemberAccess);
                }

                void EmitBindCoreCall(string varName)
                {
                    string bindCoreCall = $@"{GetHelperMethodDisplayString(Identifier.BindCore)}({expressionForConfigArg}, ref {varName}, {Identifier.binderOptions});";
                    _writer.WriteLine(bindCoreCall);
                }
            }

            private void EmitBindCoreCallForProperty(
                PropertySpec property,
                TypeSpec effectivePropertyType,
                string expressionForPropertyAccess)
            {
                _writer.WriteBlockStart($"if ({Identifier.HasChildren}({Identifier.section}))");

                bool canGet = property.CanGet;
                bool canSet = property.CanSet;
                string effectivePropertyTypeDisplayString = effectivePropertyType.MinimalDisplayString;

                string tempVarName = GetIncrementalVarName(Identifier.temp);
                if (effectivePropertyType.IsValueType)
                {
                    if (canSet)
                    {
                        if (canGet)
                        {
                            TypeSpec actualPropertyType = property.Type;
                            if (actualPropertyType.SpecKind is TypeSpecKind.Nullable)
                            {
                                string nullableTempVarName = GetIncrementalVarName(Identifier.temp);
                                EmitAssignment(
                                    $"{actualPropertyType.MinimalDisplayString} {nullableTempVarName}", expressionForPropertyAccess);
                                EmitAssignment(
                                    $"{effectivePropertyTypeDisplayString} {tempVarName}",
                                    $"{nullableTempVarName}.{Identifier.HasValue} ? {nullableTempVarName}.{Identifier.Value} : new {effectivePropertyTypeDisplayString}()");
                            }
                            else
                            {
                                EmitAssignment($"{effectivePropertyTypeDisplayString} {tempVarName}", $"{expressionForPropertyAccess}");
                            }
                        }
                        else
                        {
                            EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.Declaration);
                        }

                        _writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");
                        EmitAssignment(expressionForPropertyAccess, tempVarName);
                    }
                }
                else if (canGet)
                {
                    EmitAssignment($"{effectivePropertyTypeDisplayString} {tempVarName}", $"{expressionForPropertyAccess}");
                    EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.AssignmentWithNullCheck);
                    _writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");

                    if (canSet)
                    {
                        EmitAssignment(expressionForPropertyAccess, tempVarName);
                    }
                }
                else
                {
                    Debug.Assert(canSet);
                    EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.Declaration);
                    _writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");
                    EmitAssignment(expressionForPropertyAccess, tempVarName);
                }

                _writer.WriteBlockEnd();
            }

            private void EmitBindLogicFromString(
                ParsableFromStringTypeSpec type,
                string expressionForMemberAccess,
                string expressionForConfigStringValue,
                string expressionForConfigValuePath,
                Action? writeOnSuccess = null)
            {
                StringParsableTypeKind typeKind = type.StringParsableTypeKind;
                Debug.Assert(typeKind is not StringParsableTypeKind.None);

                string stringValueVarName = GetIncrementalVarName(Identifier.stringValue);

                _writer.WriteBlockStart($"if ({expressionForConfigStringValue} is string {stringValueVarName})");

                string parsedValue = typeKind is StringParsableTypeKind.ConfigValue
                    ? stringValueVarName
                    : $"{GetHelperMethodDisplayString(type.ParseMethodName)}({stringValueVarName}, () => {expressionForConfigValuePath})";

                EmitAssignment(expressionForMemberAccess, parsedValue);
                writeOnSuccess?.Invoke();
                _writer.WriteBlockEnd();

                return;
            }

            private void EmitObjectInit(TypeSpec type, string expressionForMemberAccess, InitializationKind initKind)
            {
                if (initKind is InitializationKind.None)
                {
                    return;
                }

                string displayString = GetTypeDisplayString(type);

                string expressionForInit = null;
                if (type is ArraySpec)
                {
                    expressionForInit = $"new {_arrayBracketsRegex.Replace(displayString, "[0]", 1)}";
                }
                else if (type.ConstructionStrategy != ConstructionStrategy.ParameterlessConstructor)
                {
                    return;
                }
                else if (type is CollectionSpec { ConcreteType: { } concreteType })
                {
                    displayString = GetTypeDisplayString(concreteType);
                }

                // Not an array.
                expressionForInit ??= $"new {displayString}()";

                if (initKind == InitializationKind.Declaration)
                {
                    Debug.Assert(!expressionForMemberAccess.Contains("."));
                    EmitAssignment($"var {expressionForMemberAccess}", expressionForInit);
                }
                else if (initKind == InitializationKind.AssignmentWithNullCheck)
                {
                    _writer.WriteLine($"{expressionForMemberAccess} ??= {expressionForInit};");
                }
                else
                {
                    EmitAssignment(expressionForMemberAccess, expressionForInit);
                }
            }

            private void EmitCastToIConfigurationSection()
            {
                string sectionTypeDisplayString;
                string exceptionTypeDisplayString;
                if (_useFullyQualifiedNames)
                {
                    sectionTypeDisplayString = "global::Microsoft.Extensions.Configuration.IConfigurationSection";
                    exceptionTypeDisplayString = FullyQualifiedDisplayName.InvalidOperationException;
                }
                else
                {
                    sectionTypeDisplayString = Identifier.IConfigurationSection;
                    exceptionTypeDisplayString = nameof(InvalidOperationException);
                }

                _writer.WriteBlock($$"""
                    if ({{Identifier.configuration}} is not {{sectionTypeDisplayString}} {{Identifier.section}})
                    {
                        throw new {{exceptionTypeDisplayString}}();
                    }
                    """);
            }

            private void EmitIConfigurationHasValueOrChildrenCheck(bool voidReturn)
            {
                string returnPostfix = voidReturn ? string.Empty : " null";

                _writer.WriteBlock($$"""
                    if (!{{GetHelperMethodDisplayString(Identifier.HasValueOrChildren)}}({{Identifier.configuration}}))
                    {
                        return{{returnPostfix}};
                    }
                    """);
                _writer.WriteBlankLine();
            }
            #endregion

            #region Emit logic helpers.
            private void EmitBlankLineIfRequired()
            {
                if (_precedingBlockExists)
                {
                    _writer.WriteBlankLine();
                }
                {
                    _precedingBlockExists = true;
                }
            }

            private void EmitVarDeclaration(TypeSpec type, string varName) => _writer.WriteLine($"{type.MinimalDisplayString} {varName};");

            private void EmitAssignment(string lhsSource, string rhsSource) => _writer.WriteLine($"{lhsSource} = {rhsSource};");

            private void Emit_NotSupportedException_TypeNotDetectedAsInput() =>
                _writer.WriteLine(@$"throw new global::System.NotSupportedException($""{string.Format(ExceptionMessages.TypeNotDetectedAsInput, "{type}")}"");");

            private void Emit_NotSupportedExceptionTypeNotSupportedAsInput() =>
                _writer.WriteLine(@$"throw new global::System.NotSupportedException($""{string.Format(ExceptionMessages.TypeNotSupportedAsInput, "{type}")}"");");

            private void EmitCheckForNullArgument_WithBlankLine_IfRequired(bool isValueType)
            {
                if (!isValueType)
                {
                    EmitCheckForNullArgument_WithBlankLine(Identifier.obj);
                }
            }

            private void EmitCheckForNullArgument_WithBlankLine(string argName, bool useFullyQualifiedNames = false)
            {
                string exceptionTypeDisplayString = useFullyQualifiedNames
                    ? "global::System.ArgumentNullException"
                    : "ArgumentNullException";

                _writer.WriteBlock($$"""
                    if ({{argName}} is null)
                    {
                        throw new {{exceptionTypeDisplayString}}(nameof({{argName}}));
                    }
                    """);

                _writer.WriteBlankLine();
            }

            private string GetIncrementalVarName(string prefix) => $"{prefix}{_parseValueCount++}";

            private string GetTypeDisplayString(TypeSpec type) => _useFullyQualifiedNames ? type.FullyQualifiedDisplayString : type.MinimalDisplayString;

            private string GetHelperMethodDisplayString(string methodName)
            {
                if (_useFullyQualifiedNames)
                {
                    methodName = FullyQualifiedDisplayName.Helpers + "." + methodName;
                }

                return methodName;
            }
            #endregion
        }
    }
}
