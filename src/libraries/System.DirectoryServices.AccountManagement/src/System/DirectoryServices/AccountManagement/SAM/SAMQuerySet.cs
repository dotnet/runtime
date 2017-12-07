// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.DirectoryServices;
using System.Text.RegularExpressions;

namespace System.DirectoryServices.AccountManagement
{
    internal class SAMQuerySet : ResultSet
    {
        // We will iterate over all principals under ctxBase, returning only those which are in the list of types and which
        // satisfy ALL the matching properties.
        internal SAMQuerySet(
                        List<string> schemaTypes,
                        DirectoryEntries entries,
                        DirectoryEntry ctxBase,
                        int sizeLimit,
                        SAMStoreCtx storeCtx,
                        SAMMatcher samMatcher)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "SAMQuerySet: creating for path={0}, sizelimit={1}", ctxBase.Path, sizeLimit);

            _schemaTypes = schemaTypes;
            _entries = entries;
            _sizeLimit = sizeLimit;     // -1 == no limit
            _storeCtx = storeCtx;
            _ctxBase = ctxBase;
            _matcher = samMatcher;

            _enumerator = _entries.GetEnumerator();
        }

        // Return the principal we're positioned at as a Principal object.
        // Need to use our StoreCtx's GetAsPrincipal to convert the native object to a Principal
        override internal object CurrentAsPrincipal
        {
            get
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "CurrentAsPrincipal");

                // Since this class is only used internally, none of our code should be even calling this
                // if MoveNext returned false, or before calling MoveNext.
                Debug.Assert(_endReached == false && _current != null);

                return SAMUtils.DirectoryEntryAsPrincipal(_current, _storeCtx);
            }
        }

        // Advance the enumerator to the next principal in the result set, pulling in additional pages
        // of results (or ranges of attribute values) as needed.
        // Returns true if successful, false if no more results to return.
        override internal bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "Entering MoveNext");

            Debug.Assert(_enumerator != null);

            bool needToRetry = false;
            bool f;

            // Have we exceeded the requested size limit?
            if ((_sizeLimit != -1) && (_resultsReturned >= _sizeLimit))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "SAMQuerySet",
                                        "MoveNext: exceeded sizelimit, ret={0}, limit={1}",
                                        _resultsReturned,
                                        _sizeLimit);
                _endReached = true;
            }

            // End was reached previously.  Nothing more to do.
            if (_endReached)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "MoveNext: endReached");
                return false;
            }

            // Pull the next result.  We may have to repeat this several times
            // until we find a result that matches the user's filter.
            do
            {
                f = _enumerator.MoveNext();
                needToRetry = false;

                if (f)
                {
                    DirectoryEntry entry = (DirectoryEntry)_enumerator.Current;

                    // Does it match the user's properties?
                    //
                    // We'd like to use DirectoryEntries.SchemaFilter rather than calling
                    // IsOfCorrectType here, but SchemaFilter has a bug
                    // where multiple DirectoryEntries all share the same SchemaFilter ---
                    // which would create multithreading issues for us.
                    if (IsOfCorrectType(entry) && _matcher.Matches(entry))
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "MoveNext: found a match on {0}", entry.Path);

                        // Yes.  It's our new current object
                        _current = entry;
                        _resultsReturned++;
                    }
                    else
                    {
                        // No.  Retry.
                        needToRetry = true;
                    }
                }
            }
            while (needToRetry);

            if (!f)
            {
                /*
                // One more to try: the root object
                if (IsOfCorrectType(this.ctxBase) && this.matcher.Matches(this.ctxBase))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "MoveNext: found a match on root {0}", this.ctxBase);
                
                    this.current = this.ctxBase;
                    this.resultsReturned++;
                    f = true;
                }
                else
                {
                    endReached = true;
                }
                 * */
            }

            return f;
        }

        private bool IsOfCorrectType(DirectoryEntry de)
        {
            // Is the object in question one of the desired types?

            foreach (string schemaType in _schemaTypes)
            {
                if (SAMUtils.IsOfObjectClass(de, schemaType))
                    return true;
            }

            return false;
        }

        // Resets the enumerator to before the first result in the set.  This potentially can be an expensive
        // operation, e.g., if doing a paged search, may need to re-retrieve the first page of results.
        // As a special case, if the ResultSet is already at the very beginning, this is guaranteed to be
        // a no-op.
        override internal void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "Reset");

            // if current == null, we're already at the beginning
            if (_current != null)
            {
                _endReached = false;
                _current = null;

                if (_enumerator != null)
                    _enumerator.Reset();

                _resultsReturned = 0;
            }
        }

        //
        // Private fields
        //

        private SAMStoreCtx _storeCtx;
        private DirectoryEntry _ctxBase;
        private DirectoryEntries _entries;
        private IEnumerator _enumerator = null;  // the enumerator for "entries"
        private DirectoryEntry _current = null;  // the DirectoryEntry that we're currently positioned at

        // Filter parameters
        private int _sizeLimit;  // -1 == no limit
        private List<string> _schemaTypes;
        private SAMMatcher _matcher;

        // Count of number of results returned so far
        private int _resultsReturned = 0;

        // Have we run out of entries?
        private bool _endReached = false;
    }

    internal abstract class SAMMatcher
    {
        abstract internal bool Matches(DirectoryEntry de);
    }

    //
    // The matcher routines for query-by-example support
    //

    internal class QbeMatcher : SAMMatcher
    {
        private QbeFilterDescription _propertiesToMatch;

        internal QbeMatcher(QbeFilterDescription propertiesToMatch)
        {
            _propertiesToMatch = propertiesToMatch;
        }

        //
        // Static constructor: used for initializing static tables
        //
        static QbeMatcher()
        {
            //
            // Load the filterPropertiesTable
            //
            s_filterPropertiesTable = new Hashtable();

            for (int i = 0; i < s_filterPropertiesTableRaw.GetLength(0); i++)
            {
                Type qbeType = s_filterPropertiesTableRaw[i, 0] as Type;
                string winNTPropertyName = s_filterPropertiesTableRaw[i, 1] as string;
                MatcherDelegate f = s_filterPropertiesTableRaw[i, 2] as MatcherDelegate;

                Debug.Assert(qbeType != null);
                Debug.Assert(winNTPropertyName != null);
                Debug.Assert(f != null);

                // There should only be one entry per QBE type
                Debug.Assert(s_filterPropertiesTable[qbeType] == null);

                FilterPropertyTableEntry entry = new FilterPropertyTableEntry();
                entry.winNTPropertyName = winNTPropertyName;
                entry.matcher = f;

                s_filterPropertiesTable[qbeType] = entry;
            }
        }

        internal override bool Matches(DirectoryEntry de)
        {
            // If it has no SID, it's not a security principal, and we're not interested in it.
            // (In reg-SAM, computers don't have accounts and therefore don't have SIDs, but ADSI
            // creates fake Computer objects for them.  In LSAM, computers CAN have accounts, and thus
            // SIDs).
            if (de.Properties["objectSid"] == null || de.Properties["objectSid"].Count == 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "SamMatcher: Matches: skipping no-SID {0}", de.Path);
                return false;
            }

            // Try to match each specified property in turn
            foreach (FilterBase filter in _propertiesToMatch.FiltersToApply)
            {
                FilterPropertyTableEntry entry = (FilterPropertyTableEntry)s_filterPropertiesTable[filter.GetType()];

                if (entry == null)
                {
                    // Must be a property we don't support
                    throw new NotSupportedException(
                                String.Format(
                                        CultureInfo.CurrentCulture,
                                        SR.StoreCtxUnsupportedPropertyForQuery,
                                        PropertyNamesExternal.GetExternalForm(filter.PropertyName)));
                }

                if (!entry.matcher(filter, entry.winNTPropertyName, de))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "SamMatcher: Matches: no match {0}", de.Path);
                    return false;
                }
            }

            // All tests pass --- it's a match
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "SamMatcher: Matches: match {0}", de.Path);
            return true;
        }

        // We only list properties we support filtering on in this table.  At run-time, if we detect they set a
        // property that's not listed here, we throw an exception.
        private static object[,] s_filterPropertiesTableRaw =
        {
            // QbeType                                          WinNT Property          Matcher
            {typeof(DescriptionFilter),                         "Description",              new MatcherDelegate(StringMatcher)},
            {typeof(DisplayNameFilter),                         "FullName",                 new MatcherDelegate(StringMatcher)},
            {typeof(SidFilter),                                         "objectSid",                         new MatcherDelegate(SidMatcher)},
            {typeof(SamAccountNameFilter),                       "Name",                         new MatcherDelegate(SamAccountNameMatcher)},

            {typeof(AuthPrincEnabledFilter),                    "UserFlags",                new MatcherDelegate(UserFlagsMatcher)},
            {typeof(PermittedWorkstationFilter),                "LoginWorkstations",        new MatcherDelegate(MultiStringMatcher)},
            {typeof(PermittedLogonTimesFilter),                 "LoginHours",               new MatcherDelegate(BinaryMatcher)},
            {typeof(ExpirationDateFilter),                      "AccountExpirationDate",    new MatcherDelegate(ExpirationDateMatcher)},
            {typeof(SmartcardLogonRequiredFilter),              "UserFlags",                new MatcherDelegate(UserFlagsMatcher)},
            {typeof(DelegationPermittedFilter),                 "UserFlags",                new MatcherDelegate(UserFlagsMatcher)},
            {typeof(HomeDirectoryFilter),                       "HomeDirectory",            new MatcherDelegate(StringMatcher)},
            {typeof(HomeDriveFilter),                           "HomeDirDrive",             new MatcherDelegate(StringMatcher)},
            {typeof(ScriptPathFilter),                          "LoginScript",              new MatcherDelegate(StringMatcher)},
            {typeof(PasswordNotRequiredFilter),                 "UserFlags",                new MatcherDelegate(UserFlagsMatcher)},
            {typeof(PasswordNeverExpiresFilter),                "UserFlags",                new MatcherDelegate(UserFlagsMatcher)},
            {typeof(CannotChangePasswordFilter),                "UserFlags",                new MatcherDelegate(UserFlagsMatcher)},
            {typeof(AllowReversiblePasswordEncryptionFilter),   "UserFlags",                new MatcherDelegate(UserFlagsMatcher)},
            {typeof(GroupScopeFilter),                           "groupType",                new MatcherDelegate(GroupTypeMatcher)},
            {typeof(ExpiredAccountFilter),                           "AccountExpirationDate",                new MatcherDelegate(DateTimeMatcher)},
            {typeof(LastLogonTimeFilter),                           "LastLogin",                new MatcherDelegate(DateTimeMatcher)},
            {typeof(PasswordSetTimeFilter),                           "PasswordAge",                new MatcherDelegate(DateTimeMatcher)},
            {typeof(BadLogonCountFilter),                           "BadPasswordAttempts",                new MatcherDelegate(IntMatcher)},
        };

        private static Hashtable s_filterPropertiesTable = null;

        private class FilterPropertyTableEntry
        {
            internal string winNTPropertyName;
            internal MatcherDelegate matcher;
        }

        //
        // Conversion routines
        //

        private static bool WildcardStringMatch(FilterBase filter, string wildcardFilter, string property)
        {
            // Build a Regex that matches valueToMatch, and store it on the Filter (so that we don't have
            // to have the CLR constantly reparse the regex string).
            // Ideally, we'd like to use a compiled Regex (RegexOptions.Compiled) for performance,
            // but the CLR cannot release generated MSIL.  Thus, our memory usage would grow without bound
            // each time a query was performed.

            Regex regex = filter.Extra as Regex;
            if (regex == null)
            {
                regex = new Regex(SAMUtils.PAPIQueryToRegexString(wildcardFilter), RegexOptions.Singleline);
                filter.Extra = regex;
            }

            Match match = regex.Match(property);

            return match.Success;
        }
        // returns true if specified WinNT property's value matches filter.Value
        private delegate bool MatcherDelegate(FilterBase filter, string winNTPropertyName, DirectoryEntry de);

        private static bool DateTimeMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            QbeMatchType valueToMatch = (QbeMatchType)filter.Value;

            if (null == valueToMatch.Value)
            {
                if ((de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (de.Properties[winNTPropertyName].Value == null))
                    return true;
            }
            else
            {
                Debug.Assert(valueToMatch.Value is DateTime);

                if (de.Properties.Contains(winNTPropertyName) && (de.Properties[winNTPropertyName].Value != null))
                {
                    DateTime value;

                    if (winNTPropertyName == "PasswordAge")
                    {
                        PropertyValueCollection values = de.Properties["PasswordAge"];

                        if (values.Count != 0)
                        {
                            Debug.Assert(values.Count == 1);
                            Debug.Assert(values[0] is Int32);

                            int secondsLapsed = (int)values[0];

                            value = DateTime.UtcNow - new TimeSpan(0, 0, secondsLapsed);
                        }
                        else
                        {
                            // If we don't have a passwordAge then this item will never match.
                            return false;
                        }
                    }
                    else
                    {
                        value = (DateTime)de.Properties[winNTPropertyName].Value;
                    }

                    int comparisonResult = DateTime.Compare(value, (DateTime)valueToMatch.Value);
                    bool result = true;

                    switch (valueToMatch.Match)
                    {
                        case MatchType.Equals:
                            result = comparisonResult == 0;
                            break;
                        case MatchType.NotEquals:
                            result = comparisonResult != 0;
                            break;
                        case MatchType.GreaterThan:
                            result = comparisonResult > 0;
                            break;
                        case MatchType.GreaterThanOrEquals:
                            result = comparisonResult >= 0;
                            break;
                        case MatchType.LessThan:
                            result = comparisonResult < 0;
                            break;
                        case MatchType.LessThanOrEquals:
                            result = comparisonResult <= 0;
                            break;
                        default:
                            result = false;
                            break;
                    }

                    return result;
                }
            }

            return false;
        }

        private static bool StringMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            string valueToMatch = (string)filter.Value;

            if (valueToMatch == null)
            {
                if ((de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (((string)de.Properties[winNTPropertyName].Value).Length == 0))
                    return true;
            }
            else
            {
                if (de.Properties.Contains(winNTPropertyName))
                {
                    string value = (string)de.Properties[winNTPropertyName].Value;

                    if (value != null)
                    {
                        return WildcardStringMatch(filter, valueToMatch, value);
                    }
                }
            }

            return false;
        }

        private static bool IntMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            QbeMatchType valueToMatch = (QbeMatchType)filter.Value;
            bool result = false;

            if (null == valueToMatch.Value)
            {
                if ((de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (de.Properties[winNTPropertyName].Value == null))
                    result = true;
            }
            else
            {
                if (de.Properties.Contains(winNTPropertyName))
                {
                    int value = (int)de.Properties[winNTPropertyName].Value;
                    int comparisonValue = (int)valueToMatch.Value;

                    switch (valueToMatch.Match)
                    {
                        case MatchType.Equals:
                            result = (value == comparisonValue);
                            break;
                        case MatchType.NotEquals:
                            result = (value != comparisonValue);
                            break;
                        case MatchType.GreaterThan:
                            result = (value > comparisonValue);
                            break;
                        case MatchType.GreaterThanOrEquals:
                            result = (value >= comparisonValue);
                            break;
                        case MatchType.LessThan:
                            result = (value < comparisonValue);
                            break;
                        case MatchType.LessThanOrEquals:
                            result = (value <= comparisonValue);
                            break;
                        default:
                            result = false;
                            break;
                    }
                }
            }

            return result;
        }

        private static bool SamAccountNameMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            string samToMatch = (string)filter.Value;

            int index = samToMatch.IndexOf('\\');

            if (index == samToMatch.Length - 1)
                throw new InvalidOperationException(SR.StoreCtxNT4IdentityClaimWrongForm);

            string samAccountName = (index != -1) ? samToMatch.Substring(index + 1) :    // +1 to skip the '/'
                                                     samToMatch;

            if (de.Properties["Name"].Count > 0 && de.Properties["Name"].Value != null)
            {
                return WildcardStringMatch(filter, samAccountName, (string)de.Properties["Name"].Value);
                /*
                return (String.Compare(((string)de.Properties["Name"].Value),
                                       samAccountName,
                                       true,                                // acct names are not case-sensitive
                                       CultureInfo.InvariantCulture) == 0);
                                       */
            }

            return false;
        }

        private static bool SidMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            byte[] sidToMatch = Utils.StringToByteArray((string)filter.Value);

            if (sidToMatch == null)
                throw new InvalidOperationException(SR.StoreCtxSecurityIdentityClaimBadFormat);

            if (de.Properties["objectSid"].Count > 0 && de.Properties["objectSid"].Value != null)
            {
                return Utils.AreBytesEqual(sidToMatch, (byte[])de.Properties["objectSid"].Value);
            }

            return false;
        }

        private static bool UserFlagsMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            Debug.Assert(winNTPropertyName == "UserFlags");

            bool valueToMatch = (bool)filter.Value;

            // If it doesn't contain the property, it certainly can't match the user's value
            if (!de.Properties.Contains(winNTPropertyName) || de.Properties[winNTPropertyName].Count == 0)
                return false;

            int value = (int)de.Properties[winNTPropertyName].Value;

            switch (filter.PropertyName)
            {
                // We want to return true iff both value and valueToMatch are true, or both are false
                // (i.e., NOT XOR)

                case AuthPrincEnabledFilter.PropertyNameStatic:
                    // UF_ACCOUNTDISABLE
                    // Note that the logic is inverted on this one.  We expose "Enabled",
                    // but SAM stores it as "Disabled".                    
                    return (((value & 0x0002) != 0) ^ valueToMatch);

                case SmartcardLogonRequiredFilter.PropertyNameStatic:
                    // UF_SMARTCARD_REQUIRED
                    return !(((value & 0x40000) != 0) ^ valueToMatch);

                case DelegationPermittedFilter.PropertyNameStatic:
                    // UF_NOT_DELEGATED
                    // Note that the logic is inverted on this one.  That's because we expose
                    // "delegation allowed", but AD represents it as the inverse, "delegation NOT allowed"
                    return (((value & 0x100000) != 0) ^ valueToMatch);

                case PasswordNotRequiredFilter.PropertyNameStatic:
                    // UF_PASSWD_NOTREQD
                    return !(((value & 0x0020) != 0) ^ valueToMatch);

                case PasswordNeverExpiresFilter.PropertyNameStatic:
                    // UF_DONT_EXPIRE_PASSWD
                    return !(((value & 0x10000) != 0) ^ valueToMatch);

                case CannotChangePasswordFilter.PropertyNameStatic:
                    // UF_PASSWD_CANT_CHANGE
                    return !(((value & 0x0040) != 0) ^ valueToMatch);

                case AllowReversiblePasswordEncryptionFilter.PropertyNameStatic:
                    // UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED
                    return !(((value & 0x0080) != 0) ^ valueToMatch);

                default:
                    Debug.Fail("SAMQuerySet.UserFlagsMatcher: fell off end looking for " + filter.PropertyName);
                    return false;
            }
        }

        private static bool MultiStringMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            string valueToMatch = (string)filter.Value;

            if (valueToMatch == null)
            {
                if ((de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (((string)de.Properties[winNTPropertyName].Value).Length == 0))
                    return true;
            }
            else
            {
                if (de.Properties.Contains(winNTPropertyName) && (de.Properties[winNTPropertyName].Count != 0))
                {
                    foreach (string value in de.Properties[winNTPropertyName])
                    {
                        if (value != null)
                        {
                            return WildcardStringMatch(filter, valueToMatch, value);
                        }
                    }
                }
            }

            return false;
        }

        private static bool BinaryMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            byte[] valueToMatch = (byte[])filter.Value;

            if (valueToMatch == null)
            {
                if ((de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (de.Properties[winNTPropertyName].Value == null))
                    return true;
            }
            else
            {
                if (de.Properties.Contains(winNTPropertyName))
                {
                    byte[] value = (byte[])de.Properties[winNTPropertyName].Value;

                    if ((value != null) && Utils.AreBytesEqual(value, valueToMatch))
                        return true;
                }
            }

            return false;
        }

        private static bool ExpirationDateMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            Debug.Assert(filter is ExpirationDateFilter);
            Debug.Assert(winNTPropertyName == "AccountExpirationDate");

            Nullable<DateTime> valueToCompare = (Nullable<DateTime>)filter.Value;

            if (!valueToCompare.HasValue)
            {
                if ((de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (de.Properties[winNTPropertyName].Value == null))
                    return true;
            }
            else
            {
                if (de.Properties.Contains(winNTPropertyName) && (de.Properties[winNTPropertyName].Value != null))
                {
                    DateTime value = (DateTime)de.Properties[winNTPropertyName].Value;

                    if (value.Equals(valueToCompare.Value))
                        return true;
                }
            }

            return false;
        }

        private static bool GroupTypeMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            Debug.Assert(winNTPropertyName == "groupType");
            Debug.Assert(filter is GroupScopeFilter);

            GroupScope valueToMatch = (GroupScope)filter.Value;

            // All SAM local machine groups are local groups
            if (valueToMatch == GroupScope.Local)
                return true;
            else
                return false;
        }
    }

    //
    // The matcher routines for FindBy* support
    //

    internal class FindByDateMatcher : SAMMatcher
    {
        internal enum DateProperty
        {
            LogonTime,
            PasswordSetTime,
            AccountExpirationTime
        }

        private DateProperty _propertyToMatch;
        private MatchType _matchType;
        private DateTime _valueToMatch;

        internal FindByDateMatcher(DateProperty property, MatchType matchType, DateTime value)
        {
            _propertyToMatch = property;
            _matchType = matchType;
            _valueToMatch = value;
        }

        internal override bool Matches(DirectoryEntry de)
        {
            // If it has no SID, it's not a security principal, and we're not interested in it.
            // (In reg-SAM, computers don't have accounts and therefore don't have SIDs, but ADSI
            // creates fake Computer objects for them.  In LSAM, computers CAN have accounts, and thus
            // SIDs).
            if (de.Properties["objectSid"] == null || de.Properties["objectSid"].Count == 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "FindByDateMatcher: Matches: skipping no-SID {0}", de.Path);
                return false;
            }

            switch (_propertyToMatch)
            {
                case DateProperty.LogonTime:
                    return MatchOnLogonTime(de);

                case DateProperty.PasswordSetTime:
                    return MatchOnPasswordSetTime(de);

                case DateProperty.AccountExpirationTime:
                    return MatchOnAccountExpirationTime(de);

                default:
                    Debug.Fail("FindByDateMatcher.Matches: Fell off end looking for propertyToMatch=" + _propertyToMatch.ToString());
                    return false;
            }
        }

        private bool MatchOnLogonTime(DirectoryEntry de)
        {
            PropertyValueCollection values = de.Properties["LastLogin"];
            Nullable<DateTime> storeValue = null;

            // Get the logon time from the DirectoryEntry
            if (values.Count > 0)
            {
                Debug.Assert(values.Count == 1);

                storeValue = (Nullable<DateTime>)values[0];
            }

            return TestForMatch(storeValue);
        }

        private bool MatchOnAccountExpirationTime(DirectoryEntry de)
        {
            PropertyValueCollection values = de.Properties["AccountExpirationDate"];
            Nullable<DateTime> storeValue = null;

            // Get the expiration date from the DirectoryEntry
            if (values.Count > 0)
            {
                Debug.Assert(values.Count == 1);

                storeValue = (Nullable<DateTime>)values[0];
            }

            return TestForMatch(storeValue);
        }

        private bool MatchOnPasswordSetTime(DirectoryEntry de)
        {
            PropertyValueCollection values = de.Properties["PasswordAge"];
            Nullable<DateTime> storeValue = null;

            if (values.Count != 0)
            {
                Debug.Assert(values.Count == 1);
                Debug.Assert(values[0] is Int32);

                int secondsLapsed = (int)values[0];

                storeValue = DateTime.UtcNow - new TimeSpan(0, 0, secondsLapsed);
            }

            return TestForMatch(storeValue);
        }

        private bool TestForMatch(Nullable<DateTime> nullableStoreValue)
        {
            // If the store object doesn't have the property set, then the only
            // way it could match is if they asked for a not-equals test
            // (if the store object doesn't have a value, then it certainly doesn't match
            // whatever value they specified)
            if (!nullableStoreValue.HasValue)
                return (_matchType == MatchType.NotEquals) ? true : false;

            Debug.Assert(nullableStoreValue.HasValue);
            DateTime storeValue = nullableStoreValue.Value;

            switch (_matchType)
            {
                case MatchType.Equals:
                    return (storeValue == _valueToMatch);

                case MatchType.NotEquals:
                    return (storeValue != _valueToMatch);

                case MatchType.GreaterThan:
                    return (storeValue > _valueToMatch);

                case MatchType.GreaterThanOrEquals:
                    return (storeValue >= _valueToMatch);

                case MatchType.LessThan:
                    return (storeValue < _valueToMatch);

                case MatchType.LessThanOrEquals:
                    return (storeValue <= _valueToMatch);

                default:
                    Debug.Fail("FindByDateMatcher.TestForMatch: Fell off end looking for matchType=" + _matchType.ToString());
                    return false;
            }
        }
    }

    internal class GroupMemberMatcher : SAMMatcher
    {
        private byte[] _memberSidToMatch;

        internal GroupMemberMatcher(byte[] memberSidToMatch)
        {
            Debug.Assert(memberSidToMatch != null);
            Debug.Assert(memberSidToMatch.Length != 0);
            _memberSidToMatch = memberSidToMatch;
        }

        internal override bool Matches(DirectoryEntry groupDE)
        {
            // If it has no SID, it's not a security principal, and we're not interested in it.
            // (In reg-SAM, computers don't have accounts and therefore don't have SIDs, but ADSI
            // creates fake Computer objects for them.  In LSAM, computers CAN have accounts, and thus
            // SIDs).
            if (groupDE.Properties["objectSid"] == null || groupDE.Properties["objectSid"].Count == 0)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "GroupMemberMatcher: Matches: skipping no-SID group={0}", groupDE.Path);
                return false;
            }

            // Enumerate the members of the group, looking for a match
            UnsafeNativeMethods.IADsGroup iADsGroup = (UnsafeNativeMethods.IADsGroup)groupDE.NativeObject;
            UnsafeNativeMethods.IADsMembers iADsMembers = iADsGroup.Members();

            foreach (UnsafeNativeMethods.IADs nativeMember in ((IEnumerable)iADsMembers))
            {
                // Wrap the DirectoryEntry around the native ADSI object
                // (which already has the correct credentials)
                DirectoryEntry memberDE = new DirectoryEntry(nativeMember);

                // No SID --> not interesting
                if (memberDE.Properties["objectSid"] == null || memberDE.Properties["objectSid"].Count == 0)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "GroupMemberMatcher: Matches: skipping member no-SID member={0}", memberDE.Path);
                    continue;
                }

                byte[] memberSid = (byte[])memberDE.Properties["objectSid"].Value;

                // Did we find a matching member in the group?
                if (Utils.AreBytesEqual(memberSid, _memberSidToMatch))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                            "SAMQuerySet",
                                            "GroupMemberMatcher: Matches: match member={0}, group={1)",
                                            memberDE.Path,
                                            groupDE.Path);
                    return true;
                }
            }

            // We tried all the members in the group and didn't get a match on any
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "SamMatcher: Matches: no match, group={0}", groupDE.Path);
            return false;
        }
    }
}

//#endif  // PAPI_REGSAM
