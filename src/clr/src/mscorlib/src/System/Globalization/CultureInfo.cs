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

namespace System.Globalization {
    using System;
    using System.Security;
    using System.Threading;
    using System.Collections;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Security.Permissions;
    using System.Reflection;
    using Microsoft.Win32;
    using System.Diagnostics.Contracts;
    using System.Resources;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public partial class CultureInfo : ICloneable, IFormatProvider {
        //--------------------------------------------------------------------//
        //                        Internal Information                        //
        //--------------------------------------------------------------------//

        //--------------------------------------------------------------------//
        // Data members to be serialized:
        //--------------------------------------------------------------------//

        // We use an RFC4646 type string to construct CultureInfo.
        // This string is stored in m_name and is authoritative.
        // We use the m_cultureData to get the data for our object

        // WARNING
        // WARNING: All member fields declared here must also be in ndp/clr/src/vm/object.h
        // WARNING: They aren't really private because object.h can access them, but other C# stuff cannot
        // WARNING: The type loader will rearrange class member offsets so the mscorwks!CultureInfoBaseObject
        // WARNING: must be manually structured to match the true loaded class layout
        // WARNING
        internal bool m_isReadOnly;
        internal CompareInfo compareInfo;
        internal TextInfo textInfo;
        // Not serialized for now since we only build it privately for use in the CARIB (so rebuilding is OK)
#if !FEATURE_CORECLR
        [NonSerialized]internal RegionInfo regionInfo;
#endif
        internal NumberFormatInfo numInfo;
        internal DateTimeFormatInfo dateTimeInfo;
        internal Calendar calendar;
        [OptionalField(VersionAdded = 1)]
        internal int m_dataItem;       // NEVER USED, DO NOT USE THIS! (Serialized in Whidbey/Everett)
        [OptionalField(VersionAdded = 1)]
        internal int cultureID  = 0x007f;  // NEVER USED, DO NOT USE THIS! (Serialized in Whidbey/Everett)
        //
        // The CultureData instance that we are going to read data from.
        // For supported culture, this will be the CultureData instance that read data from mscorlib assembly.
        // For customized culture, this will be the CultureData instance that read data from user customized culture binary file.
        //
        [NonSerialized]internal CultureData m_cultureData;
        
        [NonSerialized]internal bool m_isInherited;
#if FEATURE_LEAK_CULTURE_INFO
        [NonSerialized]private bool m_isSafeCrossDomain;
        [NonSerialized]private int m_createdDomainID;
#endif // !FEATURE_CORECLR
#if !FEATURE_CORECLR
        [NonSerialized]private CultureInfo m_consoleFallbackCulture;
#endif // !FEATURE_CORECLR

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
        [NonSerialized]private string m_nonSortName;

        // This will hold the sorting name to be returned from CultureInfo.SortName property.
        // This might be completely unrelated to the culture name if a custom culture.  Ie en-US for fj-FJ.
        // Otherwise its the sort name, ie: de-DE or de-DE_phoneb
        [NonSerialized]private string m_sortName;


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

        //The Invariant culture;
        private static volatile CultureInfo s_InvariantCultureInfo;

        //The culture used in the user interface. This is mostly used to load correct localized resources.
        private static volatile CultureInfo s_userDefaultUICulture;

        //This is the UI culture used to install the OS.
        private static volatile CultureInfo s_InstalledUICultureInfo;

        //These are defaults that we use if a thread has not opted into having an explicit culture
        private static volatile CultureInfo s_DefaultThreadCurrentUICulture;
        private static volatile CultureInfo s_DefaultThreadCurrentCulture;

        //This is a cache of all previously created cultures.  Valid keys are LCIDs or the name.  We use two hashtables to track them,
        // depending on how they are called.
        private static volatile Hashtable s_LcidCachedCultures;
        private static volatile Hashtable s_NameCachedCultures;

#if FEATURE_APPX
        // When running under AppX, we use this to get some information about the language list
        [SecurityCritical]
        private static volatile WindowsRuntimeResourceManagerBase s_WindowsRuntimeResourceManager;

        [ThreadStatic]
        private static bool ts_IsDoingAppXCultureInfoLookup;
#endif

        //The parent culture.
        [NonSerialized]private CultureInfo m_parent;

        // LOCALE constants of interest to us internally and privately for LCID functions
        // (ie: avoid using these and use names if possible)
        internal const int LOCALE_NEUTRAL              = 0x0000;
        private  const int LOCALE_USER_DEFAULT         = 0x0400;
        private  const int LOCALE_SYSTEM_DEFAULT       = 0x0800;
        internal const int LOCALE_CUSTOM_DEFAULT       = 0x0c00;
        internal const int LOCALE_CUSTOM_UNSPECIFIED   = 0x1000;
        internal const int LOCALE_INVARIANT            = 0x007F;
        private  const int LOCALE_TRADITIONAL_SPANISH  = 0x040a;

        //
        // The CultureData  instance that reads the data provided by our CultureData class.
        //
        //Using a field initializer rather than a static constructor so that the whole class can be lazy
        //init.
        private static readonly bool init = Init();
        private static bool Init()
        {

            if (s_InvariantCultureInfo == null) 
            {
                CultureInfo temp = new CultureInfo("", false);
                temp.m_isReadOnly = true;
                s_InvariantCultureInfo = temp;
            }
            // First we set it to Invariant in case someone needs it before we're done finding it.
            // For example, if we throw an exception in InitUserDefaultCulture, we will still need an valid
            // s_userDefaultCulture to be used in Thread.CurrentCulture.
            s_userDefaultCulture = s_userDefaultUICulture = s_InvariantCultureInfo;

            s_userDefaultCulture = InitUserDefaultCulture();
            s_userDefaultUICulture = InitUserDefaultUICulture();
            return true;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        static CultureInfo InitUserDefaultCulture()
        {
            String strDefault = GetDefaultLocaleName(LOCALE_USER_DEFAULT);
            if (strDefault == null)
            {
                strDefault = GetDefaultLocaleName(LOCALE_SYSTEM_DEFAULT);

                if (strDefault == null)
                {
                    // If system default doesn't work, keep using the invariant
                    return (CultureInfo.InvariantCulture);
                }
            }
            CultureInfo temp = GetCultureByName(strDefault, true);

            temp.m_isReadOnly = true;

            return (temp);
        }

        static CultureInfo InitUserDefaultUICulture()
        {
            String strDefault = GetUserDefaultUILanguage();

            // In most of cases, UserDefaultCulture == UserDefaultUICulture, so we should use the same instance if possible.
            if (strDefault == UserDefaultCulture.Name)
            {
                return (UserDefaultCulture);
            }

            CultureInfo temp = GetCultureByName( strDefault, true);

            if (temp == null)
            {
                return (CultureInfo.InvariantCulture);
            }

            temp.m_isReadOnly = true;

            return (temp);
        }

#if FEATURE_APPX
        [SecuritySafeCritical]
        internal static CultureInfo GetCultureInfoForUserPreferredLanguageInAppX()
        {
            // If a call to GetCultureInfoForUserPreferredLanguageInAppX() generated a recursive
            // call to itself, return null, since we don't want to stack overflow.  For example, 
            // this can happen if some code in this method ends up calling CultureInfo.CurrentCulture
            // (which is common on check'd build because of BCLDebug logging which calls Int32.ToString()).  
            // In this case, returning null will mean CultureInfo.CurrentCulture gets the default Win32 
            // value, which should be fine. 
            if(ts_IsDoingAppXCultureInfoLookup)
            {
                return null;
            }

            // If running within a compilation process (mscorsvw.exe, for example), it is illegal to
            // load any non-mscorlib assembly for execution. Since WindowsRuntimeResourceManager lives
            // in System.Runtime.WindowsRuntime, caller will need to fall back to default Win32 value,
            // which should be fine because we should only ever need to access FX resources during NGEN.
            // FX resources are always loaded from satellite assemblies - even in AppX processes (see the
            // comments in code:System.Resources.ResourceManager.SetAppXConfiguration for more details).
            if (AppDomain.IsAppXNGen)
            {
                return null;
            }

            CultureInfo toReturn = null;

            try 
            {
                ts_IsDoingAppXCultureInfoLookup = true;

                if(s_WindowsRuntimeResourceManager == null)
                {
                    s_WindowsRuntimeResourceManager = ResourceManager.GetWinRTResourceManager();
                }

                toReturn = s_WindowsRuntimeResourceManager.GlobalResourceContextBestFitCultureInfo;
            } 
            finally 
            {
               ts_IsDoingAppXCultureInfoLookup = false;
            }
 
            return toReturn;
        }

        [SecuritySafeCritical]
        internal static bool SetCultureInfoForUserPreferredLanguageInAppX(CultureInfo ci)
        {
            // If running within a compilation process (mscorsvw.exe, for example), it is illegal to
            // load any non-mscorlib assembly for execution. Since WindowsRuntimeResourceManager lives
            // in System.Runtime.WindowsRuntime, caller will need to fall back to default Win32 value,
            // which should be fine because we should only ever need to access FX resources during NGEN.
            // FX resources are always loaded from satellite assemblies - even in AppX processes (see the
            // comments in code:System.Resources.ResourceManager.SetAppXConfiguration for more details).
            if (AppDomain.IsAppXNGen)
            {
                return false;
            }

            if (s_WindowsRuntimeResourceManager == null)
            {
                s_WindowsRuntimeResourceManager = ResourceManager.GetWinRTResourceManager();
            }

            return s_WindowsRuntimeResourceManager.SetGlobalResourceContextDefaultCulture(ci);
        }
#endif

        ////////////////////////////////////////////////////////////////////////
        //
        //  CultureInfo Constructors
        //
        ////////////////////////////////////////////////////////////////////////


        public CultureInfo(String name) : this(name, true) {
        }


        public CultureInfo(String name, bool useUserOverride) {
            if (name==null) {
                throw new ArgumentNullException("name",
                    Environment.GetResourceString("ArgumentNull_String"));
            }
            Contract.EndContractBlock();

            // Get our data providing record
            this.m_cultureData = CultureData.GetCultureData(name, useUserOverride);

            if (this.m_cultureData == null) {
                throw new CultureNotFoundException("name", name, Environment.GetResourceString("Argument_CultureNotSupported"));
            }

            this.m_name = this.m_cultureData.CultureName;
            this.m_isInherited = (this.GetType() != typeof(System.Globalization.CultureInfo));
        }


#if FEATURE_USE_LCID
        public CultureInfo(int culture) : this(culture, true) {
        }

        public CultureInfo(int culture, bool useUserOverride) {
            // We don't check for other invalid LCIDS here...
            if (culture < 0) {
                throw new ArgumentOutOfRangeException("culture",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
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
                    throw new CultureNotFoundException(
                        "culture", culture, Environment.GetResourceString("Argument_CultureNotSupported"));

                default:
                    // Now see if this LCID is supported in the system default CultureData  table.
                    this.m_cultureData = CultureData.GetCultureData(culture, useUserOverride);
                    break;
            }
            this.m_isInherited = (this.GetType() != typeof(System.Globalization.CultureInfo));
            this.m_name = this.m_cultureData.CultureName;
        }
#endif // FEATURE_USE_LCID

        //
        // CheckDomainSafetyObject throw if the object is customized object which cannot be attached to 
        // other object (like CultureInfo or DateTimeFormatInfo).
        //

        internal static void CheckDomainSafetyObject(Object obj, Object container)
        {
            if (obj.GetType().Assembly != typeof(System.Globalization.CultureInfo).Assembly) {
                
                throw new InvalidOperationException(
                            String.Format(
                                CultureInfo.CurrentCulture, 
                                Environment.GetResourceString("InvalidOperation_SubclassedObject"), 
                                obj.GetType(),
                                container.GetType()));
            }
            Contract.EndContractBlock();
        }

#region Serialization
        // We need to store the override from the culture data record.
        private bool    m_useUserOverride;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
#if FEATURE_USE_LCID
            // Whidbey+ should remember our name
            // but v1 and v1.1 did not store name -- only lcid
            // Whidbey did not store actual alternate sort name in m_name
            //   like we do in v4 so we can't use name for alternate sort
            // e.g. for es-ES_tradnl: v2 puts es-ES in m_name; v4 puts es-ES_tradnl
            if (m_name == null || IsAlternateSortLcid(cultureID))
            {
                Contract.Assert(cultureID >=0, "[CultureInfo.OnDeserialized] cultureID >= 0");
                InitializeFromCultureId(cultureID, m_useUserOverride);
            }
            else
            {
#endif
                Contract.Assert(m_name != null, "[CultureInfo.OnDeserialized] m_name != null");

                this.m_cultureData = CultureData.GetCultureData(m_name, m_useUserOverride);
                if (this.m_cultureData == null)
                    throw new CultureNotFoundException(
                        "m_name", m_name, Environment.GetResourceString("Argument_CultureNotSupported"));
                    
#if FEATURE_USE_LCID
            }
#endif
            m_isInherited = (this.GetType() != typeof(System.Globalization.CultureInfo));

            // in case we have non customized CultureInfo object we shouldn't allow any customized object  
            // to be attached to it for cross app domain safety.
            if (this.GetType().Assembly == typeof(System.Globalization.CultureInfo).Assembly)
            {
                if (textInfo != null)
                {
                    CheckDomainSafetyObject(textInfo, this);
                }
                
                if (compareInfo != null)
                {
                    CheckDomainSafetyObject(compareInfo, this);
                }
            }
        }

#if FEATURE_USE_LCID
        //  A locale ID is a 32 bit value which is the combination of a
        //  language ID, a sort ID, and a reserved area.  The bits are
        //  allocated as follows:
        //
        //  +------------------------+-------+--------------------------------+
        //  |        Reserved        |Sort ID|           Language ID          |
        //  +------------------------+-------+--------------------------------+
        //  31                     20 19   16 15                             0   bit
        private const int LOCALE_SORTID_MASK = 0x000f0000;

        static private bool IsAlternateSortLcid(int lcid)
        {
            if(lcid == LOCALE_TRADITIONAL_SPANISH)
            {
                return true;
            }

            return (lcid & LOCALE_SORTID_MASK) != 0;
        }
#endif

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            this.m_name              = this.m_cultureData.CultureName;
            this.m_useUserOverride   = this.m_cultureData.UseUserOverride;
#if FEATURE_USE_LCID
            // for compatibility with v2 serialize cultureID
            this.cultureID = this.m_cultureData.ILANGUAGE;
#endif
        }
#endregion Serialization

#if FEATURE_LEAK_CULTURE_INFO
        // Is it safe to send this CultureInfo as an instance member of a Thread cross AppDomain boundaries?
        // For Silverlight, the answer is always no.
        internal bool IsSafeCrossDomain {
            get {
                Contract.Assert(m_createdDomainID != 0, "[CultureInfo.IsSafeCrossDomain] m_createdDomainID != 0");
                return m_isSafeCrossDomain;
            }
        }

        internal int CreatedDomainID {
            get {
                Contract.Assert(m_createdDomainID != 0,  "[CultureInfo.CreatedDomain] m_createdDomainID != 0");
                return m_createdDomainID;
            }
        }

        internal void StartCrossDomainTracking() {
        
            // If we have decided about cross domain safety of this instance, we are done
            if (m_createdDomainID != 0)
                return;

            // If FEATURE_LEAK_CULTURE_INFO isn't enabled, we never want to pass
            // CultureInfo as an instance member of a Thread. 
            if (CanSendCrossDomain())
            {
                m_isSafeCrossDomain = true;
            }

            // m_createdDomainID has to be assigned last. We use it to signal that we have
            // completed the check.
            System.Threading.Thread.MemoryBarrier();
            m_createdDomainID = Thread.GetDomainID();
        }
#endif // FEATURE_LEAK_CULTURE_INFO

        // Is it safe to pass the CultureInfo cross AppDomain boundaries, not necessarily as an instance
        // member of Thread. This is different from IsSafeCrossDomain, which implies passing the CultureInfo
        // as a Thread instance member. 
        internal bool CanSendCrossDomain()
        {
            bool isSafe = false;
            if (this.GetType() == typeof(System.Globalization.CultureInfo))
            {
                isSafe = true;
            }
            return isSafe;
        }

        // Constructor called by SQL Server's special munged culture - creates a culture with
        // a TextInfo and CompareInfo that come from a supplied alternate source. This object
        // is ALWAYS read-only.
        // Note that we really cannot use an LCID version of this override as the cached
        // name we create for it has to include both names, and the logic for this is in
        // the GetCultureInfo override *only*.
        internal CultureInfo(String cultureName, String textAndCompareCultureName)
        {
            if (cultureName==null) {
                throw new ArgumentNullException("cultureName",
                    Environment.GetResourceString("ArgumentNull_String"));
            }
            Contract.EndContractBlock();

            this.m_cultureData = CultureData.GetCultureData(cultureName, false);
            if (this.m_cultureData == null)
                throw new CultureNotFoundException(
                    "cultureName", cultureName, Environment.GetResourceString("Argument_CultureNotSupported"));
            
            this.m_name = this.m_cultureData.CultureName;            

            CultureInfo altCulture = GetCultureInfo(textAndCompareCultureName);
            this.compareInfo = altCulture.CompareInfo;
            this.textInfo = altCulture.TextInfo;
        }

        // We do this to try to return the system UI language and the default user languages
        // The callers should have a fallback if this fails (like Invariant)
        private static CultureInfo GetCultureByName(String name, bool userOverride)
        {           
            // Try to get our culture
            try
            {
                return userOverride ? new CultureInfo(name) : CultureInfo.GetCultureInfo(name);
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

        //
        // Return a specific culture.  A tad irrelevent now since we always return valid data
        // for neutral locales.
        //
        // Note that there's interesting behavior that tries to find a smaller name, ala RFC4647,
        // if we can't find a bigger name.  That doesn't help with things like "zh" though, so
        // the approach is of questionable value
        //
#if !FEATURE_CORECLR
        public static CultureInfo CreateSpecificCulture(String name) {
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

            CultureInfo culture;

            try {
                culture = new CultureInfo(name);
            } catch(ArgumentException) {
                // When CultureInfo throws this exception, it may be because someone passed the form
                // like "az-az" because it came out of an http accept lang. We should try a little
                // parsing to perhaps fall back to "az" here and use *it* to create the neutral.

                int idx;

                culture = null;
                for(idx = 0; idx < name.Length; idx++) {
                    if('-' == name[idx]) {
                        try {
                            culture = new CultureInfo(name.Substring(0, idx));
                            break;
                        } catch(ArgumentException) {
                            // throw the original exception so the name in the string will be right
                            throw;
                        }
                    }
                }

                if(null == culture) {
                    // nothing to save here; throw the original exception
                    throw;
                }
            }

            //In the most common case, they've given us a specific culture, so we'll just return that.
            if (!(culture.IsNeutralCulture)) {
                return culture;
            }

            return (new CultureInfo(culture.m_cultureData.SSPECIFICCULTURE));
        }
#endif // !FEATURE_CORECLR

        internal static bool VerifyCultureName(String cultureName, bool throwException) 
        {
            // This function is used by ResourceManager.GetResourceFileName(). 
            // ResourceManager searches for resource using CultureInfo.Name,
            // so we should check against CultureInfo.Name.

            for (int i=0; i<cultureName.Length; i++) {
                char c = cultureName[i];

                if (Char.IsLetterOrDigit(c) || c=='-' || c=='_') {
                    continue;
                }
                if (throwException) {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidResourceCultureName", cultureName));
                }
                return false;
            }
            return true;
            
        }

        internal static bool VerifyCultureName(CultureInfo culture, bool throwException) {
            Contract.Assert(culture!=null, "[CultureInfo.VerifyCultureName]culture!=null");

            //If we have an instance of one of our CultureInfos, the user can't have changed the
            //name and we know that all names are valid in files.
            if (!culture.m_isInherited) {
                return true;
            }

            return VerifyCultureName(culture.Name, throwException);

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


        public static CultureInfo CurrentCulture
        {
            get {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

#if !FEATURE_CORECLR
                return Thread.CurrentThread.CurrentCulture;
#else
                // In the case of CoreCLR, Thread.m_CurrentCulture and
                // Thread.m_CurrentUICulture are thread static so as not to let
                // CultureInfo objects leak across AppDomain boundaries. The
                // fact that these fields are thread static introduces overhead
                // in accessing them (through Thread.CurrentCulture). There is
                // also overhead in accessing Thread.CurrentThread. In this
                // case, we can avoid the overhead of Thread.CurrentThread
                // because these fields are thread static, and so do not
                // require a Thread instance to be accessed.
#if FEATURE_APPX
                if(AppDomain.IsAppXModel()) {
                    CultureInfo culture = GetCultureInfoForUserPreferredLanguageInAppX();
                    if (culture != null)
                        return culture;
                }
#endif
                return Thread.m_CurrentCulture ??
                    s_DefaultThreadCurrentCulture ??
                    s_userDefaultCulture ??
                    UserDefaultCulture;
#endif
            }

            set {
#if FEATURE_APPX
                    if (value == null) {
                        throw new ArgumentNullException("value");
                    }                    

                    if (AppDomain.IsAppXModel()) {
                        if (SetCultureInfoForUserPreferredLanguageInAppX(value)) {
                            // successfully set the culture, otherwise fallback to legacy path
                            return; 
                        }
                    }
#endif
                    Thread.CurrentThread.CurrentCulture = value;
            }
        }

        //
        // This is the equivalence of the Win32 GetUserDefaultLCID()
        //
        internal static CultureInfo UserDefaultCulture {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                CultureInfo temp = s_userDefaultCulture;
                if (temp == null)
                {
                    //
                    // setting the s_userDefaultCulture with invariant culture before intializing it is a protection
                    // against recursion problem just in case if somebody called CurrentCulture from the CultureInfo
                    // creation path. the recursion can happen if the current user culture is a replaced custom culture.
                    //
                    
                    s_userDefaultCulture = CultureInfo.InvariantCulture;
                    temp = InitUserDefaultCulture();
                    s_userDefaultCulture = temp;
                }
                return (temp);
            }
        }

        //
        //  This is the equivalence of the Win32 GetUserDefaultUILanguage()
        //
        internal static CultureInfo UserDefaultUICulture {
            get {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                CultureInfo temp = s_userDefaultUICulture;
                if (temp == null) 
                {
                    //
                    // setting the s_userDefaultCulture with invariant culture before intializing it is a protection
                    // against recursion problem just in case if somebody called CurrentUICulture from the CultureInfo
                    // creation path. the recursion can happen if the current user culture is a replaced custom culture.
                    //
                    
                    s_userDefaultUICulture = CultureInfo.InvariantCulture;
                    
                    temp = InitUserDefaultUICulture();
                    s_userDefaultUICulture = temp;
                }
                return (temp);
            }
        }


        public static CultureInfo CurrentUICulture {
            get {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

#if !FEATURE_CORECLR
                return Thread.CurrentThread.CurrentUICulture;
#else
                // In the case of CoreCLR, Thread.m_CurrentCulture and
                // Thread.m_CurrentUICulture are thread static so as not to let
                // CultureInfo objects leak across AppDomain boundaries. The
                // fact that these fields are thread static introduces overhead
                // in accessing them (through Thread.CurrentCulture). There is
                // also overhead in accessing Thread.CurrentThread. In this
                // case, we can avoid the overhead of Thread.CurrentThread
                // because these fields are thread static, and so do not
                // require a Thread instance to be accessed.
#if FEATURE_APPX
                if(AppDomain.IsAppXModel()) {
                    CultureInfo culture = GetCultureInfoForUserPreferredLanguageInAppX();
                    if (culture != null)
                        return culture;
                }
#endif
                return Thread.m_CurrentUICulture ??
                    s_DefaultThreadCurrentUICulture ??
                    s_userDefaultUICulture ??
                    UserDefaultUICulture;
#endif
            }

            set {
#if FEATURE_APPX
                    if (value == null) {
                        throw new ArgumentNullException("value");
                    }                    

                    if (AppDomain.IsAppXModel()) {
                        if (SetCultureInfoForUserPreferredLanguageInAppX(value)) {
                            // successfully set the culture, otherwise fallback to legacy path
                            return; 
                        }
                    }
#endif
                    Thread.CurrentThread.CurrentUICulture = value;
            }
        }


        //
        // This is the equivalence of the Win32 GetSystemDefaultUILanguage()
        //
        public static CultureInfo InstalledUICulture {
            get {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                CultureInfo temp = s_InstalledUICultureInfo;
                if (temp == null) {
                    String strDefault = GetSystemDefaultUILanguage();
                    temp = GetCultureByName(strDefault, true);

                    if (temp == null)
                    {
                        temp = InvariantCulture;
                    }

                    temp.m_isReadOnly = true;
                    s_InstalledUICultureInfo = temp;
                }
                return (temp);
            }
        }

        public static CultureInfo DefaultThreadCurrentCulture {
            get {
                return s_DefaultThreadCurrentCulture;
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
            [SecurityPermission(SecurityAction.Demand, ControlThread = true)]
#pragma warning restore 618
            set {

                // If you add pre-conditions to this method, check to see if you also need to 
                // add them to Thread.CurrentCulture.set.

                s_DefaultThreadCurrentCulture = value;
            }
        }

        public static CultureInfo DefaultThreadCurrentUICulture {
            get {
                return s_DefaultThreadCurrentUICulture;
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
            [SecurityPermission(SecurityAction.Demand, ControlThread = true)]
#pragma warning restore 618
            set {

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


        public static CultureInfo InvariantCulture {
            [Pure]
            get {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
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
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

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
                        m_parent =  InvariantCulture;
                    }
                }
                return m_parent;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  LCID
        //
        //  Returns a properly formed culture identifier for the current
        //  culture info.
        //
        ////////////////////////////////////////////////////////////////////////

#if FEATURE_USE_LCID
        public virtual int LCID {
            get {
                return (this.m_cultureData.ILANGUAGE);
            }
        }
#endif

        ////////////////////////////////////////////////////////////////////////
        //
        //  BaseInputLanguage
        //
        //  Essentially an LCID, though one that may be different than LCID in the case
        //  of a customized culture (LCID == LOCALE_CUSTOM_UNSPECIFIED).
        //
        ////////////////////////////////////////////////////////////////////////
#if FEATURE_USE_LCID
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual int KeyboardLayoutId
        {
            get
            {
                int keyId = this.m_cultureData.IINPUTLANGUAGEHANDLE;

                // Not a customized culture, return the default Keyboard layout ID, which is the same as the language ID.
                return (keyId);
            }
        }
#endif

#if !FEATURE_CORECLR
        public static CultureInfo[] GetCultures(CultureTypes types) {
            Contract.Ensures(Contract.Result<CultureInfo[]>() != null);
            // internally we treat UserCustomCultures as Supplementals but v2
            // treats as Supplementals and Replacements
            if((types & CultureTypes.UserCustomCulture) == CultureTypes.UserCustomCulture)
            {
                types |= CultureTypes.ReplacementCultures;
            }
            return (CultureData.GetCultures(types));
        }
#endif

        ////////////////////////////////////////////////////////////////////////
        //
        //  Name
        //
        //  Returns the full name of the CultureInfo. The name is in format like
        //  "en-US"  This version does NOT include sort information in the name.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String Name {
            get {
                Contract.Ensures(Contract.Result<String>() != null);

                // We return non sorting name here.
                if (this.m_nonSortName == null) {
                    this.m_nonSortName = this.m_cultureData.SNAME;
                    if (this.m_nonSortName == null) {
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

#if !FEATURE_CORECLR
        [System.Runtime.InteropServices.ComVisible(false)]
        public String IetfLanguageTag
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);

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
#endif

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
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                Contract.Assert(m_name != null, "[CultureInfo.DisplayName]Always expect m_name to be set");

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
        public virtual String NativeName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
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
        public virtual String EnglishName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SENGDISPLAYNAME);
            }
        }
      
        // ie: en
        public virtual String TwoLetterISOLanguageName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SISO639LANGNAME);
            }
        }

#if !FEATURE_CORECLR
        // ie: eng
        public virtual String ThreeLetterISOLanguageName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SISO639LANGNAME2);
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
        public virtual String ThreeLetterWindowsLanguageName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SABBREVLANGNAME);
            }
        }
