// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace System.Resources
#if RESOURCES_EXTENSIONS
    .Extensions
#endif
{
#pragma warning disable IDE0065
#if RESOURCES_EXTENSIONS
    using ResourceReader = DeserializingResourceReader;
#endif
#pragma warning restore IDE0065

    // A RuntimeResourceSet stores all the resources defined in one
    // particular CultureInfo, with some loading optimizations.
    //
    // It is expected that nearly all the runtime's users will be satisfied with the
    // default resource file format, and it will be more efficient than most simple
    // implementations.  Users who would consider creating their own ResourceSets and/or
    // ResourceReaders and ResourceWriters are people who have to interop with a
    // legacy resource file format, are creating their own resource file format
    // (using XML, for instance), or require doing resource lookups at runtime over
    // the network.  This group will hopefully be small, but all the infrastructure
    // should be in place to let these users write & plug in their own tools.
    //
    // The Default Resource File Format
    //
    // The fundamental problems addressed by the resource file format are:
    //
    // * Versioning - A ResourceReader could in theory support many different
    // file format revisions.
    // * Storing intrinsic datatypes (ie, ints, Strings, DateTimes, etc) in a compact
    // format
    // * Support for user-defined classes - Accomplished using Serialization
    // * Resource lookups should not require loading an entire resource file - If you
    // look up a resource, we only load the value for that resource, minimizing working set.
    //
    //
    // There are four sections to the default file format.  The first
    // is the Resource Manager header, which consists of a magic number
    // that identifies this as a Resource file, and a ResourceSet class name.
    // The class name is written here to allow users to provide their own
    // implementation of a ResourceSet (and a matching ResourceReader) to
    // control policy.  If objects greater than a certain size or matching a
    // certain naming scheme shouldn't be stored in memory, users can tweak that
    // with their own subclass of ResourceSet.
    //
    // The second section in the system default file format is the
    // RuntimeResourceSet specific header.  This contains a version number for
    // the .resources file, the number of resources in this file, the number of
    // different types contained in the file, followed by a list of fully
    // qualified type names.  After this, we include an array of hash values for
    // each resource name, then an array of virtual offsets into the name section
    // of the file.  The hashes allow us to do a binary search on an array of
    // integers to find a resource name very quickly without doing many string
    // compares (except for once we find the real type, of course).  If a hash
    // matches, the index into the array of hash values is used as the index
    // into the name position array to find the name of the resource.  The type
    // table allows us to read multiple different classes from the same file,
    // including user-defined types, in a more efficient way than using
    // Serialization, at least when your .resources file contains a reasonable
    // proportion of base data types such as Strings or ints.  We use
    // Serialization for all the non-intrinsic types.
    //
    // The third section of the file is the name section.  It contains a
    // series of resource names, written out as byte-length prefixed little
    // endian Unicode strings (UTF-16).  After each name is a four byte virtual
    // offset into the data section of the file, pointing to the relevant
    // string or serialized blob for this resource name.
    //
    // The fourth section in the file is the data section, which consists
    // of a type and a blob of bytes for each item in the file.  The type is
    // an integer index into the type table.  The data is specific to that type,
    // but may be a number written in binary format, a String, or a serialized
    // Object.
    //
    // The system default file format (V1) is as follows:
    //
    //     What                                               Type of Data
    // ====================================================   ===========
    //
    //                        Resource Manager header
    // Magic Number (0xBEEFCACE)                              Int32
    // Resource Manager header version                        Int32
    // Num bytes to skip from here to get past this header    Int32
    // Class name of IResourceReader to parse this file       String
    // Class name of ResourceSet to parse this file           String
    //
    //                       RuntimeResourceReader header
    // ResourceReader version number                          Int32
    // [Only in debug V2 builds - "***DEBUG***"]              String
    // Number of resources in the file                        Int32
    // Number of types in the type table                      Int32
    // Name of each type                                      Set of Strings
    // Padding bytes for 8-byte alignment (use PAD)           Bytes (0-7)
    // Hash values for each resource name                     Int32 array, sorted
    // Virtual offset of each resource name                   Int32 array, coupled with hash values
    // Absolute location of Data section                      Int32
    //
    //                     RuntimeResourceReader Name Section
    // Name & virtual offset of each resource                 Set of (UTF-16 String, Int32) pairs
    //
    //                     RuntimeResourceReader Data Section
    // Type and Value of each resource                Set of (Int32, blob of bytes) pairs
    //
    // This implementation, when used with the default ResourceReader class,
    // loads only the strings that you look up for.  It can do string comparisons
    // without having to create a new String instance due to some memory mapped
    // file optimizations in the ResourceReader and FastResourceComparer
    // classes.  This keeps the memory we touch to a minimum when loading
    // resources.
    //
    // If you use a different IResourceReader class to read a file, or if you
    // do case-insensitive lookups (and the case-sensitive lookup fails) then
    // we will load all the names of each resource and each resource value.
    // This could probably use some optimization.
    //
    // In addition, this supports object serialization in a similar fashion.
    // We build an array of class types contained in this file, and write it
    // to RuntimeResourceReader header section of the file.  Every resource
    // will contain its type (as an index into the array of classes) with the data
    // for that resource.  We will use the Runtime's serialization support for this.
    //
    // All strings in the file format are written with BinaryReader and
    // BinaryWriter, which writes out the length of the String in bytes as an
    // Int32 then the contents as Unicode chars encoded in UTF-8.  In the name
    // table though, each resource name is written in UTF-16 so we can do a
    // string compare byte by byte against the contents of the file, without
    // allocating objects.  Ideally we'd have a way of comparing UTF-8 bytes
    // directly against a String object, but that may be a lot of work.
    //
    // The offsets of each resource string are relative to the beginning
    // of the Data section of the file.  This way, if a tool decided to add
    // one resource to a file, it would only need to increment the number of
    // resources, add the hash &amp; location of last byte in the name section
    // to the array of resource hashes and resource name positions (carefully
    // keeping these arrays sorted), add the name to the end of the name &amp;
    // offset list, possibly add the type list of types (and increase
    // the number of items in the type table), and add the resource value at
    // the end of the file.  The other offsets wouldn't need to be updated to
    // reflect the longer header section.
    //
    // Resource files are currently limited to 2 gigabytes due to these
    // design parameters.  A future version may raise the limit to 4 gigabytes
    // by using unsigned integers, or may use negative numbers to load items
    // out of an assembly manifest.  Also, we may try sectioning the resource names
    // into smaller chunks, each of size sqrt(n), would be substantially better for
    // resource files containing thousands of resources.
    //
    internal sealed class RuntimeResourceSet : ResourceSet, IEnumerable
    {
        // Cache for resources.  Key is the resource name, which can be cached
        // for arbitrarily long times, since the object is usually a string
        // literal that will live for the lifetime of the appdomain.  The
        // value is a ResourceLocator instance, which might cache the object.
        private Dictionary<string, ResourceLocator>? _resCache;


        // For our special load-on-demand reader. The
        // RuntimeResourceSet's implementation knows how to treat this reader specially.
        private ResourceReader? _defaultReader;

        // This is a lookup table for case-insensitive lookups, and may be null.
        // Consider always using a case-insensitive resource cache, as we don't
        // want to fill this out if we can avoid it.  The problem is resource
        // fallback will somewhat regularly cause us to look up resources that
        // don't exist.
        private Dictionary<string, ResourceLocator>? _caseInsensitiveTable;

#if !RESOURCES_EXTENSIONS
        internal RuntimeResourceSet(string fileName) :
            this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
        }

        internal RuntimeResourceSet(Stream stream, bool permitDeserialization = false) :
            base(false)
        {
            _resCache = new Dictionary<string, ResourceLocator>(FastResourceComparer.Default);
            _defaultReader = new ResourceReader(stream, _resCache, permitDeserialization);
        }
#else
        internal RuntimeResourceSet(IResourceReader reader) :
            // explicitly do not call IResourceReader constructor since it caches all resources
            // the purpose of RuntimeResourceSet is to lazily load and cache.
            base()
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            _defaultReader = reader as DeserializingResourceReader ?? throw new ArgumentException(SR.Format(SR.NotSupported_WrongResourceReader_Type, reader.GetType()), nameof(reader));
            _resCache = new Dictionary<string, ResourceLocator>(FastResourceComparer.Default);

            // in the CoreLib version RuntimeResourceSet creates ResourceReader and passes this in,
            // in the custom case ManifestBasedResourceReader creates the ResourceReader and passes it in
            // so we must initialize the cache here.
            _defaultReader._resCache = _resCache;
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (_defaultReader is null)
                return;

            if (disposing)
            {
                _defaultReader?.Close();
            }

            _defaultReader = null;
            _resCache = null;
            _caseInsensitiveTable = null;
            base.Dispose(disposing);
        }

        public override IDictionaryEnumerator GetEnumerator()
        {
            return GetEnumeratorHelper();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorHelper();
        }

        private IDictionaryEnumerator GetEnumeratorHelper()
        {
            ResourceReader? reader = _defaultReader;
            if (reader is null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

            return reader.GetEnumerator();
        }

        public override string? GetString(string key)
        {
            object? o = GetObject(key, false, true);
            return (string?)o;
        }

        public override string? GetString(string key, bool ignoreCase)
        {
            object? o = GetObject(key, ignoreCase, true);
            return (string?)o;
        }

        public override object? GetObject(string key)
        {
            return GetObject(key, false, false);
        }

        public override object? GetObject(string key, bool ignoreCase)
        {
            return GetObject(key, ignoreCase, false);
        }

        private object? GetObject(string key, bool ignoreCase, bool isString)
        {
#if RESOURCES_EXTENSIONS
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
#else
            ArgumentNullException.ThrowIfNull(key);
#endif

            ResourceReader? reader = _defaultReader;
            Dictionary<string, ResourceLocator>? cache = _resCache;
            if (reader is null || cache is null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

            object? value;
            ResourceLocator resEntry;

            // Lock the cache first, then the reader (reader locks implicitly through its methods).
            // Lock order MUST match ResourceReader.ResourceEnumerator.Entry to avoid deadlock.
            Debug.Assert(!Monitor.IsEntered(reader));
            lock (cache)
            {
                // Find the offset within the data section
                int dataPos;
                if (cache.TryGetValue(key, out resEntry))
                {
                    value = resEntry.Value;
                    if (value != null)
                        return value;

                    // When data type cannot be cached
                    dataPos = resEntry.DataPosition;
                    return isString ? reader.LoadString(dataPos) : reader.LoadObject(dataPos);
                }

                dataPos = reader.FindPosForResource(key);
                if (dataPos >= 0)
                {
                    value = ReadValue(reader, dataPos, isString, out resEntry);
                    cache[key] = resEntry;
                    return value;
                }
            }

            if (!ignoreCase)
            {
                return null;
            }

            // We haven't found the particular resource we're looking for
            // and may have to search for it in a case-insensitive way.
            bool initialize = false;
            Dictionary<string, ResourceLocator>? caseInsensitiveTable = _caseInsensitiveTable;
            if (caseInsensitiveTable == null)
            {
                caseInsensitiveTable = new Dictionary<string, ResourceLocator>(StringComparer.OrdinalIgnoreCase);
                initialize = true;
            }

            lock (caseInsensitiveTable)
            {
                if (initialize)
                {
                    ResourceReader.ResourceEnumerator en = reader.GetEnumeratorInternal();
                    while (en.MoveNext())
                    {
                        // The resource key must be read before the data position.
                        string currentKey = (string)en.Key;
                        ResourceLocator resLoc = new ResourceLocator(en.DataPosition, null);
                        caseInsensitiveTable.Add(currentKey, resLoc);
                    }

                    _caseInsensitiveTable = caseInsensitiveTable;
                }

                if (!caseInsensitiveTable.TryGetValue(key, out resEntry))
                    return null;

                if (resEntry.Value != null)
                    return resEntry.Value;

                value = ReadValue(reader, resEntry.DataPosition, isString, out resEntry);

                if (resEntry.Value != null)
                    caseInsensitiveTable[key] = resEntry;
            }

            return value;
        }

        private static object? ReadValue(ResourceReader reader, int dataPos, bool isString, out ResourceLocator locator)
        {
            object? value;
            ResourceTypeCode typeCode;

            if (isString)
            {
                value = reader.LoadString(dataPos);
                typeCode = ResourceTypeCode.String;
            }
            else
            {
                value = reader.LoadObject(dataPos, out typeCode);
            }

            locator = new ResourceLocator(dataPos, ResourceLocator.CanCache(typeCode) ? value : null);
            return value;
        }
    }
}
