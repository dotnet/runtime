// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryRdnPrefix("CN")]
    public class ComputerPrincipal : AuthenticablePrincipal
    {
        //
        // Public constructors
        //
        public ComputerPrincipal(PrincipalContext context) : base(context)
        {
            if (Context.ContextType == ContextType.ApplicationDirectory && this.GetType() == typeof(ComputerPrincipal))
                throw new InvalidOperationException(SR.ComputerInvalidForAppDirectoryStore);

            this.ContextRaw = context;
            this.unpersisted = true;
        }

        public ComputerPrincipal(PrincipalContext context, string samAccountName, string password, bool enabled) : this(context)
        {
            if (samAccountName == null || password == null)
                throw new ArgumentException(SR.NullArguments);

            if (Context.ContextType == ContextType.ApplicationDirectory && this.GetType() == typeof(ComputerPrincipal))
                throw new InvalidOperationException(SR.ComputerInvalidForAppDirectoryStore);

            if (Context.ContextType != ContextType.ApplicationDirectory)
                this.SamAccountName = samAccountName;

            this.Name = samAccountName;
            this.SetPassword(password);
            this.Enabled = enabled;
        }

        //
        // Public properties
        //

        // ServicePrincipalNames
        private PrincipalValueCollection<string> _servicePrincipalNames = new PrincipalValueCollection<string>();
        private LoadState _servicePrincipalNamesLoaded = LoadState.NotSet;

        public PrincipalValueCollection<string> ServicePrincipalNames
        {
            get
            {
                return HandleGet<PrincipalValueCollection<string>>(ref _servicePrincipalNames, PropertyNames.ComputerServicePrincipalNames, ref _servicePrincipalNamesLoaded);
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
            return (ComputerPrincipal)FindByIdentityWithType(context, typeof(ComputerPrincipal), identityValue);
        }

        public static new ComputerPrincipal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            return (ComputerPrincipal)FindByIdentityWithType(context, typeof(ComputerPrincipal), identityType, identityValue);
        }

        //
        // Internal "constructor": Used for constructing Computer returned by a query
        //
        internal static ComputerPrincipal MakeComputer(PrincipalContext ctx)
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

            switch (propertyName)
            {
                case (PropertyNames.ComputerServicePrincipalNames):
                    _servicePrincipalNames.Load((List<string>)value);
                    _servicePrincipalNamesLoaded = LoadState.Loaded;
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

            return propertyName switch
            {
                PropertyNames.ComputerServicePrincipalNames => _servicePrincipalNames.Changed,
                _ => base.GetChangeStatusForProperty(propertyName),
            };
        }

        // Given a property name, returns the current value for the property.
        internal override object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Computer", "GetValueForProperty: name=" + propertyName);

            return propertyName switch
            {
                PropertyNames.ComputerServicePrincipalNames => _servicePrincipalNames,
                _ => base.GetValueForProperty(propertyName),
            };
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal override void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Computer", "ResetAllChangeStatus");

            _servicePrincipalNames.ResetTracking();

            base.ResetAllChangeStatus();
        }
    }
}
