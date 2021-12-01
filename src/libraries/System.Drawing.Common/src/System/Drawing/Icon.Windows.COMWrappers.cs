// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
                Guid iid = DrawingCom.IPicture.IID;
                IntPtr lpPicture;
                Marshal.ThrowExceptionForHR(OleCreatePictureIndirect(&pictdesc, &iid, fOwn: 0, &lpPicture));

                IntPtr streamPtr = IntPtr.Zero;
                try
                {
                    // Use UniqueInstance here because we never want to cache the wrapper. It only gets used once and then disposed.
                    using DrawingCom.IPicture picture = (DrawingCom.IPicture)DrawingCom.Instance
                        .GetOrCreateObjectForComInstance(lpPicture, CreateObjectFlags.UniqueInstance);

                    var gpStream = new GPStream(outputStream, makeSeekable: false);
                    streamPtr = DrawingCom.Instance.GetOrCreateComInterfaceForObject(gpStream, CreateComInterfaceFlags.None);

                    DrawingCom.ThrowExceptionForHR(picture.SaveAsFile(streamPtr, -1, null));
                }
                finally
                {
                    if (streamPtr != IntPtr.Zero)
                    {
                        int count = Marshal.Release(streamPtr);
                        Debug.Assert(count == 0);
                    }

                    if (lpPicture != IntPtr.Zero)
                    {
                        int count = Marshal.Release(lpPicture);
                        Debug.Assert(count == 0);
                    }
                }
            }
        }

        [DllImport(Interop.Libraries.Oleaut32)]
        private static unsafe extern int OleCreatePictureIndirect(PICTDESC* pictdesc, Guid* refiid, int fOwn, IntPtr* lplpvObj);

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct PICTDESC
        {
            public readonly int SizeOfStruct;
            public readonly int PicType;
            public readonly IntPtr Icon;

            private unsafe PICTDESC(int picType, IntPtr hicon)
            {
                SizeOfStruct = sizeof(PICTDESC);
                PicType = picType;
                Icon = hicon;
            }

            public static PICTDESC CreateIconPICTDESC(IntPtr hicon) =>
                new PICTDESC(Ole.PICTYPE_ICON, hicon);
        }
    }
}
