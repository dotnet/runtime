// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Collections
{
    /// <summary>
    /// Provides a mechanism for a <see cref="Hashtable"/> user to override the default
    /// GetHashCode() function on Objects, providing their own hash function.
    /// </summary>
    [Obsolete("Please use IEqualityComparer instead.")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public interface IHashCodeProvider
    {
        /// <summary>Returns a hash code for the given object.</summary>
        int GetHashCode(object obj);
    }
}
