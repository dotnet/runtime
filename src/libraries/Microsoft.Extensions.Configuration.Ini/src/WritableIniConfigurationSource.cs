// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.Configuration.Ini
{
    /// <summary>
    /// An <see cref="IConfigurationSource"/> backed by a writable INI file at a
    /// physical path. Values are exposed as <c>Section:Key</c> configuration keys
    /// (e.g. <c>C64SC:VICIIModel</c>), matching the built-in INI provider, and the
    /// matching <see cref="WritableIniConfigurationProvider"/> can write changes
    /// back losslessly via <see cref="WritableIniConfigurationProvider.Save"/>.
    /// </summary>
    public sealed class WritableIniConfigurationSource : IConfigurationSource
    {
        /// <summary>The physical path of the INI file to read from and write to.</summary>
        public required string Path { get; set; }

        /// <summary>
        /// When true (default) a missing file yields empty configuration instead of
        /// throwing on <see cref="WritableIniConfigurationProvider.Load"/>.
        /// </summary>
        public bool Optional { get; set; } = true;

        /// <summary>Builds the provider for this source.</summary>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => new WritableIniConfigurationProvider(this);
    }
}
