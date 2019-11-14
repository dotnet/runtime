// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Tracing.Tests.Common
{
    public class NetPerfFile : IDisposable
    {
        public string Path { get; }
        private bool KeepOutput { get; }

        private NetPerfFile(string fileName, bool keep)
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

        public static NetPerfFile Create(string[] args)
        {
            if (args.Length >= 1)
                return new NetPerfFile(args[0], true);

            return new NetPerfFile(System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".netperf", false);
        }
    }
}
