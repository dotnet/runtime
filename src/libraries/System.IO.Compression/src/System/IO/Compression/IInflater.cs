// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression
{
    internal interface IInflater : IDisposable
    {
        /// <summary>
        /// The remaining free space at next_out i.e. number of bytes available
        /// in output
        /// </summary>
        int AvailableOutput { get;}

        /// <summary>
        /// Performs the actual inflation of data currently in the input buffer of the Inflater
        /// and stores it into the given array with the given information
        /// </summary>
        /// <param name="bytes">Buffer for the output of the inflation</param>
        /// <param name="offset">The starting point at which to store output data</param>
        /// <param name="length">The maximum number of bytes to write to "bytes"</param>
        /// <returns>The number of bytes written to "bytes"</returns>
        int Inflate(byte[] bytes, int offset, int length);

        /// <summary>
        /// Whether the end of the stream has been reached and no more data is available
        /// </summary>
        bool Finished();

        /// <summary>
        /// Returns true if the number of available bytes to be inflated is equal to
        /// zero.
        /// </summary>
        /// <remarks>Before running Inflate(), this should return false.</remarks>
        bool NeedsInput();

        /// <summary>
        /// Sets the input of the underlying stream to be equal to the byte array
        /// provided.
        /// </summary>
        /// <param name="inputBytes">Bytes to put into next_in</param>
        /// <param name="offset">Offset into inputBytes</param>
        /// <param name="length">Number of bytes to write to the input stream</param>
        /// <remarks>requires that NeedsInput() == true</remarks>
        void SetInput(byte[] inputBytes, int offset, int length);
    }
}
