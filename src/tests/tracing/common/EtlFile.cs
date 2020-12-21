// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Tracing.Tests.Common
{
    public class EtlFile : IDisposable
    {
        public string Path { get; }
        private bool KeepOutput { get; }

        private EtlFile(string fileName, bool keep)
        {
            Path = fileName;
            KeepOutput = keep;
        }

        public void Dispose()
        {
            if (KeepOutput)
                Console.WriteLine("\n\tOutput file: {0}", Path);
            else
                File.Delete(Path);
        }

        public static EtlFile Create(string[] args)
        {
            if (args.Length >= 1)
                return new EtlFile(args[0], true);

            return new EtlFile(System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".etl", false);
        }
    }
}
