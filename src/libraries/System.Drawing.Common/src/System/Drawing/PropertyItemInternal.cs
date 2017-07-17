// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    // sdkinc\imaging.h
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class PropertyItemInternal : IDisposable
    {
        public int id;
        public int len;
        public short type;
        public IntPtr value = IntPtr.Zero;

        internal PropertyItemInternal()
        {
        }

        ~PropertyItemInternal()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (value != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(value);
                value = IntPtr.Zero;
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        internal static PropertyItemInternal ConvertFromPropertyItem(PropertyItem propItem)
        {
            PropertyItemInternal propItemInternal = new PropertyItemInternal();
            propItemInternal.id = propItem.Id;
            propItemInternal.len = 0;
            propItemInternal.type = propItem.Type;

            byte[] propItemValue = propItem.Value;
            if (propItemValue != null)
            {
                int length = propItemValue.Length;
                propItemInternal.len = length;
                propItemInternal.value = Marshal.AllocHGlobal(length);
                Marshal.Copy(propItemValue, 0, propItemInternal.value, length);
            }

            return propItemInternal;
        }

        internal static PropertyItem[] ConvertFromMemory(IntPtr propdata, int count)
        {
            PropertyItem[] props = new PropertyItem[count];

            for (int i = 0; i < count; i++)
            {
                PropertyItemInternal propcopy = null;
                try
                {
                    propcopy = (PropertyItemInternal)Marshal.PtrToStructure(propdata,
                                                  typeof(PropertyItemInternal));

                    props[i] = new PropertyItem();
                    props[i].Id = propcopy.id;
                    props[i].Len = propcopy.len;
                    props[i].Type = propcopy.type;

                    // this calls Marshal.Copy and creates a copy of the original memory into a byte array.
                    props[i].Value = propcopy.Value;

                    propcopy.value = IntPtr.Zero;  // we dont actually own this memory so dont free it.
                }
                finally
                {
                    if (propcopy != null)
                    {
                        propcopy.Dispose();
                    }
                }

                propdata = (IntPtr)((long)propdata + (int)Marshal.SizeOf(typeof(PropertyItemInternal)));
            }

            return props;
        }

        public byte[] Value
        {
            get
            {
                if (len == 0)
                {
                    return null;
                }

                byte[] bytes = new byte[len];

                Marshal.Copy(value,
                             bytes,
                             0,
                             (int)len);
                return bytes;
            }
        }
    }
}

