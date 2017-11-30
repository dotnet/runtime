// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.Internal
{
    internal static class LazyServices
    {
        public static Lazy<T> AsLazy<T>(this T t)
            where T : class
        {
            return new Lazy<T>(() => t, LazyThreadSafetyMode.PublicationOnly);
        }
    }
}
