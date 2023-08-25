// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Parses information for all use-site attributes and tracks their usage.
    /// </summary>
    public sealed class UseSiteAttributeProvider
    {
        private readonly ImmutableDictionary<int, UseSiteAttributeData> _useSiteAttributesByIndirectionDepth;
        private readonly int _maxIndirectionLevelDataProvided;
        private readonly GeneratorDiagnosticsBag _diagnostics;
        private int _maxIndirectionLevelUsed;

        /// <summary>
        /// Construct a new <see cref="UseSiteAttributeProvider"/> for a given usage site.
        /// </summary>
        /// <param name="useSiteAttributeParsers">The parsers for the attributes at the given usage site.</param>
        /// <param name="useSiteAttributes">The attributes at the usage site.</param>
        /// <param name="elementInfoProvider">The provider for additional element information, used by the attribute parsers.</param>
        /// <param name="diagnostics">Diagnostics sink for any invalid configurations.</param>
        /// <param name="getMarshallingInfoCallback">A callback to get marshalling information for other elements. Used by <paramref name="elementInfoProvider"/>.</param>
        internal UseSiteAttributeProvider(
            ImmutableArray<IUseSiteAttributeParser> useSiteAttributeParsers,
            IEnumerable<AttributeData> useSiteAttributes,
            IElementInfoProvider elementInfoProvider,
            GeneratorDiagnosticsBag diagnostics,
            GetMarshallingInfoCallback getMarshallingInfoCallback)
        {
            ImmutableDictionary<int, UseSiteAttributeData>.Builder useSiteAttributesByIndirectionDepth = ImmutableDictionary.CreateBuilder<int, UseSiteAttributeData>();
            _maxIndirectionLevelDataProvided = 0;
            foreach (AttributeData attribute in useSiteAttributes)
            {
                UseSiteAttributeData? useSiteAttributeData = GetUseSiteInfoForAttribute(attribute);
                if (useSiteAttributeData is not null)
                {
                    int indirectionDepth = useSiteAttributeData.IndirectionDepth;
                    if (useSiteAttributesByIndirectionDepth.ContainsKey(indirectionDepth))
                    {
                        diagnostics.ReportInvalidMarshallingAttributeInfo(attribute, nameof(SR.DuplicateMarshallingInfo), indirectionDepth.ToString());
                    }
                    else
                    {
                        useSiteAttributesByIndirectionDepth.Add(indirectionDepth, useSiteAttributeData);
                        _maxIndirectionLevelDataProvided = Math.Max(_maxIndirectionLevelDataProvided, indirectionDepth);
                    }
                }
            }
            _useSiteAttributesByIndirectionDepth = useSiteAttributesByIndirectionDepth.ToImmutable();
            _diagnostics = diagnostics;

            UseSiteAttributeData? GetUseSiteInfoForAttribute(AttributeData attribute)
            {
                foreach (var parser in useSiteAttributeParsers)
                {
                    // Automatically ignore invalid attributes.
                    // The compiler will already error on them.
                    if (attribute.AttributeConstructor is not null && parser.CanParseAttributeType(attribute.AttributeClass))
                    {
                        return parser.ParseAttribute(attribute, elementInfoProvider, getMarshallingInfoCallback);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Get the <see cref="UseSiteAttributeData"/> provided for a given <paramref name="indirectionDepth"/>, if it exists.
        /// </summary>
        /// <param name="indirectionDepth">The indirection depth to retrieve info for.</param>
        /// <param name="useSiteInfo">The use site information, if it exists.</param>
        /// <returns><c>true</c> if an attribute was provided for the given indirection depth.</returns>
        public bool TryGetUseSiteAttributeInfo(int indirectionDepth, out UseSiteAttributeData useSiteInfo)
        {
            _maxIndirectionLevelUsed = Math.Max(indirectionDepth, _maxIndirectionLevelUsed);
            return _useSiteAttributesByIndirectionDepth.TryGetValue(indirectionDepth, out useSiteInfo);
        }

        /// <summary>
        /// Call when no more of the use-site attribute information will be used.
        /// Records any information or diagnostics about unused marshalling information.
        /// </summary>
        internal void OnAttributeUsageFinished()
        {
            if (_maxIndirectionLevelUsed < _maxIndirectionLevelDataProvided)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(
                    _useSiteAttributesByIndirectionDepth[_maxIndirectionLevelDataProvided].AttributeData,
                    nameof(SR.ExtraneousMarshallingInfo),
                    _maxIndirectionLevelDataProvided.ToString(),
                    _maxIndirectionLevelUsed.ToString());
            }
        }
    }
}
