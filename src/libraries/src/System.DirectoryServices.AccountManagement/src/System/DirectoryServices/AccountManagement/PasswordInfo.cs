/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    PasswordInfo.cs

Abstract:

    Implements the PasswordInfo class.

History:

    17-May-2004    MattRim     Created

--*/


using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
#if TESTHOOK
    public class PasswordInfo
#else
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    class PasswordInfo
#endif
    {
        //
        // Properties exposed to the public through AuthenticablePrincipal
        //

        // LastPasswordSet
        Nullable<DateTime> lastPasswordSet = null;
        LoadState lastPasswordSetLoaded = LoadState.NotSet;
        
        public Nullable<DateTime> LastPasswordSet
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Nullable`1<System.DateTime>>(System.Nullable`1<System.DateTime>&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Nullable`1<System.DateTime>" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {           
                return this.owningPrincipal.HandleGet<Nullable<DateTime>>(ref this.lastPasswordSet, PropertyNames.PwdInfoLastPasswordSet, ref lastPasswordSetLoaded);                                                                                
            }
        }

        // LastBadPasswordAttempt
        Nullable<DateTime> lastBadPasswordAttempt = null;
        LoadState lastBadPasswordAttemptLoaded = LoadState.NotSet;
        
        public Nullable<DateTime> LastBadPasswordAttempt
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Nullable`1<System.DateTime>>(System.Nullable`1<System.DateTime>&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Nullable`1<System.DateTime>" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {           
                return this.owningPrincipal.HandleGet<Nullable<DateTime>>(ref this.lastBadPasswordAttempt, PropertyNames.PwdInfoLastBadPasswordAttempt, ref lastBadPasswordAttemptLoaded);                    
            }
        }

        // PasswordNotRequired
        bool passwordNotRequired = false;
        LoadState passwordNotRequiredChanged = LoadState.NotSet;

        public bool PasswordNotRequired
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Boolean>(System.Boolean&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Boolean" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {           
                return this.owningPrincipal.HandleGet<bool>(ref this.passwordNotRequired, PropertyNames.PwdInfoPasswordNotRequired, ref passwordNotRequiredChanged);                
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.Boolean>(System.Boolean&,System.Boolean,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                this.owningPrincipal.HandleSet<bool>(ref this.passwordNotRequired, value, ref this.passwordNotRequiredChanged,
                                  PropertyNames.PwdInfoPasswordNotRequired);
            }
        }

        // PasswordNeverExpires
        bool passwordNeverExpires = false;
        LoadState passwordNeverExpiresChanged = LoadState.NotSet;

        public bool PasswordNeverExpires
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Boolean>(System.Boolean&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Boolean" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {           
                return this.owningPrincipal.HandleGet<bool>(ref this.passwordNeverExpires, PropertyNames.PwdInfoPasswordNeverExpires, ref passwordNeverExpiresChanged);                
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.Boolean>(System.Boolean&,System.Boolean,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                this.owningPrincipal.HandleSet<bool>(ref this.passwordNeverExpires, value, ref this.passwordNeverExpiresChanged,
                                  PropertyNames.PwdInfoPasswordNeverExpires);                
            }
        }

        // UserCannotChangePassword
        bool cannotChangePassword = false;
        LoadState cannotChangePasswordChanged = LoadState.NotSet;
        bool cannotChangePasswordRead = false;

        // For this property we are doing an on demand load.  The store will not load this property when load is called beacuse
        // the loading of this property is perf intensive.  HandleGet still needs to be called to load the other object properties if 
        // needed.  We read the status directly from the store and then cache it for use later.
        public bool UserCannotChangePassword
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Boolean>(System.Boolean&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Boolean" />
            // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {

                this.owningPrincipal.HandleGet<bool>(ref this.cannotChangePassword, PropertyNames.PwdInfoCannotChangePassword, ref cannotChangePasswordChanged);
                
                if ( (cannotChangePasswordChanged != LoadState.Changed) && !cannotChangePasswordRead  && !this.owningPrincipal.unpersisted )
                {
                    cannotChangePassword = this.owningPrincipal.GetStoreCtxToUse().AccessCheck(this.owningPrincipal, PrincipalAccessMask.ChangePassword);
                    cannotChangePasswordRead = true;
                }
                                
                return cannotChangePassword;
                
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.Boolean>(System.Boolean&,System.Boolean,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                this.owningPrincipal.HandleSet<bool>(ref this.cannotChangePassword, value, ref this.cannotChangePasswordChanged,
                                  PropertyNames.PwdInfoCannotChangePassword);                                
            }
        }

        // AllowReversiblePasswordEncryption
        bool allowReversiblePasswordEncryption = false;
        LoadState allowReversiblePasswordEncryptionChanged = LoadState.NotSet;

        public bool AllowReversiblePasswordEncryption
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleGet<System.Boolean>(System.Boolean&,System.String,System.DirectoryServices.AccountManagement.LoadState&):System.Boolean" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleGet(T&,System.String,System.DirectoryServices.AccountManagement.LoadState&):T" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {           
                return this.owningPrincipal.HandleGet<bool>(ref this.allowReversiblePasswordEncryption, PropertyNames.PwdInfoAllowReversiblePasswordEncryption, ref allowReversiblePasswordEncryptionChanged);                
            }

            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="Principal.HandleSet<System.Boolean>(System.Boolean&,System.Boolean,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" />
            // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
            // <ReferencesCritical Name="Method: Principal.HandleSet(T&,T,System.DirectoryServices.AccountManagement.LoadState&,System.String):System.Void" Ring="1" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            set
            {
                this.owningPrincipal.HandleSet<bool>(ref this.allowReversiblePasswordEncryption, value, ref this.allowReversiblePasswordEncryptionChanged,
                                  PropertyNames.PwdInfoAllowReversiblePasswordEncryption);                
            }
        }
        

        //
        // Methods exposed to the public through AuthenticablePrincipal
        //

         string storedNewPassword = null;
        
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
        // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public void SetPassword(string newPassword)
        {
            if (newPassword == null)
                throw new ArgumentNullException("newPassword");               
        
            // If we're not persisted, we just save up the change until we're Saved
            if (this.owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "SetPassword: saving until persisted");            
                this.storedNewPassword = newPassword;
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "SetPassword: sending request");
                this.owningPrincipal.GetStoreCtxToUse().SetPassword(this.owningPrincipal, newPassword);            
            }
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
        // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public void ChangePassword(string oldPassword, string newPassword)
        {
            if (oldPassword == null)
                throw new ArgumentNullException("oldPassword");                

            if (newPassword == null)
                throw new ArgumentNullException("newPassword");                

        
            // While you can reset the password on an unpersisted principal (and it will be used as the initial password
            // for the pricipal), changing the password on a principal that doesn't exist yet doesn't make sense
            if (this.owningPrincipal.unpersisted)
                throw new InvalidOperationException(StringResources.PasswordInfoChangePwdOnUnpersistedPrinc);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "ChangePassword: sending request");
            this.owningPrincipal.GetStoreCtxToUse().ChangePassword(this.owningPrincipal, oldPassword, newPassword);            
        }

        bool expirePasswordImmediately = false;

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
        // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public void ExpirePasswordNow()
        {
            // If we're not persisted, we just save up the change until we're Saved        
            if (this.owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "ExpirePasswordNow: saving until persisted");
                this.expirePasswordImmediately = true;
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "ExpirePasswordNow: sending request");
                this.owningPrincipal.GetStoreCtxToUse().ExpirePassword(this.owningPrincipal);            
            }
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" />
        // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
        // <ReferencesCritical Name="Method: Principal.GetStoreCtxToUse():System.DirectoryServices.AccountManagement.StoreCtx" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public void RefreshExpiredPassword()
        {
            // If we're not persisted, we undo the expiration we saved up when ExpirePasswordNow was called (if it was).
            if (this.owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info,"PasswordInfo",  "RefreshExpiredPassword: saving until persisted");
                this.expirePasswordImmediately = false;
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info,"PasswordInfo",  "RefreshExpiredPassword: sending request");
                this.owningPrincipal.GetStoreCtxToUse().UnexpirePassword(this.owningPrincipal);
            }
        }
        

        //
        // Internal constructor
        //
        // <SecurityKernel Critical="True" Ring="1">
        // <ReferencesCritical Name="Field: owningPrincipal" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        internal PasswordInfo(AuthenticablePrincipal principal)
        {
            this.owningPrincipal = principal;
        }

        //
        // Private implementation
        //
        AuthenticablePrincipal owningPrincipal;
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
            if ( value != null  )
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "LoadValueIntoProperty: name=" + propertyName + " value=" + value.ToString());
            }
            
            switch (propertyName)
            {
                case(PropertyNames.PwdInfoLastPasswordSet):
                    this.lastPasswordSet = (Nullable<DateTime>) value;
                    lastPasswordSetLoaded = LoadState.Loaded;
                    break;

                case(PropertyNames.PwdInfoLastBadPasswordAttempt):
                    this.lastBadPasswordAttempt = (Nullable<DateTime>)value;
                    lastBadPasswordAttemptLoaded = LoadState.Loaded;                    
                    break;

                case(PropertyNames.PwdInfoPasswordNotRequired):
                    this.passwordNotRequired = (bool) value;
                    this.passwordNotRequiredChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.PwdInfoPasswordNeverExpires):
                    this.passwordNeverExpires = (bool) value;
                    this.passwordNeverExpiresChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.PwdInfoCannotChangePassword):
                    this.cannotChangePassword = (bool) value;
                    this.cannotChangePasswordChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.PwdInfoAllowReversiblePasswordEncryption):
                    this.allowReversiblePasswordEncryption = (bool) value;
                    this.allowReversiblePasswordEncryptionChanged = LoadState.Loaded;
                    break;

                default:
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, "PasswordInfo.LoadValueIntoProperty: fell off end looking for {0}", propertyName));
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
                case(PropertyNames.PwdInfoPasswordNotRequired):
                    return this.passwordNotRequiredChanged == LoadState.Changed;

                case(PropertyNames.PwdInfoPasswordNeverExpires):
                    return this.passwordNeverExpiresChanged == LoadState.Changed;

                case(PropertyNames.PwdInfoCannotChangePassword):
                    return this.cannotChangePasswordChanged == LoadState.Changed;

                case(PropertyNames.PwdInfoAllowReversiblePasswordEncryption):
                    return this.allowReversiblePasswordEncryptionChanged == LoadState.Changed;

                case(PropertyNames.PwdInfoPassword):
                    return (this.storedNewPassword != null);

                case(PropertyNames.PwdInfoExpireImmediately):
                    return (this.expirePasswordImmediately != false);

                default:
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, "PasswordInfo.GetChangeStatusForProperty: fell off end looking for {0}", propertyName));
                    return false;
            }
        }

        // Given a property name, returns the current value for the property.
        internal object GetValueForProperty(string propertyName)
        {        
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "GetValueForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case(PropertyNames.PwdInfoPasswordNotRequired):
                    return this.passwordNotRequired;

                case(PropertyNames.PwdInfoPasswordNeverExpires):
                    return this.passwordNeverExpires;

                case(PropertyNames.PwdInfoCannotChangePassword):
                    return this.cannotChangePassword;

                case(PropertyNames.PwdInfoAllowReversiblePasswordEncryption):
                    return this.allowReversiblePasswordEncryption;

                case(PropertyNames.PwdInfoPassword):
                    return this.storedNewPassword;

                case(PropertyNames.PwdInfoExpireImmediately):
                    return this.expirePasswordImmediately;

                default:
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, "PasswordInfo.GetValueForProperty: fell off end looking for {0}", propertyName));
                    return null;        
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PasswordInfo", "ResetAllChangeStatus");
        
            this.passwordNotRequiredChanged =  ( this.passwordNotRequiredChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.passwordNeverExpiresChanged =   ( this.passwordNeverExpiresChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.cannotChangePasswordChanged = ( this.cannotChangePasswordChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.allowReversiblePasswordEncryptionChanged = ( this.allowReversiblePasswordEncryptionChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;

            this.storedNewPassword = null;
            this.expirePasswordImmediately = false;
        }        
    }
}
