// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
#if TESTHOOK
    public class AccountInfo
#else
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    internal class AccountInfo
#endif
    {
        //
        // Properties exposed to the public through AuthenticablePrincipal
        //

        // AccountLockoutTime
        private Nullable<DateTime> _accountLockoutTime = null;
        private LoadState _accountLockoutTimeLoaded = LoadState.NotSet;

        public Nullable<DateTime> AccountLockoutTime
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Nullable`1<System.DateTime>>(System.Nullable`1<System.DateTime>&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Nullable`1<System.DateTime>" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<Nullable<DateTime>>(ref _accountLockoutTime, PropertyNames.AcctInfoAcctLockoutTime, ref _accountLockoutTimeLoaded);
            }
        }

        // LastLogon
        private Nullable<DateTime> _lastLogon = null;
        private LoadState _lastLogonLoaded = LoadState.NotSet;

        public Nullable<DateTime> LastLogon
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Nullable`1<System.DateTime>>(System.Nullable`1<System.DateTime>&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Nullable`1<System.DateTime>" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<Nullable<DateTime>>(ref _lastLogon, PropertyNames.AcctInfoLastLogon, ref _lastLogonLoaded);
            }
        }

        // PermittedWorkstations
        private PrincipalValueCollection<string> _permittedWorkstations = new PrincipalValueCollection<string>();
        private LoadState _permittedWorkstationsLoaded = LoadState.NotSet;

        public PrincipalValueCollection<string> PermittedWorkstations
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.DirectoryServices.AccountManagement.PrincipalValueCollection`1<System.String>>(System.DirectoryServices.AccountManagement.PrincipalValueCollection`1<System.String>&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.DirectoryServices.AccountManagement.PrincipalValueCollection`1<System.String>" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoPermittedWorkstations))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                return _owningPrincipal.HandleGet<PrincipalValueCollection<string>>(ref _permittedWorkstations, PropertyNames.AcctInfoPermittedWorkstations, ref _permittedWorkstationsLoaded);
            }
        }

        // PermittedLogonTimes
        //  We have to handle the change-tracking for this differently than for the other properties, because
        //  a byte[] is mutable.  After calling the get accessor, the app can change the permittedLogonTimes,
        //  without needing to ever call the set accessor.  Therefore, rather than a simple "changed" flag set
        //  by the set accessor, we need to track the original value of the property, and flag it as changed
        //  if current value != original value.
        private byte[] _permittedLogonTimes = null;
        private byte[] _permittedLogonTimesOriginal = null;
        private LoadState _permittedLogonTimesLoaded = LoadState.NotSet;

        public byte[] PermittedLogonTimes
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Byte[]>(System.Byte[]&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Byte[]" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<byte[]>(ref _permittedLogonTimes, PropertyNames.AcctInfoPermittedLogonTimes, ref _permittedLogonTimesLoaded);
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                // We don't use HandleSet<T> here because of the slightly non-standard implementation of the change-tracking
                // for this property.

                // Check that we actually support this propery in our store
                //this.owningPrincipal.CheckSupportedProperty(PropertyNames.AcctInfoPermittedLogonTimes);

                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoPermittedLogonTimes))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                // If we get to this point we know that the value of the property has changed and we should not load it from the store.
                // If value is retrived the state is set to loaded.  Even if user modifies the reference we will
                // not overwrite it because we mark it as loaded.
                // If the user sets it before reading it we mark it as changed.  When the users accesses it we just return the current
                // value.  All change tracking to the store is done off of an actual object comparison because users can change the value
                // either through property set or modifying the reference returned.
                _permittedLogonTimesLoaded = LoadState.Changed;

                _permittedLogonTimes = value;
            }
        }

        // AccountExpirationDate
        private Nullable<DateTime> _expirationDate = null;
        private LoadState _expirationDateChanged = LoadState.NotSet;

        public Nullable<DateTime> AccountExpirationDate
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Nullable`1<System.DateTime>>(System.Nullable`1<System.DateTime>&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Nullable`1<System.DateTime>" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<Nullable<DateTime>>(ref _expirationDate, PropertyNames.AcctInfoExpirationDate, ref _expirationDateChanged);
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.Nullable`1<System.DateTime>>(System.Nullable`1<System.DateTime>&,System.Nullable`1<System.DateTime>,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoExpirationDate))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<Nullable<DateTime>>(ref _expirationDate, value, ref _expirationDateChanged,
                                               PropertyNames.AcctInfoExpirationDate);
            }
        }

        // SmartcardLogonRequired
        private bool _smartcardLogonRequired = false;
        private LoadState _smartcardLogonRequiredChanged = LoadState.NotSet;

        public bool SmartcardLogonRequired
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Boolean>(System.Boolean&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Boolean" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<bool>(ref _smartcardLogonRequired, PropertyNames.AcctInfoSmartcardRequired, ref _smartcardLogonRequiredChanged);
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.Boolean>(System.Boolean&,System.Boolean,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoSmartcardRequired))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<bool>(ref _smartcardLogonRequired, value, ref _smartcardLogonRequiredChanged,
                                 PropertyNames.AcctInfoSmartcardRequired);
            }
        }

        // DelegationPermitted
        private bool _delegationPermitted = false;
        private LoadState _delegationPermittedChanged = LoadState.NotSet;

        public bool DelegationPermitted
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Boolean>(System.Boolean&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Boolean" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<bool>(ref _delegationPermitted, PropertyNames.AcctInfoDelegationPermitted, ref _delegationPermittedChanged);
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.Boolean>(System.Boolean&,System.Boolean,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoDelegationPermitted))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<bool>(ref _delegationPermitted, value, ref _delegationPermittedChanged,
                                 PropertyNames.AcctInfoDelegationPermitted);
            }
        }

        // BadLogonCount
        private int _badLogonCount = 0;
        private LoadState _badLogonCountChanged = LoadState.NotSet;

        public int BadLogonCount
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Int32>(System.Int32&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Int32" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<int>(ref _badLogonCount, PropertyNames.AcctInfoBadLogonCount, ref _badLogonCountChanged);
            }
        }

        // HomeDirectory
        private string _homeDirectory = null;
        private LoadState _homeDirectoryChanged = LoadState.NotSet;

        public string HomeDirectory
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.String>(System.String&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.String" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<string>(ref _homeDirectory, PropertyNames.AcctInfoHomeDirectory, ref _homeDirectoryChanged);
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.String>(System.String&,System.String,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoHomeDirectory))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<string>(ref _homeDirectory, value, ref _homeDirectoryChanged,
                                   PropertyNames.AcctInfoHomeDirectory);
            }
        }

        // HomeDrive
        private string _homeDrive = null;
        private LoadState _homeDriveChanged = LoadState.NotSet;

        public string HomeDrive
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.String>(System.String&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.String" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<string>(ref _homeDrive, PropertyNames.AcctInfoHomeDrive, ref _homeDriveChanged);
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.String>(System.String&,System.String,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoHomeDrive))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<string>(ref _homeDrive, value, ref _homeDriveChanged,
                                   PropertyNames.AcctInfoHomeDrive);
            }
        }

        // ScriptPath
        private string _scriptPath = null;
        private LoadState _scriptPathChanged = LoadState.NotSet;

        public string ScriptPath
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.String>(System.String&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.String" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return _owningPrincipal.HandleGet<string>(ref _scriptPath, PropertyNames.AcctInfoScriptPath, ref _scriptPathChanged);
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.String>(System.String&,System.String,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoScriptPath))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<string>(ref _scriptPath, value, ref _scriptPathChanged,
                                   PropertyNames.AcctInfoScriptPath);
            }
        }

        //
        // Methods exposed to the public through AuthenticablePrincipal
        //
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Principal.get_Context():System.DirectoryServices.AccountManagement.PrincipalContext" />
        // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
        // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public bool IsAccountLockedOut()
        {
            if (!_owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AccountInfo", "IsAccountLockedOut: sending lockout query");

                Debug.Assert(_owningPrincipal.Context != null);

                return _owningPrincipal.GetStoreCtxToUse().IsLockedOut(_owningPrincipal);
            }
            else
            {
                // A Principal that hasn't even been persisted can't be locked out
                return false;
            }
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Principal.get_Context():System.DirectoryServices.AccountManagement.PrincipalContext" />
        // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
        // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public void UnlockAccount()
        {
            if (!_owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AccountInfo", "UnlockAccount: sending unlock request");

                Debug.Assert(_owningPrincipal.Context != null);

                _owningPrincipal.GetStoreCtxToUse().UnlockAccount(_owningPrincipal);
            }

            // Since a Principal that's not persisted can't have been locked-out,
            // there's nothing to do in that case
        }

        //
        // Internal constructor
        //
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalValueCollection`1<System.String>..ctor()" />
        // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        internal AccountInfo(AuthenticablePrincipal principal)
        {
            _owningPrincipal = principal;
        }

        //
        // Private implementation
        //
        private AuthenticablePrincipal _owningPrincipal;

        //
        // Load/Store
        //

        //
        // Loading with query results
        //

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalValueCollection`1<System.String>.Load(System.Collections.Generic.List`1<System.String>):System.Void" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        internal void LoadValueIntoProperty(string propertyName, object value)
        {
            //            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AccountInfo", "LoadValueIntoProperty: name=" + propertyName + " value=" + value.ToString());

            switch (propertyName)
            {
                case (PropertyNames.AcctInfoAcctLockoutTime):
                    _accountLockoutTime = (Nullable<DateTime>)value;
                    _accountLockoutTimeLoaded = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoLastLogon):
                    _lastLogon = (Nullable<DateTime>)value;
                    _lastLogonLoaded = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoPermittedWorkstations):
                    _permittedWorkstations.Load((List<string>)value);
                    _permittedWorkstationsLoaded = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoPermittedLogonTimes):
                    _permittedLogonTimes = (byte[])value;
                    _permittedLogonTimesOriginal = (byte[])((byte[])value).Clone();
                    _permittedLogonTimesLoaded = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoExpirationDate):
                    _expirationDate = (Nullable<DateTime>)value;
                    _expirationDateChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoSmartcardRequired):
                    _smartcardLogonRequired = (bool)value;
                    _smartcardLogonRequiredChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoDelegationPermitted):
                    _delegationPermitted = (bool)value;
                    _delegationPermittedChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoBadLogonCount):
                    _badLogonCount = (int)value;
                    _badLogonCountChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoHomeDirectory):
                    _homeDirectory = (string)value;
                    _homeDirectoryChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoHomeDrive):
                    _homeDrive = (string)value;
                    _homeDriveChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.AcctInfoScriptPath):
                    _scriptPath = (string)value;
                    _scriptPathChanged = LoadState.Loaded;
                    break;

                default:
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, "AccountInfo.LoadValueIntoProperty: fell off end looking for {0}", propertyName));
                    break;
            }
        }

        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        // Given a property name, returns true if that property has changed since it was loaded, false otherwise.
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalValueCollection`1<System.String>.get_Changed():System.Boolean" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        internal bool GetChangeStatusForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AccountInfo", "GetChangeStatusForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case (PropertyNames.AcctInfoPermittedWorkstations):
                    return _permittedWorkstations.Changed;

                case (PropertyNames.AcctInfoPermittedLogonTimes):
                    // If they're equal, they have _not_ changed
                    if ((_permittedLogonTimes == null) && (_permittedLogonTimesOriginal == null))
                        return false;

                    if ((_permittedLogonTimes == null) || (_permittedLogonTimesOriginal == null))
                        return true;

                    return !Utils.AreBytesEqual(_permittedLogonTimes, _permittedLogonTimesOriginal);

                case (PropertyNames.AcctInfoExpirationDate):
                    return _expirationDateChanged == LoadState.Changed;

                case (PropertyNames.AcctInfoSmartcardRequired):
                    return _smartcardLogonRequiredChanged == LoadState.Changed;

                case (PropertyNames.AcctInfoDelegationPermitted):
                    return _delegationPermittedChanged == LoadState.Changed;

                case (PropertyNames.AcctInfoHomeDirectory):
                    return _homeDirectoryChanged == LoadState.Changed;

                case (PropertyNames.AcctInfoHomeDrive):
                    return _homeDriveChanged == LoadState.Changed;

                case (PropertyNames.AcctInfoScriptPath):
                    return _scriptPathChanged == LoadState.Changed;

                default:
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, "AccountInfo.GetChangeStatusForProperty: fell off end looking for {0}", propertyName));
                    return false;
            }
        }

        // Given a property name, returns the current value for the property.
        internal object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AccountInfo", "GetValueForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case (PropertyNames.AcctInfoPermittedWorkstations):
                    return _permittedWorkstations;

                case (PropertyNames.AcctInfoPermittedLogonTimes):
                    return _permittedLogonTimes;

                case (PropertyNames.AcctInfoExpirationDate):
                    return _expirationDate;

                case (PropertyNames.AcctInfoSmartcardRequired):
                    return _smartcardLogonRequired;

                case (PropertyNames.AcctInfoDelegationPermitted):
                    return _delegationPermitted;

                case (PropertyNames.AcctInfoHomeDirectory):
                    return _homeDirectory;

                case (PropertyNames.AcctInfoHomeDrive):
                    return _homeDrive;

                case (PropertyNames.AcctInfoScriptPath):
                    return _scriptPath;

                default:
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, "AccountInfo.GetValueForProperty: fell off end looking for {0}", propertyName));
                    return null;
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalValueCollection`1<System.String>.ResetTracking():System.Void" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        internal void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AccountInfo", "ResetAllChangeStatus");

            _permittedWorkstations.ResetTracking();

            _permittedLogonTimesOriginal = (_permittedLogonTimes != null) ?
                                                            (byte[])_permittedLogonTimes.Clone() :
                                                            null;
            _expirationDateChanged = (_expirationDateChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _smartcardLogonRequiredChanged = (_smartcardLogonRequiredChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _delegationPermittedChanged = (_delegationPermittedChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _homeDirectoryChanged = (_homeDirectoryChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _homeDriveChanged = (_homeDriveChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _scriptPathChanged = (_scriptPathChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
        }
    }
}
