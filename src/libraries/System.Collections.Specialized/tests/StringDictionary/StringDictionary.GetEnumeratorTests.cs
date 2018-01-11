// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryGetEnumeratorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void GetEnumerator(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);

            IEnumerator enumerator = stringDictionary.GetEnumerator();
            Assert.NotSame(stringDictionary.GetEnumerator(), stringDictionary.GetEnumerator());
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    DictionaryEntry current = (DictionaryEntry)enumerator.Current;

                    string key = (string)current.Key;
                    string value = (string)current.Value;
                    Assert.True(stringDictionary.ContainsKey(key));
                    Assert.True(stringDictionary.ContainsValue(value));

                    counter++;
                }
                Assert.Equal(stringDictionary.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public void GetEnumerator_Invalid()
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(10);
            IEnumerator enumerator = stringDictionary.GetEnumerator();

            // Enumerator has not started
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Enumerator has finished
            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.False(enumerator.MoveNext());

            // Enumerator has been reset
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.True(enumerator.MoveNext());

            // Modifying collection during enumeration
            object previousCurrent = enumerator.Current;
            stringDictionary.Add("newkey1", "newvalue");
            Assert.Equal(previousCurrent, enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

            // Modifying collection after enumeration
            enumerator = stringDictionary.GetEnumerator();
            enumerator.MoveNext();
            previousCurrent = enumerator.Current;
            stringDictionary.Add("newkey2", "newvalue");
            Assert.Equal(previousCurrent, enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        }
    }
}
