/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    AccountInfo.cs

Abstract:

    Implements the AccountInfo class.

History:

    17-May-2004    MattRim     Created

--*/


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
    class AccountInfo
#endif
    {
    
        //
        // Properties exposed to the public through AuthenticablePrincipal
        //

        // AccountLockoutTime
        Nullable<DateTime> accountLockoutTime = null;
        LoadState accountLockoutTimeLoaded = LoadState.NotSet;
        
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
                return this.owningPrincipal.HandleGet<Nullable<DateTime>>(ref this.accountLockoutTime, PropertyNames.AcctInfoAcctLockoutTime, ref accountLockoutTimeLoaded);
            }
        }

        // LastLogon
        Nullable<DateTime> lastLogon = null;
        LoadState lastLogonLoaded = LoadState.NotSet;
        
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
                return this.owningPrincipal.HandleGet<Nullable<DateTime>>(ref this.lastLogon, PropertyNames.AcctInfoLastLogon, ref lastLogonLoaded);
            }
        }


        // PermittedWorkstations
        PrincipalValueCollection<string> permittedWorkstations = new PrincipalValueCollection<string>();
        LoadState permittedWorkstationsLoaded = LoadState.NotSet;        

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
                if (!this.owningPrincipal.GetStoreCtxToUse().IsValidProperty(this.owningPrincipal, PropertyNames.AcctInfoPermittedWorkstations))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
         
                return this.owningPrincipal.HandleGet<PrincipalValueCollection<string>>(ref this.permittedWorkstations, PropertyNames.AcctInfoPermittedWorkstations, ref permittedWorkstationsLoaded);
            }
        }

        // PermittedLogonTimes
        //  We have to handle the change-tracking for this differently than for the other properties, because
        //  a byte[] is mutable.  After calling the get accessor, the app can change the permittedLogonTimes,
        //  without needing to ever call the set accessor.  Therefore, rather than a simple "changed" flag set
        //  by the set accessor, we need to track the original value of the property, and flag it as changed
        //  if current value != original value.
        byte[] permittedLogonTimes = null;
        byte[] permittedLogonTimesOriginal = null;
        LoadState permittedLogonTimesLoaded = LoadState.NotSet;   
        
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
                return this.owningPrincipal.HandleGet<byte[]>(ref this.permittedLogonTimes, PropertyNames.AcctInfoPermittedLogonTimes, ref permittedLogonTimesLoaded);                                
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

                if ( !this.owningPrincipal.GetStoreCtxToUse().IsValidProperty( this.owningPrincipal, PropertyNames.AcctInfoPermittedLogonTimes))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);

                // If we get to this point we know that the value of the property has changed and we should not load it from the store.
                // If value is retrived the state is set to loaded.  Even if user modifies the reference we will
                // not overwrite it because we mark it as loaded.
                // If the user sets it before reading it we mark it as changed.  When the users accesses it we just return the current
                // value.  All change tracking to the store is done off of an actual object comparison because users can change the value
                // either through property set or modifying the reference returned.
                permittedLogonTimesLoaded = LoadState.Changed;
                
                this.permittedLogonTimes = value;
            }
        }
        

        // AccountExpirationDate
        Nullable<DateTime> expirationDate = null;
        LoadState expirationDateChanged = LoadState.NotSet;

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
                return this.owningPrincipal.HandleGet<Nullable<DateTime>>(ref this.expirationDate, PropertyNames.AcctInfoExpirationDate, ref expirationDateChanged);                
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
                if ( !this.owningPrincipal.GetStoreCtxToUse().IsValidProperty( this.owningPrincipal, PropertyNames.AcctInfoExpirationDate))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                this.owningPrincipal.HandleSet<Nullable<DateTime>>(ref this.expirationDate, value, ref this.expirationDateChanged,
                                               PropertyNames.AcctInfoExpirationDate);
            }
        }

        // SmartcardLogonRequired
        bool smartcardLogonRequired = false;
        LoadState smartcardLogonRequiredChanged = LoadState.NotSet;

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
                return this.owningPrincipal.HandleGet<bool>(ref this.smartcardLogonRequired, PropertyNames.AcctInfoSmartcardRequired, ref smartcardLogonRequiredChanged);                
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
                if ( !this.owningPrincipal.GetStoreCtxToUse().IsValidProperty( this.owningPrincipal, PropertyNames.AcctInfoSmartcardRequired))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                this.owningPrincipal.HandleSet<bool>(ref this.smartcardLogonRequired, value, ref this.smartcardLogonRequiredChanged,
                                 PropertyNames.AcctInfoSmartcardRequired);                
            }
        }

        // DelegationPermitted
        bool delegationPermitted = false;
        LoadState delegationPermittedChanged = LoadState.NotSet;

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
                return this.owningPrincipal.HandleGet<bool>(ref this.delegationPermitted, PropertyNames.AcctInfoDelegationPermitted, ref delegationPermittedChanged);                                
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
                if ( !this.owningPrincipal.GetStoreCtxToUse().IsValidProperty( this.owningPrincipal, PropertyNames.AcctInfoDelegationPermitted))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                this.owningPrincipal.HandleSet<bool>(ref this.delegationPermitted, value, ref this.delegationPermittedChanged,
                                 PropertyNames.AcctInfoDelegationPermitted);                                
            }
        }

        // BadLogonCount
        int badLogonCount = 0;
        LoadState badLogonCountChanged = LoadState.NotSet;

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
                return this.owningPrincipal.HandleGet<int>(ref this.badLogonCount, PropertyNames.AcctInfoBadLogonCount, ref badLogonCountChanged);                                                
            }
        }

        
        // HomeDirectory
        string homeDirectory = null;
        LoadState  homeDirectoryChanged = LoadState.NotSet;

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
                return this.owningPrincipal.HandleGet<string>(ref this.homeDirectory, PropertyNames.AcctInfoHomeDirectory, ref homeDirectoryChanged);                                                                
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
                if ( !this.owningPrincipal.GetStoreCtxToUse().IsValidProperty( this.owningPrincipal, PropertyNames.AcctInfoHomeDirectory))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                this.owningPrincipal.HandleSet<string>(ref this.homeDirectory, value, ref this.homeDirectoryChanged,
                                   PropertyNames.AcctInfoHomeDirectory);                                                
            }
        }

        // HomeDrive
        string homeDrive = null;
        LoadState homeDriveChanged = LoadState.NotSet;

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
                return this.owningPrincipal.HandleGet<string>(ref this.homeDrive, PropertyNames.AcctInfoHomeDrive, ref homeDriveChanged);                                                                                
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
                if ( !this.owningPrincipal.GetStoreCtxToUse().IsValidProperty( this.owningPrincipal, PropertyNames.AcctInfoHomeDrive))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                this.owningPrincipal.HandleSet<string>(ref this.homeDrive, value, ref this.homeDriveChanged,
                                   PropertyNames.AcctInfoHomeDrive);                                                                
            }
        }

        // ScriptPath
        string scriptPath = null;
        LoadState scriptPathChanged = LoadState.NotSet;

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
                return this.owningPrincipal.HandleGet<string>(ref this.scriptPath, PropertyNames.AcctInfoScriptPath, ref scriptPathChanged);                
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
                if ( !this.owningPrincipal.GetStoreCtxToUse().IsValidProperty( this.owningPrincipal, PropertyNames.AcctInfoScriptPath))
                    throw new InvalidOperationException(StringResources.InvalidPropertyForStore);
            
                this.owningPrincipal.HandleSet<string>(ref this.scriptPath, value, ref this.scriptPathChanged,
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
            if (!this.owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info,  "AccountInfo", "IsAccountLockedOut: sending lockout query");
            
                Debug.Assert(this.owningPrincipal.Context != null);

                return this.owningPrincipal.GetStoreCtxToUse().IsLockedOut(this.owningPrincipal);
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
            if (!this.owningPrincipal.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "AccountInfo", "UnlockAccount: sending unlock request");
            
                Debug.Assert(this.owningPrincipal.Context != null);

                this.owningPrincipal.GetStoreCtxToUse().UnlockAccount(this.owningPrincipal);
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
            this.owningPrincipal = principal;
        }

        //
        // Private implementation
        //
        AuthenticablePrincipal owningPrincipal;

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
            
            switch(propertyName)
            {
                case(PropertyNames.AcctInfoAcctLockoutTime):
                    this.accountLockoutTime = (Nullable<DateTime>) value;
                    this.accountLockoutTimeLoaded = LoadState.Loaded;
                    break;

                case(PropertyNames.AcctInfoLastLogon):
                    this.lastLogon = (Nullable<DateTime>) value;
                    this.lastLogonLoaded= LoadState.Loaded;
                    break;
            
                case(PropertyNames.AcctInfoPermittedWorkstations):
                    this.permittedWorkstations.Load((List<string>)value);
                    this.permittedWorkstationsLoaded = LoadState.Loaded;
                    break;

                case(PropertyNames.AcctInfoPermittedLogonTimes):
                    this.permittedLogonTimes = (byte[]) value;
                    this.permittedLogonTimesOriginal =  (byte[]) ((byte[]) value).Clone();
                    this.permittedLogonTimesLoaded = LoadState.Loaded;
                    break;

                case(PropertyNames.AcctInfoExpirationDate):
                    this.expirationDate = (Nullable<DateTime>) value;
                    this.expirationDateChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.AcctInfoSmartcardRequired):
                    this.smartcardLogonRequired = (bool) value;
                    this.smartcardLogonRequiredChanged = LoadState.Loaded;
                    break;
                    
                case(PropertyNames.AcctInfoDelegationPermitted):
                    this.delegationPermitted = (bool) value;
                    this.delegationPermittedChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.AcctInfoBadLogonCount):
                    this.badLogonCount = (int) value;
                    this.badLogonCountChanged = LoadState.Loaded;
                    break;
                    
                case(PropertyNames.AcctInfoHomeDirectory):
                    this.homeDirectory = (string) value;
                    this.homeDirectoryChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.AcctInfoHomeDrive):
                    this.homeDrive = (string) value;
                    this.homeDriveChanged = LoadState.Loaded;
                    break;

                case(PropertyNames.AcctInfoScriptPath):
                    this.scriptPath = (string) value;
                    this.scriptPathChanged = LoadState.Loaded;
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
                case(PropertyNames.AcctInfoPermittedWorkstations):
                    return this.permittedWorkstations.Changed;

                case(PropertyNames.AcctInfoPermittedLogonTimes):
                    // If they're equal, they have _not_ changed
                    if ((this.permittedLogonTimes == null) && (this.permittedLogonTimesOriginal == null))
                        return false;

                    if ((this.permittedLogonTimes == null) || (this.permittedLogonTimesOriginal == null))
                        return true;
                    
                    return !Utils.AreBytesEqual(this.permittedLogonTimes, this.permittedLogonTimesOriginal);

                case(PropertyNames.AcctInfoExpirationDate):
                    return this.expirationDateChanged == LoadState.Changed;                    

                case(PropertyNames.AcctInfoSmartcardRequired):
                    return this.smartcardLogonRequiredChanged == LoadState.Changed;                    
                    
                case(PropertyNames.AcctInfoDelegationPermitted):
                    return this.delegationPermittedChanged == LoadState.Changed;                    
                                        
                case(PropertyNames.AcctInfoHomeDirectory):
                    return this.homeDirectoryChanged == LoadState.Changed;                    
                    
                case(PropertyNames.AcctInfoHomeDrive):
                    return this.homeDriveChanged == LoadState.Changed;                    
                    
                case(PropertyNames.AcctInfoScriptPath):
                    return this.scriptPathChanged == LoadState.Changed;                    
                    
                default:
                    Debug.Fail(String.Format(CultureInfo.CurrentCulture, "AccountInfo.GetChangeStatusForProperty: fell off end looking for {0}", propertyName));
                    return false ;
            }
        }

        // Given a property name, returns the current value for the property.
        internal object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AccountInfo", "GetValueForProperty: name=" + propertyName);
        
            switch (propertyName)
            {
                case(PropertyNames.AcctInfoPermittedWorkstations):
                    return this.permittedWorkstations;

                case(PropertyNames.AcctInfoPermittedLogonTimes):
                    return this.permittedLogonTimes;

                case(PropertyNames.AcctInfoExpirationDate):
                    return this.expirationDate;

                case(PropertyNames.AcctInfoSmartcardRequired):
                    return this.smartcardLogonRequired;
                    
                case(PropertyNames.AcctInfoDelegationPermitted):
                    return this.delegationPermitted;
                    
                case(PropertyNames.AcctInfoHomeDirectory):
                    return this.homeDirectory;

                case(PropertyNames.AcctInfoHomeDrive):
                    return this.homeDrive;

                case(PropertyNames.AcctInfoScriptPath):
                    return this.scriptPath;

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
        
            this.permittedWorkstations.ResetTracking();

            this.permittedLogonTimesOriginal = (this.permittedLogonTimes != null) ?
                                                            (byte[]) this.permittedLogonTimes.Clone() :
                                                            null;
            this.expirationDateChanged = ( this.expirationDateChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.smartcardLogonRequiredChanged = ( this.smartcardLogonRequiredChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.delegationPermittedChanged = ( this.delegationPermittedChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.homeDirectoryChanged = ( this.homeDirectoryChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.homeDriveChanged = ( this.homeDriveChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.scriptPathChanged = ( this.scriptPathChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
        }        

    }
}
