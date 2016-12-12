//------------------------------------------------------------------------------
// <copyright file="ActiveDirectoryInterSiteTransport.cs" company="Microsoft">
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
    using System.Globalization;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Security.Permissions;

    [DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)]
    public class ActiveDirectoryInterSiteTransport:IDisposable {
        DirectoryContext context = null;
        DirectoryEntry cachedEntry = null;
        ActiveDirectoryTransportType transport;
        private bool disposed = false; 
        private bool linkRetrieved = false;
        private bool bridgeRetrieved = false;

        private ReadOnlySiteLinkCollection siteLinkCollection = new ReadOnlySiteLinkCollection();
        private ReadOnlySiteLinkBridgeCollection bridgeCollection = new ReadOnlySiteLinkBridgeCollection();

        internal ActiveDirectoryInterSiteTransport(DirectoryContext context, ActiveDirectoryTransportType transport, DirectoryEntry entry)
        {
            this.context = context;
            this.transport = transport;
            this.cachedEntry = entry;
        }

        public static ActiveDirectoryInterSiteTransport FindByTransportType(DirectoryContext context, ActiveDirectoryTransportType transport)
        {
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

            if (transport < ActiveDirectoryTransportType.Rpc || transport > ActiveDirectoryTransportType.Smtp) 
                throw new InvalidEnumArgumentException("value", (int)transport, typeof(ActiveDirectoryTransportType));        

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de;

            try {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string) PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext);
                string containerDN = "CN=Inter-Site Transports,CN=Sites," + config;
                if(transport == ActiveDirectoryTransportType.Rpc)
                    containerDN = "CN=IP," + containerDN;
                else
                    containerDN = "CN=SMTP," + containerDN;
                de = DirectoryEntryManager.GetDirectoryEntry(context, containerDN);
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
                de.RefreshCache(new string[] {"options"});
            }
            catch(COMException e)
            {
                if ( e.ErrorCode == unchecked((int)0x80072030) )
                {       
                    // if it is ADAM and transport type is SMTP, throw NotSupportedException.
                    DirectoryEntry tmpDE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                    if(Utils.CheckCapability(tmpDE, Capability.ActiveDirectoryApplicationMode) && transport == ActiveDirectoryTransportType.Smtp)
                    {
                        throw new NotSupportedException(Res.GetString(Res.NotSupportTransportSMTP));
                    }
                    
                    throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.TransportNotFound, transport.ToString()), typeof(ActiveDirectoryInterSiteTransport), transport.ToString());
                }
                else
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            return new ActiveDirectoryInterSiteTransport(context, transport, de);
            
        }    

        public ActiveDirectoryTransportType TransportType {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return transport;
            }
        }

        public bool IgnoreReplicationSchedule {
            get {
                 if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                 
                int option = 0;
                try
                {
                    if(cachedEntry.Properties.Contains("options"))
                        option = (int) cachedEntry.Properties["options"][0];
                }
                catch(COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                // NTDSTRANSPORT_OPT_IGNORE_SCHEDULES ( 1 << 0 )  Schedules disabled
                if((option & 0x1) != 0)
                    return true;
                else
                    return false;
            }
            set {
                 if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                 
                int option = 0;
                try
                {
                    if(cachedEntry.Properties.Contains("options"))
                        option = (int) cachedEntry.Properties["options"][0];

                    // NTDSTRANSPORT_OPT_IGNORE_SCHEDULES ( 1 << 0 )  Schedules disabled
                    if(value)
                        option |= 0x1;
                    else
                        option &= (~(0x1));

                    cachedEntry.Properties["options"].Value = option;
                }
                catch(COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public bool BridgeAllSiteLinks {
            get {
                 if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);
                 
                int option = 0;
                try
                {
                    if(cachedEntry.Properties.Contains("options"))
                        option = (int) cachedEntry.Properties["options"][0];
                }
                catch(COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                // NTDSTRANSPORT_OPT_BRIDGES_REQUIRED (1 << 1 ) siteLink bridges are required
                // That is to say, if this bit is set, it means that all site links are not bridged and user needs to create specific bridge 
                if((option & 0x2) != 0)
                    return false;
                else
                    return true;
            }
            set {
                 if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                 int option = 0;
                 try
                 {
                     if(cachedEntry.Properties.Contains("options"))
                        option = (int) cachedEntry.Properties["options"][0];

                     // NTDSTRANSPORT_OPT_BRIDGES_REQUIRED (1 << 1 ) siteLink bridges are required, all site links are not bridged
                     // That is to say, if this bit is set, it means that all site links are not bridged and user needs to create specific bridge 
                     // if this bit is not set, all the site links are bridged
                     if(value)
                        option &= (~(0x2));
                     else
                        option |= 0x2;

                     cachedEntry.Properties["options"].Value = option;
                 }
                 catch(COMException e)
                 {
                     throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                 }
             }
        }

        public ReadOnlySiteLinkCollection SiteLinks {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if(!linkRetrieved)
                {                    
                    siteLinkCollection.Clear();                    
                    
                    ADSearcher adSearcher = new ADSearcher(cachedEntry,
                                                             "(&(objectClass=siteLink)(objectCategory=SiteLink))",
                                                             new string[] {"cn"},
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
                        foreach(SearchResult result in results)
                        {
                            DirectoryEntry connectionEntry = result.GetDirectoryEntry();
                            string cn = (string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn);
                            ActiveDirectorySiteLink link = new ActiveDirectorySiteLink(context, cn, transport, true, connectionEntry);
                            siteLinkCollection.Add(link);
                        }
                    }
                    finally
                    {
                        results.Dispose();
                    }

                    linkRetrieved = true;                    
                }

                return siteLinkCollection;
            }
        }

        public ReadOnlySiteLinkBridgeCollection SiteLinkBridges {
            get {
                if(this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if(!bridgeRetrieved)
                {
                    bridgeCollection.Clear();
                    
                    ADSearcher adSearcher = new ADSearcher(cachedEntry,
                                                             "(&(objectClass=siteLinkBridge)(objectCategory=SiteLinkBridge))",
                                                             new string[] {"cn"},
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
                        foreach(SearchResult result in results)
                        {
                            DirectoryEntry connectionEntry = result.GetDirectoryEntry();
                            string cn = (string) PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn);
                            ActiveDirectorySiteLinkBridge bridge = new ActiveDirectorySiteLinkBridge(context, cn, transport, true);
                            bridge.cachedEntry = connectionEntry;
                            bridgeCollection.Add(bridge);
                        }
                    }
                    finally
                    {
                        results.Dispose();
                    }

                    bridgeRetrieved = true;
                }

                return bridgeCollection;
            }
        }

        public void Save()
        {
            if(this.disposed)
                throw new ObjectDisposedException(GetType().Name);
            
            try
            {
                cachedEntry.CommitChanges();
            }
            catch(COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }       

        public DirectoryEntry GetDirectoryEntry()
        {
            if(this.disposed)
                throw new ObjectDisposedException(GetType().Name);

            return DirectoryEntryManager.GetDirectoryEntryInternal(context, cachedEntry.Path);                  
        }

        public void Dispose() 
        {            
            Dispose(true);
            GC.SuppressFinalize(this); 
        }

        public override string ToString() 
        {
            if(this.disposed)
                throw new ObjectDisposedException(GetType().Name);
            
            return transport.ToString();
        }

        protected virtual void Dispose(bool disposing) 
        {            
            if (disposing) {
                // free other state (managed objects)                
                if(cachedEntry != null)
                    cachedEntry.Dispose();                
            }

            // free your own state (unmanaged objects)   

            disposed = true;        	
        }


        
    }
}
