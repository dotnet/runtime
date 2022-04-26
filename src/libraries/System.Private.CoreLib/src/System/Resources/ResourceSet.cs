// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace System.Resources
{
    // A ResourceSet stores all the resources defined in one particular CultureInfo.
    //
    // The method used to load resources is straightforward - this class
    // enumerates over an IResourceReader, loading every name and value, and
    // stores them in a hash table.  Custom IResourceReaders can be used.
    //
    public class ResourceSet : IDisposable, IEnumerable
    {
        protected IResourceReader Reader = null!;

        private Dictionary<object, object?>? _table;
        private Dictionary<string, object?>? _caseInsensitiveTable;  // For case-insensitive lookups.

        protected ResourceSet()
        {
            // To not inconvenience people subclassing us, we should allocate a new
            // hashtable here just so that Table is set to something.
            _table = new Dictionary<object, object?>();
        }

        // For RuntimeResourceSet, ignore the Table parameter - it's a wasted
        // allocation.
        internal ResourceSet(bool _)
        {
        }

        // Creates a ResourceSet using the system default ResourceReader
        // implementation.  Use this constructor to open & read from a file
        // on disk.
        //
        public ResourceSet(string fileName)
            : this()
        {
            Reader = new ResourceReader(fileName);
            ReadResources();
        }

        // Creates a ResourceSet using the system default ResourceReader
        // implementation.  Use this constructor to read from an open stream
        // of data.
        //
        public ResourceSet(Stream stream)
            : this()
        {
            Reader = new ResourceReader(stream);
            ReadResources();
        }

        public ResourceSet(IResourceReader reader)
            : this()
        {
            ArgumentNullException.ThrowIfNull(reader);

            Reader = reader;
            ReadResources();
        }

        // Closes and releases any resources used by this ResourceSet, if any.
        // All calls to methods on the ResourceSet after a call to close may
        // fail.  Close is guaranteed to be safely callable multiple times on a
        // particular ResourceSet, and all subclasses must support these semantics.
        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Close the Reader in a thread-safe way.
                IResourceReader? copyOfReader = Reader;
                Reader = null!;
                if (copyOfReader != null)
                    copyOfReader.Close();
            }
            Reader = null!;
            _caseInsensitiveTable = null;
            _table = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        // Returns the preferred IResourceReader class for this kind of ResourceSet.
        // Subclasses of ResourceSet using their own Readers &; should override
        // GetDefaultReader and GetDefaultWriter.
        public virtual Type GetDefaultReader()
        {
            return typeof(ResourceReader);
        }

        // Returns the preferred IResourceWriter class for this kind of ResourceSet.
        // Subclasses of ResourceSet using their own Readers &; should override
        // GetDefaultReader and GetDefaultWriter.
        public virtual Type GetDefaultWriter()
        {
            return Type.GetType("System.Resources.ResourceWriter, System.Resources.Writer", throwOnError: true)!;
        }

        public virtual IDictionaryEnumerator GetEnumerator()
        {
            return GetEnumeratorHelper();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorHelper();
        }

        private IDictionaryEnumerator GetEnumeratorHelper()
        {
            IDictionary? copyOfTableAsIDictionary = _table;  // Avoid a race with Dispose
            if (copyOfTableAsIDictionary == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

             // Use IDictionary.GetEnumerator() for backward compatibility. Callers expect the enumerator to return DictionaryEntry instances.
            return copyOfTableAsIDictionary.GetEnumerator();
        }

        // Look up a string value for a resource given its name.
        //
        public virtual string? GetString(string name)
        {
            object? obj = GetObjectInternal(name);
            if (obj is string s)
                return s;

            if (obj is null)
                return null;

            throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));
        }

        public virtual string? GetString(string name, bool ignoreCase)
        {
            // Case-sensitive lookup
            object? obj = GetObjectInternal(name);
            if (obj is string s)
                return s;

            if (obj is not null)
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));

            if (!ignoreCase)
                return null;

            // Try doing a case-insensitive lookup
            obj = GetCaseInsensitiveObjectInternal(name);
            if (obj is string si)
                return si;

            if (obj is null)
                return null;

            throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));
        }

        // Look up an object value for a resource given its name.
        //
        public virtual object? GetObject(string name)
        {
            return GetObjectInternal(name);
        }

        public virtual object? GetObject(string name, bool ignoreCase)
        {
            object? obj = GetObjectInternal(name);

            if (obj != null || !ignoreCase)
                return obj;

            return GetCaseInsensitiveObjectInternal(name);
        }

        protected virtual void ReadResources()
        {
            Debug.Assert(_table != null);
            Debug.Assert(Reader != null);
            IDictionaryEnumerator en = Reader.GetEnumerator();
            while (en.MoveNext())
            {
                _table.Add(en.Key, en.Value);
            }
            // While technically possible to close the Reader here, don't close it
            // to help with some WinRes lifetime issues.
        }

        private object? GetObjectInternal(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            Dictionary<object, object?>? copyOfTable = _table;  // Avoid a race with Dispose

            if (copyOfTable == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

            copyOfTable.TryGetValue(name, out object? value);
            return value;
        }

        private object? GetCaseInsensitiveObjectInternal(string name)
        {
            Dictionary<object, object?>? copyOfTable = _table;  // Avoid a race with Dispose

            if (copyOfTable == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

            Dictionary<string, object?>? caseTable = _caseInsensitiveTable;  // Avoid a race condition with Close
            if (caseTable == null)
            {
                caseTable = new Dictionary<string, object?>(copyOfTable.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var item in copyOfTable)
                {
                    if (item.Key is not string s)
                        continue;

                    caseTable.Add(s, item.Value);
                }
                _caseInsensitiveTable = caseTable;
            }

            caseTable.TryGetValue(name, out object? value);
            return value;
        }
    }
}
