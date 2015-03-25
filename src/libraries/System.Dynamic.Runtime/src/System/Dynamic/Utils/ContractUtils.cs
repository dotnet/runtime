// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                throw new ArgumentException(Strings.MethodPreconditionViolated);
            }
        }
    }
}
