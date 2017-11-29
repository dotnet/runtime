// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition.Primitives;

namespace Microsoft.Internal
{
    internal static class ContractServices
    {
        public static bool TryCast(Type contractType, object value, out object result)
        {
            if (value == null)
            {
                result = null;
                return true;
            }
            if (contractType.IsInstanceOfType(value))
            {
                result = value;
                return true;
            }

            // We couldn't cast see if a delegate works for us.
            if (typeof(Delegate).IsAssignableFrom(contractType))
            {
                ExportedDelegate exportedDelegate = value as ExportedDelegate;
                if (exportedDelegate != null)
                {
                    result = exportedDelegate.CreateDelegate(contractType.UnderlyingSystemType);
                    return (result != null);
                }
            }

            result = null;
            return false;
        }
    }
}

