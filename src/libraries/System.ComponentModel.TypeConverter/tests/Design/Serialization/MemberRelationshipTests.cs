// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Tests;
using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
    public class MemberRelationshipTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var memberRelationship = new MemberRelationship();
            Assert.Null(memberRelationship.Owner);
            Assert.Null(memberRelationship.Member);
            Assert.True(memberRelationship.IsEmpty);
        }

        [Fact]
        public void Ctor_Owner_Member()
        {
            var owner = new object();
            MemberDescriptor member = new MockPropertyDescriptor();
            var memberRelationship = new MemberRelationship(owner, member);

            Assert.Same(owner, memberRelationship.Owner);
            Assert.Same(member, memberRelationship.Member);
            Assert.False(memberRelationship.IsEmpty);
        }

        [Fact]
        public void Ctor_NullOwner_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("owner", () => new MemberRelationship(null, new MockPropertyDescriptor()));
        }

        [Fact]
        public void Ctor_NullMember_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("member", () => new MemberRelationship(1, null));
        }

        [Fact]
        public void Empty_Get_ReturnsExpected()
        {
            MemberRelationship memberRelationship = MemberRelationship.Empty;
            Assert.Null(memberRelationship.Owner);
            Assert.Null(memberRelationship.Member);
            Assert.True(memberRelationship.IsEmpty);
        }

        public static IEnumerable<object> Equals_TestData()
        {
            var owner1 = new object();
            var owner2 = new object();
            MemberDescriptor member1 = TypeDescriptor.GetProperties(typeof(TestClass))[0];
            MemberDescriptor member2 = TypeDescriptor.GetProperties(typeof(TestClass))[1];

            var memberRelationship = new MemberRelationship(owner1, member1);

            yield return new object[] { memberRelationship, memberRelationship, true };
            yield return new object[] { memberRelationship, new MemberRelationship(owner1, member1), true };
            yield return new object[] { memberRelationship, new MemberRelationship(owner2, member1), false };
            yield return new object[] { memberRelationship, new MemberRelationship(owner1, member2), false };

            yield return new object[] { memberRelationship, new object(), false };
            yield return new object[] { memberRelationship, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(MemberRelationship memberRelationship, object other, bool expected)
        {
            Assert.Equal(expected, memberRelationship.Equals(other));
            if (other is MemberRelationship otherMemberRelationship)
            {
                Assert.Equal(expected, memberRelationship == otherMemberRelationship);
                Assert.Equal(!expected, memberRelationship != otherMemberRelationship);
            }
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimesWithOwner_ReturnsEqual()
        {
            var memberRelationship = new MemberRelationship(new object(), TypeDescriptor.GetProperties(typeof(TestClass))[0]);
            Assert.Equal(memberRelationship.GetHashCode(), memberRelationship.GetHashCode());
        }

        [Fact]
        public void GetHashCode_InvokeMultipleTimesWithoutOwner_ReturnsEqual()
        {
            var memberRelationship = new MemberRelationship();
            Assert.Equal(memberRelationship.GetHashCode(), memberRelationship.GetHashCode());
        }

        private class TestClass
        {
            public string Property1 { get; set; }
            public string Property2 { get; set; }
        }
    }
}
