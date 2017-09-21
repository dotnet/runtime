// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Imaging.MetaHeader.cs
//
// Authors:
//   Everaldo Canuto (everaldo.canuto@bol.com.br)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//   Dennis Hayes (dennish@raytek.com)
//   Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004, 2006-2007 Novell, Inc (http://www.novell.com)
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

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct WmfMetaHeader
    {
        // field order match: http://wvware.sourceforge.net/caolan/ora-wmf.html
        // for WMFHEAD structure
        public short file_type;
        public short header_size;
        public short version;
        // this is unaligned and fails on the SPARC architecture (see bug #81254 for details)
        // public int file_size;
        public ushort file_size_low;
        public ushort file_size_high;
        public short num_of_objects;
        public int max_record_size;
        public short num_of_params;
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class MetaHeader
    {

        private WmfMetaHeader wmf;

        public MetaHeader()
        {
        }

        internal MetaHeader(WmfMetaHeader header)
        {
            wmf.file_type = header.file_type;
            wmf.header_size = header.header_size;
            wmf.version = header.version;
            wmf.file_size_low = header.file_size_low;
            wmf.file_size_high = header.file_size_high;
            wmf.num_of_objects = header.num_of_objects;
            wmf.max_record_size = header.max_record_size;
            wmf.num_of_params = header.num_of_params;
        }


        public short HeaderSize
        {
            get { return wmf.header_size; }
            set { wmf.header_size = value; }
        }

        public int MaxRecord
        {
            get { return wmf.max_record_size; }
            set { wmf.max_record_size = value; }
        }

        public short NoObjects
        {
            get { return wmf.num_of_objects; }
            set { wmf.num_of_objects = value; }
        }

        public short NoParameters
        {
            get { return wmf.num_of_params; }
            set { wmf.num_of_params = value; }
        }

        public int Size
        {
            get
            {
                if (BitConverter.IsLittleEndian)
                    return (wmf.file_size_high << 16) | wmf.file_size_low;
                else
                    return (wmf.file_size_low << 16) | wmf.file_size_high;
            }
            set
            {
                if (BitConverter.IsLittleEndian)
                {
                    wmf.file_size_high = (ushort)(value >> 16);
                    wmf.file_size_low = (ushort)value;
                }
                else
                {
                    wmf.file_size_high = (ushort)value;
                    wmf.file_size_low = (ushort)(value >> 16);
                }
            }
        }

        public short Type
        {
            get { return wmf.file_type; }
            set { wmf.file_type = value; }
        }

        public short Version
        {
            get { return wmf.version; }
            set { wmf.version = value; }
        }
    }
}
