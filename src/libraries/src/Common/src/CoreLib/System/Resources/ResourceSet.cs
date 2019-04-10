// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
/*============================================================
**
**
** 
**
**
** Purpose: Culture-specific collection of resources.
**
** 
===========================================================*/

using System.Collections;
using System.Diagnostics;
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
        internal Hashtable? Table; // TODO-NULLABLE: should not be nulled out in Dispose

        private Hashtable? _caseInsensitiveTable;  // For case-insensitive lookups.

        protected ResourceSet()
        {
            // To not inconvenience people subclassing us, we should allocate a new
            // hashtable here just so that Table is set to something.
            Table = new Hashtable();
        }

        // For RuntimeResourceSet, ignore the Table parameter - it's a wasted 
        // allocation.
        internal ResourceSet(bool junk)
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
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
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
                Reader = null!; // TODO-NULLABLE: should not be nulled out in the Dispose
                if (copyOfReader != null)
                    copyOfReader.Close();
            }
            Reader = null!; // TODO-NULLABLE: should not be nulled out in the Dispose
            _caseInsensitiveTable = null;
            Table = null;
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
            Assembly resourceWriterAssembly = Assembly.Load("System.Resources.Writer, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            return resourceWriterAssembly.GetType("System.Resources.ResourceWriter", true);
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
            Hashtable? copyOfTable = Table;  // Avoid a race with Dispose
            if (copyOfTable == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);
            return copyOfTable.GetEnumerator();
        }

        // Look up a string value for a resource given its name.
        // 
        public virtual string? GetString(string name)
        {
            object? obj = GetObjectInternal(name);
            try
            {
                return (string?)obj;
            }
            catch (InvalidCastException)
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));
            }
        }

        public virtual string? GetString(string name, bool ignoreCase)
        {
            object? obj;
            string? s;

            // Case-sensitive lookup
            obj = GetObjectInternal(name);
            try
            {
                s = (string?)obj;
            }
            catch (InvalidCastException)
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));
            }

            // case-sensitive lookup succeeded
            if (s != null || !ignoreCase)
            {
                return s;
            }

            // Try doing a case-insensitive lookup
            obj = GetCaseInsensitiveObjectInternal(name);
            try
            {
                return (string?)obj;
            }
            catch (InvalidCastException)
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));
            }
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
            Debug.Assert(Table != null);
            Debug.Assert(Reader != null);
            IDictionaryEnumerator en = Reader.GetEnumerator();
            while (en.MoveNext())
            {
                object? value = en.Value;
                Table.Add(en.Key, value);
            }
            // While technically possible to close the Reader here, don't close it
            // to help with some WinRes lifetime issues.
        }

        private object? GetObjectInternal(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Hashtable? copyOfTable = Table;  // Avoid a race with Dispose

            if (copyOfTable == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

            return copyOfTable[name];
        }

        private object? GetCaseInsensitiveObjectInternal(string name)
        {
            Hashtable? copyOfTable = Table;  // Avoid a race with Dispose

            if (copyOfTable == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

            Hashtable? caseTable = _caseInsensitiveTable;  // Avoid a race condition with Close
            if (caseTable == null)
            {
                caseTable = new Hashtable(StringComparer.OrdinalIgnoreCase);

                IDictionaryEnumerator en = copyOfTable.GetEnumerator();
                while (en.MoveNext())
                {
                    caseTable.Add(en.Key, en.Value);
                }
                _caseInsensitiveTable = caseTable;
            }

            return caseTable[name];
        }
    }
}
