// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Composition.Hosting.Util
{
    internal static class MethodInfoExtensions
    {
        public static T CreateStaticDelegate<T>(this MethodInfo methodInfo)
        {
            return (T)(object)methodInfo.CreateDelegate(typeof(T));
        }
    }
}
