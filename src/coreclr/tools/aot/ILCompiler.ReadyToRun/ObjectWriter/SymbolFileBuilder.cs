// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ILCompiler.Diagnostics;

namespace ILCompiler.PEWriter
{
    public class SymbolFileBuilder
    {
        private readonly OutputInfoBuilder _outputInfoBuilder;

        public SymbolFileBuilder(OutputInfoBuilder outputInfoBuilder)
        {
            _outputInfoBuilder = outputInfoBuilder;
        }

        public void SavePdb(string pdbPath, string dllFileName)
        {
            Console.WriteLine("Emitting PDB file: {0}", Path.Combine(pdbPath, Path.GetFileNameWithoutExtension(dllFileName) + ".ni.pdb"));

            new PdbWriter(pdbPath, PDBExtraData.None).WritePDBData(dllFileName, _outputInfoBuilder.EnumerateMethods());
        }

        public void SavePerfMap(string perfMapPath, string dllFileName, Guid? perfMapMvid)
        {
            string mvidComponent = (perfMapMvid.HasValue ? perfMapMvid.Value.ToString() : "composite");
            string perfMapFileName = Path.Combine(perfMapPath, Path.GetFileNameWithoutExtension(dllFileName) + ".ni.{" + mvidComponent + "}.map");
            Console.WriteLine("Emitting PerfMap file: {0}", perfMapFileName);
            PerfMapWriter.Write(perfMapFileName, _outputInfoBuilder.EnumerateMethods());
        }
    }
}
