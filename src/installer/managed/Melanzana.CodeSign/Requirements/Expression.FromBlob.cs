using System.Buffers.Binary;
using System.Text;
using Melanzana.MachO;

namespace Melanzana.CodeSign.Requirements
{
    public abstract partial class Expression
    {
        private static byte[] GetData(ReadOnlySpan<byte> expr, out int bytesRead)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(expr);
            bytesRead = 4 + Align(length);
            return expr.Slice(4, length).ToArray();
        }

        private static byte[]? GetMatchData(ReadOnlySpan<byte> expr, ExpressionMatchType matchType, out int bytesRead)
        {
            if (matchType != ExpressionMatchType.Exists && matchType != ExpressionMatchType.Absent)
            {
                return GetData(expr, out bytesRead);
            }

            bytesRead = 0;
            return null;
        }

        private static Expression FromBlob(ReadOnlySpan<byte> expr, out int bytesRead)
        {
            if (expr.Length == 0)
                throw new NotImplementedException();

            var op = (ExpressionOperation)BinaryPrimitives.ReadUInt32BigEndian(expr);
            switch (op)
            {
                case ExpressionOperation.False:
                    bytesRead = 4;
                    return False;

                case ExpressionOperation.True:
                    bytesRead = 4;
                    return True;

                case ExpressionOperation.Ident:
                    var identifier = GetData(expr.Slice(4), out bytesRead);
                    bytesRead += 4;
                    return Ident(Encoding.ASCII.GetString(identifier));

                case ExpressionOperation.AppleAnchor:
                    bytesRead = 4;
                    return AppleAnchor;

                case ExpressionOperation.AnchorHash:
                    var certificateIndex = BinaryPrimitives.ReadInt32BigEndian(expr.Slice(4, 4));
                    var hash = GetData(expr.Slice(8), out bytesRead);
                    bytesRead += 8;
                    return AnchorHash(certificateIndex, hash);

                case ExpressionOperation.InfoKeyValue:
                    var field = GetData(expr.Slice(4), out var fieldBytesRead);
                    var value = GetData(expr.Slice(4 + fieldBytesRead), out bytesRead);
                    bytesRead += 4 + fieldBytesRead;
                    return InfoKeyValue(Encoding.ASCII.GetString(field), Encoding.ASCII.GetString(value));

                case ExpressionOperation.And:
                    var left = FromBlob(expr.Slice(4), out var leftBytesRead);
                    var right = FromBlob(expr.Slice(4 + leftBytesRead), out bytesRead);
                    bytesRead += 4 + leftBytesRead;
                    return And(left, right);

                case ExpressionOperation.Or:
                    left = FromBlob(expr.Slice(4), out leftBytesRead);
                    right = FromBlob(expr.Slice(4 + leftBytesRead), out bytesRead);
                    bytesRead += 4 + leftBytesRead;
                    return Or(left, right);

                case ExpressionOperation.CDHash:
                    hash = GetData(expr.Slice(4), out bytesRead);
                    bytesRead += 4;
                    return CDHash(hash);

                case ExpressionOperation.Not:
                    var inner = FromBlob(expr.Slice(4), out bytesRead);
                    bytesRead += 4;
                    return Not(inner);

                case ExpressionOperation.InfoKeyField:
                    field = GetData(expr.Slice(4), out fieldBytesRead);
                    var matchType = (ExpressionMatchType)BinaryPrimitives.ReadUInt32BigEndian(expr.Slice(4 + fieldBytesRead, 4));
                    var matchValue = GetMatchData(expr.Slice(8 + fieldBytesRead), matchType, out var matchValueBytesRead);
                    bytesRead = 8 + fieldBytesRead + matchValueBytesRead;
                    return InfoKeyField(Encoding.ASCII.GetString(field), matchType, matchValue != null ? Encoding.ASCII.GetString(matchValue) : null);

                case ExpressionOperation.CertField:
                case ExpressionOperation.CertGeneric:
                case ExpressionOperation.CertPolicy:
                case ExpressionOperation.CertFieldDate:
                    certificateIndex = BinaryPrimitives.ReadInt32BigEndian(expr.Slice(4, 4));
                    field = GetData(expr.Slice(8), out fieldBytesRead);
                    matchType = (ExpressionMatchType)BinaryPrimitives.ReadUInt32BigEndian(expr.Slice(8 + fieldBytesRead, 4));
                    matchValue = GetMatchData(expr.Slice(12 + fieldBytesRead), matchType, out matchValueBytesRead);
                    bytesRead = 12 + fieldBytesRead + matchValueBytesRead;
                    return new CertExpression(op, certificateIndex, field, matchType, matchValue);

                case ExpressionOperation.TrustedCert:
                    certificateIndex = BinaryPrimitives.ReadInt32BigEndian(expr.Slice(4, 4));
                    bytesRead = 8;
                    return TrustedCert(certificateIndex);

                case ExpressionOperation.TrustedCerts:
                    bytesRead = 4;
                    return TrustedCerts;

                case ExpressionOperation.AppleGenericAnchor:
                    bytesRead = 4;
                    return AppleGenericAnchor;

                case ExpressionOperation.EntitlementField:
                    field = GetData(expr.Slice(4), out fieldBytesRead);
                    matchType = (ExpressionMatchType)BinaryPrimitives.ReadUInt32BigEndian(expr.Slice(4 + fieldBytesRead, 4));
                    matchValue = GetMatchData(expr.Slice(8 + fieldBytesRead), matchType, out matchValueBytesRead);
                    bytesRead = 8 + fieldBytesRead + matchValueBytesRead;
                    return EntitlementField(Encoding.ASCII.GetString(field), matchType, matchValue != null ? Encoding.ASCII.GetString(matchValue) : null);

                case ExpressionOperation.NamedAnchor:
                    var anchorName = GetData(expr.Slice(4), out bytesRead);
                    bytesRead += 4;
                    return NamedAnchor(Encoding.ASCII.GetString(anchorName));

                case ExpressionOperation.NamedCode:
                    var codeName = GetData(expr.Slice(4), out bytesRead);
                    bytesRead += 4;
                    return NamedCode(Encoding.ASCII.GetString(codeName));

                case ExpressionOperation.Platform:
                    var platform = (MachPlatform)BinaryPrimitives.ReadInt32BigEndian(expr.Slice(4, 4));
                    bytesRead = 8;
                    return Platform(platform);

                case ExpressionOperation.Notarized:
                    bytesRead = 4;
                    return Notarized;

                case ExpressionOperation.LegacyDevID:
                    bytesRead = 4;
                    return LegacyDevID;

                default:
                    throw new NotSupportedException();
            }
        }

        public static Expression FromBlob(ReadOnlySpan<byte> expr)
        {
            return FromBlob(expr, out _);
        }
    }
}
