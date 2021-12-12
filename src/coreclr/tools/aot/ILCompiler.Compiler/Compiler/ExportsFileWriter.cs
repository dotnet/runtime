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
        private string _exportsFile;
        private List<EcmaMethod> _methods;
        private TypeSystemContext _context;

        public ExportsFileWriter(TypeSystemContext context, string exportsFile)
        {
            _exportsFile = exportsFile;
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
                    foreach (var method in _methods)
                        streamWriter.WriteLine($"   {method.GetUnmanagedCallersOnlyExportName()}");
                }
                else if(_context.Target.IsOSX)
                {
                    foreach (var method in _methods)
                        streamWriter.WriteLine($"_{method.GetUnmanagedCallersOnlyExportName()}");
                }
                else
                {
                    streamWriter.WriteLine("V1.0 {");
                    streamWriter.WriteLine("    global: _init; _fini;");
                    foreach (var method in _methods)
                        streamWriter.WriteLine($"        {method.GetUnmanagedCallersOnlyExportName()};");
                    streamWriter.WriteLine("    local: *;");
                    streamWriter.WriteLine("};");
                }
            }
        }
    }
}
