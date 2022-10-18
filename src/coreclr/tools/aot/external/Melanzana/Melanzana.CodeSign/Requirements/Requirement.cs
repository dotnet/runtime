using System.Buffers.Binary;
using System.Diagnostics;
using Melanzana.CodeSign.Blobs;

namespace Melanzana.CodeSign.Requirements
{
    public class Requirement
    {
        public Requirement(Expression expression)
        {
            Expression = expression;
        }

        public Expression Expression { get; private set; }

        public static Requirement FromBlob(ReadOnlySpan<byte> blob)
        {
            var blobMagic = BinaryPrimitives.ReadUInt32BigEndian(blob.Slice(0, 4));
            var blobSize = BinaryPrimitives.ReadInt32BigEndian(blob.Slice(4, 4));
            var matchType = BinaryPrimitives.ReadUInt32BigEndian(blob.Slice(8, 4));
            Debug.Assert(blobMagic == (uint)BlobMagic.Requirement);
            Debug.Assert(blobSize == blob.Length);
            Debug.Assert(matchType == 1u); // Expression
            return new Requirement(Expression.FromBlob(blob.Slice(12)));
        }

        public byte[] AsBlob()
        {
            byte[] blobBuffer = new byte[Expression.Size + 12];

            BinaryPrimitives.WriteUInt32BigEndian(blobBuffer.AsSpan(0, 4), (uint)BlobMagic.Requirement);
            BinaryPrimitives.WriteInt32BigEndian(blobBuffer.AsSpan(4, 4), blobBuffer.Length);
            BinaryPrimitives.WriteUInt32BigEndian(blobBuffer.AsSpan(8, 4), 1u); // Expression
            Expression.Write(blobBuffer.AsSpan(12), out var _);

            return blobBuffer;
        }

        public override string? ToString() => Expression.ToString();
    }
}