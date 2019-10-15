// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// This is the exception that is thrown when a RegEx matching timeout occurs.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class RegexMatchTimeoutException : TimeoutException, ISerializable
    {
        /// <summary>
        /// Constructs a new RegexMatchTimeoutException.
        /// </summary>
        /// <param name="regexInput">Matching timeout occurred during matching within the specified input.</param>
        /// <param name="regexPattern">Matching timeout occurred during matching to the specified pattern.</param>
        /// <param name="matchTimeout">Matching timeout occurred because matching took longer than the specified timeout.</param>
        public RegexMatchTimeoutException(string regexInput, string regexPattern, TimeSpan matchTimeout)
            : base(SR.RegexMatchTimeoutException_Occurred)
        {
            Input = regexInput;
            Pattern = regexPattern;
            MatchTimeout = matchTimeout;
        }

        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public RegexMatchTimeoutException(string input, string pattern, TimeSpan matchTimeout)</code>.
        /// </summary>
        public RegexMatchTimeoutException() { }

        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public RegexMatchTimeoutException(string input, string pattern, TimeSpan matchTimeout)</code>.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public RegexMatchTimeoutException(string message) : base(message) { }

        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public RegexMatchTimeoutException(string input, string pattern, TimeSpan matchTimeout)</code>.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a <code>null</code>.</param>
        public RegexMatchTimeoutException(string message, Exception inner) : base(message, inner) { }

        protected RegexMatchTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Input = info.GetString("regexInput");
            Pattern = info.GetString("regexPattern");
            MatchTimeout = new TimeSpan(info.GetInt64("timeoutTicks"));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("regexInput", Input);
            info.AddValue("regexPattern", Pattern);
            info.AddValue("timeoutTicks", MatchTimeout.Ticks);
        }

        public string Input { get; } = string.Empty;

        public string Pattern { get; } = string.Empty;

        public TimeSpan MatchTimeout { get; } = TimeSpan.FromTicks(-1);
    }
}
