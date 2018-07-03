// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    // NOTE: This is *currently* 1:1 with the enum, but it exists to reserve room for more options 
    //       such as custom # of PSS salt bytes without having to modify other parts of the API
    //       surface.

    /// <summary>
    /// Specifies the padding mode  and parameters to use with RSA signature creation or verification operations.
    /// </summary>
    public sealed class RSASignaturePadding : IEquatable<RSASignaturePadding>
    {
        private static readonly RSASignaturePadding s_pkcs1 = new RSASignaturePadding(RSASignaturePaddingMode.Pkcs1);
        private static readonly RSASignaturePadding s_pss = new RSASignaturePadding(RSASignaturePaddingMode.Pss);

        private readonly RSASignaturePaddingMode _mode;

        private RSASignaturePadding(RSASignaturePaddingMode mode)
        {
            _mode = mode;
        }

        /// <summary>
        /// <see cref="RSASignaturePaddingMode.Pkcs1"/> mode.
        /// </summary>
        public static RSASignaturePadding Pkcs1
        {
            get { return s_pkcs1; }
        }

        /// <summary>
        /// <see cref="RSASignaturePaddingMode.Pss"/> mode with the number of salt bytes equal to the size of the hash.
        /// </summary>
        public static RSASignaturePadding Pss
        {
            get { return s_pss; }
        }

        /// <summary>
        /// Gets the padding mode to use.
        /// </summary>
        public RSASignaturePaddingMode Mode
        {
            get { return _mode; }
        }

        public override int GetHashCode()
        {
            return _mode.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RSASignaturePadding);
        }

        public bool Equals(RSASignaturePadding other)
        {
            return other != null && _mode == other._mode;
        }

        public static bool operator ==(RSASignaturePadding left, RSASignaturePadding right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(RSASignaturePadding left, RSASignaturePadding right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return _mode.ToString();
        }
    }
}
