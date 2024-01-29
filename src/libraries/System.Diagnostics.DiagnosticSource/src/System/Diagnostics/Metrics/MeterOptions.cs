// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Options for creating a <see cref="Meter"/>.
    /// </summary>
    public class MeterOptions
    {
        private string _name;

        /// <summary>
        /// The Meter name.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _name = value;
            }
        }

        /// <summary>
        /// The optional Meter version.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// The optional list of key-value pair tags associated with the Meter.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>>? Tags { get; set; }

        /// <summary>
        /// The optional opaque object to attach to the Meter. The scope object can be attached to multiple meters for scoping purposes.
        /// </summary>
        public object? Scope { get; set; }

        /// <summary>
        /// Constructs a new instance of <see cref="MeterOptions"/>.
        /// </summary>
        /// <param name="name">The Meter name.</param>
        public MeterOptions(string name)
        {
            Name = name;

            Debug.Assert(_name is not null);
        }
    }
}
