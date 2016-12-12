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
    using System.Diagnostics;
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
        public RegionInfo(String name) {
            if (name==null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) //The InvariantCulture has no matching region
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_NoRegionInvariantCulture"), nameof(name));
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
                        Environment.GetResourceString("Argument_InvalidCultureName"), name), nameof(name));


            // Not supposed to be neutral
            if (this.m_cultureData.IsNeutralCulture)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNeutralRegionName", name), nameof(name));

            SetName(name);
        }

#if FEATURE_USE_LCID
        // We'd rather people use the named version since this doesn't allow custom locales
        public RegionInfo(int culture)
        {
            if (culture == CultureInfo.LOCALE_INVARIANT) //The InvariantCulture has no matching region
            { 
                throw new ArgumentException(Environment.GetResourceString("Argument_NoRegionInvariantCulture"));
            }
            
            if (culture == CultureInfo.LOCALE_NEUTRAL)
            {
                // Not supposed to be neutral
                throw new ArgumentException(Environment.GetResourceString("Argument_CultureIsNeutral", culture), nameof(culture));
            }

            if (culture == CultureInfo.LOCALE_CUSTOM_DEFAULT)
            {
                // Not supposed to be neutral
                throw new ArgumentException(Environment.GetResourceString("Argument_CustomCultureCannotBePassedByNumber", culture), nameof(culture));
            }
            
            this.m_cultureData = CultureData.GetCultureData(culture,true);
            this.m_name = this.m_cultureData.SREGIONNAME;

            if (this.m_cultureData.IsNeutralCulture)
            {
                // Not supposed to be neutral
                throw new ArgumentException(Environment.GetResourceString("Argument_CultureIsNeutral", culture), nameof(culture));
            }
            m_cultureId = culture;
        }
#endif
        
        internal RegionInfo(CultureData cultureData)
        {
            this.m_cultureData = cultureData;
            this.m_name = this.m_cultureData.SREGIONNAME;
        }

        private void SetName(string name)
        {
            // Use the name of the region we found
            this.m_name = this.m_cultureData.SREGIONNAME;
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

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            // This won't happen anyway since CoreCLR doesn't support serialization
            this.m_cultureData = CultureData.GetCultureData(m_name, true);

            if (this.m_cultureData == null)
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Argument_InvalidCultureName"), m_name), nameof(m_name));

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
                Debug.Assert(m_name != null, "Expected RegionInfo.m_name to be populated already");
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
            get
            {
                return (this.m_cultureData.SISO3166CTRYNAME);
            }
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  ThreeLetterISORegionName
        //
        //  Returns the three letter ISO region name (ie: USA)
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String ThreeLetterISORegionName
        {
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
            get
            {
                return (this.m_cultureData.SABBREVCTRYNAME);
            }
        }

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
