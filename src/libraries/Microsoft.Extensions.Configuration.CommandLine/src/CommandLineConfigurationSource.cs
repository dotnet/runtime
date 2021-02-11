// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.CommandLine
{
    /// <summary>
    /// Represents command line arguments as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class CommandLineConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Gets or sets the switch mappings.
        /// </summary>
        public IDictionary<string, string> SwitchMappings { get; set; }

        /// <summary>
        /// Gets or sets the command line args.
        /// </summary>
        public IEnumerable<string> Args { get; set; }

        /// <summary>
        /// Builds the <see cref="CommandLineConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="CommandLineConfigurationProvider"/></returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new CommandLineConfigurationProvider(Args, SwitchMappings);
        }
    }
}
