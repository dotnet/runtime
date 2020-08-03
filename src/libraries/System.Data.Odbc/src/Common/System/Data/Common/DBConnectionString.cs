// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Data.Common
{
    internal sealed class DBConnectionString
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by permission classes so it is much easier to verify correctness
        // when not worried about the class being modified during execution

        private static class KEY
        {
            internal const string Password = "password";
            internal const string PersistSecurityInfo = "persist security info";
            internal const string Pwd = "pwd";
        };

        // this class is serializable with Everett, so ugly field names can't be changed
        private readonly string _encryptedUsersConnectionString;

        // hash of unique keys to values
        private readonly Dictionary<string, string?> _parsetable;

        // a linked list of key/value and their length in _encryptedUsersConnectionString
        private readonly NameValuePair? _keychain;

        // track the existance of "password" or "pwd" in the connection string
        // not used for anything anymore but must keep it set correct for V1.1 serialization
        private readonly bool _hasPassword;

        private readonly string[]? _restrictionValues;
        private readonly string? _restrictions;

        private readonly KeyRestrictionBehavior _behavior;

#pragma warning disable CS0169
#pragma warning disable CA1823
        // this field is no longer used, hence the warning was disabled
        // however, it can not be removed or it will break serialization with V1.1
        private readonly string? _encryptedActualConnectionString;
#pragma warning restore CA1823
#pragma warning restore CS0169

        internal DBConnectionString(string value, string restrictions, KeyRestrictionBehavior behavior, Dictionary<string, string> synonyms, bool useOdbcRules)
            : this(new DbConnectionOptions(value, synonyms, useOdbcRules), restrictions, behavior, synonyms, false)
        {
            // useOdbcRules is only used to parse the connection string, not to parse restrictions because values don't apply there
            // the hashtable doesn't need clone since it isn't shared with anything else
        }

        internal DBConnectionString(DbConnectionOptions connectionOptions)
            : this(connectionOptions, null, KeyRestrictionBehavior.AllowOnly, null, true)
        {
            // used by DBDataPermission to convert from DbConnectionOptions to DBConnectionString
            // since backward compatability requires Everett level classes
        }

        private DBConnectionString(DbConnectionOptions connectionOptions, string? restrictions, KeyRestrictionBehavior behavior, Dictionary<string, string>? synonyms, bool mustCloneDictionary)
        { // used by DBDataPermission
            Debug.Assert(null != connectionOptions, "null connectionOptions");
            switch (behavior)
            {
                case KeyRestrictionBehavior.PreventUsage:
                case KeyRestrictionBehavior.AllowOnly:
                    _behavior = behavior;
                    break;
                default:
                    throw ADP.InvalidKeyRestrictionBehavior(behavior);
            }

            // grab all the parsed details from DbConnectionOptions
            _encryptedUsersConnectionString = connectionOptions.UsersConnectionString(false);
            _hasPassword = connectionOptions._hasPasswordKeyword;
            _parsetable = connectionOptions.Parsetable;
            _keychain = connectionOptions._keyChain;

            // we do not want to serialize out user password unless directed so by "persist security info=true"
            // otherwise all instances of user's password will be replaced with "*"
            if (_hasPassword && !connectionOptions.HasPersistablePassword)
            {
                if (mustCloneDictionary)
                {
                    // clone the hashtable to replace user's password/pwd value with "*"
                    // we only need to clone if coming from DbConnectionOptions and password exists
                    _parsetable = new Dictionary<string, string?>(_parsetable);
                }

                // different than Everett in that instead of removing password/pwd from
                // the hashtable, we replace the value with '*'.  This is okay since we
                // serialize out with '*' so already knows what we do.  Better this way
                // than to treat password specially later on which causes problems.
                const string star = "*";
                if (_parsetable.ContainsKey(KEY.Password))
                {
                    _parsetable[KEY.Password] = star;
                }
                if (_parsetable.ContainsKey(KEY.Pwd))
                {
                    _parsetable[KEY.Pwd] = star;
                }

                // replace user's password/pwd value with "*" in the linked list and build a new string
                _keychain = connectionOptions.ReplacePasswordPwd(out _encryptedUsersConnectionString, true);
            }

            if (!string.IsNullOrEmpty(restrictions))
            {
                _restrictionValues = ParseRestrictions(restrictions, synonyms);
                _restrictions = restrictions;
            }
        }

        private DBConnectionString(DBConnectionString connectionString, string[]? restrictionValues, KeyRestrictionBehavior behavior)
        {
            // used by intersect for two equal connection strings with different restrictions
            _encryptedUsersConnectionString = connectionString._encryptedUsersConnectionString;
            _parsetable = connectionString._parsetable;
            _keychain = connectionString._keychain;
            _hasPassword = connectionString._hasPassword;

            _restrictionValues = restrictionValues;
            _restrictions = null;
            _behavior = behavior;

            Verify(restrictionValues);
        }

        internal KeyRestrictionBehavior Behavior
        {
            get { return _behavior; }
        }

        internal string ConnectionString
        {
            get { return _encryptedUsersConnectionString; }
        }

        internal bool IsEmpty
        {
            get { return (null == _keychain); }
        }

        internal NameValuePair? KeyChain
        {
            get { return _keychain; }
        }

        internal string Restrictions
        {
            get
            {
                string? restrictions = _restrictions;
                if (null == restrictions)
                {
                    string[]? restrictionValues = _restrictionValues;
                    if ((null != restrictionValues) && (0 < restrictionValues.Length))
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int i = 0; i < restrictionValues.Length; ++i)
                        {
                            if (!string.IsNullOrEmpty(restrictionValues[i]))
                            {
                                builder.Append(restrictionValues[i]);
                                builder.Append("=;");
                            }
#if DEBUG
                            else
                            {
                                Debug.Fail("empty restriction");
                            }
#endif
                        }
                        restrictions = builder.ToString();
                    }
                }
                return ((null != restrictions) ? restrictions : "");
            }
        }

        internal string? this[string keyword]
        {
            get { return _parsetable[keyword]; }
        }

        internal bool ContainsKey(string keyword)
        {
            return _parsetable.ContainsKey(keyword);
        }

        internal DBConnectionString Intersect(DBConnectionString? entry)
        {
            KeyRestrictionBehavior behavior = _behavior;
            string[]? restrictionValues = null;

            if (null == entry)
            {
                //Debug.WriteLine("0 entry AllowNothing");
                behavior = KeyRestrictionBehavior.AllowOnly;
            }
            else if (_behavior != entry._behavior)
            { // subset of the AllowOnly array
                behavior = KeyRestrictionBehavior.AllowOnly;

                if (KeyRestrictionBehavior.AllowOnly == entry._behavior)
                { // this PreventUsage and entry AllowOnly
                    if (!ADP.IsEmptyArray(_restrictionValues))
                    {
                        if (!ADP.IsEmptyArray(entry._restrictionValues))
                        {
                            //Debug.WriteLine("1 this PreventUsage with restrictions and entry AllowOnly with restrictions");
                            restrictionValues = NewRestrictionAllowOnly(entry._restrictionValues, _restrictionValues);
                        }
                        else
                        {
                            //Debug.WriteLine("2 this PreventUsage with restrictions and entry AllowOnly with no restrictions");
                        }
                    }
                    else
                    {
                        //Debug.WriteLine("3/4 this PreventUsage with no restrictions and entry AllowOnly");
                        restrictionValues = entry._restrictionValues;
                    }
                }
                else if (!ADP.IsEmptyArray(_restrictionValues))
                { // this AllowOnly and entry PreventUsage
                    if (!ADP.IsEmptyArray(entry._restrictionValues))
                    {
                        //Debug.WriteLine("5 this AllowOnly with restrictions and entry PreventUsage with restrictions");
                        restrictionValues = NewRestrictionAllowOnly(_restrictionValues, entry._restrictionValues);
                    }
                    else
                    {
                        //Debug.WriteLine("6 this AllowOnly and entry PreventUsage with no restrictions");
                        restrictionValues = _restrictionValues;
                    }
                }
                else
                {
                    //Debug.WriteLine("7/8 this AllowOnly with no restrictions and entry PreventUsage");
                }
            }
            else if (KeyRestrictionBehavior.PreventUsage == _behavior)
            { // both PreventUsage
                if (ADP.IsEmptyArray(_restrictionValues))
                {
                    //Debug.WriteLine("9/10 both PreventUsage and this with no restrictions");
                    restrictionValues = entry._restrictionValues;
                }
                else if (ADP.IsEmptyArray(entry._restrictionValues))
                {
                    //Debug.WriteLine("11 both PreventUsage and entry with no restrictions");
                    restrictionValues = _restrictionValues;
                }
                else
                {
                    //Debug.WriteLine("12 both PreventUsage with restrictions");
                    restrictionValues = NoDuplicateUnion(_restrictionValues, entry._restrictionValues);
                }
            }
            else if (!ADP.IsEmptyArray(_restrictionValues) && !ADP.IsEmptyArray(entry._restrictionValues))
            { // both AllowOnly with restrictions
                if (_restrictionValues.Length <= entry._restrictionValues.Length)
                {
                    //Debug.WriteLine("13a this AllowOnly with restrictions and entry AllowOnly with restrictions");
                    restrictionValues = NewRestrictionIntersect(_restrictionValues, entry._restrictionValues);
                }
                else
                {
                    //Debug.WriteLine("13b this AllowOnly with restrictions and entry AllowOnly with restrictions");
                    restrictionValues = NewRestrictionIntersect(entry._restrictionValues, _restrictionValues);
                }
            }
            else
            { // both AllowOnly
                //Debug.WriteLine("14/15/16 this AllowOnly and entry AllowOnly but no restrictions");
            }

            // verify _hasPassword & _parsetable are in [....] between Everett/Whidbey
            Debug.Assert(!_hasPassword || ContainsKey(KEY.Password) || ContainsKey(KEY.Pwd), "OnDeserialized password mismatch this");
            Debug.Assert(null == entry || !entry._hasPassword || entry.ContainsKey(KEY.Password) || entry.ContainsKey(KEY.Pwd), "OnDeserialized password mismatch entry");

            DBConnectionString value = new DBConnectionString(this, restrictionValues, behavior);
            ValidateCombinedSet(this, value);
            ValidateCombinedSet(entry, value);

            return value;
        }

        [Conditional("DEBUG")]
        private void ValidateCombinedSet(DBConnectionString? componentSet, DBConnectionString combinedSet)
        {
            Debug.Assert(combinedSet != null, "The combined connection string should not be null");
            if ((componentSet != null) && (combinedSet._restrictionValues != null) && (componentSet._restrictionValues != null))
            {
                if (componentSet._behavior == KeyRestrictionBehavior.AllowOnly)
                {
                    if (combinedSet._behavior == KeyRestrictionBehavior.AllowOnly)
                    {
                        // Component==Allow, Combined==Allow
                        // All values in the Combined Set should also be in the Component Set
                        // Combined - Component == null
                        Debug.Assert(!combinedSet._restrictionValues.Except(componentSet._restrictionValues).Any(), "Combined set allows values not allowed by component set");
                    }
                    else if (combinedSet._behavior == KeyRestrictionBehavior.PreventUsage)
                    {
                        // Component==Allow, Combined==PreventUsage
                        // Preventions override allows, so there is nothing to check here
                    }
                    else
                    {
                        Debug.Fail($"Unknown behavior for combined set: {combinedSet._behavior}");
                    }
                }
                else if (componentSet._behavior == KeyRestrictionBehavior.PreventUsage)
                {
                    if (combinedSet._behavior == KeyRestrictionBehavior.AllowOnly)
                    {
                        // Component==PreventUsage, Combined==Allow
                        // There shouldn't be any of the values from the Component Set in the Combined Set
                        // Intersect(Component, Combined) == null
                        Debug.Assert(!combinedSet._restrictionValues.Intersect(componentSet._restrictionValues).Any(), "Combined values allows values prevented by component set");
                    }
                    else if (combinedSet._behavior == KeyRestrictionBehavior.PreventUsage)
                    {
                        // Component==PreventUsage, Combined==PreventUsage
                        // All values in the Component Set should also be in the Combined Set
                        // Component - Combined == null
                        Debug.Assert(!componentSet._restrictionValues.Except(combinedSet._restrictionValues).Any(), "Combined values does not prevent all of the values prevented by the component set");
                    }
                    else
                    {
                        Debug.Fail($"Unknown behavior for combined set: {combinedSet._behavior}");
                    }
                }
                else
                {
                    Debug.Fail($"Unknown behavior for component set: {componentSet._behavior}");
                }
            }
        }

        private bool IsRestrictedKeyword(string key)
        {
            // restricted if not found
            return ((null == _restrictionValues) || (0 > Array.BinarySearch(_restrictionValues, key, StringComparer.Ordinal)));
        }

        internal bool IsSupersetOf(DBConnectionString entry)
        {
            Debug.Assert(!_hasPassword || ContainsKey(KEY.Password) || ContainsKey(KEY.Pwd), "OnDeserialized password mismatch this");
            Debug.Assert(!entry._hasPassword || entry.ContainsKey(KEY.Password) || entry.ContainsKey(KEY.Pwd), "OnDeserialized password mismatch entry");

            switch (_behavior)
            {
                case KeyRestrictionBehavior.AllowOnly:
                    // every key must either be in the resticted connection string or in the allowed keywords
                    // keychain may contain duplicates, but it is better than GetEnumerator on _parsetable.Keys
                    for (NameValuePair? current = entry.KeyChain; null != current; current = current.Next)
                    {
                        if (!ContainsKey(current.Name) && IsRestrictedKeyword(current.Name))
                        {
                            return false;
                        }
                    }
                    break;
                case KeyRestrictionBehavior.PreventUsage:
                    // every key can not be in the restricted keywords (even if in the restricted connection string)
                    if (null != _restrictionValues)
                    {
                        foreach (string restriction in _restrictionValues)
                        {
                            if (entry.ContainsKey(restriction))
                            {
                                return false;
                            }
                        }
                    }
                    break;
                default:
                    Debug.Fail("invalid KeyRestrictionBehavior");
                    throw ADP.InvalidKeyRestrictionBehavior(_behavior);
            }
            return true;
        }

        private static string[]? NewRestrictionAllowOnly(string[] allowonly, string[] preventusage)
        {
            List<string>? newlist = null;
            for (int i = 0; i < allowonly.Length; ++i)
            {
                if (0 > Array.BinarySearch(preventusage, allowonly[i], StringComparer.Ordinal))
                {
                    if (null == newlist)
                    {
                        newlist = new List<string>();
                    }
                    newlist.Add(allowonly[i]);
                }
            }
            string[]? restrictionValues = null;
            if (null != newlist)
            {
                restrictionValues = newlist.ToArray();
            }
            Verify(restrictionValues);
            return restrictionValues;
        }

        private static string[]? NewRestrictionIntersect(string[] a, string[] b)
        {
            List<string>? newlist = null;
            for (int i = 0; i < a.Length; ++i)
            {
                if (0 <= Array.BinarySearch(b, a[i], StringComparer.Ordinal))
                {
                    if (null == newlist)
                    {
                        newlist = new List<string>();
                    }
                    newlist.Add(a[i]);
                }
            }
            string[]? restrictionValues = null;
            if (newlist != null)
            {
                restrictionValues = newlist.ToArray();
            }
            Verify(restrictionValues);
            return restrictionValues;
        }

        private static string[] NoDuplicateUnion(string[] a, string[] b)
        {
#if DEBUG
            Debug.Assert(null != a && 0 < a.Length, "empty a");
            Debug.Assert(null != b && 0 < b.Length, "empty b");
            Verify(a);
            Verify(b);
#endif
            List<string> newlist = new List<string>(a.Length + b.Length);
            for (int i = 0; i < a.Length; ++i)
            {
                newlist.Add(a[i]);
            }
            for (int i = 0; i < b.Length; ++i)
            { // find duplicates
                if (0 > Array.BinarySearch(a, b[i], StringComparer.Ordinal))
                {
                    newlist.Add(b[i]);
                }
            }
            string[] restrictionValues = newlist.ToArray();
            Array.Sort(restrictionValues, StringComparer.Ordinal);
            Verify(restrictionValues);
            return restrictionValues;
        }

        private static string[]? ParseRestrictions(string restrictions, Dictionary<string, string>? synonyms)
        {
            List<string> restrictionValues = new List<string>();
            StringBuilder buffer = new StringBuilder(restrictions.Length);

            int nextStartPosition = 0;
            int endPosition = restrictions.Length;
            while (nextStartPosition < endPosition)
            {
                int startPosition = nextStartPosition;

                string? keyname, keyvalue; // since parsing restrictions ignores values, it doesn't matter if we use ODBC rules or OLEDB rules
                nextStartPosition = DbConnectionOptions.GetKeyValuePair(restrictions, startPosition, buffer, false, out keyname, out keyvalue);
                if (!string.IsNullOrEmpty(keyname))
                {
                    string realkeyname = ((null != synonyms) ? (string)synonyms[keyname] : keyname); // MDAC 85144
                    if (string.IsNullOrEmpty(realkeyname))
                    {
                        throw ADP.KeywordNotSupported(keyname);
                    }
                    restrictionValues.Add(realkeyname);
                }
            }
            return RemoveDuplicates(restrictionValues.ToArray());
        }

        internal static string[] RemoveDuplicates(string?[] restrictions)
        {
            int count = restrictions.Length;
            if (0 < count)
            {
                Array.Sort(restrictions, StringComparer.Ordinal);

                for (int i = 1; i < restrictions.Length; ++i)
                {
                    string prev = restrictions[i - 1]!;
                    if ((0 == prev.Length) || (prev == restrictions[i]))
                    {
                        restrictions[i - 1] = null;
                        count--;
                    }
                }
                if (0 == restrictions[restrictions.Length - 1]!.Length)
                {
                    restrictions[restrictions.Length - 1] = null;
                    count--;
                }
                if (count != restrictions.Length)
                {
                    string[] tmp = new string[count];
                    count = 0;
                    for (int i = 0; i < restrictions.Length; ++i)
                    {
                        if (restrictions[i] is string restriction)
                        {
                            tmp[count++] = restriction;
                        }
                    }
                    restrictions = tmp;
                }
            }
            Verify(restrictions);
            return restrictions!;
        }

        [ConditionalAttribute("DEBUG")]
        private static void Verify(string?[]? restrictionValues)
        {
            if (null != restrictionValues)
            {
                for (int i = 1; i < restrictionValues.Length; ++i)
                {
                    Debug.Assert(!string.IsNullOrEmpty(restrictionValues[i - 1]), "empty restriction");
                    Debug.Assert(!string.IsNullOrEmpty(restrictionValues[i]), "empty restriction");
                    Debug.Assert(0 >= StringComparer.Ordinal.Compare(restrictionValues[i - 1], restrictionValues[i]));
                }
            }
        }
    }
}
