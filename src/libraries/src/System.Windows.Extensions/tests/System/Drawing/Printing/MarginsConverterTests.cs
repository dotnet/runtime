// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.Drawing.Printing.Tests
{
    public class MarginsConverterTests
    {
        [Fact]
        public void CanConvertFrom()
        {
            MarginsConverter mc = new MarginsConverter();

            // try once with then once without context
            for (var context = new MyTypeDescriptorContext(); context != null; context = null)
            {
                Assert.True(mc.CanConvertFrom(context, typeof(string)));
                Assert.False(mc.CanConvertFrom(context, typeof(Guid)));
                Assert.False(mc.CanConvertFrom(context, typeof(object)));
                Assert.False(mc.CanConvertFrom(context, typeof(int)));
            }
        }

        [Fact]
        public void CanConvertTo()
        {
            MarginsConverter mc = new MarginsConverter();

            // try once with then once without context
            for (var context = new MyTypeDescriptorContext(); context != null; context = null)
            {
                Assert.True(mc.CanConvertTo(context, typeof(string)));
                Assert.False(mc.CanConvertTo(context, typeof(Guid)));
                Assert.False(mc.CanConvertTo(context, typeof(object)));
                Assert.False(mc.CanConvertTo(context, typeof(int)));
            }
        }

        [Fact]
        public void CreateInstance()
        {
            MarginsConverter mc = new MarginsConverter();
            MyTypeDescriptorContext context = new MyTypeDescriptorContext();

            IDictionary values = new Dictionary<string, int>();
            values.Add("Left", 1);
            values.Add("Right", 2);
            values.Add("Top", 3);
            Assert.Throws<ArgumentException>(() => mc.CreateInstance(context, values));
            values.Add("Bottom", 4);

            object result = mc.CreateInstance(context, values);
            Assert.NotNull(result);

            Assert.IsType<Margins>(result);
            Margins margins = result as Margins;
            Assert.Equal(1, margins.Left);
            Assert.Equal(2, margins.Right);
            Assert.Equal(3, margins.Top);
            Assert.Equal(4, margins.Bottom);
        }

        [Fact]
        public void GetCreateInstanceSupported()
        {
            MarginsConverter mc = new MarginsConverter();
            Assert.True(mc.GetCreateInstanceSupported(null));
            Assert.True(mc.GetCreateInstanceSupported(new MyTypeDescriptorContext()));
        }

        [Fact]
        public void ConvertFrom()
        {
            MarginsConverter mc = new MarginsConverter();
            CultureInfo culture = CultureInfo.InvariantCulture;

            // try once with then once without context
            for (var context = new MyTypeDescriptorContext(); context != null; context = null)
            {
                object result;
                Assert.Equal(',', culture.TextInfo.ListSeparator[0]);
                AssertExtensions.Throws<ArgumentException, Exception>(() => mc.ConvertFrom(context, culture, "1;2;3;4"));
                result = mc.ConvertFrom(context, culture, "1,2,3,4");
                Assert.IsType<Margins>(result);
                Margins margins = result as Margins;
                Assert.Equal(1, margins.Left);
                Assert.Equal(2, margins.Right);
                Assert.Equal(3, margins.Top);
                Assert.Equal(4, margins.Bottom);
            }
        }

        [Fact]
        public void ConvertFrom_Throws()
        {

            MarginsConverter mc = new MarginsConverter();
            CultureInfo culture = CultureInfo.InvariantCulture;

            // try once with then once without context
            for (var context = new MyTypeDescriptorContext(); context != null; context = null)
            {
                Assert.Throws<NotSupportedException>(() => mc.ConvertFrom(context, null, null));
                Assert.Throws<NotSupportedException>(() => mc.ConvertFrom(context, culture, null));
                Assert.Throws<NotSupportedException>(() => mc.ConvertFrom(context, culture, Guid.NewGuid()));
                AssertExtensions.Throws<ArgumentException, Exception>(() => mc.ConvertFrom(context, null, "wrong string format"));
                AssertExtensions.Throws<ArgumentException, Exception>(() => mc.ConvertFrom(context, culture, "wrong string format"));
            }
        }

        [Fact]
        public void ConvertTo()
        {
            MarginsConverter mc = new MarginsConverter();
            Guid guid = Guid.NewGuid();
            CultureInfo culture = CultureInfo.InvariantCulture;
            Margins margins = new Margins() { Left = 1, Right = 2, Top = 3, Bottom = 4 };

            // try once with then once without context
            for (var context = new MyTypeDescriptorContext(); context != null; context = null)
            {
                Assert.Equal("1;2;3;4", mc.ConvertTo(context, culture, "1;2;3;4", typeof(string)));

                object converted = mc.ConvertTo(context, culture, margins, typeof(string));
                Assert.IsType<string>(converted);
                Assert.Equal(',', culture.TextInfo.ListSeparator[0]);
                Assert.Equal("1, 2, 3, 4", converted);

                converted = mc.ConvertTo(context, culture, margins, typeof(InstanceDescriptor));
                Assert.IsType<InstanceDescriptor>(converted);
                Assert.Equal(new object[] { 1, 2, 3, 4 }, ((InstanceDescriptor)converted).Arguments);

                Assert.Throws<NotSupportedException>(() => mc.ConvertTo(context, culture, new object(), typeof(object)));
                Assert.Throws<NotSupportedException>(() => mc.ConvertTo(context, culture, 12, typeof(int)));
                Assert.Throws<NotSupportedException>(() => mc.ConvertTo(context, culture, guid, typeof(Guid)));
            }
        }

        private class MyTypeDescriptorContext : ITypeDescriptorContext
        {
            public IContainer Container => null;
            public object Instance { get { return null; } }
            public PropertyDescriptor PropertyDescriptor { get { return null; } }
            public bool OnComponentChanging() { return true; }
            public void OnComponentChanged() { }
            public object GetService(Type serviceType) { return null; }
        }
    }
}
