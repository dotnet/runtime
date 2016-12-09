/*++

Copyright (c) 2007  Microsoft Corporation

Module Name:

    ExtensionCache

Abstract:

    Class which wraps the attribute cache used by extension classes for custom attributes and search filters

History:

    16-Apr-2007    TQuerec     Created

--*/

namespace System.DirectoryServices.AccountManagement
{

	using System;
	using System.Collections.Generic;
	using System.Collections;
	using System.DirectoryServices;

	class ExtensionCacheValue
	{

	    internal ExtensionCacheValue( object[] value ) 
	    { 
	        this.value = value;
	        this.filterOnly = false;
	    }

	    internal ExtensionCacheValue(object[] value, Type type, MatchType matchType)
	    {
	        this.value = value;
	        this.type = type;
	        this.matchType = matchType;
	        this.filterOnly = true;
	    }

	    internal object[] Value
	    {
	        get { return value; }
	    }
	    internal bool Filter
	    {
	        get { return filterOnly;  }
	    }
	    internal Type Type
	    {
	        get { return this.type;  }
	    }
	    internal MatchType MatchType
	    {
	        get { return this.matchType;  }
	    }
		
	    object[] value;
	    bool filterOnly;
	    Type type;
	    MatchType matchType;
	}

	class ExtensionCache
	{
	    Dictionary<string, ExtensionCacheValue> cache = new Dictionary<string, ExtensionCacheValue>();

	    internal ExtensionCache() {  }

	    internal bool TryGetValue(string attr, out ExtensionCacheValue o)
	    {
	        return (cache.TryGetValue(attr, out o));
	    }

	    internal Dictionary<string, ExtensionCacheValue> properties
	    {
	        get
	        {
	            return cache;
	        }
	    }

	}
}
