// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        // We need this because time is queried using Environment.TickCount for performance reasons
        // (Environment.TickCount returns milliseconds as an int and cycles):
        private const ulong MaximumMatchTimeoutTicks = 10_000UL * (int.MaxValue - 1); // TimeSpan.FromMilliseconds(int.MaxValue - 1).Ticks;

        // During static initialisation of Regex we check
        private const string DefaultMatchTimeout_ConfigKeyName = "REGEX_DEFAULT_MATCH_TIMEOUT";

        // Number of ticks represented by InfiniteMatchTimeout
        private const long InfiniteMatchTimeoutTicks = -10_000; // InfiniteMatchTimeout.Ticks

        // InfiniteMatchTimeout specifies that match timeout is switched OFF. It allows for faster code paths
        // compared to simply having a very large timeout.
        // We do not want to ask users to use System.Threading.Timeout.InfiniteTimeSpan as a parameter because:
        //   (1) We do not want to imply any relation between using a Regex timeout and using multi-threading.
        //   (2) We do not want to require users to take ref to a contract assembly for threading just to use RegEx.
        // We create a public Regex.InfiniteMatchTimeout constant, which for consistency uses the same underlying
        // value as Timeout.InfiniteTimeSpan, creating an implementation detail dependency only.
        public static readonly TimeSpan InfiniteMatchTimeout = Timeout.InfiniteTimeSpan;

        // DefaultMatchTimeout specifies the match timeout to use if no other timeout was specified
        // by one means or another. Typically, it is set to InfiniteMatchTimeout.
        internal static readonly TimeSpan s_defaultMatchTimeout = InitDefaultMatchTimeout();

        // timeout for the execution of this regex
        protected internal TimeSpan internalMatchTimeout;

        /// <summary>
        /// The match timeout used by this Regex instance.
        /// </summary>
        public TimeSpan MatchTimeout => internalMatchTimeout;

        /// <summary>Gets the default matching timeout value.</summary>
        /// <remarks>
        /// The default is queried from <code>AppContext</code>. If the AppContext's data value for that key is
        /// not a <code>TimeSpan</code> value or if it is outside the valid range, an exception is thrown.
        /// If the AppContext's data value for that key is <code>null</code>, an infinite timeout is returned.
        /// </remarks>
        private static TimeSpan InitDefaultMatchTimeout()
        {
            object? defaultTimeout = AppContext.GetData(DefaultMatchTimeout_ConfigKeyName);

            // If no default is specified, use fallback
            if (defaultTimeout is null)
            {
                return InfiniteMatchTimeout;
            }

            if (defaultTimeout is TimeSpan defaultMatchTimeOut)
            {
                // If default timeout is outside the valid range, throw. It will result in a TypeInitializationException:
                try
                {
                    ValidateMatchTimeout(defaultMatchTimeOut);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentOutOfRangeException(SR.Format(SR.IllegalDefaultRegexMatchTimeoutInAppDomain, DefaultMatchTimeout_ConfigKeyName, defaultMatchTimeOut));
                }

                return defaultMatchTimeOut;
            }

            throw new InvalidCastException(SR.Format(SR.IllegalDefaultRegexMatchTimeoutInAppDomain, DefaultMatchTimeout_ConfigKeyName, defaultTimeout));
        }
    }
}
