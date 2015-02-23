using System;

using Xunit;

namespace System.Diagnostics.Tests
{
    class AttributeTests
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


            // All other values are invalid.
            for(int i = int.MinValue; i < 0; i++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new DebuggerBrowsableAttribute((DebuggerBrowsableState)i));
            }

            for(int i = 4; i <= int.MaxValue; i++)
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
        void NullIsInvalidForDebuggerDisplayType()
        {
            Assert.Throws<ArgumentNullException>(() => (new DebuggerDisplayAttribute("myValue")).Type = null);
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
        }
    }
}
