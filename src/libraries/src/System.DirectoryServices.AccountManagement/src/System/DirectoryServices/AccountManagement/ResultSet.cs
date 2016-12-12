// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.DirectoryServices.AccountManagement
{
    internal abstract class ResultSet : IDisposable
    {
        // Return the principal we're positioned at as a Principal object.
        // Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
        abstract internal object CurrentAsPrincipal { get; }

        // Advance the enumerator to the next principal in the result set, pulling in additional pages
        // of results (or ranges of attribute values) as needed.
        // Returns true if successful, false if no more results to return.
        abstract internal bool MoveNext();

        // Resets the enumerator to before the first result in the set.  This potentially can be an expensive
        // operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
        // As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
        // a no-op.
        abstract internal void Reset();

        // IDisposable implementation
        public virtual void Dispose()
        {
            // Nothing to do in base class
        }
    }

    internal abstract class BookmarkableResultSet : ResultSet
    {
        abstract internal ResultSetBookmark BookmarkAndReset();

        abstract internal void RestoreBookmark(ResultSetBookmark bookmark);
    }

    internal abstract class ResultSetBookmark
    {
    }
}
