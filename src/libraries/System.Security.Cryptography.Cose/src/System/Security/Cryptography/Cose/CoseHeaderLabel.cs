// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Represents a COSE header label.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct CoseHeaderLabel : IEquatable<CoseHeaderLabel>
    {
        internal string LabelName => LabelAsString != null ? $"\"{LabelAsString}\"" : LabelAsInt32.ToString();
        private string DebuggerDisplay => $"Label = {LabelName}, Type = {(LabelAsString != null ? typeof(string) : typeof(int))}";

        // https://www.iana.org/assignments/cose/cose.xhtml#header-parameters
        /// <summary>
        /// Gets a header label that represents the known header parameter "alg".
        /// </summary>
        /// <value>A header label that represents the known header parameter "alg".</value>
        public static CoseHeaderLabel Algorithm => new CoseHeaderLabel(KnownHeaders.Alg);
        /// <summary>
        /// Gets a header label that represents the known header parameter "crit".
        /// </summary>
        /// <value>A header label that represents the known header parameter "crit".</value>
        public static CoseHeaderLabel CriticalHeaders => new CoseHeaderLabel(KnownHeaders.Crit);
        /// <summary>
        /// Gets a header label that represents the known header parameter "content type".
        /// </summary>
        /// <value>A header label> that represents the known header parameter "content type".</value>
        public static CoseHeaderLabel ContentType => new CoseHeaderLabel(KnownHeaders.ContentType);
        /// <summary>
        /// Gets a header label that represents the known header parameter "kid".
        /// </summary>
        /// <value>A header label that represents the known header parameter "kid".</value>
        public static CoseHeaderLabel KeyIdentifier => new CoseHeaderLabel(KnownHeaders.Kid);

        internal int LabelAsInt32 { get; }
        internal string? LabelAsString { get; }
        internal int EncodedSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoseHeaderLabel"/> struct.
        /// </summary>
        /// <param name="label">The header label as an integer.</param>
        public CoseHeaderLabel(int label)
        {
            this = default;
            LabelAsInt32 = label;
            EncodedSize = CoseHelpers.GetIntegerEncodedSize(label);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoseHeaderLabel"/> struct.
        /// </summary>
        /// <param name="label">The header label as a text string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
        public CoseHeaderLabel(string label)
        {
            if (label is null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            this = default;
            LabelAsString = label;
            EncodedSize = CoseHelpers.GetTextStringEncodedSize(label);
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to the specified instance.
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><see langword="true"/> if the value parameter equals the value of this instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(CoseHeaderLabel other)
        {
            return LabelAsString == other.LabelAsString && LabelAsInt32 == other.LabelAsInt32;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><see langword="true"/> if value is an instance of <see cref="CoseHeaderLabel"/> and equals the value of this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CoseHeaderLabel otherObj && Equals(otherObj);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            // Since this type is used as a key in a dictionary (see CoseHeaderMap)
            // and since the label is potentially adversary-provided, we'll need
            // to randomize the hash code.

            if (LabelAsString != null)
            {
                return LabelAsString.GetRandomizedOrdinalHashCode();
            }

            return LabelAsInt32.GetRandomizedHashCode();
        }

        /// <summary>
        /// Determines whether two specified header label instances are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if left and right represent the same label; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(CoseHeaderLabel left, CoseHeaderLabel right) => left.Equals(right);

        /// <summary>
        /// Determines whether two specified header label instances are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if left and right do not represent the same label; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(CoseHeaderLabel left, CoseHeaderLabel right) => !left.Equals(right);
    }
}
