using System.Buffers.Binary;
using System.Text;
using System.Formats.Asn1;
using Melanzana.MachO;

namespace Melanzana.CodeSign.Requirements
{
    public abstract partial class Expression
    {
        class SimpleExpression : Expression
        {
            ExpressionOperation op;

            public SimpleExpression(ExpressionOperation op)
            {
                this.op = op;
            }

            public override int Size => 4;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)op);
                bytesWritten = 4;
            }

            public override string ToString()
            {
                return op switch {
                    ExpressionOperation.False => "never",
                    ExpressionOperation.True => "always",
                    ExpressionOperation.AppleAnchor => "anchor apple",
                    ExpressionOperation.AppleGenericAnchor => "anchor apple generic",
                    ExpressionOperation.TrustedCerts => "anchor trusted",
                    ExpressionOperation.Notarized => "notarized",
                    ExpressionOperation.LegacyDevID => "legacy",
                    _ => "unknown",
                };
            }
        }

        class BinaryOperatorExpression : Expression
        {
            ExpressionOperation op;
            Expression left;
            Expression right;

            public BinaryOperatorExpression(ExpressionOperation op, Expression left, Expression right)
            {
                this.op = op;
                this.left = left;
                this.right = right;
            }

            public override int Size => 4 + left.Size + right.Size;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)op);
                left.Write(buffer.Slice(4), out var bytesWrittenLeft);
                right.Write(buffer.Slice(4 + bytesWrittenLeft), out var bytesWrittenRight);
                bytesWritten = 4 + bytesWrittenLeft + bytesWrittenRight;
            }

            private string? WrapInnerExpression(Expression innerExpression)
            {
                if (innerExpression is BinaryOperatorExpression boe &&
                    boe.op != op)
                {
                    return $"({boe})";
                }

                return innerExpression.ToString();
            }

            public override string ToString()
            {
                return op switch {
                    ExpressionOperation.And => $"{WrapInnerExpression(left)} and {WrapInnerExpression(right)}",
                    ExpressionOperation.Or => $"{WrapInnerExpression(left)} or {WrapInnerExpression(right)}",
                    _ => "unknown",
                };
            }
        }

        class UnaryOperatorExpression : Expression
        {
            ExpressionOperation op;
            Expression inner;

            public UnaryOperatorExpression(ExpressionOperation op, Expression inner)
            {
                this.op = op;
                this.inner = inner;
            }

            public override int Size => 4 + inner.Size;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)op);
                inner.Write(buffer.Slice(4), out var bytesWrittenInner);
                bytesWritten = 4 + bytesWrittenInner;
            }

            public override string ToString()
            {
                return op switch {
                    ExpressionOperation.Not => $"! {inner}",
                    _ => "unknown",
                };
            }
        }

        class StringExpression : Expression
        {
            ExpressionOperation op;
            string opString;

            public StringExpression(ExpressionOperation op, string opString)
            {
                this.op = op;
                this.opString = opString;
            }

            public override int Size => 8 + Align(Encoding.UTF8.GetByteCount(opString));

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                byte[] opStringBytes = Encoding.UTF8.GetBytes(opString);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)op);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4), opStringBytes.Length);
                opStringBytes.CopyTo(buffer.Slice(8, opStringBytes.Length));
                buffer.Slice(8 + opStringBytes.Length, Align(opStringBytes.Length) - opStringBytes.Length).Fill((byte)0);
                bytesWritten = 8 + Align(opStringBytes.Length);
            }

            public override string ToString()
            {
                return op switch {
                    ExpressionOperation.Ident => $"identifier \"{opString}\"", // FIXME: Escaping
                    _ => "unknown",
                };
            }
        }

        class CDHashExpression : Expression
        {
            private readonly byte[] codeDirectoryHash;

            public CDHashExpression(byte[] codeDirectoryHash)
            {
                this.codeDirectoryHash = codeDirectoryHash;
            }

            public override int Size => 4 + codeDirectoryHash.Length;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)ExpressionOperation.CDHash);
                codeDirectoryHash.CopyTo(buffer.Slice(4, codeDirectoryHash.Length));
                bytesWritten = 4 + codeDirectoryHash.Length;
            }

            public override string ToString()
            {
                return $"cdhash H\"{Convert.ToHexString(codeDirectoryHash)}\"";
            }
        }

        class AnchorHashExpression : Expression
        {
            private readonly int certificateIndex;
            private readonly byte[] anchorHash;

            public AnchorHashExpression(int certificateIndex, byte[] anchorHash)
            {
                this.certificateIndex = certificateIndex;
                this.anchorHash = anchorHash;
            }

            public override int Size => 8 + anchorHash.Length;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0, 4), (uint)ExpressionOperation.AnchorHash);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4, 4), anchorHash.Length);
                anchorHash.CopyTo(buffer.Slice(8, anchorHash.Length));
                bytesWritten = 8 + anchorHash.Length;
            }

            public override string ToString()
            {
                return $"certificate {CertificateSlotToString(certificateIndex)} = H\"{Convert.ToHexString(anchorHash)}\"";
            }
        }

        abstract class MatchExpression : Expression
        {
            ExpressionMatchType matchType;
            byte[]? matchValue;

            public MatchExpression(ExpressionMatchType matchType, byte[]? matchValue)
            {
                this.matchType = matchType;
                this.matchValue = matchValue;
            }

            public override int Size =>
                4 +
                (matchValue != null ? 4 + Align(matchValue.Length) : 0);

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)matchType);
                if (matchValue != null)
                {
                    BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4), matchValue.Length);
                    matchValue.CopyTo(buffer.Slice(8, matchValue.Length));
                    buffer.Slice(8 + matchValue.Length, Align(matchValue.Length) - matchValue.Length).Fill((byte)0);
                    bytesWritten = 8 + Align(matchValue.Length);
                }
                else
                {
                    bytesWritten = 4;
                }
            }

            public override string ToString()
            {
                return matchType switch {
                    ExpressionMatchType.Exists => "/* exists */",
                    ExpressionMatchType.Absent => "absent",
                    ExpressionMatchType.Equal => $"= {ValueToString(matchValue!)}",
                    ExpressionMatchType.Contains => $"~ {ValueToString(matchValue!)}",
                    ExpressionMatchType.BeginsWith => $"= {ValueToString(matchValue!)}*",
                    ExpressionMatchType.EndsWith => $"= *{ValueToString(matchValue!)}",
                    ExpressionMatchType.LessThan => $"< {ValueToString(matchValue!)}",
                    ExpressionMatchType.GreaterEqual => $">= {ValueToString(matchValue!)}",
                    ExpressionMatchType.LessEqual => $"<= {ValueToString(matchValue!)}",
                    ExpressionMatchType.GreaterThan => $">= {ValueToString(matchValue!)}",
                    ExpressionMatchType.On => $"= timestamp \"{GetTimestampString(matchValue!)}\"",
                    ExpressionMatchType.Before => $"< timestamp \"{GetTimestampString(matchValue!)}\"",
                    ExpressionMatchType.After => $"> timestamp \"{GetTimestampString(matchValue!)}\"",
                    ExpressionMatchType.OnOrBefore => $"<= timestamp \"{GetTimestampString(matchValue!)}\"",
                    ExpressionMatchType.OnOrAfter => $">= timestamp \"{GetTimestampString(matchValue!)}\"",
                    _ => "unknown",
                };
            }
        }

        class FieldMatchExpression : MatchExpression
        {
            private readonly ExpressionOperation op;
            private readonly object field;
            private readonly byte[] fieldBytes;

            public FieldMatchExpression(
                ExpressionOperation op,
                string field,
                ExpressionMatchType matchType,
                byte[]? matchValue)
                : base(matchType, matchValue) 
            {
                this.op = op;
                this.field = field;
                fieldBytes = Encoding.ASCII.GetBytes(field);
            }

            public override int Size =>
                8 +
                Align(fieldBytes.Length) +
                base.Size;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0, 4), (uint)op);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4, 4), fieldBytes.Length);
                fieldBytes.CopyTo(buffer.Slice(8, fieldBytes.Length));
                buffer.Slice(8 + fieldBytes.Length, Align(fieldBytes.Length) - fieldBytes.Length).Fill((byte)0);
                bytesWritten = 8 + Align(fieldBytes.Length);
                base.Write(buffer.Slice(bytesWritten), out var matchExpressionSize);
                bytesWritten += matchExpressionSize;
            }


            public override string ToString()
            {
                return op switch {
                    ExpressionOperation.InfoKeyField => $"info[{field}] {base.ToString()}",
                    ExpressionOperation.EntitlementField => $"entitlement[{field}] {base.ToString()}",
                    _ => "unknown",
                };
            }
        }

        class CertExpression : MatchExpression
        {
            private readonly ExpressionOperation op;
            private readonly int certificateIndex;
            private readonly byte[] certificateField;

            public CertExpression(
                ExpressionOperation op,
                int certificateIndex,
                byte[] certificateField,
                ExpressionMatchType matchType,
                byte[]? matchValue)
                : base(matchType, matchValue) 
            {
                this.op = op;
                this.certificateIndex = certificateIndex;
                this.certificateField = certificateField;
            }

            public override int Size =>
                12 +
                Align(certificateField.Length) +
                base.Size;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)op);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4), certificateIndex);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(8), certificateField.Length);
                certificateField.CopyTo(buffer.Slice(12, certificateField.Length));
                buffer.Slice(12 + certificateField.Length, Align(certificateField.Length) - certificateField.Length).Fill((byte)0);
                bytesWritten = 12 + Align(certificateField.Length);
                base.Write(buffer.Slice(bytesWritten), out var matchExpressionSize);
                bytesWritten += matchExpressionSize;
            }


            public override string ToString()
            {
                return op switch {
                    ExpressionOperation.CertField => $"certificate {CertificateSlotToString(certificateIndex)}[{Encoding.ASCII.GetString(certificateField)}] {base.ToString()}",
                    ExpressionOperation.CertGeneric => $"certificate {CertificateSlotToString(certificateIndex)}[field.{GetOidString(certificateField)}] {base.ToString()}",
                    ExpressionOperation.CertPolicy => $"certificate {CertificateSlotToString(certificateIndex)}[policy.{GetOidString(certificateField)}] {base.ToString()}",
                    ExpressionOperation.CertFieldDate => $"certificate {CertificateSlotToString(certificateIndex)}[timestamp.{GetOidString(certificateField)}] {base.ToString()}",
                    _ => "unknown",
                };
            }
        }

        class InfoKeyValueExpression : Expression
        {
            private readonly object field;
            private readonly byte[] fieldBytes;
            private readonly byte[] matchValue;

            public InfoKeyValueExpression(
                string field,
                byte[] matchValue)
            {
                this.field = field;
                this.matchValue = matchValue;

                fieldBytes = Encoding.ASCII.GetBytes(field);
            }

            public override int Size =>
                12 +
                Align(fieldBytes.Length) +
                Align(matchValue.Length);

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0, 4), (uint)ExpressionOperation.InfoKeyValue);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4, 4), fieldBytes.Length);
                fieldBytes.CopyTo(buffer.Slice(8, fieldBytes.Length));
                buffer.Slice(8 + fieldBytes.Length, Align(fieldBytes.Length) - fieldBytes.Length).Fill((byte)0);
                bytesWritten = 8 + Align(fieldBytes.Length);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(bytesWritten, 4), matchValue.Length);
                fieldBytes.CopyTo(buffer.Slice(bytesWritten + 4, matchValue.Length));
                buffer.Slice(4 + bytesWritten + matchValue.Length, Align(matchValue.Length) - matchValue.Length).Fill((byte)0);
                bytesWritten += 4 + Align(matchValue.Length);
            }

            public override string ToString()
            {
                return $"info[{field}] = {ValueToString(matchValue)}";
            }
        }

        class NamedExpression : Expression
        {
            private readonly ExpressionOperation op;
            private readonly byte[] name;

            public NamedExpression(
                ExpressionOperation op,
                byte[] name)
            {
                this.op = op;
                this.name = name;
            }

            public override int Size =>
                8 +
                Align(name.Length);

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0, 4), (uint)op);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4, 4), name.Length);
                name.CopyTo(buffer.Slice(8, name.Length));
                buffer.Slice(8 + name.Length, Align(name.Length) - name.Length).Fill((byte)0);
                bytesWritten = 8 + Align(name.Length);
            }

            public override string ToString()
            {
                return op switch {
                    ExpressionOperation.NamedAnchor => $"anchor apple {ValueToString(name)}",
                    ExpressionOperation.NamedCode => $"({ValueToString(name)})",
                    _ => "unknown",
                };
            }
        }

        class TrustedCertExpression : Expression
        {
            private readonly int certificateIndex;

            public TrustedCertExpression(int certificateIndex)
            {
                this.certificateIndex = certificateIndex;
            }

            public override int Size => 8;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0, 4), (uint)ExpressionOperation.TrustedCert);
                BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(4, 4), certificateIndex);
                bytesWritten = 8;
            }

            public override string ToString()
            {
                return $"certificate {CertificateSlotToString(certificateIndex)} trusted";
            }
        }

        class PlatformExpression : Expression
        {
            private readonly MachPlatform platform;

            public PlatformExpression(MachPlatform platform)
            {
                this.platform = platform;
            }

            public override int Size => 8;

            public override void Write(Span<byte> buffer, out int bytesWritten)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0, 4), (uint)ExpressionOperation.Platform);
                BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4, 4), (uint)platform);
                bytesWritten = 8;
            }

            public override string ToString()
            {
                return $"platform = {platform}";
            }
        }
    }
}
