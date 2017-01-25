// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Security.Claims
{
    public class ClaimTests
    {
        [Fact]
        public void Ctor_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new Claim(null));
        }

        [Fact]
        public void Claim_SerializeDeserialize_Roundtrip()
        {
            var id = new ClaimsIdentity("someAuthType", "someNameType", "someRoleType");
            var c1 = new Claim("someType", "someValue", "someValueType", "anIssuer", "anOriginalIssuer", id);
            Assert.Same(id, c1.Subject);

            Claim c2 = BinaryFormatterHelpers.Clone(c1);
            Assert.Equal(c1.Type, c2.Type);
            Assert.Equal(c1.Value, c2.Value);
            Assert.Equal(c1.ValueType, c2.ValueType);
            Assert.Equal(c1.Issuer, c2.Issuer);
            Assert.Equal(c1.OriginalIssuer, c2.OriginalIssuer);
            Assert.Null(c2.Subject);
        }
    }
}
