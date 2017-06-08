// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
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
            myCalc = new Stack<System.Numerics.BigInteger>();
            snCalc = new Stack<System.Numerics.BigInteger>();

            string delimStr = " ";
            char[] delimiter = delimStr.ToCharArray();
            input = _input.Split(delimiter);

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

            if (op.StartsWith("u"))
            {
                checkValues = true;

                snnum1 = snCalc.Pop();
                snCalc.Push(DoUnaryOperatorSN(snnum1, op));

                mynum1 = myCalc.Pop();
                myCalc.Push(MyBigIntImp.DoUnaryOperatorMine(mynum1, op));

                ret = true;
            }
            else if (op.StartsWith("b"))
            {
                checkValues = true;

                snnum1 = snCalc.Pop();
                snnum2 = snCalc.Pop();
                snCalc.Push(DoBinaryOperatorSN(snnum1, snnum2, op, out _snOut));

                mynum1 = myCalc.Pop();
                mynum2 = myCalc.Pop();
                myCalc.Push(MyBigIntImp.DoBinaryOperatorMine(mynum1, mynum2, op, out _myOut));

                ret = true;
            }
            else if (op.StartsWith("t"))
            {
                checkValues = true;
                snnum1 = snCalc.Pop();
                snnum2 = snCalc.Pop();
                snnum3 = snCalc.Pop();

                snCalc.Push(DoTertanaryOperatorSN(snnum1, snnum2, snnum3, op));

                mynum1 = myCalc.Pop();
                mynum2 = myCalc.Pop();
                mynum3 = myCalc.Pop();
                myCalc.Push(MyBigIntImp.DoTertanaryOperatorMine(mynum1, mynum2, mynum3, op));

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
            BigInteger ret = new BigInteger(0);
            string op = operators.Dequeue();

            while (String.CompareOrdinal(op, "endmake") != 0)
            {
                bytes.Add(byte.Parse(op));
                op = operators.Dequeue();
            }

            return new BigInteger(bytes.ToArray());
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
                    return MyBigIntImp.ApproximateBigInteger(BigInteger.Log10(num1));
                case "uLog":
                    return MyBigIntImp.ApproximateBigInteger(BigInteger.Log(num1));
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
                case "uMultiply":
                    return BigInteger.Multiply(num1, num1);
                case "u*":
                    return num1 * num1;
                default:
                    throw new ArgumentException(String.Format("Invalid operation found: {0}", op));
            }
        }

        private BigInteger DoBinaryOperatorSN(BigInteger num1, BigInteger num2, string op)
        {
            BigInteger num3;

            return DoBinaryOperatorSN(num1, num2, op, out num3);
        }

        private BigInteger DoBinaryOperatorSN(BigInteger num1, BigInteger num2, string op, out BigInteger num3)
        {
            num3 = 0;
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
                    return MyBigIntImp.ApproximateBigInteger(BigInteger.Log(num1, (double)num2));
                case "bGCD":
                    return BigInteger.GreatestCommonDivisor(num1, num2);
                case "bPow":
                    int arg2 = (int)num2;
                    return BigInteger.Pow(num1, arg2);
                case "bDivRem":
                    return BigInteger.DivRem(num1, num2, out num3);
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
                    throw new ArgumentException(String.Format("Invalid operation found: {0}", op));
            }
        }

        private BigInteger DoTertanaryOperatorSN(BigInteger num1, BigInteger num2, BigInteger num3, string op)
        {
            switch (op)
            {
                case "tModPow":
                    return BigInteger.ModPow(num1, num2, num3);
                default:
                    throw new ArgumentException(String.Format("Invalid operation found: {0}", op));
            }
        }

        public void VerifyOutParameter()
        {
            Assert.Equal(_snOut, _myOut);

            _snOut = 0;
            _myOut = 0;
        }

        private static String Print(byte[] bytes)
        {
           return MyBigIntImp.PrintFormatX(bytes);
        }
    }
}
