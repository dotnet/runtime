// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime.Utilities;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Tests
{
    public class DiagnsoticsHelperTest
    {
        public static readonly TheoryData<string, string> GetRedactedUriString_Data = new TheoryData<string, string>()
        {
            { "http://q.app/foo", "http://q.app/foo" },
            { "http://q.app:123/foo", "http://q.app:123/foo" },
            { "http://user:xxx@q.app/foo", "http://q.app/foo" }, // has user info
            { "http://q.app/foo?", "http://q.app/foo?" },
            { "http://q.app/foo?XXX", "http://q.app/foo?*" },
            { "http://q.app/a/b/c?a=b%20c&x=1", "http://q.app/a/b/c?*" },
            { "http://q.app:4242/a/b/c?a=b%20c&x=1", "http://q.app:4242/a/b/c?*" },
        };

        [Theory]
        [MemberData(nameof(GetRedactedUriString_Data))]
        public void GetRedactedUriString_RedactsUriByDefault(string original, string expected)
        {
            string redacted = DiagnosticsHelper.GetRedactedUriString(new Uri(original));
            Assert.Equal(expected, redacted);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task GetRedactedUriString_DisableUriRedaction_DoesNotRedactUri()
        {
            await RemoteExecutor.Invoke(() =>
            {
                AppContext.SetSwitch("System.Net.Http.DisableUriRedaction", true);

                Uri[] uris = GetRedactedUriString_Data.Select(a => a[0] == null ? null : new Uri((string)a[0], UriKind.RelativeOrAbsolute)).ToArray();

                foreach (Uri uri in uris)
                {
                    string actual = DiagnosticsHelper.GetRedactedUriString(uri);
                    Assert.Equal(uri.AbsoluteUri, actual);
                }
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
