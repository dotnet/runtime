// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
**
** Purpose: Specifies which version of a satellite assembly
**          the ResourceManager should ask for.
**
**
===========================================================*/

using System.ComponentModel;

namespace System.Resources
{
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
