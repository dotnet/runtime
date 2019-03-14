// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
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
        
        [Fact]
        public void SkipHours_GetWithElementExtension_ReturnsExpected()
        {
            var feed = new SyndicationFeed();
            feed.ElementExtensions.Add(new SyndicationElementExtension("other", "", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension("skipHours", "other", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension(
                new XElement("skipHours",
                    new XElement("hour", 0),
                    new XElement("hour", 10),
                    new XElement("other", 10),
                    new XElement("hour", 23)
                ).CreateReader())
            );

            Assert.Equal(new int[] { 0, 10, 23 }, feed.SkipHours);
            Assert.Same(feed.SkipHours, feed.SkipHours);
        }
        
        [Theory]
        [InlineData(-1)]
        [InlineData(24)]
        [InlineData("invalid")]
        [InlineData("")]
        public void SkipHours_GetWithInvalidElementExtension_ThrowsFormatException(string value)
        {
            var feed = new SyndicationFeed();
            feed.ElementExtensions.Add(new SyndicationElementExtension("other", "", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension("skipHours", "other", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension(
                new XElement("skipHours",
                    new XElement("hour", 0),
                    new XElement("hour", 10),
                    new XElement("other", 10),
                    new XElement("hour", value)
                ).CreateReader())
            );

            Assert.Throws<FormatException>(() => feed.SkipHours);
        }
        
        [Fact]
        public void SkipDays_GetWithElementExtension_ReturnsExpected()
        {
            var feed = new SyndicationFeed();
            feed.ElementExtensions.Add(new SyndicationElementExtension("other", "", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension("skipDays", "other", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension(
                new XElement("skipDays",
                    new XElement("day", "monday"),
                    new XElement("day", "tuesday"),
                    new XElement("other", 10),
                    new XElement("day", "wednesday"),
                    new XElement("day", "thursday"),
                    new XElement("day", "friday"),
                    new XElement("day", "SATURDAY"),
                    new XElement("day", ""),
                    new XElement("day", "invalid"),
                    new XElement("day", "sunday")
                ).CreateReader())
            );

            Assert.Equal(new string[] { "monday", "tuesday", "wednesday", "thursday", "friday", "SATURDAY", "sunday"  }, feed.SkipDays);
            Assert.Same(feed.SkipDays, feed.SkipDays);
        }
        
        [Fact]
        public void TextInput_GetWithElementExtension_ReturnsExpected()
        {
            var feed = new SyndicationFeed();
            feed.ElementExtensions.Add(new SyndicationElementExtension("other", "", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension("textInput", "other", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension(
                new XElement("textInput",
                    new XElement("name", "Name"),
                    new XElement("description", "Description"),
                    new XElement("other", 10),
                    new XElement("title", "Title"),
                    new XElement("link", "http://google.com")
                ).CreateReader())
            );

            Assert.Equal("Name", feed.TextInput.Name);
            Assert.Equal("Description", feed.TextInput.Description);
            Assert.Equal("Title", feed.TextInput.Title);
            Assert.Equal(new Uri("http://google.com"), feed.TextInput.Link.Uri);
        }

        public static IEnumerable<object[]> TextInput_GetInvalid_TestData()
        {
            yield return new object[]
            {
                new SyndicationElementExtension(
                    new XElement("textInput",
                        new XElement("description", "Description"),
                        new XElement("title", "Title"),
                        new XElement("link", "http://google.com")
                    ).CreateReader()
                )
            };
            yield return new object[]
            {
                new SyndicationElementExtension(
                    new XElement("textInput",
                        new XElement("name", "Name"),
                        new XElement("title", "Title"),
                        new XElement("link", "http://google.com")
                    ).CreateReader()
                )
            };
            yield return new object[]
            {
                new SyndicationElementExtension(
                    new XElement("textInput",
                        new XElement("name", "Name"),
                        new XElement("description", "Description"),
                        new XElement("link", "http://google.com")
                    ).CreateReader()
                )
            };
            yield return new object[]
            {
                new SyndicationElementExtension(
                    new XElement("textInput",
                        new XElement("name", "Name"),
                        new XElement("description", "Description"),
                        new XElement("title", "Title")
                    ).CreateReader()
                )
            };
        }

        [Theory]
        [MemberData(nameof(TextInput_GetInvalid_TestData))]
        public void TextInput_GetInvalid_ReturnsNull(SyndicationElementExtension extension)
        {
            var feed = new SyndicationFeed();
            feed.ElementExtensions.Add(extension);
            Assert.Null(feed.TextInput);
        }
        
        public static IEnumerable<object[]> TimeToLive_GetWithElementExtension_TestData()
        {
            yield return new object[] { 10, TimeSpan.FromMinutes(10) };
            yield return new object[] { -1, null };
            yield return new object[] { "", null };
            yield return new object[] { "invalid", null };
        }

        [Theory]
        [MemberData(nameof(TimeToLive_GetWithElementExtension_TestData))]
        public void TimeToLive_GetWithElementExtension_ReturnsExpected(object value, TimeSpan? expectedValue)
        {
            var feed = new SyndicationFeed();
            feed.ElementExtensions.Add(new SyndicationElementExtension("other", "", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension("ttl", "other", 10));
            feed.ElementExtensions.Add(new SyndicationElementExtension("ttl", "", value));

            Assert.Equal(expectedValue, feed.TimeToLive);
        }

        public static IEnumerable<object[]> TimeToLive_Set_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new TimeSpan(hours: 0, minutes: 0, seconds: 0) };
            yield return new object[] { new TimeSpan(hours: 0, minutes: 1, seconds: 0) };
            yield return new object[] { new TimeSpan(hours: 0, minutes: 1000, seconds: 0) };
        }

        [Theory]
        [MemberData(nameof(TimeToLive_Set_TestData))]
        public static void TimeToLive_Set_GetReturnsExpected(TimeSpan? value)
        {
            var feed = new SyndicationFeed
            {
                TimeToLive = value
            };
            Assert.Equal(value, feed.TimeToLive);
        }

        public static IEnumerable<object[]> TimeToLive_SetInvalid_TestData()
        {
            yield return new object[] { new TimeSpan(days: 0, hours: 0, minutes: 1, seconds: 1, milliseconds: 0) };
            yield return new object[] { new TimeSpan(days: 0, hours: 0, minutes: 1, seconds: 0, milliseconds: 1) };
            yield return new object[] { new TimeSpan(hours: 0, minutes: -1, seconds: 0) };
        }

        [Theory]
        [MemberData(nameof(TimeToLive_SetInvalid_TestData))]
        public void TimeToLive_SetInvalid_ThrowsArgumentOutOfRangeException(TimeSpan value)
        {
            var feed = new SyndicationFeed();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => feed.TimeToLive = value);
        }
    }
}
