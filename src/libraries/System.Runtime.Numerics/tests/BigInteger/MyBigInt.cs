// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Numerics.Tests
{
    public static class MyBigIntImp
    {
        public static BigInteger DoUnaryOperatorMine(BigInteger num1, string op)
        {
            List<byte> bytes1 = new List<byte>(num1.ToByteArray());
            int factor;
            double result;

            switch (op)
            {
                case "uSign":
                    if (IsZero(bytes1))
                    {
                        return new BigInteger(0);
                    }
                    if (IsZero(Max(bytes1, new List<byte>(new byte[] { 0 }))))
                    {
                        return new BigInteger(-1);
                    }
                    return new BigInteger(1);
                case "u~":
                    return new BigInteger(Not(bytes1).ToArray());
                case "uLog10":
                    factor = unchecked((int)BigInteger.Log(num1, 10));
                    if (factor > 100)
                    {
                        for (int i = 0; i < factor - 100; i++)
                        {
                            num1 = num1 / 10;
                        }
                    }
                    result = Math.Log10((double)num1);
                    if (factor > 100)
                    {
                        for (int i = 0; i < factor - 100; i++)
                        {
                            result = result + 1;
                        }
                    }
                    return ApproximateBigInteger(result);
                case "uLog":
                    factor = unchecked((int)BigInteger.Log(num1, 10));
                    if (factor > 100)
                    {
                        for (int i = 0; i < factor - 100; i++)
                        {
                            num1 = num1 / 10;
                        }
                    }
                    result = Math.Log((double)num1);
                    if (factor > 100)
                    {
                        for (int i = 0; i < factor - 100; i++)
                        {
                            result = result + Math.Log(10);
                        }
                    }
                    return ApproximateBigInteger(result);
                case "uAbs":
                    if ((bytes1[bytes1.Count - 1] & 0x80) != 0)
                    {
                        bytes1 = Negate(bytes1);
                    }
                    return new BigInteger(bytes1.ToArray());
                case "u--":
                    return new BigInteger(Add(bytes1, new List<byte>(new byte[] { 0xff })).ToArray());
                case "u++":
                    return new BigInteger(Add(bytes1, new List<byte>(new byte[] { 1 })).ToArray());
                case "uNegate":
                case "u-":
                    return new BigInteger(Negate(bytes1).ToArray());
                case "u+":
                    return num1;
                case "uMultiply":
                case "u*":
                    return new BigInteger(Multiply(bytes1, bytes1).ToArray());
                default:
                    throw new ArgumentException(String.Format("Invalid operation found: {0}", op));
            }
        }

        public static BigInteger DoBinaryOperatorMine(BigInteger num1, BigInteger num2, string op)
        {
            BigInteger num3;

            return DoBinaryOperatorMine(num1, num2, op, out num3);
        }

        public static BigInteger DoBinaryOperatorMine(BigInteger num1, BigInteger num2, string op, out BigInteger num3)
        {
            List<byte> bytes1 = new List<byte>(num1.ToByteArray());
            List<byte> bytes2 = new List<byte>(num2.ToByteArray());

            num3 = 0;
            switch (op)
            {
                case "bMin":
                    return new BigInteger(Negate(Max(Negate(bytes1), Negate(bytes2))).ToArray());
                case "bMax":
                    return new BigInteger(Max(bytes1, bytes2).ToArray());
                case "b>>":
                    return new BigInteger(ShiftLeft(bytes1, Negate(bytes2)).ToArray());
                case "b<<":
                    return new BigInteger(ShiftLeft(bytes1, bytes2).ToArray());
                case "b^":
                    return new BigInteger(Xor(bytes1, bytes2).ToArray());
                case "b|":
                    return new BigInteger(Or(bytes1, bytes2).ToArray());
                case "b&":
                    return new BigInteger(And(bytes1, bytes2).ToArray());
                case "bLog":
                    return ApproximateBigInteger(Math.Log((double)num1, (double)num2));
                case "bGCD":
                    return new BigInteger(GCD(bytes1, bytes2).ToArray());
                case "bPow":
                    int arg2 = (int)num2;
                    bytes2 = new List<byte>(new BigInteger(arg2).ToByteArray());
                    return new BigInteger(Pow(bytes1, bytes2).ToArray());
                case "bDivRem":
                    BigInteger ret = new BigInteger(Divide(bytes1, bytes2).ToArray());
                    bytes1 = new List<byte>(num1.ToByteArray());
                    bytes2 = new List<byte>(num2.ToByteArray());
                    num3 = new BigInteger(Remainder(bytes1, bytes2).ToArray());
                    return ret;
                case "bRemainder":
                case "b%":
                    return new BigInteger(Remainder(bytes1, bytes2).ToArray());
                case "bDivide":
                case "b/":
                    return new BigInteger(Divide(bytes1, bytes2).ToArray());
                case "bMultiply":
                case "b*":
                    return new BigInteger(Multiply(bytes1, bytes2).ToArray());
                case "bSubtract":
                case "b-":
                    bytes2 = Negate(bytes2);
                    goto case "bAdd";
                case "bAdd":
                case "b+":
                    return new BigInteger(Add(bytes1, bytes2).ToArray());
                default:
                    throw new ArgumentException(String.Format("Invalid operation found: {0}", op));
            }
        }

        public static BigInteger DoTertanaryOperatorMine(BigInteger num1, BigInteger num2, BigInteger num3, string op)
        {
            List<byte> bytes1 = new List<byte>(num1.ToByteArray());
            List<byte> bytes2 = new List<byte>(num2.ToByteArray());
            List<byte> bytes3 = new List<byte>(num3.ToByteArray());

            switch (op)
            {
                case "tModPow":
                    return new BigInteger(ModPow(bytes1, bytes2, bytes3).ToArray());

                default:
                    throw new ArgumentException(String.Format("Invalid operation found: {0}", op));
            }
        }

        public static List<byte> Add(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> bnew = new List<byte>();
            bool num1neg = (bytes1[bytes1.Count - 1] & 0x80) != 0;
            bool num2neg = (bytes2[bytes2.Count - 1] & 0x80) != 0;
            byte extender = 0;
            bool bnewneg;
            bool carry;

            NormalizeLengths(bytes1, bytes2);

            carry = false;
            for (int i = 0; i < bytes1.Count; i++)
            {
                int temp = bytes1[i] + bytes2[i];

                if (carry)
                {
                    temp++;
                }
                carry = false;

                if (temp > byte.MaxValue)
                {
                    temp -= byte.MaxValue + 1;
                    carry = true;
                }

                bnew.Add((byte)temp);
            }
            bnewneg = (bnew[bnew.Count - 1] & 0x80) != 0;

            if ((num1neg == num2neg) & (num1neg != bnewneg))
            {
                if (num1neg)
                {
                    extender = 0xff;
                }
                bnew.Add(extender);
            }

            return bnew;
        }

        public static List<byte> Negate(List<byte> bytes)
        {
            bool carry;
            List<byte> bnew = new List<byte>();
            bool bsame;

            for (int i = 0; i < bytes.Count; i++)
            {
                bytes[i] ^= 0xFF;
            }
            carry = false;
            for (int i = 0; i < bytes.Count; i++)
            {
                int temp = (i == 0 ? 0x01 : 0x00) + bytes[i];
                if (carry)
                {
                    temp++;
                }
                carry = false;

                if (temp > byte.MaxValue)
                {
                    temp -= byte.MaxValue + 1;
                    carry = true;
                }

                bnew.Add((byte)temp);
            }

            bsame = ((bnew[bnew.Count - 1] & 0x80) != 0);
            bsame &= ((bnew[bnew.Count - 1] & 0x7f) == 0);
            for (int i = bnew.Count - 2; i >= 0; i--)
            {
                bsame &= (bnew[i] == 0);
            }
            if (bsame)
            {
                bnew.Add((byte)0);
            }

            return bnew;
        }

        public static List<byte> Multiply(List<byte> bytes1, List<byte> bytes2)
        {
            NormalizeLengths(bytes1, bytes2);
            List<byte> bresult = new List<byte>();

            for (int i = 0; i < bytes1.Count; i++)
            {
                bresult.Add((byte)0x00);
                bresult.Add((byte)0x00);
            }

            NormalizeLengths(bytes2, bresult);
            NormalizeLengths(bytes1, bresult);
            BitArray ba2 = new BitArray(bytes2.ToArray());
            for (int i = ba2.Length - 1; i >= 0; i--)
            {
                if (ba2[i])
                {
                    bresult = Add(bytes1, bresult);
                }

                if (i != 0)
                {
                    bresult = ShiftLeftDrop(bresult);
                }
            }
            bresult = SetLength(bresult, bytes2.Count);

            return bresult;
        }

        public static List<byte> Divide(List<byte> bytes1, List<byte> bytes2)
        {
            bool numPos = ((bytes1[bytes1.Count - 1] & 0x80) == 0);
            bool denPos = ((bytes2[bytes2.Count - 1] & 0x80) == 0);

            if (!numPos)
            {
                bytes1 = Negate(bytes1);
            }
            if (!denPos)
            {
                bytes2 = Negate(bytes2);
            }

            bool qPos = (numPos == denPos);

            Trim(bytes1);
            Trim(bytes2);

            BitArray ba1 = new BitArray(bytes1.ToArray());
            BitArray ba2 = new BitArray(bytes2.ToArray());

            int ba11loc = 0;
            for (int i = ba1.Length - 1; i >= 0; i--)
            {
                if (ba1[i])
                {
                    ba11loc = i;
                    break;
                }
            }
            int ba21loc = 0;
            for (int i = ba2.Length - 1; i >= 0; i--)
            {
                if (ba2[i])
                {
                    ba21loc = i;
                    break;
                }
            }
            int shift = ba11loc - ba21loc;
            if (shift < 0) 
            {
                return new List<byte>(new byte[] { (byte)0 });
            }
            BitArray br = new BitArray(shift + 1, false);

            for (int i = 0; i < shift; i++)
            {
                bytes2 = ShiftLeftGrow(bytes2);
            }

            while (shift >= 0)
            {
                bytes2 = Negate(bytes2);
                bytes1 = Add(bytes1, bytes2);
                bytes2 = Negate(bytes2);
                if (bytes1[bytes1.Count - 1] < 128)
                {
                    br[shift] = true;
                }
                else
                {
                    bytes1 = Add(bytes1, bytes2);
                }
                bytes2 = ShiftRight(bytes2);
                shift--;
            }
            List<byte> result = GetBytes(br);

            if (!qPos)
            {
                result = Negate(result);
            }

            return result;
        }

        public static List<byte> Remainder(List<byte> bytes1, List<byte> bytes2)
        {
            bool numPos = ((bytes1[bytes1.Count - 1] & 0x80) == 0);
            bool denPos = ((bytes2[bytes2.Count - 1] & 0x80) == 0);

            if (!numPos)
            {
                bytes1 = Negate(bytes1); 
            }
            if (!denPos)
            {
                bytes2 = Negate(bytes2);
            }

            Trim(bytes1);
            Trim(bytes2);

            BitArray ba1 = new BitArray(bytes1.ToArray());
            BitArray ba2 = new BitArray(bytes2.ToArray());

            int ba11loc = 0;
            for (int i = ba1.Length - 1; i >= 0; i--)
            {
                if (ba1[i])
                {
                    ba11loc = i;
                    break;
                }
            }
            int ba21loc = 0;
            for (int i = ba2.Length - 1; i >= 0; i--)
            {
                if (ba2[i])
                {
                    ba21loc = i;
                    break;
                }
            }
            int shift = ba11loc - ba21loc;
            if (shift < 0)
            {
                if (!numPos)
                {
                    bytes1 = Negate(bytes1);
                }
                return bytes1;
            }
            BitArray br = new BitArray(shift + 1, false);

            for (int i = 0; i < shift; i++)
            {
                bytes2 = ShiftLeftGrow(bytes2);
            }

            while (shift >= 0)
            {
                bytes2 = Negate(bytes2);
                bytes1 = Add(bytes1, bytes2);
                bytes2 = Negate(bytes2);
                if (bytes1[bytes1.Count - 1] < 128)
                {
                    br[shift] = true;
                }
                else
                {
                    bytes1 = Add(bytes1, bytes2);
                }
                bytes2 = ShiftRight(bytes2);
                shift--;
            }

            if (!numPos)
            {
                bytes1 = Negate(bytes1);
            }
            return bytes1;
        }

        public static List<byte> Pow(List<byte> bytes1, List<byte> bytes2)
        {
            if (IsZero(bytes2))
            {
                return new List<byte>(new byte[] { 1 });
            }

            BitArray ba2 = new BitArray(bytes2.ToArray());
            int last1 = 0;
            List<byte> result = null;

            for (int i = ba2.Length - 1; i >= 0; i--)
            {
                if (ba2[i])
                {
                    last1 = i;
                    break;
                }
            }

            for (int i = 0; i <= last1; i++)
            {
                if (ba2[i])
                {
                    if (result == null)
                    {
                        result = bytes1;
                    }
                    else
                    {
                        result = Multiply(result, bytes1);
                    }
                    Trim(bytes1);
                    Trim(result);
                }
                if (i != last1)
                {
                    bytes1 = Multiply(bytes1, bytes1);
                    Trim(bytes1);
                }
            }
            return (result == null) ? new List<byte>(new byte[] { 1 }) : result;
        }

        public static List<byte> ModPow(List<byte> bytes1, List<byte> bytes2, List<byte> bytes3)
        {
            if (IsZero(bytes2))
            {
                return Remainder(new List<byte>(new byte[] { 1 }), bytes3);
            }

            BitArray ba2 = new BitArray(bytes2.ToArray());
            int last1 = 0;
            List<byte> result = null;

            for (int i = ba2.Length - 1; i >= 0; i--)
            {
                if (ba2[i])
                {
                    last1 = i;
                    break;
                }
            }

            bytes1 = Remainder(bytes1, Copy(bytes3));
            for (int i = 0; i <= last1; i++)
            {
                if (ba2[i])
                {
                    if (result == null)
                    {
                        result = bytes1;
                    }
                    else
                    {
                        result = Multiply(result, bytes1);
                        result = Remainder(result, Copy(bytes3));
                    }
                    Trim(bytes1);
                    Trim(result);
                }
                if (i != last1)
                {
                    bytes1 = Multiply(bytes1, bytes1);
                    bytes1 = Remainder(bytes1, Copy(bytes3));
                    Trim(bytes1);
                }
            }
            return (result == null) ? Remainder(new List<byte>(new byte[] { 1 }), bytes3) : result;
        }

        public static List<byte> GCD(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> temp;

            bool numPos = ((bytes1[bytes1.Count - 1] & 0x80) == 0);
            bool denPos = ((bytes2[bytes2.Count - 1] & 0x80) == 0);

            if (!numPos)
            {
                bytes1 = Negate(bytes1);
            }
            if (!denPos)
            {
                bytes2 = Negate(bytes2);
            }

            Trim(bytes1);
            Trim(bytes2);

            while (!IsZero(bytes2))
            {
                temp = Copy(bytes2);
                bytes2 = Remainder(bytes1, bytes2);
                bytes1 = temp;
            }
            return bytes1;
        }

        public static List<byte> Max(List<byte> bytes1, List<byte> bytes2)
        {
            bool b1Pos = ((bytes1[bytes1.Count - 1] & 0x80) == 0);
            bool b2Pos = ((bytes2[bytes2.Count - 1] & 0x80) == 0);

            if (b1Pos != b2Pos)
            {
                if (b1Pos)
                {
                    return bytes1;
                }
                if (b2Pos)
                {
                    return bytes2;
                }
            }

            List<byte> sum = Add(bytes1, Negate(Copy(bytes2)));

            if ((sum[sum.Count - 1] & 0x80) != 0)
            {
                return bytes2;
            }

            return bytes1;
        }

        public static List<byte> And(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> bnew = new List<byte>();
            NormalizeLengths(bytes1, bytes2);

            for (int i = 0; i < bytes1.Count; i++)
            {
                bnew.Add((byte)(bytes1[i] & bytes2[i]));
            }

            return bnew;
        }

        public static List<byte> Or(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> bnew = new List<byte>();
            NormalizeLengths(bytes1, bytes2);

            for (int i = 0; i < bytes1.Count; i++)
            {
                bnew.Add((byte)(bytes1[i] | bytes2[i]));
            }

            return bnew;
        }

        public static List<byte> Xor(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> bnew = new List<byte>();
            NormalizeLengths(bytes1, bytes2);

            for (int i = 0; i < bytes1.Count; i++)
            {
                bnew.Add((byte)(bytes1[i] ^ bytes2[i]));
            }
            return bnew;
        }

        public static List<byte> Not(List<byte> bytes)
        {
            List<byte> bnew = new List<byte>();

            for (int i = 0; i < bytes.Count; i++)
            {
                bnew.Add((byte)(bytes[i] ^ 0xFF));
            }

            return bnew;
        }

        public static List<byte> ShiftLeft(List<byte> bytes1, List<byte> bytes2)
        {
            int byteShift = (int)new BigInteger(Divide(Copy(bytes2), new List<byte>(new byte[] { 8 })).ToArray());
            sbyte bitShift = (sbyte)new BigInteger(Remainder(bytes2, new List<byte>(new byte[] { 8 })).ToArray());

            for (int i = 0; i < Math.Abs(bitShift); i++)
            {
                if (bitShift < 0)
                {
                    bytes1 = ShiftRight(bytes1);
                }
                else
                {
                    bytes1 = ShiftLeftGrow(bytes1);
                }
            }

            if (byteShift < 0)
            {
                byteShift = -byteShift;
                if (byteShift >= bytes1.Count)
                {
                    if ((bytes1[bytes1.Count - 1] & 0x80) != 0)
                    {
                        bytes1 = new List<byte>(new byte[] { 0xFF });
                    }
                    else
                    {
                        bytes1 = new List<byte>(new byte[] { 0 });
                    }
                }
                else
                {
                    List<byte> temp = new List<byte>();
                    for (int i = byteShift; i < bytes1.Count; i++)
                    {
                        temp.Add(bytes1[i]);
                    }
                    bytes1 = temp;
                }
            }
            else
            {
                List<byte> temp = new List<byte>();
                for (int i = 0; i < byteShift; i++)
                {
                    temp.Add((byte)0);
                }
                for (int i = 0; i < bytes1.Count; i++)
                {
                    temp.Add(bytes1[i]);
                }
                bytes1 = temp;
            }

            return bytes1;
        }

        public static List<byte> ShiftLeftGrow(List<byte> bytes)
        {
            List<byte> bresult = new List<byte>();

            for (int i = 0; i < bytes.Count; i++)
            {
                byte newbyte = bytes[i];

                if (newbyte > 127) 
                {
                    newbyte -= 128; 
                }
                newbyte = (byte)(newbyte * 2);
                if ((i != 0) && (bytes[i - 1] >= 128))
                {
                    newbyte++;
                }

                bresult.Add(newbyte);
            }
            if ((bytes[bytes.Count - 1] > 63) && (bytes[bytes.Count - 1] < 128))
            {
                bresult.Add((byte)0);
            }
            if ((bytes[bytes.Count - 1] > 127) && (bytes[bytes.Count - 1] < 192))
            {
                bresult.Add((byte)0xFF);
            }

            return bresult;
        }

        public static List<byte> ShiftLeftDrop(List<byte> bytes)
        {
            List<byte> bresult = new List<byte>();

            for (int i = 0; i < bytes.Count; i++)
            {
                byte newbyte = bytes[i];

                if (newbyte > 127)
                {
                    newbyte -= 128;
                }
                newbyte = (byte)(newbyte * 2);
                if ((i != 0) && (bytes[i - 1] >= 128))
                {
                    newbyte++;
                }

                bresult.Add(newbyte);
            }

            return bresult;
        }

        public static List<byte> ShiftRight(List<byte> bytes)
        {
            List<byte> bresult = new List<byte>();

            for (int i = 0; i < bytes.Count; i++)
            {
                byte newbyte = bytes[i];

                newbyte = (byte)(newbyte / 2);
                if ((i != (bytes.Count - 1)) && ((bytes[i + 1] & 0x01) == 1))
                {
                    newbyte += 128;
                }
                if ((i == (bytes.Count - 1)) && ((bytes[bytes.Count - 1] & 0x80) != 0))
                {
                    newbyte += 128;
                }
                bresult.Add(newbyte);
            }

            return bresult;
        }

        public static List<byte> SetLength(List<byte> bytes, int size)
        {
            List<byte> bresult = new List<byte>();

            for (int i = 0; i < size; i++)
            {
                bresult.Add(bytes[i]);
            }

            return bresult;
        }

        public static List<byte> Copy(List<byte> bytes)
        {
            List<byte> ret = new List<byte>();
            for (int i = 0; i < bytes.Count; i++)
            {
                ret.Add(bytes[i]);
            }
            return ret;
        }

        public static void NormalizeLengths(List<byte> bytes1, List<byte> bytes2)
        {
            bool num1neg = (bytes1[bytes1.Count - 1] & 0x80) != 0;
            bool num2neg = (bytes2[bytes2.Count - 1] & 0x80) != 0;
            byte extender = 0;

            if (bytes1.Count < bytes2.Count)
            {
                if (num1neg)
                {
                    extender = 0xff;
                }
                while (bytes1.Count < bytes2.Count)
                {
                    bytes1.Add(extender);
                }
            }
            if (bytes2.Count < bytes1.Count)
            {
                if (num2neg)
                {
                    extender = 0xff;
                }
                while (bytes2.Count < bytes1.Count)
                {
                    bytes2.Add(extender);
                }
            }
        }

        public static void Trim(List<byte> bytes)
        {
            while (bytes.Count > 1)
            {
                if ((bytes[bytes.Count - 1] & 0x80) == 0)
                {
                    if ((bytes[bytes.Count - 1] == 0) & ((bytes[bytes.Count - 2] & 0x80) == 0))
                    {
                        bytes.RemoveAt(bytes.Count - 1);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if ((bytes[bytes.Count - 1] == 0xFF) & ((bytes[bytes.Count - 2] & 0x80) != 0))
                    {
                        bytes.RemoveAt(bytes.Count - 1);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public static List<byte> GetBytes(BitArray ba)
        {
            int length = ((ba.Length) / 8) + 1;

            List<byte> mask = new List<byte>(new byte[] { 0 });

            for (int i = length - 1; i >= 0; i--)
            {
                for (int j = 7; j >= 0; j--)
                {
                    mask = ShiftLeftGrow(mask);
                    if ((8 * i + j < ba.Length) && (ba[8 * i + j]))
                    {
                        mask[0] |= (byte)1;
                    }
                }
            }

            return mask;
        }

        public static String Print(byte[] bytes)
        {
            String ret = "make ";

            for (int i = 0; i < bytes.Length; i++)
            {
                ret += bytes[i] + " ";
            }

            ret += "endmake ";
            return ret;
        }
        
        public static String PrintFormatX(byte[] bytes)
        {
            string ret = String.Empty;
            for (int i = 0; i < bytes.Length; i++)
            {
                ret += bytes[i].ToString("x");
            }
            return ret;
        }

        public static String PrintFormatX2(byte[] bytes)
        {
            string ret = String.Empty;
            for (int i = 0; i < bytes.Length; i++)
            {
                ret += bytes[i].ToString("x2") + " ";
            }
            return ret;
        }

        public static bool IsZero(List<byte> list)
        {
            return IsZero(list.ToArray());
        }

        public static bool IsZero(byte[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }
        
        public static byte[] GetNonZeroRandomByteArray(Random random, int size)
        {
            byte[] value = new byte[size];
            while (IsZero(value))
            {
                random.NextBytes(value);
            }
            return value;
        }
        
        public static byte[] GetRandomByteArray(Random random, int size)
        {
            byte[] value = new byte[size];
            random.NextBytes(value);
            return value;
        }
        
        public static BigInteger ApproximateBigInteger(double value)
        {
            //Special case values;
            if (Double.IsNaN(value))
            {
                return new BigInteger(-101);
            }
            if (Double.IsNegativeInfinity(value))
            {
                return new BigInteger(-102);
            }
            if (Double.IsPositiveInfinity(value))
            {
                return new BigInteger(-103);
            }

            BigInteger result = new BigInteger(Math.Round(value, 0));

            if (result != 0)
            {
                bool pos = (value > 0);
                if (!pos)
                {
                    value = -value;
                }

                int size = (int)Math.Floor(Math.Log10(value));

                //keep only the first 17 significant digits;
                if (size > 17)
                {
                    result = result - (result % BigInteger.Pow(10, size - 17));
                }

                if (!pos)
                {
                    value = -value;
                }
            }

            return result;
        }
    }
}
