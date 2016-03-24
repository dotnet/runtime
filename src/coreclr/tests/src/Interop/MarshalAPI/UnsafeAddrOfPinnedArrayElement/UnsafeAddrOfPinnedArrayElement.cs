// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreFXTestLibrary;

public class UnsafeAddrOfPinnedArrayElementTest 
{
    
    public static void NullParameter()
    {
        Assert.Throws<ArgumentNullException>(() => Marshal.UnsafeAddrOfPinnedArrayElement(null, 1));
        int [] array = new int[]{1,2,3};
        GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        Assert.Throws<ArgumentOutOfRangeException>(() => Marshal.UnsafeAddrOfPinnedArrayElement<int>(array, -1)); 
        Assert.Throws<ArgumentOutOfRangeException>(() => Marshal.UnsafeAddrOfPinnedArrayElement<int>(array, 3)); 

        handle.Free();
    }

    
    public static void PrimitiveType()
    {
        int [] array = new int[]{1,2,3};
        GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);


        IntPtr v0 = Marshal.UnsafeAddrOfPinnedArrayElement<int>(array, 0); 
        Assert.AreEqual(1, Marshal.ReadInt32(v0));
        
        IntPtr v1 = Marshal.UnsafeAddrOfPinnedArrayElement<int>(array, 1); 
        Assert.AreEqual(2, Marshal.ReadInt32(v1));

        IntPtr v2 = Marshal.UnsafeAddrOfPinnedArrayElement<int>(array, 2); 
        Assert.AreEqual(3, Marshal.ReadInt32(v2));

        handle.Free();
    }


    struct Point
    {
        public int x;
        public int y;
    }

    
    public static void StructType()
    {
        Point [] array = new Point[]{
            new Point(){x = 100, y = 100},
            new Point(){x = -1, y = -1},
            new Point(){x = 0, y = 0},
        };
        GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);


        IntPtr v0 = Marshal.UnsafeAddrOfPinnedArrayElement<Point>(array, 0);
        Point p0 = Marshal.PtrToStructure<Point>(v0);
        Assert.AreEqual(100, p0.x);
        Assert.AreEqual(100, p0.y);

        IntPtr v1 = Marshal.UnsafeAddrOfPinnedArrayElement<Point>(array, 1);
        Point p1 = Marshal.PtrToStructure<Point>(v1);
        Assert.AreEqual(-1, p1.x);
        Assert.AreEqual(-1, p1.y);


        IntPtr v2 = Marshal.UnsafeAddrOfPinnedArrayElement<Point>(array, 2); 
        Point p2 = Marshal.PtrToStructure<Point>(v2);
        Assert.AreEqual(0, p2.x);
        Assert.AreEqual(0, p2.y);


        handle.Free();


    }
    
    public static int Main(String[] args) {
        
        StructType();
        PrimitiveType();
        //NullParameter(); 
        return 100;       
    }
}
