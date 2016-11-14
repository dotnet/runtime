// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Xunit;

namespace System.Xml.Tests
{
    public class XmlWriterTests_InvalidSurrogate
    {
        private const int SurHighStart = 0xd800;
        private const int SurLowStart = 0xdc00;
        private const int SurLowEnd = 0xdfff;

        static XmlWriterTests_InvalidSurrogate()
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.Netcore50)]  // Switch to throw expception was introduced in NetStandard1.7
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
