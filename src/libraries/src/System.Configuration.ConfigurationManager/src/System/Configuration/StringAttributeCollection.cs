// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Text;

namespace System.Configuration
{
    public sealed class CommaDelimitedStringCollection : StringCollection
    {
        private bool _modified;
        private string _originalString;

        public CommaDelimitedStringCollection()
        {
            IsReadOnly = false;
            _modified = false;
            _originalString = ToString();
        }

        public bool IsModified => _modified || (ToString() != _originalString);

        public new bool IsReadOnly { get; private set; }

        public new string this[int index]
        {
            get { return base[index]; }
            set
            {
                ThrowIfReadOnly();
                ThrowIfContainsDelimiter(value);
                _modified = true;
                base[index] = value.Trim();
            }
        }

        internal void FromString(string list)
        {
            char[] delimiters = { ',' };
            if (list != null)
            {
                string[] items = list.Split(delimiters);
                foreach (string item in items)
                {
                    string trimmedItem = item.Trim();
                    if (trimmedItem.Length != 0) Add(item.Trim());
                }
            }
            _originalString = ToString();
            IsReadOnly = false;
            _modified = false;
        }

        public override string ToString()
        {
            if (Count <= 0) return null;

            StringBuilder sb = new StringBuilder();
            foreach (string str in this)
            {
                ThrowIfContainsDelimiter(str);
                // Since the add methods are not virtual they could still add bad data
                // by casting the collection to a string collection.  This check will catch
                // it before serialization, late is better than never.
                sb.Append(str.Trim());
                sb.Append(',');
            }

            if (sb.Length > 0) sb.Length = sb.Length - 1;
            return sb.Length == 0 ? null : sb.ToString();
        }

        private void ThrowIfReadOnly()
        {
            if (IsReadOnly) throw new ConfigurationErrorsException(SR.Config_base_read_only);
        }

        private static void ThrowIfContainsDelimiter(string value)
        {
            if (value.Contains(",")) // string.Contains(char) is .NetCore2.1+ specific
                throw new ConfigurationErrorsException(SR.Format(SR.Config_base_value_cannot_contain, ","));
        }

        public void SetReadOnly()
        {
            IsReadOnly = true;
        }

        public new void Add(string value)
        {
            ThrowIfReadOnly();
            ThrowIfContainsDelimiter(value);
            _modified = true;
            base.Add(value.Trim());
        }

        public new void AddRange(string[] range)
        {
            ThrowIfReadOnly();
            _modified = true;
            foreach (string str in range)
            {
                ThrowIfContainsDelimiter(str);
                base.Add(str.Trim());
            }
        }

        public new void Clear()
        {
            ThrowIfReadOnly();
            _modified = true;
            base.Clear();
        }

        public new void Insert(int index, string value)
        {
            ThrowIfReadOnly();
            ThrowIfContainsDelimiter(value);
            _modified = true;
            base.Insert(index, value.Trim());
        }

        public new void Remove(string value)
        {
            ThrowIfReadOnly();
            ThrowIfContainsDelimiter(value);
            _modified = true;
            base.Remove(value.Trim());
        }

        public CommaDelimitedStringCollection Clone()
        {
            CommaDelimitedStringCollection copy = new CommaDelimitedStringCollection();

            // Copy all values
            foreach (string str in this) copy.Add(str);

            // Copy Attributes
            copy._modified = false;
            copy.IsReadOnly = IsReadOnly;
            copy._originalString = _originalString;

            return copy;
        }
    }
}
