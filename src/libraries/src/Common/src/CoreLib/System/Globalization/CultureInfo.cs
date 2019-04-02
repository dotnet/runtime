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

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
#if ENABLE_WINRT
using Internal.Runtime.Augments;
#endif

namespace System.Globalization
{
    /// <summary>
    /// This class represents the software preferences of a particular culture
    /// or community. It includes information such as the language, writing
    /// system and a calendar used by the culture as well as methods for
    /// common operations such as printing dates and sorting strings.
    /// </summary>
    /// <remarks>
    /// !!!! NOTE WHEN CHANGING THIS CLASS !!!!
    /// If adding or removing members to this class, please update
    /// CultureInfoBaseObject in ndp/clr/src/vm/object.h. Note, the "actual"
    /// layout of the class may be different than the order in which members
    /// are declared. For instance, all reference types will come first in the
    /// class before value types (like ints, bools, etc) regardless of the
    /// order in which they are declared. The best way to see the actual
    /// order of the class is to do a !dumpobj on an instance of the managed
    /// object inside of the debugger.
    /// </remarks>
    public partial class CultureInfo : IFormatProvider, ICloneable
    {
        // We use an RFC4646 type string to construct CultureInfo.
        // This string is stored in _name and is authoritative.
        // We use the _cultureData to get the data for our object

        private bool _isReadOnly;
        private CompareInfo? _compareInfo;
        private TextInfo? _textInfo;
        internal NumberFormatInfo? _numInfo;
        internal DateTimeFormatInfo? _dateTimeInfo;
        private Calendar? _calendar;
        //
        // The CultureData instance that we are going to read data from.
        // For supported culture, this will be the CultureData instance that read data from mscorlib assembly.
        // For customized culture, this will be the CultureData instance that read data from user customized culture binary file.
        //
        internal CultureData _cultureData;

        internal bool _isInherited;

        private CultureInfo? _consoleFallbackCulture;

        // Names are confusing.  Here are 3 names we have:
        //
        //  new CultureInfo()   _name          _nonSortName    _sortName
        //      en-US           en-US           en-US           en-US
        //      de-de_phoneb    de-DE_phoneb    de-DE           de-DE_phoneb
        //      fj-fj (custom)  fj-FJ           fj-FJ           en-US (if specified sort is en-US)
        //      en              en
        //
        // Note that in Silverlight we ask the OS for the text and sort behavior, so the
        // textinfo and compareinfo names are the same as the name

        // This has a de-DE, de-DE_phoneb or fj-FJ style name
        internal string _name;

        // This will hold the non sorting name to be returned from CultureInfo.Name property.
        // This has a de-DE style name even for de-DE_phoneb type cultures
        private string? _nonSortName;

        // This will hold the sorting name to be returned from CultureInfo.SortName property.
        // This might be completely unrelated to the culture name if a custom culture.  Ie en-US for fj-FJ.
        // Otherwise its the sort name, ie: de-DE or de-DE_phoneb
        private string? _sortName;

        // Get the current user default culture. This one is almost always used, so we create it by default.
        private static volatile CultureInfo? s_userDefaultCulture;

        //The culture used in the user interface. This is mostly used to load correct localized resources.
        private static volatile CultureInfo? s_userDefaultUICulture;

        // WARNING: We allow diagnostic tools to directly inspect these three members (s_InvariantCultureInfo, s_DefaultThreadCurrentUICulture and s_DefaultThreadCurrentCulture)
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details.
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools.
        // Get in touch with the diagnostics team if you have questions.

        // The Invariant culture;
        private static readonly CultureInfo s_InvariantCultureInfo = new CultureInfo(CultureData.Invariant, isReadOnly: true);

        // These are defaults that we use if a thread has not opted into having an explicit culture
        private static volatile CultureInfo? s_DefaultThreadCurrentUICulture;
        private static volatile CultureInfo? s_DefaultThreadCurrentCulture;

        [ThreadStatic]
        private static CultureInfo s_currentThreadCulture;
        [ThreadStatic]
        private static CultureInfo s_currentThreadUICulture;

        private static AsyncLocal<CultureInfo>? s_asyncLocalCurrentCulture;
        private static AsyncLocal<CultureInfo>? s_asyncLocalCurrentUICulture;

        private static void AsyncLocalSetCurrentCulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            s_currentThreadCulture = args.CurrentValue;
        }

        private static void AsyncLocalSetCurrentUICulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            s_currentThreadUICulture = args.CurrentValue;
        }

        private static readonly object _lock = new object();
        private static volatile Dictionary<string, CultureInfo>? s_NameCachedCultures;
        private static volatile Dictionary<int, CultureInfo>? s_LcidCachedCultures;

        // The parent culture.
        private CultureInfo? _parent;

        // LOCALE constants of interest to us internally and privately for LCID functions
        // (ie: avoid using these and use names if possible)
        internal const int LOCALE_NEUTRAL        = 0x0000;
        private  const int LOCALE_USER_DEFAULT   = 0x0400;
        private  const int LOCALE_SYSTEM_DEFAULT = 0x0800;
        internal const int LOCALE_CUSTOM_UNSPECIFIED = 0x1000;
        internal const int LOCALE_CUSTOM_DEFAULT  = 0x0c00;
        internal const int LOCALE_INVARIANT       = 0x007F;

        private static CultureInfo InitializeUserDefaultCulture()
        {
            Interlocked.CompareExchange(ref s_userDefaultCulture, GetUserDefaultCulture(), null);
            return s_userDefaultCulture!;
        }

        private static CultureInfo InitializeUserDefaultUICulture()
        {
            Interlocked.CompareExchange(ref s_userDefaultUICulture, GetUserDefaultUICulture(), null);
            return s_userDefaultUICulture!;
        }

        public CultureInfo(string name) : this(name, true)
        {
        }

        public CultureInfo(string name, bool useUserOverride)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Get our data providing record
            CultureData? cultureData = CultureData.GetCultureData(name, useUserOverride);

            if (cultureData == null)
            {
                throw new CultureNotFoundException(nameof(name), name, SR.Argument_CultureNotSupported);
            }

            _cultureData = cultureData;
            _name = _cultureData.CultureName;
            _isInherited = GetType() != typeof(CultureInfo);
        }

        private CultureInfo(CultureData cultureData, bool isReadOnly = false)
        {
            Debug.Assert(cultureData != null);
            _cultureData = cultureData;
            _name = cultureData.CultureName;
            _isInherited = false;
            _isReadOnly = isReadOnly;
        }

        private static CultureInfo? CreateCultureInfoNoThrow(string name, bool useUserOverride)
        {
            Debug.Assert(name != null);
            CultureData? cultureData = CultureData.GetCultureData(name, useUserOverride);
            if (cultureData == null)
            {
                return null;
            }

            return new CultureInfo(cultureData);
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
                    _cultureData = CultureData.GetCultureData(culture, useUserOverride);
                    break;
            }
            _isInherited = GetType() != typeof(CultureInfo);
            _name = _cultureData.CultureName;
        }

        /// <summary>
        /// Constructor called by SQL Server's special munged culture - creates a culture with
        /// a TextInfo and CompareInfo that come from a supplied alternate source. This object
        /// is ALWAYS read-only.
        /// Note that we really cannot use an LCID version of this override as the cached
        /// name we create for it has to include both names, and the logic for this is in
        /// the GetCultureInfo override *only*.
        /// </summary>
        internal CultureInfo(string cultureName, string textAndCompareCultureName)
        {
            if (cultureName == null)
            {
                throw new ArgumentNullException(nameof(cultureName), SR.ArgumentNull_String);
            }

            CultureData? cultureData = CultureData.GetCultureData(cultureName, false);
            if (cultureData == null)
            {
                throw new CultureNotFoundException(nameof(cultureName), cultureName, SR.Argument_CultureNotSupported);
            }

            _cultureData = cultureData;

            _name = _cultureData.CultureName;

            CultureInfo altCulture = GetCultureInfo(textAndCompareCultureName);
            _compareInfo = altCulture.CompareInfo;
            _textInfo = altCulture.TextInfo;
        }

        /// <summary>
        /// We do this to try to return the system UI language and the default user languages
        /// This method will fallback if this fails (like Invariant)
        /// </summary>
        private static CultureInfo GetCultureByName(string name)
        {
            try
            {
                return new CultureInfo(name)
                {
                    _isReadOnly = true
                };
            }
            catch (ArgumentException)
            {
                return InvariantCulture;
            }
        }

        /// <summary>
        /// Return a specific culture. A tad irrelevent now since we always
        /// return valid data for neutral locales.
        ///
        /// Note that there's interesting behavior that tries to find a
        /// smaller name, ala RFC4647, if we can't find a bigger name.
        /// That doesn't help with things like "zh" though, so the approach
        /// is of questionable value
        /// </summary>
        public static CultureInfo CreateSpecificCulture(string name)
        {
            CultureInfo? culture;

            try
            {
                culture = new CultureInfo(name);
            }
            catch (ArgumentException)
            {
                // When CultureInfo throws this exception, it may be because someone passed the form
                // like "az-az" because it came out of an http accept lang. We should try a little
                // parsing to perhaps fall back to "az" here and use *it* to create the neutral.
                culture = null;
                for (int idx = 0; idx < name.Length; idx++)
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

            return new CultureInfo(culture._cultureData.SpecificCultureName);
        }

        internal static bool VerifyCultureName(string cultureName, bool throwException)
        {
            // This function is used by ResourceManager.GetResourceFileName().
            // ResourceManager searches for resource using CultureInfo.Name,
            // so we should check against CultureInfo.Name.
            for (int i = 0; i < cultureName.Length; i++)
            {
                char c = cultureName[i];
                // TODO: Names can only be RFC4646 names (ie: a-zA-Z0-9) while this allows any unicode letter/digit
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
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
            // If we have an instance of one of our CultureInfos, the user can't have changed the
            // name and we know that all names are valid in files.
            if (!culture._isInherited)
            {
                return true;
            }

            return VerifyCultureName(culture.Name, throwException);
        }

        /// <summary>
        /// This instance provides methods based on the current user settings.
        /// These settings are volatile and may change over the lifetime of the
        /// thread.
        /// </summary>
        /// <remarks>
        /// We use the following order to return CurrentCulture and CurrentUICulture
        ///      o   Use WinRT to return the current user profile language
        ///      o   use current thread culture if the user already set one using CurrentCulture/CurrentUICulture
        ///      o   use thread culture if the user already set one using DefaultThreadCurrentCulture
        ///          or DefaultThreadCurrentUICulture
        ///      o   Use NLS default user culture
        ///      o   Use NLS default system culture
        ///      o   Use Invariant culture
        /// </remarks>
        public static CultureInfo CurrentCulture
        {
            get
            {
#if ENABLE_WINRT
                WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
                if (callbacks != null && callbacks.IsAppxModel())
                {
                    return (CultureInfo)callbacks.GetUserDefaultCulture();
                }
#endif
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                {
                    CultureInfo? culture = GetCultureInfoForUserPreferredLanguageInAppX();
                    if (culture != null)
                        return culture;
                }
#endif

                return s_currentThreadCulture ??
                    s_DefaultThreadCurrentCulture ??
                    s_userDefaultCulture ??
                    InitializeUserDefaultCulture();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

#if ENABLE_WINRT
                WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
                if (callbacks != null && callbacks.IsAppxModel())
                {
                    callbacks.SetGlobalDefaultCulture(value);
                    return;
                }
#endif
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                {
                    if (SetCultureInfoForUserPreferredLanguageInAppX(value))
                    {
                        // successfully set the culture, otherwise fallback to legacy path
                        return;
                    }
                }
#endif

                if (s_asyncLocalCurrentCulture == null)
                {
                    Interlocked.CompareExchange(ref s_asyncLocalCurrentCulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentCulture), null);
                }
                s_asyncLocalCurrentCulture!.Value = value;
            }
        }

        public static CultureInfo CurrentUICulture
        {
            get
            {
#if ENABLE_WINRT
                WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
                if (callbacks != null && callbacks.IsAppxModel())
                {
                    return (CultureInfo)callbacks.GetUserDefaultCulture();
                }
#endif
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                {
                    CultureInfo? culture = GetCultureInfoForUserPreferredLanguageInAppX();
                    if (culture != null)
                        return culture;
                }
#endif

                return s_currentThreadUICulture ??
                    s_DefaultThreadCurrentUICulture ??
                    UserDefaultUICulture;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                CultureInfo.VerifyCultureName(value, true);

#if ENABLE_WINRT
                WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
                if (callbacks != null && callbacks.IsAppxModel())
                {
                    callbacks.SetGlobalDefaultCulture(value);
                    return;
                }
#endif
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                {
                    if (SetCultureInfoForUserPreferredLanguageInAppX(value))
                    {
                        // successfully set the culture, otherwise fallback to legacy path
                        return;
                    }
                }
#endif

                if (s_asyncLocalCurrentUICulture == null)
                {
                    Interlocked.CompareExchange(ref s_asyncLocalCurrentUICulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentUICulture), null);
                }

                // this one will set s_currentThreadUICulture too
                s_asyncLocalCurrentUICulture!.Value = value;
            }
        }

        internal static CultureInfo UserDefaultUICulture => s_userDefaultUICulture ?? InitializeUserDefaultUICulture();

        public static CultureInfo InstalledUICulture => s_userDefaultCulture ?? InitializeUserDefaultCulture();

        public static CultureInfo? DefaultThreadCurrentCulture
        {
            get => s_DefaultThreadCurrentCulture;
            set
            {
                // If you add pre-conditions to this method, check to see if you also need to
                // add them to Thread.CurrentCulture.set.
                s_DefaultThreadCurrentCulture = value;
            }
        }

        public static CultureInfo? DefaultThreadCurrentUICulture
        {
            get => s_DefaultThreadCurrentUICulture;
            set
            {
                // If they're trying to use a Culture with a name that we can't use in resource lookup,
                // don't even let them set it on the thread.

                // If you add more pre-conditions to this method, check to see if you also need to
                // add them to Thread.CurrentUICulture.set.

                if (value != null)
                {
                    CultureInfo.VerifyCultureName(value, true);
                }

                s_DefaultThreadCurrentUICulture = value;
            }
        }

        /// <summary>
        /// This instance provides methods, for example for casing and sorting,
        /// that are independent of the system and current user settings.  It
        /// should be used only by processes such as some system services that
        /// require such invariant results (eg. file systems).  In general,
        /// the results are not linguistically correct and do not match any
        /// culture info.
        /// </summary>
        public static CultureInfo InvariantCulture
        {
            get
            {
                Debug.Assert(s_InvariantCultureInfo != null);
                return s_InvariantCultureInfo;
            }
        }

        /// <summary>
        /// Return the parent CultureInfo for the current instance.
        /// </summary>
        public virtual CultureInfo Parent
        {
            get
            {
                if (_parent == null)
                {
                    CultureInfo culture;
                    string parentName = _cultureData.ParentName;

                    if (string.IsNullOrEmpty(parentName))
                    {
                        culture = InvariantCulture;
                    }
                    else
                    {
                        culture = CreateCultureInfoNoThrow(parentName, _cultureData.UseUserOverride) ??
                            // For whatever reason our IPARENT or SPARENT wasn't correct, so use invariant
                            // We can't allow ourselves to fail.  In case of custom cultures the parent of the
                            // current custom culture isn't installed.
                            InvariantCulture;
                    }

                    Interlocked.CompareExchange<CultureInfo?>(ref _parent, culture, null);
                }
                return _parent!;
            }
        }

        public virtual int LCID => _cultureData.LCID;

        public virtual int KeyboardLayoutId => _cultureData.KeyboardLayoutId;

        public static CultureInfo[] GetCultures(CultureTypes types)
        {
            // internally we treat UserCustomCultures as Supplementals but v2
            // treats as Supplementals and Replacements
            if ((types & CultureTypes.UserCustomCulture) == CultureTypes.UserCustomCulture)
            {
                types |= CultureTypes.ReplacementCultures;
            }
            return CultureData.GetCultures(types);
        }

        /// <summary>
        /// Returns the full name of the CultureInfo. The name is in format like
        /// "en-US" This version does NOT include sort information in the name.
        /// </summary>
        public virtual string Name
        {
            get
            {
                // We return non sorting name here.
                if (_nonSortName == null)
                {
                    _nonSortName = _cultureData.Name ?? string.Empty;
                }
                return _nonSortName;
            }
        }

        /// <summary>
        /// This one has the sort information (ie: de-DE_phoneb)
        /// </summary>
        internal string SortName
        {
            get
            {
                if (_sortName == null)
                {
                    _sortName = _cultureData.SortName;
                }

                return _sortName;
            }
        }

        public string IetfLanguageTag
        {
            get
            {
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

        /// <summary>
        /// Returns the full name of the CultureInfo in the localized language.
        /// For example, if the localized language of the runtime is Spanish and the CultureInfo is
        /// US English, "Ingles (Estados Unidos)" will be returned.
        /// </summary>
        public virtual string DisplayName
        {
            get
            {
                Debug.Assert(_name != null, "[CultureInfo.DisplayName] Always expect _name to be set");
                return _cultureData.DisplayName;
            }
        }

        /// <summary>
        /// Returns the full name of the CultureInfo in the native language.
        /// For example, if the CultureInfo is US English, "English
        /// (United States)" will be returned.
        /// </summary>
        public virtual string NativeName => _cultureData.NativeName;

        /// <summary>
        /// Returns the full name of the CultureInfo in English.
        /// For example, if the CultureInfo is US English, "English
        /// (United States)" will be returned.
        /// </summary>
        public virtual string EnglishName => _cultureData.EnglishName;

        /// <summary>
        /// ie: en
        /// </summary>
        public virtual string TwoLetterISOLanguageName => _cultureData.TwoLetterISOLanguageName;

        /// <summary>
        /// ie: eng
        /// </summary>
        public virtual string ThreeLetterISOLanguageName => _cultureData.ThreeLetterISOLanguageName;

        /// <summary>
        /// Returns the 3 letter windows language name for the current instance.  eg: "ENU"
        /// The ISO names are much preferred
        /// </summary>
        public virtual string ThreeLetterWindowsLanguageName => _cultureData.ThreeLetterWindowsLanguageName;

        //  CompareInfo               Read-Only Property
        /// <summary>
        /// Gets the CompareInfo for this culture.
        /// </summary>
        public virtual CompareInfo CompareInfo
        {
            get
            {
                if (_compareInfo == null)
                {
                    // Since CompareInfo's don't have any overrideable properties, get the CompareInfo from
                    // the Non-Overridden CultureInfo so that we only create one CompareInfo per culture
                    _compareInfo = UseUserOverride
                                    ? GetCultureInfo(_name).CompareInfo
                                    : new CompareInfo(this);
                }
                return _compareInfo;
            }
        }

        /// <summary>
        /// Gets the TextInfo for this culture.
        /// </summary>
        public virtual TextInfo TextInfo
        {
            get
            {
                if (_textInfo == null)
                {
                    // Make a new textInfo
                    TextInfo tempTextInfo = new TextInfo(_cultureData);
                    tempTextInfo.SetReadOnlyState(_isReadOnly);
                    _textInfo = tempTextInfo;
                }
                return _textInfo;
            }
        }

        public override bool Equals(object? value)
        {
            if (object.ReferenceEquals(this, value))
            {
                return true;
            }

            if (value is CultureInfo that)
            {
                // using CompareInfo to verify the data passed through the constructor
                // CultureInfo(String cultureName, String textAndCompareCultureName)
                return Name.Equals(that.Name) && CompareInfo.Equals(that.CompareInfo);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + CompareInfo.GetHashCode();
        }


        /// <summary>
        /// Implements object.ToString(). Returns the name of the CultureInfo,
        /// eg. "de-DE_phoneb", "en-US", or "fj-FJ".
        /// </summary>
        public override string ToString() => _name;

        public virtual object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(NumberFormatInfo))
            {
                return NumberFormat;
            }
            if (formatType == typeof(DateTimeFormatInfo))
            {
                return DateTimeFormat;
            }

            return null;
        }

        public virtual bool IsNeutralCulture => _cultureData.IsNeutralCulture;

        public CultureTypes CultureTypes
        {
            get
            {
                CultureTypes types = 0;

                if (_cultureData.IsNeutralCulture)
                {
                    types |= CultureTypes.NeutralCultures;
                }
                else
                {
                    types |= CultureTypes.SpecificCultures;
                }

                types |= _cultureData.IsWin32Installed ? CultureTypes.InstalledWin32Cultures : 0;

                // Disable  warning 618: System.Globalization.CultureTypes.FrameworkCultures' is obsolete
#pragma warning disable 618
                types |= _cultureData.IsFramework ? CultureTypes.FrameworkCultures : 0;
#pragma warning restore 618

                types |= _cultureData.IsSupplementalCustomCulture ? CultureTypes.UserCustomCulture : 0;
                types |= _cultureData.IsReplacementCulture ? CultureTypes.ReplacementCultures | CultureTypes.UserCustomCulture : 0;

                return types;
            }
        }

        public virtual NumberFormatInfo NumberFormat
        {
            get
            {
                if (_numInfo == null)
                {
                    NumberFormatInfo temp = new NumberFormatInfo(_cultureData);
                    temp._isReadOnly = _isReadOnly;
                    Interlocked.CompareExchange(ref _numInfo, temp, null);
                }
                return _numInfo!;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                VerifyWritable();
                _numInfo = value;
            }
        }

        /// <summary>
        /// Create a DateTimeFormatInfo, and fill in the properties according to
        /// the CultureID.
        /// </summary>
        public virtual DateTimeFormatInfo DateTimeFormat
        {
            get
            {
                if (_dateTimeInfo == null)
                {
                    // Change the calendar of DTFI to the specified calendar of this CultureInfo.
                    DateTimeFormatInfo temp = new DateTimeFormatInfo(_cultureData, this.Calendar);
                    temp._isReadOnly = _isReadOnly;
                    Interlocked.CompareExchange(ref _dateTimeInfo, temp, null);
                }
                return _dateTimeInfo!;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                VerifyWritable();
                _dateTimeInfo = value;
            }
        }

        public void ClearCachedData()
        {
            // reset the default culture values
            s_userDefaultCulture = GetUserDefaultCulture();
            s_userDefaultUICulture = GetUserDefaultUICulture();

            RegionInfo.s_currentRegionInfo = null;
#pragma warning disable 0618 // disable the obsolete warning
            TimeZone.ResetTimeZone();
#pragma warning restore 0618
            TimeZoneInfo.ClearCachedData();
            s_LcidCachedCultures = null;
            s_NameCachedCultures = null;

            CultureData.ClearCachedData();
        }

        /// <summary>
        /// Map a Win32 CALID to an instance of supported calendar.
        /// </summary>
        /// <remarks>
        /// Shouldn't throw exception since the calType value is from our data
        /// table or from Win32 registry.
        /// If we are in trouble (like getting a weird value from Win32
        /// registry), just return the GregorianCalendar.
        /// </remarks>
        internal static Calendar GetCalendarInstance(CalendarId calType)
        {
            if (calType == CalendarId.GREGORIAN)
            {
                return new GregorianCalendar();
            }

            return GetCalendarInstanceRare(calType);
        }

        /// <summary>
        /// This function exists as a shortcut to prevent us from loading all of the non-gregorian
        /// calendars unless they're required.
        /// </summary>
        internal static Calendar GetCalendarInstanceRare(CalendarId calType)
        {
            Debug.Assert(calType != CalendarId.GREGORIAN, "calType!=CalendarId.GREGORIAN");

            switch (calType)
            {
                case CalendarId.GREGORIAN_US:               // Gregorian (U.S.) calendar
                case CalendarId.GREGORIAN_ME_FRENCH:        // Gregorian Middle East French calendar
                case CalendarId.GREGORIAN_ARABIC:           // Gregorian Arabic calendar
                case CalendarId.GREGORIAN_XLIT_ENGLISH:     // Gregorian Transliterated English calendar
                case CalendarId.GREGORIAN_XLIT_FRENCH:      // Gregorian Transliterated French calendar
                    return new GregorianCalendar((GregorianCalendarTypes)calType);
                case CalendarId.TAIWAN:                     // Taiwan Era calendar
                    return new TaiwanCalendar();
                case CalendarId.JAPAN:                      // Japanese Emperor Era calendar
                    return new JapaneseCalendar();
                case CalendarId.KOREA:                      // Korean Tangun Era calendar
                    return new KoreanCalendar();
                case CalendarId.THAI:                       // Thai calendar
                    return new ThaiBuddhistCalendar();
                case CalendarId.HIJRI:                      // Hijri (Arabic Lunar) calendar
                    return new HijriCalendar();
                case CalendarId.HEBREW:                     // Hebrew (Lunar) calendar
                    return new HebrewCalendar();
                case CalendarId.UMALQURA:
                    return new UmAlQuraCalendar();
                case CalendarId.PERSIAN:
                    return new PersianCalendar();
            }
            return new GregorianCalendar();
        }

        /// <summary>
        /// Return/set the default calendar used by this culture.
        /// This value can be overridden by regional option if this is a current culture.
        /// </summary>
        public virtual Calendar Calendar
        {
            get
            {
                if (_calendar == null)
                {
                    Debug.Assert(_cultureData.CalendarIds.Length > 0, "_cultureData.CalendarIds.Length > 0");
                    // Get the default calendar for this culture.  Note that the value can be
                    // from registry if this is a user default culture.
                    Calendar newObj = _cultureData.DefaultCalendar;

                    Interlocked.MemoryBarrier();
                    newObj.SetReadOnlyState(_isReadOnly);
                    _calendar = newObj;
                }
                return _calendar;
            }
        }

        /// <summary>
        /// Return an array of the optional calendar for this culture.
        /// </summary>
        public virtual Calendar[] OptionalCalendars
        {
            get
            {
                // This property always returns a new copy of the calendar array.
                CalendarId[] calID = _cultureData.CalendarIds;
                Calendar[] cals = new Calendar[calID.Length];
                for (int i = 0; i < cals.Length; i++)
                {
                    cals[i] = GetCalendarInstance(calID[i]);
                }
                return cals;
            }
        }

        public bool UseUserOverride => _cultureData.UseUserOverride;

        public CultureInfo GetConsoleFallbackUICulture()
        {
            CultureInfo? temp = _consoleFallbackCulture;
            if (temp == null)
            {
                temp = CreateSpecificCulture(_cultureData.SCONSOLEFALLBACKNAME);
                temp._isReadOnly = true;
                _consoleFallbackCulture = temp;
            }
            return temp;
        }

        public virtual object Clone()
        {
            CultureInfo ci = (CultureInfo)MemberwiseClone();
            ci._isReadOnly = false;

            // If this is exactly our type, we can make certain optimizations so that we don't allocate NumberFormatInfo or DTFI unless
            // they've already been allocated.  If this is a derived type, we'll take a more generic codepath.
            if (!_isInherited)
            {
                if (_dateTimeInfo != null)
                {
                    ci._dateTimeInfo = (DateTimeFormatInfo)_dateTimeInfo.Clone();
                }
                if (_numInfo != null)
                {
                    ci._numInfo = (NumberFormatInfo)_numInfo.Clone();
                }
            }
            else
            {
                ci.DateTimeFormat = (DateTimeFormatInfo)this.DateTimeFormat.Clone();
                ci.NumberFormat = (NumberFormatInfo)this.NumberFormat.Clone();
            }

            if (_textInfo != null)
            {
                ci._textInfo = (TextInfo)_textInfo.Clone();
            }

            if (_calendar != null)
            {
                ci._calendar = (Calendar)_calendar.Clone();
            }

            return ci;
        }

        public static CultureInfo ReadOnly(CultureInfo ci)
        {
            if (ci == null)
            {
                throw new ArgumentNullException(nameof(ci));
            }

            if (ci.IsReadOnly)
            {
                return ci;
            }
            CultureInfo newInfo = (CultureInfo)(ci.MemberwiseClone());

            if (!ci.IsNeutralCulture)
            {
                // If this is exactly our type, we can make certain optimizations so that we don't allocate NumberFormatInfo or DTFI unless
                // they've already been allocated.  If this is a derived type, we'll take a more generic codepath.
                if (!ci._isInherited)
                {
                    if (ci._dateTimeInfo != null)
                    {
                        newInfo._dateTimeInfo = DateTimeFormatInfo.ReadOnly(ci._dateTimeInfo);
                    }
                    if (ci._numInfo != null)
                    {
                        newInfo._numInfo = NumberFormatInfo.ReadOnly(ci._numInfo);
                    }
                }
                else
                {
                    newInfo.DateTimeFormat = DateTimeFormatInfo.ReadOnly(ci.DateTimeFormat);
                    newInfo.NumberFormat = NumberFormatInfo.ReadOnly(ci.NumberFormat);
                }
            }

            if (ci._textInfo != null)
            {
                newInfo._textInfo = TextInfo.ReadOnly(ci._textInfo);
            }

            if (ci._calendar != null)
            {
                newInfo._calendar = Calendar.ReadOnly(ci._calendar);
            }

            // Don't set the read-only flag too early.
            // We should set the read-only flag here.  Otherwise, info.DateTimeFormat will not be able to set.
            newInfo._isReadOnly = true;

            return newInfo;
        }


        public bool IsReadOnly => _isReadOnly;

        private void VerifyWritable()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        /// <summary>
        /// For resource lookup, we consider a culture the invariant culture by name equality.
        /// We perform this check frequently during resource lookup, so adding a property for
        /// improved readability.
        /// </summary>
        internal bool HasInvariantCultureName
        {
            get => Name == CultureInfo.InvariantCulture.Name;
        }

        /// <summary>
        /// Helper function overloads of GetCachedReadOnlyCulture.  If lcid is 0, we use the name.
        /// If lcid is -1, use the altName and create one of those special SQL cultures.
        /// </summary>
        internal static CultureInfo? GetCultureInfoHelper(int lcid, string? name, string? altName)
        {
            // retval is our return value.
            CultureInfo? retval;

            // Temporary hashtable for the names.
            Dictionary<string, CultureInfo>? tempNameHT = s_NameCachedCultures;

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
                tempNameHT = new Dictionary<string, CultureInfo>();
            }
            else
            {
                // If we are called by name, check if the object exists in the hashtable.  If so, return it.
                if (lcid == -1 || lcid == 0)
                {
                    Debug.Assert(name != null && (lcid != -1 || altName != null));
                    bool ret;
                    lock (_lock)
                    {
                        ret = tempNameHT.TryGetValue(lcid == 0 ? name! : name! + '\xfffd' + altName!, out retval);
                    }

                    if (ret && retval != null)
                    {
                        return retval;
                    }
                }
            }

            // Next, the Lcid table.
            Dictionary<int, CultureInfo>? tempLcidHT = s_LcidCachedCultures;

            if (tempLcidHT == null)
            {
                // Case insensitive is not an issue here, save the constructor call.
                tempLcidHT = new Dictionary<int, CultureInfo>();
            }
            else
            {
                // If we were called by Lcid, check if the object exists in the table.  If so, return it.
                if (lcid > 0)
                {
                    bool ret;
                    lock (_lock)
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
                        Debug.Assert(name != null && altName != null);
                        retval = new CultureInfo(name!, altName!);
                        break;

                    case 0:
                        Debug.Assert(name != null);
                        retval = new CultureInfo(name!, false);
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
            retval._isReadOnly = true;

            if (lcid == -1)
            {
                lock (_lock)
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
                string newName = CultureData.AnsiToLower(retval._name);

                // We add this new culture info object to both tables.
                lock (_lock)
                {
                    tempNameHT[newName] = retval;
                }
            }
            else
            {
                lock (_lock)
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

        /// <summary>
        /// Gets a cached copy of the specified culture from an internal
        /// hashtable (or creates it if not found). (LCID version)
        /// </summary>
        public static CultureInfo GetCultureInfo(int culture)
        {
            // Must check for -1 now since the helper function uses the value to signal
            // the altCulture code path for SQL Server.
            // Also check for zero as this would fail trying to add as a key to the hash.
            if (culture <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(culture), SR.ArgumentOutOfRange_NeedPosNum);
            }
            CultureInfo? retval = GetCultureInfoHelper(culture, null, null);
            if (null == retval)
            {
                throw new CultureNotFoundException(nameof(culture), culture, SR.Argument_CultureNotSupported);
            }
            return retval;
        }

        /// <summary>
        /// Gets a cached copy of the specified culture from an internal
        /// hashtable (or creates it if not found). (Named version)
        /// </summary>
        public static CultureInfo GetCultureInfo(string name)
        {
            // Make sure we have a valid, non-zero length string as name
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            CultureInfo? retval = GetCultureInfoHelper(0, name, null);
            if (retval == null)
            {
                throw new CultureNotFoundException(
                    nameof(name), name, SR.Argument_CultureNotSupported);
            }
            return retval;
        }

        /// <summary>
        /// Gets a cached copy of the specified culture from an internal
        /// hashtable (or creates it if not found).
        /// </summary>
        public static CultureInfo GetCultureInfo(string name, string altName)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (altName == null)
            {
                throw new ArgumentNullException(nameof(altName));
            }

            CultureInfo? retval = GetCultureInfoHelper(-1, name, altName);
            if (retval == null)
            {
                throw new CultureNotFoundException("name or altName",
                                        SR.Format(SR.Argument_OneOfCulturesNotSupported, name, altName));
            }
            return retval;
        }

        public static CultureInfo GetCultureInfoByIetfLanguageTag(string name)
        {
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
