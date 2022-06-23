// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implements System.Type
//

using System;
using Internal.Runtime.Augments;

namespace System
{
    internal static class Helpers
    {
        public static bool TryGetEEType(this Type type, out EETypePtr eeType)
        {
            RuntimeTypeHandle typeHandle = RuntimeAugments.Callbacks.GetTypeHandleIfAvailable(type);
            if (typeHandle.IsNull)
            {
                eeType = default(EETypePtr);
                return false;
            }
            eeType = typeHandle.ToEETypePtr();
            return true;
        }
    }
}
