using System.Buffers.Binary;
using System.Text;
using Claunia.PropertyList;

namespace Melanzana.CodeSign.Blobs
{
    public class EntitlementsDerBlob
    {
        public static byte[] Create(Entitlements entitlements)
        {
            var plistBytes = DerPropertyListWriter.Write(entitlements.PList);
            var blobBuffer = new byte[8 + plistBytes.Length];

            BinaryPrimitives.WriteUInt32BigEndian(blobBuffer.AsSpan(0, 4), (uint)BlobMagic.EntitlementsDer);
            BinaryPrimitives.WriteUInt32BigEndian(blobBuffer.AsSpan(4, 4), (uint)blobBuffer.Length);
            plistBytes.CopyTo(blobBuffer.AsSpan(8, plistBytes.Length));

            return blobBuffer;
        }
    }
}