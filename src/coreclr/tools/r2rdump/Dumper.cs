// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILCompiler.Reflection.ReadyToRun;

namespace R2RDump
{
    internal abstract class Dumper
    {
        protected readonly ReadyToRunReader _r2r;
        protected TextWriter _writer;
        protected readonly Disassembler _disassembler;
        protected readonly DumpModel _model;

        public Dumper(ReadyToRunReader r2r, TextWriter writer, Disassembler disassembler, DumpModel model)
        {
            _r2r = r2r;
            _writer = writer;
            _disassembler = disassembler;
            _model = model;
        }

        public IEnumerable<ReadyToRunSection> NormalizedSections(ReadyToRunCoreHeader header)
        {
            IEnumerable<ReadyToRunSection> sections = header.Sections.Values;
            if (_model.Normalize)
            {
                sections = sections.OrderBy(s => s.Type);
            }
            return sections;
        }

        public IEnumerable<ReadyToRunMethod> NormalizedMethods()
        {
            IEnumerable<ReadyToRunMethod> methods = _r2r.Methods;
            if (_model.Normalize)
            {
                methods = methods.OrderBy(m => m.SignatureString);
            }
            return methods;
        }

        /// <summary>
        /// Run right before printing output
        /// </summary>
        public abstract void Begin();

        /// <summary>
        /// Run right after printing output
        /// </summary>
        public abstract void End();
        public abstract void WriteDivider(string title);
        public abstract void WriteSubDivider();
        public abstract void SkipLine();
        public abstract void DumpHeader(bool dumpSections);
        public abstract void DumpSection(ReadyToRunSection section);
        public abstract void DumpEntryPoints();
        public abstract void DumpAllMethods();
        public abstract void DumpMethod(ReadyToRunMethod method);
        public abstract void DumpRuntimeFunction(RuntimeFunction rtf);
        public abstract void DumpDisasm(RuntimeFunction rtf, int imageOffset);
        public abstract void DumpBytes(int rva, uint size, string name = "Raw", bool convertToOffset = true);
        public abstract void DumpSectionContents(ReadyToRunSection section);
        public abstract void DumpQueryCount(string q, string title, int count);
        public abstract void DumpFixupStats();

        public TextWriter Writer => _writer;
        public DumpModel Model => _model;
        public ReadyToRunReader Reader => _r2r;
        public Disassembler Disassembler => _disassembler;
    }
}
