using System;
using Xunit;

namespace BasicEventSourceTests
{
    /// <summary>
    /// Allows testing for multiple exceptions in one test method (as opposed to the ExceptionExpectedAttribute)
    /// </summary>
    public static class ExceptionAssert
    {
        /// <summary>
        /// Validates that an exception of type T is thrown. If no or another exception is thrown the test will fail.
        /// </summary>
        /// <typeparam name="T">The expected exception type.</typeparam>
        /// <param name="testMethod">The test to execute.</param>
        public static void Throws<T>(Action testMethod) where T : Exception
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException("testMethod");
            }

            Exception exception = null;

            try
            {
                testMethod();
            }
            catch (Exception error)
            {
                exception = error;
            }

            Assert.NotNull(exception);
            Assert.IsType(typeof(T), exception);
        }

        /// <summary>
        /// Validates that an exception of type T with the specified error message is thrown. 
        /// If no or another exception is thrown the test will fail.
        /// </summary>
        /// <param name="expectedErrorMessage">The expected error message.</param>
        /// <typeparam name="T">The expected exception type.</typeparam>
        /// <param name="testMethod">The test to execute.</param>
        public static void Throws<T>(string expectedErrorMessage, Action testMethod)
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException("testMethod");
            }

            Exception exception = null;
            string message = null;

            try
            {
                testMethod();
            }
            catch (Exception error)
            {
                exception = error;
                message = error.Message;
            }

            Assert.NotNull(exception);
            Assert.NotNull(message);
            Assert.IsType(typeof(T), exception);
            Assert.Equal<string>(expectedErrorMessage, message);
        }
        public static void Throws<T>(string expectedErrorMessage1, string expectedErrorMessage2, Action testMethod)
        {
            Throws<T>(string.Join(Environment.NewLine, new string[] { expectedErrorMessage1, expectedErrorMessage2 }), testMethod);
        }
        public static void Throws<T>(string expectedErrorMessage1, string expectedErrorMessage2, string expectedErrorMessage3, Action testMethod)
        {
            Throws<T>(string.Join(Environment.NewLine, new string[] { expectedErrorMessage1, expectedErrorMessage2, expectedErrorMessage3 }), testMethod);
        }
    }
}
