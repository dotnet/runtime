// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
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
    }

    internal class NonClaimsIdentity : IIdentity
    {
        public string AuthenticationType { get; set; }
        public bool IsAuthenticated { get { return true; } }
        public string Name { get; set; }
    }
}
