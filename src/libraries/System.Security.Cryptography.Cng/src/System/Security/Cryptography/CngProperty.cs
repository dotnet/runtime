// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Wrapper representing an arbitrary property of a CNG key or provider
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]  // The [StructLayout] is here to prevent a spurious ApiReviewer alert. We do not actually depend on the layout of this struct.
    public struct CngProperty : IEquatable<CngProperty>
    {
        public CngProperty(string name, byte[] value, CngPropertyOptions options)
            : this()
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Name = name;
            Options = options;
            _lazyHashCode = default(int?);
            _value = (value == null) ? null : value.CloneByteArray();
        }

        /// <summary>
        ///     Name of the property
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Contents of the property
        /// </summary>
        /// <returns></returns>
        public byte[] GetValue()
        {
            return (_value == null) ? null : _value.CloneByteArray();
        }

        /// <summary>
        ///     Options used to set / get the property
        /// </summary>
        public CngPropertyOptions Options { get; private set; }

        public override bool Equals(object obj)
        {
            return obj is CngProperty && Equals((CngProperty)obj);
        }

        public bool Equals(CngProperty other)
        {
            //
            // We will consider CNG properties equal only if the name, options and value are all also equal
            //

            if (!string.Equals(Name, other.Name, StringComparison.Ordinal))
                return false;

            if (Options != other.Options)
                return false;

            if (_value == null)
                return other._value == null;

            if (other._value == null)
                return false;

            if (_value.Length != other._value.Length)
                return false;

            for (int i = 0; i < _value.Length; i++)
            {
                if (_value[i] != other._value[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (!_lazyHashCode.HasValue)
            {
                int hashCode = Name.GetHashCode() ^ Options.GetHashCode();

                // The hash code for a byte is just the value of that byte. Since this will only modify the
                // lower bits of the hash code, we'll xor each byte into different sections of the hash code
                if (_value != null)
                {
                    for (int i = 0; i < _value.Length; i++)
                    {
                        // Shift each byte forward by one byte, so that every 4 bytes has to potential to update
                        // each of the calculated hash code's bytes.
                        int shifted = (int)(_value[i] << ((i % 4) * 8));
                        hashCode ^= shifted;
                    }
                }

                _lazyHashCode = hashCode;
            }

            return _lazyHashCode.Value;
        }

        public static bool operator ==(CngProperty left, CngProperty right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CngProperty left, CngProperty right)
        {
            return !left.Equals(right);
        }

        internal byte[] GetValueWithoutCopying()
        {
            return _value;
        }

        private readonly byte[] _value;
        private int? _lazyHashCode;
    }
}

