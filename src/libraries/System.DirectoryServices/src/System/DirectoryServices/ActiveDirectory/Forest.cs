// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices.ActiveDirectory
{
    public enum ForestMode : int
    {
        Unknown = -1,
        Windows2000Forest = 0,
        Windows2003InterimForest = 1,
        Windows2003Forest = 2,
        Windows2008Forest = 3,
        Windows2008R2Forest = 4,
        Windows8Forest = 5,
        Windows2012R2Forest = 6,
    }

    public class Forest : IDisposable
    {
        // Private Variables
        private readonly DirectoryContext _context;
        private readonly DirectoryEntryManager _directoryEntryMgr;
        private bool _disposed;

        // Internal variables corresponding to public properties
        private readonly string _forestDnsName;
        private ReadOnlySiteCollection? _cachedSites;
        private DomainCollection? _cachedDomains;
        private GlobalCatalogCollection? _cachedGlobalCatalogs;
        private ApplicationPartitionCollection? _cachedApplicationPartitions;
        private int _forestModeLevel = -1;
        private Domain? _cachedRootDomain;
        private ActiveDirectorySchema? _cachedSchema;
        private DomainController? _cachedSchemaRoleOwner;
        private DomainController? _cachedNamingRoleOwner;

        #region constructors
        internal Forest(DirectoryContext context, string forestDnsName, DirectoryEntryManager directoryEntryMgr)
        {
            _context = context;
            _directoryEntryMgr = directoryEntryMgr;
            _forestDnsName = forestDnsName;
        }

        internal Forest(DirectoryContext context, string name)
            : this(context, name, new DirectoryEntryManager(context))
        {
        }
        #endregion constructors

        #region IDisposable

        public void Dispose() => Dispose(true);

        // private Dispose method
        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // check if this is an explicit Dispose
                // only then clean up the directory entries
                if (disposing)
                {
                    // dispose all directory entries
                    foreach (DirectoryEntry entry in _directoryEntryMgr.GetCachedDirectoryEntries())
                    {
                        entry.Dispose();
                    }
                }
                _disposed = true;
            }
        }
        #endregion IDisposable

        #region public methods

        public static Forest GetForest(DirectoryContext context)
        {
            DirectoryEntryManager? directoryEntryMgr = null;
            DirectoryEntry? rootDSE = null;
            string? rootDomainNC = null;

            // check that the argument is not null
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // contexttype should be Forest or DirectoryServer
            if ((context.ContextType != DirectoryContextType.Forest) &&
                (context.ContextType != DirectoryContextType.DirectoryServer))
            {
                throw new ArgumentException(SR.TargetShouldBeServerORForest, nameof(context));
            }

            if ((context.Name == null) && (!context.isRootDomain()))
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.ContextNotAssociatedWithDomain, typeof(Forest), null);
            }

            if (context.Name != null)
            {
                // the target should be a valid forest name or a server
                if (!((context.isRootDomain()) || (context.isServer())))
                {
                    if (context.ContextType == DirectoryContextType.Forest)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.ForestNotFound, typeof(Forest), context.Name);
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(Forest), null);
                    }
                }
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            directoryEntryMgr = new DirectoryEntryManager(context);
            // at this point we know that the target is either a
            // valid forest name or a server (may be a bogus server name -- to check bind to rootdse)
            // bind to the rootDSE of the forest specified in the context
            try
            {
                rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                if ((context.isServer()) && (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectory)))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(Forest), null);
                }
                rootDomainNC = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.RootDomainNamingContext)!;
            }
            catch (COMException e)
            {
                int errorCode = e.ErrorCode;

                if (errorCode == unchecked((int)0x8007203a))
                {
                    if (context.ContextType == DirectoryContextType.Forest)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.ForestNotFound, typeof(Forest), context.Name);
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(Forest), null);
                    }
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            // return forest object

            return new Forest(context, Utils.GetDnsNameFromDN(rootDomainNC), directoryEntryMgr);
        }

        public void RaiseForestFunctionalityLevel(int forestMode)
        {
            CheckIfDisposed();

            // check new functional level is valid or not
            if (forestMode < 0)
            {
                throw new ArgumentException(SR.InvalidMode, nameof(forestMode));
            }

            // new functional level should be higher than the old one
            if (forestMode <= ForestModeLevel)
            {
                throw new ArgumentException(SR.InvalidMode, nameof(forestMode));
            }

            // set the forest mode on AD
            DirectoryEntry partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(_context, _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));
            // NOTE:
            // If the domain is a W2K domain (W2K schema) then the msDS-Behavior-Version attribute will not be present.
            // If that is the case, the forest functionality cannot be raised.
            try
            {
                partitionsEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = forestMode;
                partitionsEntry.CommitChanges();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x8007200A))
                {
                    throw new ArgumentException(SR.NoW2K3DCsInForest, nameof(forestMode));
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
            }
            finally
            {
                partitionsEntry.Dispose();
            }

            // at this point the raise forest function has succeeded
            // invalidate the cached entry so that we will go to the server next time
            _forestModeLevel = -1;
        }

        public void RaiseForestFunctionality(ForestMode forestMode)
        {
            CheckIfDisposed();

            // check if forest mode is a valid enum value
            if (forestMode < ForestMode.Windows2000Forest)
            {
                throw new InvalidEnumArgumentException(nameof(forestMode), (int)forestMode, typeof(ForestMode));
            }

            if (forestMode <= ForestMode)
            {
                throw new ArgumentException(SR.InvalidMode, nameof(forestMode));
            }

            RaiseForestFunctionalityLevel((int)forestMode);
        }

        public override string ToString() => Name;

        public GlobalCatalog FindGlobalCatalog()
        {
            CheckIfDisposed();

            return GlobalCatalog.FindOneInternal(_context, Name, null, 0);
        }

        public GlobalCatalog FindGlobalCatalog(string siteName)
        {
            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return GlobalCatalog.FindOneInternal(_context, Name, siteName, 0);
        }

        public GlobalCatalog FindGlobalCatalog(LocatorOptions flag)
        {
            CheckIfDisposed();

            return GlobalCatalog.FindOneInternal(_context, Name, null, flag);
        }

        public GlobalCatalog FindGlobalCatalog(string siteName, LocatorOptions flag)
        {
            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return GlobalCatalog.FindOneInternal(_context, Name, siteName, flag);
        }

        public GlobalCatalogCollection FindAllGlobalCatalogs()
        {
            CheckIfDisposed();

            return GlobalCatalog.FindAllInternal(_context, null);
        }

        public GlobalCatalogCollection FindAllGlobalCatalogs(string siteName)
        {
            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return GlobalCatalog.FindAllInternal(_context, siteName);
        }

        public GlobalCatalogCollection FindAllDiscoverableGlobalCatalogs()
        {
            long flag = (long)PrivateLocatorFlags.GCRequired;

            CheckIfDisposed();
            return new GlobalCatalogCollection(Locator.EnumerateDomainControllers(_context, Name, null, flag));
        }

        public GlobalCatalogCollection FindAllDiscoverableGlobalCatalogs(string siteName)
        {
            long flag = (long)PrivateLocatorFlags.GCRequired;

            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            if (siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
            }

            return new GlobalCatalogCollection(Locator.EnumerateDomainControllers(_context, Name, siteName, flag));
        }

        public TrustRelationshipInformationCollection GetAllTrustRelationships()
        {
            CheckIfDisposed();

            return GetTrustsHelper(null);
        }

        public ForestTrustRelationshipInformation GetTrustRelationship(string targetForestName)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            TrustRelationshipInformationCollection collection = GetTrustsHelper(targetForestName);
            if (collection.Count != 0)
            {
                Debug.Assert(collection.Count == 1);
                return (ForestTrustRelationshipInformation)collection[0];
            }
            else
            {
                // trust relationship does not exist
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ForestTrustDoesNotExist, Name, targetForestName), typeof(TrustRelationshipInformation), null);
            }
        }

        public bool GetSelectiveAuthenticationStatus(string targetForestName)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            return TrustHelper.GetTrustedDomainInfoStatus(_context, Name, targetForestName, Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION, true);
        }

        public void SetSelectiveAuthenticationStatus(string targetForestName, bool enable)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            TrustHelper.SetTrustedDomainInfoStatus(_context, Name, targetForestName, Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION, enable, true);
        }

        public bool GetSidFilteringStatus(string targetForestName)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            return TrustHelper.GetTrustedDomainInfoStatus(_context, Name, targetForestName, Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL, true);
        }

        public void SetSidFilteringStatus(string targetForestName, bool enable)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            TrustHelper.SetTrustedDomainInfoStatus(_context, Name, targetForestName, Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_TREAT_AS_EXTERNAL, enable, true);
        }

        public void DeleteLocalSideOfTrustRelationship(string targetForestName)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            // delete local side of trust only
            TrustHelper.DeleteTrust(_context, Name, targetForestName, true);
        }

        public void DeleteTrustRelationship(Forest targetForest)
        {
            CheckIfDisposed();

            if (targetForest == null)
                throw new ArgumentNullException(nameof(targetForest));

            // first delete the trust on the remote side
            TrustHelper.DeleteTrust(targetForest.GetDirectoryContext(), targetForest.Name, Name, true);

            // then delete the local side trust
            TrustHelper.DeleteTrust(_context, Name, targetForest.Name, true);
        }

        public void VerifyOutboundTrustRelationship(string targetForestName)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            TrustHelper.VerifyTrust(_context, Name, targetForestName, true/*forest*/, TrustDirection.Outbound, false /*just TC verification*/, null /* no need to go to specific server*/);
        }

        public void VerifyTrustRelationship(Forest targetForest, TrustDirection direction)
        {
            CheckIfDisposed();

            if (targetForest == null)
                throw new ArgumentNullException(nameof(targetForest));

            if (direction < TrustDirection.Inbound || direction > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(TrustDirection));

            // verify outbound trust first
            if ((direction & TrustDirection.Outbound) != 0)
            {
                try
                {
                    TrustHelper.VerifyTrust(_context, Name, targetForest.Name, true/*forest*/, TrustDirection.Outbound, false/*just TC verification*/, null /* no need to go to specific server*/);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetForest.Name, direction), typeof(ForestTrustRelationshipInformation), null);
                }
            }

            // verify inbound trust
            if ((direction & TrustDirection.Inbound) != 0)
            {
                try
                {
                    TrustHelper.VerifyTrust(targetForest.GetDirectoryContext(), targetForest.Name, Name, true/*forest*/, TrustDirection.Outbound, false/*just TC verification*/, null /* no need to go to specific server*/);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetForest.Name, direction), typeof(ForestTrustRelationshipInformation), null);
                }
            }
        }

        public void CreateLocalSideOfTrustRelationship(string targetForestName, TrustDirection direction, string trustPassword)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            if (direction < TrustDirection.Inbound || direction > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(TrustDirection));

            if (trustPassword == null)
                throw new ArgumentNullException(nameof(trustPassword));

            if (trustPassword.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(trustPassword));

            // verify first that the target forest name is valid
            Locator.GetDomainControllerInfo(null, targetForestName, null, (long)(PrivateLocatorFlags.DirectoryServicesRequired | PrivateLocatorFlags.GCRequired));

            DirectoryContext targetContext = Utils.GetNewDirectoryContext(targetForestName, DirectoryContextType.Forest, _context);

            TrustHelper.CreateTrust(_context, Name, targetContext, targetForestName, true, direction, trustPassword);
        }

        public void CreateTrustRelationship(Forest targetForest, TrustDirection direction)
        {
            CheckIfDisposed();

            if (targetForest == null)
                throw new ArgumentNullException(nameof(targetForest));

            if (direction < TrustDirection.Inbound || direction > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(TrustDirection));

            string password = TrustHelper.CreateTrustPassword();

            // first create trust on local side
            TrustHelper.CreateTrust(_context, Name, targetForest.GetDirectoryContext(), targetForest.Name, true, direction, password);

            // then create trust on remote side
            int reverseDirection = 0;
            if ((direction & TrustDirection.Inbound) != 0)
                reverseDirection |= (int)TrustDirection.Outbound;
            if ((direction & TrustDirection.Outbound) != 0)
                reverseDirection |= (int)TrustDirection.Inbound;

            TrustHelper.CreateTrust(targetForest.GetDirectoryContext(), targetForest.Name, _context, Name, true, (TrustDirection)reverseDirection, password);
        }

        public void UpdateLocalSideOfTrustRelationship(string targetForestName, string newTrustPassword)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            if (newTrustPassword == null)
                throw new ArgumentNullException(nameof(newTrustPassword));

            if (newTrustPassword.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(newTrustPassword));

            TrustHelper.UpdateTrust(_context, Name, targetForestName, newTrustPassword, true);
        }

        public void UpdateLocalSideOfTrustRelationship(string targetForestName, TrustDirection newTrustDirection, string newTrustPassword)
        {
            CheckIfDisposed();

            if (targetForestName == null)
                throw new ArgumentNullException(nameof(targetForestName));

            if (targetForestName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetForestName));

            if (newTrustDirection < TrustDirection.Inbound || newTrustDirection > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(newTrustDirection), (int)newTrustDirection, typeof(TrustDirection));

            if (newTrustPassword == null)
                throw new ArgumentNullException(nameof(newTrustPassword));

            if (newTrustPassword.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(newTrustPassword));

            TrustHelper.UpdateTrustDirection(_context, Name, targetForestName, newTrustPassword, true /*is forest*/, newTrustDirection);
        }

        public void UpdateTrustRelationship(Forest targetForest, TrustDirection newTrustDirection)
        {
            CheckIfDisposed();

            if (targetForest == null)
                throw new ArgumentNullException(nameof(targetForest));

            if (newTrustDirection < TrustDirection.Inbound || newTrustDirection > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(newTrustDirection), (int)newTrustDirection, typeof(TrustDirection));

            // no we generate trust password
            string password = TrustHelper.CreateTrustPassword();

            TrustHelper.UpdateTrustDirection(_context, Name, targetForest.Name, password, true /*is forest*/, newTrustDirection);

            // then create trust on remote side
            TrustDirection reverseDirection = 0;
            if ((newTrustDirection & TrustDirection.Inbound) != 0)
                reverseDirection |= TrustDirection.Outbound;
            if ((newTrustDirection & TrustDirection.Outbound) != 0)
                reverseDirection |= TrustDirection.Inbound;

            TrustHelper.UpdateTrustDirection(targetForest.GetDirectoryContext(), targetForest.Name, Name, password, true /*is forest*/, reverseDirection);
        }

        public void RepairTrustRelationship(Forest targetForest)
        {
            TrustDirection direction = TrustDirection.Bidirectional;
            CheckIfDisposed();

            if (targetForest == null)
                throw new ArgumentNullException(nameof(targetForest));

            // first try to reset the secure channel
            try
            {
                direction = GetTrustRelationship(targetForest.Name).TrustDirection;

                // verify outbound trust first
                if ((direction & TrustDirection.Outbound) != 0)
                {
                    TrustHelper.VerifyTrust(_context, Name, targetForest.Name, true /*is forest*/, TrustDirection.Outbound, true/*reset secure channel*/, null /* no need to go to specific server*/);
                }

                // verify inbound trust
                if ((direction & TrustDirection.Inbound) != 0)
                {
                    TrustHelper.VerifyTrust(targetForest.GetDirectoryContext(), targetForest.Name, Name, true/*is forest*/, TrustDirection.Outbound, true/*reset secure channel*/, null /* no need to go to specific server*/);
                }
            }
            catch (ActiveDirectoryOperationException)
            {
                RepairTrustHelper(targetForest, direction);
            }
            catch (UnauthorizedAccessException)
            {
                RepairTrustHelper(targetForest, direction);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetForest.Name, direction), typeof(ForestTrustRelationshipInformation), null);
            }
        }

        public static Forest GetCurrentForest() => GetForest(new DirectoryContext(DirectoryContextType.Forest));

        #endregion public methods

        #region public properties

        public string Name
        {
            get
            {
                CheckIfDisposed();
                return _forestDnsName;
            }
        }

        public ReadOnlySiteCollection Sites
        {
            get
            {
                CheckIfDisposed();
                return _cachedSites ??= new ReadOnlySiteCollection(GetSites());
            }
        }

        public DomainCollection Domains
        {
            get
            {
                CheckIfDisposed();
                return _cachedDomains ??= new DomainCollection(GetDomains());
            }
        }

        public GlobalCatalogCollection GlobalCatalogs
        {
            get
            {
                CheckIfDisposed();
                return _cachedGlobalCatalogs ??= FindAllGlobalCatalogs();
            }
        }

        public ApplicationPartitionCollection ApplicationPartitions
        {
            get
            {
                CheckIfDisposed();
                return _cachedApplicationPartitions ??= new ApplicationPartitionCollection(GetApplicationPartitions());
            }
        }

        public int ForestModeLevel
        {
            get
            {
                CheckIfDisposed();
                if (_forestModeLevel == -1)
                {
                    _forestModeLevel = GetForestModeLevel();
                }
                return _forestModeLevel;
            }
        }

        public ForestMode ForestMode
        {
            get
            {
                CheckIfDisposed();
                // if forest mode is known cast to proper enum
                if (ForestModeLevel <= (int)ForestMode.Windows2012R2Forest)
                {
                    return (ForestMode)ForestModeLevel;
                }
                // else return unknown
                return ForestMode.Unknown;
            }
        }

        public Domain RootDomain
        {
            get
            {
                CheckIfDisposed();
                if (_cachedRootDomain == null)
                {
                    // Domain context is created by passing the name of the forest
                    // (since the root domain and the forest have the same name)
                    DirectoryContext domainContext = Utils.GetNewDirectoryContext(Name, DirectoryContextType.Domain, _context);
                    _cachedRootDomain = new Domain(domainContext, Name);
                }
                return _cachedRootDomain;
            }
        }

        public ActiveDirectorySchema Schema
        {
            get
            {
                CheckIfDisposed();
                if (_cachedSchema == null)
                {
                    try
                    {
                        _cachedSchema = new ActiveDirectorySchema(_context, _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext));
                    }
                    catch (COMException e)
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                    }
                }
                return _cachedSchema;
            }
        }

        public DomainController SchemaRoleOwner
        {
            get
            {
                CheckIfDisposed();
                return _cachedSchemaRoleOwner ??= GetRoleOwner(ActiveDirectoryRole.SchemaRole);
            }
        }

        public DomainController NamingRoleOwner
        {
            get
            {
                CheckIfDisposed();
                return _cachedNamingRoleOwner ??= GetRoleOwner(ActiveDirectoryRole.NamingRole);
            }
        }

        #endregion public properties

        #region private methods

        internal DirectoryContext GetDirectoryContext() => _context;

        private int GetForestModeLevel()
        {
            int forestModeValue = -1;
            DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(_context, WellKnownDN.RootDSE);
            try
            {
                if (!rootDSE.Properties.Contains(PropertyManager.ForestFunctionality))
                {
                    // Since this value is not set, it is a Win2000 forest (with W2K schema)
                    forestModeValue = 0;
                }
                else
                {
                    forestModeValue = int.Parse((string)rootDSE.Properties[PropertyManager.ForestFunctionality].Value!, NumberFormatInfo.InvariantInfo);
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                rootDSE.Dispose();
            }
            return forestModeValue;
        }

        //
        // Returns a DomainController object for the DC that holds the specified FSMO role
        //
        private DomainController GetRoleOwner(ActiveDirectoryRole role)
        {
            DirectoryEntry? entry = null;
            string? dcName = null;

            try
            {
                switch (role)
                {
                    case ActiveDirectoryRole.SchemaRole:
                        {
                            entry = DirectoryEntryManager.GetDirectoryEntry(_context, _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext));
                            break;
                        }

                    case ActiveDirectoryRole.NamingRole:
                        {
                            entry = DirectoryEntryManager.GetDirectoryEntry(_context, _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));
                            break;
                        }

                    default:
                        // should not happen since we are calling this only internally
                        Debug.Fail("Forest.GetRoleOwner: Invalid role type.");
                        break;
                }

                dcName = Utils.GetDnsHostNameFromNTDSA(_context, (string)PropertyManager.GetPropertyValue(_context, entry, PropertyManager.FsmoRoleOwner)!);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                entry?.Dispose();
            }

            // create a new context object for the domain controller passing on  the
            // credentials from the forest context
            DirectoryContext dcContext = Utils.GetNewDirectoryContext(dcName, DirectoryContextType.DirectoryServer, _context);
            return new DomainController(dcContext, dcName);
        }

        private unsafe ArrayList GetSites()
        {
            ArrayList sites = new ArrayList();
            int result = 0;
            IntPtr dsHandle = IntPtr.Zero;
            IntPtr authIdentity = IntPtr.Zero;
            IntPtr sitesPtr = IntPtr.Zero;

            try
            {
                // get the directory handle
                GetDSHandle(out dsHandle, out authIdentity);

                // Get the sites within the forest
                // call DsListSites
                /*DWORD DsListSites(
                    HANDLE hDs,
                    PDS_NAME_RESULT* ppSites
                    );*/
                var dsListSites = (delegate* unmanaged<IntPtr, IntPtr*, int>)global::Interop.Kernel32.GetProcAddress(DirectoryContext.ADHandle, "DsListSitesW");
                if (dsListSites == null)
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastPInvokeError());
                }

                result = dsListSites(dsHandle, &sitesPtr);
                if (result == 0)
                {
                    try
                    {
                        DsNameResult dsNameResult = new DsNameResult();
                        Marshal.PtrToStructure(sitesPtr, dsNameResult);
                        IntPtr currentItem = dsNameResult.items;

                        for (int i = 0; i < dsNameResult.itemCount; i++)
                        {
                            DsNameResultItem dsNameResultItem = new DsNameResultItem();

                            Marshal.PtrToStructure(currentItem, dsNameResultItem);
                            if (dsNameResultItem.status == NativeMethods.DS_NAME_NO_ERROR)
                            {
                                string siteName = Utils.GetDNComponents(dsNameResultItem.name!)[0].Value!;
                                // an existing site
                                sites.Add(new ActiveDirectorySite(_context, siteName, true));
                            }

                            currentItem = IntPtr.Add(currentItem, Marshal.SizeOf(dsNameResultItem));
                        }
                    }
                    finally
                    {
                        // free the DsNameResult structure
                        if (sitesPtr != IntPtr.Zero)
                        {
                            // call DsFreeNameResultW
                            var dsFreeNameResultW = (delegate* unmanaged<IntPtr, void>)global::Interop.Kernel32.GetProcAddress(DirectoryContext.ADHandle, "DsFreeNameResultW");
                            if (dsFreeNameResultW == null)
                            {
                                throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastPInvokeError());
                            }
                            dsFreeNameResultW(sitesPtr);
                        }
                    }
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(result, _context.GetServerName());
                }
            }
            finally
            {
                // DsUnbind
                if (dsHandle != (IntPtr)0)
                {
                    Utils.FreeDSHandle(dsHandle, DirectoryContext.ADHandle);
                }

                // free the credentials object
                if (authIdentity != (IntPtr)0)
                {
                    Utils.FreeAuthIdentity(authIdentity, DirectoryContext.ADHandle);
                }
            }

            return sites;
        }

        private ArrayList GetApplicationPartitions()
        {
            ArrayList appNCs = new ArrayList();

            DirectoryEntry partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(_context, _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));

            // search for all the "crossRef" objects that have the
            // ADS_SYSTEMFLAG_CR_NTDS_NC set and the SYSTEMFLAG_CR_NTDS_DOMAIN flag not set
            // (one-level search is good enough)

            // setup the directory searcher object

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
            str.Append(")))");

            string filter = str.ToString();
            string[] propertiesToLoad = new string[2];
            propertiesToLoad[0] = PropertyManager.DnsRoot;
            propertiesToLoad[1] = PropertyManager.NCName;

            ADSearcher searcher = new ADSearcher(partitionsEntry, filter, propertiesToLoad, SearchScope.OneLevel);
            SearchResultCollection? resCol = null;
            try
            {
                resCol = searcher.FindAll();

                string schemaNamingContext = _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext);
                string configurationNamingContext = _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.ConfigurationNamingContext);

                foreach (SearchResult res in resCol)
                {
                    // add the name of the appNC only if it is not
                    // the Schema or Configuration partition
                    string nCName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.NCName)!;
                    if ((!(nCName.Equals(schemaNamingContext)))
                        && (!(nCName.Equals(configurationNamingContext))))
                    {
                        // create a new context to be passed on to the appNC object
                        // (pass the dns name of the appliction partition as the target)
                        string dnsName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.DnsRoot)!;
                        DirectoryContext appNCContext = Utils.GetNewDirectoryContext(dnsName, DirectoryContextType.ApplicationPartition, _context);
                        appNCs.Add(new ApplicationPartition(appNCContext, nCName, (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.DnsRoot)!, ApplicationPartitionType.ADApplicationPartition, new DirectoryEntryManager(appNCContext)));
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                resCol?.Dispose();
                partitionsEntry.Dispose();
            }
            return appNCs;
        }

        private ArrayList GetDomains()
        {
            ArrayList domains = new ArrayList();
            DirectoryEntry partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(_context, _directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));

            // search for all the "crossRef" objects that have the
            // ADS_SYSTEMFLAG_CR_NTDS_NC and SYSTEMFLAG_CR_NTDS_DOMAIN flags set
            // (one-level search is good enough)

            // setup the directory searcher object

            // build the filter
            StringBuilder str = new StringBuilder(15);
            str.Append("(&(");
            str.Append(PropertyManager.ObjectCategory);
            str.Append("=crossRef)(");
            str.Append(PropertyManager.SystemFlags);
            str.Append(":1.2.840.113556.1.4.804:=");
            str.Append((int)SystemFlag.SystemFlagNtdsNC);
            str.Append(")(");
            str.Append(PropertyManager.SystemFlags);
            str.Append(":1.2.840.113556.1.4.804:=");
            str.Append((int)SystemFlag.SystemFlagNtdsDomain);
            str.Append("))");

            string filter = str.ToString();
            string[] propertiesToLoad = new string[1];
            propertiesToLoad[0] = PropertyManager.DnsRoot;

            ADSearcher searcher = new ADSearcher(partitionsEntry, filter, propertiesToLoad, SearchScope.OneLevel);
            SearchResultCollection? resCol = null;
            try
            {
                resCol = searcher.FindAll();

                foreach (SearchResult res in resCol)
                {
                    string domainName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.DnsRoot)!;
                    DirectoryContext domainContext = Utils.GetNewDirectoryContext(domainName, DirectoryContextType.Domain, _context);
                    domains.Add(new Domain(domainContext, domainName));
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                resCol?.Dispose();
                partitionsEntry.Dispose();
            }
            return domains;
        }

        private void GetDSHandle(out IntPtr dsHandle, out IntPtr authIdentity)
        {
            authIdentity = Utils.GetAuthIdentity(_context, DirectoryContext.ADHandle);

            // DsBind
            if (_context.ContextType == DirectoryContextType.DirectoryServer)
            {
                dsHandle = Utils.GetDSHandle(_context.GetServerName(), null, authIdentity, DirectoryContext.ADHandle);
            }
            else
            {
                dsHandle = Utils.GetDSHandle(null, _context.GetServerName(), authIdentity, DirectoryContext.ADHandle);
            }
        }

        private void CheckIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private unsafe TrustRelationshipInformationCollection GetTrustsHelper(string? targetForestName)
        {
            string? serverName = null;
            IntPtr domains = (IntPtr)0;
            int count = 0;
            TrustRelationshipInformationCollection collection = new TrustRelationshipInformationCollection();
            bool impersonated = false;
            int error = 0;

            // first decide which server to go to
            serverName = Utils.GetPolicyServerName(_context, true, false, Name);

            // impersonate appropriately
            impersonated = Utils.Impersonate(_context);

            // call the DS API to get trust domain information
            try
            {
                try
                {
                    error = Interop.Netapi32.DsEnumerateDomainTrustsW(serverName, Interop.Netapi32.DS_DOMAINTRUST_FLAG.DS_DOMAIN_PRIMARY | Interop.Netapi32.DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_OUTBOUND | Interop.Netapi32.DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_INBOUND, out domains, out count);
                }
                finally
                {
                    if (impersonated)
                        Utils.Revert();
                }
            }
            catch { throw; }

            // check the result
            if (error != 0)
                throw ExceptionHelper.GetExceptionFromErrorCode(error, serverName);

            try
            {
                // now enumerate through the collection
                if (domains != (IntPtr)0 && count != 0)
                {
                    IntPtr addr = (IntPtr)0;
                    for (int i = 0; i < count; i++)
                    {
                        addr = IntPtr.Add(domains, i * sizeof(Interop.Netapi32.DS_DOMAIN_TRUSTS));
                        Interop.Netapi32.DS_DOMAIN_TRUSTS unmanagedTrust = *(Interop.Netapi32.DS_DOMAIN_TRUSTS*)addr;

                        // whether this is the case that a paticular forest trust info is needed
                        if (targetForestName != null)
                        {
                            bool sameTarget = false;
                            string? tmpDNSName = null;
                            string? tmpNetBIOSName = null;

                            if (unmanagedTrust.DnsDomainName != (IntPtr)0)
                                tmpDNSName = Marshal.PtrToStringUni(unmanagedTrust.DnsDomainName);
                            if (unmanagedTrust.NetbiosDomainName != (IntPtr)0)
                                tmpNetBIOSName = Marshal.PtrToStringUni(unmanagedTrust.NetbiosDomainName);

                            // check whether it is the same target
                            if (tmpDNSName != null && Utils.Compare(targetForestName, tmpDNSName) == 0)
                                sameTarget = true;
                            else if (tmpNetBIOSName != null && Utils.Compare(targetForestName, tmpNetBIOSName) == 0)
                                sameTarget = true;

                            if (!sameTarget)
                                continue;
                        }

                        // if it is up level trust and forest attribute is set
                        if (unmanagedTrust.TrustType == TrustHelper.TRUST_TYPE_UPLEVEL && ((unmanagedTrust.TrustAttributes & (int)Interop.Advapi32.TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_FOREST_TRANSITIVE) != 0))
                        {
                            // we don't want to include self
                            if ((unmanagedTrust.Flags & Interop.Netapi32.DS_DOMAINTRUST_FLAG.DS_DOMAIN_PRIMARY) != 0)
                                continue;

                            TrustRelationshipInformation trust = new ForestTrustRelationshipInformation(_context, Name, unmanagedTrust, TrustType.Forest);
                            collection.Add(trust);
                        }
                    }
                }
                return collection;
            }
            finally
            {
                if (domains != (IntPtr)0)
                    Interop.Netapi32.NetApiBufferFree(domains);
            }
        }

        private void RepairTrustHelper(Forest targetForest, TrustDirection direction)
        {
            // no we try changing trust password on both sides
            string password = TrustHelper.CreateTrustPassword();

            // first reset trust password on remote side
            string targetServerName = TrustHelper.UpdateTrust(targetForest.GetDirectoryContext(), targetForest.Name, Name, password, true /*is forest*/);

            // then reset trust password on local side
            string sourceServerName = TrustHelper.UpdateTrust(_context, Name, targetForest.Name, password, true /*is forest*/);

            // last we reset the secure channel again to make sure info is replicated and trust is indeed ready now

            // verify outbound trust first
            if ((direction & TrustDirection.Outbound) != 0)
            {
                try
                {
                    TrustHelper.VerifyTrust(_context, Name, targetForest.Name, true /*is forest*/, TrustDirection.Outbound, true/*reset secure channel*/, targetServerName /* need to specify which target server */);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetForest.Name, direction), typeof(ForestTrustRelationshipInformation), null);
                }
            }

            // verify inbound trust
            if ((direction & TrustDirection.Inbound) != 0)
            {
                try
                {
                    TrustHelper.VerifyTrust(targetForest.GetDirectoryContext(), targetForest.Name, Name, true/*is forest*/, TrustDirection.Outbound, true/*reset secure channel*/, sourceServerName /* need to specify which target server */);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetForest.Name, direction), typeof(ForestTrustRelationshipInformation), null);
                }
            }
        }

        #endregion private methods
    }
}
