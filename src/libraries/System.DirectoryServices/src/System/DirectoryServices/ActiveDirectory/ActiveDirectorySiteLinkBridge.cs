// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySiteLinkBridge : IDisposable
    {
        internal readonly DirectoryContext context;
        private readonly string _name;
        private readonly ActiveDirectoryTransportType _transport = ActiveDirectoryTransportType.Rpc;
        private bool _disposed;

        private bool _existing;
        internal DirectoryEntry? cachedEntry;
        private readonly ActiveDirectorySiteLinkCollection _links = new ActiveDirectorySiteLinkCollection();
        private bool _linksRetrieved;

        public ActiveDirectorySiteLinkBridge(DirectoryContext context, string bridgeName) : this(context, bridgeName, ActiveDirectoryTransportType.Rpc)
        {
        }

        public ActiveDirectorySiteLinkBridge(DirectoryContext context, string bridgeName, ActiveDirectoryTransportType transport)
        {
            ValidateArgument(context, bridgeName, transport);

            //  work with copy of the context
            context = new DirectoryContext(context);

            this.context = context;
            _name = bridgeName;
            _transport = transport;

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de;

            try
            {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;
                string parentDN;
                if (transport == ActiveDirectoryTransportType.Rpc)
                    parentDN = "CN=IP,CN=Inter-Site Transports,CN=Sites," + config;
                else
                    parentDN = "CN=SMTP,CN=Inter-Site Transports,CN=Sites," + config;

                de = DirectoryEntryManager.GetDirectoryEntry(context, parentDN);
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
                string rdn = "cn=" + _name;
                rdn = Utils.GetEscapedPath(rdn);
                cachedEntry = de.Children.Add(rdn, "siteLinkBridge");
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
                }

                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                de.Dispose();
            }
        }

        internal ActiveDirectorySiteLinkBridge(DirectoryContext context, string bridgeName, ActiveDirectoryTransportType transport, bool existing)
        {
            this.context = context;
            _name = bridgeName;
            _transport = transport;

            _existing = existing;
        }

        public static ActiveDirectorySiteLinkBridge FindByName(DirectoryContext context, string bridgeName)
        {
            return FindByName(context, bridgeName, ActiveDirectoryTransportType.Rpc);
        }

        public static ActiveDirectorySiteLinkBridge FindByName(DirectoryContext context, string bridgeName, ActiveDirectoryTransportType transport)
        {
            ValidateArgument(context, bridgeName, transport);

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
                ADSearcher adSearcher = new ADSearcher(de,
                                                      "(&(objectClass=siteLinkBridge)(objectCategory=SiteLinkBridge)(name=" + Utils.GetEscapedFilterValue(bridgeName) + "))",
                                                      ActiveDirectorySite.s_distinguishedName,
                                                      SearchScope.OneLevel,
                                                      false, /* don't need paged search */
                                                      false /* don't need to cache result */);
                SearchResult? srchResult = adSearcher.FindOne();

                if (srchResult == null)
                {
                    // no such site link bridge object
                    Exception e = new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySiteLinkBridge), bridgeName);
                    throw e;
                }
                else
                {
                    DirectoryEntry connectionEntry = srchResult.GetDirectoryEntry();
                    // it is an existing site link bridge object
                    ActiveDirectorySiteLinkBridge bridge = new ActiveDirectorySiteLinkBridge(context, bridgeName, transport, true);
                    bridge.cachedEntry = connectionEntry;
                    return bridge;
                }
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
                    else
                    {
                        // object is not found since we cannot even find the container in which to search
                        throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySiteLinkBridge), bridgeName);
                    }
                }

                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                de.Dispose();
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

        public ActiveDirectorySiteLinkCollection SiteLinks
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (_existing)
                {
                    // if asked the first time, we need to properly construct the subnets collection
                    if (!_linksRetrieved)
                    {
                        _links.initialized = false;
                        _links.Clear();
                        GetLinks();
                        _linksRetrieved = true;
                    }
                }
                _links.initialized = true;
                _links.de = cachedEntry;
                _links.context = context;
                return _links;
            }
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

        public void Save()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            try
            {
                cachedEntry!.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            if (_existing)
            {
                // indicates that nex time user asks for SiteLinks property, we might need to fetch it from server
                _linksRetrieved = false;
            }
            else
            {
                _existing = true;
            }
        }

        public void Delete()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!_existing)
            {
                throw new InvalidOperationException(SR.CannotDelete);
            }
            else
            {
                try
                {
                    cachedEntry!.Parent.Children.Remove(cachedEntry);
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

        public DirectoryEntry GetDirectoryEntry()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!_existing)
            {
                throw new InvalidOperationException(SR.CannotGetObject);
            }
            else
            {
                return DirectoryEntryManager.GetDirectoryEntryInternal(context, cachedEntry!.Path);
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
            }

            // free your own state (unmanaged objects)

            _disposed = true;
        }

        private static void ValidateArgument(DirectoryContext context, string bridgeName, ActiveDirectoryTransportType transport)
        {
            // basic validation first
            if (context == null)
                throw new ArgumentNullException(nameof(context));

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

            if (bridgeName == null)
                throw new ArgumentNullException(nameof(bridgeName));

            if (bridgeName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(bridgeName));

            if (transport < ActiveDirectoryTransportType.Rpc || transport > ActiveDirectoryTransportType.Smtp)
                throw new InvalidEnumArgumentException("value", (int)transport, typeof(ActiveDirectoryTransportType));
        }

        private void GetLinks()
        {
            ArrayList propertyList = new ArrayList();
            NativeComInterfaces.IAdsPathname? pathCracker = null;
            pathCracker = (NativeComInterfaces.IAdsPathname)new NativeComInterfaces.Pathname();
            // need to turn off the escaping for name
            pathCracker.EscapedMode = NativeComInterfaces.ADS_ESCAPEDMODE_OFF_EX;
            string propertyName = "siteLinkList";

            propertyList.Add(propertyName);
            Hashtable values = Utils.GetValuesWithRangeRetrieval(cachedEntry!, "(objectClass=*)", propertyList, SearchScope.Base);
            ArrayList siteLinkLists = (ArrayList)values[propertyName.ToLowerInvariant()]!;

            // somehow no site link list
            if (siteLinkLists == null)
                return;

            // construct the site link object
            for (int i = 0; i < siteLinkLists.Count; i++)
            {
                string dn = (string)siteLinkLists[i]!;
                // escaping manipulation
                pathCracker.Set(dn, NativeComInterfaces.ADS_SETTYPE_DN);
                string rdn = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_LEAF);
                Debug.Assert(rdn != null && Utils.Compare(rdn, 0, 3, "CN=", 0, 3) == 0);
                rdn = rdn.Substring(3);
                DirectoryEntry entry = DirectoryEntryManager.GetDirectoryEntry(context, dn);
                ActiveDirectorySiteLink link = new ActiveDirectorySiteLink(context, rdn, _transport, true, entry);

                _links.Add(link);
            }
        }
    }
}
