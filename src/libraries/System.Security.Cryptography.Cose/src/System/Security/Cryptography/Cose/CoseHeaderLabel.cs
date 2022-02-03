// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography.Cose
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [RequiresPreviewFeatures(CoseMessage.PreviewFeatureMessage)]
    public readonly struct CoseHeaderLabel : IEquatable<CoseHeaderLabel>
    {
        internal string LabelName => LabelAsInt32?.ToString() ?? LabelAsString ?? 0.ToString();
        private string DebuggerDisplay => $"Label = {LabelName}, Type = {(LabelAsInt32.HasValue ? typeof(int) : typeof(string))}";

        // https://www.iana.org/assignments/cose/cose.xhtml#header-parameters
        public static CoseHeaderLabel Algorithm => new CoseHeaderLabel(KnownHeaders.Alg);
        public static CoseHeaderLabel Critical => new CoseHeaderLabel(KnownHeaders.Crit);
        public static CoseHeaderLabel ContentType => new CoseHeaderLabel(KnownHeaders.ContentType);
        public static CoseHeaderLabel KeyIdentifier => new CoseHeaderLabel(KnownHeaders.Kid);
        public static CoseHeaderLabel IV => new CoseHeaderLabel(KnownHeaders.IV);
        public static CoseHeaderLabel PartialIV => new CoseHeaderLabel(KnownHeaders.PartialIV);
        public static CoseHeaderLabel CounterSignature => new CoseHeaderLabel(KnownHeaders.CounterSignature);

        internal int? LabelAsInt32 { get; }
        internal string? LabelAsString { get; }

        public CoseHeaderLabel(int label)
        {
            this = default;
            LabelAsInt32 = label;
        }

        public CoseHeaderLabel(string label)
        {
            if (label is null)
            {
                throw new ArgumentException(null, nameof(label));
            }

            this = default;
            LabelAsString = label;
        }

        public bool Equals(CoseHeaderLabel other)
        {
            if (LabelAsInt32.HasValue)
            {
                return LabelAsInt32 == other.LabelAsInt32;
            }

            if (LabelAsString != null)
            {
                return LabelAsString == other.LabelAsString;
            }

            // this is default, if other is not default treat this as new CoseHeaderLabel(0)
            return (other.LabelAsInt32 == null && other.LabelAsString == null) || 0 == other.LabelAsInt32;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CoseHeaderLabel otherObj && Equals(otherObj);

        public override int GetHashCode()
        {
            if (LabelAsInt32 != null)
            {
                return LabelAsInt32.Value.GetHashCode();
            }

            if (LabelAsString != null)
            {
                return LabelAsString.GetHashCode();
            }

            return 0.GetHashCode();
        }

        public static bool operator ==(CoseHeaderLabel left, CoseHeaderLabel right) => left.Equals(right);

        public static bool operator !=(CoseHeaderLabel left, CoseHeaderLabel right) => !left.Equals(right);
    }
}
