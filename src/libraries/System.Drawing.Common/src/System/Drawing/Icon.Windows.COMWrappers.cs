// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Drawing
{
    public sealed partial class Icon : MarshalByRefObject, ICloneable, IDisposable, ISerializable
    {
        public unsafe void Save(Stream outputStream)
        {
            if (_iconData != null)
            {
                outputStream.Write(_iconData, 0, _iconData.Length);
            }
            else
            {
                if (outputStream == null)
                    throw new ArgumentNullException(nameof(outputStream));

                // Ideally, we would pick apart the icon using
                // GetIconInfo, and then pull the individual bitmaps out,
                // converting them to DIBS and saving them into the file.
                // But, in the interest of simplicity, we just call to
                // OLE to do it for us.
                PICTDESC pictdesc = PICTDESC.CreateIconPICTDESC(Handle);
                Guid g = DrawingComWrappers.IPicture.Guid;
                IntPtr lpPicture;
                DrawingComWrappers.CheckStatus(OleCreatePictureIndirect(&pictdesc, &g, false, &lpPicture));

                IntPtr streamPtr = IntPtr.Zero;
                try
                {
                    DrawingComWrappers.IPicture picture = (DrawingComWrappers.IPicture)DrawingComWrappers.Instance.GetOrCreateObjectForComInstance(lpPicture, CreateObjectFlags.None);

                    var gpStream = new GPStream(outputStream, makeSeekable: false);
                    streamPtr = DrawingComWrappers.Instance.GetOrCreateComInterfaceForObject(gpStream, CreateComInterfaceFlags.None);

                    DrawingComWrappers.CheckStatus(picture.SaveAsFile(streamPtr, -1, out _));
                }
                finally
                {
                    if (streamPtr != IntPtr.Zero)
                    {
                        Marshal.Release(streamPtr);
                    }

                    if (lpPicture != IntPtr.Zero)
                    {
                        Marshal.Release(lpPicture);
                    }
                }
            }
        }

        [DllImport(Interop.Libraries.Oleaut32)]
        private static unsafe extern int OleCreatePictureIndirect(PICTDESC* pictdesc, Guid* refiid, bool fOwn, IntPtr* lplpvObj);

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct PICTDESC
        {
            public readonly int SizeOfStruct;
            public readonly int PicType;
            public readonly IntPtr Icon;

            private PICTDESC(int sizeOfStruct, int picType, IntPtr hicon)
            {
                SizeOfStruct = sizeOfStruct;
                PicType = picType;
                Icon = hicon;
            }

            public static PICTDESC CreateIconPICTDESC(IntPtr hicon) =>
                new PICTDESC(8 + IntPtr.Size, Ole.PICTYPE_ICON, hicon);
        }
    }
}
