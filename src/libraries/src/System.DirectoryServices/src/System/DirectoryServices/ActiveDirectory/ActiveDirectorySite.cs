//------------------------------------------------------------------------------
// <copyright file="ActiveDirectorySite.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.DirectoryServices;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.ComponentModel;
    using System.Security.Permissions;

    [Flags]
    public enum ActiveDirectorySiteOptions {
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

    [DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)]
    public class ActiveDirectorySite :IDisposable{
        internal DirectoryContext context = null;
        private string name = null;
        internal DirectoryEntry cachedEntry = null;
        private DirectoryEntry ntdsEntry = null;
        private ActiveDirectorySubnetCollection subnets = null;        
        private DirectoryServer topologyGenerator = null;
        private ReadOnlySiteCollection adjacentSites = new ReadOnlySiteCollection();        
        private bool disposed = false;
        private DomainCollection domains = new DomainCollection(null);
        private ReadOnlyDirectoryServerCollection servers = new ReadOnlyDirectoryServerCollection();
        private ReadOnlySiteLinkCollection links = new ReadOnlySiteLinkCollection();
        private ActiveDirectorySiteOptions siteOptions = ActiveDirectorySiteOptions.None;
        private ReadOnlyDirectoryServerCollection bridgeheadServers = new ReadOnlyDirectoryServerCollection();
        private DirectoryServerCollection SMTPBridgeheadServers = null;
        private DirectoryServerCollection RPCBridgeheadServers = null;
        private byte[] replicationSchedule = null;

        internal bool existing = false;        
        private bool subnetRetrieved = false;
        private bool isADAMServer = false;
        private bool checkADAM = false;
        private bool topologyTouched = false;        
        private bool adjacentSitesRetrieved = false;
        private string siteDN = null;
        private bool domainsRetrieved = false;
        private bool serversRetrieved = false;
        private bool belongLinksRetrieved = false;
        private bool bridgeheadServerRetrieved = false;
        private bool SMTPBridgeRetrieved = false;
        private bool RPCBridgeRetrieved = false;

        static int ERROR_NO_SITENAME = 1919;
        
        public static ActiveDirectorySite FindByName(DirectoryContext context, string siteName)
        {  
            // find an existing site
            ValidateArgument(context, siteName);

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de;
            string sitedn;
            try {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);                  
                sitedn = "CN=Sites," + (string) PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext);                        
                de = DirectoryEntryManager.GetDirectoryEntry(context, sitedn);            
            }
            catch (COMException e) 
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            catch (ActiveDirectoryObjectNotFoundException) {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(Res.GetString(Res.ADAMInstanceNotFoundInConfigSet, context.Name));
            }
            
            try
            {
                ADSearcher adSearcher = new ADSearcher(de,
                                                      "(&(objectClass=site)(objectCategory=site)(name=" + Utils.GetEscapedFilterValue(siteName) + "))",
                                                      new string[] {"distinguishedName"},
                                                      SearchScope.OneLevel,
                                                      false, /* don't need paged search */
                                                      false /* don't need to cache result */); 
                SearchResult srchResult = adSearcher.FindOne();
                if(srchResult == null)
                {
                    // no such site object
                    throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySite), siteName);
                }
                // it is an existing site object
                ActiveDirectorySite site = new ActiveDirectorySite(context, siteName, true);                                        
                return site;  
            }
            catch (COMException e) 
            {
                if (e.ErrorCode == unchecked((int)  0x80072030)) {
                    // object is not found since we cannot even find the container in which to search
                    throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySite), siteName);
                }
                else {
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
            this.name = siteName;          

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de = null;
            try {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string) PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext);
                siteDN = "CN=Sites," + config;
                // bind to the site container
                de = DirectoryEntryManager.GetDirectoryEntry(context, siteDN);

                string rdn = "cn=" + name;
                rdn = Utils.GetEscapedPath(rdn);
                cachedEntry = de.Children.Add(rdn, "site");
            }
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            catch (ActiveDirectoryObjectNotFoundException) {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(Res.GetString(Res.ADAMInstanceNotFoundInConfigSet, context.Name));
            }
            finally
            {
                if (de != null) 
                    de.Dispose();
            }

            subnets = new ActiveDirectorySubnetCollection(context, "CN=" + siteName + "," + siteDN);
            string transportDN = "CN=IP,CN=Inter-Site Transports," + siteDN;
            RPCBridgeheadServers = new DirectoryServerCollection(context, "CN=" + siteName + "," + siteDN, transportDN);
            transportDN = "CN=SMTP,CN=Inter-Site Transports," + siteDN;
            SMTPBridgeheadServers = new DirectoryServerCollection(context, "CN=" + siteName + "," + siteDN, transportDN);                                    
        }

        internal ActiveDirectorySite(DirectoryContext context, string siteName, bool existing)
        {
            Debug.Assert(existing == true);

            this.context = context;
            this.name = siteName;            
            this.existing = existing;     

            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            siteDN = "CN=Sites," + (string) PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext);            
            
            cachedEntry = DirectoryEntryManager.GetDirectoryEntry(context, "CN=" + siteName + "," + siteDN);                                             
            subnets = new ActiveDirectorySubnetCollection(context, "CN=" + siteName + "," + siteDN);

            string transportDN = "CN=IP,CN=Inter-Site Transports," + siteDN;
            RPCBridgeheadServers = new DirectoryServerCollection(context, (string) PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName), transportDN);
            transportDN = "CN=SMTP,CN=Inter-Site Transports," + siteDN;
            SMTPBridgeheadServers = new DirectoryServerCollection(context, (string) PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName), transportDN);                        
        }

        public static ActiveDirectorySite GetComputerSite() 
       {
                // make sure that this is the platform that we support
                new DirectoryContext(DirectoryContextType.Forest);
                
                IntPtr ptr = (IntPtr)0;                
                
                int result = UnsafeNativeMethods.DsGetSiteName(null, ref ptr);
                if(result != 0)
                {
                    // computer is not in a site
                    if(result == ERROR_NO_SITENAME)
                        throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.NoCurrentSite), typeof(ActiveDirectorySite), null);
                    else
                        throw ExceptionHelper.GetExceptionFromErrorCode(result);
                }
                else
                {
                    try
                    {
                        string siteName = Marshal.PtrToStringUni(ptr);
                        Debug.Assert(siteName != null);

                        // find the forest this machine belongs to
                        string forestName = Locator.GetDomainControllerInfo(null, null, null, (long) PrivateLocatorFlags.DirectoryServicesRequired).DnsForestName;
                        DirectoryContext currentContext = Utils.GetNewDirectoryContext(forestName, DirectoryContextType.Forest, null);                 
                        
                        // existing site
                        ActiveDirectorySite site = ActiveDirectorySite.FindByName(currentContext, siteName);                       
                        return site;
                                        
                    }
                    finally
                    {
                        if(ptr != (IntPtr)0)
                            Marshal.FreeHGlobal(ptr);
                    }
                }
        }       

        public string Name {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                return name;
            }            
        }

        public DomainCollection Domains {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if(existing)
                {
                    if(!domainsRetrieved)
                    {
                        // clear it first to be safe in case GetDomains fail in the middle and leave partial results there
                        domains.Clear();
                        GetDomains();
                        domainsRetrieved = true;
                    }
                }                

                return domains;
            }
        }

        public ActiveDirectorySubnetCollection Subnets {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(existing)
                {
                    // if asked the first time, we need to properly construct the subnets collection
                    if(!subnetRetrieved)
                    {
                        subnets.initialized = false;
                        subnets.Clear();
                        GetSubnets();                        
                        subnetRetrieved = true;
                    }                    
                }
                subnets.initialized = true;                
                
                return subnets;                
            }
        }    

        public ReadOnlyDirectoryServerCollection Servers {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if(existing)
                {
                    if(!serversRetrieved)
                    {
                        servers.Clear();
                        GetServers();
                        serversRetrieved = true;
                    }
                }

                return servers;
                
            }
        }

        public ReadOnlySiteCollection AdjacentSites {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(existing)
                {
                    if(!adjacentSitesRetrieved)
                    {
                        adjacentSites.Clear();
                        GetAdjacentSites();
                        adjacentSitesRetrieved = true;
                    }
                }
                return adjacentSites;
            }
        }

        public ReadOnlySiteLinkCollection SiteLinks {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(existing)
                {
                    if(!belongLinksRetrieved)
                    {
                        links.Clear();
                        GetLinks();
                        belongLinksRetrieved = true;
                    }
                }
                return links;
            }
        }

        public DirectoryServer InterSiteTopologyGenerator {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(existing)
                {
                    // have not load topology generator information from the directory and user has not set it yet
                    if(topologyGenerator == null && !topologyTouched)
                    {
                        bool ISTGExist;
                        try
                        {
                            ISTGExist = NTDSSiteEntry.Properties.Contains("interSiteTopologyGenerator");
                        }
                        catch(COMException e)
                        {
                            throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                        }

                    
                        if(ISTGExist)
                        {                            
                            string serverDN = (string) PropertyManager.GetPropertyValue(context, NTDSSiteEntry, PropertyManager.InterSiteTopologyGenerator);
                            string hostname = null;
                            DirectoryEntry tmp = DirectoryEntryManager.GetDirectoryEntry(context, serverDN);
                            
                            try
                            {                                
                                hostname = (string) PropertyManager.GetPropertyValue(context, tmp.Parent, PropertyManager.DnsHostName);
                            }
                            catch(COMException e)
                            {
                                if(e.ErrorCode == unchecked((int)0x80072030))
                                {
                                    // indicates a demoted server
                                    return null;
                                }
                            }         
                            if(IsADAM)
                            {                                
                                int port = (int) PropertyManager.GetPropertyValue(context, tmp, PropertyManager.MsDSPortLDAP);
                                string fullHostName = hostname;
                                if(port != 389)
                                {
                                    fullHostName = hostname + ":" + port;                                    
                                }
                                topologyGenerator = new AdamInstance(Utils.GetNewDirectoryContext(fullHostName, DirectoryContextType.DirectoryServer, context), fullHostName);
                            }
                            else
                            {
                                topologyGenerator = new DomainController(Utils.GetNewDirectoryContext(hostname, DirectoryContextType.DirectoryServer, context), hostname);
                            }
                        }
                        
                    }
                }

                return topologyGenerator;
                
            }
            set {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(value == null)
                    throw new ArgumentNullException("value");               

                if(existing)
                {
                    // for existing site, nTDSSiteSettings needs to exist
                    DirectoryEntry tmp = NTDSSiteEntry;
                }

                topologyTouched = true;
                topologyGenerator = value;
            }
        }       

        public ActiveDirectorySiteOptions Options {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(existing)
                {
                    try
                    {
                        if(NTDSSiteEntry.Properties.Contains("options"))
                        {
                            return (ActiveDirectorySiteOptions) NTDSSiteEntry.Properties["options"][0];
                        }
                        else
                            return ActiveDirectorySiteOptions.None;
                    }
                    catch(COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                    return siteOptions;
            }
            set {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(existing)
                {
                    try
                    {
                        NTDSSiteEntry.Properties["options"].Value = value;
                    }
                    catch(COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                    siteOptions = value;
                
            }
        }

        public string Location {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    if(cachedEntry.Properties.Contains("location"))
                    {
                        return (string) cachedEntry.Properties["location"][0];
                    }
                    else
                        return null;                
                }
                catch(COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            set {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                try
                {
                    if(value == null)
                    {
                        if(cachedEntry.Properties.Contains("location"))
                            cachedEntry.Properties["location"].Clear();
                    }
                    else
                    {         
                        cachedEntry.Properties["location"].Value = value;        
                    }
                }
                catch(COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
               
            }
        }

        public ReadOnlyDirectoryServerCollection BridgeheadServers {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(!bridgeheadServerRetrieved)
                {
                    bridgeheadServers = GetBridgeheadServers();
                    bridgeheadServerRetrieved = true;
                }

                return bridgeheadServers;
            }
        }

        public DirectoryServerCollection PreferredSmtpBridgeheadServers {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(existing)
                {
                    if(!SMTPBridgeRetrieved)
                    {
                        SMTPBridgeheadServers.initialized = false;
                        SMTPBridgeheadServers.Clear();
                        GetPreferredBridgeheadServers(ActiveDirectoryTransportType.Smtp);                        
                        SMTPBridgeRetrieved = true;
                    }
                }

                SMTPBridgeheadServers.initialized = true;

                return SMTPBridgeheadServers;
            }
        }

        public DirectoryServerCollection PreferredRpcBridgeheadServers {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                
                if(existing)
                {
                    if(!RPCBridgeRetrieved)
                    {
                        RPCBridgeheadServers.initialized = false;
                        RPCBridgeheadServers.Clear();
                        GetPreferredBridgeheadServers(ActiveDirectoryTransportType.Rpc);                        
                        RPCBridgeRetrieved = true;
                    }
                }
                RPCBridgeheadServers.initialized = true;
                
                return RPCBridgeheadServers;
            }
        }

        public ActiveDirectorySchedule IntraSiteReplicationSchedule {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                ActiveDirectorySchedule schedule = null;
                
                if(existing) 
                {
                    // if exists in the cache, return it, otherwise null is returned
                    try
                    {
                        if(NTDSSiteEntry.Properties.Contains("schedule"))
                        {
                            byte[] tmpSchedule = (byte[]) NTDSSiteEntry.Properties["schedule"][0];
                            Debug.Assert(tmpSchedule != null && tmpSchedule.Length == 188);
                            schedule = new ActiveDirectorySchedule();
                            schedule.SetUnmanagedSchedule(tmpSchedule);                        
                        } 
                    }
                    catch(COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                {
                    if(replicationSchedule != null)
                    {
                        // newly created site, get the schedule if already has been set by the user
                        schedule = new ActiveDirectorySchedule();
                        schedule.SetUnmanagedSchedule(replicationSchedule);
                    }
                }

                return schedule;
                
            }
            set {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if(existing)
                {
                    try
                    {
                        if(value == null)
                        {
                            // clear it out if existing before
                            if(NTDSSiteEntry.Properties.Contains("schedule"))
                                NTDSSiteEntry.Properties["schedule"].Clear();
                        }
                        else
                            // replace with the new value
                            NTDSSiteEntry.Properties["schedule"].Value = value.GetUnmanagedSchedule();
                    }
                    catch(COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                else
                {
                    // clear out the schedule
                    if(value == null)
                        replicationSchedule = null;
                    else
                    {
                        // replace with the new value
                        replicationSchedule = value.GetUnmanagedSchedule();
                    }
                }               
                           
            }
        }

        private bool IsADAM {
            get {
                if(!checkADAM)
                {
                    DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);      
                    PropertyValueCollection values = null;
                    try
                    {
                        values = de.Properties["supportedCapabilities"];
                    }
                    catch(COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    
                    if(values.Contains(SupportedCapability.ADAMOid))
                        this.isADAMServer = true;
                }

                return this.isADAMServer;
            }
        }

        private DirectoryEntry NTDSSiteEntry {
            get {
                if(ntdsEntry == null)
                {                    
                    DirectoryEntry tmp = DirectoryEntryManager.GetDirectoryEntry(context, "CN=NTDS Site Settings," + (string) PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName));
                    try
                    {
                        tmp.RefreshCache();
                    }
                    catch(COMException e)
                    {
                        if ( e.ErrorCode == unchecked((int)0x80072030) )
                        {
                            string message = Res.GetString(Res.NTDSSiteSetting, name);                           
                            throw new ActiveDirectoryOperationException(message, e, 0x2030);
                        }
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    ntdsEntry = tmp;
                    
                }

                return ntdsEntry;
            }
        }        

        public void Save()
        {
            if(this.disposed)
                throw new ObjectDisposedException(GetType().Name);
            
            try
            {

                // commit changes           
                cachedEntry.CommitChanges();

                foreach(DictionaryEntry e in subnets.changeList)
                {
                    try
                    {
                        ((DirectoryEntry) e.Value).CommitChanges();                       
                    }
                    catch(COMException exception)
                    {
                        // there is a bug in ADSI that when targeting ADAM, permissive modify control is not used.
                        if(exception.ErrorCode != unchecked((int)0x8007200A))
                            throw ExceptionHelper.GetExceptionFromCOMException(exception);
                    }
                }

                 // reset status variables            
                subnets.changeList.Clear();           
                subnetRetrieved = false;

                // need to throw better exception for ADAM since its SMTP transport is not available            
                foreach(DictionaryEntry e in SMTPBridgeheadServers.changeList)
                {
                    try
                    {
                        ((DirectoryEntry) e.Value).CommitChanges();                       
                    }
                    catch(COMException exception)
                    {
                        // SMTP transport is not supported on ADAM
                        if(IsADAM && (exception.ErrorCode == unchecked((int)0x8007202F)))
                            throw new NotSupportedException(Res.GetString(Res.NotSupportTransportSMTP));
                        
                        // there is a bug in ADSI that when targeting ADAM, permissive modify control is not used.
                        if(exception.ErrorCode != unchecked((int)0x8007200A))
                            throw ExceptionHelper.GetExceptionFromCOMException(exception);
                    }
                }
                
                SMTPBridgeheadServers.changeList.Clear();            
                SMTPBridgeRetrieved = false;            
                
                foreach(DictionaryEntry e in RPCBridgeheadServers.changeList)
                {                
                    try
                    {
                        ((DirectoryEntry) e.Value).CommitChanges();                       
                    }
                    catch(COMException exception)
                    {      
                        // there is a bug in ADSI that when targeting ADAM, permissive modify control is not used.
                        if(exception.ErrorCode != unchecked((int)0x8007200A))
                            throw ExceptionHelper.GetExceptionFromCOMException(exception);
                    }
                }           
                
                RPCBridgeheadServers.changeList.Clear();            
                RPCBridgeRetrieved = false;
                
                if(existing)
                {  
                    // topology generator is changed
                    if(topologyTouched)
                    {                    
                        try
                        {
                            DirectoryServer server = InterSiteTopologyGenerator;
                            string ntdsaName = (server is DomainController) ? ((DomainController)server).NtdsaObjectName : ((AdamInstance)server).NtdsaObjectName;
                            NTDSSiteEntry.Properties["interSiteTopologyGenerator"].Value = ntdsaName;
                        }
                        catch(COMException e)
                        {
                            throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                        }                           
                    }         

                    NTDSSiteEntry.CommitChanges();                    
                    topologyTouched = false;
                    
                }
                else
                {   
                     try
                     {
                         // create nTDSSiteSettings object
                         DirectoryEntry tmpEntry = cachedEntry.Children.Add("CN=NTDS Site Settings", "nTDSSiteSettings");
                         //set properties on the Site NTDS settings object
                         DirectoryServer replica = InterSiteTopologyGenerator;
                         if(replica != null)
                         {
                             string ntdsaName = (replica is DomainController) ? ((DomainController)replica).NtdsaObjectName : ((AdamInstance)replica).NtdsaObjectName;
                             tmpEntry.Properties["interSiteTopologyGenerator"].Value = ntdsaName;
                         }
                         tmpEntry.Properties["options"].Value = siteOptions;
                         if(replicationSchedule != null)
                         {                    
                             tmpEntry.Properties["schedule"].Value = replicationSchedule;
                         }
                    
                         tmpEntry.CommitChanges();
                         // cached the entry                 
                         this.ntdsEntry = tmpEntry;

                         // create servers contain object
                         tmpEntry = cachedEntry.Children.Add("CN=Servers", "serversContainer");
                         tmpEntry.CommitChanges();

                         if(!IsADAM)
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
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        public void Delete()
        {
            if(this.disposed)
                throw new ObjectDisposedException(GetType().Name);
            
            if(!existing)
            {
                throw new InvalidOperationException(Res.GetString(Res.CannotDelete));
            }
            else
            {
                try
                {
                    cachedEntry.DeleteTree();
                }
                catch(COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public override string ToString() 
        {
            if(this.disposed)
                throw new ObjectDisposedException(GetType().Name);
            
            return name;            
        }

        private ReadOnlyDirectoryServerCollection GetBridgeheadServers()
        {            
            NativeComInterfaces.IAdsPathname pathCracker = (NativeComInterfaces.IAdsPathname) new NativeComInterfaces.Pathname();
            // need to turn off the escaping for name
            pathCracker.EscapedMode = NativeComInterfaces.ADS_ESCAPEDMODE_OFF_EX;
            
            ReadOnlyDirectoryServerCollection collection = new ReadOnlyDirectoryServerCollection();
            if(existing)
            {                
                Hashtable bridgeHeadTable = new Hashtable();
                Hashtable nonBridgHeadTable = new Hashtable();
                Hashtable hostNameTable = new Hashtable();
                const string ocValue = "CN=Server";
                
                // get destination bridgehead servers

                // first go to the servers container under the current site and then do a search to get the all server objects.
                string serverContainer = "CN=Servers," + (string) PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName);
                DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, serverContainer);                
                
                try
                {
                    // go through connection objects and find out its fromServer property. 
                    ADSearcher adSearcher = new ADSearcher(de, 
                                                          "(|(objectCategory=server)(objectCategory=NTDSConnection))",
                                                          new string[] {"fromServer", "distinguishedName", "dNSHostName", "objectCategory"},
                                                          SearchScope.Subtree,
                                                          true, /* need paged search */
                                                          true /* need cached result as we need to go back to the first record */);                    
                    SearchResultCollection conResults = null;
                    try
                    {
                        conResults = adSearcher.FindAll();
                    }
                    catch(COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    
                    try
                    {
                        // find out whether fromServer indicates replicating from a server in another site.
                        foreach(SearchResult r in conResults)
                        {  
                            string objectCategoryValue = (string) PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.ObjectCategory);
                            if(Utils.Compare(objectCategoryValue, 0, ocValue.Length, ocValue, 0, ocValue.Length) == 0)
                            {
                                hostNameTable.Add((string) PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.DistinguishedName), (string) PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.DnsHostName));                                                                
                            }
                        }

                         foreach(SearchResult r in conResults)
                         {
                             string objectCategoryValue = (string) PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.ObjectCategory);
                             if(Utils.Compare(objectCategoryValue, 0, ocValue.Length, ocValue, 0, ocValue.Length) != 0)
                             {
                                string fromServer = (string) PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.FromServer);                                
                                
                                // escaping manipulation                                
                                string fromSite = Utils.GetPartialDN(fromServer, 3);
                                pathCracker.Set(fromSite, NativeComInterfaces.ADS_SETTYPE_DN);
                                fromSite = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_LEAF);                   
                                Debug.Assert(fromSite != null && Utils.Compare(fromSite, 0, 3, "CN=", 0, 3) == 0);
                                fromSite = fromSite.Substring(3);
                                
                                string serverObjectName = Utils.GetPartialDN((string) PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.DistinguishedName), 2);                                                            
                                // don't know whether it is a bridgehead server yet.
                                if(!bridgeHeadTable.Contains(serverObjectName))
                                {
                                    string hostName = (string) hostNameTable[serverObjectName];                                    
                                    // add if not yet done
                                    if(!nonBridgHeadTable.Contains(serverObjectName))
                                        nonBridgHeadTable.Add(serverObjectName, hostName);
                                
                                    // check whether from different site
                                    if(Utils.Compare((string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.Cn), fromSite) != 0)
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
                if(nonBridgHeadTable.Count != 0)
                {
                    // go to sites container to get all the connecdtion object that replicates from servers in the current sites that have
                    // not been determined whether it is a bridgehead server or not.
                    DirectoryEntry serverEntry = DirectoryEntryManager.GetDirectoryEntry(context, siteDN);                    
                    // constructing the filter
                    StringBuilder str = new StringBuilder(100);
                    if(nonBridgHeadTable.Count > 1)
                        str.Append("(|");
                    foreach(DictionaryEntry val in nonBridgHeadTable)
                    {
                        str.Append("(fromServer=");
                        str.Append("CN=NTDS Settings,");
                        str.Append(Utils.GetEscapedFilterValue((string) val.Key));
                        str.Append(")");
                    }
                    if(nonBridgHeadTable.Count > 1)
                        str.Append(")");                    
                    ADSearcher adSearcher = new ADSearcher(serverEntry, 
                                                          "(&(objectClass=nTDSConnection)(objectCategory=NTDSConnection)" + str.ToString() + ")",
                                                          new string[] {"fromServer", "distinguishedName"},
                                                          SearchScope.Subtree);
                    SearchResultCollection conResults = null;
                    try
                    {
                        conResults = adSearcher.FindAll();
                    }
                    catch(COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    
                    try
                    {
                        foreach(SearchResult r in conResults)
                        {
                            string fromServer = (string) PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.FromServer);
                            string serverObject = fromServer.Substring(17);
                            
                            if(nonBridgHeadTable.Contains(serverObject))
                            {
                                string otherSite = Utils.GetPartialDN((string) PropertyManager.GetSearchResultPropertyValue(r, PropertyManager.DistinguishedName), 4);
                                // escaping manipulation
                                pathCracker.Set(otherSite, NativeComInterfaces.ADS_SETTYPE_DN);
                                otherSite = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_LEAF);                   
                                Debug.Assert(otherSite != null && Utils.Compare(otherSite, 0, 3, "CN=", 0, 3) == 0);
                                otherSite = otherSite.Substring(3);                                
                                
                                // check whether from different sites
                                if(Utils.Compare(otherSite, (string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.Cn)) != 0)
                                {
                                    string val = (string) nonBridgHeadTable[serverObject];
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

                DirectoryEntry ADAMEntry = null;
                foreach(DictionaryEntry e in bridgeHeadTable)
                {
                    DirectoryServer replica = null;
                    string host = (string) e.Value;
                    // construct directoryreplica
                    if(IsADAM)
                    {                        
                        ADAMEntry = DirectoryEntryManager.GetDirectoryEntry(context, "CN=NTDS Settings," + e.Key);                                                                    
                        int port = (int) PropertyManager.GetPropertyValue(context, ADAMEntry, PropertyManager.MsDSPortLDAP);
                        string fullhost = host;
                        if(port != 389)
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
            if(this.disposed)
                throw new ObjectDisposedException(GetType().Name);

            if(!existing)
            {
                throw new InvalidOperationException(Res.GetString(Res.CannotGetObject));
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
            if (disposing) {
                // free other state (managed objects)                
                if(cachedEntry != null)
                    cachedEntry.Dispose();            

                if(ntdsEntry != null)
                    ntdsEntry.Dispose();
            }

            // free your own state (unmanaged objects)   

            disposed = true;        	
        }
        
        private static void ValidateArgument(DirectoryContext context, string siteName)
        {
            // basic validation first
            if(context == null)
                throw new ArgumentNullException("context");

            // if target is not specified, then we determin the target from the logon credential, so if it is a local user context, it should fail
            if ((context.Name == null) && (!context.isRootDomain())) 
            {
                throw new ArgumentException(Res.GetString(Res.ContextNotAssociatedWithDomain), "context");
            }

             // more validation for the context, if the target is not null, then it should be either forest name or server name
            if(context.Name != null)
            {
                if(!(context.isRootDomain() || context.isServer() || context.isADAMConfigSet()))
                    throw new ArgumentException(Res.GetString(Res.NotADOrADAM), "context");
            }  

            if(siteName == null)
                throw new ArgumentNullException("siteName");

            if(siteName.Length == 0)
                throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "siteName");
        }      

        private void GetSubnets()
        {
            // performs a search to find out the subnets that belong to this site     
            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            string config = (string) PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext);
            string subnetContainer = "CN=Subnets,CN=Sites," + config;
            de = DirectoryEntryManager.GetDirectoryEntry(context, subnetContainer);            
             
            ADSearcher adSearcher = new ADSearcher(de,
                                                  "(&(objectClass=subnet)(objectCategory=subnet)(siteObject=" + Utils.GetEscapedFilterValue((string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)) + "))",
                                                  new string[] {"cn", "location"},
                                                  SearchScope.OneLevel
                                                  );
            SearchResultCollection results = null;

            try
            {
                results = adSearcher.FindAll();
            }
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            
            try
            {
                string subnetName = null;
                foreach(SearchResult result in results)
                {                    
                    subnetName = (string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn);
                    ActiveDirectorySubnet subnet = new ActiveDirectorySubnet(context, subnetName, null, true);
                    // set the cached entry
                    subnet.cachedEntry = result.GetDirectoryEntry();
                    // set the site info
                    subnet.Site = this;
                    
                    subnets.Add(subnet);
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
            string config = (string) de.Properties["configurationNamingContext"][0];
            string transportContainer = "CN=Inter-Site Transports,CN=Sites," + config;
            de = DirectoryEntryManager.GetDirectoryEntry(context, transportContainer);
            ADSearcher adSearcher = new ADSearcher(de,
                                                  "(&(objectClass=siteLink)(objectCategory=SiteLink)(siteList=" + Utils.GetEscapedFilterValue((string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName))+ "))",
                                                  new string[] {"cn", "distinguishedName"},
                                                  SearchScope.Subtree);
            SearchResultCollection results = null;            

            try
            {
                results = adSearcher.FindAll();
            }
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            
            try
            {
                ActiveDirectorySiteLink link = null;
                
                foreach(SearchResult result in results)
                {  
                    string dn = (string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.DistinguishedName);
                    string linkName = (string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn);
                    string transportName = (string) Utils.GetDNComponents(dn)[1].Value;
                    ActiveDirectoryTransportType transportType;
                    if (String.Compare(transportName, "IP", StringComparison.OrdinalIgnoreCase) == 0)
                        transportType = ActiveDirectoryTransportType.Rpc;
                    else if (String.Compare(transportName, "SMTP", StringComparison.OrdinalIgnoreCase) == 0)
                        transportType = ActiveDirectoryTransportType.Smtp;
                    else 
                    {
                        // should not happen
                        string message = Res.GetString(Res.UnknownTransport, transportName);
                        throw new ActiveDirectoryOperationException(message); 
                    }

                    try
                    {
                        link = new ActiveDirectorySiteLink(context, linkName, transportType, true, result.GetDirectoryEntry());
                        foreach(ActiveDirectorySite tmpSite in link.Sites)
                        {
                            // don't add itself                            
                            if(Utils.Compare(tmpSite.Name, Name) == 0)
                                continue;
                            
                            if(!adjacentSites.Contains(tmpSite))
                                adjacentSites.Add(tmpSite);
                        }    
                    }
                    finally
                    {
                        link.Dispose();
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
            string config = (string) PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext);
            string transportContainer = "CN=Inter-Site Transports,CN=Sites," + config;
            de = DirectoryEntryManager.GetDirectoryEntry(context, transportContainer);            
            ADSearcher adSearcher = new ADSearcher(de,
                                                  "(&(objectClass=siteLink)(objectCategory=SiteLink)(siteList=" + Utils.GetEscapedFilterValue((string)PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName)) + "))",
                                                  new string[] {"cn", "distinguishedName"},
                                                  SearchScope.Subtree);
            SearchResultCollection results = null;
            
            try
            {
                results = adSearcher.FindAll();
            }
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            
            try
            {
                foreach(SearchResult result in results)
                {                  
                    // construct the sitelinks at the same time
                    DirectoryEntry connectionEntry = result.GetDirectoryEntry();
                    string cn = (string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn);
                    string transport = Utils.GetDNComponents((string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.DistinguishedName))[1].Value;
                    ActiveDirectorySiteLink link = null;
                    if(String.Compare(transport, "IP", StringComparison.OrdinalIgnoreCase) == 0)
                        link = new ActiveDirectorySiteLink(context, cn, ActiveDirectoryTransportType.Rpc, true, connectionEntry);
                    else if(String.Compare(transport, "SMTP", StringComparison.OrdinalIgnoreCase) == 0)
                        link = new ActiveDirectorySiteLink(context, cn, ActiveDirectoryTransportType.Smtp, true, connectionEntry);
                    else
                    {                    
                        // should not happen
                        string message = Res.GetString(Res.UnknownTransport, transport);
                        throw new ActiveDirectoryOperationException(message);     
                    }

                    links.Add(link);
                    
                    
                }
            }
            finally
            {
                results.Dispose();
                de.Dispose();
            }
            
        }

        private void GetDomains()
        {
            // for ADAM, there is no concept of domain, we just return empty collection which is good enough
            if(!IsADAM)
            {
                string serverName = cachedEntry.Options.GetCurrentServerName();                
                DomainController dc = DomainController.GetDomainController(Utils.GetNewDirectoryContext(serverName, DirectoryContextType.DirectoryServer, context));
                IntPtr handle = dc.Handle;

                Debug.Assert(handle != (IntPtr)0);                

                IntPtr info = (IntPtr) 0;
                // call DsReplicaSyncAllW
                IntPtr functionPtr = UnsafeNativeMethods.GetProcAddress(DirectoryContext.ADHandle, "DsListDomainsInSiteW");
                if(functionPtr == (IntPtr)0)
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
                }
                UnsafeNativeMethods.DsListDomainsInSiteW dsListDomainsInSiteW = (UnsafeNativeMethods.DsListDomainsInSiteW)Marshal.GetDelegateForFunctionPointer(functionPtr, typeof(UnsafeNativeMethods.DsListDomainsInSiteW));

                int result = dsListDomainsInSiteW(handle, (string) PropertyManager.GetPropertyValue(context, cachedEntry, PropertyManager.DistinguishedName), ref info);
                if(result != 0)
                    throw ExceptionHelper.GetExceptionFromErrorCode(result, serverName);

                try
                {
                    DS_NAME_RESULT names = new DS_NAME_RESULT();
                    Marshal.PtrToStructure(info, names);
                    int count = names.cItems;                     
                    IntPtr val = names.rItems;
                    if(count > 0)
                    {
                        Debug.Assert(val != (IntPtr) 0);
                        int status = Marshal.ReadInt32(val);                    
                        IntPtr tmpPtr = (IntPtr)0;
                        for(int i = 0; i < count; i++)
                        {
                            tmpPtr = IntPtr.Add(val, Marshal.SizeOf(typeof(DS_NAME_RESULT_ITEM)) * i);
                            DS_NAME_RESULT_ITEM nameResult = new DS_NAME_RESULT_ITEM();
                            Marshal.PtrToStructure(tmpPtr, nameResult);
                            if(nameResult.status == DS_NAME_ERROR.DS_NAME_NO_ERROR || nameResult.status == DS_NAME_ERROR.DS_NAME_ERROR_DOMAIN_ONLY)
                            {
                                string domainName = Marshal.PtrToStringUni(nameResult.pName);
                                if(domainName != null && domainName.Length > 0)
                                {
                                    string d = Utils.GetDnsNameFromDN(domainName);                                    
                                    Domain domain = new Domain(Utils.GetNewDirectoryContext(d, DirectoryContextType.Domain, context), d);
                                    domains.Add(domain);
                                }
                            }
                           
                        }
                    }
                }
                finally
                {
                    // call DsFreeNameResultW
                    functionPtr = UnsafeNativeMethods.GetProcAddress(DirectoryContext.ADHandle, "DsFreeNameResultW");
                    if(functionPtr == (IntPtr)0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
                    }
                    UnsafeNativeMethods.DsFreeNameResultW dsFreeNameResultW = (UnsafeNativeMethods.DsFreeNameResultW)Marshal.GetDelegateForFunctionPointer(functionPtr, typeof(UnsafeNativeMethods.DsFreeNameResultW));

                    dsFreeNameResultW(info);
                }
                   
            }
               
        }

        private void GetServers()
        {            
            ADSearcher adSearcher = new ADSearcher(cachedEntry,
                                                  "(&(objectClass=server)(objectCategory=server))",
                                                  new string[] {"dNSHostName"},
                                                  SearchScope.Subtree);
            SearchResultCollection results = null;
            try
            {
                results = adSearcher.FindAll();
            }
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            
            try
            {
                foreach(SearchResult result in results)
                {
                    string hostName = (string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.DnsHostName);
                    DirectoryEntry de = result.GetDirectoryEntry();
                    DirectoryEntry child = null;
                    DirectoryServer replica = null;
                    // make sure that the server is not demoted
                    try
                    {
                        child = de.Children.Find("CN=NTDS Settings", "nTDSDSA");
                    }
                    catch(COMException e)
                    {
                        if(e.ErrorCode == unchecked((int)0x80072030))
                        {
                            continue;
                        }
                        else
                            throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    if(IsADAM)
                    {                        
                        int port = (int) PropertyManager.GetPropertyValue(context, child, PropertyManager.MsDSPortLDAP);
                        string fullHostName = hostName;
                        if(port != 389)
                        {
                            fullHostName = hostName + ":" + port;                            
                        }
                        replica = new AdamInstance(Utils.GetNewDirectoryContext(fullHostName, DirectoryContextType.DirectoryServer, context), fullHostName);                            
                    }
                    else
                        replica = new DomainController(Utils.GetNewDirectoryContext(hostName, DirectoryContextType.DirectoryServer, context), hostName);

                    servers.Add(replica);
                    
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
            string transportDN = null;
            if(transport == ActiveDirectoryTransportType.Smtp)
                transportDN = "CN=SMTP,CN=Inter-Site Transports," + siteDN;
            else
                transportDN = "CN=IP,CN=Inter-Site Transports," + siteDN;               
            
            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, serverContainerDN);            
            ADSearcher adSearcher = new ADSearcher(de,
                                                  "(&(objectClass=server)(objectCategory=Server)(bridgeheadTransportList=" + Utils.GetEscapedFilterValue(transportDN) + "))",
                                                  new string[] {"dNSHostName", "distinguishedName"},
                                                  SearchScope.OneLevel);
            SearchResultCollection results = null;

            try
            {
                results = adSearcher.FindAll();
            }
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            
            try
            {
                DirectoryEntry ADAMEntry = null;
                foreach(SearchResult result in results)
                {                    
                    string hostName = (string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.DnsHostName);
                    DirectoryEntry resultEntry = result.GetDirectoryEntry();                   
                    DirectoryServer replica = null;                   

                    try
                    {
                        ADAMEntry = resultEntry.Children.Find("CN=NTDS Settings", "nTDSDSA");                    
                    }
                    catch(COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    
                    if(IsADAM)
                    {                       
                        int port = (int) PropertyManager.GetPropertyValue(context, ADAMEntry, PropertyManager.MsDSPortLDAP);
                        string fullHostName = hostName;
                        if(port != 389)
                        {
                            fullHostName = hostName + ":" + port;                            
                        }
                        replica = new AdamInstance(Utils.GetNewDirectoryContext(fullHostName, DirectoryContextType.DirectoryServer, context), fullHostName);                            
                    }
                    else
                        replica = new DomainController(Utils.GetNewDirectoryContext(hostName, DirectoryContextType.DirectoryServer, context), hostName);

                    if(transport == ActiveDirectoryTransportType.Smtp)
                        SMTPBridgeheadServers.Add(replica);
                    else
                        RPCBridgeheadServers.Add(replica);
                    
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
