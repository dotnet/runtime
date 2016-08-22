// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


////////////////////////////////////////////////////////////////////////////
//
//
//  Purpose:  This class represents settings specified by de jure or
//            de facto standards for a particular country/region.  In
//            contrast to CultureInfo, the RegionInfo does not represent
//            preferences of the user and does not depend on the user's
//            language or culture.
//
//
////////////////////////////////////////////////////////////////////////////

namespace System.Globalization {

    using System;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    [Serializable] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public partial class RegionInfo
    {
        //--------------------------------------------------------------------//
        //                        Internal Information                        //
        //--------------------------------------------------------------------//

        //
        //  Variables.
        //

        //
        // Name of this region (ie: es-US): serialized, the field used for deserialization
        //
        internal String m_name;

        //
        // The CultureData instance that we are going to read data from.
        //
        [NonSerialized]internal CultureData m_cultureData;

        //
        // The RegionInfo for our current region
        //
        internal static volatile RegionInfo s_currentRegionInfo;


        ////////////////////////////////////////////////////////////////////////
        //
        //  RegionInfo Constructors
        //
        //  Note: We prefer that a region be created with a full culture name (ie: en-US)
        //  because otherwise the native strings won't be right.
        //
        //  In Silverlight we enforce that RegionInfos must be created with a full culture name
        //
        ////////////////////////////////////////////////////////////////////////
        [System.Security.SecuritySafeCritical]  // auto-generated
        public RegionInfo(String name) {
            if (name==null)
                throw new ArgumentNullException("name");

            if (name.Length == 0) //The InvariantCulture has no matching region
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_NoRegionInvariantCulture"), "name");
            }
            
            Contract.EndContractBlock();

            //
            // First try it as an entire culture. We must have user override as true here so
            // that we can pick up custom cultures *before* built-in ones (if they want to
            // prefer built-in cultures they will pass "us" instead of "en-US").
            //
            this.m_cultureData = CultureData.GetCultureDataForRegion(name,true);
            // this.m_name = name.ToUpper(CultureInfo.InvariantCulture);

            if (this.m_cultureData == null)
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Argument_InvalidCultureName"), name), "name");


            // Not supposed to be neutral
            if (this.m_cultureData.IsNeutralCulture)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNeutralRegionName", name), "name");

            SetName(name);
        }

#if FEATURE_USE_LCID
        // We'd rather people use the named version since this doesn't allow custom locales
        [System.Security.SecuritySafeCritical]  // auto-generated
        public RegionInfo(int culture)
        {
            if (culture == CultureInfo.LOCALE_INVARIANT) //The InvariantCulture has no matching region
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_NoRegionInvariantCulture"));
            }
            
            if (culture == CultureInfo.LOCALE_NEUTRAL)
            {
                // Not supposed to be neutral
                throw new ArgumentException(Environment.GetResourceString("Argument_CultureIsNeutral", culture), "culture");
            }

            if (culture == CultureInfo.LOCALE_CUSTOM_DEFAULT)
            {
                // Not supposed to be neutral
                throw new ArgumentException(Environment.GetResourceString("Argument_CustomCultureCannotBePassedByNumber", culture), "culture");
            }
            
            this.m_cultureData = CultureData.GetCultureData(culture,true);
            this.m_name = this.m_cultureData.SREGIONNAME;

            if (this.m_cultureData.IsNeutralCulture)
            {
                // Not supposed to be neutral
                throw new ArgumentException(Environment.GetResourceString("Argument_CultureIsNeutral", culture), "culture");
            }
            m_cultureId = culture;
        }
