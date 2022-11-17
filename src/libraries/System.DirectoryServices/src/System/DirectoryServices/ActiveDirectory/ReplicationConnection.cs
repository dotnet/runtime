// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;

namespace System.DirectoryServices.ActiveDirectory
{
    public enum NotificationStatus
    {
        NoNotification = 0,
        IntraSiteOnly = 1,
        NotificationAlways = 2
    }

    public enum ReplicationSpan
    {
        IntraSite = 0,
        InterSite = 1
    }

    public class ReplicationConnection : IDisposable
    {
        internal readonly DirectoryContext context;
        internal readonly DirectoryEntry cachedDirectoryEntry;
        internal bool existingConnection;
        private bool _disposed;
        private bool _isADAMServer;
        private int _options;

        private readonly string _connectionName;
        private string? _sourceServerName;
        private string? _destinationServerName;
        private readonly ActiveDirectoryTransportType _transport = ActiveDirectoryTransportType.Rpc;

        private const string ADAMGuid = "1.2.840.113556.1.4.1851";

        public static ReplicationConnection FindByName(DirectoryContext context, string name)
        {
            ValidateArgument(context, name);

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the rootdse to get the servername property
            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            try
            {
                string serverDN = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ServerName)!;
                string connectionContainer = "CN=NTDS Settings," + serverDN;
                de = DirectoryEntryManager.GetDirectoryEntry(context, connectionContainer);
                // doing the search to find the connection object based on its name
                ADSearcher adSearcher = new ADSearcher(de,
                                                      "(&(objectClass=nTDSConnection)(objectCategory=NTDSConnection)(name=" + Utils.GetEscapedFilterValue(name) + "))",
                                                      new string[] { "distinguishedName" },
                                                      SearchScope.OneLevel,
                                                      false, /* no paged search */
                                                      false /* don't cache results */);
                SearchResult? srchResult = null;
                try
                {
                    srchResult = adSearcher.FindOne();
                }
                catch (COMException e)
                {
                    if (e.ErrorCode == unchecked((int)0x80072030))
                    {
                        // object is not found since we cannot even find the container in which to search
                        throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ReplicationConnection), name);
                    }
                    else
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }

