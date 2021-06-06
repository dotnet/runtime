// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

using Internal.TypeSystem;

namespace ILCompiler.Diagnostics
{
    public class PerfMapWriter
    {
        public enum PseudoRVA : uint
        {
            OutputGuid         = 0xFFFFFFFF,
            TargetOS           = 0xFFFFFFFE,
            TargetArchitecture = 0xFFFFFFFD,
            InputAssemblyName  = 0xFFFFFFFC,
            InputAssemblyMvid  = 0xFFFFFFFB,
        }

        private TextWriter _writer;

        private PerfMapWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public static void Write(string perfMapFileName, IEnumerable<MethodInfo> methods, IEnumerable<AssemblyInfo> inputAssemblies, TargetOS targetOS, TargetArchitecture targetArch)
        {
            using (TextWriter writer = new StreamWriter(perfMapFileName))
            {
                IEnumerable<AssemblyInfo> orderedInputs = inputAssemblies.OrderBy(asm => asm.Name, StringComparer.OrdinalIgnoreCase);

                PerfMapWriter perfMapWriter = new PerfMapWriter(writer);

                List<byte> inputHash = new List<byte>();
                foreach (AssemblyInfo inputAssembly in orderedInputs)
                {
                    inputHash.AddRange(inputAssembly.Mvid.ToByteArray());
                }
                inputHash.Add((byte)targetOS);
                inputHash.Add((byte)targetArch);
                Guid outputGuid = new Guid(MD5.HashData(inputHash.ToArray()));
                perfMapWriter.WriteLine(outputGuid.ToString(), (uint)PseudoRVA.OutputGuid, 0);
                perfMapWriter.WriteLine(targetOS.ToString(), (uint)PseudoRVA.TargetOS, 0);
                perfMapWriter.WriteLine(targetArch.ToString(), (uint)PseudoRVA.TargetArchitecture, 0);

                uint inputAssemblyIndex = 0;
                foreach (AssemblyInfo inputAssembly in orderedInputs)
                {
                    perfMapWriter.WriteLine(inputAssembly.Name, (uint)PseudoRVA.InputAssemblyName, inputAssemblyIndex);
                    perfMapWriter.WriteLine(inputAssembly.Mvid.ToString(), (uint)PseudoRVA.InputAssemblyMvid, inputAssemblyIndex);
                    inputAssemblyIndex++;
                }

                foreach (MethodInfo methodInfo in methods)
                {
                    if (methodInfo.HotRVA != 0 && methodInfo.HotLength != 0)
                    {
                        perfMapWriter.WriteLine(methodInfo.Name, methodInfo.HotRVA, methodInfo.HotLength);
                    }
                    if (methodInfo.ColdRVA != 0 && methodInfo.ColdLength != 0)
                    {
                        perfMapWriter.WriteLine(methodInfo.Name, methodInfo.ColdRVA, methodInfo.ColdLength);
                    }
                }
            }
        }

        private void WriteLine(string methodName, uint rva, uint length)
        {
            _writer.WriteLine($@"{rva:X8} {length:X2} {methodName}");
        }
    }
}
