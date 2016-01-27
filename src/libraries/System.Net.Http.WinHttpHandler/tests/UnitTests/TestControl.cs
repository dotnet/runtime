// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public static class TestControl
    {
        public static ApiControl WinHttpOpen { get; private set; }
        public static ApiControl WinHttpQueryDataAvailable { get; private set; }
        public static ApiControl WinHttpReadData { get; private set; }
        public static ApiControl WinHttpReceiveResponse { get; private set; }
        public static ApiControl WinHttpWriteData { get; private set; }
        
        public static int LastWin32Error { get; set; }
        
        public static bool WinHttpAutomaticProxySupport { get; set; }
        public static bool WinHttpDecompressionSupport { get; set; }

        public static bool PACFileNotDetectedOnNetwork { get; set; }

        public static X509Certificate2Collection CurrentUserCertificateStore{ get; set; }

        public static void Reset()
        {
            WinHttpOpen = new ApiControl();
            WinHttpQueryDataAvailable = new ApiControl();
            WinHttpReadData = new ApiControl();
            WinHttpReceiveResponse = new ApiControl();
            WinHttpWriteData = new ApiControl();
            
            WinHttpAutomaticProxySupport = true;
            WinHttpDecompressionSupport = true;
            
            LastWin32Error = 0;
            
            PACFileNotDetectedOnNetwork = false;
            
            CurrentUserCertificateStore = new X509Certificate2Collection();
        }

        public static void ResetAll()
        {
            APICallHistory.Reset();
            FakeRegistry.Reset();
            TestControl.Reset();
            TestServer.Reset();
        }
    }

    public sealed class ApiControl : IDisposable
    {
        private bool _disposed = false;
        private ManualResetEvent _callbackCompletionEvent;
        
        public ApiControl()
        {
            ErrorWithApiCall = false;
            ErrorOnCompletion = false;
            Delay = 0;
            
            _callbackCompletionEvent = new ManualResetEvent(true);
        }
        
        public bool ErrorWithApiCall { get; set; }
        public bool ErrorOnCompletion { get; set; }
        public int Delay { get; set; }

        public void Pause()
        {
            _callbackCompletionEvent.Reset();
        }

        public void Resume()
        {
            _callbackCompletionEvent.Set();
        }

        public void Wait()
        {
            _callbackCompletionEvent.WaitOne();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _callbackCompletionEvent.Dispose();
        }
    }
}
