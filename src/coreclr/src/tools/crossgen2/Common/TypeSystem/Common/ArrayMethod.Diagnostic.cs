// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    public partial class ArrayMethod
    {
        public override string DiagnosticName
        {
            get
            {
                // The ArrayMethod.Name property is gauranteed to not throw
                return Name;
            }
        }
    }
}
