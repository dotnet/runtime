// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Classes:  Access Control List (ACL) family of classes
**
**
===========================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace System.Security.AccessControl
{
    public sealed class AceEnumerator : IEnumerator
    {
        #region Private Members

        //
        // Current enumeration index
        //

        private int _current;

        //
        // Parent collection
        //

        private readonly GenericAcl _acl;

        #endregion

        #region Constructors

        internal AceEnumerator(GenericAcl collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _acl = collection;
            Reset();
        }

        #endregion

        #region IEnumerator Interface

        object IEnumerator.Current
        {
            get
            {
                if (_current == -1 ||
                    _current >= _acl.Count)
                {
                    throw new InvalidOperationException(SR.Arg_InvalidOperationException);
                }

                return _acl[_current];
            }
        }

        public GenericAce Current
        {
            get { return (GenericAce)((IEnumerator)this).Current!; }
        }

        public bool MoveNext()
        {
            _current++;

            return (_current < _acl.Count);
        }

        public void Reset()
        {
            _current = -1;
        }

        #endregion
    }


    public abstract class GenericAcl : ICollection
    {
        #region Constructors

        protected GenericAcl()
        { }

        #endregion

        #region Public Constants

        //
        // ACL revisions
        //

        public static readonly byte AclRevision = 2;
        public static readonly byte AclRevisionDS = 4;

        //
        // Maximum length of a binary representation of the ACL
        //

        public static readonly int MaxBinaryLength = ushort.MaxValue;

        #endregion

        #region Protected Members

        //
        //  Define an ACL and the ACE format.  The structure of an ACL header
        //  followed by one or more ACEs.  Pictorally the structure of an ACL header
        //  is as follows:
        //
        //       3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
        //       1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
        //      +-------------------------------+---------------+---------------+
        //      |            AclSize            |      Sbz1     |  AclRevision  |
        //      +-------------------------------+---------------+---------------+
        //      |              Sbz2             |           AceCount            |
        //      +-------------------------------+-------------------------------+
        //

        internal const int HeaderLength = 8;

        #endregion

        #region Public Properties

        //
        // Returns the revision of the ACL
        //

        public abstract byte Revision { get; }

        //
        // Returns the length of the binary representation of the ACL
        //

        public abstract int BinaryLength { get; }

        //
        // Retrieves the ACE at a specified index
        //

        public abstract GenericAce this[int index] { get; set; }

        #endregion

        #region Public Methods

        //
        // Returns the binary representation of the ACL
        //

        public abstract void GetBinaryForm(byte[] binaryForm, int offset);

        #endregion

        #region ICollection Implementation

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new RankException(SR.Rank_MultiDimNotSupported);
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            else if (array.Length - index < Count)
            {
                throw new ArgumentOutOfRangeException(nameof(array), SR.ArgumentOutOfRange_ArrayTooSmall);
            }

            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index + i);
            }
        }

        public void CopyTo(GenericAce[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        public abstract int Count { get; }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public virtual object SyncRoot
        {
            get { return this; }
        }

        #endregion

        #region IEnumerable Implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AceEnumerator(this);
        }

        public AceEnumerator GetEnumerator()
        {
            return (AceEnumerator)((IEnumerable)this).GetEnumerator();
        }

        #endregion
    }


    public sealed class RawAcl : GenericAcl
    {
        #region Private Members

        private byte _revision;
        private List<GenericAce> _aces;

        #endregion

        #region Private Methods

        private static void VerifyHeader(byte[] binaryForm, int offset, out byte revision, out int count, out int length)
        {
            if (binaryForm == null)
            {
                throw new ArgumentNullException(nameof(binaryForm));
            }

            if (offset < 0)
            {
                //
                // Offset must not be negative
                //

                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (binaryForm.Length - offset < HeaderLength)
            {
                //
                // We expect at least the ACL header
                //

                goto InvalidParameter;
            }

            revision = binaryForm[offset + 0];
            length = (binaryForm[offset + 2] << 0) + (binaryForm[offset + 3] << 8);
            count = (binaryForm[offset + 4] << 0) + (binaryForm[offset + 5] << 8);

            if (length > binaryForm.Length - offset)
            {
                //
                // Reported length of ACL ought to be no longer than the
                // length of the buffer passed in
                //

                goto InvalidParameter;
            }

            return;

        InvalidParameter:

            throw new ArgumentOutOfRangeException(nameof(binaryForm), SR.ArgumentOutOfRange_ArrayTooSmall);
        }

        private void MarshalHeader(byte[] binaryForm, int offset)
        {
            if (binaryForm == null)
            {
                throw new ArgumentNullException(nameof(binaryForm));
            }
            else if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            else if (BinaryLength > MaxBinaryLength)
            {
                throw new InvalidOperationException(SR.AccessControl_AclTooLong);
            }
            else if (binaryForm.Length - offset < BinaryLength)
            {
                throw new ArgumentOutOfRangeException(nameof(binaryForm), SR.ArgumentOutOfRange_ArrayTooSmall);
            }

            binaryForm[offset + 0] = Revision;
            binaryForm[offset + 1] = 0;
            binaryForm[offset + 2] = unchecked((byte)(BinaryLength >> 0));
            binaryForm[offset + 3] = (byte)(BinaryLength >> 8);
            binaryForm[offset + 4] = unchecked((byte)(Count >> 0));
            binaryForm[offset + 5] = (byte)(Count >> 8);
            binaryForm[offset + 6] = 0;
            binaryForm[offset + 7] = 0;
        }

        [MemberNotNull(nameof(_aces))]
        internal void SetBinaryForm(byte[] binaryForm, int offset)
        {
            int count, length;

            //
            // Verify the header and extract interesting header info
            //

            VerifyHeader(binaryForm, offset, out _revision, out count, out length);

            //
            // Remember how far ahead the binary form should end (for later verification)
            //

            length += offset;

            offset += HeaderLength;

            _aces = new List<GenericAce>(count);
            int binaryLength = HeaderLength;

            for (int i = 0; i < count; i++)
            {
                GenericAce ace = GenericAce.CreateFromBinaryForm(binaryForm, offset);

                int aceLength = ace.BinaryLength;

                if (binaryLength + aceLength > MaxBinaryLength)
                {
                    //
                    // The ACE was too long - it would overflow the ACL maximum length
                    //

                    throw new ArgumentException(SR.ArgumentException_InvalidAclBinaryForm, nameof(binaryForm));
                }

                _aces.Add(ace);

                if (aceLength % 4 != 0)
                {
                    //
                    // This indicates a bug in one of the ACE classes.
                    // Binary length of an ace must ALWAYS be divisible by 4.
                    //

                    Debug.Assert(false, "aceLength % 4 != 0");
                    // Replacing SystemException with InvalidOperationException. This code path
                    // indicates a bad ACE, but I don't know of a great exception to represent that.
                    // InvalidOperation seems to be the closest, though it's definitely not exactly
                    // right for this scenario.
                    throw new InvalidOperationException();
                }

                binaryLength += aceLength;

                if (_revision == AclRevisionDS)
                {
                    //
                    // Increment the offset by the advertised length rather than the
                    // actual binary length. (Ideally these two should match, but for
                    // object aces created through ADSI, the actual length is 32 bytes
                    // less than the allocated size of the ACE. This is a bug in ADSI.)
                    //
                    offset += (binaryForm[offset + 2] << 0) + (binaryForm[offset + 3] << 8);
                }
                else
                {
                    offset += aceLength;
                }

                //
                // Verify that no more than the advertised length of the ACL was consumed
                //

                if (offset > length)
                {
                    goto InvalidParameter;
                }
            }

            return;

        InvalidParameter:

            throw new ArgumentException(
                SR.ArgumentException_InvalidAclBinaryForm,
                nameof(binaryForm));
        }

        #endregion

        #region Constructors

        //
        // Creates an empty ACL
        //

        public RawAcl(byte revision, int capacity)
            : base()
        {
            _revision = revision;
            _aces = new List<GenericAce>(capacity);
        }

        //
        // Creates an ACL from its binary representation
        //

        public RawAcl(byte[] binaryForm, int offset)
            : base()
        {
            SetBinaryForm(binaryForm, offset);
        }

        #endregion

        #region Public Properties

        //
        // Returns the revision of the ACL
        //

        public override byte Revision
        {
            get { return _revision; }
        }

        //
        // Returns the number of ACEs in the ACL
        //

        public override int Count
        {
            get { return _aces.Count; }
        }

        //
        // Returns the length of the binary representation of the ACL
        //

        public override int BinaryLength
        {
            get
            {
                int binaryLength = HeaderLength;

                for (int i = 0; i < Count; i++)
                {
                    GenericAce ace = _aces[i];
                    binaryLength += ace.BinaryLength;
                }

                return binaryLength;
            }
        }

        #endregion

        #region Public Methods

        //
        // Returns the binary representation of the ACL
        //

        public override void GetBinaryForm(byte[] binaryForm, int offset)
        {
            //
            // Populate the header
            //

            MarshalHeader(binaryForm, offset);
            offset += HeaderLength;

            for (int i = 0; i < Count; i++)
            {
                GenericAce ace = _aces[i];

                ace.GetBinaryForm(binaryForm, offset);

                int aceLength = ace.BinaryLength;

                if (aceLength % 4 != 0)
                {
                    //
                    // This indicates a bug in one of the ACE classes.
                    // Binary length of an ace must ALWAYS be divisible by 4.
                    //

                    Debug.Assert(false, "aceLength % 4 != 0");
                    // Replacing SystemException with InvalidOperationException. This code path
                    // indicates a bad ACE, but I don't know of a great exception to represent that.
                    // InvalidOperation seems to be the closest, though it's definitely not exactly
                    // right for this scenario.
                    throw new InvalidOperationException();
                }

                offset += aceLength;
            }
        }

        //
        // Return an ACE at a particular index
        // The ACE is not cloned prior to returning, enabling the caller
        // to modify the ACE in place (a potentially dangerous operation)
        //

        public override GenericAce this[int index]
        {
            get
            {
                return _aces[index];
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.BinaryLength % 4 != 0)
                {
                    //
                    // This indicates a bug in one of the ACE classes.
                    // Binary length of an ace must ALWAYS be divisible by 4.
                    //

                    Debug.Assert(false, "aceLength % 4 != 0");
                    // Replacing SystemException with InvalidOperationException. This code path
                    // indicates a bad ACE, but I don't know of a great exception to represent that.
                    // InvalidOperation seems to be the closest, though it's definitely not exactly
                    // right for this scenario.
                    throw new InvalidOperationException();
                }

                int newBinaryLength = BinaryLength - (index < _aces.Count ? _aces[index].BinaryLength : 0) + value.BinaryLength;

                if (newBinaryLength > MaxBinaryLength)
                {
                    throw new OverflowException(SR.AccessControl_AclTooLong);
                }

                _aces[index] = value;
            }
        }

        //
        // Adds an ACE at the specified index
        //

        public void InsertAce(int index, GenericAce ace)
        {
            if (ace == null)
            {
                throw new ArgumentNullException(nameof(ace));
            }

            if (BinaryLength + ace.BinaryLength > MaxBinaryLength)
            {
                throw new OverflowException(SR.AccessControl_AclTooLong);
            }

            _aces.Insert(index, ace);
        }

        //
        // Removes an ACE at the specified index
        //

        public void RemoveAce(int index)
        {
            _aces.RemoveAt(index);
        }

        #endregion
    }


    public abstract class CommonAcl : GenericAcl
    {
        #region Add/Remove Logic Support

        [Flags]
        private enum AF    // ACE flags
        {
            CI = 0x8,     // container inherit
            OI = 0x4,     // object inherit
            IO = 0x2,     // inherit only
            NP = 0x1,     // no propagate inherit
            Invalid = NP, // not a valid combination of flags
        }

        [Flags]
        private enum PM    // Propagation matrix
        {
            F = 0x10,     // folder
            CF = 0x08,    // child folder
            CO = 0x04,    // child object
            GF = 0x02,    // grandchild folder
            GO = 0x01,    // grandchild object
            Invalid = GO, // not a valid combination of flags
        }

        private static readonly PM[] s_AFtoPM = CreateAFtoPMConversionMatrix();    // AceFlags-to-Propagation conversion matrix
        private static readonly AF[] s_PMtoAF = CreatePMtoAFConversionMatrix();    // Propagation-to-AceFlags conversion matrix

        private static PM[] CreateAFtoPMConversionMatrix()
        {
            var afToPm = new PM[16];

            for (int i = 0; i < afToPm.Length; i++)
            {
                afToPm[i] = PM.Invalid;
            }

            //
            // This table specifies what effect various combinations of inheritance bits
            // have on how ACEs are inherited onto child objects
            // Important: Not all combinations of inheritance bits are valid
            //

            afToPm[(int)(   0   |   0   |   0   |   0   )] = PM.F |   0   |   0   |   0   |     0;
            afToPm[(int)(   0   | AF.OI |   0   |   0   )] = PM.F |   0   | PM.CO |   0   | PM.GO;
            afToPm[(int)(   0   | AF.OI |   0   | AF.NP )] = PM.F |   0   | PM.CO |   0   |     0;
            afToPm[(int)(   0   | AF.OI | AF.IO |   0   )] =   0  |   0   | PM.CO |   0   | PM.GO;
            afToPm[(int)(   0   | AF.OI | AF.IO | AF.NP )] =   0  |   0   | PM.CO |   0   |     0;
            afToPm[(int)( AF.CI |   0   |   0   |   0   )] = PM.F | PM.CF |   0   | PM.GF |     0;
            afToPm[(int)( AF.CI |   0   |   0   | AF.NP )] = PM.F | PM.CF |   0   |   0   |     0;
            afToPm[(int)( AF.CI |   0   | AF.IO |   0   )] =   0  | PM.CF |   0   | PM.GF |     0;
            afToPm[(int)( AF.CI |   0   | AF.IO | AF.NP )] =   0  | PM.CF |   0   |   0   |     0;
            afToPm[(int)( AF.CI | AF.OI |   0   |   0   )] = PM.F | PM.CF | PM.CO | PM.GF | PM.GO;
            afToPm[(int)( AF.CI | AF.OI |   0   | AF.NP )] = PM.F | PM.CF | PM.CO |   0   |     0;
            afToPm[(int)( AF.CI | AF.OI | AF.IO |   0   )] =   0  | PM.CF | PM.CO | PM.GF | PM.GO;
            afToPm[(int)( AF.CI | AF.OI | AF.IO | AF.NP )] =   0  | PM.CF | PM.CO |   0   |     0;

            return afToPm;
        }

        private static AF[] CreatePMtoAFConversionMatrix()
        {
            var pmToAf = new AF[32];

            for (int i = 0; i < pmToAf.Length; i++)
            {
                pmToAf[i] = AF.Invalid;
            }

            //
            // This table is a reverse lookup table of the AFtoPM table
            // Given how inheritance is applied to child objects and containers,
            // it helps figure out whether that pattern is expressible using
            // the four ACE inheritance bits
            //

            pmToAf[(int)( PM.F |   0   |   0   |   0   |   0   )] =    0   |   0   |   0   |     0;
            pmToAf[(int)( PM.F |   0   | PM.CO |   0   | PM.GO )] =    0   | AF.OI |   0   |     0;
            pmToAf[(int)( PM.F |   0   | PM.CO |   0   |   0   )] =    0   | AF.OI |   0   | AF.NP;
            pmToAf[(int)(   0  |   0   | PM.CO |   0   | PM.GO )] =    0   | AF.OI | AF.IO |     0;
            pmToAf[(int)(   0  |   0   | PM.CO |   0   |   0   )] =    0   | AF.OI | AF.IO | AF.NP;
            pmToAf[(int)( PM.F | PM.CF |   0   | PM.GF |   0   )] =  AF.CI |   0   |   0   |     0;
            pmToAf[(int)( PM.F | PM.CF |   0   |   0   |   0   )] =  AF.CI |   0   |   0   | AF.NP;
            pmToAf[(int)(   0  | PM.CF |   0   | PM.GF |   0   )] =  AF.CI |   0   | AF.IO |     0;
            pmToAf[(int)(   0  | PM.CF |   0   |   0   |   0   )] =  AF.CI |   0   | AF.IO | AF.NP;
            pmToAf[(int)( PM.F | PM.CF | PM.CO | PM.GF | PM.GO )] =  AF.CI | AF.OI |   0   |     0;
            pmToAf[(int)( PM.F | PM.CF | PM.CO |   0   |   0   )] =  AF.CI | AF.OI |   0   | AF.NP;
            pmToAf[(int)(   0  | PM.CF | PM.CO | PM.GF | PM.GO )] =  AF.CI | AF.OI | AF.IO |     0;
            pmToAf[(int)(   0  | PM.CF | PM.CO |   0   |   0   )] =  AF.CI | AF.OI | AF.IO | AF.NP;

            return pmToAf;
        }

        //
        // Canonicalizes AceFlags into a form that the mapping tables understand
        //

        private static AF AFFromAceFlags(AceFlags aceFlags, bool isDS)
        {
            AF af = 0;

            if ((aceFlags & AceFlags.ContainerInherit) != 0)
            {
                af |= AF.CI;
            }

            //
            // ObjectInherit applies only to regular aces not object aces
            // so it can be ignored in the object aces case
            //
            if ((!isDS) && ((aceFlags & AceFlags.ObjectInherit) != 0))
            {
                af |= AF.OI;
            }

            if ((aceFlags & AceFlags.InheritOnly) != 0)
            {
                af |= AF.IO;
            }

            if ((aceFlags & AceFlags.NoPropagateInherit) != 0)
            {
                af |= AF.NP;
            }

            return af;
        }

        //
        // Converts lookup table representation of AceFlags into the "public" form
        //

        private static AceFlags AceFlagsFromAF(AF af, bool isDS)
        {
            AceFlags aceFlags = 0;

            if ((af & AF.CI) != 0)
            {
                aceFlags |= AceFlags.ContainerInherit;
            }

            //
            // ObjectInherit applies only to regular aces not object aces
            // so it can be ignored in the object aces case
            //
            if ((!isDS) && ((af & AF.OI) != 0))
            {
                aceFlags |= AceFlags.ObjectInherit;
            }

            if ((af & AF.IO) != 0)
            {
                aceFlags |= AceFlags.InheritOnly;
            }

            if ((af & AF.NP) != 0)
            {
                aceFlags |= AceFlags.NoPropagateInherit;
            }

            return aceFlags;
        }

        //
        // Implements the merge of inheritance bits during the 'ADD' operation
        //

        private static bool MergeInheritanceBits(AceFlags left, AceFlags right, bool isDS, out AceFlags result)
        {
            result = 0;

            AF leftAF = AFFromAceFlags(left, isDS);
            AF rightAF = AFFromAceFlags(right, isDS);

            PM leftPM = s_AFtoPM[(int)leftAF];
            PM rightPM = s_AFtoPM[(int)rightAF];

            if (leftPM == PM.Invalid || rightPM == PM.Invalid)
            {
                return false; // incorrect ACE flags?
            }

            PM resultPM = leftPM | rightPM;
            AF resultAF = s_PMtoAF[(int)resultPM];

            if (resultAF == AF.Invalid)
            {
                return false;
            }
            else
            {
                result = AceFlagsFromAF(resultAF, isDS);
                return true;
            }
        }

        private static bool RemoveInheritanceBits(AceFlags existing, AceFlags remove, bool isDS, out AceFlags result, out bool total)
        {
            result = 0;
            total = false;

            AF leftAF = AFFromAceFlags(existing, isDS);
            AF rightAF = AFFromAceFlags(remove, isDS);

            PM leftPM = s_AFtoPM[(int)leftAF];
            PM rightPM = s_AFtoPM[(int)rightAF];

            if (leftPM == PM.Invalid || rightPM == PM.Invalid)
            {
                return false; // incorrect ACE flags?
            }

            PM resultPM;
            unchecked { resultPM = leftPM & ~rightPM; }

            //
            // If the resulting propagation matrix is zero,
            // communicate back the fact that removal is "total"
            //

            if (resultPM == 0)
            {
                total = true;
                return true;
            }

            AF resultAF = s_PMtoAF[(int)resultPM];

            if (resultAF == AF.Invalid)
            {
                return false;
            }
            else
            {
                result = AceFlagsFromAF(resultAF, isDS);
                return true;
            }
        }

        #endregion

        #region Private Members

        private readonly RawAcl _acl;
        private bool _isDirty;
        private readonly bool _isCanonical;
        private readonly bool _isContainer;

        //
        // To distinguish between a directory object acl and other common acls.
        //

        private readonly bool _isDS;

        #endregion

        #region Private Methods

        private void CanonicalizeIfNecessary()
        {
            if (_isDirty)
            {
                Canonicalize(false, this is DiscretionaryAcl);
                _isDirty = false;
            }
        }

        private enum ComparisonResult
        {
            LessThan,
            EqualTo,
            GreaterThan,
        }

        //
        // Compares two discretionary ACEs and returns
        //    LessThan if ace1 < ace2
        //    EqualTo if ace1 == ace2
        //    GreaterThan if ace1 > ace2
        //
        // The order is:
        //        - explicit Access Denied ACEs
        //          [regular aces first, then object aces]
        //        - explicit Access Allowed ACEs
        //          [regular aces first, then object aces]
        //        - inherited ACEs (in the original order )
        //        - user-defined ACEs (in the original order )
        //

        private static int DaclAcePriority(GenericAce ace)
        {
            int result;
            AceType type = ace.AceType;

            if ((ace.AceFlags & AceFlags.Inherited) != 0)
            {
                //
                // inherited aces are at the end as a group
                //

                result = 2 * ushort.MaxValue + ace._indexInAcl;
            }
            else if (type == AceType.AccessDenied ||
                type == AceType.AccessDeniedCallback)
            {
                result = 0;
            }
            else if (type == AceType.AccessDeniedObject ||
                type == AceType.AccessDeniedCallbackObject)
            {
                result = 1;
            }
            else if (type == AceType.AccessAllowed ||
                type == AceType.AccessAllowedCallback)
            {
                result = 2;
            }
            else if (type == AceType.AccessAllowedObject ||
                type == AceType.AccessAllowedCallbackObject)
            {
                result = 3;
            }
            else
            {
                //
                // custom aces are at the second group
                //
                result = ushort.MaxValue + ace._indexInAcl;
            }

            return result;
        }

        //
        // Compares two system ACEs and returns
        //    LessThan if ace1 < ace2
        //    EqualTo if ace1 == ace2
        //    GreaterThan if ace1 > ace2
        //
        // The order is:
        //        - explicit audit or alarm ACEs
        //        - explicit audit or alarm object ACEs
        //        - inherited ACEs (in the original order )
        //        - user-defined ACEs (in the original order )
        //

        private static int SaclAcePriority(GenericAce ace)
        {
            int result;
            AceType type = ace.AceType;

            if ((ace.AceFlags & AceFlags.Inherited) != 0)
            {
                result = 2 * ushort.MaxValue + ace._indexInAcl;
            }
            else if (type == AceType.SystemAudit ||
                type == AceType.SystemAlarm ||
                type == AceType.SystemAuditCallback ||
                type == AceType.SystemAlarmCallback)
            {
                result = 0;
            }
            else if (type == AceType.SystemAuditObject ||
                type == AceType.SystemAlarmObject ||
                type == AceType.SystemAuditCallbackObject ||
                type == AceType.SystemAlarmCallbackObject)
            {
                result = 1;
            }
            else
            {
                result = ushort.MaxValue + ace._indexInAcl;
            }

            return result;
        }

        private static ComparisonResult CompareAces(GenericAce ace1, GenericAce ace2, bool isDacl)
        {
            int ace1Priority = isDacl ? DaclAcePriority(ace1) : SaclAcePriority(ace1);
            int ace2Priority = isDacl ? DaclAcePriority(ace2) : SaclAcePriority(ace2);

            if (ace1Priority < ace2Priority)
            {
                return ComparisonResult.LessThan;
            }
            else if (ace1Priority > ace2Priority)
            {
                return ComparisonResult.GreaterThan;
            }
            else
            {
                if (ace1 is KnownAce k_ace1 && ace2 is KnownAce k_ace2)
                {
                    int result = k_ace1.SecurityIdentifier.CompareTo(k_ace2.SecurityIdentifier);

                    if (result < 0)
                    {
                        return ComparisonResult.LessThan;
                    }
                    else if (result > 0)
                    {
                        return ComparisonResult.GreaterThan;
                    }
                }

                return ComparisonResult.EqualTo;
            }
        }

        private void QuickSort(int left, int right, bool isDacl)
        {
            GenericAce pivot;
            int leftHold, rightHold;
            int pivotIndex;

            if (left >= right)
            {
                return;
            }

            leftHold = left;
            rightHold = right;

            pivot = _acl[left];
            pivotIndex = left;

            while (left < right)
            {
                // while (( _acl[right] >= pivot ) && ( left < right ))
                while ((ComparisonResult.LessThan != CompareAces(_acl[right], pivot, isDacl)) && (left < right))
                {
                    right--;
                }

                if (left != right)
                {
                    _acl[left] = _acl[right];
                    left++;
                }

                // while (( _acl[left] <= pivot ) && ( left < right ))
                while ((ComparisonResult.GreaterThan != CompareAces(_acl[left], pivot, isDacl)) && (left < right))
                {
                    left++;
                }

                if (left != right)
                {
                    _acl[right] = _acl[left];
                    right--;
                }
            }

            _acl[left] = pivot;
            pivotIndex = left;
            left = leftHold;
            right = rightHold;

            if (left < pivotIndex)
            {
                QuickSort(left, pivotIndex - 1, isDacl);
            }

            if (right > pivotIndex)
            {
                QuickSort(pivotIndex + 1, right, isDacl);
            }
        }

        //
        // Inspects the ACE, modifies it by stripping away unnecessary or
        // meaningless flags.
        // Returns 'true' if the ACE should remain in the ACL, 'false' otherwise
        //

        private bool InspectAce(ref GenericAce ace, bool isDacl)
        {
            const AceFlags AuditFlags =
                AceFlags.SuccessfulAccess |
                AceFlags.FailedAccess;

            const AceFlags InheritFlags =
                AceFlags.ObjectInherit |
                AceFlags.ContainerInherit |
                AceFlags.NoPropagateInherit |
                AceFlags.InheritOnly;

            //
            // Any ACE without at least one bit set in the access mask can be removed
            //

            KnownAce? knownAce = ace as KnownAce;

            if (knownAce != null)
            {
                if (knownAce.AccessMask == 0)
                {
                    return false;
                }
            }

            if (!IsContainer)
            {
                //
                // On a leaf object ACL, inheritance bits are meaningless.
                // Specifically, an ACE marked "inherit-only" will never participate
                // in access control and can be removed.
                // Similarly, an ACE marked "container-inherit", "no-propagate-inherit"
                // or "object-inherit" can have those bits cleared since they carry
                // no meaning.
                //

                if ((ace.AceFlags & AceFlags.InheritOnly) != 0)
                {
                    return false;
                }

                if ((ace.AceFlags & InheritFlags) != 0)
                {
                    unchecked { ace.AceFlags &= ~InheritFlags; }
                }
            }
            else
            {
                //
                // Without either "container inherit" or "object inherit" to go with it,
                // the InheritOnly bit is meaningless and the entire ACE can be removed.
                //

                if (((ace.AceFlags & AceFlags.InheritOnly) != 0) &&
                    ((ace.AceFlags & AceFlags.ContainerInherit) == 0) &&
                    ((ace.AceFlags & AceFlags.ObjectInherit) == 0))
                {
                    return false;
                }

                //
                // Without either "container inherit" or "object inherit" to go with it,
                // the NoPropagateInherit bit is meaningless and can be turned off.
                //
                if (((ace.AceFlags & AceFlags.NoPropagateInherit) != 0) &&
                    ((ace.AceFlags & AceFlags.ContainerInherit) == 0) &&
                    ((ace.AceFlags & AceFlags.ObjectInherit) == 0))
                {
                    unchecked { ace.AceFlags &= ~AceFlags.NoPropagateInherit; }
                }
            }

            QualifiedAce? qualifiedAce = knownAce as QualifiedAce;

            if (isDacl)
            {
                //
                // There is no place for audit flags on a DACL
                //

                unchecked { ace.AceFlags &= ~AuditFlags; }

                if (qualifiedAce != null)
                {
                    //
                    // Qualified ACEs in a DACL must be allow or deny ACEs
                    //

                    if (qualifiedAce.AceQualifier != AceQualifier.AccessAllowed &&
                        qualifiedAce.AceQualifier != AceQualifier.AccessDenied)
                    {
                        return false;
                    }
                }
            }
            else
            {
                //
                // On a SACL, any ACE that does not specify Success or Failure
                // flags can be removed
                //

                if ((ace.AceFlags & AuditFlags) == 0)
                {
                    return false;
                }

                //
                // Qualified ACEs in a SACL must be audit ACEs
                //

                if (qualifiedAce != null)
                {
                    if (qualifiedAce.AceQualifier != AceQualifier.SystemAudit)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //
        // Strips meaningless flags from ACEs, removes meaningless ACEs
        //

        private void RemoveMeaninglessAcesAndFlags(bool isDacl)
        {
            //
            // Be warned: do NOT use the Count property because it has
            // side-effect of calling canonicalization.
            //

            for (int i = _acl.Count - 1; i >= 0; i--)
            {
                GenericAce ace = _acl[i];

                if (false == InspectAce(ref ace, isDacl))
                {
                    _acl.RemoveAce(i);
                }
            }
        }

        //
        // Converts the ACL to its canonical form
        //

        private void Canonicalize(bool compact, bool isDacl)
        {
            //
            // for quick sort to work, we must not allow the ace's indexes - which are constantly
            // changing during sorting - to influence our element's sorting order value. For
            // that purpose, we fix the ace's _indexInAcl here and use it for creating the
            // ace's sorting order value.
            //

            for (ushort aclIndex = 0; aclIndex < _acl.Count; aclIndex++)
            {
                _acl[aclIndex]._indexInAcl = aclIndex;
            }

            QuickSort(0, _acl.Count - 1, isDacl);

            if (compact)
            {
                for (int i = 0; i < Count - 1; i++)
                {
                    QualifiedAce? thisAce = _acl[i] as QualifiedAce;

                    if (thisAce == null)
                    {
                        continue;
                    }

                    QualifiedAce? nextAce = _acl[i + 1] as QualifiedAce;

                    if (nextAce == null)
                    {
                        continue;
                    }

                    if (true == MergeAces(ref thisAce, nextAce))
                    {
                        _acl.RemoveAce(i + 1);
                    }
                }
            }
        }

        //
        // This method determines whether the object type and inherited object type from the original ace
        // should be retained or not based on access mask and aceflags for a given split
        //
        private void GetObjectTypesForSplit(ObjectAce originalAce, int accessMask, AceFlags aceFlags, out ObjectAceFlags objectFlags, out Guid objectType, out Guid inheritedObjectType)
        {

            objectFlags = 0;
            objectType = Guid.Empty;
            inheritedObjectType = Guid.Empty;

            //
            // We should retain the object type if the access mask for this split contains any bits that refer to object type
            //
            if ((accessMask & ObjectAce.AccessMaskWithObjectType) != 0)
            {
                // keep the original ace's object flags and object type
                objectType = originalAce.ObjectAceType;
                objectFlags |= originalAce.ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent;
            }

            //
            // We should retain the inherited object type if the aceflags for this contains inheritance (ContainerInherit)
            //
            if ((aceFlags & AceFlags.ContainerInherit) != 0)
            {
                // keep the original ace's object flags and object type
                inheritedObjectType = originalAce.InheritedObjectAceType;
                objectFlags |= originalAce.ObjectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent;
            }
        }

        private bool ObjectTypesMatch(QualifiedAce ace, QualifiedAce newAce)
        {
            Guid objectType = (ace is ObjectAce) ? ((ObjectAce)ace).ObjectAceType : Guid.Empty;
            Guid newObjectType = (newAce is ObjectAce) ? ((ObjectAce)newAce).ObjectAceType : Guid.Empty;

            return objectType.Equals(newObjectType);
        }

        private bool InheritedObjectTypesMatch(QualifiedAce ace, QualifiedAce newAce)
        {
            Guid inheritedObjectType = (ace is ObjectAce) ? ((ObjectAce)ace).InheritedObjectAceType : Guid.Empty;
            Guid newInheritedObjectType = (newAce is ObjectAce) ? ((ObjectAce)newAce).InheritedObjectAceType : Guid.Empty;

            return inheritedObjectType.Equals(newInheritedObjectType);
        }

        private bool AccessMasksAreMergeable(QualifiedAce ace, QualifiedAce newAce)
        {
            //
            // The access masks are mergeable in any of the following conditions
            // 1. Object types match
            // 2. (Object types do not match) The existing ace does not have an object type and
            //     already contains all the bits of the new ace which refer to the object type
            //

            if (ObjectTypesMatch(ace, newAce))
            {
                // case 1
                return true;
            }

            ObjectAceFlags objectFlags = (ace is ObjectAce) ? ((ObjectAce)ace).ObjectAceFlags : ObjectAceFlags.None;
            if (((ace.AccessMask & newAce.AccessMask & ObjectAce.AccessMaskWithObjectType) == (newAce.AccessMask & ObjectAce.AccessMaskWithObjectType)) &&
                 ((objectFlags & ObjectAceFlags.ObjectAceTypePresent) == 0))
            {
                // case 2
                return true;
            }

            return false;
        }

        private bool AceFlagsAreMergeable(QualifiedAce ace, QualifiedAce newAce)
        {
            //
            // The ace flags can be considered for merge in any of the following conditions
            // 1. Inherited object types match
            // 2. (Inherited object types do not match) The existing ace does not have an inherited object type and
            //     already contains all the bits of the new ace
            //

            if (InheritedObjectTypesMatch(ace, newAce))
            {
                // case 1
                return true;
            }

            ObjectAceFlags objectFlags = (ace is ObjectAce) ? ((ObjectAce)ace).ObjectAceFlags : ObjectAceFlags.None;
            if ((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) == 0)
            {
                // case 2

                //
                // This method is called only when the access masks of the two aces are already confirmed to be exact matches
                // therefore the second condition of case 2 is already verified
                //
                Debug.Assert((ace.AccessMask & newAce.AccessMask) == newAce.AccessMask, "AceFlagsAreMergeable:: AccessMask of existing ace does not contain all access bits of new ace.");
                return true;
            }

            return false;
        }

        private bool GetAccessMaskForRemoval(QualifiedAce ace, ObjectAceFlags objectFlags, Guid objectType, ref int accessMask)
        {
            if ((ace.AccessMask & accessMask & ObjectAce.AccessMaskWithObjectType) != 0)
            {

                //
                // If the aces have access bits in common which refer to object types
                // then we follow these rules:
                //
                //       Remove    No OT    OT = A        OT = B
                // Existing
                //
                //   No OT          Remove   Invalid        Invalid
                //
                //   OT = A        Remove   Remove      Nothing Common
                //


                if (ace is ObjectAce objectAce)
                {
                    bool commonAccessBitsWithObjectTypeExist = true;

                    //
                    // if what we are trying to remove has an object type
                    // but the existing ace does not then this is an invalid case
                    //
                    if (((objectFlags & ObjectAceFlags.ObjectAceTypePresent) != 0) &&
                        ((objectAce.ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) == 0))
                    {
                        return false;
                    }

                    //
                    // if what we are trying to remove has no object type or
                    // if object types match (since at this point we have ensured that both have object types present)
                    // then we have common access bits with object type
                    //
                    commonAccessBitsWithObjectTypeExist = ((objectFlags & ObjectAceFlags.ObjectAceTypePresent) == 0) ||
                                                                                    objectAce.ObjectTypesMatch(objectFlags, objectType);
                    if (!commonAccessBitsWithObjectTypeExist)
                    {
                        accessMask &= ~ObjectAce.AccessMaskWithObjectType;
                    }
                }
                else if ((objectFlags & ObjectAceFlags.ObjectAceTypePresent) != 0)
                {
                    // the existing ace is a common ace and the one we're removing
                    // refers to a specific object type so this is invalid
                    return false;
                }
            }

            return true;

        }

        private bool GetInheritanceFlagsForRemoval(QualifiedAce ace, ObjectAceFlags objectFlags, Guid inheritedObjectType, ref AceFlags aceFlags)
        {
            if (((ace.AceFlags & AceFlags.ContainerInherit) != 0) && ((aceFlags & AceFlags.ContainerInherit) != 0))
            {

                //
                // If the aces have inheritance bits in common
                // then we follow these rules:
                //
                //       Remove    No IOT    IOT = A        IOT = B
                // Existing
                //
                //   No IOT          Remove   Invalid        Invalid
                //
                //   IOT = A        Remove   Remove      Nothing Common
                //


                if (ace is ObjectAce objectAce)
                {
                    bool commonInheritanceFlagsExist = true;

                    //
                    // if what we are trying to remove has an inherited object type
                    // but the existing ace does not then this is an invalid case
                    //
                    if (((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0) &&
                        ((objectAce.ObjectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent) == 0))
                    {
                        return false;
                    }

                    //
                    // if what we are trying to remove has no inherited object type or
                    // if inherited object types match then we have common inheritance flags
                    //
                    commonInheritanceFlagsExist = ((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) == 0) ||
                                                                       objectAce.InheritedObjectTypesMatch(objectFlags, inheritedObjectType);
                    if (!commonInheritanceFlagsExist)
                    {
                        aceFlags &= ~AceFlags.InheritanceFlags;
                    }
                }
                else if ((objectFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0)
                {
                    // the existing ace is a common ace and the one we're removing
                    // refers to a specific child type so this is invalid
                    return false;
                }
            }

            return true;

        }

        private static bool AceOpaquesMatch(QualifiedAce ace, QualifiedAce newAce)
        {
            byte[]? aceOpaque = ace.GetOpaque();
            byte[]? newAceOpaque = newAce.GetOpaque();

            if (aceOpaque == null || newAceOpaque == null)
            {
                return aceOpaque == newAceOpaque;
            }

            if (aceOpaque.Length != newAceOpaque.Length)
            {
                return false;
            }

            for (int i = 0; i < aceOpaque.Length; ++i)
            {
                if (aceOpaque[i] != newAceOpaque[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AcesAreMergeable(QualifiedAce ace, QualifiedAce newAce)
        {
            //
            // Only interested in ACEs with the specified type
            //

            if (ace.AceType != newAce.AceType)
            {
                return false;
            }

            //
            // Only interested in explicit (non-inherited) ACEs
            //

            if ((ace.AceFlags & AceFlags.Inherited) != 0)
            {
                return false;
            }

            if ((newAce.AceFlags & AceFlags.Inherited) != 0)
            {
                return false;
            }

            //
            // Only interested in ACEs with the specified qualifier
            //

            if (ace.AceQualifier != newAce.AceQualifier)
            {
                return false;
            }

            //
            // Only interested in ACEs with the specified SID
            //

            if (ace.SecurityIdentifier != newAce.SecurityIdentifier)
            {
                return false;
            }

            //
            // Only interested in ACEs with the specified callback data
            //

            if (!AceOpaquesMatch(ace, newAce))
            {
                return false;
            }

            return true;
        }

        //
        // Merge routine for qualified ACEs
        //

        private bool MergeAces(ref QualifiedAce ace, QualifiedAce newAce)
        {
            //
            // Check whether the ACEs are potentially mergeable
            //

            if (!AcesAreMergeable(ace, newAce))
            {
                return false;
            }

            //
            // The modification algorithm proceeds in stages
            //
            // Stage 1: if flags match, add to the access mask
            //

            if (ace.AceFlags == newAce.AceFlags)
            {
                if (ace is ObjectAce || newAce is ObjectAce)
                {
                    // for object aces we need to match the inherited object types (for ace flags equality)
                    if (InheritedObjectTypesMatch(ace, newAce))
                    {
                        // also since access mask bits are further qualified by object type, they cannot always be added on
                        if (AccessMasksAreMergeable(ace, newAce))
                        {
                            ace.AccessMask |= newAce.AccessMask;
                            return true;
                        }
                    }
                }
                else
                {
                    ace.AccessMask |= newAce.AccessMask;
                    return true;
                }
            }



            //
            // Stage 2: Audit flags can be combined if the rest of the
            //          flags (both access mask and inheritance) match
            //

            if (((ace.AceFlags & AceFlags.InheritanceFlags) == (newAce.AceFlags & AceFlags.InheritanceFlags)) &&
                (ace.AccessMask == newAce.AccessMask))
            {
                if ((ace is ObjectAce) || (newAce is ObjectAce))
                {
                    // for object aces we need to match the inherited object types (for inheritance flags equality) and object type (for access mask equality) as well
                    if (InheritedObjectTypesMatch(ace, newAce) &&
                        (ObjectTypesMatch(ace, newAce)))
                    {
                        ace.AceFlags |= (newAce.AceFlags & AceFlags.AuditFlags);
                        return true;
                    }
                }
                else
                {
                    ace.AceFlags |= (newAce.AceFlags & AceFlags.AuditFlags);
                    return true;
                }

            }

            //
            // Stage 3: Inheritance flags can be combined in some cases
            //          provided access mask and audit bits are the same
            //

            if (((ace.AceFlags & AceFlags.AuditFlags) == (newAce.AceFlags & AceFlags.AuditFlags)) &&
                (ace.AccessMask == newAce.AccessMask))
            {
                AceFlags merged;

                //
                // See whether the inheritance bits can be merged
                //

                if ((ace is ObjectAce) || (newAce is ObjectAce))
                {
                    // object types need to match (for access mask equality) and inheritance flags need additional DS specific logic
                    // to check whether they can be merged
                    if ((ObjectTypesMatch(ace, newAce)) &&
                         (AceFlagsAreMergeable(ace, newAce)))
                    {
                        if (true == MergeInheritanceBits(ace.AceFlags, newAce.AceFlags, IsDS, out merged))
                        {
                            ace.AceFlags = (merged | (ace.AceFlags & AceFlags.AuditFlags));
                            return true;
                        }
                    }
                }
                else
                {
                    if (true == MergeInheritanceBits(ace.AceFlags, newAce.AceFlags, IsDS, out merged))
                    {
                        ace.AceFlags = (merged | (ace.AceFlags & AceFlags.AuditFlags));
                        return true;
                    }
                }

            }

            return false;
        }

        //
        // Returns 'true' if the ACL is in canonical order; 'false' otherwise
        //

        private bool CanonicalCheck(bool isDacl)
        {
            if (isDacl)
            {
                //
                // DACL canonical order:
                //   Explicit Deny - Explicit Allow - Inherited
                //

                const int AccessDenied = 0;
                const int AccessAllowed = 1;
                const int Inherited = 2;

                int currentStage = AccessDenied;

                //
                // In this loop, do NOT use 'Count' as upper bound of the loop,
                // since doing so will canonicalize the ACL invalidating the result
                // of this check!
                //

                for (int i = 0; i < _acl.Count; i++)
                {
                    int aceStage;

                    GenericAce ace = _acl[i];

                    if ((ace.AceFlags & AceFlags.Inherited) != 0)
                    {
                        aceStage = Inherited;
                    }
                    else
                    {
                        QualifiedAce? qualifiedAce = ace as QualifiedAce;

                        if (qualifiedAce == null)
                        {
                            //
                            // Explicit ACE is not recognized - this is not a canonical ACL
                            //

                            return false;
                        }

                        if (qualifiedAce.AceQualifier == AceQualifier.AccessAllowed)
                        {
                            aceStage = AccessAllowed;
                        }
                        else if (qualifiedAce.AceQualifier == AceQualifier.AccessDenied)
                        {
                            aceStage = AccessDenied;
                        }
                        else
                        {
                            //
                            // Only allow and deny ACEs are allowed here
                            //

                            Debug.Assert(false, "Audit and alarm ACEs must have been stripped by remove-meaningless logic");
                            return false;
                        }
                    }

                    if (aceStage > currentStage)
                    {
                        currentStage = aceStage;
                    }
                    else if (aceStage < currentStage)
                    {
                        return false;
                    }
                }
            }
            else
            {
                //
                // SACL canonical order:
                //   Explicit - Inherited
                //

                const int Explicit = 0;
                const int Inherited = 1;

                int currentStage = Explicit;

                //
                // In this loop, do NOT use 'Count' as upper bound of the loop,
                // since doing so will canonicalize the ACL invalidating the result
                // of this check!
                //

                for (int i = 0; i < _acl.Count; i++)
                {
                    int aceStage;

                    GenericAce ace = _acl[i];

                    if (ace == null)
                    {
                        //
                        // <markpu-9/19/2004> Afraid to yank this statement now
                        // for fear of destabilization, so adding an assert instead
                        //

                        Debug.Assert(ace != null, "How did a null ACE end up in a SACL?");
                        continue;
                    }

                    if ((ace.AceFlags & AceFlags.Inherited) != 0)
                    {
                        aceStage = Inherited;
                    }
                    else
                    {
                        QualifiedAce? qualifiedAce = ace as QualifiedAce;

                        if (qualifiedAce == null)
                        {
                            //
                            // Explicit ACE is not recognized - this is not a canonical ACL
                            //

                            return false;
                        }

                        if (qualifiedAce.AceQualifier == AceQualifier.SystemAudit ||
                            qualifiedAce.AceQualifier == AceQualifier.SystemAlarm)
                        {
                            aceStage = Explicit;
                        }
                        else
                        {
                            //
                            // Only audit and alarm ACEs are allowed here
                            //

                            Debug.Assert(false, "Allow and deny ACEs must have been stripped by remove-meaningless logic");
                            return false;
                        }
                    }

                    if (aceStage > currentStage)
                    {
                        currentStage = aceStage;
                    }
                    else if (aceStage < currentStage)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void ThrowIfNotCanonical()
        {
            if (!_isCanonical)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ModificationOfNonCanonicalAcl);
            }
        }

        #endregion

        #region Constructors

        //
        // Creates an empty ACL
        //

        internal CommonAcl(bool isContainer, bool isDS, byte revision, int capacity)
            : base()
        {
            _isContainer = isContainer;
            _isDS = isDS;
            _acl = new RawAcl(revision, capacity);
            _isCanonical = true; // since it is empty
        }

        //
        // Creates an ACL from a raw ACL
        // - 'trusted' (internal) callers get to pass the raw ACL
        //   that this object will take ownership of
        // - 'untrusted' callers are handled by creating a local
        //   copy of the ACL passed in
        //

        internal CommonAcl(bool isContainer, bool isDS, RawAcl rawAcl, bool trusted, bool isDacl)
            : base()
        {
            if (rawAcl == null)
            {
                throw new ArgumentNullException(nameof(rawAcl));
            }

            _isContainer = isContainer;
            _isDS = isDS;

            if (trusted)
            {
                //
                // In the trusted case, we take over ownership of the ACL passed in
                //

                _acl = rawAcl;

                RemoveMeaninglessAcesAndFlags(isDacl);
            }
            else
            {
                //
                // In the untrusted case, we create our own raw ACL to keep the ACEs in
                //

                _acl = new RawAcl(rawAcl.Revision, rawAcl.Count);

                for (int i = 0; i < rawAcl.Count; i++)
                {
                    //
                    // Clone each ACE prior to putting it in
                    //

                    GenericAce ace = rawAcl[i].Copy();

                    //
                    // Avoid inserting meaningless ACEs
                    //

                    if (true == InspectAce(ref ace, isDacl))
                    {
                        _acl.InsertAce(_acl.Count, ace);
                    }
                }
            }

            //
            // See whether the ACL is canonical to begin with
            //

            if (true == CanonicalCheck(isDacl))
            {
                //
                // Sort and compact the array
                //

                Canonicalize(true, isDacl);

                _isCanonical = true;
            }
            else
            {
                _isCanonical = false;
            }
        }

        #endregion

        #region Internal Properties

        internal RawAcl RawAcl
        {
            get { return _acl; }
        }

        #endregion

        #region Protected Methods

        internal void CheckAccessType(AccessControlType accessType)
        {
            if (accessType != AccessControlType.Allow &&
                accessType != AccessControlType.Deny)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(accessType),
                    SR.ArgumentOutOfRange_Enum);
            }
        }

        internal void CheckFlags(InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            if (IsContainer)
            {
                //
                // Supplying propagation flags without inheritance flags is illegal
                //

                if (inheritanceFlags == InheritanceFlags.None &&
                    propagationFlags != PropagationFlags.None)
                {
                    throw new ArgumentException(
                        SR.Argument_InvalidAnyFlag,
                        nameof(propagationFlags));
                }
            }
            else if (inheritanceFlags != InheritanceFlags.None)
            {
                throw new ArgumentException(
                    SR.Argument_InvalidAnyFlag,
                    nameof(inheritanceFlags));
            }
            else if (propagationFlags != PropagationFlags.None)
            {
                throw new ArgumentException(
                    SR.Argument_InvalidAnyFlag,
                    nameof(propagationFlags));
            }

            return;
        }

        //
        // Helper function behind all the AddXXX methods for qualified aces
        //

        internal void AddQualifiedAce(SecurityIdentifier sid, AceQualifier qualifier, int accessMask, AceFlags flags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            if (sid == null)
            {
                throw new ArgumentNullException(nameof(sid));
            }

            ThrowIfNotCanonical();

            bool aceMerged = false; // if still false after all attempts to merge, create new entry

            if (qualifier == AceQualifier.SystemAudit &&
                ((flags & AceFlags.AuditFlags) == 0))
            {
                throw new ArgumentException(
                    SR.Arg_EnumAtLeastOneFlag,
                    nameof(flags));
            }

            if (accessMask == 0)
            {
                throw new ArgumentException(
                    SR.Argument_ArgumentZero,
                    nameof(accessMask));
            }

            GenericAce newAce;

            if ((!IsDS) || (objectFlags == ObjectAceFlags.None))
            {
                newAce = new CommonAce(flags, qualifier, accessMask, sid, false, null);
            }
            else
            {
                newAce = new ObjectAce(flags, qualifier, accessMask, sid, objectFlags, objectType, inheritedObjectType, false, null);
            }

            //
            // Make sure the new ACE wouldn't be meaningless before proceeding
            //

            if (false == InspectAce(ref newAce, (this is DiscretionaryAcl)))
            {
                return;
            }

            //
            // See if the new ACE can be merged with any of the existing ones
            //

            for (int i = 0; i < Count; i++)
            {
                QualifiedAce? ace = _acl[i] as QualifiedAce;

                if (ace == null)
                {
                    continue;
                }

                if (true == MergeAces(ref ace, (QualifiedAce)newAce))
                {
                    aceMerged = true;
                    break;
                }
            }

            //
            // Couldn't modify any existing entry, so add a new one
            //

            if (!aceMerged)
            {
                _acl.InsertAce(_acl.Count, newAce);

                _isDirty = true;
            }
            OnAclModificationTried();
        }

        //
        // Helper function behind all the SetXXX methods
        //

        internal void SetQualifiedAce(SecurityIdentifier sid, AceQualifier qualifier, int accessMask, AceFlags flags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            if (sid == null)
            {
                throw new ArgumentNullException(nameof(sid));
            }

            if (qualifier == AceQualifier.SystemAudit &&
                ((flags & AceFlags.AuditFlags) == 0))
            {
                throw new ArgumentException(
                    SR.Arg_EnumAtLeastOneFlag,
                    nameof(flags));
            }

            if (accessMask == 0)
            {
                throw new ArgumentException(
                    SR.Argument_ArgumentZero,
                    nameof(accessMask));
            }

            ThrowIfNotCanonical();

            GenericAce newAce;

            if ((!IsDS) || (objectFlags == ObjectAceFlags.None))
            {
                newAce = new CommonAce(flags, qualifier, accessMask, sid, false, null);
            }
            else
            {
                newAce = new ObjectAce(flags, qualifier, accessMask, sid, objectFlags, objectType, inheritedObjectType, false, null);
            }

            //
            // Make sure the new ACE wouldn't be meaningless before proceeding
            //

            if (false == InspectAce(ref newAce, (this is DiscretionaryAcl)))
            {
                return;
            }

            for (int i = 0; i < Count; i++)
            {
                QualifiedAce? ace = _acl[i] as QualifiedAce;

                //
                // Not a qualified ACE - keep going
                //

                if (ace == null)
                {
                    continue;
                }

                //
                // Only interested in explicit (non-inherited) ACEs
                //

                if ((ace.AceFlags & AceFlags.Inherited) != 0)
                {
                    continue;
                }

                //
                // Only interested in ACEs with the specified qualifier
                //

                if (ace.AceQualifier != qualifier)
                {
                    continue;
                }

                //
                // Only interested in ACEs with the specified SID
                //

                if (ace.SecurityIdentifier != sid)
                {
                    continue;
                }

                //
                // This ACE corresponds to the SID and qualifier in question - remove it
                //

                _acl.RemoveAce(i);
                i--;
            }

            //
            // As a final step, add the ACE we want.
            // Add it at the end - we'll re-canonicalize later.
            //

            _acl.InsertAce(_acl.Count, newAce);

            //
            // To aid the efficiency of batch operations, recanonicalize this later
            //

            _isDirty = true;
            OnAclModificationTried();
        }

        //
        // Helper function behind all the RemoveXXX methods
        //

        internal bool RemoveQualifiedAces(SecurityIdentifier sid, AceQualifier qualifier, int accessMask, AceFlags flags, bool saclSemantics, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            if (accessMask == 0)
            {
                throw new ArgumentException(
                    SR.Argument_ArgumentZero,
                    nameof(accessMask));
            }

            if (qualifier == AceQualifier.SystemAudit &&
                ((flags & AceFlags.AuditFlags) == 0))
            {
                throw new ArgumentException(
                    SR.Arg_EnumAtLeastOneFlag,
                    nameof(flags));
            }

            if (sid == null)
            {
                throw new ArgumentNullException(nameof(sid));
            }

            ThrowIfNotCanonical();


            //
            // Two passes are made.
            // During the first pass, no changes are made to the ACL,
            // the ACEs are simply evaluated to ascertain that the operation
            // can succeed.
            // If everything is kosher, the second pass is the one that makes changes.
            //

            bool evaluationPass = true;
            bool removePossible = true; // unless proven otherwise

            //
            // Needed for DS acls to keep track of the original access mask specified for removal
            //
            int originalAccessMask = accessMask;
            AceFlags originalFlags = flags;

            //
            // It is possible that the removal will result in an overflow exception
            // because more ACEs get inserted.
            // Save the current state of the object and revert to it later if
            // and exception is thrown.
            //

            byte[] recovery = new byte[BinaryLength];
            GetBinaryForm(recovery, 0);

        MakeAnotherPass:

            try
            {
                for (int i = 0; i < Count; i++)
                {
                    QualifiedAce? ace = _acl[i] as QualifiedAce;

                    //
                    // Not a qualified ACE - keep going
                    //

                    if (ace == null)
                    {
                        continue;
                    }

                    //
                    // Only interested in explicit (non-inherited) ACEs
                    //

                    if ((ace.AceFlags & AceFlags.Inherited) != 0)
                    {
                        continue;
                    }

                    //
                    // Only interested in ACEs with the specified qualifier
                    //

                    if (ace.AceQualifier != qualifier)
                    {
                        continue;
                    }

                    //
                    // Only interested in ACEs with the specified SID
                    //

                    if (ace.SecurityIdentifier != sid)
                    {
                        continue;
                    }

                    //
                    // If access masks have nothing in common, skip the whole exercise
                    //

                    if (IsDS)
                    {
                        //
                        // incase of directory aces, if the access mask of the
                        // existing and new ace have any bits in common that need
                        // an object type, then we need to perform some checks on the
                        // object types in the two aces. Since certain bits are further qualified
                        // by the object type they cannot be determined to be common without
                        // inspecting the object type. It is possible that the same bits may be set but
                        // the object types are different in which case they are really not common bits.
                        //
                        accessMask = originalAccessMask;
                        bool objectTypesConflict = !GetAccessMaskForRemoval(ace, objectFlags, objectType, ref accessMask);

                        // if the access masks have nothing in common, skip
                        if ((ace.AccessMask & accessMask) == 0)
                        {
                            continue;
                        }

                        //
                        // incase of directory aces, if the existing and new ace are being inherited,
                        // then we need to perform some checks on the
                        // inherited object types in the two aces. Since inheritance is further qualified
                        // by the inherited object type the inheritance flags cannot be determined to be common without
                        // inspecting the inherited object type. It is possible that both aces may be further inherited but if
                        // the inherited object types are different the inheritance may not be common.
                        //
                        flags = originalFlags;
                        bool inheritedObjectTypesConflict = !GetInheritanceFlagsForRemoval(ace, objectFlags, inheritedObjectType, ref flags);

                        if ((((ace.AceFlags & AceFlags.ContainerInherit) == 0) && ((flags & AceFlags.ContainerInherit) != 0) && ((flags & AceFlags.InheritOnly) != 0)) ||
                             (((flags & AceFlags.ContainerInherit) == 0) && ((ace.AceFlags & AceFlags.ContainerInherit) != 0) && ((ace.AceFlags & AceFlags.InheritOnly) != 0)))
                        {
                            // if one ace applies only to self and the other only to children/descendents we have nothing in common
                            continue;
                        }

                        //
                        // if the ace being removed referred only to child types and child types among existing ace and
                        // ace being removed are not common then there is nothing in common between these aces (skip)
                        //
                        if (((originalFlags & AceFlags.ContainerInherit) != 0) && ((originalFlags & AceFlags.InheritOnly) != 0) && ((flags & AceFlags.ContainerInherit) == 0))
                        {
                            continue;
                        }

                        if (objectTypesConflict || inheritedObjectTypesConflict)
                        {
                            //
                            // if we reached this stage, then we've found something common between the two aces.
                            // But since there is a conflict between the object types (or inherited object types), the remove is not possible
                            //
                            removePossible = false;
                            break;
                        }
                    }
                    else
                    {
                        if ((ace.AccessMask & accessMask) == 0)
                        {
                            continue;
                        }
                    }

                    //
                    // If audit flags on a SACL have nothing in common,
                    // skip the whole exercise
                    //

                    if (saclSemantics &&
                        ((ace.AceFlags & flags & AceFlags.AuditFlags) == 0))
                    {
                        continue;
                    }

                    //
                    // See if the ACE needs to be split into several
                    // To illustrate with an example, consider this equation:
                    //            From: CI OI    NP SA FA R W
                    //          Remove:    OI IO NP SA    R
                    //
                    // PermissionSplit: CI OI    NP SA FA   W   // remove R
                    //   AuditingSplit: CI OI    NP    FA R     // remove SA
                    //      MergeSplit: CI OI    NP SA    R     // ready for merge
                    //          Remove:    OI IO NP SA    R     // same audit and perm flags as merge split
                    //
                    //          Result: CI OI    NP SA FA   W   // PermissionSplit
                    //                  CI OI    NP    FA R     // AuditingSplit
                    //                  CI       NP SA    R     // Result of perm removal
                    //

                    //
                    // Example for DS acls (when removal is possible)
                    //
                    // From: CI(Guid) LC CC(Guid)
                    // Remove: CI IO LC
                    //
                    // PermissionSplit: CI(Guid) CC(Guid) // Remove GR
                    //        MergeSplit: CI(Guid) LC // Ready for merge
                    //           Remove: CI IO LC // Removal is possible since we are trying to remove inheritance for
                    //                                            all child types when it exists for one specific child type
                    //
                    //              Result: CI(Guid) CC(Guid) // PermissionSplit
                    //                         LC // Result of perm removal
                    //
                    //
                    // Example for DS acls (when removal is NOT possible)
                    //
                    // From: CI GR CC(Guid)
                    // Remove: CI(Guid) IO LC
                    //
                    // PermissionSplit: CI CC(Guid) // Remove GR
                    //        MergeSplit: CI LC // Ready for merge
                    //           Remove: CI(Guid) IO CC // Removal is not possible since we are trying to remove
                    //                                                     inheritance for a specific child type when it exists for all child types
                    //

                    // Permission split settings
                    AceFlags ps_AceFlags = 0;
                    int ps_AccessMask = 0;
                    ObjectAceFlags ps_ObjectAceFlags = 0;
                    Guid ps_ObjectAceType = Guid.Empty;
                    Guid ps_InheritedObjectAceType = Guid.Empty;

                    // Auditing split makes sense if this is a SACL
                    AceFlags as_AceFlags = 0;
                    int as_AccessMask = 0;
                    ObjectAceFlags as_ObjectAceFlags = 0;
                    Guid as_ObjectAceType = Guid.Empty;
                    Guid as_InheritedObjectAceType = Guid.Empty;

                    // Merge split settings
                    AceFlags ms_AceFlags = 0;
                    int ms_AccessMask = 0;
                    ObjectAceFlags ms_ObjectAceFlags = 0;
                    Guid ms_ObjectAceType = Guid.Empty;
                    Guid ms_InheritedObjectAceType = Guid.Empty;

                    // Merge result settings
                    AceFlags mergeResultFlags = 0;
                    bool mergeRemoveTotal = false;

                    //
                    // First compute the permission split
                    //

                    ps_AceFlags = ace.AceFlags;
                    unchecked { ps_AccessMask = ace.AccessMask & ~accessMask; }

                    if (ace is ObjectAce oAce)
                    {
                        //
                        // determine what should be the object/inherited object types on the permission split
                        //
                        GetObjectTypesForSplit(oAce, ps_AccessMask /* access mask for this split */, ps_AceFlags /* flags remain the same */, out ps_ObjectAceFlags, out ps_ObjectAceType, out ps_InheritedObjectAceType);
                    }

                    //
                    // Next, for SACLs only, compute the auditing split
                    //

                    if (saclSemantics)
                    {
                        //
                        // This operation can set the audit bits region
                        // of ACE flags to zero;
                        // This case will be handled later
                        //

                        unchecked { as_AceFlags = ace.AceFlags & ~(flags & AceFlags.AuditFlags); }

                        //
                        // The result of this evaluation is guaranteed
                        // not to be zero by now
                        //

                        as_AccessMask = (ace.AccessMask & accessMask);

                        if (ace is ObjectAce objAce)
                        {
                            //
                            // determine what should be the object/inherited object types on the audit split
                            //
                            GetObjectTypesForSplit(objAce, as_AccessMask /* access mask for this split */, as_AceFlags /* flags remain the same for inheritance */, out as_ObjectAceFlags, out as_ObjectAceType, out as_InheritedObjectAceType);
                        }
                    }

                    //
                    // Finally, compute the merge split
                    //

                    ms_AceFlags = (ace.AceFlags & AceFlags.InheritanceFlags) | (flags & ace.AceFlags & AceFlags.AuditFlags);
                    ms_AccessMask = (ace.AccessMask & accessMask);


                    //
                    // Now is the time to obtain the result of applying the remove
                    // operation to the merge split
                    // Skipping this step for SACLs where the merge split step
                    // produced no auditing flags
                    //

                    if (!saclSemantics ||
                        ((ms_AceFlags & AceFlags.AuditFlags) != 0))
                    {
                        if (false == RemoveInheritanceBits(ms_AceFlags, flags, IsDS, out mergeResultFlags, out mergeRemoveTotal))
                        {
                            removePossible = false;
                            break;
                        }

                        if (!mergeRemoveTotal)
                        {
                            mergeResultFlags |= (ms_AceFlags & AceFlags.AuditFlags);

                            if (ace is ObjectAce objAce)
                            {
                                //
                                // determine what should be the object/inherited object types on the merge split
                                //
                                GetObjectTypesForSplit(objAce, ms_AccessMask /* access mask for this split */, mergeResultFlags /* flags for this split */, out ms_ObjectAceFlags, out ms_ObjectAceType, out ms_InheritedObjectAceType);
                            }
                        }
                    }

                    //
                    // If this is no longer an evaluation, go ahead and make the changes
                    //

                    if (!evaluationPass)
                    {
                        QualifiedAce newAce;

                        //
                        // Modify the existing ACE in-place if it has any access
                        // mask bits left, otherwise simply remove it
                        // However, if for an object ace we are removing the object type
                        // then we should really remove this ace and add a new one since
                        // we would be changing the size of this ace
                        //

                        if (ps_AccessMask != 0)
                        {
                            if ((ace is ObjectAce) &&
                                ((((ObjectAce)ace).ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) != 0) &&
                                     ((ps_ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) == 0))
                            {
                                ObjectAce newObjectAce;

                                _acl.RemoveAce(i);
                                newObjectAce = new ObjectAce(ps_AceFlags, qualifier, ps_AccessMask, ace.SecurityIdentifier, ps_ObjectAceFlags, ps_ObjectAceType, ps_InheritedObjectAceType, false, null);
                                _acl.InsertAce(i, newObjectAce);
                            }
                            else
                            {
                                ace.AceFlags = ps_AceFlags;
                                ace.AccessMask = ps_AccessMask;

                                if (ace is ObjectAce objectAce)
                                {
                                    objectAce.ObjectAceFlags = ps_ObjectAceFlags;
                                    objectAce.ObjectAceType = ps_ObjectAceType;
                                    objectAce.InheritedObjectAceType = ps_InheritedObjectAceType;
                                }
                            }
                        }
                        else
                        {
                            _acl.RemoveAce(i);
                            i--; // keep the array index honest
                        }

                        //
                        // On a SACL, the result of the auditing split must be recorded
                        //

                        if (saclSemantics && ((as_AceFlags & AceFlags.AuditFlags) != 0))
                        {
                            if (ace is CommonAce)
                            {
                                newAce = new CommonAce(as_AceFlags, qualifier, as_AccessMask, ace.SecurityIdentifier, false, null);
                            }
                            else
                            {
                                // object ace
                                newAce = new ObjectAce(as_AceFlags, qualifier, as_AccessMask, ace.SecurityIdentifier, as_ObjectAceFlags, as_ObjectAceType, as_InheritedObjectAceType, false, null);
                            }

                            i++; // so it's not considered again
                            _acl.InsertAce(i, newAce);
                        }

                        //
                        // If there are interesting bits left over from a remove, store them
                        // as a separate ACE
                        //

                        if (!mergeRemoveTotal)
                        {
                            if (ace is CommonAce)
                            {
                                newAce = new CommonAce(mergeResultFlags, qualifier, ms_AccessMask, ace.SecurityIdentifier, false, null);
                            }
                            else
                            {
                                // object ace
                                newAce = new ObjectAce(mergeResultFlags, qualifier, ms_AccessMask, ace.SecurityIdentifier, ms_ObjectAceFlags, ms_ObjectAceType, ms_InheritedObjectAceType, false, null);
                            }

                            i++; // so it's not considered again
                            _acl.InsertAce(i, newAce);
                        }
                    }
                }
            }
            catch (OverflowException)
            {
                //
                // Oops, overflow means that the ACL became too big.
                // Inform the caller that the remove was not possible.
                //

                _acl.SetBinaryForm(recovery, 0);
                return false;
            }

            //
            // Finished evaluating the possibility of a remove.
            // If it looks like it's doable, go ahead and do it.
            //

            if (evaluationPass && removePossible)
            {
                evaluationPass = false;
                goto MakeAnotherPass;
            }

            OnAclModificationTried();

            return removePossible;
        }

        internal void RemoveQualifiedAcesSpecific(SecurityIdentifier sid, AceQualifier qualifier, int accessMask, AceFlags flags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            if (accessMask == 0)
            {
                throw new ArgumentException(
                    SR.Argument_ArgumentZero,
                    nameof(accessMask));
            }

            if (qualifier == AceQualifier.SystemAudit &&
                ((flags & AceFlags.AuditFlags) == 0))
            {
                throw new ArgumentException(
                    SR.Arg_EnumAtLeastOneFlag,
                    nameof(flags));
            }

            if (sid == null)
            {
                throw new ArgumentNullException(nameof(sid));
            }

            ThrowIfNotCanonical();

            for (int i = 0; i < Count; i++)
            {
                QualifiedAce? ace = _acl[i] as QualifiedAce;

                //
                // Not a qualified ACE - keep going
                //

                if (ace == null)
                {
                    continue;
                }

                //
                // Only interested in explicit (non-inherited) ACEs
                //

                if ((ace.AceFlags & AceFlags.Inherited) != 0)
                {
                    continue;
                }

                //
                // Only interested in ACEs with the specified qualifier
                //

                if (ace.AceQualifier != qualifier)
                {
                    continue;
                }

                //
                // Only interested in ACEs with the specified SID
                //

                if (ace.SecurityIdentifier != sid)
                {
                    continue;
                }

                //
                // Only interested in exact ACE flag matches
                //

                if (ace.AceFlags != flags)
                {
                    continue;
                }

                //
                // Only interested in exact access mask matches
                //

                if (ace.AccessMask != accessMask)
                {
                    continue;
                }

                if (IsDS)
                {
                    //
                    // Incase of object aces, only interested in ACEs which match in their
                    // objectType and inheritedObjectType
                    //

                    if ((ace is ObjectAce objectAce) && (objectFlags != ObjectAceFlags.None))
                    {
                        //
                        // both are object aces, so must match in object type and inherited object type
                        //

                        if ((!objectAce.ObjectTypesMatch(objectFlags, objectType))
                            || (!objectAce.InheritedObjectTypesMatch(objectFlags, inheritedObjectType)))
                        {
                            continue;
                        }
                    }
                    else if ((ace is ObjectAce) || (objectFlags != ObjectAceFlags.None))
                    {
                        // one is object ace and the other is not, so no match
                        continue;
                    }
                }

                //
                // Got our exact match; now remove it
                //

                _acl.RemoveAce(i);
                i--; // keep the array index honest
            }
            OnAclModificationTried();
        }

        internal virtual void OnAclModificationTried()
        {
        }
        #endregion

        #region Public Properties

        //
        // Returns the revision of the ACL
        //

        public sealed override byte Revision
        {
            get { return _acl.Revision; }
        }

        //
        // Returns the number of ACEs in the ACL
        //

        public sealed override int Count
        {
            get
            {
                CanonicalizeIfNecessary();
                return _acl.Count;
            }
        }

        //
        // Returns the length of the binary representation of the ACL
        //

        public sealed override int BinaryLength
        {
            get
            {
                CanonicalizeIfNecessary();
                return _acl.BinaryLength;
            }
        }

        //
        // Returns 'true' if the ACL was canonical at creation time
        //

        public bool IsCanonical
        {
            get { return _isCanonical; }
        }

        public bool IsContainer
        {
            get { return _isContainer; }
        }

        public bool IsDS
        {
            get { return _isDS; }
        }

        #endregion

        #region Public Methods

        //
        // Returns the binary representation of the ACL
        //

        public sealed override void GetBinaryForm(byte[] binaryForm, int offset)
        {
            CanonicalizeIfNecessary();
            _acl.GetBinaryForm(binaryForm, offset);
        }

        //
        // Retrieves the ACE at a given index inside the ACL
        // Since the caller can modify the ACE it receives,
        // clone the ACE prior to returning it to the caller
        //

        public sealed override GenericAce this[int index]
        {
            get
            {
                CanonicalizeIfNecessary();
                return _acl[index].Copy();
            }

            set
            {
                throw new NotSupportedException(SR.NotSupported_SetMethod);
            }
        }

        public void RemoveInheritedAces()
        {
            ThrowIfNotCanonical();

            //
            // Iterating backwards as an optimization - all inherited ACEs
            // are usually in the back of the ACL
            //

            for (int i = _acl.Count - 1; i >= 0; i--)
            {
                GenericAce ace = _acl[i];

                if ((ace.AceFlags & AceFlags.Inherited) != 0)
                {
                    _acl.RemoveAce(i);
                }
            }
            OnAclModificationTried();
        }

        public void Purge(SecurityIdentifier sid)
        {
            if (sid == null)
            {
                throw new ArgumentNullException(nameof(sid));
            }

            ThrowIfNotCanonical();

            for (int i = Count - 1; i >= 0; i--)
            {
                KnownAce? ace = _acl[i] as KnownAce;

                //
                // Skip over unknown ACEs
                //

                if (ace == null)
                {
                    continue;
                }

                //
                // Skip over inherited ACEs
                //

                if ((ace.AceFlags & AceFlags.Inherited) != 0)
                {
                    continue;
                }

                //
                // SID matches - ACE is out
                //

                if (ace.SecurityIdentifier == sid)
                {
                    _acl.RemoveAce(i);
                }
            }
            OnAclModificationTried();
        }

        #endregion
    }


    public sealed class SystemAcl : CommonAcl
    {
        #region Constructors

        //
        // Creates an emtpy ACL
        //

        public SystemAcl(bool isContainer, bool isDS, int capacity)
            : this(isContainer, isDS, isDS ? AclRevisionDS : AclRevision, capacity)
        {
        }

        public SystemAcl(bool isContainer, bool isDS, byte revision, int capacity)
            : base(isContainer, isDS, revision, capacity)
        {
        }

        //
        // Creates an ACL from a given raw ACL
        // after canonicalizing it
        //

        public SystemAcl(bool isContainer, bool isDS, RawAcl rawAcl)
            : this(isContainer, isDS, rawAcl, false)
        {
        }

        //
        // Internal version - if 'trusted' is true,
        // takes ownership of the given raw ACL
        //

        internal SystemAcl(bool isContainer, bool isDS, RawAcl rawAcl, bool trusted)
            : base(isContainer, isDS, rawAcl, trusted, false)
        {
        }

        #endregion

        #region Public Methods

        public void AddAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            CheckFlags(inheritanceFlags, propagationFlags);
            AddQualifiedAce(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
        }

        public void SetAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            CheckFlags(inheritanceFlags, propagationFlags);
            SetQualifiedAce(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
        }

        public bool RemoveAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            return RemoveQualifiedAces(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), true, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
        }

        public void RemoveAuditSpecific(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            RemoveQualifiedAcesSpecific(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
        }

        public void AddAudit(SecurityIdentifier sid, ObjectAuditRule rule)
        {
            AddAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
        }

        public void AddAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            //
            // This is valid only for DS Acls
            //
            if (!IsDS)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperation_OnlyValidForDS);
            }

            CheckFlags(inheritanceFlags, propagationFlags);
            AddQualifiedAce(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
        }

        public void SetAudit(SecurityIdentifier sid, ObjectAuditRule rule)
        {
            SetAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
        }

        public void SetAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            //
            // This is valid only for DS Acls
            //
            if (!IsDS)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperation_OnlyValidForDS);
            }

            CheckFlags(inheritanceFlags, propagationFlags);
            SetQualifiedAce(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
        }

        public bool RemoveAudit(SecurityIdentifier sid, ObjectAuditRule rule)
        {
            return RemoveAudit(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
        }

        public bool RemoveAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            //
            // This is valid only for DS Acls
            //
            if (!IsDS)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperation_OnlyValidForDS);
            }

            return RemoveQualifiedAces(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), true, objectFlags, objectType, inheritedObjectType);
        }

        public void RemoveAuditSpecific(SecurityIdentifier sid, ObjectAuditRule rule)
        {
            RemoveAuditSpecific(rule.AuditFlags, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
        }

        public void RemoveAuditSpecific(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            //
            // This is valid only for DS Acls
            //
            if (!IsDS)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperation_OnlyValidForDS);
            }

            RemoveQualifiedAcesSpecific(sid, AceQualifier.SystemAudit, accessMask, GenericAce.AceFlagsFromAuditFlags(auditFlags) | GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
        }

        #endregion
    }


    public sealed class DiscretionaryAcl : CommonAcl
    {
        #region
        private static readonly SecurityIdentifier _sidEveryone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        private bool everyOneFullAccessForNullDacl;
        #endregion

        #region Constructors

        //
        // Creates an emtpy ACL
        //

        public DiscretionaryAcl(bool isContainer, bool isDS, int capacity)
            : this(isContainer, isDS, isDS ? AclRevisionDS : AclRevision, capacity)
        {
        }

        public DiscretionaryAcl(bool isContainer, bool isDS, byte revision, int capacity)
            : base(isContainer, isDS, revision, capacity)
        {
        }

        //
        // Creates an ACL from a given raw ACL
        // after canonicalizing it
        //

        public DiscretionaryAcl(bool isContainer, bool isDS, RawAcl? rawAcl)
            : this(isContainer, isDS, rawAcl, false)
        {
        }

        //
        // Internal version - if 'trusted' is true,
        // takes ownership of the given raw ACL
        //

        internal DiscretionaryAcl(bool isContainer, bool isDS, RawAcl? rawAcl, bool trusted)
            : base(isContainer, isDS, rawAcl == null ? new RawAcl(isDS ? AclRevisionDS : AclRevision, 0) : rawAcl, trusted, true)
        {
        }

        #endregion

        #region Public Methods

        public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            CheckAccessType(accessType);
            CheckFlags(inheritanceFlags, propagationFlags);
            everyOneFullAccessForNullDacl = false;
            AddQualifiedAce(sid, accessType == AccessControlType.Allow ? AceQualifier.AccessAllowed : AceQualifier.AccessDenied, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
        }

        public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            CheckAccessType(accessType);
            CheckFlags(inheritanceFlags, propagationFlags);
            everyOneFullAccessForNullDacl = false;
            SetQualifiedAce(sid, accessType == AccessControlType.Allow ? AceQualifier.AccessAllowed : AceQualifier.AccessDenied, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
        }

        public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            CheckAccessType(accessType);
            everyOneFullAccessForNullDacl = false;
            return RemoveQualifiedAces(sid, accessType == AccessControlType.Allow ? AceQualifier.AccessAllowed : AceQualifier.AccessDenied, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), false, ObjectAceFlags.None, Guid.Empty, Guid.Empty);
        }

        public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            CheckAccessType(accessType);
            everyOneFullAccessForNullDacl = false;
            RemoveQualifiedAcesSpecific(sid, accessType == AccessControlType.Allow ? AceQualifier.AccessAllowed : AceQualifier.AccessDenied, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), ObjectAceFlags.None, Guid.Empty, Guid.Empty);
        }

        public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule)
        {
            AddAccess(accessType, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
        }

        public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            //
            // This is valid only for DS Acls
            //
            if (!IsDS)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperation_OnlyValidForDS);
            }

            CheckAccessType(accessType);
            CheckFlags(inheritanceFlags, propagationFlags);
            everyOneFullAccessForNullDacl = false;
            AddQualifiedAce(sid, accessType == AccessControlType.Allow ? AceQualifier.AccessAllowed : AceQualifier.AccessDenied, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
        }

        public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule)
        {
            SetAccess(accessType, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
        }

        public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            //
            // This is valid only for DS Acls
            //
            if (!IsDS)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperation_OnlyValidForDS);
            }

            CheckAccessType(accessType);
            CheckFlags(inheritanceFlags, propagationFlags);
            everyOneFullAccessForNullDacl = false;
            SetQualifiedAce(sid, accessType == AccessControlType.Allow ? AceQualifier.AccessAllowed : AceQualifier.AccessDenied, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
        }

        public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule)
        {
            return RemoveAccess(accessType, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
        }

        public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            //
            // This is valid only for DS Acls
            //
            if (!IsDS)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperation_OnlyValidForDS);
            }

            CheckAccessType(accessType);
            everyOneFullAccessForNullDacl = false;
            return RemoveQualifiedAces(sid, accessType == AccessControlType.Allow ? AceQualifier.AccessAllowed : AceQualifier.AccessDenied, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), false, objectFlags, objectType, inheritedObjectType);
        }

        public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule)
        {
            RemoveAccessSpecific(accessType, sid, rule.AccessMask, rule.InheritanceFlags, rule.PropagationFlags, rule.ObjectFlags, rule.ObjectType, rule.InheritedObjectType);
        }

        public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType)
        {
            //
            // This is valid only for DS Acls
            //
            if (!IsDS)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperation_OnlyValidForDS);
            }

            CheckAccessType(accessType);
            everyOneFullAccessForNullDacl = false;
            RemoveQualifiedAcesSpecific(sid, accessType == AccessControlType.Allow ? AceQualifier.AccessAllowed : AceQualifier.AccessDenied, accessMask, GenericAce.AceFlagsFromInheritanceFlags(inheritanceFlags, propagationFlags), objectFlags, objectType, inheritedObjectType);
        }

        #endregion

        #region internals and privates

        //
        // DACL's "allow everyone full access may be created to replace a null DACL because managed
        // access control does not want to leave null DACLs around. But we need to remember this MACL
        // created ACE when the DACL is modified, we can remove it to match the same native semantics of
        // a null DACL.
        //
        internal bool EveryOneFullAccessForNullDacl
        {
            get { return everyOneFullAccessForNullDacl; }
            set { everyOneFullAccessForNullDacl = value; }
        }

        //
        // As soon as you tried successfully to modified the ACL, the internally created allow every one full access ACL is materialized
        // because in native world, a NULL dacl can't be operated on.
        //
        internal override void OnAclModificationTried()
        {
            everyOneFullAccessForNullDacl = false;
        }

        /// <summary>
        /// This static method will create an "allow everyone full control" single ACE DACL.
        /// </summary>
        /// <param name="isDS">whether it is a DS DACL</param>
        /// <param name="isContainer">whether it is a container</param>
        /// <returns>The single ACE DACL</returns>
        /// Note: This method is created to get the best behavior for using "allow everyone full access"
        /// single ACE DACL to replace null DACL from CommonSecurityObject.
        internal static DiscretionaryAcl CreateAllowEveryoneFullAccess(bool isDS, bool isContainer)
        {
            DiscretionaryAcl dcl = new DiscretionaryAcl(isContainer, isDS, 1);
            dcl.AddAccess(
                AccessControlType.Allow,
                _sidEveryone,
                -1,
                isContainer ? (InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit) : InheritanceFlags.None,
                PropagationFlags.None);

            dcl.everyOneFullAccessForNullDacl = true;
            return dcl;
        }
        #endregion
    }
}
