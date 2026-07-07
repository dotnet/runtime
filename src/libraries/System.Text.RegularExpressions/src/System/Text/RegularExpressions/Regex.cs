// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
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
    /// <remarks>
    /// <para>
    /// The <see cref="Regex"/> class represents the .NET regular expression engine.
    /// It can be used to quickly parse large amounts of text to find specific character patterns;
    /// to extract, edit, replace, or delete text substrings; and to add the extracted strings to a
    /// collection to generate a report.
    /// </para>
    /// </remarks>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference">.NET Regular Expression Language</related>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">Regular Expression Options</related>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/standard/base-types/best-practices">Best Practices for Regular Expressions</related>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/standard/base-types/backtracking-in-regular-expressions">Backtracking</related>
    public partial class Regex : ISerializable
    {
        /// <summary>The regular expression pattern that was passed to the constructor.</summary>
        [StringSyntax(StringSyntaxAttribute.Regex)]
        protected internal string? pattern;

        /// <summary>The regular expression options that were passed to the constructor.</summary>
        protected internal RegexOptions roptions;

        /// <summary>A factory used to create <see cref="RegexRunner"/> instances for executing the regular expression.</summary>
        protected internal RegexRunnerFactory? factory;

        /// <summary>
        /// When captures are sparse, maps capture numbers to their corresponding index
        /// in the capture array. Otherwise, <see langword="null"/>.
        /// </summary>
        protected internal Hashtable? caps;

        /// <summary>
        /// When named captures are used, maps capture names to their corresponding index.
        /// Otherwise, <see langword="null"/>.
        /// </summary>
        protected internal Hashtable? capnames;

        /// <summary>
        /// When captures are sparse or named captures are used, contains the sorted list of capture names.
        /// Otherwise, <see langword="null"/>.
        /// </summary>
        protected internal string[]? capslist;

        /// <summary>The number of capturing groups defined in the regular expression pattern.</summary>
        protected internal int capsize;

        private volatile RegexRunner? _runner;                // cached runner

        /// <summary>Initializes a new instance of the <see cref="Regex" /> class.</summary>
#if DEBUG
        // These members aren't used from Regex(), but we want to keep them in debug builds for now,
        // so this is a convenient place to include them rather than needing a debug-only illink file.
        [DynamicDependency(nameof(SaveDGML))]
        [DynamicDependency(nameof(GenerateUnicodeTables))]
        [DynamicDependency(nameof(SampleMatches))]
        [DynamicDependency(nameof(Explore))]
#endif
        protected Regex()
        {
            internalMatchTimeout = s_defaultMatchTimeout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Regex" /> class for the specified regular expression.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pattern" /> is <see langword="null" />.</exception>
        public Regex([StringSyntax(StringSyntaxAttribute.Regex)] string pattern) :
            this(pattern, culture: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Regex" /> class for the specified regular expression,
        /// with options that modify the pattern.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pattern" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options" /> is not a valid <see cref="RegexOptions" /> value.
        /// </exception>
        public Regex([StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) :
            this(pattern, options, s_defaultMatchTimeout, culture: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Regex" /> class for the specified regular expression,
        /// with options that modify the pattern and a value that specifies how long a pattern matching method
        /// should attempt a match before it times out.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
        /// <param name="matchTimeout">
        /// A time-out interval, or <see cref="InfiniteMatchTimeout" /> to indicate that the method should not time out.
        /// </param>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pattern" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options" /> is not a valid <see cref="RegexOptions" /> value, or
        /// <paramref name="matchTimeout" /> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        public Regex([StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) :
            this(pattern, options, matchTimeout, culture: null)
        {
        }

        internal Regex(string pattern, CultureInfo? culture)
        {
            // Validate arguments.
            ValidatePattern(pattern);

            // Parse and store the argument information.
            RegexTree tree = Init(pattern, RegexOptions.None, s_defaultMatchTimeout, ref culture);

            // Create the interpreter factory.
            factory = new RegexInterpreterFactory(tree);

            // NOTE: This overload _does not_ delegate to the one that takes options, in order
            // to avoid unnecessarily rooting the support for RegexOptions.NonBacktracking/Compiler
            // if no options are ever used.
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Compiled Regex is only used when RuntimeFeature.IsDynamicCodeCompiled is true. Workaround https://github.com/dotnet/linker/issues/2715.")]
        internal Regex(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo? culture)
        {
            // Validate arguments.
            ValidatePattern(pattern);
            ValidateOptions(options);
            ValidateMatchTimeout(matchTimeout);

            // Parse and store the argument information.
            RegexTree tree = Init(pattern, options, matchTimeout, ref culture);

            // Create the appropriate factory.
            if ((options & RegexOptions.NonBacktracking) != 0)
            {
                // If we're in non-backtracking mode, create the appropriate factory.
                factory = new SymbolicRegexRunnerFactory(tree, options, matchTimeout);
            }
            else
            {
                if (RuntimeFeature.IsDynamicCodeCompiled && (options & RegexOptions.Compiled) != 0)
                {
                    // If the compile option is set and compilation is supported, then compile the code.
                    // If the compiler can't compile this regex, it'll return null, and we'll fall back
                    // to the interpreter.
                    factory = Compile(pattern, tree, options, matchTimeout != InfiniteMatchTimeout);
                }

                // If no factory was created, fall back to creating one for the interpreter.
                factory ??= new RegexInterpreterFactory(tree);
            }
        }

        /// <summary>Stores the supplied arguments and capture information, returning the parsed expression.</summary>
        private RegexTree Init(string pattern, RegexOptions options, TimeSpan matchTimeout, [NotNull] ref CultureInfo? culture)
        {
            this.pattern = pattern;
            roptions = options;
            internalMatchTimeout = matchTimeout;
            culture ??= RegexParser.GetTargetCulture(options);

            // Parse the pattern.
            RegexTree tree = RegexParser.Parse(pattern, options, culture);

            // Store the relevant information, constructing the appropriate factory.
            capnames = tree.CaptureNameToNumberMapping;
            capslist = tree.CaptureNames;
            caps = tree.CaptureNumberSparseMapping;
            capsize = tree.CaptureCount;

            return tree;
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
            const int MaxOptionShift = 12;
            if (((((uint)options) >> MaxOptionShift) != 0) ||
                ((options & RegexOptions.ECMAScript) != 0 && (options & ~(RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant)) != 0) ||
                ((options & RegexOptions.NonBacktracking) != 0 && (options & (RegexOptions.ECMAScript | RegexOptions.RightToLeft | RegexOptions.AnyNewLine)) != 0))
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
            long matchTimeoutTicks = matchTimeout.Ticks;
            if (matchTimeoutTicks != InfiniteMatchTimeoutTicks && ((ulong)(matchTimeoutTicks - 1) >= MaximumMatchTimeoutTicks))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.matchTimeout);
            }
        }

        /// <summary>Initializes a new instance of the <see cref="Regex" /> class by using serialized data.</summary>
        /// <param name="info">The object that contains a serialized pattern and <see cref="RegexOptions" /> information.</param>
        /// <param name="context">The destination for this serialization. (This parameter is not used; specify <see langword="null" />.)</param>
        /// <exception cref="PlatformNotSupportedException">Serialization of <see cref="Regex" /> objects is not supported.</exception>
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Regex(SerializationInfo info, StreamingContext context) =>
            throw new PlatformNotSupportedException();

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context) =>
            throw new PlatformNotSupportedException();

        /// <summary>Gets or sets a dictionary that maps numbered capturing groups to their index values.</summary>
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

        /// <summary>Gets or sets a dictionary that maps named capturing groups to their index values.</summary>
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
        [RequiresDynamicCode("Compiling a RegEx requires dynamic code.")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RegexRunnerFactory? Compile(string pattern, RegexTree regexTree, RegexOptions options, bool hasTimeout) =>
            RegexCompiler.Compile(pattern, regexTree, options, hasTimeout);

        /// <summary>
        /// Compiles one or more specified <see cref="Regex" /> objects to a named assembly.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        /// Creating an assembly of compiled regular expressions is not supported.
        /// </exception>
        [Obsolete(Obsoletions.RegexCompileToAssemblyMessage, DiagnosticId = Obsoletions.RegexCompileToAssemblyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname) =>
            CompileToAssembly(regexinfos, assemblyname, null, null);

        /// <summary>
        /// Compiles one or more specified <see cref="Regex" /> objects to a named assembly with the specified attributes.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        /// Creating an assembly of compiled regular expressions is not supported.
        /// </exception>
        [Obsolete(Obsoletions.RegexCompileToAssemblyMessage, DiagnosticId = Obsoletions.RegexCompileToAssemblyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[]? attributes) =>
            CompileToAssembly(regexinfos, assemblyname, attributes, null);

        /// <summary>
        /// Compiles one or more specified <see cref="Regex" /> objects and a specified resource file
        /// to a named assembly with the specified attributes.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        /// Creating an assembly of compiled regular expressions is not supported.
        /// </exception>
        [Obsolete(Obsoletions.RegexCompileToAssemblyMessage, DiagnosticId = Obsoletions.RegexCompileToAssemblyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[]? attributes, string? resourceFile)
        {
#if DEBUG
            // This code exists only to help with the development of the RegexCompiler.
            // .NET no longer supports CompileToAssembly; the source generator should be used instead.
#pragma warning disable IL3050
            ArgumentNullException.ThrowIfNull(assemblyname);
            ArgumentNullException.ThrowIfNull(regexinfos);

            var c = new RegexAssemblyCompiler(assemblyname, attributes, resourceFile);

            for (int i = 0; i < regexinfos.Length; i++)
            {
                ArgumentNullException.ThrowIfNull(regexinfos[i]);

                string pattern = regexinfos[i].Pattern;

                RegexOptions options = regexinfos[i].Options | RegexOptions.Compiled; // ensure compiled is set; it enables more optimization specific to compilation

                string fullname = regexinfos[i].Namespace.Length == 0 ?
                    regexinfos[i].Name :
                    regexinfos[i].Namespace + "." + regexinfos[i].Name;

                RegexTree tree = RegexParser.Parse(pattern, options, (options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
                RegexInterpreterCode code = RegexWriter.Write(tree);

                c.GenerateRegexType(pattern, options, fullname, regexinfos[i].IsPublic, tree, regexinfos[i].MatchTimeout);
            }

            c.Save(assemblyname.Name ?? "RegexCompileToAssembly");
#pragma warning restore IL3050
#else
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
#endif
        }

        /// <summary>
        /// Escapes a minimal set of characters (\, *, +, ?, |, {, [, (, ), ^, $, ., #, and white space)
        /// by replacing them with their escape codes. This instructs the regular expression engine to interpret
        /// these characters literally rather than as metacharacters.
        /// </summary>
        /// <param name="str">The input string that contains the text to convert.</param>
        /// <returns>A string of characters with metacharacters converted to their escaped form.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>
        /// <see cref="Escape" /> converts a string so that the regular expression engine will interpret any
        /// metacharacters that it may contain as character literals. It is particularly important for strings
        /// that are defined dynamically using characters not known at design time.
        /// </para>
        /// <para>
        /// While this method escapes the straight opening bracket ([) and opening brace ({) characters,
        /// it does not escape their corresponding closing characters (] and }). In most cases, escaping
        /// these is not necessary.
        /// </para>
        /// </remarks>
        public static string Escape(string str)
        {
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }

            return RegexParser.Escape(str);
        }

        /// <summary>Converts any escaped characters in the input string.</summary>
        /// <param name="str">The input string containing the text to convert.</param>
        /// <returns>A string of characters with any escaped characters converted to their unescaped form.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="str" /> includes an unrecognized escape sequence.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="str" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Unescape" /> method reverses the transformation performed by the
        /// <see cref="Escape" /> method by removing the escape character ("\") from each escaped character.
        /// It also unescapes hexadecimal values in verbatim string literals, converting them to the actual
        /// printable characters (for example, <c>\x07</c> becomes <c>\a</c>).
        /// </para>
        /// </remarks>
        public static string Unescape(string str)
        {
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }

            return RegexParser.Unescape(str);
        }

        /// <summary>Gets the options that were passed into the <see cref="Regex" /> constructor.</summary>
        /// <value>
        /// One or more members of the <see cref="RegexOptions" /> enumeration that represent options that were passed
        /// to the <see cref="Regex" /> constructor.
        /// </value>
        /// <remarks>
        /// The value of the <see cref="Options" /> property consists of one or more members of the
        /// <see cref="RegexOptions" /> enumeration. If no options were defined in the constructor,
        /// its value is <see cref="RegexOptions.None" />. The <see cref="Options" /> property does
        /// not reflect inline options defined in the regular expression pattern itself.
        /// </remarks>
        public RegexOptions Options => roptions;

        /// <summary>Gets a value that indicates whether the regular expression searches from right to left.</summary>
        /// <value>
        /// <see langword="true" /> if the regular expression searches from right to left; otherwise,
        /// <see langword="false" />.
        /// </value>
        public bool RightToLeft => (roptions & RegexOptions.RightToLeft) != 0;

        /// <summary>Returns the regular expression pattern that was passed into the <see cref="Regex" /> constructor.</summary>
        /// <returns>
        /// The pattern that was passed into the <see cref="Regex" /> constructor.
        /// </returns>
        public override string ToString() => pattern!;

        /// <summary>Returns an array of capturing group names for the regular expression.</summary>
        /// <returns>A string array of group names.</returns>
        /// <remarks>
        /// The collection of group names contains the set of strings used to name capturing groups in the
        /// expression. Even if capturing groups are not explicitly named, they are automatically assigned
        /// numerical names ("0", "1", "2", and so on). Group "0" always designates the entire match.
        /// </remarks>
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

        /// <summary>Returns an array of capturing group numbers that correspond to group names in an array.</summary>
        /// <returns>An integer array of group numbers.</returns>
        /// <remarks>
        /// Groups can be referred to using both their assigned number and their name (if one was provided).
        /// The returned array is ordered so that each number maps to the same group name returned by
        /// <see cref="GetGroupNames" /> at the corresponding index.
        /// </remarks>
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
                Array.Sort(result);
            }

            return result;
        }

        /// <summary>Gets the group name that corresponds to the specified group number.</summary>
        /// <param name="i">The group number to convert to the corresponding group name.</param>
        /// <returns>
        /// A string that contains the group name associated with the specified group number. If there is no
        /// group name that corresponds to the specified group number, the method returns <see cref="string.Empty" />.
        /// </returns>
        /// <remarks>
        /// A regular expression pattern may contain either named or numbered capturing groups. Numbered groups
        /// are delimited by the syntax <c>(subexpression)</c> and are assigned numbers based on their order in
        /// the regular expression. Named groups are delimited by the syntax <c>(?&lt;name&gt;subexpression)</c>.
        /// The <see cref="GroupNameFromNumber" /> method identifies both named groups and numbered groups by
        /// their ordinal positions in the regular expression.
        /// </remarks>
        public string GroupNameFromNumber(int i)
        {
            return RegexParser.GroupNameFromNumber(caps, capslist, capsize, i);
        }

        /// <summary>Returns the group number that corresponds to the specified group name.</summary>
        /// <param name="name">The group name to convert to the corresponding group number.</param>
        /// <returns>
        /// The group number that corresponds to the specified group name, or -1 if
        /// <paramref name="name" /> is not a valid group name.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// The <see cref="GroupNumberFromName" /> method identifies both named groups and numbered groups by
        /// their ordinal positions in the regular expression. Ordinal position zero always represents the
        /// entire regular expression. All numbered groups are then counted before named groups, regardless
        /// of their actual position in the regular expression pattern.
        /// </remarks>
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
            field ??
            Interlocked.CompareExchange(ref field, new WeakReference<RegexReplacement?>(null), null) ??
            field;

        /// <summary>
        /// Used by a <see cref="Regex" /> object generated by the
        /// <see cref="CompileToAssembly(RegexCompilationInfo[], AssemblyName)" /> method. This method is obsolete.
        /// </summary>
        /// <exception cref="NotSupportedException">References have already been initialized.</exception>
        [Obsolete(Obsoletions.RegexExtensibilityImplMessage, DiagnosticId = Obsoletions.RegexExtensibilityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected void InitializeReferences()
        {
            // This method no longer has anything to initialize. It continues to exist
            // purely for API compat, as it was originally shipped as protected, with
            // assemblies generated by Regex.CompileToAssembly calling it.
        }

        /// <summary>Internal worker which will scan the passed in string <paramref name="input"/> for a match. Used by public APIs.</summary>
        internal Match? RunSingleMatch(RegexRunnerMode mode, int prevlen, string input, int beginning, int length, int startat)
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
                runner.InitializeTimeout(internalMatchTimeout);
                runner.runtext = input;
                ReadOnlySpan<char> span = input.AsSpan(beginning, length);
                runner.InitializeForScan(this, span, startat - beginning, mode);

                // If previous match was empty or failed, advance by one before matching.
                if (prevlen == 0)
                {
                    int stoppos = span.Length;
                    int bump = 1;
                    if (RightToLeft)
                    {
                        stoppos = 0;
                        bump = -1;
                    }

                    if (runner.runtextstart == stoppos)
                    {
                        return RegularExpressions.Match.Empty;
                    }

                    runner.runtextpos += bump;
                }

                return ScanInternal(mode, reuseMatchObject: mode == RegexRunnerMode.ExistenceRequired, input, beginning, runner, span, returnNullIfReuseMatchObject: true);
            }
            finally
            {
                runner.runtext = null; // drop reference to text to avoid keeping it alive in a cache.
                _runner = runner;
            }
        }

        /// <summary>Internal worker which will scan the passed in span <paramref name="input"/> for a match. Used by public APIs.</summary>
        internal (bool Success, int Index, int Length, int TextPosition) RunSingleMatch(RegexRunnerMode mode, int prevlen, ReadOnlySpan<char> input, int startat)
        {
            Debug.Assert(mode <= RegexRunnerMode.BoundsRequired);

            // startat parameter is always either 0 or input.Length since public API for IsMatch doesn't have an overload
            // that takes in startat.
            Debug.Assert(startat <= input.Length);

            RegexRunner runner = Interlocked.Exchange(ref _runner, null) ?? CreateRunner();
            try
            {
                runner.InitializeTimeout(internalMatchTimeout);
                runner.InitializeForScan(this, input, startat, mode);

                // If previous match was empty or failed, advance by one before matching.
                if (prevlen == 0)
                {
                    if (RightToLeft)
                    {
                        if (runner.runtextstart == 0)
                        {
                            return (false, -1, -1, -1);
                        }
                        runner.runtextpos--;
                    }
                    else
                    {
                        if (runner.runtextstart == input.Length)
                        {
                            return (false, -1, -1, -1);
                        }
                        runner.runtextpos++;
                    }
                }

                runner.Scan(input);

                // If runmatch is null it means that an override of Scan didn't implement it correctly, so we will
                // let this null ref since there are lots of ways where you can end up in a erroneous state.
                Match match = runner.runmatch!;
                if (match.FoundMatch)
                {
                    if (mode == RegexRunnerMode.ExistenceRequired)
                    {
                        return (true, -1, -1, -1);
                    }

                    match.Tidy(runner.runtextpos, 0, mode);
                    return (true, match.Index, match.Length, match._textpos);
                }

                return (false, -1, -1, -1);
            }
            finally
            {
                _runner = runner;
            }
        }

        /// <summary>Internal worker which will scan the passed in string <paramref name="input"/> for all matches, and will call <paramref name="callback"/> for each match found.</summary>
        internal void RunAllMatchesWithCallback<TState>(string? input, int startat, ref TState state, MatchCallback<TState> callback, RegexRunnerMode mode, bool reuseMatchObject) =>
            RunAllMatchesWithCallback(input, (ReadOnlySpan<char>)input, startat, ref state, callback, mode, reuseMatchObject);

        internal void RunAllMatchesWithCallback<TState>(ReadOnlySpan<char> input, int startat, ref TState state, MatchCallback<TState> callback, RegexRunnerMode mode, bool reuseMatchObject) =>
            RunAllMatchesWithCallback(inputString: null, input, startat, ref state, callback, mode, reuseMatchObject);

        private void RunAllMatchesWithCallback<TState>(string? inputString, ReadOnlySpan<char> inputSpan, int startat, ref TState state, MatchCallback<TState> callback, RegexRunnerMode mode, bool reuseMatchObject)
        {
            Debug.Assert(inputString is null || inputSpan.SequenceEqual(inputString));
            Debug.Assert((uint)startat <= (uint)inputSpan.Length);

            RegexRunner runner = Interlocked.Exchange(ref _runner, null) ?? CreateRunner();
            try
            {
                runner.runtext = inputString;
                runner.InitializeTimeout(internalMatchTimeout);
                int runtextpos = startat;

                while (true)
                {
                    runner.InitializeForScan(this, inputSpan, startat, mode);
                    runner.runtextpos = runtextpos;

                    // We get the Match by calling Scan. 'input' parameter is used to set the Match text which is only relevant if we are using the Run<TState> string
                    // overload, as APIs that call the span overload (like Count) don't require match.Text to be set, so we pass null in that case.
                    Match? match = ScanInternal(mode, reuseMatchObject, inputString, 0, runner, inputSpan, returnNullIfReuseMatchObject: false);
                    Debug.Assert(match is not null);

                    // If we failed to match again, we're done.
                    if (!match.Success)
                    {
                        break;
                    }

                    // We got a match.  Call the callback function with the match and prepare for next iteration.

                    if (!reuseMatchObject)
                    {
                        // We're not reusing match objects, so null out our field reference to the instance.
                        // It'll be recreated the next time one is needed.  reuseMatchObject will be false
                        // when the callback may expose the Match object to user code.
                        runner.runmatch = null;
                    }

                    if (!callback(ref state, match))
                    {
                        // If the callback returns false, we're done.
                        return;
                    }

                    // Now that we've matched successfully, update the starting position to reflect
                    // the current position, just as Match.NextMatch() would pass in _textpos as textstart.
                    runtextpos = startat = runner.runtextpos;

                    if (match.Length == 0)
                    {
                        int stoppos = inputSpan.Length;
                        int bump = 1;
                        if (RightToLeft)
                        {
                            stoppos = 0;
                            bump = -1;
                        }

                        if (runtextpos == stoppos)
                        {
                            return;
                        }

                        runtextpos += bump;
                    }

                    // Reset state for another iteration.
                    runner.runtrackpos = runner.runtrack!.Length;
                    runner.runstackpos = runner.runstack!.Length;
                    runner.runcrawlpos = runner.runcrawl!.Length;
                }
            }
            finally
            {
                runner.runtext = null; // drop reference to string to avoid keeping it alive in a cache.
                _runner = runner;
            }
        }

        /// <summary>Helper method used by RunSingleMatch and RunAllMatchesWithCallback which calls runner.Scan to find a match on the passed in span.</summary>
        private static Match? ScanInternal(RegexRunnerMode mode, bool reuseMatchObject, string? input, int beginning, RegexRunner runner, ReadOnlySpan<char> span, bool returnNullIfReuseMatchObject)
        {
            runner.Scan(span);

            Match? match = runner.runmatch;
            Debug.Assert(match is not null);

            // If we got a match, do some cleanup and return it, or return null if reuseMatchObject and returnNullIfReuseMatchObject are true.
            if (match.FoundMatch)
            {
                if (!reuseMatchObject)
                {
                    // The match object is only reusable in very specific circumstances where the internal caller
                    // extracts only the matching information (e.g. bounds) it needs from the Match object, so
                    // in such situations we don't need to fill in the input value, and because it's being reused,
                    // we don't want to null it out in the runner.  If, however, the match object isn't going to
                    // be reused, then we do need to finish populating it with the input text, and we do want to
                    // remove it from the runner so that no one else touches the object once we give it back.
                    match.Text = input;
                    runner.runmatch = null;
                }
                else if (returnNullIfReuseMatchObject)
                {
                    match.Text = null;
                    return null;
                }

                match.Tidy(runner.runtextpos, beginning, mode);

                return match;
            }

            // We failed to match, so we will return Match.Empty which means we can reuse runmatch object.
            // We do however need to clear its Text in case it was set, so as to not keep it alive in some cache.
            match.Text = null;

            return RegularExpressions.Match.Empty;
        }

        /// <summary>Creates a new runner instance.</summary>
        private RegexRunner CreateRunner() =>
            // The factory needs to be set by the ctor.  `factory` is a protected field, so it's possible a derived
            // type nulls out the factory after we've set it, but that's the nature of the design.
            factory!.CreateInstance();

        /// <summary>True if the <see cref="RegexOptions.Compiled"/> option was set.</summary>
        [Obsolete(Obsoletions.RegexExtensibilityImplMessage, DiagnosticId = Obsoletions.RegexExtensibilityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected bool UseOptionC() => (roptions & RegexOptions.Compiled) != 0;

        /// <summary>True if the <see cref="RegexOptions.RightToLeft"/> option was set.</summary>
        [Obsolete(Obsoletions.RegexExtensibilityImplMessage, DiagnosticId = Obsoletions.RegexExtensibilityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected bool UseOptionR() => RightToLeft;
    }
}
