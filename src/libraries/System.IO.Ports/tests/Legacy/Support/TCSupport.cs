// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using Xunit;

namespace Legacy.Support
{
    public class TCSupport
    {
        public enum SerialPortRequirements { None, OneSerialPort, TwoSerialPorts, NullModem, Loopback, LoopbackOrNullModem };

        static public readonly int PassExitCode = 100;
        static public readonly int FailExitCode = 1;
        static public readonly int NoNullCableExitCode = 99;
        static public readonly bool SerialPortRequirements_CausesError = false;

        private int _numErrors = 0;
        private int _numTestcases = 0;
        private int _exitValue = PassExitCode;

        private static LocalMachineSerialInfo s_localMachineSerialInfo;
        private static SerialPortRequirements s_localMachineSerialPortRequirements;

        static TCSupport()
        {
            InitializeSerialInfo();
        }

        private static void InitializeSerialInfo()
        {
            GenerateSerialInfo();

            if (s_localMachineSerialInfo.LoopbackPortName != null)
                s_localMachineSerialPortRequirements = SerialPortRequirements.Loopback;
            else if (s_localMachineSerialInfo.NullModemPresent)
                s_localMachineSerialPortRequirements = SerialPortRequirements.NullModem;
            else if (s_localMachineSerialInfo.SecondAvailablePortName != null && s_localMachineSerialInfo.SecondAvailablePortName != string.Empty)
                s_localMachineSerialPortRequirements = SerialPortRequirements.TwoSerialPorts;
            else if (s_localMachineSerialInfo.FirstAvailablePortName != null && s_localMachineSerialInfo.FirstAvailablePortName != string.Empty)
                s_localMachineSerialPortRequirements = SerialPortRequirements.OneSerialPort;
            else
                s_localMachineSerialPortRequirements = SerialPortRequirements.None;
        }

        private static void GenerateSerialInfo()
        {
            string[] installedPortNames = PortHelper.GetPorts();
            Console.WriteLine("Installed ports : " + string.Join(",", installedPortNames));
            bool nullModemPresent = false;

            string portName1 = null, portName2 = null, loopbackPortName = null;

            Array.Sort(installedPortNames);

            var openablePortNames = CheckPortsCanBeOpened(installedPortNames);

            // Find the first port which is looped-back
            foreach (var portName in openablePortNames)
            {
                if (SerialPortConnection.VerifyLoopback(portName))
                {
                    loopbackPortName = portName;
                    break;
                }
            }

            // Find any pair of ports which are null-modem connected
            // If there is a pair like this, then they take precedence over any other way of identifying two available ports
            for (var firstIndex = 0; firstIndex < openablePortNames.Count && !nullModemPresent; firstIndex++)
            {
                for (var secondIndex = firstIndex+1; secondIndex < openablePortNames.Count && !nullModemPresent; secondIndex++)
                {
                    var firstPortName = openablePortNames[firstIndex];
                    var secondPortName = openablePortNames[secondIndex];

                    if (SerialPortConnection.VerifyConnection(firstPortName, secondPortName))
                    {
                        // We have a null modem port
                        portName1 = firstPortName;
                        portName2 = secondPortName;
                        nullModemPresent = true;

                        Console.WriteLine("Null-modem connection from {0} to {1}", firstPortName, secondPortName);
                    }
                }
            }

            if (!nullModemPresent)
            {
                // If we don't have a null-modem connection, we'll just use the first two ports
                portName1 = openablePortNames.FirstOrDefault();
                portName2 = openablePortNames.Skip(1).FirstOrDefault();
            }

            s_localMachineSerialInfo = new LocalMachineSerialInfo(portName1, portName2, loopbackPortName, nullModemPresent);

        }

