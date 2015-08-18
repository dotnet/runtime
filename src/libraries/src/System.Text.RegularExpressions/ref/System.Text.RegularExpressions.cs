// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Text.RegularExpressions
{
    public partial class Capture
    {
        internal Capture() { }
        public int Index { get { return default(int); } }
        public int Length { get { return default(int); } }
        public string Value { get { return default(string); } }
        public override string ToString() { return default(string); }
    }
    public partial class CaptureCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal CaptureCollection() { }
        public int Count { get { return default(int); } }
        public System.Text.RegularExpressions.Capture this[int i] { get { return default(System.Text.RegularExpressions.Capture); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
    }
    public partial class Group : System.Text.RegularExpressions.Capture
    {
        internal Group() { }
        public System.Text.RegularExpressions.CaptureCollection Captures { get { return default(System.Text.RegularExpressions.CaptureCollection); } }
        public string Name { get { return default(string); } }
        public bool Success { get { return default(bool); } }
    }
    public partial class GroupCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal GroupCollection() { }
        public int Count { get { return default(int); } }
        public System.Text.RegularExpressions.Group this[int groupnum] { get { return default(System.Text.RegularExpressions.Group); } }
        public System.Text.RegularExpressions.Group this[string groupname] { get { return default(System.Text.RegularExpressions.Group); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
    }
    public partial class Match : System.Text.RegularExpressions.Group
    {
        internal Match() { }
        public static System.Text.RegularExpressions.Match Empty { get { return default(System.Text.RegularExpressions.Match); } }
        public virtual System.Text.RegularExpressions.GroupCollection Groups { get { return default(System.Text.RegularExpressions.GroupCollection); } }
        public System.Text.RegularExpressions.Match NextMatch() { return default(System.Text.RegularExpressions.Match); }
        public virtual string Result(string replacement) { return default(string); }
    }
    public partial class MatchCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal MatchCollection() { }
        public int Count { get { return default(int); } }
        public virtual System.Text.RegularExpressions.Match this[int i] { get { return default(System.Text.RegularExpressions.Match); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
    }
    public delegate string MatchEvaluator(System.Text.RegularExpressions.Match match);
    public abstract partial class RegexRunnerFactory
    {
        protected RegexRunnerFactory() { }
        protected internal abstract RegexRunner CreateInstance();
    }
    public partial class Regex
    {
        public static readonly System.TimeSpan InfiniteMatchTimeout;
        protected internal string pattern;
        protected internal RegexRunnerFactory factory;
        protected internal RegexOptions roptions;
        protected internal TimeSpan internalMatchTimeout;
        protected internal int capsize;
        protected internal string[] capslist;
        protected System.Collections.IDictionary Caps { get; set; }
        protected System.Collections.IDictionary CapNames { get; set; }
        protected Regex() { }
        public Regex(string pattern) { }
        public Regex(string pattern, System.Text.RegularExpressions.RegexOptions options) { }
        public Regex(string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { }
        public static int CacheSize { get { return default(int); } set { } }
        public System.TimeSpan MatchTimeout { get { return default(System.TimeSpan); } }
        public System.Text.RegularExpressions.RegexOptions Options { get { return default(System.Text.RegularExpressions.RegexOptions); } }
        public bool RightToLeft { get { return default(bool); } }
        protected void InitializeReferences() { }
        protected internal static void ValidateMatchTimeout(TimeSpan matchTimeout) { }
        public static string Escape(string str) { return default(string); }
        public string[] GetGroupNames() { return default(string[]); }
        public int[] GetGroupNumbers() { return default(int[]); }
        public string GroupNameFromNumber(int i) { return default(string); }
        public int GroupNumberFromName(string name) { return default(int); }
        public bool IsMatch(string input) { return default(bool); }
        public bool IsMatch(string input, int startat) { return default(bool); }
        public static bool IsMatch(string input, string pattern) { return default(bool); }
        public static bool IsMatch(string input, string pattern, System.Text.RegularExpressions.RegexOptions options) { return default(bool); }
        public static bool IsMatch(string input, string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { return default(bool); }
        public System.Text.RegularExpressions.Match Match(string input) { return default(System.Text.RegularExpressions.Match); }
        public System.Text.RegularExpressions.Match Match(string input, int startat) { return default(System.Text.RegularExpressions.Match); }
        public System.Text.RegularExpressions.Match Match(string input, int beginning, int length) { return default(System.Text.RegularExpressions.Match); }
        public static System.Text.RegularExpressions.Match Match(string input, string pattern) { return default(System.Text.RegularExpressions.Match); }
        public static System.Text.RegularExpressions.Match Match(string input, string pattern, System.Text.RegularExpressions.RegexOptions options) { return default(System.Text.RegularExpressions.Match); }
        public static System.Text.RegularExpressions.Match Match(string input, string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { return default(System.Text.RegularExpressions.Match); }
        public System.Text.RegularExpressions.MatchCollection Matches(string input) { return default(System.Text.RegularExpressions.MatchCollection); }
        public System.Text.RegularExpressions.MatchCollection Matches(string input, int startat) { return default(System.Text.RegularExpressions.MatchCollection); }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, string pattern) { return default(System.Text.RegularExpressions.MatchCollection); }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, string pattern, System.Text.RegularExpressions.RegexOptions options) { return default(System.Text.RegularExpressions.MatchCollection); }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { return default(System.Text.RegularExpressions.MatchCollection); }
        public string Replace(string input, string replacement) { return default(string); }
        public string Replace(string input, string replacement, int count) { return default(string); }
        public string Replace(string input, string replacement, int count, int startat) { return default(string); }
        public static string Replace(string input, string pattern, string replacement) { return default(string); }
        public static string Replace(string input, string pattern, string replacement, System.Text.RegularExpressions.RegexOptions options) { return default(string); }
        public static string Replace(string input, string pattern, string replacement, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { return default(string); }
        public static string Replace(string input, string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator) { return default(string); }
        public static string Replace(string input, string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator, System.Text.RegularExpressions.RegexOptions options) { return default(string); }
        public static string Replace(string input, string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { return default(string); }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator) { return default(string); }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator, int count) { return default(string); }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator, int count, int startat) { return default(string); }
        public string[] Split(string input) { return default(string[]); }
        public string[] Split(string input, int count) { return default(string[]); }
        public string[] Split(string input, int count, int startat) { return default(string[]); }
        public static string[] Split(string input, string pattern) { return default(string[]); }
        public static string[] Split(string input, string pattern, System.Text.RegularExpressions.RegexOptions options) { return default(string[]); }
        public static string[] Split(string input, string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { return default(string[]); }
        public override string ToString() { return default(string); }
        public static string Unescape(string str) { return default(string); }
    }
    public partial class RegexMatchTimeoutException : System.TimeoutException
    {
        public RegexMatchTimeoutException() { }
        public RegexMatchTimeoutException(string message) { }
        public RegexMatchTimeoutException(string message, System.Exception inner) { }
        public RegexMatchTimeoutException(string regexInput, string regexPattern, System.TimeSpan matchTimeout) { }
        public string Input { get { return default(string); } }
        public System.TimeSpan MatchTimeout { get { return default(System.TimeSpan); } }
        public string Pattern { get { return default(string); } }
    }
    [System.FlagsAttribute]
    public enum RegexOptions
    {
        Compiled = 8,
        CultureInvariant = 512,
        ECMAScript = 256,
        ExplicitCapture = 4,
        IgnoreCase = 1,
        IgnorePatternWhitespace = 32,
        Multiline = 2,
        None = 0,
        RightToLeft = 64,
        Singleline = 16,
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public abstract partial class RegexRunner
    {
        protected internal int[] runcrawl;
        protected internal int runcrawlpos;
        protected internal System.Text.RegularExpressions.Match runmatch;
        protected internal System.Text.RegularExpressions.Regex runregex;
        protected internal int[] runstack;
        protected internal int runstackpos;
        protected internal string runtext;
        protected internal int runtextbeg;
        protected internal int runtextend;
        protected internal int runtextpos;
        protected internal int runtextstart;
        protected internal int[] runtrack;
        protected internal int runtrackcount;
        protected internal int runtrackpos;
        protected internal RegexRunner() { }
        protected void Capture(int capnum, int start, int end) { }
        protected static bool CharInClass(char ch, string charClass) { return default(bool); }
        protected static bool CharInSet(char ch, string @set, string category) { return default(bool); }
        protected void CheckTimeout() { }
        protected void Crawl(int i) { }
        protected int Crawlpos() { return default(int); }
        protected void DoubleCrawl() { }
        protected void DoubleStack() { }
        protected void DoubleTrack() { }
        protected void EnsureStorage() { }
        protected abstract bool FindFirstChar();
        protected abstract void Go();
        protected abstract void InitTrackCount();
        protected bool IsBoundary(int index, int startpos, int endpos) { return default(bool); }
        protected bool IsECMABoundary(int index, int startpos, int endpos) { return default(bool); }
        protected bool IsMatched(int cap) { return default(bool); }
        protected int MatchIndex(int cap) { return default(int); }
        protected int MatchLength(int cap) { return default(int); }
        protected int Popcrawl() { return default(int); }
        protected internal System.Text.RegularExpressions.Match Scan(System.Text.RegularExpressions.Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick) { return default(System.Text.RegularExpressions.Match); }
        protected internal System.Text.RegularExpressions.Match Scan(System.Text.RegularExpressions.Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick, System.TimeSpan timeout) { return default(System.Text.RegularExpressions.Match); }
        protected void TransferCapture(int capnum, int uncapnum, int start, int end) { }
        protected void Uncapture() { }
    }
}
