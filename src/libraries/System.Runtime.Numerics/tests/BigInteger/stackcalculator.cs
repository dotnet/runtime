// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Xunit;

namespace Tools
{
    public class StackCalc
    {
        public string[] input;
        public Stack<BigInteger> myCalc;
        public Stack<BigInteger> snCalc;
        public Queue<string> operators;
        private BigInteger _snOut = 0;
        private BigInteger _myOut = 0;

        public StackCalc(string _input)
        {
#if Verbose
            Console.WriteLine("StackCalculator is {0}", _input);
#endif
            myCalc = new Stack<System.Numerics.BigInteger>();
            snCalc = new Stack<System.Numerics.BigInteger>();
            string delimStr = " ";
            char[] delimiter = delimStr.ToCharArray();
            input = _input.Split(delimiter);

#if Verbose
            Console.WriteLine("StackCalculatorInputs are");
            int i = 0;
            foreach (var s in input)
            {
                Console.WriteLine("\t Input {0} is :{1}", i++, s);
            }
#endif
            operators = new Queue<string>(input);
        }

        public bool DoNextOperation()
        {
            string op = "";
            bool ret = false;
            bool checkValues = false;
            BigInteger snnum1 = 0;
            BigInteger snnum2 = 0;
            BigInteger snnum3 = 0;
            BigInteger mynum1 = 0;
            BigInteger mynum2 = 0;
            BigInteger mynum3 = 0;

            if (operators.Count == 0)
            {
                return false;
            }
            op = operators.Dequeue();
#if Verbose
            Console.WriteLine("\t ***** DoNextOperation {0}", op);
#endif
            if (op.StartsWith("u"))
            {
                checkValues = true;

                snnum1 = snCalc.Pop();
                snCalc.Push(DoUnaryOperatorSN(snnum1, op));

                mynum1 = myCalc.Pop();
                myCalc.Push(DoUnaryOperatorMine(mynum1, op));
                ret = true;
            }
            else if (op.StartsWith("b"))
            {
                checkValues = true;

                snnum1 = snCalc.Pop();
                snnum2 = snCalc.Pop();
                snCalc.Push(DoBinaryOperatorSN(snnum1, snnum2, op));

                mynum1 = myCalc.Pop();
                mynum2 = myCalc.Pop();
                myCalc.Push(DoBinaryOperatorMine(mynum1, mynum2, op));
                ret = true;
            }
            else if (op.StartsWith("t"))
            {
                checkValues = true;
#if Verbose
                Console.WriteLine("\t ***** sncalc length is {0}:", snCalc.Count);
#endif
                snnum1 = snCalc.Pop();

#if Verbose
                Console.WriteLine("\t sncalc element 1 is: {0}", snnum1);
#endif
                snnum2 = snCalc.Pop();

#if Verbose
                Console.WriteLine("\t sncalc element 2 is: {0}", snnum2);
#endif
                snnum3 = snCalc.Pop();

#if Verbose
                Console.WriteLine("\t sncalc element 3 is: {0}", snnum3);
#endif

                snCalc.Push(DoTertanaryOperatorSN(snnum1, snnum2, snnum3, op));

                mynum1 = myCalc.Pop();
                mynum2 = myCalc.Pop();
                mynum3 = myCalc.Pop();
                myCalc.Push(DoTertanaryOperatorMine(mynum1, mynum2, mynum3, op));

                ret = true;
            }
            else
            {
                if (op.Equals("make"))
                {
                    snnum1 = DoConstruction();
                    snCalc.Push(snnum1);
                    myCalc.Push(snnum1);
                }
                else if (op.Equals("Corruption"))
                {
                    snCalc.Push(-33);
                    myCalc.Push(-555);
                }
                else if (BigInteger.TryParse(op, out snnum1))
                {
                    snCalc.Push(snnum1);
                    myCalc.Push(snnum1);
                }
                else
                {
                    Console.WriteLine("Failed to parse string {0}", op);
                }

                ret = true;
            }

            if (checkValues)
            {
                if ((snnum1 != mynum1) || (snnum2 != mynum2) || (snnum3 != mynum3))
                {
                    operators.Enqueue("Corruption");
                }
            }

            return ret;
        }

