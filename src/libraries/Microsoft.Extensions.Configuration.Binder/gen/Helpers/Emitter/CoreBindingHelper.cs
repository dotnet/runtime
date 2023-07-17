// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private bool ShouldEmitMethods(MethodsToGen_CoreBindingHelper methods) => (_sourceGenSpec.MethodsToGen_CoreBindingHelper & methods) != 0;

            private void Emit_CoreBindingHelper()
            {
                Debug.Assert(_emitBlankLineBeforeNextStatement);
                _writer.WriteBlankLine();
                _emitBlankLineBeforeNextStatement = false;

                _writer.WriteBlockStart($"namespace {ProjectName}");
                EmitHelperUsingStatements();

                _writer.WriteBlankLine();

                _writer.WriteBlock($$"""
                    /// <summary>Provide core binding logic.</summary>
                    {{GetGeneratedCodeAttributeSrc()}}
                    file static class {{Identifier.CoreBindingHelper}}
                    {
                    """);

                EmitConfigurationKeyCaches();
                EmitGetCoreMethod();
                EmitGetValueCoreMethod();
                EmitBindCoreUntypedMethod();
                EmitBindCoreMethods();
                EmitInitializeMethods();
                EmitHelperMethods();

                _writer.WriteBlockEnd(); // End helper class.
                _writer.WriteBlockEnd(); // End namespace.
            }

            private void EmitHelperUsingStatements()
            {
                foreach (string @namespace in _sourceGenSpec.TypeNamespaces.ToImmutableSortedSet())
                {
                    _writer.WriteLine($"using {@namespace};");
                }
            }

            private void EmitConfigurationKeyCaches()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.BindCore, out HashSet<TypeSpec> targetTypes))
                {
                    return;
                }

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
                    _writer.WriteLine($@"private readonly static Lazy<{MinimalDisplayString.HashSetOfString}> {fieldName} = new(() => new {MinimalDisplayString.HashSetOfString}(StringComparer.OrdinalIgnoreCase) {{ {configKeysSource} }});");
                }

                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitGetCoreMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.GetCore, out HashSet<TypeSpec>? types))
                {
                    return;
                }

                EmitBlankLineIfRequired();
                _writer.WriteBlockStart($"public static object? {nameof(MethodsToGen_CoreBindingHelper.GetCore)}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, Action<{Identifier.BinderOptions}>? {Identifier.configureOptions})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureOptions});");
                _writer.WriteBlankLine();

                EmitIConfigurationHasValueOrChildrenCheck(voidReturn: false);

                foreach (TypeSpec type in types)
                {
                    TypeSpecKind kind = type.SpecKind;

                    _writer.WriteBlockStart($"if (type == typeof({type.MinimalDisplayString}))");

                    if (type is ParsableFromStringSpec stringParsableType)
                    {
                        EmitCastToIConfigurationSection();
                        EmitBindLogicFromString(
                            stringParsableType,
                            Expression.sectionValue,
                            Expression.sectionPath,
                            writeOnSuccess: parsedValueExpr => _writer.WriteLine($"return {parsedValueExpr};"),
                            checkForNullSectionValue: stringParsableType.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue,
                            useIncrementalStringValueIdentifier: false);
                    }
                    else if (!EmitInitException(type))
                    {
                        EmitBindCoreCall(type, Identifier.obj, Identifier.configuration, InitializationKind.Declaration);
                        _writer.WriteLine($"return {Identifier.obj};");
                    }

                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                Emit_NotSupportedException_TypeNotDetectedAsInput();
                _writer.WriteBlockEnd();
                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitGetValueCoreMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.GetValueCore, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                EmitBlankLineIfRequired();
                _writer.WriteBlockStart($"public static object? {nameof(MethodsToGen_CoreBindingHelper.GetValueCore)}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);
                _writer.WriteLine($@"{Identifier.IConfigurationSection} {Identifier.section} = {GetSectionFromConfigurationExpression(Identifier.key, addQuotes: false)};");
                _writer.WriteBlankLine();

                _writer.WriteBlock($$"""
                    if ({{Expression.sectionValue}} is not string {{Identifier.value}})
                    {
                        return null;
                    }
                    """);

                _writer.WriteBlankLine();

                foreach (TypeSpec type in targetTypes)
                {
                    _writer.WriteBlockStart($"if ({Identifier.type} == typeof({type.MinimalDisplayString}))");

                    EmitBindLogicFromString(
                        (ParsableFromStringSpec)type.EffectiveType,
                        Identifier.value,
                        Expression.sectionPath,
                        writeOnSuccess: (parsedValueExpr) => _writer.WriteLine($"return {parsedValueExpr};"),
                        checkForNullSectionValue: false,
                        useIncrementalStringValueIdentifier: false);

                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                _writer.WriteLine("return null;");
                _writer.WriteBlockEnd();
                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitBindCoreUntypedMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.BindCoreUntyped, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                EmitBlankLineIfRequired();

                _writer.WriteBlockStart($"public static void {nameof(MethodsToGen_CoreBindingHelper.BindCoreUntyped)}(this {Identifier.IConfiguration} {Identifier.configuration}, object {Identifier.obj}, Type {Identifier.type}, {MinimalDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureOptions});");
                _writer.WriteBlankLine();

                EmitIConfigurationHasValueOrChildrenCheck(voidReturn: true);

                foreach (TypeSpec type in targetTypes)
                {
                    _writer.WriteBlockStart($"if (type == typeof({type.MinimalDisplayString}))");

                    TypeSpec effectiveType = type.EffectiveType;
                    if (!EmitInitException(effectiveType))
                    {
                        _writer.WriteLine($"var {Identifier.temp} = ({effectiveType.MinimalDisplayString}){Identifier.obj};");
                        EmitBindCoreCall(type, Identifier.temp, Identifier.configuration, InitializationKind.None);
                        _writer.WriteLine($"return;");
                    }

                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                Emit_NotSupportedException_TypeNotDetectedAsInput();
                _writer.WriteBlockEnd();
                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitBindCoreMethods()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.BindCore, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                foreach (TypeSpec type in targetTypes)
                {
                    Debug.Assert(type.NeedsMemberBinding);
                    EmitBlankLineIfRequired();
                    EmitBindCoreMethod(type);
                }
            }

            private void EmitBindCoreMethod(TypeSpec type)
            {
                Debug.Assert(type.CanInitialize);

                string objParameterExpression = $"ref {type.MinimalDisplayString} {Identifier.obj}";
                _writer.WriteBlockStart(@$"public static void {nameof(MethodsToGen_CoreBindingHelper.BindCore)}({Identifier.IConfiguration} {Identifier.configuration}, {objParameterExpression}, {Identifier.BinderOptions}? {Identifier.binderOptions})");

                EmitCheckForNullArgument_WithBlankLine_IfRequired(type.IsValueType);

                TypeSpec effectiveType = type.EffectiveType;
                if (effectiveType is EnumerableSpec enumerable)
                {
                    if (effectiveType.InitializationStrategy is InitializationStrategy.Array)
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

                _writer.WriteBlockEnd();
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
                Debug.Assert(type.CanInitialize);
                List<ParameterSpec> ctorParams = type.ConstructorParameters;
                IEnumerable<PropertySpec> initOnlyProps = type.Properties.Values.Where(prop => prop is { SetOnInit: true });
                List<string> ctorArgList = new();
                string displayString = type.MinimalDisplayString;

                _writer.WriteBlockStart($"public static {type.MinimalDisplayString} {GetInitalizeMethodDisplayString(type)}({Identifier.IConfiguration} {Identifier.configuration}, {Identifier.BinderOptions}? {Identifier.binderOptions})");
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
                    if (property.ShouldBind() && property.MatchingCtorParam is null)
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
                    _writer.WriteBlockStart(returnExpression);
                    foreach (PropertySpec property in initOnlyProps)
                    {
                        string propertyName = property.Name;
                        _writer.WriteLine($@"{propertyName} = {propertyName},");
                    }
                    _writer.WriteBlockEnd(";");
                }

                // End method.
                _writer.WriteBlockEnd();
                _emitBlankLineBeforeNextStatement = true;

                void EmitBindImplForMember(MemberSpec member)
                {
                    TypeSpec memberType = member.Type;
                    bool errorOnFailedBinding = member.ErrorOnFailedBinding;

                    string parsedMemberIdentifierDeclarationPrefix = $"{memberType.MinimalDisplayString} {member.Name}";
                    string parsedMemberIdentifier;

                    if (memberType is ParsableFromStringSpec { StringParsableTypeKind: StringParsableTypeKind.AssignFromSectionValue })
                    {
                        parsedMemberIdentifier = parsedMemberIdentifierDeclarationPrefix;

                        if (errorOnFailedBinding)
                        {
                            string condition = $@" if ({Identifier.configuration}[""{member.ConfigurationKeyName}""] is not {memberType.MinimalDisplayString} {member.Name})";
                            EmitThrowBlock(condition);
                            _writer.WriteBlankLine();
                            return;
                        }
                    }
                    else
                    {
                        parsedMemberIdentifier = member.Name;

                        string declarationSuffix;
                        if (errorOnFailedBinding)
                        {
                            declarationSuffix = ";";
                        }
                        else
                        {
                            string bangExpr = memberType.IsValueType ? string.Empty : "!";
                            declarationSuffix = memberType.CanInitialize
                                ? $" = {member.DefaultValueExpr}{bangExpr};"
                                : ";";
                        }

                        string parsedMemberIdentifierDeclaration = $"{parsedMemberIdentifierDeclarationPrefix}{declarationSuffix}";
                        _writer.WriteLine(parsedMemberIdentifierDeclaration);
                        _emitBlankLineBeforeNextStatement = false;
                    }

                    bool canBindToMember = this.EmitBindImplForMember(
                        member,
                        parsedMemberIdentifier,
                        sectionPathExpr: GetSectionPathFromConfigurationExpression(member.ConfigurationKeyName),
                        canSet: true);

                    if (canBindToMember)
                    {
                        if (errorOnFailedBinding)
                        {
                            // Add exception logic for parameter ctors; must be present in configuration object.
                            EmitThrowBlock(condition: "else");
                        }

                        _writer.WriteBlankLine();
                    }

                    void EmitThrowBlock(string condition) =>
                        _writer.WriteBlock($$"""
                            {{condition}}
                            {
                                throw new {{GetInvalidOperationDisplayName()}}("{{string.Format(ExceptionMessages.ParameterHasNoMatchingConfig, type.Name, member.Name)}}");
                            }
                            """);
                }
            }

            private void EmitHelperMethods()
            {
                if (ShouldEmitMethods(MethodsToGen_CoreBindingHelper.BindCore))
                {
                    EmitValidateConfigurationKeysMethod();
                }

                if (ShouldEmitMethods(MethodsToGen_CoreBindingHelper.BindCoreUntyped | MethodsToGen_CoreBindingHelper.GetCore))
                {
                    _writer.WriteBlankLine();
                    EmitHasValueOrChildrenMethod();
                    _writer.WriteBlankLine();
                    EmitAsConfigWithChildrenMethod();
                    _emitBlankLineBeforeNextStatement = true;
                }
                else if (ShouldEmitMethods(MethodsToGen_CoreBindingHelper.AsConfigWithChildren))
                {
                    _writer.WriteBlankLine();
                    EmitAsConfigWithChildrenMethod();
                    _emitBlankLineBeforeNextStatement = true;
                }

                if (ShouldEmitMethods(
                    MethodsToGen_CoreBindingHelper.BindCoreUntyped | MethodsToGen_CoreBindingHelper.GetCore) ||
                    ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions))
                {
                    _writer.WriteBlankLine();
                    EmitGetBinderOptionsHelper();
                    _emitBlankLineBeforeNextStatement = true;
                }

                foreach (ParsableFromStringSpec type in _sourceGenSpec.PrimitivesForHelperGen)
                {
                    EmitBlankLineIfRequired();
                    EmitPrimitiveParseMethod(type);
                }
            }

            private void EmitValidateConfigurationKeysMethod()
            {
                const string keysIdentifier = "keys";
                string exceptionMessage = string.Format(ExceptionMessages.MissingConfig, Identifier.ErrorOnUnknownConfiguration, Identifier.BinderOptions, $"{{{Identifier.type}}}", $@"{{string.Join("", "", {Identifier.temp})}}");

                EmitBlankLineIfRequired();
                _writer.WriteBlock($$"""
                    /// <summary>If required by the binder options, validates that there are no unknown keys in the input configuration object.</summary>
                    public static void {{Identifier.ValidateConfigurationKeys}}(Type {{Identifier.type}}, {{MinimalDisplayString.LazyHashSetOfString}} {{keysIdentifier}}, {{Identifier.IConfiguration}} {{Identifier.configuration}}, {{Identifier.BinderOptions}}? {{Identifier.binderOptions}})
                    {
                        if ({{Identifier.binderOptions}}?.{{Identifier.ErrorOnUnknownConfiguration}} is true)
                        {
                            {{MinimalDisplayString.ListOfString}}? {{Identifier.temp}} = null;
                    
                            foreach ({{Identifier.IConfigurationSection}} {{Identifier.section}} in {{Identifier.configuration}}.{{Identifier.GetChildren}}())
                            {
                                if (!{{keysIdentifier}}.Value.Contains({{Expression.sectionKey}}))
                                {
                                    ({{Identifier.temp}} ??= new {{MinimalDisplayString.ListOfString}}()).Add($"'{{{Expression.sectionKey}}}'");
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
                _writer.WriteBlock($$"""
                    public static bool {{Identifier.HasValueOrChildren}}({{Identifier.IConfiguration}} {{Identifier.configuration}})
                    {
                        if (({{Identifier.configuration}} as {{Identifier.IConfigurationSection}})?.{{Identifier.Value}} is not null)
                        {
                            return true;
                        }
                        return {{Identifier.AsConfigWithChildren}}({{Identifier.configuration}}) is not null;
                    }
                    """);
            }

            private void EmitAsConfigWithChildrenMethod()
            {
                _writer.WriteBlock($$"""
                    public static {{Identifier.IConfiguration}}? {{Identifier.AsConfigWithChildren}}({{Identifier.IConfiguration}} {{Identifier.configuration}})
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
                _writer.WriteBlock($$"""
                    public static {{Identifier.BinderOptions}}? {{Identifier.GetBinderOptions}}({{MinimalDisplayString.NullableActionOfBinderOptions}} {{Identifier.configureOptions}})
                    {
                        if ({{Identifier.configureOptions}} is null)
                        {
                            return null;
                        }

                        {{Identifier.BinderOptions}} {{Identifier.binderOptions}} = new();
                        {{Identifier.configureOptions}}({{Identifier.binderOptions}});

                        if ({{Identifier.binderOptions}}.BindNonPublicProperties)
                        {
                            throw new global::System.NotSupportedException($"{{string.Format(ExceptionMessages.CannotSpecifyBindNonPublicProperties)}}");
                        }

                        return {{Identifier.binderOptions}};
                    }
                    """);
            }

            private void EmitPrimitiveParseMethod(ParsableFromStringSpec type)
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

                StringParsableTypeKind typeKind = type.StringParsableTypeKind;
                string typeDisplayString = type.MinimalDisplayString;

                string invariantCultureExpression = $"{cultureInfoTypeDisplayString}.InvariantCulture";

                string parsedValueExpr;
                switch (typeKind)
                {
                    case StringParsableTypeKind.Enum:
                        {
                            parsedValueExpr = $"({typeDisplayString}){Identifier.Enum}.{Identifier.Parse}(typeof({typeDisplayString}), {Identifier.value}, ignoreCase: true)";
                        }
                        break;
                    case StringParsableTypeKind.ByteArray:
                        {
                            parsedValueExpr = $"Convert.FromBase64String({Identifier.value})";
                        }
                        break;
                    case StringParsableTypeKind.Integer:
                        {
                            parsedValueExpr = $"{typeDisplayString}.{Identifier.Parse}({Identifier.value}, {numberStylesTypeDisplayString}.Integer, {invariantCultureExpression})";
                        }
                        break;
                    case StringParsableTypeKind.Float:
                        {
                            parsedValueExpr = $"{typeDisplayString}.{Identifier.Parse}({Identifier.value}, {numberStylesTypeDisplayString}.Float, {invariantCultureExpression})";
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
                            parsedValueExpr = $"{cultureInfoTypeDisplayString}.GetCultureInfo({Identifier.value})";
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

                _writer.WriteBlock($$"""
                    public static {{typeDisplayString}} {{type.ParseMethodName}}(string {{Identifier.value}}, Func<string?> {{Identifier.getPath}})
                    {
                        try
                        {
                            return {{parsedValueExpr}};
                    """);

                string exceptionArg1 = string.Format(ExceptionMessages.FailedBinding, $"{{{Identifier.getPath}()}}", $"{{typeof({typeDisplayString})}}");

                _writer.WriteBlock($$"""
                        }
                        catch ({{innerExceptionTypeDisplayString}} {{Identifier.exception}})
                        {
                            throw new {{GetInvalidOperationDisplayName()}}($"{{exceptionArg1}}", {{Identifier.exception}});
                        }
                    }
                    """);
            }

            private void EmitPopulationImplForArray(EnumerableSpec type)
            {
                EnumerableSpec concreteType = (EnumerableSpec)type.ConcreteType;

                // Create list and bind elements.
                string tempIdentifier = GetIncrementalIdentifier(Identifier.temp);
                EmitBindCoreCall(concreteType, tempIdentifier, Identifier.configuration, InitializationKind.Declaration);

                // Resize array and add binded elements.
                _writer.WriteBlock($$"""
                    {{Identifier.Int32}} {{Identifier.originalCount}} = {{Identifier.obj}}.{{Identifier.Length}};
                    {{Identifier.Array}}.{{Identifier.Resize}}(ref {{Identifier.obj}}, {{Identifier.originalCount}} + {{tempIdentifier}}.{{Identifier.Count}});
                    {{tempIdentifier}}.{{Identifier.CopyTo}}({{Identifier.obj}}, {{Identifier.originalCount}});
                    """);
            }

            private void EmitPopulationImplForEnumerableWithAdd(EnumerableSpec type)
            {
                EmitCollectionCastIfRequired(type, out string objIdentifier);

                Emit_Foreach_Section_In_ConfigChildren_BlockHeader();

                TypeSpec elementType = type.ElementType;

                if (elementType is ParsableFromStringSpec stringParsableType)
                {
                    EmitBindLogicFromString(
                        stringParsableType,
                        Expression.sectionValue,
                        Expression.sectionPath,
                        (parsedValueExpr) => _writer.WriteLine($"{objIdentifier}.{Identifier.Add}({parsedValueExpr});"),
                        checkForNullSectionValue: true,
                        useIncrementalStringValueIdentifier: false);
                }
                else
                {
                    EmitBindCoreCall(elementType, Identifier.value, Identifier.section, InitializationKind.Declaration);
                    _writer.WriteLine($"{objIdentifier}.{Identifier.Add}({Identifier.value});");
                }

                _writer.WriteBlockEnd();
            }

            private void EmitBindCoreImplForDictionary(DictionarySpec type)
            {
                EmitCollectionCastIfRequired(type, out string objIdentifier);

                Emit_Foreach_Section_In_ConfigChildren_BlockHeader();

                ParsableFromStringSpec keyType = type.KeyType;
                TypeSpec elementType = type.ElementType;

                // Parse key
                EmitBindLogicFromString(
                    keyType,
                    Expression.sectionKey,
                    Expression.sectionPath,
                    Emit_BindAndAddLogic_ForElement,
                    checkForNullSectionValue: false,
                    useIncrementalStringValueIdentifier: false);

                void Emit_BindAndAddLogic_ForElement(string parsedKeyExpr)
                {
                    if (elementType is ParsableFromStringSpec stringParsableElementType)
                    {
                        EmitBindLogicFromString(
                            stringParsableElementType,
                            Expression.sectionValue,
                            Expression.sectionPath,
                            writeOnSuccess: parsedValueExpr => _writer.WriteLine($"{objIdentifier}[{parsedKeyExpr}] = {parsedValueExpr};"),
                            checkForNullSectionValue: true,
                            useIncrementalStringValueIdentifier: false);
                    }
                    else // For complex types:
                    {
                        Debug.Assert(elementType.CanInitialize);

                        if (keyType.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue)
                        {
                            // Save value to local to avoid parsing twice - during look-up and during add.
                            _writer.WriteLine($"{keyType.MinimalDisplayString} {Identifier.key} = {parsedKeyExpr};");
                            parsedKeyExpr = Identifier.key;
                        }

                        bool isValueType = elementType.IsValueType;
                        string expressionForElementIsNotNull = $"{Identifier.element} is not null";
                        string elementTypeDisplayString = elementType.MinimalDisplayString + (elementType.IsValueType ? string.Empty : "?");

                        string expressionForElementExists = $"{objIdentifier}.{Identifier.TryGetValue}({parsedKeyExpr}, out {elementTypeDisplayString} {Identifier.element})";
                        string conditionToUseExistingElement = expressionForElementExists;

                        // If key already exists, bind to existing element instance if not null (for ref types).
                        if (!isValueType)
                        {
                            conditionToUseExistingElement += $" && {expressionForElementIsNotNull}";
                        }

                        _writer.WriteBlockStart($"if (!({conditionToUseExistingElement}))");
                        EmitObjectInit(elementType, Identifier.element, InitializationKind.SimpleAssignment, Identifier.section);
                        _writer.WriteBlockEnd();

                        if (elementType is CollectionSpec { InitializationStrategy: InitializationStrategy.ParameterizedConstructor or InitializationStrategy.ToEnumerableMethod } collectionSpec)
                        {
                            // This is a read-only collection. If the element exists and is not null,
                            // we need to copy its contents into a new instance & then append/bind to that.

                            string initExpression = collectionSpec.InitializationStrategy is InitializationStrategy.ParameterizedConstructor
                                ? $"new {collectionSpec.ConcreteType.MinimalDisplayString}({Identifier.element})"
                                : $"{Identifier.element}.{collectionSpec.ToEnumerableMethodCall!}";

                            _writer.WriteBlock($$"""
                                else
                                {
                                    {{Identifier.element}} = {{initExpression}};
                                }
                                """);
                        }

                        EmitBindCoreCall(elementType, $"{Identifier.element}", Identifier.section, InitializationKind.None);
                        _writer.WriteLine($"{objIdentifier}[{parsedKeyExpr}] = {Identifier.element};");
                    }

                }

                _writer.WriteBlockEnd();
            }

            private void EmitBindCoreImplForObject(ObjectSpec type)
            {
                Debug.Assert(type.NeedsMemberBinding);

                string keyCacheFieldName = GetConfigKeyCacheFieldName(type);
                string validateMethodCallExpr = $"{Identifier.ValidateConfigurationKeys}(typeof({type.MinimalDisplayString}), {keyCacheFieldName}, {Identifier.configuration}, {Identifier.binderOptions});";
                _writer.WriteLine(validateMethodCallExpr);

                foreach (PropertySpec property in type.Properties.Values)
                {
                    bool noSetter_And_IsReadonly = !property.CanSet && property.Type is CollectionSpec { InitializationStrategy: InitializationStrategy.ParameterizedConstructor };
                    if (property.ShouldBind() && !noSetter_And_IsReadonly)
                    {
                        string containingTypeRef = property.IsStatic ? type.MinimalDisplayString : Identifier.obj;
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

                if (effectiveMemberType is ParsableFromStringSpec stringParsableType)
                {
                    if (canSet)
                    {
                        bool checkForNullSectionValue = member is ParameterSpec
                            ? true
                            : stringParsableType.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue;

                        string nullBangExpr = checkForNullSectionValue ? string.Empty : "!";

                        EmitBlankLineIfRequired();
                        EmitBindLogicFromString(
                            stringParsableType,
                            $@"{Identifier.configuration}[""{member.ConfigurationKeyName}""]",
                            sectionPathExpr,
                            writeOnSuccess: parsedValueExpr => _writer.WriteLine($"{memberAccessExpr} = {parsedValueExpr}{nullBangExpr};"),
                            checkForNullSectionValue,
                            useIncrementalStringValueIdentifier: true);
                    }

                    return true;
                }

                string sectionParseExpr = $"{GetSectionFromConfigurationExpression(member.ConfigurationKeyName)}";

                EmitBlankLineIfRequired();

                if (effectiveMemberType.SpecKind is TypeSpecKind.IConfigurationSection)
                {
                    _writer.WriteLine($"{memberAccessExpr} = {sectionParseExpr};");
                    return true;
                }

                string sectionValidationCall = $"{Identifier.AsConfigWithChildren}({sectionParseExpr})";
                string sectionIdentifier = GetIncrementalIdentifier(Identifier.section);

                _writer.WriteBlockStart($"if ({sectionValidationCall} is {Identifier.IConfigurationSection} {sectionIdentifier})");

                bool success = !EmitInitException(effectiveMemberType);
                if (success)
                {
                    EmitBindCoreCallForMember(member, memberAccessExpr, sectionIdentifier, canSet);
                }

                _writer.WriteBlockEnd();
                return success;
            }

            private void EmitBindCoreCallForMember(
                MemberSpec member,
                string memberAccessExpr,
                string configArgExpr,
                bool canSet)
            {

                TypeSpec memberType = member.Type;
                TypeSpec effectiveMemberType = memberType.EffectiveType;
                string effectiveMemberTypeDisplayString = effectiveMemberType.MinimalDisplayString;
                bool canGet = member.CanGet;

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
                    initKind = InitializationKind.None;

                    if (memberType.SpecKind is TypeSpecKind.Nullable)
                    {
                        string nullableTempIdentifier = GetIncrementalIdentifier(Identifier.temp);

                        _writer.WriteLine($"{memberType.MinimalDisplayString} {nullableTempIdentifier} = {memberAccessExpr};");

                        _writer.WriteLine(
                            $"{effectiveMemberTypeDisplayString} {tempIdentifier} = {nullableTempIdentifier}.{Identifier.HasValue} ? {nullableTempIdentifier}.{Identifier.Value} : new {effectiveMemberTypeDisplayString}();");
                    }
                    else
                    {
                        _writer.WriteLine($"{effectiveMemberTypeDisplayString} {tempIdentifier} = {memberAccessExpr};");
                    }

                    targetObjAccessExpr = tempIdentifier;
                }
                else if (canGet)
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

                EmitBindCoreCall(
                    effectiveMemberType,
                    targetObjAccessExpr,
                    configArgExpr,
                    initKind,
                    writeOnSuccess);
            }

            private void EmitCollectionCastIfRequired(CollectionSpec type, out string objIdentifier)
            {
                objIdentifier = Identifier.obj;
                if (type.PopulationStrategy is CollectionPopulationStrategy.Cast_Then_Add)
                {
                    objIdentifier = Identifier.temp;
                    _writer.WriteBlock($$"""
                        if ({{Identifier.obj}} is not {{type.PopulationCastType!.MinimalDisplayString}} {{objIdentifier}})
                        {
                            return;
                        }
                        """);
                    _writer.WriteBlankLine();
                }
            }

            private void Emit_Foreach_Section_In_ConfigChildren_BlockHeader() =>
                _writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");

            private static string GetSectionPathFromConfigurationExpression(string configurationKeyName)
                => $@"{GetSectionFromConfigurationExpression(configurationKeyName)}.{Identifier.Path}";

            private static string GetSectionFromConfigurationExpression(string configurationKeyName, bool addQuotes = true)
            {
                string argExpr = addQuotes ? $@"""{configurationKeyName}""" : configurationKeyName;
                return $@"{Expression.configurationGetSection}({argExpr})";
            }

            private static string GetConfigKeyCacheFieldName(ObjectSpec type) =>
                $"s_configKeys_{type.DisplayStringWithoutSpecialCharacters}";

            private void Emit_NotSupportedException_TypeNotDetectedAsInput() =>
                _writer.WriteLine(@$"throw new global::System.NotSupportedException($""{string.Format(ExceptionMessages.TypeNotDetectedAsInput, "{type}")}"");");
        }
    }
}
