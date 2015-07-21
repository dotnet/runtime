// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Xunit;

namespace System.Diagnostics.Tests
{
    public class AttributeTests
    {
        [Fact]
        void DebuggerBrowsableAttributeOnlyAllowsKnownModes()
        {
            new DebuggerBrowsableAttribute(DebuggerBrowsableState.Never);
            new DebuggerBrowsableAttribute(DebuggerBrowsableState.Collapsed);
            new DebuggerBrowsableAttribute(DebuggerBrowsableState.RootHidden);

            // "1" would corespond to DebuggerBrowsableState.Expanded if it was
            // present.  The current implementation allows this value even though
            // it is not part of the enum.
            new DebuggerBrowsableAttribute((DebuggerBrowsableState)1);

            // All other values are invalid.  Test a few...
            foreach (int i in new[] { int.MinValue, -10, -1, 4, 5, 10, int.MaxValue })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new DebuggerBrowsableAttribute((DebuggerBrowsableState)i));
            }
        }

        [Fact]
        void DebuggerBrowsableStateStateValueIsRetained()
        {
            Assert.Equal<DebuggerBrowsableState>(DebuggerBrowsableState.Never, (new DebuggerBrowsableAttribute(DebuggerBrowsableState.Never)).State);
            Assert.Equal<DebuggerBrowsableState>(DebuggerBrowsableState.Collapsed, (new DebuggerBrowsableAttribute(DebuggerBrowsableState.Collapsed)).State);
            Assert.Equal<DebuggerBrowsableState>(DebuggerBrowsableState.RootHidden, (new DebuggerBrowsableAttribute(DebuggerBrowsableState.RootHidden)).State);
        }

        [Fact]
        void EmptyAttributesCanBeConstructed()
        {
            new DebuggerHiddenAttribute();
            new DebuggerNonUserCodeAttribute();
            new DebuggerStepThroughAttribute();
        }

        [Fact]
        void NullIsValidForDebuggerDisplayValue()
        {
            Assert.Equal(string.Empty, new DebuggerDisplayAttribute(null).Value);
        }

        [Fact]
        void NullIsInvalidForDebuggerDisplayTarget()
        {
            Assert.Throws<ArgumentNullException>(() => (new DebuggerDisplayAttribute("myValue")).Target = null);
        }

        [Fact]
        void DebuggerDisplayAttributePropertiesRoundTrip()
        {
            DebuggerDisplayAttribute d = new DebuggerDisplayAttribute("myValue");
            d.Name = "name";
            d.Target = typeof(AttributeTests);

            Assert.Equal("myValue", d.Value);
            Assert.Equal("name", d.Name);
            Assert.Equal(typeof(AttributeTests), d.Target);
            Assert.Equal(typeof(AttributeTests).AssemblyQualifiedName, d.TargetTypeName);

            d.TargetTypeName = typeof(DebugTests).AssemblyQualifiedName;
            Assert.Equal(typeof(DebugTests).AssemblyQualifiedName, d.TargetTypeName);

            d.Type = "typeName";
            Assert.Equal("typeName", d.Type);
        }

        [Fact]
        void DebuggerTypeProxyAttributeConstruction()
        {
            Assert.Throws<ArgumentNullException>(() => new DebuggerTypeProxyAttribute((Type)null));

            Assert.Null(new DebuggerTypeProxyAttribute((string)null).TargetTypeName);
            Assert.Equal(typeof(DebugTests).AssemblyQualifiedName, new DebuggerTypeProxyAttribute(typeof(DebugTests)).ProxyTypeName);
            Assert.Equal(typeof(DebugTests).AssemblyQualifiedName, new DebuggerTypeProxyAttribute(typeof(DebugTests).AssemblyQualifiedName).ProxyTypeName);
        }

        [Fact]
        void DebuggerTypeProxyAttributePropertiesRoundTrip()
        {
            DebuggerTypeProxyAttribute d = new DebuggerTypeProxyAttribute(typeof(DebugTests));

            d.Target = typeof(AttributeTests);
            Assert.Equal(typeof(AttributeTests), d.Target);
            Assert.Equal(typeof(AttributeTests).AssemblyQualifiedName, d.TargetTypeName);

            Assert.Throws<ArgumentNullException>(() => d.Target = null);

            d.TargetTypeName = typeof(DebugTests).AssemblyQualifiedName;
            Assert.Equal(typeof(DebugTests).AssemblyQualifiedName, d.TargetTypeName);
            Assert.Equal(typeof(AttributeTests), d.Target);
        }
    }
}
