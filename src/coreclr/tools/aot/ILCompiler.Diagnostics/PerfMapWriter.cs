// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace ILCompiler.Diagnostics
{
    public class PerfMapWriter
    {
        private TextWriter _writer;

        private PerfMapWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public static void Write(string perfMapFileName, IEnumerable<MethodInfo> methods)
        {
            using (TextWriter writer = new StreamWriter(perfMapFileName))
            {
                PerfMapWriter perfMapWriter = new PerfMapWriter(writer);
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
