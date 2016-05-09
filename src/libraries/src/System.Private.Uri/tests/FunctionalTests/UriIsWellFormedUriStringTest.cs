// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            Assert.True(String.CompareOrdinal(rel.ToString(), 0, "./", 0, 2) == 0, "Cannot have colon in first segment, must append ./");
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

            Assert.True(String.CompareOrdinal(rel.ToString(), 0, "./", 0, 2) == 0, "Cannot have colon in first segment, must append ./");
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

            Assert.True(String.CompareOrdinal(rel.ToString(), 0, "./", 0, 2) == 0, "Cannot have colon in first segment, must append ./");
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
    }
}
