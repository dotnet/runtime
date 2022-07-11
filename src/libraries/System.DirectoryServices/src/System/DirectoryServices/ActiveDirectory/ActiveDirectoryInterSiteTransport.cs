// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.ComponentModel;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectoryInterSiteTransport : IDisposable
    {
        private readonly DirectoryContext _context;
        private readonly DirectoryEntry _cachedEntry;
        private readonly ActiveDirectoryTransportType _transport;
        private bool _disposed;
        private bool _linkRetrieved;
        private bool _bridgeRetrieved;

        private readonly ReadOnlySiteLinkCollection _siteLinkCollection = new ReadOnlySiteLinkCollection();
        private readonly ReadOnlySiteLinkBridgeCollection _bridgeCollection = new ReadOnlySiteLinkBridgeCollection();

        internal ActiveDirectoryInterSiteTransport(DirectoryContext context, ActiveDirectoryTransportType transport, DirectoryEntry entry)
        {
            _context = context;
            _transport = transport;
            _cachedEntry = entry;
        }

        public static ActiveDirectoryInterSiteTransport FindByTransportType(DirectoryContext context, ActiveDirectoryTransportType transport)
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

            if (transport < ActiveDirectoryTransportType.Rpc || transport > ActiveDirectoryTransportType.Smtp)
                throw new InvalidEnumArgumentException("value", (int)transport, typeof(ActiveDirectoryTransportType));

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de;

            try
            {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;
                string containerDN = "CN=Inter-Site Transports,CN=Sites," + config;
                if (transport == ActiveDirectoryTransportType.Rpc)
                    containerDN = "CN=IP," + containerDN;
                else
                    containerDN = "CN=SMTP," + containerDN;
                de = DirectoryEntryManager.GetDirectoryEntry(context, containerDN);
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
                de.RefreshCache(new string[] { "options" });
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80072030))
                {
                    // if it is ADAM and transport type is SMTP, throw NotSupportedException.
                    DirectoryEntry tmpDE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                    if (Utils.CheckCapability(tmpDE, Capability.ActiveDirectoryApplicationMode) && transport == ActiveDirectoryTransportType.Smtp)
                    {
                        throw new NotSupportedException(SR.NotSupportTransportSMTP);
                    }

                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.TransportNotFound, transport.ToString()), typeof(ActiveDirectoryInterSiteTransport), transport.ToString());
                }
                else
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            return new ActiveDirectoryInterSiteTransport(context, transport, de);
        }

        public ActiveDirectoryTransportType TransportType
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _transport;
            }
        }

        public bool IgnoreReplicationSchedule
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int option = 0;
                try
                {
                    if (_cachedEntry.Properties.Contains("options"))
                        option = (int)_cachedEntry.Properties["options"][0]!;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }

                // NTDSTRANSPORT_OPT_IGNORE_SCHEDULES ( 1 << 0 )  Schedules disabled
                if ((option & 0x1) != 0)
                    return true;
                else
                    return false;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int option = 0;
                try
                {
                    if (_cachedEntry.Properties.Contains("options"))
                        option = (int)_cachedEntry.Properties["options"][0]!;

                    // NTDSTRANSPORT_OPT_IGNORE_SCHEDULES ( 1 << 0 )  Schedules disabled
                    if (value)
                        option |= 0x1;
                    else
                        option &= (~(0x1));

                    _cachedEntry.Properties["options"].Value = option;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
            }
        }

        public bool BridgeAllSiteLinks
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int option = 0;
                try
                {
                    if (_cachedEntry.Properties.Contains("options"))
                        option = (int)_cachedEntry.Properties["options"][0]!;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }

                // NTDSTRANSPORT_OPT_BRIDGES_REQUIRED (1 << 1 ) siteLink bridges are required
                // That is to say, if this bit is set, it means that all site links are not bridged and user needs to create specific bridge
                if ((option & 0x2) != 0)
                    return false;
                else
                    return true;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int option = 0;
                try
                {
                    if (_cachedEntry.Properties.Contains("options"))
                        option = (int)_cachedEntry.Properties["options"][0]!;

                    // NTDSTRANSPORT_OPT_BRIDGES_REQUIRED (1 << 1 ) siteLink bridges are required, all site links are not bridged
                    // That is to say, if this bit is set, it means that all site links are not bridged and user needs to create specific bridge
                    // if this bit is not set, all the site links are bridged
                    if (value)
                        option &= (~(0x2));
                    else
                        option |= 0x2;

                    _cachedEntry.Properties["options"].Value = option;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
            }
        }

        public ReadOnlySiteLinkCollection SiteLinks
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (!_linkRetrieved)
                {
                    _siteLinkCollection.Clear();

                    ADSearcher adSearcher = new ADSearcher(_cachedEntry,
                                                             "(&(objectClass=siteLink)(objectCategory=SiteLink))",
                                                             new string[] { "cn" },
                                                             SearchScope.OneLevel);
                    SearchResultCollection? results = null;

                    try
                    {
                        results = adSearcher.FindAll();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                    }

                    try
                    {
                        foreach (SearchResult result in results)
                        {
                            DirectoryEntry connectionEntry = result.GetDirectoryEntry();
                            string cn = (string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn)!;
                            ActiveDirectorySiteLink link = new ActiveDirectorySiteLink(_context, cn, _transport, true, connectionEntry);
                            _siteLinkCollection.Add(link);
                        }
                    }
                    finally
                    {
                        results.Dispose();
                    }

                    _linkRetrieved = true;
                }

                return _siteLinkCollection;
            }
        }

        public ReadOnlySiteLinkBridgeCollection SiteLinkBridges
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (!_bridgeRetrieved)
                {
                    _bridgeCollection.Clear();

                    ADSearcher adSearcher = new ADSearcher(_cachedEntry,
                                                             "(&(objectClass=siteLinkBridge)(objectCategory=SiteLinkBridge))",
                                                             new string[] { "cn" },
                                                             SearchScope.OneLevel);
                    SearchResultCollection? results = null;

                    try
                    {
                        results = adSearcher.FindAll();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                    }

                    try
                    {
                        foreach (SearchResult result in results)
                        {
                            DirectoryEntry connectionEntry = result.GetDirectoryEntry();
                            string cn = (string)PropertyManager.GetSearchResultPropertyValue(result, PropertyManager.Cn)!;
                            ActiveDirectorySiteLinkBridge bridge = new ActiveDirectorySiteLinkBridge(_context, cn, _transport, true);
                            bridge.cachedEntry = connectionEntry;
                            _bridgeCollection.Add(bridge);
                        }
                    }
                    finally
                    {
                        results.Dispose();
                    }

                    _bridgeRetrieved = true;
                }

                return _bridgeCollection;
            }
        }

        public void Save()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            try
            {
                _cachedEntry.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
        }

        public DirectoryEntry GetDirectoryEntry()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            return DirectoryEntryManager.GetDirectoryEntryInternal(_context, _cachedEntry.Path);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            return _transport.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free other state (managed objects)
                _cachedEntry?.Dispose();
            }

            // free your own state (unmanaged objects)

            _disposed = true;
        }
    }
}
