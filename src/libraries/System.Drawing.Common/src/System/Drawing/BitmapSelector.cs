// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;

namespace System.Drawing
{
    /// <summary>
    /// Provides methods to select bitmaps.
    /// </summary>
    internal static class BitmapSelector
    {
        /// <summary>
        /// Returns a resource stream loaded from the appropriate location according to the current
        /// suffix.
        /// </summary>
        /// <param name="assembly">The assembly from which the stream is loaded</param>
        /// <param name="type">The type whose namespace is used to scope the manifest resource name</param>
        /// <param name="originalName">The name of the manifest resource being requested</param>
        /// <returns>
        /// The manifest resource stream corresponding to <paramref name="originalName"/>.
        /// </returns>
        public static Stream? GetResourceStream(Assembly assembly, Type type, string originalName)
        {
            return assembly.GetManifestResourceStream(type, originalName);
        }

        /// <summary>
        /// Returns a resource stream loaded from the appropriate location according to the current
        /// suffix.
        /// </summary>
        /// <param name="type">The type from whose assembly the stream is loaded and whose namespace is used to scope the resource name</param>
        /// <param name="originalName">The name of the manifest resource being requested</param>
        /// <returns>
        /// The manifest resource stream corresponding to <paramref name="originalName"/>.
        /// </returns>
        public static Stream? GetResourceStream(Type type, string originalName)
        {
            return GetResourceStream(type.Module.Assembly, type, originalName);
        }
    }
}
