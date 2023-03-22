// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingSourceGenerator
    {
        private const string GeneratorProjectName = "Microsoft.Extensions.Configuration.Binder.SourceGeneration";

        private static DiagnosticDescriptor TypeNotSupported { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1100",
            title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.TypeNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            category: GeneratorProjectName,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor PropertyNotSupported { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1101",
            title: new LocalizableResourceString(nameof(SR.PropertyNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.PropertyNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            category: GeneratorProjectName,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        // Unlike sourcegen warnings, exception messages should not be localized so we keep them in source.
        private static class ExceptionMessages
        {
            public const string TypeNotSupported = "Unable to bind to type '{0}': '{1}'";
        }

        private static class Identifier
        {
            public const string configuration = nameof(configuration);
            public const string element = nameof(element);
            public const string enumValue = nameof(enumValue);
            public const string key = nameof(key);
            public const string obj = nameof(obj);
            public const string originalCount = nameof(originalCount);
            public const string path = nameof(path);
            public const string section = nameof(section);
            public const string services = nameof(services);
            public const string stringValue = nameof(stringValue);
            public const string temp = nameof(temp);

            public const string Add = nameof(Add);
            public const string Any = nameof(Any);
            public const string ArgumentNullException = nameof(ArgumentNullException);
            public const string Array = nameof(Array);
            public const string Bind = nameof(Bind);
            public const string BindCore = nameof(BindCore);
            public const string Configure = nameof(Configure);
            public const string CopyTo = nameof(CopyTo);
            public const string ContainsKey = nameof(ContainsKey);
            public const string Count = nameof(Count);
            public const string Enum = nameof(Enum);
            public const string GeneratedConfigurationBinder = nameof(GeneratedConfigurationBinder);
            public const string Get = nameof(Get);
            public const string GetChildren = nameof(GetChildren);
            public const string GetSection = nameof(GetSection);
            public const string HasChildren = nameof(HasChildren);
            public const string HasValueOrChildren = nameof(HasValueOrChildren);
            public const string HasValue = nameof(HasValue);
            public const string Helpers = nameof(Helpers);
            public const string IConfiguration = nameof(IConfiguration);
            public const string IConfigurationSection = nameof(IConfigurationSection);
            public const string Int32 = "int";
            public const string Length = nameof(Length);
            public const string Parse = nameof(Parse);
            public const string Resize = nameof(Resize);
            public const string TryGetValue = nameof(TryGetValue);
            public const string TryParse = nameof(TryParse);
            public const string Value = nameof(Value);
        }

        private static class NotSupportedReason
        {
            public const string AbstractOrInterfaceNotSupported = "Abstract or interface types are not supported";
            public const string NeedPublicParameterlessConstructor = "Only objects with public parameterless ctors are supported";
            public const string CollectionNotSupported = "The collection type is not supported";
            public const string DictionaryKeyNotSupported = "The dictionary key type is not supported";
            public const string ElementTypeNotSupported = "The collection element type is not supported";
            public const string MultiDimArraysNotSupported = "Multidimensional arrays are not supported.";
            public const string NullableUnderlyingTypeNotSupported = "Nullable underlying type is not supported";
            public const string TypeNotDetectedAsInput = "Generator parser did not detect the type as input";
            public const string TypeNotSupported = "The type is not supported";
        }

        private static class TypeFullName
        {
            public const string ConfigurationKeyNameAttribute = "Microsoft.Extensions.Configuration.ConfigurationKeyNameAttribute";
            public const string Dictionary = "System.Collections.Generic.Dictionary`2";
            public const string GenericIDictionary = "System.Collections.Generic.IDictionary`2";
            public const string HashSet = "System.Collections.Generic.HashSet`1";
            public const string IConfiguration = "Microsoft.Extensions.Configuration.IConfiguration";
            public const string IConfigurationSection = "Microsoft.Extensions.Configuration.IConfigurationSection";
            public const string IDictionary = "System.Collections.Generic.IDictionary";
            public const string ISet = "System.Collections.Generic.ISet`1";
            public const string IServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
            public const string List = "System.Collections.Generic.List`1";
        }

        private static bool TypesAreEqual(ITypeSymbol first, ITypeSymbol second)
                => first.Equals(second, SymbolEqualityComparer.Default);
    }
}
