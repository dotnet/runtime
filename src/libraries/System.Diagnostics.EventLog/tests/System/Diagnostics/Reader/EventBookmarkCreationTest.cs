// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET7_0_OR_GREATER
using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventBookmarkCreationTest
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Create_InvalidXml_Throws()
        {
            const string bookmarXml = "InvalidData";
            Assert.Throws<EventLogException>(() => EventBookmark.Create(bookmarXml));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Create_ValidXml_ReturnsEventBookmark()
        {
            string bookmarkXml = $"<BookmarkList><Bookmark Channel=\"Application\" RecordId=\"2022\" IsCurrent=\"True\"/></BookmarkList>";
            var createdBookmark = EventBookmark.Create(bookmarkXml);

            Assert.Equal(bookmarkXml, createdBookmark.GetBookmarkXml());
        }
    }
}
#endif
