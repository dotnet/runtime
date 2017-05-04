// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Build.Tasks.Utility
{
    // This is grabbed directly from buildtools and we should not duplicate this when we move ListAzureBlobs back to buildtools
    public static class AzureHelper
    {
        /// <summary>
        ///     The storage api version.
        /// </summary>
        public static readonly string StorageApiVersion = "2015-04-05";
        public const string DateHeaderString = "x-ms-date";
        public const string VersionHeaderString = "x-ms-version";
        public const string AuthorizationHeaderString = "Authorization";
        public const string CacheControlString = "x-ms-blob-cache-control";
        public const string ContentTypeString = "x-ms-blob-content-type";

        public enum SasAccessType
        {
            Read,
            Write,
        };

        public static string AuthorizationHeader(
            string storageAccount,
            string storageKey,
            string method,
            DateTime now,
            HttpRequestMessage request,
            string ifMatch = "",
            string contentMD5 = "",
            string size = "",
            string contentType = "")
        {
            string stringToSign = string.Empty;
            stringToSign = string.Format(
                "{0}\n\n\n{1}\n{5}\n{6}\n\n\n{2}\n\n\n\n{3}{4}",
                method,
                (size == string.Empty) ? string.Empty : size,
                ifMatch,
                GetCanonicalizedHeaders(request),
                GetCanonicalizedResource(request.RequestUri, storageAccount),
                contentMD5,
                contentType);
            byte[] signatureBytes = Encoding.UTF8.GetBytes(stringToSign);
            string authorizationHeader;
            using (HMACSHA256 hmacsha256 = new HMACSHA256(Convert.FromBase64String(storageKey)))
            {
                authorizationHeader = "SharedKey " + storageAccount + ":"
                                      + Convert.ToBase64String(hmacsha256.ComputeHash(signatureBytes));
            }

            return authorizationHeader;
        }

        public static string CreateContainerSasToken(
            string accountName,
            string containerName,
            string key,
            SasAccessType accessType,
            int validityTimeInDays)
        {
            string signedPermissions = string.Empty;
            switch (accessType)
            {
                case SasAccessType.Read:
                    signedPermissions = "r";
                    break;
                case SasAccessType.Write:
                    signedPermissions = "wdl";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessType), accessType, "Unrecognized value");
            }

            string signedStart = DateTime.UtcNow.ToString("O");
            string signedExpiry = DateTime.UtcNow.AddDays(validityTimeInDays).ToString("O");
            string canonicalizedResource = "/blob/" + accountName + "/" + containerName;
            string signedIdentifier = string.Empty;
            string signedVersion = StorageApiVersion;

            string stringToSign = ConstructServiceStringToSign(
                signedPermissions,
                signedVersion,
                signedExpiry,
                canonicalizedResource,
                signedIdentifier,
                signedStart);

            byte[] signatureBytes = Encoding.UTF8.GetBytes(stringToSign);
            string signature;
            using (HMACSHA256 hmacSha256 = new HMACSHA256(Convert.FromBase64String(key)))
            {
                signature = Convert.ToBase64String(hmacSha256.ComputeHash(signatureBytes));
            }

            string sasToken = string.Format(
                "?sv={0}&sr={1}&sig={2}&st={3}&se={4}&sp={5}",
                WebUtility.UrlEncode(signedVersion),
                WebUtility.UrlEncode("c"),
                WebUtility.UrlEncode(signature),
                WebUtility.UrlEncode(signedStart),
                WebUtility.UrlEncode(signedExpiry),
                WebUtility.UrlEncode(signedPermissions));

            return sasToken;
        }

        public static string GetCanonicalizedHeaders(HttpRequestMessage request)
        {
            StringBuilder sb = new StringBuilder();
            List<string> headerNameList = (from headerName in request.Headers
                                           where
                                               headerName.Key.ToLowerInvariant()
                                               .StartsWith("x-ms-", StringComparison.Ordinal)
                                           select headerName.Key.ToLowerInvariant()).ToList();
            headerNameList.Sort();
            foreach (string headerName in headerNameList)
            {
                StringBuilder builder = new StringBuilder(headerName);
                string separator = ":";
                foreach (string headerValue in GetHeaderValues(request.Headers, headerName))
                {
                    string trimmedValue = headerValue.Replace("\r\n", string.Empty);
                    builder.Append(separator);
                    builder.Append(trimmedValue);
                    separator = ",";
                }

                sb.Append(builder);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        public static string GetCanonicalizedResource(Uri address, string accountName)
        {
            StringBuilder str = new StringBuilder();
            StringBuilder builder = new StringBuilder("/");
            builder.Append(accountName);
            builder.Append(address.AbsolutePath);
            str.Append(builder);
            Dictionary<string, HashSet<string>> queryKeyValues = ExtractQueryKeyValues(address);
            Dictionary<string, HashSet<string>> dictionary = GetCommaSeparatedList(queryKeyValues);

            foreach (KeyValuePair<string, HashSet<string>> pair in dictionary.OrderBy(p => p.Key))
            {
                StringBuilder stringBuilder = new StringBuilder(string.Empty);
                stringBuilder.Append(pair.Key + ":");
                string commaList = string.Join(",", pair.Value);
                stringBuilder.Append(commaList);
                str.Append("\n");
                str.Append(stringBuilder);
            }

            return str.ToString();
        }

        public static List<string> GetHeaderValues(HttpRequestHeaders headers, string headerName)
        {
            List<string> list = new List<string>();
            headers.TryGetValues(headerName, out IEnumerable<string> values);
            if (values != null)
            {
                list.Add((values.FirstOrDefault() ?? string.Empty).TrimStart(null));
            }

            return list;
        }

        private static bool IsWithinRetryRange(HttpStatusCode statusCode)
        {
            // Retry on http client and server error codes (4xx - 5xx) as well as redirect

            var rawStatus = (int)statusCode;
            if (rawStatus == 302)
                return true;
            else if (rawStatus >= 400 && rawStatus <= 599)
                return true;
            else
                return false;
        }

        public static async Task<HttpResponseMessage> RequestWithRetry(TaskLoggingHelper loggingHelper, HttpClient client,
            Func<HttpRequestMessage> createRequest, Func<HttpResponseMessage, bool> validationCallback = null, int retryCount = 5,
            int retryDelaySeconds = 5, string url="")
        {
            if (loggingHelper == null)
                throw new ArgumentNullException(nameof(loggingHelper));
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            if (createRequest == null)
                throw new ArgumentNullException(nameof(createRequest));
            if (retryCount < 1)
                throw new ArgumentException(nameof(retryCount));
            if (retryDelaySeconds < 1)
                throw new ArgumentException(nameof(retryDelaySeconds));

            int retries = 0;
            HttpResponseMessage response = null;

            // add a bit of randomness to the retry delay
            var rng = new Random();

            while (retries < retryCount)
            {
                if (retries > 0)
                {
                    if (response != null)
                    {
                        response.Dispose();
                        response = null;
                    }

                    int delay = retryDelaySeconds * retries * rng.Next(1, 5);
                    loggingHelper.LogMessage(MessageImportance.Low, "Waiting {0} seconds before retry", delay);
                    await System.Threading.Tasks.Task.Delay(delay * 1000);
                }

                try
                {
                    using (var request = createRequest())
                        response = await client.SendAsync(request);
                }
                catch (Exception e)
                {
                    loggingHelper.LogWarningFromException(e, true);

                    // if this is the final iteration let the exception bubble up
                    if (retries + 1 == retryCount)
                        throw;
                }

                // response can be null if we fail to send the request
                if (response != null)
                {
                    if (validationCallback == null)
                    {
                        // check if the response code is within the range of failures
                        if (!IsWithinRetryRange(response.StatusCode))
                        {
                            return response;
                        }
                    }
                    else
                    {
                        bool isSuccess = validationCallback(response);
                        if (!isSuccess)
                        {
                            loggingHelper.LogMessage("Validation callback returned retry for status code {0}", response.StatusCode);
                        }
                        else
                        {
                            loggingHelper.LogMessage("Validation callback returned success for status code {0}", response.StatusCode);
                            return response;
                        }
                    }
                }

                ++retries;
            }

            // retry count exceeded
            loggingHelper.LogWarning("Retry count {0} exceeded", retryCount);

            // set some default values in case response is null
            var statusCode = "None";
            var contentStr = "Null";
            if (response != null)
            {
                statusCode = response.StatusCode.ToString();
                contentStr = await response.Content.ReadAsStringAsync();
                response.Dispose();
            }

            throw new HttpRequestException(string.Format("Request failed with status {0} response {1}", statusCode, contentStr));
        }

        private static string ConstructServiceStringToSign(
            string signedPermissions,
            string signedVersion,
            string signedExpiry,
            string canonicalizedResource,
            string signedIdentifier,
            string signedStart,
            string signedIP = "",
            string signedProtocol = "",
            string rscc = "",
            string rscd = "",
            string rsce = "",
            string rscl = "",
            string rsct = "")
        {
            // constructing string to sign based on spec in https://msdn.microsoft.com/en-us/library/azure/dn140255.aspx
            var stringToSign = string.Join(
                "\n",
                signedPermissions,
                signedStart,
                signedExpiry,
                canonicalizedResource,
                signedIdentifier,
                signedIP,
                signedProtocol,
                signedVersion,
                rscc,
                rscd,
                rsce,
                rscl,
                rsct);
            return stringToSign;
        }

        private static Dictionary<string, HashSet<string>> ExtractQueryKeyValues(Uri address)
        {
            Dictionary<string, HashSet<string>> values = new Dictionary<string, HashSet<string>>();
            //Decode this to allow the regex to pull out the correct groups for signing
            address = new Uri(WebUtility.UrlDecode(address.ToString()));
            Regex newreg = new Regex(@"(?:\?|&)([^=]+)=([^&]+)");
            MatchCollection matches = newreg.Matches(address.Query);
            foreach (Match match in matches)
            {
                string key, value;
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    key = match.Groups[1].Value;
                    value = match.Groups[2].Value;
                }
                else
                {
                    key = match.Groups[3].Value;
                    value = match.Groups[4].Value;
                }

                if (values.TryGetValue(key, out HashSet<string> setOfValues))
                {
                    setOfValues.Add(value);
                }
                else
                {
                    HashSet<string> newSet = new HashSet<string> { value };
                    values.Add(key, newSet);
                }
            }

            return values;
        }

        private static Dictionary<string, HashSet<string>> GetCommaSeparatedList(
            Dictionary<string, HashSet<string>> queryKeyValues)
        {
            Dictionary<string, HashSet<string>> dictionary = new Dictionary<string, HashSet<string>>();

            foreach (string queryKeys in queryKeyValues.Keys)
            {
                queryKeyValues.TryGetValue(queryKeys, out HashSet<string> setOfValues);
                List<string> list = new List<string>();
                list.AddRange(setOfValues);
                list.Sort();
                string commaSeparatedValues = string.Join(",", list);
                string key = queryKeys.ToLowerInvariant();
                if (dictionary.TryGetValue(key, out HashSet<string> setOfValues2))
                {
                    setOfValues2.Add(commaSeparatedValues);
                }
                else
                {
                    HashSet<string> newSet = new HashSet<string> { commaSeparatedValues };
                    dictionary.Add(key, newSet);
                }
            }

            return dictionary;
        }
        // This is duplicated code from https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.Build.CloudTestTasks/AzureConnectionStringBuildTask.cs
        // when we move "ListAzureBlobs.cs" into buildtools, we should eliminate this duplication.
        public static Tuple<string, string> ParseConnectionString(string connectionString)
        {
            Regex storageConnectionStringRegex = new Regex("AccountName=(?<name>.+?);AccountKey=(?<key>.+?);");

            MatchCollection matches = storageConnectionStringRegex.Matches(connectionString);
            if (matches.Count > 0)
            {
                return new Tuple<string, string>(matches[0].Groups["name"].Value, matches[0].Groups["key"].Value);
            }
            else
            {
                return null;
            }
        }

        public static Func<HttpRequestMessage> RequestMessage(string method, string url, string accountName, string accountKey, List<Tuple<string, string>> additionalHeaders = null, string body = null)
        {
            Func<HttpRequestMessage> requestFunc = () =>
            {
                HttpMethod httpMethod = HttpMethod.Get;
                if(method == "PUT")
                {
                    httpMethod = HttpMethod.Put;
                }
                else if (method == "DELETE")
                {
                    httpMethod = HttpMethod.Delete;
                }
                DateTime dateTime = DateTime.UtcNow;
                var request = new HttpRequestMessage(httpMethod, url);
                request.Headers.Add(AzureHelper.DateHeaderString, dateTime.ToString("R", CultureInfo.InvariantCulture));
                request.Headers.Add(AzureHelper.VersionHeaderString, AzureHelper.StorageApiVersion);
                if (additionalHeaders != null)
                {
                    foreach (Tuple<string, string> additionalHeader in additionalHeaders)
                    {
                        request.Headers.Add(additionalHeader.Item1, additionalHeader.Item2);
                    }
                }
                if (body != null)
                {
                    request.Content = new StringContent(body);
                    request.Headers.Add(AzureHelper.AuthorizationHeaderString, AzureHelper.AuthorizationHeader(
                        accountName,
                        accountKey,
                        method,
                        dateTime,
                        request,
                        "",
                        "",
                        request.Content.Headers.ContentLength.ToString(),
                        request.Content.Headers.ContentType.ToString()));
                }
                else
                {
                    request.Headers.Add(AzureHelper.AuthorizationHeaderString, AzureHelper.AuthorizationHeader(
                        accountName,
                        accountKey,
                        method,
                        dateTime,
                        request));
                }
                return request;
            };
            return requestFunc;
        }

        public static string GetBlobRestUrl(string accountName, string containerName, string blob)
        {
            return $"https://{accountName}.blob.core.windows.net/{containerName}/{blob}";
        }
    }
}
