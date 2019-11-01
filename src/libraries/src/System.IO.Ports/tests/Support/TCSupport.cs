// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Legacy.Support
{
    public static class TCSupport
    {
        public enum SerialPortRequirements { None, OneSerialPort, TwoSerialPorts, NullModem, Loopback, LoopbackOrNullModem };

        private static LocalMachineSerialInfo s_localMachineSerialInfo;
        private static SerialPortRequirements s_localMachineSerialPortRequirements;

        // Set this true to display port info to the console rather than Debug.WriteLine
        private static bool s_displayPortInfoOnConsole = true;

        static TCSupport()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeSerialInfo();
        }

        private static void InitializeSerialInfo()
        {
            if (PlatformDetection.IsWindowsNanoServer)
            {
                s_localMachineSerialPortRequirements = SerialPortRequirements.None;
                return;
            }

            GenerateSerialInfo();

            if (s_localMachineSerialInfo.LoopbackPortName != null)
                s_localMachineSerialPortRequirements = SerialPortRequirements.Loopback;
            else if (s_localMachineSerialInfo.NullModemPresent)
                s_localMachineSerialPortRequirements = SerialPortRequirements.NullModem;
            else if (!string.IsNullOrEmpty(s_localMachineSerialInfo.SecondAvailablePortName))
                s_localMachineSerialPortRequirements = SerialPortRequirements.TwoSerialPorts;
            else if (!string.IsNullOrEmpty(s_localMachineSerialInfo.FirstAvailablePortName))
                s_localMachineSerialPortRequirements = SerialPortRequirements.OneSerialPort;
            else
                s_localMachineSerialPortRequirements = SerialPortRequirements.None;
        }

        private static void GenerateSerialInfo()
        {
            string[] installedPortNames = PortHelper.GetPorts();
            bool nullModemPresent = false;
            string portName1 = null, portName2 = null, loopbackPortName = null;

            Array.Sort(installedPortNames);
            PrintInfo("Installed ports : " + string.Join(",", installedPortNames));

            IList<string> openablePortNames = CheckPortsCanBeOpened(installedPortNames);

            PrintInfo("Openable ports  : " + string.Join(",", openablePortNames));

            // Find any pair of ports which are null-modem connected
            // If there is a pair like this, then they take precedence over any other way of identifying two available ports
            for (int firstIndex = 0; firstIndex < openablePortNames.Count && !nullModemPresent; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < openablePortNames.Count && !nullModemPresent; secondIndex++)
                {
                    string firstPortName = openablePortNames[firstIndex];
                    string secondPortName = openablePortNames[secondIndex];

                    if (SerialPortConnection.VerifyConnection(firstPortName, secondPortName))
                    {
                        // We have a null modem port
                        portName1 = firstPortName;
                        portName2 = secondPortName;
                        nullModemPresent = true;

                        PrintInfo("Null-modem connection from {0} to {1}", firstPortName, secondPortName);
                    }
                }
            }

            if (!nullModemPresent)
            {
                // If we don't have a null-modem connection - check for a loopback connection
                foreach (string portName in openablePortNames)
                {
                    if (SerialPortConnection.VerifyLoopback(portName))
                    {
                        portName1 = loopbackPortName = portName;
                        break;
                    }
                }

                if (portName1 == null)
                {
                    portName1 = openablePortNames.FirstOrDefault();
                }

                portName2 = openablePortNames.FirstOrDefault(name => name != portName1);
            }

            // See Github issues #15961, #16033, #20764 - hardware tests are currently insufficiently stable on master CI
            if (loopbackPortName == null && !nullModemPresent)
            {
                // We don't have any supporting hardware - disable all the tests which would use just an open port
                PrintInfo("No support hardware - not using serial ports");
                portName1 = portName2 = null;
            }

            PrintInfo("First available port name  : " + portName1);
            PrintInfo("Second available port name : " + portName2);
            PrintInfo("Loopback port name         : " + loopbackPortName);
            PrintInfo("NullModem present          : " + nullModemPresent);

            s_localMachineSerialInfo = new LocalMachineSerialInfo(portName1, portName2, loopbackPortName, nullModemPresent);

            if (portName1 != null)
            {
                // Measure how big a packet we need to write to be sure to see blocking behaviour at a port
                try
                {
                    s_flowControlCapabilities = SerialPortConnection.MeasureFlowControlCapabilities(portName1);
                }
                catch (Exception e)
                {
                    PrintInfo(e.ToString());
                }

                PrintInfo("{0}: Flow capabilities {1}", portName1, s_flowControlCapabilities);
            }
        }

        private static void PrintInfo(string format, params object[] args)
        {
            if (s_displayPortInfoOnConsole)
            {
                Console.WriteLine(format, args);
            }
            else
            {
                Debug.WriteLine(format, args);
            }
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
                    catch (UnauthorizedAccessException) { }
                    catch (Exception e)
                    {
                        PrintInfo("Exception opening port {0}: {1}", portName, e);
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

        public static LocalMachineSerialInfo LocalMachineSerialInfo => s_localMachineSerialInfo;

        /// <summary>
        /// Set this true to shorten the very long-running stress tests
        /// </summary>
        public static bool RunShortStressTests { get; set; } = true;

        public static int MinimumBlockingByteCount => s_flowControlCapabilities.MinimumBlockingByteCount;
        public static int HardwareTransmitBufferSize => s_flowControlCapabilities.HardwareTransmitBufferSize;
        public static bool HardwareWriteBlockingAvailable => s_flowControlCapabilities.HardwareWriteBlockingAvailable;

        public delegate bool Predicate();

        public delegate T ValueGenerator<T>();

        public static void WaitForPredicate(Predicate predicate, int maxWait, string errorMessageFormat, params object[] formatArgs)
        {
            WaitForPredicate(predicate, maxWait, string.Format(errorMessageFormat, formatArgs));
        }
        public static void WaitForPredicate(Predicate predicate, int maxWait, string errorMessage)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            bool predicateValue = false;

            while (!predicateValue && stopWatch.ElapsedMilliseconds < maxWait)
            {
                predicateValue = predicate();
                Thread.Sleep(10);
            }

            Assert.True(predicateValue, errorMessage);
        }

        public static void WaitForExpected<T>(ValueGenerator<T> actualValueGenerator, T expectedValue, int maxWait,
            string errorMessage)
        {
            Stopwatch stopWatch = new Stopwatch();
            bool result;
            T actualValue;
            int iterationWaitTime = 0;

            stopWatch.Start();

            do
            {
                actualValue = actualValueGenerator();
                result = actualValue == null ? null == expectedValue : actualValue.Equals(expectedValue);

                Thread.Sleep(iterationWaitTime);
                iterationWaitTime = 10; //This is just to ensure there is no delay the first time we check
            } while (!result && stopWatch.ElapsedMilliseconds < maxWait);

            Assert.True(result, errorMessage +
                                " Expected:" + (null == expectedValue ? "<null>" : expectedValue.ToString()) +
                                " Actual:" + (null == actualValue ? "<null>" : actualValue.ToString()));
        }

        private const int MIN_RANDOM_CHAR = 0;

        private const int MIN_HIGH_SURROGATE = 0xD800;
        private const int MAX_HIGH_SURROGATE = 0xDBFF;

        private const int MIN_LOW_SURROGATE = 0xDC00;
        private const int MAX_LOW_SURROGATE = 0xDFFF;

        private const int MIN_RANDOM_ASCII_CHAR = 0;
        private const int MAX_RANDOM_ASCII_CHAR = 127;

        private static readonly Random s_random = new Random(-55);
        private static FlowControlCapabilities s_flowControlCapabilities = new FlowControlCapabilities(0, 0, false);

        [Flags]
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

        public static StringBuilder GetRandomStringBuilder(int count, CharacterOptions options)
        {
            StringBuilder sb = new StringBuilder(count);
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
            for (int i = 0; i < count; ++i)
            {
                int randomChar = GenerateRandomCharWithHighSurrogate();

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
                bytes[i + index] = (byte)s_random.Next(0, 256);
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

        private static int OrdinalIndexOf(string input, int startIndex, int count, string search)
        {
            int lastSearchIndex = (count + startIndex) - search.Length;
            if (lastSearchIndex >= input.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(input), "Searching will result in accessing element past the end of the array");
            }

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
            foreach (char chr in chars)
            {
                Debug.WriteLine("(char){0}, //Char={1}, {0:X}", (int)chr, chr);
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
            int tempLength = length + index;
            for (int i = index; i < tempLength; ++i)
            {
                bool result = expectedArray[i] == null ? null != actualArray[i] : expectedArray[i].Equals(actualArray[i]);

                Assert.True(result, string.Format("Err_55808aoped Items differ at {0} expected {1} actual {2}", i, expectedArray[i], actualArray[i]));
            }
        }

        /// <summary>
        /// Set both ports to 115200 baud to speed test performance
        /// </summary>
        public static void SetHighSpeed(SerialPort com1, SerialPort com2)
        {
            if (com1 != null)
            {
                com1.BaudRate = 115200;
            }
            if (com2 != null && com2 != com1)
            {
                com2.BaudRate = 115200;
            }
        }


        /// <summary>
        /// Wait for write data to be written into a blocked (by adverse flow control) port
        /// </summary>
        public static void WaitForWriteBufferToLoad(SerialPort com, int bufferLength)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (com.BytesToWrite + HardwareTransmitBufferSize < bufferLength)
            {
                Thread.Sleep(50);
                Assert.True(sw.ElapsedMilliseconds < 3000, $"Timeout while waiting for data to be written to port (wrote {bufferLength}, queued {com.BytesToWrite}, bufSize {HardwareTransmitBufferSize})");
            }
        }

        /// <summary>
        /// Wait for write data to be written into a blocked (by adverse flow control) port,
        /// then check that exactly the expected quantity is present
        /// </summary>
        public static void WaitForExactWriteBufferLoad(SerialPort com, int bufferLength)
        {
            WaitForWriteBufferToLoad(com, bufferLength);
            Assert.Equal(bufferLength, com.BytesToWrite + HardwareTransmitBufferSize);
        }

        /// <summary>
        /// Wait for the data to arrive into the read buffer
        /// </summary>
        public static void WaitForReadBufferToLoad(SerialPort com, int bufferLength)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (com.BytesToRead < bufferLength)
            {
                Thread.Sleep(50);
                Assert.True(sw.ElapsedMilliseconds < 3000, $"Timeout while waiting for data to be arrive at port (expected {bufferLength}, available {com.BytesToRead})");
            }
        }

        public static void WaitForTaskToStart(Task task)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (task.Status < TaskStatus.Running)
            {
                // Wait for the thread to start
                Thread.Sleep(50);
                Assert.True(sw.ElapsedMilliseconds < 2000, "Timeout waiting for task to start");
            }
        }

        public static void WaitForTaskCompletion(Task task)
        {
            Assert.True(task.Wait(5000), "Timeout waiting for task completion");
        }
    }
}
