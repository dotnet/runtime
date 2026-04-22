// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration
{
    // Test-only compatibility shim mapping the legacy EnableReferenceResolution / ConfigureReferenceResolution
    // names to SetReferenceMode. Keeps the large test suite compiling without per-call rewrites.
    internal static class ReferenceResolutionTestShims
    {
        public static IConfigurationBuilder EnableReferenceResolution(this IConfigurationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Apply Scan only to sources that haven't already been assigned a mode, so earlier
            // ConfigureReferenceResolution(mode) / ConfigureReferenceResolution(source, mode) calls
            // remain in effect.
            const string SourceModesPropertyName = "Microsoft.Extensions.Configuration.ReferenceResolution.SourceModes";
            var existing = builder.Properties.TryGetValue(SourceModesPropertyName, out object? raw)
                ? raw as System.Collections.IDictionary
                : null;
            foreach (IConfigurationSource source in builder.Sources)
            {
                if (existing is null || !existing.Contains(source))
                {
                    builder.SetReferenceMode(source, ReferenceMode.Scan);
                }
            }
            return builder;
        }

        // Marks every currently-added source as Scan. Tests that use this pair it with the most-recently
        // added source; since SetReferenceMode(source, ...) is used where targeting is needed, the
        // default overload marks all sources as Scan.
        public static IConfigurationBuilder ConfigureReferenceResolution(this IConfigurationBuilder builder, ReferenceMode mode)
        {
            ArgumentNullException.ThrowIfNull(builder);
            if (builder.Sources.Count == 0)
            {
                throw new InvalidOperationException("No sources.");
            }
            return builder.SetReferenceMode(builder.Sources[builder.Sources.Count - 1], mode);
        }

        public static IConfigurationBuilder ConfigureReferenceResolution(this IConfigurationBuilder builder, IConfigurationSource source, ReferenceMode mode)
            => builder.SetReferenceMode(source, mode);
    }
}
