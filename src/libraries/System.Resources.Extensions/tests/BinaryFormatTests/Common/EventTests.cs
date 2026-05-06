// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common;

public abstract class EventTests<T> : SerializationTest<T> where T : ISerializer
{
    [Fact]
    public void SerializationEvents_FireAsExpected()
    {
        IncrementCountsDuringRoundtrip obj = new (null);

        Assert.Equal(0, obj.IncrementedDuringOnSerializingMethod);
        Assert.Equal(0, obj.IncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);

        Stream stream = Serialize(obj);

        Assert.Equal(1, obj.IncrementedDuringOnSerializingMethod);
        Assert.Equal(1, obj.IncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);

        var result = (IncrementCountsDuringRoundtrip)Deserialize(stream);

        Assert.Equal(1, obj.IncrementedDuringOnSerializingMethod);
        Assert.Equal(1, obj.IncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);

        Assert.Equal(1, result.IncrementedDuringOnSerializingMethod);
        Assert.Equal(0, result.IncrementedDuringOnSerializedMethod);
        Assert.Equal(1, result.IncrementedDuringOnDeserializingMethod);
        Assert.Equal(1, result.IncrementedDuringOnDeserializedMethod);
    }

    [Fact]
    public void SerializationEvents_DerivedTypeWithEvents_FireAsExpected()
    {
        DerivedIncrementCountsDuringRoundtrip obj = new(null);

        Assert.Equal(0, obj.IncrementedDuringOnSerializingMethod);
        Assert.Equal(0, obj.IncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);
        Assert.Equal(0, obj._derivedIncrementedDuringOnSerializingMethod);
        Assert.Equal(0, obj._derivedIncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj._derivedIncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj._derivedIncrementedDuringOnDeserializedMethod);

        Stream stream = Serialize(obj);

        Assert.Equal(1, obj.IncrementedDuringOnSerializingMethod);
        Assert.Equal(1, obj.IncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);
        Assert.Equal(1, obj._derivedIncrementedDuringOnSerializingMethod);
        Assert.Equal(1, obj._derivedIncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj._derivedIncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj._derivedIncrementedDuringOnDeserializedMethod);

        var result = (DerivedIncrementCountsDuringRoundtrip)Deserialize(stream);

        Assert.Equal(1, obj.IncrementedDuringOnSerializingMethod);
        Assert.Equal(1, obj.IncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);
        Assert.Equal(1, obj._derivedIncrementedDuringOnSerializingMethod);
        Assert.Equal(1, obj._derivedIncrementedDuringOnSerializedMethod);
        Assert.Equal(0, obj._derivedIncrementedDuringOnDeserializingMethod);
        Assert.Equal(0, obj._derivedIncrementedDuringOnDeserializedMethod);

        Assert.Equal(1, result.IncrementedDuringOnSerializingMethod);
        Assert.Equal(0, result.IncrementedDuringOnSerializedMethod);
        Assert.Equal(1, result.IncrementedDuringOnDeserializingMethod);
        Assert.Equal(1, result.IncrementedDuringOnDeserializedMethod);
        Assert.Equal(1, result._derivedIncrementedDuringOnSerializingMethod);
        Assert.Equal(0, result._derivedIncrementedDuringOnSerializedMethod);
        Assert.Equal(1, result._derivedIncrementedDuringOnDeserializingMethod);
        Assert.Equal(1, result._derivedIncrementedDuringOnDeserializedMethod);
    }

    [Serializable]
    public class IncrementCountsDuringRoundtrip
    {
        public int IncrementedDuringOnSerializingMethod;
        public int IncrementedDuringOnSerializedMethod;
        [NonSerialized] public int IncrementedDuringOnDeserializingMethod;
        public int IncrementedDuringOnDeserializedMethod;

        // non-default ctor so that we can observe changes from OnDeserializing
        public IncrementCountsDuringRoundtrip(string? ignored) { _ = ignored; }

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context) => IncrementedDuringOnSerializingMethod++;

        [OnSerialized]
        private void OnSerializedMethod(StreamingContext context) => IncrementedDuringOnSerializedMethod++;

        [OnDeserializing]
        private void OnDeserializingMethod(StreamingContext context) => IncrementedDuringOnDeserializingMethod++;

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context) => IncrementedDuringOnDeserializedMethod++;
    }

    [Serializable]
    public sealed class DerivedIncrementCountsDuringRoundtrip : IncrementCountsDuringRoundtrip
    {
        internal int _derivedIncrementedDuringOnSerializingMethod;
        internal int _derivedIncrementedDuringOnSerializedMethod;
        [NonSerialized] internal int _derivedIncrementedDuringOnDeserializingMethod;
        internal int _derivedIncrementedDuringOnDeserializedMethod;

        public DerivedIncrementCountsDuringRoundtrip(string? ignored) : base(ignored) { }

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context) => _derivedIncrementedDuringOnSerializingMethod++;

        [OnSerialized]
        private void OnSerializedMethod(StreamingContext context) => _derivedIncrementedDuringOnSerializedMethod++;

        [OnDeserializing]
        private void OnDeserializingMethod(StreamingContext context) => _derivedIncrementedDuringOnDeserializingMethod++;

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context) => _derivedIncrementedDuringOnDeserializedMethod++;
    }
}
