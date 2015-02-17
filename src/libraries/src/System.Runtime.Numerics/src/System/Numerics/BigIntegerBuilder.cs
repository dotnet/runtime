// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace System.Numerics
{
    /// <summary>
    /// BigIntegerBuilder holds a multiprecision unsigned integer value. It is mutable and
    /// supports common arithmetic operations. Be careful NOT to simply assign one
    /// BigIntegerBuilder to another, unless you really know what you are doing. The proper
    /// way to replicate a BigIntegerBuilder is via the constructor "BigIntegerBuilder(ref BigIntegerBuilder reg)",
    /// or with reg1.Load(ref reg2). Using the ctor marks the buffer as shared so changing the
    /// value of one register will not affect the other. Using Load copies the contents from
    /// one to the other. Either way, the internal buffer isn't aliased incorrectly.
    /// </summary>
    internal struct BigIntegerBuilder
    {
        private const int kcbitUint = 32;

        // For a single uint, _iuLast is 0.
        private int _iuLast;

        // Used if _iuLast == 0.
        private uint _uSmall;

        // Used if _iuLast > 0.
        private uint[] _rgu;

        // This is false whenever _rgu is null or is shared with a NewBigInteger
        // or with another BigIntegerBuilder.
        private bool _fWritable;

        [Conditional("DEBUG")]
        private void AssertValid(bool fTrimmed)
        {
            if (_iuLast <= 0)
            {
                Contract.Assert(_iuLast == 0);
                Contract.Assert(!_fWritable || _rgu != null);
            }
            else
            {
                Contract.Assert(_rgu != null && _rgu.Length > _iuLast);
                Contract.Assert(!fTrimmed || _rgu[_iuLast] != 0);
            }
        }

        public BigIntegerBuilder(ref BigIntegerBuilder reg)
        {
            reg.AssertValid(true);
            this = reg;
            if (_fWritable)
            {
                _fWritable = false;
                if (_iuLast == 0)
                    _rgu = null;
                else
                    reg._fWritable = false;
            }
            AssertValid(true);
        }

        public BigIntegerBuilder(int cuAlloc)
        {
            _iuLast = 0;
            _uSmall = 0;
            if (cuAlloc > 1)
            {
                _rgu = new uint[cuAlloc];
                _fWritable = true;
            }
            else
            {
                _rgu = null;
                _fWritable = false;
            }
            AssertValid(true);
        }

        public BigIntegerBuilder(BigInteger bn)
        {
            _fWritable = false;
            _rgu = bn._Bits;
            if (_rgu == null)
            {
                _iuLast = 0;
                _uSmall = NumericsHelpers.Abs(bn._Sign);
            }
            else
            {
                _iuLast = _rgu.Length - 1;
                _uSmall = _rgu[0];
                while (_iuLast > 0 && _rgu[_iuLast] == 0)
                    --_iuLast;
            }
            AssertValid(true);
        }

        public BigIntegerBuilder(BigInteger bn, ref int sign)
        {
            Contract.Requires(sign == +1 || sign == -1);

            _fWritable = false;
            _rgu = bn._Bits;
            int n = bn._Sign;
            int mask = n >> (kcbitUint - 1);
            sign = (sign ^ mask) - mask;
            if (_rgu == null)
            {
                _iuLast = 0;
                _uSmall = (uint)((n ^ mask) - mask);
            }
            else
            {
                _iuLast = _rgu.Length - 1;
                _uSmall = _rgu[0];
                while (_iuLast > 0 && _rgu[_iuLast] == 0)
                    --_iuLast;
            }
            AssertValid(true);
        }

        public BigInteger GetInteger(int sign)
        {
            Contract.Requires(sign == +1 || sign == -1);
            AssertValid(true);

            uint[] bits;
            GetIntegerParts(sign, out sign, out bits);
            return new BigInteger(sign, bits);
        }

        internal void GetIntegerParts(int signSrc, out int sign, out uint[] bits)
        {
            Contract.Requires(signSrc == +1 || signSrc == -1);
            AssertValid(true);

            if (_iuLast == 0)
            {
                if (_uSmall <= int.MaxValue)
                {
                    sign = signSrc * (int)_uSmall;
                    bits = null;
                    return;
                }
                if (_rgu == null)
                    _rgu = new uint[1] { _uSmall };
                else if (_fWritable)
                    _rgu[0] = _uSmall;
                else if (_rgu[0] != _uSmall)
                    _rgu = new uint[1] { _uSmall };
            }

            // The sign is +/- 1.
            sign = signSrc;

            int cuExtra = _rgu.Length - _iuLast - 1;
            Contract.Assert(cuExtra >= 0);
            if (cuExtra <= 1)
            {
                if (cuExtra == 0 || _rgu[_iuLast + 1] == 0)
                {
                    _fWritable = false;
                    bits = _rgu;
                    return;
                }
                if (_fWritable)
                {
                    _rgu[_iuLast + 1] = 0;
                    _fWritable = false;
                    bits = _rgu;
                    return;
                }
                // The buffer isn't writable, but has an extra uint that is non-zero,
                // so we have to allocate a new buffer.
            }

            // Keep the bigger buffer (if it is writable), but create a smaller one for the BigInteger.
            bits = _rgu;
            Array.Resize(ref bits, _iuLast + 1);
            if (!_fWritable)
                _rgu = bits;
        }

        public void Set(uint u)
        {
            _uSmall = u;
            _iuLast = 0;
            AssertValid(true);
        }

        public void Set(ulong uu)
        {
            uint uHi = NumericsHelpers.GetHi(uu);
            if (uHi == 0)
            {
                _uSmall = NumericsHelpers.GetLo(uu);
                _iuLast = 0;
            }
            else
            {
                SetSizeLazy(2);
                _rgu[0] = (uint)uu;
                _rgu[1] = uHi;
            }
            AssertValid(true);
        }

        public int Size { get { return _iuLast + 1; } }

        public uint High { get { return _iuLast == 0 ? _uSmall : _rgu[_iuLast]; } }


        public void GetApproxParts(out int exp, out ulong man)
        {
            AssertValid(true);
            if (_iuLast == 0)
            {
                man = (ulong)_uSmall;
                exp = 0;
                return;
            }

            int cuLeft = _iuLast - 1;
            man = NumericsHelpers.MakeUlong(_rgu[cuLeft + 1], _rgu[cuLeft]);
            exp = cuLeft * kcbitUint;

            int cbit;
            if (cuLeft > 0 && (cbit = NumericsHelpers.CbitHighZero(_rgu[cuLeft + 1])) > 0)
            {
                // Get 64 bits.
                Contract.Assert(cbit < kcbitUint);
                man = (man << cbit) | (_rgu[cuLeft - 1] >> (kcbitUint - cbit));
                exp -= cbit;
            }
        }

        private void Trim()
        {
            AssertValid(false);
            if (_iuLast > 0 && _rgu[_iuLast] == 0)
            {
                _uSmall = _rgu[0];
                while (--_iuLast > 0 && _rgu[_iuLast] == 0)
                    ;
            }
            AssertValid(true);
        }

        private int CuNonZero
        {
            get
            {
                Contract.Assert(_iuLast > 0);
                int cu = 0;
                for (int iu = _iuLast; iu >= 0; --iu)
                {
                    if (_rgu[iu] != 0)
                        cu++;
                }
                return cu;
            }
        }

        // Sets the size to cu and makes sure the buffer is writable (if cu > 1),
        // but makes no guarantees about the contents of the buffer.
        private void SetSizeLazy(int cu)
        {
            Contract.Requires(cu > 0);
            AssertValid(false);
            if (cu <= 1)
            {
                _iuLast = 0;
                return;
            }
            if (!_fWritable || _rgu.Length < cu)
            {
                _rgu = new uint[cu];
                _fWritable = true;
            }
            _iuLast = cu - 1;
            AssertValid(false);
        }

        // Sets the size to cu, makes sure the buffer is writable (if cu > 1),
        // and set the contents to all zero.
        private void SetSizeClear(int cu)
        {
            Contract.Requires(cu > 0);
            AssertValid(false);
            if (cu <= 1)
            {
                _iuLast = 0;
                _uSmall = 0;
                return;
            }
            if (!_fWritable || _rgu.Length < cu)
            {
                _rgu = new uint[cu];
                _fWritable = true;
            }
            else
                Array.Clear(_rgu, 0, cu);
            _iuLast = cu - 1;
            AssertValid(false);
        }

        // Sets the size to cu, makes sure the buffer is writable (if cu > 1),
        // and maintains the contents. If the buffer is reallocated, cuExtra
        // uints are also allocated.
        private void SetSizeKeep(int cu, int cuExtra)
        {
            Contract.Requires(cu > 0 && cuExtra >= 0);
            AssertValid(false);

            if (cu <= 1)
            {
                if (_iuLast > 0)
                    _uSmall = _rgu[0];
                _iuLast = 0;
                return;
            }
            if (!_fWritable || _rgu.Length < cu)
            {
                uint[] rgu = new uint[cu + cuExtra];
                if (_iuLast == 0)
                    rgu[0] = _uSmall;
                else
                    Array.Copy(_rgu, rgu, Math.Min(cu, _iuLast + 1));
                _rgu = rgu;
                _fWritable = true;
            }
            else if (_iuLast + 1 < cu)
            {
                Array.Clear(_rgu, _iuLast + 1, cu - _iuLast - 1);
                if (_iuLast == 0)
                    _rgu[0] = _uSmall;
            }
            _iuLast = cu - 1;
            AssertValid(false);
        }

        // Makes sure the buffer is writable and can support cu uints.
        // Preserves the contents of the buffer up to min(_iuLast + 1, cu).
        // Changes the size if cu <= _iuLast and the buffer needs to be allocated.
        public void EnsureWritable(int cu, int cuExtra)
        {
            Contract.Requires(cu > 1 && cuExtra >= 0);
            AssertValid(false);

            if (_fWritable && _rgu.Length >= cu)
                return;

            uint[] rgu = new uint[cu + cuExtra];
            if (_iuLast > 0)
            {
                if (_iuLast >= cu)
                    _iuLast = cu - 1;
                Array.Copy(_rgu, rgu, _iuLast + 1);
            }
            _rgu = rgu;
            _fWritable = true;
            AssertValid(false);
        }

        // Makes sure the buffer is writable and can support _iuLast + 1 uints.
        // Preserves the contents of the buffer.
        public void EnsureWritable(int cuExtra)
        {
            Contract.Requires(cuExtra >= 0);
            AssertValid(false);
            Contract.Assert(_iuLast > 0);

            if (_fWritable)
                return;

            uint[] rgu = new uint[_iuLast + 1 + cuExtra];
            Array.Copy(_rgu, rgu, _iuLast + 1);
            _rgu = rgu;
            _fWritable = true;
            AssertValid(false);
        }

        public void EnsureWritable()
        {
            EnsureWritable(0);
        }

        // Loads the value of reg into this register.
        public void Load(ref BigIntegerBuilder reg)
        {
            Load(ref reg, 0);
        }

        // Loads the value of reg into this register. If we need to allocate memory
        // to perform the load, allocate cuExtra elements.
        public void Load(ref BigIntegerBuilder reg, int cuExtra)
        {
            Contract.Requires(cuExtra >= 0);
            AssertValid(false);
            reg.AssertValid(true);

            if (reg._iuLast == 0)
            {
                _uSmall = reg._uSmall;
                _iuLast = 0;
            }
            else
            {
                if (!_fWritable || _rgu.Length <= reg._iuLast)
                {
                    _rgu = new uint[reg._iuLast + 1 + cuExtra];
                    _fWritable = true;
                }
                _iuLast = reg._iuLast;
                Array.Copy(reg._rgu, _rgu, _iuLast + 1);
            }
            AssertValid(true);
        }

        public void Add(uint u)
        {
            AssertValid(true);
            if (_iuLast == 0)
            {
                if ((_uSmall += u) >= u)
                    return;
                SetSizeLazy(2);
                _rgu[0] = _uSmall;
                _rgu[1] = 1;
                return;
            }

            if (u == 0)
                return;

            uint uNew = _rgu[0] + u;
            if (uNew < u)
            {
                // Have carry.
                EnsureWritable(1);
                ApplyCarry(1);
            }
            else if (!_fWritable)
                EnsureWritable();
            _rgu[0] = uNew;
            AssertValid(true);
        }

        public void Add(ref BigIntegerBuilder reg)
        {
            AssertValid(true);
            reg.AssertValid(true);

            if (reg._iuLast == 0)
            {
                Add(reg._uSmall);
                return;
            }
            if (_iuLast == 0)
            {
                uint u = _uSmall;
                if (u == 0)
                    this = new BigIntegerBuilder(ref reg);
                else
                {
                    Load(ref reg, 1);
                    Add(u);
                }
                return;
            }

            EnsureWritable(Math.Max(_iuLast, reg._iuLast) + 1, 1);

            int cuAdd = reg._iuLast + 1;
            if (_iuLast < reg._iuLast)
            {
                cuAdd = _iuLast + 1;
                Array.Copy(reg._rgu, _iuLast + 1, _rgu, _iuLast + 1, reg._iuLast - _iuLast);
                Contract.Assert(_iuLast > 0);
                _iuLast = reg._iuLast;
            }

            // Add, tracking carry.
            uint uCarry = 0;
            for (int iu = 0; iu < cuAdd; iu++)
            {
                uCarry = AddCarry(ref _rgu[iu], reg._rgu[iu], uCarry);
                Contract.Assert(uCarry <= 1);
            }

            // Deal with extra carry.
            if (uCarry != 0)
                ApplyCarry(cuAdd);
            AssertValid(true);
        }

        public void Sub(ref int sign, uint u)
        {
            Contract.Requires(sign == +1 || sign == -1);
            AssertValid(true);
            if (_iuLast == 0)
            {
                if (u <= _uSmall)
                    _uSmall -= u;
                else
                {
                    _uSmall = u - _uSmall;
                    sign = -sign;
                }
                AssertValid(true);
                return;
            }

            if (u == 0)
                return;

            EnsureWritable();

            uint uTmp = _rgu[0];
            _rgu[0] = uTmp - u;
            if (uTmp < u)
            {
                ApplyBorrow(1);
                Trim();
            }
            AssertValid(true);
        }

        public void Sub(ref int sign, ref BigIntegerBuilder reg)
        {
            Contract.Requires(sign == +1 || sign == -1);
            AssertValid(true);
            reg.AssertValid(true);

            if (reg._iuLast == 0)
            {
                Sub(ref sign, reg._uSmall);
                return;
            }
            if (_iuLast == 0)
            {
                uint u = _uSmall;
                if (u == 0)
                    this = new BigIntegerBuilder(ref reg);
                else
                {
                    Load(ref reg);
                    Sub(ref sign, u);
                }
                sign = -sign;
                return;
            }

            if (_iuLast < reg._iuLast)
            {
                SubRev(ref reg);
                sign = -sign;
                return;
            }

            int cuSub = reg._iuLast + 1;
            if (_iuLast == reg._iuLast)
            {
                // Determine which is larger.
                _iuLast = BigInteger.GetDiffLength(_rgu, reg._rgu, _iuLast + 1) - 1;
                if (_iuLast < 0)
                {
                    _iuLast = 0;
                    _uSmall = 0;
                    return;
                }

                uint u1 = _rgu[_iuLast];
                uint u2 = reg._rgu[_iuLast];
                if (_iuLast == 0)
                {
                    if (u1 < u2)
                    {
                        _uSmall = u2 - u1;
                        sign = -sign;
                    }
                    else
                        _uSmall = u1 - u2;
                    AssertValid(true);
                    return;
                }

                if (u1 < u2)
                {
                    Contract.Assert(_iuLast > 0);
                    reg._iuLast = _iuLast;
                    SubRev(ref reg);
                    reg._iuLast = cuSub - 1;
                    Contract.Assert(reg._iuLast > 0);
                    sign = -sign;
                    return;
                }
                cuSub = _iuLast + 1;
            }

            EnsureWritable();

            // Subtract, tracking borrow.
            uint uBorrow = 0;
            for (int iu = 0; iu < cuSub; iu++)
            {
                uBorrow = SubBorrow(ref _rgu[iu], reg._rgu[iu], uBorrow);
                Contract.Assert(uBorrow <= 1);
            }
            if (uBorrow != 0)
            {
                Contract.Assert(uBorrow == 1 && cuSub <= _iuLast);
                ApplyBorrow(cuSub);
            }
            Trim();
        }

        // Subtract this register from the given one and put the result in this one.
        // Asserts that reg is larger in the most significant uint.
        private void SubRev(ref BigIntegerBuilder reg)
        {
            Contract.Assert(0 < _iuLast && _iuLast <= reg._iuLast);
            Contract.Assert(_iuLast < reg._iuLast || _rgu[_iuLast] < reg._rgu[_iuLast]);

            EnsureWritable(reg._iuLast + 1, 0);

            int cuSub = _iuLast + 1;
            if (_iuLast < reg._iuLast)
            {
                Array.Copy(reg._rgu, _iuLast + 1, _rgu, _iuLast + 1, reg._iuLast - _iuLast);
                Contract.Assert(_iuLast > 0);
                _iuLast = reg._iuLast;
            }

            uint uBorrow = 0;
            for (int iu = 0; iu < cuSub; iu++)
            {
                uBorrow = SubRevBorrow(ref _rgu[iu], reg._rgu[iu], uBorrow);
                Contract.Assert(uBorrow <= 1);
            }
            if (uBorrow != 0)
            {
                Contract.Assert(uBorrow == 1);
                ApplyBorrow(cuSub);
            }
            Trim();
        }

        public void Mul(uint u)
        {
            if (u == 0)
            {
                Set(0);
                return;
            }
            if (u == 1)
                return;

            if (_iuLast == 0)
            {
                Set((ulong)_uSmall * u);
                return;
            }

            EnsureWritable(1);

            uint uCarry = 0;
            for (int iu = 0; iu <= _iuLast; iu++)
                uCarry = MulCarry(ref _rgu[iu], u, uCarry);

            if (uCarry != 0)
            {
                SetSizeKeep(_iuLast + 2, 0);
                _rgu[_iuLast] = uCarry;
            }
        }

        // This version may share memory with regMul.
        public void Mul(ref BigIntegerBuilder regMul)
        {
            AssertValid(true);
            regMul.AssertValid(true);

            if (regMul._iuLast == 0)
                Mul(regMul._uSmall);
            else if (_iuLast == 0)
            {
                uint u = _uSmall;
                if (u == 1)
                    this = new BigIntegerBuilder(ref regMul);
                else if (u != 0)
                {
                    Load(ref regMul, 1);
                    Mul(u);
                }
            }
            else
            {
                int cuBase = _iuLast + 1;
                SetSizeKeep(cuBase + regMul._iuLast, 1);

                for (int iu = cuBase; --iu >= 0;)
                {
                    uint uMul = _rgu[iu];
                    _rgu[iu] = 0;
                    uint uCarry = 0;
                    for (int iuSrc = 0; iuSrc <= regMul._iuLast; iuSrc++)
                        uCarry = AddMulCarry(ref _rgu[iu + iuSrc], regMul._rgu[iuSrc], uMul, uCarry);
                    if (uCarry != 0)
                    {
                        for (int iuDst = iu + regMul._iuLast + 1; uCarry != 0 && iuDst <= _iuLast; iuDst++)
                            uCarry = AddCarry(ref _rgu[iuDst], 0, uCarry);
                        if (uCarry != 0)
                        {
                            SetSizeKeep(_iuLast + 2, 0);
                            _rgu[_iuLast] = uCarry;
                        }
                    }
                }
                AssertValid(true);
            }
        }

        // Multiply reg1 times reg2, putting the result in 'this'. This version never shares memory
        // with either of the operands. This is useful when performing a series of arithmetic operations
        // and large working buffers are allocated up front.
        public void Mul(ref BigIntegerBuilder reg1, ref BigIntegerBuilder reg2)
        {
            AssertValid(true);
            reg1.AssertValid(true);
            reg2.AssertValid(true);

            if (reg1._iuLast == 0)
            {
                if (reg2._iuLast == 0)
                    Set((ulong)reg1._uSmall * reg2._uSmall);
                else
                {
                    Load(ref reg2, 1);
                    Mul(reg1._uSmall);
                }
            }
            else if (reg2._iuLast == 0)
            {
                Load(ref reg1, 1);
                Mul(reg2._uSmall);
            }
            else
            {
                Contract.Assert(reg1._iuLast > 0 && reg2._iuLast > 0);
                SetSizeClear(reg1._iuLast + reg2._iuLast + 2);

                uint[] rgu1, rgu2;
                int cu1, cu2;

                // We prefer more iterations on the inner loop and fewer on the outer.
                if (reg1.CuNonZero <= reg2.CuNonZero)
                {
                    rgu1 = reg1._rgu; cu1 = reg1._iuLast + 1;
                    rgu2 = reg2._rgu; cu2 = reg2._iuLast + 1;
                }
                else
                {
                    rgu1 = reg2._rgu; cu1 = reg2._iuLast + 1;
                    rgu2 = reg1._rgu; cu2 = reg1._iuLast + 1;
                }

                for (int iu1 = 0; iu1 < cu1; iu1++)
                {
                    uint uCur = rgu1[iu1];
                    if (uCur == 0)
                        continue;

                    uint uCarry = 0;
                    int iuRes = iu1;
                    for (int iu2 = 0; iu2 < cu2; iu2++, iuRes++)
                        uCarry = AddMulCarry(ref _rgu[iuRes], uCur, rgu2[iu2], uCarry);
                    while (uCarry != 0)
                        uCarry = AddCarry(ref _rgu[iuRes++], 0, uCarry);
                }
                Trim();
            }
        }

        // Divide 'this' by uDen, leaving the quotient in 'this' and returning the remainder.
        public uint DivMod(uint uDen)
        {
            AssertValid(true);

            if (uDen == 1)
                return 0;
            if (_iuLast == 0)
            {
                uint uTmp = _uSmall;
                _uSmall = uTmp / uDen;
                return uTmp % uDen;
            }

            EnsureWritable();

            ulong uu = 0;
            for (int iv = _iuLast; iv >= 0; iv--)
            {
                uu = NumericsHelpers.MakeUlong((uint)uu, _rgu[iv]);
                _rgu[iv] = (uint)(uu / uDen);
                uu %= uDen;
            }
            Trim();
            return (uint)uu;
        }

        // Divide regNum by uDen, returning the remainder and tossing the quotient.
        public static uint Mod(ref BigIntegerBuilder regNum, uint uDen)
        {
            regNum.AssertValid(true);

            if (uDen == 1)
                return 0;
            if (regNum._iuLast == 0)
                return regNum._uSmall % uDen;

            ulong uu = 0;
            for (int iv = regNum._iuLast; iv >= 0; iv--)
            {
                uu = NumericsHelpers.MakeUlong((uint)uu, regNum._rgu[iv]);
                uu %= uDen;
            }
            return (uint)uu;
        }

        // Divide 'this' by regDen, leaving the remainder in 'this' and tossing the quotient.
        public void Mod(ref BigIntegerBuilder regDen)
        {
            AssertValid(true);
            regDen.AssertValid(true);

            if (regDen._iuLast == 0)
            {
                Set(Mod(ref this, regDen._uSmall));
                return;
            }
            if (_iuLast == 0)
                return;

            BigIntegerBuilder regTmp = new BigIntegerBuilder();
            ModDivCore(ref this, ref regDen, false, ref regTmp);
        }

        // Divide 'this' by regDen, leaving the quotient in 'this' and tossing the remainder.
        public void Div(ref BigIntegerBuilder regDen)
        {
            AssertValid(true);
            regDen.AssertValid(true);

            if (regDen._iuLast == 0)
            {
                DivMod(regDen._uSmall);
                return;
            }
            if (_iuLast == 0)
            {
                _uSmall = 0;
                return;
            }

            BigIntegerBuilder regTmp = new BigIntegerBuilder();
            ModDivCore(ref this, ref regDen, true, ref regTmp);
            NumericsHelpers.Swap(ref this, ref regTmp);
        }

        // Divide regNum by regDen, leaving the remainder in regNum and the quotient in regQuo (if fQuo is true).
        public void ModDiv(ref BigIntegerBuilder regDen, ref BigIntegerBuilder regQuo)
        {
            if (regDen._iuLast == 0)
            {
                regQuo.Set(DivMod(regDen._uSmall));
                NumericsHelpers.Swap(ref this, ref regQuo);
                return;
            }
            if (_iuLast == 0)
                return;

            ModDivCore(ref this, ref regDen, true, ref regQuo);
        }

        private static void ModDivCore(ref BigIntegerBuilder regNum, ref BigIntegerBuilder regDen, bool fQuo, ref BigIntegerBuilder regQuo)
        {
            Contract.Assert(regNum._iuLast > 0 && regDen._iuLast > 0);

            regQuo.Set(0);
            if (regNum._iuLast < regDen._iuLast)
                return;

            Contract.Assert(0 < regDen._iuLast && regDen._iuLast <= regNum._iuLast);
            int cuDen = regDen._iuLast + 1;
            int cuDiff = regNum._iuLast - regDen._iuLast;

            // Determine whether the result will have cuDiff "digits" or cuDiff+1 "digits".
            int cuQuo = cuDiff;
            for (int iu = regNum._iuLast; ; iu--)
            {
                if (iu < cuDiff)
                {
                    cuQuo++;
                    break;
                }
                if (regDen._rgu[iu - cuDiff] != regNum._rgu[iu])
                {
                    if (regDen._rgu[iu - cuDiff] < regNum._rgu[iu])
                        cuQuo++;
                    break;
                }
            }

            if (cuQuo == 0)
                return;

            if (fQuo)
                regQuo.SetSizeLazy(cuQuo);

            // Get the uint to use for the trial divisions. We normalize so the high bit is set.
            uint uDen = regDen._rgu[cuDen - 1];
            uint uDenNext = regDen._rgu[cuDen - 2];
            int cbitShiftLeft = NumericsHelpers.CbitHighZero(uDen);
            int cbitShiftRight = kcbitUint - cbitShiftLeft;
            if (cbitShiftLeft > 0)
            {
                uDen = (uDen << cbitShiftLeft) | (uDenNext >> cbitShiftRight);
                uDenNext <<= cbitShiftLeft;
                if (cuDen > 2)
                    uDenNext |= regDen._rgu[cuDen - 3] >> cbitShiftRight;
            }
            Contract.Assert((uDen & 0x80000000) != 0);

            // Allocate and initialize working space.
            Contract.Assert(cuQuo + cuDen == regNum._iuLast + 1 || cuQuo + cuDen == regNum._iuLast + 2);
            regNum.EnsureWritable();

            for (int iu = cuQuo; --iu >= 0;)
            {
                // Get the high (normalized) bits of regNum.
                uint uNumHi = (iu + cuDen <= regNum._iuLast) ? regNum._rgu[iu + cuDen] : 0;
                Contract.Assert(uNumHi <= regDen._rgu[cuDen - 1]);

                ulong uuNum = NumericsHelpers.MakeUlong(uNumHi, regNum._rgu[iu + cuDen - 1]);
                uint uNumNext = regNum._rgu[iu + cuDen - 2];
                if (cbitShiftLeft > 0)
                {
                    uuNum = (uuNum << cbitShiftLeft) | (uNumNext >> cbitShiftRight);
                    uNumNext <<= cbitShiftLeft;
                    if (iu + cuDen >= 3)
                        uNumNext |= regNum._rgu[iu + cuDen - 3] >> cbitShiftRight;
                }

                // Divide to get the quotient digit.
                ulong uuQuo = uuNum / uDen;
                ulong uuRem = (uint)(uuNum % uDen);
                Contract.Assert(uuQuo <= (ulong)uint.MaxValue + 2);
                if (uuQuo > uint.MaxValue)
                {
                    uuRem += uDen * (uuQuo - uint.MaxValue);
                    uuQuo = uint.MaxValue;
                }
                while (uuRem <= uint.MaxValue && uuQuo * uDenNext > NumericsHelpers.MakeUlong((uint)uuRem, uNumNext))
                {
                    uuQuo--;
                    uuRem += uDen;
                }

                // Multiply and subtract. Note that uuQuo may be 1 too large. If we have a borrow
                // at the end, we'll add the denominator back on and decrement uuQuo.
                if (uuQuo > 0)
                {
                    ulong uuBorrow = 0;
                    for (int iu2 = 0; iu2 < cuDen; iu2++)
                    {
                        uuBorrow += regDen._rgu[iu2] * uuQuo;
                        uint uSub = (uint)uuBorrow;
                        uuBorrow >>= kcbitUint;
                        if (regNum._rgu[iu + iu2] < uSub)
                            uuBorrow++;
                        regNum._rgu[iu + iu2] -= uSub;
                    }

                    Contract.Assert(uNumHi == uuBorrow || uNumHi == uuBorrow - 1);
                    if (uNumHi < uuBorrow)
                    {
                        // Add, tracking carry.
                        uint uCarry = 0;
                        for (int iu2 = 0; iu2 < cuDen; iu2++)
                        {
                            uCarry = AddCarry(ref regNum._rgu[iu + iu2], regDen._rgu[iu2], uCarry);
                            Contract.Assert(uCarry <= 1);
                        }
                        Contract.Assert(uCarry == 1);
                        uuQuo--;
                    }
                    regNum._iuLast = iu + cuDen - 1;
                }

                if (fQuo)
                {
                    if (cuQuo == 1)
                        regQuo._uSmall = (uint)uuQuo;
                    else
                        regQuo._rgu[iu] = (uint)uuQuo;
                }
            }

            Contract.Assert(cuDen > 1 && regNum._iuLast > 0);
            regNum._iuLast = cuDen - 1;
            regNum.Trim();
        }

        private static readonly double s_kdblLn2To32 = 32 * Math.Log(2);

        public void ShiftRight(int cbit)
        {
            AssertValid(true);
            if (cbit <= 0)
            {
                if (cbit < 0)
                    ShiftLeft(-cbit);
                return;
            }
            ShiftRight(cbit / kcbitUint, cbit % kcbitUint);
        }

        public void ShiftRight(int cuShift, int cbitShift)
        {
            Contract.Requires(cuShift >= 0);
            Contract.Requires(0 <= cbitShift);
            Contract.Assert(cbitShift < kcbitUint);
            AssertValid(true);

            if ((cuShift | cbitShift) == 0)
                return;
            if (cuShift > _iuLast)
            {
                Set(0);
                return;
            }
            if (_iuLast == 0)
            {
                _uSmall >>= cbitShift;
                AssertValid(true);
                return;
            }

            uint[] rguSrc = _rgu;
            int cuSrc = _iuLast + 1;
            _iuLast -= cuShift;
            if (_iuLast == 0)
                _uSmall = rguSrc[cuShift] >> cbitShift;
            else
            {
                Contract.Assert(_rgu.Length > _iuLast);
                if (!_fWritable)
                {
                    _rgu = new uint[_iuLast + 1];
                    _fWritable = true;
                }
                if (cbitShift > 0)
                {
                    for (int iuSrc = cuShift + 1, iuDst = 0; iuSrc < cuSrc; iuSrc++, iuDst++)
                        _rgu[iuDst] = (rguSrc[iuSrc - 1] >> cbitShift) | (rguSrc[iuSrc] << (kcbitUint - cbitShift));
                    _rgu[_iuLast] = rguSrc[cuSrc - 1] >> cbitShift;
                    Trim();
                }
                else
                    Array.Copy(rguSrc, cuShift, _rgu, 0, _iuLast + 1);
            }
            AssertValid(true);
        }

        public void ShiftLeft(int cbit)
        {
            AssertValid(true);
            if (cbit <= 0)
            {
                if (cbit < 0)
                    ShiftRight(-cbit);
                return;
            }
            ShiftLeft(cbit / kcbitUint, cbit % kcbitUint);
        }

        public void ShiftLeft(int cuShift, int cbitShift)
        {
            Contract.Requires(cuShift >= 0);
            Contract.Requires(0 <= cbitShift);
            Contract.Assert(cbitShift < kcbitUint);
            AssertValid(true);

            int iuLastNew = _iuLast + cuShift;
            uint uHigh = 0;
            if (cbitShift > 0)
            {
                uHigh = this.High >> (kcbitUint - cbitShift);
                if (uHigh != 0)
                    iuLastNew++;
            }
            if (iuLastNew == 0)
            {
                _uSmall <<= cbitShift;
                return;
            }

            uint[] rguSrc = _rgu;
            bool fClearLow = cuShift > 0;
            if (!_fWritable || _rgu.Length <= iuLastNew)
            {
                _rgu = new uint[iuLastNew + 1];
                _fWritable = true;
                fClearLow = false;
            }
            if (_iuLast == 0)
            {
                if (uHigh != 0)
                    _rgu[cuShift + 1] = uHigh;
                _rgu[cuShift] = _uSmall << cbitShift;
            }
            else if (cbitShift == 0)
                Array.Copy(rguSrc, 0, _rgu, cuShift, _iuLast + 1);
            else
            {
                int iuSrc = _iuLast;
                int iuDst = _iuLast + cuShift;
                if (iuDst < iuLastNew)
                    _rgu[iuLastNew] = uHigh;
                for (; iuSrc > 0; iuSrc--, iuDst--)
                    _rgu[iuDst] = (rguSrc[iuSrc] << cbitShift) | (rguSrc[iuSrc - 1] >> (kcbitUint - cbitShift));
                _rgu[cuShift] = rguSrc[0] << cbitShift;
            }
            _iuLast = iuLastNew;
            if (fClearLow)
                Array.Clear(_rgu, 0, cuShift);
        }

        // Get the high two uints, combined into a ulong, zero extending to
        // length cu if necessary. Asserts cu > _iuLast and _iuLast > 0.
        private ulong GetHigh2(int cu)
        {
            Contract.Requires(cu >= 2);
            Contract.Assert(_iuLast > 0);
            Contract.Assert(cu > _iuLast);

            if (cu - 1 <= _iuLast)
                return NumericsHelpers.MakeUlong(_rgu[cu - 1], _rgu[cu - 2]);
            if (cu - 2 == _iuLast)
                return _rgu[cu - 2];
            return 0;
        }

        // Apply a single carry starting at iu, extending the register
        // if needed.
        private void ApplyCarry(int iu)
        {
            Contract.Requires(0 <= iu);
            Contract.Assert(_fWritable && _iuLast > 0);
            Contract.Assert(iu <= _iuLast + 1);

            for (; ; iu++)
            {
                if (iu > _iuLast)
                {
                    if (_iuLast + 1 == _rgu.Length)
                        Array.Resize(ref _rgu, _iuLast + 2);
                    _rgu[++_iuLast] = 1;
                    break;
                }
                if (++_rgu[iu] > 0)
                    break;
            }
        }

        // Apply a single borrow starting at iu. This does NOT trim the result.
        private void ApplyBorrow(int iuMin)
        {
            Contract.Requires(0 < iuMin);
            Contract.Assert(_fWritable && _iuLast > 0);
            Contract.Assert(iuMin <= _iuLast);

            for (int iu = iuMin; iu <= _iuLast; iu++)
            {
                uint u = _rgu[iu]--;
                if (u > 0)
                    return;
            }
            // Borrowed off the end!
            Contract.Assert(false, "Invalid call to ApplyBorrow");
        }

        private static uint AddCarry(ref uint u1, uint u2, uint uCarry)
        {
            ulong uu = (ulong)u1 + u2 + uCarry;
            u1 = (uint)uu;
            return (uint)(uu >> kcbitUint);
        }

        private static uint SubBorrow(ref uint u1, uint u2, uint uBorrow)
        {
            ulong uu = (ulong)u1 - u2 - uBorrow;
            u1 = (uint)uu;
            return (uint)-(int)(uu >> kcbitUint);
        }

        private static uint SubRevBorrow(ref uint u1, uint u2, uint uBorrow)
        {
            ulong uu = (ulong)u2 - u1 - uBorrow;
            u1 = (uint)uu;
            return (uint)-(int)(uu >> kcbitUint);
        }

        private static uint MulCarry(ref uint u1, uint u2, uint uCarry)
        {
            // This is guaranteed not to overflow.
            ulong uuRes = (ulong)u1 * u2 + uCarry;
            u1 = (uint)uuRes;
            return (uint)(uuRes >> kcbitUint);
        }

        private static uint AddMulCarry(ref uint uAdd, uint uMul1, uint uMul2, uint uCarry)
        {
            // This is guaranteed not to overflow.
            ulong uuRes = (ulong)uMul1 * uMul2 + uAdd + uCarry;
            uAdd = (uint)uuRes;
            return (uint)(uuRes >> kcbitUint);
        }


        public static void GCD(ref BigIntegerBuilder reg1, ref BigIntegerBuilder reg2)
        {
            // Use Lehmer's GCD, with improvements, after eliminating common powers of 2.
            if (reg1._iuLast > 0 && reg1._rgu[0] == 0 || reg2._iuLast > 0 && reg2._rgu[0] == 0)
            {
                int cbit1 = reg1.MakeOdd();
                int cbit2 = reg2.MakeOdd();
                LehmerGcd(ref reg1, ref reg2);
                int cbitMin = Math.Min(cbit1, cbit2);
                if (cbitMin > 0)
                    reg1.ShiftLeft(cbitMin);
            }
            else
                LehmerGcd(ref reg1, ref reg2);
        }

        // This leaves the GCD in reg1 and trash in reg2.
        // This uses Lehmer's method, with test due to Jebelean / Belnkiy and Vidunas.
        // See Knuth, vol 2, page 345; Jebelean (1993) "Improving the Multiprecision Euclidean Algorithm";
        // and Belenkiy & Vidunas (1998) "A Greatest Common Divisor Algorithm".
        private static void LehmerGcd(ref BigIntegerBuilder reg1, ref BigIntegerBuilder reg2)
        {
            // This value has no real significance. Occassionally we want to subtract
            // the two registers and keep the absolute value of the difference. To do
            // so we need to pass a ref sign to Sub.
            int signTmp = +1;

            for (; ;)
            {
                reg1.AssertValid(true);
                reg2.AssertValid(true);

                int cuMax = reg1._iuLast + 1;
                int cuMin = reg2._iuLast + 1;
                if (cuMax < cuMin)
                {
                    NumericsHelpers.Swap(ref reg1, ref reg2);
                    NumericsHelpers.Swap(ref cuMax, ref cuMin);
                }
                Contract.Assert(cuMax == reg1._iuLast + 1);
                Contract.Assert(cuMin == reg2._iuLast + 1);

                if (cuMin == 1)
                {
                    if (cuMax == 1)
                        reg1._uSmall = NumericsHelpers.GCD(reg1._uSmall, reg2._uSmall);
                    else if (reg2._uSmall != 0)
                        reg1.Set(NumericsHelpers.GCD(Mod(ref reg1, reg2._uSmall), reg2._uSmall));
                    return;
                }

                if (cuMax == 2)
                {
                    reg1.Set(NumericsHelpers.GCD(reg1.GetHigh2(2), reg2.GetHigh2(2)));
                    return;
                }

                if (cuMin <= cuMax - 2)
                {
                    // reg1 is much larger than reg2, so just mod.
                    reg1.Mod(ref reg2);
                    continue;
                }

                ulong uu1 = reg1.GetHigh2(cuMax);
                ulong uu2 = reg2.GetHigh2(cuMax);
                Contract.Assert(uu1 != 0 && uu2 != 0);

                int cbit = NumericsHelpers.CbitHighZero(uu1 | uu2);
                if (cbit > 0)
                {
                    uu1 = (uu1 << cbit) | (reg1._rgu[cuMax - 3] >> (kcbitUint - cbit));
                    // Note that [cuMax - 3] is correct, NOT [cuMin - 3].
                    uu2 = (uu2 << cbit) | (reg2._rgu[cuMax - 3] >> (kcbitUint - cbit));
                }
                if (uu1 < uu2)
                {
                    NumericsHelpers.Swap(ref uu1, ref uu2);
                    NumericsHelpers.Swap(ref reg1, ref reg2);
                }

                // Make sure we don't overflow.
                if (uu1 == ulong.MaxValue || uu2 == ulong.MaxValue)
                {
                    uu1 >>= 1;
                    uu2 >>= 1;
                }
                Contract.Assert(uu1 >= uu2); // We ensured this above.
                if (uu1 == uu2)
                {
                    // The high bits are the same, so we don't know which
                    // is larger. No matter, just subtract one from the other
                    // and keep the absolute value of the result.
                    Contract.Assert(cuMax == cuMin);
                    reg1.Sub(ref signTmp, ref reg2);
                    Contract.Assert(reg1._iuLast < cuMin - 1);
                    continue;
                }
                if (NumericsHelpers.GetHi(uu2) == 0)
                {
                    // reg1 is much larger than reg2, so just mod.
                    reg1.Mod(ref reg2);
                    continue;
                }

                // These are the coefficients to apply to reg1 and reg2 to get
                // the new values, using: a * reg1 - b * reg2 and -c * reg1 + d * reg2.
                uint a = 1, b = 0;
                uint c = 0, d = 1;

                for (; ;)
                {
                    Contract.Assert(uu1 + a > a); // no overflow
                    Contract.Assert(uu2 + d > d);
                    Contract.Assert(uu1 > b);
                    Contract.Assert(uu2 > c);
                    Contract.Assert(uu2 + d <= uu1 - b);

                    uint uQuo = 1;
                    ulong uuNew = uu1 - uu2;
                    while (uuNew >= uu2 && uQuo < 32)
                    {
                        uuNew -= uu2;
                        uQuo++;
                    }
                    if (uuNew >= uu2)
                    {
                        ulong uuQuo = uu1 / uu2;
                        if (uuQuo > uint.MaxValue)
                            break;
                        uQuo = (uint)uuQuo;
                        uuNew = uu1 - uQuo * uu2;
                    }
                    ulong uuAdNew = a + (ulong)uQuo * c;
                    ulong uuBcNew = b + (ulong)uQuo * d;
                    if (uuAdNew > int.MaxValue || uuBcNew > int.MaxValue)
                        break;
                    // Jebelean / Belenkiy-Vidunas conditions
                    if (uuNew < uuBcNew || uuNew + uuAdNew > uu2 - c)
                        break;

                    Contract.Assert(uQuo == (uu1 + a - 1) / (uu2 - c));
                    Contract.Assert(uQuo == (uu1 - b) / (uu2 + d));

                    a = (uint)uuAdNew;
                    b = (uint)uuBcNew;
                    uu1 = uuNew;

                    if (uu1 <= b)
                    {
                        Contract.Assert(uu1 == b);
                        break;
                    }

                    Contract.Assert(uu1 + a > a); // no overflow
                    Contract.Assert(uu2 + d > d);
                    Contract.Assert(uu2 > c);
                    Contract.Assert(uu1 > b);
                    Contract.Assert(uu1 + a <= uu2 - c);

                    uQuo = 1;
                    uuNew = uu2 - uu1;
                    while (uuNew >= uu1 && uQuo < 32)
                    {
                        uuNew -= uu1;
                        uQuo++;
                    }
                    if (uuNew >= uu1)
                    {
                        ulong uuQuo = uu2 / uu1;
                        if (uuQuo > uint.MaxValue)
                            break;
                        uQuo = (uint)uuQuo;
                        uuNew = uu2 - uQuo * uu1;
                    }
                    uuAdNew = d + (ulong)uQuo * b;
                    uuBcNew = c + (ulong)uQuo * a;
                    if (uuAdNew > int.MaxValue || uuBcNew > int.MaxValue)
                        break;
                    // Jebelean / Belenkiy-Vidunas conditions
                    if (uuNew < uuBcNew || uuNew + uuAdNew > uu1 - b)
                        break;

                    Contract.Assert(uQuo == (uu2 + d - 1) / (uu1 - b));
                    Contract.Assert(uQuo == (uu2 - c) / (uu1 + a));

                    d = (uint)uuAdNew;
                    c = (uint)uuBcNew;
                    uu2 = uuNew;

                    if (uu2 <= c)
                    {
                        Contract.Assert(uu2 == c);
                        break;
                    }
                }

                if (b == 0)
                {
                    Contract.Assert(a == 1 && c == 0 && d == 1);
                    Contract.Assert(uu1 > uu2); // We ensured this above.
                    if (uu1 / 2 >= uu2)
                        reg1.Mod(ref reg2);
                    else
                        reg1.Sub(ref signTmp, ref reg2);
                }
                else
                {
                    // Replace reg1 with  a * reg1 - b * reg2.
                    // Replace reg2 with -c * reg1 + d * reg2.
                    // Do everything mod cuMin uint's.
                    reg1.SetSizeKeep(cuMin, 0);
                    reg2.SetSizeKeep(cuMin, 0);
                    int nCarry1 = 0;
                    int nCarry2 = 0;
                    for (int iu = 0; iu < cuMin; iu++)
                    {
                        uint u1 = reg1._rgu[iu];
                        uint u2 = reg2._rgu[iu];
                        long nn1 = (long)u1 * a - (long)u2 * b + nCarry1;
                        long nn2 = (long)u2 * d - (long)u1 * c + nCarry2;
                        nCarry1 = (int)(nn1 >> kcbitUint);
                        nCarry2 = (int)(nn2 >> kcbitUint);
                        reg1._rgu[iu] = (uint)nn1;
                        reg2._rgu[iu] = (uint)nn2;
                    }
                    reg1.Trim();
                    reg2.Trim();
                }
            }
        }

        public int CbitLowZero()
        {
            AssertValid(true);
            if (_iuLast == 0)
            {
                if ((_uSmall & 1) != 0 || _uSmall == 0)
                    return 0;
                return NumericsHelpers.CbitLowZero(_uSmall);
            }

            int iuMin = 0;
            while (_rgu[iuMin] == 0)
                iuMin++;
            int cbit = NumericsHelpers.CbitLowZero(_rgu[iuMin]);
            return cbit + iuMin * kcbitUint;
        }

        // Shift right until the number is odd. Return the number of bits
        // shifted. Asserts that the register is trimmed.
        public int MakeOdd()
        {
            AssertValid(true);
            int cbit = CbitLowZero();
            if (cbit > 0)
                ShiftRight(cbit);
            return cbit;
        }

        private static readonly byte[] s_rgbInv = new byte[128] {
      0x01, 0xAB, 0xCD, 0xB7, 0x39, 0xA3, 0xC5, 0xEF,
      0xF1, 0x1B, 0x3D, 0xA7, 0x29, 0x13, 0x35, 0xDF,
      0xE1, 0x8B, 0xAD, 0x97, 0x19, 0x83, 0xA5, 0xCF,
      0xD1, 0xFB, 0x1D, 0x87, 0x09, 0xF3, 0x15, 0xBF,
      0xC1, 0x6B, 0x8D, 0x77, 0xF9, 0x63, 0x85, 0xAF,
      0xB1, 0xDB, 0xFD, 0x67, 0xE9, 0xD3, 0xF5, 0x9F,
      0xA1, 0x4B, 0x6D, 0x57, 0xD9, 0x43, 0x65, 0x8F,
      0x91, 0xBB, 0xDD, 0x47, 0xC9, 0xB3, 0xD5, 0x7F,
      0x81, 0x2B, 0x4D, 0x37, 0xB9, 0x23, 0x45, 0x6F,
      0x71, 0x9B, 0xBD, 0x27, 0xA9, 0x93, 0xB5, 0x5F,
      0x61, 0x0B, 0x2D, 0x17, 0x99, 0x03, 0x25, 0x4F,
      0x51, 0x7B, 0x9D, 0x07, 0x89, 0x73, 0x95, 0x3F,
      0x41, 0xEB, 0x0D, 0xF7, 0x79, 0xE3, 0x05, 0x2F,
      0x31, 0x5B, 0x7D, 0xE7, 0x69, 0x53, 0x75, 0x1F,
      0x21, 0xCB, 0xED, 0xD7, 0x59, 0xC3, 0xE5, 0x0F,
      0x11, 0x3B, 0x5D, 0xC7, 0x49, 0x33, 0x55, 0xFF
    };
    }
}
