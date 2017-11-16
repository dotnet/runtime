// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The Regex class represents a single compiled instance of a regular
// expression.

using System.Collections;
using System.Collections.Generic;
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
    /// Represents an immutable, compiled regular expression. Also
    /// contains static methods that allow use of regular expressions without instantiating
    /// a Regex explicitly.
    /// </summary>
    public class Regex : ISerializable
    {
        protected internal string pattern;                   // The string pattern provided
        protected internal RegexOptions roptions;            // the top-level options from the options string

        // *********** Match timeout fields { ***********

        // We need this because time is queried using Environment.TickCount for performance reasons
        // (Environment.TickCount returns milliseconds as an int and cycles):
        private static readonly TimeSpan MaximumMatchTimeout = TimeSpan.FromMilliseconds(int.MaxValue - 1);

        // InfiniteMatchTimeout specifies that match timeout is switched OFF. It allows for faster code paths
        // compared to simply having a very large timeout.
        // We do not want to ask users to use System.Threading.Timeout.InfiniteTimeSpan as a parameter because:
        //   (1) We do not want to imply any relation between having using a RegEx timeout and using multi-threading.
        //   (2) We do not want to require users to take ref to a contract assembly for threading just to use RegEx.
        //       There may in theory be a SKU that has RegEx, but no multithreading.
        // We create a public Regex.InfiniteMatchTimeout constant, which for consistency uses the save underlying
        // value as Timeout.InfiniteTimeSpan creating an implementation detail dependency only.
        public static readonly TimeSpan InfiniteMatchTimeout = Timeout.InfiniteTimeSpan;

        protected internal TimeSpan internalMatchTimeout;   // timeout for the execution of this regex

        // During static initialisation of Regex we check
        private const string DefaultMatchTimeout_ConfigKeyName = "REGEX_DEFAULT_MATCH_TIMEOUT";

        // DefaultMatchTimeout specifies the match timeout to use if no other timeout was specified
        // by one means or another. Typically, it is set to InfiniteMatchTimeout.
        internal static readonly TimeSpan DefaultMatchTimeout = InitDefaultMatchTimeout();

        // *********** } match timeout fields ***********

        protected internal RegexRunnerFactory factory;

        protected internal Hashtable caps;          // if captures are sparse, this is the hashtable capnum->index
        protected internal Hashtable capnames;      // if named captures are used, this maps names->index

        protected internal string[] capslist;              // if captures are sparse or named captures are used, this is the sorted list of names
        protected internal int capsize;                    // the size of the capture array

        [CLSCompliant(false)]
        protected IDictionary Caps
        {
            get
            {
                return caps;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                caps = value as Hashtable;
                if (caps == null)
                {
                    caps = new Hashtable(value);
                }
            }
        }

        [CLSCompliant(false)]
        protected IDictionary CapNames
        {
            get
            {
                return capnames;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                capnames = value as Hashtable;
                if (capnames == null)
                {
                    capnames = new Hashtable(value);
                }
            }
        }


        internal ExclusiveReference _runnerref;             // cached runner
        internal SharedReference _replref;                  // cached parsed replacement pattern
        internal RegexCode _code;                           // if interpreted, this is the code for RegexInterpreter
        internal bool _refsInitialized = false;

        internal static LinkedList<CachedCodeEntry> s_livecode = new LinkedList<CachedCodeEntry>();// the cache of code and factories that are currently loaded
        internal static int s_cacheSize = 15;

        internal const int MaxOptionShift = 10;

        protected Regex()
        {
            internalMatchTimeout = DefaultMatchTimeout;
        }

        /// <summary>
        /// Creates and compiles a regular expression object for the specified regular
        /// expression.
        /// </summary>
        public Regex(string pattern)
            : this(pattern, RegexOptions.None, DefaultMatchTimeout, false)
        {
        }

        /// <summary>
        /// Creates and compiles a regular expression object for the
        /// specified regular expression with options that modify the pattern.
        /// </summary>
        public Regex(string pattern, RegexOptions options)
            : this(pattern, options, DefaultMatchTimeout, false)
        {
        }

        public Regex(string pattern, RegexOptions options, TimeSpan matchTimeout)
            : this(pattern, options, matchTimeout, false)
        {
        }

        protected Regex(SerializationInfo info, StreamingContext context)
            : this(info.GetString("pattern"), (RegexOptions)info.GetInt32("options"))
        {
            throw new PlatformNotSupportedException();
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        private Regex(string pattern, RegexOptions options, TimeSpan matchTimeout, bool addToCache)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (options < RegexOptions.None || (((int)options) >> MaxOptionShift) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if ((options & RegexOptions.ECMAScript) != 0
             && (options & ~(RegexOptions.ECMAScript |
                             RegexOptions.IgnoreCase |
                             RegexOptions.Multiline |
                             RegexOptions.Compiled |
                             RegexOptions.CultureInvariant
#if DEBUG
                           | RegexOptions.Debug
#endif
                                               )) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            ValidateMatchTimeout(matchTimeout);

            string cultureKey;
            if ((options & RegexOptions.CultureInvariant) != 0)
                cultureKey = CultureInfo.InvariantCulture.ToString();
            else
                cultureKey = CultureInfo.CurrentCulture.ToString();

            // Try to look up this regex in the cache.
            var key = new CachedCodeEntryKey(options, cultureKey, pattern);
            CachedCodeEntry cached = LookupCachedAndUpdate(key);

            this.pattern = pattern;
            roptions = options;
            internalMatchTimeout = matchTimeout;

            if (cached == null)
            {
                // Parse the input
                RegexTree tree = RegexParser.Parse(pattern, roptions);

                // Extract the relevant information
                capnames = tree._capnames;
                capslist = tree._capslist;
                _code = RegexWriter.Write(tree);
                caps = _code._caps;
                capsize = _code._capsize;

                InitializeReferences();

                tree = null;
                if (addToCache)
                    cached = CacheCode(key);
            }
            else
            {
                caps = cached._caps;
                capnames = cached._capnames;
                capslist = cached._capslist;
                capsize = cached._capsize;
                _code = cached._code;
#if FEATURE_COMPILED
                factory = cached._factory;
#endif
                _runnerref = cached._runnerref;
                _replref = cached._replref;
                _refsInitialized = true;
            }

#if FEATURE_COMPILED
            // if the compile option is set, then compile the code if it's not already
            if (UseOptionC() && factory == null)
            {
                factory = Compile(_code, roptions);

                if (addToCache && cached != null)
                {
                    cached.AddCompiled(factory);
                }

                _code = null;
            }
#endif
        }

        // Note: "&lt;" is the XML entity for smaller ("<").
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

            // Change this to make sure timeout is not longer then Environment.Ticks cycle length:
            if (TimeSpan.Zero < matchTimeout && matchTimeout <= MaximumMatchTimeout)
                return;

            throw new ArgumentOutOfRangeException(nameof(matchTimeout));
        }

        /// <summary>
        /// Specifies the default RegEx matching timeout value (i.e. the timeout that will be used if no
        /// explicit timeout is specified).
        /// The default is queried from the current <code>AppDomain</code>.
        /// If the AddDomain's data value for that key is not a <code>TimeSpan</code> value or if it is outside the
        /// valid range, an exception is thrown.
        /// If the AddDomain's data value for that key is <code>null</code>, a fallback value is returned.
        /// </summary>
        /// <returns>The default RegEx matching timeout for this AppDomain</returns>
        private static TimeSpan InitDefaultMatchTimeout()
        {
            // Query AppDomain
            AppDomain ad = AppDomain.CurrentDomain;
            object defaultMatchTimeoutObj = ad.GetData(DefaultMatchTimeout_ConfigKeyName);

            // If no default is specified, use fallback
            if (defaultMatchTimeoutObj == null)
            {
                return InfiniteMatchTimeout;
            }

            if (defaultMatchTimeoutObj is TimeSpan defaultMatchTimeOut)
            {
                // If default timeout is outside the valid range, throw. It will result in a TypeInitializationException:
                try
                {
                    ValidateMatchTimeout(defaultMatchTimeOut);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentOutOfRangeException(SR.Format(SR.IllegalDefaultRegexMatchTimeoutInAppDomain, DefaultMatchTimeout_ConfigKeyName, defaultMatchTimeOut));
                }

                return defaultMatchTimeOut;
            }

            throw new InvalidCastException(SR.Format(SR.IllegalDefaultRegexMatchTimeoutInAppDomain, DefaultMatchTimeout_ConfigKeyName, defaultMatchTimeoutObj));
        }

#if FEATURE_COMPILED
        /// <summary>
        /// This method is here for perf reasons: if the call to RegexCompiler is NOT in the 
        /// Regex constructor, we don't load RegexCompiler and its reflection classes when
        /// instantiating a non-compiled regex.
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private RegexRunnerFactory Compile(RegexCode code, RegexOptions roptions)
        {
            return RegexCompiler.Compile(code, roptions);
        }
#endif

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
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unescape", Justification = "Already shipped since v1 - can't fix without causing a breaking change")]
        public static string Unescape(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            return RegexParser.Unescape(str);
        }

        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread-safety")]
        public static int CacheSize
        {
            get
            {
                return s_cacheSize;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                s_cacheSize = value;
                if (s_livecode.Count > s_cacheSize)
                {
                    lock (s_livecode)
                    {
                        while (s_livecode.Count > s_cacheSize)
                            s_livecode.RemoveLast();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the options passed into the constructor
        /// </summary>
        public RegexOptions Options
        {
            get { return roptions; }
        }


        /// <summary>
        /// The match timeout used by this Regex instance.
        /// </summary>
        public TimeSpan MatchTimeout
        {
            get { return internalMatchTimeout; }
        }

        /// <summary>
        /// Indicates whether the regular expression matches from right to left.
        /// </summary>
        public bool RightToLeft
        {
            get
            {
                return UseOptionR();
            }
        }

        /// <summary>
        /// Returns the regular expression pattern passed into the constructor
        /// </summary>
        public override string ToString()
        {
            return pattern;
        }

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
                int max = capsize;
                result = new string[max];

                for (int i = 0; i < max; i++)
                {
                    result[i] = Convert.ToString(i, CultureInfo.InvariantCulture);
                }
            }
            else
            {
                result = new string[capslist.Length];

                Array.Copy(capslist, 0, result, 0, capslist.Length);
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
                    result[(int)de.Value] = (int)de.Key;
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
                    return i.ToString(CultureInfo.InvariantCulture);

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
            int result = -1;

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // look up name if we have a hashtable of names
            if (capnames != null)
            {
                if (!capnames.TryGetValue(name, out result))
                    return -1;

                return result;
            }

            // convert to an int if it looks like a number
            result = 0;
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];

                if (ch > '9' || ch < '0')
                    return -1;

                result *= 10;
                result += (ch - '0');
            }

            // return int if it's in range
            if (result >= 0 && result < capsize)
                return result;

            return -1;
        }

        /*
         * Static version of simple IsMatch call
         */
        /// <summary>
        /// Searches the input string for one or more occurrences of the text supplied in the given pattern.
        /// </summary>
        public static bool IsMatch(string input, string pattern)
        {
            return IsMatch(input, pattern, RegexOptions.None, DefaultMatchTimeout);
        }

        /*
         * Static version of simple IsMatch call
         */
        /// <summary>
        /// Searches the input string for one or more occurrences of the text
        /// supplied in the pattern parameter with matching options supplied in the options
        /// parameter.
        /// </summary>
        public static bool IsMatch(string input, string pattern, RegexOptions options)
        {
            return IsMatch(input, pattern, options, DefaultMatchTimeout);
        }

        public static bool IsMatch(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).IsMatch(input);
        }

        /*
         * Returns true if the regex finds a match within the specified string
         */
        /// <summary>
        /// Searches the input string for one or more matches using the previous pattern,
        /// options, and starting position.
        /// </summary>
        public bool IsMatch(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return IsMatch(input, UseOptionR() ? input.Length : 0);
        }

        /*
         * Returns true if the regex finds a match after the specified position
         * (proceeding leftward if the regex is leftward and rightward otherwise)
         */
        /// <summary>
        /// Searches the input string for one or more matches using the previous pattern and options,
        /// with a new starting position.
        /// </summary>
        public bool IsMatch(string input, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return (null == Run(true, -1, input, 0, input.Length, startat));
        }

        /*
         * Static version of simple Match call
         */
        /// <summary>
        /// Searches the input string for one or more occurrences of the text
        /// supplied in the pattern parameter.
        /// </summary>
        public static Match Match(string input, string pattern)
        {
            return Match(input, pattern, RegexOptions.None, DefaultMatchTimeout);
        }

        /*
         * Static version of simple Match call
         */
        /// <summary>
        /// Searches the input string for one or more occurrences of the text
        /// supplied in the pattern parameter. Matching is modified with an option
        /// string.
        /// </summary>
        public static Match Match(string input, string pattern, RegexOptions options)
        {
            return Match(input, pattern, options, DefaultMatchTimeout);
        }


        public static Match Match(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Match(input);
        }

        /*
         * Finds the first match for the regular expression starting at the beginning
         * of the string (or at the end of the string if the regex is leftward)
         */
        /// <summary>
        /// Matches a regular expression with a string and returns
        /// the precise result as a RegexMatch object.
        /// </summary>
        public Match Match(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Match(input, UseOptionR() ? input.Length : 0);
        }

        /*
         * Finds the first match, starting at the specified position
         */
        /// <summary>
        /// Matches a regular expression with a string and returns
        /// the precise result as a RegexMatch object.
        /// </summary>
        public Match Match(string input, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Run(false, -1, input, 0, input.Length, startat);
        }

        /*
         * Finds the first match, restricting the search to the specified interval of
         * the char array.
         */
        /// <summary>
        /// Matches a regular expression with a string and returns the precise result as a
        /// RegexMatch object.
        /// </summary>
        public Match Match(string input, int beginning, int length)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Run(false, -1, input, beginning, length, UseOptionR() ? beginning + length : beginning);
        }

        /*
         * Static version of simple Matches call
         */
        /// <summary>
        /// Returns all the successful matches as if Match were called iteratively numerous times.
        /// </summary>
        public static MatchCollection Matches(string input, string pattern)
        {
            return Matches(input, pattern, RegexOptions.None, DefaultMatchTimeout);
        }

        /*
         * Static version of simple Matches call
         */
        /// <summary>
        /// Returns all the successful matches as if Match were called iteratively numerous times.
        /// </summary>
        public static MatchCollection Matches(string input, string pattern, RegexOptions options)
        {
            return Matches(input, pattern, options, DefaultMatchTimeout);
        }

        public static MatchCollection Matches(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Matches(input);
        }

        /*
         * Finds the first match for the regular expression starting at the beginning
         * of the string Enumerator(or at the end of the string if the regex is leftward)
         */
        /// <summary>
        /// Returns all the successful matches as if Match was called iteratively numerous times.
        /// </summary>
        public MatchCollection Matches(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Matches(input, UseOptionR() ? input.Length : 0);
        }

        /*
         * Finds the first match, starting at the specified position
         */
        /// <summary>
        /// Returns all the successful matches as if Match was called iteratively numerous times.
        /// </summary>
        public MatchCollection Matches(string input, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return new MatchCollection(this, input, 0, input.Length, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the pattern with the <paramref name="replacement"/> pattern, starting at
        /// the first character in the input string.
        /// </summary>
        public static string Replace(string input, string pattern, string replacement)
        {
            return Replace(input, pattern, replacement, RegexOptions.None, DefaultMatchTimeout);
        }

        /// <summary>
        /// Replaces all occurrences of
        /// the <paramref name="pattern "/>with the <paramref name="replacement "/>
        /// pattern, starting at the first character in the input string.
        /// </summary>
        public static string Replace(string input, string pattern, string replacement, RegexOptions options)
        {
            return Replace(input, pattern, replacement, options, DefaultMatchTimeout);
        }

        public static string Replace(string input, string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Replace(input, replacement);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the first character in the
        /// input string.
        /// </summary>
        public string Replace(string input, string replacement)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, replacement, -1, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the first character in the
        /// input string.
        /// </summary>
        public string Replace(string input, string replacement, int count)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, replacement, count, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the character position
        /// <paramref name="startat"/>.
        /// </summary>
        public string Replace(string input, string replacement, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            // a little code to grab a cached parsed replacement object
            RegexReplacement repl = (RegexReplacement)_replref.Get();

            if (repl == null || !repl.Pattern.Equals(replacement))
            {
                repl = RegexParser.ParseReplacement(replacement, caps, capsize, capnames, roptions);
                _replref.Cache(repl);
            }

            return repl.Replace(this, input, count, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern.
        /// </summary>
        public static string Replace(string input, string pattern, MatchEvaluator evaluator)
        {
            return Replace(input, pattern, evaluator, RegexOptions.None, DefaultMatchTimeout);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern, starting at the first character.
        /// </summary>
        public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options)
        {
            return Replace(input, pattern, evaluator, options, DefaultMatchTimeout);
        }

        public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Replace(input, evaluator);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the first character position.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, evaluator, -1, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the first character position.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator, int count)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, evaluator, count, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the character position
        /// <paramref name="startat"/>.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return RegexReplacement.Replace(evaluator, this, input, count, startat);
        }

        /// <summary>
        /// Splits the <paramref name="input "/>string at the position defined
        /// by <paramref name="pattern"/>.
        /// </summary>
        public static string[] Split(string input, string pattern)
        {
            return Split(input, pattern, RegexOptions.None, DefaultMatchTimeout);
        }

        /// <summary>
        /// Splits the <paramref name="input "/>string at the position defined by <paramref name="pattern"/>.
        /// </summary>
        public static string[] Split(string input, string pattern, RegexOptions options)
        {
            return Split(input, pattern, options, DefaultMatchTimeout);
        }

        public static string[] Split(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Split(input);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public string[] Split(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Split(input, 0, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public string[] Split(string input, int count)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return RegexReplacement.Split(this, input, count, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a previous pattern.
        /// </summary>
        public string[] Split(string input, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return RegexReplacement.Split(this, input, count, startat);
        }

#if FEATURE_COMPILED
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "assemblyname", Justification = "Microsoft: already shipped since v1 - can't fix without causing a breaking change")]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "assemblyname", Justification = "Microsoft: already shipped since v1 - can't fix without causing a breaking change")]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[] attributes)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "assemblyname", Justification = "Microsoft: already shipped since v1 - can't fix without causing a breaking change")]
        public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[] attributes, string resourceFile)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
        }
