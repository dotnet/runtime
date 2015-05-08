// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using Xunit;
using Xunit.Abstractions;

#if RELEASE 
namespace Microsoft.Framework.WebEncoders
{
    public class PerformanceTests
    {
        const int SmallItterations = 500000;
        const string SmallString = "<Hello World";

        const int LargeItterations = 150000;
        const string LargeString = "<Hello World aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        const string LargeStringThatIsNotEncoded = "Hello World aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string LargeStringWithOnlyEndEncoded = "Hello World aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa<aaa>";

        private readonly ITestOutputHelper output;

        public PerformanceTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void JavaScriptEncodeStringToString()
        {
            IJavaScriptStringEncoder oldEncoder = JavaScriptStringEncoderOld.Default;
            IJavaScriptStringEncoder newEncoder = JavaScriptStringEncoder.Default;
            Stopwatch timer = new Stopwatch();

            // warmup
            EncodeJavaScript(oldEncoder, SmallString, timer, SmallItterations);
            EncodeJavaScript(newEncoder, SmallString, timer, SmallItterations);

            var oldTime = EncodeJavaScript(oldEncoder, SmallString, timer, SmallItterations);
            var newTime = EncodeJavaScript(newEncoder, SmallString, timer, SmallItterations);
            var message = String.Format("JavaScriptEncodeStringToStringSmall: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeJavaScript(oldEncoder, LargeString, timer, LargeItterations);
            newTime = EncodeJavaScript(newEncoder, LargeString, timer, LargeItterations);
            message = String.Format("JavaScriptEncodeStringToStringLarge: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeJavaScript(oldEncoder, LargeStringThatIsNotEncoded, timer, LargeItterations);
            newTime = EncodeJavaScript(newEncoder, LargeStringThatIsNotEncoded, timer, LargeItterations);
            message = String.Format("JavaScriptEncodeStringToStringLargeNoEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeJavaScript(oldEncoder, LargeStringWithOnlyEndEncoded, timer, LargeItterations);
            newTime = EncodeJavaScript(newEncoder, LargeStringWithOnlyEndEncoded, timer, LargeItterations);
            message = String.Format("JavaScriptEncodeStringToStringLargeEndEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);
        }

        private TimeSpan EncodeJavaScript(IJavaScriptStringEncoder encoder, string text, Stopwatch timer, int itterations)
        {
            timer.Restart();
            for (int itteration = 0; itteration < itterations; itteration++)
            {
                Ignore(encoder.JavaScriptStringEncode(text));
            }
            return timer.Elapsed;
        }

        [Fact]
        public void HtmlEncodeStringToStringSmall()
        {
            IHtmlEncoder oldEncoder = HtmlEncoderOld.Default;
            IHtmlEncoder newEncoder = HtmlEncoder.Default;
            Stopwatch timer = new Stopwatch();

            // warmup
            EncodeHtml(oldEncoder, SmallString, timer, SmallItterations);
            EncodeHtml(newEncoder, SmallString, timer, SmallItterations);

            var oldTime = EncodeHtml(oldEncoder, SmallString, timer, SmallItterations);
            var newTime = EncodeHtml(newEncoder, SmallString, timer, SmallItterations);
            var message = String.Format("HtmlEncodeStringToStringSmall: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtml(oldEncoder, LargeString, timer, LargeItterations);
            newTime = EncodeHtml(newEncoder, LargeString, timer, LargeItterations);
            message = String.Format("HtmlEncodeStringToStringLarge: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtml(oldEncoder, LargeStringThatIsNotEncoded, timer, LargeItterations);
            newTime = EncodeHtml(newEncoder, LargeStringThatIsNotEncoded, timer, LargeItterations);
            message = String.Format("HtmlEncodeStringToStringLargeNoEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtml(oldEncoder, LargeStringWithOnlyEndEncoded, timer, LargeItterations);
            newTime = EncodeHtml(newEncoder, LargeStringWithOnlyEndEncoded, timer, LargeItterations);
            message = String.Format("HtmlEncodeStringToStringLargeEndEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);
        }

        private TimeSpan EncodeHtml(IHtmlEncoder encoder, string text, Stopwatch timer, int itterations)
        {
            timer.Restart();
            for(int itteration=0; itteration<itterations; itteration++)
            {
                Ignore(encoder.HtmlEncode(text));
            }
            return timer.Elapsed;
        }

        [Fact]
        public void HtmlEncodeTextWriter()
        {
            IHtmlEncoder oldEncoder = HtmlEncoderOld.Default;
            IHtmlEncoder newEncoder = HtmlEncoder.Default;
            Stopwatch timer = new Stopwatch();

            // warmup
            EncodeHtmlToTextWriter(oldEncoder, SmallString, timer, SmallItterations);
            EncodeHtmlToTextWriter(newEncoder, SmallString, timer, SmallItterations);

            var oldTime = EncodeHtmlToTextWriter(oldEncoder, SmallString, timer, SmallItterations);
            var newTime = EncodeHtmlToTextWriter(newEncoder, SmallString, timer, SmallItterations);
            var message = String.Format("HtmlEncodeToTextWriter: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtmlToTextWriter(oldEncoder, LargeString, timer, LargeItterations);
            newTime = EncodeHtmlToTextWriter(newEncoder, LargeString, timer, LargeItterations);
            message = String.Format("HtmlEncodeToTextWriterLarge: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtmlToTextWriter(oldEncoder, LargeStringThatIsNotEncoded, timer, LargeItterations);
            newTime = EncodeHtmlToTextWriter(newEncoder, LargeStringThatIsNotEncoded, timer, LargeItterations);
            message = String.Format("HtmlEncodeToTextWriterLargeNoEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtmlToTextWriter(oldEncoder, LargeStringWithOnlyEndEncoded, timer, LargeItterations);
            newTime = EncodeHtmlToTextWriter(newEncoder, LargeStringWithOnlyEndEncoded, timer, LargeItterations);
            message = String.Format("HtmlEncodeToTextWriterLargeEndEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);
        }

        private static TimeSpan EncodeHtmlToTextWriter(IHtmlEncoder encoder, string text, Stopwatch timer, int itterations)
        {
            var defaultEncoder = System.Text.Encodings.Web.HtmlEncoder.Default;

            var stream = new MemoryStream(text.Length * 2 * defaultEncoder.MaxOutputCharsPerInputChar);
            var writer = new StreamWriter(stream);
            timer.Restart();
            for (int itteration = 0; itteration < itterations; itteration++)
            {
                encoder.HtmlEncode(text, writer);
            }
            writer.Flush();
            writer.Dispose();
            return timer.Elapsed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Ignore<T>(T value)
        { }
    }
}
#endif
