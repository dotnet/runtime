// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Internal.TypeSystem;
using ILCompiler.Diagnostics;

namespace ILCompiler.PEWriter
{
    public class SymbolFileBuilder
    {
        private readonly OutputInfoBuilder _outputInfoBuilder;
        private readonly TargetDetails _details;

        public SymbolFileBuilder(OutputInfoBuilder outputInfoBuilder, TargetDetails details)
        {
            _outputInfoBuilder = outputInfoBuilder;
            _details = details;
        }

        public void SavePdb(string pdbPath, string dllFileName)
        {
            Console.WriteLine("Emitting PDB file: {0}", Path.Combine(pdbPath, Path.GetFileNameWithoutExtension(dllFileName) + ".ni.pdb"));

            new PdbWriter(pdbPath, PDBExtraData.None, _details).WritePDBData(dllFileName, _outputInfoBuilder.EnumerateMethods());
        }

        public void SavePerfMap(string perfMapPath, int perfMapFormatVersion, string dllFileName)
        {
            string perfMapExtension;
            if (perfMapFormatVersion == PerfMapWriter.LegacyCrossgen1FormatVersion)
            {
                string mvidComponent = null;
                foreach (AssemblyInfo inputAssembly in _outputInfoBuilder.EnumerateInputAssemblies())
                {
                    if (mvidComponent == null)
                    {
                        mvidComponent = inputAssembly.Mvid.ToString();
                    }
                    else
                    {
                        mvidComponent = "composite";
                        break;
                    }
                }
                perfMapExtension = ".ni.{" + mvidComponent + "}.map";
            }
            else
            {
                perfMapExtension = ".ni.r2rmap";
            }

            string perfMapFileName = Path.Combine(perfMapPath, Path.GetFileNameWithoutExtension(dllFileName) + perfMapExtension);
            Console.WriteLine("Emitting PerfMap file: {0}", perfMapFileName);
            PerfMapWriter.Write(perfMapFileName, perfMapFormatVersion, _outputInfoBuilder.EnumerateMethods(), _outputInfoBuilder.EnumerateInputAssemblies(), _details);
        }
    }
}
