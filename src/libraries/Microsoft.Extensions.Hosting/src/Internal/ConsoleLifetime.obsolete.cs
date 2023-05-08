// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class ConsoleLifetime : IHostLifetime
    {
        [Obsolete("IHostingEnvironment and IApplicationLifetime have been deprecated. Use Microsoft.Extensions.Hosting.IHostEnvironment and IHostApplicationLifetime instead.")]
        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostingEnvironment environment, IApplicationLifetime applicationLifetime)
            : this (options, new HostingEnvironmentAdapter(environment), new ApplicationLifetimeAdapter(applicationLifetime), new OptionsWrapper<HostOptions>(new HostOptions())) { }

        #pragma warning disable CS0618
        private struct HostingEnvironmentAdapter : IHostEnvironment
        {
            private IHostingEnvironment _hostingEnvironment;
            public HostingEnvironmentAdapter(IHostingEnvironment hostingEnvironment)
            {
                _hostingEnvironment = hostingEnvironment;
            }

            public string EnvironmentName
            {
                get => _hostingEnvironment.EnvironmentName;
                set => _hostingEnvironment.EnvironmentName = value;
            }
            public string ApplicationName
            {
                get => _hostingEnvironment.ApplicationName;
                set => _hostingEnvironment.ApplicationName = value;
            }
            public string ContentRootPath
            {
                get => _hostingEnvironment.ContentRootPath;
                set => _hostingEnvironment.ContentRootPath = value;
            }
            public IFileProvider ContentRootFileProvider
            {
                get => _hostingEnvironment.ContentRootFileProvider;

                set => _hostingEnvironment.ContentRootFileProvider = value;
            }
        }

        private struct ApplicationLifetimeAdapter : IHostApplicationLifetime
        {
            private IApplicationLifetime _applicationLifetime;

            public ApplicationLifetimeAdapter(IApplicationLifetime applicationLifetime)
            {
                _applicationLifetime = applicationLifetime;
            }

            public CancellationToken ApplicationStarted { get => _applicationLifetime.ApplicationStarted; }
            public CancellationToken ApplicationStopping { get => _applicationLifetime.ApplicationStopping; }
            public CancellationToken ApplicationStopped { get => _applicationLifetime.ApplicationStopped; }
            public void StopApplication() => _applicationLifetime.StopApplication();
        }
        #pragma warning restore CS0618

    }
}
