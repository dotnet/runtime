// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// Summary description for UriRelativeResolution
    /// </summary>
    public class UriRelativeResolutionTest
    {
        // See RFC 3986 Section 5.2.2 and 5.4 http://www.ietf.org/rfc/rfc3986.txt

        private readonly Uri _fullBaseUri = new Uri("http://user:PLACEHOLDER@host:9090/path1/path2/path3/fileA?query#fragment");
        private const string FullBaseUriGetLeftPart_Path = "http://user:PLACEHOLDER@host:9090/path1/path2/path3/fileA";
        private const string FullBaseUriGetLeftPart_Authority = "http://user:PLACEHOLDER@host:9090";
        private const string FullBaseUriGetLeftPart_Query = "http://user:PLACEHOLDER@host:9090/path1/path2/path3/fileA?query";

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

            string expectedResult = FullBaseUriGetLeftPart_Authority + fullPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsQueryAndFragment_ReturnsBaseAuthorityAndPathPlusQueryAndFragment()
        {
            string queryAndFragment = "?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, queryAndFragment);

            string expectedResult = FullBaseUriGetLeftPart_Path + queryAndFragment;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsQuery_ReturnsBaseAuthorityAndPathPlusQuery()
        {
            string query = "?AQuery";
            Uri resolved = new Uri(_fullBaseUri, query);

            string expectedResult = FullBaseUriGetLeftPart_Path + query;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsFragment_ReturnsBasePlusFragment()
        {
            string fragment = "#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, fragment);

            string expectedResult = FullBaseUriGetLeftPart_Query + fragment;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        //  Drop the 'filename' part of the path
        //  IE: http://a/b/c/d;p?q + y = http://a/b/c/y
        public void Uri_Relative_BaseVsPartialPath_ReturnsMergedPaths()
        {
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, partialPath);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSimplePartialPath_ReturnsMergedPaths()
        {
            string partialPath = "p1";
            Uri resolved = new Uri(_fullBaseUri, partialPath);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSimplePartialPathTrailingSlash_ReturnsMergedPaths()
        {
            string partialPath = "p1/";
            Uri resolved = new Uri(_fullBaseUri, partialPath);

            string baseUri = FullBaseUriGetLeftPart_Path;
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
        public void Uri_Relative_SimplePartialPathWithUnknownScheme_Unicode_ReturnsPartialPathWithScheme()
        {
            string schemeAndRelative = "scheme:\u011E";
            Uri resolved = new Uri(schemeAndRelative, UriKind.RelativeOrAbsolute);

            string expectedResult = schemeAndRelative;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_SimplePartialPathWithScheme_Unicode_ReturnsPartialPathWithScheme()
        {
            string schemeAndRelative = "http:\u00C7";
            Uri resolved = new Uri(schemeAndRelative, UriKind.RelativeOrAbsolute);

            string expectedResult = schemeAndRelative;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_RightToLeft()
        {
            string loremIpsumArabic = "\u0643\u0644 \u0627\u0644\u0649 \u0627\u0644\u0639\u0627\u0644\u0645";

            string schemeAndRelative = "scheme:" + loremIpsumArabic;
            Uri resolved = new Uri(schemeAndRelative, UriKind.RelativeOrAbsolute);

            string expectedResult = schemeAndRelative;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_Unicode_Glitchy()
        {
            string glitchy = "4\u0308\u0311\u031A\u030B\u0352\u034A\u030D\u036C\u036C\u036B\u0344\u0312\u0322\u0334\u0328\u0319\u0323\u0359\u0317\u0324\u0319\u032D\u0331\u0319\u031F\u0331\u0330\u0347\u0353\u0318\u032F\u032C\u03162\u0303\u0313\u031A\u0368\u036E\u0368\u0301\u0367\u0368\u0306\u0305\u0350\u036A\u036F\u0307\u0328\u035F\u0321\u0361\u0320\u032F\u032B\u034E\u0326\u033B";

            string schemeAndRelative = "scheme:" + glitchy;
            Uri resolved = new Uri(schemeAndRelative, UriKind.RelativeOrAbsolute);

            string expectedResult = schemeAndRelative;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Unicode_Format_Character_Combinations_Scheme()
        {
            IEnumerable<string> combinations = CartesianProductAll(c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format && !UriHelper.IsIriDisallowedBidi(c));

            foreach (string combination in combinations)
            {
                string escaped = UriHelper.IriEscapeNonUcsChars(combination);

                string schemeAndRelative = $"scheme:{escaped}";
                Uri resolved = new Uri(schemeAndRelative, UriKind.RelativeOrAbsolute);

                string expectedResult = schemeAndRelative;
                Assert.Equal(expectedResult, resolved.ToString());
            }
        }

        [Fact]
        public void Uri_Unicode_Reserved_Character_Combinations_Scheme()
        {
            IEnumerable<string> combinations = CartesianProductAll(UriHelper.IsIriReserved);

            foreach (string combination in combinations)
            {
                string schemeAndRelative = $"scheme:{combination}";
                Uri resolved = new Uri(schemeAndRelative, UriKind.RelativeOrAbsolute);

                string expectedResult = schemeAndRelative;
                Assert.Equal(expectedResult, resolved.ToString());
            }
        }

        [Fact]
        public void Uri_Unicode_IriUnreserved_Character_Combinations_Scheme()
        {
            IEnumerable<string> combinations = CartesianProductAll(UriHelper.IsIriUnreserved, _ => false, false);

            foreach (string combination in combinations)
            {
                string schemeAndRelative = $"scheme:{combination}";
                Uri resolved = new Uri(schemeAndRelative, UriKind.RelativeOrAbsolute);

                string expectedResult = schemeAndRelative;
                Assert.Equal(expectedResult, resolved.ToString());
            }
        }

        [Fact]
        public void Uri_Unicode_SurrogatePairs_Scheme()
        {
            IEnumerable<string> combinations = CartesianProductAll(char.IsHighSurrogate, char.IsLowSurrogate, false);

            foreach (string combination in combinations)
            {
                bool escape = !UriHelper.IsIriAllowedSurrogate(combination);

                string schemeAndRelative = escape ? $"scheme:{UriHelper.IriEscapeAll(combination)}" : $"scheme:{combination}";
                Uri resolved = new Uri(schemeAndRelative, UriKind.RelativeOrAbsolute);

                string expectedResult = schemeAndRelative;
                Assert.Equal(expectedResult, resolved.ToString());
            }
        }

        private IEnumerable<string> CartesianProductAll(Func<char, bool> filter, Func<char, bool> secondFilter = null, bool includeSingleChars = true)
        {
            char[] characters = Enumerable.Range(0, 0xFFFF).Select(c => (char)c).Where(filter).ToArray();

            char[] secondCharacters = secondFilter == null ? characters : Enumerable.Range(0, 0xFFFF).Select(c => (char)c).Where(secondFilter).ToArray();

            return CartesianProduct(characters, secondCharacters, includeSingleChars);
        }

        private IEnumerable<string> CartesianProduct(IEnumerable<char> first, IEnumerable<char> second, bool includeSingleChars = true)
        {
            var cartesian = from c1 in first
                            from c2 in second
                            select new string(new[] { c1, c2 });

            if (includeSingleChars)
            {
                cartesian = cartesian.Concat(first.Select(_ => _.ToString()));
                cartesian = cartesian.Union(second.Select(_ => _.ToString()));
            }

            return cartesian.ToList();
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
            string partialPath = "g:a";
            Assert.ThrowsAny<FormatException>(() => new Uri(_fullBaseUri, partialPath));
        }

        #region PathCompression

        [Fact]
        public void Uri_Relative_BaseVsSingleDotSlashStartingCompressPath_ReturnsMergedPathsWithoutSingleDot()
        {
            string compressable = "./";
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, compressable + partialPath);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsDoubleDotSlashStartingCompressPath_ReturnsBasePathBacksteppedOncePlusRelativePath()
        {
            string compressable = "../";
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, compressable + partialPath);

            string baseUri = FullBaseUriGetLeftPart_Path;
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

            string baseUri = FullBaseUriGetLeftPart_Path;
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

            string baseUri = FullBaseUriGetLeftPart_Authority;
            string expectedResult = baseUri + "/" + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsTooManyDoubleDotSlashStartingCompressPath_ReturnsBaseWithoutPathPlusRelativePath()
        {
            string compressable = "../../../../";
            string partialPath = "p1/p2/p3/p4/file1?AQuery#TheFragment";
            Uri resolved = new Uri(_fullBaseUri, compressable + partialPath);

            string baseUri = FullBaseUriGetLeftPart_Authority;
            string expectedResult = baseUri + "/" + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSingleDotSlashEndingCompressPath_ReturnsMergedPathsWithoutSingleDot()
        {
            string compressable = "./";
            string partialPath = "p1/p2/p3/p4/";
            Uri resolved = new Uri(_fullBaseUri, partialPath + compressable);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSingleDotEndingCompressPath_ReturnsMergedPathsWithoutSingleDot()
        {
            string compressable = ".";
            string partialPath = "p1/p2/p3/p4/";
            Uri resolved = new Uri(_fullBaseUri, partialPath + compressable);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + partialPath;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSingleDot_ReturnsBasePathMinusFileWithoutSingleDot()
        {
            string compressable = ".";
            Uri resolved = new Uri(_fullBaseUri, compressable);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1);
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDot_ReturnsBaseMinusPath()
        {
            string compressable = "/.";
            Uri resolved = new Uri(_fullBaseUri, compressable);

            string expectedResult = FullBaseUriGetLeftPart_Authority + "/";
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDotSlashFile_ReturnsBasePlusRelativeFile()
        {
            string compressable = "/./file";
            Uri resolved = new Uri(_fullBaseUri, compressable);

            string expectedResult = FullBaseUriGetLeftPart_Authority + "/file";
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDoubleDotSlashFile_ReturnsBasePlusRelativeFile()
        {
            string compressable = "/../file";
            Uri resolved = new Uri(_fullBaseUri, compressable);

            string expectedResult = FullBaseUriGetLeftPart_Authority + "/file";
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsCharDot_ReturnsBasePathPlusCharDot()
        {
            string nonCompressable = "f.";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsDotChar_ReturnsBasePathPlusDotChar()
        {
            string nonCompressable = ".f";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsCharDoubleDot_ReturnsBasePathPlusCharDoubleDot()
        {
            string nonCompressable = "f..";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsDoubleDotChar_ReturnsBasePathPlusDoubleDotChar()
        {
            string nonCompressable = "..f";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsTrippleDot_ReturnsBasePathPlusTrippleDot()
        {
            string nonCompressable = "...";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string baseUri = FullBaseUriGetLeftPart_Path;
            string expectedResult = baseUri.Substring(0, baseUri.LastIndexOf("/") + 1) + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsCharDotSlash_ReturnsCharDotSlash()
        {
            string nonCompressable = "/f./";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = FullBaseUriGetLeftPart_Authority + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDotCharSlash_ReturnsSlashDotCharSlash()
        {
            string nonCompressable = "/.f/";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = FullBaseUriGetLeftPart_Authority + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsCharDoubleDotSlash_ReturnsCharDoubleDotSlash()
        {
            string nonCompressable = "/f../";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = FullBaseUriGetLeftPart_Authority + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashDoubleDotCharSlash_ReturnsSlashDoubleDotCharSlash()
        {
            string nonCompressable = "/..f/";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = FullBaseUriGetLeftPart_Authority + nonCompressable;
            Assert.Equal(expectedResult, resolved.ToString());
        }

        [Fact]
        public void Uri_Relative_BaseVsSlashTrippleDotSlash_ReturnsSlashTrippleDotSlash()
        {
            string nonCompressable = "/.../";
            Uri resolved = new Uri(_fullBaseUri, nonCompressable);

            string expectedResult = FullBaseUriGetLeftPart_Authority + nonCompressable;
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

        /// <summary>
        /// Performs IRI character class lookups and character encodings.
        /// </summary>
        private static class UriHelper
        {
            private static readonly IEnumerable<Tuple<char, char>> _iriUnreservedRanges = new List<Tuple<char, char>>()
            {
                // ALPHA
                Tuple.Create('A', 'Z'),
                Tuple.Create('a', 'z'),

                // DIGIT
                Tuple.Create('0', '9'),

                // single chars
                Tuple.Create('-', '-'),
                Tuple.Create('.', '.'),
                Tuple.Create('_', '-'),
                Tuple.Create('~', '~'),

                // UCSCHAR
                Tuple.Create('\u00A0', '\uD7FF'),
                Tuple.Create('\uF900', '\uFDCF'),
                Tuple.Create('\uFDF0', '\uFFEF'),
            };

            private static readonly IEnumerable<Tuple<char, char>> _iriUcscharRanges = new List<Tuple<char, char>>()
            {
                // UCSCHAR
                Tuple.Create('\u00A0', '\uD7FF'),
                Tuple.Create('\uF900', '\uFDCF'),
                Tuple.Create('\uFDF0', '\uFFEF'),
            };

            private static readonly IEnumerable<Tuple<string, string>> _iriAllowedSurrogateRanges = new List<Tuple<string, string>>()
            {
                // UCSCHAR
                Tuple.Create("\U00010000", "\U0001FFFD"),
                Tuple.Create("\U00020000", "\U0002FFFD"),
                Tuple.Create("\U00030000", "\U0003FFFD"),
                Tuple.Create("\U00040000", "\U0004FFFD"),
                Tuple.Create("\U00050000", "\U0005FFFD"),
                Tuple.Create("\U00060000", "\U0006FFFD"),
                Tuple.Create("\U00070000", "\U0007FFFD"),
                Tuple.Create("\U00080000", "\U0008FFFD"),
                Tuple.Create("\U00090000", "\U0009FFFD"),
                Tuple.Create("\U000A0000", "\U000AFFFD"),
                Tuple.Create("\U000B0000", "\U000BFFFD"),
                Tuple.Create("\U000C0000", "\U000CFFFD"),
                Tuple.Create("\U000D0000", "\U000DFFFD"),
                Tuple.Create("\U000E1000", "\U000EFFFD"),
            };

            private static readonly HashSet<char> _iriReserved = new HashSet<char>()
            {
                ':', ',', '?', '#', '[', ']', '@', '!', '$', '&', '\'', '(', ')', '*', '+', ',', ';', '='
            };

            // https://tools.ietf.org/html/rfc3987#page-18 (LRM, RLM, LRE, RLE, LRO, RLO, and PDF)
            private static readonly HashSet<char> _iriDisallowedBidi = new HashSet<char>()
            {
                '\u200E', '\u200F', '\u202A', '\u202B', '\u202D', '\u202E', '\u202C'
            };

            public static string IriEscapeAll(string input)
            {
                var result = new StringBuilder();

                foreach (byte b in Encoding.UTF8.GetBytes(input))
                {
                    result.Append($"%{b:X2}");
                }

                return result.ToString();
            }

            public static string IriEscapeNonUcsChars(string input)
            {
                var result = new StringBuilder();

                foreach (char c in input)
                {
                    if (IsIriUcschar(c))
                    {
                        result.Append(c);

                        continue;
                    }

                    foreach (byte b in Encoding.UTF8.GetBytes(c.ToString()))
                    {
                        result.Append($"%{b:X2}");
                    }
                }

                return result.ToString();
            }
            public static bool IsIriAllowedSurrogate(string pair)
            {
                bool inRange =
                    _iriAllowedSurrogateRanges.Any(
                        _ => string.CompareOrdinal(_.Item1, pair) <= 0 && string.CompareOrdinal(_.Item2, pair) >= 0);

                return inRange;
            }

            public static bool IsIriDisallowedBidi(char c)
            {
                return _iriDisallowedBidi.Contains(c);
            }
            public static bool IsIriReserved(char c)
            {
                /*
                reserved       = gen-delims / sub-delims
                gen-delims     = ":" / "/" / "?" / "#" / "[" / "]" / "@"
                sub-delims     = "!" / "$" / "&" / "'" / "(" / ")"
                                / "*" / "+" / "," / ";" / "="
                */

                return _iriReserved.Contains(c);
            }

            private static bool IsIriUcschar(char c)
            {
                bool inRange = _iriUcscharRanges.Any(_ => _.Item1 <= c && _.Item2 >= c);

                return inRange;
            }

            public static bool IsIriUnreserved(char c)
            {
                /*
                ALPHA          =  %x41-5A / %x61-7A   ; A-Z / a-z
                DIGIT          =  %x30-39

                iunreserved    = ALPHA / DIGIT / "-" / "." / "_" / "~" / ucschar

                ucschar        = %xA0-D7FF / %xF900-FDCF / %xFDF0-FFEF
                                / %x10000-1FFFD / %x20000-2FFFD / %x30000-3FFFD
                                / %x40000-4FFFD / %x50000-5FFFD / %x60000-6FFFD
                                / %x70000-7FFFD / %x80000-8FFFD / %x90000-9FFFD
                                / %xA0000-AFFFD / %xB0000-BFFFD / %xC0000-CFFFD
                                / %xD0000-DFFFD / %xE1000-EFFFD
                */

                // https://www.ietf.org/rfc/rfc3987.txt 2.2
                bool inRange = _iriUnreservedRanges.Any(_ => _.Item1 <= c && _.Item2 >= c);

                return inRange;
            }
        }
    }
}
