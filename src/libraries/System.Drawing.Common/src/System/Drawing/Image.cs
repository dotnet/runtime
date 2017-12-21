// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;

namespace System.Drawing
{
    /// <summary>
    /// An abstract base class that provides functionality for 'Bitmap', 'Icon', 'Cursor', and 'Metafile' descended classes.
    /// </summary>
    [ImmutableObject(true)]
    public abstract partial class Image : MarshalByRefObject, IDisposable, ICloneable, ISerializable
    {
        // used to work around lack of animated gif encoder... rarely set...
        private byte[] _rawData;

        internal void SetNativeImage(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Format(SR.NativeHandle0), "handle");

            nativeImage = handle;
        }

        internal static void EnsureSave(Image image, string filename, Stream dataStream)
        {
            if (image.RawFormat.Equals(ImageFormat.Gif))
            {
                bool animatedGif = false;

                Guid[] dimensions = image.FrameDimensionsList;
                foreach (Guid guid in dimensions)
                {
                    FrameDimension dimension = new FrameDimension(guid);
                    if (dimension.Equals(FrameDimension.Time))
                    {
                        animatedGif = image.GetFrameCount(FrameDimension.Time) > 1;
                        break;
                    }
                }


                if (animatedGif)
                {
                    try
                    {
                        Stream created = null;
                        long lastPos = 0;
                        if (dataStream != null)
                        {
                            lastPos = dataStream.Position;
                            dataStream.Position = 0;
                        }

                        try
                        {
                            if (dataStream == null)
                            {
                                created = dataStream = File.OpenRead(filename);
                            }

                            image._rawData = new byte[(int)dataStream.Length];
                            dataStream.Read(image._rawData, 0, (int)dataStream.Length);
                        }
                        finally
                        {
                            if (created != null)
                            {
                                created.Close();
                            }
                            else
                            {
                                dataStream.Position = lastPos;
                            }
                        }
                    }
                    // possible exceptions for reading the filename
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                    // possible exceptions for setting/getting the position inside dataStream
                    catch (NotSupportedException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    // possible exception when reading stuff into dataStream
                    catch (ArgumentException)
                    {
                    }
                }
            }
        }
    }
}

