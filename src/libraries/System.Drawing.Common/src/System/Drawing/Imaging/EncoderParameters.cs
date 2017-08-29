// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    public sealed class EncoderParameters : IDisposable
    {
        private EncoderParameter[] _param;

        public EncoderParameters(int count)
        {
            _param = new EncoderParameter[count];
        }

        public EncoderParameters()
        {
            _param = new EncoderParameter[1];
        }

        public EncoderParameter[] Param
        {
            get
            {
                return _param;
            }
            set
            {
                _param = value;
            }
        }

        /// <summary>
        /// Copy the EncoderParameters data into a chunk of memory to be consumed by native GDI+ code.
        ///
        /// We need to marshal the EncoderParameters info from/to native GDI+ ourselves since the definition of the managed/unmanaged classes
        /// are different and the native class is a bit weird. The native EncoderParameters class is defined in GDI+ as follows:
        /// 
        /// class EncoderParameters {
        ///     UINT Count;                      // Number of parameters in this structure
        ///     EncoderParameter Parameter[1];   // Parameter values
        /// };
        ///
        /// We don't have the 'Count' field since the managed array contains it. In order for this structure to work with more than one
        /// EncoderParameter we need to preallocate memory for the extra n-1 elements, something like this:
        ///
        /// EncoderParameters* pEncoderParameters = (EncoderParameters*) malloc(sizeof(EncoderParameters) + (n-1) * sizeof(EncoderParameter));
        ///
        /// Also, in 64-bit platforms, 'Count' is aligned in 8 bytes (4 extra padding bytes) so we use IntPtr instead of Int32 to account for
        /// that.
        /// </summary>
        internal IntPtr ConvertToMemory()
        {
            int size = Marshal.SizeOf(typeof(EncoderParameter));

            int length = _param.Length;
            IntPtr memory = Marshal.AllocHGlobal(checked(length * size + Marshal.SizeOf(typeof(IntPtr))));

            if (memory == IntPtr.Zero)
            {
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.OutOfMemory);
            }

            Marshal.WriteIntPtr(memory, (IntPtr)length);

            long arrayOffset = checked((long)memory + Marshal.SizeOf(typeof(IntPtr)));

            for (int i = 0; i < length; i++)
            {
                Marshal.StructureToPtr(_param[i], (IntPtr)(arrayOffset + i * size), false);
            }

            return memory;
        }

        /// <summary>
        /// Copy the native GDI+ EncoderParameters data from a chunk of memory into a managed EncoderParameters object.
        /// See ConvertToMemory for more info.
        /// </summary>
        internal static EncoderParameters ConvertFromMemory(IntPtr memory)
        {
            if (memory == IntPtr.Zero)
            {
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);
            }

            int count = Marshal.ReadIntPtr(memory).ToInt32();

            EncoderParameters p = new EncoderParameters(count);
            int size = Marshal.SizeOf(typeof(EncoderParameter));
            long arrayOffset = (long)memory + Marshal.SizeOf(typeof(IntPtr));

            for (int i = 0; i < count; i++)
            {
                Guid guid = (Guid)Marshal.PtrToStructure((IntPtr)(i * size + arrayOffset), typeof(Guid));
                int numberOfValues = Marshal.ReadInt32((IntPtr)(i * size + arrayOffset + 16));
                EncoderParameterValueType type = (EncoderParameterValueType)Marshal.ReadInt32((IntPtr)(i * size + arrayOffset + 20));
                IntPtr value = Marshal.ReadIntPtr((IntPtr)(i * size + arrayOffset + 24));

                p._param[i] = new EncoderParameter(new Encoder(guid), numberOfValues, type, value);
            }

            return p;
        }

        public void Dispose()
        {
            foreach (EncoderParameter p in _param)
            {
                if (p != null)
                {
                    p.Dispose();
                }
            }
            _param = null;
        }
    }
}
