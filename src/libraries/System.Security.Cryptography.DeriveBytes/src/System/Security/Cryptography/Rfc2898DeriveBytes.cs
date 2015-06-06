// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public class Rfc2898DeriveBytes : DeriveBytes
    {
        public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations)
        {
            Salt = salt;
            IterationCount = iterations;
            if (password == null)
                throw new NullReferenceException();  // This "should" be ArgumentNullException but for compat, we throw NullReferenceException.
            _password = password.CloneByteArray();
            _hmacSha1 = new HMACSHA1(_password);

            // We "should" call Initialize() here but we've already called it twice indirectly through setting the Salt and IterationCount properties.
            return;
        }

        public Rfc2898DeriveBytes(String password, byte[] salt)
             : this(password, salt, 1000)
        {
        }

        public Rfc2898DeriveBytes(String password, byte[] salt, int iterations)
            : this(new UTF8Encoding(false).GetBytes(password), salt, iterations)
        {
        }

        public Rfc2898DeriveBytes(String password, int saltSize)
            : this(password, saltSize, 1000)
        {
        }

        public Rfc2898DeriveBytes(String password, int saltSize, int iterations)
        {
            if (saltSize < 0)
                throw new ArgumentOutOfRangeException("saltSize", SR.ArgumentOutOfRange_NeedNonNegNum);

            Salt = Helpers.GenerateRandom(saltSize);
            IterationCount = iterations;
            _password = new UTF8Encoding(false).GetBytes(password);
            _hmacSha1 = new HMACSHA1(_password);

            // We "should" call Initialize() here but we've already called it twice indirectly through setting the Salt and IterationCount properties.
            return;
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
                    throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedPosNum);
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
                    throw new ArgumentNullException("value");
                if (value.Length < 8)
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
                throw new ArgumentOutOfRangeException("cb", SR.ArgumentOutOfRange_NeedPosNum);
            byte[] password = new byte[cb];

            int offset = 0;
            int size = _endIndex - _startIndex;
            if (size > 0)
            {
                if (cb >= size)
                {
                    Array.Copy(_buffer, _startIndex, password, 0, size);
                    _startIndex = _endIndex = 0;
                    offset += size;
                }
                else
                {
                    Array.Copy(_buffer, _startIndex, password, 0, cb);
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
                    Array.Copy(T_block, 0, password, offset, BlockSize);
                    offset += BlockSize;
                }
                else
                {
                    Array.Copy(T_block, 0, password, offset, remainder);
                    offset += remainder;
                    Array.Copy(T_block, remainder, _buffer, _startIndex, BlockSize - remainder);
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
            byte[] INT_block = Helpers.Int(_block);

            byte[] temp = new byte[_salt.Length + INT_block.Length];
            Buffer.BlockCopy(_salt, 0, temp, 0, _salt.Length);
            Buffer.BlockCopy(INT_block, 0, temp, _salt.Length, INT_block.Length);

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
    }
}
