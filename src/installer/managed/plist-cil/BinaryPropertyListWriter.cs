// plist-cil - An open source library to parse and generate property lists for .NET
// Copyright (C) 2015 Natalia Portillo
//
// This code is based on:
// plist - An open source library to parse and generate property lists
// Copyright (C) 2014 Daniel Dreibrodt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;

namespace Claunia.PropertyList
{
    /// <summary>
    ///     <para>A BinaryPropertyListWriter is a helper class for writing out binary property list files.</para>
    ///     <para>
    ///         It contains an output stream and various structures for keeping track of which NSObjects have already been
    ///         serialized, and where they were put in the file.
    ///     </para>
    /// </summary>
    /// @author Keith Randall
    /// @author Natalia Portillo
    public partial class BinaryPropertyListWriter
    {
        /// <summary>Binary property list version 0.0</summary>
        public const int VERSION_00 = 0;
        /// <summary>Binary property list version 1.0</summary>
        public const int VERSION_10 = 10;
        /// <summary>Binary property list version 1.5</summary>
        public const int VERSION_15 = 15;
        /// <summary>Binary property list version 2.0</summary>
        public const int VERSION_20 = 20;

        // map from object to its ID
        protected readonly Dictionary<NSObject, int> idDict  = new(new AddObjectEqualityComparer());
        protected readonly Dictionary<NSObject, int> idDict2 = new(new GetObjectEqualityComparer());

        // raw output stream to result file
        readonly Stream outStream;

        readonly int version = VERSION_00;

        // # of bytes written so far
        long          count;
        protected int currentId;
        int           idSizeInBytes;

        /// <summary>Creates a new binary property list writer</summary>
        /// <param name="outStr">The output stream into which the binary property list will be written</param>
        /// <exception cref="IOException">If an error occured while writing to the stream</exception>
        public BinaryPropertyListWriter(Stream outStr) => outStream = outStr;

        public BinaryPropertyListWriter(Stream outStr, int version)
        {
            this.version = version;
            outStream    = outStr;
        }

