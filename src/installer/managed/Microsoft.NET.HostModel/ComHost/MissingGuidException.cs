// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.NET.HostModel.ComHost
{
    /// <summary>
    /// The type <see cref="TypeName"/> is public and ComVisible but does not have a <see cref="System.Runtime.InteropServices.GuidAttribute"/> attribute.
    /// </summary>
    public class MissingGuidException : Exception
    {
        public MissingGuidException(string typeName)
        {
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }
            TypeName = typeName;
        }

        public string TypeName { get; }
    }
}
