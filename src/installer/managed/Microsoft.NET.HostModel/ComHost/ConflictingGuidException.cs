// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.NET.HostModel.ComHost
{
    /// <summary>
    /// The same Guid has been specified for two public ComVisible classes in the assembly.
    /// </summary>
    public class ConflictingGuidException : Exception
    {
        public ConflictingGuidException(string typeName1, string typeName2, Guid guid)
        {
            if (typeName1 is null)
            {
                throw new ArgumentNullException(nameof(typeName1));
            }
            if (typeName2 is null)
            {
                throw new ArgumentNullException(nameof(typeName2));
            }
            TypeName1 = typeName1;
            TypeName2 = typeName2;
            Guid = guid;
        }

        public string TypeName1 { get; }
        public string TypeName2 { get; }
        public Guid Guid { get; }
    }
}
