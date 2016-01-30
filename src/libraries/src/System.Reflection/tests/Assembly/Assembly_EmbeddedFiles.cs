// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public class EmbeddedFilesTest
    {
        [Fact]
        public void TestEmbeddedFiles()
        {
            var resources = typeof(EmbeddedFilesTest).GetTypeInfo().Assembly.GetManifestResourceNames();

            Assert.Contains("EmbeddedImage.png", resources);

            Stream s = typeof(EmbeddedFilesTest).GetTypeInfo().Assembly.GetManifestResourceStream("EmbeddedImage.png");
            Assert.NotNull(s);

            s = typeof(EmbeddedFilesTest).GetTypeInfo().Assembly.GetManifestResourceStream("NotExistFile");
            Assert.Null(s);
        }
    }
}
