// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal sealed class TempFileHolder : IDisposable
    {
        public string FilePath { get; }

        public TempFileHolder(ReadOnlySpan<char> content)
        {
            FilePath = Path.GetTempFileName();

            using (StreamWriter writer = new StreamWriter(FilePath, append: false))
            {
                writer.Write(
#if NET
                    content
#else
                    content.ToArray()
#endif
                    );
            }
        }

        public TempFileHolder(byte[] content)
        {
            FilePath = Path.GetTempFileName();

            File.WriteAllBytes(FilePath, content);
        }

        public void Dispose()
        {
            try
            {
                File.Delete(FilePath);
            }
            catch
            {
                // Best effort
            }
        }
    }
}
