// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

using static Microsoft.Extensions.Configuration.Binder.SourceGeneration.Emitter;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed partial record CoreBindingHelperMethodSpec : MethodSpec
    {
        public override void Emit(Emitter emitter)
        {
            Debug.Assert(emitter.PrecedingBlockExists);
            emitter.Writer.WriteBlankLine();
            emitter.PrecedingBlockExists = false;

            emitter.Writer.WriteBlockStart($"namespace {ConfigurationBindingGenerator.ProjectName}");
            EmitHelperUsingStatements(emitter);

            emitter.Writer.WriteBlankLine();

            emitter.Writer.WriteLine("/// <summary>Provide core binding logic.</summary>");
            emitter.Writer.WriteBlockStart($"internal static class {Identifier.CoreBindingHelper}");

            EmitGetCoreMethod(emitter);
            EmitGetValueCoreMethod(emitter);
            EmitBindCoreUntypedMethod(emitter);
            EmitBindCoreMethods(emitter);
            EmitInitializeMethods(emitter);
            EmitHelperMethods(emitter);

            emitter.Writer.WriteBlockEnd(); // End helper class.
            emitter.Writer.WriteBlockEnd(); // End namespace.
        }

        private bool ShouldEmitMethods(MethodSpecifier methods) => (_methodsToGen & methods) != 0;

        private void EmitHelperUsingStatements(Emitter emitter)
        {
            foreach (string @namespace in TypeNamespaces.ToImmutableSortedSet())
            {
                emitter.Writer.WriteLine($"using {@namespace};");
            }
        }

        private void EmitGetCoreMethod(Emitter emitter)
        {
            if (!TypesForCoreBindingMethodGen.TryGetValue(MethodSpecifier.GetCore, out HashSet<TypeSpec>? types))
            {
                return;
            }

            emitter.Writer.WriteBlockStart($"public static object? {Identifier.GetCore}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, Action<{Identifier.BinderOptions}>? {Identifier.configureOptions})");

            emitter.EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

            emitter.Writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureOptions});");
            emitter.Writer.WriteBlankLine();

            emitter.EmitIConfigurationHasValueOrChildrenCheck(voidReturn: false);

            foreach (TypeSpec type in types)
            {
                emitter.Writer.WriteBlockStart($"if (type == typeof({type.MinimalDisplayString}))");

                if (type.InitializationStrategy is InitializationStrategy.None || !emitter.EmitInitException(type))
                {
                    emitter.EmitBindLogicFromRootMethod(type, Identifier.obj, InitializationKind.Declaration);
                    emitter.Writer.WriteLine($"return {Identifier.obj};");
                }

                emitter.Writer.WriteBlockEnd();
                emitter.Writer.WriteBlankLine();
            }

            Emit_NotSupportedException_TypeNotDetectedAsInput(emitter);
            emitter.Writer.WriteBlockEnd();
            emitter.PrecedingBlockExists = true;
        }

        private void EmitGetValueCoreMethod(Emitter emitter)
        {
            if (!TypesForCoreBindingMethodGen.TryGetValue(MethodSpecifier.GetValueCore, out HashSet<TypeSpec>? targetTypes))
            {
                return;
            }

            emitter.EmitBlankLineIfRequired();

            emitter.Writer.WriteBlockStart($"public static object? {Identifier.GetValueCore}(this {Identifier.IConfiguration} {Identifier.configuration}, Type {Identifier.type}, string {Identifier.key})");

            emitter.EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

            emitter.Writer.WriteLine($"{Identifier.IConfigurationSection} {Identifier.section} = {Identifier.configuration}.{Identifier.GetSection}({Identifier.key});");

            emitter.Writer.WriteBlankLine();

            foreach (TypeSpec type in targetTypes)
            {
                ParsableFromStringSpec effectiveType = (ParsableFromStringSpec)((type as NullableSpec)?.UnderlyingType ?? type);
                emitter.Writer.WriteBlockStart($"if ({Identifier.type} == typeof({type.MinimalDisplayString}))");

                emitter.EmitBindLogicFromString(
                        effectiveType,
                        Expression.sectionValue,
                        Expression.sectionPath,
                        writeOnSuccess: (parsedValueExpr) => emitter.Writer.WriteLine($"return {parsedValueExpr};"));

                emitter.Writer.WriteBlockEnd();
                emitter.Writer.WriteBlankLine();
            }

            emitter.Writer.WriteLine("return null;");
            emitter.Writer.WriteBlockEnd();
            emitter.PrecedingBlockExists = true;
        }

        private void EmitBindCoreUntypedMethod(Emitter emitter)
        {
            if (!TypesForCoreBindingMethodGen.TryGetValue(MethodSpecifier.BindCoreUntyped, out HashSet<TypeSpec>? targetTypes))
            {
                return;
            }

            emitter.EmitBlankLineIfRequired();

            emitter.Writer.WriteBlockStart($"public static void {Identifier.BindCoreUntyped}(this {Identifier.IConfiguration} {Identifier.configuration}, object {Identifier.obj}, Type {Identifier.type}, {MinimalDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions})");

            emitter.EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

            emitter.Writer.WriteLine($"{Identifier.BinderOptions}? {Identifier.binderOptions} = {Identifier.GetBinderOptions}({Identifier.configureOptions});");
            emitter.Writer.WriteBlankLine();

            emitter.EmitIConfigurationHasValueOrChildrenCheck(voidReturn: true);

            foreach (TypeSpec type in targetTypes)
            {
                emitter.Writer.WriteBlockStart($"if (type == typeof({type.MinimalDisplayString}))");

                if (type.InitializationStrategy is InitializationStrategy.None || !emitter.EmitInitException(type))
                {
                    emitter.Writer.WriteLine($"var {Identifier.temp} = ({type.MinimalDisplayString}){Identifier.obj};");
                    emitter.EmitBindLogicFromRootMethod(type, Identifier.temp, InitializationKind.None);
                    emitter.Writer.WriteLine($"return;");
                }

                emitter.Writer.WriteBlockEnd();
                emitter.Writer.WriteBlankLine();
            }

            Emit_NotSupportedException_TypeNotDetectedAsInput(emitter);
            emitter.Writer.WriteBlockEnd();
            emitter.PrecedingBlockExists = true;
        }

        private void EmitBindCoreMethods(Emitter emitter)
        {
            if (!TypesForCoreBindingMethodGen.TryGetValue(MethodSpecifier.BindCore, out HashSet<TypeSpec>? targetTypes))
            {
                return;
            }

            foreach (TypeSpec type in targetTypes)
            {
                if (type.SpecKind is TypeSpecKind.ParsableFromString)
                {
                    continue;
                }

                emitter.EmitBlankLineIfRequired();
                EmitBindCoreMethod(emitter, type);
            }
        }

        private static void EmitBindCoreMethod(Emitter emitter, TypeSpec type)
        {
            //Debug.Assert(type.CanInitialize);
            if (!type.CanInitialize)
            {
                return;
            }

            string objParameterExpression = $"ref {type.MinimalDisplayString} {Identifier.obj}";
            emitter.Writer.WriteBlockStart(@$"public static void {Identifier.BindCore}({Identifier.IConfiguration} {Identifier.configuration}, {objParameterExpression}, {Identifier.BinderOptions}? {Identifier.binderOptions})");
            EmitBindCoreImpl(emitter, type);
            emitter.Writer.WriteBlockEnd();
        }

        private void EmitInitializeMethods(Emitter emitter)
        {
            if (!TypesForCoreBindingMethodGen.TryGetValue(MethodSpecifier.Initialize, out HashSet<TypeSpec>? targetTypes))
            {
                return;
            }

            foreach (ObjectSpec type in targetTypes)
            {
                emitter.EmitBlankLineIfRequired();
                EmitInitializeMethod(emitter, type);
            }
        }

        private static void EmitInitializeMethod(Emitter emitter, ObjectSpec type)
        {
            Debug.Assert(type.CanInitialize);

            List<ParameterSpec> ctorParams = type.ConstructorParameters;
            IEnumerable<PropertySpec> initOnlyProps = type.Properties.Values.Where(prop => prop.SetOnInit);
            string displayString = type.MinimalDisplayString;

            emitter.Writer.WriteBlockStart($"public static {displayString} {type.InitializeMethodDisplayString}({Identifier.IConfiguration} {Identifier.configuration}, {Identifier.BinderOptions}? {Identifier.binderOptions})");

            foreach (ParameterSpec parameter in ctorParams)
            {
                if (!parameter.HasExplicitDefaultValue)
                {
                    emitter.Writer.WriteLine($@"({parameter.Type.MinimalDisplayString} {Identifier.Value}, bool {Identifier.HasConfig}) {parameter.Name} = ({parameter.DefaultValue}, false);");
                }
                else
                {
                    emitter.Writer.WriteLine($@"{parameter.Type.MinimalDisplayString} {parameter.Name} = {parameter.DefaultValue};");
                }
            }

            foreach (PropertySpec property in initOnlyProps)
            {
                if (property.MatchingCtorParam is null)
                {
                    emitter.Writer.WriteLine($@"{property.Type.MinimalDisplayString} {property.Name} = default!;");
                }
            }

            emitter.Writer.WriteBlankLine();

            emitter.Writer.WriteBlock($$"""
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

            EmitSwitchDefault(emitter, "continue;", addBreak: false);

            emitter.Writer.WriteBlockEnd();
            emitter.Writer.WriteBlockEnd();

            emitter.PrecedingBlockExists = true;

            foreach (ParameterSpec parameter in ctorParams)
            {
                if (!parameter.HasExplicitDefaultValue)
                {
                    string parameterName = parameter.Name;

                    emitter.EmitBlankLineIfRequired();
                    emitter.Writer.WriteBlock($$"""
                        if (!{{parameterName}}.{{Identifier.HasConfig}})
                        {
                            throw new {{emitter.GetInvalidOperationDisplayName()}}("{{string.Format(ExceptionMessages.ParameterHasNoMatchingConfig, type.Name, parameterName)}}");
                        }
                        """);
                }
            }

            emitter.EmitBlankLineIfRequired();

            string returnExpression = $"return new {displayString}({string.Join(", ", argumentList)})";
            if (!initOnlyProps.Any())
            {
                emitter.Writer.WriteLine($"{returnExpression};");
            }
            else
            {
                emitter.Writer.WriteBlockStart(returnExpression);
                foreach (PropertySpec property in initOnlyProps)
                {
                    string propertyName = property.Name;
                    string initValue = propertyName + (property.MatchingCtorParam is null or ParameterSpec { HasExplicitDefaultValue: true } ? string.Empty : $".{Identifier.Value}");
                    emitter.Writer.WriteLine($@"{propertyName} = {initValue},");
                }
                emitter.Writer.WriteBlockEnd(";");
            }

            // End method.
            emitter.Writer.WriteBlockEnd();

            void EmitMemberBindLogic(string memberName, TypeSpec memberType, string configurationKeyName, bool configValueMustExist = false)
            {
                string lhs = memberName + (configValueMustExist ? $".{Identifier.Value}" : string.Empty);

                emitter.Writer.WriteLine($@"case ""{configurationKeyName}"":");
                emitter.Writer.IndentationLevel++;
                emitter.Writer.WriteBlockStart();

                EmitMemberBindLogicCore(memberType, lhs);

                if (configValueMustExist)
                {
                    emitter.Writer.WriteLine($"{memberName}.{Identifier.HasConfig} = true;");
                }

                emitter.Writer.WriteBlockEnd();
                emitter.Writer.WriteLine("break;");
                emitter.Writer.IndentationLevel--;

                void EmitMemberBindLogicCore(TypeSpec type, string lhs)
                {
                    TypeSpecKind kind = type.SpecKind;

                    if (kind is TypeSpecKind.Nullable)
                    {
                        EmitMemberBindLogicCore(((NullableSpec)type).UnderlyingType, lhs);
                    }
                    else if (type is ParsableFromStringSpec stringParsableType)
                    {
                        emitter.EmitBindLogicFromString(
                            stringParsableType,
                            Expression.sectionValue,
                            Expression.sectionPath,
                            (parsedValueExpr) => emitter.Writer.WriteLine($"{lhs} = {parsedValueExpr}!;"));
                    }
                    else if (!emitter.EmitInitException(type))
                    {
                        emitter.EmitBindCoreCall(type, lhs, Identifier.section, InitializationKind.SimpleAssignment);
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

        private void EmitHelperMethods(Emitter emitter)
        {
            if (ShouldEmitMethods(MethodSpecifier.BindCoreUntyped | MethodSpecifier.GetCore))
            {
                emitter.Writer.WriteBlankLine();
                EmitHasValueOrChildrenMethod(emitter);
                emitter.Writer.WriteBlankLine();
                EmitHasChildrenMethod(emitter);
                emitter.PrecedingBlockExists = true;
            }
            else if (ShouldEmitHasChildren)
            {
                emitter.Writer.WriteBlankLine();
                EmitHasChildrenMethod(emitter);
                emitter.PrecedingBlockExists = true;
            }

            if (ShouldEmitMethods(
                MethodSpecifier.BindCoreUntyped | MethodSpecifier.GetCore) ||
                emitter.SourceGenSpec.ConfigBinderSpec.ShouldEmitMethods(ConfigBinderMethodSpec.MethodSpecifier.Bind_instance_BinderOptions))
            {
                emitter.Writer.WriteBlankLine();
                EmitGetBinderOptionsHelper(emitter);
                emitter.PrecedingBlockExists = true;
            }

            foreach (ParsableFromStringSpec type in PrimitivesForHelperGen)
            {
                emitter.EmitBlankLineIfRequired();
                EmitPrimitiveParseMethod(emitter, type);
            }
        }

        private static void EmitHasValueOrChildrenMethod(Emitter emitter)
        {
            emitter.Writer.WriteBlock($$"""
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

        private static void EmitHasChildrenMethod(Emitter emitter)
        {
            emitter.Writer.WriteBlock($$"""
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

        private static void EmitGetBinderOptionsHelper(Emitter emitter)
        {
            emitter.Writer.WriteBlock($$"""
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

        private static void EmitPrimitiveParseMethod(Emitter emitter, ParsableFromStringSpec type)
        {
            string innerExceptionTypeDisplayString;
            string cultureInfoTypeDisplayString;
            string numberStylesTypeDisplayString;

            if (emitter.UseFullyQualifiedNames)
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

            emitter.Writer.WriteBlock($$"""
                    public static {{typeDisplayString}} {{type.ParseMethodName}}(string {{Identifier.stringValue}}, Func<string?> {{Identifier.getPath}})
                    {
                        try
                        {
                            return {{expressionForParsedValue}};
                    """);

            string exceptionArg1 = string.Format(ExceptionMessages.FailedBinding, $"{{{Identifier.getPath}()}}", $"{{typeof({typeDisplayString})}}");

            emitter.Writer.WriteBlock($$"""
                        }
                        catch ({{innerExceptionTypeDisplayString}} {{Identifier.exception}})
                        {
                            throw new {{emitter.GetInvalidOperationDisplayName()}}($"{{exceptionArg1}}", {{Identifier.exception}});
                        }
                    }
                    """);
        }

        private static void EmitBindCoreImpl(Emitter emitter, TypeSpec type)
        {
            switch (type.SpecKind)
            {
                case TypeSpecKind.Enumerable:
                case TypeSpecKind.Dictionary:
                case TypeSpecKind.Object:
                    {
                        Debug.Assert(type.CanInitialize);
                        emitter.EmitCheckForNullArgument_WithBlankLine_IfRequired(type.IsValueType);
                        EmitBindCoreImplForComplexType(emitter, type);
                    }
                    break;
                case TypeSpecKind.Nullable:
                    {
                        EmitBindCoreImpl(emitter, ((NullableSpec)type).UnderlyingType);
                    }
                    break;
                case TypeSpecKind.IConfigurationSection:
                    {
                        emitter.EmitCastToIConfigurationSection();
                        emitter.Writer.WriteLine($"{Identifier.obj} = {Identifier.section};");
                    }
                    break;
                default:
                    Debug.Fail("Invalid type kind", type.SpecKind.ToString());
                    break;
            }
        }

        private static void EmitBindCoreImplForComplexType(Emitter emitter, TypeSpec type)
        {
            if (type.InitializationStrategy is InitializationStrategy.Array)
            {
                EmitPopulationImplForArray(emitter, (EnumerableSpec)type);
            }
            else if (type is EnumerableSpec enumerable)
            {
                EmitPopulationImplForEnumerableWithAdd(emitter, enumerable);
            }
            else if (type is DictionarySpec dictionary)
            {
                EmitBindCoreImplForDictionary(emitter, dictionary);
            }
            else
            {
                EmitBindCoreImplForObject(emitter, (ObjectSpec)type);
            }
        }

        private static void EmitPopulationImplForArray(Emitter emitter, EnumerableSpec type)
        {
            EnumerableSpec concreteType = (EnumerableSpec)type.ConcreteType;

            // Create, bind, and add elements to temp list.
            string tempVarName = emitter.GetIncrementalVarName(Identifier.temp);
            emitter.EmitBindCoreCall(concreteType, tempVarName, Identifier.configuration, InitializationKind.Declaration);

            // Resize array and copy additional elements.
            emitter.Writer.WriteBlock($$"""
                    {{Identifier.Int32}} {{Identifier.originalCount}} = {{Identifier.obj}}.{{Identifier.Length}};
                    {{Identifier.Array}}.{{Identifier.Resize}}(ref {{Identifier.obj}}, {{Identifier.originalCount}} + {{tempVarName}}.{{Identifier.Count}});
                    {{tempVarName}}.{{Identifier.CopyTo}}({{Identifier.obj}}, {{Identifier.originalCount}});
                    """);
        }

        private static void EmitPopulationImplForEnumerableWithAdd(Emitter emitter, EnumerableSpec type)
        {
            EmitCollectionCastIfRequired(emitter, type, out string objIdentifier);

            emitter.Writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");

            TypeSpec elementType = type.ElementType;

            if (elementType is ParsableFromStringSpec stringParsableType)
            {
                emitter.EmitBindLogicFromString(
                    stringParsableType,
                    Expression.sectionValue,
                    Expression.sectionPath,
                    (parsedValueExpr) => emitter.Writer.WriteLine($"{objIdentifier}.{Identifier.Add}({parsedValueExpr}!);"),
                    isCollectionElement: true);
            }
            else
            {
                emitter.EmitBindCoreCall(elementType, Identifier.element, Identifier.section, InitializationKind.Declaration);
                emitter.Writer.WriteLine($"{objIdentifier}.{Identifier.Add}({Identifier.element});");
            }

            emitter.Writer.WriteBlockEnd();
        }

        private static void EmitBindCoreImplForDictionary(Emitter emitter, DictionarySpec type)
        {
            EmitCollectionCastIfRequired(emitter, type, out string objIdentifier);

            emitter.Writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");

            ParsableFromStringSpec keyType = type.KeyType;
            TypeSpec elementType = type.ElementType;

            // Parse key
            emitter.EmitBindLogicFromString(
                    keyType,
                    Expression.sectionKey,
                    Expression.sectionPath,
                    Emit_BindAndAddLogic_ForElement);

            void Emit_BindAndAddLogic_ForElement(string parsedKeyExpr)
            {
                if (elementType is ParsableFromStringSpec stringParsableElementType)
                {
                    emitter.EmitBindLogicFromString(
                        stringParsableElementType,
                        Expression.sectionValue,
                        Expression.sectionPath,
                        (parsedValueExpr) => emitter.Writer.WriteLine($"{objIdentifier}[{parsedKeyExpr}!] = {parsedValueExpr}!;"),
                        isCollectionElement: true);
                }
                else // For complex types:
                {
                    Debug.Assert(elementType.CanInitialize);

                    parsedKeyExpr += "!";
                    if (keyType.StringParsableTypeKind is not StringParsableTypeKind.ConfigValue)
                    {
                        // Save value to local to avoid parsing twice - during look-up and during add.
                        emitter.Writer.WriteLine($"{keyType.MinimalDisplayString} {Identifier.key} = {parsedKeyExpr};");
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

                    emitter.Writer.WriteBlockStart($"if (!({conditionToUseExistingElement}))");
                    emitter.EmitObjectInit(elementType, Identifier.element, InitializationKind.SimpleAssignment);
                    emitter.Writer.WriteBlockEnd();

                    if (elementType is CollectionSpec { InitializationStrategy: InitializationStrategy.ParameterizedConstructor or InitializationStrategy.ToEnumerableMethod } collectionSpec)
                    {
                        // This is a read-only collection. If the element exists and is not null,
                        // we need to copy its contents into a new instance & then append/bind to that.

                        string initExpression = collectionSpec.InitializationStrategy is InitializationStrategy.ParameterizedConstructor
                            ? $"new {collectionSpec.ConcreteType.MinimalDisplayString}({Identifier.element})"
                            : $"{Identifier.element}.{collectionSpec.ToEnumerableMethodCall!}";

                        emitter.Writer.WriteBlock($$"""
                                else
                                {
                                    {{Identifier.element}} = {{initExpression}};
                                }
                                """);
                    }

                    emitter.EmitBindCoreCall(elementType, $"{Identifier.element}!", Identifier.section, InitializationKind.None);
                    emitter.Writer.WriteLine($"{objIdentifier}[{parsedKeyExpr}] = {Identifier.element};");
                }

            }

            emitter.Writer.WriteBlockEnd();
        }

        private static void EmitBindCoreImplForObject(Emitter emitter, ObjectSpec type)
        {
            if (type.Properties.Count == 0)
            {
                return;
            }

            string listOfStringDisplayName = "List<string>";
            emitter.Writer.WriteLine($"{listOfStringDisplayName}? {Identifier.temp} = null;");

            emitter.Writer.WriteBlockStart($"foreach ({Identifier.IConfigurationSection} {Identifier.section} in {Identifier.configuration}.{Identifier.GetChildren}())");
            emitter.Writer.WriteBlockStart($"switch ({Expression.sectionKey})");

            foreach (PropertySpec property in type.Properties.Values)
            {
                emitter.Writer.WriteLine($@"case ""{property.ConfigurationKeyName}"":");
                emitter.Writer.IndentationLevel++;
                emitter.Writer.WriteBlockStart();

                bool success = true;
                if (property.ShouldBind())
                {
                    success = EmitBindCoreImplForProperty(emitter, property, property.Type, parentType: type);
                }

                emitter.Writer.WriteBlockEnd();

                if (success)
                {
                    emitter.Writer.WriteLine("break;");
                }

                emitter.Writer.IndentationLevel--;
            }

            EmitSwitchDefault(emitter, $$"""
                if ({{Identifier.binderOptions}}?.ErrorOnUnknownConfiguration == true)
                {
                    ({{Identifier.temp}} ??= new {{listOfStringDisplayName}}()).Add($"'{{{Expression.sectionKey}}}'");
                }
                """);

            // End switch on config child key.
            emitter.Writer.WriteBlockEnd();

            // End foreach on config.GetChildren().
            emitter.Writer.WriteBlockEnd();

            emitter.Writer.WriteBlankLine();

            string exceptionMessage = string.Format(ExceptionMessages.MissingConfig, Identifier.ErrorOnUnknownConfiguration, Identifier.BinderOptions, $"{{typeof({type.MinimalDisplayString})}}", $@"{{string.Join("", "", {Identifier.temp})}}");
            emitter.Writer.WriteBlock($$"""
                    if ({{Identifier.temp}} is not null)
                    {
                        throw new InvalidOperationException($"{{exceptionMessage}}");
                    }
                    """);

        }

        private static bool EmitBindCoreImplForProperty(Emitter emitter, PropertySpec property, TypeSpec propertyType, TypeSpec parentType)
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
                            emitter.EmitBindLogicFromString(
                                stringParsableType,
                                expressionForConfigValueIndexer,
                                Expression.sectionPath,
                                (parsedValueExpr) => emitter.Writer.WriteLine($"{expressionForPropertyAccess} = {parsedValueExpr}!;"));
                        }
                    }
                    break;
                case TypeSpecKind.IConfigurationSection:
                    {
                        emitter.Writer.WriteLine($"{expressionForPropertyAccess} = {Identifier.section};");
                    }
                    break;
                case TypeSpecKind.Nullable:
                    {
                        TypeSpec underlyingType = ((NullableSpec)propertyType).UnderlyingType;
                        EmitBindCoreImplForProperty(emitter, property, underlyingType, parentType);
                    }
                    break;
                default:
                    {
                        if (emitter.EmitInitException(propertyType))
                        {
                            return false;
                        }

                        EmitBindCoreCallForProperty(emitter, property, propertyType, expressionForPropertyAccess);
                    }
                    break;
            }

            return true;
        }

        private static void EmitBindCoreCallForProperty(
            Emitter emitter,
            PropertySpec property,
            TypeSpec effectivePropertyType,
            string expressionForPropertyAccess)
        {
            emitter.Writer.WriteBlockStart($"if ({Identifier.HasChildren}({Identifier.section}))");

            bool canGet = property.CanGet;
            bool canSet = property.CanSet;
            string effectivePropertyTypeDisplayString = effectivePropertyType.MinimalDisplayString;

            string tempVarName = emitter.GetIncrementalVarName(Identifier.temp);
            if (effectivePropertyType.IsValueType)
            {
                if (canSet)
                {
                    if (canGet)
                    {
                        TypeSpec actualPropertyType = property.Type;
                        if (actualPropertyType.SpecKind is TypeSpecKind.Nullable)
                        {
                            string nullableTempVarName = emitter.GetIncrementalVarName(Identifier.temp);

                            emitter.Writer.WriteLine($"{actualPropertyType.MinimalDisplayString} {nullableTempVarName} = {expressionForPropertyAccess};");

                            emitter.Writer.WriteLine(
                                $"{effectivePropertyTypeDisplayString} {tempVarName} = {nullableTempVarName}.{Identifier.HasValue} ? {nullableTempVarName}.{Identifier.Value} : new {effectivePropertyTypeDisplayString}();");
                        }
                        else
                        {
                            emitter.Writer.WriteLine($"{effectivePropertyTypeDisplayString} {tempVarName} = {expressionForPropertyAccess};");
                        }
                    }
                    else
                    {
                        emitter.EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.Declaration);
                    }

                    emitter.Writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");
                    emitter.Writer.WriteLine($"{expressionForPropertyAccess} = {tempVarName};");
                }
            }
            else
            {
                if (canGet)
                {
                    emitter.Writer.WriteLine($"{effectivePropertyTypeDisplayString} {tempVarName} = {expressionForPropertyAccess};");
                    emitter.EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.AssignmentWithNullCheck);
                    emitter.Writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");

                    if (canSet)
                    {
                        emitter.Writer.WriteLine($"{expressionForPropertyAccess} = {tempVarName};");
                    }
                }
                else
                {
                    Debug.Assert(canSet);
                    emitter.EmitObjectInit(effectivePropertyType, tempVarName, InitializationKind.Declaration);
                    emitter.Writer.WriteLine($@"{Identifier.BindCore}({Identifier.section}, ref {tempVarName}, {Identifier.binderOptions});");
                    emitter.Writer.WriteLine($"{expressionForPropertyAccess} = {tempVarName};");
                }
            }

            emitter.Writer.WriteBlockEnd();
        }

        private static void EmitCollectionCastIfRequired(Emitter emitter, CollectionSpec type, out string objIdentifier)
        {
            objIdentifier = Identifier.obj;
            if (type.PopulationStrategy is CollectionPopulationStrategy.Cast_Then_Add)
            {
                objIdentifier = Identifier.temp;
                emitter.Writer.WriteBlock($$"""
                        if ({{Identifier.obj}} is not {{type.PopulationCastType!.MinimalDisplayString}} {{objIdentifier}})
                        {
                            return;
                        }
                        """);
                emitter.Writer.WriteBlankLine();
            }
        }

        private static void EmitSwitchDefault(Emitter emitter, string caseLogic, bool addBreak = true)
        {
            emitter.Writer.WriteLine("default:");
            emitter.Writer.IndentationLevel++;
            emitter.Writer.WriteBlockStart();
            emitter.Writer.WriteBlock(caseLogic);
            emitter.Writer.WriteBlockEnd();

            if (addBreak)
            {
                emitter.Writer.WriteLine("break;");
            }

            emitter.Writer.IndentationLevel--;
        }

        private static void Emit_NotSupportedException_TypeNotDetectedAsInput(Emitter emitter) =>
            emitter.Writer.WriteLine(@$"throw new global::System.NotSupportedException($""{string.Format(ExceptionMessages.TypeNotDetectedAsInput, "{type}")}"");");
    }
}
