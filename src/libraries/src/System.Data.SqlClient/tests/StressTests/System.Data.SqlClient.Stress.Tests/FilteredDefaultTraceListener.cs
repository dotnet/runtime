// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Stress.Data.SqlClient
{
    /// <summary>
    /// A DefaultTraceListener that can filter out given asserts
    /// </summary>
    internal class FilteredDefaultTraceListener : DefaultTraceListener
    {
        private static readonly Assembly s_systemDataAssembly = typeof(System.Data.SqlClient.SqlConnection).GetTypeInfo().Assembly;
        private const RegexOptions AssertMessageRegexOptions = RegexOptions.Singleline | RegexOptions.CultureInvariant;

        private enum MatchType : byte
        {
            Exact,
            Regex,
        }

        private enum HandlingOption : byte
        {
            CovertToException,
            WriteToConsole,
        }

        /// <summary>
        /// Represents a single assert to filter out
        /// </summary>
        private struct FilteredAssert
        {
            public FilteredAssert(string messageOrRegex, int bugNumber, MatchType matchType, HandlingOption assertHandlingOption, params string[] stackFrames)
            {
                if (matchType == MatchType.Exact)
                {
                    Message = messageOrRegex;
                    MessageRegex = null;
                }
                else
                {
                    Message = null;
                    MessageRegex = new Regex(messageOrRegex, AssertMessageRegexOptions);
                }


                StackFrames = stackFrames;
                BugNumber = bugNumber;
                Handler = assertHandlingOption;
            }

            /// <summary>
            /// The assert's message (NOTE: MessageRegex must be null if this is specified)
            /// </summary>
            public string Message;
            /// <summary>
            /// A regex that matches the assert's message (NOTE: Message must be null if this is specified)
            /// </summary>
            public Regex MessageRegex;
            /// <summary>
            /// The most recent frames on the stack when the assert was hit (i.e. 0 is most recent, 1 is next, etc.). Null if stack should not be checked.
            /// </summary>
            public string[] StackFrames;
            /// <summary>
            /// Product bug to fix the assert
            /// </summary>
            public int BugNumber;
            /// <summary>
            /// How the assert will be handled once it is matched
            /// </summary>
            /// <remarks>
            /// In most cases this can be set to WriteToConsole - typically the assert is either invalid or there will be an exception thrown by the product code anyway.
            /// However, in the case where this is state corruption AND the product code has no exception in place, this will need to be set to CovertToException to prevent further corruption\asserts
            /// </remarks>
            public HandlingOption Handler;
        }

        private static readonly FilteredAssert[] s_assertsToFilter = new FilteredAssert[] {
            new FilteredAssert("TdsParser::ThrowExceptionAndWarning called with no exceptions or warnings!", 433324, MatchType.Exact, HandlingOption.WriteToConsole,
                "System.Data.SqlClient.TdsParser.ThrowExceptionAndWarning",
                "System.Data.SqlClient.TdsParserStateObject.ThrowExceptionAndWarning",
                "System.Data.SqlClient.TdsParserStateObject.ReadAsyncCallbackCaptureException"),
        };

        public FilteredDefaultTraceListener(DefaultTraceListener listenerToClone) : base()
        {
            base.Filter = listenerToClone.Filter;
            base.IndentLevel = listenerToClone.IndentLevel;
            base.IndentSize = listenerToClone.IndentSize;
            base.TraceOutputOptions = listenerToClone.TraceOutputOptions;
        }

        public override void Fail(string message)
        {
            Fail(message, null);
        }

        public override void Fail(string message, string detailMessage)
        {
            FilteredAssert? foundAssert = FindAssertInList(message);
            if (!foundAssert.HasValue)
            {
                // Don't filter this assert - pass it down to the underlying DefaultTraceListener which will show the UI, break into the debugger, etc.
                base.Fail(message, detailMessage);
            }
            else
            {
                // Assert is to be filtered, either convert to an exception or a message
                var assert = foundAssert.Value;
                if (assert.Handler == HandlingOption.CovertToException)
                {
                    throw new FailedAssertException(message, assert.BugNumber);
                }
                else if (assert.Handler == HandlingOption.WriteToConsole)
                {
                    Console.WriteLine("Hit known assert, Bug {0}: {1}", assert.BugNumber, message);
                }
            }
        }

        private FilteredAssert? FindAssertInList(string message)
        {
            StackTrace actualCallstack = null;
            foreach (var assert in s_assertsToFilter)
            {
                if (((assert.Message != null) && (assert.Message == message)) || ((assert.MessageRegex != null) && (assert.MessageRegex.IsMatch(message))))
                {
                    if (assert.StackFrames != null)
                    {
                        // Skipping four frames:
                        // Stress.Data.SqlClient.FilteredDefaultTraceListener.FindAssertInList
                        // Stress.Data.SqlClient.FilteredDefaultTraceListener.Fail (This may be in the stack twice due to the overloads calling each other)
                        // System.Diagnostics.TraceInternal.Fail
                        // System.Diagnostics.Debug.Assert
                        if (actualCallstack == null)
                        {
                            actualCallstack = new StackTrace(e: new InvalidOperationException(), fNeedFileInfo: false);
                        }

                        StackFrame[] frames = actualCallstack.GetFrames();
                        if (frames.Length >= assert.StackFrames.Length)
                        {
                            int actualStackFrameCounter = 0;
                            bool foundMatch = true;
                            foreach (var expectedStack in assert.StackFrames)
                            {
                                // Get the method information for the next stack which came from System.Data.dll
                                MethodBase actualStackMethod;
                                do
                                {
                                    actualStackMethod = frames[actualStackFrameCounter].GetMethod();
                                    actualStackFrameCounter++;
                                } while (((actualStackMethod.DeclaringType == null) || (actualStackMethod.DeclaringType.GetTypeInfo().Assembly != s_systemDataAssembly)) && (actualStackFrameCounter < frames.Length));

                                if ((actualStackFrameCounter > frames.Length) || (string.Format("{0}.{1}", actualStackMethod.DeclaringType.FullName, actualStackMethod.Name) != expectedStack))
                                {
                                    // Ran out of actual frames while there were still expected frames or the current frames didn't match
                                    foundMatch = false;
                                    break;
                                }
                            }

                            // Message and all frames matched
                            if (foundMatch)
                            {
                                return assert;
                            }
                        }
                    }
                    else
                    {
                        // Messages match, and there are no frames to verify
                        return assert;
                    }
                }
            }

            // Fall through - didn't find the assert
            return null;
        }
    }

    internal class FailedAssertException : Exception
    {
        /// <summary>
        /// Number of the bug that caused the assert to fire
        /// </summary>
        public int BugNumber { get; private set; }

        /// <summary>
        /// Creates an exception to represent hitting a known assert
        /// </summary>
        /// <param name="message">Message of the assert</param>
        /// <param name="bugNumber">Number of the bug that caused the assert</param>
        public FailedAssertException(string message, int bugNumber)
            : base(message)
        {
            BugNumber = bugNumber;
        }

        public override string ToString()
        {
            return string.Format("{1}\r\nAssert caused by Bug {0}", BugNumber, base.ToString());
        }
    }
}
