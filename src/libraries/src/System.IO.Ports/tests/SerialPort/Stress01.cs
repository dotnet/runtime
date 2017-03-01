// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class Write_char_int_int_stress01 : PortsTest
    {
        private const int RECEIVE_BUFFER_SIZE = 4096;
        private const int TRANSMIT_BUFFER_SIZE = 4096;
        private const int MAX_BUFFER_SIZE = 4096;

        private static readonly TimeSpan s_testDuration = TCSupport.RunShortStressTests ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(20);

        [ConditionalFact(nameof(HasNullModem))]
        public void WriteChars()
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                char[] xmitCharBuffer = TCSupport.GetRandomChars(TRANSMIT_BUFFER_SIZE, TCSupport.CharacterOptions.None);
                char[] rcvCharBuffer = new char[RECEIVE_BUFFER_SIZE];
                Random random = new Random(-55);
                Stopwatch sw = new Stopwatch();
                Buffer<char> buffer = new Buffer<char>(MAX_BUFFER_SIZE);

                com1.Encoding = Encoding.Unicode;
                com2.Encoding = Encoding.Unicode;

                com1.BaudRate = 115200;
                com2.BaudRate = 115200;

                com1.Open();

                if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                sw.Start();
                while (sw.ElapsedMilliseconds < s_testDuration.TotalMilliseconds)
                {
                    switch (random.Next(0, 2))
                    {
                        case 0: //Write
                            if (com2.BytesToRead < MAX_BUFFER_SIZE)
                            {
                                int maxNumberOfCharactes = (MAX_BUFFER_SIZE - com2.BytesToRead) / 2;
                                int numberOfCharacters = random.Next(0,
                                    Math.Min(xmitCharBuffer.Length, maxNumberOfCharactes) + 1);
                                int expectedBytesToRead = com2.BytesToRead + 2 * numberOfCharacters;

                                //						Debug.WriteLine("Writing {0,5} characters BytesToRead={1,5}", numberOfCharacters, com2.BytesToRead);
                                com1.Write(xmitCharBuffer, 0, numberOfCharacters);
                                buffer.Append(xmitCharBuffer, 0, numberOfCharacters);

                                TCSupport.WaitForPredicate(delegate () { return com2.BytesToRead == expectedBytesToRead; },
                                    60000,
                                    "Err_29829haie Expected to received {0} bytes actual={1}", expectedBytesToRead,
                                    com2.BytesToRead);
                            }
                            break;
                        case 1: //Read
                            if (0 < com2.BytesToRead)
                            {
                                int maxNumberOfCharactes = com2.BytesToRead / 2;
                                int numberOfCharacters = random.Next(0,
                                    Math.Min(rcvCharBuffer.Length, maxNumberOfCharactes) + 1);
                                int actualNumberOfCharactersRead;
                                int expectedBytesToRead = com2.BytesToRead - (2 * numberOfCharacters);

                                //						Debug.WriteLine("Reading {0,5} characters BytesToRead={1,5}", numberOfCharacters, com2.BytesToRead);
                                actualNumberOfCharactersRead = com2.Read(rcvCharBuffer, 0, numberOfCharacters);

                                if (actualNumberOfCharactersRead == numberOfCharacters)
                                {
                                    buffer.CompareAndRemove(rcvCharBuffer, 0, numberOfCharacters);

                                    if (com2.BytesToRead != expectedBytesToRead)
                                    {
                                        Fail("Err_895879uhedbuz Expected to BytesToRead={0} actual={1}",
                                            expectedBytesToRead, com2.BytesToRead);
                                    }
                                }
                                else
                                {
                                    Fail("Err_895879uhedbuz Expected to read {0} chars actual {1}",
                                        numberOfCharacters, actualNumberOfCharactersRead);
                                }
                            }
                            break;
                    }
                }
            }
        }
    }

    public class Buffer<T>
    {
        private readonly Queue<T> _queue;
        private readonly EqualityComparer<T> _comparer;

        public Buffer()
        {
            _queue = new Queue<T>();
            _comparer = EqualityComparer<T>.Default;
        }

        public Buffer(int capacity)
        {
            _queue = new Queue<T>(capacity);
            _comparer = EqualityComparer<T>.Default;
        }

        public void Append(T[] data, int index, int count)
        {
            count += index;

            for (; index < count; ++index)
            {
                _queue.Enqueue(data[index]);
            }
        }

        public void Remove(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                _queue.Dequeue();
            }
        }

        public void Compare(T[] data, int index, int count)
        {
            IEnumerator<T> enumerator = _queue.GetEnumerator();

            count += index;

            while (enumerator.MoveNext() && index < count)
            {
                if (!_comparer.Equals(enumerator.Current, data[index]))
                {
                    PortsTest.Fail("Err_84264lked Expected {0} actual {1}", data[index], enumerator.Current);
                }

                ++index;
            }

            if (index != count)
            {
                PortsTest.Fail("Err_5587456jdivmeo Expected to iterate through {0} items actual {1}", count, index);
            }
        }

        public void CompareAndRemove(T[] data, int index, int count)
        {
            count += index;

            while (0 < _queue.Count && index < count)
            {
                T currentItem = _queue.Dequeue();

                if (!_comparer.Equals(currentItem, data[index]))
                {
                    PortsTest.Fail("Err_84264lked Expected {0} actual {1}", data[index], currentItem);
                }

                ++index;
            }

            if (index != count)
            {
                PortsTest.Fail("Err_5587456jdivmeo Expected to iterate through {0} items actual {1}", count, index);
            }
        }
    }
}