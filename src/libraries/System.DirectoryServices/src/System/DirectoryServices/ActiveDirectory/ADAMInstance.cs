// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class AdamInstance : DirectoryServer
    {
        private readonly string[] _becomeRoleOwnerAttrs = null;
        private bool _disposed = false;

        // for public properties
        private string _cachedHostName = null;
        private int _cachedLdapPort = -1;
        private int _cachedSslPort = -1;
        private bool _defaultPartitionInitialized = false;
        private bool _defaultPartitionModified = false;
        private ConfigurationSet _currentConfigSet = null;
        private string _cachedDefaultPartition = null;
        private AdamRoleCollection _cachedRoles = null;

        private IntPtr _ADAMHandle = (IntPtr)0;
        private IntPtr _authIdentity = IntPtr.Zero;
        private SyncUpdateCallback _userDelegate = null;
        private readonly SyncReplicaFromAllServersCallback _syncAllFunctionPointer = null;

        #region constructors
        internal AdamInstance(DirectoryContext context, string adamInstanceName)
            : this(context, adamInstanceName, new DirectoryEntryManager(context), true)
        {
        }

        internal AdamInstance(DirectoryContext context, string adamInstanceName, DirectoryEntryManager directoryEntryMgr, bool nameIncludesPort)
        {
            this.context = context;
            this.replicaName = adamInstanceName;
            this.directoryEntryMgr = directoryEntryMgr;

            // initialize the transfer role owner attributes
            _becomeRoleOwnerAttrs = new String[2];
            _becomeRoleOwnerAttrs[0] = PropertyManager.BecomeSchemaMaster;
            _becomeRoleOwnerAttrs[1] = PropertyManager.BecomeDomainMaster;

            // initialize the callback function
            _syncAllFunctionPointer = new SyncReplicaFromAllServersCallback(SyncAllCallbackRoutine);
        }

        internal AdamInstance(DirectoryContext context, string adamHostName, DirectoryEntryManager directoryEntryMgr)
        {
            this.context = context;

            // the replica name should be in the form dnshostname:port
            this.replicaName = adamHostName;
            string portNumber;
            Utils.SplitServerNameAndPortNumber(context.Name, out portNumber);
            if (portNumber != null)
            {
                this.replicaName = this.replicaName + ":" + portNumber;
            }

            // initialize the directory entry manager
            this.directoryEntryMgr = directoryEntryMgr;

            // initialize the transfer role owner attributes
            _becomeRoleOwnerAttrs = new String[2];
            _becomeRoleOwnerAttrs[0] = PropertyManager.BecomeSchemaMaster;
            _becomeRoleOwnerAttrs[1] = PropertyManager.BecomeDomainMaster;

            // initialize the callback function
            _syncAllFunctionPointer = new SyncReplicaFromAllServersCallback(SyncAllCallbackRoutine);
        }
        #endregion constructors

        #region IDisposable

        ~AdamInstance()
        {
            // finalizer is called => Dispose has not been called yet.
            Dispose(false);
        }

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
                    FreeADAMHandle();
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

        public static AdamInstance GetAdamInstance(DirectoryContext context)
        {
            DirectoryEntryManager directoryEntryMgr = null;
            string dnsHostName = null;

            // check that the context is not null
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            // contexttype should be DirectoryServer
            if (context.ContextType != DirectoryContextType.DirectoryServer)
            {
                throw new ArgumentException(SR.TargetShouldBeADAMServer, "context");
            }

            // target must be a server
            if ((!context.isServer()))
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.AINotFound , context.Name), typeof(AdamInstance), context.Name);
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the given adam instance
            try
            {
                directoryEntryMgr = new DirectoryEntryManager(context);
                DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);

                // This will ensure that we are talking to ADAM instance only
                if (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectoryApplicationMode))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.AINotFound , context.Name), typeof(AdamInstance), context.Name);
                }
                dnsHostName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DnsHostName);
            }
            catch (COMException e)
            {
                int errorCode = e.ErrorCode;

                if (errorCode == unchecked((int)0x8007203a))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.AINotFound , context.Name), typeof(AdamInstance), context.Name);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            return new AdamInstance(context, dnsHostName, directoryEntryMgr);
        }

        public static AdamInstance FindOne(DirectoryContext context, string partitionName)
        {
            // validate parameters (partitionName validated by the call to ConfigSet)
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            // contexttype should be ConfigurationSet
            if (context.ContextType != DirectoryContextType.ConfigurationSet)
            {
                throw new ArgumentException(SR.TargetShouldBeConfigSet, "context");
            }

            if (partitionName == null)
            {
                throw new ArgumentNullException("partitionName");
            }

            if (partitionName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, "partitionName");
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            return ConfigurationSet.FindOneAdamInstance(context, partitionName, null);
        }

        public static AdamInstanceCollection FindAll(DirectoryContext context, string partitionName)
        {
            AdamInstanceCollection adamInstanceCollection = null;

            // validate parameters (partitionName validated by the call to ConfigSet)
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            // contexttype should be ConfigurationSet
            if (context.ContextType != DirectoryContextType.ConfigurationSet)
            {
                throw new ArgumentException(SR.TargetShouldBeConfigSet, "context");
            }

            if (partitionName == null)
            {
                throw new ArgumentNullException("partitionName");
            }

            if (partitionName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, "partitionName");
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            try
            {
                adamInstanceCollection = ConfigurationSet.FindAdamInstances(context, partitionName, null);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this is the case where  we could not find an ADAM instance in that config set (return empty collection)
                adamInstanceCollection = new AdamInstanceCollection(new ArrayList());
            }

            return adamInstanceCollection;
        }

        public void TransferRoleOwnership(AdamRole role)
        {
            CheckIfDisposed();

            if (role < AdamRole.SchemaRole || role > AdamRole.NamingRole)
            {
                throw new InvalidEnumArgumentException("role", (int)role, typeof(AdamRole));
            }

            // set the appropriate attribute on the root dse 
            try
            {
                DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                rootDSE.Properties[_becomeRoleOwnerAttrs[(int)role]].Value = 1;
                rootDSE.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            // invalidate the role collection so that it gets loaded again next time
            _cachedRoles = null;
        }

        public void SeizeRoleOwnership(AdamRole role)
        {
            // set the "fsmoRoleOwner" attribute on the appropriate role object
            // to the NTDSAObjectName of this ADAM Instance
            string roleObjectDN = null;

            CheckIfDisposed();

            switch (role)
            {
                case AdamRole.SchemaRole:
                    {
                        roleObjectDN = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext);
                        break;
                    }
                case AdamRole.NamingRole:
                    {
                        roleObjectDN = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer);
                        break;
                    }
                default:
                    {
                        throw new InvalidEnumArgumentException("role", (int)role, typeof(AdamRole));
                    }
            }

            DirectoryEntry roleObjectEntry = null;
            try
            {
                roleObjectEntry = DirectoryEntryManager.GetDirectoryEntry(context, roleObjectDN);
                roleObjectEntry.Properties[PropertyManager.FsmoRoleOwner].Value = NtdsaObjectName;
                roleObjectEntry.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                if (roleObjectEntry != null)
                {
                    roleObjectEntry.Dispose();
                }
            }

            // invalidate the role collection so that it gets loaded again next time
            _cachedRoles = null;
        }

        public override void CheckReplicationConsistency()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the handle
            GetADAMHandle();

            // call private helper function
            CheckConsistencyHelper(_ADAMHandle, DirectoryContext.ADAMHandle);
        }

        public override ReplicationCursorCollection GetReplicationCursors(string partition)
        {
            IntPtr info = (IntPtr)0;
            int context = 0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException("partition");

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "partition");

            // get the handle
            GetADAMHandle();
            info = GetReplicationInfoHelper(_ADAMHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_CURSORS_3_FOR_NC, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_CURSORS_FOR_NC, partition, ref advanced, context, DirectoryContext.ADAMHandle);
            return ConstructReplicationCursors(_ADAMHandle, advanced, info, partition, this, DirectoryContext.ADAMHandle);
        }

        public override ReplicationOperationInformation GetReplicationOperationInformation()
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the handle
            GetADAMHandle();
            info = GetReplicationInfoHelper(_ADAMHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_PENDING_OPS, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_PENDING_OPS, null, ref advanced, 0, DirectoryContext.ADAMHandle);
            return ConstructPendingOperations(info, this, DirectoryContext.ADAMHandle);
        }

        public override ReplicationNeighborCollection GetReplicationNeighbors(string partition)
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException("partition");

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "partition");

            // get the handle
            GetADAMHandle();
            info = GetReplicationInfoHelper(_ADAMHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_NEIGHBORS, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_NEIGHBORS, partition, ref advanced, 0, DirectoryContext.ADAMHandle);
            return ConstructNeighbors(info, this, DirectoryContext.ADAMHandle);
        }

        public override ReplicationNeighborCollection GetAllReplicationNeighbors()
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the handle
            GetADAMHandle();
            info = GetReplicationInfoHelper(_ADAMHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_NEIGHBORS, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_NEIGHBORS, null, ref advanced, 0, DirectoryContext.ADAMHandle);
            return ConstructNeighbors(info, this, DirectoryContext.ADAMHandle);
        }

        public override ReplicationFailureCollection GetReplicationConnectionFailures()
        {
            return GetReplicationFailures(DS_REPL_INFO_TYPE.DS_REPL_INFO_KCC_DSA_CONNECT_FAILURES);
        }

        public override ActiveDirectoryReplicationMetadata GetReplicationMetadata(string objectPath)
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (objectPath == null)
                throw new ArgumentNullException("objectPath");

            if (objectPath.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "objectPath");

            // get the handle
            GetADAMHandle();
            info = GetReplicationInfoHelper(_ADAMHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_METADATA_2_FOR_OBJ, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_METADATA_FOR_OBJ, objectPath, ref advanced, 0, DirectoryContext.ADAMHandle);
            return ConstructMetaData(advanced, info, this, DirectoryContext.ADAMHandle);
        }

        public override void SyncReplicaFromServer(string partition, string sourceServer)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException("partition");

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "partition");

            if (sourceServer == null)
                throw new ArgumentNullException("sourceServer");

            if (sourceServer.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "sourceServer");

            // get the dsHandle
            GetADAMHandle();
            SyncReplicaHelper(_ADAMHandle, true, partition, sourceServer, 0, DirectoryContext.ADAMHandle);
        }

        public override void TriggerSyncReplicaFromNeighbors(string partition)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException("partition");

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "partition");

            // get the dsHandle
            GetADAMHandle();
            SyncReplicaHelper(_ADAMHandle, true, partition, null, DS_REPSYNC_ASYNCHRONOUS_OPERATION | DS_REPSYNC_ALL_SOURCES, DirectoryContext.ADAMHandle);
        }

        public override void SyncReplicaFromAllServers(string partition, SyncFromAllServersOptions options)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException("partition");

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, "partition");

            // get the dsHandle
            GetADAMHandle();
            SyncReplicaAllHelper(_ADAMHandle, _syncAllFunctionPointer, partition, options, SyncFromAllServersCallback, DirectoryContext.ADAMHandle);
        }

        public void Save()
        {
            CheckIfDisposed();

            // only thing to be saved is the ntdsa entry (for default partition)
            if (_defaultPartitionModified)
            {
                DirectoryEntry ntdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);
                try
                {
                    ntdsaEntry.CommitChanges();
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            // reset the bools associated with this property
            _defaultPartitionInitialized = false;
            _defaultPartitionModified = false;
        }

        #endregion public methods

        #region public properties

        public ConfigurationSet ConfigurationSet
        {
            get
            {
                CheckIfDisposed();
                if (_currentConfigSet == null)
                {
                    DirectoryContext configSetContext = Utils.GetNewDirectoryContext(Name, DirectoryContextType.DirectoryServer, context);
                    _currentConfigSet = ConfigurationSet.GetConfigurationSet(configSetContext);
                }
                return _currentConfigSet;
            }
        }

        public string HostName
        {
            get
            {
                CheckIfDisposed();
                if (_cachedHostName == null)
                {
                    DirectoryEntry serverEntry = directoryEntryMgr.GetCachedDirectoryEntry(ServerObjectName);
                    _cachedHostName = (string)PropertyManager.GetPropertyValue(context, serverEntry, PropertyManager.DnsHostName);
                }
                return _cachedHostName;
            }
        }

        public int LdapPort
        {
            get
            {
                CheckIfDisposed();
                if (_cachedLdapPort == -1)
                {
                    DirectoryEntry ntdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);
                    _cachedLdapPort = (int)PropertyManager.GetPropertyValue(context, ntdsaEntry, PropertyManager.MsDSPortLDAP);
                }
                return _cachedLdapPort;
            }
        }

        public int SslPort
        {
            get
            {
                CheckIfDisposed();
                if (_cachedSslPort == -1)
                {
                    DirectoryEntry ntdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);
                    _cachedSslPort = (int)PropertyManager.GetPropertyValue(context, ntdsaEntry, PropertyManager.MsDSPortSSL);
                }
                return _cachedSslPort;
            }
        }

        // Abstract Properties

        public AdamRoleCollection Roles
        {
            get
            {
                CheckIfDisposed();
                DirectoryEntry schemaEntry = null;
                DirectoryEntry partitionsEntry = null;

                try
                {
                    if (_cachedRoles == null)
                    {
                        // check for the fsmoRoleOwner attribute on the Config and Schema objects
                        ArrayList roleList = new ArrayList();
                        schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext));

                        if (NtdsaObjectName.Equals((string)PropertyManager.GetPropertyValue(context, schemaEntry, PropertyManager.FsmoRoleOwner)))
                        {
                            roleList.Add(AdamRole.SchemaRole);
                        }

                        partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));

                        if (NtdsaObjectName.Equals((string)PropertyManager.GetPropertyValue(context, partitionsEntry, PropertyManager.FsmoRoleOwner)))
                        {
                            roleList.Add(AdamRole.NamingRole);
                        }

                        _cachedRoles = new AdamRoleCollection(roleList);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
                finally
                {
                    if (schemaEntry != null)
                    {
                        schemaEntry.Dispose();
                    }
                    if (partitionsEntry != null)
                    {
                        partitionsEntry.Dispose();
                    }
                }
                return _cachedRoles;
            }
        }

        public string DefaultPartition
        {
            get
            {
                CheckIfDisposed();
                if (!_defaultPartitionInitialized || _defaultPartitionModified)
                {
                    DirectoryEntry ntdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);
                    try
                    {
                        ntdsaEntry.RefreshCache();
                        if (ntdsaEntry.Properties[PropertyManager.MsDSDefaultNamingContext].Value == null)
                        {
                            // property has not been set 
                            _cachedDefaultPartition = null;
                        }
                        else
                        {
                            _cachedDefaultPartition = (string)PropertyManager.GetPropertyValue(context, ntdsaEntry, PropertyManager.MsDSDefaultNamingContext);
                        }
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    _defaultPartitionInitialized = true;
                }
                return _cachedDefaultPartition;
            }
            set
            {
                CheckIfDisposed();

                DirectoryEntry ntdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);
                if (value == null)
                {
                    if (ntdsaEntry.Properties.Contains(PropertyManager.MsDSDefaultNamingContext))
                    {
                        ntdsaEntry.Properties[PropertyManager.MsDSDefaultNamingContext].Clear();
                    }
                }
                else
                {
                    //
                    // should be in DN format 
                    //
                    if (!Utils.IsValidDNFormat(value))
                    {
                        throw new ArgumentException(SR.InvalidDNFormat, "value");
                    }

                    // this value should be one of partitions currently being hosted by
                    // this adam instance
                    if (!Partitions.Contains(value))
                    {
                        throw new ArgumentException(SR.Format(SR.ServerNotAReplica , value), "value");
                    }
                    ntdsaEntry.Properties[PropertyManager.MsDSDefaultNamingContext].Value = value;
                }
                _defaultPartitionModified = true;
            }
        }

        public override string IPAddress
        {
            get
            {
                CheckIfDisposed();

                IPHostEntry hostEntry = Dns.GetHostEntry(HostName);
                if (hostEntry.AddressList.GetLength(0) > 0)
                {
                    return (hostEntry.AddressList[0]).ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        public override String SiteName
        {
            get
            {
                CheckIfDisposed();
                if (cachedSiteName == null)
                {
                    DirectoryEntry siteEntry = DirectoryEntryManager.GetDirectoryEntry(context, SiteObjectName);
                    try
                    {
                        cachedSiteName = (string)PropertyManager.GetPropertyValue(context, siteEntry, PropertyManager.Cn);
                    }
                    finally
                    {
                        siteEntry.Dispose();
                    }
                }
                return cachedSiteName;
            }
        }

        internal String SiteObjectName
        {
            get
            {
                CheckIfDisposed();
                if (cachedSiteObjectName == null)
                {
                    // get the site object name from the server object name
                    // CN=server1,CN=Servers,CN=Site1,CN=Sites
                    // the site object name is the third component onwards
                    string[] components = ServerObjectName.Split(new char[] { ',' });
                    if (components.GetLength(0) < 3)
                    {
                        // should not happen
                        throw new ActiveDirectoryOperationException(SR.InvalidServerNameFormat);
                    }
                    cachedSiteObjectName = components[2];
                    for (int i = 3; i < components.GetLength(0); i++)
                    {
                        cachedSiteObjectName += "," + components[i];
                    }
                }
                return cachedSiteObjectName;
            }
        }

        internal String ServerObjectName
        {
            get
            {
                CheckIfDisposed();
                if (cachedServerObjectName == null)
                {
                    DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                    try
                    {
                        cachedServerObjectName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.ServerName);
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    finally
                    {
                        rootDSE.Dispose();
                    }
                }
                return cachedServerObjectName;
            }
        }

        internal String NtdsaObjectName
        {
            get
            {
                CheckIfDisposed();
                if (cachedNtdsaObjectName == null)
                {
                    DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                    try
                    {
                        cachedNtdsaObjectName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DsServiceName);
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    finally
                    {
                        rootDSE.Dispose();
                    }
                }
                return cachedNtdsaObjectName;
            }
        }

        internal Guid NtdsaObjectGuid
        {
            get
            {
                CheckIfDisposed();
                if (cachedNtdsaObjectGuid == Guid.Empty)
                {
                    DirectoryEntry ntdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);
                    byte[] guidByteArray = (byte[])PropertyManager.GetPropertyValue(context, ntdsaEntry, PropertyManager.ObjectGuid);
                    cachedNtdsaObjectGuid = new Guid(guidByteArray);
                }
                return cachedNtdsaObjectGuid;
            }
        }

        public override SyncUpdateCallback SyncFromAllServersCallback
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _userDelegate;
            }

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                _userDelegate = value;
            }
        }

        public override ReplicationConnectionCollection InboundConnections => GetInboundConnectionsHelper();

        public override ReplicationConnectionCollection OutboundConnections => GetOutboundConnectionsHelper();

        #endregion public properties

        #region private methods

        private ReplicationFailureCollection GetReplicationFailures(DS_REPL_INFO_TYPE type)
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the handle
            GetADAMHandle();
            info = GetReplicationInfoHelper(_ADAMHandle, (int)type, (int)type, null, ref advanced, 0, DirectoryContext.ADAMHandle);
            return ConstructFailures(info, this, DirectoryContext.ADAMHandle);
        }

        private void GetADAMHandle()
        {
            // this part of the code needs to be synchronized
            lock (this)
            {
                if (_ADAMHandle == IntPtr.Zero)
                {
                    // get the credentials object
                    if (_authIdentity == IntPtr.Zero)
                    {
                        _authIdentity = Utils.GetAuthIdentity(context, DirectoryContext.ADAMHandle);
                    }

                    // DSBind, but we need to have port as annotation specified
                    string bindingString = HostName + ":" + LdapPort;

                    _ADAMHandle = Utils.GetDSHandle(bindingString, null, _authIdentity, DirectoryContext.ADAMHandle);
                }
            }
        }

        private void FreeADAMHandle()
        {
            lock (this)
            {
                // DsUnbind
                Utils.FreeDSHandle(_ADAMHandle, DirectoryContext.ADAMHandle);

                // free the credentials object
                Utils.FreeAuthIdentity(_authIdentity, DirectoryContext.ADAMHandle);
            }
        }

        #endregion private methods
    }
}
