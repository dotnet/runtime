// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    partial class ParameterizedType
    {
        public sealed override bool IsRuntimeDeterminedSubtype
        {
            get
            {
                return _parameterType.IsRuntimeDeterminedSubtype;
            }
        }
    }
}