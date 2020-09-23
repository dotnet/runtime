// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;


namespace HttpStress
{
    [Flags]
    public enum RunMode { server = 1, client = 2, both = server | client };

    public class Configuration
    {
        public string ServerUri { get; set; } = "";
        public RunMode RunMode { get; set; }
        public bool ListOperations { get; set; }

        public Version HttpVersion { get; set; } = new Version();
        public bool UseWinHttpHandler { get; set; }
        public int ConcurrentRequests { get; set; }
        public int RandomSeed { get; set; }
        public int MaxContentLength { get; set; }
        public int MaxRequestUriSize { get; set; }
        public int MaxRequestHeaderCount { get; set; }
        public int MaxRequestHeaderTotalSize { get; set; }
        public int MaxParameters { get; set; }
        public int[]? OpIndices { get; set; }
        public int[]? ExcludedOpIndices { get; set; }
        public TimeSpan DisplayInterval { get; set; }
        public TimeSpan DefaultTimeout { get; set; }
        public TimeSpan? ConnectionLifetime { get; set; }
        public TimeSpan? MaximumExecutionTime { get; set; }
        public double CancellationProbability { get; set; }

        public bool UseHttpSys { get; set; }
        public bool LogAspNet { get; set; }
        public bool Trace { get; set; }
        public int? ServerMaxConcurrentStreams { get; set; }
        public int? ServerMaxFrameSize { get; set; }
        public int? ServerInitialConnectionWindowSize { get; set; }
        public int? ServerMaxRequestHeaderFieldSize { get; set; }
    }

}
