// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Xunit;

public class GroupCollectionTests
{
    [Fact]
    public static void GetGroupTest()
    {
        string inputWithoutGroup = "Today is a great day for coding.";
        string inputWithGroup = "I had had an accident.";
        Regex regex = new Regex(@"(\w+)\s(\1)");
        Match matchPos = regex.Match(inputWithGroup);
        Match matchNeg = regex.Match(inputWithoutGroup);

        //test if Success returns true with group(s) present
        Assert.True(matchPos.Groups[0].Success);

        //public [] operator calling internal GetGroup()
        //should return internal _emptygroup thus making
        //call to Success() false
        Assert.False(matchNeg.Groups[0].Success);
    }
}
