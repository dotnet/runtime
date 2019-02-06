// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryRdnPrefix("CN")]
    public class UserPrincipal : AuthenticablePrincipal
    {
        //
        // Public constructors
        //
        public UserPrincipal(PrincipalContext context) : base(context)
        {
            if (context == null)
                throw new ArgumentException(SR.NullArguments);

            this.ContextRaw = context;
            this.unpersisted = true;
        }

        public UserPrincipal(PrincipalContext context, string samAccountName, string password, bool enabled) : this(context)
        {
            if (samAccountName == null || password == null)
                throw new ArgumentException(SR.NullArguments);

            if (Context.ContextType != ContextType.ApplicationDirectory)
                this.SamAccountName = samAccountName;

            this.Name = samAccountName;
            this.SetPassword(password);
            this.Enabled = enabled;
        }

        //
        // Public properties
        //

        // GivenName
        private string _givenName = null;        // the actual property value
        private LoadState _givenNameChanged = LoadState.NotSet;   // change-tracking

        public string GivenName
        {
            get
            {
                return HandleGet<string>(ref _givenName, PropertyNames.UserGivenName, ref _givenNameChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.UserGivenName))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _givenName, value, ref _givenNameChanged,
                                  PropertyNames.UserGivenName);
            }
        }

        // MiddleName
        private string _middleName = null;        // the actual property value
        private LoadState _middleNameChanged = LoadState.NotSet;   // change-tracking

        public string MiddleName
        {
            get
            {
                return HandleGet<string>(ref _middleName, PropertyNames.UserMiddleName, ref _middleNameChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.UserMiddleName))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _middleName, value, ref _middleNameChanged,
                                  PropertyNames.UserMiddleName);
            }
        }

        // Surname
        private string _surname = null;        // the actual property value
        private LoadState _surnameChanged = LoadState.NotSet;   // change-tracking

        public string Surname
        {
            get
            {
                return HandleGet<string>(ref _surname, PropertyNames.UserSurname, ref _surnameChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.UserSurname))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _surname, value, ref _surnameChanged,
                                  PropertyNames.UserSurname);
            }
        }

        // EmailAddress
        private string _emailAddress = null;        // the actual property value
        private LoadState _emailAddressChanged = LoadState.NotSet;   // change-tracking

        public string EmailAddress
        {
            get
            {
                return HandleGet<string>(ref _emailAddress, PropertyNames.UserEmailAddress, ref _emailAddressChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.UserEmailAddress))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _emailAddress, value, ref _emailAddressChanged,
                                  PropertyNames.UserEmailAddress);
            }
        }

        // VoiceTelephoneNumber
        private string _voiceTelephoneNumber = null;        // the actual property value
        private LoadState _voiceTelephoneNumberChanged = LoadState.NotSet;   // change-tracking

        public string VoiceTelephoneNumber
        {
            get
            {
                return HandleGet<string>(ref _voiceTelephoneNumber, PropertyNames.UserVoiceTelephoneNumber, ref _voiceTelephoneNumberChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.UserVoiceTelephoneNumber))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _voiceTelephoneNumber, value, ref _voiceTelephoneNumberChanged,
                                  PropertyNames.UserVoiceTelephoneNumber);
            }
        }

        // EmployeeId
        private string _employeeID = null;        // the actual property value
        private LoadState _employeeIDChanged = LoadState.NotSet;   // change-tracking

        public string EmployeeId
        {
            get
            {
                return HandleGet<string>(ref _employeeID, PropertyNames.UserEmployeeID, ref _employeeIDChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.UserEmployeeID))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _employeeID, value, ref _employeeIDChanged,
                                  PropertyNames.UserEmployeeID);
            }
        }

        public override AdvancedFilters AdvancedSearchFilter { get { return rosf; } }

        public static UserPrincipal Current
        {
            get
            {
                PrincipalContext context;

                // Get the correct PrincipalContext to query against, depending on whether we're running
                // as a local or domain user
                if (Utils.IsSamUser())
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "Current: is local user");

#if PAPI_REGSAM
                    context = new PrincipalContext(ContextType.Machine);
#else
                    // This implementation doesn't support Reg-SAM/MSAM (machine principals)
                    throw new NotSupportedException(SR.UserLocalNotSupportedOnPlatform);       
#endif // PAPI_REGSAM
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "Current: is not local user");

#if PAPI_AD
                    context = new PrincipalContext(ContextType.Domain);
#else
                    // This implementation doesn't support AD (domain principals)
                    throw new NotSupportedException(SR.UserDomainNotSupportedOnPlatform);       
