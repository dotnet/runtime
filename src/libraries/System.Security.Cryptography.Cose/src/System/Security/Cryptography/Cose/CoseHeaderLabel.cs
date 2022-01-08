// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography.Cose
{
    [DebuggerDisplay("Label = {LabelAsInt32.HasValue ? LabelAsInt32 : LabelAsString}, Type = {LabelAsInt32.HasValue ? typeof(int) : typeof(string),nq}")]
    public readonly struct CoseHeaderLabel : IEquatable<CoseHeaderLabel>
    {
        // https://www.iana.org/assignments/cose/cose.xhtml#header-parameters
        public static readonly CoseHeaderLabel Algorithm = new CoseHeaderLabel(KnownHeaders.Alg);
        public static readonly CoseHeaderLabel Critical = new CoseHeaderLabel(KnownHeaders.Crit);
        public static readonly CoseHeaderLabel ContentType = new CoseHeaderLabel(KnownHeaders.ContentType);
        public static readonly CoseHeaderLabel KeyIdentifier = new CoseHeaderLabel(KnownHeaders.Kid);
        public static readonly CoseHeaderLabel IV = new CoseHeaderLabel(KnownHeaders.IV);
        public static readonly CoseHeaderLabel PartialIV = new CoseHeaderLabel(KnownHeaders.PartialIV);
        public static readonly CoseHeaderLabel CounterSignature = new CoseHeaderLabel(KnownHeaders.CounterSignature);

        internal int? LabelAsInt32 { get; }
        internal string? LabelAsString { get; }

        public CoseHeaderLabel(int label)
        {
            LabelAsInt32 = label;
            LabelAsString = null;
        }

        public CoseHeaderLabel(string label)
        {
            LabelAsInt32 = null;
            LabelAsString = label ?? throw new ArgumentException(null, nameof(label));
        }

        public bool Equals(CoseHeaderLabel other)
            => LabelAsInt32.HasValue ? LabelAsInt32 == other.LabelAsInt32 : LabelAsInt32 == other.LabelAsInt32;
    }
}
