// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// This is the exception that is thrown when a RegEx matching timeout occurs.
    /// </summary>
    [Serializable]
    public class RegexMatchTimeoutException : TimeoutException, ISerializable
    {
        private string _regexInput = null;

        private string _regexPattern = null;

        private TimeSpan _matchTimeout = TimeSpan.FromTicks(-1);


        /// <summary>
        /// Constructs a new RegexMatchTimeoutException.
        /// </summary>
        /// <param name="regexInput">Matching timeout occurred during matching within the specified input.</param>
        /// <param name="regexPattern">Matching timeout occurred during matching to the specified pattern.</param>
        /// <param name="matchTimeout">Matching timeout occurred because matching took longer than the specified timeout.</param>
        public RegexMatchTimeoutException(string regexInput, string regexPattern, TimeSpan matchTimeout) :
            base(SR.RegexMatchTimeoutException_Occurred)
        {
            Init(regexInput, regexPattern, matchTimeout);
        }


        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public RegexMatchTimeoutException(string input, string pattern, TimeSpan matchTimeout)</code>.
        /// </summary>
        public RegexMatchTimeoutException()
            : base()
        {
            Init();
        }


        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public RegexMatchTimeoutException(string input, string pattern, TimeSpan matchTimeout)</code>.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public RegexMatchTimeoutException(string message)
            : base(message)
        {
            Init();
        }


        /// <summary>
        /// This constructor is provided in compliance with common NetFx design patterns;
        /// developers should prefer using the constructor
        /// <code>public RegexMatchTimeoutException(string input, string pattern, TimeSpan matchTimeout)</code>.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a <code>null</code>.</param>
        public RegexMatchTimeoutException(string message, Exception inner)
            : base(message, inner)
        {
            Init();
        }

        private void Init()
        {
            Init("", "", TimeSpan.FromTicks(-1));
        }

        private void Init(string input, string pattern, TimeSpan timeout)
        {
            _regexInput = input;
            _regexPattern = pattern;
            _matchTimeout = timeout;
        }

        public string Pattern
        {
            [SecurityCritical]
            get
            { return _regexPattern; }
        }

        public string Input
        {
            [SecurityCritical]
            get
            { return _regexInput; }
        }


        public TimeSpan MatchTimeout
        {
            [SecurityCritical]
            get
            { return _matchTimeout; }
        }

        protected RegexMatchTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            string input = info.GetString("regexInput");
            string pattern = info.GetString("regexPattern");
            TimeSpan timeout = new TimeSpan(info.GetInt64("timeoutTicks"));
            Init(input, pattern, timeout);
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            base.GetObjectData(si, context);
            si.AddValue("regexInput", _regexInput);
            si.AddValue("regexPattern", _regexPattern);
            si.AddValue("timeoutTicks", _matchTimeout.Ticks);
        }
    }
}