                if (srchResult == null)
                {
                    // no such connection object
                    Exception e = new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ReplicationConnection), name);
                    throw e;
                }
                else
                {
                    DirectoryEntry connectionEntry = srchResult.GetDirectoryEntry();
                    return new ReplicationConnection(context, connectionEntry, name);
                }
            }
            finally
            {
                de.Dispose();
            }
        }

        internal ReplicationConnection(DirectoryContext context, DirectoryEntry connectionEntry, string name)
        {
            this.context = context;
            cachedDirectoryEntry = connectionEntry;
            _connectionName = name;
            // this is an existing connection object
            existingConnection = true;
        }

        public ReplicationConnection(DirectoryContext context, string name, DirectoryServer sourceServer) : this(context, name, sourceServer, null, ActiveDirectoryTransportType.Rpc)
        {
        }

        public ReplicationConnection(DirectoryContext context, string name, DirectoryServer sourceServer, ActiveDirectorySchedule? schedule) : this(context, name, sourceServer, schedule, ActiveDirectoryTransportType.Rpc)
        {
        }

        public ReplicationConnection(DirectoryContext context, string name, DirectoryServer sourceServer, ActiveDirectoryTransportType transport) : this(context, name, sourceServer, null, transport)
        {
        }

        public ReplicationConnection(DirectoryContext context, string name, DirectoryServer sourceServer, ActiveDirectorySchedule? schedule, ActiveDirectoryTransportType transport)
        {
            ValidateArgument(context, name);

            if (sourceServer == null)
                throw new ArgumentNullException(nameof(sourceServer));

            if (transport < ActiveDirectoryTransportType.Rpc || transport > ActiveDirectoryTransportType.Smtp)
                throw new InvalidEnumArgumentException("value", (int)transport, typeof(ActiveDirectoryTransportType));

            //  work with copy of the context
            context = new DirectoryContext(context);

            ValidateTargetAndSourceServer(context, sourceServer);

            this.context = context;
            _connectionName = name;
            _transport = transport;

            DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            try
            {
                string serverDN = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ServerName)!;
                string connectionContainer = "CN=NTDS Settings," + serverDN;
                de = DirectoryEntryManager.GetDirectoryEntry(context, connectionContainer);

                // create the connection entry
                string rdn = "cn=" + _connectionName;
                rdn = Utils.GetEscapedPath(rdn);
                cachedDirectoryEntry = de.Children.Add(rdn, "nTDSConnection");

                // set all the properties

                // sourceserver property
                DirectoryContext sourceServerContext = sourceServer.Context;
                de = DirectoryEntryManager.GetDirectoryEntry(sourceServerContext, WellKnownDN.RootDSE);
                string serverName = (string)PropertyManager.GetPropertyValue(sourceServerContext, de, PropertyManager.ServerName)!;
                serverName = "CN=NTDS Settings," + serverName;

                cachedDirectoryEntry.Properties["fromServer"].Add(serverName);

                // schedule property
                if (schedule != null)
                    cachedDirectoryEntry.Properties[nameof(schedule)].Value = schedule.GetUnmanagedSchedule();

                // transporttype property
                string transportPath = Utils.GetDNFromTransportType(TransportType, context);
                // verify that the transport is supported
                de = DirectoryEntryManager.GetDirectoryEntry(context, transportPath);
                try
                {
                    de.Bind(true);
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

                cachedDirectoryEntry.Properties["transportType"].Add(transportPath);

                // enabledConnection property
                cachedDirectoryEntry.Properties["enabledConnection"].Value = false;

                // options
                cachedDirectoryEntry.Properties["options"].Value = 0;
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                de.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && cachedDirectoryEntry != null)
                    cachedDirectoryEntry.Dispose();

                _disposed = true;
            }
        }

        ~ReplicationConnection()
        {
            Dispose(false);      // finalizer is called => Dispose has not been called yet.
        }

        public string Name
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _connectionName;
            }
        }

        public string? SourceServer
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                // get the source server
                if (_sourceServerName == null)
                {
                    string sourceServerDN = (string)PropertyManager.GetPropertyValue(context, cachedDirectoryEntry, PropertyManager.FromServer)!;
                    DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, sourceServerDN);
                    if (IsADAM)
                    {
                        int portnumber = (int)PropertyManager.GetPropertyValue(context, de, PropertyManager.MsDSPortLDAP)!;
                        string? tmpServerName = (string?)PropertyManager.GetPropertyValue(context, de.Parent, PropertyManager.DnsHostName);
                        if (portnumber != 389)
                        {
                            _sourceServerName = tmpServerName + ":" + portnumber;
                        }
                    }
                    else
                    {
                        _sourceServerName = (string?)PropertyManager.GetPropertyValue(context, de.Parent, PropertyManager.DnsHostName);
                    }
                }
                return _sourceServerName;
            }
        }

        public string DestinationServer
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (_destinationServerName == null)
                {
                    DirectoryEntry? NTDSObject = null;
                    DirectoryEntry? serverObject = null;
                    try
                    {
                        NTDSObject = cachedDirectoryEntry.Parent;
                        serverObject = NTDSObject.Parent;
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    string hostName = (string)PropertyManager.GetPropertyValue(context, serverObject, PropertyManager.DnsHostName)!;
                    if (IsADAM)
                    {
                        int portnumber = (int)PropertyManager.GetPropertyValue(context, NTDSObject, PropertyManager.MsDSPortLDAP)!;
                        if (portnumber != 389)
                        {
                            _destinationServerName = hostName + ":" + portnumber;
                        }
                        else
                            _destinationServerName = hostName;
                    }
                    else
                        _destinationServerName = hostName;
                }
                return _destinationServerName;
            }
        }

        public bool Enabled
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                // fetch the property value
                try
                {
                    if (cachedDirectoryEntry.Properties.Contains("enabledConnection"))
                        return (bool)cachedDirectoryEntry.Properties["enabledConnection"][0]!;
                    else
                        return false;
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
                    cachedDirectoryEntry.Properties["enabledConnection"].Value = value;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public ActiveDirectoryTransportType TransportType
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                // for exisint connection, we need to check its property, for newly created and not committed one, we just return
                // the member variable value directly
                if (existingConnection)
                {
                    PropertyValueCollection? propValue = null;
                    try
                    {
                        propValue = cachedDirectoryEntry.Properties["transportType"];
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }

                    if (propValue.Count == 0)
                    {
                        // if the property does not exist, then default is to RPC over IP
                        return ActiveDirectoryTransportType.Rpc;
                    }
                    else
                    {
                        return Utils.GetTransportTypeFromDN((string)propValue[0]!);
                    }
                }
                else
                    return _transport;
            }
        }

        public bool GeneratedByKcc
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                PropertyValueCollection? propValue = null;

                try
                {
                    propValue = cachedDirectoryEntry.Properties["options"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (propValue.Count == 0)
                {
                    _options = 0;
                }
                else
                {
                    _options = (int)propValue[0]!;
                }

                // NTDSCONN_OPT_IS_GENERATED ( 1 << 0 )   object generated by DS, not admin
                if ((_options & 0x1) == 0)
                    return false;
                else
                    return true;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    PropertyValueCollection propValue = cachedDirectoryEntry.Properties["options"];
                    if (propValue.Count == 0)
                    {
                        _options = 0;
                    }
                    else
                    {
                        _options = (int)propValue[0]!;
                    }

                    // NTDSCONN_OPT_IS_GENERATED ( 1 << 0 )   object generated by DS, not admin
                    if (value)
                    {
                        _options |= 0x1;
                    }
                    else
                    {
                        _options &= (~(0x1));
                    }

                    // put the value into cache
                    cachedDirectoryEntry.Properties["options"].Value = _options;
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

                PropertyValueCollection? propValue = null;
                try
                {
                    propValue = cachedDirectoryEntry.Properties["options"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (propValue.Count == 0)
                {
                    // if the property does not exist, then default is to RPC over IP
                    _options = 0;
                }
                else
                {
                    _options = (int)propValue[0]!;
                }

                // NTDSCONN_OPT_TWOWAY_SYNC  ( 1 << 1 )  force sync in opposite direction at end of sync
                if ((_options & 0x2) == 0)
                    return false;
                else
                    return true;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    PropertyValueCollection propValue = cachedDirectoryEntry.Properties["options"];
                    if (propValue.Count == 0)
                    {
                        _options = 0;
                    }
                    else
                    {
                        _options = (int)propValue[0]!;
                    }

                    // NTDSCONN_OPT_TWOWAY_SYNC  ( 1 << 1 )  force sync in opposite direction at end of sync
                    if (value)
                    {
                        _options |= 0x2;
                    }
                    else
                    {
                        _options &= (~(0x2));
                    }

                    // put the value into cache
                    cachedDirectoryEntry.Properties["options"].Value = _options;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public NotificationStatus ChangeNotificationStatus
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                PropertyValueCollection? propValue = null;
                try
                {
                    propValue = cachedDirectoryEntry.Properties["options"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (propValue.Count == 0)
                {
                    // if the property does not exist, then default is to RPC over IP
                    _options = 0;
                }
                else
                {
                    _options = (int)propValue[0]!;
                }

                int overrideNotify = _options & 0x4;
                int userNotify = _options & 0x8;
                if (overrideNotify == 0x4 && userNotify == 0)
                    return NotificationStatus.NoNotification;
                else if (overrideNotify == 0x4 && userNotify == 0x8)
                    return NotificationStatus.NotificationAlways;
                else
                    return NotificationStatus.IntraSiteOnly;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (value < NotificationStatus.NoNotification || value > NotificationStatus.NotificationAlways)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(NotificationStatus));

                try
                {
                    PropertyValueCollection propValue = cachedDirectoryEntry.Properties["options"];
                    if (propValue.Count == 0)
                    {
                        _options = 0;
                    }
                    else
                    {
                        _options = (int)propValue[0]!;
                    }

                    if (value == NotificationStatus.IntraSiteOnly)
                    {
                        _options &= (~(0x4));
                        _options &= (~(0x8));
                    }
                    else if (value == NotificationStatus.NoNotification)
                    {
                        _options |= (0x4);
                        _options &= (~(0x8));
                    }
                    else
                    {
                        _options |= (0x4);
                        _options |= (0x8);
                    }

                    // put the value into cache
                    cachedDirectoryEntry.Properties["options"].Value = _options;
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

                PropertyValueCollection? propValue = null;
                try
                {
                    propValue = cachedDirectoryEntry.Properties["options"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (propValue.Count == 0)
                {
                    // if the property does not exist, then default is to RPC over IP
                    _options = 0;
                }
                else
                {
                    _options = (int)propValue[0]!;
                }

                //NTDSCONN_OPT_DISABLE_INTERSITE_COMPRESSION    (1 << 4)
                //  0 - Compression of replication data enabled
                //  1 - Compression of replication data disabled
                if ((_options & 0x10) == 0)
                    return true;
                else
                    return false;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    PropertyValueCollection propValue = cachedDirectoryEntry.Properties["options"];
                    if (propValue.Count == 0)
                    {
                        _options = 0;
                    }
                    else
                    {
                        _options = (int)propValue[0]!;
                    }

                    //NTDSCONN_OPT_DISABLE_INTERSITE_COMPRESSION    (1 << 4)
                    //  0 - Compression of replication data enabled
                    //  1 - Compression of replication data disabled
                    if (value == false)
                    {
                        _options |= 0x10;
                    }
                    else
                    {
                        _options &= (~(0x10));
                    }

                    // put the value into cache
                    cachedDirectoryEntry.Properties["options"].Value = _options;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public bool ReplicationScheduleOwnedByUser
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                PropertyValueCollection? propValue = null;
                try
                {
                    propValue = cachedDirectoryEntry.Properties["options"];
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (propValue.Count == 0)
                {
                    // if the property does not exist, then default is to RPC over IP
                    _options = 0;
                }
                else
                {
                    _options = (int)propValue[0]!;
                }

                // NTDSCONN_OPT_USER_OWNED_SCHEDULE (1 << 5)
                if ((_options & 0x20) == 0)
                    return false;
                else
                    return true;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    PropertyValueCollection propValue = cachedDirectoryEntry.Properties["options"];
                    if (propValue.Count == 0)
                    {
                        _options = 0;
                    }
                    else
                    {
                        _options = (int)propValue[0]!;
                    }

                    // NTDSCONN_OPT_USER_OWNED_SCHEDULE (1 << 5)
                    if (value)
                    {
                        _options |= 0x20;
                    }
                    else
                    {
                        _options &= (~(0x20));
                    }

                    // put the value into cache
                    cachedDirectoryEntry.Properties["options"].Value = _options;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public ReplicationSpan ReplicationSpan
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                // find out whether the site and the destination is in the same site
                string destinationPath = (string)PropertyManager.GetPropertyValue(context, cachedDirectoryEntry, PropertyManager.FromServer)!;
                string? destinationSite = Utils.GetDNComponents(destinationPath)[3].Value;

                DirectoryEntry de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string serverDN = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ServerName)!;
                string? serverSite = Utils.GetDNComponents(serverDN)[2].Value;

                if (Utils.Compare(destinationSite, serverSite) == 0)
                {
                    return ReplicationSpan.IntraSite;
                }
                else
                {
                    return ReplicationSpan.InterSite;
                }
            }
        }

        public ActiveDirectorySchedule? ReplicationSchedule
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                ActiveDirectorySchedule? schedule = null;
                bool scheduleExists = false;
                try
                {
                    scheduleExists = cachedDirectoryEntry.Properties.Contains("schedule");
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }

                if (scheduleExists)
                {
                    byte[] tmpSchedule = (byte[])cachedDirectoryEntry.Properties["schedule"][0]!;
                    Debug.Assert(tmpSchedule != null && tmpSchedule.Length == 188);
                    schedule = new ActiveDirectorySchedule();
                    schedule.SetUnmanagedSchedule(tmpSchedule);
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
                        if (cachedDirectoryEntry.Properties.Contains("schedule"))
                            cachedDirectoryEntry.Properties["schedule"].Clear();
                    }
                    else
                    {
                        cachedDirectoryEntry.Properties["schedule"].Value = value.GetUnmanagedSchedule();
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
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

                if (values.Contains(ADAMGuid))
                    _isADAMServer = true;

                return _isADAMServer;
            }
        }

        public void Delete()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!existingConnection)
            {
                throw new InvalidOperationException(SR.CannotDelete);
            }
            else
            {
                try
                {
                    cachedDirectoryEntry.Parent.Children.Remove(cachedDirectoryEntry);
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
                cachedDirectoryEntry.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            if (!existingConnection)
            {
                existingConnection = true;
            }
        }

        public override string ToString()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            return Name;
        }

        public DirectoryEntry GetDirectoryEntry()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!existingConnection)
            {
                throw new InvalidOperationException(SR.CannotGetObject);
            }
            else
            {
                return DirectoryEntryManager.GetDirectoryEntryInternal(context, cachedDirectoryEntry.Path);
            }
        }

        private static void ValidateArgument(DirectoryContext context, string name)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // the target of the scope must be server
            if (context.Name == null || !context.isServer())
                throw new ArgumentException(SR.DirectoryContextNeedHost);

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(name));
        }

        private void ValidateTargetAndSourceServer(DirectoryContext context, DirectoryServer sourceServer)
        {
            bool targetIsDC = false;
            DirectoryEntry? targetDE = null;
            DirectoryEntry? sourceDE = null;

            // first find out target is a dc or ADAM instance
            targetDE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            try
            {
                if (Utils.CheckCapability(targetDE, Capability.ActiveDirectory))
                {
                    targetIsDC = true;
                }
                else if (!Utils.CheckCapability(targetDE, Capability.ActiveDirectoryApplicationMode))
                {
                    // if it is also not an ADAM instance, it is invalid then
                    throw new ArgumentException(SR.DirectoryContextNeedHost, nameof(context));
                }

                if (targetIsDC && !(sourceServer is DomainController))
                {
                    // target and sourceServer are not of the same type
                    throw new ArgumentException(SR.ConnectionSourcServerShouldBeDC, nameof(sourceServer));
                }
                else if (!targetIsDC && (sourceServer is DomainController))
                {
                    // target and sourceServer are not of the same type
                    throw new ArgumentException(SR.ConnectionSourcServerShouldBeADAM, nameof(sourceServer));
                }

                sourceDE = DirectoryEntryManager.GetDirectoryEntry(sourceServer.Context, WellKnownDN.RootDSE);

                // now if they are both dc, we need to check whether they come from the same forest
                if (targetIsDC)
                {
                    string? targetRoot = (string?)PropertyManager.GetPropertyValue(context, targetDE, PropertyManager.RootDomainNamingContext);
                    string? sourceRoot = (string?)PropertyManager.GetPropertyValue(sourceServer.Context, sourceDE, PropertyManager.RootDomainNamingContext);
                    if (Utils.Compare(targetRoot, sourceRoot) != 0)
                    {
                        throw new ArgumentException(SR.ConnectionSourcServerSameForest, nameof(sourceServer));
                    }
                }
                else
                {
                    string? targetRoot = (string?)PropertyManager.GetPropertyValue(context, targetDE, PropertyManager.ConfigurationNamingContext);
                    string? sourceRoot = (string?)PropertyManager.GetPropertyValue(sourceServer.Context, sourceDE, PropertyManager.ConfigurationNamingContext);
                    if (Utils.Compare(targetRoot, sourceRoot) != 0)
                    {
                        throw new ArgumentException(SR.ConnectionSourcServerSameConfigSet, nameof(sourceServer));
                    }
                }
            }
            catch (COMException e)
            {
                ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                targetDE?.Close();
                sourceDE?.Close();
            }
        }
    }
}
