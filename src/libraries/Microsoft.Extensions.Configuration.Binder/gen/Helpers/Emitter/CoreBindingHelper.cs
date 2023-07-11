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
                Debug.Assert(_precedingBlockExists);
                _writer.WriteBlankLine();
                _precedingBlockExists = false;

                _writer.WriteBlockStart($"namespace {ProjectName}");
                EmitHelperUsingStatements();

                _writer.WriteBlankLine();

                _writer.WriteLine("/// <summary>Provide core binding logic.</summary>");
                _writer.WriteBlockStart($"internal static class {Identifier.CoreBindingHelper}");

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

            private void EmitGetCoreMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.GetCore, out HashSet<TypeSpec>? types))
                {
                    return;
                }

                _writer.WriteBlockStart($"public static object? {Identifier.GetCore}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, Action<{Identifier.BinderOptions}>? {Identifier.configureOptions})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureOptions});");
                _writer.WriteBlankLine();

                EmitIConfigurationHasValueOrChildrenCheck(voidReturn: false);

                foreach (TypeSpec type in types)
                {
                    _writer.WriteBlockStart($"if (type == typeof({type.MinimalDisplayString}))");

                    if (type.InitializationStrategy is InitializationStrategy.None || !EmitInitException(type))
                    {
                        EmitBindLogicFromRootMethod(type, Identifier.obj, InitializationKind.Declaration);
                        _writer.WriteLine($"return {Identifier.obj};");
                    }

                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                Emit_NotSupportedException_TypeNotDetectedAsInput();
                _writer.WriteBlockEnd();
                _precedingBlockExists = true;
            }

            private void EmitGetValueCoreMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.GetValueCore, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                EmitBlankLineIfRequired();

                _writer.WriteBlockStart($"public static object? {Identifier.GetValueCore}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteLine($"{Identifier.IConfigurationSection} {Identifier.section} = {Identifier.configuration}.{Identifier.GetSection}({Identifier.key});");

                _writer.WriteBlankLine();

                foreach (TypeSpec type in targetTypes)
                {
                    ParsableFromStringSpec effectiveType = (ParsableFromStringSpec)((type as NullableSpec)?.UnderlyingType ?? type);
                    _writer.WriteBlockStart($"if ({Identifier.type} == typeof({type.MinimalDisplayString}))");

                    EmitBindLogicFromString(
                            effectiveType,
                            Expression.sectionValue,
                            Expression.sectionPath,
                            writeOnSuccess: (parsedValueExpr) => _writer.WriteLine($"return {parsedValueExpr};"));

                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                _writer.WriteLine("return null;");
                _writer.WriteBlockEnd();
                _precedingBlockExists = true;
            }

            private void EmitBindCoreUntypedMethod()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.BindCoreUntyped, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                EmitBlankLineIfRequired();

                _writer.WriteBlockStart($"public static void {Identifier.BindCoreUntyped}(this {Identifier.IConfiguration} {Identifier.configuration}, object {Identifier.obj}, Type {Identifier.type}, {MinimalDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions})");

                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureOptions});");
                _writer.WriteBlankLine();

                EmitIConfigurationHasValueOrChildrenCheck(voidReturn: true);

                foreach (TypeSpec type in targetTypes)
                {
                    _writer.WriteBlockStart($"if (type == typeof({type.MinimalDisplayString}))");

                    if (type.InitializationStrategy is InitializationStrategy.None || !EmitInitException(type))
                    {
                        _writer.WriteLine($"var {Identifier.temp} = ({type.MinimalDisplayString}){Identifier.obj};");
                        EmitBindLogicFromRootMethod(type, Identifier.temp, InitializationKind.None);
                        _writer.WriteLine($"return;");
                    }

                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                Emit_NotSupportedException_TypeNotDetectedAsInput();
                _writer.WriteBlockEnd();
                _precedingBlockExists = true;
            }

            private void EmitBindCoreMethods()
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(MethodsToGen_CoreBindingHelper.BindCore, out HashSet<TypeSpec>? targetTypes))
                {
                    return;
                }

                foreach (TypeSpec type in targetTypes)
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
                if (!type.CanInitialize)
                {
                    return;
                }

                string objParameterExpression = $"ref {type.MinimalDisplayString} {Identifier.obj}";
                _writer.WriteBlockStart(@$"public static void {Identifier.BindCore}({Identifier.IConfiguration} {Identifier.configuration}, {objParameterExpression}, {Identifier.BinderOptions}? {Identifier.binderOptions})");
                EmitBindCoreImpl(type);
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
                IEnumerable<PropertySpec> initOnlyProps = type.Properties.Values.Where(prop => prop.SetOnInit);
                string displayString = type.MinimalDisplayString;

                _writer.WriteBlockStart($"public static {displayString} {type.InitializeMethodDisplayString}({Identifier.IConfiguration} {Identifier.configuration}, {Identifier.BinderOptions}? {Identifier.binderOptions})");

                foreach (ParameterSpec parameter in ctorParams)
                {
                    if (!parameter.HasExplicitDefaultValue)
                    {
                        _writer.WriteLine($@"({parameter.Type.MinimalDisplayString} {Identifier.Value}, bool {Identifier.HasConfig}) {parameter.Name} = ({parameter.DefaultValue}, false);");
                    }
                    else
                    {
                        _writer.WriteLine($@"{parameter.Type.MinimalDisplayString} {parameter.Name} = {parameter.DefaultValue};");
                    }
                }

                foreach (PropertySpec property in initOnlyProps)
                {
                    if (property.MatchingCtorParam is null)
                    {
                        _writer.WriteLine($@"{property.Type.MinimalDisplayString} {property.Name} = default!;");
                    }
                }

                _writer.WriteBlankLine();

                _writer.WriteBlock($$"""
                        foreach ({{Identifier.IConfigurationSection}} {{Identifier.section}} in {{Identifier.configuration}}.{{Identifier.GetChildren}}())
                        {
                            switch ({{Expression.sectionKey}})
                            {
                    """);

                List<string> argumentList = new();

                foreach (ParameterSpec parameter in ctorParams)
                {
                    EmitMemberBindLogic(parameter.Name, parameter.Type, parameter.ConfigurationKeyName, configValueMustExist: !parameter.HasExplicitDefaultValue);
                    argumentList.Add(GetExpressionForArgument(parameter));
                }

                foreach (PropertySpec property in initOnlyProps)
                {
                    if (property.ShouldBind() && property.MatchingCtorParam is null)
                    {
                        EmitMemberBindLogic(property.Name, property.Type, property.ConfigurationKeyName);
                    }
                }

                EmitSwitchDefault("continue;", addBreak: false);

                _writer.WriteBlockEnd();
                _writer.WriteBlockEnd();

                _precedingBlockExists = true;

                foreach (ParameterSpec parameter in ctorParams)
                {
                    if (!parameter.HasExplicitDefaultValue)
                    {
                        string parameterName = parameter.Name;

                        EmitBlankLineIfRequired();
                        _writer.WriteBlock($$"""
                        if (!{{parameterName}}.{{Identifier.HasConfig}})
                        {
                            throw new {{GetInvalidOperationDisplayName()}}("{{string.Format(ExceptionMessages.ParameterHasNoMatchingConfig, type.Name, parameterName)}}");
                        }
                        """);
                    }
                }

                EmitBlankLineIfRequired();

                string returnExpression = $"return new {displayString}({string.Join(", ", argumentList)})";
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
                        string initValue = propertyName + (property.MatchingCtorParam is null or ParameterSpec { HasExplicitDefaultValue: true } ? string.Empty : $".{Identifier.Value}");
                        _writer.WriteLine($@"{propertyName} = {initValue},");
                    }
                    _writer.WriteBlockEnd(";");
                }

                // End method.
                _writer.WriteBlockEnd();

                void EmitMemberBindLogic(string memberName, TypeSpec memberType, string configurationKeyName, bool configValueMustExist = false)
                {
                    string lhs = memberName + (configValueMustExist ? $".{Identifier.Value}" : string.Empty);

                    _writer.WriteLine($@"case ""{configurationKeyName}"":");
                    _writer.Indentation++;
                    _writer.WriteBlockStart();

                    EmitMemberBindLogicCore(memberType, lhs);

                    if (configValueMustExist)
                    {
                        _writer.WriteLine($"{memberName}.{Identifier.HasConfig} = true;");
                    }

                    _writer.WriteBlockEnd();
                    _writer.WriteLine("break;");
                    _writer.Indentation--;

                    void EmitMemberBindLogicCore(TypeSpec type, string lhs)
                    {
                        TypeSpecKind kind = type.SpecKind;

                        if (kind is TypeSpecKind.Nullable)
                        {
                            EmitMemberBindLogicCore(((NullableSpec)type).UnderlyingType, lhs);
                        }
                        else if (type is ParsableFromStringSpec stringParsableType)
                        {
                            EmitBindLogicFromString(
                                stringParsableType,
                                Expression.sectionValue,
                                Expression.sectionPath,
                                (parsedValueExpr) => _writer.WriteLine($"{lhs} = {parsedValueExpr}!;"));
                        }
                        else if (!EmitInitException(type))
                        {
                            EmitBindCoreCall(type, lhs, Identifier.section, InitializationKind.SimpleAssignment);
                        }
                    }
                }

                static string GetExpressionForArgument(ParameterSpec parameter)
                {
                    string name = parameter.Name + (parameter.HasExplicitDefaultValue ? string.Empty : $".{Identifier.Value}");

                    return parameter.RefKind switch
                    {
                        RefKind.None => name,
                        RefKind.Ref => $"ref {name}",
                        RefKind.Out => "out _",
                        RefKind.In => $"in {name}",
                        _ => throw new InvalidOperationException()
                    };
                }
            }

            private void EmitHelperMethods()
            {
                if (ShouldEmitMethods(MethodsToGen_CoreBindingHelper.BindCoreUntyped | MethodsToGen_CoreBindingHelper.GetCore))
                {
                    _writer.WriteBlankLine();
                    EmitHasValueOrChildrenMethod();
                    _writer.WriteBlankLine();
                    EmitHasChildrenMethod();
                    _precedingBlockExists = true;
                }
                else if (_sourceGenSpec.ShouldEmitHasChildren)
                {
                    _writer.WriteBlankLine();
                    EmitHasChildrenMethod();
                    _precedingBlockExists = true;
                }

                if (ShouldEmitMethods(
                    MethodsToGen_CoreBindingHelper.BindCoreUntyped | MethodsToGen_CoreBindingHelper.GetCore) ||
                    ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions))
                {
                    _writer.WriteBlankLine();
                    EmitGetBinderOptionsHelper();
                    _precedingBlockExists = true;
                }

                foreach (ParsableFromStringSpec type in _sourceGenSpec.PrimitivesForHelperGen)
                {
                    EmitBlankLineIfRequired();
                    EmitPrimitiveParseMethod(type);
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
                            throw new {{GetInvalidOperationDisplayName()}}($"{{exceptionArg1}}", {{Identifier.exception}});
                        }
                    }
                    """);
            }

            private void EmitBindCoreImpl(TypeSpec type)
            {
                switch (type.SpecKind)
                {
                    case TypeSpecKind.Enumerable:
                    case TypeSpecKind.Dictionary:
                    case TypeSpecKind.Object:
                        {
                            Debug.Assert(type.CanInitialize);
                            EmitCheckForNullArgument_WithBlankLine_IfRequired(type.IsValueType);
                            EmitBindCoreImplForComplexType(type);
                        }
                        break;
                    case TypeSpecKind.Nullable:
                        {
                            EmitBindCoreImpl(((NullableSpec)type).UnderlyingType);
                        }
                        break;
                    case TypeSpecKind.IConfigurationSection:
                        {
                            EmitCastToIConfigurationSection();
                            _writer.WriteLine($"{Identifier.obj} = {Identifier.section};");
                        }
                        break;
                    default:
                        Debug.Fail("Invalid type kind", type.SpecKind.ToString());
                        break;
                }
            }

            private void EmitBindCoreImplForComplexType(TypeSpec type)
            {
                if (type.InitializationStrategy is InitializationStrategy.Array)
                {
                    EmitPopulationImplForArray((EnumerableSpec)type);
                }
                else if (type is EnumerableSpec enumerable)
                {
                    EmitPopulationImplForEnumerableWithAdd(enumerable);
                }
                else if (type is DictionarySpec dictionary)
                {
                    EmitBindCoreImplForDictionary(dictionary);
                }
                else
                {
                    EmitBindCoreImplForObject((ObjectSpec)type);
                }
            }

            private void EmitPopulationImplForArray(EnumerableSpec type)
            {
                EnumerableSpec concreteType = (EnumerableSpec)type.ConcreteType;

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

            private void EmitPopulationImplForEnumerableWithAdd(EnumerableSpec type)
            {
                EmitCollectionCastIfRequired(type, out string objIdentifier);

                _writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");

                TypeSpec elementType = type.ElementType;

                if (elementType is ParsableFromStringSpec stringParsableType)
                {
                    EmitBindLogicFromString(
                        stringParsableType,
                        Expression.sectionValue,
                        Expression.sectionPath,
                        (parsedValueExpr) => _writer.WriteLine($"{objIdentifier}.{Identifier.Add}({parsedValueExpr}!);"),
                        isCollectionElement: true);
                }
                else
                {
                    EmitBindCoreCall(elementType, Identifier.element, Identifier.section, InitializationKind.Declaration);
                    _writer.WriteLine($"{objIdentifier}.{Identifier.Add}({Identifier.element});");
                }

                _writer.WriteBlockEnd();
            }

            private void EmitBindCoreImplForDictionary(DictionarySpec type)
            {
                EmitCollectionCastIfRequired(type, out string objIdentifier);

                _writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");

                ParsableFromStringSpec keyType = type.KeyType;
                TypeSpec elementType = type.ElementType;

                // Parse key
                EmitBindLogicFromString(
                        keyType,
                        Expression.sectionKey,
                        Expression.sectionPath,
                        Emit_BindAndAddLogic_ForElement);

                void Emit_BindAndAddLogic_ForElement(string parsedKeyExpr)
                {
                    if (elementType is ParsableFromStringSpec stringParsableElementType)
                    {
                        EmitBindLogicFromString(
                            stringParsableElementType,
                            Expression.sectionValue,
                            Expression.sectionPath,
                            (parsedValueExpr) => _writer.WriteLine($"{objIdentifier}[{parsedKeyExpr}!] = {parsedValueExpr}!;"),
                            isCollectionElement: true);
                    }
                    else // For complex types:
                    {
                        Debug.Assert(elementType.CanInitialize);

                        parsedKeyExpr += "!";
                        if (keyType.StringParsableTypeKind is not StringParsableTypeKind.ConfigValue)
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
                        EmitObjectInit(elementType, Identifier.element, InitializationKind.SimpleAssignment);
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

                        EmitBindCoreCall(elementType, $"{Identifier.element}!", Identifier.section, InitializationKind.None);
                        _writer.WriteLine($"{objIdentifier}[{parsedKeyExpr}] = {Identifier.element};");
                    }

                }

                _writer.WriteBlockEnd();
            }

            private void EmitBindCoreImplForObject(ObjectSpec type)
            {
                if (type.Properties.Count == 0)
                {
                    return;
                }

                string listOfStringDisplayName = "List<string>";
                _writer.WriteLine($"{listOfStringDisplayName}? {Identifier.temp} = null;");

                _writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");
                _writer.WriteBlockStart($"switch ({Expression.sectionKey})");

                foreach (PropertySpec property in type.Properties.Values)
                {
                    _writer.WriteLine($@"case ""{property.ConfigurationKeyName}"":");
                    _writer.Indentation++;
                    _writer.WriteBlockStart();

                    bool success = true;
                    if (property.ShouldBind())
                    {
                        success = EmitBindCoreImplForProperty(property, property.Type, parentType: type);
                    }

                    _writer.WriteBlockEnd();

                    if (success)
                    {
                        _writer.WriteLine("break;");
                    }

                    _writer.Indentation--;
                }

                EmitSwitchDefault($$"""
                if ({{Identifier.binderOptions}}?.ErrorOnUnknownConfiguration == true)
                {
                    ({{Identifier.temp}} ??= new {{listOfStringDisplayName}}()).Add($"'{{{Expression.sectionKey}}}'");
                }
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

            private bool EmitBindCoreImplForProperty(PropertySpec property, TypeSpec propertyType, TypeSpec parentType)
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
                            if (canSet && propertyType is ParsableFromStringSpec stringParsableType)
                            {
                                EmitBindLogicFromString(
                                    stringParsableType,
                                    expressionForConfigValueIndexer,
                                    Expression.sectionPath,
                                    (parsedValueExpr) => _writer.WriteLine($"{expressionForPropertyAccess} = {parsedValueExpr}!;"));
                            }
                        }
                        break;
                    case TypeSpecKind.IConfigurationSection:
                        {
                            _writer.WriteLine($"{expressionForPropertyAccess} = {Identifier.section};");
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
                            if (EmitInitException(propertyType))
                            {
                                return false;
                            }

                            EmitBindCoreCallForProperty(property, propertyType, expressionForPropertyAccess);
                        }
                        break;
                }

                return true;
            }

            private void EmitBindCoreCallForProperty(PropertySpec property, TypeSpec effectivePropertyType, string expressionForPropertyAccess)
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

                                _writer.WriteLine($"{actualPropertyType.MinimalDisplayString} {nullableTempVarName} = {expressionForPropertyAccess};");

                                _writer.WriteLine(
                                    $"{effectivePropertyTypeDisplayString} {tempVarName} = {nullableTempVarName}.{Identifier.HasValue} ? {nullableTempVarName}.{Identifier.Value} : new {effectivePropertyTypeDisplayString}();");
                            }
                            else
                            {
                                _writer.WriteLine($"{effectivePropertyTypeDisplayString} {tempVarName} = {expressionForPropertyAccess};");
                            }
                        }
                        else
                        {
                            EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.Declaration);
                        }

                        _writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");
                        _writer.WriteLine($"{expressionForPropertyAccess} = {tempVarName};");
                    }
                }
                else
                {
                    if (canGet)
                    {
                        _writer.WriteLine($"{effectivePropertyTypeDisplayString} {tempVarName} = {expressionForPropertyAccess};");
                        EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.AssignmentWithNullCheck);
                        _writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");

                        if (canSet)
                        {
                            _writer.WriteLine($"{expressionForPropertyAccess} = {tempVarName};");
                        }
                    }
                    else
                    {
                        Debug.Assert(canSet);
                        EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.Declaration);
                        _writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");
                        _writer.WriteLine($"{expressionForPropertyAccess} = {tempVarName};");
                    }
                }

                _writer.WriteBlockEnd();
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

            private void EmitSwitchDefault(string caseLogic, bool addBreak = true)
            {
                _writer.WriteLine("default:");
                _writer.Indentation++;
                _writer.WriteBlockStart();
                _writer.WriteBlock(caseLogic);
                _writer.WriteBlockEnd();

                if (addBreak)
                {
                    _writer.WriteLine("break;");
                }

                _writer.Indentation--;
            }

            private void Emit_NotSupportedException_TypeNotDetectedAsInput() =>
                _writer.WriteLine(@$"throw new global::System.NotSupportedException($""{string.Format(ExceptionMessages.TypeNotDetectedAsInput, "{type}")}"");");
        }
    }
}
