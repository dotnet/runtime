// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using TestLibrary;

unsafe class ThisCallNative
{
    public struct C
    {
        public struct VtableLayout
        {
            public IntPtr getSize;
        }

        public VtableLayout* vtable;
        private int c;
        private float width;
        private float height;
    }

    public struct SizeF
    {
        public float width;
        public float height;
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate SizeF GetSizeFn(C* c);

    [DllImport(nameof(ThisCallNative))]
    public static extern C* CreateInstanceOfC(float width, float height);
}

class ThisCallTest
{
    public unsafe static int Main(string[] args)
    {
        try
        {
            float width = 1.0f;
            float height = 2.0f;
            ThisCallNative.C* instance = ThisCallNative.CreateInstanceOfC(width, height);
            ThisCallNative.GetSizeFn callback = Marshal.GetDelegateForFunctionPointer<ThisCallNative.GetSizeFn>(instance->vtable->getSize);

            ThisCallNative.SizeF result = callback(instance);

            Assert.AreEqual(width, result.width);
            Assert.AreEqual(height, result.height);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return 101;
        }
        return 100;
    }
}
