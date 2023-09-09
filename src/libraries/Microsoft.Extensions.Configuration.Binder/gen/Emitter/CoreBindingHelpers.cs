// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private int _valueSuffixIndex;
            private bool _emitBlankLineBeforeNextStatement;
            private static readonly Regex s_arrayBracketsRegex = new(Regex.Escape("[]"));

            private bool ShouldEmitMethods(MethodsToGen_CoreBindingHelper methods) => (_sourceGenSpec.MethodsToGen_CoreBindingHelper & methods) != 0;

            private void EmitCoreBindingHelpers()
            {
                Debug.Assert(_emitBlankLineBeforeNextStatement);
                EmitBindingExtStartRegion("Core binding");
                EmitConfigurationKeyCaches();
                EmitGetCoreMethod();
                EmitGetValueCoreMethod();
                EmitBindCoreMainMethod();
                EmitBindCoreMethods();
                EmitInitializeMethods();
                EmitHelperMethods();
                EmitBindingExtEndRegion();
            }

            private void EmitConfigurationKeyCaches()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.BindCore, out HashSet<TypeSpec> targetTypes))
                {
                    return;
                }

                EmitBlankLineIfRequired();

                foreach (TypeSpec type in targetTypes)
                {
                    if (type is not ObjectSpec objectType)
                    {
                        continue;
                    }

                    HashSet<string> keys = new(objectType.ConstructorParameters.Select(m => GetCacheElement(m)));
                    keys.UnionWith(objectType.Properties.Values.Select(m => GetCacheElement(m)));
                    static string GetCacheElement(MemberSpec member) => $@"""{member.ConfigurationKeyName}""";

                    string configKeysSource = string.Join(", ", keys);
                    string fieldName = GetConfigKeyCacheFieldName(objectType);
                    _writer.WriteLine($@"private readonly static Lazy<{TypeDisplayString.HashSetOfString}> {fieldName} = new(() => new {TypeDisplayString.HashSetOfString}(StringComparer.OrdinalIgnoreCase) {{ {configKeysSource} }});");
                }
            }

            private void EmitGetCoreMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.GetCore, out HashSet<TypeSpec>? types))
                {
                    return;
                }

                EmitBlankLineIfRequired();
                EmitStartBlock($"public static object? {nameof(MethodsToGen_CoreBindingHelper.GetCore)}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, Action<{Identifier.BinderOptions}>? {Identifier.configureOptions})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureOptions});");
                _writer.WriteLine();

                EmitIConfigurationHasValueOrChildrenCheck(voidReturn: false);

                bool isFirstType = true;
                foreach (TypeSpec type in types)
                {
                    TypeSpec effectiveType = type.EffectiveType;
                    TypeSpecKind kind = effectiveType.SpecKind;
                    string conditionKindExpr = GetConditionKindExpr(ref isFirstType);

                    EmitStartBlock($"{conditionKindExpr} ({Identifier.type} == typeof({type.DisplayString}))");

                    switch (effectiveType)
                    {
                        case ParsableFromStringSpec stringParsableType:
                            {
                                EmitCastToIConfigurationSection();
                                EmitBindingLogic(
                                    stringParsableType,
                                    Expression.sectionValue,
                                    Expression.sectionPath,
                                    writeOnSuccess: parsedValueExpr => _writer.WriteLine($"return {parsedValueExpr};"),
                                    checkForNullSectionValue: stringParsableType.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue,
                                    useIncrementalStringValueIdentifier: false);
                            }
                            break;
                        case ConfigurationSectionSpec configurationSectionSpec:
                            {
                                EmitCastToIConfigurationSection();
                                _writer.WriteLine($"return {Identifier.section};");
                            }
                            break;
                        case ComplexTypeSpec complexType:
                            {
                                if (complexType.CanInstantiate)
                                {
                                    EmitBindingLogic(complexType, Identifier.instance, Identifier.configuration, InitializationKind.Declaration);
                                    _writer.WriteLine($"return {Identifier.instance};");
                                }
                                else if (type is ObjectSpec { InitExceptionMessage: string exMsg })
                                {
                                    _writer.WriteLine($@"throw new {Identifier.InvalidOperationException}(""{exMsg}"");");
                                }
                            }
                            break;
                    }

                    EmitEndBlock(); // End if-check for input type.
                }

                _writer.WriteLine();
                Emit_NotSupportedException_TypeNotDetectedAsInput();
                EmitEndBlock();
                _emitBlankLineBeforeNextStatement = true;

                void EmitCastToIConfigurationSection() =>
                    _writer.WriteLine($$"""
                        if ({{Identifier.configuration}} is not {{Identifier.IConfigurationSection}} {{Identifier.section}})
                        {
                            throw new {{Identifier.InvalidOperationException}}();
                        }
                        """);
            }

            private void EmitGetValueCoreMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.GetValueCore, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                EmitBlankLineIfRequired();
                EmitStartBlock($"public static object? {nameof(MethodsToGen_CoreBindingHelper.GetValueCore)}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);
                _writer.WriteLine($@"{Identifier.IConfigurationSection} {Identifier.section} = {GetSectionFromConfigurationExpression(Identifier.key, addQuotes: false)};");
                _writer.WriteLine();

                _writer.WriteLine($$"""
                    if ({{Expression.sectionValue}} is not string {{Identifier.value}})
                    {
                        return null;
                    }
                    """);

                _writer.WriteLine();

                bool isFirstType = true;
                foreach (TypeSpec type in targetTypes)
                {
                    string conditionKindExpr = GetConditionKindExpr(ref isFirstType);
                    EmitStartBlock($"{conditionKindExpr} ({Identifier.type} == typeof({type.DisplayString}))");

                    EmitBindingLogic(
                        (ParsableFromStringSpec)type.EffectiveType,
                        Identifier.value,
                        Expression.sectionPath,
                        writeOnSuccess: (parsedValueExpr) => _writer.WriteLine($"return {parsedValueExpr};"),
                        checkForNullSectionValue: false,
                        useIncrementalStringValueIdentifier: false);

                    EmitEndBlock();
                }

                _writer.WriteLine();
                _writer.WriteLine("return null;");
                EmitEndBlock();
                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitBindCoreMainMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.BindCoreMain, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                EmitBlankLineIfRequired();
                EmitStartBlock($"public static void {nameof(MethodsToGen_CoreBindingHelper.BindCoreMain)}({Identifier.IConfiguration} {Identifier.configuration}, object {Identifier.instance}, Type {Identifier.type}, {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions})");
                EmitCheckForNullArgument_WithBlankLine(Identifier.instance, voidReturn: true);
                EmitIConfigurationHasValueOrChildrenCheck(voidReturn: true);
                _writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureOptions});");
                _writer.WriteLine();

                bool isFirstType = true;
                foreach (ComplexTypeSpec type in targetTypes)
                {
                    ComplexTypeSpec effectiveType = (ComplexTypeSpec)type.EffectiveType;
                    Debug.Assert(effectiveType.HasBindableMembers);
                    string conditionKindExpr = GetConditionKindExpr(ref isFirstType);

                    EmitStartBlock($"{conditionKindExpr} ({Identifier.type} == typeof({type.DisplayString}))");
                    _writer.WriteLine($"var {Identifier.temp} = ({effectiveType.DisplayString}){Identifier.instance};");
                    EmitBindingLogic(type, Identifier.temp, Identifier.configuration, InitializationKind.None);
                    _writer.WriteLine($"return;");
                    EmitEndBlock();
                }

                _writer.WriteLine();
                Emit_NotSupportedException_TypeNotDetectedAsInput();
                EmitEndBlock();
            }

            private void EmitBindCoreMethods()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.BindCore, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                foreach (ComplexTypeSpec type in targetTypes)
                {
                    Debug.Assert(type.HasBindableMembers);
                    EmitBlankLineIfRequired();
                    EmitBindCoreMethod(type);
                }
            }

            private void EmitBindCoreMethod(ComplexTypeSpec type)
            {
                string objParameterExpression = $"ref {type.DisplayString} {Identifier.instance}";
                EmitStartBlock(@$"public static void {nameof(MethodsToGen_CoreBindingHelper.BindCore)}({Identifier.IConfiguration} {Identifier.configuration}, {objParameterExpression}, {Identifier.BinderOptions}? {Identifier.binderOptions})");

                ComplexTypeSpec effectiveType = (ComplexTypeSpec)type.EffectiveType;
                if (effectiveType is EnumerableSpec enumerable)
                {
                    if (effectiveType.InstantiationStrategy is InstantiationStrategy.Array)
                    {
                        Debug.Assert(type == effectiveType);
                        EmitPopulationImplForArray((EnumerableSpec)type);
                    }
                    else
                    {
                        EmitPopulationImplForEnumerableWithAdd(enumerable);
                    }
                }
                else if (effectiveType is DictionarySpec dictionary)
                {
                    EmitBindCoreImplForDictionary(dictionary);
                }
                else
                {
                    EmitBindCoreImplForObject((ObjectSpec)effectiveType);
                }

                EmitEndBlock();
            }

            private void EmitInitializeMethods()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.Initialize, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                foreach (ObjectSpec type in targetTypes)
                {
                    EmitBlankLineIfRequired();
                    EmitInitializeMethod(type);
                }
            }

            private void EmitInitializeMethod(ObjectSpec type)
            {
                Debug.Assert(type.CanInstantiate);
                List<ParameterSpec> ctorParams = type.ConstructorParameters;
                IEnumerable<PropertySpec> initOnlyProps = type.Properties.Values.Where(prop => prop is { SetOnInit: true });
                List<string> ctorArgList = new();
                string displayString = type.DisplayString;

                EmitStartBlock($"public static {type.DisplayString} {GetInitalizeMethodDisplayString(type)}({Identifier.IConfiguration} {Identifier.configuration}, {Identifier.BinderOptions}? {Identifier.binderOptions})");
                _emitBlankLineBeforeNextStatement = false;

                foreach (ParameterSpec parameter in ctorParams)
                {
                    string name = parameter.Name;
                    string argExpr = parameter.RefKind switch
                    {
                        RefKind.None => name,
                        RefKind.Ref => $"ref {name}",
                        RefKind.Out => "out _",
                        RefKind.In => $"in {name}",
                        _ => throw new InvalidOperationException()
                    };

                    ctorArgList.Add(argExpr);
                    EmitBindImplForMember(parameter);
                }

                foreach (PropertySpec property in initOnlyProps)
                {
                    if (property.ShouldBindTo && property.MatchingCtorParam is null)
                    {
                        EmitBindImplForMember(property);
                    }
                }

                string returnExpression = $"return new {displayString}({string.Join(", ", ctorArgList)})";
                if (!initOnlyProps.Any())
                {
                    _writer.WriteLine($"{returnExpression};");
                }
                else
                {
                    EmitStartBlock(returnExpression);
                    foreach (PropertySpec property in initOnlyProps)
                    {
                        string propertyName = property.Name;
                        _writer.WriteLine($@"{propertyName} = {propertyName},");
                    }
                    EmitEndBlock(endBraceTrailingSource: ";");
                }

                // End method.
                EmitEndBlock();
                _emitBlankLineBeforeNextStatement = true;

                void EmitBindImplForMember(MemberSpec member)
                {
                    TypeSpec memberType = member.Type;
                    bool errorOnFailedBinding = member.ErrorOnFailedBinding;

                    string parsedMemberDeclarationLhs = $"{memberType.DisplayString} {member.Name}";
                    string configKeyName = member.ConfigurationKeyName;
                    string parsedMemberAssignmentLhsExpr;

                    switch (memberType)
                    {
                        case ParsableFromStringSpec { StringParsableTypeKind: StringParsableTypeKind.AssignFromSectionValue }:
                            {
                                if (errorOnFailedBinding)
                                {
                                    string condition = $@"if ({Identifier.configuration}[""{configKeyName}""] is not {parsedMemberDeclarationLhs})";
                                    EmitThrowBlock(condition);
                                    _writer.WriteLine();
                                    return;
                                }

                                parsedMemberAssignmentLhsExpr = parsedMemberDeclarationLhs;
                            }
                            break;
                        case ConfigurationSectionSpec:
                            {
                                _writer.WriteLine($"{parsedMemberDeclarationLhs} = {GetSectionFromConfigurationExpression(configKeyName)};");
                                return;
                            }
                        default:
                            {
                                string bangExpr = memberType.IsValueType ? string.Empty : "!";
                                string parsedMemberIdentifierDeclaration = $"{parsedMemberDeclarationLhs} = {member.DefaultValueExpr}{bangExpr};";

                                _writer.WriteLine(parsedMemberIdentifierDeclaration);
                                _emitBlankLineBeforeNextStatement = false;

                                parsedMemberAssignmentLhsExpr = member.Name;
                            }
                            break;
                    }

                    bool canBindToMember = this.EmitBindImplForMember(
                        member,
                        parsedMemberAssignmentLhsExpr,
                        sectionPathExpr: GetSectionPathFromConfigurationExpression(configKeyName),
                        canSet: true);

                    if (canBindToMember)
                    {
                        if (errorOnFailedBinding)
                        {
                            // Add exception logic for parameter ctors; must be present in configuration object.
                            EmitThrowBlock(condition: "else");
                        }

                        _writer.WriteLine();
                    }

                    void EmitThrowBlock(string condition) =>
                        _writer.WriteLine($$"""
                            {{condition}}
                            {
                                throw new {{Identifier.InvalidOperationException}}("{{string.Format(ExceptionMessages.ParameterHasNoMatchingConfig, type.Name, member.Name)}}");
                            }
                            """);
                }
            }

            private void EmitHelperMethods()
            {
                // Emitted if we are to bind objects with complex members, or if we're emitting BindCoreMain or GetCore methods.
                bool emitAsConfigWithChildren = ShouldEmitMethods(MethodsToGen_CoreBindingHelper.AsConfigWithChildren);

                if (ShouldEmitMethods(MethodsToGen_CoreBindingHelper.BindCore))
                {
                    EmitBlankLineIfRequired();
                    EmitValidateConfigurationKeysMethod();
                }

                if (ShouldEmitMethods(MethodsToGen_CoreBindingHelper.BindCoreMain | MethodsToGen_CoreBindingHelper.GetCore))
                {
                    // HasValueOrChildren references this method.
                    Debug.Assert(emitAsConfigWithChildren);
                    EmitBlankLineIfRequired();
                    EmitHasValueOrChildrenMethod();
                }

                if (emitAsConfigWithChildren)
                {
                    EmitBlankLineIfRequired();
                    EmitAsConfigWithChildrenMethod();
                }

                if (ShouldEmitMethods(MethodsToGen_CoreBindingHelper.BindCoreMain | MethodsToGen_CoreBindingHelper.GetCore) ||
                    ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions))
                {
                    EmitBlankLineIfRequired();
                    EmitGetBinderOptionsHelper();
                }

                bool enumTypeExists = false;

                foreach (ParsableFromStringSpec type in _sourceGenSpec.PrimitivesForHelperGen)
                {
                    EmitBlankLineIfRequired();

                    if (type.StringParsableTypeKind == StringParsableTypeKind.Enum)
                    {
                        if (!enumTypeExists)
                        {
                            EmitEnumParseMethod();
                            enumTypeExists = true;
                        }
                    }
                    else
                    {
                        EmitPrimitiveParseMethod(type);
                    }
                }
            }

            private void EmitValidateConfigurationKeysMethod()
            {
                const string keysIdentifier = "keys";
                string exceptionMessage = string.Format(ExceptionMessages.MissingConfig, Identifier.ErrorOnUnknownConfiguration, Identifier.BinderOptions, $"{{{Identifier.type}}}", $@"{{string.Join("", "", {Identifier.temp})}}");

                EmitBlankLineIfRequired();
                _writer.WriteLine($$"""
                    /// <summary>If required by the binder options, validates that there are no unknown keys in the input configuration object.</summary>
                    public static void {{Identifier.ValidateConfigurationKeys}}(Type {{Identifier.type}}, {{TypeDisplayString.LazyHashSetOfString}} {{keysIdentifier}}, {{Identifier.IConfiguration}} {{Identifier.configuration}}, {{Identifier.BinderOptions}}? {{Identifier.binderOptions}})
                    {
                        if ({{Identifier.binderOptions}}?.{{Identifier.ErrorOnUnknownConfiguration}} is true)
                        {
                            {{TypeDisplayString.ListOfString}}? {{Identifier.temp}} = null;
                    
                            foreach ({{Identifier.IConfigurationSection}} {{Identifier.section}} in {{Identifier.configuration}}.{{Identifier.GetChildren}}())
                            {
                                if (!{{keysIdentifier}}.Value.Contains({{Expression.sectionKey}}))
                                {
                                    ({{Identifier.temp}} ??= new {{TypeDisplayString.ListOfString}}()).Add($"'{{{Expression.sectionKey}}}'");
                                }
                            }

                            if ({{Identifier.temp}} is not null)
                            {
                                throw new InvalidOperationException($"{{exceptionMessage}}");
                            }
                        }
                    }
                    """);
            }

            private void EmitHasValueOrChildrenMethod()
            {
                _writer.WriteLine($$"""
                    public static bool {{Identifier.HasValueOrChildren}}({{Identifier.IConfiguration}} {{Identifier.configuration}})
                    {
                        if (({{Identifier.configuration}} as {{Identifier.IConfigurationSection}})?.{{Identifier.Value}} is not null)
                        {
                            return true;
                        }
                        return {{MethodsToGen_CoreBindingHelper.AsConfigWithChildren}}({{Identifier.configuration}}) is not null;
                    }
                    """);
            }

            private void EmitAsConfigWithChildrenMethod()
            {
                _writer.WriteLine($$"""
                    public static {{Identifier.IConfiguration}}? {{MethodsToGen_CoreBindingHelper.AsConfigWithChildren}}({{Identifier.IConfiguration}} {{Identifier.configuration}})
                    {
                        foreach ({{Identifier.IConfigurationSection}} _ in {{Identifier.configuration}}.{{Identifier.GetChildren}}())
                        {
                            return {{Identifier.configuration}};
                        }
                        return null;
                    }
                    """);
            }

            private void EmitGetBinderOptionsHelper()
            {
                _writer.WriteLine($$"""
                    public static {{Identifier.BinderOptions}}? {{Identifier.GetBinderOptions}}({{TypeDisplayString.NullableActionOfBinderOptions}} {{Identifier.configureOptions}})
                    {
                        if ({{Identifier.configureOptions}} is null)
                        {
                            return null;
                        }

                        {{Identifier.BinderOptions}} {{Identifier.binderOptions}} = new();
                        {{Identifier.configureOptions}}({{Identifier.binderOptions}});

                        if ({{Identifier.binderOptions}}.BindNonPublicProperties)
                        {
                            throw new NotSupportedException($"{{string.Format(ExceptionMessages.CannotSpecifyBindNonPublicProperties)}}");
                        }

                        return {{Identifier.binderOptions}};
                    }
                    """);
            }

            private void EmitEnumParseMethod()
            {
                string exceptionArg1 = string.Format(ExceptionMessages.FailedBinding, $"{{{Identifier.getPath}()}}", $"{{typeof(T)}}");

                _writer.WriteLine($$"""
                    public static T ParseEnum<T>(string value, Func<string?> getPath) where T : struct
                    {
                        try
                        {
                            #if NETFRAMEWORK || NETSTANDARD2_0
                                return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
                            #else
                                return Enum.Parse<T>(value, ignoreCase: true);
                            #endif
                        }
                        catch ({{Identifier.Exception}} {{Identifier.exception}})
                        {
                            throw new {{Identifier.InvalidOperationException}}($"{{exceptionArg1}}", {{Identifier.exception}});
                        }
                    }
                    """);
            }

            private void EmitPrimitiveParseMethod(ParsableFromStringSpec type)
            {
                StringParsableTypeKind typeKind = type.StringParsableTypeKind;
                string typeDisplayString = type.DisplayString;

                string invariantCultureExpression = $"{Identifier.CultureInfo}.InvariantCulture";
                string parsedValueExpr;

                switch (typeKind)
                {
                    case StringParsableTypeKind.Enum:
                        return;
                    case StringParsableTypeKind.ByteArray:
                        {
                            parsedValueExpr = $"Convert.FromBase64String({Identifier.value})";
                        }
                        break;
                    case StringParsableTypeKind.Integer:
                        {
                            parsedValueExpr = $"{typeDisplayString}.{Identifier.Parse}({Identifier.value}, {Identifier.NumberStyles}.Integer, {invariantCultureExpression})";
                        }
                        break;
                    case StringParsableTypeKind.Float:
                        {
                            parsedValueExpr = $"{typeDisplayString}.{Identifier.Parse}({Identifier.value}, {Identifier.NumberStyles}.Float, {invariantCultureExpression})";
                        }
                        break;
                    case StringParsableTypeKind.Parse:
                        {
                            parsedValueExpr = $"{typeDisplayString}.{Identifier.Parse}({Identifier.value})";
                        }
                        break;
                    case StringParsableTypeKind.ParseInvariant:
                        {
                            parsedValueExpr = $"{typeDisplayString}.{Identifier.Parse}({Identifier.value}, {invariantCultureExpression})"; ;
                        }
                        break;
                    case StringParsableTypeKind.CultureInfo:
                        {
                            parsedValueExpr = $"{Identifier.CultureInfo}.GetCultureInfo({Identifier.value})";
                        }
                        break;
                    case StringParsableTypeKind.Uri:
                        {
                            parsedValueExpr = $"new Uri({Identifier.value}, UriKind.RelativeOrAbsolute)";
                        }
                        break;
                    default:
                        {
                            Debug.Fail($"Invalid string parsable kind: {typeKind}");
                            return;
                        }
                }

                string exceptionArg1 = string.Format(ExceptionMessages.FailedBinding, $"{{{Identifier.getPath}()}}", $"{{typeof({typeDisplayString})}}");

                EmitStartBlock($"public static {typeDisplayString} {type.ParseMethodName}(string {Identifier.value}, Func<string?> {Identifier.getPath})");
                EmitEndBlock($$"""
                    try
                    {
                        return {{parsedValueExpr}};
                    }
                    catch ({{Identifier.Exception}} {{Identifier.exception}})
                    {
                        throw new {{Identifier.InvalidOperationException}}($"{{exceptionArg1}}", {{Identifier.exception}});
                    }
                    """);
            }

            private void EmitPopulationImplForArray(EnumerableSpec type)
            {
                EnumerableSpec typeToInstantiate = (EnumerableSpec)type.TypeToInstantiate;

                // Create list and bind elements.
                string tempIdentifier = GetIncrementalIdentifier(Identifier.temp);
                EmitBindingLogic(typeToInstantiate, tempIdentifier, Identifier.configuration, InitializationKind.Declaration);

                // Resize array and add binded elements.
                _writer.WriteLine($$"""
                    {{Identifier.Int32}} {{Identifier.originalCount}} = {{Identifier.instance}}.{{Identifier.Length}};
                    {{Identifier.Array}}.{{Identifier.Resize}}(ref {{Identifier.instance}}, {{Identifier.originalCount}} + {{tempIdentifier}}.{{Identifier.Count}});
                    {{tempIdentifier}}.{{Identifier.CopyTo}}({{Identifier.instance}}, {{Identifier.originalCount}});
                    """);
            }

            private void EmitPopulationImplForEnumerableWithAdd(EnumerableSpec type)
            {
                EmitCollectionCastIfRequired(type, out string instanceIdentifier);

                Emit_Foreach_Section_In_ConfigChildren_StartBlock();

                string addExpr = $"{instanceIdentifier}.{Identifier.Add}";

                switch (type.ElementType)
                {
                    case ParsableFromStringSpec stringParsableType:
                        {
                            EmitBindingLogic(
                                stringParsableType,
                                Expression.sectionValue,
                                Expression.sectionPath,
                                (parsedValueExpr) => _writer.WriteLine($"{addExpr}({parsedValueExpr});"),
                                checkForNullSectionValue: true,
                                useIncrementalStringValueIdentifier: false);
                        }
                        break;
                    case ConfigurationSectionSpec configurationSection:
                        {
                            _writer.WriteLine($"{addExpr}({Identifier.section});");
                        }
                        break;
                    case ComplexTypeSpec { CanInstantiate: true } complexType:
                        {
                            EmitBindingLogic(complexType, Identifier.value, Identifier.section, InitializationKind.Declaration);
                            _writer.WriteLine($"{addExpr}({Identifier.value});");
                        }
                        break;
                }

                EmitEndBlock();
            }

            private void EmitBindCoreImplForDictionary(DictionarySpec type)
            {
                EmitCollectionCastIfRequired(type, out string instanceIdentifier);

                Emit_Foreach_Section_In_ConfigChildren_StartBlock();

                ParsableFromStringSpec keyType = type.KeyType;
                TypeSpec elementType = type.ElementType;

                // Parse key
                EmitBindingLogic(
                    keyType,
                    Expression.sectionKey,
                    Expression.sectionPath,
                    Emit_BindAndAddLogic_ForElement,
                    checkForNullSectionValue: false,
                    useIncrementalStringValueIdentifier: false);

                void Emit_BindAndAddLogic_ForElement(string parsedKeyExpr)
                {
                    switch (elementType)
                    {
                        case ParsableFromStringSpec stringParsableElementType:
                            {
                                EmitBindingLogic(
                                    stringParsableElementType,
                                    Expression.sectionValue,
                                    Expression.sectionPath,
                                    writeOnSuccess: parsedValueExpr => _writer.WriteLine($"{instanceIdentifier}[{parsedKeyExpr}] = {parsedValueExpr};"),
                                    checkForNullSectionValue: true,
                                    useIncrementalStringValueIdentifier: false);
                            }
                            break;
                        case ConfigurationSectionSpec configurationSection:
                            {
                                _writer.WriteLine($"{instanceIdentifier}[{parsedKeyExpr}] = {Identifier.section};");
                            }
                            break;
                        case ComplexTypeSpec complexElementType:
                            {
                                Debug.Assert(complexElementType.CanInstantiate);

                                if (keyType.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue)
                                {
                                    // Save value to local to avoid parsing twice - during look-up and during add.
                                    _writer.WriteLine($"{keyType.DisplayString} {Identifier.key} = {parsedKeyExpr};");
                                    parsedKeyExpr = Identifier.key;
                                }

                                bool isValueType = complexElementType.IsValueType;
                                string expressionForElementIsNotNull = $"{Identifier.element} is not null";
                                string elementTypeDisplayString = complexElementType.DisplayString + (complexElementType.IsValueType ? string.Empty : "?");

                                string expressionForElementExists = $"{instanceIdentifier}.{Identifier.TryGetValue}({parsedKeyExpr}, out {elementTypeDisplayString} {Identifier.element})";
                                string conditionToUseExistingElement = expressionForElementExists;

                                // If key already exists, bind to existing element instance if not null (for ref types).
                                if (!isValueType)
                                {
                                    conditionToUseExistingElement += $" && {expressionForElementIsNotNull}";
                                }

                                EmitStartBlock($"if (!({conditionToUseExistingElement}))");
                                EmitObjectInit(complexElementType, Identifier.element, InitializationKind.SimpleAssignment, Identifier.section);
                                EmitEndBlock();

                                EmitBindingLogic(complexElementType, Identifier.element, Identifier.section, InitializationKind.None);
                                _writer.WriteLine($"{instanceIdentifier}[{parsedKeyExpr}] = {Identifier.element};");
                            }
                            break;
                    }
                }

                EmitEndBlock();
            }

            private void EmitBindCoreImplForObject(ObjectSpec type)
            {
                Debug.Assert(type.HasBindableMembers);

                string keyCacheFieldName = GetConfigKeyCacheFieldName(type);
                string validateMethodCallExpr = $"{Identifier.ValidateConfigurationKeys}(typeof({type.DisplayString}), {keyCacheFieldName}, {Identifier.configuration}, {Identifier.binderOptions});";
                _writer.WriteLine(validateMethodCallExpr);

                foreach (PropertySpec property in type.Properties.Values)
                {
                    bool noSetter_And_IsReadonly = !property.CanSet && property.Type is CollectionSpec { InstantiationStrategy: InstantiationStrategy.ParameterizedConstructor };
                    if (property.ShouldBindTo && !noSetter_And_IsReadonly)
                    {
                        string containingTypeRef = property.IsStatic ? type.DisplayString : Identifier.instance;
                        EmitBindImplForMember(
                            property,
                            memberAccessExpr: $"{containingTypeRef}.{property.Name}",
                            GetSectionPathFromConfigurationExpression(property.ConfigurationKeyName),
                            canSet: property.CanSet);
                    }
                }
            }

            private bool EmitBindImplForMember(
                MemberSpec member,
                string memberAccessExpr,
                string sectionPathExpr,
                bool canSet)
            {
                TypeSpec effectiveMemberType = member.Type.EffectiveType;
                string sectionParseExpr = GetSectionFromConfigurationExpression(member.ConfigurationKeyName);

                switch (effectiveMemberType)
                {
                    case ParsableFromStringSpec stringParsableType:
                        {
                            if (canSet)
                            {
                                bool checkForNullSectionValue = member is ParameterSpec
                                    ? true
                                    : stringParsableType.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue;

                                string nullBangExpr = checkForNullSectionValue ? string.Empty : "!";

                                EmitBlankLineIfRequired();
                                EmitBindingLogic(
                                    stringParsableType,
                                    $@"{Identifier.configuration}[""{member.ConfigurationKeyName}""]",
                                    sectionPathExpr,
                                    writeOnSuccess: parsedValueExpr => _writer.WriteLine($"{memberAccessExpr} = {parsedValueExpr}{nullBangExpr};"),
                                    checkForNullSectionValue,
                                    useIncrementalStringValueIdentifier: true);
                            }

                            return true;
                        }
                    case ConfigurationSectionSpec:
                        {
                            if (canSet)
                            {
                                EmitBlankLineIfRequired();
                                _writer.WriteLine($"{memberAccessExpr} = {sectionParseExpr};");
                            }

                            return true;
                        }
                    case ComplexTypeSpec complexType:
                        {
                            string sectionValidationCall = $"{MethodsToGen_CoreBindingHelper.AsConfigWithChildren}({sectionParseExpr})";
                            string sectionIdentifier = GetIncrementalIdentifier(Identifier.section);

                            EmitBlankLineIfRequired();
                            EmitStartBlock($"if ({sectionValidationCall} is {Identifier.IConfigurationSection} {sectionIdentifier})");
                            EmitBindingLogicForComplexMember(member, memberAccessExpr, sectionIdentifier, canSet);
                            EmitEndBlock();

                            return complexType.CanInstantiate;
                        }
                    default:
                        return false;
                }
            }

            private void EmitBindingLogicForComplexMember(
                MemberSpec member,
                string memberAccessExpr,
                string configArgExpr,
                bool canSet)
            {

                TypeSpec memberType = member.Type;
                ComplexTypeSpec effectiveMemberType = (ComplexTypeSpec)memberType.EffectiveType;

                string tempIdentifier = GetIncrementalIdentifier(Identifier.temp);
                InitializationKind initKind;
                string targetObjAccessExpr;

                if (effectiveMemberType.IsValueType)
                {
                    if (!canSet)
                    {
                        return;
                    }

                    Debug.Assert(canSet);
                    string effectiveMemberTypeDisplayString = effectiveMemberType.DisplayString;
                    initKind = InitializationKind.None;

                    if (memberType.SpecKind is TypeSpecKind.Nullable)
                    {
                        string nullableTempIdentifier = GetIncrementalIdentifier(Identifier.temp);

                        _writer.WriteLine($"{memberType.DisplayString} {nullableTempIdentifier} = {memberAccessExpr};");

                        _writer.WriteLine(
                            $"{effectiveMemberTypeDisplayString} {tempIdentifier} = {nullableTempIdentifier}.{Identifier.HasValue} ? {nullableTempIdentifier}.{Identifier.Value} : new {effectiveMemberTypeDisplayString}();");
                    }
                    else
                    {
                        _writer.WriteLine($"{effectiveMemberTypeDisplayString} {tempIdentifier} = {memberAccessExpr};");
                    }

                    targetObjAccessExpr = tempIdentifier;
                }
                else if (member.CanGet)
                {
                    targetObjAccessExpr = memberAccessExpr;
                    initKind = InitializationKind.AssignmentWithNullCheck;
                }
                else
                {
                    targetObjAccessExpr = memberAccessExpr;
                    initKind = InitializationKind.SimpleAssignment;
                }

                Action<string>? writeOnSuccess = !canSet
                     ? null
                     : bindedValueIdentifier =>
                         {
                             if (memberAccessExpr != bindedValueIdentifier)
                             {
                                 _writer.WriteLine($"{memberAccessExpr} = {bindedValueIdentifier};");
                             }
                         };

                EmitBindingLogic(
                    effectiveMemberType,
                    targetObjAccessExpr,
                    configArgExpr,
                    initKind,
                    writeOnSuccess);
            }

            private void EmitBindingLogic(
                ComplexTypeSpec type,
                string memberAccessExpr,
                string configArgExpr,
                InitializationKind initKind,
                Action<string>? writeOnSuccess = null)
            {
                if (!type.HasBindableMembers)
                {
                    if (initKind is not InitializationKind.None)
                    {
                        if (type.CanInstantiate)
                        {
                            EmitObjectInit(type, memberAccessExpr, initKind, configArgExpr);
                        }
                        else if (type is ObjectSpec { InitExceptionMessage: string exMsg })
                        {
                            _writer.WriteLine($@"throw new {Identifier.InvalidOperationException}(""{exMsg}"");");
                        }
                    }

                    return;
                }

                string tempIdentifier = GetIncrementalIdentifier(Identifier.temp);
                if (initKind is InitializationKind.AssignmentWithNullCheck)
                {
                    Debug.Assert(!type.IsValueType);
                    _writer.WriteLine($"{type.DisplayString}? {tempIdentifier} = {memberAccessExpr};");
                    EmitBindingLogic(tempIdentifier, InitializationKind.AssignmentWithNullCheck);
                }
                else if (initKind is InitializationKind.None && type.IsValueType)
                {
                    EmitBindingLogic(tempIdentifier, InitializationKind.Declaration);
                    _writer.WriteLine($"{memberAccessExpr} = {tempIdentifier};");
                }
                else
                {
                    EmitBindingLogic(memberAccessExpr, initKind);
                }

                void EmitBindingLogic(string instanceToBindExpr, InitializationKind initKind)
                {
                    string bindCoreCall = $@"{nameof(MethodsToGen_CoreBindingHelper.BindCore)}({configArgExpr}, ref {instanceToBindExpr}, {Identifier.binderOptions});";

                    if (type.CanInstantiate)
                    {
                        if (initKind is not InitializationKind.None)
                        {
                            EmitObjectInit(type, instanceToBindExpr, initKind, configArgExpr);
                        }

                        EmitBindCoreCall();
                    }
                    else
                    {
                        Debug.Assert(!type.IsValueType);

                        if (type is ObjectSpec { InitExceptionMessage: string exMsg })
                        {
                            _writer.WriteLine($@"throw new {Identifier.InvalidOperationException}(""{exMsg}"");");
                        }
                        else
                        {
                            EmitStartBlock($"if ({instanceToBindExpr} is not null)");
                            EmitBindCoreCall();
                            EmitEndBlock();
                        }
                    }

                    void EmitBindCoreCall()
                    {
                        _writer.WriteLine(bindCoreCall);
                        writeOnSuccess?.Invoke(instanceToBindExpr);
                    }
                }
            }

            private void EmitBindingLogic(
                ParsableFromStringSpec type,
                string sectionValueExpr,
                string sectionPathExpr,
                Action<string>? writeOnSuccess,
                bool checkForNullSectionValue,
                bool useIncrementalStringValueIdentifier)
            {
                StringParsableTypeKind typeKind = type.StringParsableTypeKind;
                Debug.Assert(typeKind is not StringParsableTypeKind.None);

                string nonNull_StringValue_Identifier = useIncrementalStringValueIdentifier ? GetIncrementalIdentifier(Identifier.value) : Identifier.value;
                string stringValueToParse_Expr = checkForNullSectionValue ? nonNull_StringValue_Identifier : sectionValueExpr;
                string parsedValueExpr = typeKind switch
                {
                    StringParsableTypeKind.AssignFromSectionValue => stringValueToParse_Expr,
                    StringParsableTypeKind.Enum => $"ParseEnum<{type.DisplayString}>({stringValueToParse_Expr}, () => {sectionPathExpr})",
                    _ => $"{type.ParseMethodName}({stringValueToParse_Expr}, () => {sectionPathExpr})",
                };

                if (!checkForNullSectionValue)
                {
                    InvokeWriteOnSuccess();
                }
                else
                {
                    EmitStartBlock($"if ({sectionValueExpr} is string {nonNull_StringValue_Identifier})");
                    InvokeWriteOnSuccess();
                    EmitEndBlock();
                }

                void InvokeWriteOnSuccess() => writeOnSuccess?.Invoke(parsedValueExpr);
            }

            private bool EmitObjectInit(ComplexTypeSpec type, string memberAccessExpr, InitializationKind initKind, string configArgExpr)
            {
                CollectionSpec? collectionType = type as CollectionSpec;
                string initExpr;

                string effectiveDisplayString = type.DisplayString;
                if (collectionType is not null)
                {
                    if (collectionType is EnumerableSpec { InstantiationStrategy: InstantiationStrategy.Array })
                    {
                        initExpr = $"new {s_arrayBracketsRegex.Replace(effectiveDisplayString, "[0]", 1)}";
                    }
                    else
                    {
                        effectiveDisplayString = (collectionType.TypeToInstantiate ?? collectionType).DisplayString;
                        initExpr = $"new {effectiveDisplayString}()";
                    }
                }
                else if (type.InstantiationStrategy is InstantiationStrategy.ParameterlessConstructor)
                {
                    initExpr = $"new {effectiveDisplayString}()";
                }
                else
                {
                    Debug.Assert(type.InstantiationStrategy is InstantiationStrategy.ParameterizedConstructor);
                    string initMethodIdentifier = GetInitalizeMethodDisplayString(((ObjectSpec)type));
                    initExpr = $"{initMethodIdentifier}({configArgExpr}, {Identifier.binderOptions})";
                }

                switch (initKind)
                {
                    case InitializationKind.Declaration:
                        {
                            Debug.Assert(!memberAccessExpr.Contains("."));
                            _writer.WriteLine($"var {memberAccessExpr} = {initExpr};");
                        }
                        break;
                    case InitializationKind.AssignmentWithNullCheck:
                        {
                            if (collectionType is CollectionSpec
                                {
                                    InstantiationStrategy: InstantiationStrategy.ParameterizedConstructor or InstantiationStrategy.ToEnumerableMethod
                                })
                            {
                                if (collectionType.InstantiationStrategy is InstantiationStrategy.ParameterizedConstructor)
                                {
                                    _writer.WriteLine($"{memberAccessExpr} = {memberAccessExpr} is null ? {initExpr} : new {effectiveDisplayString}({memberAccessExpr});");
                                }
                                else
                                {
                                    Debug.Assert(collectionType is DictionarySpec);
                                    _writer.WriteLine($"{memberAccessExpr} = {memberAccessExpr} is null ? {initExpr} : {memberAccessExpr}.ToDictionary(pair => pair.Key, pair => pair.Value);");
                                }
                            }
                            else
                            {
                                _writer.WriteLine($"{memberAccessExpr} ??= {initExpr};");
                            }
                        }
                        break;
                    case InitializationKind.SimpleAssignment:
                        {
                            _writer.WriteLine($"{memberAccessExpr} = {initExpr};");
                        }
                        break;
                    default:
                        {
                            Debug.Fail($"Invaild initialization kind: {initKind}");
                        }
                        break;
                }

                return true;
            }

            private void EmitIConfigurationHasValueOrChildrenCheck(bool voidReturn)
            {
                string returnPostfix = voidReturn ? string.Empty : " null";
                _writer.WriteLine($$"""
                    if (!{{Identifier.HasValueOrChildren}}({{Identifier.configuration}}))
                    {
                        return{{returnPostfix}};
                    }
                    """);
                _writer.WriteLine();
            }

            private void EmitCollectionCastIfRequired(CollectionSpec type, out string instanceIdentifier)
            {
                instanceIdentifier = Identifier.instance;
                if (type.PopulationStrategy is CollectionPopulationStrategy.Cast_Then_Add)
                {
                    instanceIdentifier = Identifier.temp;
                    _writer.WriteLine($$"""
                        if ({{Identifier.instance}} is not {{type.PopulationCastType!.DisplayString}} {{instanceIdentifier}})
                        {
                            return;
                        }
                        """);
                    _writer.WriteLine();
                }
            }

            private void Emit_Foreach_Section_In_ConfigChildren_StartBlock() =>
                EmitStartBlock($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");

            private void Emit_NotSupportedException_TypeNotDetectedAsInput() =>
                _writer.WriteLine(@$"throw new NotSupportedException($""{string.Format(ExceptionMessages.TypeNotDetectedAsInput, "{type}")}"");");

            private static string GetSectionPathFromConfigurationExpression(string configurationKeyName)
                => $@"{GetSectionFromConfigurationExpression(configurationKeyName)}.{Identifier.Path}";

            private static string GetSectionFromConfigurationExpression(string configurationKeyName, bool addQuotes = true)
            {
                string argExpr = addQuotes ? $@"""{configurationKeyName}""" : configurationKeyName;
                return $@"{Identifier.configuration}.{Identifier.GetSection}({argExpr})";
            }

            private static string GetConditionKindExpr(ref bool isFirstType)
            {
                if (isFirstType)
                {
                    isFirstType = false;
                    return "if";
                }

                return "else if";
            }

            private static string GetConfigKeyCacheFieldName(ObjectSpec type) =>
                $"s_configKeys_{type.IdentifierCompatibleSubstring}";
        }
    }
}
