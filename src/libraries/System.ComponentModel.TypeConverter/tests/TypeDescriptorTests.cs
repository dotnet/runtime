// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeDescriptorTests
    {
        [Fact]
        public static void GetConverter()
        {
            foreach (Tuple<Type, Type> pair in s_typesWithConverters)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(pair.Item1);
                Assert.NotNull(converter);
                Assert.Equal(pair.Item2, converter.GetType());
                Assert.True(converter.CanConvertTo(typeof(string)));
            }
        }

        [Fact]
        public static void GetConverter_null()
        {
            Assert.Throws<ArgumentNullException>(() => TypeDescriptor.GetConverter(null));
        }

        [Fact]
        public static void GetConverter_NotAvailable()
        {
            Assert.Throws<MissingMethodException>(
                 () => TypeDescriptor.GetConverter(typeof(ClassWithInvalidConverter)));
            // GetConverter should throw MissingMethodException because parameterless constructor is missing in the InvalidConverter class.
        }

        private static Tuple<Type, Type>[] s_typesWithConverters =
        {
            new Tuple<Type, Type> (typeof(bool), typeof(BooleanConverter)),
            new Tuple<Type, Type> (typeof(byte), typeof(ByteConverter)),
            new Tuple<Type, Type> (typeof(SByte), typeof(SByteConverter)),
            new Tuple<Type, Type> (typeof(char), typeof(CharConverter)),
            new Tuple<Type, Type> (typeof(double), typeof(DoubleConverter)),
            new Tuple<Type, Type> (typeof(string), typeof(StringConverter)),
            new Tuple<Type, Type> (typeof(short), typeof(Int16Converter)),
            new Tuple<Type, Type> (typeof(int), typeof(Int32Converter)),
            new Tuple<Type, Type> (typeof(long), typeof(Int64Converter)),
            new Tuple<Type, Type> (typeof(float), typeof(SingleConverter)),
            new Tuple<Type, Type> (typeof(UInt16), typeof(UInt16Converter)),
            new Tuple<Type, Type> (typeof(UInt32), typeof(UInt32Converter)),
            new Tuple<Type, Type> (typeof(UInt64), typeof(UInt64Converter)),
            new Tuple<Type, Type> (typeof(object), typeof(TypeConverter)),
            new Tuple<Type, Type> (typeof(void), typeof(TypeConverter)),
            new Tuple<Type, Type> (typeof(DateTime), typeof(DateTimeConverter)),
            new Tuple<Type, Type> (typeof(DateTimeOffset), typeof(DateTimeOffsetConverter)),
            new Tuple<Type, Type> (typeof(Decimal), typeof(DecimalConverter)),
            new Tuple<Type, Type> (typeof(TimeSpan), typeof(TimeSpanConverter)),
            new Tuple<Type, Type> (typeof(Guid), typeof(GuidConverter)),
            new Tuple<Type, Type> (typeof(Array), typeof(ArrayConverter)),
            new Tuple<Type, Type> (typeof(ICollection), typeof(CollectionConverter)),
            new Tuple<Type, Type> (typeof(Enum), typeof(EnumConverter)),
            new Tuple<Type, Type> (typeof(SomeEnum), typeof(EnumConverter)),
            new Tuple<Type, Type> (typeof(SomeValueType?), typeof(NullableConverter)),
            new Tuple<Type, Type> (typeof(int?), typeof(NullableConverter)),
            new Tuple<Type, Type> (typeof(ClassWithNoConverter), typeof(TypeConverter)),
            new Tuple<Type, Type> (typeof(BaseClass), typeof(BaseClassConverter)),
            new Tuple<Type, Type> (typeof(DerivedClass), typeof(DerivedClassConverter)),
            new Tuple<Type, Type> (typeof(IBase), typeof(IBaseConverter)),
            new Tuple<Type, Type> (typeof(IDerived), typeof(IBaseConverter)),
            new Tuple<Type, Type> (typeof(ClassIBase), typeof(IBaseConverter)),
            new Tuple<Type, Type> (typeof(ClassIDerived), typeof(IBaseConverter))
        };
    }
}
