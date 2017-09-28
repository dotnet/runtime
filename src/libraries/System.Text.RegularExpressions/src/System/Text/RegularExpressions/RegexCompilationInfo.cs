// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.RegularExpressions
{
    public class RegexCompilationInfo
    {
        private string _pattern;
        private string _name;
        private string _nspace;
        private TimeSpan _matchTimeout;

        public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic)
            : this(pattern, options, name, fullnamespace, ispublic, Regex.DefaultMatchTimeout)
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
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(Name));
                }

                if (value.Length == 0)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidEmptyArgument, nameof(Name)), nameof(Name));
                }

                _name = value;
            }
        }

        public string Namespace
        {
            get => _nspace;
            set
            {
                _nspace = value ?? throw new ArgumentNullException(nameof(Namespace));
            }
        }

        public RegexOptions Options { get; set; }

        public string Pattern
        {
            get => _pattern;
            set => _pattern = value ?? throw new ArgumentNullException(nameof(Pattern));
        }
    }
}
