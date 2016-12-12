
/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    EmptySet.cs

Abstract:

    Implements the always-empty ResultSet class.
    
History:

    04-June-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    class EmptySet : BookmarkableResultSet
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

    class EmptySetBookmark : ResultSetBookmark
    {

    }
}

