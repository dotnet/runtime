using System.Buffers.Binary;
using System.Diagnostics;
using Melanzana.CodeSign.Blobs;

namespace Melanzana.CodeSign.Requirements
{
    public class RequirementSet : Dictionary<RequirementType, Requirement>
    {
        public RequirementSet()
        {
        }

        public static RequirementSet FromBlob(ReadOnlySpan<byte> blob)
        {
            var requirementSet = new RequirementSet();
            var superBlobMagic = BinaryPrimitives.ReadUInt32BigEndian(blob.Slice(0, 4));
            var subBlobCount = BinaryPrimitives.ReadInt32BigEndian(blob.Slice(8, 4));
            Debug.Assert(superBlobMagic == (uint)BlobMagic.Requirements);
            for (int i = 0; i < subBlobCount; i++)
            {
                var requirementType = (RequirementType)BinaryPrimitives.ReadUInt32BigEndian(blob.Slice(12 + i * 8, 4));
                var blobOffset = BinaryPrimitives.ReadInt32BigEndian(blob.Slice(16 + i * 8, 4));
                var blobSize = BinaryPrimitives.ReadInt32BigEndian(blob.Slice(blobOffset + 4, 4));
                requirementSet[requirementType] = Requirement.FromBlob(blob.Slice(blobOffset, blobSize));
            }
            return requirementSet;
        }

        public byte[] AsBlob()
        {
            var requirentBlobs = this.Select(i => new { Type = i.Key, Blob = i.Value.AsBlob() }).ToArray();
            var blobBuffer = new byte[12 + requirentBlobs.Sum(i => i.Blob.Length + 8)];

            BinaryPrimitives.WriteUInt32BigEndian(blobBuffer.AsSpan(0, 4), (uint)BlobMagic.Requirements);
            BinaryPrimitives.WriteInt32BigEndian(blobBuffer.AsSpan(4, 4), blobBuffer.Length);
            BinaryPrimitives.WriteInt32BigEndian(blobBuffer.AsSpan(8, 4), requirentBlobs.Length);

            int offset = 12 + (requirentBlobs.Length * 8);
            for (int i = 0; i < requirentBlobs.Length; i++)
            {
                BinaryPrimitives.WriteUInt32BigEndian(blobBuffer.AsSpan(12 + (i * 8), 4), (uint)requirentBlobs[i].Type);
                BinaryPrimitives.WriteInt32BigEndian(blobBuffer.AsSpan(16 + (i * 8), 4), offset);
                requirentBlobs[i].Blob.CopyTo(blobBuffer.AsSpan(offset, requirentBlobs[i].Blob.Length));
                offset += requirentBlobs[i].Blob.Length;
            }

            return blobBuffer;
        }

        //public override string? ToString() => Expression.ToString();

        public static RequirementSet CreateDefault(string bundleIdentifier, string certificateFriendlyName)
        {
            if (string.IsNullOrEmpty(bundleIdentifier))
                throw new ArgumentNullException(nameof(bundleIdentifier));
            if (string.IsNullOrEmpty(certificateFriendlyName))
                throw new ArgumentNullException(nameof(certificateFriendlyName));

            var expression = Expression.And(
                Expression.Ident(bundleIdentifier),
                Expression.And(
                    Expression.AppleGenericAnchor,
                    Expression.And(
                        Expression.CertField(0, "subject.CN", ExpressionMatchType.Equal, certificateFriendlyName),
                        Expression.CertGeneric(1, "1.2.840.113635.100.6.2.1", ExpressionMatchType.Exists)
                    )
                )
            );
            return new RequirementSet { { RequirementType.Designated, new Requirement(expression) }};
        }

    }
}