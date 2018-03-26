// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlUriResolverTests
    {
        [Fact]
        public void Resolving_RelativeBase_Throws()
        {
            var resolver = new XmlUrlResolver();
            Assert.Throws<NotSupportedException>(() => resolver.ResolveUri(
                new Uri(Environment.CurrentDirectory + Path.DirectorySeparatorChar, UriKind.Relative), "test.xml"));
        }

        [Theory]
        [MemberData(nameof(GetBaseUriAndPath))]
        public void Resolving_LocalPath_Ok(Uri baseUri, string path)
        {
            var resolver = new XmlUrlResolver();
            Uri resolvedUri = resolver.ResolveUri(baseUri, path);

            Assert.Equal(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path)), resolvedUri.LocalPath);
            Assert.True(resolvedUri.LocalPath.EndsWith(path.Replace('/', Path.DirectorySeparatorChar)));
        }

        [Theory]
        [MemberData(nameof(XmlFileTargets))]
        public void Resolving_OnlyWithBaseUri_Ok(string basePath)
        {
            var baseUri = new Uri(Path.GetFullPath(basePath));
            var resolver = new XmlUrlResolver();
            Uri resolvedUri = resolver.ResolveUri(baseUri, string.Empty);

            Assert.Equal(Path.GetFullPath(basePath), resolvedUri.LocalPath);
        }

        public static IEnumerable<object[]> GetBaseUriAndPath()
        {
            // Base URI as null is the default for internal Xml operation.
            var baseUris = new List<Uri> { null };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // The case below does not work on Unix, the '#' ends up treated as a fragment and the path is cut there.
                var currDirWithDirSeparator = Environment.CurrentDirectory + Path.DirectorySeparatorChar;
                baseUris.Add(new Uri(currDirWithDirSeparator, UriKind.Absolute));
                baseUris.Add(new Uri(string.Empty, UriKind.RelativeOrAbsolute));
            }

            foreach (Uri baseUri in baseUris)
            {
                foreach (object[] targetFile in XmlFileTargets)
                    yield return new object[] { baseUri, targetFile[0] };
            }
        }

        public static IEnumerable<object[]> XmlFileTargets => new object[][]
        {
            new object[] { "f#/t/\u00eb/test.xml" },
            new object[] { "/f#/t/\u00eb/t#st.xml" },
            new object[] { "/f#/\u00e3/\u00eb/t\u00ebst.xml" },
            new object[] { "u/t/c/test.xml" },
            new object[] { "u/t/c/t#st.xml" },
            new object[] { "/u/t/c/t\u00ebst.xml" },
            new object[] { "test.xml" },
            new object[] { "t#st.xml" },
            new object[] { "t\u00ebst.xml" }
        };
    }
}
