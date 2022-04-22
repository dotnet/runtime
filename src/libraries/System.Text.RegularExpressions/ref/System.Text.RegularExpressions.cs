// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.RegularExpressions
{
    public partial class Capture
    {
        internal Capture() { }
        public int Index { get { throw null; } }
        public int Length { get { throw null; } }
        public string Value { get { throw null; } }
        public System.ReadOnlySpan<char> ValueSpan { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public partial class CaptureCollection : System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>, System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Capture>, System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>, System.Collections.Generic.IReadOnlyCollection<System.Text.RegularExpressions.Capture>, System.Collections.Generic.IReadOnlyList<System.Text.RegularExpressions.Capture>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal CaptureCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Text.RegularExpressions.Capture this[int i] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        System.Text.RegularExpressions.Capture System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>.this[int index] { get { throw null; } set { } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void CopyTo(System.Array array, int arrayIndex) { }
        public void CopyTo(System.Text.RegularExpressions.Capture[] array, int arrayIndex) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>.Add(System.Text.RegularExpressions.Capture item) { }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>.Contains(System.Text.RegularExpressions.Capture item) { throw null; }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>.Remove(System.Text.RegularExpressions.Capture item) { throw null; }
        System.Collections.Generic.IEnumerator<System.Text.RegularExpressions.Capture> System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Capture>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>.IndexOf(System.Text.RegularExpressions.Capture item) { throw null; }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>.Insert(int index, System.Text.RegularExpressions.Capture item) { }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>.RemoveAt(int index) { }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
    public partial class Group : System.Text.RegularExpressions.Capture
    {
        internal Group() { }
        public System.Text.RegularExpressions.CaptureCollection Captures { get { throw null; } }
        public string Name { get { throw null; } }
        public bool Success { get { throw null; } }
        public static System.Text.RegularExpressions.Group Synchronized(System.Text.RegularExpressions.Group inner) { throw null; }
    }
    public partial class GroupCollection : System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Text.RegularExpressions.Group>>, System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Group>, System.Collections.Generic.IList<System.Text.RegularExpressions.Group>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, System.Text.RegularExpressions.Group>>, System.Collections.Generic.IReadOnlyCollection<System.Text.RegularExpressions.Group>, System.Collections.Generic.IReadOnlyDictionary<string, System.Text.RegularExpressions.Group>, System.Collections.Generic.IReadOnlyList<System.Text.RegularExpressions.Group>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal GroupCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Text.RegularExpressions.Group this[int groupnum] { get { throw null; } }
        public System.Text.RegularExpressions.Group this[string groupname] { get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> Keys { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        System.Text.RegularExpressions.Group System.Collections.Generic.IList<System.Text.RegularExpressions.Group>.this[int index] { get { throw null; } set { } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Group> Values { get { throw null; } }
        public bool ContainsKey(string key) { throw null; }
        public void CopyTo(System.Array array, int arrayIndex) { }
        public void CopyTo(System.Text.RegularExpressions.Group[] array, int arrayIndex) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>.Add(System.Text.RegularExpressions.Group item) { }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>.Contains(System.Text.RegularExpressions.Group item) { throw null; }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>.Remove(System.Text.RegularExpressions.Group item) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, System.Text.RegularExpressions.Group>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Text.RegularExpressions.Group>>.GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Text.RegularExpressions.Group> System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Group>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<System.Text.RegularExpressions.Group>.IndexOf(System.Text.RegularExpressions.Group item) { throw null; }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Group>.Insert(int index, System.Text.RegularExpressions.Group item) { }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Group>.RemoveAt(int index) { }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Text.RegularExpressions.Group? value) { throw null; }
    }
    public partial class Match : System.Text.RegularExpressions.Group
    {
        internal Match() { }
        public static System.Text.RegularExpressions.Match Empty { get { throw null; } }
        public virtual System.Text.RegularExpressions.GroupCollection Groups { get { throw null; } }
        public System.Text.RegularExpressions.Match NextMatch() { throw null; }
        public virtual string Result(string replacement) { throw null; }
        public static System.Text.RegularExpressions.Match Synchronized(System.Text.RegularExpressions.Match inner) { throw null; }
    }
    public partial class MatchCollection : System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>, System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Match>, System.Collections.Generic.IList<System.Text.RegularExpressions.Match>, System.Collections.Generic.IReadOnlyCollection<System.Text.RegularExpressions.Match>, System.Collections.Generic.IReadOnlyList<System.Text.RegularExpressions.Match>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        internal MatchCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public virtual System.Text.RegularExpressions.Match this[int i] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        System.Text.RegularExpressions.Match System.Collections.Generic.IList<System.Text.RegularExpressions.Match>.this[int index] { get { throw null; } set { } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void CopyTo(System.Array array, int arrayIndex) { }
        public void CopyTo(System.Text.RegularExpressions.Match[] array, int arrayIndex) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>.Add(System.Text.RegularExpressions.Match item) { }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>.Contains(System.Text.RegularExpressions.Match item) { throw null; }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>.Remove(System.Text.RegularExpressions.Match item) { throw null; }
        System.Collections.Generic.IEnumerator<System.Text.RegularExpressions.Match> System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Match>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<System.Text.RegularExpressions.Match>.IndexOf(System.Text.RegularExpressions.Match item) { throw null; }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Match>.Insert(int index, System.Text.RegularExpressions.Match item) { }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Match>.RemoveAt(int index) { }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
    public delegate string MatchEvaluator(System.Text.RegularExpressions.Match match);
    public partial class Regex : System.Runtime.Serialization.ISerializable
    {
        protected internal System.Collections.Hashtable? capnames;
        protected internal System.Collections.Hashtable? caps;
        protected internal int capsize;
        protected internal string[]? capslist;
        protected internal System.Text.RegularExpressions.RegexRunnerFactory? factory;
        public static readonly System.TimeSpan InfiniteMatchTimeout;
        protected internal System.TimeSpan internalMatchTimeout;
        [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
        protected internal string? pattern;
        protected internal System.Text.RegularExpressions.RegexOptions roptions;
        protected Regex() { }
        protected Regex(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public Regex([System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)] string pattern) { }
        public Regex([System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex, "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { }
        public Regex([System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex, "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { }
        public static int CacheSize { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        protected System.Collections.IDictionary? CapNames { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        protected System.Collections.IDictionary? Caps { get { throw null; } set { } }
        public System.TimeSpan MatchTimeout { get { throw null; } }
        public System.Text.RegularExpressions.RegexOptions Options { get { throw null; } }
        public bool RightToLeft { get { throw null; } }
        [System.ObsoleteAttribute("Regex.CompileToAssembly is obsolete and not supported. Use the RegexGeneratorAttribute with the regular expression source generator instead.", DiagnosticId = "SYSLIB0036", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static void CompileToAssembly(System.Text.RegularExpressions.RegexCompilationInfo[] regexinfos, System.Reflection.AssemblyName assemblyname) { }
        [System.ObsoleteAttribute("Regex.CompileToAssembly is obsolete and not supported. Use the RegexGeneratorAttribute with the regular expression source generator instead.", DiagnosticId = "SYSLIB0036", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static void CompileToAssembly(System.Text.RegularExpressions.RegexCompilationInfo[] regexinfos, System.Reflection.AssemblyName assemblyname, System.Reflection.Emit.CustomAttributeBuilder[]? attributes) { }
        [System.ObsoleteAttribute("Regex.CompileToAssembly is obsolete and not supported. Use the RegexGeneratorAttribute with the regular expression source generator instead.", DiagnosticId = "SYSLIB0036", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static void CompileToAssembly(System.Text.RegularExpressions.RegexCompilationInfo[] regexinfos, System.Reflection.AssemblyName assemblyname, System.Reflection.Emit.CustomAttributeBuilder[]? attributes, string? resourceFile) { }
        public int Count(string input) { throw null; }
        public int Count(System.ReadOnlySpan<char> input) { throw null; }
        public static int Count(string input, [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)] string pattern) { throw null; }
        public static int Count(string input, [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex, "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static int Count(string input, [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex, "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public static int Count(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)] string pattern) { throw null; }
        public static int Count(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex, "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static int Count(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex, "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public static string Escape(string str) { throw null; }
        public System.Text.RegularExpressions.Regex.ValueMatchEnumerator EnumerateMatches(System.ReadOnlySpan<char> input) { throw null; }
        public static System.Text.RegularExpressions.Regex.ValueMatchEnumerator EnumerateMatches(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern) { throw null; }
        public static System.Text.RegularExpressions.Regex.ValueMatchEnumerator EnumerateMatches(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", new object[]{ "options"})] string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static System.Text.RegularExpressions.Regex.ValueMatchEnumerator EnumerateMatches(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", new object[]{ "options"})] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public string[] GetGroupNames() { throw null; }
        public int[] GetGroupNumbers() { throw null; }
        public string GroupNameFromNumber(int i) { throw null; }
        public int GroupNumberFromName(string name) { throw null; }
        protected void InitializeReferences() { }
        public bool IsMatch(System.ReadOnlySpan<char> input) { throw null; }
        public static bool IsMatch(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern) { throw null; }
        public static bool IsMatch(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static bool IsMatch(System.ReadOnlySpan<char> input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public bool IsMatch(string input) { throw null; }
        public bool IsMatch(string input, int startat) { throw null; }
        public static bool IsMatch(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern) { throw null; }
        public static bool IsMatch(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static bool IsMatch(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public System.Text.RegularExpressions.Match Match(string input) { throw null; }
        public System.Text.RegularExpressions.Match Match(string input, int startat) { throw null; }
        public System.Text.RegularExpressions.Match Match(string input, int beginning, int length) { throw null; }
        public static System.Text.RegularExpressions.Match Match(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern) { throw null; }
        public static System.Text.RegularExpressions.Match Match(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static System.Text.RegularExpressions.Match Match(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public System.Text.RegularExpressions.MatchCollection Matches(string input) { throw null; }
        public System.Text.RegularExpressions.MatchCollection Matches(string input, int startat) { throw null; }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern) { throw null; }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static System.Text.RegularExpressions.MatchCollection Matches(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public string Replace(string input, string replacement) { throw null; }
        public string Replace(string input, string replacement, int count) { throw null; }
        public string Replace(string input, string replacement, int count, int startat) { throw null; }
        public static string Replace(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern, string replacement) { throw null; }
        public static string Replace(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, string replacement, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static string Replace(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, string replacement, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public static string Replace(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator) { throw null; }
        public static string Replace(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static string Replace(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.MatchEvaluator evaluator, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator) { throw null; }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator, int count) { throw null; }
        public string Replace(string input, System.Text.RegularExpressions.MatchEvaluator evaluator, int count, int startat) { throw null; }
        public string[] Split(string input) { throw null; }
        public string[] Split(string input, int count) { throw null; }
        public string[] Split(string input, int count, int startat) { throw null; }
        public static string[] Split(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern) { throw null; }
        public static string[] Split(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { throw null; }
        public static string[] Split(string input, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex", "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, System.TimeSpan matchTimeout) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
        public static string Unescape(string str) { throw null; }
        protected bool UseOptionC() { throw null; }
        protected internal bool UseOptionR() { throw null; }
        protected internal static void ValidateMatchTimeout(System.TimeSpan matchTimeout) { }
        public ref partial struct ValueMatchEnumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public readonly System.Text.RegularExpressions.ValueMatch Current { get { throw null; } }
            public readonly System.Text.RegularExpressions.Regex.ValueMatchEnumerator GetEnumerator() { throw null; }
            public bool MoveNext() { throw null; }
        }
    }
    [System.ObsoleteAttribute("Regex.CompileToAssembly is obsolete and not supported. Use the RegexGeneratorAttribute with the regular expression source generator instead.", DiagnosticId = "SYSLIB0036", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public partial class RegexCompilationInfo
    {
        public RegexCompilationInfo(string pattern, System.Text.RegularExpressions.RegexOptions options, string name, string fullnamespace, bool ispublic) { }
        public RegexCompilationInfo(string pattern, System.Text.RegularExpressions.RegexOptions options, string name, string fullnamespace, bool ispublic, System.TimeSpan matchTimeout) { }
        public bool IsPublic { get { throw null; } set { } }
        public System.TimeSpan MatchTimeout { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        public System.Text.RegularExpressions.RegexOptions Options { get { throw null; } set { } }
        public string Pattern { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed partial class RegexGeneratorAttribute : System.Attribute
    {
        public RegexGeneratorAttribute([System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)] string pattern) { }
        public RegexGeneratorAttribute([System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex, "options")] string pattern, System.Text.RegularExpressions.RegexOptions options) { }
        public RegexGeneratorAttribute([System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex, "options")] string pattern, System.Text.RegularExpressions.RegexOptions options, int matchTimeoutMilliseconds) { }
        public string Pattern { get; }
        public System.Text.RegularExpressions.RegexOptions Options { get; }
        public int MatchTimeoutMilliseconds { get; }
    }
    public partial class RegexMatchTimeoutException : System.TimeoutException, System.Runtime.Serialization.ISerializable
    {
        public RegexMatchTimeoutException() { }
        protected RegexMatchTimeoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public RegexMatchTimeoutException(string message) { }
        public RegexMatchTimeoutException(string message, System.Exception inner) { }
        public RegexMatchTimeoutException(string regexInput, string regexPattern, System.TimeSpan matchTimeout) { }
        public string Input { get { throw null; } }
        public System.TimeSpan MatchTimeout { get { throw null; } }
        public string Pattern { get { throw null; } }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    [System.FlagsAttribute]
    public enum RegexOptions
    {
        None = 0,
        IgnoreCase = 1,
        Multiline = 2,
        ExplicitCapture = 4,
        Compiled = 8,
        Singleline = 16,
        IgnorePatternWhitespace = 32,
        RightToLeft = 64,
        ECMAScript = 256,
        CultureInvariant = 512,
        NonBacktracking = 1024,
    }
    public enum RegexParseError
    {
        Unknown = 0,
        AlternationHasTooManyConditions = 1,
        AlternationHasMalformedCondition = 2,
        InvalidUnicodePropertyEscape = 3,
        MalformedUnicodePropertyEscape = 4,
        UnrecognizedEscape = 5,
        UnrecognizedControlCharacter = 6,
        MissingControlCharacter = 7,
        InsufficientOrInvalidHexDigits = 8,
        QuantifierOrCaptureGroupOutOfRange = 9,
        UndefinedNamedReference = 10,
        UndefinedNumberedReference = 11,
        MalformedNamedReference = 12,
        UnescapedEndingBackslash = 13,
        UnterminatedComment = 14,
        InvalidGroupingConstruct = 15,
        AlternationHasNamedCapture = 16,
        AlternationHasComment = 17,
        AlternationHasMalformedReference = 18,
        AlternationHasUndefinedReference = 19,
        CaptureGroupNameInvalid = 20,
        CaptureGroupOfZero = 21,
        UnterminatedBracket = 22,
        ExclusionGroupNotLast = 23,
        ReversedCharacterRange = 24,
        ShorthandClassInCharacterRange = 25,
        InsufficientClosingParentheses = 26,
        ReversedQuantifierRange = 27,
        NestedQuantifiersNotParenthesized = 28,
        QuantifierAfterNothing = 29,
        InsufficientOpeningParentheses = 30,
        UnrecognizedUnicodeProperty = 31,
    }
    public sealed partial class RegexParseException : System.ArgumentException
    {
        private RegexParseException() { }
        public System.Text.RegularExpressions.RegexParseError Error { get { throw null; } }
        public int Offset { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class RegexRunner
    {
        protected internal int[]? runcrawl;
        protected internal int runcrawlpos;
        protected internal System.Text.RegularExpressions.Match? runmatch;
        protected internal System.Text.RegularExpressions.Regex? runregex;
        protected internal int[]? runstack;
        protected internal int runstackpos;
        protected internal string? runtext;
        protected internal int runtextbeg;
        protected internal int runtextend;
        protected internal int runtextpos;
        protected internal int runtextstart;
        protected internal int[]? runtrack;
        protected internal int runtrackcount;
        protected internal int runtrackpos;
        protected internal RegexRunner() { }
        protected void Capture(int capnum, int start, int end) { }
        protected static bool CharInClass(char ch, string charClass) { throw null; }
        protected static bool CharInSet(char ch, string @set, string category) { throw null; }
        protected void CheckTimeout() { }
        protected void Crawl(int i) { }
        protected int Crawlpos() { throw null; }
        protected void DoubleCrawl() { }
        protected void DoubleStack() { }
        protected void DoubleTrack() { }
        protected void EnsureStorage() { }
        protected virtual bool FindFirstChar() { throw null; }
        protected virtual void Go() { throw null; }
        protected virtual void InitTrackCount() { throw null; }
        protected bool IsBoundary(int index, int startpos, int endpos) { throw null; }
        protected bool IsECMABoundary(int index, int startpos, int endpos) { throw null; }
        protected bool IsMatched(int cap) { throw null; }
        protected int MatchIndex(int cap) { throw null; }
        protected int MatchLength(int cap) { throw null; }
        protected int Popcrawl() { throw null; }
        protected internal System.Text.RegularExpressions.Match? Scan(System.Text.RegularExpressions.Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick) { throw null; }
        protected internal System.Text.RegularExpressions.Match? Scan(System.Text.RegularExpressions.Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick, System.TimeSpan timeout) { throw null; }
        protected internal virtual void Scan(System.ReadOnlySpan<char> text) { throw null; }
        protected void TransferCapture(int capnum, int uncapnum, int start, int end) { }
        protected void Uncapture() { }
    }
    public abstract partial class RegexRunnerFactory
    {
        protected RegexRunnerFactory() { }
        protected internal abstract System.Text.RegularExpressions.RegexRunner CreateInstance();
    }
    public readonly ref partial struct ValueMatch
    {
        private readonly int _dummyPrimitive;
        public int Index { get { throw null; } }
        public int Length { get { throw null; } }
    }
}
