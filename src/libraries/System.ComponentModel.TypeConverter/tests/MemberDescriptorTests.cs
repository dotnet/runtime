// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class MemberDescriptorTests
    {
        static MemberDescriptorTests()
        {
            if (PlatformDetection.IsFullFramework)
            {
                // MethodDescriptor.Equals checks for this quirk and if it is set to true the behavior will be different to what we expect and the test will fail.
                // This quirk is set to true by default on .NET versions earlier than 4.6.1 and Xunit runner was built against 4.5, so we override the quirk value to have the expected behavior.
                AppContext.SetSwitch(@"Switch.System.MemberDescriptorEqualsReturnsFalseIfEquivalent", false);
            }
        }

        [Fact]
        public void CopiedMemberDescriptorEqualsItsSource()
        {
            var attributes = new Attribute[]
            {
                new CategoryAttribute("category"),
                new DescriptionAttribute("description")
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
            Assert.Throws<ArgumentException>(() => new MockMemberDescriptor((string)null));
            Assert.Throws<ArgumentException>(() => new MockMemberDescriptor(""));
        }

        [Fact]
        public void MemberDescriptorFromNameAndAttributes()
        {
            Assert.Throws<ArgumentException>(() => new MockMemberDescriptor((string)null, new Attribute[0]));
            Assert.Throws<ArgumentException>(() => new MockMemberDescriptor("", new Attribute[0]));

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
