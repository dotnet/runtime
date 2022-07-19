// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Context containing the common services on the <see cref="IHost" />. Some properties may be null until set by the <see cref="IHost" />.
    /// </summary>
    public class HostBuilderContext
    {
        public HostBuilderContext(IDictionary<object, object> properties)
        {
            ThrowHelper.ThrowIfNull(properties);

            Properties = properties;
        }

        /// <summary>
        /// The <see cref="IHostEnvironment" /> initialized by the <see cref="IHost" />.
        /// </summary>
        public IHostEnvironment HostingEnvironment { get; set; } = null!;

        /// <summary>
        /// The <see cref="IConfiguration" /> containing the merged configuration of the application and the <see cref="IHost" />.
        /// </summary>
        public IConfiguration Configuration { get; set; } = null!;

        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        public IDictionary<object, object> Properties { get; }
    }
}
