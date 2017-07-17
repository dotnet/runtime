// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    internal enum NCFlags : int
    {
        InstanceTypeIsNCHead = 1,
        InstanceTypeIsWriteable = 4
    }

    internal enum ApplicationPartitionType : int
    {
        Unknown = -1,
        ADApplicationPartition = 0,
        ADAMApplicationPartition = 1
    }

    public class ApplicationPartition : ActiveDirectoryPartition
    {
        private bool _disposed = false;
        private ApplicationPartitionType _appType = (ApplicationPartitionType)(-1);
        private bool _committed = true;
        private DirectoryEntry _domainDNSEntry = null;
        private DirectoryEntry _crossRefEntry = null;
        private string _dnsName = null;
        private DirectoryServerCollection _cachedDirectoryServers = null;
        private bool _securityRefDomainModified = false;
        private string _securityRefDomain = null;

        #region constructors
        // Public Constructors
        public ApplicationPartition(DirectoryContext context, string distinguishedName)
        {
            // validate the parameters
            ValidateApplicationPartitionParameters(context, distinguishedName, null, false);

            // call private function for creating the application partition
            CreateApplicationPartition(distinguishedName, "domainDns");
        }

        public ApplicationPartition(DirectoryContext context, string distinguishedName, string objectClass)
        {
            // validate the parameters
            ValidateApplicationPartitionParameters(context, distinguishedName, objectClass, true);

            // call private function for creating the application partition
            CreateApplicationPartition(distinguishedName, objectClass);
        }

        // Internal Constructors
        internal ApplicationPartition(DirectoryContext context, string distinguishedName, string dnsName, ApplicationPartitionType appType, DirectoryEntryManager directoryEntryMgr)
            : base(context, distinguishedName)
        {
            this.directoryEntryMgr = directoryEntryMgr;
            _appType = appType;
            _dnsName = dnsName;
        }

        internal ApplicationPartition(DirectoryContext context, string distinguishedName, string dnsName, DirectoryEntryManager directoryEntryMgr)
            : this(context, distinguishedName, dnsName, GetApplicationPartitionType(context), directoryEntryMgr)
        {
        }
        #endregion constructors

        #region IDisposable
        // private Dispose method
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                try
                {
                    // if there are any managed or unmanaged 
                    // resources to be freed, those should be done here
                    // if disposing = true, only unmanaged resources should 
                    // be freed, else both managed and unmanaged.
                    if (_crossRefEntry != null)
                    {
                        _crossRefEntry.Dispose();
                        _crossRefEntry = null;
                    }
                    if (_domainDNSEntry != null)
                    {
                        _domainDNSEntry.Dispose();
                        _domainDNSEntry = null;
                    }

                    _disposed = true;
                }
                finally
                {
                    base.Dispose();
                }
            }
        }
        #endregion IDisposable

        #region public methods

        public static ApplicationPartition GetApplicationPartition(DirectoryContext context)
        {
            // validate the context
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            // contexttype should be ApplicationPartiton
            if (context.ContextType != DirectoryContextType.ApplicationPartition)
            {
                throw new ArgumentException(SR.TargetShouldBeAppNCDnsName, "context");
            }

            // target must be ndnc dns name
            if (!context.isNdnc())
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.NDNCNotFound, typeof(ApplicationPartition), context.Name);
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the application partition head (this will verify credentials)
            string distinguishedName = Utils.GetDNFromDnsName(context.Name);
            DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(context);
            DirectoryEntry appNCHead = null;

            try
            {
                appNCHead = directoryEntryMgr.GetCachedDirectoryEntry(distinguishedName);
                // need to force the bind
                appNCHead.Bind(true);
            }
            catch (COMException e)
            {
                int errorCode = e.ErrorCode;

                if (errorCode == unchecked((int)0x8007203a))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.NDNCNotFound, typeof(ApplicationPartition), context.Name);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            return new ApplicationPartition(context, distinguishedName, context.Name, ApplicationPartitionType.ADApplicationPartition, directoryEntryMgr);
        }

        public static ApplicationPartition FindByName(DirectoryContext context, string distinguishedName)
        {
            ApplicationPartition partition = null;
            DirectoryEntryManager directoryEntryMgr = null;
            DirectoryContext appNCContext = null;

            // check that the argument is not null
            if (context == null)
                throw new ArgumentNullException("context");

            if ((context.Name == null) && (!context.isRootDomain()))
            {
                throw new ArgumentException(SR.ContextNotAssociatedWithDomain, "context");
            }

            if (context.Name != null)
            {
                // the target should be a valid forest name, configset name or a server
                if (!((context.isRootDomain()) || (context.isADAMConfigSet()) || context.isServer()))
                {
                    throw new ArgumentException(SR.NotADOrADAM, "context");
                }
            }

            // check that the distingushed name of the application partition is not null or empty
            if (distinguishedName == null)
                throw new ArgumentNullException("distinguishedName");

            if (distinguishedName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "distinguishedName");

            if (!Utils.IsValidDNFormat(distinguishedName))
                throw new ArgumentException(SR.InvalidDNFormat, "distinguishedName");

            //  work with copy of the context
            context = new DirectoryContext(context);

            // search in the partitions container of the forest for 
            // crossRef objects that have their nCName set to the specified distinguishedName
            directoryEntryMgr = new DirectoryEntryManager(context);
            DirectoryEntry partitionsEntry = null;

            try
            {
                partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet , context.Name));
            }

            // build the filter
            StringBuilder str = new StringBuilder(15);
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
            str.Append("))(");
            str.Append(PropertyManager.NCName);
            str.Append("=");
            str.Append(Utils.GetEscapedFilterValue(distinguishedName));
            str.Append("))");

            string filter = str.ToString();
            string[] propertiesToLoad = new string[2];

            propertiesToLoad[0] = PropertyManager.DnsRoot;
            propertiesToLoad[1] = PropertyManager.NCName;

            ADSearcher searcher = new ADSearcher(partitionsEntry, filter, propertiesToLoad, SearchScope.OneLevel, false /*not paged search*/, false /*no cached results*/);
            SearchResult res = null;

            try
            {
                res = searcher.FindOne();
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80072030))
                {
                    // object is not found since we cannot even find the container in which to search
                    throw new ActiveDirectoryObjectNotFoundException(SR.AppNCNotFound, typeof(ApplicationPartition), distinguishedName);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            finally
            {
                partitionsEntry.Dispose();
            }

            if (res == null)
            {
                // the specified application partition could not be found in the given forest
                throw new ActiveDirectoryObjectNotFoundException(SR.AppNCNotFound, typeof(ApplicationPartition), distinguishedName);
            }

            string appNCDnsName = null;
            try
            {
                appNCDnsName = (res.Properties[PropertyManager.DnsRoot].Count > 0) ? (string)res.Properties[PropertyManager.DnsRoot][0] : null;
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            // verify that if the target is a server, then this partition is a naming context on it
            ApplicationPartitionType appType = GetApplicationPartitionType(context);
            if (context.ContextType == DirectoryContextType.DirectoryServer)
            {
                bool hostsCurrentPartition = false;
                DistinguishedName appNCDN = new DistinguishedName(distinguishedName);
                DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);

                try
                {
                    foreach (string namingContext in rootDSE.Properties[PropertyManager.NamingContexts])
                    {
                        DistinguishedName dn = new DistinguishedName(namingContext);

                        if (dn.Equals(appNCDN))
                        {
                            hostsCurrentPartition = true;
                            break;
                        }
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
                finally
                {
                    rootDSE.Dispose();
                }

                if (!hostsCurrentPartition)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.AppNCNotFound, typeof(ApplicationPartition), distinguishedName);
                }

                appNCContext = context;
            }
            else
            {
                // we need to find a server which hosts this application partition
                if (appType == ApplicationPartitionType.ADApplicationPartition)
                {
                    int errorCode = 0;
                    DomainControllerInfo domainControllerInfo;

                    errorCode = Locator.DsGetDcNameWrapper(null, appNCDnsName, null, (long)PrivateLocatorFlags.OnlyLDAPNeeded, out domainControllerInfo);

                    if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.AppNCNotFound, typeof(ApplicationPartition), distinguishedName);
                    }
                    else if (errorCode != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                    }

                    Debug.Assert(domainControllerInfo.DomainControllerName.Length > 2, "ApplicationPartition:FindByName - domainControllerInfo.DomainControllerName.Length <= 2");
                    string serverName = domainControllerInfo.DomainControllerName.Substring(2);
                    appNCContext = Utils.GetNewDirectoryContext(serverName, DirectoryContextType.DirectoryServer, context);
                }
                else
                {
                    // this will find an adam instance that hosts this partition and which is alive and responding.
                    string adamInstName = ConfigurationSet.FindOneAdamInstance(context.Name, context, distinguishedName, null).Name;
                    appNCContext = Utils.GetNewDirectoryContext(adamInstName, DirectoryContextType.DirectoryServer, context);
                }
            }
            partition = new ApplicationPartition(appNCContext, (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.NCName), appNCDnsName, appType, directoryEntryMgr);

            return partition;
        }

        public DirectoryServer FindDirectoryServer()
        {
            DirectoryServer directoryServer = null;

            CheckIfDisposed();

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                // AD
                directoryServer = FindDirectoryServerInternal(null, false);
            }
            else
            {
                // Check that the application partition has been committed
                if (!_committed)
                {
                    throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
                }
                directoryServer = ConfigurationSet.FindOneAdamInstance(context, Name, null);
            }
            return directoryServer;
        }

        public DirectoryServer FindDirectoryServer(string siteName)
        {
            DirectoryServer directoryServer = null;

            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException("siteName");
            }

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                // AD
                directoryServer = FindDirectoryServerInternal(siteName, false);
            }
            else
            {
                // Check that the application partition has been committed
                if (!_committed)
                {
                    throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
                }

                directoryServer = ConfigurationSet.FindOneAdamInstance(context, Name, siteName);
            }
            return directoryServer;
        }

        public DirectoryServer FindDirectoryServer(bool forceRediscovery)
        {
            DirectoryServer directoryServer = null;

            CheckIfDisposed();

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                // AD
                directoryServer = FindDirectoryServerInternal(null, forceRediscovery);
            }
            else
            {
                // Check that the application partition has been committed
                if (!_committed)
                {
                    throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
                }

                // forceRediscovery is ignored for ADAM Application partition
                directoryServer = ConfigurationSet.FindOneAdamInstance(context, Name, null);
            }
            return directoryServer;
        }

        public DirectoryServer FindDirectoryServer(string siteName, bool forceRediscovery)
        {
            DirectoryServer directoryServer = null;

            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException("siteName");
            }

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                // AD
                directoryServer = FindDirectoryServerInternal(siteName, forceRediscovery);
            }
            else
            {
                // Check that the application partition has been committed
                if (!_committed)
                {
                    throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
                }

                // forceRediscovery is ignored for ADAM Application partition
                directoryServer = ConfigurationSet.FindOneAdamInstance(context, Name, siteName);
            }
            return directoryServer;
        }

        public ReadOnlyDirectoryServerCollection FindAllDirectoryServers()
        {
            CheckIfDisposed();

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                // AD
                return FindAllDirectoryServersInternal(null);
            }
            else
            {
                // Check that the application partition has been committed
                if (!_committed)
                {
                    throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
                }

                ReadOnlyDirectoryServerCollection directoryServers = new ReadOnlyDirectoryServerCollection();
                directoryServers.AddRange(ConfigurationSet.FindAdamInstances(context, Name, null));
                return directoryServers;
            }
        }

        public ReadOnlyDirectoryServerCollection FindAllDirectoryServers(string siteName)
        {
            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException("siteName");
            }

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                // AD
                return FindAllDirectoryServersInternal(siteName);
            }
            else
            {
                // Check that the application partition has been committed
                if (!_committed)
                {
                    throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
                }

                ReadOnlyDirectoryServerCollection directoryServers = new ReadOnlyDirectoryServerCollection();
                directoryServers.AddRange(ConfigurationSet.FindAdamInstances(context, Name, siteName));
                return directoryServers;
            }
        }

        public ReadOnlyDirectoryServerCollection FindAllDiscoverableDirectoryServers()
        {
            CheckIfDisposed();

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                // AD
                return FindAllDiscoverableDirectoryServersInternal(null);
            }
            else
            {
                //
                // throw exception for ADAM
                //
                throw new NotSupportedException(SR.OperationInvalidForADAM);
            }
        }

        public ReadOnlyDirectoryServerCollection FindAllDiscoverableDirectoryServers(string siteName)
        {
            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException("siteName");
            }

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                // AD
                return FindAllDiscoverableDirectoryServersInternal(siteName);
            }
            else
            {
                //
                // throw exception for ADAM
                //
                throw new NotSupportedException(SR.OperationInvalidForADAM);
            }
        }

        public void Delete()
        {
            CheckIfDisposed();

            // Check that the application partition has been committed
            if (!_committed)
            {
                throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
            }

            // Get the partitions container and delete the crossRef entry for this 
            // application partition
            DirectoryEntry partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));
            try
            {
                GetCrossRefEntry();
                partitionsEntry.Children.Remove(_crossRefEntry);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                partitionsEntry.Dispose();
            }
        }

        public void Save()
        {
            CheckIfDisposed();

            if (!_committed)
            {
                bool createManually = false;

                if (_appType == ApplicationPartitionType.ADApplicationPartition)
                {
                    try
                    {
                        _domainDNSEntry.CommitChanges();
                    }
                    catch (COMException e)
                    {
                        if (e.ErrorCode == unchecked((int)0x80072029))
                        {
                            // inappropriate authentication (we might have fallen back to NTLM)
                            createManually = true;
                        }
                        else
                        {
                            throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                        }
                    }
                }
                else
                {
                    // for ADAM we always create the crossRef manually before creating the domainDNS object
                    createManually = true;
                }

                if (createManually)
                {
                    // we need to first save the cross ref entry
                    try
                    {
                        InitializeCrossRef(partitionName);
                        _crossRefEntry.CommitChanges();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    try
                    {
                        _domainDNSEntry.CommitChanges();
                    }
                    catch (COMException e)
                    {
                        //
                        //delete the crossRef entry
                        //
                        DirectoryEntry partitionsEntry = _crossRefEntry.Parent;
                        try
                        {
                            partitionsEntry.Children.Remove(_crossRefEntry);
                        }
                        catch (COMException e2)
                        {
                            throw ExceptionHelper.GetExceptionFromCOMException(e2);
                        }

                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    // if the crossRef is created manually we need to refresh the cross ref entry to get the changes that were made 
                    // due to the creation of the partition
                    try
                    {
                        _crossRefEntry.RefreshCache();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }

                // When we create a domainDNS object on DC1 (Naming Master = DC2),
                // then internally DC1 will contact DC2 to create the disabled crossRef object.
                // DC2 will force replicate the crossRef object to DC1. DC1 will then create
                // the domainDNS object and enable the crossRef on DC1 (not DC2). 
                // Here we need to force replicate the enabling of the crossRef to the FSMO (DC2)
                // so that we can later add replicas (which need to modify an attribute on the crossRef
                // on DC2, the FSMO, and can only be done if the crossRef on DC2 is enabled)
                // get the ntdsa name of the server on which the partition is created
                DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                string primaryServerNtdsaName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DsServiceName);

                // get the DN of the crossRef entry that needs to be replicated to the fsmo role
                if (_appType == ApplicationPartitionType.ADApplicationPartition)
                {
                    // for AD we may not have the crossRef entry yet
                    GetCrossRefEntry();
                }
                string crossRefDN = (string)PropertyManager.GetPropertyValue(context, _crossRefEntry, PropertyManager.DistinguishedName);

                // Now set the operational attribute "replicateSingleObject" on the Rootdse of the fsmo role
                // to <ntdsa name of the source>:<DN of the crossRef object which needs to be replicated>
                DirectoryContext fsmoContext = Utils.GetNewDirectoryContext(GetNamingRoleOwner(), DirectoryContextType.DirectoryServer, context);
                DirectoryEntry fsmoRootDSE = DirectoryEntryManager.GetDirectoryEntry(fsmoContext, WellKnownDN.RootDSE);

                try
                {
                    fsmoRootDSE.Properties[PropertyManager.ReplicateSingleObject].Value = primaryServerNtdsaName + ":" + crossRefDN;
                    fsmoRootDSE.CommitChanges();
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
                finally
                {
                    fsmoRootDSE.Dispose();
                }

                // the partition has been created
                _committed = true;

                // commit the replica locations information or security reference domain if applicable
                if ((_cachedDirectoryServers != null) || (_securityRefDomainModified))
                {
                    if (_cachedDirectoryServers != null)
                    {
                        _crossRefEntry.Properties[PropertyManager.MsDSNCReplicaLocations].AddRange(_cachedDirectoryServers.GetMultiValuedProperty());
                    }
                    if (_securityRefDomainModified)
                    {
                        _crossRefEntry.Properties[PropertyManager.MsDSSDReferenceDomain].Value = _securityRefDomain;
                    }
                    try
                    {
                        _crossRefEntry.CommitChanges();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
            }
            else
            {
                // just save the crossRef entry for teh directory servers and the 
                // security reference domain information
                if ((_cachedDirectoryServers != null) || (_securityRefDomainModified))
                {
                    try
                    {
                        // we should already have the crossRef entries as some attribute on it has already 
                        // been modified
                        Debug.Assert(_crossRefEntry != null, "ApplicationPartition::Save - crossRefEntry on already committed partition which is being modified is null.");
                        _crossRefEntry.CommitChanges();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
            }

            // invalidate cached info
            _cachedDirectoryServers = null;
            _securityRefDomainModified = false;
        }

        public override DirectoryEntry GetDirectoryEntry()
        {
            CheckIfDisposed();

            if (!_committed)
            {
                throw new InvalidOperationException(SR.CannotGetObject);
            }

            return DirectoryEntryManager.GetDirectoryEntry(context, Name);
        }

        #endregion public methods

        #region public properties

        public DirectoryServerCollection DirectoryServers
        {
            get
            {
                CheckIfDisposed();
                if (_cachedDirectoryServers == null)
                {
                    ReadOnlyDirectoryServerCollection servers = (_committed) ? FindAllDirectoryServers() : new ReadOnlyDirectoryServerCollection();
                    bool isADAM = (_appType == ApplicationPartitionType.ADAMApplicationPartition) ? true : false;

                    // Get the cross ref entry if we don't already have it
                    if (_committed)
                    {
                        GetCrossRefEntry();
                    }
                    // 
                    // If the application partition is already committed at this point, we pass in the directory entry for teh crossRef, so any modifications
                    // are made directly on the crossRef entry. If at this point we do not have a crossRefEntry, we pass in null, while saving, we get the information 
                    // from the collection and set it on the appropriate attribute on the crossRef directory entry.
                    //
                    //
                    _cachedDirectoryServers = new DirectoryServerCollection(context, (_committed) ? _crossRefEntry : null, isADAM, servers);
                }
                return _cachedDirectoryServers;
            }
        }

        public string SecurityReferenceDomain
        {
            get
            {
                CheckIfDisposed();

                if (_appType == ApplicationPartitionType.ADAMApplicationPartition)
                {
                    throw new NotSupportedException(SR.PropertyInvalidForADAM);
                }

                if (_committed)
                {
                    GetCrossRefEntry();

                    try
                    {
                        if (_crossRefEntry.Properties[PropertyManager.MsDSSDReferenceDomain].Count > 0)
                        {
                            return (string)_crossRefEntry.Properties[PropertyManager.MsDSSDReferenceDomain].Value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                {
                    return _securityRefDomain;
                }
            }
            set
            {
                CheckIfDisposed();

                if (_appType == ApplicationPartitionType.ADAMApplicationPartition)
                {
                    throw new NotSupportedException(SR.PropertyInvalidForADAM);
                }

                if (_committed)
                {
                    GetCrossRefEntry();
                    // modify the security reference domain 
                    // this will get committed when the crossRefEntry is committed
                    if (value == null)
                    {
                        if (_crossRefEntry.Properties.Contains(PropertyManager.MsDSSDReferenceDomain))
                        {
                            _crossRefEntry.Properties[PropertyManager.MsDSSDReferenceDomain].Clear();
                            _securityRefDomainModified = true;
                        }
                    }
                    else
                    {
                        _crossRefEntry.Properties[PropertyManager.MsDSSDReferenceDomain].Value = value;
                        _securityRefDomainModified = true;
                    }
                }
                else
                {
                    if (!((_securityRefDomain == null) && (value == null)))
                    {
                        _securityRefDomain = value;
                        _securityRefDomainModified = true;
                    }
                }
            }
        }
        #endregion public properties

        #region private methods
        private void ValidateApplicationPartitionParameters(DirectoryContext context, string distinguishedName, string objectClass, bool objectClassSpecified)
        {
            // validate context 
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            // contexttype should be DirectoryServer
            if ((context.Name == null) || (!context.isServer()))
            {
                throw new ArgumentException(SR.TargetShouldBeServer, "context");
            }

            // check that the distinguished name is not null or empty
            if (distinguishedName == null)
            {
                throw new ArgumentNullException("distinguishedName");
            }

            if (distinguishedName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, "distinguishedName");
            }

            // initialize private variables
            this.context = new DirectoryContext(context);
            this.directoryEntryMgr = new DirectoryEntryManager(this.context);

            // validate the distinguished name
            // Utils.GetDnsNameFromDN will throw an ArgumentException if the dn is not valid (cannot be syntactically converted to dns name)
            _dnsName = Utils.GetDnsNameFromDN(distinguishedName);
            this.partitionName = distinguishedName;

            //
            // if the partition being created is a one-level partition, we do not support it
            //
            Component[] components = Utils.GetDNComponents(distinguishedName);
            if (components.Length == 1)
            {
                throw new NotSupportedException(SR.OneLevelPartitionNotSupported);
            }

            // check if the object class can be specified
            _appType = GetApplicationPartitionType(this.context);
            if ((_appType == ApplicationPartitionType.ADApplicationPartition) && (objectClassSpecified))
            {
                throw new InvalidOperationException(SR.NoObjectClassForADPartition);
            }
            else if (objectClassSpecified)
            {
                // ADAM case and objectClass is explicitly specified, so must be validated

                if (objectClass == null)
                {
                    throw new ArgumentNullException("objectClass");
                }

                if (objectClass.Length == 0)
                {
                    throw new ArgumentException(SR.EmptyStringParameter, "objectClass");
                }
            }

            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                //
                // the target in the directory context could be the netbios name of the server, so we will get the dns name
                // (since application partition creation will fail if dns name is not specified)
                //

                string serverDnsName = null;
                try
                {
                    DirectoryEntry rootDSEEntry = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                    serverDnsName = (string)PropertyManager.GetPropertyValue(this.context, rootDSEEntry, PropertyManager.DnsHostName);
                }
                catch (COMException e)
                {
                    ExceptionHelper.GetExceptionFromCOMException(this.context, e);
                }

                this.context = Utils.GetNewDirectoryContext(serverDnsName, DirectoryContextType.DirectoryServer, context);
            }
        }

        private void CreateApplicationPartition(string distinguishedName, string objectClass)
        {
            if (_appType == ApplicationPartitionType.ADApplicationPartition)
            {
                //
                // AD
                // 1. Bind to the non-existent application partition using the fast bind and delegation option
                // 2. Get the Parent object and create a new "domainDNS" object under it
                // 3. Set the instanceType and the description for the application partitin object
                //

                DirectoryEntry tempEntry = null;
                DirectoryEntry parent = null;

                try
                {
                    AuthenticationTypes authType = Utils.DefaultAuthType | AuthenticationTypes.FastBind | AuthenticationTypes.Delegation;


                    authType |= AuthenticationTypes.ServerBind;

                    tempEntry = new DirectoryEntry("LDAP://" + context.GetServerName() + "/" + distinguishedName, context.UserName, context.Password, authType);
                    parent = tempEntry.Parent;
                    _domainDNSEntry = parent.Children.Add(Utils.GetRdnFromDN(distinguishedName), PropertyManager.DomainDNS);
                    // set the instance type to 5
                    _domainDNSEntry.Properties[PropertyManager.InstanceType].Value = NCFlags.InstanceTypeIsNCHead | NCFlags.InstanceTypeIsWriteable;
                    // mark this as uncommitted
                    _committed = false;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
                finally
                {
                    // dispose all resources
                    if (parent != null)
                    {
                        parent.Dispose();
                    }

                    if (tempEntry != null)
                    {
                        tempEntry.Dispose();
                    }
                }
            }
            else
            {
                //
                // ADAM
                // 1. Bind to the partitions container on the domain naming owner instance
                // 2. Create a disabled crossRef object for the new application partition
                // 3. Bind to the target hint and follow the same steps as for AD
                //

                try
                {
                    InitializeCrossRef(distinguishedName);

                    DirectoryEntry tempEntry = null;
                    DirectoryEntry parent = null;

                    try
                    {
                        AuthenticationTypes authType = Utils.DefaultAuthType | AuthenticationTypes.FastBind;


                        authType |= AuthenticationTypes.ServerBind;
                        
                        tempEntry = new DirectoryEntry("LDAP://" + context.Name + "/" + distinguishedName, context.UserName, context.Password, authType);
                        parent = tempEntry.Parent;
                        _domainDNSEntry = parent.Children.Add(Utils.GetRdnFromDN(distinguishedName), objectClass);

                        // set the instance type to 5
                        _domainDNSEntry.Properties[PropertyManager.InstanceType].Value = NCFlags.InstanceTypeIsNCHead | NCFlags.InstanceTypeIsWriteable;

                        // mark this as uncommitted
                        _committed = false;
                    }
                    finally
                    {
                        // dispose all resources
                        if (parent != null)
                        {
                            parent.Dispose();
                        }

                        if (tempEntry != null)
                        {
                            tempEntry.Dispose();
                        }
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        private void InitializeCrossRef(string distinguishedName)
        {
            if (_crossRefEntry != null)
                // already initialized
                return;

            DirectoryEntry partitionsEntry = null;

            try
            {
                string namingFsmoName = GetNamingRoleOwner();
                DirectoryContext roleOwnerContext = Utils.GetNewDirectoryContext(namingFsmoName, DirectoryContextType.DirectoryServer, context);
                partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(roleOwnerContext, WellKnownDN.PartitionsContainer);
                string uniqueName = "CN={" + Guid.NewGuid() + "}";
                _crossRefEntry = partitionsEntry.Children.Add(uniqueName, "crossRef");

                string dnsHostName = null;
                if (_appType == ApplicationPartitionType.ADAMApplicationPartition)
                {
                    // Bind to rootdse and get the server name
                    DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                    string ntdsaName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DsServiceName);
                    dnsHostName = Utils.GetAdamHostNameAndPortsFromNTDSA(context, ntdsaName);
                }
                else
                {
                    // for AD the name in the context will be the dns name of the server
                    dnsHostName = context.Name;
                }

                // create disabled cross ref object
                _crossRefEntry.Properties[PropertyManager.DnsRoot].Value = dnsHostName;
                _crossRefEntry.Properties[PropertyManager.Enabled].Value = false;
                _crossRefEntry.Properties[PropertyManager.NCName].Value = distinguishedName;
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                if (partitionsEntry != null)
                {
                    partitionsEntry.Dispose();
                }
            }
        }

        private static ApplicationPartitionType GetApplicationPartitionType(DirectoryContext context)
        {
            ApplicationPartitionType type = ApplicationPartitionType.Unknown;

            DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            try
            {
                foreach (string supportedCapability in rootDSE.Properties[PropertyManager.SupportedCapabilities])
                {
                    if (String.Compare(supportedCapability, SupportedCapability.ADOid, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        type = ApplicationPartitionType.ADApplicationPartition;
                    }
                    if (String.Compare(supportedCapability, SupportedCapability.ADAMOid, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        type = ApplicationPartitionType.ADAMApplicationPartition;
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                rootDSE.Dispose();
            }

            // should not happen
            if (type == ApplicationPartitionType.Unknown)
            {
                throw new ActiveDirectoryOperationException(SR.ApplicationPartitionTypeUnknown);
            }
            return type;
        }

        // we always get the crossEntry bound to the FSMO role
        // this is so that we do not encounter any replication delay related issues
        internal DirectoryEntry GetCrossRefEntry()
        {
            if (_crossRefEntry != null)
            {
                return _crossRefEntry;
            }

            DirectoryEntry partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));
            try
            {
                _crossRefEntry = Utils.GetCrossRefEntry(context, partitionsEntry, Name);
            }
            finally
            {
                partitionsEntry.Dispose();
            }
            return _crossRefEntry;
        }

        internal string GetNamingRoleOwner()
        {
            string namingFsmo = null;
            DirectoryEntry partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));
            try
            {
                if (_appType == ApplicationPartitionType.ADApplicationPartition)
                {
                    namingFsmo = Utils.GetDnsHostNameFromNTDSA(context, (string)PropertyManager.GetPropertyValue(context, partitionsEntry, PropertyManager.FsmoRoleOwner));
                }
                else
                {
                    namingFsmo = Utils.GetAdamDnsHostNameFromNTDSA(context, (string)PropertyManager.GetPropertyValue(context, partitionsEntry, PropertyManager.FsmoRoleOwner));
                }
            }
            finally
            {
                partitionsEntry.Dispose();
            }
            return namingFsmo;
        }

        private DirectoryServer FindDirectoryServerInternal(string siteName, bool forceRediscovery)
        {
            DirectoryServer directoryServer = null;
            LocatorOptions flag = 0;
            int errorCode = 0;
            DomainControllerInfo domainControllerInfo;

            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, "siteName");
            }

            // Check that the application partition has been committed
            if (!_committed)
            {
                throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
            }

            // set the force rediscovery flag if required
            if (forceRediscovery)
            {
                flag = LocatorOptions.ForceRediscovery;
            }

            // call DsGetDcName
            errorCode = Locator.DsGetDcNameWrapper(null, _dnsName, siteName, (long)flag | (long)PrivateLocatorFlags.OnlyLDAPNeeded, out domainControllerInfo);

            if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.ReplicaNotFound, typeof(DirectoryServer), null);
            }
            else if (errorCode != 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
            }

            Debug.Assert(domainControllerInfo.DomainControllerName.Length > 2, "ApplicationPartition:FindDirectoryServerInternal - domainControllerInfo.DomainControllerName.Length <= 2");
            string dcName = domainControllerInfo.DomainControllerName.Substring(2);

            // create a new context object for the domain controller passing on only the 
            // credentials from the forest context
            DirectoryContext dcContext = Utils.GetNewDirectoryContext(dcName, DirectoryContextType.DirectoryServer, context);

            directoryServer = new DomainController(dcContext, dcName);
            return directoryServer;
        }

        private ReadOnlyDirectoryServerCollection FindAllDirectoryServersInternal(string siteName)
        {
            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, "siteName");
            }

            // Check that the application partition has been committed
            if (!_committed)
            {
                throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
            }

            ArrayList dcList = new ArrayList();
            foreach (string dcName in Utils.GetReplicaList(context, Name, siteName, false /* isDefaultNC */, false /* isADAM */, false /* mustBeGC */))
            {
                DirectoryContext dcContext = Utils.GetNewDirectoryContext(dcName, DirectoryContextType.DirectoryServer, context);
                dcList.Add(new DomainController(dcContext, dcName));
            }

            return new ReadOnlyDirectoryServerCollection(dcList);
        }

        private ReadOnlyDirectoryServerCollection FindAllDiscoverableDirectoryServersInternal(string siteName)
        {
            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, "siteName");
            }

            // Check that the application partition has been committed
            if (!_committed)
            {
                throw new InvalidOperationException(SR.CannotPerformOperationOnUncommittedObject);
            }

            long flag = (long)PrivateLocatorFlags.OnlyLDAPNeeded;

            return new ReadOnlyDirectoryServerCollection(Locator.EnumerateDomainControllers(context, _dnsName, siteName, flag));
        }

        #endregion private methods
    }
}
