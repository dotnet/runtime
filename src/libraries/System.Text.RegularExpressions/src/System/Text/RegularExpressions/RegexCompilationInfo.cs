// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    [Obsolete(Obsoletions.RegexCompileToAssemblyMessage, DiagnosticId = Obsoletions.RegexCompileToAssemblyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public class RegexCompilationInfo
    {
        private string _pattern;
        private string _name;
        private string _nspace;

        private TimeSpan _matchTimeout;

        public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic)
            : this(pattern, options, name, fullnamespace, ispublic, Regex.s_defaultMatchTimeout)
        {
        }

        public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic, TimeSpan matchTimeout)
        {
            Pattern = pattern;
            Name = name;
            Namespace = fullnamespace;
            Options = options;
            IsPublic = ispublic;
            MatchTimeout = matchTimeout;
        }

        public bool IsPublic { get; set; }

        public TimeSpan MatchTimeout
        {
            get => _matchTimeout;
            set
            {
                Regex.ValidateMatchTimeout(value);
                _matchTimeout = value;
            }
        }

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

        public RegexOptions Options { get; set; }

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
