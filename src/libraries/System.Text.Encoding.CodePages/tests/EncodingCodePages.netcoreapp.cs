// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public partial class EncodingTest : IClassFixture<CultureSetup>
    {
        private class EncodingInformation
        {
            public EncodingInformation(int codePage, string name)
            {
                CodePage = codePage;
                Name = name;
            }

            public int CodePage { get; }
            public string Name { get; }
        }

        private static EncodingInformation [] s_defaultEncoding = new EncodingInformation []
        {
            new EncodingInformation(1200, "utf-16"),
            new EncodingInformation(1201, "utf-16BE"),
            new EncodingInformation(12000, "utf-32"),
            new EncodingInformation(12001, "utf-32BE"),
            new EncodingInformation(20127, "us-ascii"),
            new EncodingInformation(28591, "iso-8859-1"),
            new EncodingInformation(65001, "utf-8")
        };

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestGetEncodings()
        {
            RemoteExecutor.Invoke(() => {
                EncodingInfo [] list = Encoding.GetEncodings();

                foreach (EncodingInformation eInfo in s_defaultEncoding)
                {
                    Assert.NotNull(list.FirstOrDefault(o => o.CodePage == eInfo.CodePage && o.Name == eInfo.Name));
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestGetEncodingsWithProvider()
        {
            RemoteExecutor.Invoke(() => {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                foreach (EncodingInfo ei in Encoding.GetEncodings())
                {
                    Encoding encoding = ei.GetEncoding();
                    Assert.Equal(ei.CodePage, encoding.CodePage);

                    Assert.True(ei.Name.Equals(encoding.WebName, StringComparison.OrdinalIgnoreCase), $"Encodinginfo.Name `{ei.Name}` != Encoding.WebName `{encoding.WebName}`");
                }
            }).Dispose();
        }
    }
}
