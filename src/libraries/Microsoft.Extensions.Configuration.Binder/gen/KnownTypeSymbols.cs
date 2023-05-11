// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record KnownTypeSymbols
    {
        public INamedTypeSymbol String { get; }
        public INamedTypeSymbol? CultureInfo { get; }
        public INamedTypeSymbol? DateOnly { get; }
        public INamedTypeSymbol? DateTimeOffset { get; }
        public INamedTypeSymbol? Guid { get; }
        public INamedTypeSymbol? Half { get; }
        public INamedTypeSymbol? Int128 { get; }
        public INamedTypeSymbol? TimeOnly { get; }
        public INamedTypeSymbol? TimeSpan { get; }
        public INamedTypeSymbol? UInt128 { get; }
        public INamedTypeSymbol? Uri { get; }
        public INamedTypeSymbol? Version { get; }

        public INamedTypeSymbol? ActionOfBinderOptions { get; }
        public INamedTypeSymbol? ConfigurationKeyNameAttribute { get; }

        public INamedTypeSymbol GenericIList { get; }
        public INamedTypeSymbol GenericICollection { get; }
        public INamedTypeSymbol GenericICollection_Unconstructed { get; }
        public INamedTypeSymbol GenericIEnumerable { get; }
        public INamedTypeSymbol IEnumerable { get; }
        public INamedTypeSymbol? Dictionary_Unconstructed { get; }
        public INamedTypeSymbol? GenericIDictionary { get; }
        public INamedTypeSymbol? GenericIDictionary_Unconstructed { get; }
        public INamedTypeSymbol? HashSet_Unconstructed { get; }
        public INamedTypeSymbol? IConfiguration { get; }
        public INamedTypeSymbol? IConfigurationSection { get; }
        public INamedTypeSymbol? IDictionary { get; }
        public INamedTypeSymbol? IReadOnlyCollection { get; }
        public INamedTypeSymbol? IReadOnlyDictionary { get; }
        public INamedTypeSymbol? IReadOnlyList { get; }
        public INamedTypeSymbol? IReadOnlySet { get; }
        public INamedTypeSymbol? IServiceCollection { get; }
        public INamedTypeSymbol? ISet { get; }
        public INamedTypeSymbol? ISet_Unconstructed { get; }
        public INamedTypeSymbol? List_Unconstructed { get; }

        public KnownTypeSymbols(CSharpCompilation compilation)
        {
            // Primitives (needed because they are Microsoft.CodeAnalysis.SpecialType.None)
            CultureInfo = compilation.GetBestTypeByMetadataName(TypeFullName.CultureInfo);
            DateOnly = compilation.GetBestTypeByMetadataName(TypeFullName.DateOnly);
            DateTimeOffset = compilation.GetBestTypeByMetadataName(TypeFullName.DateTimeOffset);
            Guid = compilation.GetBestTypeByMetadataName(TypeFullName.Guid);
            Half = compilation.GetBestTypeByMetadataName(TypeFullName.Half);
            Int128 = compilation.GetBestTypeByMetadataName(TypeFullName.Int128);
            TimeOnly = compilation.GetBestTypeByMetadataName(TypeFullName.TimeOnly);
            TimeSpan = compilation.GetBestTypeByMetadataName(TypeFullName.TimeSpan);
            UInt128 = compilation.GetBestTypeByMetadataName(TypeFullName.UInt128);
            Uri = compilation.GetBestTypeByMetadataName(TypeFullName.Uri);
            Version = compilation.GetBestTypeByMetadataName(TypeFullName.Version);

            // Used to verify input configuation binding API calls.
            INamedTypeSymbol? binderOptions = compilation.GetBestTypeByMetadataName(TypeFullName.BinderOptions);
            ActionOfBinderOptions = binderOptions is null ? null : compilation.GetBestTypeByMetadataName(TypeFullName.Action)?.Construct(binderOptions);

            ConfigurationKeyNameAttribute = compilation.GetBestTypeByMetadataName(TypeFullName.ConfigurationKeyNameAttribute);
            IConfiguration = compilation.GetBestTypeByMetadataName(TypeFullName.IConfiguration);
            IConfigurationSection = compilation.GetBestTypeByMetadataName(TypeFullName.IConfigurationSection);
            IServiceCollection = compilation.GetBestTypeByMetadataName(TypeFullName.IServiceCollection);

            // Used to test what kind of collection a type is.
            IEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
            IDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.IDictionary);

            // Used to construct concrete types at runtime; cannot also be constructed.
            Dictionary_Unconstructed = compilation.GetBestTypeByMetadataName(TypeFullName.Dictionary);
            GenericICollection_Unconstructed = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T);
            GenericIDictionary_Unconstructed = compilation.GetBestTypeByMetadataName(TypeFullName.GenericIDictionary);
            HashSet_Unconstructed = compilation.GetBestTypeByMetadataName(TypeFullName.HashSet);
            List_Unconstructed = compilation.GetBestTypeByMetadataName(TypeFullName.List);
            ISet_Unconstructed = compilation.GetBestTypeByMetadataName(TypeFullName.ISet);
            ISet = ISet_Unconstructed?.ConstructUnboundGenericType();

            // Used for type equivalency checks for unbounded generics.
            GenericICollection = GenericICollection_Unconstructed?.ConstructUnboundGenericType();
            GenericIDictionary = GenericIDictionary_Unconstructed?.ConstructUnboundGenericType();
            GenericIEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).ConstructUnboundGenericType();
            GenericIList = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).ConstructUnboundGenericType();
            IReadOnlyDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.IReadOnlyDictionary)?.ConstructUnboundGenericType();
            IReadOnlyCollection = compilation.GetBestTypeByMetadataName(TypeFullName.IReadOnlyCollection)?.ConstructUnboundGenericType();
            IReadOnlyList = compilation.GetBestTypeByMetadataName(TypeFullName.IReadOnlyList)?.ConstructUnboundGenericType();
            IReadOnlySet = compilation.GetBestTypeByMetadataName(TypeFullName.IReadOnlySet)?.ConstructUnboundGenericType();
        }

        private static class TypeFullName
        {
            public const string Action = "System.Action`1";
            public const string BinderOptions = "Microsoft.Extensions.Configuration.BinderOptions";
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
            public const string IReadOnlyCollection = "System.Collections.Generic.IReadOnlyCollection`1";
            public const string IReadOnlyDictionary = "System.Collections.Generic.IReadOnlyDictionary`2";
            public const string IReadOnlyList = "System.Collections.Generic.IReadOnlyList`1";
            public const string IReadOnlySet = "System.Collections.Generic.IReadOnlySet`1";
            public const string ISet = "System.Collections.Generic.ISet`1";
            public const string IServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
            public const string List = "System.Collections.Generic.List`1";
            public const string TimeOnly = "System.TimeOnly";
            public const string TimeSpan = "System.TimeSpan";
            public const string Type = "System.Type";
            public const string UInt128 = "System.UInt128";
            public const string Uri = "System.Uri";
            public const string Version = "System.Version";
        }
    }
}
