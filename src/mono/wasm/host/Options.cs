// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Options.cs
//
// Authors:
//  Jonathan Pryor <jpryor@novell.com>, <Jonathan.Pryor@microsoft.com>
//  Federico Di Gregorio <fog@initd.org>
//  Rolf Bjarne Kvinge <rolf@xamarin.com>
//
// Copyright (C) 2008 Novell (http://www.novell.com)
// Copyright (C) 2009 Federico Di Gregorio.
// Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
// Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// Compile With:
//   mcs -debug+ -r:System.Core Options.cs -o:Mono.Options.dll -t:library
//   mcs -debug+ -d:LINQ -r:System.Core Options.cs -o:Mono.Options.dll -t:library
//
// The LINQ version just changes the implementation of
// OptionSet.Parse(IEnumerable<string>), and confers no semantic changes.

//
// A Getopt::Long-inspired option parsing library for C#.
//
// Mono.Options.OptionSet is built upon a key/value table, where the
// key is a option format string and the value is a delegate that is
// invoked when the format string is matched.
//
// Option format strings:
//  Regex-like BNF Grammar:
//    name: .+
//    type: [=:]
//    sep: ( [^{}]+ | '{' .+ '}' )?
//    aliases: ( name type sep ) ( '|' name type sep )*
//
// Each '|'-delimited name is an alias for the associated action.  If the
// format string ends in a '=', it has a required value.  If the format
// string ends in a ':', it has an optional value.  If neither '=' or ':'
// is present, no value is supported.  `=' or `:' need only be defined on one
// alias, but if they are provided on more than one they must be consistent.
//
// Each alias portion may also end with a "key/value separator", which is used
// to split option values if the option accepts > 1 value.  If not specified,
// it defaults to '=' and ':'.  If specified, it can be any character except
// '{' and '}' OR the *string* between '{' and '}'.  If no separator should be
// used (i.e. the separate values should be distinct arguments), then "{}"
// should be used as the separator.
//
// Options are extracted either from the current option by looking for
// the option name followed by an '=' or ':', or is taken from the
// following option IFF:
//  - The current option does not contain a '=' or a ':'
//  - The current option requires a value (i.e. not a Option type of ':')
//
// The `name' used in the option format string does NOT include any leading
// option indicator, such as '-', '--', or '/'.  All three of these are
// permitted/required on any named option.
//
// Option bundling is permitted so long as:
//   - '-' is used to start the option group
//   - all of the bundled options are a single character
//   - at most one of the bundled options accepts a value, and the value
//     provided starts from the next character to the end of the string.
//
// This allows specifying '-a -b -c' as '-abc', and specifying '-D name=value'
// as '-Dname=value'.
//
// Option processing is disabled by specifying "--".  All options after "--"
// are returned by OptionSet.Parse() unchanged and unprocessed.
//
// Unprocessed options are returned from OptionSet.Parse().
//
// Examples:
//  int verbose = 0;
//  OptionSet p = new OptionSet ()
//    .Add ("v", v => ++verbose)
//    .Add ("name=|value=", v => Console.WriteLine (v));
//  p.Parse (new string[]{"-v", "--v", "/v", "-name=A", "/name", "B", "extra"});
//
// The above would parse the argument string array, and would invoke the
// lambda expression three times, setting `verbose' to 3 when complete.
// It would also print out "A" and "B" to standard output.
// The returned array would contain the string "extra".
//
// C# 3.0 collection initializers are supported and encouraged:
//  var p = new OptionSet () {
//    { "h|?|help", v => ShowHelp () },
//  };
//
// System.ComponentModel.TypeConverter is also supported, allowing the use of
// custom data types in the callback type; TypeConverter.ConvertFromString()
// is used to convert the value option to an instance of the specified
// type:
//
//  var p = new OptionSet () {
//    { "foo=", (Foo f) => Console.WriteLine (f.ToString ()) },
//  };
//
// Random other tidbits:
//  - Boolean options (those w/o '=' or ':' in the option format string)
//    are explicitly enabled if they are followed with '+', and explicitly
//    disabled if they are followed with '-':
//      string a = null;
//      var p = new OptionSet () {
//        { "a", s => a = s },
//      };
//      p.Parse (new string[]{"-a"});   // sets v != null
//      p.Parse (new string[]{"-a+"});  // sets v != null
//      p.Parse (new string[]{"-a-"});  // sets v == null
//

//
// Mono.Options.CommandSet allows easily having separate commands and
// associated command options, allowing creation of a *suite* along the
// lines of **git**(1), **svn**(1), etc.
//
// CommandSet allows intermixing plain text strings for `--help` output,
// Option values -- as supported by OptionSet -- and Command instances,
// which have a name, optional help text, and an optional OptionSet.
//
//  var suite = new CommandSet ("suite-name") {
//    // Use strings and option values, as with OptionSet
//    "usage: suite-name COMMAND [OPTIONS]+",
//    { "v:", "verbosity", (int? v) => Verbosity = v.HasValue ? v.Value : Verbosity+1 },
//    // Commands may also be specified
//    new Command ("command-name", "command help") {
//      Options = new OptionSet {/*...*/},
//      Run     = args => { /*...*/},
//    },
//    new MyCommandSubclass (),
//  };
//  return suite.Run (new string[]{...});
//
// CommandSet provides a `help` command, and forwards `help COMMAND`
// to the registered Command instance by invoking Command.Invoke()
// with `--help` as an option.
//

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
#if PCL
using System.Reflection;
#else
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif
using System.Text;
using System.Text.RegularExpressions;

#if LINQ
using System.Linq;
#endif

#if TEST
using NDesk.Options;
#endif

#if PCL
using MessageLocalizerConverter = System.Func<string, string>;
#else
using MessageLocalizerConverter = System.Converter<string, string>;
#endif

