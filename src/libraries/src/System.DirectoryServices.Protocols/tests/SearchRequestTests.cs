// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class SearchRequestTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var request = new SearchRequest();
            Assert.Equal(DereferenceAlias.Never, request.Aliases);
            Assert.Empty(request.Attributes);
            Assert.Empty(request.Controls);
            Assert.Null(request.DistinguishedName);
            Assert.Null(request.Filter);
            Assert.Equal(SearchScope.Subtree, request.Scope);
            Assert.Null(request.RequestId);
            Assert.Equal(TimeSpan.Zero, request.TimeLimit);
            Assert.False(request.TypesOnly);
        }

        [Theory]
        [InlineData(null, null, SearchScope.Subtree, null)]
        [InlineData("", "", SearchScope.OneLevel, new string[0])]
        [InlineData("DistinguishedName", "LdapFilter", SearchScope.Base, new string[] { "attribute" })]
        [InlineData("DistinguishedName", "LdapFilter", SearchScope.OneLevel, new string[] { null })]
        public void Ctor_DistinguishedName_LdapFilter_SearchScope_AttributeList(string distinguishedName, string ldapFilter, SearchScope searchScope, string[] attributeList)
        {
            var request = new SearchRequest(distinguishedName, ldapFilter, searchScope, attributeList);
            Assert.Equal(DereferenceAlias.Never, request.Aliases);
            Assert.Equal(attributeList ?? Enumerable.Empty<string>(), request.Attributes.Cast<string>());
            Assert.Empty(request.Controls);
            Assert.Equal(distinguishedName, request.DistinguishedName);
            Assert.Equal(ldapFilter, request.Filter);
            Assert.Equal(searchScope, request.Scope);
            Assert.Null(request.RequestId);
            Assert.Equal(TimeSpan.Zero, request.TimeLimit);
            Assert.False(request.TypesOnly);
        }

        [Theory]
        [InlineData(SearchScope.Base - 1)]
        [InlineData(SearchScope.Subtree + 1)]
        public void Ctor_InvalidScope_ThrowsInvalidEnumArgumentException(SearchScope searchScope)
        {
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => new SearchRequest("DistinguishedName", "LdapFilter", searchScope));
        }

        [Fact]
        public void DistinguishedName_Set_GetReturnsExpected()
        {
            var request = new SearchRequest { DistinguishedName = "Name" };
            Assert.Equal("Name", request.DistinguishedName);
        }

        [Fact]
        public void Filter_Set_GetReturnsExpected()
        {
            var request = new SearchRequest { Filter = "filter" };
            Assert.Equal("filter", request.Filter);

            request.Filter = null;
            Assert.Null(request.Filter);
        }

        [Fact]
        public void Filter_SetInvalid_ThrowsArgumentException()
        {
            var request = new SearchRequest();
            AssertExtensions.Throws<ArgumentException>("value", () => request.Filter = 1);
        }

        [Fact]
        public void Aliases_SetValid_GetReturnsExpected()
        {
            var request = new SearchRequest { Aliases = DereferenceAlias.Always };
            Assert.Equal(DereferenceAlias.Always, request.Aliases);
        }

        [Theory]
        [InlineData(DereferenceAlias.Never - 1)]
        [InlineData(DereferenceAlias.Always + 1)]
        public void Aliases_SetInvalid_ThrowsInvalidEnumArgumentException(DereferenceAlias aliases)
        {
            var request = new SearchRequest();
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => request.Aliases = aliases);
        }


        [Fact]
        public void SizeLimit_SetValid_GetReturnsExpected()
        {
            var request = new SearchRequest { SizeLimit = 0 };
            Assert.Equal(0, request.SizeLimit);
        }

        [Fact]
        public void SizeLimit_SetNegative_ThrowsArgumentException()
        {
            var request = new SearchRequest();
            AssertExtensions.Throws<ArgumentException>("value", () => request.SizeLimit = -1);
        }

        [Fact]
        public void TimeLimit_SetValid_GetReturnsExpected()
        {
            var request = new SearchRequest { TimeLimit = TimeSpan.FromSeconds(1) };
            Assert.Equal(TimeSpan.FromSeconds(1), request.TimeLimit);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((long)int.MaxValue + 1)]
        public void TimeLimit_SetInvalid_ThrowsArgumentException(long totalSeconds)
        {
            var request = new SearchRequest();
            AssertExtensions.Throws<ArgumentException>("value", () => request.TimeLimit = TimeSpan.FromSeconds(totalSeconds));
        }

        [Fact]
        public void TypesOnly_Set_GetReturnsExpected()
        {
            var request = new SearchRequest { TypesOnly = true };
            Assert.True(request.TypesOnly);
        }
    }
}
