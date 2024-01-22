// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    public class AdvancedFilters
    {
        protected internal AdvancedFilters(Principal p)
        {
            _p = p;
        }

        private bool _badPasswordAttemptChanged;
        private QbeMatchType _badPasswordAttemptVal;
        private readonly Principal _p;

        public void LastBadPasswordAttempt(DateTime lastAttempt, MatchType match)
        {
            if (null == _badPasswordAttemptVal)
            {
                _badPasswordAttemptVal = new QbeMatchType(lastAttempt, match);
            }
            else
            {
                _badPasswordAttemptVal.Match = match;
                _badPasswordAttemptVal.Value = lastAttempt;
            }
            _badPasswordAttemptChanged = true;
        }

        private bool _expirationTimeChanged;
        private QbeMatchType _expirationTimeVal;

        public void AccountExpirationDate(DateTime expirationTime, MatchType match)
        {
            if (null == _expirationTimeVal)
            {
                _expirationTimeVal = new QbeMatchType(expirationTime, match);
            }
            else
            {
                _expirationTimeVal.Match = match;
                _expirationTimeVal.Value = expirationTime;
            }
            _expirationTimeChanged = true;
        }

        private bool _lockoutTimeChanged;
        private QbeMatchType _lockoutTimeVal;

        public void AccountLockoutTime(DateTime lockoutTime, MatchType match)
        {
            if (null == _lockoutTimeVal)
            {
                _lockoutTimeVal = new QbeMatchType(lockoutTime, match);
            }
            else
            {
                _lockoutTimeVal.Match = match;
                _lockoutTimeVal.Value = lockoutTime;
            }
            _lockoutTimeChanged = true;
        }

        private bool _badLogonCountChanged;
        private QbeMatchType _badLogonCountVal;

        public void BadLogonCount(int badLogonCount, MatchType match)
        {
            if (null == _badLogonCountVal)
            {
                _badLogonCountVal = new QbeMatchType(badLogonCount, match);
            }
            else
            {
                _badLogonCountVal.Match = match;
                _badLogonCountVal.Value = badLogonCount;
            }
            _badLogonCountChanged = true;
        }

        private bool _logonTimeChanged;
        private QbeMatchType _logonTimeVal;

        public void LastLogonTime(DateTime logonTime, MatchType match)
        {
            if (null == _logonTimeVal)
            {
                _logonTimeVal = new QbeMatchType(logonTime, match);
            }
            else
            {
                _logonTimeVal.Match = match;
                _logonTimeVal.Value = logonTime;
            }
            _logonTimeChanged = true;
        }

        private bool _passwordSetTimeChanged;
        private QbeMatchType _passwordSetTimeVal;

        public void LastPasswordSetTime(DateTime passwordSetTime, MatchType match)
        {
            if (null == _passwordSetTimeVal)
            {
                _passwordSetTimeVal = new QbeMatchType(passwordSetTime, match);
            }
            else
            {
                _passwordSetTimeVal.Match = match;
                _passwordSetTimeVal.Value = passwordSetTime;
            }
            _passwordSetTimeChanged = true;
        }

        protected void AdvancedFilterSet(string attribute, object value, Type objectType, MatchType mt)
        {
            _p.AdvancedFilterSet(attribute, value, objectType, mt);
        }

        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        internal bool? GetChangeStatusForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AdvancedFilters", "GetChangeStatusForProperty: name=" + propertyName);

            return propertyName switch
            {
                PropertyNames.PwdInfoLastBadPasswordAttempt => _badPasswordAttemptChanged,
                PropertyNames.AcctInfoExpiredAccount => _expirationTimeChanged,
                PropertyNames.AcctInfoBadLogonCount => _badLogonCountChanged,
                PropertyNames.AcctInfoLastLogon => _logonTimeChanged,
                PropertyNames.AcctInfoAcctLockoutTime => _lockoutTimeChanged,
                PropertyNames.PwdInfoLastPasswordSet => _passwordSetTimeChanged,
                _ => (bool?)null,
            };
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
        internal object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AdvancedFilters", "GetValueForProperty: name=" + propertyName);

            return propertyName switch
            {
                PropertyNames.PwdInfoLastBadPasswordAttempt => _badPasswordAttemptVal,
                PropertyNames.AcctInfoExpiredAccount => _expirationTimeVal,
                PropertyNames.AcctInfoBadLogonCount => _badLogonCountVal,
                PropertyNames.AcctInfoLastLogon => _logonTimeVal,
                PropertyNames.AcctInfoAcctLockoutTime => _lockoutTimeVal,
                PropertyNames.PwdInfoLastPasswordSet => _passwordSetTimeVal,
                _ => null,
            };
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        // This is used by StoreCtx.Insert() and StoreCtx.Update() to reset the change-tracking after they
        // have persisted all current changes to the store.
        internal virtual void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "ResetAllChangeStatus");

            _badPasswordAttemptChanged = false;
            _expirationTimeChanged = false;
            _logonTimeChanged = false;
            _lockoutTimeChanged = false;
            _passwordSetTimeChanged = false;
        }
    }
}
