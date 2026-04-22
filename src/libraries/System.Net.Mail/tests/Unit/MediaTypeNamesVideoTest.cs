// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Mime.Tests
{
    public class MediaTypeNamesVideoTest
    {
        [Theory]
        [InlineData(MediaTypeNames.Video.Mp4, "video/mp4")]
        [InlineData(MediaTypeNames.Video.Mpeg, "video/mpeg")]
        [InlineData(MediaTypeNames.Video.Ogg, "video/ogg")]
        [InlineData(MediaTypeNames.Video.QuickTime, "video/quicktime")]
        [InlineData(MediaTypeNames.Video.WebM, "video/webm")]
        public void VideoMediaTypeNames_MatchExpectedValues(string actual, string expected)
        {
            Assert.Equal(expected, actual);
        }
    }
}
