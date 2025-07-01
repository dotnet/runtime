// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace System.IO.Packaging.Tests
{
    public partial class Tests : FileCleanupTestBase
    {
        [Fact]
        public void Can_Create_Package_On_Unseekable_Stream()
        {
            var documentPath = "untitled.txt";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            using (WrappedStream ws = new WrappedStream(ms, true, true, false))
            {
                Package package = Package.Open(ws, FileMode.Create, FileAccess.Write);
                PackagePart part = package.CreatePart(partUriDocument, "application/text");

                package.Flush();
                package.Close();
                (package as IDisposable).Dispose();

                ms.Seek(0, SeekOrigin.Begin);

                package = Package.Open(ws, FileMode.Open, FileAccess.Read);

                Assert.NotNull(package.GetPart(partUriDocument));
            }
        }
    }
}
