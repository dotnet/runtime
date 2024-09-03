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
using System.IO;
using System.Text;

namespace Claunia.PropertyList
{
    /// <summary>
    ///     This class provides methods to parse property lists. It can handle files, input streams and byte arrays. All
    ///     known property list formats are supported. This class also provides methods to save and convert property lists.
    /// </summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public static class PropertyListParser
    {
        const int TYPE_XML           = 0;
        const int TYPE_BINARY        = 1;
        const int TYPE_ASCII         = 2;
        const int TYPE_ERROR_BLANK   = 10;
        const int TYPE_ERROR_UNKNOWN = 11;

        /// <summary>Determines the type of a property list by means of the first bytes of its data</summary>
        /// <returns>The type of the property list</returns>
        /// <param name="dataBeginning">The very first bytes of data of the property list (minus any whitespace) as a string</param>
        static int DetermineTypeExact(ReadOnlySpan<byte> dataBeginning)
        {
            if(dataBeginning.Length == 0)
                return TYPE_ERROR_BLANK;

            if(dataBeginning[0] == '(' ||
               dataBeginning[0] == '{' ||
               dataBeginning[0] == '/')
                return TYPE_ASCII;

            if(dataBeginning[0] == '<')
                return TYPE_XML;

            if(dataBeginning.Length >= 6   &&
               dataBeginning[0]     == 'b' &&
               dataBeginning[1]     == 'p' &&
               dataBeginning[2]     == 'l' &&
               dataBeginning[3]     == 'i' &&
               dataBeginning[4]     == 's' &&
               dataBeginning[5]     == 't')
                return TYPE_BINARY;

            return TYPE_ERROR_UNKNOWN;
        }

        /// <summary>Determines the type of a property list by means of the first bytes of its data</summary>
        /// <returns>The very first bytes of data of the property list (minus any whitespace)</returns>
        /// <param name="bytes">The type of the property list</param>
        static int DetermineType(ReadOnlySpan<byte> bytes)
        {
            if(bytes.Length == 0)
                return TYPE_ERROR_BLANK;

            //Skip any possible whitespace at the beginning of the file
            int offset = 0;

            if(bytes.Length      >= 3    &&
               (bytes[0] & 0xFF) == 0xEF &&
               (bytes[1] & 0xFF) == 0xBB &&
               (bytes[2] & 0xFF) == 0xBF)
                offset += 3;

            while(offset < bytes.Length &&
                  (bytes[offset] == ' ' || bytes[offset] == '\t' || bytes[offset] == '\r' || bytes[offset] == '\n' ||
                   bytes[offset] == '\f'))
                offset++;

            ReadOnlySpan<byte> header = bytes.Slice(offset, Math.Min(8, bytes.Length - offset));

            return DetermineTypeExact(header);
        }

        /// <summary>Determines the type of a property list by means of the first bytes of its data</summary>
        /// <returns>The type of the property list</returns>
        /// <param name="fs">
        ///     An input stream pointing to the beginning of the property list data. The stream will be reset to the
        ///     beginning of the property list data after the type has been determined.
        /// </param>
        static int DetermineType(Stream fs, long offset = 0)
        {
            if(fs.Length == 0)
                return TYPE_ERROR_BLANK;

            long index     = offset;
            long readLimit = index + 1024;
            long mark      = readLimit;
            fs.Seek(offset, SeekOrigin.Current);
            int  b;
            bool bom = false;

            //Skip any possible whitespace at the beginning of the file
            do
            {
                if(++index > readLimit)
                {
                    fs.Seek(mark, SeekOrigin.Begin);

                    return DetermineType(fs, readLimit);
                }

                b = fs.ReadByte();

                //Check if we are reading the Unicode byte order mark (BOM) and skip it
                bom = index < 3 && ((index == 0 && b == 0xEF) ||
                                    (bom        && ((index == 1 && b == 0xBB) || (index == 2 && b == 0xBF))));
            } while(b != -1 &&
                    (b is ' ' or '\t' or '\r' or '\n' or '\f' || bom));

            if(b == -1)
                return TYPE_ERROR_BLANK;

            byte[] magicBytes = new byte[8];
            magicBytes[0] = (byte)b;
            int read = fs.Read(magicBytes, 1, 7);

            int type = DetermineTypeExact(magicBytes.AsSpan(0, read));
            fs.Seek(mark, SeekOrigin.Begin);

            return type;
        }

        /// <summary>Reads all bytes from an Stream and stores them in an array, up to a maximum count.</summary>
        /// <param name="fs">The Stream pointing to the data that should be stored in the array.</param>
        internal static byte[] ReadAll(Stream fs)
        {
            using var outputStream = new MemoryStream();

            fs.CopyTo(outputStream);

            return outputStream.ToArray();
        }

        /// <summary>Parses a property list from a file.</summary>
        /// <param name="filePath">Path to the property list file.</param>
        /// <returns>The root object in the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(string filePath) => Parse(new FileInfo(filePath));

        /// <summary>Parses a property list from a file.</summary>
        /// <param name="f">The property list file.</param>
        /// <returns>The root object in the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(FileInfo f)
        {
            using FileStream fis = f.OpenRead();

            return Parse(fis);
        }

        /// <summary>Parses a property list from a byte array.</summary>
        /// <param name="bytes">The property list data as a byte array.</param>
        /// <returns>The root object in the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(byte[] bytes)
        {
            switch(DetermineType(bytes))
            {
                case TYPE_BINARY: return BinaryPropertyListParser.Parse(bytes);
                case TYPE_XML:    return XmlPropertyListParser.Parse(bytes);
                case TYPE_ASCII:  return ASCIIPropertyListParser.Parse(bytes);
                default:
                    throw new
                        PropertyListFormatException("The given data is not a property list of a supported format.");
            }
        }

        /// <summary>Parses a property list from a byte array.</summary>
        /// <param name="bytes">The property list data as a byte array.</param>
        /// <param name="offset">The length of the property list.</param>
        /// <param name="count">The offset at which to start reading the property list.</param>
        /// <returns>The root object in the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(byte[] bytes, int offset, int length) => Parse(bytes.AsSpan(offset, length));

        /// <summary>Parses a property list from a byte span.</summary>
        /// <param name="bytes">The property list data as a byte array.</param>
        /// <returns>The root object in the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(ReadOnlySpan<byte> bytes)
        {
            switch(DetermineType(bytes))
            {
                case TYPE_BINARY: return BinaryPropertyListParser.Parse(bytes);
                case TYPE_XML:    return XmlPropertyListParser.Parse(bytes.ToArray());
                case TYPE_ASCII:  return ASCIIPropertyListParser.Parse(bytes);
                default:
                    throw new
                        PropertyListFormatException("The given data is not a property list of a supported format.");
            }
        }

        /// <summary>Parses a property list from an Stream.</summary>
        /// <param name="fs">The Stream delivering the property list data.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        public static NSObject Parse(Stream fs) => Parse(ReadAll(fs));

        /// <summary>Saves a property list with the given object as root into a XML file.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="outFile">The output file.</param>
        /// <exception cref="IOException">When an error occurs during the writing process.</exception>
        public static void SaveAsXml(NSObject root, FileInfo outFile)
        {
            string parent = outFile.DirectoryName;

            if(!Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            // Use Create here -- to make sure that when the updated file is shorter than
            // the original file, no "obsolete" data is left at the end.
            using Stream fous = outFile.Open(FileMode.Create, FileAccess.ReadWrite);

            SaveAsXml(root, fous);
        }

        /// <summary>Saves a property list with the given object as root in XML format into an output stream.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="outStream">The output stream.</param>
        /// <exception cref="IOException">When an error occurs during the writing process.</exception>
        public static void SaveAsXml(NSObject root, Stream outStream)
        {
            using var w = new StreamWriter(outStream, Encoding.UTF8, 1024, true);

            w.Write(root.ToXmlPropertyList());
        }

        /// <summary>Converts a given property list file into the OS X and iOS XML format.</summary>
        /// <param name="inFile">The source file.</param>
        /// <param name="outFile">The target file.</param>
        public static void ConvertToXml(FileInfo inFile, FileInfo outFile)
        {
            NSObject root = Parse(inFile);
            SaveAsXml(root, outFile);
        }

        /// <summary>Saves a property list with the given object as root into a binary file.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="outFile">The output file.</param>
        /// <exception cref="IOException">When an error occurs during the writing process.</exception>
        public static void SaveAsBinary(NSObject root, FileInfo outFile)
        {
            string parent = outFile.DirectoryName;

            if(!Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            BinaryPropertyListWriter.Write(outFile, root);
        }

        /// <summary>Saves a property list with the given object as root in binary format into an output stream.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="outStream">The output stream.</param>
        /// <exception cref="IOException">When an error occurs during the writing process.</exception>
        public static void SaveAsBinary(NSObject root, Stream outStream) =>
            BinaryPropertyListWriter.Write(outStream, root);

        /// <summary>Converts a given property list file into the OS X and iOS binary format.</summary>
        /// <param name="inFile">The source file.</param>
        /// <param name="outFile">The target file.</param>
        public static void ConvertToBinary(FileInfo inFile, FileInfo outFile)
        {
            NSObject root = Parse(inFile);
            SaveAsBinary(root, outFile);
        }

        /// <summary>Saves a property list with the given object as root into a ASCII file.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="outFile">The output file.</param>
        /// <exception cref="IOException">When an error occurs during the writing process.</exception>
        public static void SaveAsASCII(NSDictionary root, FileInfo outFile)
        {
            string parent = outFile.DirectoryName;

            if(!Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            using Stream fous = outFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

            using var w = new StreamWriter(fous, Encoding.ASCII);

            w.Write(root.ToASCIIPropertyList());
        }

        /// <summary>Saves a property list with the given object as root into a ASCII file.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="outFile">The output file.</param>
        /// <exception cref="IOException">When an error occurs during the writing process.</exception>
        public static void SaveAsASCII(NSArray root, FileInfo outFile)
        {
            string parent = outFile.DirectoryName;

            if(!Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            using Stream fous = outFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

            using var w = new StreamWriter(fous, Encoding.ASCII);

            w.Write(root.ToASCIIPropertyList());
        }

        /// <summary>Converts a given property list file into ASCII format.</summary>
        /// <param name="inFile">The source file.</param>
        /// <param name="outFile">The target file.</param>
        public static void ConvertToASCII(FileInfo inFile, FileInfo outFile)
        {
            NSObject root = Parse(inFile);

            if(root is NSDictionary dictionary)
                SaveAsASCII(dictionary, outFile);
            else if(root is NSArray array)
                SaveAsASCII(array, outFile);
            else
                throw new PropertyListFormatException("The root of the given input property list " +
                                                      "is neither a Dictionary nor an Array!");
        }

        /// <summary>Saves a property list with the given object as root into a GnuStep ASCII file.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="outFile">The output file.</param>
        /// <exception cref="IOException">When an error occurs during the writing process.</exception>
        public static void SaveAsGnuStepASCII(NSDictionary root, FileInfo outFile)
        {
            string parent = outFile.DirectoryName;

            if(!Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            using Stream fous = outFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

            using var w = new StreamWriter(fous, Encoding.ASCII);

            w.Write(root.ToGnuStepASCIIPropertyList());
        }

        /// <summary>Saves a property list with the given object as root into a GnuStep ASCII file.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="outFile">The output file.</param>
        /// <exception cref="IOException">When an error occurs during the writing process.</exception>
        public static void SaveAsGnuStepASCII(NSArray root, FileInfo outFile)
        {
            string parent = outFile.DirectoryName;

            if(!Directory.Exists(parent))
                Directory.CreateDirectory(parent);

            using Stream fous = outFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);

            using var w = new StreamWriter(fous, Encoding.ASCII);

            w.Write(root.ToGnuStepASCIIPropertyList());
        }

        /// <summary>Converts a given property list file into GnuStep ASCII format.</summary>
        /// <param name="inFile">The source file.</param>
        /// <param name="outFile">The target file.</param>
        public static void ConvertToGnuStepASCII(FileInfo inFile, FileInfo outFile)
        {
            NSObject root = Parse(inFile);

            switch(root)
            {
                case NSDictionary dictionary:
                    SaveAsGnuStepASCII(dictionary, outFile);

                    break;
                case NSArray array:
                    SaveAsGnuStepASCII(array, outFile);

                    break;
                default:
                    throw new PropertyListFormatException("The root of the given input property list " +
                                                          "is neither a Dictionary nor an Array!");
            }
        }
    }
}