// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Buffers.Binary;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Base implementation for ELF and Mach-O object file format writers. Implements
    /// the common code for DWARF debugging and exception handling information.
    /// </summary>
    internal abstract partial class UnixObjectWriter : ObjectWriter
    {
        private sealed record UnixSectionDefinition(string SymbolName, Stream SectionStream);
        private readonly List<UnixSectionDefinition> _sections = new();

        private static readonly ObjectNodeSection LsdaSection = new ObjectNodeSection(".dotnet_eh_table", SectionType.ReadOnly);
        private static readonly ObjectNodeSection EhFrameSection = new ObjectNodeSection(".eh_frame", SectionType.UnwindData);

        protected UnixObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
        }

        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, int sectionIndex, Stream sectionStream)
        {
            if (section.Type != SectionType.Debug &&
                section != LsdaSection &&
                section != EhFrameSection &&
                (comdatName is null || Equals(comdatName, symbolName)))
            {
                // Record code and data sections that can be referenced from debugging information
                _sections.Add(new UnixSectionDefinition(symbolName, sectionStream));
            }
            else
            {
                _sections.Add(null);
            }
        }
    }
}