#endif

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
                Contract.Ensures(Contract.Result<CompareInfo>() != null);

                if (this.compareInfo == null)
                {
                    // Since CompareInfo's don't have any overrideable properties, get the CompareInfo from
                    // the Non-Overridden CultureInfo so that we only create one CompareInfo per culture
                    CompareInfo temp = UseUserOverride 
                                        ? GetCultureInfo(this.m_name).CompareInfo 
                                        : new CompareInfo(this);
                    if (CompatibilitySwitches.IsCompatibilityBehaviorDefined)
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

#if !FEATURE_CORECLR
        ////////////////////////////////////////////////////////////////////////
        //
        //  RegionInfo
        //
        //  Gets the RegionInfo for this culture.
        //
        ////////////////////////////////////////////////////////////////////////
        private RegionInfo Region
        {
            get
            {
                if (regionInfo==null)
                {
                    // Make a new regionInfo
                    RegionInfo tempRegionInfo = new RegionInfo(this.m_cultureData);
                    regionInfo = tempRegionInfo;
                }
                return (regionInfo);
            }
        }
#endif // FEATURE_CORECLR



        ////////////////////////////////////////////////////////////////////////
        //
        //  TextInfo
        //
        //  Gets the TextInfo for this culture.
        //
        ////////////////////////////////////////////////////////////////////////


        public virtual TextInfo TextInfo {
            get {
                Contract.Ensures(Contract.Result<TextInfo>() != null);

                if (textInfo==null) 
                {
                    // Make a new textInfo
                    TextInfo tempTextInfo = new TextInfo(this.m_cultureData);
                    tempTextInfo.SetReadOnlyState(m_isReadOnly);

                    if (CompatibilitySwitches.IsCompatibilityBehaviorDefined)
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
            Contract.Ensures(Contract.Result<String>() != null);

            Contract.Assert(m_name != null, "[CultureInfo.ToString]Always expect m_name to be set");
            return m_name;
        }


        public virtual Object GetFormat(Type formatType) {
            if (formatType == typeof(NumberFormatInfo)) {
                return (NumberFormat);
            }
            if (formatType == typeof(DateTimeFormatInfo)) {
                return (DateTimeFormat);
            }
            return (null);
        }

        public virtual bool IsNeutralCulture {
            get {
                return this.m_cultureData.IsNeutralCulture;
            }
        }

#if !FEATURE_CORECLR
        [System.Runtime.InteropServices.ComVisible(false)]
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
#endif

        public virtual NumberFormatInfo NumberFormat {
            get 
            {
                Contract.Ensures(Contract.Result<NumberFormatInfo>() != null);

                if (numInfo == null) {
                    NumberFormatInfo temp = new NumberFormatInfo(this.m_cultureData);
                    temp.isReadOnly = m_isReadOnly;
                    numInfo = temp;
                }
                return (numInfo);
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("value",
                        Environment.GetResourceString("ArgumentNull_Obj"));
                }
                Contract.EndContractBlock();
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


        public virtual DateTimeFormatInfo DateTimeFormat {
            get {
                Contract.Ensures(Contract.Result<DateTimeFormatInfo>() != null);

                if (dateTimeInfo == null) {
                    // Change the calendar of DTFI to the specified calendar of this CultureInfo.
                    DateTimeFormatInfo temp = new DateTimeFormatInfo(
                        this.m_cultureData, this.Calendar);
                    temp.m_isReadOnly = m_isReadOnly;
                    System.Threading.Thread.MemoryBarrier();
                    dateTimeInfo = temp;
                }
                return (dateTimeInfo);
            }

            set {
                if (value == null) {
                    throw new ArgumentNullException("value",
                        Environment.GetResourceString("ArgumentNull_Obj"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                dateTimeInfo = value;
            }
        }



        public void ClearCachedData() {
            s_userDefaultUICulture = null;
            s_userDefaultCulture = null;

            RegionInfo.s_currentRegionInfo = null;
#if !FEATURE_CORECLR // System.TimeZone does not exist in CoreCLR
            TimeZone.ResetTimeZone();
#endif // FEATURE_CORECLR
            TimeZoneInfo.ClearCachedData();
            // Delete the cached cultures.
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
        internal static Calendar GetCalendarInstance(int calType) {
            if (calType==Calendar.CAL_GREGORIAN) {
                return (new GregorianCalendar());
            }
            return GetCalendarInstanceRare(calType);
        }

        //This function exists as a shortcut to prevent us from loading all of the non-gregorian
        //calendars unless they're required.
        internal static Calendar GetCalendarInstanceRare(int calType) {
            Contract.Assert(calType!=Calendar.CAL_GREGORIAN, "calType!=Calendar.CAL_GREGORIAN");

            switch (calType) {
                case Calendar.CAL_GREGORIAN_US:               // Gregorian (U.S.) calendar
                case Calendar.CAL_GREGORIAN_ME_FRENCH:        // Gregorian Middle East French calendar
                case Calendar.CAL_GREGORIAN_ARABIC:           // Gregorian Arabic calendar
                case Calendar.CAL_GREGORIAN_XLIT_ENGLISH:     // Gregorian Transliterated English calendar
                case Calendar.CAL_GREGORIAN_XLIT_FRENCH:      // Gregorian Transliterated French calendar
                    return (new GregorianCalendar((GregorianCalendarTypes)calType));
                case Calendar.CAL_TAIWAN:                     // Taiwan Era calendar
                    return (new TaiwanCalendar());
                case Calendar.CAL_JAPAN:                      // Japanese Emperor Era calendar
                    return (new JapaneseCalendar());
                case Calendar.CAL_KOREA:                      // Korean Tangun Era calendar
                    return (new KoreanCalendar());
                case Calendar.CAL_THAI:                       // Thai calendar
                    return (new ThaiBuddhistCalendar());
                case Calendar.CAL_HIJRI:                      // Hijri (Arabic Lunar) calendar
                    return (new HijriCalendar());
                case Calendar.CAL_HEBREW:                     // Hebrew (Lunar) calendar
                    return (new HebrewCalendar());
                case Calendar.CAL_UMALQURA:
                    return (new UmAlQuraCalendar());
                case Calendar.CAL_PERSIAN:
                    return (new PersianCalendar());
                case Calendar.CAL_CHINESELUNISOLAR:
                    return (new ChineseLunisolarCalendar());
                case Calendar.CAL_JAPANESELUNISOLAR:
                    return (new JapaneseLunisolarCalendar());
                case Calendar.CAL_KOREANLUNISOLAR:
                    return (new KoreanLunisolarCalendar());
                case Calendar.CAL_TAIWANLUNISOLAR:
                    return (new TaiwanLunisolarCalendar());
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


        public virtual Calendar Calendar {
            get {
                Contract.Ensures(Contract.Result<Calendar>() != null);
                if (calendar == null) {
                    Contract.Assert(this.m_cultureData.CalendarIds.Length > 0, "this.m_cultureData.CalendarIds.Length > 0");
                    // Get the default calendar for this culture.  Note that the value can be
                    // from registry if this is a user default culture.
                    Calendar newObj = this.m_cultureData.DefaultCalendar;

                    System.Threading.Thread.MemoryBarrier();
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


        public virtual Calendar[] OptionalCalendars {
            get {
                Contract.Ensures(Contract.Result<Calendar[]>() != null);

                //
                // This property always returns a new copy of the calendar array.
                //
                int[] calID = this.m_cultureData.CalendarIds;
                Calendar [] cals = new Calendar[calID.Length];
                for (int i = 0; i < cals.Length; i++) {
                    cals[i] = GetCalendarInstance(calID[i]);
                }
                return (cals);
            }
        }


        public bool UseUserOverride {
            get {
                return (this.m_cultureData.UseUserOverride);
            }
        }

#if !FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(false)]
        public CultureInfo GetConsoleFallbackUICulture()
        {
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

            CultureInfo temp = m_consoleFallbackCulture;
            if (temp == null)
            {
                temp = CreateSpecificCulture(this.m_cultureData.SCONSOLEFALLBACKNAME);
                temp.m_isReadOnly = true;
                m_consoleFallbackCulture = temp;
            }
            return (temp);
        }
#endif

        public virtual Object Clone()
        {
            Contract.Ensures(Contract.Result<Object>() != null);

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
                ci.NumberFormat   = (NumberFormatInfo)this.NumberFormat.Clone();
            }

            if (textInfo != null)
            {
                ci.textInfo = (TextInfo) textInfo.Clone();
            }

            if (calendar != null)
            {
                ci.calendar = (Calendar) calendar.Clone();
            }

            return (ci);
        }


        public static CultureInfo ReadOnly(CultureInfo ci) {
            if (ci == null) {
                throw new ArgumentNullException("ci");
            }
            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();

            if (ci.IsReadOnly) {
                return (ci);
            }
            CultureInfo newInfo = (CultureInfo)(ci.MemberwiseClone());

            if (!ci.IsNeutralCulture)
            {
                //If this is exactly our type, we can make certain optimizations so that we don't allocate NumberFormatInfo or DTFI unless
                //they've already been allocated.  If this is a derived type, we'll take a more generic codepath.
                if (!ci.m_isInherited) {
                    if (ci.dateTimeInfo != null) {
                        newInfo.dateTimeInfo = DateTimeFormatInfo.ReadOnly(ci.dateTimeInfo);
                    }
                    if (ci.numInfo != null) {
                        newInfo.numInfo = NumberFormatInfo.ReadOnly(ci.numInfo);
                    }

                } else {
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


        public bool IsReadOnly {
            get {
                return (m_isReadOnly);
            }
        }

        private void VerifyWritable() {
            if (m_isReadOnly) {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }
            Contract.EndContractBlock();
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
            // There is a race condition in this code with the side effect that the second thread's value
            // clobbers the first in the dictionary. This is an acceptable race condition since the CultureInfo objects
            // are content equal (but not reference equal). Since we make no guarantees there, this race condition is
            // acceptable.
            // See code:Dictionary#DictionaryVersusHashtableThreadSafety for details on Dictionary versus 
            // Hashtable thread safety.

            // retval is our return value.
            CultureInfo retval;

            // Temporary hashtable for the names.
            Hashtable tempNameHT = s_NameCachedCultures;

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
                tempNameHT = Hashtable.Synchronized(new Hashtable());
            }
            else
            {
                // If we are called by name, check if the object exists in the hashtable.  If so, return it.
                if (lcid == -1)
                {
                    retval = (CultureInfo)tempNameHT[name + '\xfffd' + altName];
                    if (retval != null)
                    {
                        return retval;
                    }
                }
                else if (lcid == 0)
                {
                    retval = (CultureInfo)tempNameHT[name];
                    if (retval != null)
                    {
                        return retval;
                    }
                }
            }
#if FEATURE_USE_LCID
            // Next, the Lcid table.
            Hashtable tempLcidHT = s_LcidCachedCultures;

            if (tempLcidHT == null)
            {
                // Case insensitive is not an issue here, save the constructor call.
                tempLcidHT = Hashtable.Synchronized(new Hashtable());
            }
            else
            {
                // If we were called by Lcid, check if the object exists in the table.  If so, return it.
                if (lcid > 0)
                {
                    retval = (CultureInfo) tempLcidHT[lcid];
                    if (retval != null)
                    {
                        return retval;
                    }
                }
            }
#endif
            // We now have two temporary hashtables and the desired object was not found.
            // We'll construct it.  We catch any exceptions from the constructor call and return null.
            try
            {
                switch(lcid)
                {
                    case -1:
                        // call the private constructor
                        retval = new CultureInfo(name, altName);
                        break;

                    case 0:
                        retval = new CultureInfo(name, false);
                        break;

                    default:
#if FEATURE_USE_LCID
                        retval = new CultureInfo(lcid, false);
                        break;
#else
                        return null;
#endif
                }
            }
            catch(ArgumentException)
            {
                return null;
            }

            // Set it to read-only
            retval.m_isReadOnly = true;

            if (lcid == -1)
            {
                // This new culture will be added only to the name hash table.
                tempNameHT[name + '\xfffd' + altName] = retval;

                // when lcid == -1 then TextInfo object is already get created and we need to set it as read only.
                retval.TextInfo.SetReadOnlyState(true);
            }
            else
            {
                // Remember our name (as constructed).  Do NOT use alternate sort name versions because
                // we have internal state representing the sort.  (So someone would get the wrong cached version)
                string newName = CultureData.AnsiToLower(retval.m_name);
                
                // We add this new culture info object to both tables.
                tempNameHT[newName] = retval;
#if FEATURE_USE_LCID
                const int LCID_ZH_CHS_HANS = 0x0004;
                const int LCID_ZH_CHT_HANT = 0x7c04;

                if ((retval.LCID == LCID_ZH_CHS_HANS && newName == "zh-hans")
                 || (retval.LCID == LCID_ZH_CHT_HANT && newName == "zh-hant"))
                {
                    // do nothing because we only want zh-CHS and zh-CHT to cache
                    // by lcid
                }
                else
                {
                    tempLcidHT[retval.LCID] = retval;
                }

#endif
            }

#if FEATURE_USE_LCID
            // Copy the two hashtables to the corresponding member variables.  This will potentially overwrite
            // new tables simultaneously created by a new thread, but maximizes thread safety.
            if(-1 != lcid)
            {
                // Only when we modify the lcid hash table, is there a need to overwrite.
                s_LcidCachedCultures = tempLcidHT;
            }
#endif

            s_NameCachedCultures = tempNameHT;

            // Finally, return our new CultureInfo object.
            return retval;
        }

#if FEATURE_USE_LCID
        // Gets a cached copy of the specified culture from an internal hashtable (or creates it
        // if not found).  (LCID version)... use named version
        public static CultureInfo GetCultureInfo(int culture)
        {
            // Must check for -1 now since the helper function uses the value to signal
            // the altCulture code path for SQL Server.
            // Also check for zero as this would fail trying to add as a key to the hash.
            if (culture <= 0) {
                throw new ArgumentOutOfRangeException("culture",
                    Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }
            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();
            CultureInfo retval = GetCultureInfoHelper(culture, null, null);
            if (null == retval)
            {
                throw new CultureNotFoundException(
                    "culture", culture, Environment.GetResourceString("Argument_CultureNotSupported"));
            }
            return retval;
        }
#endif

        // Gets a cached copy of the specified culture from an internal hashtable (or creates it
        // if not found).  (Named version)
        public static CultureInfo GetCultureInfo(string name)
        {
            // Make sure we have a valid, non-zero length string as name
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();

            CultureInfo retval = GetCultureInfoHelper(0, name, null);
            if (retval == null)
            {
                throw new CultureNotFoundException(
                    "name", name, Environment.GetResourceString("Argument_CultureNotSupported"));
                
            }
            return retval;
        }

        // Gets a cached copy of the specified culture from an internal hashtable (or creates it
        // if not found).
        public static CultureInfo GetCultureInfo(string name, string altName)
        {
            // Make sure we have a valid, non-zero length string as name
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (null == altName)
            {
                throw new ArgumentNullException("altName");
            }
            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();

            CultureInfo retval = GetCultureInfoHelper(-1, name, altName);
            if (retval == null)
            {
                throw new CultureNotFoundException("name or altName",
                                        String.Format(
                                            CultureInfo.CurrentCulture, 
                                            Environment.GetResourceString("Argument_OneOfCulturesNotSupported"), 
                                            name,
                                            altName));
            }
            return retval;
        }


#if !FEATURE_CORECLR
        // This function is deprecated, we don't like it
        public static CultureInfo GetCultureInfoByIetfLanguageTag(string name)
        {
            Contract.Ensures(Contract.Result<CultureInfo>() != null);

            // Disallow old zh-CHT/zh-CHS names
            if (name == "zh-CHT" || name == "zh-CHS")
            {
                throw new CultureNotFoundException(
                            "name",
                            String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_CultureIetfNotSupported"), name)
                            );
            }
            
            CultureInfo ci = GetCultureInfo(name);

            // Disallow alt sorts and es-es_TS
            if (ci.LCID > 0xffff || ci.LCID == 0x040a)
            {
                throw new CultureNotFoundException(
                            "name",
                            String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_CultureIetfNotSupported"), name)
                            );
            }
            
            return ci;
        }
#endif
        private static volatile bool s_isTaiwanSku;
        private static volatile bool s_haveIsTaiwanSku;
        internal static bool IsTaiwanSku
        {
            get
            {
                if (!s_haveIsTaiwanSku)
                {
                    s_isTaiwanSku = (GetSystemDefaultUILanguage() == "zh-TW");
                    s_haveIsTaiwanSku = true;
                }
                return (bool)s_isTaiwanSku;
            }
        }

        //
        //  Helper Methods.
        //
        
        // Get Locale Info Ex calls.  So we don't have to muck with the different int/string return types we declared two of these:
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String nativeGetLocaleInfoEx(String localeName, uint field);
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int nativeGetLocaleInfoExInt(String localeName, uint field);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool nativeSetThreadLocale(String localeName);

        [System.Security.SecurityCritical]
        private static String GetDefaultLocaleName(int localeType)
        {
            Contract.Assert(localeType == LOCALE_USER_DEFAULT || localeType == LOCALE_SYSTEM_DEFAULT, "[CultureInfo.GetDefaultLocaleName] localeType must be LOCALE_USER_DEFAULT or LOCALE_SYSTEM_DEFAULT");

            string localeName = null;
            if(InternalGetDefaultLocaleName(localeType, JitHelpers.GetStringHandleOnStack(ref localeName)))
            {
                return localeName;
            }
            return string.Empty;
        }

        // Get the default locale name
        [System.Security.SecurityCritical]  // auto-generated
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternalGetDefaultLocaleName(int localetype, StringHandleOnStack localeString);

        [System.Security.SecuritySafeCritical] // auto-generated
        private static String GetUserDefaultUILanguage()
        {
            string userDefaultUiLanguage = null;
            if(InternalGetUserDefaultUILanguage(JitHelpers.GetStringHandleOnStack(ref userDefaultUiLanguage)))
            {
                return userDefaultUiLanguage;
            }
            return String.Empty;
        }
        
        // Get the user's default UI language, return locale name
        [System.Security.SecurityCritical]  // auto-generated
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternalGetUserDefaultUILanguage(StringHandleOnStack userDefaultUiLanguage);

        [System.Security.SecuritySafeCritical] // auto-generated
        private static String GetSystemDefaultUILanguage()
        {
            string systemDefaultUiLanguage = null;
            if(InternalGetSystemDefaultUILanguage(JitHelpers.GetStringHandleOnStack(ref systemDefaultUiLanguage)))
            {
                return systemDefaultUiLanguage;
            }
            return String.Empty;

        }

        [System.Security.SecurityCritical] // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternalGetSystemDefaultUILanguage(StringHandleOnStack systemDefaultUiLanguage);

// Added but disabled from desktop in .NET 4.0, stayed disabled in .NET 4.5
#if FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String[] nativeGetResourceFallbackArray();
#endif
    }
}

