// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The Regex class represents a single compiled instance of a regular
// expression.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
#if FEATURE_COMPILED
using System.Runtime.CompilerServices;
#endif
using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents an immutable, compiled regular expression. Also
    /// contains static methods that allow use of regular expressions without instantiating
    /// a Regex explicitly.
    /// </summary>
    public partial class Regex : ISerializable
    {
        internal const int MaxOptionShift = 10;

        protected internal string pattern;                   // The string pattern provided
        protected internal RegexOptions roptions;            // the top-level options from the options string
        protected internal RegexRunnerFactory factory;
        protected internal Hashtable caps;                   // if captures are sparse, this is the hashtable capnum->index
        protected internal Hashtable capnames;               // if named captures are used, this maps names->index
        protected internal string[] capslist;                // if captures are sparse or named captures are used, this is the sorted list of names
        protected internal int capsize;                      // the size of the capture array
        
        internal ExclusiveReference _runnerref;              // cached runner
        internal WeakReference<RegexReplacement> _replref; // cached parsed replacement pattern
        internal RegexCode _code;                            // if interpreted, this is the code for RegexInterpreter
        internal bool _refsInitialized = false;

        protected Regex()
        {
            internalMatchTimeout = s_defaultMatchTimeout;
        }

        /// <summary>
        /// Creates and compiles a regular expression object for the specified regular
        /// expression.
        /// </summary>
        public Regex(string pattern)
            : this(pattern, RegexOptions.None, s_defaultMatchTimeout, false)
        {
        }

        /// <summary>
        /// Creates and compiles a regular expression object for the
        /// specified regular expression with options that modify the pattern.
        /// </summary>
        public Regex(string pattern, RegexOptions options)
            : this(pattern, options, s_defaultMatchTimeout, false)
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

            // After parameter validation assign 
            this.pattern = pattern;
            roptions = options;
            internalMatchTimeout = matchTimeout;

            // Cache handling. Try to look up this regex in the cache.
            CultureInfo culture = (options & RegexOptions.CultureInvariant) != 0 ?
                CultureInfo.InvariantCulture :
                CultureInfo.CurrentCulture;
            var key = new CachedCodeEntryKey(options, culture.ToString(), pattern);
            CachedCodeEntry cached = GetCachedCode(key, false);

            if (cached == null)
            {
                // Parse the input
                RegexTree tree = RegexParser.Parse(pattern, roptions, culture);

                // Extract the relevant information
                capnames = tree.CapNames;
                capslist = tree.CapsList;
                _code = RegexWriter.Write(tree);
                caps = _code.Caps;
                capsize = _code.CapSize;

                InitializeReferences();

                tree = null;
                if (addToCache)
                    cached = GetCachedCode(key, true);
            }
            else
            {
                caps = cached.Caps;
                capnames = cached.Capnames;
                capslist = cached.Capslist;
                capsize = cached.Capsize;
                _code = cached.Code;
#if FEATURE_COMPILED
                factory = cached.Factory;
#endif

                // Cache runner and replacement
                _runnerref = cached.Runnerref;
                _replref = cached.ReplRef;
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

                caps = value as Hashtable ?? new Hashtable(value);
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

                capnames = value as Hashtable ?? new Hashtable(value);
            }
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

#if FEATURE_COMPILEAPIS
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
#endif // FEATURE_COMPILEAPIS

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
        public override string ToString() => pattern;

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

        protected void InitializeReferences()
        {
            if (_refsInitialized)
                throw new NotSupportedException(SR.OnlyAllowedOnce);

            _refsInitialized = true;
            _runnerref = new ExclusiveReference();
            _replref = new WeakReference<RegexReplacement>(null);
        }

        /// <summary>
        /// Internal worker called by all the public APIs
        /// </summary>
        /// <returns></returns>
        internal Match Run(bool quick, int prevlen, string input, int beginning, int length, int startat)
        {
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            if (length < 0 || length > input.Length)
                throw new ArgumentOutOfRangeException(nameof(length), SR.LengthNotNegative);

            // There may be a cached runner; grab ownership of it if we can.
            RegexRunner runner = _runnerref.Get();

            // Create a RegexRunner instance if we need to
            if (runner == null)
            {
                // Use the compiled RegexRunner factory if the code was compiled to MSIL
                if (factory != null)
                    runner = factory.CreateInstance();
                else
                    runner = new RegexInterpreter(_code, UseOptionInvariant() ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
            }

            Match match;
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

        protected bool UseOptionC() => (roptions & RegexOptions.Compiled) != 0;

        /*
         * True if the L option was set
         */
        protected internal bool UseOptionR() => (roptions & RegexOptions.RightToLeft) != 0;

        internal bool UseOptionInvariant() => (roptions & RegexOptions.CultureInvariant) != 0;

#if DEBUG
        /// <summary>
        /// True if the regex has debugging enabled
        /// </summary>
        internal bool Debug => (roptions & RegexOptions.Debug) != 0;
#endif
    }
}
