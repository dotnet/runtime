// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Provides information about a regular expression that is used to compile a regular expression to a stand-alone assembly.
    /// </summary>
    [Obsolete(Obsoletions.RegexCompileToAssemblyMessage, DiagnosticId = Obsoletions.RegexCompileToAssemblyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public class RegexCompilationInfo
    {
        private string _pattern;
        private string _name;
        private string _nspace;

        private TimeSpan _matchTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexCompilationInfo"/> class that contains information
        /// about a regular expression to be included in an assembly.
        /// </summary>
        /// <param name="pattern">The regular expression to compile.</param>
        /// <param name="options">The regular expression options to use when compiling the regular expression.</param>
        /// <param name="name">The name of the type that represents the compiled regular expression.</param>
        /// <param name="fullnamespace">The namespace to which the new type belongs.</param>
        /// <param name="ispublic">
        /// <see langword="true"/> to make the compiled regular expression publicly visible; otherwise, <see langword="false"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="pattern"/> is <see langword="null"/>, or <paramref name="name"/> is <see langword="null"/>,
        /// or <paramref name="fullnamespace"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see cref="string.Empty"/>.</exception>
        public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic)
            : this(pattern, options, name, fullnamespace, ispublic, Regex.s_defaultMatchTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexCompilationInfo"/> class that contains information
        /// about a regular expression with a specified time-out value to be included in an assembly.
        /// </summary>
        /// <param name="pattern">The regular expression to compile.</param>
        /// <param name="options">The regular expression options to use when compiling the regular expression.</param>
        /// <param name="name">The name of the type that represents the compiled regular expression.</param>
        /// <param name="fullnamespace">The namespace to which the new type belongs.</param>
        /// <param name="ispublic">
        /// <see langword="true"/> to make the compiled regular expression publicly visible; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="matchTimeout">The default time-out interval for the regular expression.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="pattern"/> is <see langword="null"/>, or <paramref name="name"/> is <see langword="null"/>,
        /// or <paramref name="fullnamespace"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see cref="string.Empty"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic, TimeSpan matchTimeout)
        {
            Pattern = pattern;
            Name = name;
            Namespace = fullnamespace;
            Options = options;
            IsPublic = ispublic;
            MatchTimeout = matchTimeout;
        }

        /// <summary>Gets or sets a value that indicates whether the compiled regular expression has public visibility.</summary>
        /// <value>
        /// <see langword="true"/> if the regular expression has public visibility; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsPublic { get; set; }

        /// <summary>Gets or sets the regular expression's default time-out interval.</summary>
        /// <value>
        /// The default maximum time interval that can elapse in a pattern-matching operation before a
        /// <see cref="RegexMatchTimeoutException"/> is thrown, or <see cref="Regex.InfiniteMatchTimeout"/> if time-outs are disabled.
        /// </value>
        public TimeSpan MatchTimeout
        {
            get => _matchTimeout;
            set
            {
                Regex.ValidateMatchTimeout(value);
                _matchTimeout = value;
            }
        }

        /// <summary>Gets or sets the name of the type that represents the compiled regular expression.</summary>
        /// <value>The name of the new type.</value>
        /// <exception cref="ArgumentNullException">The value for this property is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The value for this property is an empty string.</exception>
        public string Name
        {
            get => _name;
            [MemberNotNull(nameof(_name))]
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(value, nameof(Name));
                _name = value;
            }
        }

        /// <summary>Gets or sets the namespace to which the new type belongs.</summary>
        /// <value>The namespace of the new type.</value>
        /// <exception cref="ArgumentNullException">The value for this property is <see langword="null"/>.</exception>
        public string Namespace
        {
            get => _nspace;
            [MemberNotNull(nameof(_nspace))]
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Namespace));
                _nspace = value;
            }
        }

        /// <summary>Gets or sets the options to use when compiling the regular expression.</summary>
        /// <value>A bitwise combination of the enumeration values.</value>
        public RegexOptions Options { get; set; }

        /// <summary>Gets or sets the regular expression to compile.</summary>
        /// <value>The regular expression to compile.</value>
        /// <exception cref="ArgumentNullException">The value for this property is <see langword="null"/>.</exception>
        public string Pattern
        {
            get => _pattern;
            [MemberNotNull(nameof(_pattern))]
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Pattern));
                _pattern = value;
            }
        }
    }
}
