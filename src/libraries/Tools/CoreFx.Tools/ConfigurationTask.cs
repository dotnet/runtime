// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public abstract partial class ConfigurationTask : BuildTask
    {
        /// <summary>
        /// List of properties in order of precedence.
        /// Metadata:
        ///   DefaultValue: default value for the property.  Default values may be omitted from configuration strings.
        ///   Precedence: integer indicating selection precedence.
        ///   Order: integer indicating configuration string ordering.
        /// </summary>
        [Required]
        public ITaskItem[] Properties { get; set; }

        /// <summary>
        /// Relations between property values.
        /// 
        /// Identity: PropertyValue
        /// Metadata: 
        ///   Property: Name of property to which this value applies
        ///   Imports: List of other property values to consider, in breadth first order, after this value.
        ///   CompatibleWith: List of additional property values to consider, after all imports have been considered.
        ///   Each value will independently undergo a breadth-first traversal of imports.
        ///   Other values: Properties to be set when this configuration property is set.
        /// </summary>
        [Required]
        public ITaskItem[] PropertyValues { get; set; }


        protected void LoadConfiguration()
        {
            ConfigurationFactory = new ConfigurationFactory(Properties, PropertyValues);
        }

        public ConfigurationFactory ConfigurationFactory { get; private set; }
    }
}
