/*--
Copyright (c) 2004  Microsoft Corporation

Module Name:

    SAMQuerySet.cs

Abstract:

    Implements the SAMQuerySet ResultSet class.
    
History:

    10-June-2004    MattRim     Created

--*/
 
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.DirectoryServices;
using System.Text.RegularExpressions;

namespace System.DirectoryServices.AccountManagement
{
    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class SAMQuerySet : ResultSet
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
        
            this.schemaTypes = schemaTypes;
            this.entries = entries;
            this.sizeLimit = sizeLimit;     // -1 == no limit
            this.storeCtx = storeCtx;
            this.ctxBase = ctxBase;
            this.matcher = samMatcher;
                
            this.enumerator = this.entries.GetEnumerator();
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
                Debug.Assert(this.endReached == false && this.current != null);

                return SAMUtils.DirectoryEntryAsPrincipal(this.current, this.storeCtx);
    	    }
	    }

    	// Advance the enumerator to the next principal in the result set, pulling in additional pages
    	// of results (or ranges of attribute values) as needed.
    	// Returns true if successful, false if no more results to return.
    	override internal bool MoveNext()
    	{
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "Entering MoveNext");            
    	
            Debug.Assert(this.enumerator != null);

            bool needToRetry = false;
            bool f;

            // Have we exceeded the requested size limit?
            if ((this.sizeLimit != -1) && (this.resultsReturned >= this.sizeLimit))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                        "SAMQuerySet", 
                                        "MoveNext: exceeded sizelimit, ret={0}, limit={1}", 
                                        this.resultsReturned, 
                                        this.sizeLimit);            
                this.endReached = true;
            }

            // End was reached previously.  Nothing more to do.
            if (this.endReached)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "MoveNext: endReached");
                return false;
            }
            
            // Pull the next result.  We may have to repeat this several times
            // until we find a result that matches the user's filter.
            do
            {
                f = this.enumerator.MoveNext();
                needToRetry = false;

                if (f)
                {
                    DirectoryEntry entry = (DirectoryEntry) this.enumerator.Current;

                    // Does it match the user's properties?
                    //
                    // We'd like to use DirectoryEntries.SchemaFilter rather than calling
                    // IsOfCorrectType here, but SchemaFilter has a bug (VSWhidbey # 336654)
                    // where multiple DirectoryEntries all share the same SchemaFilter ---
                    // which would create multithreading issues for us.
                    if (IsOfCorrectType(entry) && this.matcher.Matches(entry))
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "MoveNext: found a match on {0}", entry.Path);
                    
                        // Yes.  It's our new current object
                        this.current = entry;
                        this.resultsReturned++;
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

        bool IsOfCorrectType(DirectoryEntry de)
        {
            // Is the object in question one of the desired types?
                    
            foreach (string schemaType in this.schemaTypes)
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
            if (this.current != null)
            {
                this.endReached = false;
                this.current = null;

                if (this.enumerator != null)
                    this.enumerator.Reset();

                this.resultsReturned = 0;
            }
        }


        //
        // Private fields
        //

        SAMStoreCtx storeCtx;
        DirectoryEntry ctxBase;
        DirectoryEntries entries;
        IEnumerator enumerator = null;  // the enumerator for "entries"
        DirectoryEntry current = null;  // the DirectoryEntry that we're currently positioned at

        // Filter parameters
        int sizeLimit;  // -1 == no limit
        List<string> schemaTypes;
        SAMMatcher matcher;

        // Count of number of results returned so far
        int resultsReturned = 0;

        // Have we run out of entries?
        bool endReached = false;
    }



    abstract class SAMMatcher
    {
        abstract internal bool Matches(DirectoryEntry de);
    }


    //
    // The matcher routines for query-by-example support
    //

    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class QbeMatcher : SAMMatcher
    {
    
        QbeFilterDescription propertiesToMatch;        

        internal QbeMatcher(QbeFilterDescription propertiesToMatch)
        {
            this.propertiesToMatch = propertiesToMatch;
        }

        //
        // Static constructor: used for initializing static tables
        //
        static QbeMatcher()
        {
            //
            // Load the filterPropertiesTable
            //
            filterPropertiesTable = new Hashtable();

            for (int i=0; i<filterPropertiesTableRaw.GetLength(0);i++)
            {
                Type qbeType = filterPropertiesTableRaw[i, 0] as Type;
                string winNTPropertyName = filterPropertiesTableRaw[i, 1] as string;
                MatcherDelegate f = filterPropertiesTableRaw[i, 2] as MatcherDelegate;

                Debug.Assert(qbeType != null);
                Debug.Assert(winNTPropertyName != null);
                Debug.Assert(f != null);

                // There should only be one entry per QBE type
                Debug.Assert(filterPropertiesTable[qbeType] == null);

                FilterPropertyTableEntry entry = new FilterPropertyTableEntry();
                entry.winNTPropertyName = winNTPropertyName;
                entry.matcher = f;

                filterPropertiesTable[qbeType] = entry;
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
            foreach (FilterBase filter in this.propertiesToMatch.FiltersToApply)
            {
                FilterPropertyTableEntry entry =(FilterPropertyTableEntry) filterPropertiesTable[filter.GetType()];

                if (entry == null)
                {
                    // Must be a property we don't support
                    throw new NotSupportedException(
                                String.Format(
                                        CultureInfo.CurrentCulture,
                                        StringResources.StoreCtxUnsupportedPropertyForQuery,
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
        static object[,] filterPropertiesTableRaw = 
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

        static Hashtable filterPropertiesTable = null;

        class FilterPropertyTableEntry
        {
            internal string winNTPropertyName;
            internal MatcherDelegate matcher;
        }

        //
        // Conversion routines
        //


        static bool WildcardStringMatch( FilterBase filter, string wildcardFilter, string property )
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
        delegate bool MatcherDelegate(FilterBase filter, string winNTPropertyName, DirectoryEntry de);
            
        static bool DateTimeMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {        
            QbeMatchType valueToMatch = (QbeMatchType) filter.Value;

            if (null == valueToMatch.Value)
            {
                if ( (de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (de.Properties[winNTPropertyName].Value == null) )
                    return true;
            }
            else
            {

                Debug.Assert(valueToMatch.Value is DateTime);
            
                if (de.Properties.Contains(winNTPropertyName) && (de.Properties[winNTPropertyName].Value != null))
                {

                    DateTime value;
                    
                    if ( winNTPropertyName == "PasswordAge" )
                    {
                        PropertyValueCollection values = de.Properties["PasswordAge"];

                        if (values.Count != 0)
                        {
                            Debug.Assert(values.Count == 1);
                            Debug.Assert(values[0] is Int32);

                            int secondsLapsed = (int) values[0];

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
                        value = (DateTime) de.Properties[winNTPropertyName].Value;
                    }

                    

                    int comparisonResult = DateTime.Compare( value, (DateTime)valueToMatch.Value);
                    bool result = true;

                    switch( valueToMatch.Match )
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

        static bool StringMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {        
            string valueToMatch = (string) filter.Value;

            if (valueToMatch == null)
            {
                if ( (de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (((string)de.Properties[winNTPropertyName].Value).Length == 0) )
                    return true;
            }
            else
            {            
                if (de.Properties.Contains(winNTPropertyName))
                {
                    string value = (string)de.Properties[winNTPropertyName].Value;

                    if (value != null)
                    {
                        return WildcardStringMatch( filter, valueToMatch, value );                   
                    }
                }
            }

            return false;
        }

        static bool IntMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {        
            QbeMatchType valueToMatch = (QbeMatchType) filter.Value;
            bool result = false;

            if (null == valueToMatch.Value)
            {
                if ( (de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (de.Properties[winNTPropertyName].Value == null) )
                    result = true;
            }
            else
            {

                if (de.Properties.Contains(winNTPropertyName))
                {
                    int value = (int)de.Properties[winNTPropertyName].Value;       
                    int comparisonValue = (int) valueToMatch.Value;
                
                    switch( valueToMatch.Match )
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

        static bool SamAccountNameMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            string samToMatch = (string) filter.Value;
            
            int index = samToMatch.IndexOf('\\');

            if (index == samToMatch.Length-1)
                throw new InvalidOperationException(StringResources.StoreCtxNT4IdentityClaimWrongForm);

            string samAccountName = (index != -1 ) ? samToMatch.Substring(index+1) :    // +1 to skip the '/'
                                                     samToMatch;

            if (de.Properties["Name"].Count > 0 && de.Properties["Name"].Value != null)
            {

                return WildcardStringMatch( filter, samAccountName, (string)de.Properties["Name"].Value );
                /*
                return (String.Compare(((string)de.Properties["Name"].Value),
                                       samAccountName,
                                       true,                                // acct names are not case-sensitive
                                       CultureInfo.InvariantCulture) == 0);
                                       */
            }

            return false;
        }

        static bool SidMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            byte[] sidToMatch = Utils.StringToByteArray((string) filter.Value);

            if (sidToMatch == null)
                throw new InvalidOperationException(StringResources.StoreCtxSecurityIdentityClaimBadFormat);

            if (de.Properties["objectSid"].Count > 0 && de.Properties["objectSid"].Value != null)
            {
                return Utils.AreBytesEqual(sidToMatch, (byte[]) de.Properties["objectSid"].Value);
            }
            
            return false;            
        }       

        static bool UserFlagsMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            Debug.Assert(winNTPropertyName == "UserFlags");

            bool valueToMatch = (bool) filter.Value;

            // If it doesn't contain the property, it certainly can't match the user's value
            if (!de.Properties.Contains(winNTPropertyName) || de.Properties[winNTPropertyName].Count == 0)
                return false;

            int value = (int) de.Properties[winNTPropertyName].Value;
    
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

        static bool MultiStringMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            string valueToMatch = (string) filter.Value;

            if (valueToMatch == null)
            {
                if ( (de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (((string)de.Properties[winNTPropertyName].Value).Length == 0) )
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
                            return WildcardStringMatch( filter, valueToMatch, value );                   
                        }                    
                    }
                }
            }

            return false;
            
        }
        
        static bool BinaryMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            byte[] valueToMatch = (byte[]) filter.Value;

            if (valueToMatch == null)
            {
                if ( (de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (de.Properties[winNTPropertyName].Value == null) )
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

        static bool ExpirationDateMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        {
            Debug.Assert(filter is ExpirationDateFilter);
            Debug.Assert(winNTPropertyName == "AccountExpirationDate");
        
            Nullable<DateTime> valueToCompare = (Nullable<DateTime>) filter.Value;

            if (!valueToCompare.HasValue)
            {
                if ( (de.Properties.Contains(winNTPropertyName) == false) ||
                     (de.Properties[winNTPropertyName].Count == 0) ||
                     (de.Properties[winNTPropertyName].Value == null) )
                    return true;
            }
            else
            {
                if (de.Properties.Contains(winNTPropertyName) && (de.Properties[winNTPropertyName].Value != null))
                {
                    DateTime value = (DateTime) de.Properties[winNTPropertyName].Value;

                    if (value.Equals(valueToCompare.Value))
                        return true;
                }
            }

            return false;
        }

        static bool GroupTypeMatcher(FilterBase filter, string winNTPropertyName, DirectoryEntry de)
        { 
            Debug.Assert(winNTPropertyName == "groupType");
            Debug.Assert(filter is GroupScopeFilter);

            GroupScope valueToMatch = (GroupScope) filter.Value;

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

    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class FindByDateMatcher : SAMMatcher
    {
        internal enum DateProperty
        {
            LogonTime,
            PasswordSetTime,
            AccountExpirationTime
        }

        DateProperty propertyToMatch;
        MatchType matchType;
        DateTime valueToMatch;

        internal FindByDateMatcher(DateProperty property, MatchType matchType, DateTime value)
        {
            this.propertyToMatch = property;
            this.matchType = matchType;
            this.valueToMatch = value;
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
        
            switch (this.propertyToMatch)
            {
                case DateProperty.LogonTime:
                    return MatchOnLogonTime(de);

                case DateProperty.PasswordSetTime:
                    return MatchOnPasswordSetTime(de);

                case DateProperty.AccountExpirationTime:
                    return MatchOnAccountExpirationTime(de);

                default:
                    Debug.Fail("FindByDateMatcher.Matches: Fell off end looking for propertyToMatch=" + this.propertyToMatch.ToString());
                    return false;
            }
        }

        bool MatchOnLogonTime(DirectoryEntry de)
        {
            PropertyValueCollection values = de.Properties["LastLogin"];
            Nullable<DateTime> storeValue = null;

            // Get the logon time from the DirectoryEntry
            if (values.Count > 0)
            {
                Debug.Assert(values.Count == 1);

                storeValue = (Nullable<DateTime>) values[0];
            }

            return TestForMatch(storeValue);
        }

        bool MatchOnAccountExpirationTime(DirectoryEntry de)
        {
            PropertyValueCollection values = de.Properties["AccountExpirationDate"];
            Nullable<DateTime> storeValue = null;

            // Get the expiration date from the DirectoryEntry
            if (values.Count > 0)
            {
                Debug.Assert(values.Count == 1);

                storeValue = (Nullable<DateTime>) values[0];
            }

            return TestForMatch(storeValue);
        }

        bool MatchOnPasswordSetTime(DirectoryEntry de)
        {
            PropertyValueCollection values = de.Properties["PasswordAge"];
            Nullable<DateTime> storeValue = null;

            if (values.Count != 0)
            {
                Debug.Assert(values.Count == 1);
                Debug.Assert(values[0] is Int32);

                int secondsLapsed = (int) values[0];

                storeValue = DateTime.UtcNow - new TimeSpan(0, 0, secondsLapsed);
            }

            return TestForMatch(storeValue);
        }


        bool TestForMatch(Nullable<DateTime> nullableStoreValue)
        {

            // If the store object doesn't have the property set, then the only
            // way it could match is if they asked for a not-equals test
            // (if the store object doesn't have a value, then it certainly doesn't match
            // whatever value they specified)
            if (!nullableStoreValue.HasValue)
                return (this.matchType == MatchType.NotEquals) ? true : false;

            Debug.Assert(nullableStoreValue.HasValue);
            DateTime storeValue = nullableStoreValue.Value;


            switch (this.matchType)
            {
                case MatchType.Equals:
                    return (storeValue == this.valueToMatch);

                case MatchType.NotEquals:
                    return (storeValue != this.valueToMatch);

                case MatchType.GreaterThan:
                    return (storeValue > this.valueToMatch);

                case MatchType.GreaterThanOrEquals:
                    return (storeValue >= this.valueToMatch);

                case MatchType.LessThan:
                    return (storeValue < this.valueToMatch);

                case MatchType.LessThanOrEquals:
                    return (storeValue <= this.valueToMatch);

                default:
                    Debug.Fail("FindByDateMatcher.TestForMatch: Fell off end looking for matchType=" + this.matchType.ToString());
                    return false;                
            }
        }
    }



    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class GroupMemberMatcher : SAMMatcher
    {
        byte[] memberSidToMatch;
    
        internal GroupMemberMatcher(byte[] memberSidToMatch)
        {
            Debug.Assert(memberSidToMatch != null);
            Debug.Assert(memberSidToMatch.Length != 0);
            this.memberSidToMatch = memberSidToMatch;
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
            UnsafeNativeMethods.IADsGroup iADsGroup = (UnsafeNativeMethods.IADsGroup) groupDE.NativeObject;
            UnsafeNativeMethods.IADsMembers iADsMembers = iADsGroup.Members();

            foreach (UnsafeNativeMethods.IADs nativeMember in ((IEnumerable) iADsMembers))
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

                byte[] memberSid = (byte[]) memberDE.Properties["objectSid"].Value;

                // Did we find a matching member in the group?
                if (Utils.AreBytesEqual(memberSid, this.memberSidToMatch))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, 
                                            "SAMQuerySet",
                                            "GroupMemberMatcher: Matches: match member={0}, group={1)", 
                                            memberDE.Path,
                                            groupDE.Path);                
                    return true;
                }
            }

            // We tried all the members in the group and didnt' get a match on any
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SAMQuerySet", "SamMatcher: Matches: no match, group={0}", groupDE.Path);                        
            return false;
        }
    }
    
}

//#endif  // PAPI_REGSAM