#if NDESK_OPTIONS
namespace NDesk.Options
#else
namespace Mono.Options
#endif
{
    internal static class StringCoda
    {

        public static IEnumerable<string> WrappedLines(string self, params int[] widths)
        {
            IEnumerable<int> w = widths;
            return WrappedLines(self, w);
        }

        public static IEnumerable<string> WrappedLines(string self, IEnumerable<int> widths)
        {
            if (widths == null)
                throw new ArgumentNullException(nameof(widths));
            return CreateWrappedLinesIterator(self, widths);
        }

        private static IEnumerable<string> CreateWrappedLinesIterator(string self, IEnumerable<int> widths)
        {
            if (string.IsNullOrEmpty(self))
            {
                yield return string.Empty;
                yield break;
            }
            using (IEnumerator<int> ewidths = widths.GetEnumerator())
            {
                bool? hw = null;
                int width = GetNextWidth(ewidths, int.MaxValue, ref hw);
                int start = 0, end;
                do
                {
                    end = GetLineEnd(start, width, self);
                    // endCorrection is 1 if the line end is '\n', and might be 2 if the line end is '\r\n'.
                    int endCorrection = 1;
                    if (end >= 2 && self.Substring(end - 2, 2).Equals("\r\n"))
                        endCorrection = 2;
                    char c = self[end - endCorrection];
                    if (char.IsWhiteSpace(c))
                        end -= endCorrection;
                    bool needContinuation = end != self.Length && !IsEolChar(c);
                    string continuation = "";
                    if (needContinuation)
                    {
                        --end;
                        continuation = "-";
                    }
                    string line = string.Concat(self.AsSpan(start, end - start), continuation);
                    yield return line;
                    start = end;
                    if (char.IsWhiteSpace(c))
                        start += endCorrection;
                    width = GetNextWidth(ewidths, width, ref hw);
                } while (start < self.Length);
            }
        }

        private static int GetNextWidth(IEnumerator<int> ewidths, int curWidth, ref bool? eValid)
        {
            if (!eValid.HasValue || (eValid.HasValue && eValid.Value))
            {
                curWidth = (eValid = ewidths.MoveNext()).Value ? ewidths.Current : curWidth;
                // '.' is any character, - is for a continuation
                const string minWidth = ".-";
                if (curWidth < minWidth.Length)
                    throw new ArgumentOutOfRangeException("widths",
                            string.Format("Element must be >= {0}, was {1}.", minWidth.Length, curWidth));
                return curWidth;
            }
            // no more elements, use the last element.
            return curWidth;
        }

        private static bool IsEolChar(char c)
        {
            return !char.IsLetterOrDigit(c);
        }

        private static int GetLineEnd(int start, int length, string description)
        {
            int end = System.Math.Min(start + length, description.Length);
            int sep = -1;
            for (int i = start; i < end; ++i)
            {
                if (i + 2 <= description.Length && description.Substring(i, 2).Equals("\r\n"))
                    return i + 2;
                if (description[i] == '\n')
                    return i + 1;
                if (IsEolChar(description[i]))
                    sep = i + 1;
            }
            if (sep == -1 || end == description.Length)
                return end;
            return sep;
        }
    }

    public class OptionValueCollection : IList, IList<string>
    {

        private List<string> values = new List<string>();
        private OptionContext c;

        internal OptionValueCollection(OptionContext c)
        {
            this.c = c;
        }

        #region ICollection
        void ICollection.CopyTo(Array array, int index) { (values as ICollection).CopyTo(array, index); }
        bool ICollection.IsSynchronized { get { return (values as ICollection).IsSynchronized; } }
        object ICollection.SyncRoot { get { return (values as ICollection).SyncRoot; } }
        #endregion

        #region ICollection<T>
        public void Add(string item) { values.Add(item); }
        public void Clear() { values.Clear(); }
        public bool Contains(string item) { return values.Contains(item); }
        public void CopyTo(string[] array, int arrayIndex) { values.CopyTo(array, arrayIndex); }
        public bool Remove(string item) { return values.Remove(item); }
        public int Count { get { return values.Count; } }
        public bool IsReadOnly { get { return false; } }
        #endregion

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator() { return values.GetEnumerator(); }
        #endregion

        #region IEnumerable<T>
        public IEnumerator<string> GetEnumerator() { return values.GetEnumerator(); }
        #endregion

        #region IList
        int IList.Add(object value) { return (values as IList).Add(value); }
        bool IList.Contains(object value) { return (values as IList).Contains(value); }
        int IList.IndexOf(object value) { return (values as IList).IndexOf(value); }
        void IList.Insert(int index, object value) { (values as IList).Insert(index, value); }
        void IList.Remove(object value) { (values as IList).Remove(value); }
        void IList.RemoveAt(int index) { (values as IList).RemoveAt(index); }
        bool IList.IsFixedSize { get { return false; } }
        object IList.this[int index] { get { return this[index]; } set { (values as IList)[index] = value; } }
        #endregion

        #region IList<T>
        public int IndexOf(string item) { return values.IndexOf(item); }
        public void Insert(int index, string item) { values.Insert(index, item); }
        public void RemoveAt(int index) { values.RemoveAt(index); }

        private void AssertValid(int index)
        {
            if (c.Option == null)
                throw new InvalidOperationException("OptionContext.Option is null.");
            if (index >= c.Option.MaxValueCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (c.Option.OptionValueType == OptionValueType.Required &&
                    index >= values.Count)
                throw new OptionException(string.Format(
                            c.OptionSet.MessageLocalizer("Missing required value for option '{0}'."), c.OptionName),
                        c.OptionName);
        }

        public string this[int index]
        {
            get
            {
                AssertValid(index);
                return index >= values.Count ? null : values[index];
            }
            set
            {
                values[index] = value;
            }
        }
        #endregion

        public List<string> ToList()
        {
            return new List<string>(values);
        }

        public string[] ToArray()
        {
            return values.ToArray();
        }

        public override string ToString()
        {
            return string.Join(", ", values.ToArray());
        }
    }

    public class OptionContext
    {
        private Option option;
        private string name;
        private int index;
        private OptionSet set;
        private OptionValueCollection c;

        public OptionContext(OptionSet set)
        {
            this.set = set;
            this.c = new OptionValueCollection(this);
        }

        public Option Option
        {
            get { return option; }
            set { option = value; }
        }

        public string OptionName
        {
            get { return name; }
            set { name = value; }
        }

        public int OptionIndex
        {
            get { return index; }
            set { index = value; }
        }

        public OptionSet OptionSet
        {
            get { return set; }
        }

        public OptionValueCollection OptionValues
        {
            get { return c; }
        }
    }

    public enum OptionValueType
    {
        None,
        Optional,
        Required,
    }

    public abstract class Option
    {
        private string prototype, description;
        private string[] names;
        private OptionValueType type;
        private int count;
        private string[] separators;
        private bool hidden;

        protected Option(string prototype, string description)
            : this(prototype, description, 1, false)
        {
        }

        protected Option(string prototype, string description, int maxValueCount)
            : this(prototype, description, maxValueCount, false)
        {
        }

        protected Option(string prototype, string description, int maxValueCount, bool hidden)
        {
            if (prototype == null)
                throw new ArgumentNullException(nameof(prototype));
            if (prototype.Length == 0)
                throw new ArgumentException("Cannot be the empty string.", nameof(prototype));
            if (maxValueCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValueCount));

            this.prototype = prototype;
            this.description = description;
            this.count = maxValueCount;
            this.names = (this is OptionSet.Category)
                // append GetHashCode() so that "duplicate" categories have distinct
                // names, e.g. adding multiple "" categories should be valid.
                ? new[] { prototype + this.GetHashCode() }
                : prototype.Split('|');

            if (this is OptionSet.Category || this is CommandOption)
                return;

            this.type = ParsePrototype();
            this.hidden = hidden;

            if (this.count == 0 && type != OptionValueType.None)
                throw new ArgumentException(
                        "Cannot provide maxValueCount of 0 for OptionValueType.Required or " +
                            "OptionValueType.Optional.",
                        nameof(maxValueCount));
            if (this.type == OptionValueType.None && maxValueCount > 1)
                throw new ArgumentException(
                        string.Format("Cannot provide maxValueCount of {0} for OptionValueType.None.", maxValueCount),
                        nameof(maxValueCount));
            if (Array.IndexOf(names, "<>") >= 0 &&
                    ((names.Length == 1 && this.type != OptionValueType.None) ||
                     (names.Length > 1 && this.MaxValueCount > 1)))
                throw new ArgumentException(
                        "The default option handler '<>' cannot require values.",
                        nameof(prototype));
        }

        public string Prototype { get { return prototype; } }
        public string Description { get { return description; } }
        public OptionValueType OptionValueType { get { return type; } }
        public int MaxValueCount { get { return count; } }
        public bool Hidden { get { return hidden; } }

        public string[] GetNames()
        {
            return (string[])names.Clone();
        }

        public string[] GetValueSeparators()
        {
            if (separators == null)
                return Array.Empty<string>();
            return (string[])separators.Clone();
        }

        protected static T Parse<T>(string value, OptionContext c)
        {
            Type tt = typeof(T);
#if PCL
			TypeInfo ti = tt.GetTypeInfo ();
#else
            Type ti = tt;
#endif
            bool nullable =
                ti.IsValueType &&
                ti.IsGenericType &&
                !ti.IsGenericTypeDefinition &&
                ti.GetGenericTypeDefinition() == typeof(Nullable<>);
#if PCL
			Type targetType = nullable ? tt.GenericTypeArguments [0] : tt;
#else
            Type targetType = nullable ? tt.GetGenericArguments()[0] : tt;
#endif
            T t = default(T);
            try
            {
                if (value != null)
                {
#if PCL
					if (targetType.GetTypeInfo ().IsEnum)
						t = (T) Enum.Parse (targetType, value, true);
					else
						t = (T) Convert.ChangeType (value, targetType);
#else
                    TypeConverter conv = TypeDescriptor.GetConverter(targetType);
                    t = (T)conv.ConvertFromString(value);
#endif
                }
            }
            catch (Exception e)
            {
                throw new OptionException(
                        string.Format(
                            c.OptionSet.MessageLocalizer("Could not convert string `{0}' to type {1} for option `{2}'."),
                            value, targetType.Name, c.OptionName),
                        c.OptionName, e);
            }
            return t;
        }

        internal string[] Names { get { return names; } }
        internal string[] ValueSeparators { get { return separators; } }

        private static readonly char[] NameTerminator = new char[] { '=', ':' };

        private OptionValueType ParsePrototype()
        {
            char type = '\0';
            List<string> seps = new List<string>();
            for (int i = 0; i < names.Length; ++i)
            {
                string name = names[i];
                if (name.Length == 0)
                    throw new ArgumentException("Empty option names are not supported.", "prototype");

                int end = name.IndexOfAny(NameTerminator);
                if (end == -1)
                    continue;
                names[i] = name.Substring(0, end);
                if (type == '\0' || type == name[end])
                    type = name[end];
                else
                    throw new ArgumentException(
                            string.Format("Conflicting option types: '{0}' vs. '{1}'.", type, name[end]),
                            "prototype");
                AddSeparators(name, end, seps);
            }

            if (type == '\0')
                return OptionValueType.None;

            if (count <= 1 && seps.Count != 0)
                throw new ArgumentException(
                        string.Format("Cannot provide key/value separators for Options taking {0} value(s).", count),
                        "prototype");
            if (count > 1)
            {
                if (seps.Count == 0)
                    this.separators = new string[] { ":", "=" };
                else if (seps.Count == 1 && seps[0].Length == 0)
                    this.separators = null;
                else
                    this.separators = seps.ToArray();
            }

            return type == '=' ? OptionValueType.Required : OptionValueType.Optional;
        }

        private static void AddSeparators(string name, int end, ICollection<string> seps)
        {
            int start = -1;
            for (int i = end + 1; i < name.Length; ++i)
            {
                switch (name[i])
                {
                    case '{':
                        if (start != -1)
                            throw new ArgumentException(
                                    string.Format("Ill-formed name/value separator found in \"{0}\".", name),
                                    "prototype");
                        start = i + 1;
                        break;
                    case '}':
                        if (start == -1)
                            throw new ArgumentException(
                                    string.Format("Ill-formed name/value separator found in \"{0}\".", name),
                                    "prototype");
                        seps.Add(name.Substring(start, i - start));
                        start = -1;
                        break;
                    default:
                        if (start == -1)
                            seps.Add(name[i].ToString());
                        break;
                }
            }
            if (start != -1)
                throw new ArgumentException(
                        string.Format("Ill-formed name/value separator found in \"{0}\".", name),
                        "prototype");
        }

        public void Invoke(OptionContext c)
        {
            OnParseComplete(c);
            c.OptionName = null;
            c.Option = null;
            c.OptionValues.Clear();
        }

        protected abstract void OnParseComplete(OptionContext c);

        internal void InvokeOnParseComplete(OptionContext c)
        {
            OnParseComplete(c);
        }

        public override string ToString()
        {
            return Prototype;
        }
    }

    public abstract class ArgumentSource
    {

        protected ArgumentSource()
        {
        }

        public abstract string[] GetNames();
        public abstract string Description { get; }
        public abstract bool GetArguments(string value, out IEnumerable<string> replacement);

#if !PCL || NETSTANDARD1_3
        public static IEnumerable<string> GetArgumentsFromFile(string file)
        {
            return GetArguments(File.OpenText(file), true);
        }
#endif

        public static IEnumerable<string> GetArguments(TextReader reader)
        {
            return GetArguments(reader, false);
        }

        // Cribbed from mcs/driver.cs:LoadArgs(string)
        private static IEnumerable<string> GetArguments(TextReader reader, bool close)
        {
            try
            {
                StringBuilder arg = new StringBuilder();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int t = line.Length;

                    for (int i = 0; i < t; i++)
                    {
                        char c = line[i];

                        if (c == '"' || c == '\'')
                        {
                            char end = c;

                            for (i++; i < t; i++)
                            {
                                c = line[i];

                                if (c == end)
                                    break;
                                arg.Append(c);
                            }
                        }
                        else if (c == ' ')
                        {
                            if (arg.Length > 0)
                            {
                                yield return arg.ToString();
                                arg.Length = 0;
                            }
                        }
                        else
                            arg.Append(c);
                    }
                    if (arg.Length > 0)
                    {
                        yield return arg.ToString();
                        arg.Length = 0;
                    }
                }
            }
            finally
            {
                if (close)
                    reader.Dispose();
            }
        }
    }

