// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Drawing
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
#if netcoreapp
    [TypeConverter("System.Drawing.IconConverter, System.Windows.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51")]
#endif
    public sealed partial class Icon : MarshalByRefObject, ICloneable, IDisposable, ISerializable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif

        private static int s_bitDepth;

        // The PNG signature is specified at http://www.w3.org/TR/PNG/#5PNG-file-signature
        private const int PNGSignature1 = 137 + ('P' << 8) + ('N' << 16) + ('G' << 24);
        private const int PNGSignature2 = 13 + (10 << 8) + (26 << 16) + (10 << 24);

        // Icon data
        private readonly byte[] _iconData;
        private int _bestImageOffset;
        private int _bestBitDepth;
        private int _bestBytesInRes;
        private bool? _isBestImagePng = null;
        private Size _iconSize = Size.Empty;
        private IntPtr _handle = IntPtr.Zero;
        private bool _ownHandle = true;

        private Icon() { }

        internal Icon(IntPtr handle) : this(handle, false)
        {
        }

        internal Icon(IntPtr handle, bool takeOwnership)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException(SR.Format(SR.InvalidGDIHandle, nameof(Icon)));
            }

            _handle = handle;
            _ownHandle = takeOwnership;
        }

        public Icon(string fileName) : this(fileName, 0, 0)
        {
        }

        public Icon(string fileName, Size size) : this(fileName, size.Width, size.Height)
        {
        }

        public Icon(string fileName, int width, int height) : this()
        {
            using (FileStream f = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Debug.Assert(f != null, "File.OpenRead returned null instead of throwing an exception");
                _iconData = new byte[(int)f.Length];
                f.Read(_iconData, 0, _iconData.Length);
            }

            Initialize(width, height);
        }

        public Icon(Icon original, Size size) : this(original, size.Width, size.Height)
        {
        }

        public Icon(Icon original, int width, int height) : this()
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            _iconData = original._iconData;

            if (_iconData == null)
            {
                _iconSize = original.Size;
                _handle = SafeNativeMethods.CopyImage(new HandleRef(original, original.Handle), SafeNativeMethods.IMAGE_ICON, _iconSize.Width, _iconSize.Height, 0);
            }
            else
            {
                Initialize(width, height);
            }
        }

        public Icon(Type type, string resource) : this()
        {
            Stream stream = type.Module.Assembly.GetManifestResourceStream(type, resource);
            if (stream == null)
            {
                throw new ArgumentException(SR.Format(SR.ResourceNotFound, type, resource));
            }

            _iconData = new byte[(int)stream.Length];
            stream.Read(_iconData, 0, _iconData.Length);
            Initialize(0, 0);
        }

        public Icon(Stream stream) : this(stream, 0, 0)
        {
        }

        public Icon(Stream stream, Size size) : this(stream, size.Width, size.Height)
        {
        }

        public Icon(Stream stream, int width, int height) : this()
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _iconData = new byte[(int)stream.Length];
            stream.Read(_iconData, 0, _iconData.Length);
            Initialize(width, height);
        }

        private Icon(SerializationInfo info, StreamingContext context)
        {
            _iconData = (byte[])info.GetValue("IconData", typeof(byte[])); // Do not rename (binary serialization)
            _iconSize = (Size)info.GetValue("IconSize", typeof(Size)); // Do not rename (binary serialization)
            Initialize(_iconSize.Width, _iconSize.Height);
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            if (_iconData != null)
            {
                si.AddValue("IconData", _iconData, typeof(byte[])); // Do not rename (binary serialization)
            }
            else
            {
                MemoryStream stream = new MemoryStream();
                Save(stream);
                si.AddValue("IconData", stream.ToArray(), typeof(byte[])); // Do not rename (binary serialization)
            }

            si.AddValue("IconSize", _iconSize, typeof(Size)); // Do not rename (binary serialization)
        }

        public static Icon ExtractAssociatedIcon(string filePath) => ExtractAssociatedIcon(filePath, 0);

        private static Icon ExtractAssociatedIcon(string filePath, int index)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            Uri uri;
            try
            {
                uri = new Uri(filePath);
            }
            catch (UriFormatException)
            {
                // It's a relative pathname, get its full path as a file. 
                filePath = Path.GetFullPath(filePath);
                uri = new Uri(filePath);
            }

            if (!uri.IsFile)
            {
                return null;
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            var sb = new StringBuilder(NativeMethods.MAX_PATH);
            sb.Append(filePath);

            IntPtr hIcon = SafeNativeMethods.ExtractAssociatedIcon(NativeMethods.NullHandleRef, sb, ref index);
            if (hIcon != IntPtr.Zero)
            {
                return new Icon(hIcon, true);
            }

            return null;
        }

        [Browsable(false)]
        public IntPtr Handle
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                return _handle;
            }
        }

        [Browsable(false)]
        public int Height => Size.Height;

        public Size Size
        {
            get
            {
                if (_iconSize.IsEmpty)
                {
                    var info = new SafeNativeMethods.ICONINFO();
                    SafeNativeMethods.GetIconInfo(new HandleRef(this, Handle), info);
                    var bmp = new SafeNativeMethods.BITMAP();

                    if (info.hbmColor != IntPtr.Zero)
                    {
                        SafeNativeMethods.GetObject(new HandleRef(null, info.hbmColor), Marshal.SizeOf(typeof(SafeNativeMethods.BITMAP)), bmp);
                        SafeNativeMethods.IntDeleteObject(new HandleRef(null, info.hbmColor));
                        _iconSize = new Size(bmp.bmWidth, bmp.bmHeight);
                    }
                    else if (info.hbmMask != IntPtr.Zero)
                    {
                        SafeNativeMethods.GetObject(new HandleRef(null, info.hbmMask), Marshal.SizeOf(typeof(SafeNativeMethods.BITMAP)), bmp);
                        _iconSize = new Size(bmp.bmWidth, bmp.bmHeight / 2);
                    }

                    if (info.hbmMask != IntPtr.Zero)
                    {
                        SafeNativeMethods.IntDeleteObject(new HandleRef(null, info.hbmMask));
                    }
                }

                return _iconSize;
            }
        }

        [Browsable(false)]
        public int Width => Size.Width;

        public object Clone() => new Icon(this, Size.Width, Size.Height);

        // Called when this object is going to destroy it's Win32 handle.  You
        // may override this if there is something special you need to do to
        // destroy the handle.  This will be called even if the handle is not
        // owned by this object, which is handy if you want to create a
        // derived class that has it's own create/destroy semantics.
        //
        // The default implementation will call the appropriate Win32
        // call to destroy the handle if this object currently owns the
        // handle.  It will do nothing if the object does not currently
        // own the handle.
        internal void DestroyHandle()
        {
            if (_ownHandle)
            {
                SafeNativeMethods.DestroyIcon(new HandleRef(this, _handle));
                _handle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_handle != IntPtr.Zero)
            {
#if FINALIZATION_WATCH
                if (!disposing)
                {
                    Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
                }
#endif
                DestroyHandle();
            }
        }

        // Draws this image to a graphics object.  The drawing command originates on the graphics
        // object, but a graphics object generally has no idea how to render a given image.  So,
        // it passes the call to the actual image.  This version crops the image to the given
        // dimensions and allows the user to specify a rectangle within the image to draw.
        private void DrawIcon(IntPtr dc, Rectangle imageRect, Rectangle targetRect, bool stretch)
        {
            int imageX = 0;
            int imageY = 0;
            int imageWidth;
            int imageHeight;
            int targetX = 0;
            int targetY = 0;
            int targetWidth = 0;
            int targetHeight = 0;

            Size cursorSize = Size;

            // Compute the dimensions of the icon if needed.
            if (!imageRect.IsEmpty)
            {
                imageX = imageRect.X;
                imageY = imageRect.Y;
                imageWidth = imageRect.Width;
                imageHeight = imageRect.Height;
            }
            else
            {
                imageWidth = cursorSize.Width;
                imageHeight = cursorSize.Height;
            }

            if (!targetRect.IsEmpty)
            {
                targetX = targetRect.X;
                targetY = targetRect.Y;
                targetWidth = targetRect.Width;
                targetHeight = targetRect.Height;
            }
            else
            {
                targetWidth = cursorSize.Width;
                targetHeight = cursorSize.Height;
            }

            int drawWidth, drawHeight;
            int clipWidth, clipHeight;

            if (stretch)
            {
                drawWidth = cursorSize.Width * targetWidth / imageWidth;
                drawHeight = cursorSize.Height * targetHeight / imageHeight;
                clipWidth = targetWidth;
                clipHeight = targetHeight;
            }
            else
            {
                drawWidth = cursorSize.Width;
                drawHeight = cursorSize.Height;
                clipWidth = targetWidth < imageWidth ? targetWidth : imageWidth;
                clipHeight = targetHeight < imageHeight ? targetHeight : imageHeight;
            }

            // The ROP is SRCCOPY, so we can be simple here and take
            // advantage of clipping regions.  Drawing the cursor
            // is merely a matter of offsetting and clipping.
            IntPtr hSaveRgn = SafeNativeMethods.SaveClipRgn(dc);
            try
            {
                SafeNativeMethods.IntersectClipRect(new HandleRef(this, dc), targetX, targetY, targetX + clipWidth, targetY + clipHeight);
                SafeNativeMethods.DrawIconEx(new HandleRef(null, dc),
                                            targetX - imageX,
                                            targetY - imageY,
                                            new HandleRef(this, _handle),
                                            drawWidth,
                                            drawHeight,
                                            0,
                                            NativeMethods.NullHandleRef,
                                            SafeNativeMethods.DI_NORMAL);
            }
            finally
            {
                SafeNativeMethods.RestoreClipRgn(dc, hSaveRgn);
            }
        }

        internal void Draw(Graphics graphics, int x, int y)
        {
            Size size = Size;
            Draw(graphics, new Rectangle(x, y, size.Width, size.Height));
        }

        // Draws this image to a graphics object.  The drawing command originates on the graphics
        // object, but a graphics object generally has no idea how to render a given image.  So,
        // it passes the call to the actual image.  This version stretches the image to the given
        // dimensions and allows the user to specify a rectangle within the image to draw.
        internal void Draw(Graphics graphics, Rectangle targetRect)
        {
            Rectangle copy = targetRect;
            copy.X += (int)graphics.Transform.OffsetX;
            copy.Y += (int)graphics.Transform.OffsetY;

            using (WindowsGraphics wg = WindowsGraphics.FromGraphics(graphics, ApplyGraphicsProperties.Clipping))
            {
                IntPtr dc = wg.GetHdc();
                DrawIcon(dc, Rectangle.Empty, copy, true);
            }
        }

        // Draws this image to a graphics object.  The drawing command originates on the graphics
        // object, but a graphics object generally has no idea how to render a given image.  So,
        // it passes the call to the actual image.  This version crops the image to the given
        // dimensions and allows the user to specify a rectangle within the image to draw.
        internal void DrawUnstretched(Graphics graphics, Rectangle targetRect)
        {
            Rectangle copy = targetRect;
            copy.X += (int)graphics.Transform.OffsetX;
            copy.Y += (int)graphics.Transform.OffsetY;

            using (WindowsGraphics wg = WindowsGraphics.FromGraphics(graphics, ApplyGraphicsProperties.Clipping))
            {
                IntPtr dc = wg.GetHdc();
                DrawIcon(dc, Rectangle.Empty, copy, false);
            }
        }

        ~Icon() => Dispose(false);

        public static Icon FromHandle(IntPtr handle) => new Icon(handle);

        private unsafe short GetShort(byte* pb)
        {
            int retval = 0;
            if (0 != (unchecked((byte)pb) & 1))
            {
                retval = *pb;
                pb++;
                retval = unchecked(retval | (*pb << 8));
            }
            else
            {
                retval = unchecked(*(short*)pb);
            }
            return unchecked((short)retval);
        }

        private unsafe int GetInt(byte* pb)
        {
            int retval = 0;
            if (0 != (unchecked((byte)pb) & 3))
            {
                retval = *pb; pb++;
                retval = retval | (*pb << 8); pb++;
                retval = retval | (*pb << 16); pb++;
                retval = unchecked(retval | (*pb << 24));
            }
            else
            {
                retval = *(int*)pb;
            }
            return retval;
        }

        // Initializes this Image object.  This is identical to calling the image's
        // constructor with picture, but this allows non-constructor initialization,
        // which may be necessary in some instances.
        private unsafe void Initialize(int width, int height)
        {
            if (_iconData == null || _handle != IntPtr.Zero)
            {
                throw new InvalidOperationException(SR.Format(SR.IllegalState, GetType().Name));
            }

            int icondirSize = Marshal.SizeOf(typeof(SafeNativeMethods.ICONDIR));
            if (_iconData.Length < icondirSize)
            {
                throw new ArgumentException(SR.Format(SR.InvalidPictureType, "picture", nameof(Icon)));
            }

            // Get the correct width and height.
            if (width == 0)
            {
                width = UnsafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_CXICON);
            }

            if (height == 0)
            {
                height = UnsafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_CYICON);
            }

            if (s_bitDepth == 0)
            {
                IntPtr dc = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
                s_bitDepth = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dc), SafeNativeMethods.BITSPIXEL);
                s_bitDepth *= UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dc), SafeNativeMethods.PLANES);
                UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, dc));

                // If the bitdepth is 8, make it 4 because windows does not
                // choose a 256 color icon if the display is running in 256 color mode
                // due to palette flicker.
                if (s_bitDepth == 8)
                {
                    s_bitDepth = 4;
                }
            }

            fixed (byte* pbIconData = _iconData)
            {
                short idReserved = GetShort(pbIconData);
                short idType = GetShort(pbIconData + 2);
                short idCount = GetShort(pbIconData + 4);

                if (idReserved != 0 || idType != 1 || idCount == 0)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidPictureType, "picture", nameof(Icon)));
                }

                SafeNativeMethods.ICONDIRENTRY EntryTemp;

                byte bestWidth = 0;
                byte bestHeight = 0;

                byte* pbIconDirEntry = unchecked(pbIconData + 6);
                int icondirEntrySize = Marshal.SizeOf(typeof(SafeNativeMethods.ICONDIRENTRY));

                if ((icondirEntrySize * (idCount - 1) + icondirSize) > _iconData.Length)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidPictureType, "picture", nameof(Icon)));
                }

                for (int i = 0; i < idCount; i++)
                {
                    // Fill in EntryTemp
                    EntryTemp.bWidth = pbIconDirEntry[0];
                    EntryTemp.bHeight = pbIconDirEntry[1];
                    EntryTemp.bColorCount = pbIconDirEntry[2];
                    EntryTemp.bReserved = pbIconDirEntry[3];
                    EntryTemp.wPlanes = GetShort(pbIconDirEntry + 4);
                    EntryTemp.wBitCount = GetShort(pbIconDirEntry + 6);
                    EntryTemp.dwBytesInRes = GetInt(pbIconDirEntry + 8);
                    EntryTemp.dwImageOffset = GetInt(pbIconDirEntry + 12);

                    bool fUpdateBestFit = false;
                    int iconBitDepth = 0;
                    if (EntryTemp.bColorCount != 0)
                    {
                        iconBitDepth = 4;
                        if (EntryTemp.bColorCount < 0x10)
                        {
                            iconBitDepth = 1;
                        }
                    }
                    else
                    {
                        iconBitDepth = EntryTemp.wBitCount;
                    }

                    // If it looks like if nothing is specified at this point then set the bits per pixel to 8.
                    if (iconBitDepth == 0)
                    {
                        iconBitDepth = 8;
                    }

                    //  Windows rules for specifing an icon:
                    //
                    //  1.  The icon with the closest size match.
                    //  2.  For matching sizes, the image with the closest bit depth.
                    //  3.  If there is no color depth match, the icon with the closest color depth that does not exceed the display.
                    //  4.  If all icon color depth > display, lowest color depth is chosen.
                    //  5.  color depth of > 8bpp are all equal.
                    //  6.  Never choose an 8bpp icon on an 8bpp system.
                    //

                    if (_bestBytesInRes == 0)
                    {
                        fUpdateBestFit = true;
                    }
                    else
                    {
                        int bestDelta = Math.Abs(bestWidth - width) + Math.Abs(bestHeight - height);
                        int thisDelta = Math.Abs(EntryTemp.bWidth - width) + Math.Abs(EntryTemp.bHeight - height);

                        if ((thisDelta < bestDelta) ||
                            (thisDelta == bestDelta && (iconBitDepth <= s_bitDepth && iconBitDepth > _bestBitDepth || _bestBitDepth > s_bitDepth && iconBitDepth < _bestBitDepth)))
                        {
                            fUpdateBestFit = true;
                        }
                    }

                    if (fUpdateBestFit)
                    {
                        bestWidth = EntryTemp.bWidth;
                        bestHeight = EntryTemp.bHeight;
                        _bestImageOffset = EntryTemp.dwImageOffset;
                        _bestBytesInRes = EntryTemp.dwBytesInRes;
                        _bestBitDepth = iconBitDepth;
                    }

                    pbIconDirEntry += icondirEntrySize;
                }

                if (_bestImageOffset < 0)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidPictureType, "picture", nameof(Icon)));
                }

                if (_bestBytesInRes < 0)
                {
                    throw new Win32Exception(SafeNativeMethods.ERROR_INVALID_PARAMETER);
                }

                int endOffset;
                try
                {
                    endOffset = checked(_bestImageOffset + _bestBytesInRes);
                }
                catch (OverflowException)
                {
                    throw new Win32Exception(SafeNativeMethods.ERROR_INVALID_PARAMETER);
                }

                if (endOffset > _iconData.Length)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidPictureType, "picture", nameof(Icon)));
                }

                // Copy the bytes into an aligned buffer if needed.
                if ((_bestImageOffset % IntPtr.Size) != 0)
                {
                    // Beginning of icon's content is misaligned.
                    byte[] alignedBuffer = new byte[_bestBytesInRes];
                    Array.Copy(_iconData, _bestImageOffset, alignedBuffer, 0, _bestBytesInRes);

                    fixed (byte* pbAlignedBuffer = alignedBuffer)
                    {
                        _handle = SafeNativeMethods.CreateIconFromResourceEx(pbAlignedBuffer, _bestBytesInRes, true, 0x00030000, 0, 0, 0);
                    }
                }
                else
                {
                    try
                    {
                        _handle = SafeNativeMethods.CreateIconFromResourceEx(checked(pbIconData + _bestImageOffset), _bestBytesInRes, true, 0x00030000, 0, 0, 0);
                    }
                    catch (OverflowException)
                    {
                        throw new Win32Exception(SafeNativeMethods.ERROR_INVALID_PARAMETER);
                    }
                }
                if (_handle == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
            }
        }

        public void Save(Stream outputStream)
        {
            if (_iconData != null)
            {
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
                        // We threw this way on NetFX
                        if (outputStream == null)
                            throw new ArgumentNullException("dataStream");

                        picture.SaveAsFile(new GPStream(outputStream, makeSeekable: false), -1, out int temp);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(picture);
                    }
                }
            }
        }

        private void CopyBitmapData(BitmapData sourceData, BitmapData targetData)
        {
            int offsetSrc = 0;
            int offsetDest = 0;

            Debug.Assert(sourceData.Height == targetData.Height, "Unexpected height. How did this happen?");

            for (int i = 0; i < Math.Min(sourceData.Height, targetData.Height); i++)
            {
                IntPtr srcPtr, destPtr;
                if (IntPtr.Size == 4)
                {
                    srcPtr = new IntPtr(sourceData.Scan0.ToInt32() + offsetSrc);
                    destPtr = new IntPtr(targetData.Scan0.ToInt32() + offsetDest);
                }
                else
                {
                    srcPtr = new IntPtr(sourceData.Scan0.ToInt64() + offsetSrc);
                    destPtr = new IntPtr(targetData.Scan0.ToInt64() + offsetDest);
                }

                UnsafeNativeMethods.CopyMemory(new HandleRef(this, destPtr), new HandleRef(this, srcPtr), Math.Abs(targetData.Stride));

                offsetSrc += sourceData.Stride;
                offsetDest += targetData.Stride;
            }
        }

        private static bool BitmapHasAlpha(BitmapData bmpData)
        {
            bool hasAlpha = false;
            for (int i = 0; i < bmpData.Height; i++)
            {
                for (int j = 3; j < Math.Abs(bmpData.Stride); j += 4)
                {
                    // Stride here is fine since we know we're doing this on the whole image.
                    unsafe
                    {
                        byte* candidate = unchecked(((byte*)bmpData.Scan0.ToPointer()) + (i * bmpData.Stride) + j);
                        if (*candidate != 0)
                        {
                            hasAlpha = true;
                            return hasAlpha;
                        }
                    }
                }
            }

            return false;
        }

        public Bitmap ToBitmap()
        {
            // DontSupportPngFramesInIcons is true when the application is targeting framework version below 4.6
            // and false when the application is targeting 4.6 and above. Downlevel application can also set the following switch
            // to false in the .config file's runtime section in order to opt-in into the new behavior:
            // <AppContextSwitchOverrides value="Switch.System.Drawing.DontSupportPngFramesInIcons=false" />
            if (HasPngSignature() && !LocalAppContextSwitches.DontSupportPngFramesInIcons)
            {
                return PngFrame();
            }

            return BmpFrame();
        }

        private Bitmap BmpFrame()
        {
            Bitmap bitmap = null;
            if (_iconData != null && _bestBitDepth == 32)
            {
                // GDI+ doesnt handle 32 bpp icons with alpha properly
                // we load the icon ourself from the byte table
                bitmap = new Bitmap(Size.Width, Size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Debug.Assert(_bestImageOffset >= 0 && (_bestImageOffset + _bestBytesInRes) <= _iconData.Length, "Illegal offset/length for the Icon data");

                unsafe
                {
                    BitmapData bmpdata = bitmap.LockBits(new Rectangle(0, 0, Size.Width, Size.Height),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format32bppArgb);
                    try
                    {
                        uint* pixelPtr = (uint*)bmpdata.Scan0.ToPointer();

                        // jumping the image header
                        int newOffset = _bestImageOffset + Marshal.SizeOf(typeof(SafeNativeMethods.BITMAPINFOHEADER));
                        // there is no color table that we need to skip since we're 32bpp

                        int lineLength = Size.Width * 4;
                        int width = Size.Width;
                        for (int j = (Size.Height - 1) * 4; j >= 0; j -= 4)
                        {
                            Marshal.Copy(_iconData, newOffset + j * width, (IntPtr)pixelPtr, lineLength);
                            pixelPtr += width;
                        }

                        // note: we ignore the mask that's available after the pixel table
                    }
                    finally
                    {
                        bitmap.UnlockBits(bmpdata);
                    }
                }
            }
            else if (_bestBitDepth == 0 || _bestBitDepth == 32)
            {
                // This may be a 32bpp icon or an icon without any data.
                var info = new SafeNativeMethods.ICONINFO();
                SafeNativeMethods.GetIconInfo(new HandleRef(this, _handle), info);
                var bmp = new SafeNativeMethods.BITMAP();
                try
                {
                    if (info.hbmColor != IntPtr.Zero)
                    {
                        SafeNativeMethods.GetObject(new HandleRef(null, info.hbmColor), Marshal.SizeOf(typeof(SafeNativeMethods.BITMAP)), bmp);
                        if (bmp.bmBitsPixel == 32)
                        {
                            Bitmap tmpBitmap = null;
                            BitmapData bmpData = null;
                            BitmapData targetData = null;
                            try
                            {
                                tmpBitmap = Image.FromHbitmap(info.hbmColor);

                                // In GDI+ the bits are there but the bitmap was created with no alpha channel
                                // so copy the bits by hand to a new bitmap
                                // we also need to go around a limitation in the way the ICON is stored (ie if it's another bpp 
                                // but stored in 32bpp all pixels are transparent and not opaque)
                                // (Here you mostly need to remain calm....)
                                bmpData = tmpBitmap.LockBits(new Rectangle(0, 0, tmpBitmap.Width, tmpBitmap.Height), ImageLockMode.ReadOnly, tmpBitmap.PixelFormat);

                                // we need do the following if the image has alpha because otherwise the image is fully transparent even though it has data
                                if (BitmapHasAlpha(bmpData))
                                {
                                    bitmap = new Bitmap(bmpData.Width, bmpData.Height, PixelFormat.Format32bppArgb);
                                    targetData = bitmap.LockBits(new Rectangle(0, 0, bmpData.Width, bmpData.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                                    CopyBitmapData(bmpData, targetData);
                                }
                            }
                            finally
                            {
                                if (tmpBitmap != null && bmpData != null)
                                {
                                    tmpBitmap.UnlockBits(bmpData);
                                }
                                if (bitmap != null && targetData != null)
                                {
                                    bitmap.UnlockBits(targetData);
                                }
                            }
                            tmpBitmap.Dispose();
                        }
                    }
                }
                finally
                {
                    if (info.hbmColor != IntPtr.Zero)
                    {
                        SafeNativeMethods.IntDeleteObject(new HandleRef(null, info.hbmColor));
                    }
                    if (info.hbmMask != IntPtr.Zero)
                    {
                        SafeNativeMethods.IntDeleteObject(new HandleRef(null, info.hbmMask));
                    }
                }
            }


            if (bitmap == null)
            {
                // last chance... all the other cases (ie non 32 bpp icons coming from a handle or from the bitmapData)

                // we have to do this rather than just return Bitmap.FromHIcon because 
                // the bitmap returned from that, even though it's 32bpp, just paints where the mask allows it
                // seems like another GDI+ weirdness. might be interesting to investigate further. In the meantime
                // this looks like the right thing to do and is not more expansive that what was present before.

                Size size = Size;
                bitmap = new Bitmap(size.Width, size.Height); // initialized to transparent
                Graphics graphics = null;
                using (graphics = Graphics.FromImage(bitmap))
                {
                    try
                    {
                        using (Bitmap tmpBitmap = Bitmap.FromHicon(Handle))
                        {
                            graphics.DrawImage(tmpBitmap, new Rectangle(0, 0, size.Width, size.Height));
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Sometimes FromHicon will crash with no real reason.
                        // The backup plan is to just draw the image like we used to. 
                        // NOTE: FromHIcon is also where we have the buffer overrun
                        // if width and height are mismatched.
                        Draw(graphics, new Rectangle(0, 0, size.Width, size.Height));
                    }
                }


                // GDI+ fills the surface with a sentinel color for GetDC, but does
                // not correctly clean it up again, so we have to do it.
                Color fakeTransparencyColor = Color.FromArgb(0x0d, 0x0b, 0x0c);
                bitmap.MakeTransparent(fakeTransparencyColor);
            }

            Debug.Assert(bitmap != null, "Bitmap cannot be null");
            return bitmap;
        }

        private Bitmap PngFrame()
        {
            Debug.Assert(_iconData != null);
            using (var stream = new MemoryStream())
            {
                stream.Write(_iconData, _bestImageOffset, _bestBytesInRes);
                return new Bitmap(stream);
            }
        }

        private bool HasPngSignature()
        {
            if (!_isBestImagePng.HasValue)
            {
                if (_iconData != null && _iconData.Length >= _bestImageOffset + 8)
                {
                    int iconSignature1 = BitConverter.ToInt32(_iconData, _bestImageOffset);
                    int iconSignature2 = BitConverter.ToInt32(_iconData, _bestImageOffset + 4);
                    _isBestImagePng = (iconSignature1 == PNGSignature1) && (iconSignature2 == PNGSignature2);
                }
                else
                {
                    _isBestImagePng = false;
                }
            }

            return _isBestImagePng.Value;
        }

        public override string ToString() => SR.toStringIcon;

        [DllImport(ExternDll.Oleaut32, PreserveSig = false)]
        internal static extern IPicture OleCreatePictureIndirect(PICTDESC pictdesc, [In]ref Guid refiid, bool fOwn);

        [ComImport()]
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

        internal class Ole
        {
            public const int PICTYPE_ICON = 3;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class PICTDESC
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
