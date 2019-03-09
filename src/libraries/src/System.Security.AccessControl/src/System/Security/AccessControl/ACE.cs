// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Classes:  Access Control Entry (ACE) family of classes
**
**
===========================================================*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;

namespace System.Security.AccessControl
{
    //
    // Predefined ACE types
    // Anything else is considered user-defined
    //


    public enum AceType : byte
    {
        AccessAllowed = 0x00,
        AccessDenied = 0x01,
        SystemAudit = 0x02,
        SystemAlarm = 0x03,
        AccessAllowedCompound = 0x04,
        AccessAllowedObject = 0x05,
        AccessDeniedObject = 0x06,
        SystemAuditObject = 0x07,
        SystemAlarmObject = 0x08,
        AccessAllowedCallback = 0x09,
        AccessDeniedCallback = 0x0A,
        AccessAllowedCallbackObject = 0x0B,
        AccessDeniedCallbackObject = 0x0C,
        SystemAuditCallback = 0x0D,
        SystemAlarmCallback = 0x0E,
        SystemAuditCallbackObject = 0x0F,
        SystemAlarmCallbackObject = 0x10,
        MaxDefinedAceType = SystemAlarmCallbackObject,
    }

    //
    // Predefined ACE flags
    // The inheritance and auditing flags are stored in the
    // same field - this is to follow Windows ACE design
    //

    [Flags]

    public enum AceFlags : byte
    {
        None = 0x00,
        ObjectInherit = 0x01,
        ContainerInherit = 0x02,
        NoPropagateInherit = 0x04,
        InheritOnly = 0x08,
        Inherited = 0x10,
        SuccessfulAccess = 0x40,
        FailedAccess = 0x80,

        InheritanceFlags = ObjectInherit | ContainerInherit | NoPropagateInherit | InheritOnly,
        AuditFlags = SuccessfulAccess | FailedAccess,
    }


    public abstract class GenericAce
    {
        #region Private Members

        //
        // The 'byte' type is used to accommodate user-defined,
        // as well as well-known ACE types.
        //

        private readonly AceType _type;
        private AceFlags _flags;
        internal ushort _indexInAcl;
        #endregion

        #region Internal Constants

        //
        // Length of the ACE header in binary form
        //

        internal const int HeaderLength = 4;

        #endregion

        #region Internal Methods

        //
        // Format of the ACE header from ntseapi.h
        //
        // typedef struct _ACE_HEADER {
        //     UCHAR AceType;
        //     UCHAR AceFlags;
        //     USHORT AceSize;
        // } ACE_HEADER;
        //

        //
        // Marshal the ACE header into the given array starting at the given offset
        //