        private BigInteger DoConstruction()
        {
            List<byte> bytes = new List<byte>();
            string op = "";
            BigInteger ret = new BigInteger(0);
            op = operators.Dequeue();

            while (String.CompareOrdinal(op, "endmake") != 0)
            {
                bytes.Add(byte.Parse(op));
                op = operators.Dequeue();
            }

            try
            {
                ret = new BigInteger(bytes.ToArray());
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine(Print(bytes.ToArray()));
                throw;
            }

            return ret;
        }

        private BigInteger DoUnaryOperatorSN(BigInteger num1, string op)
        {
            switch (op)
            {
                case "uSign":
                    return new BigInteger(num1.Sign);
                case "u~":
                    return (~(num1));
                case "uLog10":
                    return ApproximateBigInteger(BigInteger.Log10(num1));
                case "uLog":
                    return ApproximateBigInteger(BigInteger.Log(num1));
                case "uAbs":
                    return BigInteger.Abs(num1);
                case "uNegate":
                    return BigInteger.Negate(num1);
                case "u--":
                    return (--(num1));
                case "u++":
                    return (++(num1));
                case "u-":
                    return (-(num1));
                case "u+":
                    return (+(num1));
                default:
                    Console.WriteLine("Invalid operation found: {0}", op);
                    break;
            }
            return new BigInteger();
        }
        private BigInteger DoBinaryOperatorSN(BigInteger num1, BigInteger num2, string op)
        {
            switch (op)
            {
                case "bMin":
                    return BigInteger.Min(num1, num2);
                case "bMax":
                    return BigInteger.Max(num1, num2);
                case "b>>":
                    return num1 >> (int)num2;
                case "b<<":
                    return num1 << (int)num2;
                case "b^":
                    return num1 ^ num2;
                case "b|":
                    return num1 | num2;
                case "b&":
                    return num1 & num2;
                case "b%":
                    return num1 % num2;
                case "b/":
                    return num1 / num2;
                case "b*":
                    return num1 * num2;
                case "b-":
                    return num1 - num2;
                case "b+":
                    return num1 + num2;
                case "bLog":
                    return ApproximateBigInteger(BigInteger.Log(num1, (double)num2));
                case "bGCD":
                    return BigInteger.GreatestCommonDivisor(num1, num2);
                case "bPow":
                    int arg2 = (int)num2;
                    return BigInteger.Pow(num1, arg2);
                case "bDivRem":
                    BigInteger num3;
                    BigInteger ret = BigInteger.DivRem(num1, num2, out num3);
                    SetSNOutCheck(num3);
                    return ret;
                case "bRemainder":
                    return BigInteger.Remainder(num1, num2);
                case "bDivide":
                    return BigInteger.Divide(num1, num2);
                case "bMultiply":
                    return BigInteger.Multiply(num1, num2);
                case "bSubtract":
                    return BigInteger.Subtract(num1, num2);
                case "bAdd":
                    return BigInteger.Add(num1, num2);
                default:
                    Console.WriteLine("Invalid operation found: {0}", op);
                    break;
            }
            return new BigInteger();
        }
        private BigInteger DoTertanaryOperatorSN(BigInteger num1, BigInteger num2, BigInteger num3, string op)
        {
#if Verbose
            Console.WriteLine("DoTertanaryOperatorSN inputs are: {0} {1} {2}", num1, num2, num3);
#endif
            switch (op)
            {
                case "tModPow":
                    return BigInteger.ModPow(num1, num2, num3);
                default:
                    Console.WriteLine("Invalid operation found: {0}", op);
                    break;
            }
            return new BigInteger();
        }

