// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Specialized;
using System.DirectoryServices.Interop;
using System.ComponentModel;

using INTPTR_INTPTRCAST = System.IntPtr;
using System.Diagnostics.CodeAnalysis;

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Performs queries against the Active Directory hierarchy.
    /// </devdoc>
    public class DirectorySearcher : Component
    {
        private DirectoryEntry? _searchRoot;
        private string? _filter = defaultFilter;
        private StringCollection? _propertiesToLoad;
        private bool _disposed;

        private static readonly TimeSpan s_minusOneSecond = new TimeSpan(0, 0, -1);

        // search preference variables
        private SearchScope _scope = System.DirectoryServices.SearchScope.Subtree;
        private bool _scopeSpecified;
        private int _sizeLimit;
        private TimeSpan _serverTimeLimit = s_minusOneSecond;
        private TimeSpan _clientTimeout = s_minusOneSecond;
        private int _pageSize;
        private TimeSpan _serverPageTimeLimit = s_minusOneSecond;
        private ReferralChasingOption _referralChasing = ReferralChasingOption.External;
        private SortOption _sort = new SortOption();
        private bool _cacheResults = true;
        private bool _cacheResultsSpecified;
        private bool _rootEntryAllocated;             // true: if a temporary entry inside Searcher has been created
        private string? _assertDefaultNamingContext;
        private string _attributeScopeQuery = "";
        private bool _attributeScopeQuerySpecified;
        private DereferenceAlias _derefAlias = DereferenceAlias.Never;
        private SecurityMasks _securityMask = SecurityMasks.None;
        private ExtendedDN _extendedDN = ExtendedDN.None;
        private DirectorySynchronization? _sync;
        internal bool directorySynchronizationSpecified;
        private DirectoryVirtualListView? _vlv;
        internal bool directoryVirtualListViewSpecified;
        internal SearchResultCollection? searchResult;

        private const string defaultFilter = "(objectClass=*)";

        /// <devdoc>
        /// Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>,
        /// <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default values.
        /// </devdoc>
        public DirectorySearcher() : this(null, defaultFilter, null, System.DirectoryServices.SearchScope.Subtree)
        {
            _scopeSpecified = false;
        }

        /// <devdoc>
        /// Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with
        /// <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default
        ///  values, and <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/> set to the given value.
        /// </devdoc>
        public DirectorySearcher(DirectoryEntry? searchRoot) : this(searchRoot, defaultFilter, null, System.DirectoryServices.SearchScope.Subtree)
        {
            _scopeSpecified = false;
        }

        /// <devdoc>
        /// Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with
        /// <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/> and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default
        /// values, and <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/> and <see cref='System.DirectoryServices.DirectorySearcher.Filter'/> set to the respective given values.
        /// </devdoc>
        public DirectorySearcher(DirectoryEntry? searchRoot, string? filter) : this(searchRoot, filter, null, System.DirectoryServices.SearchScope.Subtree)
        {
            _scopeSpecified = false;
        }

        /// <devdoc>
        /// Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with
        /// <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to its default
        /// value, and <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>, <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, and <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/> set to the respective given values.
        /// </devdoc>
        public DirectorySearcher(DirectoryEntry? searchRoot, string? filter, string[]? propertiesToLoad) : this(searchRoot, filter, propertiesToLoad, System.DirectoryServices.SearchScope.Subtree)
        {
            _scopeSpecified = false;
        }

        /// <devdoc>
        /// Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>,
        /// <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default
        ///    values, and <see cref='System.DirectoryServices.DirectorySearcher.Filter'/> set to the given value.
        /// </devdoc>
        public DirectorySearcher(string? filter) : this(null, filter, null, System.DirectoryServices.SearchScope.Subtree)
        {
            _scopeSpecified = false;
        }

        /// <devdoc>
        /// Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>
        /// and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default
        /// values, and <see cref='System.DirectoryServices.DirectorySearcher.Filter'/> and <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/> set to the respective given values.
        /// </devdoc>
        public DirectorySearcher(string? filter, string[]? propertiesToLoad) : this(null, filter, propertiesToLoad, System.DirectoryServices.SearchScope.Subtree)
        {
            _scopeSpecified = false;
        }

        /// <devdoc>
        /// Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/> set to its default
        /// value, and <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to the respective given values.
        /// </devdoc>
        public DirectorySearcher(string? filter, string[]? propertiesToLoad, SearchScope scope) : this(null, filter, propertiesToLoad, scope)
        {
        }

        /// <devdoc>
        /// Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with the <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>, <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> properties set to the given
        /// values.
        /// </devdoc>
        public DirectorySearcher(DirectoryEntry? searchRoot, string? filter, string[]? propertiesToLoad, SearchScope scope)
        {
            _searchRoot = searchRoot;
            _filter = filter;
            if (propertiesToLoad != null)
                PropertiesToLoad.AddRange(propertiesToLoad);
            this.SearchScope = scope;
        }

        protected override void Dispose(bool disposing)
        {
            // safe to call while finalizing or disposing
            //
            if (!_disposed && disposing)
            {
                if (_rootEntryAllocated)
                    _searchRoot!.Dispose();
                _rootEntryAllocated = false;
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <devdoc>
        /// Gets or sets a value indicating whether the result should be cached on the
        /// client machine.
        /// </devdoc>
        [DefaultValue(true)]
        public bool CacheResults
        {
            get => _cacheResults;
            set
            {
                // user explicitly set CacheResults to true and also want VLV
                if (directoryVirtualListViewSpecified == true && value == true)
                    throw new ArgumentException(SR.DSBadCacheResultsVLV);

                _cacheResults = value;

                _cacheResultsSpecified = true;
            }
        }

        /// <devdoc>
        ///  Gets or sets the maximum amount of time that the client waits for
        ///  the server to return results. If the server does not respond within this time,
        ///  the search is aborted, and no results are returned.
        /// </devdoc>
        public TimeSpan ClientTimeout
        {
            get => _clientTimeout;
            set
            {
                // prevent integer overflow
                if (value.TotalSeconds > int.MaxValue)
                {
                    throw new ArgumentException(SR.TimespanExceedMax, nameof(value));
                }

                _clientTimeout = value;
            }
        }

        /// <devdoc>
        /// Gets or sets a value indicating whether the search should retrieve only the names of requested
        /// properties or the names and values of requested properties.
        /// </devdoc>
        [DefaultValue(false)]
        public bool PropertyNamesOnly { get; set; }

        /// <devdoc>
        /// Gets or sets the Lightweight Directory Access Protocol (LDAP) filter string format.
        /// </devdoc>
        [DefaultValue(defaultFilter)]
        public string? Filter
        {
            get => _filter;
            set
            {
                if (value == null || value.Length == 0)
                    value = defaultFilter;
                _filter = value;
            }
        }

        /// <devdoc>
        /// Gets or sets the page size in a paged search.
        /// </devdoc>
        [DefaultValue(0)]
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.DSBadPageSize);

                // specify non-zero pagesize explicitly and also want dirsync
                if (directorySynchronizationSpecified == true && value != 0)
                    throw new ArgumentException(SR.DSBadPageSizeDirsync);

                _pageSize = value;
            }
        }

        /// <devdoc>
        /// Gets the set of properties retrieved during the search. By default, the <see cref='System.DirectoryServices.DirectoryEntry.Path'/>
        /// and <see cref='System.DirectoryServices.DirectoryEntry.Name'/> properties are retrieved.
        /// </devdoc>
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public StringCollection PropertiesToLoad
        {
            get
            {
                if (_propertiesToLoad == null)
                {
                    _propertiesToLoad = new StringCollection();
                }
                return _propertiesToLoad;
            }
        }

        /// <devdoc>
        /// Gets or sets how referrals are chased.
        /// </devdoc>
        [DefaultValue(ReferralChasingOption.External)]
        public ReferralChasingOption ReferralChasing
        {
            get => _referralChasing;
            set
            {
                if (value != ReferralChasingOption.None &&
                    value != ReferralChasingOption.Subordinate &&
                    value != ReferralChasingOption.External &&
                    value != ReferralChasingOption.All)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ReferralChasingOption));

                _referralChasing = value;
            }
        }

        /// <devdoc>
        /// Gets or sets the scope of the search that should be observed by the server.
        /// </devdoc>
        [DefaultValue(SearchScope.Subtree)]
        public SearchScope SearchScope
        {
            get => _scope;
            set
            {
                if (value < SearchScope.Base || value > SearchScope.Subtree)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(SearchScope));

                // user explicitly set SearchScope to something other than Base and also want to do ASQ, it is not supported
                if (_attributeScopeQuerySpecified == true && value != SearchScope.Base)
                {
                    throw new ArgumentException(SR.DSBadASQSearchScope);
                }

                _scope = value;

                _scopeSpecified = true;
            }
        }

        /// <devdoc>
        /// Gets or sets the time limit that the server should observe to search a page of results (as
        /// opposed to the time limit for the entire search).
        /// </devdoc>
        public TimeSpan ServerPageTimeLimit
        {
            get => _serverPageTimeLimit;
            set
            {
                // prevent integer overflow
                if (value.TotalSeconds > int.MaxValue)
                {
                    throw new ArgumentException(SR.TimespanExceedMax, nameof(value));
                }

                _serverPageTimeLimit = value;
            }
        }

        /// <devdoc>
        /// Gets or sets the maximum amount of time the server spends searching. If the
        /// time limit is reached, only entries found up to that point will be returned.
        /// </devdoc>
        public TimeSpan ServerTimeLimit
        {
            get => _serverTimeLimit;
            set
            {
                // prevent integer overflow
                if (value.TotalSeconds > int.MaxValue)
                {
                    throw new ArgumentException(SR.TimespanExceedMax, nameof(value));
                }

                _serverTimeLimit = value;
            }
        }

        /// <devdoc>
        ///  Gets or sets the maximum number of objects that the
        ///  server should return in a search.
        /// </devdoc>
        [DefaultValue(0)]
        public int SizeLimit
        {
            get => _sizeLimit;
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.DSBadSizeLimit);
                _sizeLimit = value;
            }
        }

        /// <devdoc>
        /// Gets or sets the node in the Active Directory hierarchy
        /// at which the search will start.
        /// </devdoc>
        [DefaultValue(null)]
        public DirectoryEntry? SearchRoot
        {
            get
            {
                if (_searchRoot == null && !DesignMode)
                {
                    // get the default naming context. This should be the default root for the search.
                    DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE", true, null, null, AuthenticationTypes.Secure);

                    //SECREVIEW: Searching the root of the DS will demand browse permissions
                    //                     on "*" or "LDAP://RootDSE".
                    string defaultNamingContext = (string)rootDSE.Properties["defaultNamingContext"][0]!;
                    rootDSE.Dispose();

                    _searchRoot = new DirectoryEntry("LDAP://" + defaultNamingContext, true, null, null, AuthenticationTypes.Secure);
                    _rootEntryAllocated = true;
                    _assertDefaultNamingContext = "LDAP://" + defaultNamingContext;
                }
                return _searchRoot;
            }
            set
            {
                if (_rootEntryAllocated)
                    _searchRoot!.Dispose();
                _rootEntryAllocated = false;

                _assertDefaultNamingContext = null;
                _searchRoot = value;
            }
        }

        /// <devdoc>
        /// Gets the property on which the results should be sorted.
        /// </devdoc>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public SortOption Sort
        {
            get => _sort;
            set => _sort = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <devdoc>
        /// Gets or sets a value indicating whether searches should be carried out in an asynchronous
        /// way.
        /// </devdoc>
        [DefaultValue(false)]
        public bool Asynchronous { get; set; }

        /// <devdoc>
        /// Gets or sets a value indicating whether the search should also return deleted objects that match the search
        /// filter.
        /// </devdoc>
        [DefaultValue(false)]
        public bool Tombstone { get; set; }

        /// <devdoc>
        /// Gets or sets an attribute name to indicate that an attribute-scoped query search should be
        /// performed.
        /// </devdoc>
        [DefaultValue("")]
        [AllowNull]
        public string AttributeScopeQuery
        {
            get => _attributeScopeQuery;
            set
            {
                if (value == null)
                    value = "";

                // user explicitly set AttributeScopeQuery and value is not null or empty string
                if (value.Length != 0)
                {
                    if (_scopeSpecified == true && SearchScope != SearchScope.Base)
                    {
                        throw new ArgumentException(SR.DSBadASQSearchScope);
                    }

                    // if user did not explicitly set search scope
                    _scope = SearchScope.Base;

                    _attributeScopeQuerySpecified = true;
                }
                else
                // user explicitly sets the value to default one and doesn't want to do asq
                {
                    _attributeScopeQuerySpecified = false;
                }

                _attributeScopeQuery = value;
            }
        }

        /// <devdoc>
        /// Gets or sets a value to indicate how the aliases of found objects are to be
        /// resolved.
        /// </devdoc>
        [DefaultValue(DereferenceAlias.Never)]
        public DereferenceAlias DerefAlias
        {
            get => _derefAlias;
            set
            {
                if (value < DereferenceAlias.Never || value > DereferenceAlias.Always)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(DereferenceAlias));

                _derefAlias = value;
            }
        }

        /// <devdoc>
        /// Gets or sets a value to indicate the search should return security access information for the specified
        /// attributes.
        /// </devdoc>
        [DefaultValue(SecurityMasks.None)]
        public SecurityMasks SecurityMasks
        {
            get => _securityMask;
            set
            {
                // make sure the behavior is consistent with native ADSI
                if (value > (SecurityMasks.None | SecurityMasks.Owner | SecurityMasks.Group | SecurityMasks.Dacl | SecurityMasks.Sacl))
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(SecurityMasks));

                _securityMask = value;
            }
        }

        /// <devdoc>
        /// Gets or sets a value to return extended DNs according to the requested
        /// format.
        /// </devdoc>
        [DefaultValue(ExtendedDN.None)]
        public ExtendedDN ExtendedDN
        {
            get => _extendedDN;
            set
            {
                if (value < ExtendedDN.None || value > ExtendedDN.Standard)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ExtendedDN));

                _extendedDN = value;
            }
        }

        /// <devdoc>
        /// Gets or sets a value to indicate a directory synchronization search, which returns all changes since a specified
        /// state.
        /// </devdoc>
        [DefaultValue(null)]
        public DirectorySynchronization? DirectorySynchronization
        {
            get
            {
                // if user specifies dirsync search preference and search is executed
                if (directorySynchronizationSpecified && searchResult != null)
                {
                    _sync!.ResetDirectorySynchronizationCookie(searchResult.DirsyncCookie);
                }
                return _sync;
            }

            set
            {
                // specify non-zero pagesize explicitly and also want dirsync
                if (value != null)
                {
                    if (PageSize != 0)
                        throw new ArgumentException(SR.DSBadPageSizeDirsync);

                    directorySynchronizationSpecified = true;
                }
                else
                // user explicitly sets the value to default one and doesn't want to do dirsync
                {
                    directorySynchronizationSpecified = false;
                }

                _sync = value;
            }
        }

        /// <devdoc>
        /// Gets or sets a value to indicate the search should use the LDAP virtual list view (VLV)
        /// control.
        /// </devdoc>
        [DefaultValue(null)]
        public DirectoryVirtualListView? VirtualListView
        {
            get
            {
                // if user specifies dirsync search preference and search is executed
                if (directoryVirtualListViewSpecified && searchResult != null)
                {
                    DirectoryVirtualListView tempval = searchResult.VLVResponse;
                    _vlv!.Offset = tempval.Offset;
                    _vlv.ApproximateTotal = tempval.ApproximateTotal;
                    _vlv.DirectoryVirtualListViewContext = tempval.DirectoryVirtualListViewContext;
                    if (_vlv.ApproximateTotal != 0)
                        _vlv.TargetPercentage = (int)((double)_vlv.Offset / _vlv.ApproximateTotal * 100);
                    else
                        _vlv.TargetPercentage = 0;
                }
                return _vlv;
            }
            set
            {
                // if user explicitly set CacheResults to true and also want to set VLV
                if (value != null)
                {
                    if (_cacheResultsSpecified == true && CacheResults == true)
                        throw new ArgumentException(SR.DSBadCacheResultsVLV);

                    directoryVirtualListViewSpecified = true;
                    // if user does not explicit specify cache results to true and also do vlv, then cache results is default to false
                    _cacheResults = false;
                }
                else
                // user explicitly sets the value to default one and doesn't want to do vlv
                {
                    directoryVirtualListViewSpecified = false;
                }

                _vlv = value;
            }
        }

        /// <devdoc>
        /// Executes the search and returns only the first entry that is found.
        /// </devdoc>
        public SearchResult? FindOne()
        {
            DirectorySynchronization? tempsync = null;
            DirectoryVirtualListView? tempvlv = null;
            SearchResult? resultEntry = null;

            SearchResultCollection results = FindAll(false);

            try
            {
                foreach (SearchResult entry in results)
                {
                    // need to get the dirsync cookie
                    if (directorySynchronizationSpecified)
                        tempsync = DirectorySynchronization;

                    // need to get the vlv response
                    if (directoryVirtualListViewSpecified)
                        tempvlv = VirtualListView;

                    resultEntry = entry;
                    break;
                }
            }
            finally
            {
                searchResult = null;

                // still need to properly release the resource
                results.Dispose();
            }

            return resultEntry;
        }

        /// <devdoc>
        /// Executes the search and returns a collection of the entries that are found.
        /// </devdoc>
        public SearchResultCollection FindAll() => FindAll(true);

        private SearchResultCollection FindAll(bool findMoreThanOne)
        {
            DirectoryEntry clonedRoot = SearchRoot!.CloneBrowsable();

            UnsafeNativeMethods.IAds adsObject = clonedRoot.AdsObject;
            if (!(adsObject is UnsafeNativeMethods.IDirectorySearch))
                throw new NotSupportedException(SR.Format(SR.DSSearchUnsupported, SearchRoot.Path));

            // this is a little bit hacky, but we need to perform a bind here, so we make sure the LDAP connection that we hold has more than
            // one reference count, one by SearchResultCollection object, one by DirectorySearcher object. In this way, when user calls
            // Dispose on SearchResultCollection, the connection is still there instead of reference count dropping to zero and being closed.
            // It is especially important for virtuallistview case, in order to reuse the vlv response, the search must be performed on the same ldap connection

            // only do it when vlv is used
            if (directoryVirtualListViewSpecified)
            {
                SearchRoot.Bind(true);
            }

            UnsafeNativeMethods.IDirectorySearch adsSearch = (UnsafeNativeMethods.IDirectorySearch)adsObject;
            SetSearchPreferences(adsSearch, findMoreThanOne);

            string[]? properties = null;
            if (PropertiesToLoad.Count > 0)
            {
                if (!PropertiesToLoad.Contains("ADsPath"))
                {
                    // if we don't get this property, we won't be able to return a list of DirectoryEntry objects!
                    PropertiesToLoad.Add("ADsPath");
                }
                properties = new string[PropertiesToLoad.Count];
                PropertiesToLoad.CopyTo(properties, 0);
            }

            IntPtr resultsHandle;
            if (properties != null)
            {
                adsSearch.ExecuteSearch(Filter, properties, properties.Length, out resultsHandle);
            }
            else
            {
                adsSearch.ExecuteSearch(Filter, null, -1, out resultsHandle);
                properties = Array.Empty<string>();
            }

            return new SearchResultCollection(clonedRoot, resultsHandle, properties, this);
        }

        private unsafe void SetSearchPreferences(UnsafeNativeMethods.IDirectorySearch adsSearch, bool findMoreThanOne)
        {
            ArrayList prefList = new ArrayList();
            AdsSearchPreferenceInfo info;

            // search scope
            info = default;
            info.dwSearchPref = (int)AdsSearchPreferences.SEARCH_SCOPE;
            info.vValue = new AdsValueHelper((int)SearchScope).GetStruct();
            prefList.Add(info);

            // size limit
            if (_sizeLimit != 0 || !findMoreThanOne)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.SIZE_LIMIT;
                info.vValue = new AdsValueHelper(findMoreThanOne ? SizeLimit : 1).GetStruct();
                prefList.Add(info);
            }

            // time limit
            if (ServerTimeLimit >= new TimeSpan(0))
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.TIME_LIMIT;
                info.vValue = new AdsValueHelper((int)ServerTimeLimit.TotalSeconds).GetStruct();
                prefList.Add(info);
            }

            // propertyNamesOnly
            info = default;
            info.dwSearchPref = (int)AdsSearchPreferences.ATTRIBTYPES_ONLY;
            info.vValue = new AdsValueHelper(PropertyNamesOnly).GetStruct();
            prefList.Add(info);

            // Timeout
            if (ClientTimeout >= new TimeSpan(0))
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.TIMEOUT;
                info.vValue = new AdsValueHelper((int)ClientTimeout.TotalSeconds).GetStruct();
                prefList.Add(info);
            }

            // page size
            if (PageSize != 0)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.PAGESIZE;
                info.vValue = new AdsValueHelper(PageSize).GetStruct();
                prefList.Add(info);
            }

            // page time limit
            if (ServerPageTimeLimit >= new TimeSpan(0))
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.PAGED_TIME_LIMIT;
                info.vValue = new AdsValueHelper((int)ServerPageTimeLimit.TotalSeconds).GetStruct();
                prefList.Add(info);
            }

            // chase referrals
            info = default;
            info.dwSearchPref = (int)AdsSearchPreferences.CHASE_REFERRALS;
            info.vValue = new AdsValueHelper((int)ReferralChasing).GetStruct();
            prefList.Add(info);

            // asynchronous
            if (Asynchronous == true)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.ASYNCHRONOUS;
                info.vValue = new AdsValueHelper(Asynchronous).GetStruct();
                prefList.Add(info);
            }

            // tombstone
            if (Tombstone == true)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.TOMBSTONE;
                info.vValue = new AdsValueHelper(Tombstone).GetStruct();
                prefList.Add(info);
            }

            // attributescopequery
            if (_attributeScopeQuerySpecified)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.ATTRIBUTE_QUERY;
                info.vValue = new AdsValueHelper(AttributeScopeQuery, AdsType.ADSTYPE_CASE_IGNORE_STRING).GetStruct();
                prefList.Add(info);
            }

            // derefalias
            if (DerefAlias != DereferenceAlias.Never)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.DEREF_ALIASES;
                info.vValue = new AdsValueHelper((int)DerefAlias).GetStruct();
                prefList.Add(info);
            }

            // securitymask
            if (SecurityMasks != SecurityMasks.None)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.SECURITY_MASK;
                info.vValue = new AdsValueHelper((int)SecurityMasks).GetStruct();
                prefList.Add(info);
            }

            // extendeddn
            if (ExtendedDN != ExtendedDN.None)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.EXTENDED_DN;
                info.vValue = new AdsValueHelper((int)ExtendedDN).GetStruct();
                prefList.Add(info);
            }

            // dirsync
            if (directorySynchronizationSpecified)
            {
                info = default;
                info.dwSearchPref = (int)AdsSearchPreferences.DIRSYNC;
                info.vValue = new AdsValueHelper(DirectorySynchronization!.GetDirectorySynchronizationCookie(), AdsType.ADSTYPE_PROV_SPECIFIC).GetStruct();
                prefList.Add(info);

                if (DirectorySynchronization.Option != DirectorySynchronizationOptions.None)
                {
                    info = default;
                    info.dwSearchPref = (int)AdsSearchPreferences.DIRSYNC_FLAG;
                    info.vValue = new AdsValueHelper((int)DirectorySynchronization.Option).GetStruct();
                    prefList.Add(info);
                }
            }

            IntPtr ptrToFree = (IntPtr)0;
            IntPtr ptrVLVToFree = (IntPtr)0;
            IntPtr ptrVLVContexToFree = (IntPtr)0;

            try
            {
                // sort
                if (Sort.PropertyName != null && Sort.PropertyName.Length > 0)
                {
                    info = default;
                    info.dwSearchPref = (int)AdsSearchPreferences.SORT_ON;
                    AdsSortKey sortKey = default;
                    sortKey.pszAttrType = Marshal.StringToCoTaskMemUni(Sort.PropertyName);
                    ptrToFree = sortKey.pszAttrType; // so we can free it later.
                    sortKey.pszReserved = (IntPtr)0;
                    sortKey.fReverseOrder = (Sort.Direction == SortDirection.Descending) ? -1 : 0;
                    byte[] sortKeyBytes = new byte[Marshal.SizeOf(sortKey)];
                    Marshal.Copy((INTPTR_INTPTRCAST)(&sortKey), sortKeyBytes, 0, sortKeyBytes.Length);
                    info.vValue = new AdsValueHelper(sortKeyBytes, AdsType.ADSTYPE_PROV_SPECIFIC).GetStruct();
                    prefList.Add(info);
                }

                // vlv
                if (directoryVirtualListViewSpecified)
                {
                    info = default;
                    info.dwSearchPref = (int)AdsSearchPreferences.VLV;
                    AdsVLV vlvValue = new AdsVLV();
                    vlvValue.beforeCount = _vlv!.BeforeCount;
                    vlvValue.afterCount = _vlv.AfterCount;
                    vlvValue.offset = _vlv.Offset;
                    //we need to treat the empty string as null here
                    if (_vlv.Target.Length != 0)
                        vlvValue.target = Marshal.StringToCoTaskMemUni(_vlv.Target);
                    else
                        vlvValue.target = IntPtr.Zero;
                    ptrVLVToFree = vlvValue.target;
                    if (_vlv.DirectoryVirtualListViewContext == null)
                    {
                        vlvValue.contextIDlength = 0;
                        vlvValue.contextID = (IntPtr)0;
                    }
                    else
                    {
                        vlvValue.contextIDlength = _vlv.DirectoryVirtualListViewContext._context.Length;
                        vlvValue.contextID = Marshal.AllocCoTaskMem(vlvValue.contextIDlength);
                        ptrVLVContexToFree = vlvValue.contextID;
                        Marshal.Copy(_vlv.DirectoryVirtualListViewContext._context, 0, vlvValue.contextID, vlvValue.contextIDlength);
                    }
                    IntPtr vlvPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(AdsVLV)));
                    byte[] vlvBytes = new byte[Marshal.SizeOf(vlvValue)];
                    try
                    {
                        Marshal.StructureToPtr(vlvValue, vlvPtr, false);
                        Marshal.Copy(vlvPtr, vlvBytes, 0, vlvBytes.Length);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(vlvPtr);
                    }
                    info.vValue = new AdsValueHelper(vlvBytes, AdsType.ADSTYPE_PROV_SPECIFIC).GetStruct();
                    prefList.Add(info);
                }

                // cacheResults
                if (_cacheResultsSpecified)
                {
                    info = default;
                    info.dwSearchPref = (int)AdsSearchPreferences.CACHE_RESULTS;
                    info.vValue = new AdsValueHelper(CacheResults).GetStruct();
                    prefList.Add(info);
                }

                //
                // now make the call
                //
                AdsSearchPreferenceInfo[] prefs = new AdsSearchPreferenceInfo[prefList.Count];
                for (int i = 0; i < prefList.Count; i++)
                {
                    prefs[i] = (AdsSearchPreferenceInfo)prefList[i]!;
                }

                DoSetSearchPrefs(adsSearch, prefs);
            }
            finally
            {
                if (ptrToFree != (IntPtr)0)
                    Marshal.FreeCoTaskMem(ptrToFree);

                if (ptrVLVToFree != (IntPtr)0)
                    Marshal.FreeCoTaskMem(ptrVLVToFree);

                if (ptrVLVContexToFree != (IntPtr)0)
                    Marshal.FreeCoTaskMem(ptrVLVContexToFree);
            }
        }

        private static void DoSetSearchPrefs(UnsafeNativeMethods.IDirectorySearch adsSearch, AdsSearchPreferenceInfo[] prefs)
        {
            int structSize = Marshal.SizeOf(typeof(AdsSearchPreferenceInfo));
            IntPtr ptr = Marshal.AllocHGlobal((IntPtr)(structSize * prefs.Length));
            try
            {
                IntPtr tempPtr = ptr;
                for (int i = 0; i < prefs.Length; i++)
                {
                    Marshal.StructureToPtr(prefs[i], tempPtr, false);
                    tempPtr = IntPtr.Add(tempPtr, structSize);
                }

                adsSearch.SetSearchPreference(ptr, prefs.Length);

                // Check for the result status for all preferences
                tempPtr = ptr;
                for (int i = 0; i < prefs.Length; i++)
                {
                    int status = Marshal.ReadInt32(tempPtr, 32);
                    if (status != 0)
                    {
                        string property = prefs[i].dwSearchPref switch
                        {
                            (int)AdsSearchPreferences.SEARCH_SCOPE => "SearchScope",
                            (int)AdsSearchPreferences.SIZE_LIMIT => "SizeLimit",
                            (int)AdsSearchPreferences.TIME_LIMIT => "ServerTimeLimit",
                            (int)AdsSearchPreferences.ATTRIBTYPES_ONLY => "PropertyNamesOnly",
                            (int)AdsSearchPreferences.TIMEOUT => "ClientTimeout",
                            (int)AdsSearchPreferences.PAGESIZE => "PageSize",
                            (int)AdsSearchPreferences.PAGED_TIME_LIMIT => "ServerPageTimeLimit",
                            (int)AdsSearchPreferences.CHASE_REFERRALS => "ReferralChasing",
                            (int)AdsSearchPreferences.SORT_ON => "Sort",
                            (int)AdsSearchPreferences.CACHE_RESULTS => "CacheResults",
                            (int)AdsSearchPreferences.ASYNCHRONOUS => "Asynchronous",
                            (int)AdsSearchPreferences.TOMBSTONE => "Tombstone",
                            (int)AdsSearchPreferences.ATTRIBUTE_QUERY => "AttributeScopeQuery",
                            (int)AdsSearchPreferences.DEREF_ALIASES => "DerefAlias",
                            (int)AdsSearchPreferences.SECURITY_MASK => "SecurityMasks",
                            (int)AdsSearchPreferences.EXTENDED_DN => "ExtendedDn",
                            (int)AdsSearchPreferences.DIRSYNC => "DirectorySynchronization",
                            (int)AdsSearchPreferences.DIRSYNC_FLAG => "DirectorySynchronizationFlag",
                            (int)AdsSearchPreferences.VLV => "VirtualListView",
                            _ => "",
                        };
                        throw new InvalidOperationException(SR.Format(SR.DSSearchPreferencesNotAccepted, property));
                    }

                    tempPtr = IntPtr.Add(tempPtr, structSize);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
