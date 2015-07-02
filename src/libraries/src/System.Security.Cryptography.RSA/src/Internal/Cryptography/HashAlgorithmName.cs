// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Internal.Cryptography
{
    // Temporarily defining HashAlgorithmName here until it comes online officially with the contract refresh.
    // (If the contract refresh is happening and this file is causing problems, just delete it)
    internal struct HashAlgorithmName
    {
        public static HashAlgorithmName MD5 { get { return new HashAlgorithmName("MD5"); } }
        public static HashAlgorithmName SHA1 { get { return new HashAlgorithmName("SHA1"); } }
        public static HashAlgorithmName SHA256 { get { return new HashAlgorithmName("SHA256"); } }
        public static HashAlgorithmName SHA384 { get { return new HashAlgorithmName("SHA384"); } }
        public static HashAlgorithmName SHA512 { get { return new HashAlgorithmName("SHA512"); } }

        private readonly string _name;

        public HashAlgorithmName(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }

        public override string ToString()
        {
            return _name ?? string.Empty;
        }

        public override bool Equals(object obj)
        {
            return obj is HashAlgorithmName && Equals((HashAlgorithmName)obj);
        }

        public bool Equals(HashAlgorithmName other)
        {
            return _name == other._name;
        }

        public override int GetHashCode()
        {
            return _name == null ? 0 : _name.GetHashCode();
        }

        public static bool operator ==(HashAlgorithmName left, HashAlgorithmName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HashAlgorithmName left, HashAlgorithmName right)
        {
            return !(left == right);
        }
    }
}
