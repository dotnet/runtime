// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using DpiHelper = System.Windows.Forms.DpiHelper;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    /// <summary>
    /// ToolboxBitmapAttribute defines the images associated with a specified component.
    /// The component can offer a small and large image (large is optional).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ToolboxBitmapAttribute : Attribute
    {
        private Image _smallImage;
        private Image _largeImage;

        private readonly string _imageFile;
        private readonly Type _imageType;

        private readonly string _imageName;

        private static readonly Size s_largeSize = new Size(32, 32);
        private static readonly Size s_smallSize = new Size(16, 16);

        // Used to help cache the last result of BitmapSelector.GetFileName.
        private static string s_lastOriginalFileName;
        private static string s_lastUpdatedFileName;

        public ToolboxBitmapAttribute(string imageFile) : this(GetImageFromFile(imageFile, false), GetImageFromFile(imageFile, true))
        {
            _imageFile = imageFile;
        }

        public ToolboxBitmapAttribute(Type t) : this(GetImageFromResource(t, null, false), GetImageFromResource(t, null, true))
        {
            _imageType = t;
        }

        public ToolboxBitmapAttribute(Type t, string name)
            : this(GetImageFromResource(t, name, false), GetImageFromResource(t, name, true))
        {
            _imageType = t;
            _imageName = name;
        }

        private ToolboxBitmapAttribute(Image smallImage, Image largeImage)
        {
            _smallImage = smallImage;
            _largeImage = largeImage;
        }

        public override bool Equals(object value)
        {
            if (value == this)
            {
                return true;
            }

            if (value is ToolboxBitmapAttribute attr)
            {
                return attr._smallImage == _smallImage && attr._largeImage == _largeImage;
            }

            return false;
        }

        public override int GetHashCode() => base.GetHashCode();

        public Image GetImage(object component) => GetImage(component, true);

        public Image GetImage(object component, bool large)
        {
            if (component != null)
            {
                return GetImage(component.GetType(), large);
            }

            return null;
        }

        public Image GetImage(Type type) => GetImage(type, false);

        public Image GetImage(Type type, bool large) => GetImage(type, null, large);
        
        public Image GetImage(Type type, string imgName, bool large)
        {
            if ((large && _largeImage == null) || (!large && _smallImage == null))
            {
                Image img = null;
                if (large)
                {
                    img = _largeImage;
                }
                else
                {
                    img = _smallImage;
                }

                if (img == null)
                {
                    img = GetImageFromResource(type, imgName, large);
                }

                // last resort for large images.
                if (large && _largeImage == null && _smallImage != null)
                {
                    img = new Bitmap((Bitmap)_smallImage, s_largeSize.Width, s_largeSize.Height);
                }

                if (img is Bitmap b)
                {
                    MakeBackgroundAlphaZero(b);
                }

                if (img == null)
                {
                    img = s_defaultComponent.GetImage(type, large);

                    // We don't want to hand out the static shared image 
                    // because otherwise it might get disposed. 
                    if (img != null)
                    {
                        img = (Image)img.Clone();
                    }
                }

                if (large)
                {
                    _largeImage = img;
                }
                else
                {
                    _smallImage = img;
                }
            }

            Image toReturn = (large) ? _largeImage : _smallImage;

            if (Equals(Default))
            {
                _largeImage = null;
                _smallImage = null;
            }

            return toReturn;
        }

        // Helper to get the right icon from the given stream that represents an icon.
        private static Image GetIconFromStream(Stream stream, bool large, bool scaled)
        {
            if (stream == null)
            {
                return null;
            }
            Icon ico = new Icon(stream);
            Icon sizedico = new Icon(ico, large ? s_largeSize : s_smallSize);
            Bitmap b = sizedico.ToBitmap();
            if (DpiHelper.IsScalingRequired && scaled)
            {
                DpiHelper.ScaleBitmapLogicalToDevice(ref b);
            }
            return b;
        }

        // Cache the last result of BitmapSelector.GetFileName because we commonly load images twice
        // in succession from the same file and we don't need to compute the name twice.
        private static string GetFileNameFromBitmapSelector(string originalName)
        {
            if (originalName != s_lastOriginalFileName)
            {
                s_lastOriginalFileName = originalName;
                s_lastUpdatedFileName = BitmapSelector.GetFileName(originalName);
            }

            return s_lastUpdatedFileName;
        }

        // Just forwards to Image.FromFile eating any non-critical exceptions that may result.
        private static Image GetImageFromFile(string imageFile, bool large, bool scaled = true)
        {
            Image image = null;
            try
            {
                if (imageFile != null)
                {
                    imageFile = GetFileNameFromBitmapSelector(imageFile);

                    string ext = Path.GetExtension(imageFile);
                    if (ext != null && string.Equals(ext, ".ico", StringComparison.OrdinalIgnoreCase))
                    {
                        //ico files support both large and small, so we respect the large flag here.
                        using (FileStream reader = File.OpenRead(imageFile))
                        {
                            image = GetIconFromStream(reader, large, scaled);
                        }
                    }
                    else if (!large)
                    {
                        //we only read small from non-ico files.
                        image = Image.FromFile(imageFile);
                        Bitmap b = image as Bitmap;
                        if (DpiHelper.IsScalingRequired && scaled)
                        {
                            DpiHelper.ScaleBitmapLogicalToDevice(ref b);
                        }
                    }
                }
            }
            catch (Exception e) when (!ClientUtils.IsCriticalException(e))
            {
            }

            return image;
        }

        private static Image GetBitmapFromResource(Type t, string bitmapname, bool large, bool scaled)
        {
            if (bitmapname == null)
            {
                return null;
            }

            Image img = null;

            // Load the image from the manifest resources.
            Stream stream = BitmapSelector.GetResourceStream(t, bitmapname);
            if (stream != null)
            {
                Bitmap b = new Bitmap(stream);
                img = b;
                MakeBackgroundAlphaZero(b);
                if (large)
                {
                    img = new Bitmap(b, s_largeSize.Width, s_largeSize.Height);
                }
                if (DpiHelper.IsScalingRequired && scaled)
                {
                    b = (Bitmap)img;
                    DpiHelper.ScaleBitmapLogicalToDevice(ref b);
                    img = b;
                }
            }
            return img;
        }

        private static Image GetIconFromResource(Type t, string bitmapname, bool large, bool scaled)
        {
            if (bitmapname == null)
            {
                return null;
            }

            return GetIconFromStream(BitmapSelector.GetResourceStream(t, bitmapname), large, scaled);
        }

        public static Image GetImageFromResource(Type t, string imageName, bool large)
        {
            return GetImageFromResource(t, imageName, large, scaled: true);
        }

        internal static Image GetImageFromResource(Type t, string imageName, bool large, bool scaled)
        {
            Image img = null;
            try
            {
                string name = imageName;
                string iconname = null;
                string bmpname = null;
                string rawbmpname = null;

                // If we didn't get a name, use the class name
                if (name == null)
                {
                    name = t.FullName;
                    int indexDot = name.LastIndexOf('.');
                    if (indexDot != -1)
                    {
                        name = name.Substring(indexDot + 1);
                    }

                    // All bitmap images from winforms runtime are changed to Icons
                    // and logical names, now, does not contain any extension.
                    rawbmpname = name;
                    iconname = name + ".ico";
                    bmpname = name + ".bmp";
                }
                else
                {
                    if (string.Equals(Path.GetExtension(imageName), ".ico", StringComparison.CurrentCultureIgnoreCase))
                    {
                        iconname = name;
                    }
                    else if (string.Equals(Path.GetExtension(imageName), ".bmp", StringComparison.CurrentCultureIgnoreCase))
                    {
                        bmpname = name;
                    }
                    else
                    {
                        // We don't recognize the name as either bmp or ico. we need to try three things.
                        // 1.  the name as a bitmap (back compat)
                        // 2.  name+.bmp
                        // 3.  name+.ico
                        rawbmpname = name;
                        bmpname = name + ".bmp";
                        iconname = name + ".ico";
                    }
                }
                if (rawbmpname != null)
                {
                    img = GetBitmapFromResource(t, rawbmpname, large, scaled);
                }
                if (img == null && bmpname != null)
                {
                    img = GetBitmapFromResource(t, bmpname, large, scaled);
                }
                if (img == null && iconname != null)
                {
                    img = GetIconFromResource(t, iconname, large, scaled);
                }
            }
            catch (Exception) { }
            return img;
        }

        private static void MakeBackgroundAlphaZero(Bitmap img)
        {
            // Bitmap derived from Icon is already transparent.
            if (img.RawFormat.Guid == ImageFormat.Icon.Guid)
                return;

            Color bottomLeft = img.GetPixel(0, img.Height - 1);
            img.MakeTransparent();

            Color newBottomLeft = Color.FromArgb(0, bottomLeft);
            img.SetPixel(0, img.Height - 1, newBottomLeft);
        }

        public static readonly ToolboxBitmapAttribute Default = new ToolboxBitmapAttribute(null, (Image)null);

        private static readonly ToolboxBitmapAttribute s_defaultComponent;

        static ToolboxBitmapAttribute()
        {
            // When we call Gdip.DummyFunction, JIT will make sure Gdip..cctor will be called.
            Gdip.DummyFunction();
            
            Stream stream = BitmapSelector.GetResourceStream(typeof(ToolboxBitmapAttribute), "DefaultComponent.bmp");
            Debug.Assert(stream != null, "DefaultComponent.bmp must be present as an embedded resource.");
            
            var bitmap = new Bitmap(stream);
            MakeBackgroundAlphaZero(bitmap);
            s_defaultComponent = new ToolboxBitmapAttribute(bitmap, null);
        }
    }
}
