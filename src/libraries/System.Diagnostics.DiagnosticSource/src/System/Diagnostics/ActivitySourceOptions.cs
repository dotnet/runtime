// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// Options for creating a <see cref="ActivitySource"/>.
    /// </summary>
    public class ActivitySourceOptions
    {
        private string _name;

        /// <summary>
        /// Constructs a new instance of <see cref="ActivitySourceOptions"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="ActivitySourceOptions"/> object</param>
        public ActivitySourceOptions(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Get or set the <see cref="ActivitySourceOptions"/> object name. Cannot be null.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The optional <see cref="ActivitySourceOptions"/> version. Defaulted to empty string.
        /// </summary>
        public string? Version { get; set; } = string.Empty;

        /// <summary>
        /// The optional list of key-value pair tags associated with the <see cref="ActivitySourceOptions"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>>? Tags { get; set; }

        /// <summary>
        /// The optional schema URL specifies a location of a <see href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/schemas/file_format_v1.1.0.md">Schema File</see> that
        /// can be retrieved using HTTP or HTTPS protocol.
        /// </summary>
        public string? TelemetrySchemaUrl { get; set; }
    }
}
