// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace CoreFx.Private.TestUtilities.Tests
{
    public class TheoryExtensionTests : IClassFixture<TheoryExtensionTests.TheoryDataFixture>
    {
        public class TheoryDataFixture : IDisposable
        {
            private static List<string> s_receivedTheoryData = new List<string>();

            public void AddValue(string value)
            {
                s_receivedTheoryData.Add(value);
            }

            public void Dispose()
            {
                Assert.Equal(3, s_receivedTheoryData.Count);
                Assert.Equal(new string[] { "one", "two", "three" }, s_receivedTheoryData);
            }
        }

        private TheoryDataFixture _fixture;

        public TheoryExtensionTests(TheoryDataFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory,
            MemberData(nameof(ToTheoryDataData))]
        public void ToTheoryData_Basic(string data)
        {
            _fixture.AddValue(data);
        }

        public static TheoryData ToTheoryDataData
        {
            get
            {
                List<string> data = new List<string>
                {
                    "one", "two", "three"
                };

                return data.ToTheoryData();
            }
        }
    }
}
