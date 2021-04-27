// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL
{
    /// <summary>
    /// Provides IL for <see cref="MethodDesc"/> method bodies either by reading
    /// the IL bytes from the source ECMA-335 assemblies, or through other means.
    /// </summary>
    public abstract class ILProvider
    {
        public abstract MethodIL GetMethodIL(MethodDesc method);
    }
}
