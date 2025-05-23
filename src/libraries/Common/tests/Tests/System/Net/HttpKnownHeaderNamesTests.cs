// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

using Xunit;

namespace Tests.System.Net
{
    public class HttpKnownHeaderNamesTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("this should not be found")]
        public void TryGetHeaderName_UnknownStrings_NotFound(string shouldNotBeFound)
        {
            string name;
            Assert.False(HttpKnownHeaderNames.TryGetHeaderName(shouldNotBeFound, out name));
            Assert.Null(name);
        }

        [Theory]
        [MemberData(nameof(HttpKnownHeaderNamesPublicStringConstants))]
        public void TryGetHeaderName_AllHttpKnownHeaderNamesPublicStringConstants_Found(string constant)
        {
            string name1;
            Assert.True(HttpKnownHeaderNames.TryGetHeaderName(constant, out name1));
            Assert.NotNull(name1);
            Assert.Equal(constant, name1);

            string name2;
            Assert.True(HttpKnownHeaderNames.TryGetHeaderName(constant, out name2));
            Assert.NotNull(name2);
            Assert.Equal(constant, name2);

            Assert.Same(name1, name2);
        }

        public static IEnumerable<object[]> HttpKnownHeaderNamesPublicStringConstants
        {
            get
            {
                string[] constants = typeof(HttpKnownHeaderNames)
                    .GetTypeInfo()
                    .DeclaredFields
                    .Where(f => f.IsLiteral && f.IsStatic && f.IsPublic && f.FieldType == typeof(string))
                    .Select(f => (string)f.GetValue(null))
                    .ToArray();

                Assert.NotEmpty(constants);
                Assert.DoesNotContain(constants, c => string.IsNullOrEmpty(c));

                return constants.Select(c => new object[] { c });
            }
        }
    }
}
