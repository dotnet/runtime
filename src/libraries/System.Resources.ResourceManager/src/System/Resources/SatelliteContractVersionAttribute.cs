// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Resources
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class SatelliteContractVersionAttribute : Attribute
    {
        private readonly string _version;

        public SatelliteContractVersionAttribute(string version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            _version = version;
        }

        public string Version
        {
            get { return _version; }
        }
    }
}