#if !PCL || NETSTANDARD1_3
    public class ResponseFileSource : ArgumentSource
    {

        public override string[] GetNames()
        {
            return new string[] { "@file" };
        }

        public override string Description
        {
            get { return "Read response file for more options."; }
        }

        public override bool GetArguments(string value, out IEnumerable<string> replacement)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("@"))
            {
                replacement = null;
                return false;
            }
            replacement = ArgumentSource.GetArgumentsFromFile(value.Substring(1));
            return true;
        }
    }
#endif

#if !PCL
    [Serializable]
#endif
    public class OptionException : Exception
    {
        private string option;

        public OptionException()
        {
        }

        public OptionException(string message, string optionName)
            : base(message)
        {
            this.option = optionName;
        }

        public OptionException(string message, string optionName, Exception innerException)
            : base(message, innerException)
        {
            this.option = optionName;
        }

#if !PCL
        protected OptionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.option = info.GetString("OptionName");
        }
#endif

        public string OptionName
        {
            get { return this.option; }
        }

#if !PCL
#pragma warning disable 618 // SecurityPermissionAttribute is obsolete
        // [SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
#pragma warning restore 618
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("OptionName", option);
        }
#endif
    }

    public delegate void OptionAction<TKey, TValue>(TKey key, TValue value);

    public class OptionSet : KeyedCollection<string, Option>
    {
        public OptionSet()
            : this(null, null)
        {
        }

        public OptionSet(MessageLocalizerConverter localizer)
            : this(localizer, null)
        {
        }

        public OptionSet(StringComparer comparer)
            : this(null, comparer)
        {
        }

        public OptionSet(MessageLocalizerConverter localizer, StringComparer comparer)
            : base(comparer)
        {
            this.roSources = new ReadOnlyCollection<ArgumentSource>(sources);
            this.localizer = localizer;
            if (this.localizer == null)
            {
                this.localizer = delegate (string f)
                {
                    return f;
                };
            }
        }

        private MessageLocalizerConverter localizer;

        public MessageLocalizerConverter MessageLocalizer
        {
            get { return localizer; }
            internal set { localizer = value; }
        }

        private List<ArgumentSource> sources = new List<ArgumentSource>();
        private ReadOnlyCollection<ArgumentSource> roSources;

        public ReadOnlyCollection<ArgumentSource> ArgumentSources
        {
            get { return roSources; }
        }


        protected override string GetKeyForItem(Option item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Names != null && item.Names.Length > 0)
                return item.Names[0];
            // This should never happen, as it's invalid for Option to be
            // constructed w/o any names.
            throw new InvalidOperationException("Option has no names!");
        }

        [Obsolete("Use KeyedCollection.this[string]")]
        protected Option GetOptionForName(string option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));
            try
            {
                return base[option];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        protected override void InsertItem(int index, Option item)
        {
            base.InsertItem(index, item);
            AddImpl(item);
        }

        protected override void RemoveItem(int index)
        {
            Option p = Items[index];
            base.RemoveItem(index);
            // KeyedCollection.RemoveItem() handles the 0th item
            for (int i = 1; i < p.Names.Length; ++i)
            {
                Dictionary.Remove(p.Names[i]);
            }
        }

        protected override void SetItem(int index, Option item)
        {
            base.SetItem(index, item);
            AddImpl(item);
        }

        private void AddImpl(Option option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));
            List<string> added = new List<string>(option.Names.Length);
            try
            {
                // KeyedCollection.InsertItem/SetItem handle the 0th name.
                for (int i = 1; i < option.Names.Length; ++i)
                {
                    Dictionary.Add(option.Names[i], option);
                    added.Add(option.Names[i]);
                }
            }
            catch (Exception)
            {
                foreach (string name in added)
                    Dictionary.Remove(name);
                throw;
            }
        }

        public OptionSet Add(string header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));
            Add(new Category(header));
            return this;
        }

        internal sealed class Category : Option
        {

            // Prototype starts with '=' because this is an invalid prototype
            // (see Option.ParsePrototype(), and thus it'll prevent Category
            // instances from being accidentally used as normal options.
            public Category(string description)
                : base("=:Category:= " + description, description)
            {
            }

            protected override void OnParseComplete(OptionContext c)
            {
                throw new NotSupportedException("Category.OnParseComplete should not be invoked.");
            }
        }


        public new OptionSet Add(Option option)
        {
            base.Add(option);
            return this;
        }

        internal sealed class ActionOption : Option
        {
            private Action<OptionValueCollection> action;

            public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action)
                : this(prototype, description, count, action, false)
            {
            }

            public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action, bool hidden)
                : base(prototype, description, count, hidden)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                this.action = action;
            }

            protected override void OnParseComplete(OptionContext c)
            {
                action(c.OptionValues);
            }
        }

        public OptionSet Add(string prototype, Action<string> action)
        {
            return Add(prototype, null, action);
        }

        public OptionSet Add(string prototype, string description, Action<string> action)
        {
            return Add(prototype, description, action, false);
        }

        public OptionSet Add(string prototype, string description, Action<string> action, bool hidden)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Option p = new ActionOption(prototype, description, 1,
                    delegate (OptionValueCollection v) { action(v[0]); }, hidden);
            base.Add(p);
            return this;
        }

        public OptionSet Add(string prototype, OptionAction<string, string> action)
        {
            return Add(prototype, null, action);
        }

        public OptionSet Add(string prototype, string description, OptionAction<string, string> action)
        {
            return Add(prototype, description, action, false);
        }

        public OptionSet Add(string prototype, string description, OptionAction<string, string> action, bool hidden)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Option p = new ActionOption(prototype, description, 2,
                    delegate (OptionValueCollection v) { action(v[0], v[1]); }, hidden);
            base.Add(p);
            return this;
        }

        internal sealed class ActionOption<T> : Option
        {
            private Action<T> action;

            public ActionOption(string prototype, string description, Action<T> action)
                : base(prototype, description, 1)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                this.action = action;
            }

            protected override void OnParseComplete(OptionContext c)
            {
                action(Parse<T>(c.OptionValues[0], c));
            }
        }

        internal sealed class ActionOption<TKey, TValue> : Option
        {
            private OptionAction<TKey, TValue> action;

            public ActionOption(string prototype, string description, OptionAction<TKey, TValue> action)
                : base(prototype, description, 2)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));
                this.action = action;
            }

            protected override void OnParseComplete(OptionContext c)
            {
                action(
                        Parse<TKey>(c.OptionValues[0], c),
                        Parse<TValue>(c.OptionValues[1], c));
            }
        }

        public OptionSet Add<T>(string prototype, Action<T> action)
        {
            return Add(prototype, null, action);
        }

        public OptionSet Add<T>(string prototype, string description, Action<T> action)
        {
            return Add(new ActionOption<T>(prototype, description, action));
        }

        public OptionSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
        {
            return Add(prototype, null, action);
        }

        public OptionSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
        {
            return Add(new ActionOption<TKey, TValue>(prototype, description, action));
        }

        public OptionSet Add(ArgumentSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            sources.Add(source);
            return this;
        }

        protected virtual OptionContext CreateOptionContext()
        {
            return new OptionContext(this);
        }

        public List<string> Parse(IEnumerable<string> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));
            OptionContext c = CreateOptionContext();
            c.OptionIndex = -1;
            bool process = true;
            List<string> unprocessed = new List<string>();
            Option def = Contains("<>") ? this["<>"] : null;
            ArgumentEnumerator ae = new ArgumentEnumerator(arguments);
            foreach (string argument in ae)
            {
                ++c.OptionIndex;
                if (argument == "--")
                {
                    process = false;
                    continue;
                }
                if (!process)
                {
                    Unprocessed(unprocessed, def, c, argument);
                    continue;
                }
                if (AddSource(ae, argument))
                    continue;
                if (!Parse(argument, c))
                    Unprocessed(unprocessed, def, c, argument);
            }
            if (c.Option != null)
                c.Option.Invoke(c);
            return unprocessed;
        }

        internal sealed class ArgumentEnumerator : IEnumerable<string>
        {
            private List<IEnumerator<string>> sources = new List<IEnumerator<string>>();

            public ArgumentEnumerator(IEnumerable<string> arguments)
            {
                sources.Add(arguments.GetEnumerator());
            }

            public void Add(IEnumerable<string> arguments)
            {
                sources.Add(arguments.GetEnumerator());
            }

            public IEnumerator<string> GetEnumerator()
            {
                do
                {
                    IEnumerator<string> c = sources[sources.Count - 1];
                    if (c.MoveNext())
                        yield return c.Current;
                    else
                    {
                        c.Dispose();
                        sources.RemoveAt(sources.Count - 1);
                    }
                } while (sources.Count > 0);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private bool AddSource(ArgumentEnumerator ae, string argument)
        {
            foreach (ArgumentSource source in sources)
            {
                IEnumerable<string> replacement;
                if (!source.GetArguments(argument, out replacement))
                    continue;
                ae.Add(replacement);
                return true;
            }
            return false;
        }

        private static bool Unprocessed(ICollection<string> extra, Option def, OptionContext c, string argument)
        {
            if (def == null)
            {
                extra.Add(argument);
                return false;
            }
            c.OptionValues.Add(argument);
            c.Option = def;
            c.Option.Invoke(c);
            return false;
        }

        private readonly Regex ValueOption = new Regex(
            @"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

        protected bool GetOptionParts(string argument, out string flag, out string name, out string sep, out string value)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            flag = name = sep = value = null;
            Match m = ValueOption.Match(argument);
            if (!m.Success)
            {
                return false;
            }
            flag = m.Groups["flag"].Value;
            name = m.Groups["name"].Value;
            if (m.Groups["sep"].Success && m.Groups["value"].Success)
            {
                sep = m.Groups["sep"].Value;
                value = m.Groups["value"].Value;
            }
            return true;
        }

        protected virtual bool Parse(string argument, OptionContext c)
        {
            if (c.Option != null)
            {
                ParseValue(argument, c);
                return true;
            }

            string f, n, s, v;
            if (!GetOptionParts(argument, out f, out n, out s, out v))
                return false;

            Option p;
            if (Contains(n))
            {
                p = this[n];
                c.OptionName = f + n;
                c.Option = p;
                switch (p.OptionValueType)
                {
                    case OptionValueType.None:
                        c.OptionValues.Add(n);
                        c.Option.Invoke(c);
                        break;
                    case OptionValueType.Optional:
                    case OptionValueType.Required:
                        ParseValue(v, c);
                        break;
                }
                return true;
            }
            // no match; is it a bool option?
            if (ParseBool(argument, n, c))
                return true;
            // is it a bundled option?
            if (ParseBundledValue(f, string.Concat(n + s + v), c))
                return true;

            return false;
        }

        private void ParseValue(string option, OptionContext c)
        {
            if (option != null)
                foreach (string o in c.Option.ValueSeparators != null
                        ? option.Split(c.Option.ValueSeparators, c.Option.MaxValueCount - c.OptionValues.Count, StringSplitOptions.None)
                        : new string[] { option })
                {
                    c.OptionValues.Add(o);
                }
            if (c.OptionValues.Count == c.Option.MaxValueCount ||
                    c.Option.OptionValueType == OptionValueType.Optional)
                c.Option.Invoke(c);
            else if (c.OptionValues.Count > c.Option.MaxValueCount)
            {
                throw new OptionException(localizer(string.Format(
                                "Error: Found {0} option values when expecting {1}.",
                                c.OptionValues.Count, c.Option.MaxValueCount)),
                        c.OptionName);
            }
        }

        private bool ParseBool(string option, string n, OptionContext c)
        {
            Option p;
            string rn;
            if (n.Length >= 1 && (n[n.Length - 1] == '+' || n[n.Length - 1] == '-') &&
                    Contains((rn = n.Substring(0, n.Length - 1))))
            {
                p = this[rn];
                string v = n[n.Length - 1] == '+' ? option : null;
                c.OptionName = option;
                c.Option = p;
                c.OptionValues.Add(v);
                p.Invoke(c);
                return true;
            }
            return false;
        }

        private bool ParseBundledValue(string f, string n, OptionContext c)
        {
            if (f != "-")
                return false;
            for (int i = 0; i < n.Length; ++i)
            {
                Option p;
                string opt = f + n[i].ToString();
                string rn = n[i].ToString();
                if (!Contains(rn))
                {
                    if (i == 0)
                        return false;
                    throw new OptionException(string.Format(localizer(
                                    "Cannot use unregistered option '{0}' in bundle '{1}'."), rn, f + n), null);
                }
                p = this[rn];
                switch (p.OptionValueType)
                {
                    case OptionValueType.None:
                        Invoke(c, opt, n, p);
                        break;
                    case OptionValueType.Optional:
                    case OptionValueType.Required:
                        {
                            string v = n.Substring(i + 1);
                            c.Option = p;
                            c.OptionName = opt;
                            ParseValue(v.Length != 0 ? v : null, c);
                            return true;
                        }
                    default:
                        throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
                }
            }
            return true;
        }

        private static void Invoke(OptionContext c, string name, string value, Option option)
        {
            c.OptionName = name;
            c.Option = option;
            c.OptionValues.Add(value);
            option.Invoke(c);
        }

        private const int OptionWidth = 29;
        private const int Description_FirstWidth = 80 - OptionWidth;
        private const int Description_RemWidth = 80 - OptionWidth - 2;

        private static readonly string CommandHelpIndentStart = new string(' ', OptionWidth);
        private static readonly string CommandHelpIndentRemaining = new string(' ', OptionWidth + 2);

        public void WriteOptionDescriptions(TextWriter o)
        {
            foreach (Option p in this)
            {
                int written = 0;

                if (p.Hidden)
                    continue;

                Category c = p as Category;
                if (c != null)
                {
                    WriteDescription(o, p.Description, "", 80, 80);
                    continue;
                }
                CommandOption co = p as CommandOption;
                if (co != null)
                {
                    WriteCommandDescription(o, co.Command, co.CommandName);
                    continue;
                }

                if (!WriteOptionPrototype(o, p, ref written))
                    continue;

                if (written < OptionWidth)
                    o.Write(new string(' ', OptionWidth - written));
                else
                {
                    o.WriteLine();
                    o.Write(new string(' ', OptionWidth));
                }

                WriteDescription(o, p.Description, new string(' ', OptionWidth + 2),
                        Description_FirstWidth, Description_RemWidth);
            }

            foreach (ArgumentSource s in sources)
            {
                string[] names = s.GetNames();
                if (names == null || names.Length == 0)
                    continue;

                int written = 0;

                Write(o, ref written, "  ");
                Write(o, ref written, names[0]);
                for (int i = 1; i < names.Length; ++i)
                {
                    Write(o, ref written, ", ");
                    Write(o, ref written, names[i]);
                }

                if (written < OptionWidth)
                    o.Write(new string(' ', OptionWidth - written));
                else
                {
                    o.WriteLine();
                    o.Write(new string(' ', OptionWidth));
                }

                WriteDescription(o, s.Description, new string(' ', OptionWidth + 2),
                        Description_FirstWidth, Description_RemWidth);
            }
        }

        internal void WriteCommandDescription(TextWriter o, Command c, string commandName)
        {
            var name = new string(' ', 8) + (commandName ?? c.Name);
            if (name.Length < OptionWidth - 1)
            {
                WriteDescription(o, name + new string(' ', OptionWidth - name.Length) + c.Help, CommandHelpIndentRemaining, 80, Description_RemWidth);
            }
            else
            {
                WriteDescription(o, name, "", 80, 80);
                WriteDescription(o, CommandHelpIndentStart + c.Help, CommandHelpIndentRemaining, 80, Description_RemWidth);
            }
        }

        private void WriteDescription(TextWriter o, string value, string prefix, int firstWidth, int remWidth)
        {
            bool indent = false;
            foreach (string line in GetLines(localizer(GetDescription(value)), firstWidth, remWidth))
            {
                if (indent)
                    o.Write(prefix);
                o.WriteLine(line);
                indent = true;
            }
        }

        private bool WriteOptionPrototype(TextWriter o, Option p, ref int written)
        {
            string[] names = p.Names;

            int i = GetNextOptionIndex(names, 0);
            if (i == names.Length)
                return false;

            if (names[i].Length == 1)
            {
                Write(o, ref written, "  -");
                Write(o, ref written, names[0]);
            }
            else
            {
                Write(o, ref written, "      --");
                Write(o, ref written, names[0]);
            }

            for (i = GetNextOptionIndex(names, i + 1);
                    i < names.Length; i = GetNextOptionIndex(names, i + 1))
            {
                Write(o, ref written, ", ");
                Write(o, ref written, names[i].Length == 1 ? "-" : "--");
                Write(o, ref written, names[i]);
            }

            if (p.OptionValueType == OptionValueType.Optional ||
                    p.OptionValueType == OptionValueType.Required)
            {
                if (p.OptionValueType == OptionValueType.Optional)
                {
                    Write(o, ref written, localizer("["));
                }
                Write(o, ref written, localizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
                string sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0
                    ? p.ValueSeparators[0]
                    : " ";
                for (int c = 1; c < p.MaxValueCount; ++c)
                {
                    Write(o, ref written, localizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
                }
                if (p.OptionValueType == OptionValueType.Optional)
                {
                    Write(o, ref written, localizer("]"));
                }
            }
            return true;
        }

        private static int GetNextOptionIndex(string[] names, int i)
        {
            while (i < names.Length && names[i] == "<>")
            {
                ++i;
            }
            return i;
        }

        private static void Write(TextWriter o, ref int n, string s)
        {
            n += s.Length;
            o.Write(s);
        }

        private static string GetArgumentName(int index, int maxIndex, string description)
        {
            var matches = Regex.Matches(description ?? "", @"(?<=(?<!\{)\{)[^{}]*(?=\}(?!\}))"); // ignore double braces
            string argName = "";
            foreach (Match match in matches)
            {
                var parts = match.Value.Split(':');
                // for maxIndex=1 it can be {foo} or {0:foo}
                if (maxIndex == 1)
                {
                    argName = parts[parts.Length - 1];
                }
                // look for {i:foo} if maxIndex > 1
                if (maxIndex > 1 && parts.Length == 2 &&
                    parts[0] == index.ToString(CultureInfo.InvariantCulture))
                {
                    argName = parts[1];
                }
            }

            if (string.IsNullOrEmpty(argName))
            {
                argName = maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
            }
            return argName;
        }

        private static string GetDescription(string description)
        {
            if (description == null)
                return string.Empty;
            StringBuilder sb = new StringBuilder(description.Length);
            int start = -1;
            for (int i = 0; i < description.Length; ++i)
            {
                switch (description[i])
                {
                    case '{':
                        if (i == start)
                        {
                            sb.Append('{');
                            start = -1;
                        }
                        else if (start < 0)
                            start = i + 1;
                        break;
                    case '}':
                        if (start < 0)
                        {
                            if ((i + 1) == description.Length || description[i + 1] != '}')
                                throw new InvalidOperationException("Invalid option description: " + description);
                            ++i;
                            sb.Append('}');
                        }
                        else
                        {
                            sb.Append(description.AsSpan(start, i - start));
                            start = -1;
                        }
                        break;
                    case ':':
                        if (start < 0)
                            goto default;
                        start = i + 1;
                        break;
                    default:
                        if (start < 0)
                            sb.Append(description[i]);
                        break;
                }
            }
            return sb.ToString();
        }

        private static IEnumerable<string> GetLines(string description, int firstWidth, int remWidth)
        {
            return StringCoda.WrappedLines(description, firstWidth, remWidth);
        }
    }

    public class Command
    {
        public string Name { get; }
        public string Help { get; }

        public OptionSet Options { get; set; }
        public Action<IEnumerable<string>> Run { get; set; }

        public CommandSet CommandSet { get; internal set; }

        public Command(string name, string help = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Name = NormalizeCommandName(name);
            Help = help;
        }

        private static string NormalizeCommandName(string name)
        {
            var value = new StringBuilder(name.Length);
            var space = false;
            for (int i = 0; i < name.Length; ++i)
            {
                if (!char.IsWhiteSpace(name, i))
                {
                    space = false;
                    value.Append(name[i]);
                }
                else if (!space)
                {
                    space = true;
                    value.Append(' ');
                }
            }
            return value.ToString();
        }

        public virtual int Invoke(IEnumerable<string> arguments)
        {
            var rest = Options?.Parse(arguments) ?? arguments;
            Run?.Invoke(rest);
            return 0;
        }
    }

    internal sealed class CommandOption : Option
    {
        public Command Command { get; }
        public string CommandName { get; }

        // Prototype starts with '=' because this is an invalid prototype
        // (see Option.ParsePrototype(), and thus it'll prevent Category
        // instances from being accidentally used as normal options.
        public CommandOption(Command command, string commandName = null, bool hidden = false)
            : base("=:Command:= " + (commandName ?? command?.Name), (commandName ?? command?.Name), maxValueCount: 0, hidden: hidden)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            Command = command;
            CommandName = commandName ?? command.Name;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            throw new NotSupportedException("CommandOption.OnParseComplete should not be invoked.");
        }
    }

    internal sealed class HelpOption : Option
    {
        private Option option;
        private CommandSet commands;

        public HelpOption(CommandSet commands, Option d)
            : base(d.Prototype, d.Description, d.MaxValueCount, d.Hidden)
        {
            this.commands = commands;
            this.option = d;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            commands.showHelp = true;

            option?.InvokeOnParseComplete(c);
        }
    }

    internal sealed class CommandOptionSet : OptionSet
    {
        private CommandSet commands;

        public CommandOptionSet(CommandSet commands, MessageLocalizerConverter localizer)
            : base(localizer)
        {
            this.commands = commands;
        }

        protected override void SetItem(int index, Option item)
        {
            if (ShouldWrapOption(item))
            {
                base.SetItem(index, new HelpOption(commands, item));
                return;
            }
            base.SetItem(index, item);
        }

        private static bool ShouldWrapOption(Option item)
        {
            if (item == null)
                return false;
            var help = item as HelpOption;
            if (help != null)
                return false;
            foreach (var n in item.Names)
            {
                if (n == "help")
                    return true;
            }
            return false;
        }

        protected override void InsertItem(int index, Option item)
        {
            if (ShouldWrapOption(item))
            {
                base.InsertItem(index, new HelpOption(commands, item));
                return;
            }
            base.InsertItem(index, item);
        }
    }

    public class CommandSet : KeyedCollection<string, Command>
    {
        private readonly string suite;

        private OptionSet options;
        private TextWriter outWriter;
        private TextWriter errorWriter;

        internal List<CommandSet> NestedCommandSets;

        internal HelpCommand help;

        internal bool showHelp;

        internal OptionSet Options => options;

#if !PCL || NETSTANDARD1_3
        public CommandSet(string suite, MessageLocalizerConverter localizer = null)
            : this(suite, Console.Out, Console.Error, localizer)
        {
        }
#endif

        public CommandSet(string suite, TextWriter output, TextWriter error, MessageLocalizerConverter localizer = null)
        {
            if (suite == null)
                throw new ArgumentNullException(nameof(suite));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            this.suite = suite;
            options = new CommandOptionSet(this, localizer);
            outWriter = output;
            errorWriter = error;
        }

        public string Suite => suite;
        public TextWriter Out => outWriter;
        public TextWriter Error => errorWriter;
        public MessageLocalizerConverter MessageLocalizer => options.MessageLocalizer;

        protected override string GetKeyForItem(Command item)
        {
            return item?.Name;
        }

        public new CommandSet Add(Command value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            AddCommand(value);
            options.Add(new CommandOption(value));
            return this;
        }

        private void AddCommand(Command value)
        {
            if (value.CommandSet != null && value.CommandSet != this)
            {
                throw new ArgumentException("Command instances can only be added to a single CommandSet.", nameof(value));
            }
            value.CommandSet = this;
            if (value.Options != null)
            {
                value.Options.MessageLocalizer = options.MessageLocalizer;
            }

            base.Add(value);

            help = help ?? value as HelpCommand;
        }

        public CommandSet Add(string header)
        {
            options.Add(header);
            return this;
        }

        public CommandSet Add(Option option)
        {
            options.Add(option);
            return this;
        }

        public CommandSet Add(string prototype, Action<string> action)
        {
            options.Add(prototype, action);
            return this;
        }

        public CommandSet Add(string prototype, string description, Action<string> action)
        {
            options.Add(prototype, description, action);
            return this;
        }

        public CommandSet Add(string prototype, string description, Action<string> action, bool hidden)
        {
            options.Add(prototype, description, action, hidden);
            return this;
        }

        public CommandSet Add(string prototype, OptionAction<string, string> action)
        {
            options.Add(prototype, action);
            return this;
        }

        public CommandSet Add(string prototype, string description, OptionAction<string, string> action)
        {
            options.Add(prototype, description, action);
            return this;
        }

        public CommandSet Add(string prototype, string description, OptionAction<string, string> action, bool hidden)
        {
            options.Add(prototype, description, action, hidden);
            return this;
        }

        public CommandSet Add<T>(string prototype, Action<T> action)
        {
            options.Add(prototype, null, action);
            return this;
        }

        public CommandSet Add<T>(string prototype, string description, Action<T> action)
        {
            options.Add(prototype, description, action);
            return this;
        }

        public CommandSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
        {
            options.Add(prototype, action);
            return this;
        }

        public CommandSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
        {
            options.Add(prototype, description, action);
            return this;
        }

        public CommandSet Add(ArgumentSource source)
        {
            options.Add(source);
            return this;
        }

        public CommandSet Add(CommandSet nestedCommands)
        {
            if (nestedCommands == null)
                throw new ArgumentNullException(nameof(nestedCommands));

            if (NestedCommandSets == null)
            {
                NestedCommandSets = new List<CommandSet>();
            }

            if (!AlreadyAdded(nestedCommands))
            {
                NestedCommandSets.Add(nestedCommands);
                foreach (var o in nestedCommands.options)
                {
                    if (o is CommandOption c)
                    {
                        options.Add(new CommandOption(c.Command, $"{nestedCommands.Suite} {c.CommandName}"));
                    }
                    else
                    {
                        options.Add(o);
                    }
                }
            }

            nestedCommands.options = this.options;
            nestedCommands.outWriter = this.outWriter;
            nestedCommands.errorWriter = this.errorWriter;

            return this;
        }

        private bool AlreadyAdded(CommandSet value)
        {
            if (value == this)
                return true;
            if (NestedCommandSets == null)
                return false;
            foreach (var nc in NestedCommandSets)
            {
                if (nc.AlreadyAdded(value))
                    return true;
            }
            return false;
        }

        public IEnumerable<string> GetCompletions(string prefix = null)
        {
            string rest;
            ExtractToken(ref prefix, out rest);

            foreach (var command in this)
            {
                if (command.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    yield return command.Name;
                }
            }

            if (NestedCommandSets == null)
                yield break;

            foreach (var subset in NestedCommandSets)
            {
                if (subset.Suite.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var c in subset.GetCompletions(rest))
                    {
                        yield return $"{subset.Suite} {c}";
                    }
                }
            }
        }

        private static void ExtractToken(ref string input, out string rest)
        {
            rest = "";
            input = input ?? "";

            int top = input.Length;
            for (int i = 0; i < top; i++)
            {
                if (char.IsWhiteSpace(input[i]))
                    continue;

                for (int j = i; j < top; j++)
                {
                    if (char.IsWhiteSpace(input[j]))
                    {
                        rest = input.Substring(j).Trim();
                        input = input.Substring(i, j).Trim();
                        return;
                    }
                }
                rest = "";
                if (i != 0)
                    input = input.Substring(i).Trim();
                return;
            }
        }

        public int Run(IEnumerable<string> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            this.showHelp = false;
            if (help == null)
            {
                help = new HelpCommand();
                AddCommand(help);
            }
            Action<string> setHelp = v => showHelp = v != null;
            if (!options.Contains("help"))
            {
                options.Add("help", "", setHelp, hidden: true);
            }
            if (!options.Contains("?"))
            {
                options.Add("?", "", setHelp, hidden: true);
            }
            var extra = options.Parse(arguments);
            if (extra.Count == 0)
            {
                if (showHelp)
                {
                    return help.Invoke(extra);
                }
                Out.WriteLine(options.MessageLocalizer($"Use `{Suite} help` for usage."));
                return 1;
            }
            var command = GetCommand(extra);
            if (command == null)
            {
                help.WriteUnknownCommand(extra[0]);
                return 1;
            }
            if (showHelp)
            {
                if (command.Options?.Contains("help") ?? true)
                {
                    extra.Add("--help");
                    return command.Invoke(extra);
                }
                command.Options.WriteOptionDescriptions(Out);
                return 0;
            }
            return command.Invoke(extra);
        }

        internal Command GetCommand(List<string> extra)
        {
            return TryGetLocalCommand(extra) ?? TryGetNestedCommand(extra);
        }

        private Command TryGetLocalCommand(List<string> extra)
        {
            var name = extra[0];
            if (Contains(name))
            {
                extra.RemoveAt(0);
                return this[name];
            }
            for (int i = 1; i < extra.Count; ++i)
            {
                name = name + " " + extra[i];
                if (!Contains(name))
                    continue;
                extra.RemoveRange(0, i + 1);
                return this[name];
            }
            return null;
        }

        private Command TryGetNestedCommand(List<string> extra)
        {
            if (NestedCommandSets == null)
                return null;

            var nestedCommands = NestedCommandSets.Find(c => c.Suite == extra[0]);
            if (nestedCommands == null)
                return null;

            var extraCopy = new List<string>(extra);
            extraCopy.RemoveAt(0);
            if (extraCopy.Count == 0)
                return null;

            var command = nestedCommands.GetCommand(extraCopy);
            if (command != null)
            {
                extra.Clear();
                extra.AddRange(extraCopy);
                return command;
            }
            return null;
        }
    }

    public class HelpCommand : Command
    {
        public HelpCommand()
            : base("help", help: "Show this message and exit")
        {
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            var extra = new List<string>(arguments ?? Array.Empty<string>());
            var _ = CommandSet.Options.MessageLocalizer;
            if (extra.Count == 0)
            {
                CommandSet.Options.WriteOptionDescriptions(CommandSet.Out);
                return 0;
            }
            var command = CommandSet.GetCommand(extra);
            if (command == this || extra.Contains("--help"))
            {
                CommandSet.Out.WriteLine(_($"Usage: {CommandSet.Suite} COMMAND [OPTIONS]"));
                CommandSet.Out.WriteLine(_($"Use `{CommandSet.Suite} help COMMAND` for help on a specific command."));
                CommandSet.Out.WriteLine();
                CommandSet.Out.WriteLine(_($"Available commands:"));
                CommandSet.Out.WriteLine();
                var commands = GetCommands();
                commands.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase));
                foreach (var c in commands)
                {
                    if (c.Key == "help")
                    {
                        continue;
                    }
                    CommandSet.Options.WriteCommandDescription(CommandSet.Out, c.Value, c.Key);
                }
                CommandSet.Options.WriteCommandDescription(CommandSet.Out, CommandSet.help, "help");
                return 0;
            }
            if (command == null)
            {
                WriteUnknownCommand(extra[0]);
                return 1;
            }
            if (command.Options != null)
            {
                command.Options.WriteOptionDescriptions(CommandSet.Out);
                return 0;
            }
            return command.Invoke(new[] { "--help" });
        }

        private List<KeyValuePair<string, Command>> GetCommands()
        {
            var commands = new List<KeyValuePair<string, Command>>();

            foreach (var c in CommandSet)
            {
                commands.Add(new KeyValuePair<string, Command>(c.Name, c));
            }

            if (CommandSet.NestedCommandSets == null)
                return commands;

            foreach (var nc in CommandSet.NestedCommandSets)
            {
                AddNestedCommands(commands, "", nc);
            }

            return commands;
        }

        private void AddNestedCommands(List<KeyValuePair<string, Command>> commands, string outer, CommandSet value)
        {
            foreach (var v in value)
            {
                commands.Add(new KeyValuePair<string, Command>($"{outer}{value.Suite} {v.Name}", v));
            }
            if (value.NestedCommandSets == null)
                return;
            foreach (var nc in value.NestedCommandSets)
            {
                AddNestedCommands(commands, $"{outer}{value.Suite} ", nc);
            }
        }

        internal void WriteUnknownCommand(string unknownCommand)
        {
            CommandSet.Error.WriteLine(CommandSet.Options.MessageLocalizer($"{CommandSet.Suite}: Unknown command: {unknownCommand}"));
            CommandSet.Error.WriteLine(CommandSet.Options.MessageLocalizer($"{CommandSet.Suite}: Use `{CommandSet.Suite} help` for usage."));
        }
    }
}
