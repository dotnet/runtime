// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class MemberInfoTests
    {
        [Fact]
        public void MetadataToken()
        {
            Assert.True(Enumerable.SequenceEqual(GetMembers(typeof(SampleClass)), GetMembers(typeof(SampleClass))));
            Assert.True(Enumerable.SequenceEqual(GetMembers(new MemberInfoTests().GetType()), GetMembers(new MemberInfoTests().GetType())));
            Assert.True(Enumerable.SequenceEqual(GetMembers(new Dictionary<int, string>().GetType()), GetMembers(new Dictionary<int, int>().GetType())));
            Assert.True(Enumerable.SequenceEqual(GetMembers(typeof(int)), GetMembers(typeof(int))));
            Assert.True(Enumerable.SequenceEqual(GetMembers(typeof(Dictionary<,>)), GetMembers(typeof(Dictionary<,>))));
            Assert.False(Enumerable.SequenceEqual(GetMembers(new Dictionary<int, string>().GetType()), GetMembers(new HashSet<int>().GetType())));
        }

        private IEnumerable<int> GetMembers(Type type)
        {
            return type.GetTypeInfo().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Select(m => m.MetadataToken);
        }

#pragma warning disable 0067, 0169
        public class SampleClass
        {
            public int PublicField;
            private int PrivateField;

            public SampleClass() { }
            private SampleClass(int x) { }

            public void PublicMethod() { }
            private void PrivateMethod() { }

            public int PublicProp { get; set; }
            private int PrivateProp { get; set; }

            public event EventHandler PublicEvent;
            private event EventHandler PrivateEvent;
        }
#pragma warning restore 0067, 0169
    }
}