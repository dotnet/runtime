// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ActivityTagsCollectionTests : IDisposable
    {
        private readonly static KeyValuePair<string, object> [] s_list = new KeyValuePair<string, object>[]
        {
            new KeyValuePair<string, object>("Key1", "Value1"),
            new KeyValuePair<string, object>("Key2", "Value2"),
            new KeyValuePair<string, object>("Key3", "Value3"),
            new KeyValuePair<string, object>("Key4", "Value4"),

            // Duplicate Keys to replace the old one
            new KeyValuePair<string, object>("Key3", 3),
            new KeyValuePair<string, object>("Key4", true),

            // null values is not allowed.
            new KeyValuePair<string, object>("Key5", null),
        };


        [Fact]
        public void TestDefaultConstructor()
        {
            ActivityTagsCollection tags = new ActivityTagsCollection();
            Assert.Equal(0, tags.Count);
            Assert.Equal(0, tags.Keys.Count);
            Assert.Equal(0, tags.Values.Count);
            Assert.False(tags.Remove(""));
            Assert.False(tags.Remove(new KeyValuePair<string, object>("", null)));
            Assert.False(tags.TryGetValue("", out object _));
            Assert.False(tags.Contains(new KeyValuePair<string, object>("", null)));
            Assert.False(tags.ContainsKey(""));
            Assert.False(tags.IsReadOnly);
            Assert.Null(tags[""]);
        }

        [Fact]
        public void TestNonDefaultConstructor()
        {
            ActivityTagsCollection tags = new ActivityTagsCollection(s_list);

            Assert.Equal(4, tags.Count);
            Assert.Equal(4, tags.Keys.Count);
            Assert.Equal(4, tags.Values.Count);

            Assert.Equal(3, tags["Key3"]);
            Assert.True((bool) tags["Key4"]);
        }

        [Fact]
        public void TestAdd()
        {
            ActivityTagsCollection tags = new ActivityTagsCollection();
            tags.Add("k1", "v1");
            tags.Add("k2", 2);
            tags.Add("k3", new List<string>());

            Assert.Equal(3, tags.Count);
            Assert.Equal("v1", tags["k1"]);
            Assert.Equal(2, tags["k2"]);
            Assert.True(tags["k3"] is List<string>);
            Assert.Null(tags["k4"]);

            AssertExtensions.Throws<ArgumentNullException>(() => tags.Add(null, "v"));
            AssertExtensions.Throws<InvalidOperationException>(() => tags.Add("k1", "v"));
        }

        [Fact]
        public void TestTryGetValue()
        {
            ActivityTagsCollection tags = new ActivityTagsCollection();
            tags.Add("k1", "v1");
            tags.Add("k2", 2);

            var list = new List<string>();
            tags.Add("k3", list);

            Assert.True(tags.TryGetValue("k1", out object o));
            Assert.Equal("v1", o);

            Assert.True(tags.TryGetValue("k2", out o));
            Assert.Equal(2, o);

            Assert.True(tags.TryGetValue("k3", out o));
            Assert.Equal(list, tags["k3"]);

            Assert.False(tags.TryGetValue("k4", out o));
        }

        [Fact]
        public void TestIndexer()
        {
            ActivityTagsCollection tags = new ActivityTagsCollection();
            Assert.Null(tags["k1"]);
            Assert.Equal(0, tags.Count);

            tags["k1"] = "v1";
            Assert.Equal("v1", tags["k1"]);
            Assert.Equal(1, tags.Count);

            tags["k1"] = "v2";
            Assert.Equal("v2", tags["k1"]);
            Assert.Equal(1, tags.Count);

            tags["k1"] = null;
            Assert.Null(tags["k1"]);
            Assert.Equal(0, tags.Count);

            AssertExtensions.Throws<ArgumentNullException>(() => tags[null] = "");
        }

        [Fact]
        public void TestContains()
        {
            ActivityTagsCollection tags = new ActivityTagsCollection(s_list);
            Assert.True(tags.ContainsKey("Key1"));
            Assert.True(tags.Contains(s_list[0]));

            Assert.True(tags.ContainsKey("Key2"));
            Assert.True(tags.Contains(s_list[1]));

            Assert.True(tags.ContainsKey("Key3"));
            Assert.False(tags.Contains(s_list[2]));
            Assert.True(tags.Contains(s_list[4]));

            Assert.True(tags.ContainsKey("Key4"));
            Assert.False(tags.Contains(s_list[3]));
            Assert.True(tags.Contains(s_list[5]));
        }

        [Fact]
        public void TestRemove()
        {
            ActivityTagsCollection tags = new ActivityTagsCollection(s_list);
            Assert.True(tags.ContainsKey("Key1"));
            Assert.True(tags.Remove("Key1"));
            Assert.False(tags.ContainsKey("Key1"));

            Assert.True(tags.ContainsKey("Key2"));
            Assert.True(tags.Remove(new KeyValuePair<string, object>("Key2", "Value2")));
            Assert.False(tags.ContainsKey("Key2"));

            Assert.True(tags.Contains(new KeyValuePair<string, object>("Key3", 3)));
            Assert.False(tags.Remove(new KeyValuePair<string, object>("Key3", 4)));
            Assert.True(tags.Remove(new KeyValuePair<string, object>("Key3", 3)));
        }

        [Fact]
        public void TestEnumeration()
        {
            var list = new KeyValuePair<string, object>[20];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = new KeyValuePair<string, object>(i.ToString(), i);
            }

            ActivityTagsCollection tags = new ActivityTagsCollection(list);

            int index = 0;
            foreach (KeyValuePair<string, object> kvp in tags)
            {
                Assert.Equal(new KeyValuePair<string, object>(index.ToString(), index), kvp);
                index++;
            }
            Assert.Equal(list.Length, index);

            index = 0;
            foreach (string key in tags.Keys)
            {
                Assert.Equal(index.ToString(), key);
                index++;
            }
            Assert.Equal(list.Length, index);

            index = 0;
            foreach (object value in tags.Values)
            {
                Assert.Equal(index, value);
                index++;
            }
            Assert.Equal(list.Length, index);
        }


        public void Dispose() => Activity.Current = null;
    }
}
