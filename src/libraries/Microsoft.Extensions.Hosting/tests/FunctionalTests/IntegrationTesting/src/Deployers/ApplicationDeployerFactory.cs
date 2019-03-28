// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    /// <summary>
    /// Factory to create an appropriate deployer based on <see cref="DeploymentParameters"/>.
    /// </summary>
    public class ApplicationDeployerFactory
    {
        /// <summary>
        /// Creates a deployer instance based on settings in <see cref="DeploymentParameters"/>.
        /// </summary>
        /// <param name="deploymentParameters"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public static ApplicationDeployer Create(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
        {
            if (deploymentParameters == null)
            {
                throw new ArgumentNullException(nameof(deploymentParameters));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            return new SelfHostDeployer(deploymentParameters, loggerFactory);
        }
    }
}
