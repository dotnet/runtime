// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>
        /// Reads from both standard output and standard error pipes using async IO with tasks.
        /// On Windows, the underlying handles are opened for async IO, so async reads can be cancelled.
        /// </summary>
        private static partial void ReadBothPipes(
            SafeFileHandle outputHandle,
            SafeFileHandle errorHandle,
            int timeoutMs,
            ref byte[] outputBuffer,
            ref int outputBytesRead,
            ref byte[] errorBuffer,
            ref int errorBytesRead)
        {
            CancellationTokenSource cts = timeoutMs >= 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            CancellationToken cancellationToken = cts.Token;

            Task<int> outputTask = RandomAccess.ReadAsync(outputHandle, outputBuffer.AsMemory(outputBytesRead), fileOffset: -1, cancellationToken).AsTask();
            Task<int> errorTask = RandomAccess.ReadAsync(errorHandle, errorBuffer.AsMemory(errorBytesRead), fileOffset: -1, cancellationToken).AsTask();

            bool outputDone = false;
            bool errorDone = false;

            try
            {
                while (!outputDone || !errorDone)
                {
                    bool isError;
                    int bytesRead;

                    if (outputDone)
                    {
                        bytesRead = errorTask.GetAwaiter().GetResult();
                        isError = true;
                    }
                    else if (errorDone)
                    {
                        bytesRead = outputTask.GetAwaiter().GetResult();
                        isError = false;
                    }
                    else
                    {
#pragma warning disable CA2025 // Tasks complete or are cancelled before CTS disposal
                        Task<int> completed = Task.WhenAny(outputTask, errorTask).GetAwaiter().GetResult();
#pragma warning restore CA2025
                        isError = completed == errorTask;
                        bytesRead = completed.GetAwaiter().GetResult();
                    }

                    if (bytesRead > 0)
                    {
                        if (isError)
                        {
                            errorBytesRead += bytesRead;
                            if (errorBytesRead == errorBuffer.Length)
                            {
                                RentLargerBuffer(ref errorBuffer, errorBytesRead);
                            }
                            errorTask = RandomAccess.ReadAsync(errorHandle, errorBuffer.AsMemory(errorBytesRead), fileOffset: -1, cancellationToken).AsTask();
                        }
                        else
                        {
                            outputBytesRead += bytesRead;
                            if (outputBytesRead == outputBuffer.Length)
                            {
                                RentLargerBuffer(ref outputBuffer, outputBytesRead);
                            }
                            outputTask = RandomAccess.ReadAsync(outputHandle, outputBuffer.AsMemory(outputBytesRead), fileOffset: -1, cancellationToken).AsTask();
                        }
                    }
                    else
                    {
                        if (isError)
                        {
                            errorDone = true;
                        }
                        else
                        {
                            outputDone = true;
                        }
                    }
                }

                cts.Dispose();
            }
            catch (OperationCanceledException)
            {
                cts.Dispose();
                throw new TimeoutException();
            }
        }
    }
}
