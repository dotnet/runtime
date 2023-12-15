// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

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
            ArgumentException.ThrowIfNullOrEmpty(accountName);

            if (accountName.Length > MaximumAccountNameLength)
            {
                throw new ArgumentException(SR.IdentityReference_AccountNameTooLong, nameof(accountName));
            }

            if (domainName != null && domainName.Length > MaximumDomainNameLength)
            {
                throw new ArgumentException(SR.IdentityReference_DomainNameTooLong, nameof(domainName));
            }

            if (string.IsNullOrEmpty(domainName))
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
            ArgumentException.ThrowIfNullOrEmpty(name);

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
            ArgumentNullException.ThrowIfNull(targetType);

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

        public override bool Equals([NotNullWhen(true)] object? o)
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
            IdentityReferenceCollection result = Translate(sourceAccounts, targetType, out bool someFailed);

            if (forceSuccess && someFailed)
            {
                IdentityReferenceCollection UnmappedIdentities = new IdentityReferenceCollection();

                foreach (IdentityReference id in result)
                {
                    if (id.GetType() != targetType)
                    {
                        UnmappedIdentities.Add(id);
                    }
                }

                throw new IdentityNotMappedException(SR.IdentityReference_IdentityNotMapped, UnmappedIdentities);
            }

            return result;
        }

        internal static IdentityReferenceCollection Translate(IdentityReferenceCollection sourceAccounts, Type targetType, out bool someFailed)
        {
            ArgumentNullException.ThrowIfNull(sourceAccounts);

            if (targetType == typeof(SecurityIdentifier))
            {
                return TranslateToSids(sourceAccounts, out someFailed);
            }

            throw new ArgumentException(SR.IdentityReference_MustBeIdentityReference, nameof(targetType));
        }

        #endregion

        #region Operators

        public static bool operator ==(NTAccount? left, NTAccount? right)
        {
            object? l = left;
            object? r = right;

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
                return (left!.ToString().Equals(right!.ToString(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool operator !=(NTAccount? left, NTAccount? right)
        {
            return !(left == right); // invoke operator==
        }

        #endregion

        #region Private methods


        private static unsafe IdentityReferenceCollection TranslateToSids(IdentityReferenceCollection sourceAccounts, out bool someFailed)
        {
            ArgumentNullException.ThrowIfNull(sourceAccounts);

            if (sourceAccounts.Count == 0)
            {
                throw new ArgumentException(SR.Arg_EmptyCollection, nameof(sourceAccounts));
            }

            SafeLsaPolicyHandle? LsaHandle = null;
            SafeLsaMemoryHandle? ReferencedDomainsPtr = null;
            SafeLsaMemoryHandle? SidsPtr = null;

            try
            {
                //
                // Construct an array of unicode strings
                //

                Interop.Advapi32.MARSHALLED_UNICODE_STRING[] Names = new Interop.Advapi32.MARSHALLED_UNICODE_STRING[sourceAccounts.Count];

                int currentName = 0;
                foreach (IdentityReference id in sourceAccounts)
                {
                    if (!(id is NTAccount nta))
                    {
                        throw new ArgumentException(SR.Argument_ImproperType, nameof(sourceAccounts));
                    }

                    Names[currentName].Buffer = nta.ToString();

                    if (Names[currentName].Buffer.Length * 2 + 2 > ushort.MaxValue)
                    {
                        // this should never happen since we are already validating account name length in constructor and
                        // it is less than this limit
                        Debug.Fail("NTAccount::TranslateToSids - source account name is too long.");
                        throw new InvalidOperationException();
                    }

                    Names[currentName].Length = (ushort)(Names[currentName].Buffer.Length * 2);
                    Names[currentName].MaximumLength = (ushort)(Names[currentName].Length + 2);
                    currentName++;
                }

                //
                // Open LSA policy (for lookup requires it)
                //

                LsaHandle = Win32.LsaOpenPolicy(null, Interop.Advapi32.PolicyRights.POLICY_LOOKUP_NAMES);

                //
                // Now perform the actual lookup
                //

                someFailed = false;
                uint ReturnCode;

                ReturnCode = Interop.Advapi32.LsaLookupNames2(LsaHandle, 0, sourceAccounts.Count, Names, out ReferencedDomainsPtr, out SidsPtr);

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
                        Debug.Fail($"Interop.LsaLookupNames(2) returned unrecognized error {win32ErrorCode}");
                    }

                    throw new Win32Exception(unchecked((int)win32ErrorCode));
                }

                //
                // Interpret the results and generate SID objects
                //

                IdentityReferenceCollection Result = new IdentityReferenceCollection(sourceAccounts.Count);

                if (ReturnCode == 0 || ReturnCode == Interop.StatusOptions.STATUS_SOME_NOT_MAPPED)
                {
                    SidsPtr.Initialize((uint)sourceAccounts.Count, (uint)sizeof(Interop.LSA_TRANSLATED_SID2));
                    ReferencedDomainsPtr.InitializeReferencedDomainsList();
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
                                Result.Add(new SecurityIdentifier(Lts.Sid));
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
                LsaHandle?.Dispose();
                ReferencedDomainsPtr?.Dispose();
                SidsPtr?.Dispose();
            }
        }
        #endregion
    }
}