        public BinaryPropertyListWriter(Stream outStr, int version,
                                        IEqualityComparer<NSObject> addObjectEqualityComparer,
                                        IEqualityComparer<NSObject> getObjectEqualityComparer)
        {
            this.version = version;
            outStream    = outStr;
            idDict       = new Dictionary<NSObject, int>(addObjectEqualityComparer);
            idDict2      = new Dictionary<NSObject, int>(getObjectEqualityComparer);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether two equivalent objects should be serialized once in the binary
        ///     property list file, or whether the value should be stored multiple times in the binary property list file. The
        ///     default is <see langword="false" /> .
        /// </summary>
        /// <remarks>
        ///     In most scenarios, you want this to be <see langword="true" />, as it reduces the size of the binary proeprty
        ///     list file. However, by default, the Apple tools do not seem to implement this optimization, so set this value to
        ///     <see langword="false" /> if you want to maintain binary compatibility with the Apple tools.
        /// </remarks>
        public bool ReuseObjectIds { get; set; }

        /// <summary>Finds out the minimum binary property list format version that can be used to save the given NSObject tree.</summary>
        /// <returns>Version code</returns>
        /// <param name="root">Object root.</param>
        static int GetMinimumRequiredVersion(NSObject root)
        {
            int minVersion = VERSION_00;

            switch(root)
            {
                case null:
                    minVersion = VERSION_10;

                    break;
                case NSDictionary dict:
                {
                    foreach(NSObject o in dict.GetDictionary().Values)
                    {
                        int v = GetMinimumRequiredVersion(o);

                        if(v > minVersion)
                            minVersion = v;
                    }

                    break;
                }
                case NSArray array:
                {
                    foreach(NSObject o in array)
                    {
                        int v = GetMinimumRequiredVersion(o);

                        if(v > minVersion)
                            minVersion = v;
                    }

                    break;
                }
                case NSSet set:
                {
                    //Sets are only allowed in property lists v1+
                    minVersion = VERSION_10;

                    foreach(NSObject o in set.AllObjects())
                    {
                        int v = GetMinimumRequiredVersion(o);

                        if(v > minVersion)
                            minVersion = v;
                    }

                    break;
                }
            }

            return minVersion;
        }

        /// <summary>Writes a binary plist file with the given object as the root.</summary>
        /// <param name="file">the file to write to</param>
        /// <param name="root">the source of the data to write to the file</param>
        /// <exception cref="IOException"></exception>
        public static void Write(FileInfo file, NSObject root)
        {
            using FileStream fous = file.OpenWrite();

            Write(fous, root);
        }

        /// <summary>Writes a binary plist serialization of the given object as the root.</summary>
        /// <param name="outStream">the stream to write to</param>
        /// <param name="root">the source of the data to write to the stream</param>
        /// <exception cref="IOException"></exception>
        public static void Write(Stream outStream, NSObject root)
        {
            int minVersion = GetMinimumRequiredVersion(root);

            if(minVersion > VERSION_00)
            {
                string versionString = minVersion == VERSION_10
                                           ? "v1.0"
                                           : minVersion == VERSION_15
                                               ? "v1.5"
                                               : minVersion == VERSION_20
                                                   ? "v2.0"
                                                   : "v0.0";

                throw new IOException("The given property list structure cannot be saved. " +
                                      "The required version of the binary format ("         + versionString +
                                      ") is not yet supported.");
            }

            var w = new BinaryPropertyListWriter(outStream, minVersion);
            w.Write(root);
        }

        /// <summary>Writes a binary plist serialization of the given object as the root into a byte array.</summary>
        /// <returns>The byte array containing the serialized property list</returns>
        /// <param name="root">The root object of the property list</param>
        /// <exception cref="IOException"></exception>
        public static byte[] WriteToArray(NSObject root)
        {
            var bout = new MemoryStream();
            Write(bout, root);

            return bout.ToArray();
        }

        public void Write(NSObject root)
        {
            // magic bytes
            Write(new[]
            {
                (byte)'b', (byte)'p', (byte)'l', (byte)'i', (byte)'s', (byte)'t'
            });

            //version
            switch(version)
            {
                case VERSION_00:
                {
                    Write(new[]
                    {
                        (byte)'0', (byte)'0'
                    });

                    break;
                }
                case VERSION_10:
                {
                    Write(new[]
                    {
                        (byte)'1', (byte)'0'
                    });

                    break;
                }
                case VERSION_15:
                {
                    Write(new[]
                    {
                        (byte)'1', (byte)'5'
                    });

                    break;
                }
                case VERSION_20:
                {
                    Write(new[]
                    {
                        (byte)'2', (byte)'0'
                    });

                    break;
                }
            }

            // assign IDs to all the objects.
            root.AssignIDs(this);

            idSizeInBytes = ComputeIdSizeInBytes(idDict.Count);

            // offsets of each object, indexed by ID
            long[] offsets = new long[idDict.Count];

            // write each object, save offset
            foreach(KeyValuePair<NSObject, int> pair in idDict)
            {
                NSObject obj = pair.Key;
                int      id  = pair.Value;
                offsets[id] = count;

                if(obj == null)
                    Write(0x00);
                else
                    obj.ToBinary(this);
            }

            // write offset table
            long offsetTableOffset = count;
            int  offsetSizeInBytes = ComputeOffsetSizeInBytes(count);

            foreach(long offset in offsets)
                WriteBytes(offset, offsetSizeInBytes);

            if(version != VERSION_15)
            {
                // write trailer
                // 6 null bytes
                Write(new byte[6]);

                // size of an offset
                Write(offsetSizeInBytes);

                // size of a ref
                Write(idSizeInBytes);

                // number of objects
                WriteLong(idDict.Count);

                // top object
                int rootID = idDict[root];
                WriteLong(rootID);

                // offset table offset
                WriteLong(offsetTableOffset);
            }

            outStream.Flush();
        }

        protected internal virtual void AssignID(NSObject obj)
        {
            if(ReuseObjectIds)
            {
                if(!idDict.ContainsKey(obj))
                    idDict.Add(obj, currentId++);
            }
            else
            {
                if(!idDict2.ContainsKey(obj))
                    idDict2.Add(obj, currentId);

                if(!idDict.ContainsKey(obj))
                    idDict.Add(obj, currentId++);
            }
        }

        internal int GetID(NSObject obj) => ReuseObjectIds ? idDict[obj] : idDict2[obj];

        static int ComputeIdSizeInBytes(int numberOfIds)
        {
            if(numberOfIds < 256)
                return 1;

            return numberOfIds < 65536 ? 2 : 4;
        }

        static int ComputeOffsetSizeInBytes(long maxOffset) => maxOffset switch
        {
            < 256         => 1,
            < 65536       => 2,
            < 4294967296L => 4,
            _             => 8
        };

        internal void WriteIntHeader(int kind, int value)
        {
            switch(value)
            {
                case < 0: throw new ArgumentException("value must be greater than or equal to 0", "value");
                case < 15:
                    Write((kind << 4) + value);

                    break;
                case < 256:
                    Write((kind << 4) + 15);
                    Write(0x10);
                    WriteBytes(value, 1);

                    break;
                case < 65536:
                    Write((kind << 4) + 15);
                    Write(0x11);
                    WriteBytes(value, 2);

                    break;
                default:
                    Write((kind << 4) + 15);
                    Write(0x12);
                    WriteBytes(value, 4);

                    break;
            }
        }

        internal void Write(int b)
        {
            outStream.WriteByte((byte)b);
            count++;
        }

        internal void Write(byte[] bytes)
        {
            outStream.Write(bytes, 0, bytes.Length);
            count += bytes.Length;
        }

        internal void Write(Span<byte> bytes)
        {
        #if NATIVE_SPAN
            outStream.Write(bytes);
            count += bytes.Length;
        #else
            Write(bytes.ToArray());
        #endif
        }

        internal void WriteBytes(long value, int bytes)
        {
            // write low-order bytes big-endian style
            for(int i = bytes - 1; i >= 0; i--)
                Write((int)(value >> (8 * i)));
        }

        internal void WriteID(int id) => WriteBytes(id, idSizeInBytes);

        internal void WriteLong(long value) => WriteBytes(value, 8);

        internal void WriteDouble(double value) => WriteLong(BitConverter.DoubleToInt64Bits(value));

        internal static bool IsSerializationPrimitive(NSString obj)
        {
            string content = obj.Content;

            // This is a list of "special" values which are only added once to a binary property
            // list, and can be referenced multiple times.
            return content is "$class" or "$classes" or "$classname" or "NS.objects" or "NS.keys" or "NS.base" or
                       "NS.relative" or "NS.string" or "NSURL" or "NSDictionary" or "NSObject" or "NSMutableDictionary"
                       or "NSMutableArray" or "NSArray" or "NSUUID" or "NSKeyedArchiver" or "NSMutableString";
        }

        internal static bool IsSerializationPrimitive(NSNumber n) => n.isBoolean();
    }
}