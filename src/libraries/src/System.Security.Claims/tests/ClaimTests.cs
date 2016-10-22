// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.Serialization;
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

        [Fact]
        public void CustomClaimIdentity_SerializeDeserialize_Roundtrip()
        {
            var id1 = new CustomClaimsIdentity("someAuthType", "someNameType", "someRoleType");
            ClaimsIdentity id2 = BinaryFormatterHelpers.Clone(id1);

            Assert.Equal(id1.Actor, id2.Actor);
            Assert.Equal(id1.AuthenticationType, id2.AuthenticationType);
            Assert.Equal(id1.BootstrapContext, id2.BootstrapContext);
            Assert.Equal(id1.IsAuthenticated, id2.IsAuthenticated);
            Assert.Equal(id1.Label, id2.Label);
            Assert.Equal(id1.Name, id2.Name);
            Assert.Equal(id1.NameClaimType, id2.NameClaimType);
            Assert.Equal(id1.RoleClaimType, id2.RoleClaimType);
        }

        [Fact]
        public void ClaimPrincipal_SerializeDeserialize_Roundtrip()
        {
            Assert.NotNull(BinaryFormatterHelpers.Clone(new ClaimsPrincipal()));
        }

        [Serializable]
        private sealed class CustomClaimsIdentity : ClaimsIdentity, ISerializable
        {
            public CustomClaimsIdentity(string authenticationType, string nameType, string roleType) : base(authenticationType, nameType, roleType)
            {
            }

            public CustomClaimsIdentity(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
            }
        }

        [Serializable]
        private sealed class CustomClaimsPrincipal : ClaimsPrincipal, ISerializable
        {
            public CustomClaimsPrincipal()
            {
            }

            public CustomClaimsPrincipal(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
            }
        }
    }
}
