// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.Eventing.Reader
{
    //
    // NOTE: This class must be generic enough to be used across
    // eventing base implementations.  Cannot add anything
    // that ties it to one particular implementation.
    //

    /// <summary>
    /// Represents an opaque Event Bookmark obtained from an EventRecord.
    /// The bookmark denotes a unique identifier for the event instance as
    /// well as marks the location in the result set of the EventReader
    /// that the event instance was obtained from.
    /// </summary>
    public class EventBookmark
    {
        internal EventBookmark(string bookmarkText)
        {
            BookmarkText = bookmarkText ?? throw new ArgumentNullException(nameof(bookmarkText));
        }

        internal string BookmarkText { get; }
    }
}

