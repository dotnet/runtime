// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ArgumentNullException.ThrowIfNull(bookmarkText);

            BookmarkText = bookmarkText;
        }

        internal string BookmarkText { get; }

        /// <summary>
        /// Gets the XML string that represents the bookmark.
        /// </summary>
        /// <returns>The bookmark's XML string.</returns>
        public string GetBookmarkXml() => BookmarkText;

        /// <summary>
        /// Creates a bookmark that identifies an event in a channel.
        /// </summary>
        /// <param name="bookmarkXml">An XML string that contains the bookmark.</param>
        /// <returns>The created <see cref="EventBookmark"/> instance.</returns>
        public static EventBookmark Create(string bookmarkXml)
        {
            ArgumentNullException.ThrowIfNull(bookmarkXml);

            // tries to create a bookmark handle, if the xml string is invalid an exception will be thrown
            using EventLogHandle bookmarkHandle = NativeWrapper.EvtCreateBookmark(bookmarkXml);
            return new EventBookmark(bookmarkXml);
        }
    }
}
