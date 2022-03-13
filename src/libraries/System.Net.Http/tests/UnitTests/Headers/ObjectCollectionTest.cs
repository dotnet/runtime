// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class ObjectCollectionTest
    {
        [Fact]
        public void Ctor_ExecuteBothOverloads_MatchExpectation()
        {
            // Use default validator
            ObjectCollection<string> c = new UnvalidatedObjectCollection<string>();

            c.Add("value1");
            c.Add("value2");

            Assert.Throws<ArgumentNullException>(() => { c.Add(null); });

            Assert.Equal(2, c.Count);
            Assert.True(c.Contains("value2"));
            Assert.True(c.Contains("value1"));

            // Use custom validator
            c = new StringlyItemCollection();

            c.Add("value1");

            Assert.Throws<InvalidOperationException>(() => { c.Add(null); });
        }

        [Fact]
        public void ContainsAndRemove_UsesEqualitySemantics()
        {
            var c = new UnvalidatedObjectCollection<string>();

            string val1 = "value1";
            string val1DifferentReference = "value" + 1;
            Assert.NotSame(val1, val1DifferentReference);
            Assert.Equal(val1, val1DifferentReference);

            string val2 = "value2";
            string val2DifferentReference = "value" + 2;
            Assert.NotSame(val2, val2DifferentReference);
            Assert.Equal(val2, val2DifferentReference);

            string val3 = "value3";

            // Start empty
            Assert.Equal(0, c.Count);
            Assert.False(c.Contains(val1));

            // Single item
            c.Add(val1);
            Assert.Equal(1, c.Count);
            Assert.True(c.Contains(val1));
            Assert.True(c.Contains(val1DifferentReference));
            Assert.False(c.Contains(val2));

            // Single item removal
            Assert.True(c.Remove(val1));
            Assert.Equal(0, c.Count);
            Assert.False(c.Contains(val1));

            // Multi-value
            c.Add(val1);
            c.Add(val2);
            Assert.Equal(2, c.Count);
            Assert.True(c.Contains(val1));
            Assert.True(c.Contains(val1DifferentReference));
            Assert.True(c.Contains(val2));
            Assert.True(c.Contains(val1DifferentReference));
            Assert.False(c.Contains(val3));

            // Removal when multiple exist, using different reference.
            Assert.True(c.Remove(val1));
            Assert.False(c.Contains(val1));
            Assert.True(c.Contains(val2));
            Assert.Equal(1, c.Count);

            // Removal of non-existent
            Assert.False(c.Remove(val3));
            Assert.False(c.Remove(val1DifferentReference));
            Assert.Equal(1, c.Count);
            Assert.True(c.Contains(val2DifferentReference));

            // Removal last item
            Assert.True(c.Remove(val2DifferentReference));
            Assert.Equal(0, c.Count);
            Assert.False(c.Contains(val2));
            Assert.False(c.Contains(val1));

            // Remove from empty
            Assert.False(c.Remove(val1));
            Assert.False(c.Remove(val2));
            Assert.False(c.Remove(val3));
            Assert.Equal(0, c.Count);
        }

        private sealed class StringlyItemCollection : ObjectCollection<string>
        {
            public override void Validate(string item)
            {
                if (item == null)
                {
                    throw new InvalidOperationException("custom");
                }
            }
        }
    }
}
