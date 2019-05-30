// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class MemberDescriptorTests
    {
        [Fact]
        public void CopiedMemberDescriptorEqualsItsSource()
        {
            string description = "MockCategory";
            var attributes = new Attribute[]
            {
                new CategoryAttribute(description),
                // setting Decription and Category the same as .NET Framework has a bug in the equals that compares them
                // instead of each other. In .NET 4.6.2 and greater this is no longer an issue but to make the test
                // work on all platforms just setting them to be the same.
                new DescriptionAttribute(description)
            };

            var firstDescriptor = new MockMemberDescriptor(nameof(MemberDescriptor), attributes);
            var copiedDescriptor = new MockMemberDescriptor(firstDescriptor);

            // call getters to ensure their backing fields aren't null
            Assert.Equal(firstDescriptor.Category, copiedDescriptor.Category);
            Assert.Equal(firstDescriptor.Description, copiedDescriptor.Description);

            Assert.True(firstDescriptor.Equals(copiedDescriptor));
        }

        [Fact]
        public void MemberDescriptorFromName()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new MockMemberDescriptor((string)null));
            AssertExtensions.Throws<ArgumentException>(null, () => new MockMemberDescriptor(""));
        }

        [Fact]
        public void MemberDescriptorFromNameAndAttributes()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new MockMemberDescriptor((string)null, new Attribute[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new MockMemberDescriptor("", new Attribute[0]));

            var name = nameof(MemberDescriptorFromNameAndAttributes);
            var attributes = new Attribute[] { new MockAttribute1(), new MockAttribute2() };

            var descriptor = new MockMemberDescriptor(name, attributes);

            Assert.Equal(name, descriptor.Name);
            Assert.Equal(name, descriptor.DisplayName);

            Assert.Equal(attributes.OrderBy(a => a.GetHashCode()), descriptor.Attributes.Cast<Attribute>().OrderBy(a => a.GetHashCode()));
        }

        [Fact]
        public void MemberDescriptorFromMemberDescriptor()
        {
            var name = nameof(MemberDescriptorFromNameAndAttributes);
            var attributes = new Attribute[] { new MockAttribute1(), new MockAttribute2() };
            var oldDescriptor = new MockMemberDescriptor(name, attributes);

            var descriptor = new MockMemberDescriptor(oldDescriptor);

            Assert.Equal(attributes.OrderBy(a => a.GetHashCode()), descriptor.Attributes.Cast<Attribute>().OrderBy(a => a.GetHashCode()));
        }

        [Fact]
        public void DisplayNameWithAttribute()
        {
            var name = "name";
            var displayName = "displayName";
            var attributes = new Attribute[] { new DisplayNameAttribute(displayName) };

            var descriptor = new MockMemberDescriptor(name, attributes);

            Assert.Equal(name, descriptor.Name);
            Assert.Equal(displayName, descriptor.DisplayName);
        }

        private class MockMemberDescriptor : MemberDescriptor
        {
            public MockMemberDescriptor(string name)
                : base(name)
            { }

            public MockMemberDescriptor(string name, Attribute[] attributes)
                : base(name, attributes)
            { }

            public MockMemberDescriptor(MemberDescriptor other)
                : base(other)
            { }

            public MockMemberDescriptor(MemberDescriptor other, Attribute[] attributes)
                : base(other, attributes)
            { }
        }

        private sealed class MockAttribute1 : Attribute { }
        private sealed class MockAttribute2 : Attribute { }
    }
}
