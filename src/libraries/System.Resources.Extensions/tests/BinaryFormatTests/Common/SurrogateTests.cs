// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Resources.Extensions.Tests.Common.TestTypes;

namespace System.Resources.Extensions.Tests.Common;

public abstract class SurrogateTests<T> : SerializationTest<T> where T : ISerializer
{
    [Fact]
    public void SerializePointWithSurrogate_NonRefChange()
    {
        Point point = new(42, 43);

        // Surrogates cannot change the equality of types that they change. BinaryFormattedObject would allow this
        // unless you wrap the selector by calling FormatterServices.GetSurrogateForCyclicalReference. Ours always
        // allows value types to change as they are always applied as a fixup.
        SurrogateSelector selector = CreateSurrogateSelector<Point>(new PointSerializationSurrogate(refUnbox: false));

        Stream stream = Serialize(point);
        Deserialize(stream, surrogateSelector: selector);
    }

    [Fact]
    public void SerializePointWithSurrogate_RefChange()
    {
        Point point = new(42, 43);

        Stream stream = Serialize(point);

        // Surrogates can change the equality of the structs they are passed
        // if they unbox as ref (using Unsafe).
        SurrogateSelector selector = CreateSurrogateSelector<Point>(new PointSerializationSurrogate(refUnbox: true));

        Point deserialized = (Point)Deserialize(stream, surrogateSelector: selector);
        Assert.Equal(point, deserialized);
    }

    public class PointSerializationSurrogate : ISerializationSurrogate
    {
        private readonly bool _refUnbox;

        public PointSerializationSurrogate(bool refUnbox) => _refUnbox = refUnbox;

        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) =>
            throw new NotImplementedException();

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector? selector)
        {
            if (_refUnbox)
            {
                ref Point pointRef = ref Unsafe.Unbox<Point>(obj);
                pointRef.X = info.GetInt32("x");
                pointRef.Y = info.GetInt32("y");
                return obj;
            }

            Point point = (Point)obj;
            point.X = info.GetInt32("x");
            point.Y = info.GetInt32("y");
            return point;
        }
    }

    [Fact]
    public void SerializePointWithNullSurrogate()
    {
        Point point = new(42, 43);

        Stream stream = Serialize(point);

        // Not sure why one would want to do this, but returning null will skip setting the value back.
        SurrogateSelector selector = CreateSurrogateSelector<Point>(new NullSurrogate());
        Point deserialized = (Point)Deserialize(stream, surrogateSelector: selector);

        Assert.Equal(0, deserialized.X);
        Assert.Equal(0, deserialized.Y);
    }

    public class NullSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) =>
            throw new NotImplementedException();

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector? selector) =>
            null!;
    }

    public class EqualsButDifferentSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) =>
            throw new NotImplementedException();

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector? selector)
        {
            return null!;
        }
    }

    [Fact]
    public void SerializeNonSerializableTypeWithSurrogate()
    {
        NonSerializablePair<int, string> pair = new() { Value1 = 1, Value2 = "2" };
        Assert.False(pair.GetType().IsSerializable);
        Assert.Throws<SerializationException>(() => Serialize(pair));

        SurrogateSelector selector = CreateSurrogateSelector<NonSerializablePair<int, string>>(new NonSerializablePairSurrogate());

        var deserialized = RoundTrip(pair, surrogateSelector: selector);
        Assert.NotSame(pair, deserialized);
        Assert.Equal(pair.Value1, deserialized.Value1);
        Assert.Equal(pair.Value2, deserialized.Value2);
    }
}
