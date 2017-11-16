// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Net.Tests
{
    public class HttpListenerTimeoutManagerTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData((long)uint.MaxValue + 1)]
        public void MinSendBytesPerSecond_NotUInt_ThrowsArgumentOutOfRangeException(long value)
        {
            using (var listener = new HttpListener())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => listener.TimeoutManager.MinSendBytesPerSecond = value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((uint)ushort.MaxValue + 1)]
        public void TimeoutValue_NotUShort_ThrowsArgumentOutOfRangeException(long totalSeconds)
        {
            using (var listener = new HttpListener())
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => listener.TimeoutManager.EntityBody = timeSpan);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => listener.TimeoutManager.DrainEntityBody = timeSpan);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => listener.TimeoutManager.RequestQueue = timeSpan);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => listener.TimeoutManager.IdleConnection = timeSpan);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => listener.TimeoutManager.HeaderWait = timeSpan);
            }
        }

        [Fact]
        public void Get_Disposed_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            listener.Close();

            Assert.Throws<ObjectDisposedException>(() => listener.TimeoutManager);
        }
    }

    [PlatformSpecific(TestPlatforms.Windows)]
    public class HttpListenerTimeoutManagerWindowsTests : IDisposable
    {
        private const string HTTPAPI = "httpapi.dll";

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpQueryUrlGroupProperty(
            ulong urlGroupId,
            HTTP_SERVER_PROPERTY serverProperty, IntPtr pPropertyInfo,
            uint propertyInfoLength,
            IntPtr reserved);

        internal enum HTTP_TIMEOUT_TYPE
        {
            EntityBody,
            DrainEntityBody,
            RequestQueue,
            IdleConnection,
            HeaderWait,
            MinSendRate,
        }

        internal enum HTTP_SERVER_PROPERTY
        {
            HttpServerAuthenticationProperty,
            HttpServerLoggingProperty,
            HttpServerQosProperty,
            HttpServerTimeoutsProperty,
            HttpServerQueueLengthProperty,
            HttpServerStateProperty,
            HttpServer503VerbosityProperty,
            HttpServerBindingProperty,
            HttpServerExtendedAuthenticationProperty,
            HttpServerListenEndpointProperty,
            HttpServerChannelBindProperty,
            HttpServerProtectionLevelProperty,
        }

        [Flags]
        internal enum HTTP_FLAGS : uint
        {
            NONE = 0x00000000,
            HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY = 0x00000001,
            HTTP_RECEIVE_SECURE_CHANNEL_TOKEN = 0x00000001,
            HTTP_SEND_RESPONSE_FLAG_DISCONNECT = 0x00000001,
            HTTP_SEND_RESPONSE_FLAG_MORE_DATA = 0x00000002,
            HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA = 0x00000004,
            HTTP_SEND_RESPONSE_FLAG_RAW_HEADER = 0x00000004,
            HTTP_SEND_REQUEST_FLAG_MORE_DATA = 0x00000001,
            HTTP_PROPERTY_FLAG_PRESENT = 0x00000001,
            HTTP_INITIALIZE_SERVER = 0x00000001,
            HTTP_INITIALIZE_CBT = 0x00000004,
            HTTP_SEND_RESPONSE_FLAG_OPAQUE = 0x00000040,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HTTP_TIMEOUT_LIMIT_INFO
        {
            internal HTTP_FLAGS Flags;
            internal ushort EntityBody;
            internal ushort DrainEntityBody;
            internal ushort RequestQueue;
            internal ushort IdleConnection;
            internal ushort HeaderWait;
            internal uint MinSendRate;
        }

        private HttpListener _listener;

        public HttpListenerTimeoutManagerWindowsTests()
        {
            _listener = new HttpListener();
        }

        public void Dispose() => _listener.Close();

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void TimeoutManager_AccessNoStart_Success()
        {
            // Access the TimeoutManager without calling Start and make sure it is initialized.
            HttpListenerTimeoutManager timeoutManager = _listener.TimeoutManager;
            Assert.NotNull(timeoutManager);

            uint rate = GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.MinSendRate);
            Assert.Equal(rate, timeoutManager.MinSendBytesPerSecond);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void TimeoutManager_AccessAfterStart_Success()
        {
            // Access the TimeoutManager after calling Start and make sure it is initialized.
            _listener.Start();
            HttpListenerTimeoutManager timeoutManager = _listener.TimeoutManager;
            Assert.NotNull(timeoutManager);

            uint rate = GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.MinSendRate);
            Assert.Equal(rate, timeoutManager.MinSendBytesPerSecond);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void TimeoutManager_AccessAfterClose_GetObjectDisposedException()
        {
            // Access the TimeoutManager after calling Close and make sure it is not accessible.
            _listener.Close();

            Assert.Throws<ObjectDisposedException>(() => _listener.TimeoutManager);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void TimeoutManager_AccessBeforeAndAfterClose_GetObjectDisposedException()
        {
            // Access the TimeoutManager after calling Close and make sure it is not accessible.
            _listener.Start();
            HttpListenerTimeoutManager timeoutManager = _listener.TimeoutManager;
            Assert.NotNull(timeoutManager);
            _listener.Close();
            Assert.Throws<ObjectDisposedException>(() => timeoutManager.MinSendBytesPerSecond = 10);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void TimeoutManager_AccessAfterStop_Success()
        {
            // Access the TimeoutManager after calling Stop and make sure it is accessible.
            _listener.Start();
            _listener.Stop();

            HttpListenerTimeoutManager timeoutManager = _listener.TimeoutManager;
            Assert.NotNull(timeoutManager);

            uint rate = GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.MinSendRate);
            Assert.Equal(rate, timeoutManager.MinSendBytesPerSecond);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void DrainEntityBody_SetTimeoutNoStart_GetReturnsNewValue()
        {
            // Set the DrainEntityBody timeout without calling Start and make sure that native layer return new value.
            _listener.TimeoutManager.DrainEntityBody = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.DrainEntityBody);

            Assert.Equal(seconds, _listener.TimeoutManager.DrainEntityBody.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void DrainEntityBody_SetTimeoutAfterStart_GetReturnsNewValue()
        {
            // Set the DrainEntityBody timeout after calling Start and make sure that native layer return new value.
            _listener.Start();
            _listener.TimeoutManager.DrainEntityBody = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.DrainEntityBody);

            Assert.Equal(seconds, _listener.TimeoutManager.DrainEntityBody.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void EntityBody_SetTimeoutNoStart_GetReturnsNewValue()
        {
            // Set the DrainEntityBody timeout without calling Start and make sure that native layer return new value.
            _listener.TimeoutManager.EntityBody = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.EntityBody);

            Assert.Equal(seconds, _listener.TimeoutManager.EntityBody.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void EntityBody_SetTimeoutAfterStart_GetReturnsNewValue()
        {
            // Set the EntityBody timeout after calling Start and make sure that native layer return new value.
            _listener.Start();
            _listener.TimeoutManager.EntityBody = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.EntityBody);

            Assert.Equal(seconds, _listener.TimeoutManager.EntityBody.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void HeaderWait_SetTimeoutNoStart_GetReturnsNewValue()
        {
            // Set the HeaderWait timeout without calling Start and make sure that native layer return new value.
            _listener.TimeoutManager.HeaderWait = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.HeaderWait);

            Assert.Equal(seconds, _listener.TimeoutManager.HeaderWait.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void HeaderWait_SetTimeoutAfterStart_GetReturnsNewValue()
        {
            // Set the HeaderWait timeout after calling Start and make sure that native layer return new value.
            _listener.Start();
            _listener.TimeoutManager.HeaderWait = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.HeaderWait);

            Assert.Equal(seconds, _listener.TimeoutManager.HeaderWait.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void RequestQueue_SetTimeoutNoStart_GetReturnsNewValue()
        {
            // Set the DrainEntityBody timeout without calling Start and make sure that native layer return new value.
            _listener.TimeoutManager.RequestQueue = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.RequestQueue);

            Assert.Equal(seconds, _listener.TimeoutManager.RequestQueue.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void RequestQueue_SetTimeoutAfterStart_GetReturnsNewValue()
        {
            // Set the RequestQueue timeout after calling Start and make sure that native layer return new value.
            _listener.Start();
            _listener.TimeoutManager.RequestQueue = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.RequestQueue);

            Assert.Equal(seconds, _listener.TimeoutManager.RequestQueue.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void IdleConnection_SetTimeoutNoStart_GetReturnsNewValue()
        {
            // Set the IdleConnection timeout without calling Start and make sure that native layer return new value.
            _listener.TimeoutManager.IdleConnection = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.IdleConnection);

            Assert.Equal(seconds, _listener.TimeoutManager.IdleConnection.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void IdleConnection_SetTimeoutAfterStart_GetReturnsNewValue()
        {
            // Set the IdleConnection timeout after calling Start and make sure that native layer return new value.
            _listener.Start();
            _listener.TimeoutManager.IdleConnection = new TimeSpan(0, 0, 300);
            int seconds = (int)GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.IdleConnection);

            Assert.Equal(seconds, _listener.TimeoutManager.IdleConnection.TotalSeconds);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void MinSendBytesPerSecond_SetNoStart_GetReturnsNewValue()
        {
            // Set the MinSendBytesPerSecond timeout without calling Start and make sure that native layer 
            // return new value.
            _listener.TimeoutManager.MinSendBytesPerSecond = 10 * 1024 * 1024;
            uint rate = GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.MinSendRate);

            Assert.Equal(rate, _listener.TimeoutManager.MinSendBytesPerSecond);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void MinSendBytesPerSecond_SetAfterStart_GetReturnsNewValue()
        {
            // Set the MinSendBytesPerSecond timeout after calling Start and make sure that native 
            // layer return new value.
            _listener.Start();
            _listener.TimeoutManager.MinSendBytesPerSecond = 10 * 1024 * 1024;
            uint rate = GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.MinSendRate);

            Assert.Equal(rate, _listener.TimeoutManager.MinSendBytesPerSecond);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void MinSendBytesPerSecond_SetAfterClose_GetObjectDisposedException()
        {
            // Set the MinSendBytesPerSecond timeout after calling Close and make sure that we get the exception.
            _listener.Start();
            _listener.Close();
            Assert.Throws<ObjectDisposedException>(() => _listener.TimeoutManager.MinSendBytesPerSecond = 10 * 1024 * 1024);
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        public void MinSendBytesPerSecond_SetAfterStop_GetReturnsNewValue()
        {
            // Set the MinSendBytesPerSecond timeout after calling Stop and make sure that native 
            // layer return new value.
            _listener.Start();
            _listener.Stop();
            _listener.TimeoutManager.MinSendBytesPerSecond = 10 * 1024 * 1024;
            uint rate = GetServerTimeout(_listener, HTTP_TIMEOUT_TYPE.MinSendRate);

            Assert.Equal(rate, _listener.TimeoutManager.MinSendBytesPerSecond);
        }

        private unsafe uint GetServerTimeout(HttpListener _listener, HTTP_TIMEOUT_TYPE type)
        {
            // There are 6 different timeouts supported and native layer returns all timeouts in every call.
            uint[] timeouts = new uint[6];

            // We need url group id which is private so we get it using reflection.
            string urlGroupIdName = PlatformDetection.IsFullFramework ? "m_UrlGroupId" : "_urlGroupId";
            FieldInfo info = typeof(HttpListener).GetField(urlGroupIdName, BindingFlags.Instance | BindingFlags.NonPublic);
            ulong urlGroupId = (ulong)info.GetValue(_listener);

            HTTP_TIMEOUT_LIMIT_INFO timeoutinfo = new HTTP_TIMEOUT_LIMIT_INFO();

            // Query timeouts from native layer.
            GetUrlGroupProperty(
                urlGroupId,
                HTTP_SERVER_PROPERTY.HttpServerTimeoutsProperty,
                new IntPtr(&timeoutinfo),
                (uint)Marshal.SizeOf(typeof(HTTP_TIMEOUT_LIMIT_INFO)));

            timeouts[(uint)HTTP_TIMEOUT_TYPE.DrainEntityBody] =
                timeoutinfo.DrainEntityBody;
            timeouts[(uint)HTTP_TIMEOUT_TYPE.EntityBody] =
                timeoutinfo.EntityBody;
            timeouts[(uint)HTTP_TIMEOUT_TYPE.RequestQueue] =
                timeoutinfo.RequestQueue;
            timeouts[(uint)HTTP_TIMEOUT_TYPE.IdleConnection] =
                timeoutinfo.IdleConnection;
            timeouts[(uint)HTTP_TIMEOUT_TYPE.HeaderWait] =
                timeoutinfo.HeaderWait;
            timeouts[(uint)HTTP_TIMEOUT_TYPE.MinSendRate] =
                timeoutinfo.MinSendRate;

            return timeouts[(int)type];
        }

        private unsafe void GetUrlGroupProperty(ulong urlGroupId, HTTP_SERVER_PROPERTY property, IntPtr info, uint infosize)
        {
            uint statusCode = 0;

            statusCode = HttpQueryUrlGroupProperty(urlGroupId, HTTP_SERVER_PROPERTY.HttpServerTimeoutsProperty, info, infosize, IntPtr.Zero);

            if (statusCode != 0)
            {
                throw new Exception("HttpQueryUrlGroupProperty failed with " + (int)statusCode);
            }
        }


        [ConditionalTheory(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))]
        [InlineData(1.3, 1)]
        [InlineData(1.6, 2)]
        public void TimeoutValue_Double_Truncates(double seconds, int expected)
        {
            using (var listener = new HttpListener())
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                listener.TimeoutManager.EntityBody = timeSpan;
                Assert.Equal(expected, listener.TimeoutManager.EntityBody.TotalSeconds);
            }
        }
    }

    public class HttpListenerTimeoutManagerUnixTests
    {
        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsManagedImplementation))] // [PlatformSpecific(TestPlatforms.AnyUnix)] // managed implementation doesn't support all members
        public void Properties_DefaultValues()
        {
            using (var listener = new HttpListener())
            {
                HttpListenerTimeoutManager m = listener.TimeoutManager;
                Assert.NotNull(m);
                Assert.Equal(TimeSpan.Zero, m.DrainEntityBody);
                Assert.Equal(TimeSpan.Zero, m.EntityBody);
                Assert.Equal(TimeSpan.Zero, m.HeaderWait);
                Assert.Equal(TimeSpan.Zero, m.IdleConnection);
                Assert.Equal(0, m.MinSendBytesPerSecond);
                Assert.Equal(TimeSpan.Zero, m.RequestQueue);
            }
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsManagedImplementation))] // [PlatformSpecific(TestPlatforms.AnyUnix)] // managed implementation doesn't support all members
        public void UnsupportedProperties_Throw()
        {
            using (var listener = new HttpListener())
            {
                HttpListenerTimeoutManager m = listener.TimeoutManager;
                Assert.Throws<PlatformNotSupportedException>(() => m.EntityBody = TimeSpan.Zero);
                Assert.Throws<PlatformNotSupportedException>(() => m.HeaderWait = TimeSpan.Zero);
                Assert.Throws<PlatformNotSupportedException>(() => m.MinSendBytesPerSecond = 0);
                Assert.Throws<PlatformNotSupportedException>(() => m.RequestQueue = TimeSpan.Zero);
            }
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsManagedImplementation))] // [PlatformSpecific(TestPlatforms.AnyUnix)] // managed implementation doesn't support all members
        public void DrainEntityBody_Roundtrips()
        {
            using (var listener = new HttpListener())
            {
                HttpListenerTimeoutManager m = listener.TimeoutManager;
                Assert.Equal(TimeSpan.Zero, m.DrainEntityBody);

                TimeSpan value = TimeSpan.FromSeconds(123);
                m.DrainEntityBody = value;
                Assert.Equal(value, m.DrainEntityBody);
            }
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsManagedImplementation))] // [PlatformSpecific(TestPlatforms.AnyUnix)] // managed implementation doesn't support all members
        public void IdleConnection_Roundtrips()
        {
            using (var listener = new HttpListener())
            {
                HttpListenerTimeoutManager m = listener.TimeoutManager;
                Assert.Equal(TimeSpan.Zero, m.IdleConnection);

                TimeSpan value = TimeSpan.FromSeconds(123);
                m.IdleConnection = value;
                Assert.Equal(value, m.IdleConnection);
            }
        }
    }
}
