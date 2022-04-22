// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>The maximum allowed timeout duration.</summary>
        /// <remarks>
        /// Previously the timeout was based on Environment.TickCount, which can overflow. The implementation now uses Environment.TickCount64,
        /// such that this constraint could be relaxed in the future if desired.
        /// </remarks>
        private const ulong MaximumMatchTimeoutTicks = 10_000UL * (int.MaxValue - 1); // TimeSpan.FromMilliseconds(int.MaxValue - 1).Ticks;

        /// <summary>Name of the AppContext slot that may be set to a default timeout override.</summary>
        /// <remarks>
        /// Setting this to a valid TimeSpan will cause that TimeSpan to be used as the timeout for <see cref="Regex"/> instances created
        /// without a timeout. If a timeout is explicitly provided, even if it's infinite, this value will be ignored.
        /// </remarks>
        private const string DefaultMatchTimeout_ConfigKeyName = "REGEX_DEFAULT_MATCH_TIMEOUT";

        /// <summary>Number of ticks represented by <see cref="InfiniteMatchTimeout"/>.</summary>
        private const long InfiniteMatchTimeoutTicks = -10_000; // InfiniteMatchTimeout.Ticks

        // Historical note:
        // Regex.InifiniteMatchTimeout was created instead of Timeout.InfiniteTimeSpan because of:
        // 1) Concerns about a connection between Regex and multi-threading.
        // 2) Concerns around requiring an extra contract assembly reference to access Timeout.
        // Neither of these would motivate such an addition now, but the API exists.

        /// <summary>Specifies that a pattern-matching operation should not time out.</summary>
        public static readonly TimeSpan InfiniteMatchTimeout = Timeout.InfiniteTimeSpan;

        /// <summary>
        /// The default timeout value to use if one isn't explicitly specified when creating the <see cref="Regex"/>
        /// or using its static methods (which implicitly create one if one can't be found in the cache).
        /// </summary>
        /// <remarks>
        /// The default defaults to <see cref="InfiniteMatchTimeout"/> but can be overridden by setting
        /// the <see cref="DefaultMatchTimeout_ConfigKeyName"/> <see cref="AppContext"/> slot.
        /// </remarks>
        internal static readonly TimeSpan s_defaultMatchTimeout = InitDefaultMatchTimeout();

        /// <summary>Timeout for the execution of this <see cref="Regex"/>.</summary>
        protected internal TimeSpan internalMatchTimeout;

        /// <summary>Gets the timeout interval of the current instance.</summary>
        /// <remarks>
        /// The <see cref="MatchTimeout"/> property defines the approximate maximum time interval for a <see cref="Regex"/> instance to execute a single matching
        /// operation before the operation times out. The regular expression engine throws a <see cref="RegexMatchTimeoutException"/> exception during
        /// its next timing check after the timeout interval has elapsed. This prevents the regular expression engine from processing input strings
        /// that require excessive backtracking. The backtracking implementations guarantee that no more than O(N) work (N == length of the input)
        /// will be performed between timeout checks, though may check more frequently. This enables the implementations to use mechanisms like
        /// <see cref="string.IndexOf(char)"/> to search the input. Timeouts are considered optional for non-backtracking implementations
        /// (<see cref="RegexOptions.NonBacktracking"/>), as the purpose of the timeout is to thwart excessive backtracking. Such implementations
        /// may incur up to O(N * M) operations (N == length of the input, M == length of the pattern) as part of processing input.
        /// </remarks>
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

            if (defaultTimeout is not TimeSpan defaultMatchTimeOut)
            {
                // If not default was specified in AppContext, default to infinite.
                if (defaultTimeout is null)
                {
                    return InfiniteMatchTimeout;
                }

                // If a default was specified that's not a TimeSpan, throw.
                throw new InvalidCastException(SR.Format(SR.IllegalDefaultRegexMatchTimeoutInAppDomain, DefaultMatchTimeout_ConfigKeyName, defaultTimeout));
            }

            // If default timeout is outside the valid range, throw. As this is used in the static cctor, it will result in a TypeInitializationException
            // for all subsequent Regex use.
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
    }
}
