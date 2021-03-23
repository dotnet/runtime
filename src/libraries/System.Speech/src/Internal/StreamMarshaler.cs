// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Speech.Internal
{
    internal sealed class StreamMarshaler : IDisposable
    {
        #region Constructors

        internal StreamMarshaler()
        {
        }

        internal StreamMarshaler(Stream stream)
        {
            _stream = stream;
        }

        public void Dispose()
        {
            _safeHMem.Dispose();
        }

        #endregion

        #region internal Methods
        internal void ReadArray<T>(T[] ao, int c)
        {
            Type type = typeof(T);
            int sizeOfOne = Marshal.SizeOf(type);
            int sizeObject = sizeOfOne * c;
            byte[] ab = Helpers.ReadStreamToByteArray(_stream, sizeObject);

            IntPtr buffer = _safeHMem.Buffer(sizeObject);

            Marshal.Copy(ab, 0, buffer, sizeObject);
            for (int i = 0; i < c; i++)
            {
                ao[i] = (T)Marshal.PtrToStructure((IntPtr)((long)buffer + i * sizeOfOne), type);
            }
        }

        internal void WriteArray<T>(T[] ao, int c)
        {
            Type type = typeof(T);
            int sizeOfOne = Marshal.SizeOf(type);
            int sizeObject = sizeOfOne * c;
            byte[] ab = new byte[sizeObject];
            IntPtr buffer = _safeHMem.Buffer(sizeObject);

            for (int i = 0; i < c; i++)
            {
                Marshal.StructureToPtr(ao[i], (IntPtr)((long)buffer + i * sizeOfOne), false);
            }

            Marshal.Copy(buffer, ab, 0, sizeObject);
            _stream.Write(ab, 0, sizeObject);
        }

        internal void ReadArrayChar(char[] ach, int c)
        {
            int sizeObject = c * Helpers._sizeOfChar;

            if (sizeObject > 0)
            {
                byte[] ab = Helpers.ReadStreamToByteArray(_stream, sizeObject);

                IntPtr buffer = _safeHMem.Buffer(sizeObject);

                Marshal.Copy(ab, 0, buffer, sizeObject);
                Marshal.Copy(buffer, ach, 0, c);
            }
        }

#pragma warning disable 56518 // BinaryReader can't be disposed because underlying stream still in use.

        // Helper method to read a Unicode string from a stream.
        internal string ReadNullTerminatedString()
        {
            BinaryReader br = new(_stream, Encoding.Unicode);
            StringBuilder stringBuilder = new();

            while (true)
            {
                char c = br.ReadChar();
                if (c == '\0')
                {
                    break;
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString();
        }

#pragma warning restore 56518

        internal void WriteArrayChar(char[] ach, int c)
        {
            int sizeObject = c * Helpers._sizeOfChar;

            if (sizeObject > 0)
            {
                byte[] ab = new byte[sizeObject];
                IntPtr buffer = _safeHMem.Buffer(sizeObject);

                Marshal.Copy(ach, 0, buffer, c);
                Marshal.Copy(buffer, ab, 0, sizeObject);
                _stream.Write(ab, 0, sizeObject);
            }
        }

        internal void ReadStream(object o)
        {
            int sizeObject = Marshal.SizeOf(o.GetType());
            byte[] ab = Helpers.ReadStreamToByteArray(_stream, sizeObject);

            IntPtr buffer = _safeHMem.Buffer(sizeObject);

            Marshal.Copy(ab, 0, buffer, sizeObject);
            Marshal.PtrToStructure(buffer, o);
        }

        internal void WriteStream(object o)
        {
            int sizeObject = Marshal.SizeOf(o.GetType());
            byte[] ab = new byte[sizeObject];
            IntPtr buffer = _safeHMem.Buffer(sizeObject);

            Marshal.StructureToPtr(o, buffer, false);
            Marshal.Copy(buffer, ab, 0, sizeObject);

            // Read the Header
            _stream.Write(ab, 0, sizeObject);
        }

        #endregion

        #region internal Fields

        internal Stream Stream
        {
            get
            {
                return _stream;
            }
        }

        internal uint Position
        {
            set
            {
                _stream.Position = value;
            }
        }

        #endregion

        #region Private Fields

        private HGlobalSafeHandle _safeHMem = new();

        private Stream _stream;

        #endregion
    }
}
