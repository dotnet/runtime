// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public class ExportsFileWriter
    {
        private readonly string _exportsFile;
        private readonly IEnumerable<string> _exportSymbols;
        private readonly List<EcmaMethod> _methods;
        private readonly TypeSystemContext _context;

        public ExportsFileWriter(TypeSystemContext context, string exportsFile, IEnumerable<string> exportSymbols)
        {
            _exportsFile = exportsFile;
            _exportSymbols = exportSymbols;
            _context = context;
            _methods = new List<EcmaMethod>();
        }

        public void AddExportedMethods(IEnumerable<EcmaMethod> methods)
            => _methods.AddRange(methods.Where(m => m.Module != _context.SystemModule));

        public void EmitExportedMethods()
        {
            FileStream fileStream = new FileStream(_exportsFile, FileMode.Create);
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                if (_context.Target.IsWindows)
                {
                    streamWriter.WriteLine("EXPORTS");
                    foreach (string symbol in _exportSymbols)
                        streamWriter.WriteLine($"   {symbol.Replace(',', ' ')}");
                    foreach (var method in _methods)
                        streamWriter.WriteLine($"   {method.GetUnmanagedCallersOnlyExportName()}");
                }
                else if(_context.Target.IsApplePlatform)
                {
                    foreach (string symbol in _exportSymbols)
                        streamWriter.WriteLine($"_{symbol}");
                    foreach (var method in _methods)
                        streamWriter.WriteLine($"_{method.GetUnmanagedCallersOnlyExportName()}");
                }
                else
                {
                    streamWriter.WriteLine("V1.0 {");
                    streamWriter.WriteLine("    global: _init; _fini;");
                    foreach (string symbol in _exportSymbols)
                        streamWriter.WriteLine($"        {symbol};");
                    foreach (var method in _methods)
                        streamWriter.WriteLine($"        {method.GetUnmanagedCallersOnlyExportName()};");
                    streamWriter.WriteLine("    local: *;");
                    streamWriter.WriteLine("};");
                }
            }
        }
    }
}
