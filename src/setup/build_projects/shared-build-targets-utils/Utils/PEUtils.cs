using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;

namespace Microsoft.DotNet.Cli.Build
{
    public static class PEUtils
    {
        public static bool HasMetadata(string pathToFile)
        {
            try
            {
                using (var inStream = File.OpenRead(pathToFile))
                {
                    using (var peReader = new PEReader(inStream))
                    {
                        return peReader.HasMetadata;
                    }
                }
            }
            catch (BadImageFormatException) { }

            return false;
        }
    }
}
