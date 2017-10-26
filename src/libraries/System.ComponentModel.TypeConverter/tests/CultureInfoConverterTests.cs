// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information. 

//
// System.ComponentModel.CultureInfoConverter test cases
//
// Authors:
// 	Gert Driesen (drieseng@users.sourceforge.net)
//
// (c) 2008 Gert Driesen
//

using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class CultureInfoConverterTest
    {
        private CultureInfoConverter converter;

        public CultureInfoConverterTest()
        {
            converter = new CultureInfoConverter();
        }

        [Fact]
        public void CanConvertFrom()
        {
            Assert.True(converter.CanConvertFrom(typeof(string)));
            Assert.False(converter.CanConvertFrom(typeof(CultureInfo)));
            Assert.False(converter.CanConvertFrom(typeof(object)));
            Assert.False(converter.CanConvertFrom(typeof(int)));
        }

        [Fact]
        public void CanConvertTo()
        {
            Assert.True(converter.CanConvertTo(typeof(string)));
            Assert.False(converter.CanConvertTo(typeof(object)));
            Assert.False(converter.CanConvertTo(typeof(CultureInfo)));
            Assert.False(converter.CanConvertTo(typeof(int)));
            Assert.True(converter.CanConvertTo(typeof(InstanceDescriptor)));
        }

        [Fact]
        public void ConvertFrom_String()
        {
            CultureInfo c;

            c = (CultureInfo)converter.ConvertFrom(null, CultureInfo.InvariantCulture,
                String.Empty);
            Assert.Equal(CultureInfo.InvariantCulture, c);

            c = (CultureInfo)converter.ConvertFrom(null, CultureInfo.InvariantCulture,
                "nl-BE");
            Assert.Equal(new CultureInfo("nl-BE"), c);

            try
            {
                // Linux can create such cultures
                var cul = new CultureInfo("Dutch (Bel");
            }
            catch (CultureNotFoundException)
            {
                // if we cannot create the cultures we should get exception from the Converter too
                AssertExtensions.Throws<ArgumentException>(null, () => c = (CultureInfo)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "Dutch (Bel"));
                AssertExtensions.Throws<ArgumentException>(null, () => c = (CultureInfo)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "duTcH (Bel"));
            }

            c = (CultureInfo)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "(Default)");
            Assert.Equal(CultureInfo.InvariantCulture, c);
        }

        [Fact]
        public void ConvertFrom_String_IncompleteName()
        {
            converter.ConvertFrom(null, CultureInfo.InvariantCulture,
                "nl-B");
        }

        [Fact]
        public void ConvertFrom_String_InvalidCulture()
        {
            ArgumentException ex;

            try
            {
                // Linux can create such cultures
                var cul = new CultureInfo("(default)");
            }
            catch (CultureNotFoundException)
            {
                // if we cannot create the cultures we should get exception from the Converter too
                ex = AssertExtensions.Throws<ArgumentException>(null, () => converter.ConvertFrom(null, CultureInfo.InvariantCulture, "(default)"));
                // The (default) culture cannot be converted to
                // a CultureInfo object on this computer
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
                {
                    Assert.NotNull(ex.Message);
                    Assert.True(ex.Message.IndexOf(typeof(CultureInfo).Name) != -1);
                    Assert.True(ex.Message.IndexOf("(default)") != -1);
                    Assert.Null(ex.ParamName);
                }
            }

            try
            {
                // Linux can create such cultures
                var cul = new CultureInfo(" ");
            }
            catch (CultureNotFoundException)
            {
                ex = AssertExtensions.Throws<ArgumentException>(null, () => converter.ConvertFrom(null, CultureInfo.InvariantCulture, " "));
                // The   culture cannot be converted to
                // a CultureInfo object on this computer
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
                {
                    Assert.NotNull(ex.Message);
                    Assert.True(ex.Message.IndexOf(typeof(CultureInfo).Name) != -1);
                    Assert.True(ex.Message.IndexOf("   ") != -1);
                    Assert.Null(ex.ParamName);
                }
            }

            try
            {
                // Linux can create such cultures
                var cul = new CultureInfo("\r\n");
            }
            catch (CultureNotFoundException)
            {
                ex = AssertExtensions.Throws<ArgumentException>(null, () => converter.ConvertFrom(null, CultureInfo.InvariantCulture, "\r\n"));
                // The \r\n culture cannot be converted to
                // a CultureInfo object on this computer
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
                {
                    Assert.NotNull(ex.Message);
                    Assert.True(ex.Message.IndexOf(typeof(CultureInfo).Name) != -1);
                    Assert.True(ex.Message.IndexOf("\r\n") != -1);
                    Assert.Null(ex.ParamName);
                }
            }
        }

        [Fact]
        public void ConvertFrom_Value_Null()
        {
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, (string)null));
            // CultureInfoConverter cannot convert from (null)
            Assert.Equal(typeof(NotSupportedException), ex.GetType());
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.True(ex.Message.IndexOf(typeof(CultureInfoConverter).Name) != -1);
            Assert.True(ex.Message.IndexOf("(null)") != -1);
        }

        [Fact]
        public void ConvertToString()
        {
            Assert.Equal("nl-BE", converter.ConvertToString(null, CultureInfo.InvariantCulture, new MyCultureInfo()));
            Assert.Equal("(Default)", converter.ConvertToString(null, CultureInfo.InvariantCulture, null));
            Assert.Equal("(Default)", converter.ConvertToString(null, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture));
            Assert.Equal("nl-BE", converter.ConvertToString(null, CultureInfo.InvariantCulture, new CultureInfo("nl-BE")));
        }

        [Serializable]
        private sealed class MyCultureInfo : CultureInfo
        {
            internal MyCultureInfo() : base("nl-BE")
            {
            }

            public override string DisplayName
            {
                get { return "display"; }
            }

            public override string EnglishName
            {
                get { return "english"; }
            }
        }

        [Fact]
        public void GetCultureName()
        {
            CustomCultureInfoConverter custom_converter = new CustomCultureInfoConverter();

            CultureInfo fr_culture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal(fr_culture.Name, custom_converter.GetCultureName(fr_culture));

            CultureInfo es_culture = CultureInfo.GetCultureInfo("es-MX");
            Assert.Equal(es_culture.Name, custom_converter.GetCultureName(es_culture));
        }

        private class CustomCultureInfoConverter : CultureInfoConverter
        {
            public new string GetCultureName(CultureInfo culture)
            {
                return base.GetCultureName(culture);
            }
        }
    }
}
