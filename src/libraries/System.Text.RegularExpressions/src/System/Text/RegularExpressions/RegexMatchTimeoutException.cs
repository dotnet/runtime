// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// This is the exception that is thrown when a RegEx matching timeout occurs.
    /// </summary>
    public class RegexMatchTimeoutException : TimeoutException
    {
        private string _regexInput = null;

        private string _regexPattern = null;

        private TimeSpan _matchTimeout = TimeSpan.FromTicks(-1);


        /// <summary>
        /// Constructs a new RegexMatchTimeoutException.
        /// </summary>
        /// <param name="regexInput">Matching timeout occured during mathing within the specified input.</param>
        /// <param name="regexPattern">Matching timeout occured during mathing to the specified pattern.</param>
        /// <param name="matchTimeout">Matching timeout occured becasue matching took longer than the specified timeout.</param>
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
    }
}
