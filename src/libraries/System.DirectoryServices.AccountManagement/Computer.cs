/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    Computer.cs

Abstract:

    Implements the Computer class.

History:

    04-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Permissions;

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
    public class ComputerPrincipal : AuthenticablePrincipal
    {

        //
        // Public constructors
        //
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
        public ComputerPrincipal(PrincipalContext context) : base(context)
        {
            if (context == null)
                throw new ArgumentException(StringResources.NullArguments);
            
            if ( Context.ContextType == ContextType.ApplicationDirectory && this.GetType() == typeof(ComputerPrincipal))
                throw new InvalidOperationException(StringResources.ComputerInvalidForAppDirectoryStore);                    

            this.ContextRaw = context;
            this.unpersisted = true;
        }

        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
        public ComputerPrincipal(PrincipalContext context, string samAccountName, string password, bool enabled) : this(context)
        {
            if (samAccountName == null || password == null )
                throw new ArgumentException(StringResources.NullArguments);
            
            if ( Context.ContextType == ContextType.ApplicationDirectory && this.GetType() == typeof(ComputerPrincipal))
                throw new InvalidOperationException(StringResources.ComputerInvalidForAppDirectoryStore);                    
            
            if ( Context.ContextType != ContextType.ApplicationDirectory)
                this.SamAccountName = samAccountName;
            
            this.Name = samAccountName;
            this.SetPassword(password);
            this.Enabled = enabled;
        }


        //
        // Public properties
        //

        // ServicePrincipalNames
        PrincipalValueCollection<string> servicePrincipalNames = new PrincipalValueCollection<string>();
        LoadState servicePrincipalNamesLoaded = LoadState.NotSet;
        
        public PrincipalValueCollection<string> ServicePrincipalNames
        {
            get
            {
                return HandleGet<PrincipalValueCollection<string>>(ref this.servicePrincipalNames, PropertyNames.ComputerServicePrincipalNames, ref servicePrincipalNamesLoaded);
            }
        }

        //
        // Public methods
        //
        public static new PrincipalSearchResult<ComputerPrincipal> FindByLockoutTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByLockoutTime<ComputerPrincipal>(context, time, type);
        }
                
        public static new PrincipalSearchResult<ComputerPrincipal> FindByLogonTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByLogonTime<ComputerPrincipal>(context, time, type);
        }
        
        public static new PrincipalSearchResult<ComputerPrincipal> FindByExpirationTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByExpirationTime<ComputerPrincipal>(context, time, type);
        }

        public static new PrincipalSearchResult<ComputerPrincipal> FindByBadPasswordAttempt(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByBadPasswordAttempt<ComputerPrincipal>(context, time, type);
        }

        public static new PrincipalSearchResult<ComputerPrincipal> FindByPasswordSetTime(PrincipalContext context, DateTime time, MatchType type)
        {
            return FindByPasswordSetTime<ComputerPrincipal>(context, time, type);
        }
        
        public static new ComputerPrincipal FindByIdentity(PrincipalContext context, string identityValue)
        {
            return (ComputerPrincipal) FindByIdentityWithType(context, typeof(ComputerPrincipal), identityValue);
        }
        
        public static new ComputerPrincipal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            return (ComputerPrincipal) FindByIdentityWithType(context, typeof(ComputerPrincipal), identityType, identityValue);
        }
        

        //
        // Internal "constructor": Used for constructing Computer returned by a query
        //
        static internal ComputerPrincipal MakeComputer(PrincipalContext ctx)
        {
            ComputerPrincipal computer = new ComputerPrincipal(ctx);
            computer.unpersisted = false;

            return computer;
        }

        
        //
        // Load/Store implementation
        //

        //
        // Loading with query results
        //
        internal override void LoadValueIntoProperty(string propertyName, object value)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Computer", "LoadValueIntoProperty: name=" + propertyName + " value=" + (value == null ? "null" : value.ToString()));
            
            switch(propertyName)
            {
                case(PropertyNames.ComputerServicePrincipalNames):
                    this.servicePrincipalNames.Load((List<string>)value);
                    this.servicePrincipalNamesLoaded = LoadState.Loaded;
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
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Computer", "GetChangeStatusForProperty: name=" + propertyName);
        
            switch(propertyName)
            {
                case(PropertyNames.ComputerServicePrincipalNames):
                    return this.servicePrincipalNames.Changed;

                default:
                    return base.GetChangeStatusForProperty(propertyName);
            }

        }

        // Given a property name, returns the current value for the property.
        internal override object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Computer", "GetValueForProperty: name=" + propertyName);
        
            switch(propertyName)
            {
                case(PropertyNames.ComputerServicePrincipalNames):
                    return this.servicePrincipalNames;
                    
                default:
                    return base.GetValueForProperty(propertyName);
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal override void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Computer", "ResetAllChangeStatus");
        
            this.servicePrincipalNames.ResetTracking();
            
            base.ResetAllChangeStatus();
        }
    }

}
