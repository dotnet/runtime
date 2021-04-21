// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;


namespace System.Xml
{
    internal abstract class ArrayHelper<TArgument, TArray>
    {
        public TArray[] ReadArray(XmlDictionaryReader reader, TArgument localName, TArgument namespaceUri, int maxArrayLength)
        {
            TArray[][]? arrays = null;
            TArray[]? array = null;
            int arrayCount = 0;
            int totalRead = 0;
            int count;
            if (reader.TryGetArrayLength(out count))
            {
                if (count > XmlDictionaryReader.MaxInitialArrayLength)
                    count = XmlDictionaryReader.MaxInitialArrayLength;
            }
            else
            {
                count = 32;
            }
            while (true)
            {
                array = new TArray[count];
                int read = 0;
                while (read < array.Length)
                {
                    int actual = ReadArray(reader, localName, namespaceUri, array, read, array.Length - read);
                    if (actual == 0)
                        break;
                    read += actual;
                }
                totalRead += read;
                if (read < array.Length || reader.NodeType == XmlNodeType.EndElement)
                    break;
                if (arrays == null)
                    arrays = new TArray[32][];
                arrays[arrayCount++] = array;
                count = count * 2;
            }
            if (totalRead != array.Length || arrayCount > 0)
            {
                TArray[] newArray = new TArray[totalRead];
                int offset = 0;
                for (int i = 0; i < arrayCount; i++)
                {
                    Array.Copy(arrays![i], 0, newArray, offset, arrays[i].Length);
                    offset += arrays[i].Length;
                }
                Array.Copy(array, 0, newArray, offset, totalRead - offset);
                array = newArray;
            }
            return array;
        }

        public void WriteArray(XmlDictionaryWriter writer, string prefix, TArgument localName, TArgument namespaceUri, XmlDictionaryReader reader)
        {
            int count;
            if (reader.TryGetArrayLength(out count))
                count = Math.Min(count, 256);
            else
                count = 256;
            TArray[] array = new TArray[count];
            while (true)
            {
                int actual = ReadArray(reader, localName, namespaceUri, array, 0, array.Length);
                if (actual == 0)
                    break;
                WriteArray(writer, prefix, localName, namespaceUri, array, 0, actual);
            }
        }

        protected abstract int ReadArray(XmlDictionaryReader reader, TArgument localName, TArgument namespaceUri, TArray[] array, int offset, int count);
        protected abstract void WriteArray(XmlDictionaryWriter writer, string prefix, TArgument localName, TArgument namespaceUri, TArray[] array, int offset, int count);
    }

    // Supported array types
    // bool
    // Int16
    // Int32
    // Int64
    // Float
    // Double
    // Decimal
    // DateTime
    // Guid
    // TimeSpan

    // Int8 is not supported since sbyte[] is non-CLS compliant, and uncommon
    // UniqueId is not supported since elements may be variable size strings

    internal sealed class BooleanArrayHelperWithString : ArrayHelper<string, bool>
    {
        public static readonly BooleanArrayHelperWithString Instance = new BooleanArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, bool[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, bool[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class BooleanArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, bool>
    {
        public static readonly BooleanArrayHelperWithDictionaryString Instance = new BooleanArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class Int16ArrayHelperWithString : ArrayHelper<string, short>
    {
        public static readonly Int16ArrayHelperWithString Instance = new Int16ArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, short[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, short[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class Int16ArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, short>
    {
        public static readonly Int16ArrayHelperWithDictionaryString Instance = new Int16ArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class Int32ArrayHelperWithString : ArrayHelper<string, int>
    {
        public static readonly Int32ArrayHelperWithString Instance = new Int32ArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, int[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, int[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class Int32ArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, int>
    {
        public static readonly Int32ArrayHelperWithDictionaryString Instance = new Int32ArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class Int64ArrayHelperWithString : ArrayHelper<string, long>
    {
        public static readonly Int64ArrayHelperWithString Instance = new Int64ArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, long[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, long[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class Int64ArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, long>
    {
        public static readonly Int64ArrayHelperWithDictionaryString Instance = new Int64ArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class SingleArrayHelperWithString : ArrayHelper<string, float>
    {
        public static readonly SingleArrayHelperWithString Instance = new SingleArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, float[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, float[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class SingleArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, float>
    {
        public static readonly SingleArrayHelperWithDictionaryString Instance = new SingleArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class DoubleArrayHelperWithString : ArrayHelper<string, double>
    {
        public static readonly DoubleArrayHelperWithString Instance = new DoubleArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, double[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, double[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class DoubleArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, double>
    {
        public static readonly DoubleArrayHelperWithDictionaryString Instance = new DoubleArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class DecimalArrayHelperWithString : ArrayHelper<string, decimal>
    {
        public static readonly DecimalArrayHelperWithString Instance = new DecimalArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, decimal[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, decimal[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class DecimalArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, decimal>
    {
        public static readonly DecimalArrayHelperWithDictionaryString Instance = new DecimalArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class DateTimeArrayHelperWithString : ArrayHelper<string, DateTime>
    {
        public static readonly DateTimeArrayHelperWithString Instance = new DateTimeArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, DateTime[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, DateTime[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class DateTimeArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, DateTime>
    {
        public static readonly DateTimeArrayHelperWithDictionaryString Instance = new DateTimeArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class GuidArrayHelperWithString : ArrayHelper<string, Guid>
    {
        public static readonly GuidArrayHelperWithString Instance = new GuidArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, Guid[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, Guid[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class GuidArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, Guid>
    {
        public static readonly GuidArrayHelperWithDictionaryString Instance = new GuidArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class TimeSpanArrayHelperWithString : ArrayHelper<string, TimeSpan>
    {
        public static readonly TimeSpanArrayHelperWithString Instance = new TimeSpanArrayHelperWithString();

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }

    internal sealed class TimeSpanArrayHelperWithDictionaryString : ArrayHelper<XmlDictionaryString, TimeSpan>
    {
        public static readonly TimeSpanArrayHelperWithDictionaryString Instance = new TimeSpanArrayHelperWithDictionaryString();

        protected override int ReadArray(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
        {
            return reader.ReadArray(localName, namespaceUri, array, offset, count);
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
        {
            writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
        }
    }
}
