// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace ILCompiler.ObjectWriter
{
    internal sealed class WasmSections
    {
        private readonly List<WasmSection> _sections = new();
        private readonly Dictionary<string, int> _sectionNameToIndex = new();

        public int Count => _sections.Count;

        public IReadOnlyList<WasmSection> Sections => _sections;

        public WasmSection this[int sectionIndex] => _sections[sectionIndex];

        public WasmSection this[string sectionName] => _sections[_sectionNameToIndex[sectionName]];

        public void Add(string sectionName, int sectionIndex, WasmSection section)
        {
            Debug.Assert(_sections.Count == sectionIndex);
            _sections.Add(section);
            _sectionNameToIndex.Add(sectionName, sectionIndex);
        }

        public bool Contains(string sectionName) => _sectionNameToIndex.ContainsKey(sectionName);

        public int GetSectionIndex(string sectionName) => _sectionNameToIndex[sectionName];
    }
}
