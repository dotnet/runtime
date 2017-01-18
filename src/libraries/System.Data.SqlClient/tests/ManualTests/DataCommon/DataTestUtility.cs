// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class DataTestUtility
    {
        public static readonly string NpConnStr = null;
        public static readonly string TcpConnStr = null;

        static DataTestUtility()
        {
            NpConnStr = Environment.GetEnvironmentVariable("TEST_NP_CONN_STR");
            TcpConnStr = Environment.GetEnvironmentVariable("TEST_TCP_CONN_STR");

            if (!AreConnStringsSetup())
            {
                Console.WriteLine("INFO: Test connection strings not defined! Tests cannot be run. Refer README.md of Manual tests for more information. ");
            }
        }

        public static bool AreConnStringsSetup()
        {
            return !string.IsNullOrEmpty(NpConnStr) && !string.IsNullOrEmpty(TcpConnStr);
        }

        // the name length will be no more then (16 + prefix.Length + escapeLeft.Length + escapeRight.Length)
        // some providers does not support names (Oracle supports up to 30)
        public static string GetUniqueName(string prefix, string escapeLeft, string escapeRight)
        {
            string uniqueName = string.Format("{0}{1}_{2}_{3}{4}",
                escapeLeft,
                prefix,
                DateTime.Now.Ticks.ToString("X", CultureInfo.InvariantCulture), // up to 8 characters
                Guid.NewGuid().ToString().Substring(0, 6), // take the first 6 characters only
                escapeRight);
            return uniqueName;
        }

        private static bool CheckException<TException>(Exception ex, string exceptionMessage, bool innerExceptionMustBeNull) where TException : Exception
        {
            return ((ex != null) && (ex is TException) &&
                ((string.IsNullOrEmpty(exceptionMessage)) || (ex.Message.Contains(exceptionMessage))) &&
                ((!innerExceptionMustBeNull) || (ex.InnerException == null)));
        }

        public static void AssertEqualsWithDescription(object expectedValue, object actualValue, string failMessage)
        {
            var msg = string.Format("{0}\nExpected: {1}\nActual: {2}", failMessage, expectedValue, actualValue);
            if (expectedValue == null || actualValue == null)
            {
                Assert.True(expectedValue == actualValue, msg);
            }
            else
            {
                Assert.True(expectedValue.Equals(actualValue), msg);
            }
        }

        public static TException AssertThrowsWrapper<TException>(Action actionThatFails, string exceptionMessage = null, bool innerExceptionMustBeNull = false, Func<TException, bool> customExceptionVerifier = null) where TException : Exception
        {
            TException ex = Assert.Throws<TException>(actionThatFails);
            if (exceptionMessage != null)
            {
                Assert.True(ex.Message.Contains(exceptionMessage),
                    string.Format("FAILED: Exception did not contain expected message.\nExpected: {0}\nActual: {1}", exceptionMessage, ex.Message));
            }

            if (innerExceptionMustBeNull)
            {
                Assert.True(ex.InnerException == null, "FAILED: Expected InnerException to be null.");
            }

            if (customExceptionVerifier != null)
            {
                Assert.True(customExceptionVerifier(ex), "FAILED: Custom exception verifier returned false for this exception.");
            }

            return ex;
        }

        public static TException AssertThrowsWrapper<TException, TInnerException>(Action actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, bool innerExceptionMustBeNull = false, Func<TException, bool> customExceptionVerifier = null) where TException : Exception
        {
            TException ex = AssertThrowsWrapper<TException>(actionThatFails, exceptionMessage, innerExceptionMustBeNull, customExceptionVerifier);

            if (innerExceptionMessage != null)
            {
                Assert.True(ex.InnerException != null, "FAILED: Cannot check innerExceptionMessage because InnerException is null.");
                Assert.True(ex.InnerException.Message.Contains(innerExceptionMessage),
                    string.Format("FAILED: Inner Exception did not contain expected message.\nExpected: {0}\nActual: {1}", innerExceptionMessage, ex.InnerException.Message));
            }

            return ex;
        }

        public static TException AssertThrowsWrapper<TException, TInnerException, TInnerInnerException>(Action actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, string innerInnerExceptionMessage = null, bool innerInnerInnerExceptionMustBeNull = false) where TException : Exception where TInnerException : Exception where TInnerInnerException : Exception
        {
            TException ex = AssertThrowsWrapper<TException, TInnerException>(actionThatFails, exceptionMessage, innerExceptionMessage);
            if (innerInnerInnerExceptionMustBeNull)
            {
                Assert.True(ex.InnerException != null, "FAILED: Cannot check innerInnerInnerExceptionMustBeNull since InnerException is null");
                Assert.True(ex.InnerException.InnerException == null, "FAILED: Expected InnerInnerException to be null.");
            }

            if (innerInnerExceptionMessage != null)
            {
                Assert.True(ex.InnerException != null, "FAILED: Cannot check innerInnerExceptionMessage since InnerException is null");
                Assert.True(ex.InnerException.InnerException != null, "FAILED: Cannot check innerInnerExceptionMessage since InnerInnerException is null");
                Assert.True(ex.InnerException.InnerException.Message.Contains(innerInnerExceptionMessage),
                    string.Format("FAILED: Inner Exception did not contain expected message.\nExpected: {0}\nActual: {1}", innerInnerExceptionMessage, ex.InnerException.InnerException.Message));
            }

            return ex;
        }

        public static TException ExpectFailure<TException>(Action actionThatFails, string exceptionMessage = null, bool innerExceptionMustBeNull = false, Func<TException, bool> customExceptionVerifier = null) where TException : Exception
        {
            try
            {
                actionThatFails();
                Console.WriteLine("ERROR: Did not get expected exception");
                return null;
            }
            catch (Exception ex)
            {
                if ((CheckException<TException>(ex, exceptionMessage, innerExceptionMustBeNull)) && ((customExceptionVerifier == null) || (customExceptionVerifier(ex as TException))))
                {
                    return (ex as TException);
                }
                else
                {
                    throw;
                }
            }
        }

        public static TException ExpectFailure<TException, TInnerException>(Action actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, bool innerInnerExceptionMustBeNull = false) where TException : Exception where TInnerException : Exception
        {
            try
            {
                actionThatFails();
                Console.WriteLine("ERROR: Did not get expected exception");
                return null;
            }
            catch (Exception ex)
            {
                if ((CheckException<TException>(ex, exceptionMessage, false)) && (CheckException<TInnerException>(ex.InnerException, innerExceptionMessage, innerInnerExceptionMustBeNull)))
                {
                    return (ex as TException);
                }
                else
                {
                    throw;
                }
            }
        }

        public static TException ExpectFailure<TException, TInnerException, TInnerInnerException>(Action actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, string innerInnerExceptionMessage = null, bool innerInnerInnerExceptionMustBeNull = false) where TException : Exception where TInnerException : Exception where TInnerInnerException : Exception
        {
            try
            {
                actionThatFails();
                Console.WriteLine("ERROR: Did not get expected exception");
                return null;
            }
            catch (Exception ex)
            {
                if ((CheckException<TException>(ex, exceptionMessage, false)) && (CheckException<TInnerException>(ex.InnerException, innerExceptionMessage, false)) && (CheckException<TInnerInnerException>(ex.InnerException.InnerException, innerInnerExceptionMessage, innerInnerInnerExceptionMustBeNull)))
                {
                    return (ex as TException);
                }
                else
                {
                    throw;
                }
            }
        }

        public static void ExpectAsyncFailure<TException>(Func<Task> actionThatFails, string exceptionMessage = null, bool innerExceptionMustBeNull = false) where TException : Exception
        {
            ExpectFailure<AggregateException, TException>(() => actionThatFails().Wait(), null, exceptionMessage, innerExceptionMustBeNull);
        }

        public static void ExpectAsyncFailure<TException, TInnerException>(Func<Task> actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, bool innerInnerExceptionMustBeNull = false) where TException : Exception where TInnerException : Exception
        {
            ExpectFailure<AggregateException, TException, TInnerException>(() => actionThatFails().Wait(), null, exceptionMessage, innerExceptionMessage, innerInnerExceptionMustBeNull);
        }
    }
}
