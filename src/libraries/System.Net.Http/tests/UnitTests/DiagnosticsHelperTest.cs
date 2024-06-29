// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Tests
{
    public class DiagnsoticsHelperTest
    {
        public static readonly TheoryData<string, string> GetRedactedUriString_Data = new TheoryData<string, string>()
        {
            { "http://q.app/foo", "http://q.app/foo" },
            { "http://user:xxx@q.app/foo", "http://q.app/foo" }, // has user info
            { "http://q.app/foo?", "http://q.app/foo?" },
            { "http://q.app/foo?XXX", "http://q.app/foo?*" },
            { "http://q.app/a/b/c?a=b%20c&x=1", "http://q.app/a/b/c?*" },
            { "http://q.app/#", "http://q.app/#" }, // Has empty fragment.
            { "http://q.app#f", "http://q.app/#f" }, // Has fragment.
            { "http://q.app#f?a=b", "http://q.app/#f?a=b" }, // Has fragment with a '?'.
            { "http://q.app/?a=b#f?a=b", "http://q.app/?*#f?a=b" }, // Has query and fragment with a '?'.
            { "http://q.app?#f", "http://q.app/?#f" }, // Has empty query and fragment.
        };

        [Theory]
        [MemberData(nameof(GetRedactedUriString_Data))]
        public void GetRedactedUriString(string original, string expected)
        {
            string redacted = DiagnosticsHelper.GetRedactedUriString(new Uri(original));
            Assert.Equal(expected, redacted);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task GetRedactedUriString_DisableUriQueryRedaction_RedactsOnlyUserInfo()
        {
            await RemoteExecutor.Invoke(() =>
            {
                AppContext.SetSwitch("System.Net.Http.DisableUriQueryRedaction", true);
                Uri uri = new("http://user:xxx@q.app/foo?a=1&b=2");

                string redacted = DiagnosticsHelper.GetRedactedUriString(uri);

                Assert.Equal("http://q.app/foo?a=1&b=2", redacted);
            }).DisposeAsync();
        }

        [Fact]
        public void TryGetErrorType_NoError_ReturnsFalse()
        {
            HttpResponseMessage response = new(HttpStatusCode.OK);
            Assert.False(DiagnosticsHelper.TryGetErrorType(response, null, out _));
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public void TryGetErrorType_ErrorStatus_OutputsStatusCodeString(HttpStatusCode statusCode)
        {
            HttpResponseMessage response = new(statusCode);
            Assert.True(DiagnosticsHelper.TryGetErrorType(response, null, out string errorType));
        }


        public static TheoryData<HttpRequestError> TryGetErrorType_HttpRequestError_Data()
        {
            TheoryData<HttpRequestError> result = new();
            foreach (var e in Enum.GetValues(typeof(HttpRequestError)))
            {
                result.Add((HttpRequestError)e);
            }
            return result;
        }

        [Theory]
        [MemberData(nameof(TryGetErrorType_HttpRequestError_Data))]
        public void TryGetErrorType_HttpRequestError(HttpRequestError error)
        {
            HttpRequestException exception = new(error);

            Assert.True(DiagnosticsHelper.TryGetErrorType(null, exception, out string errorType));
            Assert.Equal(GetExpectedErrorType(error), errorType);

            Assert.True(DiagnosticsHelper.TryGetErrorType(new HttpResponseMessage(HttpStatusCode.OK), exception, out errorType));
            Assert.Equal(GetExpectedErrorType(error), errorType);

            static string GetExpectedErrorType(HttpRequestError error)
            {
                if (error == HttpRequestError.Unknown)
                {
                    return typeof(HttpRequestException).FullName;
                }

                string s = error.ToString();
                StringBuilder bld = new();
                bld.Append(char.ToLower(s[0]));
                for (int i = 1; i < s.Length; i++)
                {
                    bld.Append(char.IsUpper(s[i]) ? $"_{char.ToLower(s[i])}" : s[i]);
                }
                return bld.ToString();
            }
        }

        [Fact]
        public void TryGetErrorType_OperationCanceledException()
        {
            OperationCanceledException ex = new();
            Assert.True(DiagnosticsHelper.TryGetErrorType(null, ex, out string errorType));
            Assert.Equal(ex.GetType().FullName, errorType);
        }
    }
}
