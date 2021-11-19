// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.PrivateUri.Tests
{
    public class UriCreationOptionsTest
    {
        [Fact]
        public void UriCreationOptions_HasReasonableDefaults()
        {
            UriCreationOptions options = default;

            Assert.False(options.DangerousDisablePathAndQueryCanonicalization);
        }

        [Fact]
        public void UriCreationOptions_StoresCorrectValues()
        {
            var options = new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true };
            Assert.True(options.DangerousDisablePathAndQueryCanonicalization);

            options = new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = false };
            Assert.False(options.DangerousDisablePathAndQueryCanonicalization);
        }

        public static IEnumerable<object[]> DisableCanonicalization_TestData()
        {
            var schemes = new string[] { "http", "hTTp", " http", "https" };
            var hosts = new string[] { "foo", "f\u00F6\u00F6.com" };
            var ports = new string[] { ":80", ":443", ":0123", ":", "" };

            var pathAndQueries = new string[]
            {
                "",
                " ",
                "a b",
                "a%20b",
                "?a b",
                "?a%20b",
                "foo/./",
                "foo/../",
                "//\\//",
                "%41",
                "A?%41=%42",
                "?%41=%42",
                "? ",
            };

            var fragments = new string[] { "", "#", "#/foo ? %20%41/..//\\a" };
            var unicodeInPathModes = new int[] { 0, 1, 2, 3 };
            var pathDelimiters = new string[] { "", "/" };

            // Get various combinations of paths with unicode characters and delimiters
            string[] rawTargets = pathAndQueries
                .SelectMany(pq => fragments.Select(fragment => pq + fragment))
                .SelectMany(pqf => unicodeInPathModes.Select(unicodeMode => unicodeMode switch
                {
                    0 => pqf,
                    1 => "\u00F6" + pqf,
                    2 => pqf + "\u00F6",
                    _ => pqf.Insert(pqf.Length / 2, "\u00F6")
                }))
                .ToHashSet()
                .SelectMany(pqf => pathDelimiters.Select(delimiter => delimiter + pqf))
                .Where(target => target.StartsWith('/') || target.StartsWith('?')) // Can't see where the authority ends and the path starts otherwise
                .ToArray();

            foreach (string scheme in schemes)
            {
                foreach (string host in hosts)
                {
                    foreach (string port in ports)
                    {
                        foreach (string rawTarget in rawTargets)
                        {
                            string uriString = $"{scheme}://{host}{port}{rawTarget}";

                            int expectedPort = port.Length > 1 ? int.Parse(port.AsSpan(1)) : new Uri($"{scheme}://foo").Port;

                            string expectedQuery = rawTarget.Contains('?') ? rawTarget.Substring(rawTarget.IndexOf('?')) : "";

                            string expectedPath = rawTarget.Substring(0, rawTarget.Length - expectedQuery.Length);

                            yield return new object[] { uriString, host, expectedPort, expectedPath, expectedQuery };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(DisableCanonicalization_TestData))]
        public void DisableCanonicalization_IsRespected(string uriString, string expectedHost, int expectedPort, string expectedPath, string expectedQuery)
        {
            var options = new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true };

            var uri = new Uri(uriString, options);
            DoAsserts(uri);

            Assert.True(Uri.TryCreate(uriString, options, out uri));
            DoAsserts(uri);

            void DoAsserts(Uri uri)
            {
                Assert.Equal(new Uri($"http://{expectedHost}").Host, uri.Host);
                Assert.Equal(new Uri($"http://{expectedHost}").IdnHost, uri.IdnHost);

                Assert.Equal(expectedPort, uri.Port);

                Assert.Same(uri.AbsolutePath, uri.AbsolutePath);
                Assert.Equal(expectedPath, uri.AbsolutePath);

                Assert.Same(uri.Query, uri.Query);
                Assert.Equal(expectedQuery, uri.Query);

                string expectedPathAndQuery = expectedPath + expectedQuery;
                Assert.Same(uri.PathAndQuery, uri.PathAndQuery);
                Assert.Equal(expectedPathAndQuery, uri.PathAndQuery);

                Assert.Same(uri.Fragment, uri.Fragment);
                Assert.Empty(uri.Fragment); // Fragment is always empty if DisableCanonicalization is set
            }
        }

        [Fact]
        public void DisableCanonicalization_OnlyEqualToUrisWithMatchingFlag()
        {
            const string AbsoluteUri = "http://host";
            const string Path = "/foo";

            var absolute = new Uri(AbsoluteUri + Path, new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = false });
            var absoluteRaw = new Uri(AbsoluteUri + Path, new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
            NotEqual(absolute, absoluteRaw);
            Equal(absolute, absolute);
            Equal(absoluteRaw, absoluteRaw);

            var absoluteRawCopy = new Uri(AbsoluteUri + Path, new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
            Equal(absoluteRaw, absoluteRawCopy);

            var absoluteRawDifferentPath = new Uri(AbsoluteUri + "/bar", new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
            NotEqual(absoluteRaw, absoluteRawDifferentPath);

            var absoluteRawSameAuthority = new Uri(AbsoluteUri + ":80" + Path, new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
            Equal(absoluteRaw, absoluteRawSameAuthority);

            static void Equal(Uri left, Uri right)
            {
                Assert.True(left.Equals(right));
                Assert.True(right.Equals(left));
                Assert.Equal(left.GetHashCode(), right.GetHashCode());
            }

            static void NotEqual(Uri left, Uri right)
            {
                Assert.False(left.Equals(right));
                Assert.False(right.Equals(left));
            }
        }

        private const string FilePathRawData = "//\\A%41 %20\u00F6/.././%5C%2F#%42?%43#%44";

        public static IEnumerable<object[]> ImplicitFilePaths_TestData()
        {
            yield return Entry("C:/");
            yield return Entry("C|/");

            yield return Entry(@"//foo");
            yield return Entry(@"\/foo");
            yield return Entry(@"/\foo");
            yield return Entry(@"\\foo");

            if (!PlatformDetection.IsWindows)
            {
                yield return Entry("/foo");
            }

            static object[] Entry(string filePath) => new object[] { $"{filePath}/{FilePathRawData}" };
        }

        [Theory]
        [MemberData(nameof(ImplicitFilePaths_TestData))]
        public void DisableCanonicalization_WorksWithFileUris(string implicitFilePath)
        {
            var options = new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true };

            var uri = new Uri(implicitFilePath, options);
            DoAsserts(uri);

            Assert.True(Uri.TryCreate(implicitFilePath, options, out uri));
            DoAsserts(uri);

            static void DoAsserts(Uri uri)
            {
                Assert.True(uri.IsAbsoluteUri);
                Assert.True(uri.IsFile);
                Assert.Contains(FilePathRawData, uri.AbsolutePath);
                Assert.Contains(FilePathRawData, uri.AbsoluteUri);
                Assert.Contains(FilePathRawData, uri.ToString());
            }
        }

        [Theory]
        [InlineData("http")]
        [InlineData("https")]
        [InlineData("ftp")]
        [InlineData("file")]
        [InlineData("custom-unknown")]
        [InlineData("custom-registered")]
        public void DisableCanonicalization_WorksWithDifferentSchemes(string scheme)
        {
            if (scheme == "custom-registered")
            {
                scheme += "DisableCanonicalization";
                UriParser.Register(new HttpStyleUriParser(), scheme, defaultPort: Random.Shared.Next(-1, 65536));
            }

            string uriString = $"{scheme}://host/p%41th?a=%42#fragm%45nt";
            var options = new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true };

            var referenceUri = new Uri(uriString);

            var uri = new Uri(uriString, options);
            DoAsserts(uri);

            Assert.True(Uri.TryCreate(uriString, options, out uri));
            DoAsserts(uri);

            void DoAsserts(Uri uri)
            {
                Assert.Same(referenceUri.Scheme, uri.Scheme);
                Assert.Equal(referenceUri.Host, uri.Host);
                Assert.Equal(referenceUri.IdnHost, uri.IdnHost);
                Assert.Equal(referenceUri.Authority, uri.Authority);
                Assert.Equal(referenceUri.Port, uri.Port);
                Assert.Equal(referenceUri.IsDefaultPort, uri.IsDefaultPort);

                string referencePath = "/pAth";
                string referenceQuery = "?a=B";
                string path = "/p%41th";
                string query = "?a=%42#fragm%45nt";

                if (scheme == "ftp") // No query
                {
                    referencePath += referenceQuery.Replace("?", "%3F");
                    path += query;

                    referenceQuery = string.Empty;
                    query = string.Empty;
                }

                Assert.Equal(referencePath, referenceUri.AbsolutePath);
                Assert.Equal(path, uri.AbsolutePath);

                Assert.Equal(referenceQuery, referenceUri.Query);
                Assert.Equal(query, uri.Query);

                Assert.Equal(referencePath + referenceQuery, referenceUri.PathAndQuery);
                Assert.Equal(path + query, uri.PathAndQuery);

                Assert.Equal("#fragmEnt", referenceUri.Fragment);
                Assert.Empty(uri.Fragment);

                _ = referenceUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
                Assert.Throws<InvalidOperationException>(() => uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped));
            }
        }

        [Theory]
        [InlineData(UriFormat.UriEscaped)]
        [InlineData(UriFormat.Unescaped)]
        [InlineData(UriFormat.SafeUnescaped)]
        public void DisableCanonicalization_GetComponentsThrowsForPathAndQuery(UriFormat format)
        {
            var uri = new Uri("http://host/foo?bar=abc#fragment", new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });

            Assert.Equal("http", uri.GetComponents(UriComponents.Scheme, format));
            Assert.Equal("host", uri.GetComponents(UriComponents.Host, format));
            Assert.Equal("80", uri.GetComponents(UriComponents.StrongPort, format));
            Assert.Empty(uri.GetComponents(UriComponents.Fragment, format));

            Assert.Throws<InvalidOperationException>(() => uri.GetComponents(UriComponents.Path, format));
            Assert.Throws<InvalidOperationException>(() => uri.GetComponents(UriComponents.Query, format));
            Assert.Throws<InvalidOperationException>(() => uri.GetComponents(UriComponents.PathAndQuery, format));
            Assert.Throws<InvalidOperationException>(() => uri.GetComponents(UriComponents.AbsoluteUri, format));
        }


        private sealed class CustomUriParser : UriParser { }
    }
}
