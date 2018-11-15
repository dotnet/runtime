// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Xunit;

namespace System.ComponentModel.TypeConverterTests
{
    public class IconConverterTest
    {
        private readonly Icon _icon = null;
        private readonly IconConverter _icoConv = null;
        private readonly IconConverter _icoConvFrmTD = null;
        private readonly string _iconStr = null;
        private readonly byte[] _iconBytes = null;

        public IconConverterTest()
        {
            _icon = new Icon(Path.Combine("bitmaps", "TestIcon.ico"));
            _iconStr = _icon.ToString();

            using (MemoryStream destStream = new MemoryStream())
            {
                _icon.Save(destStream);
                _iconBytes = destStream.ToArray();
            }

            _icoConv = new IconConverter();
            _icoConvFrmTD = (IconConverter)TypeDescriptor.GetConverter(_icon);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TestCanConvertFrom()
        {
            Assert.True(_icoConv.CanConvertFrom(typeof(byte[])), "byte[] (no context)");
            Assert.True(_icoConv.CanConvertFrom(null, typeof(byte[])), "byte[]");
            Assert.True(_icoConv.CanConvertFrom(null, _iconBytes.GetType()), "_iconBytes.GetType()");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(string)), "string");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(Rectangle)), "Rectangle");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(Point)), "Point");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(PointF)), "PointF");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(Size)), "Size");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(SizeF)), "SizeF");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(object)), "object");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(int)), "int");
            Assert.False(_icoConv.CanConvertFrom(null, typeof(Metafile)), "Metafile");

            Assert.True(_icoConvFrmTD.CanConvertFrom(typeof(byte[])), "TD byte[] (no context)");
            Assert.True(_icoConvFrmTD.CanConvertFrom(null, typeof(byte[])), "TD byte[]");
            Assert.True(_icoConvFrmTD.CanConvertFrom(null, _iconBytes.GetType()), "TD _iconBytes.GetType()");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(string)), "TD string");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(Rectangle)), "TD Rectangle");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(Point)), "TD Point");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(PointF)), "TD PointF");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(Size)), "TD Size");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(SizeF)), "TD SizeF");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(object)), "TD object");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(int)), "TD int");
            Assert.False(_icoConvFrmTD.CanConvertFrom(null, typeof(Metafile)), "TD Metafile");
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TestCanConvertTo()
        {
            Assert.True(_icoConv.CanConvertTo(typeof(string)), "string (no context)");
            Assert.True(_icoConv.CanConvertTo(null, typeof(string)), "string");
            Assert.True(_icoConv.CanConvertTo(null, _iconStr.GetType()), "_iconStr.GetType()");
            Assert.True(_icoConv.CanConvertTo(typeof(byte[])), "byte[] (no context)");
            Assert.True(_icoConv.CanConvertTo(null, typeof(byte[])), "byte[]");
            Assert.True(_icoConv.CanConvertTo(null, _iconBytes.GetType()), "_iconBytes.GetType()");
            Assert.True(_icoConv.CanConvertTo(typeof(Image)), "Image (no context)");
            Assert.True(_icoConv.CanConvertTo(null, typeof(Image)), "Image");
            Assert.True(_icoConv.CanConvertTo(typeof(Bitmap)), "Bitmap (no context)");
            Assert.True(_icoConv.CanConvertTo(null, typeof(Bitmap)), "Bitmap");
            Assert.False(_icoConv.CanConvertTo(null, typeof(Rectangle)), "Rectangle");
            Assert.False(_icoConv.CanConvertTo(null, typeof(Point)), "Point");
            Assert.False(_icoConv.CanConvertTo(null, typeof(PointF)), "PointF");
            Assert.False(_icoConv.CanConvertTo(null, typeof(Size)), "Size");
            Assert.False(_icoConv.CanConvertTo(null, typeof(SizeF)), "SizeF");
            Assert.False(_icoConv.CanConvertTo(null, typeof(object)), "object");
            Assert.False(_icoConv.CanConvertTo(null, typeof(int)), "int");

            Assert.True(_icoConvFrmTD.CanConvertTo(typeof(string)), "TD string (no context)");
            Assert.True(_icoConvFrmTD.CanConvertTo(null, typeof(string)), "TD string");
            Assert.True(_icoConvFrmTD.CanConvertTo(null, _iconStr.GetType()), "TD _iconStr.GetType()");
            Assert.True(_icoConvFrmTD.CanConvertTo(typeof(byte[])), "TD byte[] (no context)");
            Assert.True(_icoConvFrmTD.CanConvertTo(null, typeof(byte[])), "TD byte[]");
            Assert.True(_icoConvFrmTD.CanConvertTo(null, _iconBytes.GetType()), "TD _iconBytes.GetType()");
            Assert.True(_icoConvFrmTD.CanConvertTo(typeof(Image)), "TD Image (no context)");
            Assert.True(_icoConvFrmTD.CanConvertTo(null, typeof(Image)), "TD Image");
            Assert.True(_icoConvFrmTD.CanConvertTo(typeof(Bitmap)), "TD Bitmap (no context)");
            Assert.True(_icoConvFrmTD.CanConvertTo(null, typeof(Bitmap)), "TD Bitmap");
            Assert.False(_icoConvFrmTD.CanConvertTo(null, typeof(Rectangle)), "TD Rectangle");
            Assert.False(_icoConvFrmTD.CanConvertTo(null, typeof(Point)), "TD Point");
            Assert.False(_icoConvFrmTD.CanConvertTo(null, typeof(PointF)), "TD PointF");
            Assert.False(_icoConvFrmTD.CanConvertTo(null, typeof(Size)), "TD Size");
            Assert.False(_icoConvFrmTD.CanConvertTo(null, typeof(SizeF)), "TD SizeF");
            Assert.False(_icoConvFrmTD.CanConvertTo(null, typeof(object)), "TD object");
            Assert.False(_icoConvFrmTD.CanConvertTo(null, typeof(int)), "TD int");
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TestConvertFrom()
        {
            Icon newIcon = (Icon)_icoConv.ConvertFrom(null, CultureInfo.InvariantCulture, _iconBytes);

            Assert.Equal(_icon.Height, newIcon.Height);
            Assert.Equal(_icon.Width, newIcon.Width);

            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertFrom("System.Drawing.String"));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertFrom(null, CultureInfo.InvariantCulture, "System.Drawing.String"));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertFrom(null, CultureInfo.InvariantCulture, new Bitmap(20, 20)));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertFrom(null, CultureInfo.InvariantCulture, new Point(10, 10)));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertFrom(null, CultureInfo.InvariantCulture, new SizeF(10, 10)));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertFrom(null, CultureInfo.InvariantCulture, new object()));


            newIcon = (Icon)_icoConvFrmTD.ConvertFrom(null, CultureInfo.InvariantCulture, _iconBytes);

            Assert.Equal(_icon.Height, newIcon.Height);
            Assert.Equal(_icon.Width, newIcon.Width);

            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertFrom("System.Drawing.String"));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertFrom(null, CultureInfo.InvariantCulture, "System.Drawing.String"));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertFrom(null, CultureInfo.InvariantCulture, new Bitmap(20, 20)));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertFrom(null, CultureInfo.InvariantCulture, new Point(10, 10)));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertFrom(null, CultureInfo.InvariantCulture, new SizeF(10, 10)));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertFrom(null, CultureInfo.InvariantCulture, new object()));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TestConvertTo()
        {
            Assert.Equal(_iconStr, (string)_icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(string)));
            Assert.Equal(_iconStr, (string)_icoConv.ConvertTo(_icon, typeof(string)));

            byte[] newIconBytes = (byte[])_icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, _iconBytes.GetType());
            Assert.Equal(_iconBytes, newIconBytes);

            newIconBytes = (byte[])_icoConv.ConvertTo(_icon, _iconBytes.GetType());
            Assert.Equal(_iconBytes, newIconBytes);

            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(Rectangle)));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, _icon.GetType()));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(Size)));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(Point)));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(Metafile)));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(object)));
            Assert.Throws<NotSupportedException>(() => _icoConv.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(int)));

            Assert.Equal(_iconStr, (string)_icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(string)));
            Assert.Equal(_iconStr, (string)_icoConvFrmTD.ConvertTo(_icon, typeof(string)));


            newIconBytes = (byte[])_icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, _iconBytes.GetType());
            Assert.Equal(_iconBytes, newIconBytes);

            newIconBytes = (byte[])_icoConvFrmTD.ConvertTo(_icon, _iconBytes.GetType());
            Assert.Equal(_iconBytes, newIconBytes);

            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(Rectangle)));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, _icon.GetType()));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(Size)));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(Point)));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(Metafile)));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(object)));
            Assert.Throws<NotSupportedException>(() => _icoConvFrmTD.ConvertTo(null, CultureInfo.InvariantCulture, _icon, typeof(int)));

            Assert.Equal("(none)", (string)_icoConv.ConvertTo(null, typeof(string)));
        }
    }
}
