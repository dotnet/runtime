// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// The exception that is thrown when the execution time of a regular expression pattern-matching
    /// method exceeds its time-out interval.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The presence of a <see cref="RegexMatchTimeoutException"/> exception generally indicates one
    /// of the following conditions:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///       The regular expression engine is backtracking excessively as it attempts to match the input
    ///       text to the regular expression pattern.
    ///   </item>
    ///   <item>
    ///       The time-out interval has been set too low, especially given high machine load.
    ///   </item>
    /// </list>
    /// <para>
    /// The way in which an exception handler handles an exception depends on the cause of the exception:
    /// </para>
    /// <para>
    /// If the time-out results from excessive backtracking, your exception handler should abandon the
    /// attempt to match the input and inform the user that a time-out has occurred in the regular
    /// expression pattern-matching method. If possible, information about the regular expression pattern,
    /// which is available from the <see cref="Pattern"/> property, and the input that caused excessive
    /// backtracking, which is available from the <see cref="Input"/> property, should be logged so that
    /// the issue can be investigated and the regular expression pattern modified. Time-outs due to
    /// excessive backtracking are always reproducible.
    /// </para>
    /// <para>
    /// If the time-out results from setting the time-out threshold too low, you can increase the time-out
    /// interval and retry the matching operation. The current time-out interval is available from the
    /// <see cref="MatchTimeout"/> property. When a <see cref="RegexMatchTimeoutException"/> exception is
    /// thrown, the regular expression engine maintains its state so that any future invocations return the
    /// same result, as if the exception did not occur. The recommended pattern is to wait for a brief,
    /// random time interval after the exception is thrown before calling the matching method again. This
    /// can be repeated several times. However, the number of repetitions should be small in case the
    /// time-out is caused by excessive backtracking.
    /// </para>
    /// </remarks>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class RegexMatchTimeoutException : TimeoutException, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegexMatchTimeoutException"/> class with
        /// information about the regular expression pattern, the input text, and the time-out interval.
        /// </summary>
        /// <param name="regexInput">The input text processed by the regular expression engine when the
        /// time-out occurred.</param>
        /// <param name="regexPattern">The pattern used by the regular expression engine when the time-out
        /// occurred.</param>
        /// <param name="matchTimeout">The time-out interval.</param>
        /// <remarks>
        /// The <paramref name="regexInput"/>, <paramref name="regexPattern"/>, and
        /// <paramref name="matchTimeout"/> values are assigned to the <see cref="Input"/>,
        /// <see cref="Pattern"/>, and <see cref="MatchTimeout"/> properties of the new
        /// <see cref="RegexMatchTimeoutException"/> object.
        /// </remarks>
        public RegexMatchTimeoutException(string regexInput, string regexPattern, TimeSpan matchTimeout)
            : base(SR.RegexMatchTimeoutException_Occurred)
        {
            Input = regexInput;
            Pattern = regexPattern;
            MatchTimeout = matchTimeout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexMatchTimeoutException"/> class with a
        /// system-supplied message.
        /// </summary>
        /// <remarks>
        /// This constructor initializes the <see cref="Exception.Message"/> property of the new instance
        /// to a system-supplied message that describes the error. This message is localized for the
        /// current system culture.
        /// </remarks>
        public RegexMatchTimeoutException() : base(SR.RegexMatchTimeoutException_Occurred) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexMatchTimeoutException"/> class with the
        /// specified message string.
        /// </summary>
        /// <param name="message">A string that describes the exception.</param>
        /// <remarks>
        /// The <paramref name="message"/> string is assigned to the
        /// <see cref="Exception.Message"/> property. The string should be localized for the current
        /// culture.
        /// </remarks>
        public RegexMatchTimeoutException(string message) : base(message ?? SR.RegexMatchTimeoutException_Occurred) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexMatchTimeoutException"/> class with a
        /// specified error message and a reference to the inner exception that is the cause of this
        /// exception.
        /// </summary>
        /// <param name="message">A string that describes the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        /// <remarks>
        /// <para>
        /// Typically, you use this overload to handle an exception in a <c>try</c>/<c>catch</c> block.
        /// The <paramref name="inner"/> parameter should be a reference to the exception object handled
        /// in the <c>catch</c> block, or it can be <see langword="null"/>. This value is then assigned
        /// to the <see cref="Exception.InnerException"/> property.
        /// </para>
        /// <para>
        /// The <paramref name="message"/> string is assigned to the
        /// <see cref="Exception.Message"/> property. The string should be localized for the current
        /// culture.
        /// </para>
        /// </remarks>
        public RegexMatchTimeoutException(string message, Exception inner) : base(message ?? SR.RegexMatchTimeoutException_Occurred, inner) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexMatchTimeoutException"/> class with
        /// serialized data.
        /// </summary>
        /// <param name="info">The object that contains the serialized data.</param>
        /// <param name="context">The stream that contains the serialized data.</param>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected RegexMatchTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Input = info.GetString("regexInput")!;
            Pattern = info.GetString("regexPattern")!;
            MatchTimeout = new TimeSpan(info.GetInt64("timeoutTicks"));
        }

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> object with the data needed to serialize a
        /// <see cref="RegexMatchTimeoutException"/> object.
        /// </summary>
        /// <param name="info">The serialization information object to populate with data.</param>
        /// <param name="context">The destination for this serialization.</param>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("regexInput", Input);
            info.AddValue("regexPattern", Pattern);
            info.AddValue("timeoutTicks", MatchTimeout.Ticks);
        }

        /// <summary>
        /// Gets the input text that the regular expression engine was processing when the time-out
        /// occurred.
        /// </summary>
        /// <value>The regular expression input text.</value>
        /// <remarks>
        /// <para>
        /// This property reflects the value of the <c>regexInput</c> parameter of the
        /// <see cref="RegexMatchTimeoutException(string, string, TimeSpan)"/> constructor. If this
        /// parameter is not explicitly initialized in a constructor call, its value is
        /// <see cref="string.Empty"/>.
        /// </para>
        /// <para>
        /// When the exception is thrown by the regular expression engine, the value of the
        /// <see cref="Input"/> property reflects the entire input string passed to the regular
        /// expression engine. It does not reflect a partial string, such as the substring that the
        /// engine searches in the call to a method such as
        /// <see cref="Regex.Match(string, int)"/>.
        /// </para>
        /// </remarks>
        public string Input { get; } = string.Empty;

        /// <summary>
        /// Gets the regular expression pattern that was used in the matching operation when the time-out
        /// occurred.
        /// </summary>
        /// <value>The regular expression pattern.</value>
        /// <remarks>
        /// This property reflects the value of the <c>regexPattern</c> parameter of the
        /// <see cref="RegexMatchTimeoutException(string, string, TimeSpan)"/> constructor. If the
        /// parameter is not properly initialized in a constructor call, its value is
        /// <see cref="string.Empty"/>.
        /// </remarks>
        public string Pattern { get; } = string.Empty;

        /// <summary>Gets the time-out interval for a regular expression match.</summary>
        /// <value>The time-out interval.</value>
        /// <remarks>
        /// <para>
        /// This property reflects the value of the <c>matchTimeout</c> parameter of the
        /// <see cref="RegexMatchTimeoutException(string, string, TimeSpan)"/> constructor. If the
        /// parameter is not properly initialized in a constructor call, its value is
        /// <c>TimeSpan.FromTicks(-1)</c>.
        /// </para>
        /// <para>
        /// The value of this property reflects the time-out interval set in the call to the
        /// <see cref="Regex"/> constructor or static method. It does not reflect the exact interval
        /// that has elapsed from the beginning of the method call to the time the exception is thrown.
        /// </para>
        /// </remarks>
        public TimeSpan MatchTimeout { get; } = TimeSpan.FromTicks(-1);
    }
}