#endif

        protected void InitializeReferences()
        {
            if (_refsInitialized)
                throw new NotSupportedException(SR.OnlyAllowedOnce);

            _refsInitialized = true;
            _runnerref = new ExclusiveReference();
            _replref = new SharedReference();
        }


        /*
         * Internal worker called by all the public APIs
         */
        internal Match Run(bool quick, int prevlen, string input, int beginning, int length, int startat)
        {
            Match match;
            RegexRunner runner = null;

            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            if (length < 0 || length > input.Length)
                throw new ArgumentOutOfRangeException(nameof(length), SR.LengthNotNegative);

            // There may be a cached runner; grab ownership of it if we can.

            runner = (RegexRunner)_runnerref.Get();

            // Create a RegexRunner instance if we need to

            if (runner == null)
            {
                // Use the compiled RegexRunner factory if the code was compiled to MSIL

                if (factory != null)
                    runner = factory.CreateInstance();
                else
                    runner = new RegexInterpreter(_code, UseOptionInvariant() ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
            }

            try
            {
                // Do the scan starting at the requested position
                match = runner.Scan(this, input, beginning, beginning + length, startat, prevlen, quick, internalMatchTimeout);
            }
            finally
            {
                // Release or fill the cache slot
                _runnerref.Release(runner);
            }

#if DEBUG
            if (Debug && match != null)
                match.Dump();
#endif
            return match;
        }

        /*
         * Find code cache based on options+pattern
         */
        private static CachedCodeEntry LookupCachedAndUpdate(CachedCodeEntryKey key)
        {
            lock (s_livecode)
            {
                for (LinkedListNode<CachedCodeEntry> current = s_livecode.First; current != null; current = current.Next)
                {
                    if (current.Value._key == key)
                    {
                        // If we find an entry in the cache, move it to the head at the same time.
                        s_livecode.Remove(current);
                        s_livecode.AddFirst(current);
                        return current.Value;
                    }
                }
            }

            return null;
        }

        /*
         * Add current code to the cache
         */
        private CachedCodeEntry CacheCode(CachedCodeEntryKey key)
        {
            CachedCodeEntry newcached = null;

            lock (s_livecode)
            {
                // first look for it in the cache and move it to the head
                for (LinkedListNode<CachedCodeEntry> current = s_livecode.First; current != null; current = current.Next)
                {
                    if (current.Value._key == key)
                    {
                        s_livecode.Remove(current);
                        s_livecode.AddFirst(current);
                        return current.Value;
                    }
                }

                // it wasn't in the cache, so we'll add a new one.  Shortcut out for the case where cacheSize is zero.
                if (s_cacheSize != 0)
                {
                    newcached = new CachedCodeEntry(key, capnames, capslist, _code, caps, capsize, _runnerref, _replref);
                    s_livecode.AddFirst(newcached);
                    if (s_livecode.Count > s_cacheSize)
                        s_livecode.RemoveLast();
                }
            }

            return newcached;
        }

        protected bool UseOptionC()
        {
            return (roptions & RegexOptions.Compiled) != 0;
        }

        /*
         * True if the L option was set
         */
        protected internal bool UseOptionR()
        {
            return (roptions & RegexOptions.RightToLeft) != 0;
        }

        internal bool UseOptionInvariant()
        {
            return (roptions & RegexOptions.CultureInvariant) != 0;
        }

#if DEBUG
        /*
         * True if the regex has debugging enabled
         */
        internal bool Debug
        {
            get
            {
                return (roptions & RegexOptions.Debug) != 0;
            }
        }
#endif
    }


    /*
     * Callback class
     */
    public delegate string MatchEvaluator(Match match);

    /*
     * Used as a key for CacheCodeEntry
     */
    internal readonly struct CachedCodeEntryKey : IEquatable<CachedCodeEntryKey>
    {
        private readonly RegexOptions _options;
        private readonly string _cultureKey;
        private readonly string _pattern;

        internal CachedCodeEntryKey(RegexOptions options, string cultureKey, string pattern)
        {
            _options = options;
            _cultureKey = cultureKey;
            _pattern = pattern;
        }

        public override bool Equals(object obj)
        {
            return obj is CachedCodeEntryKey && Equals((CachedCodeEntryKey)obj);
        }

        public bool Equals(CachedCodeEntryKey other)
        {
            return this == other;
        }

        public static bool operator ==(CachedCodeEntryKey left, CachedCodeEntryKey right)
        {
            return left._options == right._options && left._cultureKey == right._cultureKey && left._pattern == right._pattern;
        }

        public static bool operator !=(CachedCodeEntryKey left, CachedCodeEntryKey right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return ((int)_options) ^ _cultureKey.GetHashCode() ^ _pattern.GetHashCode();
        }
    }

    /*
     * Used to cache byte codes
     */
    internal sealed class CachedCodeEntry
    {
        internal CachedCodeEntryKey _key;
        internal RegexCode _code;
        internal Hashtable _caps;
        internal Hashtable _capnames;
        internal string[] _capslist;
#if FEATURE_COMPILED
        internal RegexRunnerFactory _factory;
#endif
        internal int _capsize;
        internal ExclusiveReference _runnerref;
        internal SharedReference _replref;

        internal CachedCodeEntry(CachedCodeEntryKey key, Hashtable capnames, string[] capslist, RegexCode code, Hashtable caps, int capsize, ExclusiveReference runner, SharedReference repl)
        {
            _key = key;
            _capnames = capnames;
            _capslist = capslist;

            _code = code;
            _caps = caps;
            _capsize = capsize;

            _runnerref = runner;
            _replref = repl;
        }

#if FEATURE_COMPILED
        internal void AddCompiled(RegexRunnerFactory factory)
        {
            _factory = factory;
            _code = null;
        }
#endif
    }

    /*
     * Used to cache one exclusive runner reference
     */
    internal sealed class ExclusiveReference
    {
        private RegexRunner _ref;
        private object _obj;
        private int _locked;

        /*
         * Return an object and grab an exclusive lock.
         *
         * If the exclusive lock can't be obtained, null is returned;
         * if the object can't be returned, the lock is released.
         *
         */
        internal object Get()
        {
            // try to obtain the lock

            if (0 == Interlocked.Exchange(ref _locked, 1))
            {
                // grab reference


                object obj = _ref;

                // release the lock and return null if no reference

                if (obj == null)
                {
                    _locked = 0;
                    return null;
                }

                // remember the reference and keep the lock

                _obj = obj;
                return obj;
            }

            return null;
        }

        /*
         * Release an object back to the cache
         *
         * If the object is the one that's under lock, the lock
         * is released.
         *
         * If there is no cached object, then the lock is obtained
         * and the object is placed in the cache.
         *
         */
        internal void Release(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            // if this reference owns the lock, release it

            if (_obj == obj)
            {
                _obj = null;
                _locked = 0;
                return;
            }

            // if no reference owns the lock, try to cache this reference

            if (_obj == null)
            {
                // try to obtain the lock

                if (0 == Interlocked.Exchange(ref _locked, 1))
                {
                    // if there's really no reference, cache this reference

                    if (_ref == null)
                        _ref = (RegexRunner)obj;

                    // release the lock

                    _locked = 0;
                    return;
                }
            }
        }
    }

    /*
     * Used to cache a weak reference in a threadsafe way
     */
    internal sealed class SharedReference
    {
        private WeakReference _ref = new WeakReference(null);
        private int _locked;

        /*
         * Return an object from a weakref, protected by a lock.
         *
         * If the exclusive lock can't be obtained, null is returned;
         *
         * Note that _ref.Target is referenced only under the protection
         * of the lock. (Is this necessary?)
         */
        internal object Get()
        {
            if (0 == Interlocked.Exchange(ref _locked, 1))
            {
                object obj = _ref.Target;
                _locked = 0;
                return obj;
            }

            return null;
        }

        /*
         * Suggest an object into a weakref, protected by a lock.
         *
         * Note that _ref.Target is referenced only under the protection
         * of the lock. (Is this necessary?)
         */
        internal void Cache(object obj)
        {
            if (0 == Interlocked.Exchange(ref _locked, 1))
            {
                _ref.Target = obj;
                _locked = 0;
            }
        }
    }
}
