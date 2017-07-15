// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.DirectoryServices.Interop;
using System.ComponentModel;

namespace System.DirectoryServices
{
    public class DirectoryEntryConfiguration
    {
        private readonly DirectoryEntry _entry;
        private const int ISC_RET_MUTUAL_AUTH = 0x00000002;

        internal DirectoryEntryConfiguration(DirectoryEntry entry)
        {
            _entry = entry;
        }

        public ReferralChasingOption Referral
        {
            get
            {
                return (ReferralChasingOption)((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).GetOption((int)AdsOptions.ADS_OPTION_REFERRALS);
            }
            set
            {
                if (value != ReferralChasingOption.None &&
                    value != ReferralChasingOption.Subordinate &&
                    value != ReferralChasingOption.External &&
                    value != ReferralChasingOption.All)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ReferralChasingOption));

                ((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).SetOption((int)AdsOptions.ADS_OPTION_REFERRALS, value);
            }
        }

        public SecurityMasks SecurityMasks
        {
            get
            {
                return (SecurityMasks)((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).GetOption((int)AdsOptions.ADS_OPTION_SECURITY_MASK);
            }
            set
            {
                if (value > (SecurityMasks.None | SecurityMasks.Owner | SecurityMasks.Group | SecurityMasks.Dacl | SecurityMasks.Sacl))
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SecurityMasks));

                ((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).SetOption((int)AdsOptions.ADS_OPTION_SECURITY_MASK, value);
            }
        }

        public int PageSize
        {
            get
            {
                return (int)((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).GetOption((int)AdsOptions.ADS_OPTION_PAGE_SIZE);
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.DSBadPageSize);

                ((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).SetOption((int)AdsOptions.ADS_OPTION_PAGE_SIZE, value);
            }
        }

        public int PasswordPort
        {
            get
            {
                return (int)((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).GetOption((int)AdsOptions.ADS_OPTION_PASSWORD_PORTNUMBER);
            }
            set
            {
                ((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).SetOption((int)AdsOptions.ADS_OPTION_PASSWORD_PORTNUMBER, value);
            }
        }

        public PasswordEncodingMethod PasswordEncoding
        {
            get
            {
                return (PasswordEncodingMethod)((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).GetOption((int)AdsOptions.ADS_OPTION_PASSWORD_METHOD);
            }
            set
            {
                if (value < PasswordEncodingMethod.PasswordEncodingSsl || value > PasswordEncodingMethod.PasswordEncodingClear)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(PasswordEncodingMethod));

                ((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).SetOption((int)AdsOptions.ADS_OPTION_PASSWORD_METHOD, value);
            }
        }

        public string GetCurrentServerName()
        {
            // underneath it uses the same handle and binds to the same object, so no permission is required as it has been done in Bind call            
            return (string)((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).GetOption((int)AdsOptions.ADS_OPTION_SERVERNAME);
        }

        public bool IsMutuallyAuthenticated()
        {
            try
            {
                int val = (int)((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).GetOption((int)AdsOptions.ADS_OPTION_MUTUAL_AUTH_STATUS);
                if ((val & ISC_RET_MUTUAL_AUTH) != 0)
                    return true;
                else
                    return false;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                // if SSL is used, ADSI will return E_ADS_BAD_PARAMETER, we should catch it here
                if (e.ErrorCode == unchecked((int)0x80005008))
                    return false;
                throw;
            }
        }

        public void SetUserNameQueryQuota(string accountName)
        {
            ((UnsafeNativeMethods.IAdsObjectOptions)_entry.AdsObject).SetOption((int)AdsOptions.ADS_OPTION_QUOTA, accountName);
        }
    }
}
