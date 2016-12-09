/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    QbeFilterDescription.cs

Abstract:

    Implements the QbeFilterDescription class.

History:

    05-May-2004    MattRim     Created

--*/

using System;
using System.Collections;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    //
    // A collection of individual property filters
    //
    class QbeFilterDescription
    {
        ArrayList filtersToApply = new ArrayList();

        public QbeFilterDescription()
        {
            // Nothing to do
        }       
        
        public ArrayList FiltersToApply
        {
            get
            {
                return filtersToApply;
            }
        }
    }

    //
    // Constructs individual property filters, given the name of the property
    //
    class FilterFactory
    {
        // Put a private constructor because this class should only be used as static methods
        private FilterFactory() { }
    
        static Hashtable subclasses = new Hashtable();

        static FilterFactory()
        {
            subclasses[DescriptionFilter.PropertyNameStatic] = typeof(DescriptionFilter);
            subclasses[DisplayNameFilter.PropertyNameStatic] = typeof(DisplayNameFilter);
            subclasses[IdentityClaimFilter.PropertyNameStatic] = typeof(IdentityClaimFilter);
            subclasses[SamAccountNameFilter.PropertyNameStatic] = typeof(SamAccountNameFilter);
            subclasses[DistinguishedNameFilter.PropertyNameStatic] = typeof(DistinguishedNameFilter);
            subclasses[GuidFilter.PropertyNameStatic] = typeof(GuidFilter);
            subclasses[UserPrincipalNameFilter.PropertyNameStatic] = typeof(UserPrincipalNameFilter);      
            subclasses[StructuralObjectClassFilter.PropertyNameStatic] = typeof(StructuralObjectClassFilter);            
            subclasses[NameFilter.PropertyNameStatic] = typeof(NameFilter);            
            subclasses[CertificateFilter.PropertyNameStatic] = typeof(CertificateFilter);
            subclasses[AuthPrincEnabledFilter.PropertyNameStatic] = typeof(AuthPrincEnabledFilter);
            subclasses[PermittedWorkstationFilter.PropertyNameStatic] = typeof(PermittedWorkstationFilter);
            subclasses[PermittedLogonTimesFilter.PropertyNameStatic] = typeof(PermittedLogonTimesFilter);
            subclasses[ExpirationDateFilter.PropertyNameStatic] = typeof(ExpirationDateFilter);
            subclasses[SmartcardLogonRequiredFilter.PropertyNameStatic] = typeof(SmartcardLogonRequiredFilter);
            subclasses[DelegationPermittedFilter.PropertyNameStatic] = typeof(DelegationPermittedFilter);
            subclasses[HomeDirectoryFilter.PropertyNameStatic] = typeof(HomeDirectoryFilter);
            subclasses[HomeDriveFilter.PropertyNameStatic] = typeof(HomeDriveFilter);
            subclasses[ScriptPathFilter.PropertyNameStatic] = typeof(ScriptPathFilter);
            subclasses[PasswordNotRequiredFilter.PropertyNameStatic] = typeof(PasswordNotRequiredFilter);
            subclasses[PasswordNeverExpiresFilter.PropertyNameStatic] = typeof(PasswordNeverExpiresFilter);
            subclasses[CannotChangePasswordFilter.PropertyNameStatic] = typeof(CannotChangePasswordFilter);
            subclasses[AllowReversiblePasswordEncryptionFilter.PropertyNameStatic] = typeof(AllowReversiblePasswordEncryptionFilter);
            subclasses[GivenNameFilter.PropertyNameStatic] = typeof(GivenNameFilter);
            subclasses[MiddleNameFilter.PropertyNameStatic] = typeof(MiddleNameFilter);
            subclasses[SurnameFilter.PropertyNameStatic] = typeof(SurnameFilter);
            subclasses[EmailAddressFilter.PropertyNameStatic] = typeof(EmailAddressFilter);
            subclasses[VoiceTelephoneNumberFilter.PropertyNameStatic] = typeof(VoiceTelephoneNumberFilter);
            subclasses[EmployeeIDFilter.PropertyNameStatic] = typeof(EmployeeIDFilter);
            subclasses[GroupIsSecurityGroupFilter.PropertyNameStatic] = typeof(GroupIsSecurityGroupFilter);
            subclasses[GroupScopeFilter.PropertyNameStatic] = typeof(GroupScopeFilter);
            subclasses[ServicePrincipalNameFilter.PropertyNameStatic] = typeof(ServicePrincipalNameFilter);
            subclasses[ExtensionCacheFilter.PropertyNameStatic] = typeof(ExtensionCacheFilter);
            subclasses[BadPasswordAttemptFilter.PropertyNameStatic] = typeof(BadPasswordAttemptFilter);
            subclasses[ExpiredAccountFilter.PropertyNameStatic] = typeof(ExpiredAccountFilter);
            subclasses[LastLogonTimeFilter.PropertyNameStatic] = typeof(LastLogonTimeFilter);
            subclasses[LockoutTimeFilter.PropertyNameStatic] = typeof(LockoutTimeFilter);
            subclasses[PasswordSetTimeFilter.PropertyNameStatic] = typeof(PasswordSetTimeFilter);
            subclasses[BadLogonCountFilter.PropertyNameStatic] = typeof(BadLogonCountFilter);
            
        }


        // Given a property name, constructs and returns the appropriate individual property
        // filter
        static public object CreateFilter(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "QbeFilterDescription", "FilterFactory.CreateFilter: name=" + propertyName);
        
            Type type = (Type) subclasses[propertyName];
            Debug.Assert(type != null);

            return System.Activator.CreateInstance(type);
        }
    }


    //
    // The individual property filters
    //

    // Base class: Defines the external interface shared by all individual property filters
    abstract class FilterBase
    {
        public object Value
        {
            get {return this.value;}
            set {this.value = value;}
        }

        object value;


        // Some providers need place to store extra state, e.g., a processed form of Value
        public object Extra
        {
            get {return this.extra;}
            set {this.extra = value;}
        }

        object extra = null;
        
        public abstract string PropertyName {get;}
    }

    // The derived classes
 
    class DescriptionFilter : FilterBase
    {   
        public const string PropertyNameStatic = PropertyNames.PrincipalDescription;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class SidFilter : FilterBase
    {   
        public const string PropertyNameStatic = PropertyNames.PrincipalSid;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class SamAccountNameFilter : FilterBase
    {   
        public const string PropertyNameStatic = PropertyNames.PrincipalSamAccountName;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }

    class DistinguishedNameFilter : FilterBase
    {   
        public const string PropertyNameStatic = PropertyNames.PrincipalDistinguishedName;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }
    class GuidFilter : FilterBase
    {   
        public const string PropertyNameStatic = PropertyNames.PrincipalGuid;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }
    class IdentityClaimFilter : FilterBase
    {    
        public const string PropertyNameStatic = PropertyNames.PrincipalIdentityClaims;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }

    class UserPrincipalNameFilter : FilterBase
    {   
        public const string PropertyNameStatic = PropertyNames.PrincipalUserPrincipalName;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }
    class  StructuralObjectClassFilter : FilterBase
    {   
        public const string PropertyNameStatic = PropertyNames.PrincipalStructuralObjectClass;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }
    class  NameFilter: FilterBase
    {   
        public const string PropertyNameStatic = PropertyNames.PrincipalName;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }

    class DisplayNameFilter : FilterBase
    {    
        public const string PropertyNameStatic = PropertyNames.PrincipalDisplayName;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }
    class CertificateFilter : FilterBase
    {    
        public const string PropertyNameStatic = PropertyNames.AuthenticablePrincipalCertificates;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class AuthPrincEnabledFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AuthenticablePrincipalEnabled;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class PermittedWorkstationFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoPermittedWorkstations;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }
    
    class PermittedLogonTimesFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoPermittedLogonTimes;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class ExpirationDateFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoExpirationDate;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class SmartcardLogonRequiredFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoSmartcardRequired;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class DelegationPermittedFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoDelegationPermitted;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class HomeDirectoryFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoHomeDirectory;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class HomeDriveFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoHomeDrive;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class ScriptPathFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoScriptPath;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class PasswordNotRequiredFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.PwdInfoPasswordNotRequired;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }
    
    class PasswordNeverExpiresFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.PwdInfoPasswordNeverExpires;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }
    
    class CannotChangePasswordFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.PwdInfoCannotChangePassword;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }
    
    class AllowReversiblePasswordEncryptionFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.PwdInfoAllowReversiblePasswordEncryption;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class GivenNameFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.UserGivenName;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class MiddleNameFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.UserMiddleName;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class SurnameFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.UserSurname;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class EmailAddressFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.UserEmailAddress;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class VoiceTelephoneNumberFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.UserVoiceTelephoneNumber;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class EmployeeIDFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.UserEmployeeID;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class GroupIsSecurityGroupFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.GroupIsSecurityGroup;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class GroupScopeFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.GroupGroupScope;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }

    class ServicePrincipalNameFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.ComputerServicePrincipalNames;
        public override string PropertyName { get {return PropertyNameStatic;} }

    }


    class ExtensionCacheFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.PrincipalExtensionCache;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }

    class BadPasswordAttemptFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.PwdInfoLastBadPasswordAttempt;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }
    
    class LastLogonTimeFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoLastLogon;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }
    
    class LockoutTimeFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoAcctLockoutTime;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }

    class ExpiredAccountFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoExpiredAccount;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }
    
    class PasswordSetTimeFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.PwdInfoLastPasswordSet;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }

    class BadLogonCountFilter : FilterBase
    {
        public const string PropertyNameStatic = PropertyNames.AcctInfoBadLogonCount;
        public override string PropertyName { get {return PropertyNameStatic;} }
    }    
}
