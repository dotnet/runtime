// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
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
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
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
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("pl-PL");

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
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
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
