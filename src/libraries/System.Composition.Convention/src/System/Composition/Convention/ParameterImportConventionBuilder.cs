// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Composition.Convention
{
    /// <summary>
    /// Used when configuring a <see cref="PartConventionBuilder{T}"/>. Used only
    /// in expressions, instances of this type are never created.
    /// </summary>
    public abstract class ParameterImportConventionBuilder
    {
        private ParameterImportConventionBuilder() { }

        /// <summary>
        /// Import with contract <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <returns>The imported instance.</returns>
        public T Import<T>()
        {
            return default;
        }

        /// <summary>
        /// Import with contract <typeparamref name="T"/>.
        /// </summary>
        /// <param name="configure">Configuration for the import.</param>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <returns>The imported instance.</returns>
        public T Import<T>(Action<ImportConventionBuilder> configure)
        {
            return default;
        }
    }
}
