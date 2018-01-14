// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
    public sealed class NTAccount : IdentityReference
    {
        #region Private members

        private readonly string _name;

        //
        // Limit for nt account names for users is 20 while that for groups is 256
        //
        internal const int MaximumAccountNameLength = 256;

        //
        // Limit for dns domain names is 255
        //
        internal const int MaximumDomainNameLength = 255;

        #endregion

        #region Constructors

        public NTAccount(string domainName, string accountName)
        {
            if (accountName == null)
            {
                throw new ArgumentNullException(nameof(accountName));
            }

            if (accountName.Length == 0)
            {
                throw new ArgumentException(SR.Argument_StringZeroLength, nameof(accountName));
            }

            if (accountName.Length > MaximumAccountNameLength)
            {
                throw new ArgumentException(SR.IdentityReference_AccountNameTooLong, nameof(accountName));
            }

            if (domainName != null && domainName.Length > MaximumDomainNameLength)
            {
                throw new ArgumentException(SR.IdentityReference_DomainNameTooLong, nameof(domainName));
            }

            if (domainName == null || domainName.Length == 0)
            {
                _name = accountName;
            }
            else
            {
                _name = domainName + "\\" + accountName;
            }
        }

        public NTAccount(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Argument_StringZeroLength, nameof(name));
            }

            if (name.Length > (MaximumDomainNameLength + 1 /* '\' */ + MaximumAccountNameLength))
            {
                throw new ArgumentException(SR.IdentityReference_AccountNameTooLong, nameof(name));
            }

            _name = name;
        }

        #endregion

        #region Inherited properties and methods
        public override string Value
        {
            get
            {
                return ToString();
            }
        }

        public override bool IsValidTargetType(Type targetType)
        {
            if (targetType == typeof(SecurityIdentifier))
            {
                return true;
            }
            else if (targetType == typeof(NTAccount))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override IdentityReference Translate(Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (targetType == typeof(NTAccount))
            {
                return this; // assumes that NTAccount objects are immutable
            }
            else if (targetType == typeof(SecurityIdentifier))
            {
                IdentityReferenceCollection irSource = new IdentityReferenceCollection(1);
                irSource.Add(this);
                IdentityReferenceCollection irTarget;

                irTarget = NTAccount.Translate(irSource, targetType, true);

                return irTarget[0];
            }
            else
            {
                throw new ArgumentException(SR.IdentityReference_MustBeIdentityReference, nameof(targetType));
            }
        }

        public override bool Equals(object o)
        {
            return (this == o as NTAccount); // invokes operator==
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_name);
        }

        public override string ToString()
        {
            return _name;
        }

        internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceAccounts, Type targetType, bool forceSuccess)
        {
            bool SomeFailed = false;
            IdentityReferenceCollection Result;

            Result = Translate(sourceAccounts, targetType, out SomeFailed);

            if (forceSuccess && SomeFailed)
            {
                IdentityReferenceCollection UnmappedIdentities = new IdentityReferenceCollection();

                foreach (IdentityReference id in Result)
                {
                    if (id.GetType() != targetType)
                    {
                        UnmappedIdentities.Add(id);
                    }
                }

                throw new IdentityNotMappedException(SR.IdentityReference_IdentityNotMapped, UnmappedIdentities);
            }

            return Result;
        }
        
        internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceAccounts, Type targetType, out bool someFailed)
        {
            if (sourceAccounts == null)
            {
                throw new ArgumentNullException(nameof(sourceAccounts));
            }

            if (targetType == typeof(SecurityIdentifier))
            {
                return TranslateToSids(sourceAccounts, out someFailed);
            }

            throw new ArgumentException(SR.IdentityReference_MustBeIdentityReference, nameof(targetType));
        }

        #endregion

        #region Operators

        public static bool operator ==(NTAccount left, NTAccount right)
        {
            object l = left;
            object r = right;

            if (l == r)
            {
                return true;
            }
            else if (l == null || r == null)
            {
                return false;
            }
            else
            {
                return (left.ToString().Equals(right.ToString(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool operator !=(NTAccount left, NTAccount right)
        {
            return !(left == right); // invoke operator==
        }

        #endregion

        #region Private methods


        private static IdentityReferenceCollection TranslateToSids(IdentityReferenceCollection sourceAccounts, out bool someFailed)
        {
            if (sourceAccounts == null)
            {
                throw new ArgumentNullException(nameof(sourceAccounts));
            }

            if (sourceAccounts.Count == 0)
            {
                throw new ArgumentException(SR.Arg_EmptyCollection, nameof(sourceAccounts));
            }

            SafeLsaPolicyHandle LsaHandle = SafeLsaPolicyHandle.InvalidHandle;
            SafeLsaMemoryHandle ReferencedDomainsPtr = SafeLsaMemoryHandle.InvalidHandle;
            SafeLsaMemoryHandle SidsPtr = SafeLsaMemoryHandle.InvalidHandle;

            try
            {
                //
                // Construct an array of unicode strings
                //

                Interop.UNICODE_STRING[] Names = new Interop.UNICODE_STRING[sourceAccounts.Count];

                int currentName = 0;
                foreach (IdentityReference id in sourceAccounts)
                {
                    NTAccount nta = id as NTAccount;

                    if (nta == null)
                    {
                        throw new ArgumentException(SR.Argument_ImproperType, nameof(sourceAccounts));
                    }

                    Names[currentName].Buffer = nta.ToString();

                    if (Names[currentName].Buffer.Length * 2 + 2 > ushort.MaxValue)
                    {
                        // this should never happen since we are already validating account name length in constructor and 
                        // it is less than this limit
                        Debug.Assert(false, "NTAccount::TranslateToSids - source account name is too long.");
                        throw new InvalidOperationException();
                    }

                    Names[currentName].Length = (ushort)(Names[currentName].Buffer.Length * 2);
                    Names[currentName].MaximumLength = (ushort)(Names[currentName].Length + 2);
                    currentName++;
                }

                //
                // Open LSA policy (for lookup requires it)
                //

                LsaHandle = Win32.LsaOpenPolicy(null, PolicyRights.POLICY_LOOKUP_NAMES);

                //
                // Now perform the actual lookup
                //

                someFailed = false;
                uint ReturnCode;

                ReturnCode = Interop.Advapi32.LsaLookupNames2(LsaHandle, 0, sourceAccounts.Count, Names, ref ReferencedDomainsPtr, ref SidsPtr);

                //
                // Make a decision regarding whether it makes sense to proceed
                // based on the return code and the value of the forceSuccess argument
                //

                if (ReturnCode == Interop.StatusOptions.STATUS_NO_MEMORY ||
                    ReturnCode == Interop.StatusOptions.STATUS_INSUFFICIENT_RESOURCES)
                {
                    throw new OutOfMemoryException();
                }
                else if (ReturnCode == Interop.StatusOptions.STATUS_ACCESS_DENIED)
                {
                    throw new UnauthorizedAccessException();
                }
                else if (ReturnCode == Interop.StatusOptions.STATUS_NONE_MAPPED ||
                    ReturnCode == Interop.StatusOptions.STATUS_SOME_NOT_MAPPED)
                {
                    someFailed = true;
                }
                else if (ReturnCode != 0)
                {
                    uint win32ErrorCode = Interop.Advapi32.LsaNtStatusToWinError(ReturnCode);

                    if (unchecked((int)win32ErrorCode) != Interop.Errors.ERROR_TRUSTED_RELATIONSHIP_FAILURE)
                    {
                        Debug.Assert(false, string.Format(CultureInfo.InvariantCulture, "Interop.LsaLookupNames(2) returned unrecognized error {0}", win32ErrorCode));
                    }

                    throw new Win32Exception(unchecked((int)win32ErrorCode));
                }

                //
                // Interpret the results and generate SID objects
                //

                IdentityReferenceCollection Result = new IdentityReferenceCollection(sourceAccounts.Count);

                if (ReturnCode == 0 || ReturnCode == Interop.StatusOptions.STATUS_SOME_NOT_MAPPED)
                {
                    SidsPtr.Initialize((uint)sourceAccounts.Count, (uint)Marshal.SizeOf<Interop.LSA_TRANSLATED_SID2>());
                    Win32.InitializeReferencedDomainsPointer(ReferencedDomainsPtr);
                    Interop.LSA_TRANSLATED_SID2[] translatedSids = new Interop.LSA_TRANSLATED_SID2[sourceAccounts.Count];
                    SidsPtr.ReadArray(0, translatedSids, 0, translatedSids.Length);

                    for (int i = 0; i < sourceAccounts.Count; i++)
                    {
                        Interop.LSA_TRANSLATED_SID2 Lts = translatedSids[i];

                        //
                        // Only some names are recognized as NTAccount objects
                        //

                        switch ((SidNameUse)Lts.Use)
                        {
                            case SidNameUse.User:
                            case SidNameUse.Group:
                            case SidNameUse.Alias:
                            case SidNameUse.Computer:
                            case SidNameUse.WellKnownGroup:
                                Result.Add(new SecurityIdentifier(Lts.Sid, true));
                                break;

                            default:
                                someFailed = true;
                                Result.Add(sourceAccounts[i]);
                                break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < sourceAccounts.Count; i++)
                    {
                        Result.Add(sourceAccounts[i]);
                    }
                }

                return Result;
            }
            finally
            {
                LsaHandle.Dispose();
                ReferencedDomainsPtr.Dispose();
                SidsPtr.Dispose();
            }
        }
        #endregion
    }
}
