// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace System.Drawing.Tests
{
    public abstract class DrawingTest
    {
        private static Security.Cryptography.MD5 s_md5 = Security.Cryptography.MD5.Create();

        protected void ValidateImageContent(Image image, byte[] expectedHash)
        {
            using (MemoryStream stream = new MemoryStream(4096))
            {
                image.Save(stream, ImageFormat.Bmp);
                stream.Seek(0, SeekOrigin.Begin);
                byte[] hash = s_md5.ComputeHash(stream);
                Assert.Equal(expectedHash, hash);
            }
        }
    }
}
