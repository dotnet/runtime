// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
#if FEATURE_COMPILED
using System.Runtime.CompilerServices;
#endif
using System.Runtime.Serialization;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents an immutable regular expression. Also contains static methods that
    /// allow use of regular expressions without instantiating a Regex explicitly.
    /// </summary>
    public partial class Regex : ISerializable
    {
        internal const int MaxOptionShift = 10;

        protected internal string? pattern;                   // The string pattern provided
        protected internal RegexOptions roptions;             // the top-level options from the options string
        protected internal RegexRunnerFactory? factory;
        protected internal Hashtable? caps;                   // if captures are sparse, this is the hashtable capnum->index
        protected internal Hashtable? capnames;               // if named captures are used, this maps names->index
        protected internal string[]? capslist;                // if captures are sparse or named captures are used, this is the sorted list of names
        protected internal int capsize;                       // the size of the capture array

        internal WeakReference<RegexReplacement?>? _replref;  // cached parsed replacement pattern
        private volatile RegexRunner? _runner;                // cached runner
        private RegexCode? _code;                             // if interpreted, this is the code for RegexInterpreter
        private bool _refsInitialized = false;

        protected Regex()
        {
            internalMatchTimeout = s_defaultMatchTimeout;
        }

        /// <summary>
        /// Creates a regular expression object for the specified regular expression.
        /// </summary>
        public Regex(string pattern) :
            this(pattern, culture: null)
        {
        }

        /// <summary>
        /// Creates a regular expression object for the specified regular expression, with options that modify the pattern.
        /// </summary>
        public Regex(string pattern, RegexOptions options) :
            this(pattern, options, s_defaultMatchTimeout, culture: null)
        {
        }

        public Regex(string pattern, RegexOptions options, TimeSpan matchTimeout) :
            this(pattern, options, matchTimeout, culture: null)
        {
        }

        internal Regex(string pattern, CultureInfo? culture)
        {
            // Call Init directly rather than delegating to a Regex ctor that takes
            // options to enable linking / tree shaking to remove the Regex compiler
            // if it may not be used.
            Init(pattern, RegexOptions.None, s_defaultMatchTimeout, culture);
        }

        internal Regex(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo? culture)
        {
            Init(pattern, options, matchTimeout, culture);

#if FEATURE_COMPILED
            // if the compile option is set, then compile the code
            if (UseOptionC())
            {
                // Storing into this Regex's factory will also implicitly update the cache
                factory = Compile(_code!, options, matchTimeout != InfiniteMatchTimeout);
                _code = null;
            }
#endif
        }

        /// <summary>Initializes the instance.</summary>
        /// <remarks>
        /// This is separated out of the constructor so that an app only using 'new Regex(pattern)'
        /// rather than 'new Regex(pattern, options)' can avoid statically referencing the Regex
        /// compiler, such that a tree shaker / linker can trim it away if it's not otherwise used.
        /// </remarks>
        private void Init(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo? culture)
        {
            ValidatePattern(pattern);
            ValidateOptions(options);
            ValidateMatchTimeout(matchTimeout);

            this.pattern = pattern;
            roptions = options;
            internalMatchTimeout = matchTimeout;

            // Parse the input
            RegexTree tree = RegexParser.Parse(pattern, roptions, culture ?? ((options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture));

            // Extract the relevant information
            capnames = tree.CapNames;
            capslist = tree.CapsList;
            _code = RegexWriter.Write(tree);
            caps = _code.Caps;
            capsize = _code.CapSize;

            InitializeReferences();
        }

        internal static void ValidatePattern(string pattern)
        {
            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }
        }

        internal static void ValidateOptions(RegexOptions options)
        {
            if (options < RegexOptions.None || (((int)options) >> MaxOptionShift) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if ((options & RegexOptions.ECMAScript) != 0 &&
                (options & ~(RegexOptions.ECMAScript |
                             RegexOptions.IgnoreCase |
                             RegexOptions.Multiline |
                             RegexOptions.Compiled |
#if DEBUG
                             RegexOptions.Debug |
#endif
                             RegexOptions.CultureInvariant)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }
        }

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

            // make sure timeout is not longer then Environment.Ticks cycle length:
            if (TimeSpan.Zero < matchTimeout && matchTimeout <= s_maximumMatchTimeout)
                return;

            throw new ArgumentOutOfRangeException(nameof(matchTimeout));
        }

        protected Regex(SerializationInfo info, StreamingContext context) =>
            throw new PlatformNotSupportedException();

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context) =>
            throw new PlatformNotSupportedException();

        [CLSCompliant(false), DisallowNull]
        protected IDictionary? Caps
        {
            get => caps;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                caps = value as Hashtable ?? new Hashtable(value);
            }
        }

        [CLSCompliant(false), DisallowNull]
        protected IDictionary? CapNames
        {
            get => capnames;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                capnames = value as Hashtable ?? new Hashtable(value);
            }
        }

#if FEATURE_COMPILED
        /// <summary>
        /// This method is here for perf reasons: if the call to RegexCompiler is NOT in the
        /// Regex constructor, we don't load RegexCompiler and its reflection classes when
        /// instantiating a non-compiled regex.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RegexRunnerFactory Compile(RegexCode code, RegexOptions options, bool hasTimeout)
        {
            return RegexCompiler.Compile(code, options, hasTimeout);
        }
