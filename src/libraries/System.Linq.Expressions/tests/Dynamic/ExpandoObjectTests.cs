// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace System.Dynamic.Tests
{
    // Tests for ExpandoObject accessed directly, rather than through dynamic.
    public class ExpandoObjectTests
    {
        [Fact]
        public void InitialState()
        {
            IDictionary<string, object> eo = new ExpandoObject();
            Assert.Equal(0, eo.Count);
            Assert.False(eo.IsReadOnly);
        }

        [Fact]
        public void Indexer()
        {
            // Deliberately using keys that the C# syntax won't use, to check they're covered too.
            var boxedInts = Enumerable.Range(0, 10).Select(i => (object)i).ToArray();
            IDictionary<string, object> eo = new ExpandoObject();
            foreach (var boxed in boxedInts)
                eo[boxed.ToString()] = boxed;

            Assert.Equal(10, eo.Count);
            foreach (var boxed in boxedInts)
                Assert.Same(boxed, eo[boxed.ToString()]);

            var knfe = Assert.Throws<KeyNotFoundException>(() => eo["A string that's a key"]);
            Assert.Contains("A string that's a key", knfe.Message);

            Assert.Throws<KeyNotFoundException>(() => eo[null]);
            AssertExtensions.Throws<ArgumentNullException>("key", () => eo[null] = 0);
            // Can overwrite
            eo["1"] = 1;
        }

        [Fact]
        public void AddDup()
        {
            IDictionary<string, object> eo = new ExpandoObject();
            eo.Add("The test key to add.", "value");
            var ae = AssertExtensions.Throws<ArgumentException>("key", () => eo.Add("The test key to add.", "value"));
            Assert.Contains("The test key to add.", ae.Message);
        }

        [Fact]
        public void DeleteAndReInsert()
        {
            IDictionary<string, object> eo = new ExpandoObject();
            eo.Add("key", 1);
            Assert.Equal(Enumerable.Repeat(new KeyValuePair<string, object>("key", 1), 1), eo);
            eo.Remove("key");
            Assert.Empty(eo);
            eo.Add("key", 2);
            Assert.Equal(Enumerable.Repeat(new KeyValuePair<string, object>("key", 2), 1), eo);
        }

        [Fact, ActiveIssue(13541)]
        public void DictionaryMatchesProperties()
        {
            dynamic eo = new ExpandoObject();
            IDictionary<string, object> dict = eo;
            eo.X = 1;
            eo.Y = 2;
            Assert.Equal(2, dict.Count);
            Assert.Equal(1, dict["X"]);
            Assert.Equal(2, dict["Y"]);
        }

        [Fact]
        public void PropertyNotify()
        {
            var eo = new ExpandoObject();
            IDictionary<string, object> dict = eo;
            INotifyPropertyChanged note = eo;
            int changeCount = 0;
            string lastKey = null;
            note.PropertyChanged += (sender, args) =>
            {
                Assert.Same(eo, sender);
                ++changeCount;
                lastKey = args.PropertyName;
            };
            Assert.Equal(0, changeCount);
            dict.Add("X", 1);
            Assert.Equal(1, changeCount);
            Assert.Equal(lastKey, "X");
            dict["Y"] = 2;
            Assert.Equal(2, changeCount);
            Assert.Equal(lastKey, "Y");
            object boxed = 0;
            dict["Z"] = boxed;
            Assert.Equal(3, changeCount);
            Assert.Equal("Z", lastKey);
            dict["Z"] = boxed; // exact same object.
            Assert.Equal(3, changeCount);
            dict["Z"] = 0; // same value but different instance.
            Assert.Equal(4, changeCount);
            dict.Remove("Not there");
            Assert.Equal(4, changeCount);
            dict.Remove("Z");
            Assert.Equal(5, changeCount);
            dict.Clear();
            Assert.Equal(7, changeCount); // trigger for each key.
        }

        [Fact]
        public void KeyCollection()
        {
            IDictionary<string, object> dict = new ExpandoObject();
            dict["X"] = 1;
            dict["Z"] = 3;
            dict["Y"] = 2;
            dict.Remove("Z");
            var keys = dict.Keys;
            Assert.Equal(2, keys.Count);
            Assert.True(keys.IsReadOnly);
            Assert.Equal(new[] {"X", "Y"}, keys.OrderBy(k => k)); // OrderBy because order is not guaranteed.
            Assert.Throws<NotSupportedException>(() => keys.Add("Z"));
            Assert.Throws<NotSupportedException>(() => keys.Clear());
            Assert.Throws<NotSupportedException>(() => keys.Remove("X"));
            Assert.True(keys.Contains("X"));
            Assert.False(keys.Contains("x"));
            string[] array = new string[3];
            keys.CopyTo(array, 1);
            Assert.Null(array[0]);
            Array.Sort(array); // Because order is not guaranteed.
            Assert.Equal(new[] {null, "X", "Y"}, array);
            var keysEnumerated = new List<object>();
            foreach (var key in (IEnumerable)keys) // going through non-generic to test delegation to generic
            {
                keysEnumerated.Add(key);
            }
            Assert.Equal(new[] {"X", "Y"}, keysEnumerated.OrderBy(k => k));
            using (var en = keys.GetEnumerator())
            {
                en.MoveNext();
                dict["Z"] = 3;
                Assert.Throws<InvalidOperationException>(() => en.MoveNext());
            }
        }

        [Fact]
        public void ValueCollection()
        {
            IDictionary<string, object> dict = new ExpandoObject();
            dict["X"] = 1;
            dict["Z"] = 3;
            dict["Y"] = 2;
            dict.Remove("Z");
            var values = dict.Values;
            Assert.Equal(2, values.Count);
            Assert.True(values.IsReadOnly);
            Assert.Equal(new object[] {1, 2}, values.OrderBy(k => k)); // OrderBy because order is not guaranteed.
            Assert.Throws<NotSupportedException>(() => values.Add(3));
            Assert.Throws<NotSupportedException>(() => values.Clear());
            Assert.Throws<NotSupportedException>(() => values.Remove(1));
            Assert.True(values.Contains(1));
            Assert.False(values.Contains(1.0));
            object[] array = new object[3];
            values.CopyTo(array, 1);
            Assert.Null(array[0]);
            Array.Sort(array); // Because order is not guaranteed.
            Assert.Equal(new object[] {null, 1, 2}, array);
            var valuesEnumerated = new List<object>();
            foreach (var value in (IEnumerable)values) // going through non-generic to test delegation to generic
            {
                valuesEnumerated.Add(value);
            }
            Assert.Equal(new object[] {1, 2}, valuesEnumerated.OrderBy(k => k));
            using (var en = values.GetEnumerator())
            {
                en.MoveNext();
                dict["Z"] = 3;
                Assert.Throws<InvalidOperationException>(() => en.MoveNext());
            }
        }
    }
}
