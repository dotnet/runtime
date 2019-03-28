// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    /// <summary>
    /// Result of a deployment.
    /// </summary>
    public class DeploymentResult
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// The folder where the application is hosted. This path can be different from the
        /// original application source location if published before deployment.
        /// </summary>
        public string ContentRoot { get; }

        /// <summary>
        /// Original deployment parameters used for this deployment.
        /// </summary>
        public DeploymentParameters DeploymentParameters { get; }

        /// <summary>
        /// Triggered when the host process dies or pulled down.
        /// </summary>
        public CancellationToken HostShutdownToken { get; }

        public DeploymentResult(ILoggerFactory loggerFactory, DeploymentParameters deploymentParameters)
            : this(loggerFactory, deploymentParameters: deploymentParameters, contentRoot: string.Empty, hostShutdownToken: CancellationToken.None)
        { }

        public DeploymentResult(ILoggerFactory loggerFactory, DeploymentParameters deploymentParameters, string contentRoot, CancellationToken hostShutdownToken)
        {
            _loggerFactory = loggerFactory;

            ContentRoot = contentRoot;
            DeploymentParameters = deploymentParameters;
            HostShutdownToken = hostShutdownToken;
        }
    }
}
