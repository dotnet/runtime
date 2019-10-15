// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class ContentDispositionTest
    {
        private const string ValidDateGmt = "Sun, 17 May 2009 15:34:07 GMT";
        private const string ValidDateGmtOffset = "Sun, 17 May 2009 15:34:07 +0000";
        private const string ValidDateUnspecified = "Sun, 17 May 2009 15:34:07 -0000";
        private const string ValidDateTimeLocal = "Sun, 17 May 2009 15:34:07 -0800";
        private const string ValidDateTimeNotLocal = "Sun, 17 May 2009 15:34:07 -0200";
        private const string InvalidDate = "Sun, 32 Say 2009 25:15:15 7m-gte";
        private static readonly TimeSpan s_localTimeOffset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2009, 5, 17, 15, 34, 07, DateTimeKind.Local));

        [Fact]
        public static void DefaultCtor_ExpectedDefaultPropertyValues()
        {
            var cd = new ContentDisposition();
            Assert.Equal(DateTime.MinValue, cd.CreationDate);
            Assert.Equal("attachment", cd.DispositionType);
            Assert.Null(cd.FileName);
            Assert.False(cd.Inline);
            Assert.Equal(DateTime.MinValue, cd.ModificationDate);
            Assert.Empty(cd.Parameters);
            Assert.Equal(DateTime.MinValue, cd.ReadDate);
            Assert.Equal(-1, cd.Size);
            Assert.Equal("attachment", cd.ToString());
        }

        [Fact]
        public void ConstructorWithOtherPropertyValues_ShouldSetAppropriately()
        {
            var cd = new ContentDisposition("attachment;creation-date=\"" + ValidDateTimeLocal + "\";read-date=\"" +
                ValidDateTimeLocal + "\";modification-date=\"" +
                ValidDateTimeLocal + "\";filename=\"=?utf-8?B?dGVzdC50eHTnkIY=?=\";size=200");

            Assert.Equal("attachment", cd.DispositionType);
            Assert.False(cd.Inline);
            Assert.Equal(200, cd.Size);
            Assert.Equal("=?utf-8?B?dGVzdC50eHTnkIY=?=", cd.FileName);
            cd.FileName = "test.txt\x7406";
            Assert.Equal("test.txt\x7406", cd.FileName);
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null)]
        [InlineData(typeof(FormatException), "inline; creation-date=\"" + InvalidDate + "\";")]
        [InlineData(typeof(FormatException), "inline; size=\"notANumber\"")]
        public static void Ctor_InvalidThrows(Type exceptionType, string contentDisposition)
        {
            Assert.Throws(exceptionType, () => new ContentDisposition(contentDisposition));
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null)]
        [InlineData(typeof(ArgumentException), "")]
        public static void DispositionType_SetValue_InvalidThrows(Type exceptionType, string contentDisposition)
        {
            Assert.Throws(exceptionType, () => new ContentDisposition().DispositionType = contentDisposition);
        }

        [Fact]
        public static void Filename_Roundtrip()
        {
            var cd = new ContentDisposition();

            Assert.Null(cd.FileName);
            Assert.Empty(cd.Parameters);

            cd.FileName = "hello";
            Assert.Equal("hello", cd.FileName);
            Assert.Equal(1, cd.Parameters.Count);
            Assert.Equal("hello", cd.Parameters["filename"]);
            Assert.Equal("attachment; filename=hello", cd.ToString());

            cd.FileName = "world";
            Assert.Equal("world", cd.FileName);
            Assert.Equal(1, cd.Parameters.Count);
            Assert.Equal("world", cd.Parameters["filename"]);
            Assert.Equal("attachment; filename=world", cd.ToString());

            cd.FileName = null;
            Assert.Null(cd.FileName);
            Assert.Empty(cd.Parameters);

            cd.FileName = string.Empty;
            Assert.Null(cd.FileName);
            Assert.Empty(cd.Parameters);
        }

        [Fact]
        public static void Inline_Roundtrip()
        {
            var cd = new ContentDisposition();
            Assert.False(cd.Inline);

            cd.Inline = true;
            Assert.True(cd.Inline);

            cd.Inline = false;
            Assert.False(cd.Inline);

            Assert.Empty(cd.Parameters);
        }

        [Fact]
        public static void Dates_RoundtripWithoutImpactingOtherDates()
        {
            var cd = new ContentDisposition();

            Assert.Equal(DateTime.MinValue, cd.CreationDate);
            Assert.Equal(DateTime.MinValue, cd.ModificationDate);
            Assert.Equal(DateTime.MinValue, cd.ReadDate);
            Assert.Empty(cd.Parameters);

            DateTime dt1 = DateTime.Now;
            cd.CreationDate = dt1;
            Assert.Equal(1, cd.Parameters.Count);

            DateTime dt2 = DateTime.Now;
            cd.ModificationDate = dt2;
            Assert.Equal(2, cd.Parameters.Count);

            DateTime dt3 = DateTime.Now;
            cd.ReadDate = dt3;
            Assert.Equal(3, cd.Parameters.Count);

            Assert.Equal(dt1, cd.CreationDate);
            Assert.Equal(dt2, cd.ModificationDate);
            Assert.Equal(dt3, cd.ReadDate);

            Assert.Equal(3, cd.Parameters.Count);
        }

        [Fact]
        public void SetAndResetDateViaParameters_ShouldWorkCorrectly()
        {
            var cd = new ContentDisposition("inline");
            cd.Parameters["creation-date"] = ValidDateUnspecified;

            Assert.Equal(DateTimeKind.Unspecified, cd.CreationDate.Kind);
            Assert.Equal(ValidDateUnspecified, cd.Parameters["creation-date"]);

            cd.Parameters["creation-date"] = ValidDateTimeLocal;
            Assert.Equal(ValidDateTimeLocal, cd.Parameters["creation-date"]);
            Assert.Equal(ValidDateTimeLocal, cd.Parameters["Creation-Date"]);
        }

        [Fact]
        public static void DispositionType_Roundtrip()
        {
            var cd = new ContentDisposition();

            Assert.Equal("attachment", cd.DispositionType);
            Assert.Empty(cd.Parameters);

            cd.DispositionType = "hello";
            Assert.Equal("hello", cd.DispositionType);

            cd.DispositionType = "world";
            Assert.Equal("world", cd.DispositionType);

            Assert.Equal(0, cd.Parameters.Count);
        }

        [Fact]
        public static void Size_Roundtrip()
        {
            var cd = new ContentDisposition();

            Assert.Equal(-1, cd.Size);
            Assert.Empty(cd.Parameters);

            cd.Size = 42;
            Assert.Equal(42, cd.Size);
            Assert.Equal(1, cd.Parameters.Count);
        }

        [Fact]
        public static void ConstructorWithDateTimesBefore10AM_DateTimesAreValidForReUse()
        {
            ContentDisposition contentDisposition =
                new ContentDisposition("attachment; filename=\"image.jpg\"; size=461725;\tcreation-date=\"Sun, 15 Apr 2012 09:55:44 GMT\";\tmodification-date=\"Sun, 15 Apr 2012 06:30:20 GMT\"");

            var contentDisposition2 = new ContentDisposition();
            contentDisposition2.Parameters.Add("creation-date", contentDisposition.Parameters["creation-date"]);
            contentDisposition2.Parameters.Add("modification-date", contentDisposition.Parameters["modification-date"]);

            Assert.Equal(contentDisposition.CreationDate, contentDisposition2.CreationDate);
            Assert.Equal(contentDisposition.ModificationDate, contentDisposition2.ModificationDate);
        }

        [Fact]
        public static void UseDifferentCultureAndConstructorWithDateTimesBefore10AM_DateTimesAreValidForReUse()
        {
            ContentDisposition contentDisposition =
                new ContentDisposition("attachment; filename=\"image.jpg\"; size=461725;\tcreation-date=\"Sun, 15 Apr 2012 09:55:44 GMT\";\tmodification-date=\"Sun, 15 Apr 2012 06:30:20 GMT\"");

            CultureInfo origCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("zh-cn");
            try
            {
                ContentDisposition contentDisposition2 = new ContentDisposition();
                contentDisposition2.Parameters.Add("creation-date", contentDisposition.Parameters["creation-date"]);
                contentDisposition2.Parameters.Add("modification-date", contentDisposition.Parameters["modification-date"]);

                Assert.Equal(contentDisposition.CreationDate, contentDisposition2.CreationDate);
                Assert.Equal(contentDisposition.ModificationDate, contentDisposition2.ModificationDate);
            }
            finally
            {
                CultureInfo.CurrentCulture = origCulture;
            }
        }

        [Fact]
        public void SetDateViaConstructor_ShouldPersistToPropertyAndPersistToParametersCollection()
        {
            string disposition = "inline; creation-date=\"" + ValidDateGmt + "\";";
            string dispositionValue = "inline; creation-date=\"" + ValidDateGmtOffset + "\"";

            var cd = new ContentDisposition(disposition);

            Assert.Equal(ValidDateGmtOffset, cd.Parameters["creation-date"]);
            Assert.Equal(new DateTime(2009, 5, 17, 15, 34, 07, DateTimeKind.Local) + s_localTimeOffset, cd.CreationDate);

            Assert.Equal("inline", cd.DispositionType);
            Assert.Equal(dispositionValue, cd.ToString());
        }

        [Fact]
        public static void SetDateViaProperty_ShouldPersistToProprtyAndParametersCollection_AndRespectKindProperty()
        {
            DateTime date = new DateTime(2009, 5, 17, 15, 34, 07, DateTimeKind.Unspecified);

            var cd = new ContentDisposition("inline");
            cd.CreationDate = date;

            Assert.Equal(ValidDateUnspecified, cd.Parameters["creation-date"]);
            Assert.Equal(date, cd.CreationDate);
        }

        [Fact]
        public static void GetViaDateTimeProperty_WithUniversalTime_ShouldSetDateTimeKindAppropriately()
        {
            var cd = new ContentDisposition("inline");
            cd.Parameters["creation-date"] = ValidDateGmt;

            Assert.Equal(DateTimeKind.Local, cd.CreationDate.Kind);
            Assert.Equal(new DateTime(2009, 5, 17, 15, 34, 07, DateTimeKind.Local) + s_localTimeOffset, cd.CreationDate);
        }

        [Fact]
        public void GetViaDateTimeProperty_WithLocalTime_ShouldSetDateTimeKindAppropriately()
        {
            var cd = new ContentDisposition("inline");
            cd.Parameters["creation-date"] = ValidDateTimeLocal;

            Assert.Equal(DateTimeKind.Local, cd.CreationDate.Kind);
            Assert.Equal(cd.Parameters["creation-date"], ValidDateTimeLocal);
        }

        [Fact]
        public void GetViaDateTimeProperty_WithOtherTime_ShouldSetDateTimeKindAppropriately()
        {
            var cd = new ContentDisposition("inline");
            cd.Parameters["creation-date"] = ValidDateUnspecified;

            Assert.Equal(DateTimeKind.Unspecified, cd.CreationDate.Kind);
        }

        [Fact]
        public void GetViaDateTimeProperty_WithNonLocalTimeZone_ShouldWorkCorrectly()
        {
            var cd = new ContentDisposition("inline");
            cd.Parameters["creation-date"] = ValidDateTimeNotLocal;
            DateTime result = cd.CreationDate;

            Assert.Equal(ValidDateTimeNotLocal, cd.Parameters["creation-date"]);
            Assert.Equal(DateTimeKind.Local, result.Kind);

            // be sure that the local offset isn't the same as the offset we're testing
            // so that this comparison will be valid
            if (s_localTimeOffset != new TimeSpan(-2, 0, 0))
            {
                Assert.NotEqual(ValidDateTimeNotLocal, result.ToString("ddd, dd MMM yyyy H:mm:ss"));
            }
        }

        [Fact]
        public void SetCreateDateViaParameters_WithInvalidDate_ShouldThrow()
        {
            var cd = new ContentDisposition("inline");
            Assert.Throws<FormatException>(() =>
            {
                cd.Parameters["creation-date"] = InvalidDate;
                //string date = cd.Parameters["creation-date"];
            });
        }

        [Fact]
        public void GetCustomerParameterThatIsNotSet_ShouldReturnNull()
        {
            var cd = new ContentDisposition("inline");

            Assert.Null(cd.Parameters["doesnotexist"]);
            Assert.Equal("inline", cd.DispositionType);
            Assert.True(cd.Inline);
        }

        [Fact]
        public void SetDispositionViaConstructor_ShouldSetCorrectly_AndRespectCustomValues()
        {
            var cd = new ContentDisposition("inline; creation-date=\"" + ValidDateGmtOffset + "\"; X-Test=\"value\"");

            Assert.Equal("inline", cd.DispositionType);
            Assert.Equal(ValidDateGmtOffset, cd.Parameters["creation-date"]);
            Assert.Equal(new DateTime(2009, 5, 17, 15, 34, 07, DateTimeKind.Local) + s_localTimeOffset, cd.CreationDate);
        }

        [Fact]
        public void CultureSensitiveSetDateViaProperty_ShouldPersistToProprtyAndParametersCollectionAndRespectKindProperty()
        {
            CultureInfo origCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("zh");

                var cd = new ContentDisposition("inline");

                var date = new DateTime(2011, 6, 8, 15, 34, 07, DateTimeKind.Unspecified);
                cd.CreationDate = date;

                Assert.Equal("Wed, 08 Jun 2011 15:34:07 -0000", cd.Parameters["creation-date"]);
                Assert.Equal(date, cd.CreationDate);
                Assert.Equal("inline; creation-date=\"Wed, 08 Jun 2011 15:34:07 -0000\"", cd.ToString());
            }
            finally
            {
                CultureInfo.CurrentCulture = origCulture;
            }
        }
    }
}
