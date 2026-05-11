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
        // Regex.InfiniteMatchTimeout was created instead of Timeout.InfiniteTimeSpan because of:
        // 1) Concerns about a connection between Regex and multi-threading.
        // 2) Concerns around requiring an extra contract assembly reference to access Timeout.
        // Neither of these would motivate such an addition now, but the API exists.

        /// <summary>Specifies that a pattern-matching operation should not time out.</summary>
        /// <remarks>
        /// <para>
        /// The <see cref="Regex(string, RegexOptions, TimeSpan)"/> constructor and a number of static matching
        /// methods use the <see cref="InfiniteMatchTimeout"/> constant to indicate that the attempt to find a
        /// pattern match should not time out.
        /// </para>
        /// <para>
        /// Setting the regular expression engine's time-out value to <see cref="InfiniteMatchTimeout"/> can
        /// cause regular expressions that rely on excessive backtracking to appear to stop responding when
        /// processing text that nearly matches the regular expression pattern. If you disable time-outs, you
        /// should ensure that your regular expression does not rely on excessive backtracking and that it
        /// handles text that nearly matches the regular expression pattern. For more information about handling
        /// backtracking, see
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/backtracking-in-regular-expressions">Backtracking</see>.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// The maximum amount of time that can elapse in a pattern-matching operation before the operation
        /// times out.
        /// </summary>
        protected internal TimeSpan internalMatchTimeout;

        /// <summary>Gets the time-out interval of the current instance.</summary>
        /// <value>
        /// The maximum time interval that can elapse in a pattern-matching operation before a
        /// <see cref="RegexMatchTimeoutException"/> is thrown, or <see cref="InfiniteMatchTimeout"/> if
        /// time-outs are disabled.
        /// </value>
        /// <remarks>
        /// <para>
        /// The <see cref="MatchTimeout"/> property defines the approximate maximum time interval for a
        /// <see cref="Regex"/> instance to execute a single matching operation before the operation times out.
        /// The regular expression engine throws a <see cref="RegexMatchTimeoutException"/> exception during
        /// its next timing check after the time-out interval has elapsed. This prevents the regular expression
        /// engine from processing input strings that require excessive backtracking. For more information, see
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/backtracking-in-regular-expressions">Backtracking</see>
        /// and
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/best-practices">Best Practices for Regular Expressions</see>.
        /// </para>
        /// <para>
        /// This property is read-only. You can set its value explicitly for an individual <see cref="Regex"/>
        /// object by calling the <see cref="Regex(string, RegexOptions, TimeSpan)"/> constructor; and you can
        /// set its value for all <see cref="Regex"/> matching operations in an application domain by calling
        /// the <see cref="AppDomain.SetData(string, object?)"/> method and providing a <see cref="TimeSpan"/>
        /// value for the "REGEX_DEFAULT_MATCH_TIMEOUT" property.
        /// </para>
        /// <para>
        /// If you do not explicitly set a time-out interval, the default value
        /// <see cref="InfiniteMatchTimeout"/> is used, and matching operations do not time out.
        /// </para>
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
