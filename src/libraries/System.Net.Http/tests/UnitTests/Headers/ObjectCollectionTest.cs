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
            ObjectCollection<string> c = new ObjectCollection<string>();

            c.Add("value1");
            c.Add("value2");

            Assert.Throws<ArgumentNullException>(() => { c.Add(null); });

            Assert.Equal(2, c.Count);
            Assert.True(c.Contains("value2"));
            Assert.True(c.Contains("value1"));

            // Use custom validator
            c = new ObjectCollection<string>(item =>
            {
                if (item == null)
                {
                    throw new InvalidOperationException("custom");
                }
            });

            c.Add("value1");

            Assert.Throws<InvalidOperationException>(() => { c.Add(null); });
        }

        [Fact]
        public void ContainsAndRemove_UsesEqualitySemantics()
        {
            // Use default validator
            ObjectCollection<string> c = new ObjectCollection<string>();

            Assert.Equal(0, c.Count);
            Assert.False(c.Contains("value" + 1));

            c.Add("value" + 1);

            Assert.Equal(1, c.Count);
            // Force the reference to be different to ensure we are checking for semantic equality
            // and not reference equality.
            Assert.True(c.Contains("value" + 1));
            c.Add("value" + 2);

            Assert.True(c.Remove("value" + 1));
            Assert.Equal(1, c.Count);

            Assert.True(c.Contains("value" + 2));
        }
    }
}
