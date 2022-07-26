// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.IO;
using System.Globalization;

namespace System.Xml.Serialization
{
    internal sealed class CaseInsensitiveKeyComparer : CaseInsensitiveComparer, IEqualityComparer
    {
        public CaseInsensitiveKeyComparer() : base(CultureInfo.CurrentCulture)
        {
        }

        bool IEqualityComparer.Equals(object? x, object? y)
        {
            return (Compare(x, y) == 0);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            string? s = obj as string;
            if (s == null)
                throw new ArgumentException(null, nameof(obj));

            return CultureInfo.CurrentCulture.TextInfo.ToUpper(s).GetHashCode();
        }
    }

    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class CodeIdentifiers
    {
        private Hashtable _identifiers;
        private Hashtable _reservedIdentifiers;
        private ArrayList _list;
        private bool _camelCase;

        public CodeIdentifiers() : this(true)
        {
        }

        public CodeIdentifiers(bool caseSensitive)
        {
            if (caseSensitive)
            {
                _identifiers = new Hashtable();
                _reservedIdentifiers = new Hashtable();
            }
            else
            {
                IEqualityComparer comparer = new CaseInsensitiveKeyComparer();
                _identifiers = new Hashtable(comparer);
                _reservedIdentifiers = new Hashtable(comparer);
            }

            _list = new ArrayList();
        }

        public void Clear()
        {
            _identifiers.Clear();
            _list.Clear();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool UseCamelCasing
        {
            get { return _camelCase; }
            set { _camelCase = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MakeRightCase(string identifier)
        {
            if (_camelCase)
                return CodeIdentifier.MakeCamel(identifier);
            else
                return CodeIdentifier.MakePascal(identifier);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MakeUnique(string identifier)
        {
            if (IsInUse(identifier))
            {
                for (int i = 1; ; i++)
                {
                    string newIdentifier = identifier + i.ToString(CultureInfo.InvariantCulture);
                    if (!IsInUse(newIdentifier))
                    {
                        identifier = newIdentifier;
                        break;
                    }
                }
            }
            // Check that we did not violate the identifier length after appending the suffix.
            if (identifier.Length > CodeIdentifier.MaxIdentifierLength)
            {
                return MakeUnique("Item");
            }
            return identifier;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void AddReserved(string identifier)
        {
            _reservedIdentifiers.Add(identifier, identifier);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void RemoveReserved(string identifier)
        {
            _reservedIdentifiers.Remove(identifier);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string AddUnique(string identifier, object? value)
        {
            identifier = MakeUnique(identifier);
            Add(identifier, value);
            return identifier;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsInUse(string identifier)
        {
            return _identifiers.Contains(identifier) || _reservedIdentifiers.Contains(identifier);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Add(string identifier, object? value)
        {
            _identifiers.Add(identifier, value);
            _list.Add(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(string identifier)
        {
            _list.Remove(_identifiers[identifier]);
            _identifiers.Remove(identifier);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object ToArray(Type type)
        {
            //Array array = Array.CreateInstance(type, identifiers.Values.Count);
            //identifiers.Values.CopyTo(array, 0);
            Array array = Array.CreateInstance(type, _list.Count);
            _list.CopyTo(array, 0);
            return array;
        }

        internal CodeIdentifiers Clone()
        {
            CodeIdentifiers newIdentifiers = new CodeIdentifiers();
            newIdentifiers._identifiers = (Hashtable)_identifiers.Clone();
            newIdentifiers._reservedIdentifiers = (Hashtable)_reservedIdentifiers.Clone();
            newIdentifiers._list = (ArrayList)_list.Clone();
            newIdentifiers._camelCase = _camelCase;

            return newIdentifiers;
        }
    }
}
