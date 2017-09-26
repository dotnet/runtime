// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Stress.Data
{
    public enum ErrorHandlingAction
    {
        // If you add an item here, remember to add it to all of the methods below
        DebugBreak,
        ThrowException
    }

    /// <summary>
    /// Static class containing methods to report errors.
    /// 
    /// The StressTest executor will eat exceptions that are thrown and write them out to the console. In theory these should all be
    /// either harmless exceptions or product bugs, however at present there are a large number of test issues that will cause a flood
    /// of exceptions. Therefore if something actually bad happens (e.g. a known product bug is hit due to regression, or a major test
    /// programming error) this error would be easy to miss if it were reported just by throwing an exception. To solve this, we use
    /// this class for structured & consistent handling of errors.
    /// </summary>
    public static class DataStressErrors
    {
        private static void DebugBreak(string message, Exception exception)
        {
            // Print out the error before breaking to make debugging easier
            Console.WriteLine(message);
            if (exception != null)
            {
                Console.WriteLine(exception);
            }

            Debugger.Break();
        }

        /// <summary>
        /// Reports that a product bug has been hit. The action that will be taken is configurable in the .config file. 
        /// This can be used to check for regressions of known product bugs.
        /// </summary>
        /// <param name="description">A description of the product bug hit (e.g. title, bug number & database, more information)</param>
        /// <param name="exception">The exception that was thrown that indicates a product bug, or null if the product bug was detected without 
        /// having thrown an exception</param>
        /// <returns>An exception that the caller should throw.</returns>
        public static Exception ProductError(string description, Exception exception = null)
        {
            switch (DataStressSettings.Instance.ActionOnProductError)
            {
                case ErrorHandlingAction.DebugBreak:
                    DebugBreak("Hit product error: " + description, exception);
                    return new ProductErrorException(description, exception);

                case ErrorHandlingAction.ThrowException:
                    return new ProductErrorException(description, exception);

                default:
                    throw UnhandledCaseError(DataStressSettings.Instance.ActionOnProductError);
            }
        }

        /// <summary>
        /// Reports that a non-fatal test error has been hit. The action that will be taken is configurable in the .config file. 
        /// This should be used for test errors that do not prevent the test from running.
        /// </summary>
        /// <param name="description">A description of the error</param>
        /// <param name="exception">The exception that was thrown that indicates an error, or null if the error was detected without 
        /// <returns>An exception that the caller should throw.</returns>
        public static Exception TestError(string description, Exception exception = null)
        {
            switch (DataStressSettings.Instance.ActionOnTestError)
            {
                case ErrorHandlingAction.DebugBreak:
                    DebugBreak("Hit test error: " + description, exception);
                    return new TestErrorException(description, exception);

                case ErrorHandlingAction.ThrowException:
                    return new TestErrorException(description, exception);

                default:
                    throw UnhandledCaseError(DataStressSettings.Instance.ActionOnTestError);
            }
        }

        /// <summary>
        /// Reports that a programming error in the test code has occurred. The action that will be taken is configurable in the .config file. 
        /// This must strictly be used to report programming errors. It should not be in any way possible to see one of these errors unless 
        /// you make an incorrect change to the code, for example having an unhandled case in a switch statement.
        /// </summary>
        /// <param name="description">A description of the error</param>
        /// <param name="exception">The exception that was thrown that indicates an error, or null if the error was detected without 
        /// having thrown an exception</param>
        /// <returns>An exception that the caller should throw.</returns>
        private static Exception ProgrammingError(string description, Exception exception = null)
        {
            switch (DataStressSettings.Instance.ActionOnProgrammingError)
            {
                case ErrorHandlingAction.DebugBreak:
                    DebugBreak("Hit programming error: " + description, exception);
                    return new ProgrammingErrorException(description, exception);

                case ErrorHandlingAction.ThrowException:
                    return new ProgrammingErrorException(description, exception);

                default:
                    // If we are here then it's a programming error, but calling UnhandledCaseError here would cause an inifite loop.
                    goto case ErrorHandlingAction.DebugBreak;
            }
        }

        /// <summary>
        /// Reports that an unhandled case in a switch statement in the test code has occurred. The action that will be taken is configurable
        /// as a programming error in the .config file. It should not be in any way possible to see one of these errors unless 
        /// you make an incorrect change to the test code, for example having an unhandled case in a switch statement.
        /// </summary>
        /// <param name="unhandledValue">The value that was not handled in the switch statement</param>
        /// <returns>An exception that the caller should throw.</returns>
        public static Exception UnhandledCaseError<T>(T unhandledValue)
        {
            return ProgrammingError("Unhandled case in switch statement: " + unhandledValue);
        }

        /// <summary>
        /// Asserts that a condition is true. If the condition is false then throws a ProgrammingError. 
        /// This must strictly be used to report programming errors. It should not be in any way possible to see one of these errors unless 
        /// you make an incorrect change to the code, for example having an unhandled case in a switch statement.
        /// </summary>
        /// <param name="condition">A condition to assert</param>
        /// <param name="description">A description of the error</param>
        /// <exception cref="ProgrammingErrorException">if the condition is false</exception>
        public static void Assert(bool condition, string description)
        {
            if (!condition)
            {
                throw ProgrammingError(description);
            }
        }

        /// <summary>
        /// Reports that a fatal error has happened. This is an error that completely prevents the test from continuing, 
        /// for example a setup failure. Ordinary programming errors should not be handled by this method.
        /// </summary>
        /// <param name="description">A description of the error</param>
        /// <returns>An exception that the caller should throw.</returns>
        public static Exception FatalError(string description)
        {
            Console.WriteLine("Fatal test error: {0}", description);
            Debugger.Break();       // Give the user a chance to debug
            Environment.FailFast("Fatal error. Exit.");
            return new Exception(); // Caller should throw this to indicate to the compiler that any code after the call is unreachable
        }

        #region Exception types

        // These exception types are provided so that they can be easily found in logs, i.e. just do a text search in the console
        // output log for "ProductErrorException"

        private class ProductErrorException : Exception
        {
            public ProductErrorException()
                : base()
            {
            }

            public ProductErrorException(string message)
                : base(message)
            {
            }

            public ProductErrorException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        private class ProgrammingErrorException : Exception
        {
            public ProgrammingErrorException()
                : base()
            {
            }

            public ProgrammingErrorException(string message)
                : base(message)
            {
            }

            public ProgrammingErrorException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        private class TestErrorException : Exception
        {
            public TestErrorException()
                : base()
            {
            }

            public TestErrorException(string message)
                : base(message)
            {
            }

            public TestErrorException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }
        #endregion
    }
}
