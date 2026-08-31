// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeConverterTests
    {
        public static TypeConverter s_converter = new TypeConverter();
        public static ITypeDescriptorContext s_context = new MyTypeDescriptorContext();
        private const int c_conversionInputValue = 1;
        private const string c_conversionResult = "1";

        [Fact]
        public static void CanConvertFrom_string()
        {
            Assert.False(s_converter.CanConvertFrom(typeof(string)));
        }

        [Fact]
        public static void CanConvertFrom_InstanceDescriptor()
        {
            Assert.True(s_converter.CanConvertFrom(typeof(InstanceDescriptor)));
        }

        [Fact]
        public static void CanConvertFrom_string_WithContext()
        {
            Assert.False(s_converter.CanConvertFrom(s_context, typeof(string)));
        }

        [Fact]
        public static void CanConvertTo_string()
        {
            Assert.True(s_converter.CanConvertTo(typeof(string)));
        }

        [Fact]
        public static void CanConvertTo_string_WithContext()
        {
            Assert.True(s_converter.CanConvertTo(s_context, typeof(string)));
        }

        [Fact]
        public static void ConvertFrom_Negative()
        {
            Assert.Throws<NotSupportedException>(() => s_converter.ConvertFrom("1"));
            Assert.Throws<NotSupportedException>(() => s_converter.ConvertFrom(null));
            Assert.Throws<NotSupportedException>(() => s_converter.ConvertFrom(s_context, null, "1"));
        }

        [Fact]
        public static void ConvertFromInvariantString()
        {
            Assert.Throws<NotSupportedException>(() => s_converter.ConvertFromInvariantString("1"));
        }

        [Fact]
        public static void ConvertFromString()
        {
            Assert.Throws<NotSupportedException>(() => s_converter.ConvertFromString("1"));
        }

        [Fact]
        public static void ConvertFrom_InstanceDescriptor()
        {
            using (new ThreadCultureChange("fr-FR"))
            {
                DateTime testDateAndTime = DateTime.UtcNow;
                ConstructorInfo ctor = typeof(DateTime).GetConstructor(new Type[]
                {
                    typeof(int), typeof(int), typeof(int), typeof(int),
                    typeof(int), typeof(int), typeof(int)
                });

                InstanceDescriptor descriptor = new InstanceDescriptor(ctor, new object[]
                {
                    testDateAndTime.Year, testDateAndTime.Month, testDateAndTime.Day, testDateAndTime.Hour,
                    testDateAndTime.Minute, testDateAndTime.Second, testDateAndTime.Millisecond
                });

                const string format = "dd MMM yyyy hh:mm";
                object o = s_converter.ConvertFrom(descriptor);
                Assert.Equal(testDateAndTime.ToString(format), ((DateTime)o).ToString(format));
            }
        }

        [Fact]
        public static void ConvertFrom_DateOnlyInstanceDescriptor()
        {
            using (new ThreadCultureChange("fr-FR"))
            {
                DateOnly testDateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
                ConstructorInfo ctor = typeof(DateOnly).GetConstructor(new Type[]
                {
                    typeof(int), typeof(int), typeof(int)
                });

                InstanceDescriptor descriptor = new InstanceDescriptor(ctor, new object[]
                {
                    testDateOnly.Year, testDateOnly.Month, testDateOnly.Day
                });

                const string format = "dd MMM yyyy";
                object o = s_converter.ConvertFrom(descriptor);
                Assert.Equal(testDateOnly.ToString(format), ((DateOnly)o).ToString(format));
            }
        }

        [Fact]
        public static void ConvertFrom_TimeOnlyInstanceDescriptor()
        {
            using (new ThreadCultureChange("fr-FR"))
            {
                TimeOnly testTimeOnly = TimeOnly.FromDateTime(DateTime.UtcNow);
                ConstructorInfo ctor = typeof(TimeOnly).GetConstructor(new Type[]
                {
                    typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)
                });

                InstanceDescriptor descriptor = new InstanceDescriptor(ctor, new object[]
                {
                    testTimeOnly.Hour, testTimeOnly.Minute, testTimeOnly.Second, testTimeOnly.Millisecond, testTimeOnly.Microsecond
                });

                const string format = "HH mm ss fff tt";
                object o = s_converter.ConvertFrom(descriptor);
                Assert.Equal(testTimeOnly.ToString(format), ((TimeOnly)o).ToString(format));
            }
        }

        [Fact]
        public static void TestConverters()
        {
            TypeConverter dateOnlyConverter = TypeDescriptor.GetConverter(typeof(DateOnly));
            DateOnly? date = dateOnlyConverter.ConvertFromString("1940-10-09") as DateOnly?;
            Assert.Equal(new DateOnly(1940, 10, 9), date);

            TypeConverter timeOnlyConverter = TypeDescriptor.GetConverter(typeof(TimeOnly));
            TimeOnly? time = timeOnlyConverter.ConvertFromString("20:30:50") as TimeOnly?;
            Assert.Equal(new TimeOnly(20, 30, 50), time);

            TypeConverter halfConverter = TypeDescriptor.GetConverter(typeof(Half));
            Half? half = halfConverter.ConvertFromString(((Half)(-1.2)).ToString()) as Half?;
            Assert.Equal((Half)(-1.2), half);

            TypeConverter Int128Converter = TypeDescriptor.GetConverter(typeof(Int128));
            Int128? int128 = Int128Converter.ConvertFromString("170141183460469231731687303715884105727") as Int128?;
            Assert.Equal(Int128.MaxValue, int128);

            TypeConverter UInt128Converter = TypeDescriptor.GetConverter(typeof(UInt128));
            UInt128? uint128 = UInt128Converter.ConvertFromString("340282366920938463463374607431768211455") as UInt128?;
            Assert.Equal(UInt128.MaxValue, uint128);
        }

        [Fact]
        public static void ConvertFromString_WithContext()
        {
            Assert.Throws<NotSupportedException>(
                () => s_converter.ConvertFromString(s_context, null, "1"));
        }

        [Fact]
        public static void ConvertTo_string()
        {
            object o = s_converter.ConvertTo(c_conversionInputValue, typeof(string));
            VerifyConversionToString(o);
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            using (new ThreadCultureChange("pl-PL"))
            {
                Assert.Throws<ArgumentNullException>(
                    () => s_converter.ConvertTo(s_context, null, c_conversionInputValue, null));

                Assert.Throws<NotSupportedException>(
                    () => s_converter.ConvertTo(s_context, null, c_conversionInputValue, typeof(int)));

                object o = s_converter.ConvertTo(s_context, null, c_conversionInputValue, typeof(string));
                VerifyConversionToString(o);

                o = s_converter.ConvertTo(
                    s_context, CultureInfo.CurrentCulture, c_conversionInputValue, typeof(string));
                VerifyConversionToString(o);

                o = s_converter.ConvertTo(
                    s_context, CultureInfo.InvariantCulture, c_conversionInputValue, typeof(string));
                VerifyConversionToString(o);

                string s = s_converter.ConvertTo(
                    s_context, CultureInfo.InvariantCulture, new FormattableClass(), typeof(string)) as string;
                Assert.NotNull(s);
                Assert.Equal(FormattableClass.Token, s);
            }
        }

        [Fact]
        public static void ConvertToInvariantString()
        {
            object o = s_converter.ConvertToInvariantString(c_conversionInputValue);
            VerifyConversionToString(o);
        }

        [Fact]
        public static void ConvertToString()
        {
            object o = s_converter.ConvertToString(c_conversionInputValue);
            VerifyConversionToString(o);
        }

        [Fact]
        public static void ConvertToString_WithContext()
        {
            object o = s_converter.ConvertToString(s_context, null, c_conversionInputValue);
            VerifyConversionToString(o);
        }

        [Fact]
        public static void ProtectedMethods()
        {
            new TypeConverterTests().RunProtectedMethods();
        }

        private void RunProtectedMethods()
        {
            var tc = new TypeConverterHelper();
            tc.RunProtectedMethods();
        }

        private static void VerifyConversionToString(object o)
        {
            Assert.True(o is string);
            Assert.Equal(c_conversionResult, (string)o);
        }

        private class TypeConverterHelper : TypeConverter
        {
            public void RunProtectedMethods()
            {
                var tc = new TypeConverter();

                Assert.Throws<NotSupportedException>(() => GetConvertFromException(null));
                Assert.Throws<NotSupportedException>(() => GetConvertFromException("1"));
                Assert.Throws<NotSupportedException>(() => GetConvertFromException(new BaseClass()));
                Assert.Throws<NotSupportedException>(() => GetConvertToException(null, typeof(int)));
                Assert.Throws<NotSupportedException>(() => GetConvertToException("1", typeof(int)));
                Assert.Throws<NotSupportedException>(() => GetConvertToException(new BaseClass(), typeof(BaseClass)));
            }
        }
    }
}
