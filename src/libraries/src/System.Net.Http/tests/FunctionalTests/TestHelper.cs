// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    // TODO: This class will eventually be moved to Common once the HttpTestServers are finalized.
    public static class TestHelper
    {
        public static bool JsonMessageContainsKeyValue(string message, string key, string value)
        {
            // TODO: Align with the rest of tests w.r.t response parsing once the test server is finalized.
            // Currently not adding any new dependencies
            string pattern = string.Format(@"""{0}"": ""{1}""", key, value);
            return message.Contains(pattern);
        }

        public static bool JsonMessageContainsKey(string message, string key)
        {
            // TODO: Align with the rest of tests w.r.t response parsing once the test server is finalized.
            // Currently not adding any new dependencies
            string pattern = string.Format(@"""{0}"": """, key);
            return message.Contains(pattern);
        }

        public static void VerifyResponseBody(
            string responseContent,
            byte[] expectedMD5Hash,
            bool chunkedUpload,
            string requestBody)
        {
            // Verify that response body from the server was corrected received by comparing MD5 hash.
            byte[] actualMD5Hash = ComputeMD5Hash(responseContent);
            Assert.Equal(expectedMD5Hash, actualMD5Hash);

            // Verify upload semsntics: 'Content-Length' vs. 'Transfer-Encoding: chunked'.
            if (requestBody != null)
            {
                bool requestUsedContentLengthUpload =
                    JsonMessageContainsKeyValue(responseContent, "Content-Length", requestBody.Length.ToString());
                bool requestUsedChunkedUpload =
                    JsonMessageContainsKeyValue(responseContent, "Transfer-Encoding", "chunked");
                if (requestBody.Length > 0)
                {
                    Assert.NotEqual(requestUsedContentLengthUpload, requestUsedChunkedUpload);
                    Assert.Equal(chunkedUpload, requestUsedChunkedUpload);
                    Assert.Equal(!chunkedUpload, requestUsedContentLengthUpload);
                }

                // Verify that request body content was correctly sent to server.
                Assert.True(JsonMessageContainsKeyValue(responseContent, "BodyContent", requestBody), "Valid request body");
            }
        }

        public static void VerifyRequestMethod(HttpResponseMessage response, string expectedMethod)
        {
           IEnumerable<string> values = response.Headers.GetValues("X-HttpRequest-Method");
           foreach (string value in values)
           {
               Assert.Equal(expectedMethod, value);
           }
        }

        public static byte[] ComputeMD5Hash(string data)
        {
            return ComputeMD5Hash(Encoding.UTF8.GetBytes(data));
        }

        public static byte[] ComputeMD5Hash(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }        
        }
    }
}
