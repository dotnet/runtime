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
        public void Save(Stream outputStream)
        {
            if (_iconData != null)
            {
                ArgumentNullException.ThrowIfNull(outputStream);
                outputStream.Write(_iconData, 0, _iconData.Length);
            }
            else
            {
                // Ideally, we would pick apart the icon using
                // GetIconInfo, and then pull the individual bitmaps out,
                // converting them to DIBS and saving them into the file.
                // But, in the interest of simplicity, we just call to
                // OLE to do it for us.
                PICTDESC pictdesc = PICTDESC.CreateIconPICTDESC(Handle);
                Guid g = typeof(IPicture).GUID;
                IPicture picture = OleCreatePictureIndirect(pictdesc, ref g, false);

                if (picture != null)
                {
                    try
                    {
                        ArgumentNullException.ThrowIfNull(outputStream);
                        picture.SaveAsFile(new GPStream(outputStream, makeSeekable: false), -1, out int temp);
                    }
                    finally
                    {
                        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                        Marshal.ReleaseComObject(picture);
                    }
                }
            }
        }

        [DllImport(Interop.Libraries.Oleaut32, PreserveSig = false)]
        internal static extern IPicture OleCreatePictureIndirect(PICTDESC pictdesc, [In]ref Guid refiid, bool fOwn);

        [ComImport]
        [Guid("7BF80980-BF32-101A-8BBB-00AA00300CAB")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPicture
        {
            IntPtr GetHandle();

            IntPtr GetHPal();

            [return: MarshalAs(UnmanagedType.I2)]
            short GetPictureType();

            int GetWidth();

            int GetHeight();

            void Render();

            void SetHPal([In] IntPtr phpal);

            IntPtr GetCurDC();

            void SelectPicture([In] IntPtr hdcIn,
                               [Out, MarshalAs(UnmanagedType.LPArray)] int[] phdcOut,
                               [Out, MarshalAs(UnmanagedType.LPArray)] int[] phbmpOut);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool GetKeepOriginalFormat();

            void SetKeepOriginalFormat([In, MarshalAs(UnmanagedType.Bool)] bool pfkeep);

            void PictureChanged();

            [PreserveSig]
            int SaveAsFile([In, MarshalAs(UnmanagedType.Interface)] Interop.Ole32.IStream pstm,
                           [In] int fSaveMemCopy,
                           [Out] out int pcbSize);

            int GetAttributes();

            void SetHdc([In] IntPtr hdc);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class PICTDESC
        {
            internal int cbSizeOfStruct;
            public int picType;
            internal IntPtr union1;
            internal int union2;
            internal int union3;

            public static PICTDESC CreateIconPICTDESC(IntPtr hicon)
            {
                return new PICTDESC()
                {
                    cbSizeOfStruct = 12,
                    picType = Ole.PICTYPE_ICON,
                    union1 = hicon
                };
            }
        }
    }
}
