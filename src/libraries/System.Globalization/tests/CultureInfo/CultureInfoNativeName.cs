// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoNativeName
    {
        public static IEnumerable<object[]> NativeName_TestData()
        {
            yield return new object[] { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.NativeName };

            // Android has its own ICU, which doesn't 100% map to UsingLimitedCultures
            if (PlatformDetection.IsNotUsingLimitedCultures || PlatformDetection.IsAndroid)
            {
                yield return new object[] { "en-US", "English (United States)" };
                yield return new object[] { "en-CA", "English (Canada)" };
            }
            else if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { "ar-SA", "\u0627\u0644\u0639\u0631\u0628\u064a\u0629\u0020\u0028\u0627\u0644\u0645\u0645\u0644\u0643\u0629\u0020\u0627\u0644\u0639\u0631\u0628\u064a\u0629\u0020\u0627\u0644\u0633\u0639\u0648\u062f\u064a\u0629\u0029" };
                yield return new object[] { "de-BE", "Deutsch (Belgien)" };
                yield return new object[] { "de-DE", "Deutsch (Deutschland)" };
                yield return new object[] { "en-CA", "Canadian English" };
                yield return new object[] { "en-US", "American English" };
                yield return new object[] { "nb-NO", "norsk bokm\u00e5l (Norge)" };
                yield return new object[] { "no", "norsk" };
                yield return new object[] { "no-NO", "norsk (Norge)" };
                yield return new object[] { "sr-Cyrl-RS", "\u0441\u0440\u043f\u0441\u043a\u0438\u0020\u0028\u045b\u0438\u0440\u0438\u043b\u0438\u0446\u0430\u002c\u0020\u0421\u0440\u0431\u0438\u0458\u0430\u0029" };
                yield return new object[] { "sr-Latn-RS", "srpski (latinica, Srbija)" };
                yield return new object[] { "sw-CD", "Kiswahili (Jamhuri ya Kidemokrasia ya Kongo)" };
                yield return new object[] { "zh-TW", "\u4e2d\u6587\uff08\u53f0\u7063\uff09" };
            }
            else
            {
                // Mobile / Browser ICU doesn't contain CultureInfo.NativeName
                yield return new object[] { "en-US", "en (US)" };
                yield return new object[] { "en-CA", "en (CA)" };
            }
        }

        [Theory]
        [MemberData(nameof(NativeName_TestData))]
        public void NativeName(string name, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(name);
            Assert.Equal(expected, myTestCulture.NativeName);
        }
    }
}
