// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

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
                ["point_bin"] = new Point(4, 8),
                ["array_int_bin"] = new int[] { 1, 2, 3, 4, 5, 6 },
                ["list_int_bin"] = new List<int>() { 1, 2, 3, 4, 5, 6 },
                ["stack_Point_bin"] = new Stack<Point>(new [] { new Point(4, 8), new Point(2, 5) }),
                ["dict_string_string_bin"] = new Dictionary<string, string>()
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
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


        // Copied from  FormatterServices.cs
        internal static string GetClrTypeFullName(Type type)
        {
            return type.IsArray ?
                GetClrTypeFullNameForArray(type) :
                GetClrTypeFullNameForNonArrayTypes(type);
        }

        private static string GetClrTypeFullNameForArray(Type type)
        {
            int rank = type.GetArrayRank();
            Debug.Assert(rank >= 1);
            string typeName = GetClrTypeFullName(type.GetElementType());
            return rank == 1 ?
                typeName + "[]" :
                typeName + "[" + new string(',', rank - 1) + "]";
        }

        private static string GetClrTypeFullNameForNonArrayTypes(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            var builder = new StringBuilder(type.GetGenericTypeDefinition().FullName).Append("[");

            foreach (Type genericArgument in type.GetGenericArguments())
            {
                builder.Append("[").Append(GetClrTypeFullName(genericArgument)).Append(", ");
                builder.Append(GetClrAssemblyName(genericArgument)).Append("],");
            }

            //remove the last comma and close typename for generic with a close bracket
            return builder.Remove(builder.Length - 1, 1).Append("]").ToString();
        }

        private static string GetClrAssemblyName(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // Special case types like arrays
            Type attributedType = type;
            while (attributedType.HasElementType)
            {
                attributedType = attributedType.GetElementType();
            }

            foreach (Attribute first in attributedType.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false))
            {
                return ((TypeForwardedFromAttribute)first).AssemblyFullName;
            }

            return type.Assembly.FullName;
        }

        public static string GetSerializationTypeName(Type runtimeType)
        {
            string typeName = GetClrTypeFullName(runtimeType);
            if (runtimeType.Assembly == typeof(object).Assembly)
            {
                // In corelib. Strip the assembly name and hope its in CoreLib on other frameworks
                return typeName;
            }
            return $"{typeName}, {GetClrAssemblyName(runtimeType)}";
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
                    writer.AddResource(pair.Key, GetStringValue(pair.Value), GetSerializationTypeName(pair.Value.GetType()));
                }

                var formatter = new BinaryFormatter()
                {
                    Binder = new TypeNameManglingSerializationBinder()
                };

                foreach (var pair in BinaryFormattedWithoutDrawing)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        formatter.Serialize(memoryStream, pair.Value);
                        writer.AddBinaryFormattedResource(pair.Key, memoryStream.ToArray(), GetSerializationTypeName(pair.Value.GetType()));
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
                    writer.AddTypeConverterResource(pair.Key, buffer, GetSerializationTypeName(pair.Value.GetType()));
                }

                foreach (var pair in StringConverterWithoutDrawing)
                {
                    writer.AddResource(pair.Key, GetStringValue(pair.Value), GetSerializationTypeName(pair.Value.GetType()));
                }

                foreach (var pair in ActivatorWithoutDrawing)
                {
                    writer.AddActivatorResource(pair.Key, pair.Value.stream, GetSerializationTypeName(pair.Value.type), false);
                }

                writer.Generate();
            }
        }

        /// <summary>
        /// An approximation of ResXSerializationBinder's behavior (without retargeting) 
        /// </summary>
        internal class TypeNameManglingSerializationBinder : SerializationBinder
        {
            static readonly string s_coreAssemblyName = typeof(object).Assembly.FullName;
            static readonly string s_mscorlibAssemblyName = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                typeName = null;
                // Apply type-forwarded from here, so that we mimic what would happen in ResXWriter which runs in VS on desktop
                string assemblyQualifiedTypeName = GetSerializationTypeName(serializedType);

                // workaround for https://github.com/dotnet/runtime/issues/31289
                assemblyQualifiedTypeName = assemblyQualifiedTypeName.Replace(s_coreAssemblyName, s_mscorlibAssemblyName);

                // The logic below is intentionally incorrect. Generic type names like System.Collections.Generic.List`1[[System.String, ...]]
                // will be split on the first comma, causing unbalanced [[ and ]] in the resulting substrings. This is for compatibility
                // with the existing incorrect logic in Full Framework.

                int pos = assemblyQualifiedTypeName.IndexOf(',');
                if (pos > 0 && pos < assemblyQualifiedTypeName.Length - 1)
                {
                    assemblyName = assemblyQualifiedTypeName.Substring(pos + 1).TrimStart();
                    string newTypeName = assemblyQualifiedTypeName.Substring(0, pos);
                    if (!string.Equals(newTypeName, serializedType.FullName, StringComparison.InvariantCulture))
                    {
                        typeName = newTypeName;
                    }
                    return;
                }
                base.BindToName(serializedType, out assemblyName, out typeName);
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                // We should never be using this binder during Deserialization
                throw new NotSupportedException($"{nameof(TypeNameManglingSerializationBinder)}.{nameof(BindToType)} should not be used during testing.");
            }
        }
    }
}
