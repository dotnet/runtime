// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

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
        internal unsafe IntPtr ConvertToMemory()
        {
            int size = sizeof(EncoderParameterPrivate);

            int length = _param.Length;
            IntPtr memory = Marshal.AllocHGlobal(checked(length * size + IntPtr.Size));

            Marshal.WriteIntPtr(memory, (IntPtr)length);

            long arrayOffset = checked((long)memory + IntPtr.Size);

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
        internal static unsafe EncoderParameters ConvertFromMemory(IntPtr memory)
        {
            if (memory == IntPtr.Zero)
            {
                throw Gdip.StatusException(Gdip.InvalidParameter);
            }

            int count = *(int*)memory;
            EncoderParameterPrivate* parameters = (EncoderParameterPrivate*)((byte*)memory + IntPtr.Size);
            EncoderParameters p = new EncoderParameters(count);
            for (int i = 0; i < count; i++)
            {
                ref readonly EncoderParameterPrivate param = ref parameters[i];

                p._param[i] = new EncoderParameter(new Encoder(param.ParameterGuid), param.NumberOfValues, param.ParameterValueType, param.ParameterValue);
            }

            return p;
        }

        public void Dispose()
        {
            foreach (EncoderParameter p in _param)
            {
                p?.Dispose();
            }
            _param = null!;
        }
    }
}
