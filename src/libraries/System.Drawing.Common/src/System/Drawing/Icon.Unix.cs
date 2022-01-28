// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Drawing.Icon.cs
//
// Authors:
//   Gary Barnett (gary.barnett.mono@gmail.com)
//   Dennis Hayes (dennish@Raytek.com)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//   Sanjay Gupta (gsanjay@novell.com)
//   Peter Dennis Bartok (pbartok@novell.com)
//   Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2002 Ximian, Inc. http://www.ximian.com
// Copyright (C) 2004-2008 Novell, Inc (http://www.novell.com)
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

using System.Collections;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    [Editor("System.Drawing.Design.IconEditor, System.Drawing.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [TypeConverter(typeof(IconConverter))]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed partial class Icon : MarshalByRefObject, ISerializable, ICloneable, IDisposable
    {
        // The PNG signature is specified at http://www.w3.org/TR/PNG/#5PNG-file-signature
        private const uint PNGSignature1 = 137 + ('P' << 8) + ('N' << 16) + ('G' << 24);
        private const uint PNGSignature2 = 13 + (10 << 8) + (26 << 16) + (10 << 24);

        [StructLayout(LayoutKind.Sequential)]
        internal struct IconDirEntry
        {
            internal byte width;        // Width of icon
            internal byte height;       // Height of icon
            internal byte colorCount;   // colors in icon
            internal byte reserved; // Reserved
            internal ushort planes;         // Color Planes
            internal ushort bitCount;       // Bits per pixel
            internal uint bytesInRes;     // bytes in resource
            internal uint imageOffset;  // position in file
            internal bool png;       // for unsupported images (vista 256 png)
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct IconDir
        {
            internal ushort idReserved;   // Reserved
            internal ushort idType;       // resource type (1 for icons)
            internal ushort idCount;      // how many images?
            internal IconDirEntry[] idEntries;    // the entries for each image
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct BitmapInfoHeader
        {
            internal uint biSize;
            internal int biWidth;
            internal int biHeight;
            internal ushort biPlanes;
            internal ushort biBitCount;
            internal uint biCompression;
            internal uint biSizeImage;
            internal int biXPelsPerMeter;
            internal int biYPelsPerMeter;
            internal uint biClrUsed;
            internal uint biClrImportant;
        };

        [StructLayout(LayoutKind.Sequential)]   // added baseclass for non bmp image format support
        internal abstract class ImageData
        {
        };

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class IconImage : ImageData
        {
            internal BitmapInfoHeader iconHeader;   //image header
            internal uint[]? iconColors; //colors table
            internal byte[]? iconXOR;    // bits for XOR mask
            internal byte[]? iconAND;    //bits for AND mask
        };

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class IconDump : ImageData
        {
            internal byte[]? data;
        };

        private Size iconSize;
        private IntPtr handle = IntPtr.Zero;
        private IconDir iconDir;
        private ushort id;
        private ImageData[]? imageData;
        private bool undisposable;
        private bool disposed;
        private Bitmap? bitmap;

        private Icon()
        {
        }

        private Icon(IntPtr handle)
        {
            this.handle = handle;
            bitmap = Bitmap.FromHicon(handle);
            iconSize = new Size(bitmap.Width, bitmap.Height);
            bitmap = Bitmap.FromHicon(handle);
            iconSize = new Size(bitmap.Width, bitmap.Height);
            // FIXME: we need to convert the bitmap into an icon
            undisposable = true;
        }

        public Icon(Icon original, int width, int height)
            : this(original, new Size(width, height))
        {
        }

        public Icon(Icon original!!, Size size)
        {
            iconSize = size;
            iconDir = original.iconDir;

            int count = iconDir.idCount;
            if (count > 0)
            {
                imageData = original.imageData;
                id = ushort.MaxValue;

                for (ushort i = 0; i < count; i++)
                {
                    IconDirEntry ide = iconDir.idEntries[i];
                    if (((ide.height == size.Height) || (ide.width == size.Width)) && !ide.png)
                    {
                        id = i;
                        break;
                    }
                }

                // if a perfect match isn't found we look for the biggest icon *smaller* than specified
                if (id == ushort.MaxValue)
                {
                    int requested = Math.Min(size.Height, size.Width);
                    // previously best set to 1st image, as this might not be smallest changed loop to check all
                    IconDirEntry? best = null;
                    for (ushort i = 0; i < count; i++)
                    {
                        IconDirEntry ide = iconDir.idEntries[i];
                        if (((ide.height < requested) || (ide.width < requested)) && !ide.png)
                        {
                            if (best == null)
                            {
                                best = ide;
                                id = i;
                            }
                            else if ((ide.height > best.Value.height) || (ide.width > best.Value.width))
                            {
                                best = ide;
                                id = i;
                            }
                        }
                    }
                }

                // last one, if nothing better can be found
                if (id == ushort.MaxValue)
                {
                    int i = count;
                    while (id == ushort.MaxValue && i > 0)
                    {
                        i--;
                        if (!iconDir.idEntries[i].png)
                            id = (ushort)i;
                    }
                }

                if (id == ushort.MaxValue)
                    throw new ArgumentException(SR.NoValidIconImageFound, nameof(original));

                iconSize.Height = iconDir.idEntries[id].height;
                iconSize.Width = iconDir.idEntries[id].width;
            }
            else
            {
                iconSize.Height = size.Height;
                iconSize.Width = size.Width;
            }

            if (original.bitmap != null)
                bitmap = (Bitmap)original.bitmap.Clone();
        }

        public Icon(Stream stream) : this(stream, 32, 32)
        {
        }

        public Icon(Stream stream, int width, int height)
        {
            InitFromStreamWithSize(stream, width, height);
        }

        public Icon(string fileName)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                InitFromStreamWithSize(fs, 32, 32);
            }
        }

        public Icon(Type type, string resource!!)
        {
            // For compatibility with the .NET Framework
            if (type == null)
                throw new NullReferenceException();

            using (Stream? s = type.Assembly.GetManifestResourceStream(type, resource))
            {
                if (s == null)
                {
                    throw new ArgumentException(null);
                }
                InitFromStreamWithSize(s, 32, 32);      // 32x32 is default
            }
        }

        internal Icon(string resourceName, bool undisposable)
        {
            using (Stream? s = typeof(Icon).Assembly.GetManifestResourceStream(resourceName))
            {
                if (s == null)
                {
                    string msg = string.Format("Resource '{0}' was not found.", resourceName);
                    throw new FileNotFoundException(msg);
                }
                InitFromStreamWithSize(s, 32, 32);      // 32x32 is default
            }
            this.undisposable = true;
        }

        public Icon(Stream stream, Size size) :
            this(stream, size.Width, size.Height)
        {
        }

        public Icon(string fileName, int width, int height)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                InitFromStreamWithSize(fs, width, height);
            }
        }

        public Icon(string fileName, Size size)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                InitFromStreamWithSize(fs, size.Width, size.Height);
            }
        }

        private Icon(SerializationInfo info, StreamingContext context)
        {
            byte[] iconData = (byte[])info.GetValue("IconData", typeof(byte[]))!; // Do not rename (binary serialization)
            Size iconSize = (Size)info.GetValue("IconSize", typeof(Size))!; // Do not rename (binary serialization)
            var dataStream = new MemoryStream(iconData);

            InitFromStreamWithSize(dataStream, iconSize.Width, iconSize.Height);
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            MemoryStream ms = new MemoryStream();
            Save(ms);
            si.AddValue("IconSize", this.Size, typeof(Size)); // Do not rename (binary serialization)
            si.AddValue("IconData", ms.ToArray()); // Do not rename (binary serialization)
        }

        public static Icon ExtractAssociatedIcon(string filePath!!)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException(SR.NullOrEmptyPath, nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException(SR.CouldntFindSpecifiedFile, filePath);

            return SystemIcons.WinLogo;
        }

        public void Dispose()
        {
            // SystemIcons requires this
            if (undisposable)
                return;

            if (!disposed)
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                    bitmap = null;
                }
                GC.SuppressFinalize(this);
            }
            disposed = true;
        }

        public object Clone()
        {
            return new Icon(this, Size);
        }

        public static Icon FromHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(null, nameof(handle));

            return new Icon(handle);
        }

        private static void SaveIconImage(BinaryWriter writer, IconImage ii)
        {
            BitmapInfoHeader bih = ii.iconHeader;
            writer.Write(bih.biSize);
            writer.Write(bih.biWidth);
            writer.Write(bih.biHeight);
            writer.Write(bih.biPlanes);
            writer.Write(bih.biBitCount);
            writer.Write(bih.biCompression);
            writer.Write(bih.biSizeImage);
            writer.Write(bih.biXPelsPerMeter);
            writer.Write(bih.biYPelsPerMeter);
            writer.Write(bih.biClrUsed);
            writer.Write(bih.biClrImportant);

            //now write color table
            int colCount = ii.iconColors!.Length;
            for (int j = 0; j < colCount; j++)
                writer.Write(ii.iconColors[j]);

            //now write XOR Mask
            writer.Write(ii.iconXOR!);

            //now write AND Mask
            writer.Write(ii.iconAND!);
        }

        private static void SaveIconDump(BinaryWriter writer, IconDump id)
        {
            writer.Write(id.data!);
        }

        private static void SaveIconDirEntry(BinaryWriter writer, IconDirEntry ide, uint offset)
        {
            writer.Write(ide.width);
            writer.Write(ide.height);
            writer.Write(ide.colorCount);
            writer.Write(ide.reserved);
            writer.Write(ide.planes);
            writer.Write(ide.bitCount);
            writer.Write(ide.bytesInRes);
            writer.Write((offset == uint.MaxValue) ? ide.imageOffset : offset);
        }

        private void SaveAll(BinaryWriter writer)
        {
            writer.Write(iconDir.idReserved);
            writer.Write(iconDir.idType);
            ushort count = iconDir.idCount;
            writer.Write(count);

            for (int i = 0; i < (int)count; i++)
            {
                SaveIconDirEntry(writer, iconDir.idEntries[i], uint.MaxValue);
            }

            for (int i = 0; i < (int)count; i++)
            {

                //FIXME: HACK: 1 (out of the 8) vista type icons had additional bytes (value:0)
                //between images. This fixes the issue, but perhaps shouldnt include in production?
                while (writer.BaseStream.Length < iconDir.idEntries[i].imageOffset)
                    writer.Write((byte)0);

                if (imageData![i] is IconDump)
                    SaveIconDump(writer, (IconDump)imageData[i]);
                else
                    SaveIconImage(writer, (IconImage)imageData[i]);
            }
        }
        // TODO: check image not png (presently this method doesnt seem to be called unless width/height
        // refer to image)
        private void SaveBestSingleIcon(BinaryWriter writer, int width, int height)
        {
            writer.Write(iconDir.idReserved);
            writer.Write(iconDir.idType);
            writer.Write((ushort)1);

            // find best entry and save it
            int best = 0;
            int bitCount = 0;
            for (int i = 0; i < iconDir.idCount; i++)
            {
                IconDirEntry ide = iconDir.idEntries[i];
                if ((width == ide.width) && (height == ide.height))
                {
                    if (ide.bitCount >= bitCount)
                    {
                        bitCount = ide.bitCount;
                        best = i;
                    }
                }
            }

            SaveIconDirEntry(writer, iconDir.idEntries[best], 22);
            SaveIconImage(writer, (IconImage)imageData![best]);
        }

        private unsafe void SaveBitmapAsIcon(BinaryWriter writer)
        {
            writer.Write((ushort)0);    // idReserved must be 0
            writer.Write((ushort)1);    // idType must be 1
            writer.Write((ushort)1);    // only one icon

            // when transformed into a bitmap only a single image exists
            IconDirEntry ide = default;
            ide.width = (byte)bitmap!.Width;
            ide.height = (byte)bitmap.Height;
            ide.colorCount = 0; // 32 bbp == 0, for palette size
            ide.reserved = 0;   // always 0
            ide.planes = 0;
            ide.bitCount = 32;
            ide.imageOffset = 22;   // 22 is the first icon position (for single icon files)

            BitmapInfoHeader bih = default;
            bih.biSize = (uint)sizeof(BitmapInfoHeader);
            bih.biWidth = bitmap.Width;
            bih.biHeight = 2 * bitmap.Height; // include both XOR and AND images
            bih.biPlanes = 1;
            bih.biBitCount = 32;
            bih.biCompression = 0;
            bih.biSizeImage = 0;
            bih.biXPelsPerMeter = 0;
            bih.biYPelsPerMeter = 0;
            bih.biClrUsed = 0;
            bih.biClrImportant = 0;

            IconImage ii = new IconImage();
            ii.iconHeader = bih;
            ii.iconColors = Array.Empty<uint>();    // no palette
            int xor_size = (((bih.biBitCount * bitmap.Width + 31) & ~31) >> 3) * bitmap.Height;
            ii.iconXOR = new byte[xor_size];
            int p = 0;
            for (int y = bitmap.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color c = bitmap.GetPixel(x, y);
                    ii.iconXOR[p++] = c.B;
                    ii.iconXOR[p++] = c.G;
                    ii.iconXOR[p++] = c.R;
                    ii.iconXOR[p++] = c.A;
                }
            }
            int and_line_size = (((Width + 31) & ~31) >> 3);    // must be a multiple of 4 bytes
            int and_size = and_line_size * bitmap.Height;
            ii.iconAND = new byte[and_size];

            ide.bytesInRes = (uint)(bih.biSize + xor_size + and_size);

            SaveIconDirEntry(writer, ide, uint.MaxValue);
            SaveIconImage(writer, ii);
        }

        private void Save(Stream outputStream, int width, int height)
        {
            BinaryWriter writer = new BinaryWriter(outputStream);
            // if we have the icon information then save from this
            if (iconDir.idEntries != null)
            {
                if ((width == -1) && (height == -1))
                    SaveAll(writer);
                else
                    SaveBestSingleIcon(writer, width, height);
            }
            else if (bitmap != null)
            {
                // if the icon was created from a bitmap then convert it
                SaveBitmapAsIcon(writer);
            }
            writer.Flush();
        }

        public void Save(Stream outputStream)
        {
            if (outputStream == null)
                throw new NullReferenceException(nameof(outputStream));

            // save every icons available
            Save(outputStream, -1, -1);
        }

        internal Bitmap BuildBitmapOnWin32()
        {
            Bitmap bmp;

            if (imageData == null)
                return new Bitmap(32, 32);

            IconImage ii = (IconImage)imageData[id];
            BitmapInfoHeader bih = ii.iconHeader;
            int biHeight = bih.biHeight / 2;

            switch (bih.biBitCount)
            {
                case 1:
                    bmp = new Bitmap(bih.biWidth, biHeight, PixelFormat.Format1bppIndexed);
                    break;
                case 4:
                    bmp = new Bitmap(bih.biWidth, biHeight, PixelFormat.Format4bppIndexed);
                    break;
                case 8:
                    bmp = new Bitmap(bih.biWidth, biHeight, PixelFormat.Format8bppIndexed);
                    break;
                case 24:
                    bmp = new Bitmap(bih.biWidth, biHeight, PixelFormat.Format24bppRgb);
                    break;
                case 32:
                    bmp = new Bitmap(bih.biWidth, biHeight, PixelFormat.Format32bppArgb);
                    break;
                default:
                    string msg = string.Format("Unexpected number of bits: {0}", bih.biBitCount);
                    throw new Exception(msg);
            }

            if (bih.biBitCount < 24)
            {
                ColorPalette pal = bmp.Palette; // Managed palette

                for (int i = 0; i < ii.iconColors!.Length; i++)
                {
                    pal.Entries[i] = Color.FromArgb((int)ii.iconColors[i] | unchecked((int)0xff000000));
                }
                bmp.Palette = pal;
            }

            int bytesPerLine = (int)((((bih.biWidth * bih.biBitCount) + 31) & ~31) >> 3);
            BitmapData bits = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

            for (int y = 0; y < biHeight; y++)
            {
                Marshal.Copy(ii.iconXOR!, bytesPerLine * y,
                    (IntPtr)(bits.Scan0.ToInt64() + bits.Stride * (biHeight - 1 - y)), bytesPerLine);
            }

            bmp.UnlockBits(bits);

            bmp = new Bitmap(bmp); // This makes a 32bpp image out of an indexed one

            // Apply the mask to make properly transparent
            bytesPerLine = (int)((((bih.biWidth) + 31) & ~31) >> 3);
            for (int y = 0; y < biHeight; y++)
            {
                for (int x = 0; x < bih.biWidth / 8; x++)
                {
                    for (int bit = 7; bit >= 0; bit--)
                    {
                        if (((ii.iconAND![y * bytesPerLine + x] >> bit) & 1) != 0)
                        {
                            bmp.SetPixel(x * 8 + 7 - bit, biHeight - y - 1, Color.Transparent);
                        }
                    }
                }
            }

            return bmp;
        }

        internal Bitmap GetInternalBitmap()
        {
            if (bitmap == null)
            {
                // Mono's libgdiplus doesn't require to keep the stream alive when loading images
                using (MemoryStream ms = new MemoryStream())
                {
                    // save the current icon
                    Save(ms, Width, Height);
                    ms.Position = 0;

                    // libgdiplus can now decode icons
                    bitmap = (Bitmap)Image.LoadFromStream(ms, false);
                }
            }
            return bitmap;
        }

        // note: all bitmaps are 32bits ARGB - no matter what the icon format (bitcount) was
        public Bitmap ToBitmap()
        {
            if (disposed)
                throw new ObjectDisposedException(SR.IconInstanceWasDisposed);

            // note: we can't return the original image because
            // (a) we have no control over the bitmap instance we return (i.e. it could be disposed)
            // (b) the palette, flags won't match MS results. See MonoTests.System.Drawing.Imaging.IconCodecTest.
            //     Image16 for the differences
            return new Bitmap(GetInternalBitmap());
        }

        public override string ToString()
        {
            //is this correct, this is what returned by .Net
            return "<Icon>";
        }

        [Browsable(false)]
        public IntPtr Handle
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                // note: this handle doesn't survive the lifespan of the icon instance
                if (handle == IntPtr.Zero)
                {
                    handle = GetInternalBitmap().nativeImage;
                }
                return handle;
            }
        }

        [Browsable(false)]
        public int Height
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return iconSize.Height;
            }
        }

        public Size Size
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return iconSize;
            }
        }

        [Browsable(false)]
        public int Width
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return iconSize.Width;
            }
        }

        ~Icon()
        {
            Dispose();
        }

        private void InitFromStreamWithSize(Stream stream!!, int width, int height)
        {
            if (stream.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidPictureType, "picture", nameof(stream)));

            bool sizeObtained = false;
            ushort dirEntryCount;
            // Read the icon header
            using (var reader = new BinaryReader(stream))
            {
                iconDir.idReserved = reader.ReadUInt16();
                if (iconDir.idReserved != 0) //must be 0
                    throw new ArgumentException(SR.Format(SR.InvalidPictureType, "picture", nameof(stream)));

                iconDir.idType = reader.ReadUInt16();
                if (iconDir.idType != 1) //must be 1
                    throw new ArgumentException(SR.Format(SR.InvalidPictureType, "picture", nameof(stream)));

                dirEntryCount = reader.ReadUInt16();
                imageData = new ImageData[dirEntryCount];
                iconDir.idCount = dirEntryCount;
                iconDir.idEntries = new IconDirEntry[dirEntryCount];
                // Now read in the IconDirEntry structures
                for (int i = 0; i < dirEntryCount; i++)
                {
                    var ide = new IconDirEntry
                    {
                        width = reader.ReadByte(),
                        height = reader.ReadByte(),
                        colorCount = reader.ReadByte(),
                        reserved = reader.ReadByte(),
                        planes = reader.ReadUInt16(),
                        bitCount = reader.ReadUInt16(),
                        bytesInRes = reader.ReadUInt32(),
                        imageOffset = reader.ReadUInt32()
                    };

                    // Vista 256x256 icons points directly to a PNG bitmap
                    // 256x256 icons are decoded as 0x0 (width and height are encoded as BYTE)
                    // We mark them as png and later on we just store the raw bytes to be able to save to a file.
                    ide.png = (ide.width == 0) && (ide.height == 0);

                    iconDir.idEntries[i] = ide;

                    if (!sizeObtained)
                    {
                        if (((ide.height == height) || (ide.width == width)) && !ide.png)
                        {
                            this.id = (ushort)i;
                            sizeObtained = true;
                            this.iconSize.Height = ide.height;
                            this.iconSize.Width = ide.width;
                        }
                    }
                }

                // If we havent found the best match, return the one with the largest size.
                if (!sizeObtained)
                {
                    uint largestSize = 0;
                    for (int j = 0; j < dirEntryCount; j++)
                    {
                        if (iconDir.idEntries[j].bytesInRes >= largestSize && !iconDir.idEntries[j].png)
                        {
                            largestSize = iconDir.idEntries[j].bytesInRes;
                            this.id = (ushort)j;
                            this.iconSize.Height = iconDir.idEntries[j].height;
                            this.iconSize.Width = iconDir.idEntries[j].width;
                        }
                    }
                }

                // Now read in the icon data
                bool valid = false;
                for (int j = 0; j < dirEntryCount; j++)
                {
                    stream.Seek(iconDir.idEntries[j].imageOffset, SeekOrigin.Begin);
                    byte[] buffer = new byte[iconDir.idEntries[j].bytesInRes];
                    stream.Read(buffer, 0, buffer.Length);
                    using (var bihReader = new BinaryReader(new MemoryStream(buffer)))
                    {
                        uint headerSize = bihReader.ReadUInt32();
                        int headerWidth = bihReader.ReadInt32();

                        // Process PNG images into IconDump
                        if (iconDir.idEntries[j].png || (headerSize == PNGSignature1 && headerWidth == (int)PNGSignature2))
                        {
                            IconDump id = new IconDump();
                            id.data = buffer;
                            imageData[j] = id;
                            iconDir.idEntries[j].png = true;
                            continue;
                        }

                        // We found a valid icon BMP entry.
                        valid = true;

                        var bih = new BitmapInfoHeader
                        {
                            biSize = headerSize,
                            biWidth = headerWidth,
                            biHeight = bihReader.ReadInt32(),
                            biPlanes = bihReader.ReadUInt16(),
                            biBitCount = bihReader.ReadUInt16(),
                            biCompression = bihReader.ReadUInt32(),
                            biSizeImage = bihReader.ReadUInt32(),
                            biXPelsPerMeter = bihReader.ReadInt32(),
                            biYPelsPerMeter = bihReader.ReadInt32(),
                            biClrUsed = bihReader.ReadUInt32(),
                            biClrImportant = bihReader.ReadUInt32()
                        };
                        var iidata = new IconImage
                        {
                            iconHeader = bih
                        };
                        // Read the number of colors used and corresponding memory occupied by
                        // color table. Fill this memory chunk into rgbquad[]
                        int numColors;
                        switch (bih.biBitCount)
                        {
                            case 1:
                                numColors = 2;
                                break;
                            case 4:
                                numColors = 16;
                                break;
                            case 8:
                                numColors = 256;
                                break;
                            default:
                                numColors = 0;
                                break;
                        }

                        iidata.iconColors = new uint[numColors];
                        for (int i = 0; i < numColors; i++)
                            iidata.iconColors[i] = bihReader.ReadUInt32();

                        //XOR mask is immediately after ColorTable and its size is
                        //icon height* no. of bytes per line

                        //icon height is half of BITMAPINFOHEADER.biHeight, since it contains
                        //both XOR as well as AND mask bytes
                        int iconHeight = bih.biHeight / 2;

                        //bytes per line should be uint aligned
                        int numBytesPerLine = checked((((bih.biWidth * bih.biPlanes * bih.biBitCount) + 31) >> 5) << 2);

                        //Determine the XOR array Size
                        int xorSize = checked(numBytesPerLine * iconHeight);
                        iidata.iconXOR = new byte[xorSize];
                        int nread = bihReader.Read(iidata.iconXOR, 0, xorSize);
                        if (nread != xorSize)
                        {
                            throw new ArgumentException(SR.Format(SR.IconInvalidMaskLength, "XOR", xorSize, nread), nameof(stream));
                        }

                        //Determine the AND array size
                        numBytesPerLine = checked((((bih.biWidth) + 31) & ~31) >> 3);
                        int andSize = checked(numBytesPerLine * iconHeight);
                        iidata.iconAND = new byte[andSize];
                        nread = bihReader.Read(iidata.iconAND, 0, andSize);
                        if (nread != andSize)
                        {
                            throw new ArgumentException(SR.Format(SR.IconInvalidMaskLength, "AND", andSize, nread), nameof(stream));
                        }

                        imageData[j] = iidata;
                    }
                }

                // Throw error if no valid entries found
                if (!valid)
                    throw new Win32Exception(SafeNativeMethods.ERROR_INVALID_PARAMETER, SR.Format(SR.InvalidPictureType, "picture", nameof(stream)));
            }
        }
    }
}
