// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Composition.TypedParts.ActivationFeatures
{
    /// <summary>
    /// Represents a part property that is configured as an import.
    /// </summary>
    internal class PropertyImportSite
    {
        private readonly PropertyInfo _pi;

        public PropertyImportSite(PropertyInfo pi)
        {
            _pi = pi;
        }

        public PropertyInfo Property { get { return _pi; } }

        public override string ToString()
        {
            return _pi.Name;
        }
    }
}
