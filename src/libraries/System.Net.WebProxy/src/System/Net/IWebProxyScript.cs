// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    /// <summary>
    /// Provides the base interface to load and execute scripts for automatic proxy detection.
    /// </summary>
    public interface IWebProxyScript
    {
        /// <summary>
        /// Closes a script.
        /// </summary>
        void Close();

        /// <summary>
        /// Loads a script.
        /// </summary>
        /// <param name="scriptLocation">The URI that identifies the location of the proxy auto-configuration script.</param>
        /// <param name="script">The script content to load and prepare for execution.</param>
        /// <param name="helperType">The type that provides helper methods or services available to the script at runtime.</param>
        /// <returns>A <see cref="bool"/> indicating whether the script was successfully loaded.</returns>
        bool Load(Uri scriptLocation, string script, Type helperType);

        /// <summary>
        /// Runs a script.
        /// </summary>
        /// <param name="url">The destination URL for which proxy information is requested.</param>
        /// <param name="host">The host name associated with the destination URL.</param>
        /// <returns>
        /// A <see cref="string"/> that describes how to connect to the destination, such as a proxy configuration directive (for example, <c>"DIRECT"</c> or <c>"PROXY host:port"</c>).
        /// </returns>
        string Run(string url, string host);
    }
}
