// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingSourceGenerator
    {
        private static DiagnosticDescriptor TypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.TypeNotSupported));
        private static DiagnosticDescriptor AbstractOrInterfaceNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.AbstractOrInterfaceNotSupported));
        private static DiagnosticDescriptor NeedPublicParameterlessConstructor { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.NeedPublicParameterlessConstructor));
        private static DiagnosticDescriptor CollectionNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.CollectionNotSupported));
        private static DiagnosticDescriptor DictionaryKeyNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.DictionaryKeyNotSupported));
        private static DiagnosticDescriptor ElementTypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.ElementTypeNotSupported));
        private static DiagnosticDescriptor MultiDimArraysNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.MultiDimArraysNotSupported));
        private static DiagnosticDescriptor NullableUnderlyingTypeNotSupported { get; } = CreateTypeNotSupportedDescriptor(nameof(SR.NullableUnderlyingTypeNotSupported));

        private static DiagnosticDescriptor PropertyNotSupported { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1101",
            title: new LocalizableResourceString(nameof(SR.PropertyNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.PropertyNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            category: GeneratorProjectName,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor LanguageVersionNotSupported { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1102",
            title: new LocalizableResourceString(nameof(SR.LanguageVersionIsNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.Language_VersionIsNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            category: GeneratorProjectName,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor CreateTypeNotSupportedDescriptor(string nameofLocalizableMessageFormat) =>
            new DiagnosticDescriptor(
            id: "SYSLIB1100",
            title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            messageFormat: new LocalizableResourceString(nameofLocalizableMessageFormat, SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Configuration.Binder.SourceGeneration.SR)),
            category: GeneratorProjectName,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static class Identifier
        {
            public const string configuration = nameof(configuration);
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

            public const string Add = nameof(Add);
            public const string Any = nameof(Any);
            public const string Array = nameof(Array);
            public const string Bind = nameof(Bind);
            public const string BindCore = nameof(BindCore);
            public const string Configure = nameof(Configure);
            public const string CopyTo = nameof(CopyTo);
            public const string ContainsKey = nameof(ContainsKey);
            public const string Count = nameof(Count);
            public const string CultureInfo = nameof(CultureInfo);
            public const string CultureNotFoundException = nameof(CultureNotFoundException);
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

        private static class TypeFullName
        {
            public const string ConfigurationKeyNameAttribute = "Microsoft.Extensions.Configuration.ConfigurationKeyNameAttribute";
            public const string CultureInfo = "System.Globalization.CultureInfo";
            public const string DateOnly = "System.DateOnly";
            public const string DateTimeOffset = "System.DateTimeOffset";
            public const string Dictionary = "System.Collections.Generic.Dictionary`2";
            public const string GenericIDictionary = "System.Collections.Generic.IDictionary`2";
            public const string Guid = "System.Guid";
            public const string Half = "System.Half";
            public const string HashSet = "System.Collections.Generic.HashSet`1";
            public const string IConfiguration = "Microsoft.Extensions.Configuration.IConfiguration";
            public const string IConfigurationSection = "Microsoft.Extensions.Configuration.IConfigurationSection";
            public const string IDictionary = "System.Collections.Generic.IDictionary";
            public const string Int128 = "System.Int128";
            public const string ISet = "System.Collections.Generic.ISet`1";
            public const string IServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
            public const string List = "System.Collections.Generic.List`1";
            public const string TimeOnly = "System.TimeOnly";
            public const string TimeSpan = "System.TimeSpan";
            public const string UInt128 = "System.UInt128";
            public const string Uri = "System.Uri";
            public const string Version = "System.Version";
        }

        private static bool TypesAreEqual(ITypeSymbol first, ITypeSymbol? second)
                => first.Equals(second, SymbolEqualityComparer.Default);
    }
}
