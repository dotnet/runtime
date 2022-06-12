// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ConfigurationSet
    {
        // Private Variables
        private readonly DirectoryContext _context;
        private readonly DirectoryEntryManager _directoryEntryMgr;
        private bool _disposed;

        // variables corresponding to public properties
        private readonly string _configSetName;
        private ReadOnlySiteCollection? _cachedSites;
        private AdamInstanceCollection? _cachedADAMInstances;
        private ApplicationPartitionCollection? _cachedApplicationPartitions;
        private ActiveDirectorySchema? _cachedSchema;
        private AdamInstance? _cachedSchemaRoleOwner;
        private AdamInstance? _cachedNamingRoleOwner;
        private ReplicationSecurityLevel _cachedSecurityLevel = (ReplicationSecurityLevel)(-1);

        // 4 minutes timeout for locating an ADAM instance in the configset
        private static readonly TimeSpan s_locationTimeout = new TimeSpan(0, 4, 0);

        #region constructors
        internal ConfigurationSet(DirectoryContext context, string configSetName, DirectoryEntryManager directoryEntryMgr)
        {
            _context = context;
            _configSetName = configSetName;
            _directoryEntryMgr = directoryEntryMgr;
        }

        internal ConfigurationSet(DirectoryContext context, string configSetName)
            : this(context, configSetName, new DirectoryEntryManager(context))
        {
        }
        #endregion constructors

        #region IDisposable

        public void Dispose() => Dispose(true);

        // private Dispose method
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // check if this is an explicit Dispose
                // only then clean up the directory entries
                if (disposing)
                {
                    // dispose all directory entries
                    foreach (DirectoryEntry entry in _directoryEntryMgr.GetCachedDirectoryEntries())
                    {
                        entry.Dispose();
                    }
                }
                _disposed = true;
            }
        }
        #endregion IDisposable

        #region public methods

        public static ConfigurationSet GetConfigurationSet(DirectoryContext context)
        {
            // check that the argument is not null
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // target should ConfigurationSet or DirectoryServer
            if ((context.ContextType != DirectoryContextType.ConfigurationSet) &&
                (context.ContextType != DirectoryContextType.DirectoryServer))
            {
                throw new ArgumentException(SR.TargetShouldBeServerORConfigSet, nameof(context));
            }

            // target should be an adam config set or server
            if (((!context.isServer()) && (!context.isADAMConfigSet())))
            {
                // the target should be a server or an ADAM Config Set
                if (context.ContextType == DirectoryContextType.ConfigurationSet)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.ConfigSetNotFound, typeof(ConfigurationSet), context.Name);
                }
                else
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.AINotFound, context.Name), typeof(ConfigurationSet), null);
                }
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            //
            // bind to rootdse of an adam instance (if target is already a server, verify that it is an adam instance)
            //
            DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(context);
            DirectoryEntry? rootDSE = null;
            string configSetName;

            try
            {
                rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                if ((context.isServer()) && (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectoryApplicationMode)))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.AINotFound, context.Name), typeof(ConfigurationSet), null);
                }

                configSetName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.ConfigurationNamingContext)!;
            }
            catch (COMException e)
            {
                int errorCode = e.ErrorCode;

                if (errorCode == unchecked((int)0x8007203a))
                {
                    if (context.ContextType == DirectoryContextType.ConfigurationSet)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.ConfigSetNotFound, typeof(ConfigurationSet), context.Name);
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.AINotFound, context.Name), typeof(ConfigurationSet), null);
                    }
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                if (context.ContextType == DirectoryContextType.ConfigurationSet)
                {
                    // this is the case when we could not find an ADAM instance in that config set
                    throw new ActiveDirectoryObjectNotFoundException(SR.ConfigSetNotFound, typeof(ConfigurationSet), context.Name);
                }
                else
                    throw;
            }

            // return config set object
            return new ConfigurationSet(context, configSetName, directoryEntryMgr);
        }

        public AdamInstance FindAdamInstance()
        {
            CheckIfDisposed();
            return FindOneAdamInstance(Name, _context, null, null);
        }

        public AdamInstance FindAdamInstance(string partitionName)
        {
            CheckIfDisposed();

            if (partitionName == null)
            {
                throw new ArgumentNullException(nameof(partitionName));
            }

            return FindOneAdamInstance(Name, _context, partitionName, null);
        }

        public AdamInstance FindAdamInstance(string? partitionName, string siteName)
        {
            CheckIfDisposed();

            //
            // null partitionName would signify that we don't care about the partition
            //

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return FindOneAdamInstance(Name, _context, partitionName, siteName);
        }

        public AdamInstanceCollection FindAllAdamInstances()
        {
            CheckIfDisposed();

            return FindAdamInstances(_context, null, null);
        }

        public AdamInstanceCollection FindAllAdamInstances(string partitionName)
        {
            CheckIfDisposed();

            if (partitionName == null)
            {
                throw new ArgumentNullException(nameof(partitionName));
            }

            return FindAdamInstances(_context, partitionName, null);
        }

        public AdamInstanceCollection FindAllAdamInstances(string? partitionName, string siteName)
        {
            CheckIfDisposed();

            //
            // null partitionName would signify that we don't care about the partition
            //

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return FindAdamInstances(_context, partitionName, siteName);
        }

        public DirectoryEntry GetDirectoryEntry()
        {
            CheckIfDisposed();
            return DirectoryEntryManager.GetDirectoryEntry(_context, WellKnownDN.ConfigurationNamingContext);
        }

        public ReplicationSecurityLevel GetSecurityLevel()
        {
            CheckIfDisposed();
            if (_cachedSecurityLevel == (ReplicationSecurityLevel)(-1))
            {
                DirectoryEntry configEntry = _directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.ConfigurationNamingContext);
                _cachedSecurityLevel = (ReplicationSecurityLevel)((int)PropertyManager.GetPropertyValue(_context, configEntry, PropertyManager.MsDSReplAuthenticationMode)!);
            }
            return _cachedSecurityLevel;
        }

        public void SetSecurityLevel(ReplicationSecurityLevel securityLevel)
        {
            CheckIfDisposed();
            if (securityLevel < ReplicationSecurityLevel.NegotiatePassThrough || securityLevel > ReplicationSecurityLevel.MutualAuthentication)
            {
                throw new InvalidEnumArgumentException(nameof(securityLevel), (int)securityLevel, typeof(ReplicationSecurityLevel));
            }

            try
            {
                DirectoryEntry configEntry = _directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.ConfigurationNamingContext);
                configEntry.Properties[PropertyManager.MsDSReplAuthenticationMode].Value = (int)securityLevel;
                configEntry.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }

            // invalidate the cached entry
            _cachedSecurityLevel = (ReplicationSecurityLevel)(-1);
        }

        public override string ToString() => Name;

        #endregion public methods

        #region public properties

        public string Name
        {
            get
            {
                CheckIfDisposed();
                return _configSetName;
            }
        }

        public ReadOnlySiteCollection Sites
        {
            get
            {
                CheckIfDisposed();
                if (_cachedSites == null)
                {
                    _cachedSites = new ReadOnlySiteCollection(GetSites());
                }
                return _cachedSites;
            }
        }

        public AdamInstanceCollection AdamInstances
        {
            get
            {
                CheckIfDisposed();
                if (_cachedADAMInstances == null)
                {
                    _cachedADAMInstances = FindAllAdamInstances();
                }
                return _cachedADAMInstances;
            }
        }

        public ApplicationPartitionCollection ApplicationPartitions
        {
            get
            {
                CheckIfDisposed();
                if (_cachedApplicationPartitions == null)
                {
                    _cachedApplicationPartitions = new ApplicationPartitionCollection(GetApplicationPartitions());
                }
                return _cachedApplicationPartitions;
            }
        }

        public ActiveDirectorySchema Schema
        {
            get
            {
                CheckIfDisposed();
                if (_cachedSchema == null)
                {
                    try
                    {
                        _cachedSchema = new ActiveDirectorySchema(_context, _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext));
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                    }
                }
                return _cachedSchema;
            }
        }

        public AdamInstance SchemaRoleOwner
        {
            get
            {
                CheckIfDisposed();
                if (_cachedSchemaRoleOwner == null)
                {
                    _cachedSchemaRoleOwner = GetRoleOwner(AdamRole.SchemaRole);
                }
                return _cachedSchemaRoleOwner;
            }
        }

        public AdamInstance NamingRoleOwner
        {
            get
            {
                CheckIfDisposed();
                if (_cachedNamingRoleOwner == null)
                {
                    _cachedNamingRoleOwner = GetRoleOwner(AdamRole.NamingRole);
                }
                return _cachedNamingRoleOwner;
            }
        }

        #endregion public properties

        #region private methods

        private static DirectoryEntry GetSearchRootEntry(Forest forest)
        {
            DirectoryEntry rootEntry;
            DirectoryContext forestContext = forest.GetDirectoryContext();
            bool isServer = false;
            bool isGC = false;
            AuthenticationTypes authType = Utils.DefaultAuthType;

            if (forestContext.ContextType == DirectoryContextType.DirectoryServer)
            {
                //
                // the forest object was created by specifying a server name
                // so we will stick to that server for the search. We need to determine
                // whether or not the server is a DC or GC
                //
                isServer = true;
                DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(forestContext, WellKnownDN.RootDSE);
                string? isGCReady = (string?)PropertyManager.GetPropertyValue(forestContext, rootDSE, PropertyManager.IsGlobalCatalogReady);
                isGC = (Utils.Compare(isGCReady, "TRUE") == 0);
            }

            if (isServer)
            {
                authType |= AuthenticationTypes.ServerBind;

                if (isGC)
                {
                    rootEntry = new DirectoryEntry("GC://" + forestContext.GetServerName(), forestContext.UserName, forestContext.Password, authType);
                }
                else
                {
                    rootEntry = new DirectoryEntry("LDAP://" + forestContext.GetServerName(), forestContext.UserName, forestContext.Password, authType);
                }
            }
            else
            {
                // need to find any GC in the forest
                rootEntry = new DirectoryEntry("GC://" + forest.Name, forestContext.UserName, forestContext.Password, authType);
            }

            return rootEntry;
        }

        internal static AdamInstance FindAnyAdamInstance(DirectoryContext context)
        {
            if (context.ContextType != DirectoryContextType.ConfigurationSet)
            {
                // assuming it's an ADAM Instance
                // check that it is an ADAM server only (not AD)
                DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(context);
                DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);

                if (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectoryApplicationMode))
                {
                    directoryEntryMgr.RemoveIfExists(directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.RootDSE));
                    throw new ArgumentException(SR.TargetShouldBeServerORConfigSet, nameof(context));
                }

                string dnsHostName = (string?)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DnsHostName)!;

                return new AdamInstance(context, dnsHostName, directoryEntryMgr);
            }

            // Now this is the case where context is a Config Set
            // Here we need to search for the service connection points in the forest
            // (if the forest object was created by specifying the server, we stick to that, else search in a GC)
            DirectoryEntry rootEntry = GetSearchRootEntry(Forest.GetCurrentForest());
            ArrayList adamInstanceNames = new ArrayList();

            try
            {
                // Search for computer "serviceConnectionObjects" where the keywords attribute
                // contains the specified keyword
                // set up the searcher object

                // build the filter
                StringBuilder str = new StringBuilder(15);
                str.Append("(&(");
                str.Append(PropertyManager.ObjectCategory);
                str.Append("=serviceConnectionPoint)");
                str.Append('(');
                str.Append(PropertyManager.Keywords);
                str.Append("=1.2.840.113556.1.4.1851)(");
                str.Append(PropertyManager.Keywords);
                str.Append('=');
                str.Append(Utils.GetEscapedFilterValue(context.Name!)); // target = config set name
                str.Append("))");

                string filter = str.ToString();
                string[] propertiesToLoad = new string[1];

                propertiesToLoad[0] = PropertyManager.ServiceBindingInformation;

                ADSearcher searcher = new ADSearcher(rootEntry, filter, propertiesToLoad, SearchScope.Subtree, false /*not paged search*/, false /*no cached results*/);
                SearchResultCollection resCol = searcher.FindAll();

                try
                {
                    foreach (SearchResult res in resCol)
                    {
                        // the binding info contains two values
                        // "ldap://hostname:ldapport"
                        // and "ldaps://hostname:sslport"
                        // we need the "hostname:ldapport" value
                        string prefix = "ldap://";

                        foreach (string bindingInfo in res.Properties[PropertyManager.ServiceBindingInformation])
                        {
                            if ((bindingInfo.Length > prefix.Length) && (string.Equals(bindingInfo.Substring(0, prefix.Length), prefix, StringComparison.OrdinalIgnoreCase)))
                            {
                                adamInstanceNames.Add(bindingInfo.Substring(prefix.Length));
                            }
                        }
                    }
                }
                finally
                {
                    resCol.Dispose();
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                rootEntry.Dispose();
            }

            //
            // we have all the adam instance names in teh form of server:port from the scp
            // now we need to find one that is alive
            //
            return FindAliveAdamInstance(null, context, adamInstanceNames);
        }

        internal static AdamInstance FindOneAdamInstance(DirectoryContext context, string? partitionName, string? siteName)
        {
            return FindOneAdamInstance(null, context, partitionName, siteName);
        }

        internal static AdamInstance FindOneAdamInstance(string? configSetName, DirectoryContext context, string? partitionName, string? siteName)
        {
            // can expect valid context (non-null)
            if (partitionName != null && partitionName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(partitionName));
            }

            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
            }

            ArrayList ntdsaNames = Utils.GetReplicaList(context, partitionName, siteName, false /* isDefaultNC */, true /* isADAM */, false /* mustBeGC */);

            if (ntdsaNames.Count < 1)
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.ADAMInstanceNotFound, typeof(AdamInstance), null);
            }

            return FindAliveAdamInstance(configSetName, context, ntdsaNames);
        }

        internal static AdamInstanceCollection FindAdamInstances(DirectoryContext context, string? partitionName, string? siteName)
        {
            // can expect valid context (non-null)
            if (partitionName != null && partitionName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(partitionName));
            }

            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
            }

            ArrayList adamInstanceList = new ArrayList();

            foreach (string adamInstanceName in Utils.GetReplicaList(context, partitionName, siteName, false /* isDefaultNC */, true /* isADAM */, false /* mustBeGC */))
            {
                DirectoryContext adamInstContext = Utils.GetNewDirectoryContext(adamInstanceName, DirectoryContextType.DirectoryServer, context);
                adamInstanceList.Add(new AdamInstance(adamInstContext, adamInstanceName));
            }

            return new AdamInstanceCollection(adamInstanceList);
        }

        //
        // The input to this function is a list of adam instance names in the form server:port
        // This function tries to bind to each of the instances in this list sequentially until one of the following occurs:
        // 1.  An ADAM instance responds to an ldap_bind - we return an ADAMInstance object for that adam instance
        // 2.  We exceed the timeout duration - we return an ActiveDirectoryObjectNotFoundException
        //
        internal static AdamInstance FindAliveAdamInstance(string? configSetName, DirectoryContext context, ArrayList adamInstanceNames)
        {
            bool foundAliveADAMInstance = false;
            AdamInstance? adamInstance = null;

            // record the start time so that we can determine if the timeout duration has been exceeded or not
            DateTime startTime = DateTime.UtcNow;

            // loop through each adam instance and try to bind to the rootdse
            foreach (string adamInstanceName in adamInstanceNames)
            {
                DirectoryContext adamInstContext = Utils.GetNewDirectoryContext(adamInstanceName, DirectoryContextType.DirectoryServer, context);
                DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(adamInstContext);
                DirectoryEntry tempRootEntry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);

                try
                {
                    tempRootEntry.Bind(true);
                    adamInstance = new AdamInstance(adamInstContext, adamInstanceName, directoryEntryMgr, true /* nameIncludesPort */);
                    foundAliveADAMInstance = true;
                }
                catch (COMException e)
                {
                    // if this is server down /server busy / server unavailable / timeout  exception we should just eat this up and try the next one
                    if ((e.ErrorCode == unchecked((int)0x8007203a)) ||
                        (e.ErrorCode == unchecked((int)0x8007200e)) ||
                        (e.ErrorCode == unchecked((int)0x8007200f)) ||
                        (e.ErrorCode == unchecked((int)0x800705b4)))
                    {
                        // if we are passed the timeout period, we should throw, else do nothing
                        if (DateTime.UtcNow.Subtract(startTime) > s_locationTimeout)
                            throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet, configSetName ?? context.Name), typeof(AdamInstance), null);
                    }
                    else
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (foundAliveADAMInstance)
                {
                    return adamInstance!;
                }
            }

            // if we reach here, we haven't found an adam instance
            throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet, configSetName ?? context.Name), typeof(AdamInstance), null);
        }

        /// <returns>Returns a DomainController object for the DC that holds the specified FSMO role</returns>
        private AdamInstance GetRoleOwner(AdamRole role)
        {
            DirectoryEntry? entry = null;

            string? adamInstName = null;
            try
            {
                switch (role)
                {
                    case AdamRole.SchemaRole:
                        {
                            entry = _directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.SchemaNamingContext);
                            break;
                        }

                    case AdamRole.NamingRole:
                        {
                            entry = _directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.PartitionsContainer);
                            break;
                        }

                    default:
                        // should not happen since we are calling this only internally
                        Debug.Fail("ConfigurationSet.GetRoleOwner: Invalid role type.");
                        break;
                }
                entry.RefreshCache();
                adamInstName = Utils.GetAdamDnsHostNameFromNTDSA(_context, (string)PropertyManager.GetPropertyValue(_context, entry, PropertyManager.FsmoRoleOwner)!);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                if (entry != null)
                {
                    entry.Dispose();
                }
            }

            // create a new context object for the adam instance passing on  the
            // credentials from the context
            DirectoryContext adamInstContext = Utils.GetNewDirectoryContext(adamInstName, DirectoryContextType.DirectoryServer, _context);

            return new AdamInstance(adamInstContext, adamInstName);
        }

        private ArrayList GetSites()
        {
            ArrayList sites = new ArrayList();
            DirectoryEntry sitesEntry = _directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.SitesContainer);

            // search for all the "site" objects
            // (one-level search is good enough)
            // setup the directory searcher object
            string filter = "(" + PropertyManager.ObjectCategory + "=site)";
            string[] propertiesToLoad = new string[1];

            propertiesToLoad[0] = PropertyManager.Cn;

            ADSearcher searcher = new ADSearcher(sitesEntry, filter, propertiesToLoad, SearchScope.OneLevel);
            SearchResultCollection? resCol = null;

            try
            {
                resCol = searcher.FindAll();

                foreach (SearchResult res in resCol)
                {
                    // an existing site
                    sites.Add(new ActiveDirectorySite(_context, (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.Cn)!, true));
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                if (resCol != null)
                {
                    // call dispose on search result collection
                    resCol.Dispose();
                }
            }
            return sites;
        }

        private ArrayList GetApplicationPartitions()
        {
            ArrayList appNCs = new ArrayList();
            DirectoryEntry rootDSE = _directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
            DirectoryEntry partitionsEntry = _directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.PartitionsContainer);

            // search for all the "crossRef" objects that have the
            // ADS_SYSTEMFLAG_CR_NTDS_NC set and the SYSTEMFLAG_CR_NTDS_DOMAIN flag not set
            // (one-level search is good enough)
            // setup the directory searcher object

            // build the filter
            StringBuilder str = new StringBuilder(100);
            str.Append("(&(");
            str.Append(PropertyManager.ObjectCategory);
            str.Append("=crossRef)(");
            str.Append(PropertyManager.SystemFlags);
            str.Append(":1.2.840.113556.1.4.804:=");
            str.Append((int)SystemFlag.SystemFlagNtdsNC);
            str.Append(")(!(");
            str.Append(PropertyManager.SystemFlags);
            str.Append(":1.2.840.113556.1.4.803:=");
            str.Append((int)SystemFlag.SystemFlagNtdsDomain);
            str.Append(")))");

            string filter = str.ToString();
            string[] propertiesToLoad = new string[2];
            propertiesToLoad[0] = PropertyManager.NCName;
            propertiesToLoad[1] = PropertyManager.MsDSNCReplicaLocations;

            ADSearcher searcher = new ADSearcher(partitionsEntry, filter, propertiesToLoad, SearchScope.OneLevel);
            SearchResultCollection? resCol = null;

            try
            {
                resCol = searcher.FindAll();

                string? schemaNamingContext = (string?)PropertyManager.GetPropertyValue(_context, rootDSE, PropertyManager.SchemaNamingContext);
                string? configurationNamingContext = (string?)PropertyManager.GetPropertyValue(_context, rootDSE, PropertyManager.ConfigurationNamingContext);

                foreach (SearchResult res in resCol)
                {
                    // add the name of the appNC only if it is not
                    // the Schema or Configuration partition
                    string nCName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.NCName)!;

                    if ((!(nCName.Equals(schemaNamingContext))) && (!(nCName.Equals(configurationNamingContext))))
                    {
                        ResultPropertyValueCollection replicaLocations = res.Properties[PropertyManager.MsDSNCReplicaLocations];
                        if (replicaLocations.Count > 0)
                        {
                            string replicaName = Utils.GetAdamDnsHostNameFromNTDSA(_context, (string)replicaLocations[Utils.GetRandomIndex(replicaLocations.Count)]!);
                            DirectoryContext appNCContext = Utils.GetNewDirectoryContext(replicaName, DirectoryContextType.DirectoryServer, _context);
                            appNCs.Add(new ApplicationPartition(appNCContext, nCName, null, ApplicationPartitionType.ADAMApplicationPartition, new DirectoryEntryManager(appNCContext)));
                        }
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                if (resCol != null)
                {
                    // call dispose on search result collection
                    resCol.Dispose();
                }
            }
            return appNCs;
        }

        private void CheckIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
        #endregion private methods
    }
}
