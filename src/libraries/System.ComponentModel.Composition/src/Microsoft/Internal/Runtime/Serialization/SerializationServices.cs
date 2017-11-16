// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Internal.Runtime.Serialization
{
#if FEATURE_SERIALIZATION
    internal static class SerializationServices
    {
        public static T GetValue<T>(this SerializationInfo info, string name)
        {
            Assumes.NotNull(info, name);

            return (T)info.GetValue(name, typeof(T));
        }
    }
#endif //FEATURE_SERIALIZATION
}