        private BigInteger DoUnaryOperatorMine(BigInteger num1, string op)
        {
            List<byte> bytes1 = new List<byte>(num1.ToByteArray());
            int factor;
            double result;

            switch (op)
            {
                case "uSign":
                    if (IsZero(bytes1)) return new BigInteger(0);
                    if (IsZero(Max(bytes1, new List<byte>(new byte[] { 0 })))) return new BigInteger(-1);
                    return new BigInteger(1);
                case "u~":
                    return new BigInteger(Not(bytes1).ToArray());
                case "uLog10":
                    factor = (int)BigInteger.Log(num1, 10);
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
                    factor = (int)BigInteger.Log(num1, 10);
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
                    if ((bytes1[bytes1.Count - 1] & 0x80) != 0) bytes1 = Negate(bytes1);
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
                default:
                    Console.WriteLine("Invalid operation found: {0}", op);
                    break;
            }
            return new BigInteger();
        }
        private BigInteger DoBinaryOperatorMine(BigInteger num1, BigInteger num2, string op)
        {
            List<byte> bytes1 = new List<byte>(num1.ToByteArray());
            List<byte> bytes2 = new List<byte>(num2.ToByteArray());

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
                    SetMYOutCheck(new BigInteger(Remainder(bytes1, bytes2).ToArray()));
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
                    Console.WriteLine("Invalid operation found: {0}", op);
                    break;
            }
            return new BigInteger();
        }
        private BigInteger DoTertanaryOperatorMine(BigInteger num1, BigInteger num2, BigInteger num3, string op)
        {
            List<byte> bytes1 = new List<byte>(num1.ToByteArray());
            List<byte> bytes2 = new List<byte>(num2.ToByteArray());
            List<byte> bytes3 = new List<byte>(num3.ToByteArray());

            switch (op)
            {
                case "tModPow":
                    return new BigInteger(ModPow(bytes1, bytes2, bytes3).ToArray());
                default:
                    Console.WriteLine("Invalid operation found: {0}", op);
                    break;
            }
            return new BigInteger();
        }

        private void SetSNOutCheck(BigInteger value)
        {
            _snOut = value;
        }
        private void SetMYOutCheck(BigInteger value)
        {
            _myOut = value;
        }
        public bool VerifyOut()
        {
            bool ret = (_snOut == _myOut);

            _snOut = 0;
            _myOut = 0;

            return ret;
        }

        private static List<byte> Add(List<byte> bytes1, List<byte> bytes2)
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

