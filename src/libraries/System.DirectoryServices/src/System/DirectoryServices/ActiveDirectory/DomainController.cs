// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.ComponentModel;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.DirectoryServices.ActiveDirectory
{
    [Flags]
    public enum SyncFromAllServersOptions
    {
        None = 0,
        AbortIfServerUnavailable = 1,
        SyncAdjacentServerOnly = 2,
        CheckServerAlivenessOnly = 0x8,
        SkipInitialCheck = 0x10,
        PushChangeOutward = 0x20,
        CrossSite = 0x40
    }
    public enum SyncFromAllServersEvent
    {
        Error = 0,
        SyncStarted = 1,
        SyncCompleted = 2,
        Finished = 3
    }
    public enum SyncFromAllServersErrorCategory
    {
        ErrorContactingServer = 0,
        ErrorReplicating = 1,
        ServerUnreachable = 2
    }
    public delegate bool SyncUpdateCallback(SyncFromAllServersEvent eventType, string? targetServer, string? sourceServer, SyncFromAllServersOperationException? exception);
    internal delegate bool SyncReplicaFromAllServersCallback(IntPtr data, IntPtr update);

    public class DomainController : DirectoryServer
    {
        private IntPtr _dsHandle = IntPtr.Zero;
        private IntPtr _authIdentity = IntPtr.Zero;
        private readonly string[] _becomeRoleOwnerAttrs = null!;
        private bool _disposed;

        // internal variables for the public properties
        private string? _cachedComputerObjectName;
        private string? _cachedOSVersion;
        private double _cachedNumericOSVersion;
        private Forest? _currentForest;
        private Domain? _cachedDomain;
        private ActiveDirectoryRoleCollection? _cachedRoles;
        private bool _dcInfoInitialized;

        internal SyncUpdateCallback? userDelegate;
        internal readonly SyncReplicaFromAllServersCallback syncAllFunctionPointer = null!;

        // this is twice the maximum allowed RIDPool size which is 15k
        internal const int UpdateRidPoolSeizureValue = 30000;

        #region constructors

        // Internal constructors
        protected DomainController()
        {
        }

        internal DomainController(DirectoryContext context, string domainControllerName)
            : this(context, domainControllerName, new DirectoryEntryManager(context))
        {
        }

        internal DomainController(DirectoryContext context, string domainControllerName, DirectoryEntryManager directoryEntryMgr)
        {
            this.context = context;
            this.replicaName = domainControllerName;
            this.directoryEntryMgr = directoryEntryMgr;

            // initialize the transfer role owner attributes
            _becomeRoleOwnerAttrs = new string[5];
            _becomeRoleOwnerAttrs[0] = PropertyManager.BecomeSchemaMaster;
            _becomeRoleOwnerAttrs[1] = PropertyManager.BecomeDomainMaster;
            _becomeRoleOwnerAttrs[2] = PropertyManager.BecomePdc;
            _becomeRoleOwnerAttrs[3] = PropertyManager.BecomeRidMaster;
            _becomeRoleOwnerAttrs[4] = PropertyManager.BecomeInfrastructureMaster;

            // initialize the callback function
            syncAllFunctionPointer = new SyncReplicaFromAllServersCallback(SyncAllCallbackRoutine);
        }
        #endregion constructors

        #region IDisposable

        ~DomainController() => Dispose(false);

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
                    FreeDSHandle();
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

        public static DomainController GetDomainController(DirectoryContext context)
        {
            string? dcDnsName = null;
            DirectoryEntryManager? directoryEntryMgr = null;

            // check that the context argument is not null
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // target should be DC
            if (context.ContextType != DirectoryContextType.DirectoryServer)
            {
                throw new ArgumentException(SR.TargetShouldBeDC, nameof(context));
            }

            // target should be a server
            if (!(context.isServer()))
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(DomainController), context.Name);
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            try
            {
                // Get dns name of the dc
                // by binding to root dse and getting the "dnsHostName" attribute
                directoryEntryMgr = new DirectoryEntryManager(context);
                DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                if (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectory))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(DomainController), context.Name);
                }
                dcDnsName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DnsHostName)!;
            }
            catch (COMException e)
            {
                int errorCode = e.ErrorCode;

                if (errorCode == unchecked((int)0x8007203a))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(DomainController), context.Name);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            return new DomainController(context, dcDnsName, directoryEntryMgr);
        }

        public static DomainController FindOne(DirectoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Domain)
            {
                throw new ArgumentException(SR.TargetShouldBeDomain, nameof(context));
            }

            return FindOneWithCredentialValidation(context, null, 0);
        }

        public static DomainController FindOne(DirectoryContext context, string siteName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Domain)
            {
                throw new ArgumentException(SR.TargetShouldBeDomain, nameof(context));
            }

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return FindOneWithCredentialValidation(context, siteName, 0);
        }

        public static DomainController FindOne(DirectoryContext context, LocatorOptions flag)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Domain)
            {
                throw new ArgumentException(SR.TargetShouldBeDomain, nameof(context));
            }

            return FindOneWithCredentialValidation(context, null, flag);
        }

        public static DomainController FindOne(DirectoryContext context, string siteName, LocatorOptions flag)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Domain)
            {
                throw new ArgumentException(SR.TargetShouldBeDomain, nameof(context));
            }

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return FindOneWithCredentialValidation(context, siteName, flag);
        }

        public static DomainControllerCollection FindAll(DirectoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Domain)
            {
                throw new ArgumentException(SR.TargetShouldBeDomain, nameof(context));
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            return FindAllInternal(context, context.Name, false /* isDnsDomainName */, null);
        }

        public static DomainControllerCollection FindAll(DirectoryContext context, string siteName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Domain)
            {
                throw new ArgumentException(SR.TargetShouldBeDomain, nameof(context));
            }

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            return FindAllInternal(context, context.Name, false /* isDnsDomainName */, siteName);
        }

        public virtual GlobalCatalog EnableGlobalCatalog()
        {
            CheckIfDisposed();

            try
            {
                // bind to the server object
                DirectoryEntry serverNtdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);
                // set the NTDSDSA_OPT_IS_GC flag on the "options" property
                int options = 0;
                if (serverNtdsaEntry.Properties[PropertyManager.Options].Value != null)
                {
                    options = (int)serverNtdsaEntry.Properties[PropertyManager.Options].Value!;
                }
                serverNtdsaEntry.Properties[PropertyManager.Options].Value = options | 1;
                serverNtdsaEntry.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            // return a GlobalCatalog object
            return new GlobalCatalog(context, Name);
        }

        public virtual bool IsGlobalCatalog()
        {
            CheckIfDisposed();

            try
            {
                DirectoryEntry serverNtdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);
                serverNtdsaEntry.RefreshCache();
                // check if the NTDSDSA_OPT_IS_GC flag is set in the
                // "options" attribute (lowest bit)
                int options = 0;
                if (serverNtdsaEntry.Properties[PropertyManager.Options].Value != null)
                {
                    options = (int)serverNtdsaEntry.Properties[PropertyManager.Options].Value!;
                }
                if ((options & (1)) == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        public void TransferRoleOwnership(ActiveDirectoryRole role)
        {
            CheckIfDisposed();

            if (role < ActiveDirectoryRole.SchemaRole || role > ActiveDirectoryRole.InfrastructureRole)
            {
                throw new InvalidEnumArgumentException(nameof(role), (int)role, typeof(ActiveDirectoryRole));
            }

            try
            {
                // set the appropriate attribute on the rootDSE
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

        public void SeizeRoleOwnership(ActiveDirectoryRole role)
        {
            // set the "fsmoRoleOwner" attribute on the appropriate role object
            // to the NTDSAObjectName of this DC
            string? roleObjectDN = null;

            CheckIfDisposed();

            switch (role)
            {
                case ActiveDirectoryRole.SchemaRole:
                    {
                        roleObjectDN = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext);
                        break;
                    }
                case ActiveDirectoryRole.NamingRole:
                    {
                        roleObjectDN = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer);
                        break;
                    }
                case ActiveDirectoryRole.PdcRole:
                    {
                        roleObjectDN = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.DefaultNamingContext);
                        break;
                    }
                case ActiveDirectoryRole.RidRole:
                    {
                        roleObjectDN = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.RidManager);
                        break;
                    }
                case ActiveDirectoryRole.InfrastructureRole:
                    {
                        roleObjectDN = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.Infrastructure);
                        break;
                    }
                default:
                    throw new InvalidEnumArgumentException(nameof(role), (int)role, typeof(ActiveDirectoryRole));
            }

            DirectoryEntry? roleObjectEntry = null;
            try
            {
                roleObjectEntry = DirectoryEntryManager.GetDirectoryEntry(context, roleObjectDN);

                // For RID FSMO role
                // Increment the RIDAvailablePool by 30k.
                if (role == ActiveDirectoryRole.RidRole)
                {
                    System.DirectoryServices.Interop.UnsafeNativeMethods.IADsLargeInteger ridPool = (System.DirectoryServices.Interop.UnsafeNativeMethods.IADsLargeInteger)roleObjectEntry.Properties[PropertyManager.RIDAvailablePool].Value!;

                    // check the overflow of the low part
                    if (ridPool.LowPart + UpdateRidPoolSeizureValue < ridPool.LowPart)
                    {
                        throw new InvalidOperationException(SR.UpdateAvailableRIDPoolOverflowFailure);
                    }
                    ridPool.LowPart += UpdateRidPoolSeizureValue;
                    roleObjectEntry.Properties[PropertyManager.RIDAvailablePool].Value = ridPool;
                }
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

        public virtual DirectorySearcher GetDirectorySearcher()
        {
            CheckIfDisposed();

            return InternalGetDirectorySearcher();
        }

        public override void CheckReplicationConsistency()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the handle
            GetDSHandle();

            // call private helper function
            CheckConsistencyHelper(_dsHandle, DirectoryContext.ADHandle);
        }

        public override ReplicationCursorCollection GetReplicationCursors(string partition)
        {
            IntPtr info = (IntPtr)0;
            int context = 0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(partition));

            // get the handle
            GetDSHandle();
            info = GetReplicationInfoHelper(_dsHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_CURSORS_3_FOR_NC, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_CURSORS_FOR_NC, partition, ref advanced, context, DirectoryContext.ADHandle);
            return ConstructReplicationCursors(_dsHandle, advanced, info, partition, this, DirectoryContext.ADHandle);
        }

        public override ReplicationOperationInformation GetReplicationOperationInformation()
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the handle
            GetDSHandle();
            info = GetReplicationInfoHelper(_dsHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_PENDING_OPS, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_PENDING_OPS, null, ref advanced, 0, DirectoryContext.ADHandle);
            return ConstructPendingOperations(info, this, DirectoryContext.ADHandle);
        }

        public override ReplicationNeighborCollection GetReplicationNeighbors(string partition)
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(partition));

            // get the handle
            GetDSHandle();
            info = GetReplicationInfoHelper(_dsHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_NEIGHBORS, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_NEIGHBORS, partition, ref advanced, 0, DirectoryContext.ADHandle);
            return ConstructNeighbors(info, this, DirectoryContext.ADHandle);
        }

        public override ReplicationNeighborCollection GetAllReplicationNeighbors()
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the handle
            GetDSHandle();
            info = GetReplicationInfoHelper(_dsHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_NEIGHBORS, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_NEIGHBORS, null, ref advanced, 0, DirectoryContext.ADHandle);
            return ConstructNeighbors(info, this, DirectoryContext.ADHandle);
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
                throw new ArgumentNullException(nameof(objectPath));

            if (objectPath.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(objectPath));

            // get the handle
            GetDSHandle();
            info = GetReplicationInfoHelper(_dsHandle, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_METADATA_2_FOR_OBJ, (int)DS_REPL_INFO_TYPE.DS_REPL_INFO_METADATA_FOR_OBJ, objectPath, ref advanced, 0, DirectoryContext.ADHandle);
            return ConstructMetaData(advanced, info, this, DirectoryContext.ADHandle);
        }

        public override void SyncReplicaFromServer(string partition, string sourceServer)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(partition));

            if (sourceServer == null)
                throw new ArgumentNullException(nameof(sourceServer));

            if (sourceServer.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(sourceServer));

            // get the dsHandle
            GetDSHandle();
            SyncReplicaHelper(_dsHandle, false, partition, sourceServer, 0, DirectoryContext.ADHandle);
        }

        public override void TriggerSyncReplicaFromNeighbors(string partition)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(partition));

            // get the dsHandle
            GetDSHandle();
            SyncReplicaHelper(_dsHandle, false, partition, null, DS_REPSYNC_ASYNCHRONOUS_OPERATION | DS_REPSYNC_ALL_SOURCES, DirectoryContext.ADHandle);
        }

        public override void SyncReplicaFromAllServers(string partition, SyncFromAllServersOptions options)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            if (partition.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(partition));

            // get the dsHandle
            GetDSHandle();
            SyncReplicaAllHelper(_dsHandle, syncAllFunctionPointer, partition, options, SyncFromAllServersCallback, DirectoryContext.ADHandle);
        }
        #endregion public methods

        #region public properties

        public Forest Forest
        {
            get
            {
                CheckIfDisposed();
                if (_currentForest == null)
                {
                    DirectoryContext forestContext = Utils.GetNewDirectoryContext(Name, DirectoryContextType.DirectoryServer, context);
                    _currentForest = Forest.GetForest(forestContext);
                }
                return _currentForest;
            }
        }

        public DateTime CurrentTime
        {
            get
            {
                CheckIfDisposed();

                DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string? serverUTCTime = null;

                try
                {
                    serverUTCTime = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.CurrentTime)!;
                }
                finally
                {
                    rootDSE.Dispose();
                }
                return ParseDateTime(serverUTCTime);
            }
        }

        public long HighestCommittedUsn
        {
            get
            {
                CheckIfDisposed();

                DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string? serverHighestCommittedUsn = null;

                try
                {
                    serverHighestCommittedUsn = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.HighestCommittedUSN)!;
                }
                finally
                {
                    rootDSE.Dispose();
                }
                return long.Parse(serverHighestCommittedUsn, NumberFormatInfo.InvariantInfo);
            }
        }

        public string OSVersion
        {
            get
            {
                CheckIfDisposed();
                if (_cachedOSVersion == null)
                {
                    // get the operating system version attribute
                    DirectoryEntry computerEntry = directoryEntryMgr.GetCachedDirectoryEntry(ComputerObjectName);
                    // is in the form Windows Server 2003
                    _cachedOSVersion = (string)PropertyManager.GetPropertyValue(context, computerEntry, PropertyManager.OperatingSystem)!;
                }
                return _cachedOSVersion;
            }
        }

        internal double NumericOSVersion
        {
            get
            {
                CheckIfDisposed();
                if (_cachedNumericOSVersion == 0)
                {
                    // get the operating system version attribute
                    DirectoryEntry computerEntry = directoryEntryMgr.GetCachedDirectoryEntry(ComputerObjectName);

                    // is in the form Windows Server 2003
                    string osVersion = (string)PropertyManager.GetPropertyValue(context, computerEntry, PropertyManager.OperatingSystemVersion)!;

                    // this could be in the form 5.2 (3790), so we need to take out the (3790)
                    int index = osVersion.IndexOf('(');
                    if (index != -1)
                    {
                        osVersion = osVersion.Substring(0, index);
                    }
                    _cachedNumericOSVersion = (double)double.Parse(osVersion, NumberFormatInfo.InvariantInfo);
                }

                return _cachedNumericOSVersion;
            }
        }

        public ActiveDirectoryRoleCollection Roles
        {
            get
            {
                CheckIfDisposed();
                if (_cachedRoles == null)
                {
                    _cachedRoles = new ActiveDirectoryRoleCollection(GetRoles());
                }
                return _cachedRoles;
            }
        }

        public Domain Domain
        {
            get
            {
                CheckIfDisposed();
                if (_cachedDomain == null)
                {
                    string? domainName = null;
                    try
                    {
                        string defaultNCName = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.DefaultNamingContext);
                        domainName = Utils.GetDnsNameFromDN(defaultNCName);
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                    // For domain controllers this is always the
                    // domain naming context
                    // create a new domain context for the domain
                    DirectoryContext domainContext = Utils.GetNewDirectoryContext(Name, DirectoryContextType.DirectoryServer, context);
                    _cachedDomain = new Domain(domainContext, domainName);
                }
                return _cachedDomain;
            }
        }

        public override string? IPAddress
        {
            get
            {
                CheckIfDisposed();

                IPHostEntry hostEntry = Dns.GetHostEntry(Name);
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

        public override string SiteName
        {
            get
            {
                CheckIfDisposed();
                if (!_dcInfoInitialized || siteInfoModified)
                {
                    GetDomainControllerInfo();
                }
                if (cachedSiteName == null)
                {
                    throw new ActiveDirectoryOperationException(SR.Format(SR.SiteNameNotFound, Name));
                }

                return cachedSiteName;
            }
        }

        internal string SiteObjectName
        {
            get
            {
                CheckIfDisposed();
                if (!_dcInfoInitialized || siteInfoModified)
                {
                    GetDomainControllerInfo();
                }
                if (cachedSiteObjectName == null)
                {
                    throw new ActiveDirectoryOperationException(SR.Format(SR.SiteObjectNameNotFound, Name));
                }
                return cachedSiteObjectName;
            }
        }

        internal string ComputerObjectName
        {
            get
            {
                CheckIfDisposed();
                if (!_dcInfoInitialized)
                {
                    GetDomainControllerInfo();
                }
                if (_cachedComputerObjectName == null)
                {
                    throw new ActiveDirectoryOperationException(SR.Format(SR.ComputerObjectNameNotFound, Name));
                }
                return _cachedComputerObjectName;
            }
        }

        internal string ServerObjectName
        {
            get
            {
                CheckIfDisposed();
                if (!_dcInfoInitialized || siteInfoModified)
                {
                    GetDomainControllerInfo();
                }
                if (cachedServerObjectName == null)
                {
                    throw new ActiveDirectoryOperationException(SR.Format(SR.ServerObjectNameNotFound, Name));
                }
                return cachedServerObjectName;
            }
        }

        internal string NtdsaObjectName
        {
            get
            {
                CheckIfDisposed();
                if (!_dcInfoInitialized || siteInfoModified)
                {
                    GetDomainControllerInfo();
                }
                if (cachedNtdsaObjectName == null)
                {
                    throw new ActiveDirectoryOperationException(SR.Format(SR.NtdsaObjectNameNotFound, Name));
                }
                return cachedNtdsaObjectName;
            }
        }

        internal Guid NtdsaObjectGuid
        {
            get
            {
                CheckIfDisposed();
                if (!_dcInfoInitialized || siteInfoModified)
                {
                    GetDomainControllerInfo();
                }
                if (cachedNtdsaObjectGuid.Equals(Guid.Empty))
                {
                    throw new ActiveDirectoryOperationException(SR.Format(SR.NtdsaObjectGuidNotFound, Name));
                }
                return cachedNtdsaObjectGuid;
            }
        }

        public override SyncUpdateCallback? SyncFromAllServersCallback
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return userDelegate;
            }

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                userDelegate = value;
            }
        }

        public override ReplicationConnectionCollection InboundConnections => GetInboundConnectionsHelper();

        public override ReplicationConnectionCollection OutboundConnections => GetOutboundConnectionsHelper();

        internal IntPtr Handle
        {
            get
            {
                GetDSHandle();
                return _dsHandle;
            }
        }

        #endregion public properties

        #region private methods

        internal static void ValidateCredential(DomainController dc, DirectoryContext context)
        {
            DirectoryEntry de;

            de = new DirectoryEntry("LDAP://" + dc.Name + "/RootDSE", context.UserName, context.Password, Utils.DefaultAuthType | AuthenticationTypes.ServerBind);

            de.Bind(true);
        }

        internal static DomainController FindOneWithCredentialValidation(DirectoryContext context, string? siteName, LocatorOptions flag)
        {
            DomainController dc;
            bool retry = false;
            bool credsValidated = false;

            //  work with copy of the context
            context = new DirectoryContext(context);

            // authenticate against this DC to validate the credentials
            dc = FindOneInternal(context, context.Name, siteName, flag);
            try
            {
                ValidateCredential(dc, context);
                credsValidated = true;
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x8007203a))
                {
                    // server is down , so try again with force rediscovery if the flags did not already contain force rediscovery
                    if ((flag & LocatorOptions.ForceRediscovery) == 0)
                    {
                        retry = true;
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFoundInDomain, context.Name), typeof(DomainController), null);
                    }
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            finally
            {
                if (!credsValidated)
                {
                    dc.Dispose();
                }
            }

            if (retry)
            {
                credsValidated = false;
                dc = FindOneInternal(context, context.Name, siteName, flag | LocatorOptions.ForceRediscovery);
                try
                {
                    ValidateCredential(dc, context);
                    credsValidated = true;
                }
                catch (COMException e)
                {
                    if (e.ErrorCode == unchecked((int)0x8007203a))
                    {
                        // server is down
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFoundInDomain, context.Name), typeof(DomainController), null);
                    }
                    else
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                finally
                {
                    if (!credsValidated)
                    {
                        dc.Dispose();
                    }
                }
            }

            return dc;
        }

        internal static DomainController FindOneInternal(DirectoryContext context, string? domainName, string? siteName, LocatorOptions flag)
        {
            DomainControllerInfo domainControllerInfo;
            int errorCode = 0;

            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
            }

            // check that the flags passed have only the valid bits set
            if (((long)flag & (~((long)LocatorOptions.AvoidSelf | (long)LocatorOptions.ForceRediscovery | (long)LocatorOptions.KdcRequired | (long)LocatorOptions.TimeServerRequired | (long)LocatorOptions.WriteableRequired))) != 0)
            {
                throw new ArgumentException(SR.InvalidFlags, nameof(flag));
            }

            if (domainName == null)
            {
                domainName = DirectoryContext.GetLoggedOnDomain();
            }

            // call DsGetDcName
            errorCode = Locator.DsGetDcNameWrapper(null, domainName, siteName, (long)flag | (long)PrivateLocatorFlags.DirectoryServicesRequired, out domainControllerInfo);

            if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFoundInDomain, domainName), typeof(DomainController), null);
            }
            // this can only occur when flag is being explicitly passed (since the flags that we pass internally are valid)
            if (errorCode == NativeMethods.ERROR_INVALID_FLAGS)
            {
                throw new ArgumentException(SR.InvalidFlags, nameof(flag));
            }
            else if (errorCode != 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
            }

            // create a DomainController object
            Debug.Assert(domainControllerInfo.DomainControllerName.Length > 2);
            string domainControllerName = domainControllerInfo.DomainControllerName.Substring(2);

            // create a new context object for the domain controller
            DirectoryContext dcContext = Utils.GetNewDirectoryContext(domainControllerName, DirectoryContextType.DirectoryServer, context);

            return new DomainController(dcContext, domainControllerName);
        }

        internal static DomainControllerCollection FindAllInternal(DirectoryContext context, string? domainName, bool isDnsDomainName, string? siteName)
        {
            ArrayList dcList = new ArrayList();

            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
            }

            if (domainName == null || !isDnsDomainName)
            {
                // get the dns name of the domain
                DomainControllerInfo domainControllerInfo;
                int errorCode = Locator.DsGetDcNameWrapper(null, domainName ?? DirectoryContext.GetLoggedOnDomain(), null, (long)PrivateLocatorFlags.DirectoryServicesRequired, out domainControllerInfo);

                if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                {
                    // return an empty collection
                    return new DomainControllerCollection(dcList);
                }
                else if (errorCode != 0)
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                }

                Debug.Assert(domainControllerInfo.DomainName != null);
                domainName = domainControllerInfo.DomainName;
            }

            foreach (string dcName in Utils.GetReplicaList(context, Utils.GetDNFromDnsName(domainName), siteName, true /* isDefaultNC */, false /* isADAM */, false /* mustBeGC */))
            {
                DirectoryContext dcContext = Utils.GetNewDirectoryContext(dcName, DirectoryContextType.DirectoryServer, context);
                dcList.Add(new DomainController(dcContext, dcName));
            }

            return new DomainControllerCollection(dcList);
        }

        private unsafe void GetDomainControllerInfo()
        {
            int result = 0;
            int dcCount = 0;
            IntPtr dcInfoPtr = IntPtr.Zero;
            int dcInfoLevel = 0;
            bool initialized = false;

            // get the handle
            GetDSHandle();

            // call DsGetDomainControllerInfo
            /*DWORD DsGetDomainControllerInfo(
                HANDLE hDs,
                LPTSTR DomainName,
                DWORD InfoLevel,
                DWORD* pcOut,
                VOID** ppInfo
                );*/
            var dsGetDomainControllerInfo = (delegate* unmanaged<IntPtr, char*, int, int*, IntPtr*, int>)global::Interop.Kernel32.GetProcAddress(DirectoryContext.ADHandle, "DsGetDomainControllerInfoW");
            if (dsGetDomainControllerInfo == null)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
            }

            fixed (char* domainName = Domain.Name)
            {
                // try DsDomainControllerInfoLevel3 first which supports Read only DC (RODC)
                dcInfoLevel = NativeMethods.DsDomainControllerInfoLevel3;
                result = dsGetDomainControllerInfo(_dsHandle, domainName, dcInfoLevel, &dcCount, &dcInfoPtr);

                if (result != 0)
                {
                    // fallback to DsDomainControllerInfoLevel2
                    dcInfoLevel = NativeMethods.DsDomainControllerInfoLevel2;
                    result = dsGetDomainControllerInfo(_dsHandle, domainName, dcInfoLevel, &dcCount, &dcInfoPtr);
                }
            }

            if (result == 0)
            {
                try
                {
                    IntPtr currentDc = dcInfoPtr;
                    for (int i = 0; i < dcCount; i++)
                    {
                        if (dcInfoLevel == NativeMethods.DsDomainControllerInfoLevel3)
                        {
                            DsDomainControllerInfo3 domainControllerInfo3 = new DsDomainControllerInfo3();
                            Marshal.PtrToStructure(currentDc, domainControllerInfo3);
                            // check if this is the same as "this" DC
                            if (domainControllerInfo3 != null)
                            {
                                if (Utils.Compare(domainControllerInfo3.dnsHostName, replicaName) == 0)
                                {
                                    initialized = true;

                                    // update all the fields
                                    cachedSiteName = domainControllerInfo3.siteName;
                                    cachedSiteObjectName = domainControllerInfo3.siteObjectName;
                                    _cachedComputerObjectName = domainControllerInfo3.computerObjectName;
                                    cachedServerObjectName = domainControllerInfo3.serverObjectName;
                                    cachedNtdsaObjectName = domainControllerInfo3.ntdsaObjectName;
                                    cachedNtdsaObjectGuid = domainControllerInfo3.ntdsDsaObjectGuid;
                                }
                            }
                            currentDc = IntPtr.Add(currentDc, Marshal.SizeOf(domainControllerInfo3));
                        }
                        else
                        { //NativeMethods.DsDomainControllerInfoLevel2
                            DsDomainControllerInfo2 domainControllerInfo2 = new DsDomainControllerInfo2();
                            Marshal.PtrToStructure(currentDc, domainControllerInfo2);
                            // check if this is the same as "this" DC
                            if (domainControllerInfo2 != null)
                            {
                                if (Utils.Compare(domainControllerInfo2.dnsHostName, replicaName) == 0)
                                {
                                    initialized = true;

                                    // update all the fields
                                    cachedSiteName = domainControllerInfo2.siteName;
                                    cachedSiteObjectName = domainControllerInfo2.siteObjectName;
                                    _cachedComputerObjectName = domainControllerInfo2.computerObjectName;
                                    cachedServerObjectName = domainControllerInfo2.serverObjectName;
                                    cachedNtdsaObjectName = domainControllerInfo2.ntdsaObjectName;
                                    cachedNtdsaObjectGuid = domainControllerInfo2.ntdsDsaObjectGuid;
                                }
                            }
                            currentDc = IntPtr.Add(currentDc, Marshal.SizeOf(domainControllerInfo2));
                        }
                    }
                }
                finally
                {
                    // free the domain controller info structure
                    if (dcInfoPtr != IntPtr.Zero)
                    {
                        // call DsFreeDomainControllerInfo
                        /*VOID DsFreeDomainControllerInfo(
                            DWORD InfoLevel,
                            DWORD cInfo,
                            VOID* pInfo
                            );*/
                        var dsFreeDomainControllerInfo = (delegate* unmanaged<int, int, IntPtr, void>)global::Interop.Kernel32.GetProcAddress(DirectoryContext.ADHandle, "DsFreeDomainControllerInfoW");
                        if (dsFreeDomainControllerInfo == null)
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
                        }
                        dsFreeDomainControllerInfo(dcInfoLevel, dcCount, dcInfoPtr);
                    }
                }

                // if we couldn't get this DC's info, throw an exception
                if (!initialized)
                {
                    throw new ActiveDirectoryOperationException(SR.DCInfoNotFound);
                }

                _dcInfoInitialized = true;
                siteInfoModified = false;
            }
            else
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(result, Name);
            }
        }

        internal void GetDSHandle()
        {
            if (_disposed)
            {
                // cannot bind to the domain controller as the object has been
                // disposed (finalizer has been suppressed)
                throw new ObjectDisposedException(GetType().Name);
            }

            // this part of the code needs to be synchronized
            lock (this)
            {
                if (_dsHandle == IntPtr.Zero)
                {
                    // get the credentials object
                    if (_authIdentity == IntPtr.Zero)
                    {
                        _authIdentity = Utils.GetAuthIdentity(context, DirectoryContext.ADHandle);
                    }

                    // DsBind
                    _dsHandle = Utils.GetDSHandle(replicaName, null, _authIdentity, DirectoryContext.ADHandle);
                }
            }
        }

        internal void FreeDSHandle()
        {
            lock (this)
            {
                // DsUnbind
                Utils.FreeDSHandle(_dsHandle, DirectoryContext.ADHandle);
                // free the credentials object
                Utils.FreeAuthIdentity(_authIdentity, DirectoryContext.ADHandle);
            }
        }

        internal ReplicationFailureCollection GetReplicationFailures(DS_REPL_INFO_TYPE type)
        {
            IntPtr info = (IntPtr)0;
            bool advanced = true;

            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            // get the handle
            GetDSHandle();
            info = GetReplicationInfoHelper(_dsHandle, (int)type, (int)type, null, ref advanced, 0, DirectoryContext.ADHandle);
            return ConstructFailures(info, this, DirectoryContext.ADHandle);
        }

        private unsafe ArrayList GetRoles()
        {
            ArrayList roleList = new ArrayList();
            int result = 0;
            IntPtr rolesPtr = IntPtr.Zero;

            GetDSHandle();
            // Get the roles
            // call DsListRoles
            /*DWORD DsListRoles(
                HANDLE hDs,
                PDS_NAME_RESULTW* ppRoles
                );*/
            var dsListRoles = (delegate* unmanaged<IntPtr, IntPtr*, int>)global::Interop.Kernel32.GetProcAddress(DirectoryContext.ADHandle, "DsListRolesW");
            if (dsListRoles == null)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
            }

            result = dsListRoles(_dsHandle, &rolesPtr);
            if (result == 0)
            {
                try
                {
                    DsNameResult dsNameResult = new DsNameResult();
                    Marshal.PtrToStructure(rolesPtr, dsNameResult);
                    IntPtr currentItem = dsNameResult.items;
                    for (int i = 0; i < dsNameResult.itemCount; i++)
                    {
                        DsNameResultItem dsNameResultItem = new DsNameResultItem();
                        Marshal.PtrToStructure(currentItem, dsNameResultItem);

                        // check if the role owner is this dc
                        if (dsNameResultItem.status == NativeMethods.DsNameNoError)
                        {
                            if (dsNameResultItem.name!.Equals(NtdsaObjectName))
                            {
                                // add this role to the array
                                // the index of the item in the result signifies
                                // which role owner it is
                                roleList.Add((ActiveDirectoryRole)i);
                            }
                        }
                        currentItem = IntPtr.Add(currentItem, Marshal.SizeOf(dsNameResultItem));
                    }
                }
                finally
                {
                    // free the DsNameResult structure
                    if (rolesPtr != IntPtr.Zero)
                    {
                        // call DsFreeNameResult
                        var dsFreeNameResult = (delegate* unmanaged<IntPtr, void>)global::Interop.Kernel32.GetProcAddress(DirectoryContext.ADHandle, "DsFreeNameResultW");
                        if (dsFreeNameResult == null)
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
                        }
                        dsFreeNameResult(rolesPtr);
                    }
                }
            }
            else
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(result, Name);
            }
            return roleList;
        }

        private DateTime ParseDateTime(string dateTime)
        {
            int year = (int)int.Parse(dateTime.Substring(0, 4), NumberFormatInfo.InvariantInfo);
            int month = (int)int.Parse(dateTime.Substring(4, 2), NumberFormatInfo.InvariantInfo);
            int day = (int)int.Parse(dateTime.Substring(6, 2), NumberFormatInfo.InvariantInfo);
            int hour = (int)int.Parse(dateTime.Substring(8, 2), NumberFormatInfo.InvariantInfo);
            int min = (int)int.Parse(dateTime.Substring(10, 2), NumberFormatInfo.InvariantInfo);
            int sec = (int)int.Parse(dateTime.Substring(12, 2), NumberFormatInfo.InvariantInfo);

            // this is the UniversalTime
            return new DateTime(year, month, day, hour, min, sec, 0);
        }

        private DirectorySearcher InternalGetDirectorySearcher()
        {
            DirectoryEntry de = new DirectoryEntry("LDAP://" + Name);

            de.AuthenticationType = Utils.DefaultAuthType | AuthenticationTypes.ServerBind;

            de.Username = context.UserName;
            de.Password = context.Password;

            return new DirectorySearcher(de);
        }
        #endregion private methods
    }
}