#endif
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal RegionInfo(CultureData cultureData)
        {
            this.m_cultureData = cultureData;
            this.m_name = this.m_cultureData.SREGIONNAME;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void SetName(string name)
        {
#if FEATURE_CORECLR
            // Use the name of the region we found
            this.m_name = this.m_cultureData.SREGIONNAME;
#else
            // when creating region by culture name, we keep the region name as the culture name so regions
            // created by custom culture names can be differentiated from built in regions.
            this.m_name = name.Equals(this.m_cultureData.SREGIONNAME, StringComparison.OrdinalIgnoreCase) ? 
                                this.m_cultureData.SREGIONNAME : 
                                this.m_cultureData.CultureName;
#endif // FEATURE_CORECLR
        }
        

#region Serialization 
        //
        //  m_cultureId is needed for serialization only to detect the case if the region info is created using the name or using the LCID.
        //  in case m_cultureId is zero means that the RigionInfo is created using name. otherwise it is created using LCID.
        //

        [OptionalField(VersionAdded = 2)]
        int m_cultureId;
        // the following field is defined to keep the compatibility with Everett.
        // don't change/remove the names/types of these field.
        [OptionalField(VersionAdded = 2)]
        internal int m_dataItem = 0;

#if !FEATURE_CORECLR
        static private readonly int[] IdFromEverettRegionInfoDataItem =
        {
            0x3801, /*  0 */  // AE          ar-AE      Arabic (U.A.E.)
            0x041C, /*  1 */  // AL          sq-AL      Albanian (Albania)
            0x042B, /*  2 */  // AM          hy-AM      Armenian (Armenia)
            0x2C0A, /*  3 */  // AR          es-AR      Spanish (Argentina)
            0x0C07, /*  4 */  // AT          de-AT      German (Austria)
            0x0C09, /*  5 */  // AU          en-AU      English (Australia)
            0x042C, /*  6 */  // AZ          az-AZ-Latn Azeri (Latin) (Azerbaijan)
        //  0x082C,     6,    // AZ          az-AZ-Cyrl Azeri (Cyrillic) (Azerbaijan)
            0x080C, /*  7 */  // BE          fr-BE      French (Belgium)
        //  0x0813,     7,    // BE          nl-BE      Dutch (Belgium)
            0x0402, /*  8 */  // BG          bg-BG      Bulgarian (Bulgaria)
            0x3C01, /*  9 */  // BH          ar-BH      Arabic (Bahrain)
            0x083E, /* 10 */  // BN          ms-BN      Malay (Brunei Darussalam)
            0x400A, /* 11 */  // BO          es-BO      Spanish (Bolivia)
            0x0416, /* 12 */  // BR          pt-BR      Portuguese (Brazil)
            0x0423, /* 13 */  // BY          be-BY      Belarusian (Belarus)
            0x2809, /* 14 */  // BZ          en-BZ      English (Belize)
            0x0C0C, /* 15 */  // CA          fr-CA      French (Canada)
        //  0x1009,    15,    // CA          en-CA      English (Canada)
            0x2409, /* 16 */  // CB          en-CB      English (Caribbean)
            0x0807, /* 17 */  // CH          de-CH      German (Switzerland)
        //  0x0810,    17,    // CH          it-CH      Italian (Switzerland)
        //  0x100C,    17,    // CH          fr-CH      French (Switzerland)
            0x340A, /* 18 */  // CL          es-CL      Spanish (Chile)
            0x0804, /* 19 */  // CN          zh-CN      Chinese (People's Republic of China)
            0x240A, /* 20 */  // CO          es-CO      Spanish (Colombia)
            0x140A, /* 21 */  // CR          es-CR      Spanish (Costa Rica)
            0x0405, /* 22 */  // CZ          cs-CZ      Czech (Czech Republic)
            0x0407, /* 23 */  // DE          de-DE      German (Germany)
            0x0406, /* 24 */  // DK          da-DK      Danish (Denmark)
            0x1C0A, /* 25 */  // DO          es-DO      Spanish (Dominican Republic)
            0x1401, /* 26 */  // DZ          ar-DZ      Arabic (Algeria)
            0x300A, /* 27 */  // EC          es-EC      Spanish (Ecuador)
            0x0425, /* 28 */  // EE          et-EE      Estonian (Estonia)
            0x0C01, /* 29 */  // EG          ar-EG      Arabic (Egypt)
            0x0403, /* 30 */  // ES          ca-ES      Catalan (Catalan)
        //  0x042D,    30,    // ES          eu-ES      Basque (Basque)
        //  0x0456,    30,    // ES          gl-ES      Galician (Galician)
        //  0x0C0A,    30,    // ES          es-ES      Spanish (Spain)
            0x040B, /* 31 */  // FI          fi-FI      Finnish (Finland)
        //  0x081D,    31,    // FI          sv-FI      Swedish (Finland)
            0x0438, /* 32 */  // FO          fo-FO      Faroese (Faroe Islands)
            0x040C, /* 33 */  // FR          fr-FR      French (France)
            0x0809, /* 34 */  // GB          en-GB      English (United Kingdom)
            0x0437, /* 35 */  // GE          ka-GE      Georgian (Georgia)
            0x0408, /* 36 */  // GR          el-GR      Greek (Greece)
            0x100A, /* 37 */  // GT          es-GT      Spanish (Guatemala)
            0x0C04, /* 38 */  // HK          zh-HK      Chinese (Hong Kong S.A.R.)
            0x480A, /* 39 */  // HN          es-HN      Spanish (Honduras)
            0x041A, /* 40 */  // HR          hr-HR      Croatian (Croatia)
            0x040E, /* 41 */  // HU          hu-HU      Hungarian (Hungary)
            0x0421, /* 42 */  // ID          id-ID      Indonesian (Indonesia)
            0x1809, /* 43 */  // IE          en-IE      English (Ireland)
            0x040D, /* 44 */  // IL          he-IL      Hebrew (Israel)
            0x0439, /* 45 */  // IN          hi-IN      Hindi (India)
        //  0x0446,    45,    // IN          pa-IN      Punjabi (India)
        //  0x0447,    45,    // IN          gu-IN      Gujarati (India)
        //  0x0449,    45,    // IN          ta-IN      Tamil (India)
        //  0x044A,    45,    // IN          te-IN      Telugu (India)
        //  0x044B,    45,    // IN          kn-IN      Kannada (India)
        //  0x044E,    45,    // IN          mr-IN      Marathi (India)
        //  0x044F,    45,    // IN          sa-IN      Sanskrit (India)
        //  0x0457,    45,    // IN          kok-IN     Konkani (India)
            0x0801, /* 46 */  // IQ          ar-IQ      Arabic (Iraq)
            0x0429, /* 47 */  // IR          fa-IR      (Iran)
            0x040F, /* 48 */  // IS          is-IS      Icelandic (Iceland)
            0x0410, /* 49 */  // IT          it-IT      Italian (Italy)
            0x2009, /* 50 */  // JM          en-JM      English (Jamaica)
            0x2C01, /* 51 */  // JO          ar-JO      Arabic (Jordan)
            0x0411, /* 52 */  // JP          ja-JP      Japanese (Japan)
            0x0441, /* 53 */  // KE          sw-KE      Swahili (Kenya)
            0x0440, /* 54 */  // KG          ky-KG      Kyrgyz (Kyrgyzstan)
            0x0412, /* 55 */  // KR          ko-KR      Korean (Korea)
            0x3401, /* 56 */  // KW          ar-KW      Arabic (Kuwait)
            0x043F, /* 57 */  // KZ          kk-KZ      Kazakh (Kazakhstan)
            0x3001, /* 58 */  // LB          ar-LB      Arabic (Lebanon)
            0x1407, /* 59 */  // LI          de-LI      German (Liechtenstein)
            0x0427, /* 60 */  // LT          lt-LT      Lithuanian (Lithuania)
            0x1007, /* 61 */  // LU          de-LU      German (Luxembourg)
        //  0x140C,    61,    // LU          fr-LU      French (Luxembourg)
            0x0426, /* 62 */  // LV          lv-LV      Latvian (Latvia)
            0x1001, /* 63 */  // LY          ar-LY      Arabic (Libya)
            0x1801, /* 64 */  // MA          ar-MA      Arabic (Morocco)
            0x180C, /* 65 */  // MC          fr-MC      French (Principality of Monaco)
            0x042F, /* 66 */  // MK          mk-MK      Macedonian (Macedonia, FYRO)
            0x0450, /* 67 */  // MN          mn-MN      Mongolian (Mongolia)
            0x1404, /* 68 */  // MO          zh-MO      Chinese (Macau S.A.R.)
            0x0465, /* 69 */  // MV          div-MV     Divehi (Maldives)
            0x080A, /* 70 */  // MX          es-MX      Spanish (Mexico)
            0x043E, /* 71 */  // MY          ms-MY      Malay (Malaysia)
            0x4C0A, /* 72 */  // NI          es-NI      Spanish (Nicaragua)
            0x0413, /* 73 */  // NL          nl-NL      Dutch (Netherlands)
            0x0414, /* 74 */  // NO          nb-NO      Norwegian (Bokm?) (Norway)
        //  0x0814,    74,    // NO          nn-NO      Norwegian (Nynorsk) (Norway)
            0x1409, /* 75 */  // NZ          en-NZ      English (New Zealand)
            0x2001, /* 76 */  // OM          ar-OM      Arabic (Oman)
            0x180A, /* 77 */  // PA          es-PA      Spanish (Panama)
            0x280A, /* 78 */  // PE          es-PE      Spanish (Peru)
            0x3409, /* 79 */  // PH          en-PH      English (Republic of the Philippines)
            0x0420, /* 80 */  // PK          ur-PK      Urdu (Islamic Republic of Pakistan)
            0x0415, /* 81 */  // PL          pl-PL      Polish (Poland)
            0x500A, /* 82 */  // PR          es-PR      Spanish (Puerto Rico)
            0x0816, /* 83 */  // PT          pt-PT      Portuguese (Portugal)
            0x3C0A, /* 84 */  // PY          es-PY      Spanish (Paraguay)
            0x4001, /* 85 */  // QA          ar-QA      Arabic (Qatar)
            0x0418, /* 86 */  // RO          ro-RO      Romanian (Romania)
            0x0419, /* 87 */  // RU          ru-RU      Russian (Russia)
        //  0x0444,    87,    // RU          tt-RU      Tatar (Russia)
            0x0401, /* 88 */  // SA          ar-SA      Arabic (Saudi Arabia)
            0x041D, /* 89 */  // SE          sv-SE      Swedish (Sweden)
            0x1004, /* 90 */  // SG          zh-SG      Chinese (Singapore)
            0x0424, /* 91 */  // SI          sl-SI      Slovenian (Slovenia)
            0x041B, /* 92 */  // SK          sk-SK      Slovak (Slovakia)
            0x081A, /* 93 */  // SP          sr-SP-Latn Serbian (Latin) (Serbia)
        //  0x0C1A,    93,    // SP          sr-SP-Cyrl Serbian (Cyrillic) (Serbia)
            0x440A, /* 94 */  // SV          es-SV      Spanish (El Salvador)
            0x045A, /* 95 */  // SY          syr-SY     Syriac (Syria)
        //  0x2801,    95,    // SY          ar-SY      Arabic (Syria)
            0x041E, /* 96 */  // TH          th-TH      Thai (Thailand)
            0x1C01, /* 97 */  // TN          ar-TN      Arabic (Tunisia)
            0x041F, /* 98 */  // TR          tr-TR      Turkish (Turkey)
            0x2C09, /* 99 */  // TT          en-TT      English (Trinidad and Tobago)
            0x0404, /*100 */  // TW          zh-TW      Chinese (Taiwan)
            0x0422, /*101 */  // UA          uk-UA      Ukrainian (Ukraine)
            0x0409, /*102 */  // US          en-US      English (United States)
            0x380A, /*103 */  // UY          es-UY      Spanish (Uruguay)
            0x0443, /*104 */  // UZ          uz-UZ-Latn Uzbek (Latin) (Uzbekistan)
        //  0x0843,   104     // UZ          uz-UZ-Cyrl Uzbek (Cyrillic) (Uzbekistan)
            0x200A, /*105*/   // VE          es-VE      Spanish (Venezuela)
            0x042A, /*106*/   // VN          vi-VN      Vietnamese (Viet Nam)
            0x2401, /*107*/   // YE          ar-YE      Arabic (Yemen)
            0x0436, /*108*/   // ZA          af-ZA      Afrikaans (South Africa)
        //  0x1C09,   108,    // ZA          en-ZA      English (South Africa)
            0x3009, /*109*/   // ZW          en-ZW      English (Zimbabwe)
        };
#endif
        [System.Security.SecurityCritical]  // auto-generated
        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
#if FEATURE_CORECLR
            // This won't happen anyway since CoreCLR doesn't support serialization
            this.m_cultureData = CultureData.GetCultureData(m_name, true);
#else
            if (m_name == null)
            {
                Contract.Assert(m_dataItem >= 0, "[RegionInfo.OnDeserialized] null name and invalid dataItem");
                m_cultureId = IdFromEverettRegionInfoDataItem[m_dataItem];
            }

            if (m_cultureId == 0)
            {
                this.m_cultureData = CultureData.GetCultureDataForRegion(this.m_name, true);
            }
            else
            {
                this.m_cultureData = CultureData.GetCultureData(m_cultureId, true);
            }
                
#endif
            if (this.m_cultureData == null)
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Argument_InvalidCultureName"), m_name), "m_name");

            if (m_cultureId == 0)
            {
                SetName(this.m_name);
            }
            else
            {
                this.m_name = this.m_cultureData.SREGIONNAME;
            }
        }

        [OnSerializing] 
        private void OnSerializing(StreamingContext ctx) 
        { 
            // Used to fill in everett data item, unnecessary now
        }   