#endif

        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
        }

        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[] attributes)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
        }

        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[] attributes, string resourceFile)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
        }

        /// <summary>
        /// Escapes a minimal set of metacharacters (\, *, +, ?, |, {, [, (, ), ^, $, ., #, and
        /// whitespace) by replacing them with their \ codes. This converts a string so that
        /// it can be used as a constant within a regular expression safely. (Note that the
        /// reason # and whitespace must be escaped is so the string can be used safely
        /// within an expression parsed with x mode. If future Regex features add
        /// additional metacharacters, developers should depend on Escape to escape those
        /// characters as well.)
        /// </summary>
        public static string Escape(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            return RegexParser.Escape(str);
        }

        /// <summary>
        /// Unescapes any escaped characters in the input string.
        /// </summary>
        public static string Unescape(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            return RegexParser.Unescape(str);
        }

        /// <summary>
        /// Returns the options passed into the constructor
        /// </summary>
        public RegexOptions Options => roptions;

        /// <summary>
        /// Indicates whether the regular expression matches from right to left.
        /// </summary>
        public bool RightToLeft => UseOptionR();

        /// <summary>
        /// Returns the regular expression pattern passed into the constructor
        /// </summary>
        public override string ToString() => pattern!;

        /*
         * Returns an array of the group names that are used to capture groups
         * in the regular expression. Only needed if the regex is not known until
         * runtime, and one wants to extract captured groups. (Probably unusual,
         * but supplied for completeness.)
         */
        /// <summary>
        /// Returns the GroupNameCollection for the regular expression. This collection contains the
        /// set of strings used to name capturing groups in the expression.
        /// </summary>
        public string[] GetGroupNames()
        {
            string[] result;

            if (capslist == null)
            {
                result = new string[capsize];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = i.ToString();
                }
            }
            else
            {
                result = capslist.AsSpan().ToArray();
            }

            return result;
        }

        /*
         * Returns an array of the group numbers that are used to capture groups
         * in the regular expression. Only needed if the regex is not known until
         * runtime, and one wants to extract captured groups. (Probably unusual,
         * but supplied for completeness.)
         */
        /// <summary>
        /// Returns the integer group number corresponding to a group name.
        /// </summary>
        public int[] GetGroupNumbers()
        {
            int[] result;

            if (caps == null)
            {
                int max = capsize;
                result = new int[max];

                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = i;
                }
            }
            else
            {
                result = new int[caps.Count];

                // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                IDictionaryEnumerator de = caps.GetEnumerator();
                while (de.MoveNext())
                {
                    result[(int)de.Value!] = (int)de.Key;
                }
            }

            return result;
        }

        /*
         * Given a group number, maps it to a group name. Note that numbered
         * groups automatically get a group name that is the decimal string
         * equivalent of its number.
         *
         * Returns null if the number is not a recognized group number.
         */
        /// <summary>
        /// Retrieves a group name that corresponds to a group number.
        /// </summary>
        public string GroupNameFromNumber(int i)
        {
            if (capslist == null)
            {
                if (i >= 0 && i < capsize)
                    return i.ToString();

                return string.Empty;
            }
            else
            {
                if (caps != null)
                {
                    if (!caps.TryGetValue(i, out i))
                        return string.Empty;
                }

                if (i >= 0 && i < capslist.Length)
                    return capslist[i];

                return string.Empty;
            }
        }

        /*
         * Given a group name, maps it to a group number. Note that numbered
         * groups automatically get a group name that is the decimal string
         * equivalent of its number.
         *
         * Returns -1 if the name is not a recognized group name.
         */
        /// <summary>
        /// Returns a group number that corresponds to a group name.
        /// </summary>
        public int GroupNumberFromName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            int result;

            // look up name if we have a hashtable of names
            if (capnames != null)
            {
                return capnames.TryGetValue(name, out result) ? result : -1;
            }

            // convert to an int if it looks like a number
            result = 0;
            for (int i = 0; i < name.Length; i++)
            {
                uint digit = (uint)(name[i] - '0');
                if (digit > 9)
                {
                    return -1;
                }

                result = (result * 10) + (int)digit;
            }

            // return int if it's in range
            return result >= 0 && result < capsize ? result : -1;
        }

        protected void InitializeReferences()
        {
            if (_refsInitialized)
                throw new NotSupportedException(SR.OnlyAllowedOnce);

            _refsInitialized = true;
            _replref = new WeakReference<RegexReplacement?>(null);
        }

        /// <summary>
        /// Internal worker called by all the public APIs
        /// </summary>
        /// <returns></returns>
        internal Match? Run(bool quick, int prevlen, string input, int beginning, int length, int startat)
        {
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            if (length < 0 || length > input.Length)
                throw new ArgumentOutOfRangeException(nameof(length), SR.LengthNotNegative);

            RegexRunner runner =
                Interlocked.Exchange(ref _runner, null) ?? // use a cached runner if there is one
                (factory != null ? factory.CreateInstance() : // use the compiled RegexRunner factory if there is one
                 new RegexInterpreter(_code!, UseOptionInvariant() ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture));
            try
            {
                // Do the scan starting at the requested position
                Match? match = runner.Scan(this, input, beginning, beginning + length, startat, prevlen, quick, internalMatchTimeout);
#if DEBUG
                if (Debug) match?.Dump();
#endif
                return match;
            }
            finally
            {
                // Release the runner back to the cache
                _runner = runner;
            }
        }

        protected bool UseOptionC() => (roptions & RegexOptions.Compiled) != 0;

        /// <summary>True if the L option was set</summary>
        protected internal bool UseOptionR() => (roptions & RegexOptions.RightToLeft) != 0;

        internal bool UseOptionInvariant() => (roptions & RegexOptions.CultureInvariant) != 0;

#if DEBUG
        /// <summary>
        /// True if the regex has debugging enabled
        /// </summary>
        [ExcludeFromCodeCoverage]
        internal bool Debug => (roptions & RegexOptions.Debug) != 0;
#endif
    }
}
