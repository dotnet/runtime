// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Helper class.
    /// </summary>
    public static class Options
    {
        // By default, we're going to keep public, parameterless constructor on any Options class.
        internal const DynamicallyAccessedMemberTypes DynamicallyAccessedMembers = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

        /// <summary>
        /// The default name used for options instances: "".
        /// </summary>
        public static readonly string DefaultName = string.Empty;

        /// <summary>
        /// Creates a wrapper around an instance of <typeparamref name="TOptions"/> to return itself as an <see cref="IOptions{TOptions}"/>.
        /// </summary>
        /// <typeparam name="TOptions">Options type.</typeparam>
        /// <param name="options">Options object.</param>
        /// <returns>Wrapped options object.</returns>
        public static IOptions<TOptions> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMembers)] TOptions>(TOptions options)
            where TOptions : class
        {
            return new OptionsWrapper<TOptions>(options);
        }
    }
}
