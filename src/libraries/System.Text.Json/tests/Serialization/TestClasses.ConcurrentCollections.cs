// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace System.Text.Json.Serialization.Tests
{
    public class GenericConcurrentQueuePrivateConstructor<T> : ConcurrentQueue<T>
    {
        private GenericConcurrentQueuePrivateConstructor() { }
    }

    public class GenericConcurrentQueueInternalConstructor<T> : ConcurrentQueue<T>
    {
        internal GenericConcurrentQueueInternalConstructor() { }
    }

    public class GenericConcurrentStackPrivateConstructor<T> : ConcurrentStack<T>
    {
        private GenericConcurrentStackPrivateConstructor() { }
    }

    public class GenericConcurrentStackInternalConstructor<T> : ConcurrentStack<T>
    {
        internal GenericConcurrentStackInternalConstructor() { }
    }
}
