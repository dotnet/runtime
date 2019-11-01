// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Security buffer descriptor
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecBufferDesc : IDisposable
    {
        public int Version;
        public int BufferCount;
        public IntPtr BuffersPtr;

        /// <summary>
        /// Initialization constructor
        /// </summary>
        /// <param name="size">Size of the buffer to allocate</param>
        internal SecBufferDesc(int size)
        {
            // Set version to SECBUFFER_VERSION
            Version = (int)SecBufferDescType.Version;

            // Set the number of buffers
            BufferCount = 1;

            // Allocate a security buffer of the requested size
            SecBuffer secBuffer = new SecBuffer(size);

            // Allocate a native chunk of memory for security buffer
            BuffersPtr = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));

            try
            {
                // Copy managed data into the native memory
                Marshal.StructureToPtr(secBuffer, BuffersPtr, false);
            }
            catch (Exception)
            {
                // Delete native memory
                Marshal.FreeHGlobal(BuffersPtr);

                // Reset native buffer pointer
                BuffersPtr = IntPtr.Zero;

                // Re-throw exception
                throw;
            }
        }

        /// <summary>
        /// Initialization constructor for byte array
        /// </summary>
        /// <param name="buffer">Data</param>
        internal SecBufferDesc(byte[] buffer)
        {
            // Set version to SECBUFFER_VERSION
            Version = (int)SecBufferDescType.Version;

            // We have only one buffer
            BufferCount = 1;

            // Allocate security buffer
            SecBuffer secBuffer = new SecBuffer(buffer);

            // Allocate native memory for managed block
            BuffersPtr = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));

            try
            {
                // Copy managed data into the native memory
                Marshal.StructureToPtr(secBuffer, BuffersPtr, false);
            }
            catch (Exception)
            {
                // Delete native memory
                Marshal.FreeHGlobal(BuffersPtr);

                // Reset native buffer pointer
                BuffersPtr = IntPtr.Zero;

                // Re-throw exception
                throw;
            }
        }

        /// <summary>
        /// Dispose security buffer descriptor
        /// </summary>
        public void Dispose()
        {
            // Check if we have a buffer
            if (BuffersPtr != IntPtr.Zero)
            {
                // Iterate through each buffer than we manage
                for (int index = 0; index < BufferCount; index++)
                {
                    // Calculate pointer to the buffer
                    IntPtr currentBufferPtr = new IntPtr(BuffersPtr.ToInt64() + (index * Marshal.SizeOf(typeof(SecBuffer))));

                    // Project the buffer into the managed world
                    SecBuffer secBuffer = (SecBuffer)Marshal.PtrToStructure(currentBufferPtr, typeof(SecBuffer));

                    // Dispose it
                    secBuffer.Dispose();
                }

                // Release native memory block
                Marshal.FreeHGlobal(BuffersPtr);

                // Reset buffer pointer
                BuffersPtr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Convert to byte array
        /// </summary>
        internal byte[] ToArray()
        {
            // Check if we have a buffer
            if (BuffersPtr == IntPtr.Zero)
            {
                // We don'thave a buffer
                return null;
            }

            // Prepare a memory stream to contain all the buffers
            MemoryStream outputStream = new MemoryStream();

            // Iterate through each buffer and write the data into the stream
            for (int index = 0; index < BufferCount; index++)
            {
                // Calculate pointer to the buffer
                IntPtr currentBufferPtr = new IntPtr(BuffersPtr.ToInt64() + (index * Marshal.SizeOf(typeof(SecBuffer))));

                // Project the buffer into the managed world
                SecBuffer secBuffer = (SecBuffer)Marshal.PtrToStructure(currentBufferPtr, typeof(SecBuffer));

                // Get the byte buffer
                byte[] secBufferBytes = secBuffer.ToArray();

                // Write buffer to the stream
                outputStream.Write(secBufferBytes, 0, secBufferBytes.Length);
            }

            // Convert to byte array
            return outputStream.ToArray();
        }
    }
}
