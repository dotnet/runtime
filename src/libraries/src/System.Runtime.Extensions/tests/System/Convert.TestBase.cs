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
            }
        }

        /// <summary>
        /// Verify that the provided convert delegates produce expectedValues given testValues
        /// </summary>
        protected void VerifyFromString(Func<String, TOutput> convert, Func<String, IFormatProvider, TOutput> convertWithFormatProvider, String[] testValues, TOutput[] expectedValues)
        {
            Verify<String>(convert, testValues, expectedValues);
            Verify<String>(input => convertWithFormatProvider(input, TestFormatProvider.s_instance), testValues, expectedValues);
        }

        /// <summary>
        /// Verify that the provided convert delegates produce expectedValues given testValues
        /// </summary>
        protected void VerifyFromObject(Func<Object, TOutput> convert, Func<Object, IFormatProvider, TOutput> convertWithFormatProvider, Object[] testValues, TOutput[] expectedValues)
        {
            Verify<Object>(convert, testValues, expectedValues);
            Verify<Object>(input => convertWithFormatProvider(input, TestFormatProvider.s_instance), testValues, expectedValues);
        }

        /// <summary>
        /// Verify that the provided convert delegate produces expectedValues given testValues and testBases
        /// </summary>
        protected void VerifyFromStringWithBase(Func<String, Int32, TOutput> convert, String[] testValues, Int32[] testBases, TOutput[] expectedValues)
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
        protected void VerifyFromStringWithBaseThrows<TException>(Func<String, Int32, TOutput> convert, String[] testValues, Int32[] testBases) where TException : Exception
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
                    String message = String.Format("Expected {0} converting '{1}' (base {2}) to '{3}'", typeof(TException).FullName, testValues[i], testBases[i], typeof(TOutput).FullName);
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
                }
                catch (Exception e)
                {
                    String message = String.Format("Expected {0} converting '{1}' ({2}) to {3}", typeof(TException).FullName, testValues[i], typeof(TInput).FullName, typeof(TOutput).FullName);
                    throw new AggregateException(message, e);
                }
            }
        }

        /// <summary>
        /// Verify that the provided convert delegates throws an exception of type TException given testValues
        /// </summary>
        protected void VerifyFromStringThrows<TException>(Func<String, TOutput> convert, Func<String, IFormatProvider, TOutput> convertWithFormatProvider, String[] testValues) where TException : Exception
        {
            VerifyThrows<TException, String>(convert, testValues);
            VerifyThrows<TException, String>(input => convertWithFormatProvider(input, TestFormatProvider.s_instance), testValues);
        }

        /// <summary>
        /// Verify that the provided convert delegates throw exception of type TException given testValues
        /// </summary>
        protected void VerifyFromObjectThrows<TException>(Func<Object, TOutput> convert, Func<Object, IFormatProvider, TOutput> convertWithFormatProvider, Object[] testValues) where TException : Exception
        {
            VerifyThrows<TException, Object>(convert, testValues);
            VerifyThrows<TException, Object>(input => convertWithFormatProvider(input, TestFormatProvider.s_instance), testValues);
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

            public Object GetFormat(Type formatType)
            {
                return this;
            }

            public String Format(String format, Object arg, IFormatProvider formatProvider)
            {
                return arg.ToString();
            }
        }
    }
}
