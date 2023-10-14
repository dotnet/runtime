// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Contains the data related to a GeneratedComInterfaceAttribute, without references to Roslyn symbols.
    /// See <seealso cref="GeneratedComInterfaceCompilationData"/> for a type with a reference to the StringMarshallingCustomType
    /// </summary>
    internal sealed record GeneratedComInterfaceData : InteropAttributeData
    {
        public ComInterfaceOptions Options { get; init; }
        public static GeneratedComInterfaceData From(GeneratedComInterfaceCompilationData generatedComInterfaceAttr)
            => new GeneratedComInterfaceData() with
            {
                IsUserDefined = generatedComInterfaceAttr.IsUserDefined,
                SetLastError = generatedComInterfaceAttr.SetLastError,
                StringMarshalling = generatedComInterfaceAttr.StringMarshalling,
                StringMarshallingCustomType = generatedComInterfaceAttr.StringMarshallingCustomType is not null
                    ? ManagedTypeInfo.CreateTypeInfoForTypeSymbol(generatedComInterfaceAttr.StringMarshallingCustomType)
                    : null,
                Options = generatedComInterfaceAttr.Options
            };
    }

    /// <summary>
    /// Contains the data related to a GeneratedComInterfaceAttribute, with references to Roslyn symbols.
    /// Use <seealso cref="GeneratedComInterfaceData"/> instead when using for incremental compilation state to avoid keeping a compilation alive
    /// </summary>
    internal sealed record GeneratedComInterfaceCompilationData : InteropAttributeCompilationData
    {
        public ComInterfaceOptions Options { get; init; } = ComInterfaceOptions.ManagedObjectWrapper | ComInterfaceOptions.ComObjectWrapper;

        public static bool TryGetGeneratedComInterfaceAttributeFromInterface(INamedTypeSymbol interfaceSymbol, [NotNullWhen(true)] out AttributeData? generatedComInterfaceAttribute)
        {
            generatedComInterfaceAttribute = null;
            foreach (var attr in interfaceSymbol.GetAttributes())
            {
                if (generatedComInterfaceAttribute is null
                    && attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute)
                {
                    generatedComInterfaceAttribute = attr;
                }
            }
            return generatedComInterfaceAttribute is not null;
        }

        public static GeneratedComInterfaceCompilationData GetAttributeDataFromInterfaceSymbol(INamedTypeSymbol interfaceSymbol)
        {
            bool found = TryGetGeneratedComInterfaceAttributeFromInterface(interfaceSymbol, out var attr);
            Debug.Assert(found);
            return GetDataFromAttribute(attr);
        }

        public static GeneratedComInterfaceCompilationData GetDataFromAttribute(AttributeData attr)
        {
            Debug.Assert(attr.AttributeClass.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute);
            var generatedComInterfaceAttributeData = new GeneratedComInterfaceCompilationData();
            var args = attr.NamedArguments.ToImmutableDictionary();
            generatedComInterfaceAttributeData = generatedComInterfaceAttributeData.WithValuesFromNamedArguments(args);
            if (args.TryGetValue(nameof(Options), out var options))
            {
                generatedComInterfaceAttributeData = generatedComInterfaceAttributeData with
                {
                    Options = (ComInterfaceOptions)options.Value
                };
            }
            return generatedComInterfaceAttributeData;
        }
    }
}
