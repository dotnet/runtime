// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Internal;

namespace System.Reflection.Metadata
{
    public readonly struct BlobContentId : IEquatable<BlobContentId>
    {
        private const int Size = BlobUtilities.SizeOfGuid + sizeof(uint);

        public Guid Guid { get; }
        public uint Stamp { get; }

        public BlobContentId(Guid guid, uint stamp)
        {
            Guid = guid;
            Stamp = stamp;
        }

        public BlobContentId(ImmutableArray<byte> id) 
            : this(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(id))
        {
        }

        public unsafe BlobContentId(byte[] id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (id.Length != Size)
            {
                throw new ArgumentException(SR.Format(SR.UnexpectedArrayLength, Size), nameof(id));
            }

            fixed (byte* ptr = &id[0])
            {
                var reader = new BlobReader(ptr, id.Length);
                Guid = reader.ReadGuid();
                Stamp = reader.ReadUInt32();
            }
        }

        public bool IsDefault => Guid == default(Guid) && Stamp == 0;

        public static BlobContentId FromHash(ImmutableArray<byte> hashCode)
        {
            return FromHash(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(hashCode));
        }

        public static unsafe BlobContentId FromHash(byte[] hashCode)
        {
            const int minHashSize = 20;

            if (hashCode == null)
            {
                throw new ArgumentNullException(nameof(hashCode));
            }

            if (hashCode.Length < minHashSize)
            {
                throw new ArgumentException(SR.Format(SR.HashTooShort, minHashSize), nameof(hashCode));
            }

            Guid guid = default(Guid);
            byte* guidPtr = (byte*)&guid;
            for (var i = 0; i < BlobUtilities.SizeOfGuid; i++)
            {
                guidPtr[i] = hashCode[i];
            }

            // modify the guid data so it decodes to the form of a "random" guid ala rfc4122
            guidPtr[7] = (byte)((guidPtr[7] & 0x0f) | (4 << 4));
            guidPtr[8] = (byte)((guidPtr[8] & 0x3f) | (2 << 6));

            // compute a random-looking stamp from the remaining bits, but with the upper bit set
            uint stamp = 0x80000000u | ((uint)hashCode[19] << 24 | (uint)hashCode[18] << 16 | (uint)hashCode[17] << 8 | hashCode[16]);

            return new BlobContentId(guid, stamp);
        }

        public static Func<IEnumerable<Blob>, BlobContentId> GetTimeBasedProvider()
        {
            // In the PE File Header this is a "Time/Date Stamp" whose description is "Time and date
            // the file was created in seconds since January 1st 1970 00:00:00 or 0"
            // However, when we want to make it deterministic we fill it in (later) with bits from the hash of the full PE file.
            uint timestamp = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return content => new BlobContentId(Guid.NewGuid(), timestamp);
        }

        public bool Equals(BlobContentId other) => Guid == other.Guid && Stamp == other.Stamp;
        public override bool Equals(object obj) => obj is BlobContentId && Equals((BlobContentId)obj);
        public override int GetHashCode() => Hash.Combine(Stamp, Guid.GetHashCode());
        public static bool operator ==(BlobContentId left, BlobContentId right) => left.Equals(right);
        public static bool operator !=(BlobContentId left, BlobContentId right) => !left.Equals(right);
    }
}