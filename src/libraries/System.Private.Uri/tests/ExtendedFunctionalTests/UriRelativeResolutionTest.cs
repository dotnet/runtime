// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// Summary description for UriRelativeResolution
    /// </summary>
    public class UriRelativeResolutionTest
    {
        // See RFC 3986 Section 5.2.2 and 5.4 http://www.ietf.org/rfc/rfc3986.txt

        private readonly Uri _fullBaseUri = new Uri("http://user:PLACEHOLDER@host:9090/path1/path2/path3/fileA?query#fragment");

        [Fact]
        public void Uri_Relative_BaseVsAbsolute_ReturnsFullAbsolute()
        {
            string absolute = "http://username:password@hostname:8080/p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, absolute);

            Assert.Equal(absolute, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsAuthority_ReturnsBaseSchemePlusAuthority()
        {
            string authority = "//username:password@hostname:8080/p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, authority);

            string expectedResult = _fullBaseUri.Scheme + ":" + authority;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsFullPath_ReturnsBaseAuthorityPlusFullPath()
        {
            string fullPath = "/p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, fullPath);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + fullPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsQueryAndFragment_ReturnsBaseAuthorityAndPathPlusQueryAndFragment()
        {
            string queryAndFragment = "?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, queryAndFragment);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Path) + queryAndFragment;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsQuery_ReturnsBaseAuthorityAndPathPlusQuery()
        {
            string query = "?AQuery";
            Uri resolved = new Uri(_fullBaseUri, query);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Path) + query;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsFragment_ReturnsBasePlusFragment()
        {
            string fragment = "#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, fragment);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Query) + fragment;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        //  Drop the 'filename' part of the path
        //  IE: http://a/b/c/d;p?q + y = http://a/b/c/y
        public void Uri_Relative_BaseVsPartialPath_ReturnsMergedPaths()
        {
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, partialPath);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSimplePartialPath_ReturnsMergedPaths()
        {
            string partialPath = "p1";
            Uri resolved = new Uri(_fullBaseUri, partialPath);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSimplePartialPathTrailingSlash_ReturnsMergedPaths()
        {
            string partialPath = "p1/";
            Uri resolved = new Uri(_fullBaseUri, partialPath);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        /* RFC 3986 Section 5.4.2 - System.Uri is a strict parser, not backward compatible with RFC 1630
           "Some parsers allow the scheme name to be present in a relative
           reference if it is the same as the base URI scheme.  This is
           considered to be a loophole in prior specifications of partial URI
           [RFC1630].  Its use should be avoided but is allowed for backward
           compatibility.

              "http:g"        =  "http:g"         ; for strict parsers
                              /  "http://a/b/c/g" ; for backward compatibility "*/
        public void Uri_Relative_BaseVsSimplePartialPathWithScheme_ReturnsPartialPathWithScheme()
        {
            string partialPath = "scheme:p1";
            Uri resolved = new Uri(_fullBaseUri, partialPath);

            string expectedResult = partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsDoubleCharColinChar_ReturnsCharColinChar()
        {
            string basicUri = "gd:a";
            Uri resolved = new Uri(_fullBaseUri, basicUri);

            string expectedResult = basicUri;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsFileLikeUri_MissingRootSlash_ThrowsUriFormatException()
        {
            Assert.ThrowsAny<FormatException>(() => new Uri(_fullBaseUri, "g:a"));
        }

        #region PathCompression

        [Fact]
        public void Uri_Relative_BaseVsSingleDotSlashStartingCompressPath_ReturnsMergedPathsWithoutSingleDot()
        {
            string compressable = "./";
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, compressable + partialPath);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsDoubleDotSlashStartingCompressPath_ReturnsBasePathBacksteppedOncePlusRelativePath()
        {
            string compressable = "../";
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, compressable + partialPath);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            baseUri = baseUri.Substring(0, baseUri.LastIndexOf("/"));
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsDoubleDoubleDotSlashStartingCompressPath_ReturnsBasePathBacksteppedTwicePlusRelativePath()
        {
            string compressable = "../../";
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, compressable + partialPath);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            baseUri = baseUri.Substring(0, baseUri.LastIndexOf("/"));
            baseUri = baseUri.Substring(0, baseUri.LastIndexOf("/"));
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsTrippleDoubleDotSlashStartingCompressPath_ReturnsBaseWithoutPathPlusRelativePath()
        {
            string compressable = "../../../";
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, compressable + partialPath);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Authority);
            string expectedResult = baseUri + "/" + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsTooManyDoubleDotSlashStartingCompressPath_ReturnsBaseWithoutPathPlusRelativePath()
        {
            string compressable = "../../../../";
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, compressable + partialPath);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Authority);
            string expectedResult = baseUri + "/" + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSingleDotSlashEndingCompressPath_ReturnsMergedPathsWithoutSingleDot()
        {
            string compressable = "./";
            string partialPath = "p1/p2/p3/p4/";
            Uri resolved = new Uri(_fullBaseUri, partialPath + compressable);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSingleDotEndingCompressPath_ReturnsMergedPathsWithoutSingleDot()
        {
            string compressable = ".";
            string partialPath = "p1/p2/p3/p4/";
            Uri resolved = new Uri(_fullBaseUri, partialPath + compressable);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSingleDot_ReturnsBasePathMinusFileWithoutSingleDot()
        {
            string compressable = ".";
            Uri resolved = new Uri(_fullBaseUri, compressable);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1);
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDot_ReturnsBaseMinusPath()
        {
            string compressable = "/.";
            Uri resolved = new Uri(_fullBaseUri, compressable);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + "/";
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDotSlashFile_ReturnsBasePlusRelativeFile()
        {
            string compressable = "/./file";
            Uri resolved = new Uri(_fullBaseUri, compressable);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + "/file";
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDoubleDotSlashFile_ReturnsBasePlusRelativeFile()
        {
            string compressable = "/../file";
            Uri resolved = new Uri(_fullBaseUri, compressable);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + "/file";
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsCharDot_ReturnsBasePathPlusCharDot()
        {
            string nonCompressable = "f.";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsDotChar_ReturnsBasePathPlusDotChar()
        {
            string nonCompressable = ".f";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsCharDoubleDot_ReturnsBasePathPlusCharDoubleDot()
        {
            string nonCompressable = "f..";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsDoubleDotChar_ReturnsBasePathPlusDoubleDotChar()
        {
            string nonCompressable = "..f";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsTrippleDot_ReturnsBasePathPlusTrippleDot()
        {
            string nonCompressable = "...";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = _fullBaseUri.GetLeftPart(UriPartial.Path);
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsCharDotSlash_ReturnsCharDotSlash()
        {
            string nonCompressable = "/f./";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDotCharSlash_ReturnsSlashDotCharSlash()
        {
            string nonCompressable = "/.f/";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsCharDoubleDotSlash_ReturnsCharDoubleDotSlash()
        {
            string nonCompressable = "/f../";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDoubleDotCharSlash_ReturnsSlashDoubleDotCharSlash()
        {
            string nonCompressable = "/..f/";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashTrippleDotSlash_ReturnsSlashTrippleDotSlash()
        {
            string nonCompressable = "/.../";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = _fullBaseUri.GetLeftPart(UriPartial.Authority) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        #endregion PathCompression

        #region MakeRelativeToUri

        [Fact]
        public void Uri_Relative_BaseMadeRelativeToSamePath_ReturnsQueryAndFragment()
        {
            Uri compareUri = new Uri("http://user:PLACEHOLDER@host:9090/path1/path2/path3/fileA?AQuery#AFragment");
            Uri relative = _fullBaseUri.MakeRelativeUri(compareUri);

            string expectedResult = "?AQuery#AFragment"; // compareUri.GetParts(UriComponents.Query | UriComponents.Fragment,UriFormat.Unescaped);
            Assert.Equal(expectedResult, relative.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseMadeRelativeToLastSlash_ReturnsDotSlashPlusQueryAndFragment()
        {
            Uri compareUri = new Uri("http://user:PLACEHOLDER@host:9090/path1/path2/path3/?AQuery#AFragment");
            Uri relative = _fullBaseUri.MakeRelativeUri(compareUri);
            Uri reassembled = new Uri(_fullBaseUri, relative); // Symetric


            string expectedResult = "./" + "?AQuery#AFragment"; // compareUri.GetParts(UriComponents.Query | UriComponents.Fragment, UriFormat.Unescaped);
            Assert.Equal(expectedResult, relative.ToString());
            Assert.Equal(compareUri, reassembled);
        }

        [Fact]
        public void Uri_Relative_BaseMadeRelativeToLastSlash_ReturnsDotSlash()
        {
            Uri compareUri = new Uri("http://user:PLACEHOLDER@host:9090/path1/path2/path3/");
            Uri relative = _fullBaseUri.MakeRelativeUri(compareUri);
            Uri reassembled = new Uri(_fullBaseUri, relative); // Symetric

            string expectedResult = "./";
            Assert.Equal(expectedResult, relative.ToString());
            Assert.Equal(compareUri, reassembled);
        }

        [Fact]
        public void Uri_Relative_BaseMadeRelativeToLastSlashWithExtra_ReturnsDotSlashPlusQueryAndFragment()
        {
            Uri compareUri = new Uri("http://user:PLACEHOLDER@host:9090/path1/path2/path3/Path4/fileb?AQuery#AFragment");
            Uri relative = _fullBaseUri.MakeRelativeUri(compareUri);
            Uri reassembled = new Uri(_fullBaseUri, relative); // Symetric

            string expectedResult = "Path4/fileb" + "?AQuery#AFragment"; // compareUri.GetParts(UriComponents.Query | UriComponents.Fragment, UriFormat.Unescaped);
            Assert.Equal(expectedResult, relative.ToString());
            Assert.Equal(compareUri, reassembled);
        }

        [Fact]
        public void Uri_Relative_BaseMadeRelativeToSecondToLastSlash_ReturnsDoubleDotSlashPlusQueryAndFragment()
        {
            Uri compareUri = new Uri("http://user:PLACEHOLDER@host:9090/path1/path2/?AQuery#AFragment");
            Uri relative = _fullBaseUri.MakeRelativeUri(compareUri);
            Uri reassembled = new Uri(_fullBaseUri, relative); // Symetric

            string expectedResult = "../" + "?AQuery#AFragment";  // compareUri.GetParts(UriComponents.Query | UriComponents.Fragment, UriFormat.Unescaped);
            Assert.Equal(expectedResult, relative.ToString());
            Assert.Equal(compareUri, reassembled);
        }

        [Fact]
        public void Uri_Relative_BaseMadeRelativeToThirdToLastSlash_ReturnsDoubleDoubleDotSlashPlusQueryAndFragment()
        {
            Uri compareUri = new Uri("http://user:PLACEHOLDER@host:9090/path1/?AQuery#AFragment");
            Uri relative = _fullBaseUri.MakeRelativeUri(compareUri);
            Uri reassembled = new Uri(_fullBaseUri, relative); // Symetric

            string expectedResult = "../../" + "?AQuery#AFragment";  // compareUri.GetParts(UriComponents.Query | UriComponents.Fragment, UriFormat.Unescaped);
            Assert.Equal(expectedResult, relative.ToString());
            Assert.Equal(compareUri, reassembled);
        }

        [Fact]
        public void Uri_Relative_BaseMadeRelativeToEmptyPath_ReturnsTrippleDoubleDotSlashPlusQueryAndFragment()
        {
            Uri compareUri = new Uri("http://user:PLACEHOLDER@host:9090/?AQuery#AFragment");
            Uri relative = _fullBaseUri.MakeRelativeUri(compareUri);
            Uri reassembled = new Uri(_fullBaseUri, relative); // Symetric

            string expectedResult = "../../../" + "?AQuery#AFragment";  // compareUri.GetParts(UriComponents.Query | UriComponents.Fragment, UriFormat.Unescaped);
            Assert.Equal(expectedResult, relative.ToString());
            Assert.Equal(compareUri, reassembled);
        }

        #endregion MakeRelativeToUri

        [Fact]
        public void UriRelative_AbsoluteToAbsolute_CustomPortCarriedOver()
        {
            Uri baseUri = new Uri("http://nothing.com/");
            Uri testUri = new Uri("https://specialPort.com:00065535/path?query#fragment");
            Uri resultUri = new Uri(baseUri, testUri);

            Assert.Equal(testUri.Port, resultUri.Port);
        }
    }
}
