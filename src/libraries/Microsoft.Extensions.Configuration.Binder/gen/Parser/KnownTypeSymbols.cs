// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed class KnownTypeSymbols
    {
        public CSharpCompilation Compilation { get; }

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
        public INamedTypeSymbol? ConfigurationBinder { get; }
        public INamedTypeSymbol? ConfigurationKeyNameAttribute { get; }
        public INamedTypeSymbol? OptionsBuilderConfigurationExtensions { get; }
        public INamedTypeSymbol? OptionsBuilderOfT { get; }
        public INamedTypeSymbol? OptionsBuilderOfT_Unbound { get; }
        public INamedTypeSymbol? OptionsConfigurationServiceCollectionExtensions { get; }

        public INamedTypeSymbol GenericIList_Unbound { get; }
        public INamedTypeSymbol? GenericICollection_Unbound { get; }
        public INamedTypeSymbol GenericICollection { get; }
        public INamedTypeSymbol GenericIEnumerable_Unbound { get; }
        public INamedTypeSymbol IEnumerable { get; }
        public INamedTypeSymbol? Dictionary { get; }
        public INamedTypeSymbol? GenericIDictionary_Unbound { get; }
        public INamedTypeSymbol? GenericIDictionary { get; }
        public INamedTypeSymbol? HashSet { get; }
        public INamedTypeSymbol? IConfiguration { get; }
        public INamedTypeSymbol? IConfigurationSection { get; }
        public INamedTypeSymbol? IDictionary { get; }
        public INamedTypeSymbol? IReadOnlyCollection_Unbound { get; }
        public INamedTypeSymbol? IReadOnlyDictionary_Unbound { get; }
        public INamedTypeSymbol? IReadOnlyList_Unbound { get; }
        public INamedTypeSymbol? IReadOnlySet_Unbound { get; }
        public INamedTypeSymbol? IServiceCollection { get; }
        public INamedTypeSymbol? ISet_Unbound { get; }
        public INamedTypeSymbol? ISet { get; }
        public INamedTypeSymbol? List { get; }
        public INamedTypeSymbol Enum { get; }
        public INamedTypeSymbol? ArgumentNullException { get; }
        public INamedTypeSymbol? SerializationInfo { get; }
        public INamedTypeSymbol? IntPtr { get; }
        public INamedTypeSymbol? UIntPtr { get; }
        public INamedTypeSymbol? MemberInfo  { get; }
        public INamedTypeSymbol? ParameterInfo { get; }
        public INamedTypeSymbol? Delegate   { get; }

        public KnownTypeSymbols(CSharpCompilation compilation)
        {
            Compilation = compilation;

            // Primitives
            String = compilation.GetSpecialType(SpecialType.System_String);
            CultureInfo = compilation.GetBestTypeByMetadataName(typeof(CultureInfo));
            DateOnly = compilation.GetBestTypeByMetadataName("System.DateOnly");
            DateTimeOffset = compilation.GetBestTypeByMetadataName(typeof(DateTimeOffset));
            Guid = compilation.GetBestTypeByMetadataName(typeof(Guid));
            Half = compilation.GetBestTypeByMetadataName("System.Half");
            Int128 = compilation.GetBestTypeByMetadataName("System.Int128");
            TimeOnly = compilation.GetBestTypeByMetadataName("System.TimeOnly");
            TimeSpan = compilation.GetBestTypeByMetadataName(typeof(TimeSpan));
            UInt128 = compilation.GetBestTypeByMetadataName("System.UInt128");
            Uri = compilation.GetBestTypeByMetadataName(typeof(Uri));
            Version = compilation.GetBestTypeByMetadataName(typeof(Version));

            // Used to verify input configuation binding API calls.
            INamedTypeSymbol? binderOptions = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Configuration.BinderOptions");
            ActionOfBinderOptions = binderOptions is null ? null : compilation.GetBestTypeByMetadataName(typeof(Action<>))?.Construct(binderOptions);
            ConfigurationBinder = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Configuration.ConfigurationBinder");
            ConfigurationKeyNameAttribute = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Configuration.ConfigurationKeyNameAttribute");
            IConfiguration = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Configuration.IConfiguration");
            IConfigurationSection = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Configuration.IConfigurationSection");
            IServiceCollection = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
            OptionsBuilderConfigurationExtensions = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions");
            OptionsBuilderOfT = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Options.OptionsBuilder`1");
            OptionsBuilderOfT_Unbound = OptionsBuilderOfT?.ConstructUnboundGenericType();
            OptionsConfigurationServiceCollectionExtensions = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions");

            // Used to test what kind of collection a type is.
            IEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
            IDictionary = compilation.GetBestTypeByMetadataName(typeof(IDictionary));

            // Used to construct concrete type symbols for generic types, given their type parameters.
            // These concrete types are used to generating instantiation and casting logic in the emitted binding code.
            Dictionary = compilation.GetBestTypeByMetadataName(typeof(Dictionary<,>));
            GenericICollection = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T);
            GenericIDictionary = compilation.GetBestTypeByMetadataName(typeof(IDictionary<,>));
            HashSet = compilation.GetBestTypeByMetadataName(typeof(HashSet<>));
            List = compilation.GetBestTypeByMetadataName(typeof(List<>));
            ISet = compilation.GetBestTypeByMetadataName(typeof(ISet<>));

            // Used for type equivalency checks for unbound generics. The parameters of the types
            // returned by the Roslyn Get*Type* APIs are not unbound, so we construct unbound
            // generics to equal those corresponding to generic types in the input type graphs.
            GenericICollection_Unbound = GenericICollection.ConstructUnboundGenericType();
            GenericIDictionary_Unbound = GenericIDictionary?.ConstructUnboundGenericType();
            GenericIEnumerable_Unbound = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).ConstructUnboundGenericType();
            GenericIList_Unbound = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).ConstructUnboundGenericType();
            IReadOnlyDictionary_Unbound = compilation.GetBestTypeByMetadataName(typeof(IReadOnlyDictionary<,>))?.ConstructUnboundGenericType();
            IReadOnlyCollection_Unbound = compilation.GetBestTypeByMetadataName(typeof(IReadOnlyCollection<>))?.ConstructUnboundGenericType();
            IReadOnlyList_Unbound = compilation.GetBestTypeByMetadataName(typeof(IReadOnlyList<>))?.ConstructUnboundGenericType();
            IReadOnlySet_Unbound = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlySet`1")?.ConstructUnboundGenericType();
            ISet_Unbound = ISet?.ConstructUnboundGenericType();

            // needed to be able to know if a member exist inside the compilation unit
            Enum = compilation.GetSpecialType(SpecialType.System_Enum);
            ArgumentNullException = compilation.GetBestTypeByMetadataName(typeof(ArgumentNullException));

            SerializationInfo = compilation.GetBestTypeByMetadataName(typeof(System.Runtime.Serialization.SerializationInfo));
            MemberInfo = compilation.GetBestTypeByMetadataName(typeof(System.Reflection.MemberInfo));
            ParameterInfo = compilation.GetBestTypeByMetadataName(typeof(System.Reflection.ParameterInfo));
            IntPtr = Compilation.GetSpecialType(SpecialType.System_IntPtr);
            UIntPtr = Compilation.GetSpecialType(SpecialType.System_UIntPtr);
            Delegate = Compilation.GetSpecialType(SpecialType.System_Delegate);
        }
    }
}
