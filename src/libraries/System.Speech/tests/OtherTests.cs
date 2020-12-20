// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Speech.Synthesis;
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
    }
}