        private static IList<string> CheckPortsCanBeOpened(IEnumerable<string> installedPortNames)
        {
            List<string> openablePortNames = new List<string>();
            foreach (string portName in installedPortNames)
            {
                using (SerialPort com = new SerialPort(portName))
                {
                    try
                    {
                        com.Open();
                        com.Close();

                        openablePortNames.Add(portName);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return openablePortNames;
        }

        public static bool SufficientHardwareRequirements(SerialPortRequirements serialPortRequirements)
        {
            switch (serialPortRequirements)
            {
                case SerialPortRequirements.None:
                    return true;

                case SerialPortRequirements.OneSerialPort:
                    return s_localMachineSerialPortRequirements == SerialPortRequirements.OneSerialPort ||
                        s_localMachineSerialPortRequirements == SerialPortRequirements.TwoSerialPorts ||
                        s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback ||
                        s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem;
                case SerialPortRequirements.TwoSerialPorts:
                    return s_localMachineSerialPortRequirements == SerialPortRequirements.TwoSerialPorts ||
                        s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem;
                case SerialPortRequirements.NullModem:
                    return s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem;
                case SerialPortRequirements.Loopback:
                    return s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback;
                case SerialPortRequirements.LoopbackOrNullModem:
                    return s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback ||
                        s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem;
            }

            return false;
        }

        public static SerialPort InitFirstSerialPort()
        {
            if (LocalMachineSerialInfo.NullModemPresent)
            {
                return new SerialPort(LocalMachineSerialInfo.FirstAvailablePortName);
            }
            else if (null != LocalMachineSerialInfo.LoopbackPortName)
            {
                return new SerialPort(LocalMachineSerialInfo.LoopbackPortName);
            }
            else if (null != LocalMachineSerialInfo.FirstAvailablePortName)
            {
                return new SerialPort(LocalMachineSerialInfo.FirstAvailablePortName);
            }

            return null;
        }

        public static SerialPort InitSecondSerialPort(SerialPort com)
        {
            if (LocalMachineSerialInfo.NullModemPresent)
            {
                return new SerialPort(LocalMachineSerialInfo.SecondAvailablePortName);
            }
            else if (null != LocalMachineSerialInfo.LoopbackPortName)
            {
                return com;
            }
            else if (null != LocalMachineSerialInfo.SecondAvailablePortName)
            {
                return new SerialPort(LocalMachineSerialInfo.SecondAvailablePortName);
            }

            return null;
        }

        public int NumErrors
        {
            get
            {
                return _numErrors;
            }
        }


        public int NumTestcases
        {
            get
            {
                return _numTestcases;
            }
        }

        public int ExitValue
        {
            get
            {
                return _exitValue;
            }
        }

        public static LocalMachineSerialInfo LocalMachineSerialInfo
        {
            get
            {
                return s_localMachineSerialInfo;
            }
        }

        public static SerialPortRequirements LocalMachineSerialPortRequirements
        {
            get
            {
                return s_localMachineSerialPortRequirements;
            }
        }

        public delegate bool Predicate();

        public delegate T ValueGenerator<T>();

        public static bool WaitForPredicate(Predicate predicate, int maxWait, string errorMessageFormat, params object[] formatArgs)
        {
            return WaitForPredicate(predicate, maxWait, string.Format(errorMessageFormat, formatArgs));
        }
        public static bool WaitForPredicate(Predicate predicate, int maxWait, string errorMessage)
        {
            Stopwatch stopWatch = new Stopwatch();
            bool predicateValue = false;

            stopWatch.Start();

            while (!predicateValue && stopWatch.ElapsedMilliseconds < maxWait)
            {
                predicateValue = predicate();
                System.Threading.Thread.Sleep(10);
            }

            if (!predicateValue)
                Debug.WriteLine(errorMessage);

            return predicateValue;
        }

        public static void WaitForExpected<T>(ValueGenerator<T> actualValueGenerator, T expectedValue, int maxWait, string errorMessage)
        {
            Stopwatch stopWatch = new Stopwatch();
            bool result = false;
            T actualValue;
            int iterationWaitTime = 0;

            stopWatch.Start();

            do
            {
                actualValue = actualValueGenerator();
                result = actualValue == null ? null == expectedValue : actualValue.Equals(expectedValue);

                System.Threading.Thread.Sleep(iterationWaitTime);
                iterationWaitTime = 10;//This is just to ensure there is no delay the first time we check
            } while (!result && stopWatch.ElapsedMilliseconds < maxWait);

            if (!result)
            {
                Assert.True(false, errorMessage +
                    " Expected:" + (null == expectedValue ? "<null>" : expectedValue.ToString()) +
                    " Actual:" + (null == actualValue ? "<null>" : actualValue.ToString()));
            }
        }

        private const int MIN_RANDOM_CHAR = 0;

        public const int MIN_HIGH_SURROGATE = 0xD800;
        public const int MAX_HIGH_SURROGATE = 0xDBFF;

        public const int MIN_LOW_SURROGATE = 0xDC00;
        public const int MAX_LOW_SURROGATE = 0xDFFF;

        public const int MIN_RANDOM_ASCII_CHAR = 0;
        public const int MAX_RANDOM_ASCII_CHAR = 127;

        private static Random s_random = new Random(-55);

        public enum CharacterOptions { None, Surrogates, ASCII };

        public static char[] GetRandomChars(int count, bool withSurrogates)
        {
            if (withSurrogates)
                return GetRandomCharsWithSurrogates(count);
            else
                return GetRandomCharsWithoutSurrogates(count);
        }

        public static char[] GetRandomChars(int count, CharacterOptions options)
        {
            if (0 != (options & CharacterOptions.Surrogates))
                return GetRandomCharsWithSurrogates(count);
            if (0 != (options & CharacterOptions.ASCII))
                return GetRandomASCIIChars(count);
            else
                return GetRandomCharsWithoutSurrogates(count);
        }

        public static void GetRandomChars(char[] chars, int index, int count, CharacterOptions options)
        {
            if (0 != (options & CharacterOptions.Surrogates))
                GetRandomCharsWithSurrogates(chars, index, count);
            if (0 != (options & CharacterOptions.ASCII))
                GetRandomASCIIChars(chars, index, count);
            else
                GetRandomCharsWithoutSurrogates(chars, index, count);
        }

        public static string GetRandomString(int count, CharacterOptions options)
        {
            return new string(GetRandomChars(count, options));
        }

        public static System.Text.StringBuilder GetRandomStringBuilder(int count, CharacterOptions options)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(count);
            sb.Append(GetRandomChars(count, options));

            return sb;
        }

        public static char[] GetRandomASCIIChars(int count)
        {
            char[] chars = new char[count];

            GetRandomASCIIChars(chars, 0, count);

            return chars;
        }

        public static void GetRandomASCIIChars(char[] chars, int index, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                chars[i] = GenerateRandomASCII();
            }
        }

        public static char[] GetRandomCharsWithoutSurrogates(int count)
        {
            char[] chars = new char[count];

            GetRandomCharsWithoutSurrogates(chars, 0, count);

            return chars;
        }

        public static void GetRandomCharsWithoutSurrogates(char[] chars, int index, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                chars[i] = GenerateRandomCharNonSurrogate();
            }
        }

