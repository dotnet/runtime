// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public abstract class ConvertTestBase<TOutput>
    {
        /// <summary>
        /// Verify that the provided convert delegate produces expectedValues given testValues.
        /// </summary>
        protected void Verify<TInput>(Func<TInput, TOutput> convert, TInput[] testValues, TOutput[] expectedValues)
        {
            Assert.Equal(expectedValues.Length, testValues.Length);

            for (int i = 0; i < testValues.Length; i++)
            {
                TOutput result = convert(testValues[i]);
                Assert.Equal(expectedValues[i], result);

                if (testValues[i] is IConvertible convertible)
                {
                    Assert.Equal(expectedValues[i], convertible.ToType(typeof(TOutput), null));

                    switch (expectedValues[i])
                    {
                        case bool expected:
                            Assert.Equal(expected, convertible.ToBoolean(null));
                            break;
                        case char expected:
                            Assert.Equal(expected, convertible.ToChar(null));
                            break;
                        case sbyte expected:
                            Assert.Equal(expected, convertible.ToSByte(null));
                            break;
                        case byte expected:
                            Assert.Equal(expected, convertible.ToByte(null));
                            break;
                        case short expected:
                            Assert.Equal(expected, convertible.ToInt16(null));
                            break;
                        case ushort expected:
                            Assert.Equal(expected, convertible.ToUInt16(null));
                            break;
                        case int expected:
                            Assert.Equal(expected, convertible.ToInt32(null));
                            break;
                        case uint expected:
                            Assert.Equal(expected, convertible.ToUInt32(null));
                            break;
                        case long expected:
                            Assert.Equal(expected, convertible.ToInt64(null));
                            break;
                        case ulong expected:
                            Assert.Equal(expected, convertible.ToUInt64(null));
                            break;
                        case float expected:
                            Assert.Equal(expected, convertible.ToSingle(null));
                            break;
                        case double expected:
                            Assert.Equal(expected, convertible.ToDouble(null));
                            break;
                        case decimal expected:
                            Assert.Equal(expected, convertible.ToDecimal(null));
                            break;
                        case DateTime expected:
                            Assert.Equal(expected, convertible.ToDateTime(null));
                            break;
                        case string expected:
                            Assert.Equal(expected, convertible.ToString(null));
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Verify that the provided convert delegates produce expectedValues given testValues
        /// </summary>
        protected void VerifyFromString(Func<string, TOutput> convert, Func<string, IFormatProvider, TOutput> convertWithFormatProvider, string[] testValues, TOutput[] expectedValues)
        {
            Verify<string>(convert, testValues, expectedValues);
            Verify<string>(input => convertWithFormatProvider(input, TestFormatProvider.s_instance), testValues, expectedValues);
        }

        /// <summary>
        /// Verify that the provided convert delegates produce expectedValues given testValues
        /// </summary>
        protected void VerifyFromObject(Func<object, TOutput> convert, Func<object, IFormatProvider, TOutput> convertWithFormatProvider, object[] testValues, TOutput[] expectedValues)
        {
            Verify<object>(convert, testValues, expectedValues);
            Verify<object>(input => convertWithFormatProvider(input, TestFormatProvider.s_instance), testValues, expectedValues);
        }

        /// <summary>
        /// Verify that the provided convert delegate produces expectedValues given testValues and testBases
        /// </summary>
        protected void VerifyFromStringWithBase(Func<string, int, TOutput> convert, string[] testValues, int[] testBases, TOutput[] expectedValues)
        {
            Assert.Equal(testValues.Length, testBases.Length);
            Assert.Equal(testValues.Length, expectedValues.Length);

            for (int i = 0; i < testValues.Length; i++)
            {
                TOutput result = convert(testValues[i], testBases[i]);
                Assert.Equal(expectedValues[i], result);
            }
        }

        /// <summary>
        /// Verify that the provided convert delegate throws an exception of type TException given testValues and testBases
        /// </summary>
        protected void VerifyFromStringWithBaseThrows<TException>(Func<string, int, TOutput> convert, string[] testValues, int[] testBases) where TException : Exception
        {
            Assert.Equal(testValues.Length, testBases.Length);

            for (int i = 0; i < testValues.Length; i++)
            {
                try
                {
                    Assert.Throws<TException>(() => convert(testValues[i], testBases[i]));
                }
                catch (Exception e)
                {
                    string message = string.Format("Expected {0} converting '{1}' (base {2}) to '{3}'", typeof(TException).FullName, testValues[i], testBases[i], typeof(TOutput).FullName);
                    throw new AggregateException(message, e);
                }
            }
        }

        /// <summary>
        /// Verify that the provided convert delegate throws an exception of type TException given testValues
        /// </summary>
        protected void VerifyThrows<TException, TInput>(Func<TInput, TOutput> convert, TInput[] testValues) where TException : Exception
        {
            for (int i = 0; i < testValues.Length; i++)
            {
                try
                {
                    Assert.Throws<TException>(() => convert(testValues[i]));

                    if (testValues[i] is IConvertible convertible)
                    {
                        Assert.Throws<TException>(() => convertible.ToType(typeof(TOutput), null));

                        switch (default(TOutput))
                        {
                            case bool _:
                                Assert.Throws<TException>(() => convertible.ToBoolean(null));
                                break;
                            case char _:
                                Assert.Throws<TException>(() => convertible.ToChar(null));
                                break;
                            case sbyte _:
                                Assert.Throws<TException>(() => convertible.ToSByte(null));
                                break;
                            case byte _:
                                Assert.Throws<TException>(() => convertible.ToByte(null));
                                break;
                            case short _:
                                Assert.Throws<TException>(() => convertible.ToInt16(null));
                                break;
                            case ushort _:
                                Assert.Throws<TException>(() => convertible.ToUInt16(null));
                                break;
                            case int _:
                                Assert.Throws<TException>(() => convertible.ToInt32(null));
                                break;
                            case uint _:
                                Assert.Throws<TException>(() => convertible.ToUInt32(null));
                                break;
                            case long _:
                                Assert.Throws<TException>(() => convertible.ToInt64(null));
                                break;
                            case ulong _:
                                Assert.Throws<TException>(() => convertible.ToUInt64(null));
                                break;
                            case float _:
                                Assert.Throws<TException>(() => convertible.ToSingle(null));
                                break;
                            case double _:
                                Assert.Throws<TException>(() => convertible.ToDouble(null));
                                break;
                            case decimal _:
                                Assert.Throws<TException>(() => convertible.ToDecimal(null));
                                break;
                            case DateTime _:
                                Assert.Throws<TException>(() => convertible.ToDateTime(null));
                                break;
                            case string _:
                                Assert.Throws<TException>(() => convertible.ToString(null));
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    string message = string.Format("Expected {0} converting '{1}' ({2}) to {3}", typeof(TException).FullName, testValues[i], typeof(TInput).FullName, typeof(TOutput).FullName);
                    throw new AggregateException(message, e);
                }
            }
        }

        /// <summary>
        /// Verify that the provided convert delegates throws an exception of type TException given testValues
        /// </summary>
        protected void VerifyFromStringThrows<TException>(Func<string, TOutput> convert, Func<string, IFormatProvider, TOutput> convertWithFormatProvider, string[] testValues) where TException : Exception
        {
            VerifyThrows<TException, string>(convert, testValues);
            VerifyThrows<TException, string>(input => convertWithFormatProvider(input, TestFormatProvider.s_instance), testValues);
        }

        /// <summary>
        /// Verify that the provided convert delegates throw exception of type TException given testValues
        /// </summary>
        protected void VerifyFromObjectThrows<TException>(Func<object, TOutput> convert, Func<object, IFormatProvider, TOutput> convertWithFormatProvider, object[] testValues) where TException : Exception
        {
            VerifyThrows<TException, object>(convert, testValues);
            VerifyThrows<TException, object>(input => convertWithFormatProvider(input, TestFormatProvider.s_instance), testValues);
        }

        /// <summary>
        /// Helper class to test that the IFormatProvider is being called.
        /// </summary>
        protected class TestFormatProvider : IFormatProvider, ICustomFormatter
        {
            public static readonly TestFormatProvider s_instance = new TestFormatProvider();

            private TestFormatProvider()
            {
            }

            public object GetFormat(Type formatType)
            {
                return this;
            }

            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                return arg.ToString();
            }
        }
    }
}
