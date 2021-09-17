// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.PrivateUri.Tests
{
    public static class ConstructorTests
    {
        public static IEnumerable<object[]> TestCtor_String_UriKind_TestData
        {
            get
            {
                string uriString;

                uriString = "mapi16:///{S-1-5-0000000}/mail@example.com/곯가가가공갫갡곤갘갖갯걆겹갹곓곌갂겥강걥겤걿갨가";
                yield return new object[] { uriString, UriKind.Absolute, uriString };
            }
        }

        [Theory]
        [MemberData(nameof(TestCtor_String_UriKind_TestData))]
        public static void TestCtor_String_UriKind(string uriString, UriKind uriKind, string expectedValue)
        {
            Uri uri = new Uri(uriString, uriKind);

            Assert.Equal(expectedValue, uri.ToString());
        }
    }
}
