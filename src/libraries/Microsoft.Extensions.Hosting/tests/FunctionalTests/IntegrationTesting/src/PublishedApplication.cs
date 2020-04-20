// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public class PublishedApplication: IDisposable
    {
        private readonly ILogger _logger;

        public string Path { get; }

        public PublishedApplication(string path, ILogger logger)
        {
            _logger = logger;
            Path = path;
        }

        public void Dispose()
        {
            RetryHelper.RetryOperation(
                () => Directory.Delete(Path, true),
                e => _logger.LogWarning($"Failed to delete directory : {e.Message}"),
                retryCount: 3,
                retryDelayMilliseconds: 100);
        }
    }
}