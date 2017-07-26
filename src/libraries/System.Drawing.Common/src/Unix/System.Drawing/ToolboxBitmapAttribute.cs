// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.ToolboxBitmapAttribute.cs
//
// Authors:
//   Dennis Hayes (dennish@Raytek.com)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//
// Copyright (C) 2002 Ximian, Inc. http://www.ximian.com
// Copyright (C) 2004 Novell, Inc. http://www.novell.com
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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

using System;
using System.Reflection;

namespace System.Drawing
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ToolboxBitmapAttribute : Attribute
    {
        private Image smallImage = null;
        private Image bigImage = null;
        public static readonly ToolboxBitmapAttribute Default = new ToolboxBitmapAttribute();

        private ToolboxBitmapAttribute()
        {
        }

        public ToolboxBitmapAttribute(string imageFile)
        {
        }

        public ToolboxBitmapAttribute(Type t)
        {
            smallImage = GetImageFromResource(t, null, false);
        }

        public ToolboxBitmapAttribute(Type t, string name)
        {
            smallImage = GetImageFromResource(t, name, false);
        }

        public override bool Equals(object value)
        {
            if (!(value is ToolboxBitmapAttribute))
                return false;
            if (value == this)
                return true;
            return ((ToolboxBitmapAttribute)value).smallImage == this.smallImage;
        }

        public override int GetHashCode()
        {
            return (smallImage.GetHashCode() ^ bigImage.GetHashCode());
        }

        public Image GetImage(object component)
        {
            return GetImage(component.GetType(), null, false);
        }

        public Image GetImage(object component, bool large)
        {
            return GetImage(component.GetType(), null, large);
        }

        public Image GetImage(Type type)
        {
            return GetImage(type, null, false);
        }

        public Image GetImage(Type type, bool large)
        {
            return GetImage(type, null, large);
        }

        public Image GetImage(Type type, string imgName, bool large)
        {
            if (smallImage == null)
                smallImage = GetImageFromResource(type, imgName, false);

            if (large)
            {
                if (bigImage == null)
                    bigImage = new Bitmap(smallImage, 32, 32);
                return bigImage;
            }
            else
                return smallImage;
        }

        public static Image GetImageFromResource(Type t, string imageName, bool large)
        {
            Bitmap bitmap;
            if (imageName == null)
                imageName = t.Name + ".bmp";

            try
            {
                using (System.IO.Stream s = t.GetTypeInfo().Assembly.GetManifestResourceStream(t.Namespace + "." + imageName))
                {
                    if (s == null)
                    {
                        return null;
                    }
                    else
                    {
                        bitmap = new Bitmap(s, false);
                    }
                }

                //FIXME: thrown too easily
                //if (bitmap.Width != 16 || bitmap.Height != 16)
                //    throw new Exception ("ToolboxBitmap must be 16x16 pixels");

                if (large)
                    return new Bitmap(bitmap, 32, 32);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
