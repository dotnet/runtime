using System.Buffers.Binary;
using System.Text;
using System.Formats.Asn1;
using Melanzana.MachO;

namespace Melanzana.CodeSign.Requirements
{
    public abstract partial class Expression
    {
        private static int Align(int size) => (size + 3) & ~3;

        private static byte[] GetOidBytes(string oid)
        {
            var asnWriter = new AsnWriter(AsnEncodingRules.DER);
            asnWriter.WriteObjectIdentifier(oid);
            return asnWriter.Encode().AsSpan(2).ToArray();
        }

        private static string GetOidString(byte[] oid)
        {
            var oidBytes = new byte[oid.Length + 2];
            oidBytes[0] = 6;
            oidBytes[1] = (byte)oid.Length;
            oid.CopyTo(oidBytes.AsSpan(2));
            return AsnDecoder.ReadObjectIdentifier(oidBytes, AsnEncodingRules.DER, out _);
        }

        private static byte[] GetTimestampBytes(DateTime dateTime)
        {
            long tsSeconds = (long)(dateTime - new DateTime(2001, 1, 1)).TotalSeconds;
            var buffer = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(buffer, tsSeconds);
            return buffer;
        }

        private static string GetTimestampString(byte[] dateTime)
        {
            var tsSeconds = BinaryPrimitives.ReadInt64BigEndian(dateTime);
            return new DateTime(2001, 1, 1).AddSeconds(tsSeconds).ToString("yyyyMMddHHmmssZ");
        }

        private static string BinaryValueToString(byte[] bytes)
        {
            return $"0x{Convert.ToHexString(bytes)}";
        }

        private static string ValueToString(byte[] bytes)
        {
            bool isPrintable = bytes.All(c => !char.IsControl((char)c) && char.IsAscii((char)c));
            if (!isPrintable)
            {
                return BinaryValueToString(bytes);
            }

            bool needQuoting =
                bytes.Length == 0 ||
                char.IsDigit((char)bytes[0]) ||
                bytes.Any(c => !char.IsLetterOrDigit((char)c));
            if (needQuoting)
            {
                var sb = new StringBuilder();
                sb.Append('"');
                foreach (var c in bytes)
                {
                    if (c == (byte)'\\' || c == (byte)'"')
                    {
                        sb.Append('\\');
                    }
                    sb.Append((char)c);
                }
                sb.Append('"');
                return sb.ToString();
            }
            else
            {
                return Encoding.ASCII.GetString(bytes);
            }
        }

        private static string CertificateSlotToString(int slot)
        {
            return slot switch {
                0 => "leaf",
                -1 => "root",
                _ => slot.ToString(),
            };
        }
    }
}
