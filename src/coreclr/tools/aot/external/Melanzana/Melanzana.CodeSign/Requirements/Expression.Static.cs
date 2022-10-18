using System.Buffers.Binary;
using System.Text;
using System.Formats.Asn1;
using Melanzana.MachO;

namespace Melanzana.CodeSign.Requirements
{
    public abstract partial class Expression
    {
        public static Expression False { get; } = new SimpleExpression(ExpressionOperation.False);
        public static Expression True { get; } = new SimpleExpression(ExpressionOperation.True);
        public static Expression Ident(string identifier) => new StringExpression(ExpressionOperation.Ident, identifier);
        public static Expression AppleAnchor { get; } = new SimpleExpression(ExpressionOperation.AppleAnchor);
        public static Expression AnchorHash(int certificateIndex, byte[] anchorHash) => new AnchorHashExpression(certificateIndex, anchorHash);
        public static Expression InfoKeyValue(string field, string matchValue)
            => new InfoKeyValueExpression(field, Encoding.ASCII.GetBytes(matchValue));
        public static Expression And(Expression left, Expression right) => new BinaryOperatorExpression(ExpressionOperation.And, left, right);
        public static Expression Or(Expression left, Expression right) => new BinaryOperatorExpression(ExpressionOperation.Or, left, right);
        public static Expression CDHash(byte[] codeDirectoryHash) => new CDHashExpression(codeDirectoryHash);
        public static Expression Not(Expression inner) => new UnaryOperatorExpression(ExpressionOperation.Not, inner);
        public static Expression InfoKeyField(string field, ExpressionMatchType matchType, string? matchValue = null)
            => new FieldMatchExpression(ExpressionOperation.InfoKeyField, field, matchType, matchValue != null ? Encoding.ASCII.GetBytes(matchValue) : null);
        public static Expression CertField(int certificateIndex, string certificateField, ExpressionMatchType matchType, string? matchValue = null)
            => new CertExpression(ExpressionOperation.CertField, certificateIndex, Encoding.ASCII.GetBytes(certificateField), matchType, matchValue != null ? Encoding.ASCII.GetBytes(matchValue) : null);
        public static Expression TrustedCert(int certificateIndex) => new TrustedCertExpression(certificateIndex);
        public static Expression TrustedCerts { get; } = new SimpleExpression(ExpressionOperation.TrustedCerts);
        public static Expression CertGeneric(int certificateIndex, string certificateFieldOid, ExpressionMatchType matchType, string? matchValue = null)
            => new CertExpression(ExpressionOperation.CertGeneric, certificateIndex, GetOidBytes(certificateFieldOid), matchType, matchValue != null ? Encoding.ASCII.GetBytes(matchValue) : null);
        public static Expression AppleGenericAnchor { get; } = new SimpleExpression(ExpressionOperation.AppleGenericAnchor);
        public static Expression EntitlementField(string field, ExpressionMatchType matchType, string? matchValue = null)
            => new FieldMatchExpression(ExpressionOperation.EntitlementField, field, matchType, matchValue != null ? Encoding.ASCII.GetBytes(matchValue) : null);
        public static Expression CertPolicy(int certificateIndex, string certificateFieldOid, ExpressionMatchType matchType, string? matchValue = null)
            => new CertExpression(ExpressionOperation.CertPolicy, certificateIndex, GetOidBytes(certificateFieldOid), matchType, matchValue != null ? Encoding.ASCII.GetBytes(matchValue) : null);
        public static Expression NamedAnchor(string anchorName) => new NamedExpression(ExpressionOperation.NamedAnchor, Encoding.ASCII.GetBytes(anchorName));
        public static Expression NamedCode(string code) => new NamedExpression(ExpressionOperation.NamedCode, Encoding.ASCII.GetBytes(code));
        public static Expression Platform(MachPlatform platform) => new PlatformExpression(platform);
        public static Expression Notarized { get; } = new SimpleExpression(ExpressionOperation.Notarized);
        public static Expression CertFieldDate(int certificateIndex, string certificateFieldOid, ExpressionMatchType matchType, DateTime? matchValue = null)
            => new CertExpression(ExpressionOperation.CertFieldDate, certificateIndex, GetOidBytes(certificateFieldOid), matchType, matchValue.HasValue ? GetTimestampBytes(matchValue.Value) : null);
        public static Expression LegacyDevID { get; } = new SimpleExpression(ExpressionOperation.LegacyDevID);
    }
}
