// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;

namespace System.DirectoryServices.AccountManagement
{
    public class AdvancedFilters
    {
        internal protected AdvancedFilters(Principal p)
        {
            _p = p;
        }

        private bool _badPasswordAttemptChanged = false;
        private QbeMatchType _badPasswordAttemptVal = null;
        private Principal _p;

        public void LastBadPasswordAttempt(DateTime lastAttempt, MatchType match)
        {
            if (lastAttempt == null)
            {
                _expirationTimeChanged = false;
                _expirationTimeVal = null;
            }
            else
            {
                if (null == _badPasswordAttemptVal)
                    _badPasswordAttemptVal = new QbeMatchType(lastAttempt, match);
                else
                {
                    _badPasswordAttemptVal.Match = match;
                    _badPasswordAttemptVal.Value = lastAttempt;
                }
                _badPasswordAttemptChanged = true;
            }
        }

        private bool _expirationTimeChanged = false;
        private QbeMatchType _expirationTimeVal = null;

        public void AccountExpirationDate(DateTime expirationTime, MatchType match)
        {
            if (expirationTime == null)
            {
                _expirationTimeChanged = false;
                _expirationTimeVal = null;
            }
            else
            {
                if (null == _expirationTimeVal)
                    _expirationTimeVal = new QbeMatchType(expirationTime, match);
                else
                {
                    _expirationTimeVal.Match = match;
                    _expirationTimeVal.Value = expirationTime;
                }
                _expirationTimeChanged = true;
            }
        }

        private bool _lockoutTimeChanged = false;
        private QbeMatchType _lockoutTimeVal = null;

        public void AccountLockoutTime(DateTime lockoutTime, MatchType match)
        {
            if (lockoutTime == null)
            {
                _lockoutTimeChanged = false;
                _lockoutTimeVal = null;
            }
            else
            {
                if (null == _lockoutTimeVal)
                    _lockoutTimeVal = new QbeMatchType(lockoutTime, match);
                else
                {
                    _lockoutTimeVal.Match = match;
                    _lockoutTimeVal.Value = lockoutTime;
                }
                _lockoutTimeChanged = true;
            }
        }

        private bool _badLogonCountChanged = false;
        private QbeMatchType _badLogonCountVal = null;

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

        private bool _logonTimeChanged = false;
        private QbeMatchType _logonTimeVal = null;

        public void LastLogonTime(DateTime logonTime, MatchType match)
        {
            if (logonTime == null)
            {
                _logonTimeChanged = false;
                _logonTimeVal = null;
            }
            else
            {
                if (null == _logonTimeVal)
                    _logonTimeVal = new QbeMatchType(logonTime, match);
                else
                {
                    _logonTimeVal.Match = match;
                    _logonTimeVal.Value = logonTime;
                }
                _logonTimeChanged = true;
            }
        }

        private bool _passwordSetTimeChanged = false;
        private QbeMatchType _passwordSetTimeVal = null;

        public void LastPasswordSetTime(DateTime passwordSetTime, MatchType match)
        {
            if (passwordSetTime == null)
            {
                _passwordSetTimeChanged = false;
                _passwordSetTimeVal = null;
            }
            else
            {
                if (null == _passwordSetTimeVal)
                    _passwordSetTimeVal = new QbeMatchType(passwordSetTime, match);
                else
                {
                    _passwordSetTimeVal.Match = match;
                    _passwordSetTimeVal.Value = passwordSetTime;
                }
                _passwordSetTimeChanged = true;
            }
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

            switch (propertyName)
            {
                case PropertyNames.PwdInfoLastBadPasswordAttempt:
                    return _badPasswordAttemptChanged;

                case PropertyNames.AcctInfoExpiredAccount:
                    return _expirationTimeChanged;

                case PropertyNames.AcctInfoBadLogonCount:
                    return _badLogonCountChanged;

                case PropertyNames.AcctInfoLastLogon:
                    return _logonTimeChanged;

                case PropertyNames.AcctInfoAcctLockoutTime:
                    return _lockoutTimeChanged;

                case PropertyNames.PwdInfoLastPasswordSet:
                    return _passwordSetTimeChanged;

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
        internal object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "AdvancedFilters", "GetValueForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case PropertyNames.PwdInfoLastBadPasswordAttempt:
                    return _badPasswordAttemptVal;

                case PropertyNames.AcctInfoExpiredAccount:
                    return _expirationTimeVal;

                case PropertyNames.AcctInfoBadLogonCount:
                    return _badLogonCountVal;

                case PropertyNames.AcctInfoLastLogon:
                    return _logonTimeVal;

                case PropertyNames.AcctInfoAcctLockoutTime:
                    return _lockoutTimeVal;

                case PropertyNames.PwdInfoLastPasswordSet:
                    return _passwordSetTimeVal;

                default:
                    return null;
            }
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