        internal void MarshalHeader(byte[] binaryForm, int offset)
        {
            int Length = BinaryLength; // Invokes the most derived property

            if (binaryForm == null)
            {
                throw new ArgumentNullException(nameof(binaryForm));
            }
            else if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(
nameof(offset),
                     SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            else if (binaryForm.Length - offset < BinaryLength)
            {
                //
                // The buffer will not fit the header
                //

                throw new ArgumentOutOfRangeException(
nameof(binaryForm),
                     SR.ArgumentOutOfRange_ArrayTooSmall);
            }
            else if (Length > ushort.MaxValue)
            {
                //
                // Only have two bytes to store the length in.
                // Indicates a bug in the implementation, not in user's code.
                //

                Debug.Fail("Length > ushort.MaxValue");
                // Replacing SystemException with InvalidOperationException. It's not a perfect fit,
                // but it's the best exception type available to indicate a failure because
                // of a bug in the ACE itself.
                throw new InvalidOperationException();
            }

            binaryForm[offset + 0] = (byte)AceType;
            binaryForm[offset + 1] = (byte)AceFlags;
            binaryForm[offset + 2] = unchecked((byte)(Length >> 0));
            binaryForm[offset + 3] = (byte)(Length >> 8);
        }

        #endregion

        #region Constructors

        internal GenericAce(AceType type, AceFlags flags)
        {
            //
            // Store the values passed in;
            // do not make any checks - anything is valid here
            //

            _type = type;
            _flags = flags;
        }

        #endregion

        #region Static Methods

        //
        // These mapper routines convert audit type flags to ACE flags and vice versa
        //

        internal static AceFlags AceFlagsFromAuditFlags(AuditFlags auditFlags)
        {
            AceFlags flags = AceFlags.None;

            if ((auditFlags & AuditFlags.Success) != 0)
            {
                flags |= AceFlags.SuccessfulAccess;
            }

            if ((auditFlags & AuditFlags.Failure) != 0)
            {
                flags |= AceFlags.FailedAccess;
            }

            if (flags == AceFlags.None)
            {
                throw new ArgumentException(
                     SR.Arg_EnumAtLeastOneFlag,
nameof(auditFlags));
            }

            return flags;
        }

        //
        // These mapper routines convert inheritance type flags to ACE flags and vice versa
        //

        internal static AceFlags AceFlagsFromInheritanceFlags(InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            AceFlags flags = AceFlags.None;

            if ((inheritanceFlags & InheritanceFlags.ContainerInherit) != 0)
            {
                flags |= AceFlags.ContainerInherit;
            }

            if ((inheritanceFlags & InheritanceFlags.ObjectInherit) != 0)
            {
                flags |= AceFlags.ObjectInherit;
            }

            //
            // Propagation flags are meaningless without inheritance flags
            //

            if (flags != 0)
            {
                if ((propagationFlags & PropagationFlags.NoPropagateInherit) != 0)
                {
                    flags |= AceFlags.NoPropagateInherit;
                }

                if ((propagationFlags & PropagationFlags.InheritOnly) != 0)
                {
                    flags |= AceFlags.InheritOnly; // ContainerInherit already turned on above
                }
            }

            return flags;
        }

        //
        // Sanity-check the ACE header (used by the unmarshaling logic)
        //

        internal static void VerifyHeader(byte[] binaryForm, int offset)
        {
            if (binaryForm == null)
            {
                throw new ArgumentNullException(nameof(binaryForm));
            }
            else if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(
nameof(offset),
                     SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            else if (binaryForm.Length - offset < HeaderLength)
            {
                //
                // We expect at least the ACE header ( 4 bytes )
                //

                throw new ArgumentOutOfRangeException(
nameof(binaryForm),
                     SR.ArgumentOutOfRange_ArrayTooSmall);
            }
            else if ((binaryForm[offset + 3] << 8) + (binaryForm[offset + 2] << 0) > binaryForm.Length - offset)
            {
                //
                // Reported length of ACE ought to be no longer than the
                // length of the buffer passed in
                //

                throw new ArgumentOutOfRangeException(
nameof(binaryForm),
                     SR.ArgumentOutOfRange_ArrayTooSmall);
            }
        }

        //
        // Instantiates the most-derived ACE type based on the binary
        // representation of an ACE
        //

        public static GenericAce CreateFromBinaryForm(byte[] binaryForm, int offset)
        {
            GenericAce result;
            AceType type;

            //
            // Sanity check the header
            //

            VerifyHeader(binaryForm, offset);

            type = (AceType)binaryForm[offset];

            if (type == AceType.AccessAllowed ||
                type == AceType.AccessDenied ||
                type == AceType.SystemAudit ||
                type == AceType.SystemAlarm ||
                type == AceType.AccessAllowedCallback ||
                type == AceType.AccessDeniedCallback ||
                type == AceType.SystemAuditCallback ||
                type == AceType.SystemAlarmCallback)
            {
                AceQualifier qualifier;
                int accessMask;
                SecurityIdentifier sid;
                bool isCallback;
                byte[] opaque;

                if (true == CommonAce.ParseBinaryForm(binaryForm, offset, out qualifier, out accessMask, out sid, out isCallback, out opaque))
                {
                    AceFlags flags = (AceFlags)binaryForm[offset + 1];
                    result = new CommonAce(flags, qualifier, accessMask, sid, isCallback, opaque);
                }
                else
                {
                    goto InvalidParameter;
                }
            }
            else if (type == AceType.AccessAllowedObject ||
                type == AceType.AccessDeniedObject ||
                type == AceType.SystemAuditObject ||
                type == AceType.SystemAlarmObject ||
                type == AceType.AccessAllowedCallbackObject ||
                type == AceType.AccessDeniedCallbackObject ||
                type == AceType.SystemAuditCallbackObject ||
                type == AceType.SystemAlarmCallbackObject)
            {
                AceQualifier qualifier;
                int accessMask;
                SecurityIdentifier sid;
                ObjectAceFlags objectFlags;
                Guid objectAceType;
                Guid inheritedObjectAceType;
                bool isCallback;
                byte[] opaque;

                if (true == ObjectAce.ParseBinaryForm(binaryForm, offset, out qualifier, out accessMask, out sid, out objectFlags, out objectAceType, out inheritedObjectAceType, out isCallback, out opaque))
                {
                    AceFlags flags = (AceFlags)binaryForm[offset + 1];
                    result = new ObjectAce(flags, qualifier, accessMask, sid, objectFlags, objectAceType, inheritedObjectAceType, isCallback, opaque);
                }
                else
                {
                    goto InvalidParameter;
                }
            }
            else if (type == AceType.AccessAllowedCompound)
            {
                int accessMask;
                CompoundAceType compoundAceType;
                SecurityIdentifier sid;

                if (true == CompoundAce.ParseBinaryForm(binaryForm, offset, out accessMask, out compoundAceType, out sid))
                {
                    AceFlags flags = (AceFlags)binaryForm[offset + 1];
                    result = new CompoundAce(flags, accessMask, compoundAceType, sid);
                }
                else
                {
                    goto InvalidParameter;
                }
            }
            else
            {
                AceFlags flags = (AceFlags)binaryForm[offset + 1];
                byte[] opaque = null;
                int aceLength = (binaryForm[offset + 2] << 0) + (binaryForm[offset + 3] << 8);

                if (aceLength % 4 != 0)
                {
                    goto InvalidParameter;
                }

                int opaqueLength = aceLength - HeaderLength;

                if (opaqueLength > 0)
                {
                    opaque = new byte[opaqueLength];

                    for (int i = 0; i < opaqueLength; i++)
                    {
                        opaque[i] = binaryForm[offset + aceLength - opaqueLength + i];
                    }
                }

                result = new CustomAce(type, flags, opaque);
            }

            //
            // As a final check, confirm that the advertised ACE header length
            // was the actual parsed length
            //

            if (((!(result is ObjectAce)) && ((binaryForm[offset + 2] << 0) + (binaryForm[offset + 3] << 8) != result.BinaryLength))
                //
                // This is needed because object aces created through ADSI have the advertised ACE length
                // greater than the actual length by 32 (bug in ADSI).
                //
                || ((result is ObjectAce) && ((binaryForm[offset + 2] << 0) + (binaryForm[offset + 3] << 8) != result.BinaryLength) && (((binaryForm[offset + 2] << 0) + (binaryForm[offset + 3] << 8) - 32) != result.BinaryLength)))
            {
                goto InvalidParameter;
            }

            return result;

        InvalidParameter:

            throw new ArgumentException(
                 SR.ArgumentException_InvalidAceBinaryForm,
nameof(binaryForm));
        }

        #endregion

        #region Public Properties

        //
        // Returns the numeric type of the ACE
        // Since not all ACE types are known, this
        // property returns a byte value.
        //

        public AceType AceType
        {
            get
            {
                return _type;
            }
        }

        //
        // Sets and retrieves the flags associated with the ACE
        // No checks are performed when setting the flags.
        //

        public AceFlags AceFlags
        {
            get
            {
                return _flags;
            }

            set
            {
                _flags = value;
            }
        }

        public bool IsInherited
        {
            get
            {
                return ((this.AceFlags & AceFlags.Inherited) != 0);
            }
        }

        public InheritanceFlags InheritanceFlags
        {
            get
            {
                InheritanceFlags flags = 0;

                if ((this.AceFlags & AceFlags.ContainerInherit) != 0)
                {
                    flags |= InheritanceFlags.ContainerInherit;
                }

                if ((this.AceFlags & AceFlags.ObjectInherit) != 0)
                {
                    flags |= InheritanceFlags.ObjectInherit;
                }

                return flags;
            }
        }

        public PropagationFlags PropagationFlags
        {
            get
            {
                PropagationFlags flags = 0;

                if ((this.AceFlags & AceFlags.InheritOnly) != 0)
                {
                    flags |= PropagationFlags.InheritOnly;
                }

                if ((this.AceFlags & AceFlags.NoPropagateInherit) != 0)
                {
                    flags |= PropagationFlags.NoPropagateInherit;
                }

                return flags;
            }
        }

        public AuditFlags AuditFlags
        {
            get
            {
                AuditFlags flags = 0;

                if ((this.AceFlags & AceFlags.SuccessfulAccess) != 0)
                {
                    flags |= AuditFlags.Success;
                }

                if ((this.AceFlags & AceFlags.FailedAccess) != 0)
                {
                    flags |= AuditFlags.Failure;
                }

                return flags;
            }
        }

        //
        // The value returned is really an unsigned short
        // A signed type is used for CLS compliance
        //

        public abstract int BinaryLength { get; }

        #endregion

        #region Public Methods

        //
        // Copies the binary representation of the ACE into a given array
        // starting at the given offset.
        //

        public abstract void GetBinaryForm(byte[] binaryForm, int offset);

        //
        // Cloning is performed by calling the from-binary static factory method
        // on the binary representation of the ACE.
        // Make this routine virtual if any leaf ACE class were to ever become
        // unsealed.
        //

        public GenericAce Copy()
        {
            //
            // Allocate an array big enough to hold the binary representation of the ACE
            //

            byte[] binaryForm = new byte[BinaryLength];

            GetBinaryForm(binaryForm, 0);

            return GenericAce.CreateFromBinaryForm(binaryForm, 0);
        }

        public sealed override bool Equals(object o)
        {
            if (o == null)
            {
                return false;
            }

            GenericAce ace = (o as GenericAce);

            if (ace == null)
            {
                return false;
            }

            if (this.AceType != ace.AceType ||
                this.AceFlags != ace.AceFlags)
            {
                return false;
            }

            int thisLength = this.BinaryLength;
            int aceLength = ace.BinaryLength;

            if (thisLength != aceLength)
            {
                return false;
            }

            byte[] array1 = new byte[thisLength];
            byte[] array2 = new byte[aceLength];

            this.GetBinaryForm(array1, 0);
            ace.GetBinaryForm(array2, 0);

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            return true;
        }

        public sealed override int GetHashCode()
        {
            int binaryLength = BinaryLength;
            byte[] array = new byte[binaryLength];
            GetBinaryForm(array, 0);
            int result = 0, i = 0;

            //
            // For purposes of hash code computation,
            // treat the ACE as an array of ints (fortunately, its length is divisible by 4)
            // and simply XOR all these ints together
            //

            while (i < binaryLength)
            {
                int increment = ((int)array[i]) +
                                (((int)array[i + 1]) << 8) +
                                (((int)array[i + 2]) << 16) +
                                (((int)array[i + 3]) << 24);

                result ^= increment;
                i += 4;
            }

            return result;
        }

        public static bool operator ==(GenericAce left, GenericAce right)
        {
            object l = left;
            object r = right;

            if (l == null && r == null)
            {
                return true;
            }
            else if (l == null || r == null)
            {
                return false;
            }
            else
            {
                return left.Equals(right);
            }
        }

        public static bool operator !=(GenericAce left, GenericAce right)
        {
            return !(left == right);
        }
        #endregion
    }

    //
    // ACEs fall into two broad categories: known and user-defined
    //

    //
    // Every known ACE type contains an access mask and a SID
    //


    public abstract class KnownAce : GenericAce
    {
        #region Private Members

        //
        // All known ACE types contain an access mask and a SID
        //

        private int _accessMask;
        private SecurityIdentifier _sid;

        #endregion

        #region Internal Constants

        internal const int AccessMaskLength = 4;

        #endregion

        #region Constructors

        internal KnownAce(AceType type, AceFlags flags, int accessMask, SecurityIdentifier securityIdentifier)
            : base(type, flags)
        {
            if (securityIdentifier == null)
            {
                throw new ArgumentNullException(nameof(securityIdentifier));
            }

            //
            // The values are set by invoking the properties.
            //

            AccessMask = accessMask;
            SecurityIdentifier = securityIdentifier;
        }

        #endregion

        #region Public Properties

        //
        // Sets and retrieves the access mask associated with this ACE.
        // The access mask can be any 32-bit value.
        //

        public int AccessMask
        {
            get
            {
                return _accessMask;
            }

            set
            {
                _accessMask = value;
            }
        }

        //
        // Sets and retrieves the SID associated with this ACE.
        // The SID can not be null, but can otherwise be any valid
        // security identifier.
        //

        public SecurityIdentifier SecurityIdentifier
        {
            get
            {
                return _sid;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _sid = value;
            }
        }
        #endregion
    }

    //
    // User-defined ACEs are ACE types we don't recognize.
    // They contain a standard ACE header followed by a binary blob.
    //


    public sealed class CustomAce : GenericAce
    {
        #region Private Members

        //
        // Opaque data is what follows the ACE header.
        // It is not interpreted by any code except that which
        // understands the ACE type.
        //

        private byte[] _opaque;

        #endregion

        #region Public Constants

        //
        // Returns the maximum allowed length of opaque data
        //

        public static readonly int MaxOpaqueLength = ushort.MaxValue - HeaderLength;

        #endregion

        #region Constructors

        public CustomAce(AceType type, AceFlags flags, byte[] opaque)
            : base(type, flags)
        {
            if (type <= AceType.MaxDefinedAceType)
            {
                throw new ArgumentOutOfRangeException(
nameof(type),
                     SR.ArgumentOutOfRange_InvalidUserDefinedAceType);
            }

            SetOpaque(opaque);
        }

        #endregion

        #region Public Properties

        //
        // Returns the length of the opaque blob
        //

        public int OpaqueLength
        {
            get
            {
                if (_opaque == null)
                {
                    return 0;
                }
                else
                {
                    return _opaque.Length;
                }
            }
        }

        //
        // Returns the length of the binary representation of this ACE
        // The value returned is really an unsigned short
        //

        public /* sealed */ override int BinaryLength
        {
            get
            {
                return HeaderLength + OpaqueLength;
            }
        }

        #endregion

        #region Public Methods

        //
        // Methods to set and retrieve the opaque portion of the ACE
        // Important: the caller is given the actual (not cloned) copy of the data
        //

        public byte[] GetOpaque()
        {
            return _opaque;
        }

        public void SetOpaque(byte[] opaque)
        {
            if (opaque != null)
            {
                if (opaque.Length > MaxOpaqueLength)
                {
                    throw new ArgumentOutOfRangeException(
nameof(opaque),
                        SR.Format(SR.ArgumentOutOfRange_ArrayLength, 0, MaxOpaqueLength));
                }
                else if (opaque.Length % 4 != 0)
                {
                    throw new ArgumentOutOfRangeException(
nameof(opaque),
                        SR.Format(SR.ArgumentOutOfRange_ArrayLengthMultiple, 4));
                }
            }

            _opaque = opaque;
        }

        //
        // Copies the binary representation of the ACE into a given array
        // starting at the given offset.
        //

        public /* sealed */ override void GetBinaryForm(byte[] binaryForm, int offset)
        {
            //
            // Populate the header
            //

            MarshalHeader(binaryForm, offset);
            offset += HeaderLength;

            //
            // Header is followed by the opaque data
            //

            if (OpaqueLength != 0)
            {
                if (OpaqueLength > MaxOpaqueLength)
                {
                    Debug.Fail("OpaqueLength somehow managed to exceed MaxOpaqueLength");
                    // Replacing SystemException with InvalidOperationException. It's not a perfect fit,
                    // but it's the best exception type available to indicate a failure because
                    // of a bug in the ACE itself.
                    throw new InvalidOperationException();
                }

                GetOpaque().CopyTo(binaryForm, offset);
            }
        }
        #endregion
    }

    //
    // Known ACE types fall into two categories: compound and qualified
    //

    //
    // Compound ACEs ...
    //
    // Tne in-memory structure of a compound ACE is as follows:
    //
    // typedef struct _COMPOUND_ACCESS_ALLOWED_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     USHORT CompoundAceType;
    //     USHORT Reserved;
    //     ULONG SidStart;
    // } COMPOUND_ACCESS_ALLOWED_ACE;
    //


    public enum CompoundAceType
    {
        Impersonation = 0x01,
    }


    public sealed class CompoundAce : KnownAce
    {
        #region Private Members

        private CompoundAceType _compoundAceType;

        #endregion

        #region Private Constants

        private const int AceTypeLength = 4; // including 2 reserved bytes

        #endregion

        #region Constructors

        public CompoundAce(AceFlags flags, int accessMask, CompoundAceType compoundAceType, SecurityIdentifier sid)
            : base(AceType.AccessAllowedCompound, flags, accessMask, sid)
        {
            //
            // The compound ACE type value is deliberately not validated
            //

            _compoundAceType = compoundAceType;
        }

        #endregion

        #region Static Parser

        internal static bool ParseBinaryForm(
            byte[] binaryForm,
            int offset,
            out int accessMask,
            out CompoundAceType compoundAceType,
            out SecurityIdentifier sid)
        {
            //
            // Verify the ACE header
            //

            VerifyHeader(binaryForm, offset);

            //
            // Verify the length field
            //

            if (binaryForm.Length - offset < HeaderLength + AccessMaskLength + AceTypeLength + SecurityIdentifier.MinBinaryLength)
            {
                goto InvalidParameter;
            }

            int baseOffset = offset + HeaderLength;
            int offsetLocal = 0;

            //
            // The access mask is stored in big-endian format
            //

            accessMask =
                unchecked((int)(
                (((uint)binaryForm[baseOffset + 0]) << 0) +
                (((uint)binaryForm[baseOffset + 1]) << 8) +
                (((uint)binaryForm[baseOffset + 2]) << 16) +
                (((uint)binaryForm[baseOffset + 3]) << 24)));

            offsetLocal += AccessMaskLength;

            compoundAceType =
                (CompoundAceType)(
                (((uint)binaryForm[baseOffset + offsetLocal + 0]) << 0) +
                (((uint)binaryForm[baseOffset + offsetLocal + 1]) << 8));

            offsetLocal += AceTypeLength; // Skipping over the two reserved bits

            //
            // The access mask is followed by the SID
            //

            sid = new SecurityIdentifier(binaryForm, baseOffset + offsetLocal);

            return true;

        InvalidParameter:

            accessMask = 0;
            compoundAceType = 0;
            sid = null;

            return false;
        }

        #endregion

        #region Public Properties

        public CompoundAceType CompoundAceType
        {
            get
            {
                return _compoundAceType;
            }

            set
            {
                _compoundAceType = value;
            }
        }

        public override int BinaryLength
        {
            get
            {
                return (HeaderLength + AccessMaskLength + AceTypeLength + SecurityIdentifier.BinaryLength);
            }
        }

        #endregion

        #region Public Methods

        //
        // Copies the binary representation of the ACE into a given array
        // starting at the given offset.
        //

        public override void GetBinaryForm(byte[] binaryForm, int offset)
        {
            //
            // Populate the header
            //

            MarshalHeader(binaryForm, offset);

            int baseOffset = offset + HeaderLength;
            int offsetLocal = 0;

            //
            // Store the access mask in the big-endian format
            //
            unchecked
            {
                binaryForm[baseOffset + 0] = (byte)(AccessMask >> 0);
                binaryForm[baseOffset + 1] = (byte)(AccessMask >> 8);
                binaryForm[baseOffset + 2] = (byte)(AccessMask >> 16);
                binaryForm[baseOffset + 3] = (byte)(AccessMask >> 24);
            }

            offsetLocal += AccessMaskLength;

            //
            // Store the compound ace type and the two reserved bytes
            //

            binaryForm[baseOffset + offsetLocal + 0] = (byte)((ushort)CompoundAceType >> 0);
            binaryForm[baseOffset + offsetLocal + 1] = (byte)((ushort)CompoundAceType >> 8);
            binaryForm[baseOffset + offsetLocal + 2] = 0;
            binaryForm[baseOffset + offsetLocal + 3] = 0;

            offsetLocal += AceTypeLength;

            //
            // Store the SID
            //

            SecurityIdentifier.GetBinaryForm(binaryForm, baseOffset + offsetLocal);
        }
        #endregion
    }

    //
    // Qualified ACEs are always one of:
    //     - AccessAllowed
    //     - AccessDenied
    //     - SystemAudit
    //     - SystemAlarm
    // and may optionally support callback data
    //


    public enum AceQualifier
    {
        AccessAllowed = 0x0,
        AccessDenied = 0x1,
        SystemAudit = 0x2,
        SystemAlarm = 0x3,
    }


    public abstract class QualifiedAce : KnownAce
    {
        #region Private Members

        private readonly bool _isCallback;
        private readonly AceQualifier _qualifier;
        private byte[] _opaque;

        #endregion

        #region Private Methods

        private AceQualifier QualifierFromType(AceType type, out bool isCallback)
        {
            //
            // Better performance might be achieved by using a hard-coded table
            //

            switch (type)
            {
                case AceType.AccessAllowed:
                    isCallback = false;
                    return AceQualifier.AccessAllowed;

                case AceType.AccessDenied:
                    isCallback = false;
                    return AceQualifier.AccessDenied;

                case AceType.SystemAudit:
                    isCallback = false;
                    return AceQualifier.SystemAudit;

                case AceType.SystemAlarm:
                    isCallback = false;
                    return AceQualifier.SystemAlarm;

                case AceType.AccessAllowedCallback:
                    isCallback = true;
                    return AceQualifier.AccessAllowed;

                case AceType.AccessDeniedCallback:
                    isCallback = true;
                    return AceQualifier.AccessDenied;

                case AceType.SystemAuditCallback:
                    isCallback = true;
                    return AceQualifier.SystemAudit;

                case AceType.SystemAlarmCallback:
                    isCallback = true;
                    return AceQualifier.SystemAlarm;

                case AceType.AccessAllowedObject:
                    isCallback = false;
                    return AceQualifier.AccessAllowed;

                case AceType.AccessDeniedObject:
                    isCallback = false;
                    return AceQualifier.AccessDenied;

                case AceType.SystemAuditObject:
                    isCallback = false;
                    return AceQualifier.SystemAudit;

                case AceType.SystemAlarmObject:
                    isCallback = false;
                    return AceQualifier.SystemAlarm;

                case AceType.AccessAllowedCallbackObject:
                    isCallback = true;
                    return AceQualifier.AccessAllowed;

                case AceType.AccessDeniedCallbackObject:
                    isCallback = true;
                    return AceQualifier.AccessDenied;

                case AceType.SystemAuditCallbackObject:
                    isCallback = true;
                    return AceQualifier.SystemAudit;

                case AceType.SystemAlarmCallbackObject:
                    isCallback = true;
                    return AceQualifier.SystemAlarm;

                default:

                    //
                    // Indicates a bug in the implementation, not in user's code
                    //

                    Debug.Fail("Invalid ACE type");
                    // Replacing SystemException with InvalidOperationException. It's not a perfect fit,
                    // but it's the best exception type available to indicate a failure because
                    // of a bug in the ACE itself.
                    throw new InvalidOperationException();
            }
        }

        #endregion

        #region Constructors

        internal QualifiedAce(AceType type, AceFlags flags, int accessMask, SecurityIdentifier sid, byte[] opaque)
            : base(type, flags, accessMask, sid)
        {
            _qualifier = QualifierFromType(type, out _isCallback);
            SetOpaque(opaque);
        }

        #endregion

        #region Public Properties

        //
        // Returns the qualifier associated with this ACE
        // Qualifier is determined at object creation time and
        // can not be changed since doing so would change the ACE type
        // which is in itself an immutable property
        //

        public AceQualifier AceQualifier
        {
            get
            {
                return _qualifier;
            }
        }

        //
        // Returns 'true' if this ACE type supports resource
        // manager-specific callback data.
        // This property is determined at object creation time
        // and can not be changed.
        //

        public bool IsCallback
        {
            get
            {
                return _isCallback;
            }
        }

        //
        // ACE types that support opaque data must also specify the maximum
        // allowed length of such data
        //

        internal abstract int MaxOpaqueLengthInternal { get; }

        //
        // Returns the length of opaque blob
        //

        public int OpaqueLength
        {
            get
            {
                if (_opaque != null)
                {
                    return _opaque.Length;
                }
                else
                {
                    return 0;
                }
            }
        }

        #endregion

        #region Public Methods

        //
        // Methods to set and retrieve the opaque portion of the ACE
        // NOTE: the caller is given the actual (not cloned) copy of the data
        //

        public byte[] GetOpaque()
        {
            return _opaque;
        }

        public void SetOpaque(byte[] opaque)
        {
            if (opaque != null)
            {
                if (opaque.Length > MaxOpaqueLengthInternal)
                {
                    throw new ArgumentOutOfRangeException(
nameof(opaque),
                        SR.Format(SR.ArgumentOutOfRange_ArrayLength, 0, MaxOpaqueLengthInternal));
                }
                else if (opaque.Length % 4 != 0)
                {
                    throw new ArgumentOutOfRangeException(
nameof(opaque),
                        SR.Format(SR.ArgumentOutOfRange_ArrayLengthMultiple, 4));
                }
            }

            _opaque = opaque;
        }
        #endregion
    }

    //
    // The following eight classes are boilerplate, differing only by their ACE type
    // and support for callbacks
    // Thus their implementation will derive from the same class: CommonAce
    //
    // typedef struct _ACCESS_ALLOWED_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG SidStart;
    // } ACCESS_ALLOWED_ACE;
    //
    // typedef struct _ACCESS_DENIED_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG SidStart;
    // } ACCESS_DENIED_ACE;
    //
    // typedef struct _SYSTEM_AUDIT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG SidStart;
    // } SYSTEM_AUDIT_ACE;
    //
    // typedef struct _SYSTEM_ALARM_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG SidStart;
    // } SYSTEM_ALARM_ACE;
    //
    // typedef struct _ACCESS_ALLOWED_CALLBACK_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG SidStart;
    //     // Opaque resouce manager specific data
    // } ACCESS_ALLOWED_CALLBACK_ACE, *PACCESS_ALLOWED_CALLBACK_ACE;
    // 
    // typedef struct _ACCESS_DENIED_CALLBACK_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG SidStart;
    //     // Opaque resouce manager specific data
    // } ACCESS_DENIED_CALLBACK_ACE, *PACCESS_DENIED_CALLBACK_ACE;
    // 
    // typedef struct _SYSTEM_AUDIT_CALLBACK_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG SidStart;
    //     // Opaque resouce manager specific data
    // } SYSTEM_AUDIT_CALLBACK_ACE, *PSYSTEM_AUDIT_CALLBACK_ACE;
    // 
    // typedef struct _SYSTEM_ALARM_CALLBACK_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG SidStart;
    //     // Opaque resouce manager specific data
    // } SYSTEM_ALARM_CALLBACK_ACE, *PSYSTEM_ALARM_CALLBACK_ACE;
    //


    public sealed class CommonAce : QualifiedAce
    {
        #region Constructors

        //
        // The constructor computes the type of this ACE and passes the rest
        // to the base class constructor
        //

        public CommonAce(AceFlags flags, AceQualifier qualifier, int accessMask, SecurityIdentifier sid, bool isCallback, byte[] opaque)
            : base(TypeFromQualifier(isCallback, qualifier), flags, accessMask, sid, opaque)
        {
        }

        #endregion

        #region Private Static Methods

        //
        // Based on the is-callback and qualifier information,
        // computes the numerical type of the ACE
        //

        private static AceType TypeFromQualifier(bool isCallback, AceQualifier qualifier)
        {
            //
            // Might benefit from replacing this with a static hard-coded table
            //

            switch (qualifier)
            {
                case AceQualifier.AccessAllowed:
                    return isCallback ? AceType.AccessAllowedCallback : AceType.AccessAllowed;

                case AceQualifier.AccessDenied:
                    return isCallback ? AceType.AccessDeniedCallback : AceType.AccessDenied;

                case AceQualifier.SystemAudit:
                    return isCallback ? AceType.SystemAuditCallback : AceType.SystemAudit;

                case AceQualifier.SystemAlarm:
                    return isCallback ? AceType.SystemAlarmCallback : AceType.SystemAlarm;

                default:

                    throw new ArgumentOutOfRangeException(
nameof(qualifier),
                        SR.ArgumentOutOfRange_Enum);
            }
        }

        #endregion

        #region Static Parser

        //
        // Called by GenericAce.CreateFromBinaryForm to parse the binary
        // form of the common ACE and extract the useful pieces.
        //

        internal static bool ParseBinaryForm(
            byte[] binaryForm,
            int offset,
            out AceQualifier qualifier,
            out int accessMask,
            out SecurityIdentifier sid,
            out bool isCallback,
            out byte[] opaque)
        {
            //
            // Verify the ACE header
            //

            VerifyHeader(binaryForm, offset);

            //
            // Verify the length field
            //

            if (binaryForm.Length - offset < HeaderLength + AccessMaskLength + SecurityIdentifier.MinBinaryLength)
            {
                goto InvalidParameter;
            }

            //
            // Identify callback ACE types
            //

            AceType type = (AceType)binaryForm[offset];

            if (type == AceType.AccessAllowed ||
                type == AceType.AccessDenied ||
                type == AceType.SystemAudit ||
                type == AceType.SystemAlarm)
            {
                isCallback = false;
            }
            else if (type == AceType.AccessAllowedCallback ||
                type == AceType.AccessDeniedCallback ||
                type == AceType.SystemAuditCallback ||
                type == AceType.SystemAlarmCallback)
            {
                isCallback = true;
            }
            else
            {
                goto InvalidParameter;
            }

            //
            // Compute the qualifier from the ACE type
            //

            if (type == AceType.AccessAllowed ||
                type == AceType.AccessAllowedCallback)
            {
                qualifier = AceQualifier.AccessAllowed;
            }
            else if (type == AceType.AccessDenied ||
                type == AceType.AccessDeniedCallback)
            {
                qualifier = AceQualifier.AccessDenied;
            }
            else if (type == AceType.SystemAudit ||
                type == AceType.SystemAuditCallback)
            {
                qualifier = AceQualifier.SystemAudit;
            }
            else if (type == AceType.SystemAlarm ||
                type == AceType.SystemAlarmCallback)
            {
                qualifier = AceQualifier.SystemAlarm;
            }
            else
            {
                goto InvalidParameter;
            }

            int baseOffset = offset + HeaderLength;
            int offsetLocal = 0;

            //
            // The access mask is stored in big-endian format
            //

            accessMask =
                (int)(
                (((uint)binaryForm[baseOffset + 0]) << 0) +
                (((uint)binaryForm[baseOffset + 1]) << 8) +
                (((uint)binaryForm[baseOffset + 2]) << 16) +
                (((uint)binaryForm[baseOffset + 3]) << 24));

            offsetLocal += AccessMaskLength;

            //
            // The access mask is followed by the SID
            //

            sid = new SecurityIdentifier(binaryForm, baseOffset + offsetLocal);

            //
            // The rest of the blob is occupied by opaque callback data, if such is supported
            //

            opaque = null;

            int aceLength = (binaryForm[offset + 3] << 8) + (binaryForm[offset + 2] << 0);

            if (aceLength % 4 != 0)
            {
                goto InvalidParameter;
            }

            int opaqueLength = aceLength - HeaderLength - AccessMaskLength - (byte)sid.BinaryLength;

            if (opaqueLength > 0)
            {
                opaque = new byte[opaqueLength];

                for (int i = 0; i < opaqueLength; i++)
                {
                    opaque[i] = binaryForm[offset + aceLength - opaqueLength + i];
                }
            }

            return true;

        InvalidParameter:

            qualifier = 0;
            accessMask = 0;
            sid = null;
            isCallback = false;
            opaque = null;

            return false;
        }

        #endregion

        #region Public Properties

        public /* sealed */ override int BinaryLength
        {
            get
            {
                return (HeaderLength + AccessMaskLength + SecurityIdentifier.BinaryLength + OpaqueLength);
            }
        }

        public static int MaxOpaqueLength(bool isCallback)
        {
            return ushort.MaxValue - HeaderLength - AccessMaskLength - SecurityIdentifier.MaxBinaryLength;
        }

        internal override int MaxOpaqueLengthInternal
        {
            get { return MaxOpaqueLength(IsCallback); }
        }

        #endregion

        #region Public Methods

        //
        // Copies the binary representation of the ACE into a given array
        // starting at the given offset.
        //

        public /* sealed */ override void GetBinaryForm(byte[] binaryForm, int offset)
        {
            //
            // Populate the header
            //

            MarshalHeader(binaryForm, offset);

            int baseOffset = offset + HeaderLength;
            int offsetLocal = 0;

            //
            // Store the access mask in the big-endian format
            //

            unchecked
            {
                binaryForm[baseOffset + 0] = (byte)(AccessMask >> 0);
                binaryForm[baseOffset + 1] = (byte)(AccessMask >> 8);
                binaryForm[baseOffset + 2] = (byte)(AccessMask >> 16);
                binaryForm[baseOffset + 3] = (byte)(AccessMask >> 24);
            }

            offsetLocal += AccessMaskLength;

            //
            // Store the SID
            //

            SecurityIdentifier.GetBinaryForm(binaryForm, baseOffset + offsetLocal);
            offsetLocal += SecurityIdentifier.BinaryLength;

            //
            // Finally, if opaque is supported, store it
            //

            if (GetOpaque() != null)
            {
                if (OpaqueLength > MaxOpaqueLengthInternal)
                {
                    Debug.Fail("OpaqueLength somehow managed to exceed MaxOpaqueLength");
                    // Replacing SystemException with InvalidOperationException. It's not a perfect fit,
                    // but it's the best exception type available to indicate a failure because
                    // of a bug in the ACE itself.
                    throw new InvalidOperationException();
                }

                GetOpaque().CopyTo(binaryForm, baseOffset + offsetLocal);
            }
        }
        #endregion
    }

    //
    // The following eight classes are boilerplate, differing only by their ACE type
    // and support for opaque data
    // Thus their implementation will derive from the same class: ObjectAce
    //
    // typedef struct _ACCESS_ALLOWED_OBJECT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG Flags;
    //     GUID ObjectType;
    //     GUID InheritedObjectType;
    //     ULONG SidStart;
    // } ACCESS_ALLOWED_OBJECT_ACE, *PACCESS_ALLOWED_OBJECT_ACE;
    //
    // typedef struct _ACCESS_DENIED_OBJECT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG Flags;
    //     GUID ObjectType;
    //     GUID InheritedObjectType;
    //     ULONG SidStart;
    // } ACCESS_DENIED_OBJECT_ACE, *PACCESS_DENIED_OBJECT_ACE;
    //
    // typedef struct _SYSTEM_AUDIT_OBJECT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG Flags;
    //     GUID ObjectType;
    //     GUID InheritedObjectType;
    //     ULONG SidStart;
    // } SYSTEM_AUDIT_OBJECT_ACE, *PSYSTEM_AUDIT_OBJECT_ACE;
    //
    // typedef struct _SYSTEM_ALARM_OBJECT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG Flags;
    //     GUID ObjectType;
    //     GUID InheritedObjectType;
    //     ULONG SidStart;
    // } SYSTEM_ALARM_OBJECT_ACE, *PSYSTEM_ALARM_OBJECT_ACE;
    //
    // typedef struct _ACCESS_ALLOWED_CALLBACK_OBJECT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG Flags;
    //     GUID ObjectType;
    //     GUID InheritedObjectType;
    //     ULONG SidStart;
    //     // Opaque resouce manager specific data
    // } ACCESS_ALLOWED_CALLBACK_OBJECT_ACE, *PACCESS_ALLOWED_CALLBACK_OBJECT_ACE;
    //
    // typedef struct _ACCESS_DENIED_CALLBACK_OBJECT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG Flags;
    //     GUID ObjectType;
    //     GUID InheritedObjectType;
    //     ULONG SidStart;
    //     // Opaque resouce manager specific data
    // } ACCESS_DENIED_CALLBACK_OBJECT_ACE, *PACCESS_DENIED_CALLBACK_OBJECT_ACE;
    //
    // typedef struct _SYSTEM_AUDIT_CALLBACK_OBJECT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG Flags;
    //     GUID ObjectType;
    //     GUID InheritedObjectType;
    //     ULONG SidStart;
    //     // Opaque resouce manager specific data
    // } SYSTEM_AUDIT_CALLBACK_OBJECT_ACE, *PSYSTEM_AUDIT_CALLBACK_OBJECT_ACE;
    //
    // typedef struct _SYSTEM_ALARM_CALLBACK_OBJECT_ACE {
    //     ACE_HEADER Header;
    //     ACCESS_MASK Mask;
    //     ULONG Flags;
    //     GUID ObjectType;
    //     GUID InheritedObjectType;
    //     ULONG SidStart;
    //     // Opaque resouce manager specific data
    // } SYSTEM_ALARM_CALLBACK_OBJECT_ACE, *PSYSTEM_ALARM_CALLBACK_OBJECT_ACE;
    //

    [Flags]

    public enum ObjectAceFlags
    {
        None = 0x00,
        ObjectAceTypePresent = 0x01,
        InheritedObjectAceTypePresent = 0x02,
    }


    public sealed class ObjectAce : QualifiedAce
    {
        #region Private Members and Constants

        private ObjectAceFlags _objectFlags;
        private Guid _objectAceType;
        private Guid _inheritedObjectAceType;

        private const int ObjectFlagsLength = 4;
        private const int GuidLength = 16;

        #endregion

        #region Constructors

        public ObjectAce(AceFlags aceFlags, AceQualifier qualifier, int accessMask, SecurityIdentifier sid, ObjectAceFlags flags, Guid type, Guid inheritedType, bool isCallback, byte[] opaque)
            : base(TypeFromQualifier(isCallback, qualifier), aceFlags, accessMask, sid, opaque)
        {
            _objectFlags = flags;
            _objectAceType = type;
            _inheritedObjectAceType = inheritedType;
        }

        #endregion

        #region Private Methods

        //  
        // The following access mask bits in object aces may refer to an objectType that
        // identifies the property set, property, extended right, or type of child object to which the ACE applies
        //
        //    ADS_RIGHT_DS_CREATE_CHILD = 0x1, 
        //    ADS_RIGHT_DS_DELETE_CHILD = 0x2, 
        //    ADS_RIGHT_DS_SELF = 0x8,
        //    ADS_RIGHT_DS_READ_PROP = 0x10, 
        //    ADS_RIGHT_DS_WRITE_PROP = 0x20, 
        //    ADS_RIGHT_DS_CONTROL_ACCESS = 0x100
        //
        internal static readonly int AccessMaskWithObjectType = 0x1 | 0x2 | 0x8 | 0x10 | 0x20 | 0x100;

        private static AceType TypeFromQualifier(bool isCallback, AceQualifier qualifier)
        {
            switch (qualifier)
            {
                case AceQualifier.AccessAllowed:
                    return isCallback ? AceType.AccessAllowedCallbackObject : AceType.AccessAllowedObject;

                case AceQualifier.AccessDenied:
                    return isCallback ? AceType.AccessDeniedCallbackObject : AceType.AccessDeniedObject;

                case AceQualifier.SystemAudit:
                    return isCallback ? AceType.SystemAuditCallbackObject : AceType.SystemAuditObject;

                case AceQualifier.SystemAlarm:
                    return isCallback ? AceType.SystemAlarmCallbackObject : AceType.SystemAlarmObject;

                default:

                    throw new ArgumentOutOfRangeException(
nameof(qualifier),
                        SR.ArgumentOutOfRange_Enum);
            }
        }

        //
        // This method checks if the objectType matches with the specified object type
        // (Either both do not have an object type or they have the same object type)
        //
        internal bool ObjectTypesMatch(ObjectAceFlags objectFlags, Guid objectType)
        {
            if ((ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) != (objectFlags & ObjectAceFlags.ObjectAceTypePresent))
            {
                return false;
            }

            if (((ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) != 0) &&
                (!ObjectAceType.Equals(objectType)))
            {
                return false;
            }

            return true;
        }

        //
        // This method checks if the inheritedObjectType matches with the specified inherited object type
        // (Either both do not have an inherited object type or they have the same inherited object type)
        //
        internal bool InheritedObjectTypesMatch(ObjectAceFlags objectFlags, Guid inheritedObjectType)
        {
            if ((ObjectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != (objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent))
            {
                return false;
            }

            if (((ObjectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0) &&
                (!InheritedObjectAceType.Equals(inheritedObjectType)))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Static Parser

        //
        // Called by GenericAce.CreateFromBinaryForm to parse the binary form
        // of the object ACE and extract the useful pieces
        //

        internal static bool ParseBinaryForm(
            byte[] binaryForm,
            int offset,
            out AceQualifier qualifier,
            out int accessMask,
            out SecurityIdentifier sid,
            out ObjectAceFlags objectFlags,
            out Guid objectAceType,
            out Guid inheritedObjectAceType,
            out bool isCallback,
            out byte[] opaque)
        {
            byte[] guidArray = new byte[GuidLength];

            //
            // Verify the ACE header
            //

            VerifyHeader(binaryForm, offset);

            //
            // Verify the length field
            //

            if (binaryForm.Length - offset < HeaderLength + AccessMaskLength + ObjectFlagsLength + SecurityIdentifier.MinBinaryLength)
            {
                goto InvalidParameter;
            }

            //
            // Identify callback ACE types
            //

            AceType type = (AceType)binaryForm[offset];

            if (type == AceType.AccessAllowedObject ||
                type == AceType.AccessDeniedObject ||
                type == AceType.SystemAuditObject ||
                type == AceType.SystemAlarmObject)
            {
                isCallback = false;
            }
            else if (type == AceType.AccessAllowedCallbackObject ||
                type == AceType.AccessDeniedCallbackObject ||
                type == AceType.SystemAuditCallbackObject ||
                type == AceType.SystemAlarmCallbackObject)
            {
                isCallback = true;
            }
            else
            {
                goto InvalidParameter;
            }

            //
            // Compute the qualifier from the ACE type
            //

            if (type == AceType.AccessAllowedObject ||
                type == AceType.AccessAllowedCallbackObject)
            {
                qualifier = AceQualifier.AccessAllowed;
            }
            else if (type == AceType.AccessDeniedObject ||
                type == AceType.AccessDeniedCallbackObject)
            {
                qualifier = AceQualifier.AccessDenied;
            }
            else if (type == AceType.SystemAuditObject ||
                type == AceType.SystemAuditCallbackObject)
            {
                qualifier = AceQualifier.SystemAudit;
            }
            else if (type == AceType.SystemAlarmObject ||
                type == AceType.SystemAlarmCallbackObject)
            {
                qualifier = AceQualifier.SystemAlarm;
            }
            else
            {
                goto InvalidParameter;
            }

            int baseOffset = offset + HeaderLength;
            int offsetLocal = 0;

            accessMask =
                unchecked((int)(
                (((uint)binaryForm[baseOffset + 0]) << 0) +
                (((uint)binaryForm[baseOffset + 1]) << 8) +
                (((uint)binaryForm[baseOffset + 2]) << 16) +
                (((uint)binaryForm[baseOffset + 3]) << 24)));

            offsetLocal += AccessMaskLength;

            objectFlags =
                (ObjectAceFlags)(
                (((uint)binaryForm[baseOffset + offsetLocal + 0]) << 0) +
                (((uint)binaryForm[baseOffset + offsetLocal + 1]) << 8) +
                (((uint)binaryForm[baseOffset + offsetLocal + 2]) << 16) +
                (((uint)binaryForm[baseOffset + offsetLocal + 3]) << 24));

            offsetLocal += ObjectFlagsLength;

            if ((objectFlags & ObjectAceFlags.ObjectAceTypePresent) != 0)
            {
                for (int i = 0; i < GuidLength; i++)
                {
                    guidArray[i] = binaryForm[baseOffset + offsetLocal + i];
                }

                offsetLocal += GuidLength;
            }
            else
            {
                for (int i = 0; i < GuidLength; i++)
                {
                    guidArray[i] = 0;
                }
            }

            objectAceType = new Guid(guidArray);

            if ((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0)
            {
                for (int i = 0; i < GuidLength; i++)
                {
                    guidArray[i] = binaryForm[baseOffset + offsetLocal + i];
                }

                offsetLocal += GuidLength;
            }
            else
            {
                for (int i = 0; i < GuidLength; i++)
                {
                    guidArray[i] = 0;
                }
            }

            inheritedObjectAceType = new Guid(guidArray);

            sid = new SecurityIdentifier(binaryForm, baseOffset + offsetLocal);

            opaque = null;

            int aceLength = (binaryForm[offset + 3] << 8) + (binaryForm[offset + 2] << 0);

            if (aceLength % 4 != 0)
            {
                goto InvalidParameter;
            }

            int opaqueLength = (aceLength - HeaderLength - AccessMaskLength - ObjectFlagsLength - (byte)sid.BinaryLength);

            if ((objectFlags & ObjectAceFlags.ObjectAceTypePresent) != 0)
            {
                opaqueLength -= GuidLength;
            }

            if ((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0)
            {
                opaqueLength -= GuidLength;
            }

            if (opaqueLength > 0)
            {
                opaque = new byte[opaqueLength];

                for (int i = 0; i < opaqueLength; i++)
                {
                    opaque[i] = binaryForm[offset + aceLength - opaqueLength + i];
                }
            }

            return true;

        InvalidParameter:

            qualifier = 0;
            accessMask = 0;
            sid = null;
            objectFlags = 0;
            objectAceType = Guid.NewGuid();
            inheritedObjectAceType = Guid.NewGuid();
            isCallback = false;
            opaque = null;

            return false;
        }

        #endregion

        #region Public Properties

        //
        // Returns the object flags field of this ACE
        //

        public ObjectAceFlags ObjectAceFlags
        {
            get
            {
                return _objectFlags;
            }

            set
            {
                _objectFlags = value;
            }
        }

        //
        // Allows querying and setting the object type GUID for this ACE
        //

        public Guid ObjectAceType
        {
            get
            {
                return _objectAceType;
            }

            set
            {
                _objectAceType = value;
            }
        }

        //
        // Allows querying and setting the inherited object type
        // GUID for this ACE
        //

        public Guid InheritedObjectAceType
        {
            get
            {
                return _inheritedObjectAceType;
            }

            set
            {
                _inheritedObjectAceType = value;
            }
        }

        public /* sealed */ override int BinaryLength
        {
            get
            {
                //
                // The GUIDs may or may not be present depending on the object flags
                //

                int GuidLengths =
                    ((_objectFlags & ObjectAceFlags.ObjectAceTypePresent) != 0 ? GuidLength : 0) +
                    ((_objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0 ? GuidLength : 0);

                return (HeaderLength + AccessMaskLength + ObjectFlagsLength + GuidLengths + SecurityIdentifier.BinaryLength + OpaqueLength);
            }
        }

        public static int MaxOpaqueLength(bool isCallback)
        {
            return ushort.MaxValue - HeaderLength - AccessMaskLength - ObjectFlagsLength - 2 * GuidLength - SecurityIdentifier.MaxBinaryLength;
        }

        internal override int MaxOpaqueLengthInternal
        {
            get { return MaxOpaqueLength(IsCallback); }
        }

        #endregion

        #region Public Methods

        //
        // Copies the binary representation of the ACE into a given array
        // starting at the given offset.
        //

        public /* sealed */ override void GetBinaryForm(byte[] binaryForm, int offset)
        {
            //
            // Populate the header
            //

            MarshalHeader(binaryForm, offset);

            int baseOffset = offset + HeaderLength;
            int offsetLocal = 0;

            //
            // Store the access mask in the big-endian format
            //
            unchecked
            {
                binaryForm[baseOffset + 0] = (byte)(AccessMask >> 0);
                binaryForm[baseOffset + 1] = (byte)(AccessMask >> 8);
                binaryForm[baseOffset + 2] = (byte)(AccessMask >> 16);
                binaryForm[baseOffset + 3] = (byte)(AccessMask >> 24);
            }

            offsetLocal += AccessMaskLength;

            //
            // Store the object flags in the big-endian format
            //

            binaryForm[baseOffset + offsetLocal + 0] = (byte)(((uint)ObjectAceFlags) >> 0);
            binaryForm[baseOffset + offsetLocal + 1] = (byte)(((uint)ObjectAceFlags) >> 8);
            binaryForm[baseOffset + offsetLocal + 2] = (byte)(((uint)ObjectAceFlags) >> 16);
            binaryForm[baseOffset + offsetLocal + 3] = (byte)(((uint)ObjectAceFlags) >> 24);

            offsetLocal += ObjectFlagsLength;

            //
            // Store the object type GUIDs if present
            //

            if ((ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) != 0)
            {
                ObjectAceType.ToByteArray().CopyTo(binaryForm, baseOffset + offsetLocal);
                offsetLocal += GuidLength;
            }

            if ((ObjectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0)
            {
                InheritedObjectAceType.ToByteArray().CopyTo(binaryForm, baseOffset + offsetLocal);
                offsetLocal += GuidLength;
            }

            //
            // Store the SID
            //

            SecurityIdentifier.GetBinaryForm(binaryForm, baseOffset + offsetLocal);
            offsetLocal += SecurityIdentifier.BinaryLength;

            //
            // Finally, if opaque is supported, store it
            //

            if (GetOpaque() != null)
            {
                if (OpaqueLength > MaxOpaqueLengthInternal)
                {
                    Debug.Fail("OpaqueLength somehow managed to exceed MaxOpaqueLength");
                    // Replacing SystemException with InvalidOperationException. It's not a perfect fit,
                    // but it's the best exception type available to indicate a failure because
                    // of a bug in the ACE itself.
                    throw new InvalidOperationException();
                }

                GetOpaque().CopyTo(binaryForm, baseOffset + offsetLocal);
            }
        }
        #endregion
    }
}
