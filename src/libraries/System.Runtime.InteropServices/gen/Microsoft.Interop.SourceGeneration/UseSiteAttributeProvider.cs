// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class UseSiteAttributeProvider
    {
        private readonly ImmutableDictionary<int, UseSiteAttributeData> _useSiteAttributesByIndirectionDepth;
        private readonly int _maxIndirectionLevelDataProvided;
        private readonly IGeneratorDiagnostics _diagnostics;
        private int _maxIndirectionLevelUsed;

        public UseSiteAttributeProvider(
            ImmutableArray<IUseSiteAttributeParser> useSiteAttributeParsers,
            IEnumerable<AttributeData> useSiteAttributes,
            IElementInfoProvider elementInfoProvider,
            IGeneratorDiagnostics diagnostics,
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

        public bool TryGetUseSiteAttributeInfo(int indirectionDepth, out UseSiteAttributeData useSiteInfo)
        {
            _maxIndirectionLevelUsed = Math.Max(indirectionDepth, _maxIndirectionLevelUsed);
            return _useSiteAttributesByIndirectionDepth.TryGetValue(indirectionDepth, out useSiteInfo);
        }

        public void OnAttributeUsageFinished()
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
