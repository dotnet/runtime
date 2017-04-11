// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace System.Resources
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class SatelliteContractVersionAttribute : Attribute
    {
        public SatelliteContractVersionAttribute(String version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));
            Version = version;
        }

        public String Version { get; }
    }
}
