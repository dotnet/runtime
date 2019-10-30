// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Channels
{
    /// <summary>Provides a base class for channels that support reading and writing elements of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Specifies the type of data readable and writable in the channel.</typeparam>
    public abstract class Channel<T> : Channel<T, T> { }
}
