// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // InputFile
    //
    // InputFile - a symbol that represents an input file, either source
    // code or meta-data, of a file we may read. Its parent is the output
    // file it contributes to. MetaData files have no parent.
    // Should consider splitting in to two classes, one for metadata files 
    // and another for source files.
    // ----------------------------------------------------------------------------

    internal class InputFile : FileRecord
    {
        // Which aliases this INFILE is in. For source INFILESYMs, only bits kaidThisAssembly and kaidGlobal
        // should be set.
        private readonly HashSet<KAID> _bsetFilter;
        private KAID _aid;

        public bool isSource;               // If true, source code, if false, metadata
        // and on the module of added .netmodules

        public InputFile()
        {
            _bsetFilter = new HashSet<KAID>();
        }

        public void SetAssemblyID(KAID aid)
        {
            Debug.Assert(_aid == default(KAID));
            Debug.Assert(KAID.kaidThisAssembly <= aid && aid < KAID.kaidMinModule);

            _aid = aid;
            _bsetFilter.Add(aid);
            if (aid == KAID.kaidThisAssembly)
                _bsetFilter.Add(KAID.kaidGlobal);
        }

        public void AddToAlias(KAID aid)
        {
            Debug.Assert(0 <= aid && aid < KAID.kaidMinModule);

            // NOTE: Anything in this assembly should not be added to other aliases!
            Debug.Assert(_aid > KAID.kaidThisAssembly);
            Debug.Assert(_bsetFilter.Contains(_aid));

            _bsetFilter.Add(aid);
        }

        public void UnionAliasFilter(ref HashSet<KAID> bsetDst)
        {
            bsetDst.UnionWith(_bsetFilter);
        }

        public KAID GetAssemblyID()
        {
            Debug.Assert(_aid >= KAID.kaidThisAssembly);
            return _aid;
        }

        public bool InAlias(KAID aid)
        {
            Debug.Assert(0 <= aid);
            return _bsetFilter.Contains(aid);
        }
    }
}
