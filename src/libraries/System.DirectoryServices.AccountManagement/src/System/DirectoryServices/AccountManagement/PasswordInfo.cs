// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
#if TESTHOOK
    public class PasswordInfo
#else
    internal class PasswordInfo
#endif
    {
        //
        // Properties exposed to the public through AuthenticablePrincipal
        //

        // LastPasswordSet
        private Nullable<DateTime> _lastPasswordSet = null;
        private LoadState _lastPasswordSetLoaded = LoadState.NotSet;

        public Nullable<DateTime> LastPasswordSet
        {
            get
            {
                return _owningPrincipal.HandleGet<Nullable<DateTime>>(ref _lastPasswordSet, PropertyNames.PwdInfoLastPasswordSet, ref _lastPasswordSetLoaded);
            }
        }

        // LastBadPasswordAttempt
        private Nullable<DateTime> _lastBadPasswordAttempt = null;
        private LoadState _lastBadPasswordAttemptLoaded = LoadState.NotSet;

        public Nullable<DateTime> LastBadPasswordAttempt
        {
            get
            {
                return _owningPrincipal.HandleGet<Nullable<DateTime>>(ref _lastBadPasswordAttempt, PropertyNames.PwdInfoLastBadPasswordAttempt, ref _lastBadPasswordAttemptLoaded);
            }
        }

        // PasswordNotRequired
        private bool _passwordNotRequired = false;
        private LoadState _passwordNotRequiredChanged = LoadState.NotSet;

        public bool PasswordNotRequired
        {
            get
            {
                return _owningPrincipal.HandleGet<bool>(ref _passwordNotRequired, PropertyNames.PwdInfoPasswordNotRequired, ref _passwordNotRequiredChanged);
            }

            set
            {
                _owningPrincipal.HandleSet<bool>(ref _passwordNotRequired, value, ref _passwordNotRequiredChanged,
                                  PropertyNames.PwdInfoPasswordNotRequired);
            }
        }

        // PasswordNeverExpires
        private bool _passwordNeverExpires = false;
        private LoadState _passwordNeverExpiresChanged = LoadState.NotSet;

        public bool PasswordNeverExpires
        {
            get
            {
                return _owningPrincipal.HandleGet<bool>(ref _passwordNeverExpires, PropertyNames.PwdInfoPasswordNeverExpires, ref _passwordNeverExpiresChanged);
            }

            set
            {
                _owningPrincipal.HandleSet<bool>(ref _passwordNeverExpires, value, ref _passwordNeverExpiresChanged,
                                  PropertyNames.PwdInfoPasswordNeverExpires);
            }
        }

        // UserCannotChangePassword
        private bool _cannotChangePassword = false;
        private LoadState _cannotChangePasswordChanged = LoadState.NotSet;
        private bool _cannotChangePasswordRead = false;

        // For this property we are doing an on demand load.  The store will not load this property when load is called beacuse
        // the loading of this property is perf intensive.  HandleGet still needs to be called to load the other object properties if 
        // needed.  We read the status directly from the store and then cache it for use later.
        public bool UserCannotChangePassword
        {
            get
            {
                _owningPrincipal.HandleGet<bool>(ref _cannotChangePassword, PropertyNames.PwdInfoCannotChangePassword, ref _cannotChangePasswordChanged);

                if ((_cannotChangePasswordChanged != LoadState.Changed) && !_cannotChangePasswordRead && !_owningPrincipal.unpersisted)
                {
                    _cannotChangePassword = _owningPrincipal.GetStoreCtxToUse().AccessCheck(_owningPrincipal, PrincipalAccessMask.ChangePassword);
                    _cannotChangePasswordRead = true;
                }

                return _cannotChangePassword;
            }

            set
            {
                _owningPrincipal.HandleSet<bool>(ref _cannotChangePassword, value, ref _cannotChangePasswordChanged,
                                  PropertyNames.PwdInfoCannotChangePassword);
            }
        }

        // AllowReversiblePasswordEncryption
        private bool _allowReversiblePasswordEncryption = false;
        private LoadState _allowReversiblePasswordEncryptionChanged = LoadState.NotSet;

        public bool AllowReversiblePasswordEncryption
        {
            get
            {
                return _owningPrincipal.HandleGet<bool>(ref _allowReversiblePasswordEncryption, PropertyNames.PwdInfoAllowReversiblePasswordEncryption, ref _allowReversiblePasswordEncryptionChanged);
            }

            set
            {
                _owningPrincipal.HandleSet<bool>(ref _allowReversiblePasswordEncryption, value, ref _allowReversiblePasswordEncryptionChanged,
                                  PropertyNames.PwdInfoAllowReversiblePasswordEncryption);
            }
        }

        //
        // Methods exposed to the public through AuthenticablePrincipal
        //

        private string _storedNewPassword = null;

        public void SetPassword(string newPassword)
        {
            if (newPassword == null)
                throw new ArgumentNullException(nameof(newPassword));

            // If we're not persisted, we just save up the change until we're Saved
            if (_owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "SetPassword: saving until persisted");
                _storedNewPassword = newPassword;
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "SetPassword: sending request");
                _owningPrincipal.GetStoreCtxToUse().SetPassword(_owningPrincipal, newPassword);
            }
        }

        public void ChangePassword(string oldPassword, string newPassword)
        {
            if (oldPassword == null)
                throw new ArgumentNullException(nameof(oldPassword));

            if (newPassword == null)
                throw new ArgumentNullException(nameof(newPassword));

            // While you can reset the password on an unpersisted principal (and it will be used as the initial password
            // for the pricipal), changing the password on a principal that doesn't exist yet doesn't make sense
            if (_owningPrincipal.unpersisted)
                throw new InvalidOperationException(SR.PasswordInfoChangePwdOnUnpersistedPrinc);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "ChangePassword: sending request");
            _owningPrincipal.GetStoreCtxToUse().ChangePassword(_owningPrincipal, oldPassword, newPassword);
        }

        private bool _expirePasswordImmediately = false;

        public void ExpirePasswordNow()
        {
            // If we're not persisted, we just save up the change until we're Saved        
            if (_owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "ExpirePasswordNow: saving until persisted");
                _expirePasswordImmediately = true;
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "ExpirePasswordNow: sending request");
                _owningPrincipal.GetStoreCtxToUse().ExpirePassword(_owningPrincipal);
            }
        }

        public void RefreshExpiredPassword()
        {
            // If we're not persisted, we undo the expiration we saved up when ExpirePasswordNow was called (if it was).
            if (_owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "RefreshExpiredPassword: saving until persisted");
                _expirePasswordImmediately = false;
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "RefreshExpiredPassword: sending request");
                _owningPrincipal.GetStoreCtxToUse().UnexpirePassword(_owningPrincipal);
            }
        }

        //
        // Internal constructor
        //
        internal PasswordInfo(AuthenticablePrincipal principal)
        {
            _owningPrincipal = principal;
        }

        //
        // Private implementation
        //
        private AuthenticablePrincipal _owningPrincipal;
        /*
                // These methods implement the logic shared by all the get/set accessors for the internal properties
                T HandleGet<T>(ref T currentValue, string name)
                {
                    // Check that we actually support this propery in our store
                    //this.owningPrincipal.CheckSupportedProperty(name);

                    return currentValue;
                }

                void HandleSet<T>(ref T currentValue, T newValue, ref bool changed, string name)
                {
                    // Check that we actually support this propery in our store
                    //this.owningPrincipal.CheckSupportedProperty(name);

                    currentValue = newValue;
                    changed = true;
                }
                */

        //
        // Load/Store
        //

        //
        // Loading with query results
        //

        internal void LoadValueIntoProperty(string propertyName, object value)
        {
            if (value != null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "LoadValueIntoProperty: name=" + propertyName + " value=" + value.ToString());
            }

            switch (propertyName)
            {
                case (PropertyNames.PwdInfoLastPasswordSet):
                    _lastPasswordSet = (Nullable<DateTime>)value;
                    _lastPasswordSetLoaded = LoadState.Loaded;
                    break;

                case (PropertyNames.PwdInfoLastBadPasswordAttempt):
                    _lastBadPasswordAttempt = (Nullable<DateTime>)value;
                    _lastBadPasswordAttemptLoaded = LoadState.Loaded;
                    break;

                case (PropertyNames.PwdInfoPasswordNotRequired):
                    _passwordNotRequired = (bool)value;
                    _passwordNotRequiredChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.PwdInfoPasswordNeverExpires):
                    _passwordNeverExpires = (bool)value;
                    _passwordNeverExpiresChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.PwdInfoCannotChangePassword):
                    _cannotChangePassword = (bool)value;
                    _cannotChangePasswordChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.PwdInfoAllowReversiblePasswordEncryption):
                    _allowReversiblePasswordEncryption = (bool)value;
                    _allowReversiblePasswordEncryptionChanged = LoadState.Loaded;
                    break;

                default:
                    Debug.Fail($"PasswordInfo.LoadValueIntoProperty: fell off end looking for {propertyName}");
                    break;
            }
        }

        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        // Given a property name, returns true if that property has changed since it was loaded, false otherwise.
        internal bool GetChangeStatusForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "GetChangeStatusForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case (PropertyNames.PwdInfoPasswordNotRequired):
                    return _passwordNotRequiredChanged == LoadState.Changed;

                case (PropertyNames.PwdInfoPasswordNeverExpires):
                    return _passwordNeverExpiresChanged == LoadState.Changed;

                case (PropertyNames.PwdInfoCannotChangePassword):
                    return _cannotChangePasswordChanged == LoadState.Changed;

                case (PropertyNames.PwdInfoAllowReversiblePasswordEncryption):
                    return _allowReversiblePasswordEncryptionChanged == LoadState.Changed;

                case (PropertyNames.PwdInfoPassword):
                    return (_storedNewPassword != null);

                case (PropertyNames.PwdInfoExpireImmediately):
                    return (_expirePasswordImmediately != false);

                default:
                    Debug.Fail($"PasswordInfo.GetChangeStatusForProperty: fell off end looking for {propertyName}");
                    return false;
            }
        }

        // Given a property name, returns the current value for the property.
        internal object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "GetValueForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case (PropertyNames.PwdInfoPasswordNotRequired):
                    return _passwordNotRequired;

                case (PropertyNames.PwdInfoPasswordNeverExpires):
                    return _passwordNeverExpires;

                case (PropertyNames.PwdInfoCannotChangePassword):
                    return _cannotChangePassword;

                case (PropertyNames.PwdInfoAllowReversiblePasswordEncryption):
                    return _allowReversiblePasswordEncryption;

                case (PropertyNames.PwdInfoPassword):
                    return _storedNewPassword;

                case (PropertyNames.PwdInfoExpireImmediately):
                    return _expirePasswordImmediately;

                default:
                    Debug.Fail($"PasswordInfo.GetValueForProperty: fell off end looking for {propertyName}");
                    return null;
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "ResetAllChangeStatus");

            _passwordNotRequiredChanged = (_passwordNotRequiredChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _passwordNeverExpiresChanged = (_passwordNeverExpiresChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _cannotChangePasswordChanged = (_cannotChangePasswordChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _allowReversiblePasswordEncryptionChanged = (_allowReversiblePasswordEncryptionChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;

            _storedNewPassword = null;
            _expirePasswordImmediately = false;
        }
    }
}
