// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Reflection;

namespace XMLTests.ReaderWriter.RegressionTests
{
    public class XmlWriterTests
    {
        private const int SurHighStart = 0xd800;
        private const int SurLowStart = 0xdc00;
        private const int SurLowEnd = 0xdfff;

        static XmlWriterTests()
        {
            // Make sure that we don't cache the value of the switch to enable testing
            AppContext.SetSwitch("TestSwitch.LocalAppContext.DisableCaching", true);
        }

        [Fact]
        public static void XmlWriterChecksLowerBoundOfLowerSurrogate()
        {
            using (XmlWriter writer = CreateUtf8Writer())
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("test");
                char[] invalidSurrogatePair = new char[] { (char)(SurHighStart + 5), (char)(SurLowStart - 1) };

                Assert.Throws<ArgumentException>(() =>
                {
                    writer.WriteRaw(invalidSurrogatePair, 0, invalidSurrogatePair.Length);
                });
            }
        }

        [Fact]
        public static void XmlWriterChecksUpperBoundOfLowerSurrogate_newBehavior()
        {
            // Turn the switch off to get the new behavior in case the platform has it on by default
            AppContext.SetSwitch(@"Switch.System.Xml.DontThrowOnInvalidSurrogatePairs", false);

            using (XmlWriter writer = CreateUtf8Writer())
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("test");
                char[] invalidSurrogatePair = new char[] { (char)(SurHighStart + 5), (char)(SurLowEnd + 1) };

                Assert.Throws<ArgumentException>(() =>
                {
                    writer.WriteRaw(invalidSurrogatePair, 0, invalidSurrogatePair.Length);
                });
            }
        }

        [Fact]
        public static void XmlWriterWorksWithValidLowerSurrogate_newBehavior()
        {
            // Turn the switch off to get the new behavior in case the platform has it on by default
            AppContext.SetSwitch(@"Switch.System.Xml.DontThrowOnInvalidSurrogatePairs", false);

            using (XmlWriter writer = CreateUtf8Writer())
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("test");
                char[] validSurrogatePairs = new char[] {
                    (char)(SurHighStart + 5), (char)(SurLowEnd),
                    (char)(SurHighStart + 5), (char)(SurLowEnd - 1),
                    (char)(SurHighStart + 5), (char)(SurLowStart),
                    (char)(SurHighStart + 5), (char)(SurLowStart + 1),
                    };

                // Everything should be fine, no exceptions
                writer.WriteRaw(validSurrogatePairs, 0, validSurrogatePairs.Length);
            }
        }

        private static XmlWriter CreateUtf8Writer()
        {
            MemoryStream output = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            return XmlWriter.Create(output, settings);
        }
    }
}