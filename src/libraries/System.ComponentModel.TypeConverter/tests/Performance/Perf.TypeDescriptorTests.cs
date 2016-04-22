// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.ComponentModel.Tests
{
    public class Perf_TypeDescriptorTests
    {
        [Benchmark]
        [InlineData(typeof(bool), typeof(BooleanConverter))]
        [InlineData(typeof(byte), typeof(ByteConverter))]
        [InlineData(typeof(SByte), typeof(SByteConverter))]
        [InlineData(typeof(char), typeof(CharConverter))]
        [InlineData(typeof(double), typeof(DoubleConverter))]
        [InlineData(typeof(string), typeof(StringConverter))]
        [InlineData(typeof(short), typeof(Int16Converter))]
        [InlineData(typeof(int), typeof(Int32Converter))]
        [InlineData(typeof(long), typeof(Int64Converter))]
        [InlineData(typeof(float), typeof(SingleConverter))]
        [InlineData(typeof(UInt16), typeof(UInt16Converter))]
        [InlineData(typeof(UInt32), typeof(UInt32Converter))]
        [InlineData(typeof(UInt64), typeof(UInt64Converter))]
        [InlineData(typeof(object), typeof(TypeConverter))]
        [InlineData(typeof(void), typeof(TypeConverter))]
        [InlineData(typeof(DateTime), typeof(DateTimeConverter))]
        [InlineData(typeof(DateTimeOffset), typeof(DateTimeOffsetConverter))]
        [InlineData(typeof(Decimal), typeof(DecimalConverter))]
        [InlineData(typeof(TimeSpan), typeof(TimeSpanConverter))]
        [InlineData(typeof(Guid), typeof(GuidConverter))]
        [InlineData(typeof(Array), typeof(ArrayConverter))]
        [InlineData(typeof(ICollection), typeof(CollectionConverter))]
        [InlineData(typeof(Enum), typeof(EnumConverter))]
        [InlineData(typeof(SomeEnum), typeof(EnumConverter))]
        [InlineData(typeof(SomeValueType?), typeof(NullableConverter))]
        [InlineData(typeof(int?), typeof(NullableConverter))]
        [InlineData(typeof(ClassWithNoConverter), typeof(TypeConverter))]
        [InlineData(typeof(BaseClass), typeof(BaseClassConverter))]
        [InlineData(typeof(DerivedClass), typeof(DerivedClassConverter))]
        [InlineData(typeof(IBase), typeof(IBaseConverter))]
        [InlineData(typeof(IDerived), typeof(IBaseConverter))]
        [InlineData(typeof(ClassIBase), typeof(IBaseConverter))]
        [InlineData(typeof(ClassIDerived), typeof(IBaseConverter))]
        [InlineData(typeof(Uri), typeof(UriTypeConverter))]
        public static void GetConverter(Type typeToConvert, Type expectedConverter)
        {
            const int innerIterations = 100;
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(typeToConvert);
                        Assert.NotNull(converter);
                        Assert.Equal(expectedConverter, converter.GetType());
                        Assert.True(converter.CanConvertTo(typeof(string)));
                    }
        }
    }
}
