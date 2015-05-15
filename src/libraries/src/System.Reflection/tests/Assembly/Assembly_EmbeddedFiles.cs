// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Stream s = typeof(EmbeddedFilesTest).GetTypeInfo().Assembly.GetManifestResourceStream("EmbeddedImage.png");
            Assert.NotNull(s);

            s = typeof(EmbeddedFilesTest).GetTypeInfo().Assembly.GetManifestResourceStream("NotExistFile");
            Assert.Null(s);
        }
    }
}