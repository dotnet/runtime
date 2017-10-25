// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Threading.Channels.Tests
{
    internal static class TestExtensions
    {
        public static async ValueTask<T> ReadAsync<T>(this ChannelReader<T> reader, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                while (true)
                {
                    if (!await reader.WaitToReadAsync(cancellationToken))
                    {
                        throw new ChannelClosedException();
                    }

                    if (reader.TryRead(out T item))
                    {
                        return item;
                    }
                }
            }
            catch (Exception exc) when (!(exc is ChannelClosedException))
            {
                throw new ChannelClosedException(exc);
            }
        }
    }
}
