// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Collections;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.DirectoryServices.ActiveDirectory
{
    public enum DomainMode : int
    {
        Unknown = -1,
        Windows2000MixedDomain = 0,   // win2000, win2003, NT
        Windows2000NativeDomain = 1,  // win2000, win2003
        Windows2003InterimDomain = 2, // win2003, NT
        Windows2003Domain = 3,        // win2003
        Windows2008Domain = 4,         // win2008
        Windows2008R2Domain = 5,         // win2008 R2
        Windows8Domain = 6,             //Windows Server 2012
        Windows2012R2Domain = 7,             //Windows Server 2012 R2
    }

    public class Domain : ActiveDirectoryPartition
    {
        /// Private Variables
        private string? _crossRefDN;
        private string? _trustParent;

        // internal variables corresponding to public properties
        private DomainControllerCollection? _cachedDomainControllers;
        private DomainCollection? _cachedChildren;
        private DomainMode _currentDomainMode = (DomainMode)(-1);
        private int _domainModeLevel = -1;
        private DomainController? _cachedPdcRoleOwner;
        private DomainController? _cachedRidRoleOwner;
        private DomainController? _cachedInfrastructureRoleOwner;
        private Domain? _cachedParent;
        private Forest? _cachedForest;
        // this is needed because null value for parent is valid
        private bool _isParentInitialized;

        #region constructors
        // internal constructors
        internal Domain(DirectoryContext context, string domainName, DirectoryEntryManager directoryEntryMgr)
            : base(context, domainName)
        {
            this.directoryEntryMgr = directoryEntryMgr;
        }
        internal Domain(DirectoryContext context, string domainName)
            : this(context, domainName, new DirectoryEntryManager(context))
        {
        }
        #endregion constructors

        #region public methods

        public static Domain GetDomain(DirectoryContext context)
        {
            // check that the argument is not null
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // contexttype should be Domain or DirectoryServer
            if ((context.ContextType != DirectoryContextType.Domain) &&
                (context.ContextType != DirectoryContextType.DirectoryServer))
            {
                throw new ArgumentException(SR.TargetShouldBeServerORDomain, nameof(context));
            }

            if ((context.Name == null) && (!context.isDomain()))
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.ContextNotAssociatedWithDomain, typeof(Domain), null);
            }

            if (context.Name != null)
            {
                // the target should be a valid domain name or a server
                if (!((context.isDomain()) || (context.isServer())))
                {
                    if (context.ContextType == DirectoryContextType.Domain)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.DomainNotFound, typeof(Domain), context.Name);
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(Domain), null);
                    }
                }
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the rootDSE of the domain specified in the context
            // and get the dns name
            DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(context);
            DirectoryEntry? rootDSE = null;
            string? defaultDomainNC = null;
            try
            {
                rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                if ((context.isServer()) && (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectory)))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(Domain), null);
                }
                defaultDomainNC = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DefaultNamingContext)!;
            }
            catch (COMException e)
            {
                int errorCode = e.ErrorCode;

                if (errorCode == unchecked((int)0x8007203a))
                {
                    if (context.ContextType == DirectoryContextType.Domain)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.DomainNotFound, typeof(Domain), context.Name);
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DCNotFound, context.Name), typeof(Domain), null);
                    }
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            // return domain object
            return new Domain(context, Utils.GetDnsNameFromDN(defaultDomainNC), directoryEntryMgr);
        }

        public static Domain GetComputerDomain()
        {
            string? computerDomainName = DirectoryContext.GetDnsDomainName(null);
            if (computerDomainName == null)
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.ComputerNotJoinedToDomain, typeof(Domain), null);
            }

            return Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, computerDomainName));
        }

        public void RaiseDomainFunctionalityLevel(int domainMode)
        {
            int existingDomainModeLevel;
            CheckIfDisposed();

            // check if domainMode is within the valid range
            if (domainMode < 0)
            {
                throw new ArgumentException(SR.InvalidMode, nameof(domainMode));
            }

            // get the current domain mode
            existingDomainModeLevel = DomainModeLevel;

            if (existingDomainModeLevel >= domainMode)
            {
                throw new ArgumentException(SR.InvalidMode, nameof(domainMode));
            }

            DomainMode existingDomainMode = DomainMode;

            // set the forest mode on AD
            DirectoryEntry? domainEntry = null;

            // CurrentDomain          Valid domainMode      Action
            // -----------------
            // Windows2000Mixed        0                      ntMixedDomain = 0  msDS-Behavior-Version = 0
            // Windows2000Mixed        1                                         msDS-Behavior-Version = 1
            // Windows2000Mixed        2                      ntMixedDomain = 0, msDS-Behavior-Version = 2
            //
            // Windows2003Interim      2                      ntMixedDomain = 0, msDS-Behavior-Version = 2
            //
            // Rest                2 or above                 msDS-Behavior-Version = domainMode
            try
            {
                domainEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.DefaultNamingContext));

                // set the new functional level
                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = domainMode;

                switch (existingDomainMode)
                {
                    case DomainMode.Windows2000MixedDomain:
                        {
                            if (domainMode == 2 || domainMode == 0)
                            {
                                domainEntry.Properties[PropertyManager.NTMixedDomain].Value = 0;
                            }
                            else if (domainMode > 2) // new level should be less than or equal to Windows2003
                            {
                                throw new ArgumentException(SR.InvalidMode, nameof(domainMode));
                            }
                            break;
                        }

                    case DomainMode.Windows2003InterimDomain:
                        {
                            if (domainMode == 2) // only Windows2003 allowed
                            {
                                domainEntry.Properties[PropertyManager.NTMixedDomain].Value = 0;
                            }
                            else
                            {
                                throw new ArgumentException(SR.InvalidMode, nameof(domainMode));
                            }

                            break;
                        }

                    default:
                        break;
                }

                // NOTE:
                // If the domain controller we are talking to is W2K
                // (more specifically the schema is a W2K schema) then the
                // msDS-Behavior-Version attribute will not be present.
                // If that is the case, the domain functionality cannot be raised
                // to Windows2003InterimDomain or Windows2003Domain (which is when we would set this attribute)
                // since there are only W2K domain controllers
                // So, we catch that exception and throw a more meaningful one.
                domainEntry.CommitChanges();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x8007200A))
                {
                    // attribute does not exist which means this is not a W2K3 DC
                    // cannot raise domain functionality
                    throw new ArgumentException(SR.NoW2K3DCs, nameof(domainMode));
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            finally
            {
                domainEntry?.Dispose();
            }

            // at this point the raise domain function has succeeded
            // invalidate the domain mode so that we get it from the server the next time
            _currentDomainMode = (DomainMode)(-1);
            _domainModeLevel = -1;
        }

        public void RaiseDomainFunctionality(DomainMode domainMode)
        {
            DomainMode existingDomainMode;
            CheckIfDisposed();

            // check if domain mode is within the valid range
            if (domainMode < DomainMode.Windows2000MixedDomain || domainMode > DomainMode.Windows2012R2Domain)
            {
                throw new InvalidEnumArgumentException(nameof(domainMode), (int)domainMode, typeof(DomainMode));
            }

            // get the current domain mode
            existingDomainMode = GetDomainMode();

            // set the forest mode on AD
            DirectoryEntry? domainEntry = null;

            // CurrentDomain          Valid RequestedDomain      Action
            // -----------------
            // Windows2000Mixed        Windows2000Native         ntMixedDomain = 0
            // Windows2000Mixed        Windows2003Interim        msDS-Behavior-Version = 1
            // Windows2000Mixed        Windows2003               ntMixedDomain = 0, msDS-Behavior-Version = 2
            //
            // Windows2003Interim      Windows2003               ntMixedDomain = 0, msDS-Behavior-Version = 2
            //
            // Windows2000Native       Windows2003 or above
            // Windows2003             Windows2008 or above
            // Windows2008             Windows2008R2 or above
            // Windows2008R2           Windows2012 or above
            // Windows2012             Windows2012R2 or above
            // Windows2012R2           ERROR

            try
            {
                domainEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.DefaultNamingContext));

                switch (existingDomainMode)
                {
                    case DomainMode.Windows2000MixedDomain:
                        {
                            if (domainMode == DomainMode.Windows2000NativeDomain)
                            {
                                domainEntry.Properties[PropertyManager.NTMixedDomain].Value = 0;
                            }
                            else if (domainMode == DomainMode.Windows2003InterimDomain)
                            {
                                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = 1;
                            }
                            else if (domainMode == DomainMode.Windows2003Domain)
                            {
                                domainEntry.Properties[PropertyManager.NTMixedDomain].Value = 0;
                                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = 2;
                            }
                            else
                            {
                                throw new ArgumentException(SR.InvalidMode, nameof(domainMode));
                            }

                            break;
                        }

                    case DomainMode.Windows2003InterimDomain:
                        {
                            if (domainMode == DomainMode.Windows2003Domain)
                            {
                                domainEntry.Properties[PropertyManager.NTMixedDomain].Value = 0;
                                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = 2;
                            }
                            else
                            {
                                throw new ArgumentException(SR.InvalidMode, nameof(domainMode));
                            }

                            break;
                        }

                    case DomainMode.Windows2000NativeDomain:
                    case DomainMode.Windows2003Domain:
                    case DomainMode.Windows2008Domain:
                    case DomainMode.Windows2008R2Domain:
                    case DomainMode.Windows8Domain:
                    case DomainMode.Windows2012R2Domain:
                        {
                            if (existingDomainMode >= domainMode)
                            {
                                throw new ArgumentException(SR.InvalidMode, nameof(domainMode));
                            }

                            if (domainMode == DomainMode.Windows2003Domain)
                            {
                                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = 2;
                            }
                            else if (domainMode == DomainMode.Windows2008Domain)
                            {
                                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = 3;
                            }
                            else if (domainMode == DomainMode.Windows2008R2Domain)
                            {
                                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = 4;
                            }
                            else if (domainMode == DomainMode.Windows8Domain)
                            {
                                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = 5;
                            }
                            else if (domainMode == DomainMode.Windows2012R2Domain)
                            {
                                domainEntry.Properties[PropertyManager.MsDSBehaviorVersion].Value = 6;
                            }
                            else
                            {
                                throw new ArgumentException(SR.InvalidMode, nameof(domainMode));
                            }
                        }
                        break;
                    default:
                        {
                            // should not happen
                            throw new ActiveDirectoryOperationException();
                        }
                }

                // NOTE:
                // If the domain controller we are talking to is W2K
                // (more specifically the schema is a W2K schema) then the
                // msDS-Behavior-Version attribute will not be present.
                // If that is the case, the domain functionality cannot be raised
                // to Windows2003InterimDomain or Windows2003Domain (which is when we would set this attribute)
                // since there are only W2K domain controllers
                // So, we catch that exception and throw a more meaningful one.
                domainEntry.CommitChanges();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x8007200A))
                {
                    // attribute does not exist which means this is not a W2K3 DC
                    // cannot raise domain functionality
                    throw new ArgumentException(SR.NoW2K3DCs, nameof(domainMode));
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            finally
            {
                domainEntry?.Dispose();
            }

            // at this point the raise domain function has succeeded
            // invalidate the domain mode so that we get it from the server the next time
            _currentDomainMode = (DomainMode)(-1);
            _domainModeLevel = -1;
        }

        public DomainController FindDomainController()
        {
            CheckIfDisposed();

            return DomainController.FindOneInternal(context, Name, null, 0);
        }

        public DomainController FindDomainController(string siteName)
        {
            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return DomainController.FindOneInternal(context, Name, siteName, 0);
        }

        public DomainController FindDomainController(LocatorOptions flag)
        {
            CheckIfDisposed();

            return DomainController.FindOneInternal(context, Name, null, flag);
        }

        public DomainController FindDomainController(string siteName, LocatorOptions flag)
        {
            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return DomainController.FindOneInternal(context, Name, siteName, flag);
        }

        public DomainControllerCollection FindAllDomainControllers()
        {
            CheckIfDisposed();

            return DomainController.FindAllInternal(context, Name, true /*isDnsDomainName */, null);
        }

        public DomainControllerCollection FindAllDomainControllers(string siteName)
        {
            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return DomainController.FindAllInternal(context, Name, true /*isDnsDomainName */, siteName);
        }

        public DomainControllerCollection FindAllDiscoverableDomainControllers()
        {
            long flag = (long)PrivateLocatorFlags.DSWriteableRequired;

            CheckIfDisposed();
            return new DomainControllerCollection(Locator.EnumerateDomainControllers(context, Name, null, (long)flag));
        }

        public DomainControllerCollection FindAllDiscoverableDomainControllers(string siteName)
        {
            long flag = (long)PrivateLocatorFlags.DSWriteableRequired;

            CheckIfDisposed();

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            if (siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
            }

            return new DomainControllerCollection(Locator.EnumerateDomainControllers(context, Name, siteName, (long)flag));
        }

        public override DirectoryEntry GetDirectoryEntry()
        {
            CheckIfDisposed();
            return DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.DefaultNamingContext));
        }

        public TrustRelationshipInformationCollection GetAllTrustRelationships()
        {
            CheckIfDisposed();

            ArrayList trusts = GetTrustsHelper(null);
            TrustRelationshipInformationCollection collection = new TrustRelationshipInformationCollection(context, Name, trusts);
            return collection;
        }

        public TrustRelationshipInformation GetTrustRelationship(string targetDomainName)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            ArrayList trusts = GetTrustsHelper(targetDomainName);
            TrustRelationshipInformationCollection collection = new TrustRelationshipInformationCollection(context, Name, trusts);
            if (collection.Count == 0)
            {
                // trust relationship does not exist
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.DomainTrustDoesNotExist, Name, targetDomainName), typeof(TrustRelationshipInformation), null);
            }
            else
            {
                Debug.Assert(collection.Count == 1);
                return collection[0];
            }
        }

        public bool GetSelectiveAuthenticationStatus(string targetDomainName)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            return TrustHelper.GetTrustedDomainInfoStatus(context, Name, targetDomainName, TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION, false);
        }

        public void SetSelectiveAuthenticationStatus(string targetDomainName, bool enable)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            TrustHelper.SetTrustedDomainInfoStatus(context, Name, targetDomainName, TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_CROSS_ORGANIZATION, enable, false);
        }

        public bool GetSidFilteringStatus(string targetDomainName)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            return TrustHelper.GetTrustedDomainInfoStatus(context, Name, targetDomainName, TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN, false);
        }

        public void SetSidFilteringStatus(string targetDomainName, bool enable)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            TrustHelper.SetTrustedDomainInfoStatus(context, Name, targetDomainName, TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_QUARANTINED_DOMAIN, enable, false);
        }

        public void DeleteLocalSideOfTrustRelationship(string targetDomainName)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            // delete local side of trust only
            TrustHelper.DeleteTrust(context, Name, targetDomainName, false);
        }

        public void DeleteTrustRelationship(Domain targetDomain)
        {
            CheckIfDisposed();

            if (targetDomain == null)
                throw new ArgumentNullException(nameof(targetDomain));

            // first delete the trust on the remote side
            TrustHelper.DeleteTrust(targetDomain.GetDirectoryContext(), targetDomain.Name, Name, false);

            // then delete the local side trust
            TrustHelper.DeleteTrust(context, Name, targetDomain.Name, false);
        }

        public void VerifyOutboundTrustRelationship(string targetDomainName)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            TrustHelper.VerifyTrust(context, Name, targetDomainName, false/*not forest*/, TrustDirection.Outbound, false/*just TC verification*/, null /* no need to go to specific server*/);
        }

        public void VerifyTrustRelationship(Domain targetDomain, TrustDirection direction)
        {
            CheckIfDisposed();

            if (targetDomain == null)
                throw new ArgumentNullException(nameof(targetDomain));

            if (direction < TrustDirection.Inbound || direction > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(TrustDirection));

            // verify outbound trust first
            if ((direction & TrustDirection.Outbound) != 0)
            {
                try
                {
                    TrustHelper.VerifyTrust(context, Name, targetDomain.Name, false/*not forest*/, TrustDirection.Outbound, false/*just TC verification*/, null /* no need to go to specific server*/);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetDomain.Name, direction), typeof(TrustRelationshipInformation), null);
                }
            }

            // verify inbound trust
            if ((direction & TrustDirection.Inbound) != 0)
            {
                try
                {
                    TrustHelper.VerifyTrust(targetDomain.GetDirectoryContext(), targetDomain.Name, Name, false/*not forest*/, TrustDirection.Outbound, false/*just TC verification*/, null /* no need to go to specific server*/);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetDomain.Name, direction), typeof(TrustRelationshipInformation), null);
                }
            }
        }

        public void CreateLocalSideOfTrustRelationship(string targetDomainName, TrustDirection direction, string trustPassword)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            if (direction < TrustDirection.Inbound || direction > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(TrustDirection));

            if (trustPassword == null)
                throw new ArgumentNullException(nameof(trustPassword));

            if (trustPassword.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(trustPassword));

            // verify first that the target domain name is valid
            Locator.GetDomainControllerInfo(null, targetDomainName, null, (long)PrivateLocatorFlags.DirectoryServicesRequired);

            DirectoryContext targetContext = Utils.GetNewDirectoryContext(targetDomainName, DirectoryContextType.Domain, context);

            TrustHelper.CreateTrust(context, Name, targetContext, targetDomainName, false, direction, trustPassword);
        }

        public void CreateTrustRelationship(Domain targetDomain, TrustDirection direction)
        {
            CheckIfDisposed();

            if (targetDomain == null)
                throw new ArgumentNullException(nameof(targetDomain));

            if (direction < TrustDirection.Inbound || direction > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(TrustDirection));

            string password = TrustHelper.CreateTrustPassword();

            // first create trust on local side
            TrustHelper.CreateTrust(context, Name, targetDomain.GetDirectoryContext(), targetDomain.Name, false, direction, password);

            // then create trust on remote side
            int reverseDirection = 0;
            if ((direction & TrustDirection.Inbound) != 0)
                reverseDirection |= (int)TrustDirection.Outbound;
            if ((direction & TrustDirection.Outbound) != 0)
                reverseDirection |= (int)TrustDirection.Inbound;

            TrustHelper.CreateTrust(targetDomain.GetDirectoryContext(), targetDomain.Name, context, Name, false, (TrustDirection)reverseDirection, password);
        }

        public void UpdateLocalSideOfTrustRelationship(string targetDomainName, string newTrustPassword)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            if (newTrustPassword == null)
                throw new ArgumentNullException(nameof(newTrustPassword));

            if (newTrustPassword.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(newTrustPassword));

            TrustHelper.UpdateTrust(context, Name, targetDomainName, newTrustPassword, false);
        }

        public void UpdateLocalSideOfTrustRelationship(string targetDomainName, TrustDirection newTrustDirection, string newTrustPassword)
        {
            CheckIfDisposed();

            if (targetDomainName == null)
                throw new ArgumentNullException(nameof(targetDomainName));

            if (targetDomainName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(targetDomainName));

            if (newTrustDirection < TrustDirection.Inbound || newTrustDirection > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(newTrustDirection), (int)newTrustDirection, typeof(TrustDirection));

            if (newTrustPassword == null)
                throw new ArgumentNullException(nameof(newTrustPassword));

            if (newTrustPassword.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(newTrustPassword));

            TrustHelper.UpdateTrustDirection(context, Name, targetDomainName, newTrustPassword, false /*not a forest*/, newTrustDirection);
        }

        public void UpdateTrustRelationship(Domain targetDomain, TrustDirection newTrustDirection)
        {
            CheckIfDisposed();

            if (targetDomain == null)
                throw new ArgumentNullException(nameof(targetDomain));

            if (newTrustDirection < TrustDirection.Inbound || newTrustDirection > TrustDirection.Bidirectional)
                throw new InvalidEnumArgumentException(nameof(newTrustDirection), (int)newTrustDirection, typeof(TrustDirection));

            // no we generate trust password
            string password = TrustHelper.CreateTrustPassword();

            TrustHelper.UpdateTrustDirection(context, Name, targetDomain.Name, password, false /* not a forest */, newTrustDirection);

            // then create trust on remote side
            TrustDirection reverseDirection = 0;
            if ((newTrustDirection & TrustDirection.Inbound) != 0)
                reverseDirection |= TrustDirection.Outbound;
            if ((newTrustDirection & TrustDirection.Outbound) != 0)
                reverseDirection |= TrustDirection.Inbound;

            TrustHelper.UpdateTrustDirection(targetDomain.GetDirectoryContext(), targetDomain.Name, Name, password, false /* not a forest */, reverseDirection);
        }

        public void RepairTrustRelationship(Domain targetDomain)
        {
            TrustDirection direction = TrustDirection.Bidirectional;

            CheckIfDisposed();

            if (targetDomain == null)
                throw new ArgumentNullException(nameof(targetDomain));

            // first try to reset the secure channel
            try
            {
                direction = GetTrustRelationship(targetDomain.Name!).TrustDirection;

                // verify outbound trust first
                if ((direction & TrustDirection.Outbound) != 0)
                {
                    TrustHelper.VerifyTrust(context, Name, targetDomain.Name, false /*not forest*/, TrustDirection.Outbound, true /*reset secure channel*/, null /* no need to go to specific server*/);
                }

                // verify inbound trust
                if ((direction & TrustDirection.Inbound) != 0)
                {
                    TrustHelper.VerifyTrust(targetDomain.GetDirectoryContext(), targetDomain.Name, Name, false /*not forest*/, TrustDirection.Outbound, true/*reset secure channel*/, null /* no need to go to specific server*/);
                }
            }
            catch (ActiveDirectoryOperationException)
            {
                // secure channel setup fails
                RepairTrustHelper(targetDomain, direction);
            }
            catch (UnauthorizedAccessException)
            {
                // trust password does not match
                RepairTrustHelper(targetDomain, direction);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetDomain.Name, direction), typeof(TrustRelationshipInformation), null);
            }
        }

        public static Domain GetCurrentDomain()
        {
            return Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain));
        }

        #endregion public methods

        #region public properties

        public Forest Forest
        {
            get
            {
                CheckIfDisposed();
                if (_cachedForest == null)
                {
                    // get the name of rootDomainNamingContext
                    DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);
                    string rootDomainNC = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.RootDomainNamingContext)!;
                    string? forestName = Utils.GetDnsNameFromDN(rootDomainNC);
                    DirectoryContext forestContext = Utils.GetNewDirectoryContext(forestName, DirectoryContextType.Forest, context);
                    _cachedForest = new Forest(forestContext, forestName);
                }
                return _cachedForest;
            }
        }

        public DomainControllerCollection DomainControllers
        {
            get
            {
                CheckIfDisposed();
                return _cachedDomainControllers ??= FindAllDomainControllers();
            }
        }

        public DomainCollection Children
        {
            get
            {
                CheckIfDisposed();
                return _cachedChildren ??= new DomainCollection(GetChildDomains());
            }
        }

        public DomainMode DomainMode
        {
            get
            {
                CheckIfDisposed();
                if ((int)_currentDomainMode == -1)
                {
                    _currentDomainMode = GetDomainMode();
                }
                return _currentDomainMode;
            }
        }

        public int DomainModeLevel
        {
            get
            {
                CheckIfDisposed();
                if (_domainModeLevel == -1)
                {
                    _domainModeLevel = GetDomainModeLevel();
                }
                return _domainModeLevel;
            }
        }

        public Domain? Parent
        {
            get
            {
                CheckIfDisposed();
                if (!_isParentInitialized)
                {
                    _cachedParent = GetParent();
                    _isParentInitialized = true;
                }
                return _cachedParent;
            }
        }

        public DomainController PdcRoleOwner
        {
            get
            {
                CheckIfDisposed();
                _cachedPdcRoleOwner ??= GetRoleOwner(ActiveDirectoryRole.PdcRole);
                return _cachedPdcRoleOwner;
            }
        }

        public DomainController RidRoleOwner
        {
            get
            {
                CheckIfDisposed();
                return _cachedRidRoleOwner ??= GetRoleOwner(ActiveDirectoryRole.RidRole);
            }
        }

        public DomainController InfrastructureRoleOwner
        {
            get
            {
                CheckIfDisposed();
                return _cachedInfrastructureRoleOwner ??= GetRoleOwner(ActiveDirectoryRole.InfrastructureRole);
            }
        }

        #endregion public properties

        #region private methods

        internal DirectoryContext GetDirectoryContext() => context;

        private int GetDomainModeLevel()
        {
            DirectoryEntry? domainEntry = null;
            DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
            int domainFunctionality = 0;

            try
            {
                if (rootDSE.Properties.Contains(PropertyManager.DomainFunctionality))
                {
                    domainFunctionality = int.Parse((string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DomainFunctionality)!, NumberFormatInfo.InvariantInfo);
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                rootDSE.Dispose();
                domainEntry?.Dispose();
            }
            return domainFunctionality;
        }

        private DomainMode GetDomainMode()
        {
            // logic to check the domain mode
            // if domainFunctionality is 0,
            //            then check ntMixedDomain to differentiate between
            //            Windows2000Native and Windows2000Mixed
            // if domainFunctionality is 1 ==> Windows2003Interim
            // if domainFunctionality is 2 ==> Windows2003
            // if domainFunctionality is 3 ==> Windows2008
            // if domainFunctionality is 4 ==> Windows2008R2
            // if domainFunctionality is 5 ==> Windows2012
            // if domainFunctionality is 6 ==> Windows2012R2
            DomainMode domainMode;
            DirectoryEntry? domainEntry = null;
            int domainFunctionality = DomainModeLevel;

            try
            {
                // If the "domainFunctionality" attribute is not set on the rootdse, then
                // this is a W2K domain (with W2K schema) so just check for mixed or native
                switch (domainFunctionality)
                {
                    case 0:
                        {
                            domainEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.DefaultNamingContext));
                            int ntMixedDomain = (int)PropertyManager.GetPropertyValue(context, domainEntry, PropertyManager.NTMixedDomain)!;

                            if (ntMixedDomain == 0)
                            {
                                domainMode = DomainMode.Windows2000NativeDomain;
                            }
                            else
                            {
                                domainMode = DomainMode.Windows2000MixedDomain;
                            }
                            break;
                        }

                    case 1:
                        domainMode = DomainMode.Windows2003InterimDomain;
                        break;

                    case 2:
                        domainMode = DomainMode.Windows2003Domain;
                        break;

                    case 3:
                        domainMode = DomainMode.Windows2008Domain;
                        break;
                    case 4:
                        domainMode = DomainMode.Windows2008R2Domain;
                        break;
                    case 5:
                        domainMode = DomainMode.Windows8Domain;
                        break;
                    case 6:
                        domainMode = DomainMode.Windows2012R2Domain;
                        break;
                    default:
                        // unrecognized domain mode
                        domainMode = DomainMode.Unknown;
                        break;
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                domainEntry?.Dispose();
            }
            return domainMode;
        }

        /// <returns>Returns a DomainController object for the DC that holds the specified FSMO role</returns>
        private DomainController GetRoleOwner(ActiveDirectoryRole role)
        {
            DirectoryEntry? entry = null;

            string? dcName = null;
            try
            {
                switch (role)
                {
                    case ActiveDirectoryRole.PdcRole:
                        {
                            entry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.DefaultNamingContext));
                            break;
                        }
                    case ActiveDirectoryRole.RidRole:
                        {
                            entry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.RidManager));
                            break;
                        }
                    case ActiveDirectoryRole.InfrastructureRole:
                        {
                            entry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.Infrastructure));
                            break;
                        }
                    default:
                        // should not happen since we are calling this only internally
                        Debug.Fail("Domain.GetRoleOwner: Invalid role type.");
                        break;
                }

                dcName = Utils.GetDnsHostNameFromNTDSA(context, (string)PropertyManager.GetPropertyValue(context, entry, PropertyManager.FsmoRoleOwner)!);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                entry?.Dispose();
            }

            // create a new context object for the domain controller passing on  the
            // credentials from the domain context
            DirectoryContext dcContext = Utils.GetNewDirectoryContext(dcName, DirectoryContextType.DirectoryServer, context);
            return new DomainController(dcContext, dcName);
        }

        [MemberNotNull(nameof(_crossRefDN))]
        private void LoadCrossRefAttributes()
        {
            DirectoryEntry? partitionsEntry = null;
            try
            {
                partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));

                // now within the partitions container search for the
                // crossRef object that has it's "dnsRoot" attribute equal to the
                // dns name of the current domain

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
                str.Append(")(");
                str.Append(PropertyManager.DnsRoot);
                str.Append('=');
                str.Append(Utils.GetEscapedFilterValue(partitionName!));
                str.Append("))");

                string filter = str.ToString();
                string[] propertiesToLoad = new string[2];

                propertiesToLoad[0] = PropertyManager.DistinguishedName;
                propertiesToLoad[1] = PropertyManager.TrustParent;

                ADSearcher searcher = new ADSearcher(partitionsEntry, filter, propertiesToLoad, SearchScope.OneLevel, false /*not paged search*/, false /*no cached results*/);
                SearchResult res = searcher.FindOne()!;

                _crossRefDN = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.DistinguishedName)!;

                // "trustParent" attribute may not be set
                if (res.Properties[PropertyManager.TrustParent].Count > 0)
                {
                    _trustParent = (string?)res.Properties[PropertyManager.TrustParent][0];
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                partitionsEntry?.Dispose();
            }
        }

        private Domain? GetParent()
        {
            if (_crossRefDN == null)
            {
                LoadCrossRefAttributes();
            }
            if (_trustParent != null)
            {
                DirectoryEntry parentCrossRef = DirectoryEntryManager.GetDirectoryEntry(context, _trustParent);
                string? parentDomainName = null;
                DirectoryContext? domainContext = null;
                try
                {
                    // create a new directory context for the parent domain
                    parentDomainName = (string)PropertyManager.GetPropertyValue(context, parentCrossRef, PropertyManager.DnsRoot)!;
                    domainContext = Utils.GetNewDirectoryContext(parentDomainName, DirectoryContextType.Domain, context);
                }
                finally
                {
                    parentCrossRef.Dispose();
                }
                return new Domain(domainContext, parentDomainName);
            }
            // does not have a parent so just return null
            return null;
        }

        private ArrayList GetChildDomains()
        {
            ArrayList childDomains = new ArrayList();

            if (_crossRefDN == null)
            {
                LoadCrossRefAttributes();
            }

            DirectoryEntry? partitionsEntry = null;
            SearchResultCollection? resCol = null;
            try
            {
                partitionsEntry = DirectoryEntryManager.GetDirectoryEntry(context, directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.PartitionsContainer));
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
                str.Append(")(");
                str.Append(PropertyManager.TrustParent);
                str.Append('=');
                str.Append(Utils.GetEscapedFilterValue(_crossRefDN));
                str.Append("))");

                string filter = str.ToString();
                string[] propertiesToLoad = new string[1];
                propertiesToLoad[0] = PropertyManager.DnsRoot;

                ADSearcher searcher = new ADSearcher(partitionsEntry, filter, propertiesToLoad, SearchScope.OneLevel);
                resCol = searcher.FindAll();

                foreach (SearchResult res in resCol)
                {
                    string childDomainName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.DnsRoot)!;
                    DirectoryContext childContext = Utils.GetNewDirectoryContext(childDomainName, DirectoryContextType.Domain, context);
                    childDomains.Add(new Domain(childContext, childDomainName));
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                resCol?.Dispose();
                partitionsEntry?.Dispose();
            }
            return childDomains;
        }

        private ArrayList GetTrustsHelper(string? targetDomainName)
        {
            string? serverName;
            IntPtr domains = (IntPtr)0;
            int count = 0;
            ArrayList unmanagedTrustList = new ArrayList();
            ArrayList tmpTrustList = new ArrayList();
            int localDomainIndex = 0;
            string? localDomainParent = null;
            int error = 0;
            bool impersonated;

            // first decide which server to go to
            if (context.isServer())
            {
                serverName = context.Name!;
            }
            else
            {
                serverName = DomainController.FindOne(context).Name;
            }

            // impersonate appropriately
            impersonated = Utils.Impersonate(context);

            // call the DS API to get trust domain information
            try
            {
                try
                {
                    error = UnsafeNativeMethods.DsEnumerateDomainTrustsW(serverName, (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_IN_FOREST | (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_OUTBOUND | (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_INBOUND, out domains, out count);
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
                    int j = 0;
                    for (int i = 0; i < count; i++)
                    {
                        // get the unmanaged trust object
                        addr = IntPtr.Add(domains, +i * Marshal.SizeOf(typeof(DS_DOMAIN_TRUSTS)));
                        DS_DOMAIN_TRUSTS unmanagedTrust = new DS_DOMAIN_TRUSTS();
                        Marshal.PtrToStructure(addr, unmanagedTrust);

                        unmanagedTrustList.Add(unmanagedTrust);
                    }

                    for (int i = 0; i < unmanagedTrustList.Count; i++)
                    {
                        DS_DOMAIN_TRUSTS unmanagedTrust = (DS_DOMAIN_TRUSTS)unmanagedTrustList[i]!;

                        // make sure this is the trust object that we want
                        if ((unmanagedTrust.Flags & (int)(DS_DOMAINTRUST_FLAG.DS_DOMAIN_PRIMARY | DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_OUTBOUND | DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_INBOUND)) == 0)
                        {
                            // Not interested in indirectly trusted domains.
                            continue;
                        }

                        // we don't want to have the NT4 trust to be returned
                        if (unmanagedTrust.TrustType == TrustHelper.TRUST_TYPE_DOWNLEVEL)
                            continue;

                        TrustObject obj = new TrustObject();
                        obj.TrustType = TrustType.Unknown;
                        if (unmanagedTrust.DnsDomainName != (IntPtr)0)
                            obj.DnsDomainName = Marshal.PtrToStringUni(unmanagedTrust.DnsDomainName);
                        if (unmanagedTrust.NetbiosDomainName != (IntPtr)0)
                            obj.NetbiosDomainName = Marshal.PtrToStringUni(unmanagedTrust.NetbiosDomainName);
                        obj.Flags = unmanagedTrust.Flags;
                        obj.TrustAttributes = unmanagedTrust.TrustAttributes;
                        obj.OriginalIndex = i;
                        obj.ParentIndex = unmanagedTrust.ParentIndex;

                        // check whether it is the case that we are only interested in the trust with target as specified
                        if (targetDomainName != null)
                        {
                            bool sameTarget = false;

                            // check whether it is the same target
                            if (obj.DnsDomainName != null && Utils.Compare(targetDomainName, obj.DnsDomainName) == 0)
                                sameTarget = true;
                            else if (obj.NetbiosDomainName != null && Utils.Compare(targetDomainName, obj.NetbiosDomainName) == 0)
                                sameTarget = true;

                            // we only want to need local domain and specified target domain trusts
                            if (!sameTarget && (obj.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_PRIMARY) == 0)
                                continue;
                        }

                        // local domain case
                        if ((obj.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_PRIMARY) != 0)
                        {
                            localDomainIndex = j;

                            // verify whether this is already the root
                            if ((obj.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_TREE_ROOT) == 0)
                            {
                                // get the parent domain name
                                DS_DOMAIN_TRUSTS parentTrust = (DS_DOMAIN_TRUSTS)unmanagedTrustList[obj.ParentIndex]!;
                                if (parentTrust.DnsDomainName != (IntPtr)0)
                                    localDomainParent = Marshal.PtrToStringUni(parentTrust.DnsDomainName);
                            }

                            // this is the trust type SELF
                            obj.TrustType = (TrustType)7;
                        }
                        // this is the case of MIT kerberos trust
                        else if (unmanagedTrust.TrustType == 3)
                        {
                            obj.TrustType = TrustType.Kerberos;
                        }

                        j++;
                        tmpTrustList.Add(obj);
                    }

                    // now determine the trust type
                    for (int i = 0; i < tmpTrustList.Count; i++)
                    {
                        TrustObject tmpObject = (TrustObject)tmpTrustList[i]!;
                        // local domain case, trust type has been determined
                        if (i == localDomainIndex)
                            continue;

                        if (tmpObject.TrustType == TrustType.Kerberos)
                            continue;

                        // parent domain
                        if (localDomainParent != null && Utils.Compare(localDomainParent, tmpObject.DnsDomainName) == 0)
                        {
                            tmpObject.TrustType = TrustType.ParentChild;
                            continue;
                        }

                        if ((tmpObject.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_IN_FOREST) != 0)
                        {
                            // child domain
                            if (tmpObject.ParentIndex == ((TrustObject)tmpTrustList[localDomainIndex]!).OriginalIndex)
                            {
                                tmpObject.TrustType = TrustType.ParentChild;
                            }
                            // tree root
                            else if ((tmpObject.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_TREE_ROOT) != 0 &&
                              (((TrustObject)tmpTrustList[localDomainIndex]!).Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_TREE_ROOT) != 0)
                            {
                                string? tmpForestName = null;
                                string rootDomainNC = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.RootDomainNamingContext);
                                tmpForestName = Utils.GetDnsNameFromDN(rootDomainNC);

                                // only if either the local domain or tmpObject is the tree root, will this trust relationship be a Root, otherwise it is cross link
                                DirectoryContext tmpContext = Utils.GetNewDirectoryContext(context.Name, DirectoryContextType.Forest, context);
                                if (tmpContext.isRootDomain() || Utils.Compare(tmpObject.DnsDomainName, tmpForestName) == 0)
                                {
                                    tmpObject.TrustType = TrustType.TreeRoot;
                                }
                                else
                                {
                                    tmpObject.TrustType = TrustType.CrossLink;
                                }
                            }
                            else
                            {
                                tmpObject.TrustType = TrustType.CrossLink;
                            }

                            continue;
                        }

                        // external trust or forest trust
                        if ((tmpObject.TrustAttributes & (int)TRUST_ATTRIBUTE.TRUST_ATTRIBUTE_FOREST_TRANSITIVE) != 0)
                        {
                            // should not happen as we specify DS_DOMAIN_IN_FOREST when enumerating the trust, so forest trust will not be returned
                            tmpObject.TrustType = TrustType.Forest;
                        }
                        else
                        {
                            tmpObject.TrustType = TrustType.External;
                        }
                    }
                }

                return tmpTrustList;
            }
            finally
            {
                if (domains != (IntPtr)0)
                    UnsafeNativeMethods.NetApiBufferFree(domains);
            }
        }

        private void RepairTrustHelper(Domain targetDomain, TrustDirection direction)
        {
            // now we try changing trust password on both sides
            string password = TrustHelper.CreateTrustPassword();

            // first reset trust password on remote side
            string targetServerName = TrustHelper.UpdateTrust(targetDomain.GetDirectoryContext(), targetDomain.Name, Name, password, false);

            // then reset trust password on local side
            string sourceServerName = TrustHelper.UpdateTrust(context, Name, targetDomain.Name, password, false);

            // last we reset the secure channel again to make sure info is replicated and trust is indeed ready now

            // verify outbound trust first
            if ((direction & TrustDirection.Outbound) != 0)
            {
                try
                {
                    TrustHelper.VerifyTrust(context, Name, targetDomain.Name, false /*not forest*/, TrustDirection.Outbound, true /*reset secure channel*/, targetServerName /* need to specify which target server */);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetDomain.Name, direction), typeof(TrustRelationshipInformation), null);
                }
            }

            // verify inbound trust
            if ((direction & TrustDirection.Inbound) != 0)
            {
                try
                {
                    TrustHelper.VerifyTrust(targetDomain.GetDirectoryContext(), targetDomain.Name, Name, false /*not forest*/, TrustDirection.Outbound, true/*reset secure channel*/, sourceServerName /* need to specify which target server */);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.WrongTrustDirection, Name, targetDomain.Name, direction), typeof(TrustRelationshipInformation), null);
                }
            }
        }

        #endregion private methods
    }
}
