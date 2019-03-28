// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    /// <summary>
    /// Parameters to control application deployment.
    /// </summary>
    public class DeploymentParameters
    {
        public DeploymentParameters()
        {
            var configAttribute = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
            if (configAttribute != null && !string.IsNullOrEmpty(configAttribute.Configuration))
            {
                Configuration = configAttribute.Configuration;
            }
        }

        public DeploymentParameters(TestVariant variant)
        {
            var configAttribute = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
            if (configAttribute != null && !string.IsNullOrEmpty(configAttribute.Configuration))
            {
                Configuration = configAttribute.Configuration;
            }

            TargetFramework = variant.Tfm;
            ApplicationType = variant.ApplicationType;
            RuntimeArchitecture = variant.Architecture;
        }

        /// <summary>
        /// Creates an instance of <see cref="DeploymentParameters"/>.
        /// </summary>
        /// <param name="applicationPath">Source code location of the target location to be deployed.</param>
        /// <param name="runtimeFlavor">Flavor of the clr to run against.</param>
        /// <param name="runtimeArchitecture">Architecture of the runtime to be used.</param>
        public DeploymentParameters(
            string applicationPath,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture)
        {
            if (string.IsNullOrEmpty(applicationPath))
            {
                throw new ArgumentException("Value cannot be null.", nameof(applicationPath));
            }

            if (!Directory.Exists(applicationPath))
            {
                throw new DirectoryNotFoundException(string.Format("Application path {0} does not exist.", applicationPath));
            }

            ApplicationPath = applicationPath;
            ApplicationName = new DirectoryInfo(ApplicationPath).Name;
            RuntimeFlavor = runtimeFlavor;

            var configAttribute = Assembly.GetCallingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
            if (configAttribute != null && !string.IsNullOrEmpty(configAttribute.Configuration))
            {
                Configuration = configAttribute.Configuration;
            }
        }

        public DeploymentParameters(DeploymentParameters parameters)
        {
            foreach (var propertyInfo in typeof(DeploymentParameters).GetProperties())
            {
                if (propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(this, propertyInfo.GetValue(parameters));
                }
            }

            foreach (var kvp in parameters.EnvironmentVariables)
            {
                EnvironmentVariables.Add(kvp);
            }

            foreach (var kvp in parameters.PublishEnvironmentVariables)
            {
                PublishEnvironmentVariables.Add(kvp);
            }
        }

        public ApplicationPublisher ApplicationPublisher { get; set; }

        public RuntimeFlavor RuntimeFlavor { get; set;  }

        public RuntimeArchitecture RuntimeArchitecture { get; set; } = RuntimeArchitecture.x64;

        public string EnvironmentName { get; set; }

        public string ApplicationPath { get; set; }

        /// <summary>
        /// Gets or sets the name of the application. This is used to execute the application when deployed.
        /// Defaults to the file name of <see cref="ApplicationPath"/>.
        /// </summary>
        public string ApplicationName { get; set; }

        public string TargetFramework { get; set; }

        /// <summary>
        /// Configuration under which to build (ex: Release or Debug)
        /// </summary>
        public string Configuration { get; set; } = "Debug";

        /// <summary>
        /// Space separated command line arguments to be passed to dotnet-publish
        /// </summary>
        public string AdditionalPublishParameters { get; set; }

        /// <summary>
        /// To publish the application before deployment.
        /// </summary>
        public bool PublishApplicationBeforeDeployment { get; set; }

        public bool PreservePublishedApplicationForDebugging { get; set; } = false;

        public bool StatusMessagesEnabled { get; set; } = true;

        public ApplicationType ApplicationType { get; set; }

        public string PublishedApplicationRootPath { get; set; }

        /// <summary>
        /// Environment variables to be set before starting the host.
        /// Not applicable for IIS Scenarios.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Environment variables used when invoking dotnet publish.
        /// </summary>
        public IDictionary<string, string> PublishEnvironmentVariables { get; } = new Dictionary<string, string>();

        /// <summary>
        /// For any application level cleanup to be invoked after performing host cleanup.
        /// </summary>
        public Action<DeploymentParameters> UserAdditionalCleanup { get; set; }

        public override string ToString()
        {
            return string.Format(
                    "[Variation] :: Runtime={0}, Arch={1}, Publish={2}",
                    RuntimeFlavor,
                    RuntimeArchitecture,
                    PublishApplicationBeforeDeployment);
        }
    }
}
