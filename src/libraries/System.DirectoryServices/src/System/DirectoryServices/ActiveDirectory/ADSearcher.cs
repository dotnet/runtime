//------------------------------------------------------------------------------
// <copyright file="ADSearcher.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------


namespace System.DirectoryServices.ActiveDirectory {
	using System;
	using System.Collections.Specialized;
       using System.Security.Permissions;

	internal class ADSearcher {

		DirectorySearcher searcher = null;               
      		static TimeSpan defaultTimeSpan = new TimeSpan(0,120,0);		

		public ADSearcher(DirectoryEntry searchRoot, string filter, string[] propertiesToLoad, SearchScope scope)
		{
			this.searcher = new DirectorySearcher(searchRoot, filter, propertiesToLoad, scope);

			// set all search preferences
			// don't cache the results on the client
			searcher.CacheResults = false;
			// set the timeout to 2 minutes
			searcher.ClientTimeout = defaultTimeSpan;
                     searcher.ServerPageTimeLimit = defaultTimeSpan;
			// Page Size needs to be set so that we 
			// can get all the results even when the number of results 
			// is greater than the server set limit (1000 in Win2000 and 1500 in Win2003)
			searcher.PageSize = 512;
		}

              public ADSearcher(DirectoryEntry searchRoot, string filter, string[] propertiesToLoad, SearchScope scope, bool pagedSearch, bool cacheResults)
              {
                  this.searcher = new DirectorySearcher(searchRoot, filter, propertiesToLoad, scope);
                  // set proper time out
                  searcher.ClientTimeout = defaultTimeSpan;
                  if(pagedSearch)
                  {
                      searcher.PageSize = 512;
                      searcher.ServerPageTimeLimit = defaultTimeSpan;
                  }
                  
                  if(cacheResults)
                  {
                      searcher.CacheResults = true;
                  }
                  else
                  {
                      searcher.CacheResults = false;
                  }
              }

		public SearchResult FindOne() {
			return searcher.FindOne();
		}

		public SearchResultCollection FindAll() {
			return searcher.FindAll();
		}

		public StringCollection PropertiesToLoad {
			get {
				return searcher.PropertiesToLoad;
			}
		}

		public string Filter {
			get { 
				return searcher.Filter;
			}
			set {
				searcher.Filter = value;
			}
		}

		public void Dispose() {
			searcher.Dispose();
		}
	}
}