#endif // PAPI_AD
                }

                // Construct a query for the current user, using a SID IdentityClaim

                IntPtr pSid = IntPtr.Zero;
                UserPrincipal user = null;

                try
                {
                    pSid = Utils.GetCurrentUserSid();
                    byte[] sid = Utils.ConvertNativeSidToByteArray(pSid);
                    SecurityIdentifier sidObj = new SecurityIdentifier(sid, 0);

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "Current: using SID " + sidObj.ToString());

                    user = UserPrincipal.FindByIdentity(context, IdentityType.Sid, sidObj.ToString());
                }
                finally
                {
                    if (pSid != IntPtr.Zero)
                        System.Runtime.InteropServices.Marshal.FreeHGlobal(pSid);
                }

                // We're running as the user, we know they must exist, but perhaps we don't have access
                // to their user object
                if (user == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "User", "Current: found no user");
                    throw new NoMatchingPrincipalException(SR.UserCouldNotFindCurrent);
                }

                return user;
            }
        }

        //
        // Public methods
        //

        public static new PrincipalSearchResult<UserPrincipal> FindByLockoutTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByLockoutTime<UserPrincipal>(context, time, type);
        }

        public static new PrincipalSearchResult<UserPrincipal> FindByLogonTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByLogonTime<UserPrincipal>(context, time, type);
        }

        public static new PrincipalSearchResult<UserPrincipal> FindByExpirationTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByExpirationTime<UserPrincipal>(context, time, type);
        }

        public static new PrincipalSearchResult<UserPrincipal> FindByBadPasswordAttempt(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByBadPasswordAttempt<UserPrincipal>(context, time, type);
        }

        public static new PrincipalSearchResult<UserPrincipal> FindByPasswordSetTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByPasswordSetTime<UserPrincipal>(context, time, type);
        }

        public static new UserPrincipal FindByIdentity(PrincipalContext context, string identityValue)
        {
            return (UserPrincipal)FindByIdentityWithType(context, typeof(UserPrincipal), identityValue);
        }

        public static new UserPrincipal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            return (UserPrincipal)FindByIdentityWithType(context, typeof(UserPrincipal), identityType, identityValue);
        }

        public PrincipalSearchResult<Principal> GetAuthorizationGroups()
        {
            return new PrincipalSearchResult<Principal>(GetAuthorizationGroupsHelper());
        }

        //
        // Internal "constructor": Used for constructing Users returned by a query
        //
        static internal UserPrincipal MakeUser(PrincipalContext ctx)
        {
            UserPrincipal u = new UserPrincipal(ctx);
            u.unpersisted = false;

            return u;
        }

        //
        // Private implementation
        //

        private ResultSet GetAuthorizationGroupsHelper()
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Unpersisted principals are not members of any group
            if (this.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "GetAuthorizationGroupsHelper: unpersisted, using EmptySet");
                return new EmptySet();
            }

            StoreCtx storeCtx = GetStoreCtxToUse();
            Debug.Assert(storeCtx != null);

            GlobalDebug.WriteLineIf(
                    GlobalDebug.Info,
                    "User",
                    "GetAuthorizationGroupsHelper: retrieving AZ groups from StoreCtx of type=" + storeCtx.GetType().ToString() +
                        ", base path=" + storeCtx.BasePath);

            ResultSet resultSet = storeCtx.GetGroupsMemberOfAZ(this);

            return resultSet;
        }

        //
        // Load/Store implementation
        //

        //
        // Loading with query results
        //
        internal override void LoadValueIntoProperty(string propertyName, object value)
        {
            if (value == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "LoadValueIntoProperty: name=" + propertyName + " value= null");
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "LoadValueIntoProperty: name=" + propertyName + " value=" + value.ToString());
            }

            switch (propertyName)
            {
                case (PropertyNames.UserGivenName):
                    _givenName = (string)value;
                    _givenNameChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.UserMiddleName):
                    _middleName = (string)value;
                    _middleNameChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.UserSurname):
                    _surname = (string)value;
                    _surnameChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.UserEmailAddress):
                    _emailAddress = (string)value;
                    _emailAddressChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.UserVoiceTelephoneNumber):
                    _voiceTelephoneNumber = (string)value;
                    _voiceTelephoneNumberChanged = LoadState.Loaded;
                    break;

                case (PropertyNames.UserEmployeeID):
                    _employeeID = (string)value;
                    _employeeIDChanged = LoadState.Loaded;
                    break;

                default:
                    base.LoadValueIntoProperty(propertyName, value);
                    break;
            }
        }

        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        // Given a property name, returns true if that property has changed since it was loaded, false otherwise.
        internal override bool GetChangeStatusForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "GetChangeStatusForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case (PropertyNames.UserGivenName):
                    return _givenNameChanged == LoadState.Changed;

                case (PropertyNames.UserMiddleName):
                    return _middleNameChanged == LoadState.Changed;

                case (PropertyNames.UserSurname):
                    return _surnameChanged == LoadState.Changed;

                case (PropertyNames.UserEmailAddress):
                    return _emailAddressChanged == LoadState.Changed;

                case (PropertyNames.UserVoiceTelephoneNumber):
                    return _voiceTelephoneNumberChanged == LoadState.Changed;

                case (PropertyNames.UserEmployeeID):
                    return _employeeIDChanged == LoadState.Changed;

                default:
                    return base.GetChangeStatusForProperty(propertyName);
            }
        }

        // Given a property name, returns the current value for the property.
        internal override object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "GetValueForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case (PropertyNames.UserGivenName):
                    return _givenName;

                case (PropertyNames.UserMiddleName):
                    return _middleName;

                case (PropertyNames.UserSurname):
                    return _surname;

                case (PropertyNames.UserEmailAddress):
                    return _emailAddress;

                case (PropertyNames.UserVoiceTelephoneNumber):
                    return _voiceTelephoneNumber;

                case (PropertyNames.UserEmployeeID):
                    return _employeeID;

                default:
                    return base.GetValueForProperty(propertyName);
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal override void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "ResetAllChangeStatus");

            _givenNameChanged = (_givenNameChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet; ;
            _middleNameChanged = (_middleNameChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet; ;
            _surnameChanged = (_surnameChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet; ;
            _emailAddressChanged = (_emailAddressChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet; ;
            _voiceTelephoneNumberChanged = (_voiceTelephoneNumberChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet; ;
            _employeeIDChanged = (_employeeIDChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet; ;

            base.ResetAllChangeStatus();
        }
    }
}
