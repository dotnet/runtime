// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.PortableExecutable;

namespace ILTrim
{
    public class Program
    {
        static void Main(string[] args)
        {
            using var fs = File.OpenRead(args[0]);
            using var pe = new PEReader(fs);
            using var output = File.Create("out.exe");
            Trimmer.TrimAssembly(pe, output);
        }
    }
}
