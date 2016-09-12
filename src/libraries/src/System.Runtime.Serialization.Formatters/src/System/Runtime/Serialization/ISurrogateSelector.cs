// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    public interface ISurrogateSelector
    {
        void ChainSelector(ISurrogateSelector selector);
        ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector);
        ISurrogateSelector GetNextSelector();
    }
}
