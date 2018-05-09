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

using System.Diagnostics;

namespace System.Globalization
{
    public class RegionInfo
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
        internal String _name;

        //
        // The CultureData instance that we are going to read data from.
        //
        internal CultureData _cultureData;

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
        public RegionInfo(String name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) //The InvariantCulture has no matching region
            {
                throw new ArgumentException(SR.Argument_NoRegionInvariantCulture, nameof(name));
            }


            //
            // For CoreCLR we only want the region names that are full culture names
            //
            _cultureData = CultureData.GetCultureDataForRegion(name, true);
            if (_cultureData == null)
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        SR.Argument_InvalidCultureName, name), nameof(name));


            // Not supposed to be neutral
            if (_cultureData.IsNeutralCulture)
                throw new ArgumentException(SR.Format(SR.Argument_InvalidNeutralRegionName, name), nameof(name));

            SetName(name);
        }

        public RegionInfo(int culture)
        {
            if (culture == CultureInfo.LOCALE_INVARIANT) //The InvariantCulture has no matching region
            { 
                throw new ArgumentException(SR.Argument_NoRegionInvariantCulture);
            }

            if (culture == CultureInfo.LOCALE_NEUTRAL)
            {
                // Not supposed to be neutral
                throw new ArgumentException(SR.Format(SR.Argument_CultureIsNeutral, culture), nameof(culture));
            }

            if (culture == CultureInfo.LOCALE_CUSTOM_DEFAULT)
            {
                // Not supposed to be neutral
                throw new ArgumentException(SR.Format(SR.Argument_CustomCultureCannotBePassedByNumber, culture), nameof(culture));
            }
            
            _cultureData = CultureData.GetCultureData(culture, true);
            _name = _cultureData.SREGIONNAME;

            if (_cultureData.IsNeutralCulture)
            {
                // Not supposed to be neutral
                throw new ArgumentException(SR.Format(SR.Argument_CultureIsNeutral, culture), nameof(culture));
            }
        }

        internal RegionInfo(CultureData cultureData)
        {
            _cultureData = cultureData;
            _name = _cultureData.SREGIONNAME;
        }

        private void SetName(string name)
        {
            // Use the name of the region we found
            _name = _cultureData.SREGIONNAME;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  GetCurrentRegion
        //
        //  This instance provides methods based on the current user settings.
        //  These settings are volatile and may change over the lifetime of the
        //  thread.
        //
        ////////////////////////////////////////////////////////////////////////
        public static RegionInfo CurrentRegion
        {
            get
            {
                RegionInfo temp = s_currentRegionInfo;
                if (temp == null)
                {
                    temp = new RegionInfo(CultureInfo.CurrentCulture._cultureData);

                    // Need full name for custom cultures
                    temp._name = temp._cultureData.SREGIONNAME;
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
        public virtual String Name
        {
            get
            {
                Debug.Assert(_name != null, "Expected RegionInfo._name to be populated already");
                return (_name);
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
                return (_cultureData.SENGCOUNTRY);
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
                return (_cultureData.SLOCALIZEDCOUNTRY);
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
        public virtual String NativeName
        {
            get
            {
                return (_cultureData.SNATIVECOUNTRY);
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
                return (_cultureData.SISO3166CTRYNAME);
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
                return (_cultureData.SISO3166CTRYNAME2);
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
                // ThreeLetterWindowsRegionName is really same as ThreeLetterISORegionName 
                return ThreeLetterISORegionName;
            }
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //  IsMetric
        //
        //  Returns true if this region uses the metric measurement system
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual bool IsMetric
        {
            get
            {
                int value = _cultureData.IMEASURE;
                return (value == 0);
            }
        }

        public virtual int GeoId 
        {
            get
            {
                return (_cultureData.IGEOID);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CurrencyEnglishName
        //
        //  English name for this region's currency, ie: Swiss Franc
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual string CurrencyEnglishName
        {
            get
            {
                return (_cultureData.SENGLISHCURRENCY);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CurrencyNativeName
        //
        //  Native name for this region's currency, ie: Schweizer Franken
        //  WARNING: You need a full locale name for this to make sense.
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual string CurrencyNativeName
        {
            get
            {
                return (_cultureData.SNATIVECURRENCY);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CurrencySymbol
        //
        //  Currency Symbol for this locale, ie: Fr. or $
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String CurrencySymbol
        {
            get
            {
                return (_cultureData.SCURRENCY);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  ISOCurrencySymbol
        //
        //  ISO Currency Symbol for this locale, ie: CHF
        //
        ////////////////////////////////////////////////////////////////////////
        public virtual String ISOCurrencySymbol
        {
            get
            {
                return (_cultureData.SINTLSYMBOL);
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
