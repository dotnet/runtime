// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySiteLink : IDisposable
    {
        internal readonly DirectoryContext context = null;
        private readonly string _name = null;
        private readonly ActiveDirectoryTransportType _transport = ActiveDirectoryTransportType.Rpc;
        private bool _disposed = false;

        internal bool existing = false;
        internal readonly DirectoryEntry cachedEntry = null;
        private const int systemDefaultCost = 0;
        private readonly TimeSpan _systemDefaultInterval = new TimeSpan(0, 15, 0);
        private const int appDefaultCost = 100;
        private const int appDefaultInterval = 180;
        private readonly ActiveDirectorySiteCollection _sites = new ActiveDirectorySiteCollection();
        private bool _siteRetrieved = false;

        public ActiveDirectorySiteLink(DirectoryContext context, string siteLinkName) : this(context, siteLinkName, ActiveDirectoryTransportType.Rpc, null)
        {
        }

        public ActiveDirectorySiteLink(DirectoryContext context, string siteLinkName, ActiveDirectoryTransportType transport) : this(context, siteLinkName, transport, null)
        {
        }

        public ActiveDirectorySiteLink(DirectoryContext context, string siteLinkName, ActiveDirectoryTransportType transport, ActiveDirectorySchedule schedule)
        {
            ValidateArgument(context, siteLinkName, transport);

            //  work with copy of the context
            context = new DirectoryContext(context);

            this.context = context;
            _name = siteLinkName;
            _transport = transport;

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de;

            try
            {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext);

                string parentDN = null;
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
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet , context.Name));
            }

            try
            {
                string rdn = "cn=" + _name;
                rdn = Utils.GetEscapedPath(rdn);
                cachedEntry = de.Children.Add(rdn, "siteLink");
                cachedEntry.Properties["cost"].Value = appDefaultCost;
                cachedEntry.Properties["replInterval"].Value = appDefaultInterval;
                if (schedule != null)
                    cachedEntry.Properties["schedule"].Value = schedule.GetUnmanagedSchedule();
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

        internal ActiveDirectorySiteLink(DirectoryContext context, string siteLinkName, ActiveDirectoryTransportType transport, bool existing, DirectoryEntry entry)
        {
            this.context = context;
            _name = siteLinkName;
            _transport = transport;
            this.existing = existing;
            this.cachedEntry = entry;
        }

        public static ActiveDirectorySiteLink FindByName(DirectoryContext context, string siteLinkName)
        {
            return FindByName(context, siteLinkName, ActiveDirectoryTransportType.Rpc);
        }

        public static ActiveDirectorySiteLink FindByName(DirectoryContext context, string siteLinkName, ActiveDirectoryTransportType transport)
        {
            ValidateArgument(context, siteLinkName, transport);

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de;

            try
            {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext);
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
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet , context.Name));
            }

            try
            {
                ADSearcher adSearcher = new ADSearcher(de,
                                                      "(&(objectClass=siteLink)(objectCategory=SiteLink)(name=" + Utils.GetEscapedFilterValue(siteLinkName) + "))",
                                                      new string[] { "distinguishedName" },
                                                      SearchScope.OneLevel,
                                                      false, /* don't need paged search */
                                                      false /* don't need to cache result */
                                                      );
                SearchResult srchResult = adSearcher.FindOne();
                if (srchResult == null)
                {
                    // no such sitelink object
                    Exception e = new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySiteLink), siteLinkName);
                    throw e;
                }
                else
                {
                    DirectoryEntry connectionEntry = srchResult.GetDirectoryEntry();
                    // it is an existing site object
                    ActiveDirectorySiteLink link = new ActiveDirectorySiteLink(context, siteLinkName, transport, true, connectionEntry);
                    return link;
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
                        throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySiteLink), siteLinkName);
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

        public ActiveDirectoryTransportType TransportType
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _transport;
            }
        }

        public ActiveDirectorySiteCollection Sites
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (existing)
                {
                    // if asked the first time, we need to properly construct the site collection
                    if (!_siteRetrieved)
                    {
                        _sites.initialized = false;
                        _sites.Clear();
                        GetSites();
                        _siteRetrieved = true;
                    }
                }
                _sites.initialized = true;
                _sites.de = cachedEntry;
                _sites.context = context;
                return _sites;
            }
        }

        public int Cost
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    if (cachedEntry.Properties.Contains("cost"))
                        return (int)cachedEntry.Properties["cost"][0];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                // property is not set in the directory, we need to return the system default value
                return systemDefaultCost;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (value < 0)
                    throw new ArgumentException("value");

                try
                {
                    cachedEntry.Properties["cost"].Value = value;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public TimeSpan ReplicationInterval
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    if (cachedEntry.Properties.Contains("replInterval"))
                    {
                        int tmpValue = (int)cachedEntry.Properties["replInterval"][0];
                        return new TimeSpan(0, tmpValue, 0);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                return _systemDefaultInterval;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (value < TimeSpan.Zero)
                    throw new ArgumentException(SR.NoNegativeTime, "value");

                double tmpVal = value.TotalMinutes;
                if (tmpVal > Int32.MaxValue)
                    throw new ArgumentException(SR.ReplicationIntervalExceedMax, "value");

                int totalMinutes = (int)tmpVal;
                if (totalMinutes < tmpVal)
                    throw new ArgumentException(SR.ReplicationIntervalInMinutes, "value");

                try
                {
                    cachedEntry.Properties["replInterval"].Value = totalMinutes;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public bool ReciprocalReplicationEnabled
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int options = 0;
                PropertyValueCollection propValue = null;
                try
                {
                    propValue = cachedEntry.Properties["options"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (propValue.Count != 0)
                {
                    options = (int)propValue[0];
                }

                //NTDSSITELINK_OPT_TWOWAY_SYNC ( 1 << 1 )  force sync in opposite direction at end of sync
                if ((options & 0x2) == 0)
                    return false;
                else
                    return true;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int options = 0;
                PropertyValueCollection propValue = null;

                try
                {
                    propValue = cachedEntry.Properties["options"];

                    if (propValue.Count != 0)
                    {
                        options = (int)propValue[0];
                    }

                    //NTDSSITELINK_OPT_TWOWAY_SYNC ( 1 << 1 )  force sync in opposite direction at end of sync
                    if (value == true)
                    {
                        options |= 0x2;
                    }
                    else
                    {
                        options &= (~(0x2));
                    }
                    cachedEntry.Properties["options"].Value = options;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public bool NotificationEnabled
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int options = 0;
                PropertyValueCollection propValue = null;
                try
                {
                    propValue = cachedEntry.Properties["options"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (propValue.Count != 0)
                {
                    options = (int)propValue[0];
                }

                // NTDSSITELINK_OPT_USE_NOTIFY ( 1 << 0 )   Use notification on this link
                if ((options & 0x1) == 0)
                    return false;
                else
                    return true;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int options = 0;
                PropertyValueCollection propValue = null;

                try
                {
                    propValue = cachedEntry.Properties["options"];
                    if (propValue.Count != 0)
                    {
                        options = (int)propValue[0];
                    }

                    // NTDSSITELINK_OPT_USE_NOTIFY ( 1 << 0 )   Use notification on this link
                    if (value)
                    {
                        options |= 0x1;
                    }
                    else
                    {
                        options &= (~(0x1));
                    }

                    cachedEntry.Properties["options"].Value = options;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public bool DataCompressionEnabled
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int options = 0;
                PropertyValueCollection propValue = null;

                try
                {
                    propValue = cachedEntry.Properties["options"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (propValue.Count != 0)
                {
                    options = (int)propValue[0];
                }

                //NTDSSITELINK_OPT_DISABLE_COMPRESSION ( 1 << 2 )
                //  0 - Compression of replication data across this site link enabled
                //  1 - Compression of replication data across this site link disabled                
                if ((options & 0x4) == 0)
                    return true;
                else
                    return false;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                int options = 0;
                PropertyValueCollection propValue = null;

                try
                {
                    propValue = cachedEntry.Properties["options"];
                    if (propValue.Count != 0)
                    {
                        options = (int)propValue[0];
                    }

                    //NTDSSITELINK_OPT_DISABLE_COMPRESSION ( 1 << 2 )
                    //  0 - Compression of replication data across this site link enabled
                    //  1 - Compression of replication data across this site link disabled
                    if (value == false)
                    {
                        options |= 0x4;
                    }
                    else
                    {
                        options &= (~(0x4));
                    }
                    cachedEntry.Properties["options"].Value = options;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public ActiveDirectorySchedule InterSiteReplicationSchedule
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                ActiveDirectorySchedule schedule = null;
                try
                {
                    if (cachedEntry.Properties.Contains("schedule"))
                    {
                        byte[] tmpSchedule = (byte[])cachedEntry.Properties["schedule"][0];
                        Debug.Assert(tmpSchedule != null && tmpSchedule.Length == 188);
                        schedule = new ActiveDirectorySchedule();
                        schedule.SetUnmanagedSchedule(tmpSchedule);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                return schedule;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    if (value == null)
                    {
                        if (cachedEntry.Properties.Contains("schedule"))
                            cachedEntry.Properties["schedule"].Clear();
                    }
                    else
                    {
                        cachedEntry.Properties["schedule"].Value = value.GetUnmanagedSchedule();
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public void Save()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            try
            {
                cachedEntry.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            if (existing)
            {
                _siteRetrieved = false;
            }
            else
            {
                existing = true;
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
                    cachedEntry.Parent.Children.Remove(cachedEntry);
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
                if (cachedEntry != null)
                    cachedEntry.Dispose();
            }

            // free your own state (unmanaged objects)   

            _disposed = true;
        }

        private static void ValidateArgument(DirectoryContext context, string siteLinkName, ActiveDirectoryTransportType transport)
        {
            // basic validation first
            if (context == null)
                throw new ArgumentNullException("context");

            // if target is not specified, then we determin the target from the logon credential, so if it is a local user context, it should fail
            if ((context.Name == null) && (!context.isRootDomain()))
            {
                throw new ArgumentException(SR.ContextNotAssociatedWithDomain, "context");
            }

            // more validation for the context, if the target is not null, then it should be either forest name or server name
            if (context.Name != null)
            {
                if (!(context.isRootDomain() || context.isServer() || context.isADAMConfigSet()))
                    throw new ArgumentException(SR.NotADOrADAM, "context");
            }

            if (siteLinkName == null)
                throw new ArgumentNullException("siteLinkName");

            if (siteLinkName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "siteLinkName");

            if (transport < ActiveDirectoryTransportType.Rpc || transport > ActiveDirectoryTransportType.Smtp)
                throw new InvalidEnumArgumentException("value", (int)transport, typeof(ActiveDirectoryTransportType));
        }

        private void GetSites()
        {
            NativeComInterfaces.IAdsPathname pathCracker = null;
            pathCracker = (NativeComInterfaces.IAdsPathname)new NativeComInterfaces.Pathname();
            ArrayList propertyList = new ArrayList();
            // need to turn off the escaping for name
            pathCracker.EscapedMode = NativeComInterfaces.ADS_ESCAPEDMODE_OFF_EX;
            string propertyName = "siteList";

            propertyList.Add(propertyName);
            Hashtable values = Utils.GetValuesWithRangeRetrieval(cachedEntry, "(objectClass=*)", propertyList, SearchScope.Base);
            ArrayList siteLists = (ArrayList)values[propertyName.ToLower(CultureInfo.InvariantCulture)];

            // somehow no site list
            if (siteLists == null)
                return;

            for (int i = 0; i < siteLists.Count; i++)
            {
                string dn = (string)siteLists[i];

                // escaping manipulation
                pathCracker.Set(dn, NativeComInterfaces.ADS_SETTYPE_DN);
                string rdn = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_LEAF);
                Debug.Assert(rdn != null && Utils.Compare(rdn, 0, 3, "CN=", 0, 3) == 0);
                rdn = rdn.Substring(3);
                ActiveDirectorySite site = new ActiveDirectorySite(context, rdn, true);

                // add to the collection
                _sites.Add(site);
            }
        }
    }
}
