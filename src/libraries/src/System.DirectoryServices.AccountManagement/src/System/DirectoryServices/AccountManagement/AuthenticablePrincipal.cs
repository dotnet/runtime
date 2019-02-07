// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryRdnPrefix("CN")]
    public class AuthenticablePrincipal : Principal
    {
        //
        // Public Properties
        //

        // Enabled property
        private bool _enabled = false;          // the actual property value
        private LoadState _enabledChanged = LoadState.NotSet;   // change-tracking

        public Nullable<bool> Enabled
        {
            get
            {
                // Make sure we're not disposed or deleted.  Although HandleGet/HandleSet will check this,
                // we need to check these before we do anything else.
                CheckDisposedOrDeleted();

                // Different stores have different defaults as to the Enabled setting
                // (AD: creates disabled by default; SAM: creates enabled by default).
                // So if the principal is unpersisted (and thus we may not know what store it's
                // going to end up in), we'll just return null unless they previously
                // set an explicit value.
                if (this.unpersisted && (_enabledChanged != LoadState.Changed))
                {
                    GlobalDebug.WriteLineIf(
                                    GlobalDebug.Info,
                                    "AuthenticablePrincipal",
                                    "Enabled: returning null, unpersisted={0}, enabledChanged={1}",
                                    this.unpersisted,
                                    _enabledChanged);

                    return null;
                }

                return HandleGet<bool>(ref _enabled, PropertyNames.AuthenticablePrincipalEnabled, ref _enabledChanged);
            }

            set
            {
                // Make sure we're not disposed or deleted.  Although HandleGet/HandleSet will check this,
                // we need to check these before we do anything else.
                CheckDisposedOrDeleted();

                // We don't want to let them set a null value.
                if (!value.HasValue)
                    throw new ArgumentNullException(nameof(value));

                HandleSet<bool>(ref _enabled, value.Value, ref _enabledChanged,
                                  PropertyNames.AuthenticablePrincipalEnabled);
            }
        }

        //
        // AccountInfo-related properties/methods
        //

        private AccountInfo _accountInfo = null;

        private AccountInfo AccountInfo
        {
            get
            {
                // Make sure we're not disposed or deleted.
                CheckDisposedOrDeleted();

                if (_accountInfo == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthenticablePrincipal", "AccountInfo: creating new AccountInfo");
                    _accountInfo = new AccountInfo(this);
                }

                return _accountInfo;
            }
        }

        public Nullable<DateTime> AccountLockoutTime
        {
            get { return this.AccountInfo.AccountLockoutTime; }
        }

        public Nullable<DateTime> LastLogon
        {
            get { return this.AccountInfo.LastLogon; }
        }

        public PrincipalValueCollection<string> PermittedWorkstations
        {
            get { return this.AccountInfo.PermittedWorkstations; }
        }

        public byte[] PermittedLogonTimes
        {
            get { return this.AccountInfo.PermittedLogonTimes; }
            set { this.AccountInfo.PermittedLogonTimes = value; }
        }

        public Nullable<DateTime> AccountExpirationDate
        {
            get { return this.AccountInfo.AccountExpirationDate; }
            set { this.AccountInfo.AccountExpirationDate = value; }
        }

        public bool SmartcardLogonRequired
        {
            get { return this.AccountInfo.SmartcardLogonRequired; }
            set { this.AccountInfo.SmartcardLogonRequired = value; }
        }

        public bool DelegationPermitted
        {
            get { return this.AccountInfo.DelegationPermitted; }
            set { this.AccountInfo.DelegationPermitted = value; }
        }

        public int BadLogonCount
        {
            get { return this.AccountInfo.BadLogonCount; }
        }

        public string HomeDirectory
        {
            get { return this.AccountInfo.HomeDirectory; }
            set { this.AccountInfo.HomeDirectory = value; }
        }

        public string HomeDrive
        {
            get { return this.AccountInfo.HomeDrive; }
            set { this.AccountInfo.HomeDrive = value; }
        }

        public string ScriptPath
        {
            get { return this.AccountInfo.ScriptPath; }
            set { this.AccountInfo.ScriptPath = value; }
        }

        public bool IsAccountLockedOut()
        {
            return this.AccountInfo.IsAccountLockedOut();
        }

        public void UnlockAccount()
        {
            this.AccountInfo.UnlockAccount();
        }

        //
        // PasswordInfo-related properties/methods
        //

        private PasswordInfo _passwordInfo = null;

        private PasswordInfo PasswordInfo
        {
            get
            {
                // Make sure we're not disposed or deleted.
                CheckDisposedOrDeleted();

                if (_passwordInfo == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthenticablePrincipal", "PasswordInfo: creating new PasswordInfo");
                    _passwordInfo = new PasswordInfo(this);
                }

                return _passwordInfo;
            }
        }

        public Nullable<DateTime> LastPasswordSet
        {
            get { return this.PasswordInfo.LastPasswordSet; }
        }

        public Nullable<DateTime> LastBadPasswordAttempt
        {
            get { return this.PasswordInfo.LastBadPasswordAttempt; }
        }

        public bool PasswordNotRequired
        {
            get { return this.PasswordInfo.PasswordNotRequired; }
            set { this.PasswordInfo.PasswordNotRequired = value; }
        }

        public bool PasswordNeverExpires
        {
            get { return this.PasswordInfo.PasswordNeverExpires; }
            set { this.PasswordInfo.PasswordNeverExpires = value; }
        }

        public bool UserCannotChangePassword
        {
            get { return this.PasswordInfo.UserCannotChangePassword; }
            set { this.PasswordInfo.UserCannotChangePassword = value; }
        }

        public bool AllowReversiblePasswordEncryption
        {
            get { return this.PasswordInfo.AllowReversiblePasswordEncryption; }
            set { this.PasswordInfo.AllowReversiblePasswordEncryption = value; }
        }

        internal AdvancedFilters rosf;

        public virtual AdvancedFilters AdvancedSearchFilter
        {
            get
            {
                return rosf;
            }
        }

        public void SetPassword(string newPassword)
        {
            this.PasswordInfo.SetPassword(newPassword);
        }

        public void ChangePassword(string oldPassword, string newPassword)
        {
            this.PasswordInfo.ChangePassword(oldPassword, newPassword);
        }

        public void ExpirePasswordNow()
        {
            this.PasswordInfo.ExpirePasswordNow();
        }

        public void RefreshExpiredPassword()
        {
            this.PasswordInfo.RefreshExpiredPassword();
        }

        // Certificates property
        private X509Certificate2Collection _certificates = new X509Certificate2Collection();
        private List<string> _certificateOriginalThumbprints = new List<string>();
        private LoadState _X509Certificate2CollectionLoaded = LoadState.NotSet;

        public X509Certificate2Collection Certificates
        {
            get
            {
                return HandleGet<X509Certificate2Collection>(ref _certificates,
                                    PropertyNames.AuthenticablePrincipalCertificates, ref _X509Certificate2CollectionLoaded);
            }
        }

        //
        // Public Methods
        //
        public static PrincipalSearchResult<AuthenticablePrincipal> FindByLockoutTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByLockoutTime<AuthenticablePrincipal>(context, time, type);
        }

        public static PrincipalSearchResult<AuthenticablePrincipal> FindByLogonTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByLogonTime<AuthenticablePrincipal>(context, time, type);
        }

        public static PrincipalSearchResult<AuthenticablePrincipal> FindByExpirationTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByExpirationTime<AuthenticablePrincipal>(context, time, type);
        }

        public static PrincipalSearchResult<AuthenticablePrincipal> FindByBadPasswordAttempt(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByBadPasswordAttempt<AuthenticablePrincipal>(context, time, type);
        }

        public static PrincipalSearchResult<AuthenticablePrincipal> FindByPasswordSetTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByPasswordSetTime<AuthenticablePrincipal>(context, time, type);
        }

        //
        // Protected implementations
        //

        protected static PrincipalSearchResult<T> FindByLockoutTime<T>(PrincipalContext context, DateTime time, MatchType type)
        {
            CheckFindByArgs(context, time, type, typeof(T));

            return new PrincipalSearchResult<T>(context.QueryCtx.FindByLockoutTime(time, type, typeof(T)));
        }
        protected static PrincipalSearchResult<T> FindByLogonTime<T>(PrincipalContext context, DateTime time, MatchType type)
        {
            CheckFindByArgs(context, time, type, typeof(T));

            return new PrincipalSearchResult<T>(context.QueryCtx.FindByLogonTime(time, type, typeof(T)));
        }
        protected static PrincipalSearchResult<T> FindByExpirationTime<T>(PrincipalContext context, DateTime time, MatchType type)
        {
            CheckFindByArgs(context, time, type, typeof(T));

            return new PrincipalSearchResult<T>(context.QueryCtx.FindByExpirationTime(time, type, typeof(T)));
        }
        protected static PrincipalSearchResult<T> FindByBadPasswordAttempt<T>(PrincipalContext context, DateTime time, MatchType type)
        {
            CheckFindByArgs(context, time, type, typeof(T));

            return new PrincipalSearchResult<T>(context.QueryCtx.FindByBadPasswordAttempt(time, type, typeof(T)));
        }
        protected static PrincipalSearchResult<T> FindByPasswordSetTime<T>(PrincipalContext context, DateTime time, MatchType type)
        {
            CheckFindByArgs(context, time, type, typeof(T));

            return new PrincipalSearchResult<T>(context.QueryCtx.FindByPasswordSetTime(time, type, typeof(T)));
        }

        //
        // Private implementation
        //
        internal protected AuthenticablePrincipal(PrincipalContext context)
        {
            if (context == null)
                throw new ArgumentException(SR.NullArguments);

            this.ContextRaw = context;
            this.unpersisted = true;
            this.rosf = new AdvancedFilters(this);
        }

        internal protected AuthenticablePrincipal(PrincipalContext context, string samAccountName, string password, bool enabled) : this(context)
        {
            if (samAccountName != null)
            {
                this.SamAccountName = samAccountName;
            }

            if (password != null)
            {
                this.SetPassword(password);
            }

            this.Enabled = enabled;
        }

        static internal AuthenticablePrincipal MakeAuthenticablePrincipal(PrincipalContext ctx)
        {
            AuthenticablePrincipal ap = new AuthenticablePrincipal(ctx);
            ap.unpersisted = false;

            return ap;
        }

        private static void CheckFindByArgs(PrincipalContext context, DateTime time, MatchType type, Type subtype)
        {
            if ((subtype != typeof(AuthenticablePrincipal)) &&
                 (!subtype.IsSubclassOf(typeof(AuthenticablePrincipal))))
                throw new ArgumentException(SR.AuthenticablePrincipalMustBeSubtypeOfAuthPrinc);

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (subtype == null)
                throw new ArgumentNullException(nameof(subtype));
        }

        //
        // Load/Store
        //

        //
        // Loading with query results
        //

        internal override void LoadValueIntoProperty(string propertyName, object value)
        {
            switch (propertyName)
            {
                case PropertyNames.AuthenticablePrincipalCertificates:
                    LoadCertificateCollection((List<byte[]>)value);
                    RefreshOriginalThumbprintList();
                    _X509Certificate2CollectionLoaded = LoadState.Loaded;
                    break;

                case PropertyNames.AuthenticablePrincipalEnabled:
                    _enabled = (bool)value;
                    _enabledChanged = LoadState.Loaded;
                    break;

                default:
                    if (propertyName.StartsWith(PropertyNames.AcctInfoPrefix, StringComparison.Ordinal))
                    {
                        // If this is the first AccountInfo attribute we're loading,
                        // we'll need to create the AccountInfo to hold it
                        if (_accountInfo == null)
                            _accountInfo = new AccountInfo(this);

                        _accountInfo.LoadValueIntoProperty(propertyName, value);
                    }
                    else if (propertyName.StartsWith(PropertyNames.PwdInfoPrefix, StringComparison.Ordinal))
                    {
                        // If this is the first PasswordInfo attribute we're loading,
                        // we'll need to create the PasswordInfo to hold it
                        if (_passwordInfo == null)
                            _passwordInfo = new PasswordInfo(this);

                        _passwordInfo.LoadValueIntoProperty(propertyName, value);
                    }
                    else
                    {
                        base.LoadValueIntoProperty(propertyName, value);
                    }
                    break;
            }
        }

        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        // Given a property name, returns true if that property has changed since it was loaded, false otherwise.
        internal override bool GetChangeStatusForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthenticablePrincipal", "GetChangeStatusForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case PropertyNames.AuthenticablePrincipalCertificates:
                    return HasCertificateCollectionChanged();

                case PropertyNames.AuthenticablePrincipalEnabled:
                    return _enabledChanged == LoadState.Changed;

                default:

                    // Check if the property is supported by AdvancedFilter class.
                    // If writeable properties are added to the rosf class then we will need
                    // to add some type of tag to the property names to differentiate them here
                    bool? val = rosf.GetChangeStatusForProperty(propertyName);

                    if (val.HasValue == true)
                        return val.Value;

                    if (propertyName.StartsWith(PropertyNames.AcctInfoPrefix, StringComparison.Ordinal))
                    {
                        if (_accountInfo == null)
                            return false;

                        return _accountInfo.GetChangeStatusForProperty(propertyName);
                    }
                    else if (propertyName.StartsWith(PropertyNames.PwdInfoPrefix, StringComparison.Ordinal))
                    {
                        if (_passwordInfo == null)
                            return false;

                        return _passwordInfo.GetChangeStatusForProperty(propertyName);
                    }
                    else
                    {
                        return base.GetChangeStatusForProperty(propertyName);
                    }
            }
        }

        // Given a property name, returns the current value for the property.
        internal override object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthenticablePrincipal", "GetValueForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case PropertyNames.AuthenticablePrincipalCertificates:
                    return _certificates;

                case PropertyNames.AuthenticablePrincipalEnabled:
                    return _enabled;

                default:

                    object val = rosf.GetValueForProperty(propertyName);

                    if (null != val)
                        return val;

                    if (propertyName.StartsWith(PropertyNames.AcctInfoPrefix, StringComparison.Ordinal))
                    {
                        if (_accountInfo == null)
                        {
                            // Should never happen, since GetChangeStatusForProperty returned false
                            Debug.Fail("AuthenticablePrincipal.GetValueForProperty(AcctInfo): shouldn't be here");
                            throw new InvalidOperationException();
                        }

                        return _accountInfo.GetValueForProperty(propertyName);
                    }
                    else if (propertyName.StartsWith(PropertyNames.PwdInfoPrefix, StringComparison.Ordinal))
                    {
                        if (_passwordInfo == null)
                        {
                            // Should never happen, since GetChangeStatusForProperty returned false                
                            Debug.Fail("AuthenticablePrincipal.GetValueForProperty(PwdInfo): shouldn't be here");
                            throw new InvalidOperationException();
                        }

                        return _passwordInfo.GetValueForProperty(propertyName);
                    }
                    else
                    {
                        return base.GetValueForProperty(propertyName);
                    }
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal override void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthenticablePrincipal", "ResetAllChangeStatus");

            _enabledChanged = (_enabledChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;

            RefreshOriginalThumbprintList();

            if (_accountInfo != null)
            {
                _accountInfo.ResetAllChangeStatus();
            }

            if (_passwordInfo != null)
            {
                _passwordInfo.ResetAllChangeStatus();
            }

            rosf.ResetAllChangeStatus();

            base.ResetAllChangeStatus();
        }

        //
        // Certificate support routines
        //

        // Given a list of certs (expressed as byte[]s), loads them into
        // the certificate collection
        private void LoadCertificateCollection(List<byte[]> certificatesToLoad)
        {
            // To support reload
            _certificates.Clear();
            Debug.Assert(_certificates.Count == 0);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthenticablePrincipal", "LoadCertificateCollection: loading {0} certs", certificatesToLoad.Count);

            foreach (byte[] rawCert in certificatesToLoad)
            {
                try
                {
                    _certificates.Import(rawCert);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    // skip the invalid certificate
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "AuthenticablePrincipal", "LoadCertificateCollection: skipped bad cert");
                    continue;
                }
            }
        }

        // Regenerates the certificateOriginalThumbprints list based on what's
        // currently in the certificate collection
        private void RefreshOriginalThumbprintList()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthenticablePrincipal", "RefreshOriginalThumbprintList: resetting thumbprints");

            _certificateOriginalThumbprints.Clear();

            foreach (X509Certificate2 certificate in _certificates)
            {
                _certificateOriginalThumbprints.Add(certificate.Thumbprint);
            }
        }

        // Returns true if the certificate collection has changed since the last time
        // certificateOriginalThumbprints was refreshed (i.e., since the last time
        // RefreshOriginalThumbprintList was called)
        private bool HasCertificateCollectionChanged()
        {
            // If the size isn't the same, the collection has certainly changed
            if (_certificates.Count != _certificateOriginalThumbprints.Count)
            {
                GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "AuthenticablePrincipal",
                            "HasCertificateCollectionChanged: original count {0}, current count{1}",
                            _certificateOriginalThumbprints.Count,
                            _certificates.Count);
                return true;
            }

            // Make a copy of the thumbprint list, so we can alter the copy without effecting the original.
            List<string> remainingOriginalThumbprints = new List<string>(_certificateOriginalThumbprints);

            foreach (X509Certificate2 certificate in _certificates)
            {
                string thumbprint = certificate.Thumbprint;

                // If we found a cert whose thumbprint wasn't in the thumbprints list,
                // it was inserted --> collection has changed                
                if (!remainingOriginalThumbprints.Contains(thumbprint))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "AuthenticablePrincipal", "RefreshOriginalThumbprintList: found inserted cert");
                    return true;
                }

                // We remove the thumbprint from the list so that if, for some reason, they inserted
                // a duplicate of a certificate that was already in the list, we'll detect it as an insert
                // when we encounter that cert a second time
                remainingOriginalThumbprints.Remove(thumbprint);
            }

            // If a certificate was deleted, there are two possibilities:
            //  (1) The removal caused the size to change.  We'll have caught it above and returned true.
            //  (2) The size didn't change (because there was also an insert).  We'll have caught the insert
            //      above and returned true.  Note that even if they insert a duplicate of a cert that was
            //      already in the collection, we'll catch it because we remove the thumbprint from the
            //      local copy of the thumbprint list each time we use that thumbprint.

            Debug.Assert(remainingOriginalThumbprints.Count == 0);
            return false;
        }
    }
}
