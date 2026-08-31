// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Resources
{
    /// <summary>
    /// Instructs a <see cref="ResourceManager" /> object to ask for a particular version of a satellite assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class SatelliteContractVersionAttribute : Attribute
    {
        public SatelliteContractVersionAttribute(string version)
        {
            ArgumentNullException.ThrowIfNull(version);

            Version = version;
        }

        public string Version { get; }
    }
}
