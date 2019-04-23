// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// Summary description for UriIsWellFormedUriStringTest
    /// </summary>
    public class UriIsWellFormedUriStringTest
    {
        [Fact]
        public void UriIsWellFormed_AbsoluteWellFormed_Success()
        {
            Assert.True(Uri.IsWellFormedUriString("http://foo.com/bad:url", UriKind.Absolute));
        }

        [Fact]
        public void UriIsWellFormed_RelativeWellFormed_Success()
        {
            Assert.True(Uri.IsWellFormedUriString("/path/file?Query", UriKind.Relative));
        }

        [Fact]
        public void UriIsWellFormed_RelativeWithColon_Failure()
        {
            Assert.False(Uri.IsWellFormedUriString("http://foo", UriKind.Relative));
        }

        [Fact]
        public void UriIsWellFormed_RelativeWithPercentAndColon_Failure()
        {
            Assert.False(Uri.IsWellFormedUriString("bad%20http://foo", UriKind.Relative));
        }

        [Fact]
        public void UriIsWellFormed_NewRelativeRegisteredAbsolute_Throws()
        {
            Assert.ThrowsAny<FormatException>(() =>
          {
              Uri test = new Uri("http://foo", UriKind.Relative);
          });
        }

        [Fact]
        public void UriIsWellFormed_NewAbsoluteUnregisteredAsRelative_Throws()
        {
            Assert.ThrowsAny<FormatException>(() =>
          {
              Uri test = new Uri("any://foo", UriKind.Relative);
          });
        }

        [Fact]
        public void UriIsWellFormed_NewRelativeWithKnownSchemeAndQuery_SuccessButNotWellFormed()
        {
            Uri test = new Uri("http:?foo", UriKind.Relative);
            Assert.False(Uri.IsWellFormedUriString(test.ToString(), UriKind.Relative), "Not well formed");
            Assert.False(Uri.IsWellFormedUriString(test.ToString(), UriKind.Absolute), "Should not be well formed");
            Assert.True(Uri.TryCreate(test.ToString(), UriKind.Relative, out test), "TryCreate Mismatch");
            Uri result = new Uri(new Uri("http://host.com"), test);
            Assert.True(Uri.IsWellFormedUriString(result.ToString(), UriKind.Absolute), "Not well formed");
        }

        [Fact]
        public void UriIsWellFormed_NewRelativeWithUnknownSchemeAndQuery_Throws()
        {
            Assert.ThrowsAny<FormatException>(() =>
          {
                // The generic parser allows this kind of absolute Uri, where the http parser does not
                Uri test;
              Assert.False(Uri.TryCreate("any:?foo", UriKind.Relative, out test), "TryCreate should have Failed");
              test = new Uri("any:?foo", UriKind.Relative);
          });
        }

        [Fact]
        public void UriIsWellFormed_TryCreateNewRelativeWithColon_Failure()
        {
            Uri test;
            Assert.False(Uri.TryCreate("http://foo", UriKind.Relative, out test));
        }

        // App-compat - A colon in the first segment of a relative Uri is invalid, but we cannot reject it.
        [Fact]
        public void UriIsWellFormed_TryCreateNewRelativeWithPercentAndColon_Success()
        {
            string input = "bad%20http://foo";
            Uri test;
            Assert.True(Uri.TryCreate(input, UriKind.Relative, out test));
            Assert.False(test.IsWellFormedOriginalString());
            Assert.False(Uri.IsWellFormedUriString(input, UriKind.Relative));
            Assert.False(Uri.IsWellFormedUriString(input, UriKind.RelativeOrAbsolute));
            Assert.False(Uri.IsWellFormedUriString(input, UriKind.Absolute));
        }

        [Fact]
        public void UriIsWellFormed_AbsoluteWithColonToRelative_AppendsDotSlash()
        {
            Uri baseUri = new Uri("https://base.com/path/stuff");
            Uri test = new Uri("https://base.com/path/hi:there/", UriKind.Absolute);

            Uri rel = baseUri.MakeRelativeUri(test);

            Assert.True(Uri.IsWellFormedUriString(rel.ToString(), UriKind.Relative), "Not well formed: " + rel);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<Uri>(test, result); //"Transitivity failure"

            Assert.True(string.CompareOrdinal(rel.ToString(), 0, "./", 0, 2) == 0, "Cannot have colon in first segment, must append ./");
        }

        [Fact]
        public void UriIsWellFormed_AbsoluteWithPercentAndColonToRelative_AppendsDotSlash()
        {
            Uri baseUri = new Uri("https://base.com/path/stuff");
            Uri test = new Uri("https://base.com/path/h%20i:there/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Assert.True(Uri.IsWellFormedUriString(rel.ToString(), UriKind.Relative), "Not well formed: " + rel);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<Uri>(test, result); //"Transitivity failure"

            Assert.True(string.CompareOrdinal(rel.ToString(), 0, "./", 0, 2) == 0, "Cannot have colon in first segment, must append ./");
        }

        [Fact]
        public void UriMakeRelative_ImplicitFileCommonBaseWithColon_AppendsDotSlash()
        {
            Uri baseUri = new Uri(@"c:/base/path/stuff");
            Uri test = new Uri(@"c:/base/path/hi:there/", UriKind.Absolute);

            Uri rel = baseUri.MakeRelativeUri(test);

            Assert.True(Uri.IsWellFormedUriString(rel.ToString(), UriKind.Relative), "Not well formed: " + rel);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<String>(test.LocalPath, result.LocalPath); //  "Transitivity failure"

            Assert.True(string.CompareOrdinal(rel.ToString(), 0, "./", 0, 2) == 0, "Cannot have colon in first segment, must append ./");
        }

        [Fact]
        public void UriMakeRelative_ImplicitFileDifferentBaseWithColon_ReturnsSecondUri()
        {
            Uri baseUri = new Uri(@"c:/base/path/stuff");
            Uri test = new Uri(@"d:/base/path/hi:there/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<String>(test.LocalPath, result.LocalPath); //"Transitivity failure"
        }

        [Fact]
        public void UriMakeRelative_ExplicitFileDifferentBaseWithColon_ReturnsSecondUri()
        {
            Uri baseUri = new Uri(@"file://c:/stuff");
            Uri test = new Uri(@"file://d:/hi:there/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Assert.False(rel.IsAbsoluteUri, "Result should be relative");

            Assert.Equal<String>("d:/hi:there/", rel.ToString());

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<String>(test.LocalPath, result.LocalPath); //  "Transitivity failure"
            Assert.Equal<String>(test.ToString(), result.ToString()); //  "Transitivity failure"
        }

        [Fact]
        public void UriMakeRelative_ExplicitUncFileVsDosFile_ReturnsSecondPath()
        {
            Uri baseUri = new Uri(@"file:///u:/stuff");
            Uri test = new Uri(@"file:///unc/hi:there/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Uri result = new Uri(baseUri, rel);

            // This is a known oddity when mix and matching Unc & dos paths in this order. 
            // The other way works as expected.
            Assert.Equal<string>("file:///u:/unc/hi:there/", result.ToString());
        }

        [Fact]
        public void UriMakeRelative_ExplicitDosFileWithHost_ReturnsSecondPath()
        {
            Uri baseUri = new Uri(@"file://host/u:/stuff");
            Uri test = new Uri(@"file://host/unc/hi:there/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<String>(test.LocalPath, result.LocalPath);  // "Transitivity failure"
        }

        [Fact]
        public void UriMakeRelative_ExplicitDosFileSecondWithHost_ReturnsSecondPath()
        {
            Uri baseUri = new Uri(@"file://host/unc/stuff");
            Uri test = new Uri(@"file://host/u:/hi:there/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<String>(test.LocalPath, result.LocalPath); //"Transitivity failure"
        }

        [Fact]
        public void UriMakeRelative_ExplicitDosFileVsUncFile_ReturnsSecondUri()
        {
            Uri baseUri = new Uri(@"file:///unc/stuff");
            Uri test = new Uri(@"file:///u:/hi:there/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<String>(test.LocalPath, result.LocalPath); //"Transitivity failure"
        }

        [Fact]
        public void UriMakeRelative_ExplicitDosFileContainingImplicitDosPath_AddsDotSlash()
        {
            Uri baseUri = new Uri(@"file:///u:/stuff/file");
            Uri test = new Uri(@"file:///u:/stuff/h:there/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<String>(test.LocalPath, result.LocalPath); //"Transitivity failure"
        }

        [Fact]
        public void UriMakeRelative_DifferentSchemes_ReturnsSecondUri()
        {
            Uri baseUri = new Uri(@"http://base/path/stuff");
            Uri test = new Uri(@"https://base/path/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Assert.Equal<Uri>(test, rel);

            Assert.True(Uri.IsWellFormedUriString(rel.ToString(), UriKind.Absolute), "Not well formed: " + rel);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<Uri>(result, test);
        }

        [Fact]
        public void UriMakeRelative_DifferentHost_ReturnsSecondUri()
        {
            Uri baseUri = new Uri(@"http://host1/path/stuff");
            Uri test = new Uri(@"http://host2/path/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Assert.Equal<Uri>(test, rel);

            Assert.True(Uri.IsWellFormedUriString(rel.ToString(), UriKind.Absolute), "Not well formed: " + rel);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<Uri>(result, test);
        }

        [Fact]
        public void UriMakeRelative_DifferentPort_ReturnsSecondUri()
        {
            Uri baseUri = new Uri(@"http://host:1/path/stuff");
            Uri test = new Uri(@"http://host:2/path/", UriKind.Absolute);
            Uri rel = baseUri.MakeRelativeUri(test);

            Assert.Equal<Uri>(test, rel);

            Assert.True(Uri.IsWellFormedUriString(rel.ToString(), UriKind.Absolute), "Not well formed: " + rel);

            Uri result = new Uri(baseUri, rel);

            Assert.Equal<Uri>(result, test);
        }

        [Fact]
        public void UriIsWellFormed_IPv6HostIriOn_True()
        {
            Assert.True(Uri.IsWellFormedUriString("http://[::1]/", UriKind.Absolute));
        }

        public static IEnumerable<object[]> TestIsWellFormedUriStringData =>
        new List<object[]>
        {
            // Test ImplicitFile/UNC
            new object[] { "c:\\directory\filename", false },
            new object[] { "file://c:/directory/filename", false },
            new object[] { "\\\\?\\UNC\\Server01\\user\\docs\\Letter.txt", false },

            // Test Host
            new object[] { "http://www.contoso.com", true },
            new object[] { "http://\u00E4.contos.com", true },
            new object[] { "http://www.contos\u00E4.com", true },

            new object[] { "http://www.contoso.com ", true },
            new object[] { "http://\u00E4.contos.com ", true },

            new object[] { "http:// www.contoso.com", false },
            new object[] { "http:// \u00E4.contos.com", false },
            new object[] { "http:// www.contos\u00E4.com", false },

            new object[] { "http://www.contos o.com", false },
            new object[] { "http://www.contos \u00E4.com", false },
            

            // Test Path
            new object[] { "http://www.contoso.com/path???/file name", false },
            new object[] { "http://www.contoso.com/\u00E4???/file name", false },
            new object[] { "http:\\host/path/file", false },

            new object[] { "http://www.contoso.com/a/sek http://test.com", false },
            new object[] { "http://www.contoso.com/\u00E4/sek http://test.com", false },

            new object[] { "http://www.contoso.com/ seka http://test.com", false },
            new object[] { "http://www.contoso.com/ sek\u00E4 http://test.com", false },

            new object[] { "http://www.contoso.com/ a sek http://test.com", false },
            new object[] { "http://www.contoso.com/ \u00E4 sek http://test.com", false },

            new object[] { "http://www.contoso.com/ \u00E4/", false },
            new object[] { "http://www.contoso.com/ path/", false },

            new object[] { "http://www.contoso.com/path", true },
            new object[] { "http://www.contoso.com/\u00E4/", true },

            new object[] { "http://www.contoso.com/path/#", true },
            new object[] { "http://www.contoso.com/\u00E4/#", true },

            new object[] { "http://www.contoso.com/path/# ", true },
            new object[] { "http://www.contoso.com/\u00E4/# ", true },

            new object[] { "http://www.contoso.com/path/ # ", false },
            new object[] { "http://www.contoso.com/\u00E4/ # ", false },

            new object[] { "http://www.contoso.com/path/ #", false },
            new object[] { "http://www.contoso.com/\u00E4/ #", false },

            new object[] { "http://www.contoso.com/path ", true },
            new object[] { "http://www.contoso.com/\u00E4/ ", true },

            new object[] { "http://www.contoso.com/path/\u00E4/path /", false },
            new object[] { "http://www.contoso.com/path/\u00E4/path / ", false },
            new object[] { "http://www.contoso.com/path/\u00E4/path/", true },
            new object[] { "http://www.contoso.com/path/\u00E4 /path/", false },
            new object[] { "http://www.contoso.com/path/\u00E4 /path/ ", false },
            new object[] { "http://www.contoso.com/path/\u00E4/path/ \u00E4/", false },

            // Test Query
            new object[] { "http://www.contoso.com/path?name", true },
            new object[] { "http://www.contoso.com/path?\u00E4", true },

            new object[] { "http://www.contoso.com/path?name ", true },
            new object[] { "http://www.contoso.com/path?\u00E4 ", true },

            new object[] { "http://www.contoso.com/path ?name ", false },
            new object[] { "http://www.contoso.com/path ?\u00E4 ", false },

            new object[] { "http://www.contoso.com/path?par=val?", true },
            new object[] { "http://www.contoso.com/path?\u00E4=\u00E4?", true },

            new object[] { "http://www.contoso.com/path? name ", false },
            new object[] { "http://www.contoso.com/path? \u00E4 ", false },
            
            new object[] { "http://www.contoso.com/path?p=", true },
            new object[] { "http://www.contoso.com/path?\u00E4=", true },

            new object[] { "http://www.contoso.com/path?p= ", true },
            new object[] { "http://www.contoso.com/path?\u00E4= ", true },

            new object[] { "http://www.contoso.com/path?p= val", false },
            new object[] { "http://www.contoso.com/path?\u00E4= \u00E4", false },
            
            new object[] { "http://www.contoso.com/path?par=value& par=value", false },
            new object[] { "http://www.contoso.com/path?\u00E4=\u00E4& \u00E4=\u00E4", false },

            // Test Fragment
            new object[] { "http://www.contoso.com/path?name#", true },
            new object[] { "http://www.contoso.com/path?\u00E4#", true },

            new object[] { "http://www.contoso.com/path?name# ", true },
            new object[] { "http://www.contoso.com/path?\u00E4# ", true },

            new object[] { "http://www.contoso.com/path?name#a", true },
            new object[] { "http://www.contoso.com/path?\u00E4#\u00E4", true },

            new object[] { "http://www.contoso.com/path?name#a ", true },
            new object[] { "http://www.contoso.com/path?\u00E4#\u00E4 ", true },

            new object[] { "http://www.contoso.com/path?name# a", false },
            new object[] { "http://www.contoso.com/path?\u00E4# \u00E4", false },


            // Test Path+Query
            new object[] { "http://www.contoso.com/path? a ", false },
            new object[] { "http://www.contoso.com/\u00E4? \u00E4 ", false },

            new object[] { "http://www.contoso.com/a?val", true },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4", true },
            
            new object[] { "http://www.contoso.com/path /path?par=val", false },
            new object[] { "http://www.contoso.com/\u00E4 /\u00E4?\u00E4=\u00E4", false },

            // Test Path+Query+Fragment
            new object[] { "http://www.contoso.com/path?a#a", true },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4#\u00E4", true },

            new object[] { "http://www.contoso.com/path?par=val#a ", true },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4=\u00E4#\u00E4 ", true },

            new object[] { "http://www.contoso.com/path?val#", true },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4#", true },

            new object[] { "http://www.contoso.com/path?val#?val", true },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4#?\u00E4", true },

            new object[] { "http://www.contoso.com/path?val #", false },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4 #", false },

            new object[] { "http://www.contoso.com/path?val# val", false },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4# \u00E4", false },

            new object[] { "http://www.contoso.com/path?val# val ", false },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4# \u00E4 ", false },

            new object[] { "http://www.contoso.com/path?val#val ", true },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4#\u00E4 ", true },

            new object[] { "http://www.contoso.com/ path?a#a", false },
            new object[] { "http://www.contoso.com/ \u00E4?\u00E4#\u00E4", false },

            new object[] { "http://www.contoso.com/ path?a #a", false },
            new object[] { "http://www.contoso.com/ \u00E4?\u00E4 #\u00E4", false },

            new object[] { "http://www.contoso.com/ path?a #a ", false },
            new object[] { "http://www.contoso.com/ \u00E4?\u00E4 #\u00E4 ", false },

            new object[] { "http://www.contoso.com/path?a# a ", false },
            new object[] { "http://www.contoso.com/path?\u00E4# \u00E4 ", false },

            
            new object[] { "http://www.contoso.com/path?a#a?a", true },
            new object[] { "http://www.contoso.com/\u00E4?\u00E4#u00E4?\u00E4", true },

            // Sample in "private unsafe Check CheckCanonical(char* str, ref ushort idx, ushort end, char delim)" code comments
            new object[] { "http://www.contoso.com/\u00E4/ path2/ param=val", false },
            new object[] { "http://www.contoso.com/\u00E4? param=val", false },
            new object[] { "http://www.contoso.com/\u00E4?param=val# fragment", false },
        };

        [Theory]
        [MemberData(nameof(TestIsWellFormedUriStringData))]
        // Bug hasn't been fixed yet on NetFramework
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void TestIsWellFormedUriString(string uriString, bool expected)
        {
            Assert.Equal(expected, Uri.IsWellFormedUriString(uriString, UriKind.RelativeOrAbsolute));
        }

        public static IEnumerable<object[]> UriIsWellFormedUnwiseStringData =>
        new List<object[]>
        {
            // escaped
            new object[] { "https://www.contoso.com/?a=%7B%7C%7D&b=%E2%80%99", true },
            new object[] { "https://www.contoso.com/?a=%7B%7C%7D%E2%80%99", true },
            
            // unescaped
            new object[] { "https://www.contoso.com/?a=}", false },
            new object[] { "https://www.contoso.com/?a=|", false },
            new object[] { "https://www.contoso.com/?a={", false },

            // not query
            new object[] { "https://www.%7Bcontoso.com/", false },
            new object[] { "http%7Bs://www.contoso.com/", false },
            new object[] { "https://www.contoso.com%7B/", false },
            new object[] { "htt%7Cps://www.contoso.com/", false },
            new object[] { "https://www.con%7Ctoso.com/", false },
            new object[] { "https://www.contoso.com%7C/", false },
            new object[] { "htt%7Dps://www.contoso.com/", false },
            new object[] { "https://www.con%7Dtoso.com/", false },
            new object[] { "https://www.contoso.com%7D/", false },
            new object[] { "htt{ps://www.contoso.com/", false },
            new object[] { "https://www.con{toso.com/", false },
            new object[] { "https://www.contoso.com{/", false },
            new object[] { "htt|ps://www.contoso.com/", false },
            new object[] { "https://www.con|toso.com/", false },
            new object[] { "https://www.contoso.com|/", false },
            new object[] { "htt}ps://www.contoso.com/", false },
            new object[] { "https://www.con}toso.com/", false },
            new object[] { "https://www.contoso.com}/", false },
        };

        [Theory]
        [MemberData(nameof(UriIsWellFormedUnwiseStringData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void UriIsWellFormed_AbsoluteUnicodeWithUnwise_Success(string uriString, bool expected)
        {
            Assert.Equal(expected, Uri.IsWellFormedUriString(uriString, UriKind.Absolute));
        }
    }
}