#endregion Serialization

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetCurrentRegion
        //
        //  This instance provides methods based on the current user settings.
        //  These settings are volatile and may change over the lifetime of the
        //  thread.
        //
        ////////////////////////////////////////////////////////////////////////
        public static RegionInfo CurrentRegion {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                RegionInfo temp = s_currentRegionInfo;
                if (temp == null)
                {
                    temp = new RegionInfo(CultureInfo.CurrentCulture.m_cultureData);

                    // Need full name for custom cultures
                    temp.m_name=temp.m_cultureData.SREGIONNAME;
                    s_currentRegionInfo = temp;
                }
                return temp;
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetName
        //
        //  Returns the name of the region (ie: en-US)
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String Name {
            get {
                Contract.Assert(m_name != null, "Expected RegionInfo.m_name to be populated already");
                return (m_name);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetEnglishName
        //
        //  Returns the name of the region in English. (ie: United States)
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String EnglishName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return (this.m_cultureData.SENGCOUNTRY);
            }
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  GetDisplayName
        //
        //  Returns the display name (localized) of the region. (ie: United States
        //  if the current UI language is en-US)
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String DisplayName 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get 
            {
                return (this.m_cultureData.SLOCALIZEDCOUNTRY);
            }
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  GetNativeName
        //
        //  Returns the native name of the region. (ie: Deutschland)
        //  WARNING: You need a full locale name for this to make sense.        
        //
        ////////////////////////////////////////////////////////////////////////
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual String NativeName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return (this.m_cultureData.SNATIVECOUNTRY);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  TwoLetterISORegionName
        //
        //  Returns the two letter ISO region name (ie: US)
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String TwoLetterISORegionName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return (this.m_cultureData.SISO3166CTRYNAME);
            }
        }


