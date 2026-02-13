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
        /// <param name="scriptLocation">Internal only.</param>
        /// <param name="script">Internal only.</param>
        /// <param name="helperType">Internal only.</param>
        /// <returns>A <see cref="bool"/> indicating whether the script was successfully loaded.</returns>
        bool Load(Uri scriptLocation, string script, Type helperType);

        /// <summary>
        /// Runs a script.
        /// </summary>
        /// <param name="url">Internal only.</param>
        /// <param name="host">Internal only.</param>
        /// <returns>
        /// A <see cref="string"/>.
        /// An internal-only value returned.
        /// </returns>
        /// <remarks>
        /// <para>
        /// When the <see cref="System.Net.HttpWebRequest"/> object is run, it may need to run the WPAD (Web Proxy Automatic Detection) protocol to detect whether a proxy is required for reaching the destination URL.
        /// During this process, the system downloads and compiles the PAC (Proxy Auto-Configuration) script in memory and tries to execute the FindProxyForURL function as per the PAC specification.
        /// </para>
        /// <para>
        /// When doing so, the system creates an internal application domain inside the application which runs with minimal permissions, and, most importantly, it does not grant the UI permission to this new application domain.
        /// The evaluation of a proxy and running the FindProxyForURL javascript function happens in the context of this new application domain and during this process the system may need to run several helper functions as per the PAC specification.
        /// </para>
        /// </remarks>
        string Run(string url, string host);
    }
}
