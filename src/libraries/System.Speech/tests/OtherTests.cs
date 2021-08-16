// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Speech.Synthesis;
using System.Text;
using System.Xml;
using Xunit;

namespace SampleSynthesisTests
{
    public static class OtherTests
    {
        [Fact]
        public static void PromptBuilder()
        {
            string SsmlNs = "\"http://schemas.microsoft.com/Speech/2003/03/PromptEngine\"";
            string SsmlStartOutTag = "<peml:prompt_output xmlns:peml=" + SsmlNs + ">";
            string SsmlEndOutTag = "</peml:prompt_output>";

            PromptBuilder builder = new PromptBuilder();
            builder.AppendText("test");
            builder.AppendTextWithPronunciation("foo", "bar");
            builder.AppendSsmlMarkup(SsmlStartOutTag);
            builder.AppendSsmlMarkup("hello");
            builder.AppendSsmlMarkup(SsmlEndOutTag);

            Assert.Contains("hello", builder.ToXml());
            Assert.Equal(CultureInfo.CurrentCulture, builder.Culture);
            Assert.False(builder.IsEmpty);

            string ssml = builder.ToXml();
            builder.AppendSsml(XmlTextReader.Create(new StringReader(ssml)));
        }

        // Add this to a test to log the installed voices on a machine
        private static string DumpRegistry()
        {
            StringBuilder sb = new();
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Speech\Voices\Tokens");
            Traverse(key);

            void Traverse(RegistryKey key, int indent = 0)
            {
                sb.AppendLine(key.Name);
                string[] valnames = key.GetValueNames();
                foreach (string valname in valnames)
                {
                    sb.AppendLine(new string(' ', indent) + valname + ": " + key.GetValue(valname));
                }

                string[] names = key.GetSubKeyNames();

                foreach (var subkeyname in names)
                {
                    Traverse(key.OpenSubKey(subkeyname), indent + 1);
                }
            }

            return sb.ToString();
        }
    }
}
