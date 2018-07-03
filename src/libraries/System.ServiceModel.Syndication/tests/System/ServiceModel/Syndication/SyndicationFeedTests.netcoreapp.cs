// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public partial class SyndicationFeedTests
    {
        [Fact]
        public void Documentation_Get_ReturnsExpected()
        {
            var feed = new SyndicationFeed();
            Assert.Null(feed.Documentation);
        }

        [Fact]
        public void Documentation_Set_GetReturnsExpected()
        {
            var feed = new SyndicationFeed();
            var documentation = new SyndicationLink();
            feed.Documentation = documentation;
            Assert.Same(documentation, feed.Documentation);

            feed.Documentation = null;
            Assert.Null(feed.Documentation);
        }

        [Fact]
        public void SkipHours_Add_Success()
        {
            Collection<int> collection = new SyndicationFeed().SkipHours;
            collection.Add(10);
            collection.Add(-1);
            Assert.Equal(2, collection.Count);
        }

        [Fact]
        public void SkipHours_SetItem_GetReturnsExpected()
        {
            Collection<int> collection = new SyndicationFeed().SkipHours;
            collection.Add(10);

            collection[0] = 5;
            Assert.Equal(5, collection[0]);

            collection[0] = -1;
            Assert.Equal(-1, collection[0]);
        }

        [Fact]
        public void SkipDays_Add_Success()
        {
            Collection<string> collection = new SyndicationFeed().SkipDays;
            collection.Add("days");
            collection.Add(null);
            Assert.Equal(2, collection.Count);
        }

        [Fact]
        public void SkipDays_SetItem_GetReturnsExpected()
        {
            Collection<string> collection = new SyndicationFeed().SkipDays;
            collection.Add("monday");

            string newValue = "newValue";
            collection[0] = newValue;
            Assert.Same(newValue, collection[0]);

            collection[0] = null;
            Assert.Null(collection[0]);
        }

        [Fact]
        public void TextInput_Get_ReturnsExpected()
        {
            var feed = new SyndicationFeed();
            Assert.Null(feed.TextInput);
        }

        [Fact]
        public void TextInput_Set_GetReturnsExpected()
        {
            var feed = new SyndicationFeed();
            var input = new SyndicationTextInput();
            feed.TextInput = input;
            Assert.Same(input, feed.TextInput);

            feed.TextInput = null;
            Assert.Null(feed.TextInput);
        }

        [Fact]
        public void Ctor_NoNetcoreappProperties_Success()
        {
            var input = new SyndicationTextInput();
            var feed = new SyndicationFeed();
            var clone = new SyndicationFeedSubclass(feed, cloneItems: false);
            Assert.Null(clone.Documentation);
            Assert.Empty(clone.SkipDays);
            Assert.Empty(clone.SkipHours);
            Assert.Null(clone.TextInput);
            Assert.Null(clone.TimeToLive);
        }

        [Fact]
        public void Ctor_HasNetcoreappProperties_Success()
        {
            var input = new SyndicationTextInput();
            var feed = new SyndicationFeed
            {
                Documentation = new SyndicationLink(new Uri("http://documentation_link.com")),
                TextInput = input,
                TimeToLive = new TimeSpan(10)
            };
            feed.SkipDays.Add("monday");
            feed.SkipHours.Add(2);

            var clone = new SyndicationFeedSubclass(feed, cloneItems: false);
            Assert.Null(clone.Documentation);
            Assert.Empty(clone.SkipDays);
            Assert.Empty(clone.SkipHours);
            Assert.Null(clone.TextInput);
            Assert.Null(clone.TimeToLive);
        }
    }
}