        public static char[] GetRandomCharsWithSurrogates(int count)
        {
            char[] chars = new char[count];

            GetRandomCharsWithSurrogates(chars, 0, count);

            return chars;
        }

        public static void GetRandomCharsWithSurrogates(char[] chars, int index, int count)
        {
            int randomChar;

            for (int i = 0; i < count; ++i)
            {
                randomChar = GenerateRandomCharWithHighSurrogate();

                if (MIN_HIGH_SURROGATE <= randomChar)
                {
                    if (i < (count - 1))
                    {
                        chars[i] = (char)randomChar;

                        ++i;
                        chars[i] = GenerateRandomLowSurrogate();
                    }
                    else
                    {
                        chars[i] = GenerateRandomCharNonSurrogate();
                    }
                }
                else
                {
                    chars[i] = (char)randomChar;
                }
            }
        }

        public static char GenerateRandomASCII()
        {
            return (char)s_random.Next(MIN_RANDOM_ASCII_CHAR, MAX_RANDOM_ASCII_CHAR + 1);
        }

        public static char GenerateRandomHighSurrogate()
        {
            return (char)s_random.Next(MIN_HIGH_SURROGATE, MAX_HIGH_SURROGATE + 1);
        }

        public static char GenerateRandomLowSurrogate()
        {
            return (char)s_random.Next(MIN_LOW_SURROGATE, MAX_LOW_SURROGATE + 1);
        }

        public static char GenerateRandomCharWithHighSurrogate()
        {
            return (char)s_random.Next(MIN_RANDOM_CHAR, MAX_HIGH_SURROGATE + 1);
        }

        public static char GenerateRandomCharNonSurrogate()
        {
            return (char)s_random.Next(MIN_RANDOM_CHAR, MIN_HIGH_SURROGATE);
        }

