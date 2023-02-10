// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets.Client.Tests;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.Net.WebSockets.Client.Wasm.Tests
{
    // https://developer.chrome.com/blog/timer-throttling-in-chrome-88/
    // https://docs.google.com/document/d/11FhKHRcABGS4SWPFGwoL6g0ALMqrFKapCk5ZTKKupEk/view
    // requires chromium based browser
    // requires minimized browser or browser tab out of focus, browser can't be headless
    // requires --enable-features=IntensiveWakeUpThrottling:grace_period_seconds/1 chromeDriver flags
    // doesn't work with --disable-background-timer-throttling
    [TestCaseOrderer("System.Net.WebSockets.Client.Wasm.Tests.AlphabeticalOrderer", "System.Net.WebSockets.Client.Wasm.Tests")]
    public class BrowserTimerThrottlingTest : ClientWebSocketTestBase
    {
        public static bool IsBrowser => RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));
        const double moreThanLightThrottlingThreshold = 1900;
        const double detectLightThrottlingThreshold = 900;
        const double webSocketMessageFrequency = 45000;
        const double fastTimeoutFrequency = 100;

        public BrowserTimerThrottlingTest(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(PlatformDetection.IsBrowser))]
        [OuterLoop] // involves long delay
        // this test is influenced by usage of WS on the same browser tab in previous unit tests. we may need to wait long time for it to fizzle down
        public async Task DotnetTimersAreHeavilyThrottledWithoutWebSocket()
        {
            double maxDelayMs = 0;
            double maxLightDelayMs = 0;
            DateTime start = DateTime.Now;
            CancellationTokenSource cts = new CancellationTokenSource();

            using (var timer = new Timers.Timer(fastTimeoutFrequency))
            {
                DateTime last = DateTime.Now;
                timer.AutoReset = true;
                timer.Enabled = true;
                timer.Elapsed += (object? source, Timers.ElapsedEventArgs? e) =>
                {
                    var ms = (e.SignalTime - last).TotalMilliseconds;
                    if (maxDelayMs < ms)
                    {
                        maxDelayMs = ms;
                    }
                    if (ms > moreThanLightThrottlingThreshold)
                    {
#if DEBUG
                        Console.WriteLine("Too slow tick " + ms);
#endif
                        // stop, we are throttled heavily, this is what we are looking for
                        cts.Cancel();
                    }
                    else if (ms > detectLightThrottlingThreshold)
                    {
                        maxLightDelayMs = ms;
                        // we are lightly throttled
#if DEBUG
                        Console.WriteLine("Slow tick NO-WS " + ms);
#endif
                    }
                    last = e.SignalTime;
                };

                // test it for 10 minutes
                try { await Task.Delay(10 * 60 * 1000, cts.Token); } catch (Exception) { }
                timer.Close();
            }
            Assert.True(maxDelayMs > detectLightThrottlingThreshold, "Expect that it throttled lightly " + maxDelayMs);
            Assert.True(maxDelayMs > moreThanLightThrottlingThreshold, "Expect that it was heavily throttled " + maxDelayMs);
        }

        [ConditionalFact(nameof(WebSocketsSupported), nameof(PlatformDetection.IsBrowser))]
        [OuterLoop] // involves long delay
        public async Task WebSocketKeepsDotnetTimersOnlyLightlyThrottled()
        {
            double maxDelayMs = 0;
            double maxLightDelayMs = 0;
            DateTime start = DateTime.Now;
            CancellationTokenSource cts = new CancellationTokenSource();

            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(Test.Common.Configuration.WebSockets.RemoteEchoServer, TimeOutMilliseconds, _output))
            {
                await SendAndReceive(cws, "test");
                using (var timer = new Timers.Timer(fastTimeoutFrequency))
                {
                    DateTime last = DateTime.Now;
                    DateTime lastSent = DateTime.MinValue;
                    timer.AutoReset = true;
                    timer.Enabled = true;
                    timer.Elapsed += async (object? source, Timers.ElapsedEventArgs? e) =>
                    {
                        var ms = (e.SignalTime - last).TotalMilliseconds;
                        var msSent = (e.SignalTime - lastSent).TotalMilliseconds;
                        if (maxDelayMs < ms)
                        {
                            maxDelayMs = ms;
                        }
                        if (ms > moreThanLightThrottlingThreshold)
                        {
                            // fail fast, we are throttled heavily
#if DEBUG
                            Console.WriteLine("Too slow tick " + ms);
#endif
                            cts.Cancel();
                        }
                        else if (ms > detectLightThrottlingThreshold)
                        {
                            maxLightDelayMs = ms;
                            // we are lightly throttled
#if DEBUG
                            Console.WriteLine("Slow tick WS " + ms);
#endif
                        }
                        if (msSent > webSocketMessageFrequency)
                        {
                            await SendAndReceive(cws, "test");
                            lastSent = DateTime.Now;
                        }
                        last = e.SignalTime;
                    };

                    // test it for 10 minutes
                    try { await Task.Delay(10 * 60 * 1000, cts.Token); } catch (Exception) { }
                    timer.Close();
                }
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "WebSocketKeepsDotnetTimersOnlyLightlyThrottled", CancellationToken.None);
            }
            Assert.True(maxDelayMs > detectLightThrottlingThreshold, "Expect that it throttled lightly " + maxDelayMs);
            Assert.True(maxDelayMs < moreThanLightThrottlingThreshold, "Expect that it wasn't heavily throttled " + maxDelayMs);
        }

        private async static Task SendAndReceive(ClientWebSocket cws, string message)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await cws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

                var receiveBuffer = new byte[100];
                var receiveSegment = new ArraySegment<byte>(receiveBuffer);
                WebSocketReceiveResult recvRet = await cws.ReceiveAsync(receiveSegment, CancellationToken.None);
#if DEBUG
                Console.WriteLine("SendAndReceive");
#endif
            }
            catch (OperationCanceledException)
            {
            }
#if DEBUG
            catch (Exception ex)
            {
                Console.WriteLine("SendAndReceive fail:" + ex);
            }
#endif
        }
    }

    // this is just for convinience, as the second test has side-effect to running page, the first test would take longer if they are in another order
    public class AlphabeticalOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
                where TTestCase : ITestCase
        {
            List<TTestCase> result = testCases.ToList();
            result.Sort((x, y) => StringComparer.Ordinal.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
            return result;
        }
    }
}
