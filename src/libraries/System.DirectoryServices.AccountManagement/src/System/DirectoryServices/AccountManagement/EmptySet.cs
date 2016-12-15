// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    internal class EmptySet : BookmarkableResultSet
    {
        internal EmptySet()
        {
            // Nothing to do
        }

        override internal object CurrentAsPrincipal
        {
            get
            {
                Debug.Fail("EmptySet.CurrentAsPrincipal: Shouldn't be here");
                return null;
            }
        }
        override internal bool MoveNext()
        {
            // Mimic an empty set
            return false;
        }

        override internal void Reset()
        {
            // Nothing to do
        }

        override internal ResultSetBookmark BookmarkAndReset()
        {
            return new EmptySetBookmark();
        }

        override internal void RestoreBookmark(ResultSetBookmark bookmark)
        {
            // Nothing to do
        }
    }

    internal class EmptySetBookmark : ResultSetBookmark
    {
    }
}

