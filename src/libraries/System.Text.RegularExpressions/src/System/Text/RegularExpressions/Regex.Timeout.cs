// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        // We need this because time is queried using Environment.TickCount for performance reasons
        // (Environment.TickCount returns milliseconds as an int and cycles):
        private static readonly TimeSpan s_maximumMatchTimeout = TimeSpan.FromMilliseconds(int.MaxValue - 1);

        // During static initialisation of Regex we check
        private const string DefaultMatchTimeout_ConfigKeyName = "REGEX_DEFAULT_MATCH_TIMEOUT";

        // InfiniteMatchTimeout specifies that match timeout is switched OFF. It allows for faster code paths
        // compared to simply having a very large timeout.
        // We do not want to ask users to use System.Threading.Timeout.InfiniteTimeSpan as a parameter because:
        //   (1) We do not want to imply any relation between having using a RegEx timeout and using multi-threading.
        //   (2) We do not want to require users to take ref to a contract assembly for threading just to use RegEx.
        //       There may in theory be a SKU that has RegEx, but no multithreading.
        // We create a public Regex.InfiniteMatchTimeout constant, which for consistency uses the save underlying
        // value as Timeout.InfiniteTimeSpan creating an implementation detail dependency only.
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

        // Note: "&lt;" is the XML entity for smaller ("<").
        /// <summary>
        /// Validates that the specified match timeout value is valid.
        /// The valid range is <code>TimeSpan.Zero &lt; matchTimeout &lt;= Regex.MaximumMatchTimeout</code>.
        /// </summary>
        /// <param name="matchTimeout">The timeout value to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the specified timeout is not within a valid range.
        /// </exception>
        protected internal static void ValidateMatchTimeout(TimeSpan matchTimeout)
        {
            if (InfiniteMatchTimeout == matchTimeout)
                return;

            // Change this to make sure timeout is not longer then Environment.Ticks cycle length:
            if (TimeSpan.Zero < matchTimeout && matchTimeout <= s_maximumMatchTimeout)
                return;

            throw new ArgumentOutOfRangeException(nameof(matchTimeout));
        }

        /// <summary>
        /// Specifies the default RegEx matching timeout value (i.e. the timeout that will be used if no
        /// explicit timeout is specified).
        /// The default is queried from the current <code>AppDomain</code>.
        /// If the AddDomain's data value for that key is not a <code>TimeSpan</code> value or if it is outside the
        /// valid range, an exception is thrown.
        /// If the AddDomain's data value for that key is <code>null</code>, a fallback value is returned.
        /// </summary>
        /// <returns>The default RegEx matching timeout for this AppDomain</returns>
        private static TimeSpan InitDefaultMatchTimeout()
        {
            // Query AppDomain
            AppDomain ad = AppDomain.CurrentDomain;
            object defaultMatchTimeoutObj = ad.GetData(DefaultMatchTimeout_ConfigKeyName);

            // If no default is specified, use fallback
            if (defaultMatchTimeoutObj == null)
            {
                return InfiniteMatchTimeout;
            }

            if (defaultMatchTimeoutObj is TimeSpan defaultMatchTimeOut)
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

            throw new InvalidCastException(SR.Format(SR.IllegalDefaultRegexMatchTimeoutInAppDomain, DefaultMatchTimeout_ConfigKeyName, defaultMatchTimeoutObj));
        }
    }
}
