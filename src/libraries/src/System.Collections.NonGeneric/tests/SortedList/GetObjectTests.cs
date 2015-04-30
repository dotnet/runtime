// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class GetObjectTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }


        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void Test01()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            SortedList sl2 = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            string s1 = null;
            string s2 = null;
            string s3 = null;

            int i = 0;
            //
            // 	Constructor: Create SortedList using this as IComparer and default settings.
            //
            sl2 = new SortedList(this);

            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            //  Verify that the SortedList is empty.
            Assert.Equal(0, sl2.Count);

            //   Testcase: No_Such_Get
            Assert.Null((string)sl2["No_Such_Get"]);

            //   Testcase: add few key-val pairs
            for (i = 0; i < 100; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg.Length = 0;
                sblMsg.Append("val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                sl2.Add(s1, s2);
            }
            //
            //   Testcase: test Get (Object)
            //
            for (i = 0; i < sl2.Count; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                s3 = (string)sl2[s1];

                sblMsg.Length = 0;
                sblMsg.Append("val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                Assert.True(s3.Equals(s2));
            }

            //
            // Remove a key and then check
            //
            sblMsg.Length = 0;
            sblMsg.Append("key_50");
            s1 = sblMsg.ToString();

            sl2.Remove(s1); //removes "Key_50"
            s3 = (string)sl2[s1];
            Assert.Null(s3);

            //
            // Insert some items and change the current culture and verify we can still get every item
            //
            sl2 = new SortedList();

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            var somePopularCultureNames = new string[] {
                    "cs-CZ","da-DK","de-DE","el-GR","en-US",
                    "es-ES","fi-FI","fr-FR","hu-HU","it-IT",
                    "ja-JP","ko-KR","nb-NO","nl-NL","pl-PL",
                    "pt-BR","pt-PT","ru-RU","sv-SE","tr-TR",
                    "zh-CN","zh-HK","zh-TW" };

            CultureInfo[] installedCultures = new CultureInfo[somePopularCultureNames.Length];
            string[] cultureDisplayNames = new string[installedCultures.Length];
            int uniqueDisplayNameCount = 0;
            foreach (string cultureName in somePopularCultureNames)
            {
                CultureInfo culture = new CultureInfo(cultureName);
                installedCultures[uniqueDisplayNameCount] = culture;
                cultureDisplayNames[uniqueDisplayNameCount] = culture.DisplayName;
                sl2.Add(cultureDisplayNames[uniqueDisplayNameCount], culture);

                uniqueDisplayNameCount++;
            }

            //In Czech ch comes after h if the comparer changes based on the current culture of the thread
            //we will not be able to find some items
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("cs-CZ");

            for (i = 0; i < uniqueDisplayNameCount; ++i)
            {
                Assert.Equal(installedCultures[i], sl2[installedCultures[i].DisplayName]);
            }

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        }
    }
}
