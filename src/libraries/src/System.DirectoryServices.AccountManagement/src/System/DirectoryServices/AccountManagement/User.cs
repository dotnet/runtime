/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    User.cs

Abstract:

    Implements the User class.

History:

    04-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    // <SecurityKernel Critical="True" Ring="0">
    // <SatisfiesLinkDemand Name="AuthenticablePrincipal" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
    [DirectoryRdnPrefix("CN")]
    public class UserPrincipal : AuthenticablePrincipal
    {
        //
        // Public constructors
        //
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
        public UserPrincipal(PrincipalContext context) : base(context)
        {
            if (context == null)
                throw new ArgumentException(StringResources.NullArguments);
            
            this.ContextRaw = context;
            this.unpersisted = true;
        }

        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
        public UserPrincipal(PrincipalContext context, string samAccountName, string password, bool enabled) : this(context)
        {
            if (samAccountName == null || password == null)
                throw new ArgumentException(StringResources.NullArguments);

            if ( Context.ContextType != ContextType.ApplicationDirectory)
                this.SamAccountName = samAccountName;
            
            this.Name = samAccountName;
            this.SetPassword(password);     
            this.Enabled = enabled;
            
        }
        

        //
        // Public properties
        //

        // GivenName
        string givenName = null;        // the actual property value
        LoadState givenNameChanged = LoadState.NotSet;   // change-tracking

        public string GivenName
        {
            get
            {
                return HandleGet<string>(ref this.givenName, PropertyNames.UserGivenName, ref givenNameChanged);
            }

            set
            {
                if ( !GetStoreCtxToUse().IsValidProperty(this, PropertyNames.UserGivenName))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
                
                HandleSet<string>(ref this.givenName, value, ref this.givenNameChanged, 
                                  PropertyNames.UserGivenName);
            }
        }


        // MiddleName
        string middleName = null;        // the actual property value
        LoadState middleNameChanged = LoadState.NotSet;   // change-tracking
                
        public string MiddleName
        {
            get
            {
                return HandleGet<string>(ref this.middleName, PropertyNames.UserMiddleName, ref middleNameChanged);
            }

            set
            {
                if ( !GetStoreCtxToUse().IsValidProperty( this, PropertyNames.UserMiddleName))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                HandleSet<string>(ref this.middleName, value, ref this.middleNameChanged, 
                                  PropertyNames.UserMiddleName);
            }
        }

        // Surname
        string surname = null;        // the actual property value
        LoadState surnameChanged = LoadState.NotSet;   // change-tracking
        
        public string Surname
        {
            get
            {
                return HandleGet<string>(ref this.surname, PropertyNames.UserSurname, ref surnameChanged);
            }

            set
            {
                if ( !GetStoreCtxToUse().IsValidProperty( this, PropertyNames.UserSurname))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                HandleSet<string>(ref this.surname, value, ref this.surnameChanged, 
                                  PropertyNames.UserSurname);
            }
        }

        // EmailAddress
        string emailAddress = null;        // the actual property value
        LoadState emailAddressChanged = LoadState.NotSet;   // change-tracking
        
        public string EmailAddress
        {
            get
            {
                return HandleGet<string>(ref this.emailAddress, PropertyNames.UserEmailAddress, ref emailAddressChanged);
            }

            set
            {
                if ( !GetStoreCtxToUse().IsValidProperty( this, PropertyNames.UserEmailAddress))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                HandleSet<string>(ref this.emailAddress, value, ref this.emailAddressChanged, 
                                  PropertyNames.UserEmailAddress);
            }
        }
        
        // VoiceTelephoneNumber
        string voiceTelephoneNumber = null;        // the actual property value
        LoadState voiceTelephoneNumberChanged = LoadState.NotSet;   // change-tracking

        public string VoiceTelephoneNumber
        {
            get
            {
                return HandleGet<string>(ref this.voiceTelephoneNumber, PropertyNames.UserVoiceTelephoneNumber, ref voiceTelephoneNumberChanged);
            }

            set
            {
                if ( !GetStoreCtxToUse().IsValidProperty( this, PropertyNames.UserVoiceTelephoneNumber))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                HandleSet<string>(ref this.voiceTelephoneNumber, value, ref this.voiceTelephoneNumberChanged, 
                                  PropertyNames.UserVoiceTelephoneNumber);
            }
        }

        // EmployeeId
        string employeeID = null;        // the actual property value
        LoadState employeeIDChanged = LoadState.NotSet;   // change-tracking

        public string EmployeeId
        {
            get
            {
                return HandleGet<string>(ref this.employeeID, PropertyNames.UserEmployeeID, ref employeeIDChanged);
            }

            set
            {
                if ( !GetStoreCtxToUse().IsValidProperty(this,  PropertyNames.UserEmployeeID))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                HandleSet<string>(ref this.employeeID, value, ref this.employeeIDChanged, 
                                  PropertyNames.UserEmployeeID);
            }
        }

        public override AdvancedFilters  AdvancedSearchFilter { get { return rosf; } }
        
        public static UserPrincipal Current
        {
			[SecurityPermission(SecurityAction.Assert, Flags=SecurityPermissionFlag.UnmanagedCode)]        
            get
            {

                // We have to do this inline because declarative CAS is not allowed on properties.
                DirectoryServicesPermission dSPerm = new DirectoryServicesPermission();
                dSPerm.Demand();
                
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
                    throw new NotSupportedException(StringResources.UserLocalNotSupportedOnPlatform);       
#endif // PAPI_REGSAM
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "Current: is not local user");            
                
#if PAPI_AD
                    context = new PrincipalContext(ContextType.Domain);
#else
                    // This implementation doesn't support AD (domain principals)
                    throw new NotSupportedException(StringResources.UserDomainNotSupportedOnPlatform);       
#endif // PAPI_AD
                }

                // Construct a query for the current user, using a SID IdentityClaim

                IntPtr pSid = IntPtr.Zero;
                UserPrincipal user = null;

                try
                {
                    pSid = Utils.GetCurrentUserSid();
                    byte[] sid = Utils.ConvertNativeSidToByteArray(pSid);
                    SecurityIdentifier sidObj = new SecurityIdentifier(sid,0);

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "Current: using SID " + sidObj.ToString());            

                    user = UserPrincipal.FindByIdentity(context, IdentityType.Sid,  sidObj.ToString());
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
                    throw new NoMatchingPrincipalException(StringResources.UserCouldNotFindCurrent);
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
            return (UserPrincipal) FindByIdentityWithType(context, typeof(UserPrincipal), identityValue);
        }
        
        public static new UserPrincipal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            return (UserPrincipal) FindByIdentityWithType(context, typeof(UserPrincipal), identityType, identityValue);
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

        ResultSet GetAuthorizationGroupsHelper()
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
            if ( value == null )
               {
                  GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "LoadValueIntoProperty: name=" + propertyName + " value= null");        
               }
            else
               {
               GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "LoadValueIntoProperty: name=" + propertyName + " value=" + value.ToString());
               }
            
            switch(propertyName)
            {
                case(PropertyNames.UserGivenName):
                    this.givenName = (string) value;
                    this.givenNameChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.UserMiddleName):
                    this.middleName = (string) value;
                    this.middleNameChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.UserSurname):
                    this.surname = (string) value;
                    this.surnameChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.UserEmailAddress):
                    this.emailAddress = (string) value;
                    this.emailAddressChanged = LoadState.Loaded;
                    break;
                    
                case(PropertyNames.UserVoiceTelephoneNumber):
                    this.voiceTelephoneNumber = (string) value;
                    this.voiceTelephoneNumberChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.UserEmployeeID):
                    this.employeeID = (string) value;
                    this.employeeIDChanged = LoadState.Loaded;
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
        
            switch(propertyName)
            {
                case(PropertyNames.UserGivenName):
                    return this.givenNameChanged == LoadState.Changed;

                case(PropertyNames.UserMiddleName):
                    return this.middleNameChanged == LoadState.Changed;

                case(PropertyNames.UserSurname):
                    return this.surnameChanged == LoadState.Changed;

                case(PropertyNames.UserEmailAddress):
                    return this.emailAddressChanged == LoadState.Changed;
                    
                case(PropertyNames.UserVoiceTelephoneNumber):
                    return this.voiceTelephoneNumberChanged == LoadState.Changed;

                case(PropertyNames.UserEmployeeID):
                    return this.employeeIDChanged == LoadState.Changed;

                default:
                    return base.GetChangeStatusForProperty(propertyName);
            }

        }

        // Given a property name, returns the current value for the property.
        internal override object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "GetValueForProperty: name=" + propertyName);
        
            switch(propertyName)
            {
                case(PropertyNames.UserGivenName):
                    return this.givenName;

                case(PropertyNames.UserMiddleName):
                    return this.middleName;

                case(PropertyNames.UserSurname):
                    return this.surname;

                case(PropertyNames.UserEmailAddress):
                    return this.emailAddress;
                    
                case(PropertyNames.UserVoiceTelephoneNumber):
                    return this.voiceTelephoneNumber;

                case(PropertyNames.UserEmployeeID):
                    return this.employeeID;
                    
                default:
                    return base.GetValueForProperty(propertyName);
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal override void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "User", "ResetAllChangeStatus");
        
            this.givenNameChanged = ( this.givenNameChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;;
            this.middleNameChanged = ( this.middleNameChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;;
            this.surnameChanged = ( this.surnameChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;;
            this.emailAddressChanged = ( this.emailAddressChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;;
            this.voiceTelephoneNumberChanged = ( this.voiceTelephoneNumberChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;;
            this.employeeIDChanged = ( this.employeeIDChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;;
            
            base.ResetAllChangeStatus();
        }
    }

}