#if !FEATURE_CORECLR
        ////////////////////////////////////////////////////////////////////////
        //
        //  ThreeLetterISORegionName
        //
        //  Returns the three letter ISO region name (ie: USA)
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String ThreeLetterISORegionName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return (this.m_cultureData.SISO3166CTRYNAME2);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ThreeLetterWindowsRegionName
        //
        //  Returns the three letter windows region name (ie: USA)
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String ThreeLetterWindowsRegionName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return (this.m_cultureData.SABBREVCTRYNAME);
            }
        }
#endif

        ////////////////////////////////////////////////////////////////////////
        //
        //  IsMetric
        //
        //  Returns true if this region uses the metric measurement system
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual bool IsMetric {
            get {
                int value = this.m_cultureData.IMEASURE;
                return (value==0);
            }
        }


        [System.Runtime.InteropServices.ComVisible(false)]        
        public virtual int GeoId 
        {
            get 
            {
                return (this.m_cultureData.IGEOID);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CurrencyEnglishName
        //
        //  English name for this region's currency, ie: Swiss Franc
        //
        ////////////////////////////////////////////////////////////////////////
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual String CurrencyEnglishName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return (this.m_cultureData.SENGLISHCURRENCY);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CurrencyEnglishName
        //
        //  English name for this region's currency, ie: Schweizer Franken
        //  WARNING: You need a full locale name for this to make sense.
        //
        ////////////////////////////////////////////////////////////////////////
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual String CurrencyNativeName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return (this.m_cultureData.SNATIVECURRENCY);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CurrencySymbol
        //
        //  Currency Symbol for this locale, ie: Fr. or $
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String CurrencySymbol {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return (this.m_cultureData.SCURRENCY);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ISOCurrencySymbol
        //
        //  ISO Currency Symbol for this locale, ie: CHF
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String ISOCurrencySymbol {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return (this.m_cultureData.SINTLSYMBOL);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  Equals
        //
        //  Implements Object.Equals().  Returns a boolean indicating whether
        //  or not object refers to the same RegionInfo as the current instance.
        //
        //  RegionInfos are considered equal if and only if they have the same name
        //  (ie: en-US)
        //
        ////////////////////////////////////////////////////////////////////////
        public override bool Equals(Object value)
        {
            RegionInfo that = value as RegionInfo;
            if (that != null)
            {
                return this.Name.Equals(that.Name);
            }

            return (false);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetHashCode
        //
        //  Implements Object.GetHashCode().  Returns the hash code for the
        //  CultureInfo.  The hash code is guaranteed to be the same for RegionInfo
        //  A and B where A.Equals(B) is true.
        //
        ////////////////////////////////////////////////////////////////////////
        public override int GetHashCode()
        {
            return (this.Name.GetHashCode());
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  ToString
        //
        //  Implements Object.ToString().  Returns the name of the Region, ie: es-US
        //
        ////////////////////////////////////////////////////////////////////////
        public override String ToString()
        {
            return (Name);
        }    
    }
}
