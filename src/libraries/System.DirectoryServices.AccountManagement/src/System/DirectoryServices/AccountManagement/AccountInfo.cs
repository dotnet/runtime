// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
#if TESTHOOK
    public class AccountInfo
#else
    internal sealed class AccountInfo
#endif
    {
        //
        // Properties exposed to the public through AuthenticablePrincipal
        //

        // AccountLockoutTime
        private Nullable<DateTime> _accountLockoutTime;
        private LoadState _accountLockoutTimeLoaded = LoadState.NotSet;

        public Nullable<DateTime> AccountLockoutTime
        {
            get
            {
                return _owningPrincipal.HandleGet<Nullable<DateTime>>(ref _accountLockoutTime, PropertyNames.AcctInfoAcctLockoutTime, ref _accountLockoutTimeLoaded);
            }
        }

        // LastLogon
        private Nullable<DateTime> _lastLogon;
        private LoadState _lastLogonLoaded = LoadState.NotSet;

        public Nullable<DateTime> LastLogon
        {
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
            get
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoPermittedWorkstations))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                return _owningPrincipal.HandleGet<PrincipalValueCollection<string>>(ref _permittedWorkstations, PropertyNames.AcctInfoPermittedWorkstations, ref _permittedWorkstationsLoaded);
            }
        }

        // PermittedLogonTimes
        //  We have to handle the change-tracking for this differently than for the other properties, because
        //  a byte[] is mutable.  After calling the get accessor, the app can change the permittedLogonTimes,
        //  without needing to ever call the set accessor.  Therefore, rather than a simple "changed" flag set
        //  by the set accessor, we need to track the original value of the property, and flag it as changed
        //  if current value != original value.
        private byte[] _permittedLogonTimes;
        private byte[] _permittedLogonTimesOriginal;
        private LoadState _permittedLogonTimesLoaded = LoadState.NotSet;

        public byte[] PermittedLogonTimes
        {
            get
            {
                return _owningPrincipal.HandleGet<byte[]>(ref _permittedLogonTimes, PropertyNames.AcctInfoPermittedLogonTimes, ref _permittedLogonTimesLoaded);
            }

            set
            {
                // We don't use HandleSet<T> here because of the slightly non-standard implementation of the change-tracking
                // for this property.

                // Check that we actually support this propery in our store
                //this.owningPrincipal.CheckSupportedProperty(PropertyNames.AcctInfoPermittedLogonTimes);

                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoPermittedLogonTimes))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                // If we get to this point we know that the value of the property has changed and we should not load it from the store.
                // If value is retrieved the state is set to loaded.  Even if user modifies the reference we will
                // not overwrite it because we mark it as loaded.
                // If the user sets it before reading it we mark it as changed.  When the users accesses it we just return the current
                // value.  All change tracking to the store is done off of an actual object comparison because users can change the value
                // either through property set or modifying the reference returned.
                _permittedLogonTimesLoaded = LoadState.Changed;

                _permittedLogonTimes = value;
            }
        }

        // AccountExpirationDate
        private Nullable<DateTime> _expirationDate;
        private LoadState _expirationDateChanged = LoadState.NotSet;

        public Nullable<DateTime> AccountExpirationDate
        {
            get
            {
                return _owningPrincipal.HandleGet<Nullable<DateTime>>(ref _expirationDate, PropertyNames.AcctInfoExpirationDate, ref _expirationDateChanged);
            }

            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoExpirationDate))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<Nullable<DateTime>>(ref _expirationDate, value, ref _expirationDateChanged,
                                               PropertyNames.AcctInfoExpirationDate);
            }
        }

        // SmartcardLogonRequired
        private bool _smartcardLogonRequired;
        private LoadState _smartcardLogonRequiredChanged = LoadState.NotSet;

        public bool SmartcardLogonRequired
        {
            get
            {
                return _owningPrincipal.HandleGet<bool>(ref _smartcardLogonRequired, PropertyNames.AcctInfoSmartcardRequired, ref _smartcardLogonRequiredChanged);
            }

            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoSmartcardRequired))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<bool>(ref _smartcardLogonRequired, value, ref _smartcardLogonRequiredChanged,
                                 PropertyNames.AcctInfoSmartcardRequired);
            }
        }

        // DelegationPermitted
        private bool _delegationPermitted;
        private LoadState _delegationPermittedChanged = LoadState.NotSet;

        public bool DelegationPermitted
        {
            get
            {
                return _owningPrincipal.HandleGet<bool>(ref _delegationPermitted, PropertyNames.AcctInfoDelegationPermitted, ref _delegationPermittedChanged);
            }

            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoDelegationPermitted))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<bool>(ref _delegationPermitted, value, ref _delegationPermittedChanged,
                                 PropertyNames.AcctInfoDelegationPermitted);
            }
        }

        // BadLogonCount
        private int _badLogonCount;
        private LoadState _badLogonCountChanged = LoadState.NotSet;

        public int BadLogonCount
        {
            get
            {
                return _owningPrincipal.HandleGet<int>(ref _badLogonCount, PropertyNames.AcctInfoBadLogonCount, ref _badLogonCountChanged);
            }
        }

        // HomeDirectory
        private string _homeDirectory;
        private LoadState _homeDirectoryChanged = LoadState.NotSet;

        public string HomeDirectory
        {
            get
            {
                return _owningPrincipal.HandleGet<string>(ref _homeDirectory, PropertyNames.AcctInfoHomeDirectory, ref _homeDirectoryChanged);
            }

            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoHomeDirectory))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<string>(ref _homeDirectory, value, ref _homeDirectoryChanged,
                                   PropertyNames.AcctInfoHomeDirectory);
            }
        }

        // HomeDrive
        private string _homeDrive;
        private LoadState _homeDriveChanged = LoadState.NotSet;

        public string HomeDrive
        {
            get
            {
                return _owningPrincipal.HandleGet<string>(ref _homeDrive, PropertyNames.AcctInfoHomeDrive, ref _homeDriveChanged);
            }

            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoHomeDrive))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<string>(ref _homeDrive, value, ref _homeDriveChanged,
                                   PropertyNames.AcctInfoHomeDrive);
            }
        }

        // ScriptPath
        private string _scriptPath;
        private LoadState _scriptPathChanged = LoadState.NotSet;

        public string ScriptPath
        {
            get
            {
                return _owningPrincipal.HandleGet<string>(ref _scriptPath, PropertyNames.AcctInfoScriptPath, ref _scriptPathChanged);
            }

            set
            {
                if (!_owningPrincipal.GetStoreCtxToUse().IsValidProperty(_owningPrincipal, PropertyNames.AcctInfoScriptPath))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                _owningPrincipal.HandleSet<string>(ref _scriptPath, value, ref _scriptPathChanged,
                                   PropertyNames.AcctInfoScriptPath);
            }
        }

        //
        // Methods exposed to the public through AuthenticablePrincipal
        //
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
        internal AccountInfo(AuthenticablePrincipal principal)
        {
            _owningPrincipal = principal;
        }

        //
        // Private implementation
        //
        private readonly AuthenticablePrincipal _owningPrincipal;

        //
        // Load/Store
        //

        //
        // Loading with query results
        //

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
                    Debug.Fail($"AccountInfo.LoadValueIntoProperty: fell off end looking for {propertyName}");
                    break;
            }
        }

        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        // Given a property name, returns true if that property has changed since it was loaded, false otherwise.
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
                    Debug.Fail($"AccountInfo.GetChangeStatusForProperty: fell off end looking for {propertyName}");
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
                    Debug.Fail($"AccountInfo.GetValueForProperty: fell off end looking for {propertyName}");
                    return null;
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
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
