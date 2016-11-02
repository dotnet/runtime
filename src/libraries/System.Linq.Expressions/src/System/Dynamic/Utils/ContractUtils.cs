// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Dynamic.Utils
{
    internal static partial class ContractUtils
    {
        public static void Requires(bool precondition)
        {
            if (!precondition)
            {
                throw new ArgumentException(System.Linq.Expressions.Strings.MethodPreconditionViolated);
            }
        }
    }
}
