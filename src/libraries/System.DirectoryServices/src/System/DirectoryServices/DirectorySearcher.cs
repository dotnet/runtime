//------------------------------------------------------------------------------
// <copyright file="DirectorySearcher.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using INTPTR_INTPTRCAST = System.IntPtr;
using INTPTR_INTCAST    = System.Int32;
      
/*
 */
namespace System.DirectoryServices {

    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.DirectoryServices.Interop;
    using System.DirectoryServices.Design;
    using System.ComponentModel;
    using System.Security.Permissions;
    using System.Globalization;
    using Microsoft.Win32;

    /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher"]/*' />
    /// <devdoc>
    ///    <para> Performs queries against the Active Directory hierarchy.</para>
    /// </devdoc>
    [
    DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true),
    DSDescriptionAttribute(Res.DirectorySearcherDesc)
    ]
    public class DirectorySearcher : Component {

        private DirectoryEntry searchRoot;
        private string filter = defaultFilter;
        private StringCollection propertiesToLoad;
        private bool disposed = false;

        private static readonly TimeSpan minusOneSecond = new TimeSpan(0, 0, -1);

        // search preference variables
        private SearchScope scope = System.DirectoryServices.SearchScope.Subtree;
        private bool scopeSpecified = false;
        private int sizeLimit = 0;
        private TimeSpan serverTimeLimit = minusOneSecond;
        private bool propertyNamesOnly = false;
        private TimeSpan clientTimeout = minusOneSecond;
        private int pageSize = 0;
        private TimeSpan serverPageTimeLimit = minusOneSecond;
        private ReferralChasingOption referralChasing = ReferralChasingOption.External;
        private SortOption sort = new SortOption();
        private bool cacheResults = true;
        private bool cacheResultsSpecified = false;
        private bool rootEntryAllocated = false;             // true: if a temporary entry inside Searcher has been created
        private string assertDefaultNamingContext = null;
        private bool asynchronous = false;
        private bool tombstone = false;
        private string attributeScopeQuery = "";
        private bool attributeScopeQuerySpecified = false;
        private DereferenceAlias derefAlias = DereferenceAlias.Never;
        private SecurityMasks securityMask = SecurityMasks.None;
        private ExtendedDN extendedDN = ExtendedDN.None;        
        private DirectorySynchronization sync = null;
        internal bool directorySynchronizationSpecified = false;
        private DirectoryVirtualListView vlv = null;
        internal bool directoryVirtualListViewSpecified = false;
        internal SearchResultCollection searchResult = null;


        private const string defaultFilter = "(objectClass=*)";

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySearcher"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>, 
        /// <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default values.</para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectorySearcher() : this(null, defaultFilter, null, System.DirectoryServices.SearchScope.Subtree) {
            scopeSpecified = false;
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySearcher1"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with 
        /// <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default 
        ///    values, and <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/> set to the given value.</para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectorySearcher(DirectoryEntry searchRoot) : this(searchRoot, defaultFilter, null, System.DirectoryServices.SearchScope.Subtree) {
            scopeSpecified = false;
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySearcher2"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with 
        /// <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/> and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default 
        ///    values, and <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/> and <see cref='System.DirectoryServices.DirectorySearcher.Filter'/> set to the respective given values.</para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectorySearcher(DirectoryEntry searchRoot, string filter) : this(searchRoot, filter, null, System.DirectoryServices.SearchScope.Subtree) {
            scopeSpecified = false;

        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySearcher3"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with 
        /// <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to its default 
        ///    value, and <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>, <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, and <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/> set to the respective given values.</para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectorySearcher(DirectoryEntry searchRoot, string filter, string[] propertiesToLoad) : this(searchRoot, filter, propertiesToLoad, System.DirectoryServices.SearchScope.Subtree) {
            scopeSpecified = false;
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySearcher4"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>, 
        /// <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default 
        ///    values, and <see cref='System.DirectoryServices.DirectorySearcher.Filter'/> set to the given value.</para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectorySearcher(string filter) : this(null, filter, null, System.DirectoryServices.SearchScope.Subtree) {
             scopeSpecified = false;
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySearcher5"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/> 
        /// and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to their default
        /// values, and <see cref='System.DirectoryServices.DirectorySearcher.Filter'/> and <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/> set to the respective given values.</para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectorySearcher(string filter, string[] propertiesToLoad) : this(null, filter, propertiesToLoad, System.DirectoryServices.SearchScope.Subtree) {
            scopeSpecified = false;
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySearcher6"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/> set to its default 
        ///    value, and <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> set to the respective given values.</para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectorySearcher(string filter, string[] propertiesToLoad, SearchScope scope) : this(null, filter, propertiesToLoad, scope) {            
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySearcher7"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.DirectoryServices.DirectorySearcher'/> class with the <see cref='System.DirectoryServices.DirectorySearcher.SearchRoot'/>, <see cref='System.DirectoryServices.DirectorySearcher.Filter'/>, <see cref='System.DirectoryServices.DirectorySearcher.PropertiesToLoad'/>, and <see cref='System.DirectoryServices.DirectorySearcher.SearchScope'/> properties set to the given 
        ///    values.</para>
        /// </devdoc>
        [
            DirectoryServicesPermission(SecurityAction.Demand, Unrestricted=true)
        ]
        public DirectorySearcher(DirectoryEntry searchRoot, string filter, string[] propertiesToLoad, SearchScope scope) {            
            this.searchRoot = searchRoot;
            this.filter = filter;
            if (propertiesToLoad != null)
                PropertiesToLoad.AddRange(propertiesToLoad);
            this.SearchScope = scope;
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.Dispose"]/*' />
        /// <devdoc>        
        /// </devdoc>
        protected override void Dispose(bool disposing) {
            // safe to call while finalizing or disposing
            //
            if ( !this.disposed && disposing) {
                if ( rootEntryAllocated )
                    searchRoot.Dispose();
                rootEntryAllocated = false;    
                this.disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.CacheResults"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value indicating whether the result should be cached on the
        ///       client machine.
        ///    </para>
        /// </devdoc>
        [
            DefaultValue(true), 
            DSDescriptionAttribute(Res.DSCacheResults)
        ]                            
        public bool CacheResults {
            get {
                return cacheResults;
            }
            set {
            	  // user explicitly set CacheResults to true and also want VLV
                if(directoryVirtualListViewSpecified == true && value == true)
                    throw new ArgumentException(Res.GetString(Res.DSBadCacheResultsVLV));
                
                cacheResults = value;

                cacheResultsSpecified = true;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.ClientTimeout"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the maximum amount of time that the client waits for
        ///       the server to return results. If the server does not respond within this time,
        ///       the search is aborted, and no results are returned.</para>
        /// </devdoc>
        [
            DSDescriptionAttribute(Res.DSClientTimeout)
        ]                    
        public TimeSpan ClientTimeout {
            get {
                return clientTimeout;
            }
            set {
                // prevent integer overflow
                if(value.TotalSeconds > Int32.MaxValue)
                {
                     throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");
                }
                
                clientTimeout = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.PropertyNamesOnly"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether the search should retrieve only the names of requested
        ///       properties or the names and values of requested properties.</para>
        /// </devdoc>        
        [
            DefaultValue(false), 
            DSDescriptionAttribute(Res.DSPropertyNamesOnly)
        ]
        public bool PropertyNamesOnly {
            get {
                return propertyNamesOnly;
            }
            set {
                propertyNamesOnly = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.Filter"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the Lightweight Directory Access Protocol (LDAP) filter string
        ///       format.</para>
        ///    <![CDATA[ (objectClass=*) (!(objectClass=user)) (&(objectClass=user)(sn=Jones)) ]]>
        ///    </devdoc>
        [
            DefaultValue(defaultFilter), 
            DSDescriptionAttribute(Res.DSFilter),            
            TypeConverter("System.Diagnostics.Design.StringValueConverter, " + AssemblyRef.SystemDesign),
            SettingsBindable(true)
        ]
        public string Filter {
            get {
                return filter;
            }
            set {
                if (value == null || value.Length == 0)
                    value = defaultFilter;
                filter = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.PageSize"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the page size in a paged search.</para>
        /// </devdoc>
        [
            DefaultValue(0), 
            DSDescriptionAttribute(Res.DSPageSize)                        
        ]
        public int PageSize {
            get {
                return pageSize;
            }
            set {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.DSBadPageSize));

                // specify non-zero pagesize explicitly and also want dirsync
                if(directorySynchronizationSpecified == true && value !=0)
                    throw new ArgumentException(Res.GetString(Res.DSBadPageSizeDirsync));
                
                pageSize = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.PropertiesToLoad"]/*' />
        /// <devdoc>
        /// <para>Gets the set of properties retrieved during the search. By default, the <see cref='System.DirectoryServices.DirectoryEntry.Path'/>
        /// and <see cref='System.DirectoryServices.DirectoryEntry.Name'/> properties are retrieved.</para>
        /// </devdoc>
        [
            DSDescriptionAttribute(Res.DSPropertiesToLoad),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
            Editor("System.Windows.Forms.Design.StringCollectionEditor, " + AssemblyRef.SystemDesign, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing)
        ]
        public StringCollection PropertiesToLoad {
            get {
                if (propertiesToLoad == null) {
                    propertiesToLoad = new StringCollection();
                }
                return propertiesToLoad;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.ReferralChasing"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets how referrals are chased.</para>
        /// </devdoc>
        [
            DefaultValue(ReferralChasingOption.External), 
            DSDescriptionAttribute(Res.DSReferralChasing)                                    
        ]
        public ReferralChasingOption ReferralChasing {
            get {
                return referralChasing;
            }
            set {
                if (value != ReferralChasingOption.None &&
                    value != ReferralChasingOption.Subordinate &&
                    value != ReferralChasingOption.External &&
                    value != ReferralChasingOption.All) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ReferralChasingOption));
                    
                referralChasing = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.SearchScope"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the scope of the search that should be observed by the server.</para>
        /// </devdoc>
        [
            DefaultValue(SearchScope.Subtree), 
            DSDescriptionAttribute(Res.DSSearchScope),                                                
            SettingsBindable(true)
        ]
        public SearchScope SearchScope {
            get {
                return scope;
            }
            set {
                if (value < SearchScope.Base || value > SearchScope.Subtree) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SearchScope));

                // user explicitly set SearchScope to something other than Base and also want to do ASQ, it is not supported
                if(attributeScopeQuerySpecified == true && value != SearchScope.Base)
                {
                    throw new ArgumentException(Res.GetString(Res.DSBadASQSearchScope));
                }
                    
                scope = value;

                scopeSpecified = true;                
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.ServerPageTimeLimit"]/*' />
        /// <devdoc>
        ///    <para> Gets or sets the time limit that the server should 
        ///       observe to search a page of results (as opposed to
        ///       the time limit for the entire search).</para>
        /// </devdoc>
        [
            DSDescriptionAttribute(Res.DSServerPageTimeLimit)
        ]
        public TimeSpan ServerPageTimeLimit {
            get {
                return serverPageTimeLimit;
            }
            set {
                // prevent integer overflow
                if(value.TotalSeconds > Int32.MaxValue)
                {
                     throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");
                }
                
                serverPageTimeLimit = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.ServerTimeLimit"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the maximum amount of time the server spends searching. If the
        ///       time limit is reached, only entries found up to that point will be returned.</para>
        /// </devdoc>
        [
             DSDescriptionAttribute(Res.DSServerTimeLimit)
        ]
        public TimeSpan ServerTimeLimit {
            get {
                return serverTimeLimit;
            }
            set {
                // prevent integer overflow
                if(value.TotalSeconds > Int32.MaxValue)
                {
                     throw new ArgumentException(Res.GetString(Res.TimespanExceedMax), "value");
                }
                
                serverTimeLimit = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.SizeLimit"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the maximum number of objects that the 
        ///       server should return in a search.</para>
        /// </devdoc>
        [
            DefaultValue(0), 
            DSDescriptionAttribute(Res.DSSizeLimit)            
        ]
        public int SizeLimit {
            get {
                return sizeLimit;
            }
            set {
                if (value < 0)
                    throw new ArgumentException(Res.GetString(Res.DSBadSizeLimit));
                sizeLimit = value;
            }                                                                                 
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.SearchRoot"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the node in the Active Directory hierarchy 
        ///       at which the search will start.</para>
        /// </devdoc>
        [
            DSDescriptionAttribute(Res.DSSearchRoot),
            DefaultValueAttribute(null)            
        ]
        public DirectoryEntry SearchRoot {
            get {
                if (searchRoot == null && !DesignMode) {                    
                                                                                        
                    // get the default naming context. This should be the default root for the search.
                    DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE", true, null, null, AuthenticationTypes.Secure);
                    
                    //SECREVIEW: Searching the root of the DS will demand browse permissions
                    //                     on "*" or "LDAP://RootDSE".
                    string defaultNamingContext = (string) rootDSE.Properties["defaultNamingContext"][0];
                    rootDSE.Dispose();
                                                            
                    searchRoot = new DirectoryEntry("LDAP://" + defaultNamingContext, true, null, null, AuthenticationTypes.Secure);
                    rootEntryAllocated = true;    
                    assertDefaultNamingContext = "LDAP://" + defaultNamingContext;
                    
                }
                return searchRoot;
            }
            set {
                if ( rootEntryAllocated )
                    searchRoot.Dispose();
                rootEntryAllocated = false;    

                assertDefaultNamingContext = null;
                searchRoot = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.Sort"]/*' />
        /// <devdoc>
        ///    <para>Gets the property on which the results should be 
        ///       sorted.</para>
        /// </devdoc>
        [
            DSDescriptionAttribute(Res.DSSort),
            TypeConverterAttribute(typeof(ExpandableObjectConverter)),
            DesignerSerializationVisibility(DesignerSerializationVisibility.Content)
        ]
        public SortOption Sort {
            get {
                return sort;
            }
            
            set {
                if (value == null)
                    throw new ArgumentNullException("value");
                
                sort = value;                    
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.Asynchronous"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether searches should be carried out in an asynchronous  
        ///       way.</para>
        /// </devdoc>
        [
            DefaultValue(false), 
            DSDescriptionAttribute(Res.DSAsynchronous),
            ComVisible(false)
        ]
        public bool Asynchronous {
            get{
            	return asynchronous;
            }
            set {
            	asynchronous = value;
            }
        }        

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.Tombstone"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value indicateing whether the search should also return deleted objects that match the search  
        ///       filter.</para>
        /// </devdoc>
        [
            DefaultValue(false),
            DSDescriptionAttribute(Res.DSTombstone),
            ComVisible(false)
        ]
        public bool Tombstone {
            get{
            	return tombstone;
            }
            set{
            	tombstone = value;
            }
        }
        
        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.AttributeScopeQuery"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets an attribute name to indicate that an attribute-scoped query search should be   
        ///       performed.</para>
        /// </devdoc>
        [
            DefaultValue(""),
            DSDescriptionAttribute(Res.DSAttributeQuery),
            TypeConverter("System.Diagnostics.Design.StringValueConverter, " + AssemblyRef.SystemDesign),
            ComVisible(false)
        ]
        public string AttributeScopeQuery {
            get{
            	return attributeScopeQuery;
            }
            set {
                if (value == null)
                    value = "";

                // user explicitly set AttributeScopeQuery and value is not null or empty string
                if (value.Length != 0)
                {                    
                    if(scopeSpecified == true && SearchScope != SearchScope.Base)
                    {
                        throw new ArgumentException(Res.GetString(Res.DSBadASQSearchScope));
                    }

                    // if user did not explicitly set search scope
                    scope = SearchScope.Base;                    

                    attributeScopeQuerySpecified = true;
                }
                else
                // user explicitly sets the value to default one and doesn't want to do asq
                {
                    attributeScopeQuerySpecified = false;
                }
                
                attributeScopeQuery = value;                
                	
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DerefAlias"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate how the aliases of found objects are to be  
        ///       resolved.</para>
        /// </devdoc>
        [
            DefaultValue(DereferenceAlias.Never),
            DSDescriptionAttribute(Res.DSDerefAlias),        	
            ComVisible(false)
        ]
        public DereferenceAlias DerefAlias {
            get {
            	return derefAlias;
            }

            set {
            	if (value < DereferenceAlias.Never || value > DereferenceAlias.Always) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DereferenceAlias));
                    
                derefAlias = value;
            }
        
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.SecurityMasks"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the search should return security access information for the specified 
        ///       attributes.</para>
        /// </devdoc>
        [
            DefaultValue(SecurityMasks.None),
            DSDescriptionAttribute(Res.DSSecurityMasks),
            ComVisible(false)
        ]
        public SecurityMasks SecurityMasks{
            get {
            	return securityMask;
            }
            set { 
                // make sure the behavior is consistent with native ADSI
                if(value > (SecurityMasks.None | SecurityMasks.Owner | SecurityMasks.Group | SecurityMasks.Dacl | SecurityMasks.Sacl))
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SecurityMasks));
                
            	securityMask = value;
            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.ExtendedDn"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to return extended DNs according to the requested 
        ///       format.</para>
        /// </devdoc>
        [
            DefaultValue(ExtendedDN.None),
            DSDescriptionAttribute(Res.DSExtendedDn),
            ComVisible(false)
        ]
        public ExtendedDN ExtendedDN{
            get {
            	return extendedDN;
            }
            set {
            	if (value < ExtendedDN.None || value > ExtendedDN.Standard) 
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ExtendedDN));

            	extendedDN = value;

            }
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.DirectorySynchronization"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate a directory synchronization search, which returns all changes since a specified
        ///       state.</para>
        /// </devdoc>
        [
            DefaultValue(null),
            DSDescriptionAttribute(Res.DSDirectorySynchronization),
            Browsable(false),
            ComVisible(false)
        ]
        public DirectorySynchronization DirectorySynchronization {
            get {            	
                // if user specifies dirsync search preference and search is executed
            	if(directorySynchronizationSpecified && searchResult != null)
            	{
            	    sync.ResetDirectorySynchronizationCookie(searchResult.DirsyncCookie);
            	}   
            	return sync;
            }

            set {
                // specify non-zero pagesize explicitly and also want dirsync      
            	if(value != null)
            	{
            	    if(PageSize != 0)
                        throw new ArgumentException(Res.GetString(Res.DSBadPageSizeDirsync));
            	    
            	    directorySynchronizationSpecified = true;
            	}
            	else
                // user explicitly sets the value to default one and doesn't want to do dirsync
                {
                    directorySynchronizationSpecified = false;
                }

            	sync = value;
            }
            
        }

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.VirtualListView"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value to indicate the search should use the LDAP virtual list view (VLV)
        ///       control.</para>
        /// </devdoc>
        [
            DefaultValue(null),
            DSDescriptionAttribute(Res.DSVirtualListView),
            Browsable(false),
            ComVisible(false)
        ]
        public DirectoryVirtualListView VirtualListView {
            get {
                // if user specifies dirsync search preference and search is executed
                if(directoryVirtualListViewSpecified && searchResult != null)
                {
                    DirectoryVirtualListView tempval = searchResult.VLVResponse;
                    vlv.Offset = tempval.Offset;
                    vlv.ApproximateTotal = tempval.ApproximateTotal;
                    vlv.DirectoryVirtualListViewContext = tempval.DirectoryVirtualListViewContext;
                    if(vlv.ApproximateTotal != 0)
                        vlv.TargetPercentage = (int) ((double)vlv.Offset/vlv.ApproximateTotal*100);
                    else
                        vlv.TargetPercentage = 0;
                }
            	return vlv;
            }
            set {
                // if user explicitly set CacheResults to true and also want to set VLV                
            	if(value != null)
            	{
            	    if(cacheResultsSpecified == true && CacheResults == true)
                        throw new ArgumentException(Res.GetString(Res.DSBadCacheResultsVLV));
            	    
            	    directoryVirtualListViewSpecified = true;
            	    // if user does not explicit specify cache results to true and also do vlv, then cache results is default to false
            	    cacheResults = false;
            	}
            	else
                // user explicitly sets the value to default one and doesn't want to do vlv
                {
                    directoryVirtualListViewSpecified = false;
                }
                
            	vlv = value;
            }
        }



        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.FindOne"]/*' />
        /// <devdoc>
        ///    <para>Executes the search and returns only the first entry that is found.</para>
        /// </devdoc>                
        public SearchResult FindOne() {
            DirectorySynchronization tempsync = null;	
            DirectoryVirtualListView tempvlv = null;
            SearchResult resultEntry = null;

            SearchResultCollection results = FindAll(false);

            try
            {
                foreach (SearchResult entry in results) {
                    // need to get the dirsync cookie
                    if(directorySynchronizationSpecified)
                        tempsync = DirectorySynchronization;

                    // need to get the vlv response
                    if(directoryVirtualListViewSpecified)
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

        /// <include file='doc\DirectorySearcher.uex' path='docs/doc[@for="DirectorySearcher.FindAll"]/*' />
        /// <devdoc>
        ///    <para> Executes the search and returns a collection of the entries that are found.</para>
        /// </devdoc>                
        public SearchResultCollection FindAll() {
            return FindAll(true);
        }
        
        private SearchResultCollection FindAll(bool findMoreThanOne) {
            searchResult = null;            
                        
            DirectoryEntry clonedRoot = null;
            if (assertDefaultNamingContext == null) {                 
                clonedRoot = SearchRoot.CloneBrowsable();
            }                
            else {                                                                                                             
                clonedRoot = SearchRoot.CloneBrowsable();
            }    
            
            UnsafeNativeMethods.IAds adsObject = clonedRoot.AdsObject;                        
            if (!(adsObject is UnsafeNativeMethods.IDirectorySearch))
                throw new NotSupportedException(Res.GetString(Res.DSSearchUnsupported, SearchRoot.Path));

            // this is a little bit hacky, but we need to perform a bind here, so we make sure the LDAP connection that we hold has more than
            // one reference count, one by SearchResultCollection object, one by DirectorySearcher object. In this way, when user calls
            // Dispose on SearchResultCollection, the connection is still there instead of reference count dropping to zero and being closed.
            // It is especially important for virtuallistview case, in order to reuse the vlv response, the search must be performed on the same ldap connection

            // only do it when vlv is used
            if(directoryVirtualListViewSpecified)
            {
                SearchRoot.Bind(true);                 
            }
                                                                    
            UnsafeNativeMethods.IDirectorySearch adsSearch = (UnsafeNativeMethods.IDirectorySearch) adsObject;
            SetSearchPreferences(adsSearch, findMoreThanOne);

            string[] properties = null;
            if (PropertiesToLoad.Count > 0) {
                if ( !PropertiesToLoad.Contains("ADsPath") ) {
                    // if we don't get this property, we won't be able to return a list of DirectoryEntry objects!                
                    PropertiesToLoad.Add("ADsPath");
                }
                properties = new string[PropertiesToLoad.Count];
                PropertiesToLoad.CopyTo(properties, 0);
            }
            
            IntPtr resultsHandle;
            if ( properties != null )
                adsSearch.ExecuteSearch(Filter, properties, properties.Length, out resultsHandle);
            else {
                adsSearch.ExecuteSearch(Filter, null, -1, out resultsHandle);
                properties = new string[0];                    
            }                
            
            SearchResultCollection result = new SearchResultCollection(clonedRoot, resultsHandle, properties, this);
            searchResult = result;
            return  result;         
        }

        private unsafe void SetSearchPreferences(UnsafeNativeMethods.IDirectorySearch adsSearch, bool findMoreThanOne) {
            ArrayList prefList = new ArrayList();
            AdsSearchPreferenceInfo info;

            // search scope
            info = new AdsSearchPreferenceInfo();
            info.dwSearchPref = (int) AdsSearchPreferences.SEARCH_SCOPE;
            info.vValue = new AdsValueHelper((int) SearchScope).GetStruct();
            prefList.Add(info);

            // size limit
            if (sizeLimit != 0 || !findMoreThanOne) {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.SIZE_LIMIT;
                info.vValue = new AdsValueHelper(findMoreThanOne ? SizeLimit : 1).GetStruct();
                prefList.Add(info);
            }

            // time limit
            if (ServerTimeLimit >= new TimeSpan(0)) {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.TIME_LIMIT;
                info.vValue = new AdsValueHelper((int) ServerTimeLimit.TotalSeconds).GetStruct();
                prefList.Add(info);
            }

            // propertyNamesOnly
            info = new AdsSearchPreferenceInfo();
            info.dwSearchPref = (int) AdsSearchPreferences.ATTRIBTYPES_ONLY;
            info.vValue = new AdsValueHelper(PropertyNamesOnly).GetStruct();
            prefList.Add(info);

            // Timeout
            if (ClientTimeout >= new TimeSpan(0)) {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.TIMEOUT;
                info.vValue = new AdsValueHelper((int) ClientTimeout.TotalSeconds).GetStruct();
                prefList.Add(info);
            }

            // page size
            if (PageSize != 0) {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.PAGESIZE;
                info.vValue = new AdsValueHelper(PageSize).GetStruct();
                prefList.Add(info);
            }

            // page time limit
            if (ServerPageTimeLimit >= new TimeSpan(0)) {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.PAGED_TIME_LIMIT;
                info.vValue = new AdsValueHelper((int) ServerPageTimeLimit.TotalSeconds).GetStruct();
                prefList.Add(info);
            }

            // chase referrals
            info = new AdsSearchPreferenceInfo();
            info.dwSearchPref = (int) AdsSearchPreferences.CHASE_REFERRALS;
            info.vValue = new AdsValueHelper((int) ReferralChasing).GetStruct();
            prefList.Add(info);

            // asynchronous
            if(Asynchronous == true)
            {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.ASYNCHRONOUS;
                info.vValue = new AdsValueHelper(Asynchronous).GetStruct();
                prefList.Add(info);
            }

            // tombstone
            if(Tombstone == true)
            {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.TOMBSTONE;
                info.vValue = new AdsValueHelper(Tombstone).GetStruct();
                prefList.Add(info);
            }

            // attributescopequery
            if (attributeScopeQuerySpecified)
            {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.ATTRIBUTE_QUERY;
                info.vValue = new AdsValueHelper(AttributeScopeQuery, AdsType.ADSTYPE_CASE_IGNORE_STRING).GetStruct();
                prefList.Add(info);
            }

            // derefalias
            if(DerefAlias != DereferenceAlias.Never)
            {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.DEREF_ALIASES;
                info.vValue = new AdsValueHelper((int) DerefAlias).GetStruct();
                prefList.Add(info);
            }

            // securitymask
            if(SecurityMasks != SecurityMasks.None)
            {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.SECURITY_MASK;
                info.vValue = new AdsValueHelper((int) SecurityMasks).GetStruct();
                prefList.Add(info);
            }

            // extendeddn
            if(ExtendedDN != ExtendedDN.None)
            {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.EXTENDED_DN;
                info.vValue = new AdsValueHelper((int) ExtendedDN).GetStruct();
                prefList.Add(info);
            }

            // dirsync
            if(directorySynchronizationSpecified)
            {
                info = new AdsSearchPreferenceInfo();
                info.dwSearchPref = (int) AdsSearchPreferences.DIRSYNC;                    
                info.vValue = new AdsValueHelper(DirectorySynchronization.GetDirectorySynchronizationCookie(), AdsType.ADSTYPE_PROV_SPECIFIC).GetStruct();
                prefList.Add(info);                     

                if(DirectorySynchronization.Option != DirectorySynchronizationOptions.None)
                {
                    info = new AdsSearchPreferenceInfo();
                    info.dwSearchPref = (int) AdsSearchPreferences.DIRSYNC_FLAG;
                    info.vValue = new AdsValueHelper((int)DirectorySynchronization.Option).GetStruct();
                    prefList.Add(info);
                    
                }
                    
            }
            



            IntPtr ptrToFree = (IntPtr)0;
            IntPtr ptrVLVToFree = (IntPtr)0;
            IntPtr ptrVLVContexToFree = (IntPtr)0;

            try {
                // sort
                if (Sort.PropertyName != null && Sort.PropertyName.Length > 0) {
                    info = new AdsSearchPreferenceInfo();
                    info.dwSearchPref = (int) AdsSearchPreferences.SORT_ON;
                    AdsSortKey sortKey = new AdsSortKey();
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
                if(directoryVirtualListViewSpecified) {
                    info = new AdsSearchPreferenceInfo();
                    info.dwSearchPref = (int) AdsSearchPreferences.VLV;
                    AdsVLV vlvValue = new AdsVLV();
                    vlvValue.beforeCount = vlv.BeforeCount;
                    vlvValue.afterCount = vlv.AfterCount;
                    vlvValue.offset = vlv.Offset;
                    //we need to treat the empty string as null here
                    if(vlv.Target.Length != 0)
                        vlvValue.target = Marshal.StringToCoTaskMemUni(vlv.Target);
                    else
                        vlvValue.target = IntPtr.Zero;
                    ptrVLVToFree = vlvValue.target;
                    if(vlv.DirectoryVirtualListViewContext == null)
                    {
                        vlvValue.contextIDlength = 0;
                        vlvValue.contextID = (IntPtr)0;
                    }
                    else
                    {
                        vlvValue.contextIDlength = vlv.DirectoryVirtualListViewContext.context.Length;
                        vlvValue.contextID = Marshal.AllocCoTaskMem(vlvValue.contextIDlength);
                        ptrVLVContexToFree = vlvValue.contextID;
                        Marshal.Copy(vlv.DirectoryVirtualListViewContext.context, 0, vlvValue.contextID, vlvValue.contextIDlength);                	    
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
                if(cacheResultsSpecified)
                {
                    info = new AdsSearchPreferenceInfo();
                    info.dwSearchPref = (int) AdsSearchPreferences.CACHE_RESULTS;
                    info.vValue = new AdsValueHelper(CacheResults).GetStruct();
                    prefList.Add(info);
                }
                
                //
                // now make the call
                //                
                AdsSearchPreferenceInfo[] prefs = new AdsSearchPreferenceInfo[prefList.Count];
                for (int i = 0; i < prefList.Count; i++) {
                    prefs[i] = (AdsSearchPreferenceInfo) prefList[i];
                }

	        DoSetSearchPrefs(adsSearch, prefs);
            }
            finally {
                if (ptrToFree != (IntPtr)0)
                    Marshal.FreeCoTaskMem(ptrToFree);

                if(ptrVLVToFree != (IntPtr)0)
                    Marshal.FreeCoTaskMem(ptrVLVToFree);

                if(ptrVLVContexToFree != (IntPtr)0)
                    Marshal.FreeCoTaskMem(ptrVLVContexToFree);

            }
        }

        private static void DoSetSearchPrefs(UnsafeNativeMethods.IDirectorySearch adsSearch, AdsSearchPreferenceInfo[] prefs) {
            int structSize = Marshal.SizeOf(typeof(AdsSearchPreferenceInfo));
            IntPtr ptr = Marshal.AllocHGlobal((IntPtr)(structSize * prefs.Length));
            try {
                IntPtr tempPtr = ptr;
                for (int i = 0; i < prefs.Length; i++) {
                    Marshal.StructureToPtr( prefs[i], tempPtr, false );
                    tempPtr = IntPtr.Add( tempPtr , structSize );
                }
 
                adsSearch.SetSearchPreference(ptr, prefs.Length);   
                
                // Check for the result status for all preferences
                tempPtr = ptr;
                for (int i = 0; i < prefs.Length; i++) {
                    int status = Marshal.ReadInt32(tempPtr, 32);
                    if ( status != 0 ) {
                        int prefIndex = prefs[i].dwSearchPref;
                        string property = "";
                        switch (prefIndex) {
                            case (int) AdsSearchPreferences.SEARCH_SCOPE:
                                property = "SearchScope";
                                break; 
                            case (int) AdsSearchPreferences.SIZE_LIMIT:
                                property = "SizeLimit";
                                break; 
                            case (int) AdsSearchPreferences.TIME_LIMIT:
                                property = "ServerTimeLimit";
                                break; 
                            case (int) AdsSearchPreferences.ATTRIBTYPES_ONLY:
                                property = "PropertyNamesOnly";
                                break;                                 
                            case (int) AdsSearchPreferences.TIMEOUT:
                                property = "ClientTimeout";
                                break;                                                                 
                            case (int) AdsSearchPreferences.PAGESIZE:
                                property = "PageSize";
                                break;                                                                 
                            case (int) AdsSearchPreferences.PAGED_TIME_LIMIT:
                                property = "ServerPageTimeLimit";
                                break;                                                                                                 
                            case (int) AdsSearchPreferences.CHASE_REFERRALS:
                                property = "ReferralChasing";
                                break;                                                                                                                                                                                                                                        
                            case (int) AdsSearchPreferences.SORT_ON:
                                property = "Sort";
                                break;           
                            case (int) AdsSearchPreferences.CACHE_RESULTS:
                                property = "CacheResults";
                                break;                                                                   
                            case (int) AdsSearchPreferences.ASYNCHRONOUS:
                            	property = "Asynchronous";
                            	break;
                            case (int) AdsSearchPreferences.TOMBSTONE:
                            	property = "Tombstone";
                            	break;
                            case (int) AdsSearchPreferences.ATTRIBUTE_QUERY:
                            	property = "AttributeScopeQuery";
                            	break;
                            case (int) AdsSearchPreferences.DEREF_ALIASES:
                            	property = "DerefAlias";
                            	break;
                            case (int) AdsSearchPreferences.SECURITY_MASK:
                            	property = "SecurityMasks";
                            	break;
                            case (int) AdsSearchPreferences.EXTENDED_DN:
                            	property = "ExtendedDn";
                            	break;
                            case (int) AdsSearchPreferences.DIRSYNC:
                            	property = "DirectorySynchronization";
                            	break;
                            case (int) AdsSearchPreferences.DIRSYNC_FLAG:
                            	property = "DirectorySynchronizationFlag";
                            	break;                            
                            case (int) AdsSearchPreferences.VLV:
                            	property = "VirtualListView";
                            	break;

                        }
                        throw new InvalidOperationException(Res.GetString(Res.DSSearchPreferencesNotAccepted, property));
                    }                        
                        
                    tempPtr = IntPtr.Add(tempPtr, structSize);
                }             
            }
            finally {
                    Marshal.FreeHGlobal(ptr);
            }
        }        

    }

}
