// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Internal
{
    internal static partial class Requires
    {
        [DebuggerStepThrough]
        [ContractArgumentValidator]
        public static void NotNull<T>(T value, string parameterName)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            Contract.EndContractBlock();
        }

        [DebuggerStepThrough]
        [ContractArgumentValidator]
        public static void NotNullOrEmpty(string value, string parameterName)
        {
            NotNull(value, parameterName);

            if (value.Length == 0)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CommonStrings.ArgumentException_EmptyString, parameterName), parameterName);
            }
            Contract.EndContractBlock();
        }
    }
}
