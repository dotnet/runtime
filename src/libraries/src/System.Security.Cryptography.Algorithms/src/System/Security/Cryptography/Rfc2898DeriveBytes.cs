// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public class Rfc2898DeriveBytes : DeriveBytes
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "HMACSHA1 is needed for compat. (https://github.com/dotnet/corefx/issues/9438)")]
        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations)
        {
            if (salt == null)
                throw new ArgumentNullException(nameof(salt));
            if (salt.Length < MinimumSaltSize)
                throw new ArgumentException(SR.Cryptography_PasswordDerivedBytes_FewBytesSalt, nameof(salt));
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(iterations), SR.ArgumentOutOfRange_NeedPosNum);
            if (password == null)
                throw new NullReferenceException();  // This "should" be ArgumentNullException but for compat, we throw NullReferenceException.

            _salt = salt.CloneByteArray();
            _iterations = (uint)iterations;
            _password = password.CloneByteArray();
            _hmacSha1 = new HMACSHA1(_password);

            Initialize();
        }

        public Rfc2898DeriveBytes(string password, byte[] salt)
             : this(password, salt, 1000)
        {
        }

        public Rfc2898DeriveBytes(string password, byte[] salt, int iterations)
            : this(Encoding.UTF8.GetBytes(password), salt, iterations)
        {
        }

        public Rfc2898DeriveBytes(string password, int saltSize)
            : this(password, saltSize, 1000)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "HMACSHA1 is needed for compat. (https://github.com/dotnet/corefx/issues/9438)")]
        public Rfc2898DeriveBytes(string password, int saltSize, int iterations)
        {
            if (saltSize < 0)
                throw new ArgumentOutOfRangeException(nameof(saltSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (saltSize < MinimumSaltSize)
                throw new ArgumentException(SR.Cryptography_PasswordDerivedBytes_FewBytesSalt, nameof(saltSize));
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(iterations), SR.ArgumentOutOfRange_NeedPosNum);

            _salt = Helpers.GenerateRandom(saltSize);
            _iterations = (uint)iterations;
            _password = Encoding.UTF8.GetBytes(password);
            _hmacSha1 = new HMACSHA1(_password);

            Initialize();
        }

        public int IterationCount
        {
            get
            {
                return (int)_iterations;
            }

            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedPosNum);
                _iterations = (uint)value;
                Initialize();
            }
        }

        public byte[] Salt
        {
            get
            {
                return _salt.CloneByteArray();
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length < MinimumSaltSize)
                    throw new ArgumentException(SR.Cryptography_PasswordDerivedBytes_FewBytesSalt);
                _salt = value.CloneByteArray();
                Initialize();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hmacSha1 != null)
                    _hmacSha1.Dispose();
                _hmacSha1 = null;
                if (_buffer != null)
                    Array.Clear(_buffer, 0, _buffer.Length);
                if (_password != null)
                    Array.Clear(_password, 0, _password.Length);
                if (_salt != null)
                    Array.Clear(_salt, 0, _salt.Length);
            }
            base.Dispose(disposing);
        }

        public override byte[] GetBytes(int cb)
        {
            if (cb <= 0)
                throw new ArgumentOutOfRangeException(nameof(cb), SR.ArgumentOutOfRange_NeedPosNum);
            byte[] password = new byte[cb];

            int offset = 0;
            int size = _endIndex - _startIndex;
            if (size > 0)
            {
                if (cb >= size)
                {
                    Buffer.BlockCopy(_buffer, _startIndex, password, 0, size);
                    _startIndex = _endIndex = 0;
                    offset += size;
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _startIndex, password, 0, cb);
                    _startIndex += cb;
                    return password;
                }
            }

            Debug.Assert(_startIndex == 0 && _endIndex == 0, "Invalid start or end index in the internal buffer.");

            while (offset < cb)
            {
                byte[] T_block = Func();
                int remainder = cb - offset;
                if (remainder > BlockSize)
                {
                    Buffer.BlockCopy(T_block, 0, password, offset, BlockSize);
                    offset += BlockSize;
                }
                else
                {
                    Buffer.BlockCopy(T_block, 0, password, offset, remainder);
                    offset += remainder;
                    Buffer.BlockCopy(T_block, remainder, _buffer, _startIndex, BlockSize - remainder);
                    _endIndex += (BlockSize - remainder);
                    return password;
                }
            }
            return password;
        }

        public override void Reset()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_buffer != null)
                Array.Clear(_buffer, 0, _buffer.Length);
            _buffer = new byte[BlockSize];
            _block = 1;
            _startIndex = _endIndex = 0;
        }

        // This function is defined as follows:
        // Func (S, i) = HMAC(S || i) | HMAC2(S || i) | ... | HMAC(iterations) (S || i) 
        // where i is the block number.
        private byte[] Func()
        {
            byte[] temp = new byte[_salt.Length + sizeof(uint)];
            Buffer.BlockCopy(_salt, 0, temp, 0, _salt.Length);
            Helpers.WriteInt(_block, temp, _salt.Length);

            temp = _hmacSha1.ComputeHash(temp);
            
            byte[] ret = temp;
            for (int i = 2; i <= _iterations; i++)
            {
                temp = _hmacSha1.ComputeHash(temp);

                for (int j = 0; j < BlockSize; j++)
                {
                    ret[j] ^= temp[j];
                }
            }

            // increment the block count.
            _block++;
            return ret;
        }

        private readonly byte[] _password;
        private byte[] _salt;
        private uint _iterations;
        private HMACSHA1 _hmacSha1;

        private byte[] _buffer;
        private uint _block;
        private int _startIndex;
        private int _endIndex;

        private const int BlockSize = 20;
        private const int MinimumSaltSize = 8;
    }
}
