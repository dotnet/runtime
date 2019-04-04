// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Globalization
{
    /// <summary>
    /// This class represents settings specified by de jure or de facto
    /// standards for a particular country/region. In contrast to
    /// CultureInfo, the RegionInfo does not represent preferences of the
    /// user and does not depend on the user's language or culture.
    /// </summary>
    public class RegionInfo
    {
        // Name of this region (ie: es-US): serialized, the field used for deserialization
        private string _name;

        // The CultureData instance that we are going to read data from.
        private readonly CultureData _cultureData;

        // The RegionInfo for our current region
        internal static volatile RegionInfo? s_currentRegionInfo;

        public RegionInfo(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // The InvariantCulture has no matching region
            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_NoRegionInvariantCulture, nameof(name));
            }

            // For CoreCLR we only want the region names that are full culture names
            _cultureData = CultureData.GetCultureDataForRegion(name, true) ??
                throw new ArgumentException(SR.Format(SR.Argument_InvalidCultureName, name), nameof(name));

            // Not supposed to be neutral
            if (_cultureData.IsNeutralCulture)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidNeutralRegionName, name), nameof(name));
            }

            _name = _cultureData.RegionName;
        }

        public RegionInfo(int culture)
        {
            // The InvariantCulture has no matching region
            if (culture == CultureInfo.LOCALE_INVARIANT)
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
            _name = _cultureData.RegionName;

            if (_cultureData.IsNeutralCulture)
            {
                // Not supposed to be neutral
                throw new ArgumentException(SR.Format(SR.Argument_CultureIsNeutral, culture), nameof(culture));
            }
        }

        internal RegionInfo(CultureData cultureData)
        {
            _cultureData = cultureData;
            _name = _cultureData.RegionName;
        }

        /// <summary>
        /// This instance provides methods based on the current user settings.
        /// These settings are volatile and may change over the lifetime of the
        /// </summary>
        public static RegionInfo CurrentRegion
        {
            get
            {
                RegionInfo? temp = s_currentRegionInfo;
                if (temp == null)
                {
                    temp = new RegionInfo(CultureInfo.CurrentCulture._cultureData);

                    // Need full name for custom cultures
                    temp._name = temp._cultureData.RegionName;
                    s_currentRegionInfo = temp;
                }
                return temp;
            }
        }

        /// <summary>
        /// Returns the name of the region (ie: en-US)
        /// </summary>
        public virtual string Name
        {
            get
            {
                Debug.Assert(_name != null, "Expected RegionInfo._name to be populated already");
                return _name;
            }
        }

        /// <summary>
        /// Returns the name of the region in English. (ie: United States)
        /// </summary>
        public virtual string EnglishName => _cultureData.EnglishCountryName;

        /// <summary>
        /// Returns the display name (localized) of the region. (ie: United States
        /// if the current UI language is en-US)
        /// </summary>
        public virtual string DisplayName => _cultureData.LocalizedCountryName;

        /// <summary>
        ///  Returns the native name of the region. (ie: Deutschland)
        ///  WARNING: You need a full locale name for this to make sense.
        /// </summary>
        public virtual string NativeName => _cultureData.NativeCountryName;

        /// <summary>
        /// Returns the two letter ISO region name (ie: US)
        /// </summary>
        public virtual string TwoLetterISORegionName => _cultureData.TwoLetterISOCountryName;

        /// <summary>
        /// Returns the three letter ISO region name (ie: USA)
        /// </summary>
        public virtual string ThreeLetterISORegionName => _cultureData.ThreeLetterISOCountryName;

        /// <summary>
        /// Returns the three letter windows region name (ie: USA)
        /// </summary>
        public virtual string ThreeLetterWindowsRegionName => ThreeLetterISORegionName;


        /// <summary>
        /// Returns true if this region uses the metric measurement system
        /// </summary>
        public virtual bool IsMetric => _cultureData.MeasurementSystem == 0;

        public virtual int GeoId => _cultureData.GeoId;

        /// <summary>
        /// English name for this region's currency, ie: Swiss Franc
        /// </summary>
        public virtual string CurrencyEnglishName => _cultureData.CurrencyEnglishName;

        /// <summary>
        /// Native name for this region's currency, ie: Schweizer Franken
        /// WARNING: You need a full locale name for this to make sense.
        /// </summary>
        public virtual string CurrencyNativeName => _cultureData.CurrencyNativeName;

        /// <summary>
        /// Currency Symbol for this locale, ie: Fr. or $
        /// </summary>
        public virtual string CurrencySymbol => _cultureData.CurrencySymbol;

        /// <summary>
        /// ISO Currency Symbol for this locale, ie: CHF
        /// </summary>
        public virtual string ISOCurrencySymbol => _cultureData.ISOCurrencySymbol;

        /// <summary>
        /// Implements Object.Equals().  Returns a boolean indicating whether
        /// or not object refers to the same RegionInfo as the current instance.
        /// RegionInfos are considered equal if and only if they have the same name
        /// (ie: en-US)
        /// </summary>
        public override bool Equals(object? value)
        {
            return value is RegionInfo otherRegion
                && Name.Equals(otherRegion.Name);
        }

        public override int GetHashCode() => Name.GetHashCode();

        public override string ToString() => Name;
    }
}
