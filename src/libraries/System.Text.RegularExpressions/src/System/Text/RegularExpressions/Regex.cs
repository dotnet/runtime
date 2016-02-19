// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The Regex class represents a single compiled instance of a regular
// expression.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents an immutable, compiled regular expression. Also
    /// contains static methods that allow use of regular expressions without instantiating
    /// a Regex explicitly.
    /// </summary>
    public class Regex
    {
        internal string _pattern;                   // The string pattern provided
        internal RegexOptions _roptions;            // the top-level options from the options string

        // *********** Match timeout fields { ***********

        // We need this because time is queried using Environment.TickCount for performance reasons
        // (Environment.TickCount returns milliseconds as an int and cycles):
        private static readonly TimeSpan MaximumMatchTimeout = TimeSpan.FromMilliseconds(Int32.MaxValue - 1);

        // InfiniteMatchTimeout specifies that match timeout is switched OFF. It allows for faster code paths
        // compared to simply having a very large timeout.
        // We do not want to ask users to use System.Threading.Timeout.InfiniteTimeSpan as a parameter because:
        //   (1) We do not want to imply any relation between having using a RegEx timeout and using multi-threading.
        //   (2) We do not want to require users to take ref to a contract assembly for threading just to use RegEx.
        //       There may in theory be a SKU that has RegEx, but no multithreading.
        // We create a public Regex.InfiniteMatchTimeout constant, which for consistency uses the save underlying
        // value as Timeout.InfiniteTimeSpan creating an implementation detail dependency only.
        public static readonly TimeSpan InfiniteMatchTimeout = Timeout.InfiniteTimeSpan;

        internal TimeSpan _internalMatchTimeout;   // timeout for the execution of this regex

        // DefaultMatchTimeout specifies the match timeout to use if no other timeout was specified
        // by one means or another. Typically, it is set to InfiniteMatchTimeout.
        internal static readonly TimeSpan DefaultMatchTimeout = InfiniteMatchTimeout;

        // *********** } match timeout fields ***********


        internal Dictionary<Int32, Int32> _caps;            // if captures are sparse, this is the hashtable capnum->index
        internal Dictionary<String, Int32> _capnames;       // if named captures are used, this maps names->index

        internal String[] _capslist;                        // if captures are sparse or named captures are used, this is the sorted list of names
        internal int _capsize;                              // the size of the capture array

        internal ExclusiveReference _runnerref;             // cached runner
        internal SharedReference _replref;                  // cached parsed replacement pattern
        internal RegexCode _code;                           // if interpreted, this is the code for RegexInterpreter
        internal bool _refsInitialized = false;

        internal static LinkedList<CachedCodeEntry> s_livecode = new LinkedList<CachedCodeEntry>();// the cache of code and factories that are currently loaded
        internal static int s_cacheSize = 15;

        internal const int MaxOptionShift = 10;

        protected Regex()
        {
            _internalMatchTimeout = DefaultMatchTimeout;
        }

        /// <summary>
        /// Creates and compiles a regular expression object for the specified regular
        /// expression.
        /// </summary>
        public Regex(String pattern)
            : this(pattern, RegexOptions.None, DefaultMatchTimeout, false)
        {
        }

        /// <summary>
        /// Creates and compiles a regular expression object for the
        /// specified regular expression with options that modify the pattern.
        /// </summary>
        public Regex(String pattern, RegexOptions options)
            : this(pattern, options, DefaultMatchTimeout, false)
        {
        }

        public Regex(String pattern, RegexOptions options, TimeSpan matchTimeout)
            : this(pattern, options, matchTimeout, false)
        {
        }

        private Regex(String pattern, RegexOptions options, TimeSpan matchTimeout, bool useCache)
        {
            RegexTree tree;
            CachedCodeEntry cached = null;
            string cultureKey = null;

            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));
            if (options < RegexOptions.None || (((int)options) >> MaxOptionShift) != 0)
                throw new ArgumentOutOfRangeException(nameof(options));
            if ((options & RegexOptions.ECMAScript) != 0
             && (options & ~(RegexOptions.ECMAScript |
                             RegexOptions.IgnoreCase |
                             RegexOptions.Multiline |
                             RegexOptions.CultureInvariant
#if DEBUG
                           | RegexOptions.Debug
#endif
                                               )) != 0)
                throw new ArgumentOutOfRangeException(nameof(options));

            ValidateMatchTimeout(matchTimeout);

            // Try to look up this regex in the cache.  We do this regardless of whether useCache is true since there's
            // really no reason not to.
            if ((options & RegexOptions.CultureInvariant) != 0)
                cultureKey = CultureInfo.InvariantCulture.ToString(); // "English (United States)"
            else
                cultureKey = CultureInfo.CurrentCulture.ToString();

            var key = new CachedCodeEntryKey(options, cultureKey, pattern);
            cached = LookupCachedAndUpdate(key);

            _pattern = pattern;
            _roptions = options;

            _internalMatchTimeout = matchTimeout;

            if (cached == null)
            {
                // Parse the input
                tree = RegexParser.Parse(pattern, _roptions);

                // Extract the relevant information
                _capnames = tree._capnames;
                _capslist = tree._capslist;
                _code = RegexWriter.Write(tree);
                _caps = _code._caps;
                _capsize = _code._capsize;

                InitializeReferences();

                tree = null;
                if (useCache)
                    cached = CacheCode(key);
            }
            else
            {
                _caps = cached._caps;
                _capnames = cached._capnames;
                _capslist = cached._capslist;
                _capsize = cached._capsize;
                _code = cached._code;
                _runnerref = cached._runnerref;
                _replref = cached._replref;
                _refsInitialized = true;
            }
        }

        // Note: "&lt;" is the XML entity for smaller ("<").
        /// <summary>
        /// Validates that the specified match timeout value is valid.
        /// The valid range is <code>TimeSpan.Zero &lt; matchTimeout &lt;= Regex.MaximumMatchTimeout</code>.
        /// </summary>
        /// <param name="matchTimeout">The timeout value to validate.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">If the specified timeout is not within a valid range.
        /// </exception>
        internal static void ValidateMatchTimeout(TimeSpan matchTimeout)
        {
            if (InfiniteMatchTimeout == matchTimeout)
                return;

            // Change this to make sure timeout is not longer then Environment.Ticks cycle length:
            if (TimeSpan.Zero < matchTimeout && matchTimeout <= MaximumMatchTimeout)
                return;

            throw new ArgumentOutOfRangeException(nameof(matchTimeout));
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
        public static String Escape(String str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            return RegexParser.Escape(str);
        }

        /// <summary>
        /// Unescapes any escaped characters in the input string.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unescape", Justification = "Already shipped since v1 - can't fix without causing a breaking change")]
        public static String Unescape(String str)
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
            get { return _roptions; }
        }


        /// <summary>
        /// The match timeout used by this Regex instance.
        /// </summary>
        public TimeSpan MatchTimeout
        {
            get { return _internalMatchTimeout; }
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
            return _pattern;
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
        public String[] GetGroupNames()
        {
            String[] result;

            if (_capslist == null)
            {
                int max = _capsize;
                result = new String[max];

                for (int i = 0; i < max; i++)
                {
                    result[i] = Convert.ToString(i, CultureInfo.InvariantCulture);
                }
            }
            else
            {
                result = new String[_capslist.Length];

                System.Array.Copy(_capslist, 0, result, 0, _capslist.Length);
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

            if (_caps == null)
            {
                int max = _capsize;
                result = new int[max];

                for (int i = 0; i < max; i++)
                {
                    result[i] = i;
                }
            }
            else
            {
                result = new int[_caps.Count];

                foreach (KeyValuePair<int, int> kvp in _caps)
                {
                    result[kvp.Value] = kvp.Key;
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
        public String GroupNameFromNumber(int i)
        {
            if (_capslist == null)
            {
                if (i >= 0 && i < _capsize)
                    return i.ToString(CultureInfo.InvariantCulture);

                return String.Empty;
            }
            else
            {
                if (_caps != null)
                {
                    if (!_caps.TryGetValue(i, out i))
                        return String.Empty;
                }

                if (i >= 0 && i < _capslist.Length)
                    return _capslist[i];

                return String.Empty;
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
        public int GroupNumberFromName(String name)
        {
            int result = -1;

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // look up name if we have a hashtable of names
            if (_capnames != null)
            {
                if (!_capnames.TryGetValue(name, out result))
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
            if (result >= 0 && result < _capsize)
                return result;

            return -1;
        }

        /*
         * Static version of simple IsMatch call
         */
        /// <summary>
        /// Searches the input string for one or more occurrences of the text supplied in the given pattern.
        /// </summary>
        public static bool IsMatch(String input, String pattern)
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
        public static bool IsMatch(String input, String pattern, RegexOptions options)
        {
            return IsMatch(input, pattern, options, DefaultMatchTimeout);
        }

        public static bool IsMatch(String input, String pattern, RegexOptions options, TimeSpan matchTimeout)
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
        public bool IsMatch(String input)
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
        public bool IsMatch(String input, int startat)
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
        public static Match Match(String input, String pattern)
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
        public static Match Match(String input, String pattern, RegexOptions options)
        {
            return Match(input, pattern, options, DefaultMatchTimeout);
        }


        public static Match Match(String input, String pattern, RegexOptions options, TimeSpan matchTimeout)
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
        public Match Match(String input)
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
        public Match Match(String input, int startat)
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
        public Match Match(String input, int beginning, int length)
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
        public static MatchCollection Matches(String input, String pattern)
        {
            return Matches(input, pattern, RegexOptions.None, DefaultMatchTimeout);
        }

        /*
         * Static version of simple Matches call
         */
        /// <summary>
        /// Returns all the successful matches as if Match were called iteratively numerous times.
        /// </summary>
        public static MatchCollection Matches(String input, String pattern, RegexOptions options)
        {
            return Matches(input, pattern, options, DefaultMatchTimeout);
        }

        public static MatchCollection Matches(String input, String pattern, RegexOptions options, TimeSpan matchTimeout)
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
        public MatchCollection Matches(String input)
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
        public MatchCollection Matches(String input, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return new MatchCollection(this, input, 0, input.Length, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the pattern with the <paramref name="replacement"/> pattern, starting at
        /// the first character in the input string.
        /// </summary>
        public static String Replace(String input, String pattern, String replacement)
        {
            return Replace(input, pattern, replacement, RegexOptions.None, DefaultMatchTimeout);
        }

        /// <summary>
        /// Replaces all occurrences of
        /// the <paramref name="pattern "/>with the <paramref name="replacement "/>
        /// pattern, starting at the first character in the input string.
        /// </summary>
        public static String Replace(String input, String pattern, String replacement, RegexOptions options)
        {
            return Replace(input, pattern, replacement, options, DefaultMatchTimeout);
        }

        public static String Replace(String input, String pattern, String replacement, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Replace(input, replacement);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the first character in the
        /// input string.
        /// </summary>
        public String Replace(String input, String replacement)
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
        public String Replace(String input, String replacement, int count)
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
        public String Replace(String input, String replacement, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            // a little code to grab a cached parsed replacement object
            RegexReplacement repl = (RegexReplacement)_replref.Get();

            if (repl == null || !repl.Pattern.Equals(replacement))
            {
                repl = RegexParser.ParseReplacement(replacement, _caps, _capsize, _capnames, _roptions);
                _replref.Cache(repl);
            }

            return repl.Replace(this, input, count, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern.
        /// </summary>
        public static String Replace(String input, String pattern, MatchEvaluator evaluator)
        {
            return Replace(input, pattern, evaluator, RegexOptions.None, DefaultMatchTimeout);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern, starting at the first character.
        /// </summary>
        public static String Replace(String input, String pattern, MatchEvaluator evaluator, RegexOptions options)
        {
            return Replace(input, pattern, evaluator, options, DefaultMatchTimeout);
        }

        public static String Replace(String input, String pattern, MatchEvaluator evaluator, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Replace(input, evaluator);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the first character position.
        /// </summary>
        public String Replace(String input, MatchEvaluator evaluator)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, evaluator, -1, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the first character position.
        /// </summary>
        public String Replace(String input, MatchEvaluator evaluator, int count)
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
        public String Replace(String input, MatchEvaluator evaluator, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return RegexReplacement.Replace(evaluator, this, input, count, startat);
        }

        /// <summary>
        /// Splits the <paramref name="input "/>string at the position defined
        /// by <paramref name="pattern"/>.
        /// </summary>
        public static String[] Split(String input, String pattern)
        {
            return Split(input, pattern, RegexOptions.None, DefaultMatchTimeout);
        }

        /// <summary>
        /// Splits the <paramref name="input "/>string at the position defined by <paramref name="pattern"/>.
        /// </summary>
        public static String[] Split(String input, String pattern, RegexOptions options)
        {
            return Split(input, pattern, options, DefaultMatchTimeout);
        }

        public static String[] Split(String input, String pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Split(input);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public String[] Split(String input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Split(input, 0, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public String[] Split(String input, int count)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return RegexReplacement.Split(this, input, count, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public String[] Split(String input, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return RegexReplacement.Split(this, input, count, startat);
        }

        internal void InitializeReferences()
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
        internal Match Run(bool quick, int prevlen, String input, int beginning, int length, int startat)
        {
            Match match;
            RegexRunner runner = null;

            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException("start", SR.BeginIndexNotNegative);

            if (length < 0 || length > input.Length)
                throw new ArgumentOutOfRangeException(nameof(length), SR.LengthNotNegative);

            // There may be a cached runner; grab ownership of it if we can.

            runner = (RegexRunner)_runnerref.Get();

            // Create a RegexRunner instance if we need to

            if (runner == null)
            {
                runner = new RegexInterpreter(_code, UseOptionInvariant() ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
            }

            try
            {
                // Do the scan starting at the requested position
                match = runner.Scan(this, input, beginning, beginning + length, startat, prevlen, quick, _internalMatchTimeout);
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
                    newcached = new CachedCodeEntry(key, _capnames, _capslist, _code, _caps, _capsize, _runnerref, _replref);
                    s_livecode.AddFirst(newcached);
                    if (s_livecode.Count > s_cacheSize)
                        s_livecode.RemoveLast();
                }
            }

            return newcached;
        }


        /*
         * True if the L option was set
         */
        internal bool UseOptionR()
        {
            return (_roptions & RegexOptions.RightToLeft) != 0;
        }

        internal bool UseOptionInvariant()
        {
            return (_roptions & RegexOptions.CultureInvariant) != 0;
        }

#if DEBUG
        /*
         * True if the regex has debugging enabled
         */
        internal bool Debug
        {
            get
            {
                return (_roptions & RegexOptions.Debug) != 0;
            }
        }
#endif
    }


    /*
     * Callback class
     */
    public delegate String MatchEvaluator(Match match);

    /*
     * Used as a key for CacheCodeEntry
     */
    internal struct CachedCodeEntryKey : IEquatable<CachedCodeEntryKey>
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
        internal Dictionary<Int32, Int32> _caps;
        internal Dictionary<String, Int32> _capnames;
        internal String[] _capslist;
        internal int _capsize;
        internal ExclusiveReference _runnerref;
        internal SharedReference _replref;

        internal CachedCodeEntry(CachedCodeEntryKey key, Dictionary<String, Int32> capnames, String[] capslist, RegexCode code, Dictionary<Int32, Int32> caps, int capsize, ExclusiveReference runner, SharedReference repl)
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
    }

    /*
     * Used to cache one exclusive runner reference
     */
    internal sealed class ExclusiveReference
    {
        private RegexRunner _ref;
        private Object _obj;
        private int _locked;

        /*
         * Return an object and grab an exclusive lock.
         *
         * If the exclusive lock can't be obtained, null is returned;
         * if the object can't be returned, the lock is released.
         *
         */
        internal Object Get()
        {
            // try to obtain the lock

            if (0 == Interlocked.Exchange(ref _locked, 1))
            {
                // grab reference


                Object obj = _ref;

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
        internal void Release(Object obj)
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
        internal Object Get()
        {
            if (0 == Interlocked.Exchange(ref _locked, 1))
            {
                Object obj = _ref.Target;
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
        internal void Cache(Object obj)
        {
            if (0 == Interlocked.Exchange(ref _locked, 1))
            {
                _ref.Target = obj;
                _locked = 0;
            }
        }
    }
}
