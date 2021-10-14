// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;

namespace ILTrim
{
    public class Program
    {
        static void Main(string[] args)
        {
            var inputPath = args[0];
            int i = 1;
            List<string> referencePaths = new();
            List<string> trimPaths = new();
            while (args.Length > i) {
                if (args[i] == "-r")
                {
                    referencePaths.Add(args[i + 1]);
                    i += 2;
                }
                else if (args[i] == "-t")
                {
                    trimPaths.Add(args[i + 1]);
                    i += 2;
                }
            }

            Trimmer.TrimAssembly(inputPath, trimPaths, Directory.GetCurrentDirectory(), referencePaths);
        }
    }
}