                if (carry) temp++;
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
                if (num1neg) extender = 0xff;
                bnew.Add(extender);
            }

            return bnew;
        }
        private static List<byte> Negate(List<byte> bytes)
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
                if (carry) temp++;
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
        private static List<byte> Multiply(List<byte> bytes1, List<byte> bytes2)
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
        private static List<byte> Divide(List<byte> bytes1, List<byte> bytes2)
        {
            bool numPos = ((bytes1[bytes1.Count - 1] & 0x80) == 0);
            bool denPos = ((bytes2[bytes2.Count - 1] & 0x80) == 0);

            if (!numPos) bytes1 = Negate(bytes1);
            if (!denPos) bytes2 = Negate(bytes2);

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
            if (shift < 0) return new List<byte>(new byte[] { (byte)0 });
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
        private static List<byte> Remainder(List<byte> bytes1, List<byte> bytes2)
        {
            bool numPos = ((bytes1[bytes1.Count - 1] & 0x80) == 0);
            bool denPos = ((bytes2[bytes2.Count - 1] & 0x80) == 0);

            if (!numPos) bytes1 = Negate(bytes1);
            if (!denPos) bytes2 = Negate(bytes2);

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
        private static List<byte> Pow(List<byte> bytes1, List<byte> bytes2)
        {
            if (IsZero(bytes2)) return new List<byte>(new byte[] { 1 });

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
        private static List<byte> ModPow(List<byte> bytes1, List<byte> bytes2, List<byte> bytes3)
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
        private static List<byte> GCD(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> temp;

            bool numPos = ((bytes1[bytes1.Count - 1] & 0x80) == 0);
            bool denPos = ((bytes2[bytes2.Count - 1] & 0x80) == 0);

            if (!numPos) bytes1 = Negate(bytes1);
            if (!denPos) bytes2 = Negate(bytes2);

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
        private static List<byte> Max(List<byte> bytes1, List<byte> bytes2)
        {
            bool b1Pos = ((bytes1[bytes1.Count - 1] & 0x80) == 0);
            bool b2Pos = ((bytes2[bytes2.Count - 1] & 0x80) == 0);

            if (b1Pos != b2Pos)
            {
                if (b1Pos) return bytes1;
                if (b2Pos) return bytes2;
            }

            List<byte> sum = Add(bytes1, Negate(Copy(bytes2)));

            if ((sum[sum.Count - 1] & 0x80) != 0)
            {
                return bytes2;
            }

            return bytes1;
        }
        private static List<byte> And(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> bnew = new List<byte>();
            NormalizeLengths(bytes1, bytes2);

            for (int i = 0; i < bytes1.Count; i++)
            {
                bnew.Add((byte)(bytes1[i] & bytes2[i]));
            }

            return bnew;
        }
        private static List<byte> Or(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> bnew = new List<byte>();
            NormalizeLengths(bytes1, bytes2);

            for (int i = 0; i < bytes1.Count; i++)
            {
                bnew.Add((byte)(bytes1[i] | bytes2[i]));
            }

            return bnew;
        }
        private static List<byte> Xor(List<byte> bytes1, List<byte> bytes2)
        {
            List<byte> bnew = new List<byte>();
            NormalizeLengths(bytes1, bytes2);

            for (int i = 0; i < bytes1.Count; i++)
            {
                bnew.Add((byte)(bytes1[i] ^ bytes2[i]));
            }

            return bnew;
        }
        private static List<byte> Not(List<byte> bytes)
        {
            List<byte> bnew = new List<byte>();

            for (int i = 0; i < bytes.Count; i++)
            {
                bnew.Add((byte)(bytes[i] ^ 0xFF));
            }

            return bnew;
        }
        private static List<byte> ShiftLeft(List<byte> bytes1, List<byte> bytes2)
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
        private static List<byte> ShiftLeftGrow(List<byte> bytes)
        {
            List<byte> bresult = new List<byte>();

            for (int i = 0; i < bytes.Count; i++)
            {
                byte newbyte = bytes[i];

                if (newbyte > 127) newbyte -= 128;
                newbyte = (byte)(newbyte * 2);
                if ((i != 0) && (bytes[i - 1] >= 128)) newbyte++;

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
        private static List<byte> ShiftLeftDrop(List<byte> bytes)
        {
            List<byte> bresult = new List<byte>();

            for (int i = 0; i < bytes.Count; i++)
            {
                byte newbyte = bytes[i];

                if (newbyte > 127) newbyte -= 128;
                newbyte = (byte)(newbyte * 2);
                if ((i != 0) && (bytes[i - 1] >= 128)) newbyte++;

                bresult.Add(newbyte);
            }

            return bresult;
        }
        private static List<byte> ShiftRight(List<byte> bytes)
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
        private static List<byte> SetLength(List<byte> bytes, int size)
        {
            List<byte> bresult = new List<byte>();

            for (int i = 0; i < size; i++)
            {
                bresult.Add(bytes[i]);
            }

            return bresult;
        }
        private static List<byte> Copy(List<byte> bytes)
        {
            List<byte> ret = new List<byte>();
            for (int i = 0; i < bytes.Count; i++)
            {
                ret.Add(bytes[i]);
            }
            return ret;
        }
        private static BigInteger ApproximateBigInteger(double value)
        {
            //Special case values;
            if (Double.IsNaN(value)) return new BigInteger(-101);
            if (Double.IsNegativeInfinity(value)) return new BigInteger(-102);
            if (Double.IsPositiveInfinity(value)) return new BigInteger(-103);

            BigInteger result = new BigInteger(value);

            if (result != 0)
            {
                bool pos = (value > 0);
                if (!pos) value = -value;

                int size = (int)Math.Floor(Math.Log10(value));

                //keep only the first 17 significant digits;
                if (size > 17)
                {
                    result = result - (result % BigInteger.Pow(10, size - 17));
                }

                if (!pos) value = -value;
            }

            return result;
        }
        private static void NormalizeLengths(List<byte> bytes1, List<byte> bytes2)
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
        private static void Trim(List<byte> bytes)
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
        private static List<byte> GetBytes(BitArray ba)
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
        private static String Print(byte[] bytes)
        {
            string ret = String.Empty;
            for (int i = 0; i < bytes.Length; i++)
            {
                ret += bytes[i].ToString("x");
            }
            return ret;
        }
        private static bool IsZero(List<byte> list)
        {
            byte[] value = list.ToArray();
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) return false;
            }
            return true;
        }
    }
}
