// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using Xunit;

namespace System.Security.Claims
{
    public class ClaimsIdentityTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var id = new ClaimsIdentity();
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_AuthenticationType_Blank()
        {
            var id = new ClaimsIdentity("");
            Assert.Equal(string.Empty, id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_AuthenticationType_Null()
        {
            var id = new ClaimsIdentity((string)null);
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_AuthenticationType()
        {
            var id = new ClaimsIdentity("auth_type");
            Assert.Equal("auth_type", id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.True(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaim_Null()
        {
            var id = new ClaimsIdentity((IEnumerable<Claim>)null);
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaim_Empty()
        {
            var id = new ClaimsIdentity(new Claim[0]);
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaim_WithName()
        {
            var id = new ClaimsIdentity(
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                });
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(2, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Equal("claim_name_value", id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaim_WithoutName()
        {
            var id = new ClaimsIdentity(
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType + "_x", "claim_name_value"),
                });
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(2, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaimAuthNameRoleType()
        {
            var id = new ClaimsIdentity(new[] {
                new Claim ("claim_type", "claim_value"),
                new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                new Claim ("claim_role_type", "claim_role_value"),
            },
                       "test_auth_type", "test_name_type", "claim_role_type");
            Assert.Equal("test_auth_type", id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(3, id.Claims.Count());
            Assert.True(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal("test_name_type", id.NameClaimType);
            Assert.Equal("claim_role_type", id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaimAuthNameRoleType_AllNull()
        {
            var id = new ClaimsIdentity((IEnumerable<Claim>)null, (string)null, (string)null, (string)null);
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaimAuthNameRoleType_AllEmpty()
        {
            var id = new ClaimsIdentity(new Claim[0], "", "", "");
            Assert.Equal(string.Empty, id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaimAuthNameRoleType_TwoClaimsAndTypesEmpty()
        {
            var id = new ClaimsIdentity(
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                },
                       "", "", "");
            Assert.Equal(string.Empty, id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(2, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Equal("claim_name_value", id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_EnumerableClaimAuthNameRoleType_TwoClaimsAndTypesNull()
        {
            var id = new ClaimsIdentity(
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                },
                       (string)null, (string)null, (string)null);
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(2, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Equal("claim_name_value", id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType()
        {
            var id = new ClaimsIdentity((IIdentity)null, (IEnumerable<Claim>)null, (string)null, (string)null, (string)null);
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType_IdentityNullRestEmpty()
        {
            var id = new ClaimsIdentity(null, new Claim[0], "", "", "");
            Assert.Equal(string.Empty, id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(0, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType_ClaimsArrayEmptyTypes()
        {
            var id = new ClaimsIdentity(
                       null,
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                },
                       "", "", "");

            Assert.Equal(string.Empty, id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(2, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Equal("claim_name_value", id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType_NullClaimsArrayNulls()
        {
            var id = new ClaimsIdentity(
                       null,
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                },
                       (string)null, (string)null, (string)null);
            Assert.Null(id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(2, id.Claims.Count());
            Assert.False(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Equal("claim_name_value", id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType_NullIdentityRestFilled()
        {
            var id = new ClaimsIdentity(
                       null,
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                    new Claim ("claim_role_type", "claim_role_value"),
                },
                       "test_auth_type", "test_name_type", "claim_role_type");
            Assert.Equal("test_auth_type", id.AuthenticationType);
            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(3, id.Claims.Count());
            Assert.True(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Null(id.Name);
            Assert.Equal("test_name_type", id.NameClaimType);
            Assert.Equal("claim_role_type", id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType_ClaimsIdentityRestFilled()
        {
            var baseId = new ClaimsIdentity(
                           new[] { new Claim("base_claim_type", "base_claim_value") },
                           "base_auth_type");

            baseId.Actor = new ClaimsIdentity("base_actor");
            baseId.BootstrapContext = "bootstrap_context";
            baseId.Label = "base_label";

            Assert.True(baseId.IsAuthenticated, "#0");

            var id = new ClaimsIdentity(
                       baseId,
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                    new Claim ("claim_role_type", "claim_role_value"),
                },
                       "test_auth_type", "test_name_type", "claim_role_type");

            Assert.Equal("test_auth_type", id.AuthenticationType);

            Assert.NotNull(id.Actor);
            Assert.Equal("base_actor", id.Actor.AuthenticationType);
            Assert.Equal("bootstrap_context", id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(4, id.Claims.Count());
            Assert.Equal("base_claim_type", id.Claims.First().Type);
            Assert.True(id.IsAuthenticated);
            Assert.Equal("base_label", id.Label);
            Assert.Null(id.Name);
            Assert.Equal("test_name_type", id.NameClaimType);
            Assert.Equal("claim_role_type", id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType_NonClaimsIdentityRestEmptyWorks()
        {
            var baseId = new NonClaimsIdentity { Name = "base_name", AuthenticationType = "TestId_AuthType" };

            var id = new ClaimsIdentity(
                       baseId,
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                    new Claim ("claim_role_type", "claim_role_value"),
                },
                       "", "", "");

            Assert.Equal("TestId_AuthType", id.AuthenticationType);

            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(4, id.Claims.Count());
            Assert.Equal(2, id.Claims.Count(_ => _.Type == ClaimsIdentity.DefaultNameClaimType));
            Assert.True(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Equal("base_name", id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType_ClaimsIdentityClaim()
        {
            var baseId = new ClaimsIdentity(
                           new[] { new Claim("base_claim_type", "base_claim_value") },
                           "base_auth_type", "base_name_claim_type", null);

            baseId.Actor = new ClaimsIdentity("base_actor");
            baseId.BootstrapContext = "bootstrap_context";
            baseId.Label = "base_label";

            Assert.True(baseId.IsAuthenticated);

            var id = new ClaimsIdentity(
                       baseId,
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                    new Claim ("claim_role_type", "claim_role_value"),
                });

            Assert.Equal("base_auth_type", id.AuthenticationType);

            Assert.NotNull(id.Actor);
            Assert.Equal("base_actor", id.Actor.AuthenticationType);
            Assert.Equal("bootstrap_context", id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(4, id.Claims.Count());
            Assert.Equal("base_claim_type", id.Claims.First().Type);
            Assert.True(id.IsAuthenticated);
            Assert.Equal("base_label", id.Label);
            Assert.Null(id.Name);
            Assert.Equal("base_name_claim_type", id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Fact]
        public void Ctor_IdentityEnumerableClaimAuthNameRoleType_NonClaimsIdentityClaims()
        {
            var baseId = new NonClaimsIdentity
            {
                Name = "base_name",
                AuthenticationType = "TestId_AuthType"
            };

            var id = new ClaimsIdentity(
                       baseId,
                       new[] {
                    new Claim ("claim_type", "claim_value"),
                    new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                    new Claim ("claim_role_type", "claim_role_value"),
                });

            Assert.Equal("TestId_AuthType", id.AuthenticationType);

            Assert.Null(id.Actor);
            Assert.Null(id.BootstrapContext);
            Assert.NotNull(id.Claims);
            Assert.Equal(4, id.Claims.Count());
            Assert.Equal(2, id.Claims.Count(_ => _.Type == ClaimsIdentity.DefaultNameClaimType));
            Assert.True(id.IsAuthenticated);
            Assert.Null(id.Label);
            Assert.Equal("base_name", id.Name);
            Assert.Equal(ClaimsIdentity.DefaultNameClaimType, id.NameClaimType);
            Assert.Equal(ClaimsIdentity.DefaultRoleClaimType, id.RoleClaimType);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData((StringComparison)0xABCDE)]
        public static void Ctor_ValidateStringComparison(StringComparison stringComparison)
        {
            AssertExtensions.Throws<ArgumentException>(
                "stringComparison",
                () => new ClaimsIdentity(stringComparison: stringComparison));

            BinaryReader reader = new(Stream.Null);
            AssertExtensions.Throws<ArgumentException>(
                "stringComparison",
                () => new ClaimsIdentity(reader, stringComparison));

            AssertExtensions.Throws<ArgumentException>(
                "stringComparison",
                () => new CustomClaimsIdentity(new ClaimsIdentity(), stringComparison));
        }

        [Fact]
        public static void Ctor_NullClaimsIdentity()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "other",
                static () => new CustomClaimsIdentity(null));

            AssertExtensions.Throws<ArgumentNullException>(
                "other",
                static () => new CustomClaimsIdentity(null, StringComparison.Ordinal));
        }

        [Fact]
        public static void Ctor_NullBinaryReader()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "reader",
                static () => new ClaimsIdentity((BinaryReader)null));

            AssertExtensions.Throws<ArgumentNullException>(
                "reader",
                static () => new ClaimsIdentity((BinaryReader)null, StringComparison.Ordinal));
        }

        [Fact]
        public void Find_CaseInsensivity()
        {
            var claim_type = new Claim("TYpe", "value");
            var id = new ClaimsIdentity(
                new[] { claim_type },
                "base_auth_type", "base_name_claim_type", null);

            var f1 = id.FindFirst("tyPe");
            Assert.Equal("value", f1.Value);

            var f2 = id.FindAll("tyPE").First();
            Assert.Equal("value", f2.Value);
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase, false)]
        public void FindFirst_WithStringComparison_Match(
            string claimType,
            string findType,
            StringComparison stringComparison,
            bool copy)
        {
            Claim claim = new(claimType, "value");

            ClaimsIdentity id = new(
                claims: [claim],
                stringComparison: stringComparison);

            if (copy)
            {
                id = new CustomClaimsIdentity(id, stringComparison);
            }

            Claim found = id.FindFirst(findType);
            Assert.NotNull(found);
            Assert.Equal(claimType, found.Type);
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCulture, false)]
        [InlineData("Type", "type", StringComparison.InvariantCulture, true)]
        [InlineData("Type", "type", StringComparison.Ordinal, false)]
        [InlineData("Type", "type", StringComparison.Ordinal, true)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal, false)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal, true)]
        public void FindFirst_WithStringComparison_NoMatch(
            string claimType,
            string findType,
            StringComparison stringComparison,
            bool copy)
        {
            Claim claim = new(claimType, "value");

            ClaimsIdentity id = new(
                claims: [claim],
                stringComparison: stringComparison);

            if (copy)
            {
                id = new CustomClaimsIdentity(id, stringComparison);
            }

            Claim found = id.FindFirst(findType);
            Assert.Null(found);
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase, true)]
        public void FindAll_WithStringComparison_Match(
            string claimType,
            string findType,
            StringComparison stringComparison,
            bool copy)
        {
            Claim claim = new(claimType, "value");

            ClaimsIdentity id = new(
                claims: [claim],
                stringComparison: stringComparison);

            if (copy)
            {
                id = new CustomClaimsIdentity(id, stringComparison);
            }

            Claim found = Assert.Single(id.FindAll(findType));
            Assert.NotNull(found);
            Assert.Equal(claimType, found.Type);
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCulture, false)]
        [InlineData("Type", "type", StringComparison.InvariantCulture, true)]
        [InlineData("Type", "type", StringComparison.Ordinal, false)]
        [InlineData("Type", "type", StringComparison.Ordinal, true)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal, false)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal, true)]
        public void FindAll_WithStringComparison_NoMatch(
            string claimType,
            string findType,
            StringComparison stringComparison,
            bool copy)
        {
            Claim claim = new(claimType, "value");

            ClaimsIdentity id = new(
                claims: [claim],
                stringComparison: stringComparison);

            if (copy)
            {
                id = new CustomClaimsIdentity(id, stringComparison);
            }

            Assert.Empty(id.FindAll(findType));
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase, true)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase, false)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase, false)]
        public void HasClaim_WithStringComparison_Match(
            string claimType,
            string findType,
            StringComparison stringComparison,
            bool copy)
        {
            Claim claim = new(claimType, "value");

            ClaimsIdentity id = new(
                claims: [claim],
                stringComparison: stringComparison);

            if (copy)
            {
                id = new CustomClaimsIdentity(id, stringComparison);
            }

            AssertExtensions.TrueExpression(id.HasClaim(findType, "value"));
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCulture, true)]
        [InlineData("Type", "type", StringComparison.InvariantCulture, false)]
        [InlineData("Type", "type", StringComparison.Ordinal, true)]
        [InlineData("Type", "type", StringComparison.Ordinal, false)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal, true)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal, false)]
        public void HasClaim_WithStringComparison_NoMatch(
            string claimType,
            string findType,
            StringComparison stringComparison,
            bool copy)
        {
            Claim claim = new(claimType, "value");

            ClaimsIdentity id = new(
                claims: [claim],
                stringComparison: stringComparison);

            if (copy)
            {
                id = new CustomClaimsIdentity(id, stringComparison);
            }

            AssertExtensions.FalseExpression(id.HasClaim(findType, "value"));
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase)]
        public void FindFirst_BinaryReader_WithStringComparison_Match(
            string claimType,
            string findType,
            StringComparison stringComparison)
        {
            using BinaryReader reader = CreateReaderWithClaim(claimType);
            ClaimsIdentity id = new(reader, stringComparison: stringComparison);

            Claim found = id.FindFirst(findType);
            Assert.NotNull(found);
            Assert.Equal(claimType, found.Type);
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCulture)]
        [InlineData("Type", "type", StringComparison.Ordinal)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal)]
        public void FindFirst_BinaryReader_WithStringComparison_NoMatch(
            string claimType,
            string findType,
            StringComparison stringComparison)
        {
            using BinaryReader reader = CreateReaderWithClaim(claimType);
            ClaimsIdentity id = new(reader, stringComparison: stringComparison);

            Claim found = id.FindFirst(findType);
            Assert.Null(found);
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase)]
        public void FindAll_BinaryReader_WithStringComparison_Match(
            string claimType,
            string findType,
            StringComparison stringComparison)
        {
            using BinaryReader reader = CreateReaderWithClaim(claimType);
            ClaimsIdentity id = new(reader, stringComparison: stringComparison);

            Claim found = Assert.Single(id.FindAll(findType));
            Assert.NotNull(found);
            Assert.Equal(claimType, found.Type);
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCulture)]
        [InlineData("Type", "type", StringComparison.Ordinal)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal)]
        public void FindAll_BinaryReader_WithStringComparison_NoMatch(
            string claimType,
            string findType,
            StringComparison stringComparison)
        {
            using BinaryReader reader = CreateReaderWithClaim(claimType);
            ClaimsIdentity id = new(reader, stringComparison: stringComparison);

            Assert.Empty(id.FindAll(findType));
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("Type", "typ\u0000e", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("Type", "type", StringComparison.OrdinalIgnoreCase)]
        public void HasClaim_BinaryReader_WithStringComparison_Match(
            string claimType,
            string findType,
            StringComparison stringComparison)
        {
            using BinaryReader reader = CreateReaderWithClaim(claimType);
            ClaimsIdentity id = new(reader, stringComparison: stringComparison);

            AssertExtensions.TrueExpression(id.HasClaim(findType, "value"));
        }

        [Theory]
        [InlineData("Type", "type", StringComparison.InvariantCulture)]
        [InlineData("Type", "type", StringComparison.Ordinal)]
        [InlineData("Type", "typ\u0000e", StringComparison.Ordinal)]
        public void HasClaim_BinaryReader_WithStringComparison_NoMatch(
            string claimType,
            string findType,
            StringComparison stringComparison)
        {
            using BinaryReader reader = CreateReaderWithClaim(claimType);
            ClaimsIdentity id = new(reader, stringComparison: stringComparison);

            AssertExtensions.FalseExpression(id.HasClaim(findType, "value"));
        }

        [Fact]
        public void HasClaim_TypeValue()
        {
            var id = new ClaimsIdentity(
            new[] {
                new Claim ("claim_type", "claim_value"),
                new Claim (ClaimsIdentity.DefaultNameClaimType, "claim_name_value"),
                new Claim ("claim_role_type", "claim_role_value"),
            }, "test_authority");

            Assert.True(id.HasClaim("claim_type", "claim_value"));
            Assert.True(id.HasClaim("cLaIm_TyPe", "claim_value"));
            Assert.False(id.HasClaim("claim_type", "cLaIm_VaLuE"));
            Assert.False(id.HasClaim("Xclaim_type", "claim_value"));
            Assert.False(id.HasClaim("claim_type", "Xclaim_value"));
        }

        [Fact]
        public static void Clone_PreservesStringComparison()
        {
            Claim claim = new("Type", "value");

            ClaimsIdentity id = new(
                claims: [claim],
                stringComparison: StringComparison.Ordinal);

            ClaimsIdentity clone = id.Clone();
            AssertExtensions.FalseExpression(clone.HasClaim("TYPE", "value"));
        }

        private static BinaryReader CreateReaderWithClaim(string claimType)
        {
            Claim claim = new(claimType, "value");
            ClaimsIdentity id = new(claims: [claim]);
            MemoryStream stream = new();

            using (BinaryWriter writer = new(stream, Encoding.UTF8, true))
            {
                id.WriteTo(writer);
            }

            stream.Position = 0;
            return new BinaryReader(stream, Encoding.UTF8, false);
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

            public CustomClaimsIdentity(ClaimsIdentity claimsIdentity, StringComparison comparison)
                : base(claimsIdentity, comparison)
            {
            }

            public CustomClaimsIdentity(ClaimsIdentity claimsIdentity): base(claimsIdentity)
            {
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
            }
        }
    }

    internal class NonClaimsIdentity : IIdentity
    {
        public string AuthenticationType { get; set; }
        public bool IsAuthenticated { get { return true; } }
        public string Name { get; set; }
    }
}
