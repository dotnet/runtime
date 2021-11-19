// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions.Symbolic;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents an immutable regular expression. Also contains static methods that
    /// allow use of regular expressions without instantiating a Regex explicitly.
    /// </summary>
    public partial class Regex : ISerializable
    {
        internal const int MaxOptionShift = 11;

        protected internal string? pattern;                   // The string pattern provided
        protected internal RegexOptions roptions;             // the top-level options from the options string
        protected internal RegexRunnerFactory? factory;       // Factory used to create runner instances for executing the regex
        protected internal Hashtable? caps;                   // if captures are sparse, this is the hashtable capnum->index
        protected internal Hashtable? capnames;               // if named captures are used, this maps names->index
        protected internal string[]? capslist;                // if captures are sparse or named captures are used, this is the sorted list of names
        protected internal int capsize;                       // the size of the capture array

        private WeakReference<RegexReplacement?>? _replref;   // cached parsed replacement pattern
        private volatile RegexRunner? _runner;                // cached runner
        private RegexCode? _code;                             // if interpreted, this is the code for RegexInterpreter

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
            // and NonBacktracking implementation if it's not used.
            Init(pattern, RegexOptions.None, s_defaultMatchTimeout, culture ?? CultureInfo.CurrentCulture);
        }

        internal Regex(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo? culture)
        {
            culture ??= RegexParser.GetTargetCulture(options);
            Init(pattern, options, matchTimeout, culture);

            if ((options & RegexOptions.NonBacktracking) != 0)
            {
                // If we're in non-backtracking mode, create the appropriate factory.
                factory = new SymbolicRegexRunnerFactory(_code, options, matchTimeout, culture);
                _code = null;
            }
            else if (RuntimeFeature.IsDynamicCodeCompiled && UseOptionC())
            {
                // If the compile option is set and compilation is supported, then compile the code.
                factory = Compile(pattern, _code, options, matchTimeout != InfiniteMatchTimeout);
                _code = null;
            }
        }

        /// <summary>Initializes the instance.</summary>
        /// <remarks>
        /// This is separated out of the constructor so that an app only using 'new Regex(pattern)'
        /// rather than 'new Regex(pattern, options)' can avoid statically referencing the Regex
        /// compiler, such that a tree shaker / linker can trim it away if it's not otherwise used.
        /// </remarks>
        [MemberNotNull(nameof(_code))]
        private void Init(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo culture)
        {
            ValidatePattern(pattern);
            ValidateOptions(options);
            ValidateMatchTimeout(matchTimeout);

            this.pattern = pattern;
            internalMatchTimeout = matchTimeout;
            roptions = options;

#if DEBUG
            if (IsDebug)
            {
                Debug.WriteLine($"Pattern: {pattern}    Options: {options & ~RegexOptions.Debug}    Timeout: {(matchTimeout == InfiniteMatchTimeout ? "infinite" : matchTimeout.ToString())}");
            }
#endif

            // Parse the input
            RegexTree tree = RegexParser.Parse(pattern, roptions, culture);

            // Generate the RegexCode from the node tree.  This is required for interpreting,
            // and is used as input into RegexOptions.Compiled and RegexOptions.NonBacktracking.
            _code = RegexWriter.Write(tree, culture);

            if ((options & RegexOptions.NonBacktracking) != 0)
            {
                // NonBacktracking doesn't support captures (other than the implicit top-level capture).
                capnames = null;
                capslist = null;
                caps = null;
                capsize = 1;
            }
            else
            {
                capnames = tree.CapNames;
                capslist = tree.CapsList;
                caps = _code.Caps;
                capsize = _code.CapSize;
            }
        }

        internal static void ValidatePattern(string pattern)
        {
            if (pattern is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pattern);
            }
        }

        internal static void ValidateOptions(RegexOptions options)
        {
            if (((((uint)options) >> MaxOptionShift) != 0) ||
                ((options & RegexOptions.ECMAScript) != 0 &&
                 (options & ~(RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.NonBacktracking |
#if DEBUG
                             RegexOptions.Debug |
#endif
                             RegexOptions.CultureInvariant)) != 0))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.options);
            }
        }

        /// <summary>
        /// Validates that the specified match timeout value is valid.
        /// The valid range is <code>TimeSpan.Zero &lt; matchTimeout &lt;= Regex.MaximumMatchTimeout</code>.
        /// </summary>
        /// <param name="matchTimeout">The timeout value to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the specified timeout is not within a valid range.</exception>
        protected internal static void ValidateMatchTimeout(TimeSpan matchTimeout)
        {
            // make sure timeout is positive but not longer then Environment.Ticks cycle length
            long matchTimeoutTicks = matchTimeout.Ticks;
            if (matchTimeoutTicks != InfiniteMatchTimeoutTicks && ((ulong)(matchTimeoutTicks - 1) >= MaximumMatchTimeoutTicks))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.matchTimeout);
            }
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
                if (value is null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
                }

                caps = value as Hashtable ?? new Hashtable(value);
            }
        }

        [CLSCompliant(false), DisallowNull]
        protected IDictionary? CapNames
        {
            get => capnames;
            set
            {
                if (value is null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
                }

                capnames = value as Hashtable ?? new Hashtable(value);
            }
        }

        /// <summary>
        /// This method is here for perf reasons: if the call to RegexCompiler is NOT in the
        /// Regex constructor, we don't load RegexCompiler and its reflection classes when
        /// instantiating a non-compiled regex.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RegexRunnerFactory Compile(string pattern, RegexCode code, RegexOptions options, bool hasTimeout) =>
            RegexCompiler.Compile(pattern, code, options, hasTimeout);

        [Obsolete(Obsoletions.RegexCompileToAssemblyMessage, DiagnosticId = Obsoletions.RegexCompileToAssemblyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname) =>
            CompileToAssembly(regexinfos, assemblyname, null, null);

        [Obsolete(Obsoletions.RegexCompileToAssemblyMessage, DiagnosticId = Obsoletions.RegexCompileToAssemblyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[]? attributes) =>
            CompileToAssembly(regexinfos, assemblyname, attributes, null);

        [Obsolete(Obsoletions.RegexCompileToAssemblyMessage, DiagnosticId = Obsoletions.RegexCompileToAssemblyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[]? attributes, string? resourceFile) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);

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
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }

            return RegexParser.Escape(str);
        }

        /// <summary>
        /// Unescapes any escaped characters in the input string.
        /// </summary>
        public static string Unescape(string str)
        {
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }

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

        /// <summary>
        /// Returns the GroupNameCollection for the regular expression. This collection contains the
        /// set of strings used to name capturing groups in the expression.
        /// </summary>
        public string[] GetGroupNames()
        {
            string[] result;

            if (capslist is null)
            {
                result = new string[capsize];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = ((uint)i).ToString();
                }
            }
            else
            {
                result = capslist.AsSpan().ToArray();
            }

            return result;
        }

        /// <summary>
        /// Returns the integer group number corresponding to a group name.
        /// </summary>
        public int[] GetGroupNumbers()
        {
            int[] result;

            if (caps is null)
            {
                result = new int[capsize];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = i;
                }
            }
            else
            {
                // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                result = new int[caps.Count];
                IDictionaryEnumerator de = caps.GetEnumerator();
                while (de.MoveNext())
                {
                    result[(int)de.Value!] = (int)de.Key;
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves a group name that corresponds to a group number.
        /// </summary>
        public string GroupNameFromNumber(int i)
        {
            if (capslist is null)
            {
                return (uint)i < (uint)capsize ?
                    ((uint)i).ToString() :
                    string.Empty;
            }
            else
            {
                return caps != null && !caps.TryGetValue(i, out i) ? string.Empty :
                    (uint)i < (uint)capslist.Length ? capslist[i] :
                    string.Empty;
            }
        }

        /// <summary>
        /// Returns a group number that corresponds to a group name, or -1 if the name is not a recognized group name.
        /// </summary>
        public int GroupNumberFromName(string name)
        {
            if (name is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
            }

            if (capnames != null)
            {
                // Look up name if we have a hashtable of names.
                return capnames.TryGetValue(name, out int result) ? result : -1;
            }
            else
            {
                // Otherwise, try to parse it as a number.
                return uint.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out uint result) && result < capsize ? (int)result : -1;
            }
        }

        /// <summary>A weak reference to a regex replacement, lazily initialized.</summary>
        internal WeakReference<RegexReplacement?> RegexReplacementWeakReference =>
            _replref ??
            Interlocked.CompareExchange(ref _replref, new WeakReference<RegexReplacement?>(null), null) ??
            _replref;

        protected void InitializeReferences()
        {
            // This method no longer has anything to initialize. It continues to exist
            // purely for API compat, as it was originally shipped as protected, with
            // assemblies generated by Regex.CompileToAssembly calling it.
        }

        /// <summary>Internal worker called by the public APIs</summary>
        internal Match? Run(bool quick, int prevlen, string input, int beginning, int length, int startat)
        {
            if ((uint)startat > (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
            }
            if ((uint)length > (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.LengthNotNegative);
            }

            RegexRunner runner = Interlocked.Exchange(ref _runner, null) ?? CreateRunner();
            try
            {
                // Do the scan starting at the requested position
                Match? match = runner.Scan(this, input, beginning, beginning + length, startat, prevlen, quick, internalMatchTimeout);
#if DEBUG
                if (IsDebug) match?.Dump();
#endif
                return match;
            }
            finally
            {
                _runner = runner;
            }
        }

        internal void Run<TState>(string input, int startat, ref TState state, MatchCallback<TState> callback, bool reuseMatchObject)
        {
            Debug.Assert((uint)startat <= (uint)input.Length);
            RegexRunner runner = Interlocked.Exchange(ref _runner, null) ?? CreateRunner();
            try
            {
                runner.ScanInternal(this, input, startat, ref state, callback, reuseMatchObject, internalMatchTimeout);
            }
            finally
            {
                _runner = runner;
            }
        }

        /// <summary>Creates a new runner instance.</summary>
        private RegexRunner CreateRunner() =>
            factory?.CreateInstance() ??
            new RegexInterpreter(_code!, RegexParser.GetTargetCulture(roptions));

        /// <summary>True if the <see cref="RegexOptions.Compiled"/> option was set.</summary>
        protected bool UseOptionC() => (roptions & RegexOptions.Compiled) != 0;

        /// <summary>True if the <see cref="RegexOptions.RightToLeft"/> option was set.</summary>
        protected internal bool UseOptionR() => (roptions & RegexOptions.RightToLeft) != 0;
    }
}
