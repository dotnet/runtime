// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices.ActiveDirectory
{
    [Flags]
    public enum ActiveDirectorySiteOptions
    {
        None = 0,
        AutoTopologyDisabled = 1,
        TopologyCleanupDisabled = 2,
        AutoMinimumHopDisabled = 4,
        StaleServerDetectDisabled = 8,
        AutoInterSiteTopologyDisabled = 16,
        GroupMembershipCachingEnabled = 32,
        ForceKccWindows2003Behavior = 64,
        UseWindows2000IstgElection = 128,
        RandomBridgeHeaderServerSelectionDisabled = 256,
        UseHashingForReplicationSchedule = 512,
        RedundantServerTopologyEnabled = 1024
    }

    public class ActiveDirectorySite : IDisposable
    {
        internal readonly DirectoryContext context;
        private readonly string _name;
        internal readonly DirectoryEntry cachedEntry;
        private DirectoryEntry? _ntdsEntry;
        private readonly ActiveDirectorySubnetCollection _subnets;
        private DirectoryServer? _topologyGenerator;
        private readonly ReadOnlySiteCollection _adjacentSites = new ReadOnlySiteCollection();
        private bool _disposed;
        private readonly DomainCollection _domains = new DomainCollection(null);
        private readonly ReadOnlyDirectoryServerCollection _servers = new ReadOnlyDirectoryServerCollection();
        private readonly ReadOnlySiteLinkCollection _links = new ReadOnlySiteLinkCollection();
        private ActiveDirectorySiteOptions _siteOptions = ActiveDirectorySiteOptions.None;
        private ReadOnlyDirectoryServerCollection _bridgeheadServers = new ReadOnlyDirectoryServerCollection();
        private readonly DirectoryServerCollection _SMTPBridgeheadServers;
        private readonly DirectoryServerCollection _RPCBridgeheadServers;
        private byte[]? _replicationSchedule;

        internal bool existing;
        private bool _subnetRetrieved;
        private bool _isADAMServer;
        private bool _topologyTouched;
        private bool _adjacentSitesRetrieved;
        private readonly string _siteDN;
        private bool _domainsRetrieved;
        private bool _serversRetrieved;
        private bool _belongLinksRetrieved;
        private bool _bridgeheadServerRetrieved;
        private bool _SMTPBridgeRetrieved;
        private bool _RPCBridgeRetrieved;

        public static ActiveDirectorySite FindByName(DirectoryContext context, string siteName)
        {
            // find an existing site
            ValidateArgument(context, siteName);

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de;
            string sitedn;
            try
            {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                sitedn = "CN=Sites," + (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;
                de = DirectoryEntryManager.GetDirectoryEntry(context, sitedn);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet, context.Name));
            }

            try
            {
                ADSearcher adSearcher = new ADSearcher(de,
                                                      "(&(objectClass=site)(objectCategory=site)(name=" + Utils.GetEscapedFilterValue(siteName) + "))",
                                                      s_distinguishedName,
                                                      SearchScope.OneLevel,
                                                      false, /* don't need paged search */
                                                      false /* don't need to cache result */);
                SearchResult? srchResult = adSearcher.FindOne();
                if (srchResult == null)
                {
                    // no such site object
                    throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySite), siteName);
                }
                // it is an existing site object
                ActiveDirectorySite site = new ActiveDirectorySite(context, siteName, true);
                return site;
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80072030))
                {
                    // object is not found since we cannot even find the container in which to search
                    throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySite), siteName);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            finally
            {
                de.Dispose();
            }
        }

        public ActiveDirectorySite(DirectoryContext context, string siteName)
        {
            ValidateArgument(context, siteName);

            //  work with copy of the context
            context = new DirectoryContext(context);

            this.context = context;
            _name = siteName;

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry? de = null;
            try
            {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;
                _siteDN = "CN=Sites," + config;
                // bind to the site container
                de = DirectoryEntryManager.GetDirectoryEntry(context, _siteDN);

                string rdn = "cn=" + _name;
                rdn = Utils.GetEscapedPath(rdn);
                cachedEntry = de.Children.Add(rdn, "site");
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet, context.Name));
            }
            finally
            {
                de?.Dispose();
            }

            _subnets = new ActiveDirectorySubnetCollection(context, "CN=" + siteName + "," + _siteDN);
            string transportDN = "CN=IP,CN=Inter-Site Transports," + _siteDN;
            _RPCBridgeheadServers = new DirectoryServerCollection(context, "CN=" + siteName + "," + _siteDN, transportDN);
            transportDN = "CN=SMTP,CN=Inter-Site Transports," + _siteDN;
            _SMTPBridgeheadServers = new DirectoryServerCollection(context, "CN=" + siteName + "," + _siteDN, transportDN);
        }

        internal ActiveDirectorySite(DirectoryContext context, string siteName, bool existing)
        {
            Debug.Assert(existing);

            this.context = context;
            _name = siteName;
            this.existing = existing;

            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            _siteDN = "CN=Sites," + (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;

            cachedEntry = DirectoryEntryManager.GetDirectoryEntry(context, "CN=" + siteName + "," + _siteDN);
            _subnets = new ActiveDirectorySubnetCollection(context, "CN=" + siteName + "," + _siteDN);

            string transportDN = "CN=IP,CN=Inter-Site Transports," + _siteDN;
            _RPCBridgeheadServers = new DirectoryServerCollection(context, (string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)!, transportDN);
            transportDN = "CN=SMTP,CN=Inter-Site Transports," + _siteDN;
            _SMTPBridgeheadServers = new DirectoryServerCollection(context, (string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)!, transportDN);
        }

        public static ActiveDirectorySite GetComputerSite()
        {
            // make sure that this is the platform that we support
            new DirectoryContext(DirectoryContextType.Forest);

            IntPtr ptr = (IntPtr)0;

            int result = Interop.Netapi32.DsGetSiteName(null, ref ptr);
            if (result != 0)
            {
                // computer is not in a site
                if (result == Interop.Errors.ERROR_NO_SITENAME)
                    throw new ActiveDirectoryObjectNotFoundException(SR.NoCurrentSite, typeof(ActiveDirectorySite), null);
                else
                    throw ExceptionHelper.GetExceptionFromErrorCode(result);
            }
            else
            {
                try
                {
                    string siteName = Marshal.PtrToStringUni(ptr)!;
                    Debug.Assert(siteName != null);

                    // find the forest this machine belongs to
                    string? forestName = Locator.GetDomainControllerInfo(null, null, null, (long)PrivateLocatorFlags.DirectoryServicesRequired).DnsForestName;
                    DirectoryContext currentContext = Utils.GetNewDirectoryContext(forestName, DirectoryContextType.Forest, null);

                    // existing site
                    ActiveDirectorySite site = ActiveDirectorySite.FindByName(currentContext, siteName);
                    return site;
                }
                finally
                {
                    if (ptr != (IntPtr)0)
                        Marshal.FreeHGlobal(ptr);
                }
            }
        }

        public string Name
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _name;
            }
        }

        public DomainCollection Domains
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    if (!_domainsRetrieved)
                    {
                        // clear it first to be safe in case GetDomains fail in the middle and leave partial results there
                        _domains.Clear();
                        GetDomains();
                        _domainsRetrieved = true;
                    }
                }

                return _domains;
            }
        }

        public ActiveDirectorySubnetCollection Subnets
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    // if asked the first time, we need to properly construct the subnets collection
                    if (!_subnetRetrieved)
                    {
                        _subnets.initialized = false;
                        _subnets.Clear();
                        GetSubnets();
                        _subnetRetrieved = true;
                    }
                }
                _subnets.initialized = true;

                return _subnets;
            }
        }

        public ReadOnlyDirectoryServerCollection Servers
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    if (!_serversRetrieved)
                    {
                        _servers.Clear();
                        GetServers();
                        _serversRetrieved = true;
                    }
                }

                return _servers;
            }
        }

        public ReadOnlySiteCollection AdjacentSites
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    if (!_adjacentSitesRetrieved)
                    {
                        _adjacentSites.Clear();
                        GetAdjacentSites();
                        _adjacentSitesRetrieved = true;
                    }
                }
                return _adjacentSites;
            }
        }

        public ReadOnlySiteLinkCollection SiteLinks
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    if (!_belongLinksRetrieved)
                    {
                        _links.Clear();
                        GetLinks();
                        _belongLinksRetrieved = true;
                    }
                }
                return _links;
            }
        }

        [DisallowNull]
        public DirectoryServer? InterSiteTopologyGenerator
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    // have not load topology generator information from the directory and user has not set it yet
                    if (_topologyGenerator == null && !_topologyTouched)
                    {
                        bool ISTGExist;
                        try
                        {
                            ISTGExist = NTDSSiteEntry.Properties.Contains("interSiteTopologyGenerator");
                        }
                        catch (COMException e)
                        {
                            throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                        }

                        if (ISTGExist)
                        {
                            string serverDN = (string)PropertyManager.GetPropertyValue(context, NTDSSiteEntry, PropertyManager.InterSiteTopologyGenerator)!;
                            string? hostname = null;
                            DirectoryEntry tmp = DirectoryEntryManager.GetDirectoryEntry(context, serverDN);

                            try
                            {
                                hostname = (string)PropertyManager.GetPropertyValue(context, tmp.Parent, PropertyManager.DnsHostName)!;
                            }
                            catch (COMException e)
                            {
                                if (e.ErrorCode == unchecked((int)0x80072030))
                                {
                                    // indicates a demoted server
                                    return null;
                                }
                            }
                            if (IsADAM)
                            {
                                int port = (int)PropertyManager.GetPropertyValue(context, tmp, PropertyManager.MsDSPortLDAP)!;
                                string fullHostName = hostname!;
                                if (port != 389)
                                {
                                    fullHostName = hostname + ":" + port;
                                }
                                _topologyGenerator = new AdamInstance(Utils.GetNewDirectoryContext(fullHostName, DirectoryContextType.DirectoryServer, context), fullHostName);
                            }
                            else
                            {
                                _topologyGenerator = new DomainController(Utils.GetNewDirectoryContext(hostname, DirectoryContextType.DirectoryServer, context), hostname!);
                            }
                        }
                    }
                }

                return _topologyGenerator;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (existing)
                {
                    // for existing site, nTDSSiteSettings needs to exist
                    _ = NTDSSiteEntry;
                }

                _topologyTouched = true;
                _topologyGenerator = value;
            }
        }

        public ActiveDirectorySiteOptions Options
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    try
                    {
                        if (NTDSSiteEntry.Properties.Contains("options"))
                        {
                            return (ActiveDirectorySiteOptions)NTDSSiteEntry.Properties["options"][0]!;
                        }
                        else
                            return ActiveDirectorySiteOptions.None;
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                    return _siteOptions;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    try
                    {
                        NTDSSiteEntry.Properties["options"].Value = value;
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                    _siteOptions = value;
            }
        }

        public string? Location
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    if (cachedEntry.Properties.Contains("location"))
                    {
                        return (string)cachedEntry.Properties["location"][0]!;
                    }
                    else
                        return null;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    if (value == null)
                    {
                        if (cachedEntry.Properties.Contains("location"))
                            cachedEntry.Properties["location"].Clear();
                    }
                    else
                    {
                        cachedEntry.Properties["location"].Value = value;
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public ReadOnlyDirectoryServerCollection BridgeheadServers
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (!_bridgeheadServerRetrieved)
                {
                    _bridgeheadServers = GetBridgeheadServers();
                    _bridgeheadServerRetrieved = true;
                }

                return _bridgeheadServers;
            }
        }

        public DirectoryServerCollection PreferredSmtpBridgeheadServers
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    if (!_SMTPBridgeRetrieved)
                    {
                        _SMTPBridgeheadServers.initialized = false;
                        _SMTPBridgeheadServers.Clear();
                        GetPreferredBridgeheadServers(ActiveDirectoryTransportType.Smtp);
                        _SMTPBridgeRetrieved = true;
                    }
                }

                _SMTPBridgeheadServers.initialized = true;

                return _SMTPBridgeheadServers;
            }
        }

        public DirectoryServerCollection PreferredRpcBridgeheadServers
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    if (!_RPCBridgeRetrieved)
                    {
                        _RPCBridgeheadServers.initialized = false;
                        _RPCBridgeheadServers.Clear();
                        GetPreferredBridgeheadServers(ActiveDirectoryTransportType.Rpc);
                        _RPCBridgeRetrieved = true;
                    }
                }
                _RPCBridgeheadServers.initialized = true;

                return _RPCBridgeheadServers;
            }
        }

        public ActiveDirectorySchedule? IntraSiteReplicationSchedule
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                ActiveDirectorySchedule? schedule = null;

                if (existing)
                {
                    // if exists in the cache, return it, otherwise null is returned
                    try
                    {
                        if (NTDSSiteEntry.Properties.Contains("schedule"))
                        {
                            byte[] tmpSchedule = (byte[])NTDSSiteEntry.Properties["schedule"][0]!;
                            Debug.Assert(tmpSchedule != null && tmpSchedule.Length == 188);
                            schedule = new ActiveDirectorySchedule();
                            schedule.SetUnmanagedSchedule(tmpSchedule);
                        }
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                {
                    if (_replicationSchedule != null)
                    {
                        // newly created site, get the schedule if already has been set by the user
                        schedule = new ActiveDirectorySchedule();
                        schedule.SetUnmanagedSchedule(_replicationSchedule);
                    }
                }

                return schedule;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    try
                    {
                        if (value == null)
                        {
                            // clear it out if existing before
                            if (NTDSSiteEntry.Properties.Contains("schedule"))
                                NTDSSiteEntry.Properties["schedule"].Clear();
                        }
                        else
                            // replace with the new value
                            NTDSSiteEntry.Properties["schedule"].Value = value.GetUnmanagedSchedule();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                {
                    // clear out the schedule
                    if (value == null)
                        _replicationSchedule = null;
                    else
                    {
                        // replace with the new value
                        _replicationSchedule = value.GetUnmanagedSchedule();
                    }
                }
            }
        }

        private bool IsADAM
        {
            get
            {
                DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                PropertyValueCollection values;
                try
                {
                    values = de.Properties["supportedCapabilities"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (values.Contains(SupportedCapability.ADAMOid))
                    _isADAMServer = true;

                return _isADAMServer;
            }
        }

        private DirectoryEntry NTDSSiteEntry
        {
            get
            {
                if (_ntdsEntry == null)
                {
                    DirectoryEntry tmp = DirectoryEntryManager.GetDirectoryEntry(context, "CN=NTDS Site Settings," + (string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)!);
                    try
                    {
                        tmp.RefreshCache();
                    }
                    catch (COMException e)
                    {
                        if (e.ErrorCode == unchecked((int)0x80072030))
                        {
                            string message = SR.Format(SR.NTDSSiteSetting, _name);
                            throw new ActiveDirectoryOperationException(message, e, 0x2030);
                        }
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    _ntdsEntry = tmp;
                }

                return _ntdsEntry;
            }
        }

        internal static readonly string[] s_distinguishedName = new string[] { "distinguishedName" };
        private static readonly string[] s_propertiesToLoadArray = new string[] { "fromServer", "distinguishedName", "dNSHostName", "objectCategory" };
        private static readonly string[] s_cnLocation = new string[] { "cn", "location" };
        private static readonly string[] s_cnDistinguishedName = new string[] { "cn", "distinguishedName" };
        private static readonly string[] s_dNSHostName = new string[] { "dNSHostName" };
        private static readonly string[] s_fromServerDistinguishedName = new string[] { "fromServer", "distinguishedName" };
        private static readonly string[] s_dNSHostNameDistinguishedName = new string[] { "dNSHostName", "distinguishedName" };

        public void Save()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            try
            {
                // commit changes
                cachedEntry.CommitChanges();

                foreach (DictionaryEntry e in _subnets.changeList)
                {
                    try
                    {
                        ((DirectoryEntry)e.Value!).CommitChanges();
                    }
                    catch (COMException exception)
                    {
                        // there is a bug in ADSI that when targeting ADAM, permissive modify control is not used.
                        if (exception.ErrorCode != unchecked((int)0x8007200A))
                            throw ExceptionHelper.GetExceptionFromCOMException(exception);
                    }
                }

                // reset status variables
                _subnets.changeList.Clear();
                _subnetRetrieved = false;

                // need to throw better exception for ADAM since its SMTP transport is not available
                foreach (DictionaryEntry e in _SMTPBridgeheadServers.changeList!)
                {
                    try
                    {
                        ((DirectoryEntry)e.Value!).CommitChanges();
                    }
                    catch (COMException exception)
                    {
                        // SMTP transport is not supported on ADAM
                        if (IsADAM && (exception.ErrorCode == unchecked((int)0x8007202F)))
                            throw new NotSupportedException(SR.NotSupportTransportSMTP);

                        // there is a bug in ADSI that when targeting ADAM, permissive modify control is not used.
                        if (exception.ErrorCode != unchecked((int)0x8007200A))
                            throw ExceptionHelper.GetExceptionFromCOMException(exception);
                    }
                }

                _SMTPBridgeheadServers.changeList.Clear();
                _SMTPBridgeRetrieved = false;

                foreach (DictionaryEntry e in _RPCBridgeheadServers.changeList!)
                {
                    try
                    {
                        ((DirectoryEntry)e.Value!).CommitChanges();
                    }
                    catch (COMException exception)
                    {
                        // there is a bug in ADSI that when targeting ADAM, permissive modify control is not used.
                        if (exception.ErrorCode != unchecked((int)0x8007200A))
                            throw ExceptionHelper.GetExceptionFromCOMException(exception);
                    }
                }

                _RPCBridgeheadServers.changeList.Clear();
                _RPCBridgeRetrieved = false;

                if (existing)
                {
                    // topology generator is changed
                    if (_topologyTouched)
                    {
                        try
                        {
                            DirectoryServer server = InterSiteTopologyGenerator!;
                            string ntdsaName = (server is DomainController) ? ((DomainController)server).NtdsaObjectName : ((AdamInstance)server).NtdsaObjectName;
                            NTDSSiteEntry.Properties["interSiteTopologyGenerator"].Value = ntdsaName;
                        }
                        catch (COMException e)
                        {
                            throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                        }
                    }

                    NTDSSiteEntry.CommitChanges();
                    _topologyTouched = false;
                }
                else
                {
                    try
                    {
                        // create nTDSSiteSettings object
                        DirectoryEntry tmpEntry = cachedEntry.Children.Add("CN=NTDS Site Settings", "nTDSSiteSettings");
                        //set properties on the Site NTDS settings object
                        DirectoryServer? replica = InterSiteTopologyGenerator;
                        if (replica != null)
                        {
                            string ntdsaName = (replica is DomainController) ? ((DomainController)replica).NtdsaObjectName : ((AdamInstance)replica).NtdsaObjectName;
                            tmpEntry.Properties["interSiteTopologyGenerator"].Value = ntdsaName;
                        }
                        tmpEntry.Properties["options"].Value = _siteOptions;
                        if (_replicationSchedule != null)
                        {
                            tmpEntry.Properties["schedule"].Value = _replicationSchedule;
                        }

                        tmpEntry.CommitChanges();
                        // cached the entry
                        _ntdsEntry = tmpEntry;

                        // create servers contain object
                        tmpEntry = cachedEntry.Children.Add("CN=Servers", "serversContainer");
                        tmpEntry.CommitChanges();

                        if (!IsADAM)
                        {
                            // create the licensingSiteSettings object
                            tmpEntry = cachedEntry.Children.Add("CN=Licensing Site Settings", "licensingSiteSettings");
                            tmpEntry.CommitChanges();
                        }
                    }
                    finally
                    {
                        // entry is created on the backend store successfully
                        existing = true;
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        public void Delete()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!existing)
            {
                throw new InvalidOperationException(SR.CannotDelete);
            }
            else
            {
                try
                {
                    cachedEntry.DeleteTree();
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public override string ToString()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            return _name;
        }

        private ReadOnlyDirectoryServerCollection GetBridgeheadServers()
        {
            NativeComInterfaces.IAdsPathname pathCracker = (NativeComInterfaces.IAdsPathname)new NativeComInterfaces.Pathname();
            // need to turn off the escaping for name
            pathCracker.EscapedMode = NativeComInterfaces.ADS_ESCAPEDMODE_OFF_EX;

            ReadOnlyDirectoryServerCollection collection = new ReadOnlyDirectoryServerCollection();
            if (existing)
            {
                Hashtable bridgeHeadTable = new Hashtable();
                Hashtable nonBridgHeadTable = new Hashtable();
                Hashtable hostNameTable = new Hashtable();
                const string ocValue = "CN=Server";

                // get destination bridgehead servers

                // first go to the servers container under the current site and then do a search to get the all server objects.
                string serverContainer = "CN=Servers," + (string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)!;
                DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, serverContainer);

                try
                {
                    // go through connection objects and find out its fromServer property.
                    ADSearcher adSearcher = new ADSearcher(de,
                                                          "(|(objectCategory=server)(objectCategory=NTDSConnection))",
                                                          s_propertiesToLoadArray,
                                                          SearchScope.Subtree,
                                                          true, /* need paged search */
                                                          true /* need cached result as we need to go back to the first record */);
                    SearchResultCollection? conResults = null;
                    try
                    {
                        conResults = adSearcher.FindAll();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    try
                    {
                        // find out whether fromServer indicates replicating from a server in another site.
                        foreach (SearchResult r in conResults)
                        {
                            string objectCategoryValue = (string)PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.ObjectCategory)!;
                            if (Utils.Compare(objectCategoryValue, 0, ocValue.Length, ocValue, 0, ocValue.Length) == 0)
                            {
                                hostNameTable.Add((string)PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.DistinguishedName)!, (string)PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.DnsHostName)!);
                            }
                        }

                        foreach (SearchResult r in conResults)
                        {
                            string objectCategoryValue = (string)PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.ObjectCategory)!;
                            if (Utils.Compare(objectCategoryValue, 0, ocValue.Length, ocValue, 0, ocValue.Length) != 0)
                            {
                                string fromServer = (string)PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.FromServer)!;

                                // escaping manipulation
                                string fromSite = Utils.GetPartialDN(fromServer, 3);
                                pathCracker.Set(fromSite, NativeComInterfaces.ADS_SETTYPE_DN);
                                fromSite = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_LEAF);
                                Debug.Assert(fromSite != null && Utils.Compare(fromSite, 0, 3, "CN=", 0, 3) == 0);
                                fromSite = fromSite.Substring(3);

                                string serverObjectName = Utils.GetPartialDN((string)PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.DistinguishedName)!, 2);
                                // don't know whether it is a bridgehead server yet.
                                if (!bridgeHeadTable.Contains(serverObjectName))
                                {
                                    string hostName = (string)hostNameTable[serverObjectName]!;
                                    // add if not yet done
                                    if (!nonBridgHeadTable.Contains(serverObjectName))
                                        nonBridgHeadTable.Add(serverObjectName, hostName);

                                    // check whether from different site
                                    if (Utils.Compare((string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.Cn)!, fromSite) != 0)
                                    {
                                        // the server is a bridgehead server
                                        bridgeHeadTable.Add(serverObjectName, hostName);
                                        nonBridgHeadTable.Remove(serverObjectName);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        conResults.Dispose();
                    }
                }
                finally
                {
                    de.Dispose();
                }

                // get source bridgehead server
                if (nonBridgHeadTable.Count != 0)
                {
                    // go to sites container to get all the connecdtion object that replicates from servers in the current sites that have
                    // not been determined whether it is a bridgehead server or not.
                    DirectoryEntry serverEntry = DirectoryEntryManager.GetDirectoryEntry(context, _siteDN);
                    // constructing the filter
                    StringBuilder str = new StringBuilder(100);
                    if (nonBridgHeadTable.Count > 1)
                        str.Append("(|");
                    foreach (DictionaryEntry val in nonBridgHeadTable)
                    {
                        str.Append("(fromServer=");
                        str.Append("CN=NTDS Settings,");
                        str.Append(Utils.GetEscapedFilterValue((string)val.Key));
                        str.Append(')');
                    }
                    if (nonBridgHeadTable.Count > 1)
                        str.Append(')');
                    ADSearcher adSearcher = new ADSearcher(serverEntry,
                                                          "(&(objectClass=nTDSConnection)(objectCategory=NTDSConnection)" + str.ToString() + ")",
                                                          s_fromServerDistinguishedName,
                                                          SearchScope.Subtree);
                    SearchResultCollection? conResults = null;
                    try
                    {
                        conResults = adSearcher.FindAll();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    try
                    {
                        foreach (SearchResult r in conResults)
                        {
                            string fromServer = (string)PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.FromServer)!;
                            string serverObject = fromServer.Substring(17);

                            if (nonBridgHeadTable.Contains(serverObject))
                            {
                                string otherSite = Utils.GetPartialDN((string)PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.DistinguishedName)!, 4);
                                // escaping manipulation
                                pathCracker.Set(otherSite, NativeComInterfaces.ADS_SETTYPE_DN);
                                otherSite = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_LEAF);
                                Debug.Assert(otherSite != null && Utils.Compare(otherSite, 0, 3, "CN=", 0, 3) == 0);
                                otherSite = otherSite.Substring(3);

                                // check whether from different sites
                                if (Utils.Compare(otherSite, (string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.Cn)!) != 0)
                                {
                                    string val = (string)nonBridgHeadTable[serverObject]!;
                                    nonBridgHeadTable.Remove(serverObject);
                                    bridgeHeadTable.Add(serverObject, val);
                                }
                            }
                        }
                    }
                    finally
                    {
                        conResults.Dispose();
                        serverEntry.Dispose();
                    }
                }

                DirectoryEntry? ADAMEntry = null;
                foreach (DictionaryEntry e in bridgeHeadTable)
                {
                    DirectoryServer? replica = null;
                    string host = (string)e.Value!;
                    // construct directoryreplica
                    if (IsADAM)
                    {
                        ADAMEntry = DirectoryEntryManager.GetDirectoryEntry(context, "CN=NTDS Settings," + e.Key);
                        int port = (int)PropertyManager.GetPropertyValue(context, ADAMEntry, PropertyManager.MsDSPortLDAP)!;
                        string fullhost = host;
                        if (port != 389)
                        {
                            fullhost = host + ":" + port;
                        }
                        replica = new AdamInstance(Utils.GetNewDirectoryContext(fullhost, DirectoryContextType.DirectoryServer, context), fullhost);
                    }
                    else
                    {
                        replica = new DomainController(Utils.GetNewDirectoryContext(host, DirectoryContextType.DirectoryServer, context), host);
                    }

                    collection.Add(replica);
                }
            }

            return collection;
        }

        public DirectoryEntry GetDirectoryEntry()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!existing)
            {
                throw new InvalidOperationException(SR.CannotGetObject);
            }
            else
            {
                return DirectoryEntryManager.GetDirectoryEntryInternal(context, cachedEntry.Path);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free other state (managed objects)
                cachedEntry?.Dispose();
                _ntdsEntry?.Dispose();
            }

            // free your own state (unmanaged objects)

            _disposed = true;
        }

        private static void ValidateArgument(DirectoryContext context, string siteName)
        {
            ArgumentNullException.ThrowIfNull(context);

            // if target is not specified, then we determin the target from the logon credential, so if it is a local user context, it should fail
            if ((context.Name == null) && (!context.isRootDomain()))
            {
                throw new ArgumentException(SR.ContextNotAssociatedWithDomain, nameof(context));
            }

            // more validation for the context, if the target is not null, then it should be either forest name or server name
            if (context.Name != null)
            {
                if (!(context.isRootDomain() || context.isServer() || context.isADAMConfigSet()))
                    throw new ArgumentException(SR.NotADOrADAM, nameof(context));
            }

            if (siteName == null)
                throw new ArgumentNullException(nameof(siteName));

            if (siteName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
        }

        private void GetSubnets()
        {
            // performs a search to find out the subnets that belong to this site
            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;
            string subnetContainer = "CN=Subnets,CN=Sites," + config;
            de = DirectoryEntryManager.GetDirectoryEntry(context, subnetContainer);

            ADSearcher adSearcher = new ADSearcher(de,
                                                  "(&(objectClass=subnet)(objectCategory=subnet)(siteObject=" + Utils.GetEscapedFilterValue((string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)!) + "))",
                                                  s_cnLocation,
                                                  SearchScope.OneLevel
                                                  );
            SearchResultCollection? results = null;

            try
            {
                results = adSearcher.FindAll();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            try
            {
                string? subnetName = null;
                foreach (SearchResult result in results)
                {
                    subnetName = (string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn)!;
                    ActiveDirectorySubnet subnet = new ActiveDirectorySubnet(context, subnetName, null, true);
                    // set the cached entry
                    subnet.cachedEntry = result.GetDirectoryEntry();
                    // set the site info
                    subnet.Site = this;

                    _subnets.Add(subnet);
                }
            }
            finally
            {
                results.Dispose();
                de.Dispose();
            }
        }

        private void GetAdjacentSites()
        {
            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            string config = (string)de.Properties["configurationNamingContext"][0]!;
            string transportContainer = "CN=Inter-Site Transports,CN=Sites," + config;
            de = DirectoryEntryManager.GetDirectoryEntry(context, transportContainer);
            ADSearcher adSearcher = new ADSearcher(de,
                                                  "(&(objectClass=siteLink)(objectCategory=SiteLink)(siteList=" + Utils.GetEscapedFilterValue((string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)!) + "))",
                                                  s_cnDistinguishedName,
                                                  SearchScope.Subtree);
            SearchResultCollection? results = null;

            try
            {
                results = adSearcher.FindAll();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            try
            {
                ActiveDirectorySiteLink? link = null;

                foreach (SearchResult result in results)
                {
                    string dn = (string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.DistinguishedName)!;
                    string linkName = (string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn)!;
                    string transportName = (string)Utils.GetDNComponents(dn)[1].Value!;
                    ActiveDirectoryTransportType transportType;
                    if (string.Equals(transportName, "IP", StringComparison.OrdinalIgnoreCase))
                        transportType = ActiveDirectoryTransportType.Rpc;
                    else if (string.Equals(transportName, "SMTP", StringComparison.OrdinalIgnoreCase))
                        transportType = ActiveDirectoryTransportType.Smtp;
                    else
                    {
                        // should not happen
                        string message = SR.Format(SR.UnknownTransport, transportName);
                        throw new ActiveDirectoryOperationException(message);
                    }

                    try
                    {
                        link = new ActiveDirectorySiteLink(context, linkName, transportType, true, result.GetDirectoryEntry());
                        foreach (ActiveDirectorySite tmpSite in link.Sites)
                        {
                            // don't add itself
                            if (Utils.Compare(tmpSite.Name, Name) == 0)
                                continue;

                            if (!_adjacentSites.Contains(tmpSite))
                                _adjacentSites.Add(tmpSite);
                        }
                    }
                    finally
                    {
                        link!.Dispose();
                    }
                }
            }
            finally
            {
                results.Dispose();
                de.Dispose();
            }
        }

        private void GetLinks()
        {
            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;
            string transportContainer = "CN=Inter-Site Transports,CN=Sites," + config;
            de = DirectoryEntryManager.GetDirectoryEntry(context, transportContainer);
            ADSearcher adSearcher = new ADSearcher(de,
                                                  "(&(objectClass=siteLink)(objectCategory=SiteLink)(siteList=" + Utils.GetEscapedFilterValue((string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)!) + "))",
                                                  s_cnDistinguishedName,
                                                  SearchScope.Subtree);
            SearchResultCollection? results = null;

            try
            {
                results = adSearcher.FindAll();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            try
            {
                foreach (SearchResult result in results)
                {
                    // construct the sitelinks at the same time
                    DirectoryEntry connectionEntry = result.GetDirectoryEntry();
                    string cn = (string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn)!;
                    string transport = Utils.GetDNComponents((string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.DistinguishedName)!)[1].Value!;
                    ActiveDirectorySiteLink? link = null;
                    if (string.Equals(transport, "IP", StringComparison.OrdinalIgnoreCase))
                        link = new ActiveDirectorySiteLink(context, cn, ActiveDirectoryTransportType.Rpc, true, connectionEntry);
                    else if (string.Equals(transport, "SMTP", StringComparison.OrdinalIgnoreCase))
                        link = new ActiveDirectorySiteLink(context, cn, ActiveDirectoryTransportType.Smtp, true, connectionEntry);
                    else
                    {
                        // should not happen
                        string message = SR.Format(SR.UnknownTransport, transport);
                        throw new ActiveDirectoryOperationException(message);
                    }

                    _links.Add(link);
                }
            }
            finally
            {
                results.Dispose();
                de.Dispose();
            }
        }

        private unsafe void GetDomains()
        {
            // for ADAM, there is no concept of domain, we just return empty collection which is good enough
            if (!IsADAM)
            {
                string serverName = cachedEntry.Options!.GetCurrentServerName();
                DomainController dc = DomainController.GetDomainController(Utils.GetNewDirectoryContext(serverName, DirectoryContextType.DirectoryServer, context));
                IntPtr handle = dc.Handle;

                Debug.Assert(handle != (IntPtr)0);

                void* pDomains = null;
                // call DsReplicaSyncAllW
                var dsListDomainsInSiteW = (delegate* unmanaged<IntPtr, char*, void**, int>)global::Interop.Kernel32.GetProcAddress(DirectoryContext.ADHandle, "DsListDomainsInSiteW");
                if (dsListDomainsInSiteW == null)
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastPInvokeError());
                }

                fixed (char* distinguishedName = (string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)!)
                {
                    int result = dsListDomainsInSiteW(handle, distinguishedName, &pDomains);
                    if (result != 0)
                        throw ExceptionHelper.GetExceptionFromErrorCode(result, serverName);
                }

                try
                {
                    DS_NAME_RESULT names = new DS_NAME_RESULT();
                    Marshal.PtrToStructure((IntPtr)pDomains, names);
                    int count = names.cItems;
                    IntPtr val = names.rItems;
                    if (count > 0)
                    {
                        Debug.Assert(val != (IntPtr)0);
                        IntPtr tmpPtr = (IntPtr)0;
                        for (int i = 0; i < count; i++)
                        {
                            tmpPtr = IntPtr.Add(val, Marshal.SizeOf(typeof(DS_NAME_RESULT_ITEM)) * i);
                            DS_NAME_RESULT_ITEM nameResult = new DS_NAME_RESULT_ITEM();
                            Marshal.PtrToStructure(tmpPtr, nameResult);
                            if (nameResult.status == DS_NAME_ERROR.DS_NAME_NO_ERROR || nameResult.status == DS_NAME_ERROR.DS_NAME_ERROR_DOMAIN_ONLY)
                            {
                                string? domainName = Marshal.PtrToStringUni(nameResult.pName);
                                if (domainName != null && domainName.Length > 0)
                                {
                                    string d = Utils.GetDnsNameFromDN(domainName);
                                    Domain domain = new Domain(Utils.GetNewDirectoryContext(d, DirectoryContextType.Domain, context), d);
                                    _domains.Add(domain);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    // call DsFreeNameResultW
                    var dsFreeNameResultW = (delegate* unmanaged<void*, void>)global::Interop.Kernel32.GetProcAddress(DirectoryContext.ADHandle, "DsFreeNameResultW");
                    if (dsFreeNameResultW == null)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastPInvokeError());
                    }

                    dsFreeNameResultW(pDomains);
                }
            }
        }

        private void GetServers()
        {
            ADSearcher adSearcher = new ADSearcher(cachedEntry,
                                                  "(&(objectClass=server)(objectCategory=server))",
                                                  s_dNSHostName,
                                                  SearchScope.Subtree);
            SearchResultCollection? results = null;
            try
            {
                results = adSearcher.FindAll();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            try
            {
                foreach (SearchResult result in results)
                {
                    string hostName = (string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.DnsHostName)!;
                    DirectoryEntry de = result.GetDirectoryEntry();
                    DirectoryEntry? child = null;
                    DirectoryServer? replica = null;
                    // make sure that the server is not demoted
                    try
                    {
                        child = de.Children.Find("CN=NTDS Settings", "nTDSDSA");
                    }
                    catch (COMException e)
                    {
                        if (e.ErrorCode == unchecked((int)0x80072030))
                        {
                            continue;
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    if (IsADAM)
                    {
                        int port = (int)PropertyManager.GetPropertyValue(context, child, PropertyManager.MsDSPortLDAP)!;
                        string fullHostName = hostName;
                        if (port != 389)
                        {
                            fullHostName = hostName + ":" + port;
                        }
                        replica = new AdamInstance(Utils.GetNewDirectoryContext(fullHostName, DirectoryContextType.DirectoryServer, context), fullHostName);
                    }
                    else
                        replica = new DomainController(Utils.GetNewDirectoryContext(hostName, DirectoryContextType.DirectoryServer, context), hostName);

                    _servers.Add(replica);
                }
            }
            finally
            {
                results.Dispose();
            }
        }

        private void GetPreferredBridgeheadServers(ActiveDirectoryTransportType transport)
        {
            string serverContainerDN = "CN=Servers," + PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName);
            string? transportDN = null;
            if (transport == ActiveDirectoryTransportType.Smtp)
                transportDN = "CN=SMTP,CN=Inter-Site Transports," + _siteDN;
            else
                transportDN = "CN=IP,CN=Inter-Site Transports," + _siteDN;

            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, serverContainerDN);
            ADSearcher adSearcher = new ADSearcher(de,
                                                  "(&(objectClass=server)(objectCategory=Server)(bridgeheadTransportList=" + Utils.GetEscapedFilterValue(transportDN) + "))",
                                                  s_dNSHostNameDistinguishedName,
                                                  SearchScope.OneLevel);
            SearchResultCollection? results = null;

            try
            {
                results = adSearcher.FindAll();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            try
            {
                DirectoryEntry? ADAMEntry = null;
                foreach (SearchResult result in results)
                {
                    string hostName = (string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.DnsHostName)!;
                    DirectoryEntry resultEntry = result.GetDirectoryEntry();
                    DirectoryServer? replica = null;

                    try
                    {
                        ADAMEntry = resultEntry.Children.Find("CN=NTDS Settings", "nTDSDSA");
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    if (IsADAM)
                    {
                        int port = (int)PropertyManager.GetPropertyValue(context, ADAMEntry, PropertyManager.MsDSPortLDAP)!;
                        string fullHostName = hostName;
                        if (port != 389)
                        {
                            fullHostName = hostName + ":" + port;
                        }
                        replica = new AdamInstance(Utils.GetNewDirectoryContext(fullHostName, DirectoryContextType.DirectoryServer, context), fullHostName);
                    }
                    else
                        replica = new DomainController(Utils.GetNewDirectoryContext(hostName, DirectoryContextType.DirectoryServer, context), hostName);

                    if (transport == ActiveDirectoryTransportType.Smtp)
                        _SMTPBridgeheadServers.Add(replica);
                    else
                        _RPCBridgeheadServers.Add(replica);
                }
            }
            finally
            {
                de.Dispose();
                results.Dispose();
            }
        }
    }
}
