/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    AdvancedFilters.cs

Abstract:

    Read Only Filter holder

History:

    TQuerec Created

--*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.Security.Permissions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
    public class AdvancedFilters
    {
        internal protected  AdvancedFilters(Principal p)
        {         
            this.p = p;
        }

        bool badPasswordAttemptChanged = false;
        QbeMatchType badPasswordAttemptVal = null;
        Principal p;

        public void LastBadPasswordAttempt(DateTime lastAttempt, MatchType match) 
        {
        
            if ( lastAttempt == null )
            {
                expirationTimeChanged = false;
                expirationTimeVal = null;
            }
            else
            {

                if (null == badPasswordAttemptVal)
                    badPasswordAttemptVal = new QbeMatchType(lastAttempt, match);
                else
                {
                    badPasswordAttemptVal.Match = match;
                    badPasswordAttemptVal.Value = lastAttempt;
                }
                badPasswordAttemptChanged = true;                   
            }        
        }

        bool expirationTimeChanged = false;
        QbeMatchType expirationTimeVal = null;
        
        public void AccountExpirationDate(DateTime expirationTime, MatchType match) 
        {
            if ( expirationTime == null )
            {
                expirationTimeChanged = false;
                expirationTimeVal = null;
            }
            else
            {

                if (null == expirationTimeVal)
                    expirationTimeVal = new QbeMatchType(expirationTime, match);
                else
                {
                    expirationTimeVal.Match = match;
                    expirationTimeVal.Value = expirationTime;
                }
                expirationTimeChanged = true;                   
            }        
        }

        bool lockoutTimeChanged = false;
        QbeMatchType lockoutTimeVal = null;
        
        public void AccountLockoutTime(DateTime lockoutTime, MatchType match) 
        {
            
            if ( lockoutTime == null )
            {
                lockoutTimeChanged = false;
                lockoutTimeVal = null;
            }
            else
            {
                if (null == lockoutTimeVal)
                    lockoutTimeVal = new QbeMatchType(lockoutTime, match);
                else
                {
                    lockoutTimeVal.Match = match;
                    lockoutTimeVal.Value = lockoutTime;
                }
                lockoutTimeChanged = true;                
            }           
        }

        bool badLogonCountChanged = false;
        QbeMatchType badLogonCountVal = null;
        
        public void BadLogonCount(int badLogonCount, MatchType match) 
        {
            if (null == badLogonCountVal) {
                badLogonCountVal = new QbeMatchType(badLogonCount, match);
            }
            else
            {
                badLogonCountVal.Match = match;
                badLogonCountVal.Value = badLogonCount;
            }
            badLogonCountChanged = true;                   
        }

        bool logonTimeChanged = false;
        QbeMatchType logonTimeVal = null;
        
        public void LastLogonTime(DateTime logonTime, MatchType match) 
        {
            if ( logonTime == null )
            {
                logonTimeChanged = false;
                logonTimeVal = null;
            }
            else
            {
                if (null == logonTimeVal)
                    logonTimeVal = new QbeMatchType(logonTime, match);
                else
                {
                    logonTimeVal.Match = match;
                    logonTimeVal.Value = logonTime;
                }
                 logonTimeChanged = true;
            }           
        }

        bool passwordSetTimeChanged = false;
        QbeMatchType passwordSetTimeVal = null;
        
        public void LastPasswordSetTime(DateTime passwordSetTime, MatchType match) 
        {
            if ( passwordSetTime == null )
            {
                passwordSetTimeChanged = false;
                passwordSetTimeVal = null;
            }
            else
            {
                if (null == passwordSetTimeVal)
                    passwordSetTimeVal = new QbeMatchType(passwordSetTime, match);
                else
                {
                    passwordSetTimeVal.Match = match;
                    passwordSetTimeVal.Value = passwordSetTime;
                }
                passwordSetTimeChanged = true;

            }           
        }


        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Principal.AdvancedFilterSet(System.String,System.Object,System.Type,System.DirectoryServices.AccountManagement.MatchType):System.Void" />
        // <ReferencesCritical Name="Method: Principal.AdvancedFilterSet(System.String,System.Object,System.Type,System.DirectoryServices.AccountManagement.MatchType):System.Void" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        protected void AdvancedFilterSet(string attribute, object  value, Type objectType, MatchType mt)
        {
            p.AdvancedFilterSet(attribute, value, objectType, mt);
        }

        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        internal bool? GetChangeStatusForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AdvancedFilters", "GetChangeStatusForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case PropertyNames.PwdInfoLastBadPasswordAttempt:
                    return this.badPasswordAttemptChanged;

                case PropertyNames.AcctInfoExpiredAccount:
                    return this.expirationTimeChanged;

                case PropertyNames.AcctInfoBadLogonCount:
                    return this.badLogonCountChanged;
                    
                case PropertyNames.AcctInfoLastLogon:
                    return this.logonTimeChanged;

                case PropertyNames.AcctInfoAcctLockoutTime:
                    return this.lockoutTimeChanged;                    

                case PropertyNames.PwdInfoLastPasswordSet:
                    return this.passwordSetTimeChanged;
                    
                default:
                    return null;
            }
        }

        // Given a property name, returns the current value for the property.
        // Generally, this method is called only if GetChangeStatusForProperty indicates there are changes on the
        // property specified.
        //
        // If the property is a scalar property, the return value is an object of the property type.
        // If the property is an IdentityClaimCollection property, the return value is the IdentityClaimCollection
        // itself.
        // If the property is a ValueCollection<T>, the return value is the ValueCollection<T> itself.
        // If the property is a X509Certificate2Collection, the return value is the X509Certificate2Collection itself.
        // If the property is a PrincipalCollection, the return value is the PrincipalCollection itself.
        //[StrongNameIdentityPermission(SecurityAction.InheritanceDemand,  PublicKey = Microsoft.Internal.BuildInfo.WINDOWS_PUBLIC_KEY_STRING)]        
        internal object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AdvancedFilters", "GetValueForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case PropertyNames.PwdInfoLastBadPasswordAttempt:
                    return this.badPasswordAttemptVal;

                case PropertyNames.AcctInfoExpiredAccount:
                    return this.expirationTimeVal;

                case PropertyNames.AcctInfoBadLogonCount:
                    return this.badLogonCountVal;
                    
                case PropertyNames.AcctInfoLastLogon:
                    return this.logonTimeVal;

                case PropertyNames.AcctInfoAcctLockoutTime:
                    return this.lockoutTimeVal;                    

                case PropertyNames.PwdInfoLastPasswordSet:
                    return this.passwordSetTimeVal;
                    
                default:
                    return null;
            }
        }
        
        // Reset all change-tracking status for all properties on the object to "unchanged".
        // This is used by StoreCtx.Insert() and StoreCtx.Update() to reset the change-tracking after they
        // have persisted all current changes to the store.
        //[StrongNameIdentityPermission(SecurityAction.InheritanceDemand,  PublicKey = Microsoft.Internal.BuildInfo.WINDOWS_PUBLIC_KEY_STRING)]        
        internal virtual void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "ResetAllChangeStatus");
           
            this.badPasswordAttemptChanged = false;
            this.expirationTimeChanged = false;
            this.logonTimeChanged = false;
            this.lockoutTimeChanged = false;                    
            this.passwordSetTimeChanged = false;
        }        
    }
}
