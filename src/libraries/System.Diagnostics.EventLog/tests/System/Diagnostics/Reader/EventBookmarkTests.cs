// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Tests;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventBookmarkTests
    {
        [Fact]
        public void Ctor_NullBookmarkText_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EventBookmark(null));
        }

        [Fact]
        public void Create_ValidXml_ReturnsEventBookmark()
        {
            const string bookmarkXml = "<BookmarkList><Bookmark Channel=\"Application\" RecordId=\"2022\" IsCurrent=\"True\"/></BookmarkList>";
            var createdBookmark = new EventBookmark(bookmarkXml);

            Assert.Equal(bookmarkXml, createdBookmark.BookmarkXml);
        }
    }
}
