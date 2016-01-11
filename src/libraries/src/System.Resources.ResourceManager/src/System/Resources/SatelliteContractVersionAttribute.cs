// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                throw new ArgumentNullException("version");
            }

            _version = version;
        }

        public string Version
        {
            get { return _version; }
        }
    }
}
