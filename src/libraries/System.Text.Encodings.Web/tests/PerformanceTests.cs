// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        const int SmallIterations = 500000;
        const string SmallString = "<Hello World";

        const int LargeIterations = 150000;
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

            // warm up
            EncodeJavaScript(oldEncoder, SmallString, timer, SmallIterations);
            EncodeJavaScript(newEncoder, SmallString, timer, SmallIterations);

            var oldTime = EncodeJavaScript(oldEncoder, SmallString, timer, SmallIterations);
            var newTime = EncodeJavaScript(newEncoder, SmallString, timer, SmallIterations);
            var message = String.Format("JavaScriptEncodeStringToStringSmall: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeJavaScript(oldEncoder, LargeString, timer, LargeIterations);
            newTime = EncodeJavaScript(newEncoder, LargeString, timer, LargeIterations);
            message = String.Format("JavaScriptEncodeStringToStringLarge: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeJavaScript(oldEncoder, LargeStringThatIsNotEncoded, timer, LargeIterations);
            newTime = EncodeJavaScript(newEncoder, LargeStringThatIsNotEncoded, timer, LargeIterations);
            message = String.Format("JavaScriptEncodeStringToStringLargeNoEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeJavaScript(oldEncoder, LargeStringWithOnlyEndEncoded, timer, LargeIterations);
            newTime = EncodeJavaScript(newEncoder, LargeStringWithOnlyEndEncoded, timer, LargeIterations);
            message = String.Format("JavaScriptEncodeStringToStringLargeEndEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);
        }

        private TimeSpan EncodeJavaScript(IJavaScriptStringEncoder encoder, string text, Stopwatch timer, int iterations)
        {
            timer.Restart();
            for (int iteration = 0; iteration < iterations; iteration++)
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

            // warm up
            EncodeHtml(oldEncoder, SmallString, timer, SmallIterations);
            EncodeHtml(newEncoder, SmallString, timer, SmallIterations);

            var oldTime = EncodeHtml(oldEncoder, SmallString, timer, SmallIterations);
            var newTime = EncodeHtml(newEncoder, SmallString, timer, SmallIterations);
            var message = String.Format("HtmlEncodeStringToStringSmall: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtml(oldEncoder, LargeString, timer, LargeIterations);
            newTime = EncodeHtml(newEncoder, LargeString, timer, LargeIterations);
            message = String.Format("HtmlEncodeStringToStringLarge: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtml(oldEncoder, LargeStringThatIsNotEncoded, timer, LargeIterations);
            newTime = EncodeHtml(newEncoder, LargeStringThatIsNotEncoded, timer, LargeIterations);
            message = String.Format("HtmlEncodeStringToStringLargeNoEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtml(oldEncoder, LargeStringWithOnlyEndEncoded, timer, LargeIterations);
            newTime = EncodeHtml(newEncoder, LargeStringWithOnlyEndEncoded, timer, LargeIterations);
            message = String.Format("HtmlEncodeStringToStringLargeEndEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);
        }

        private TimeSpan EncodeHtml(IHtmlEncoder encoder, string text, Stopwatch timer, int iterations)
        {
            timer.Restart();
            for (int iteration = 0; iteration < iterations; iteration++)
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

            // warm up
            EncodeHtmlToTextWriter(oldEncoder, SmallString, timer, SmallIterations);
            EncodeHtmlToTextWriter(newEncoder, SmallString, timer, SmallIterations);

            var oldTime = EncodeHtmlToTextWriter(oldEncoder, SmallString, timer, SmallIterations);
            var newTime = EncodeHtmlToTextWriter(newEncoder, SmallString, timer, SmallIterations);
            var message = String.Format("HtmlEncodeToTextWriter: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtmlToTextWriter(oldEncoder, LargeString, timer, LargeIterations);
            newTime = EncodeHtmlToTextWriter(newEncoder, LargeString, timer, LargeIterations);
            message = String.Format("HtmlEncodeToTextWriterLarge: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtmlToTextWriter(oldEncoder, LargeStringThatIsNotEncoded, timer, LargeIterations);
            newTime = EncodeHtmlToTextWriter(newEncoder, LargeStringThatIsNotEncoded, timer, LargeIterations);
            message = String.Format("HtmlEncodeToTextWriterLargeNoEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);

            oldTime = EncodeHtmlToTextWriter(oldEncoder, LargeStringWithOnlyEndEncoded, timer, LargeIterations);
            newTime = EncodeHtmlToTextWriter(newEncoder, LargeStringWithOnlyEndEncoded, timer, LargeIterations);
            message = String.Format("HtmlEncodeToTextWriterLargeEndEncoding: Old={0}ms, New={1}ms, Delta={2:G}%", (ulong)oldTime.TotalMilliseconds, (ulong)newTime.TotalMilliseconds, (int)((newTime.TotalMilliseconds - oldTime.TotalMilliseconds) / oldTime.TotalMilliseconds * 100));
            output.WriteLine(message);
        }

        private static TimeSpan EncodeHtmlToTextWriter(IHtmlEncoder encoder, string text, Stopwatch timer, int iterations)
        {
            var defaultEncoder = System.Text.Encodings.Web.HtmlEncoder.Default;

            var stream = new MemoryStream(text.Length * 2 * defaultEncoder.MaxOutputCharactersPerInputCharacter);
            var writer = new StreamWriter(stream);
            timer.Restart();
            for (int iteration = 0; iteration < iterations; iteration++)
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
