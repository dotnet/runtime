// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ILCompiler.ObjectWriter
{
    internal sealed class WasmSections
    {
        private readonly List<SectionDataEmitter> _sections = new();
        private readonly Dictionary<string, int> _sectionNameToIndex = new();

        public int Count => _sections.Count;

        public IReadOnlyList<SectionDataEmitter> Sections => _sections;

        public SectionDataEmitter this[int sectionIndex] => _sections[sectionIndex];

        public SectionDataEmitter this[string sectionName] => _sections[_sectionNameToIndex[sectionName]];

        public TSection GetSection<TSection>(int sectionIndex)
            where TSection : SectionDataEmitter
        {
            SectionDataEmitter section = _sections[sectionIndex];
            return (TSection)section;
        }

        public TSection GetSection<TSection>(string sectionName)
            where TSection : SectionDataEmitter
        {
            return GetSection<TSection>(_sectionNameToIndex[sectionName]);
        }

        public void Add(string sectionName, int sectionIndex, SectionDataEmitter section)
        {
            Debug.Assert(_sections.Count == sectionIndex);
            _sections.Add(section);
            _sectionNameToIndex.Add(sectionName, sectionIndex);
        }

        public bool Contains(string sectionName) => _sectionNameToIndex.ContainsKey(sectionName);

        public int GetSectionIndex(string sectionName) => _sectionNameToIndex[sectionName];
    }
}
