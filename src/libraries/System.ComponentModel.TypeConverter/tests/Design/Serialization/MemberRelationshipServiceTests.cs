// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
    public class MemberRelationshipServiceTests
    {
        [Fact]
        public void Indexer_Source_Success()
        {
            var owner = new object();
            MemberDescriptor member = TypeDescriptor.GetProperties(typeof(TestClass))[0];

            MemberRelationship source = new MemberRelationship(owner, member);
            MemberRelationship memberRelationship = new MemberRelationship(new object(), TypeDescriptor.GetProperties(typeof(TestClass))[1]);

            var service = new TestMemberRelationshipService();
            service[source] = memberRelationship;
            Assert.Equal(memberRelationship, service[source]);
        }

        [Fact]
        public void Indexer_SourceOwnerSourceMember_Success()
        {
            var owner = new object();
            MemberDescriptor member = TypeDescriptor.GetProperties(typeof(TestClass))[0];

            MemberRelationship memberRelationship = new MemberRelationship(new object(), TypeDescriptor.GetProperties(typeof(TestClass))[1]);

            var service = new TestMemberRelationshipService();
            service[owner, member] = memberRelationship;
            Assert.Equal(memberRelationship, service[owner, member]);
        }

        [Fact]
        public void Indexer_NoSuchSourceOwnerOrSourceMember_ReturnsEmpty()
        {
            var owner = new object();
            MemberDescriptor member = TypeDescriptor.GetProperties(typeof(TestClass))[0];

            var service = new TestMemberRelationshipService();
            Assert.Equal(MemberRelationship.Empty, service[owner, member]);
        }

        [Fact]
        public void Indexer_NullSourceOwner_ThrowsArgumentNullException()
        {
            MemberDescriptor member = TypeDescriptor.GetProperties(typeof(TestClass))[0];

            var service = new TestMemberRelationshipService();
            Assert.Throws<ArgumentNullException>("sourceOwner", () => service[null, member]);
            Assert.Throws<ArgumentNullException>("sourceOwner", () => service[null, member] = new MemberRelationship());

            Assert.Throws<ArgumentNullException>("Owner", () => service[new MemberRelationship()]);
            Assert.Throws<ArgumentNullException>("Owner", () => service[new MemberRelationship()] = new MemberRelationship());
        }

        [Fact]
        public void Indexer_NullSourceMember_ThrowsArgumentNullException()
        {
            var service = new TestMemberRelationshipService();
            Assert.Throws<ArgumentNullException>("sourceMember", () => service[new object(), null]);
            Assert.Throws<ArgumentNullException>("sourceMember", () => service[new object(), null] = new MemberRelationship());
        }

        public static IEnumerable<object[]> IndexerSource_TestData()
        {
            yield return new object[] { new object() };

            var component = new Component();
            new Container().Add(component, "Name");
            yield return new object[] { component };
        }

        [Theory]
        [MemberData(nameof(IndexerSource_TestData))]
        public void Indexer_SetNotSupported_ThrowsArgumentException(object owner)
        {
            MemberDescriptor member = TypeDescriptor.GetProperties(typeof(TestClass))[0];
            MemberRelationship source = new MemberRelationship(owner, member);

            var service = new NotSupportingMemberRelationshipService();
            Assert.Throws<ArgumentException>(null, () => service[source] = source);
        }

        private class TestClass
        {
            public string Property1 { get; set; }
            public string Property2 { get; set; }
        }

        private class NotSupportingMemberRelationshipService : MemberRelationshipService
        {
            public override bool SupportsRelationship(MemberRelationship source, MemberRelationship relationship) => false;
        }

        private class TestMemberRelationshipService : MemberRelationshipService
        {
            public override bool SupportsRelationship(MemberRelationship source, MemberRelationship relationship) => true;
        }
    }
}
