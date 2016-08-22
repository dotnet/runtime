// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


////////////////////////////////////////////////////////////////////////////
//
//  Class:    RegionInfo
//
//  Purpose:  This class represents settings specified by de jure or
//            de facto standards for a particular country/region.  In
//            contrast to CultureInfo, the RegionInfo does not represent
//            preferences of the user and does not depend on the user's
//            language or culture.
//
//  Date:     March 31, 1999
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Globalization
{
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
        internal CultureData m_cultureData;

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
        public RegionInfo(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (name.Length == 0) //The InvariantCulture has no matching region
            {
                throw new ArgumentException(SR.Argument_NoRegionInvariantCulture, "name");
            }

            Contract.EndContractBlock();

            //
            // For CoreCLR we only want the region names that are full culture names
            //
            this.m_cultureData = CultureData.GetCultureDataForRegion(name, true);
            if (this.m_cultureData == null)
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        SR.Argument_InvalidCultureName, name), "name");


            // Not supposed to be neutral
            if (this.m_cultureData.IsNeutralCulture)
                throw new ArgumentException(SR.Format(SR.Argument_InvalidNeutralRegionName, name), "name");

            SetName(name);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal RegionInfo(CultureData cultureData)
        {
            this.m_cultureData = cultureData;
            this.m_name = this.m_cultureData.SREGIONNAME;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void SetName(string name)
        {
            // Use the name of the region we found
            this.m_name = this.m_cultureData.SREGIONNAME;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx) { }

        [System.Security.SecurityCritical]  // auto-generated
        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            m_cultureData = CultureData.GetCultureData(m_name, true);

            if (m_cultureData == null)
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, SR.Argument_InvalidCultureName, m_name),
                    "m_name");
            }

            m_name = this.m_cultureData.SREGIONNAME;
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
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                RegionInfo temp = s_currentRegionInfo;
                if (temp == null)
                {
                    temp = new RegionInfo(CultureInfo.CurrentCulture.m_cultureData);

                    // Need full name for custom cultures
                    temp.m_name = temp.m_cultureData.SREGIONNAME;
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
                int value = this.m_cultureData.IMEASURE;
                return (value == 0);
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
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
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
        public virtual String ISOCurrencySymbol
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
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
