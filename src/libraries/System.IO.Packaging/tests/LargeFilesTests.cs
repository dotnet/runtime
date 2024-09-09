// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Packaging.Tests;

[Collection(nameof(DisableParallelization))]
public partial class LargeFileTests
{
    [Fact]
    [OuterLoop]
    public void VeryLargePart()
    {
        // FileAccess.Write is important, this tells ZipPackage to open the underlying ZipArchive in
        // ZipArchiveMode.Create mode as opposed to ZipArchiveMode.Update
        // When ZipArchive is opened in Create it will write entries directly to the zip stream
        // When ZipArchive is opened in Update it will write uncompressed data to memory until
        // the archive is closed.
        using (Stream stream = new MemoryStream())
        {
            Uri partUri = PackUriHelper.CreatePartUri(new Uri("test.bin", UriKind.Relative));

            // should compress *very well*
            byte[] buffer =  new byte[1024 * 1024];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(i % 2);
            }

            const long SizeInMb = 6 * 1024; // 6GB
            long totalLength = SizeInMb * buffer.Length;

            // issue on .NET Framework we cannot use FileAccess.Write on a ZipArchive
            using (Package package = Package.Open(stream, FileMode.Create, PlatformDetection.IsNetFramework ? FileAccess.ReadWrite : FileAccess.Write))
            {
                PackagePart part = package.CreatePart(partUri,
                                                        System.Net.Mime.MediaTypeNames.Application.Octet,
                                                        CompressionOption.Fast);


                using (Stream partStream = part.GetStream())
                {
                    for (long i = 0; i < SizeInMb; i++)
                    {
                        partStream.Write(buffer, 0, buffer.Length);
                    }
                }
            }

            // reopen for read and make sure we can get the part length & data matches
            stream.Seek(0, SeekOrigin.Begin);
            using (Package readPackage = Package.Open(stream))
            {
                PackagePart part = readPackage.GetPart(partUri);

                using (Stream partStream = part.GetStream())
                {
                    Assert.Equal(totalLength, partStream.Length);
                    byte[] readBuffer = new byte[buffer.Length];
                    for (long i = 0; i < SizeInMb; i++)
                    {
                        int totalRead = 0;
                        while (totalRead < readBuffer.Length)
                        {
                            int actualRead = partStream.Read(readBuffer, totalRead, readBuffer.Length - totalRead);
                            Assert.InRange(actualRead, 1, readBuffer.Length - totalRead);
                            totalRead += actualRead;
                        }

                        Assert.Equal(readBuffer.Length, totalRead);
                        Assert.True(buffer.AsSpan().SequenceEqual(readBuffer));
                    }
                }
            }
        }
    }
}