        public static byte[] GetRandomBytes(int count)
        {
            byte[] bytes = new byte[count];

            GetRandomBytes(bytes, 0, count);

            return bytes;
        }

        /// <summary>
        /// Returns a random char that is not c
        /// </summary>
        public static char GetRandomOtherChar(char c, CharacterOptions options)
        {
            switch (options)
            {
                case CharacterOptions.ASCII:
                    return GetRandomOtherASCIIChar(c);
                default:
                    return GetRandomOtherUnicodeChar(c);
            }
        }

        public static char GetRandomOtherUnicodeChar(char c)
        {
            char newChar;

            do
            {
                newChar = GenerateRandomCharNonSurrogate();
            } while (newChar == c);

            return newChar;
        }

        public static char GetRandomOtherASCIIChar(char c)
        {
            char newChar;

            do
            {
                newChar = GenerateRandomASCII();
            } while (newChar == c);

            return newChar;
        }

        public static void GetRandomBytes(byte[] bytes, int index, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                bytes[i] = (byte)s_random.Next(0, 256);
            }
        }

        public static bool IsSurrogate(char c)
        {
            return IsHighSurrogate(c) || IsLowSurrogate(c);
        }

        public static bool IsHighSurrogate(char c)
        {
            return MIN_HIGH_SURROGATE <= c && c <= MAX_HIGH_SURROGATE;
        }

        public static bool IsLowSurrogate(char c)
        {
            return MIN_LOW_SURROGATE <= c && c <= MAX_LOW_SURROGATE;
        }

        public static int OrdinalIndexOf(string input, string search)
        {
            return OrdinalIndexOf(input, 0, input.Length, search);
        }

        public static int OrdinalIndexOf(string input, int startIndex, string search)
        {
            return OrdinalIndexOf(input, startIndex, input.Length - startIndex, search);
        }

        public static int OrdinalIndexOf(string input, int startIndex, int count, string search)
        {
            int lastSearchIndex = (count + startIndex) - search.Length;

            System.Diagnostics.Debug.Assert(lastSearchIndex < input.Length, "Searching will result in accessing element past the end of the array");

            for (int i = startIndex; i <= lastSearchIndex; ++i)
            {
                if (input[i] == search[0])
                {
                    bool match = true;
                    for (int searchIndex = 1, inputIndex = i + 1; searchIndex < search.Length; ++searchIndex, ++inputIndex)
                    {
                        match = input[inputIndex] == search[searchIndex];

                        if (!match)
                        {
                            break;
                        }
                    }

                    if (match)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static void PrintChars(char[] chars)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                Debug.WriteLine("(char){0}, //Char={1}, {0:X}", (int)chars[i], chars[i]);
            }
        }

        public static void PrintBytes(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                Debug.WriteLine("{0}, //{0:X} Index: {1}", (int)bytes[i], i);
            }
        }

        /// <summary>
        /// Verifies the contents of the array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expectedArray">The expected items in the array.</param>
        /// <param name="actualArray">The actual array.</param>
        /// <returns>true if expectedArray and actualArray have the same contents.</returns>
        public static void VerifyArray<T>(T[] expectedArray, T[] actualArray)
        {
            Assert.Equal(expectedArray.Length, actualArray.Length);
            VerifyArray(expectedArray, actualArray, 0, expectedArray.Length);
        }

        /// <summary>
        /// Verifies the contents of the array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expectedArray">The expected items in the array.</param>
        /// <param name="actualArray">The actual array.</param>
        /// <param name="index">The index to start verifying the items at.</param>
        /// <param name="length">The number of item to verify</param>
        /// <returns>true if expectedArray and actualArray have the same contents.</returns>
        public static void VerifyArray<T>(T[] expectedArray, T[] actualArray, int index, int length)
        {
            bool result;
            int tempLength;

            tempLength = length + index;
            for (int i = index; i < tempLength; ++i)
            {
                result = expectedArray[i] == null ? null != actualArray[i] : expectedArray[i].Equals(actualArray[i]);

                if (!result)
                {
                    Assert.True(false, string.Format("Err_55808aoped Items differ at {0} expected {1} actual {2}", i, expectedArray[i], actualArray[i]));
                }
            }
        }
    }
}
