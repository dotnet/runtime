// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Resources.Extensions.Tests
{
    public static class TestData
    {
        public static IReadOnlyDictionary<string, object> Primitive { get; } =
            new Dictionary<string, object>()
            {
                ["string"] = "value",
                ["bool"] = true,
                ["char"] = 'b',
                ["int"] = 42,
                ["uint"] = 4000000,
                ["byte"] = (byte)7,
                ["sbyte"] = (sbyte)-3,
                ["short"] = (short)31000,
                ["ushort"] = (ushort)61000,
                ["long"] = 10000000000,
                ["ulong"] = ulong.MaxValue,
                ["float"] = 3.14f,
                ["double"] = 3.14159,
                ["decimal"] = 3.141596536897931m,
                ["DateTime"] = new DateTime(2000, 1, 1, 0, 0, 1, DateTimeKind.Unspecified),
                ["TimeSpan"] = TimeSpan.FromDays(1)
            };

        public static IReadOnlyDictionary<string, object> PrimitiveAsString { get; } =
            Primitive.ToDictionary(p => p.Key + "_AsString", p => p.Value);

        public static IReadOnlyDictionary<string, object> BinaryFormatted
        {
            get => PlatformDetection.IsDrawingSupported ?
                  BinaryFormattedDrawing : BinaryFormattedWithoutDrawing;
        }
        public static Dictionary<string, object> BinaryFormattedWithoutDrawing { get; } =
            new Dictionary<string, object>()
            {
                ["enum_bin"] = DayOfWeek.Friday,
                ["point_bin"] = new Point(4, 8)
            };

        public static Dictionary<string, object> BinaryFormattedWithoutDrawingNoType { get; } =
            BinaryFormattedWithoutDrawing.ToDictionary(p => p.Key + "_NoType", p => p.Value);

        public static IReadOnlyDictionary<string, object> BinaryFormattedDrawing
        {
            get => new Dictionary<string, object>(BinaryFormattedWithoutDrawing)
            {
                ["bitmap_bin"] = new Bitmap(Path.Combine("bitmaps", "almogaver24bits.bmp")),
                ["font_bin"] = SystemFonts.DefaultFont
            };
        }

        public static IReadOnlyDictionary<string, object> ByteArrayConverter
        {
            // ImageConverter is part of System.Windows.Extensions.
            get => PlatformDetection.IsDrawingSupported && PlatformDetection.IsWindows ?
                ByteArrayConverterDrawing : ByteArrayConverterWithoutDrawing;
        }

        public static Dictionary<string, object> ByteArrayConverterWithoutDrawing { get; } =
            new Dictionary<string, object>()
            {
                ["myResourceType_bytes"] = new MyResourceType(new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89 })
            };

        public static IReadOnlyDictionary<string, object> ByteArrayConverterDrawing
        {
            get => new Dictionary<string, object>(ByteArrayConverterWithoutDrawing)
            {
                ["bitmap_bytes"] = new Bitmap(Path.Combine("bitmaps", "almogaver24bits.bmp")),
                ["icon_bytes"] = new Icon(Path.Combine("bitmaps", "32x32_one_entry_4bit.ico"))
            };
        }

        public static IReadOnlyDictionary<string, object> StringConverter
        {
            // ImageFormatConverter is part of System.Windows.Extensions.
            get => PlatformDetection.IsDrawingSupported && PlatformDetection.IsWindows ?
                StringConverterDrawing : StringConverterWithoutDrawing;
        }

        public static Dictionary<string, object> StringConverterWithoutDrawing { get; } =
            new Dictionary<string, object>()
            {
                ["color_string"] = Color.AliceBlue,
                ["point_string"] = new Point(2, 6),
                ["rect_string"] = new Rectangle(3, 6, 10, 20),
                ["size_string"] = new Size(4, 8),
                ["sizeF_string"] = new SizeF(4.2f, 8.5f),
                ["cultureInfo_string"] = new CultureInfo("en-US"),
                ["enum_string"] = DayOfWeek.Friday
            };

        public static IReadOnlyDictionary<string, object> StringConverterDrawing
        {
            get => new Dictionary<string, object>(StringConverterWithoutDrawing)
            {
                ["imageFormat_string"] = ImageFormat.Png,
                ["font_string"] = SystemFonts.DefaultFont
            };
        }

        public static IReadOnlyDictionary<string, (Type type, Stream stream)> Activator
        {
            get => PlatformDetection.IsDrawingSupported ?
                ActivatorDrawing : ActivatorWithoutDrawing;
        }

        public static Dictionary<string, (Type type, Stream stream)> ActivatorWithoutDrawing { get; } =
            new Dictionary<string, (Type type, Stream stream)>()
            {
                ["myResourceType_stream"] = (typeof(MyResourceType), new MemoryStream(new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89 }))
            };

        public static IReadOnlyDictionary<string, (Type type, Stream stream)> ActivatorDrawing
        {
            get => new Dictionary<string, (Type type, Stream stream)>(ActivatorWithoutDrawing)
            {
                ["icon_stream"] = (typeof(Icon), File.OpenRead(Path.Combine("bitmaps", "32x32_one_entry_4bit.ico"))),
                ["bitmap_stream"] = (typeof(Bitmap), File.OpenRead(Path.Combine("bitmaps", "almogaver24bits.bmp")))
            };
        }

        public static string GetStringValue(object value)
        {
            Type resourceType = value.GetType();
            TypeConverter converter = TypeDescriptor.GetConverter(resourceType);
            return converter.ConvertToInvariantString(value);
        }

        public static string GetSerializationTypeName(Type runtimeType)
        {
            object[] typeAttributes = runtimeType.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false);
            if (typeAttributes != null && typeAttributes.Length > 0)
            {
                TypeForwardedFromAttribute typeForwardedFromAttribute = (TypeForwardedFromAttribute)typeAttributes[0];
                return $"{runtimeType.FullName}, {typeForwardedFromAttribute.AssemblyFullName}";
            }
            else if (runtimeType.Assembly == typeof(object).Assembly)
            {
                // no attribute and in corelib. Strip the assembly name and hope its in CoreLib on other frameworks
                return runtimeType.FullName;
            }

            return runtimeType.AssemblyQualifiedName;
        }

        public static void WriteResources(string file)
        {
            WriteResourcesStream(File.Create(file));
        }

        public static void WriteResourcesStream(Stream stream)
        {
            using (var writer = new PreserializedResourceWriter(stream))
            {
                foreach (var pair in Primitive)
                {
                    writer.AddResource(pair.Key, pair.Value);
                }

                foreach (var pair in PrimitiveAsString)
                {
                    writer.AddResource(pair.Key, GetSerializationTypeName(pair.Value.GetType()), GetStringValue(pair.Value));
                }

                var formatter = new BinaryFormatter();
                foreach (var pair in BinaryFormattedWithoutDrawing)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        formatter.Serialize(memoryStream, pair.Value);
                        writer.AddBinaryFormattedResource(pair.Key, GetSerializationTypeName(pair.Value.GetType()), memoryStream.ToArray());
                    }
                }

                foreach (var pair in BinaryFormattedWithoutDrawingNoType)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        formatter.Serialize(memoryStream, pair.Value);
                        writer.AddBinaryFormattedResource(pair.Key, memoryStream.ToArray());
                    }
                }

                foreach (var pair in ByteArrayConverterWithoutDrawing)
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(pair.Value.GetType());
                    byte[] buffer = (byte[])converter.ConvertTo(pair.Value, typeof(byte[]));
                    writer.AddTypeConverterResource(pair.Key, GetSerializationTypeName(pair.Value.GetType()), buffer);
                }

                foreach (var pair in StringConverterWithoutDrawing)
                {
                    writer.AddResource(pair.Key, GetSerializationTypeName(pair.Value.GetType()), GetStringValue(pair.Value));
                }

                foreach (var pair in ActivatorWithoutDrawing)
                {
                    writer.AddActivatorResource(pair.Key, GetSerializationTypeName(pair.Value.type), pair.Value.stream, false);
                }

                writer.Generate();
            }
        }
    }
}
