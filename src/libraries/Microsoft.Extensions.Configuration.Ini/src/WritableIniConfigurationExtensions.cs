// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration.Ini;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for adding a writable INI configuration source.
    /// </summary>
    public static class WritableIniConfigurationExtensions
    {
        /// <summary>
        /// Adds a writable INI file at <paramref name="path"/> to
        /// <paramref name="builder"/>. The source resolves to a
        /// <see cref="Ini.WritableIniConfigurationProvider"/> whose
        /// <see cref="Ini.WritableIniConfigurationProvider.Save"/> writes changes
        /// back losslessly.
        /// </summary>
        /// <param name="builder">The builder to add to.</param>
        /// <param name="path">The physical path of the INI file.</param>
        /// <param name="optional">
        /// When true (default) a missing file yields empty configuration; when false
        /// loading a missing file throws.
        /// </param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IConfigurationBuilder AddWritableIniFile(
            this IConfigurationBuilder builder, string path, bool optional = true)
        {
            ArgumentNullException.ThrowIfNull(builder);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException(SR.Error_InvalidFilePath, nameof(path));

            return builder.Add(new Ini.WritableIniConfigurationSource { Path = path, Optional = optional });
        }
    }
}
