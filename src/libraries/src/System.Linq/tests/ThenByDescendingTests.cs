// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ThenByDescendingTests
    {
        private struct Record
        {
#pragma warning disable 0649
            public string Name;
            public string City;
            public string Country;
#pragma warning restore 0649
        }
        
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x1 in new int[] { 1, 6, 0, -1, 3 }
                             from x2 in new int[] { 55, 49, 9, -100, 24, 25 }
                             select new { a1 = x1, a2 = x2 };

            Assert.Equal(
                q.OrderByDescending(e => e.a2).ThenByDescending(f => f.a1), 
                q.OrderByDescending(e => e.a2).ThenByDescending(f => f.a1)
            );
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x1 in new[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                             from x2 in new[] { "!@#$%^", "C", "AAA", "", null, "Calling Twice", "SoS", String.Empty }
                             where !String.IsNullOrEmpty(x2)
                             select new { a1 = x1, a2 = x2 };
                    
            Assert.Equal(
                q.OrderBy(e => e.a1).ThenByDescending(f => f.a2),
                q.OrderBy(e => e.a1).ThenByDescending(f => f.a2)
            );
        }

        [Fact]
        public void SourceEmpty()
        {
            int[] source = { };
            int[] expected = { };

            Assert.Equal(expected, source.OrderBy(e => e).ThenByDescending(e => e));
        }

        [Fact]
        public void AscendingKeyThenDescendingKey()
        {
            Record[] source = new Record[]
            {
                new Record{ Name = "Jim", City = "Minneapolis", Country = "USA" },
                new Record{ Name = "Tim", City = "Seattle", Country = "USA" },
                new Record{ Name = "Philip", City = "Orlando", Country = "USA" },
                new Record{ Name = "Chris", City = "London", Country = "UK" },
                new Record{ Name = "Rob", City = "Kent", Country = "UK" }
            };
            Record[] expected = new Record[]
            {
                new Record{ Name = "Chris", City = "London", Country = "UK" },
                new Record{ Name = "Rob", City = "Kent", Country = "UK" },
                new Record{ Name = "Tim", City = "Seattle", Country = "USA" },
                new Record{ Name = "Philip", City = "Orlando", Country = "USA" },
                new Record{ Name = "Jim", City = "Minneapolis", Country = "USA" }
            };

            Assert.Equal(expected, source.OrderBy((e) => e.Country).ThenByDescending((e) => e.City));
        }

        [Fact]
        public void DescendingKeyThenDescendingKey()
        {
            Record[] source = new Record[]
            {
                new Record{ Name = "Jim", City = "Minneapolis", Country = "USA" },
                new Record{ Name = "Tim", City = "Seattle", Country = "USA" },
                new Record{ Name = "Philip", City = "Orlando", Country = "USA" },
                new Record{ Name = "Chris", City = "London", Country = "UK" },
                new Record{ Name = "Rob", City = "Kent", Country = "UK" }
            };
            Record[] expected = new Record[]
            {
                new Record{ Name = "Tim", City = "Seattle", Country = "USA" },
                new Record{ Name = "Philip", City = "Orlando", Country = "USA" },
                new Record{ Name = "Jim", City = "Minneapolis", Country = "USA" },
                new Record{ Name = "Chris", City = "London", Country = "UK" },
                new Record{ Name = "Rob", City = "Kent", Country = "UK" }
            };

            Assert.Equal(expected, source.OrderByDescending((e) => e.Country).ThenByDescending((e) => e.City));
        }

        [Fact]
        public void OrderIsStable()
        {
            var source = @"Because I could not stop for Death —
He kindly stopped for me —
The Carriage held but just Ourselves —
And Immortality.".Split(new []{ ' ', '\n', '\r', '—' }, StringSplitOptions.RemoveEmptyEntries);
            var expected = new []
            {
                "stopped", "kindly", "could", "stop", "held", "just", "not", "for", "for", "but", "me",
                "Immortality.", "Ourselves", "Carriage", "Because", "Death", "The", "And", "He", "I"   
            };
            
            Assert.Equal(expected, source.OrderBy(word => char.IsUpper(word[0])).ThenByDescending(word => word.Length));
        }

        [Fact]
        public void OrderIsStableCustomComparer()
        {
            var source = @"Because I could not stop for Death —
He kindly stopped for me —
The Carriage held but just Ourselves —
And Immortality.".Split(new[] { ' ', '\n', '\r', '—' }, StringSplitOptions.RemoveEmptyEntries);
            var expected = new[]
            {
                "me", "not", "for", "for", "but", "stop", "held", "just", "could", "kindly", "stopped",
                "I", "He", "The", "And", "Death", "Because", "Carriage", "Ourselves", "Immortality."
            };

            Assert.Equal(expected, source.OrderBy(word => char.IsUpper(word[0])).ThenByDescending(word => word.Length, Comparer<int>.Create((w1, w2) => w2.CompareTo(w1))));
        }


        [Fact]
        public void NullSource()
        {
            IOrderedEnumerable<int> source = null;
            Assert.Throws<ArgumentNullException>(() => source.ThenByDescending(i => i));
        }
    }
}
