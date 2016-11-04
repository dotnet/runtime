// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
//
//
//  Purpose:  This class represents the software preferences of a particular
//            culture or community.  It includes information such as the
//            language, writing system, and a calendar used by the culture
//            as well as methods for common operations such as printing
//            dates and sorting strings.
//
//
//
//  !!!! NOTE WHEN CHANGING THIS CLASS !!!!
//
//  If adding or removing members to this class, please update CultureInfoBaseObject
//  in ndp/clr/src/vm/object.h. Note, the "actual" layout of the class may be
//  different than the order in which members are declared. For instance, all
//  reference types will come first in the class before value types (like ints, bools, etc)
//  regardless of the order in which they are declared. The best way to see the
//  actual order of the class is to do a !dumpobj on an instance of the managed
//  object inside of the debugger.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System.Globalization
{

#if INSIDE_CLR
    using StringCultureInfoDictionary = Dictionary<string, CultureInfo>;
    using StringLcidDictionary = Dictionary<int, CultureInfo>;
    
    using Lock = Object;
#else
    using StringCultureInfoDictionary = LowLevelDictionary<string, CultureInfo>;
    using StringLcidDictionary = LowLevelDictionary<int, CultureInfo>;
#endif

    [Serializable]
    public partial class CultureInfo : IFormatProvider, ICloneable
    {
        //--------------------------------------------------------------------//
        //                        Internal Information                        //
        //--------------------------------------------------------------------//

        //--------------------------------------------------------------------//
        // Data members to be serialized:
        //--------------------------------------------------------------------//

        // We use an RFC4646 type string to construct CultureInfo.
        // This string is stored in m_name and is authoritative.
        // We use the m_cultureData to get the data for our object

        private bool m_isReadOnly;
        private CompareInfo compareInfo;
        private TextInfo textInfo;
        internal NumberFormatInfo numInfo;
        internal DateTimeFormatInfo dateTimeInfo;
        private Calendar calendar;
        //
        // The CultureData instance that we are going to read data from.
        // For supported culture, this will be the CultureData instance that read data from mscorlib assembly.
        // For customized culture, this will be the CultureData instance that read data from user customized culture binary file.
        //
        [NonSerialized]
        internal CultureData m_cultureData;

        [NonSerialized]
        internal bool m_isInherited;

        [NonSerialized]
        private CultureInfo m_consoleFallbackCulture;

        // Names are confusing.  Here are 3 names we have:
        //
        //  new CultureInfo()   m_name        m_nonSortName   m_sortName
        //      en-US           en-US           en-US           en-US
        //      de-de_phoneb    de-DE_phoneb    de-DE           de-DE_phoneb
        //      fj-fj (custom)  fj-FJ           fj-FJ           en-US (if specified sort is en-US)
        //      en              en              
        //
        // Note that in Silverlight we ask the OS for the text and sort behavior, so the 
        // textinfo and compareinfo names are the same as the name

        // Note that the name used to be serialized for Everett; it is now serialized
        // because alernate sorts can have alternate names.
        // This has a de-DE, de-DE_phoneb or fj-FJ style name
        internal string m_name;

        // This will hold the non sorting name to be returned from CultureInfo.Name property.
        // This has a de-DE style name even for de-DE_phoneb type cultures
        [NonSerialized]
        private string m_nonSortName;

        // This will hold the sorting name to be returned from CultureInfo.SortName property.
        // This might be completely unrelated to the culture name if a custom culture.  Ie en-US for fj-FJ.
        // Otherwise its the sort name, ie: de-DE or de-DE_phoneb
        [NonSerialized]
        private string m_sortName;

        //--------------------------------------------------------------------//
        //
        // Static data members
        //
        //--------------------------------------------------------------------//

        //Get the current user default culture.  This one is almost always used, so we create it by default.
        private static volatile CultureInfo s_userDefaultCulture;

        //
        // All of the following will be created on demand.
        //

        // WARNING: We allow diagnostic tools to directly inspect these three members (s_InvariantCultureInfo, s_DefaultThreadCurrentUICulture and s_DefaultThreadCurrentCulture)
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details. 
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools. 
        // Get in touch with the diagnostics team if you have questions.

        //The Invariant culture;
        private static volatile CultureInfo s_InvariantCultureInfo;

        //These are defaults that we use if a thread has not opted into having an explicit culture
        private static volatile CultureInfo s_DefaultThreadCurrentUICulture;
        private static volatile CultureInfo s_DefaultThreadCurrentCulture;

        [ThreadStatic]
        private static volatile CultureInfo s_currentThreadCulture;
        [ThreadStatic]
        private static volatile CultureInfo s_currentThreadUICulture;

        private static readonly Lock m_lock = new Lock();
        private static volatile StringCultureInfoDictionary s_NameCachedCultures;
        private static volatile StringLcidDictionary s_LcidCachedCultures;       

        //The parent culture.
        [NonSerialized]
        private CultureInfo m_parent;

        // LOCALE constants of interest to us internally and privately for LCID functions
        // (ie: avoid using these and use names if possible)
        internal const int LOCALE_NEUTRAL        = 0x0000;
        private  const int LOCALE_USER_DEFAULT   = 0x0400;
        private  const int LOCALE_SYSTEM_DEFAULT = 0x0800;
        internal const int LOCALE_CUSTOM_UNSPECIFIED = 0x1000;
        internal const int LOCALE_CUSTOM_DEFAULT  = 0x0c00;
        internal const int LOCALE_INVARIANT       = 0x007F;

        static AsyncLocal<CultureInfo> s_asyncLocalCurrentCulture; 
        static AsyncLocal<CultureInfo> s_asyncLocalCurrentUICulture;

        static void AsyncLocalSetCurrentCulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            s_currentThreadCulture = args.CurrentValue;
        }

        static void AsyncLocalSetCurrentUICulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            s_currentThreadUICulture = args.CurrentValue;
        }

        //
        // The CultureData  instance that reads the data provided by our CultureData class.
        //
        // Using a field initializer rather than a static constructor so that the whole class can be lazy
        // init.
        private static readonly bool init = Init();
        private static bool Init()
        {
            if (s_InvariantCultureInfo == null)
            {
                CultureInfo temp = new CultureInfo("", false);
                temp.m_isReadOnly = true;
                s_InvariantCultureInfo = temp;
            }

            s_userDefaultCulture = GetUserDefaultCulture();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CultureInfo Constructors
        //
        ////////////////////////////////////////////////////////////////////////


        public CultureInfo(String name)
            : this(name, true)
        {
        }


        public CultureInfo(String name, bool useUserOverride)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name),
                    SR.ArgumentNull_String);
            }

            InitializeFromName(name, useUserOverride);
        }

        public CultureInfo(int culture) : this(culture, true) 
        {
        }

        public CultureInfo(int culture, bool useUserOverride)
        {
            // We don't check for other invalid LCIDS here...
            if (culture < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(culture), SR.ArgumentOutOfRange_NeedPosNum);
            }
            Contract.EndContractBlock();

            InitializeFromCultureId(culture, useUserOverride);
        }

        private void InitializeFromCultureId(int culture, bool useUserOverride)
        {
            switch (culture)
            {
                case LOCALE_CUSTOM_DEFAULT:
                case LOCALE_SYSTEM_DEFAULT:
                case LOCALE_NEUTRAL:
                case LOCALE_USER_DEFAULT:
                case LOCALE_CUSTOM_UNSPECIFIED:
                    // Can't support unknown custom cultures and we do not support neutral or
                    // non-custom user locales.
                    throw new CultureNotFoundException(nameof(culture), culture, SR.Argument_CultureNotSupported);

                default:
                    // Now see if this LCID is supported in the system default CultureData table.
                    m_cultureData = CultureData.GetCultureData(culture, useUserOverride);
                    break;
            }
            m_isInherited = (this.GetType() != typeof(System.Globalization.CultureInfo));
            m_name = m_cultureData.CultureName;
        }

        private void InitializeFromName(string name, bool useUserOverride)
        {
            // Get our data providing record
            this.m_cultureData = CultureData.GetCultureData(name, useUserOverride);

            if (this.m_cultureData == null)
            {
                throw new CultureNotFoundException(nameof(name), name, SR.Argument_CultureNotSupported);
            }

            this.m_name = this.m_cultureData.CultureName;
            this.m_isInherited = (this.GetType() != typeof(System.Globalization.CultureInfo));
        }

        // Constructor called by SQL Server's special munged culture - creates a culture with
        // a TextInfo and CompareInfo that come from a supplied alternate source. This object
        // is ALWAYS read-only.
        // Note that we really cannot use an LCID version of this override as the cached
        // name we create for it has to include both names, and the logic for this is in
        // the GetCultureInfo override *only*.
        internal CultureInfo(String cultureName, String textAndCompareCultureName)
        {
            if (cultureName == null)
            {
                throw new ArgumentNullException(nameof(cultureName),SR.ArgumentNull_String);
            }
            Contract.EndContractBlock();

            m_cultureData = CultureData.GetCultureData(cultureName, false);
            if (m_cultureData == null)
                throw new CultureNotFoundException(nameof(cultureName), cultureName, SR.Argument_CultureNotSupported);
            
            m_name = m_cultureData.CultureName;

            CultureInfo altCulture = GetCultureInfo(textAndCompareCultureName);
            compareInfo = altCulture.CompareInfo;
            textInfo = altCulture.TextInfo;
        }

        // We do this to try to return the system UI language and the default user languages
        // This method will fallback if this fails (like Invariant)
        //
        // TODO: It would appear that this is only ever called with userOveride = true
        // and this method only has one caller.  Can we fold it into the caller?
        private static CultureInfo GetCultureByName(String name, bool userOverride)
        {
            CultureInfo ci = null;
            // Try to get our culture
            try
            {
                ci = userOverride ? new CultureInfo(name) : CultureInfo.GetCultureInfo(name);
            }
            catch (ArgumentException)
            {
            }

            if (ci == null)
            {
                ci = InvariantCulture;
            }

            return ci;
        }

        //
        // Return a specific culture.  A tad irrelevent now since we always return valid data
        // for neutral locales.
        //
        // Note that there's interesting behavior that tries to find a smaller name, ala RFC4647,
        // if we can't find a bigger name.  That doesn't help with things like "zh" though, so
        // the approach is of questionable value
        //
        public static CultureInfo CreateSpecificCulture(String name)
        {
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

            CultureInfo culture;

            try
            {
                culture = new CultureInfo(name);
            }
            catch (ArgumentException)
            {
                // When CultureInfo throws this exception, it may be because someone passed the form
                // like "az-az" because it came out of an http accept lang. We should try a little
                // parsing to perhaps fall back to "az" here and use *it* to create the neutral.

                int idx;

                culture = null;
                for (idx = 0; idx < name.Length; idx++)
                {
                    if ('-' == name[idx])
                    {
                        try
                        {
                            culture = new CultureInfo(name.Substring(0, idx));
                            break;
                        }
                        catch (ArgumentException)
                        {
                            // throw the original exception so the name in the string will be right
                            throw;
                        }
                    }
                }

                if (culture == null)
                {
                    // nothing to save here; throw the original exception
                    throw;
                }
            }

            // In the most common case, they've given us a specific culture, so we'll just return that.
            if (!(culture.IsNeutralCulture))
            {
                return culture;
            }

            return (new CultureInfo(culture.m_cultureData.SSPECIFICCULTURE));
        }

        //        //
        //        // Return a specific culture.  A tad irrelevent now since we always return valid data
        //        // for neutral locales.
        //        //
        //        // Note that there's interesting behavior that tries to find a smaller name, ala RFC4647,
        //        // if we can't find a bigger name.  That doesn't help with things like "zh" though, so
        //        // the approach is of questionable value
        //        //

        internal static bool VerifyCultureName(String cultureName, bool throwException)
        {
            // This function is used by ResourceManager.GetResourceFileName().
            // ResourceManager searches for resource using CultureInfo.Name,
            // so we should check against CultureInfo.Name.

            for (int i = 0; i < cultureName.Length; i++)
            {
                char c = cultureName[i];
                // TODO: Names can only be RFC4646 names (ie: a-zA-Z0-9) while this allows any unicode letter/digit
                if (Char.IsLetterOrDigit(c) || c == '-' || c == '_')
                {
                    continue;
                }
                if (throwException)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidResourceCultureName, cultureName));
                }
                return false;
            }
            return true;
        }

        internal static bool VerifyCultureName(CultureInfo culture, bool throwException)
        {
            //If we have an instance of one of our CultureInfos, the user can't have changed the
            //name and we know that all names are valid in files.
            if (!culture.m_isInherited)
            {
                return true;
            }

            return VerifyCultureName(culture.Name, throwException);
        }

        // We need to store the override from the culture data record.
        private bool m_useUserOverride;

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            m_name = m_cultureData.CultureName;
            m_useUserOverride = m_cultureData.UseUserOverride;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            Contract.Assert(m_name != null, "[CultureInfo.OnDeserialized] m_name != null");
            InitializeFromName(m_name, m_useUserOverride);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CurrentCulture
        //
        //  This instance provides methods based on the current user settings.
        //  These settings are volatile and may change over the lifetime of the
        //  thread.
        //
        ////////////////////////////////////////////////////////////////////////

        //
        // We use the following order to return CurrentCulture and CurrentUICulture
        //      o   Use WinRT to return the current user profile language
        //      o   use current thread culture if the user already set one using CurrentCulture/CurrentUICulture
        //      o   use thread culture if the user already set one using DefaultThreadCurrentCulture
        //          or DefaultThreadCurrentUICulture
        //      o   Use NLS default user culture
        //      o   Use NLS default system culture
        //      o   Use Invariant culture
        //
        public static CultureInfo CurrentCulture
        {
            get
            {
                CultureInfo ci = GetUserDefaultCultureCacheOverride();
                if (ci != null)
                {
                    return ci;
                }

                if (s_currentThreadCulture != null)
                {
                    return s_currentThreadCulture;
                }

                ci = s_DefaultThreadCurrentCulture;
                if (ci != null)
                {
                    return ci;
                }

                // if s_userDefaultCulture == null means CultureInfo statics didn't get initialized yet. this can happen if there early static 
                // method get executed which eventually hit the cultureInfo code while CultureInfo statics didn’t get chance to initialize
                if (s_userDefaultCulture == null)
                {
                    Init();
                }

                Contract.Assert(s_userDefaultCulture != null);
                return s_userDefaultCulture;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                
                if (s_asyncLocalCurrentCulture == null)
                {
                    Interlocked.CompareExchange(ref s_asyncLocalCurrentCulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentCulture), null);
                }
                // this one will set s_currentThreadCulture too
                s_asyncLocalCurrentCulture.Value = value;
            }
        }

        public static CultureInfo CurrentUICulture
        {
            get
            {
                CultureInfo ci = GetUserDefaultCultureCacheOverride();
                if (ci != null)
                {
                    return ci;
                }

                if (s_currentThreadUICulture != null)
                {
                    return s_currentThreadUICulture;
                }

                ci = s_DefaultThreadCurrentUICulture;
                if (ci != null)
                {
                    return ci;
                }

                // if s_userDefaultCulture == null means CultureInfo statics didn't get initialized yet. this can happen if there early static 
                // method get executed which eventually hit the cultureInfo code while CultureInfo statics didn’t get chance to initialize
                if (s_userDefaultCulture == null)
                {
                    Init();
                }

                Contract.Assert(s_userDefaultCulture != null);
                return s_userDefaultCulture;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                CultureInfo.VerifyCultureName(value, true);
                
                if (s_asyncLocalCurrentUICulture == null)
                {
                    Interlocked.CompareExchange(ref s_asyncLocalCurrentUICulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentUICulture), null);
                }

                // this one will set s_currentThreadUICulture too
                s_asyncLocalCurrentUICulture.Value = value;               
            }
        }

        public static CultureInfo InstalledUICulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                if (s_userDefaultCulture == null)
                {
                    Init();
                }
                Contract.Assert(s_userDefaultCulture != null, "[CultureInfo.InstalledUICulture] s_userDefaultCulture != null");
                return s_userDefaultCulture;
            }
        }

        public static CultureInfo DefaultThreadCurrentCulture
        {
            get { return s_DefaultThreadCurrentCulture; }
            set
            {
                // If you add pre-conditions to this method, check to see if you also need to
                // add them to Thread.CurrentCulture.set.

                s_DefaultThreadCurrentCulture = value;
            }
        }

        public static CultureInfo DefaultThreadCurrentUICulture
        {
            get { return s_DefaultThreadCurrentUICulture; }
            set
            {
                //If they're trying to use a Culture with a name that we can't use in resource lookup,
                //don't even let them set it on the thread.

                // If you add more pre-conditions to this method, check to see if you also need to
                // add them to Thread.CurrentUICulture.set.

                if (value != null)
                {
                    CultureInfo.VerifyCultureName(value, true);
                }

                s_DefaultThreadCurrentUICulture = value;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  InvariantCulture
        //
        //  This instance provides methods, for example for casing and sorting,
        //  that are independent of the system and current user settings.  It
        //  should be used only by processes such as some system services that
        //  require such invariant results (eg. file systems).  In general,
        //  the results are not linguistically correct and do not match any
        //  culture info.
        //
        ////////////////////////////////////////////////////////////////////////


        public static CultureInfo InvariantCulture
        {
            get
            {
                return (s_InvariantCultureInfo);
            }
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  Parent
        //
        //  Return the parent CultureInfo for the current instance.
        //
        ////////////////////////////////////////////////////////////////////////

        public virtual CultureInfo Parent
        {
            get
            {
                if (null == m_parent)
                {
                    try
                    {
                        string parentName = this.m_cultureData.SPARENT;

                        if (String.IsNullOrEmpty(parentName))
                        {
                            m_parent = InvariantCulture;
                        }
                        else
                        {
                            m_parent = new CultureInfo(parentName, this.m_cultureData.UseUserOverride);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // For whatever reason our IPARENT or SPARENT wasn't correct, so use invariant
                        // We can't allow ourselves to fail.  In case of custom cultures the parent of the
                        // current custom culture isn't installed.
                        m_parent = InvariantCulture;
                    }
                }
                return m_parent;
            }
        }

        public virtual int LCID
        {
            get
            {
                return (this.m_cultureData.ILANGUAGE);
            }
        }

        public virtual int KeyboardLayoutId
        {
            get
            {
                return m_cultureData.IINPUTLANGUAGEHANDLE;
            }
        }

        public static CultureInfo[] GetCultures(CultureTypes types)
        {
            Contract.Ensures(Contract.Result<CultureInfo[]>() != null);
            // internally we treat UserCustomCultures as Supplementals but v2
            // treats as Supplementals and Replacements
            if((types & CultureTypes.UserCustomCulture) == CultureTypes.UserCustomCulture)
            {
                types |= CultureTypes.ReplacementCultures;
            }
            return (CultureData.GetCultures(types));
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Name
        //
        //  Returns the full name of the CultureInfo. The name is in format like
        //  "en-US"  This version does NOT include sort information in the name.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String Name
        {
            get
            {
                // We return non sorting name here.
                if (this.m_nonSortName == null)
                {
                    this.m_nonSortName = this.m_cultureData.SNAME;
                    if (this.m_nonSortName == null)
                    {
                        this.m_nonSortName = String.Empty;
                    }
                }
                return this.m_nonSortName;
            }
        }

        // This one has the sort information (ie: de-DE_phoneb)
        internal String SortName
        {
            get
            {
                if (this.m_sortName == null)
                {
                    this.m_sortName = this.m_cultureData.SCOMPAREINFO;
                }

                return this.m_sortName;
            }
        }

        public string IetfLanguageTag
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);

                // special case the compatibility cultures
                switch (this.Name)
                {
                    case "zh-CHT":
                        return "zh-Hant";
                    case "zh-CHS":
                        return "zh-Hans";
                    default:
                        return this.Name;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  DisplayName
        //
        //  Returns the full name of the CultureInfo in the localized language.
        //  For example, if the localized language of the runtime is Spanish and the CultureInfo is
        //  US English, "Ingles (Estados Unidos)" will be returned.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String DisplayName
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                Contract.Assert(m_name != null, "[CultureInfo.DisplayName] Always expect m_name to be set");

                return m_cultureData.SLOCALIZEDDISPLAYNAME;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetNativeName
        //
        //  Returns the full name of the CultureInfo in the native language.
        //  For example, if the CultureInfo is US English, "English
        //  (United States)" will be returned.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String NativeName
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SNATIVEDISPLAYNAME);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetEnglishName
        //
        //  Returns the full name of the CultureInfo in English.
        //  For example, if the CultureInfo is US English, "English
        //  (United States)" will be returned.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String EnglishName
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SENGDISPLAYNAME);
            }
        }

        // ie: en
        public virtual String TwoLetterISOLanguageName
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SISO639LANGNAME);
            }
        }

        // ie: eng
        public virtual String ThreeLetterISOLanguageName
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return m_cultureData.SISO639LANGNAME2;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ThreeLetterWindowsLanguageName
        //
        //  Returns the 3 letter windows language name for the current instance.  eg: "ENU"
        //  The ISO names are much preferred
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String ThreeLetterWindowsLanguageName
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return m_cultureData.SABBREVLANGNAME;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CompareInfo               Read-Only Property
        //
        //  Gets the CompareInfo for this culture.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual CompareInfo CompareInfo
        {
            get
            {
                if (this.compareInfo == null)
                {
                    // Since CompareInfo's don't have any overrideable properties, get the CompareInfo from
                    // the Non-Overridden CultureInfo so that we only create one CompareInfo per culture
                    CompareInfo temp = UseUserOverride
                                        ? GetCultureInfo(this.m_name).CompareInfo
                                        : new CompareInfo(this);
                    if (OkayToCacheClassWithCompatibilityBehavior)
                    {
                        this.compareInfo = temp;
                    }
                    else
                    {
                        return temp;
                    }
                }
                return (compareInfo);
            }
        }

        private static bool OkayToCacheClassWithCompatibilityBehavior
        {
            get
            {
                return true;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  TextInfo
        //
        //  Gets the TextInfo for this culture.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual TextInfo TextInfo
        {
            get
            {
                if (textInfo == null)
                {
                    // Make a new textInfo
                    TextInfo tempTextInfo = new TextInfo(this.m_cultureData);
                    tempTextInfo.SetReadOnlyState(m_isReadOnly);

                    if (OkayToCacheClassWithCompatibilityBehavior)
                    {
                        textInfo = tempTextInfo;
                    }
                    else
                    {
                        return tempTextInfo;
                    }
                }
                return (textInfo);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Equals
        //
        //  Implements Object.Equals().  Returns a boolean indicating whether
        //  or not object refers to the same CultureInfo as the current instance.
        //
        ////////////////////////////////////////////////////////////////////////


        public override bool Equals(Object value)
        {
            if (Object.ReferenceEquals(this, value))
                return true;

            CultureInfo that = value as CultureInfo;

            if (that != null)
            {
                // using CompareInfo to verify the data passed through the constructor
                // CultureInfo(String cultureName, String textAndCompareCultureName)

                return (this.Name.Equals(that.Name) && this.CompareInfo.Equals(that.CompareInfo));
            }

            return (false);
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  GetHashCode
        //
        //  Implements Object.GetHashCode().  Returns the hash code for the
        //  CultureInfo.  The hash code is guaranteed to be the same for CultureInfo A
        //  and B where A.Equals(B) is true.
        //
        ////////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return (this.Name.GetHashCode() + this.CompareInfo.GetHashCode());
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  ToString
        //
        //  Implements Object.ToString().  Returns the name of the CultureInfo,
        //  eg. "de-DE_phoneb", "en-US", or "fj-FJ".
        //
        ////////////////////////////////////////////////////////////////////////


        public override String ToString()
        {
            return m_name;
        }


        public virtual Object GetFormat(Type formatType)
        {
            if (formatType == typeof(NumberFormatInfo))
                return (NumberFormat);
            if (formatType == typeof(DateTimeFormatInfo))
                return (DateTimeFormat);
            return (null);
        }

        public virtual bool IsNeutralCulture
        {
            get
            {
                return this.m_cultureData.IsNeutralCulture;
            }
        }

        public CultureTypes CultureTypes
        {
            get
            {
                CultureTypes types = 0;

                if (m_cultureData.IsNeutralCulture)
                    types |= CultureTypes.NeutralCultures;
                else
                    types |= CultureTypes.SpecificCultures;

                types |= m_cultureData.IsWin32Installed ? CultureTypes.InstalledWin32Cultures : 0;

// Disable  warning 618: System.Globalization.CultureTypes.FrameworkCultures' is obsolete
#pragma warning disable 618
                types |= m_cultureData.IsFramework ? CultureTypes.FrameworkCultures : 0;

#pragma warning restore 618
                types |= m_cultureData.IsSupplementalCustomCulture ? CultureTypes.UserCustomCulture : 0;
                types |= m_cultureData.IsReplacementCulture ? CultureTypes.ReplacementCultures | CultureTypes.UserCustomCulture : 0;

                return types;
            }
        }

        public virtual NumberFormatInfo NumberFormat
        {
            get
            {
                if (numInfo == null)
                {
                    NumberFormatInfo temp = new NumberFormatInfo(this.m_cultureData);
                    temp.isReadOnly = m_isReadOnly;
                    Interlocked.CompareExchange(ref numInfo, temp, null);
                }
                return (numInfo);
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), SR.ArgumentNull_Obj);
                }
                VerifyWritable();
                numInfo = value;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        // GetDateTimeFormatInfo
        //
        // Create a DateTimeFormatInfo, and fill in the properties according to
        // the CultureID.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual DateTimeFormatInfo DateTimeFormat
        {
            get
            {
                if (dateTimeInfo == null)
                {
                    // Change the calendar of DTFI to the specified calendar of this CultureInfo.
                    DateTimeFormatInfo temp = new DateTimeFormatInfo(this.m_cultureData, this.Calendar);
                    temp._isReadOnly = m_isReadOnly;
                    Interlocked.CompareExchange(ref dateTimeInfo, temp, null);
                }
                return (dateTimeInfo);
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), SR.ArgumentNull_Obj);
                }
                VerifyWritable();
                dateTimeInfo = value;
            }
        }

        public void ClearCachedData()
        {
            s_userDefaultCulture = null;

            RegionInfo.s_currentRegionInfo = null;
            #pragma warning disable 0618 // disable the obsolete warning 
            TimeZone.ResetTimeZone();
            #pragma warning restore 0618
            TimeZoneInfo.ClearCachedData();
            s_LcidCachedCultures = null;
            s_NameCachedCultures = null;

            CultureData.ClearCachedData();
        }

        /*=================================GetCalendarInstance==========================
        **Action: Map a Win32 CALID to an instance of supported calendar.
        **Returns: An instance of calendar.
        **Arguments: calType    The Win32 CALID
        **Exceptions:
        **      Shouldn't throw exception since the calType value is from our data table or from Win32 registry.
        **      If we are in trouble (like getting a weird value from Win32 registry), just return the GregorianCalendar.
        ============================================================================*/
        internal static Calendar GetCalendarInstance(CalendarId calType)
        {
            if (calType == CalendarId.GREGORIAN)
            {
                return (new GregorianCalendar());
            }
            return GetCalendarInstanceRare(calType);
        }

        //This function exists as a shortcut to prevent us from loading all of the non-gregorian
        //calendars unless they're required.
        internal static Calendar GetCalendarInstanceRare(CalendarId calType)
        {
            Contract.Assert(calType != CalendarId.GREGORIAN, "calType!=CalendarId.GREGORIAN");

            switch (calType)
            {
                case CalendarId.GREGORIAN_US:               // Gregorian (U.S.) calendar
                case CalendarId.GREGORIAN_ME_FRENCH:        // Gregorian Middle East French calendar
                case CalendarId.GREGORIAN_ARABIC:           // Gregorian Arabic calendar
                case CalendarId.GREGORIAN_XLIT_ENGLISH:     // Gregorian Transliterated English calendar
                case CalendarId.GREGORIAN_XLIT_FRENCH:      // Gregorian Transliterated French calendar
                    return (new GregorianCalendar((GregorianCalendarTypes)calType));
                case CalendarId.TAIWAN:                     // Taiwan Era calendar
                    return (new TaiwanCalendar());
                case CalendarId.JAPAN:                      // Japanese Emperor Era calendar
                    return (new JapaneseCalendar());
                case CalendarId.KOREA:                      // Korean Tangun Era calendar
                    return (new KoreanCalendar());
                case CalendarId.THAI:                       // Thai calendar
                    return (new ThaiBuddhistCalendar());
                case CalendarId.HIJRI:                      // Hijri (Arabic Lunar) calendar
                    return (new HijriCalendar());
                case CalendarId.HEBREW:                     // Hebrew (Lunar) calendar
                    return (new HebrewCalendar());
                case CalendarId.UMALQURA:
                    return (new UmAlQuraCalendar());
                case CalendarId.PERSIAN:
                    return (new PersianCalendar());
            }
            return (new GregorianCalendar());
        }

        /*=================================Calendar==========================
        **Action: Return/set the default calendar used by this culture.
        ** This value can be overridden by regional option if this is a current culture.
        **Returns:
        **Arguments:
        **Exceptions:
        **  ArgumentNull_Obj if the set value is null.
        ============================================================================*/
        public virtual Calendar Calendar
        {
            get
            {
                if (calendar == null)
                {
                    Contract.Assert(this.m_cultureData.CalendarIds.Length > 0, "this.m_cultureData.CalendarIds.Length > 0");
                    // Get the default calendar for this culture.  Note that the value can be
                    // from registry if this is a user default culture.
                    Calendar newObj = this.m_cultureData.DefaultCalendar;

                    System.Threading.Interlocked.MemoryBarrier();
                    newObj.SetReadOnlyState(m_isReadOnly);
                    calendar = newObj;
                }
                return (calendar);
            }
        }

        /*=================================OptionCalendars==========================
        **Action: Return an array of the optional calendar for this culture.
        **Returns: an array of Calendar.
        **Arguments:
        **Exceptions:
        ============================================================================*/


        public virtual Calendar[] OptionalCalendars
        {
            get
            {
                Contract.Ensures(Contract.Result<Calendar[]>() != null);

                //
                // This property always returns a new copy of the calendar array.
                //
                CalendarId[] calID = this.m_cultureData.CalendarIds;
                Calendar[] cals = new Calendar[calID.Length];
                for (int i = 0; i < cals.Length; i++)
                {
                    cals[i] = GetCalendarInstance(calID[i]);
                }
                return (cals);
            }
        }

        public bool UseUserOverride
        {
            get
            {
                return m_cultureData.UseUserOverride;
            }
        }

        public CultureInfo GetConsoleFallbackUICulture()
        {
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

            CultureInfo temp = m_consoleFallbackCulture;
            if (temp == null)
            {
                temp = CreateSpecificCulture(m_cultureData.SCONSOLEFALLBACKNAME);
                temp.m_isReadOnly = true;
                m_consoleFallbackCulture = temp;
            }
            return (temp);
        }

        public virtual Object Clone()
        {
            CultureInfo ci = (CultureInfo)MemberwiseClone();
            ci.m_isReadOnly = false;

            //If this is exactly our type, we can make certain optimizations so that we don't allocate NumberFormatInfo or DTFI unless
            //they've already been allocated.  If this is a derived type, we'll take a more generic codepath.
            if (!m_isInherited)
            {
                if (this.dateTimeInfo != null)
                {
                    ci.dateTimeInfo = (DateTimeFormatInfo)this.dateTimeInfo.Clone();
                }
                if (this.numInfo != null)
                {
                    ci.numInfo = (NumberFormatInfo)this.numInfo.Clone();
                }
            }
            else
            {
                ci.DateTimeFormat = (DateTimeFormatInfo)this.DateTimeFormat.Clone();
                ci.NumberFormat = (NumberFormatInfo)this.NumberFormat.Clone();
            }

            if (textInfo != null)
            {
                ci.textInfo = (TextInfo)textInfo.Clone();
            }

            if (calendar != null)
            {
                ci.calendar = (Calendar)calendar.Clone();
            }

            return (ci);
        }

        public static CultureInfo ReadOnly(CultureInfo ci)
        {
            if (ci == null)
            {
                throw new ArgumentNullException(nameof(ci));
            }
            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();

            if (ci.IsReadOnly)
            {
                return (ci);
            }
            CultureInfo newInfo = (CultureInfo)(ci.MemberwiseClone());

            if (!ci.IsNeutralCulture)
            {
                //If this is exactly our type, we can make certain optimizations so that we don't allocate NumberFormatInfo or DTFI unless
                //they've already been allocated.  If this is a derived type, we'll take a more generic codepath.
                if (!ci.m_isInherited)
                {
                    if (ci.dateTimeInfo != null)
                    {
                        newInfo.dateTimeInfo = DateTimeFormatInfo.ReadOnly(ci.dateTimeInfo);
                    }
                    if (ci.numInfo != null)
                    {
                        newInfo.numInfo = NumberFormatInfo.ReadOnly(ci.numInfo);
                    }
                }
                else
                {
                    newInfo.DateTimeFormat = DateTimeFormatInfo.ReadOnly(ci.DateTimeFormat);
                    newInfo.NumberFormat = NumberFormatInfo.ReadOnly(ci.NumberFormat);
                }
            }

            if (ci.textInfo != null)
            {
                newInfo.textInfo = TextInfo.ReadOnly(ci.textInfo);
            }

            if (ci.calendar != null)
            {
                newInfo.calendar = Calendar.ReadOnly(ci.calendar);
            }

            // Don't set the read-only flag too early.
            // We should set the read-only flag here.  Otherwise, info.DateTimeFormat will not be able to set.
            newInfo.m_isReadOnly = true;

            return (newInfo);
        }


        public bool IsReadOnly
        {
            get
            {
                return (m_isReadOnly);
            }
        }

        private void VerifyWritable()
        {
            if (m_isReadOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        // For resource lookup, we consider a culture the invariant culture by name equality. 
        // We perform this check frequently during resource lookup, so adding a property for
        // improved readability.
        internal bool HasInvariantCultureName
        {
            get { return Name == CultureInfo.InvariantCulture.Name; }
        }

        // Helper function both both overloads of GetCachedReadOnlyCulture.  If lcid is 0, we use the name.
        // If lcid is -1, use the altName and create one of those special SQL cultures.
        internal static CultureInfo GetCultureInfoHelper(int lcid, string name, string altName)
        {
            // retval is our return value.
            CultureInfo retval;

            // Temporary hashtable for the names.
            StringCultureInfoDictionary tempNameHT = s_NameCachedCultures;

            if (name != null)
            {
                name = CultureData.AnsiToLower(name);
            }
            
            if (altName != null)
            {
                altName = CultureData.AnsiToLower(altName);
            }

            // We expect the same result for both hashtables, but will test individually for added safety.
            if (tempNameHT == null)
            {
                tempNameHT = new StringCultureInfoDictionary();
            }
            else
            {
                // If we are called by name, check if the object exists in the hashtable.  If so, return it.
                if (lcid == -1 || lcid == 0)
                {
                    bool ret;
                    lock (m_lock)
                    {
                        ret = tempNameHT.TryGetValue(lcid == 0 ? name : name + '\xfffd' + altName, out retval);
                    }

                    if (ret && retval != null)
                    {
                        return retval;
                    }
                }
            }

            // Next, the Lcid table.
            StringLcidDictionary tempLcidHT = s_LcidCachedCultures;

            if (tempLcidHT == null)
            {
                // Case insensitive is not an issue here, save the constructor call.
                tempLcidHT = new StringLcidDictionary();
            }
            else
            {
                // If we were called by Lcid, check if the object exists in the table.  If so, return it.
                if (lcid > 0)
                {
                    bool ret;
                    lock (m_lock)
                    {
                        ret = tempLcidHT.TryGetValue(lcid, out retval);
                    }
                    if (ret && retval != null)
                    {
                        return retval;
                    }
                }
            }

            // We now have two temporary hashtables and the desired object was not found.
            // We'll construct it.  We catch any exceptions from the constructor call and return null.
            try
            {
                switch (lcid)
                {
                    case -1:
                        // call the private constructor
                        retval = new CultureInfo(name, altName);
                        break;

                    case 0:
                        retval = new CultureInfo(name, false);
                        break;

                    default:
                        retval = new CultureInfo(lcid, false);
                        break;
                }
            }
            catch (ArgumentException)
            {
                return null;
            }

            // Set it to read-only
            retval.m_isReadOnly = true;

            if (lcid == -1)
            {
                lock (m_lock)
                {
                    // This new culture will be added only to the name hash table.
                    tempNameHT[name + '\xfffd' + altName] = retval;
                }
                // when lcid == -1 then TextInfo object is already get created and we need to set it as read only.
                retval.TextInfo.SetReadOnlyState(true);
            }
            else if (lcid == 0)
            {
                // Remember our name (as constructed).  Do NOT use alternate sort name versions because
                // we have internal state representing the sort.  (So someone would get the wrong cached version)
                string newName = CultureData.AnsiToLower(retval.m_name);
                
                // We add this new culture info object to both tables.
                lock (m_lock)
                {
                    tempNameHT[newName] = retval;
                }
            } 
            else
            {
                lock (m_lock)
                {
                    tempLcidHT[lcid] = retval;
                }
            }

            // Copy the two hashtables to the corresponding member variables.  This will potentially overwrite
            // new tables simultaneously created by a new thread, but maximizes thread safety.
            if (-1 != lcid)
            {
                // Only when we modify the lcid hash table, is there a need to overwrite.
                s_LcidCachedCultures = tempLcidHT;
            }

            s_NameCachedCultures = tempNameHT;

            // Finally, return our new CultureInfo object.
            return retval;
        }

        // Gets a cached copy of the specified culture from an internal hashtable (or creates it
        // if not found).  (LCID version)... use named version
        public static CultureInfo GetCultureInfo(int culture)
        {
            // Must check for -1 now since the helper function uses the value to signal
            // the altCulture code path for SQL Server.
            // Also check for zero as this would fail trying to add as a key to the hash.
            if (culture <= 0) 
            {
                throw new ArgumentOutOfRangeException(nameof(culture), SR.ArgumentOutOfRange_NeedPosNum);
            }
            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();
            CultureInfo retval = GetCultureInfoHelper(culture, null, null);
            if (null == retval)
            {
                throw new CultureNotFoundException(nameof(culture), culture, SR.Argument_CultureNotSupported);
            }
            return retval;
        }

        // Gets a cached copy of the specified culture from an internal hashtable (or creates it
        // if not found).  (Named version)
        public static CultureInfo GetCultureInfo(string name)
        {
            // Make sure we have a valid, non-zero length string as name
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            CultureInfo retval = GetCultureInfoHelper(0, name, null);
            if (retval == null)
            {
                throw new CultureNotFoundException(
                    nameof(name), name, SR.Argument_CultureNotSupported);
            }
            return retval;
        }

        // Gets a cached copy of the specified culture from an internal hashtable (or creates it
        // if not found).
        public static CultureInfo GetCultureInfo(string name, string altName)
        {
            // Make sure we have a valid, non-zero length string as name
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (altName == null)
            {
                throw new ArgumentNullException(nameof(altName));
            }
            
            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();

            CultureInfo retval = GetCultureInfoHelper(-1, name, altName);
            if (retval == null)
            {
                throw new CultureNotFoundException("name or altName",
                                        SR.Format(SR.Argument_OneOfCulturesNotSupported, name, altName));
            }
            return retval;
        }

        // This function is deprecated, we don't like it
        public static CultureInfo GetCultureInfoByIetfLanguageTag(string name)
        {
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

            // Disallow old zh-CHT/zh-CHS names
            if (name == "zh-CHT" || name == "zh-CHS")
            {
                throw new CultureNotFoundException(nameof(name), SR.Format(SR.Argument_CultureIetfNotSupported, name));
            }
            
            CultureInfo ci = GetCultureInfo(name);

            // Disallow alt sorts and es-es_TS
            if (ci.LCID > 0xffff || ci.LCID == 0x040a)
            {
                throw new CultureNotFoundException(nameof(name), SR.Format(SR.Argument_CultureIetfNotSupported, name));
            }
            
            return ci;
        }
    }
}

