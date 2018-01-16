// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: CultureInfo-specific collection of resources.
**
** 
===========================================================*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace System.Resources
{
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
    // Serialization for all the non-instrinsic types.
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
    // offset list, possibly add the type list of types types (and increase 
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
#if CORERT
    public  // On CoreRT, this must be public because of need to whitelist past the ReflectionBlock.
#else
    internal
#endif
    sealed class RuntimeResourceSet : ResourceSet, IEnumerable
    {
        internal const int Version = 2;            // File format version number

        // Cache for resources.  Key is the resource name, which can be cached
        // for arbitrarily long times, since the object is usually a string
        // literal that will live for the lifetime of the appdomain.  The
        // value is a ResourceLocator instance, which might cache the object.
        private Dictionary<String, ResourceLocator> _resCache;


        // For our special load-on-demand reader, cache the cast.  The 
        // RuntimeResourceSet's implementation knows how to treat this reader specially.
        private ResourceReader _defaultReader;

        // This is a lookup table for case-insensitive lookups, and may be null.
        // Consider always using a case-insensitive resource cache, as we don't
        // want to fill this out if we can avoid it.  The problem is resource
        // fallback will somewhat regularly cause us to look up resources that 
        // don't exist.
        private Dictionary<String, ResourceLocator> _caseInsensitiveTable;

        // If we're not using our custom reader, then enumerate through all
        // the resources once, adding them into the table.
        private bool _haveReadFromReader;

        internal RuntimeResourceSet(String fileName) : base(false)
        {
            _resCache = new Dictionary<String, ResourceLocator>(FastResourceComparer.Default);
            Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            _defaultReader = new ResourceReader(stream, _resCache);
            Reader = _defaultReader;
        }

        internal RuntimeResourceSet(Stream stream) : base(false)
        {
            _resCache = new Dictionary<String, ResourceLocator>(FastResourceComparer.Default);
            _defaultReader = new ResourceReader(stream, _resCache);
            Reader = _defaultReader;
        }

        protected override void Dispose(bool disposing)
        {
            if (Reader == null)
                return;

            if (disposing)
            {
                lock (Reader)
                {
                    _resCache = null;
                    if (_defaultReader != null)
                    {
                        _defaultReader.Close();
                        _defaultReader = null;
                    }
                    _caseInsensitiveTable = null;
                    // Set Reader to null to avoid a race in GetObject.
                    base.Dispose(disposing);
                }
            }
            else
            {
                // Just to make sure we always clear these fields in the future...
                _resCache = null;
                _caseInsensitiveTable = null;
                _defaultReader = null;
                base.Dispose(disposing);
            }
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
            IResourceReader copyOfReader = Reader;
            if (copyOfReader == null || _resCache == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

            return copyOfReader.GetEnumerator();
        }


        public override String GetString(String key)
        {
            Object o = GetObject(key, false, true);
            return (String)o;
        }

        public override String GetString(String key, bool ignoreCase)
        {
            Object o = GetObject(key, ignoreCase, true);
            return (String)o;
        }

        public override Object GetObject(String key)
        {
            return GetObject(key, false, false);
        }

        public override Object GetObject(String key, bool ignoreCase)
        {
            return GetObject(key, ignoreCase, false);
        }

        private Object GetObject(String key, bool ignoreCase, bool isString)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (Reader == null || _resCache == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

            Object value = null;
            ResourceLocator resLocation;

            lock (Reader)
            {
                if (Reader == null)
                    throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);

                if (_defaultReader != null)
                {
                    // Find the offset within the data section
                    int dataPos = -1;
                    if (_resCache.TryGetValue(key, out resLocation))
                    {
                        value = resLocation.Value;
                        dataPos = resLocation.DataPosition;
                    }

                    if (dataPos == -1 && value == null)
                    {
                        dataPos = _defaultReader.FindPosForResource(key);
                    }

                    if (dataPos != -1 && value == null)
                    {
                        Debug.Assert(dataPos >= 0, "data section offset cannot be negative!");
                        // Normally calling LoadString or LoadObject requires
                        // taking a lock.  Note that in this case, we took a
                        // lock on the entire RuntimeResourceSet, which is 
                        // sufficient since we never pass this ResourceReader
                        // to anyone else.
                        ResourceTypeCode typeCode;
                        if (isString)
                        {
                            value = _defaultReader.LoadString(dataPos);
                            typeCode = ResourceTypeCode.String;
                        }
                        else
                        {
                            value = _defaultReader.LoadObject(dataPos, out typeCode);
                        }

                        resLocation = new ResourceLocator(dataPos, (ResourceLocator.CanCache(typeCode)) ? value : null);
                        lock (_resCache)
                        {
                            _resCache[key] = resLocation;
                        }
                    }

                    if (value != null || !ignoreCase)
                    {
                        return value;  // may be null
                    }
                }  // if (_defaultReader != null)

                // At this point, we either don't have our default resource reader
                // or we haven't found the particular resource we're looking for
                // and may have to search for it in a case-insensitive way.
                if (!_haveReadFromReader)
                {
                    // If necessary, init our case insensitive hash table.
                    if (ignoreCase && _caseInsensitiveTable == null)
                    {
                        _caseInsensitiveTable = new Dictionary<String, ResourceLocator>(StringComparer.OrdinalIgnoreCase);
                    }

                    if (_defaultReader == null)
                    {
                        IDictionaryEnumerator en = Reader.GetEnumerator();
                        while (en.MoveNext())
                        {
                            DictionaryEntry entry = en.Entry;
                            String readKey = (String)entry.Key;
                            ResourceLocator resLoc = new ResourceLocator(-1, entry.Value);
                            _resCache.Add(readKey, resLoc);
                            if (ignoreCase)
                                _caseInsensitiveTable.Add(readKey, resLoc);
                        }
                        // Only close the reader if it is NOT our default one,
                        // since we need it around to resolve ResourceLocators.
                        if (!ignoreCase)
                            Reader.Close();
                    }
                    else
                    {
                        Debug.Assert(ignoreCase, "This should only happen for case-insensitive lookups");
                        ResourceReader.ResourceEnumerator en = _defaultReader.GetEnumeratorInternal();
                        while (en.MoveNext())
                        {
                            // Note: Always ask for the resource key before the data position.
                            String currentKey = (String)en.Key;
                            int dataPos = en.DataPosition;
                            ResourceLocator resLoc = new ResourceLocator(dataPos, null);
                            _caseInsensitiveTable.Add(currentKey, resLoc);
                        }
                    }
                    _haveReadFromReader = true;
                }
                Object obj = null;
                bool found = false;
                bool keyInWrongCase = false;
                if (_defaultReader != null)
                {
                    if (_resCache.TryGetValue(key, out resLocation))
                    {
                        found = true;
                        obj = ResolveResourceLocator(resLocation, key, _resCache, keyInWrongCase);
                    }
                }
                if (!found && ignoreCase)
                {
                    if (_caseInsensitiveTable.TryGetValue(key, out resLocation))
                    {
                        found = true;
                        keyInWrongCase = true;
                        obj = ResolveResourceLocator(resLocation, key, _resCache, keyInWrongCase);
                    }
                }
                return obj;
            } // lock(Reader)
        }

        // The last parameter indicates whether the lookup required a 
        // case-insensitive lookup to succeed, indicating we shouldn't add 
        // the ResourceLocation to our case-sensitive cache.
        private Object ResolveResourceLocator(ResourceLocator resLocation, String key, Dictionary<String, ResourceLocator> copyOfCache, bool keyInWrongCase)
        {
            // We need to explicitly resolve loosely linked manifest
            // resources, and we need to resolve ResourceLocators with null objects.
            Object value = resLocation.Value;
            if (value == null)
            {
                ResourceTypeCode typeCode;
                lock (Reader)
                {
                    value = _defaultReader.LoadObject(resLocation.DataPosition, out typeCode);
                }
                if (!keyInWrongCase && ResourceLocator.CanCache(typeCode))
                {
                    resLocation.Value = value;
                    copyOfCache[key] = resLocation;
                }
            }
            return value;
        }
    }
}
