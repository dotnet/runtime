// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

using System;

public class Runtime_90423
{
    public static BaconClass.BaconStruct foo;

    [Fact]
    public unsafe static int EntryPoint()
    {
        foo = BaconClass.GetBacon();
            
        for (int i = 0; i < BaconClass.BaconStruct.RESOLUTION; i++)
        {
            if (Math.Abs(foo.table[i] - BaconClass.foo.table[i]) > float.Epsilon)
                return 101;
        }
        return 100;
    }
        
    public class BaconClass
    {
        public unsafe struct BaconStruct
        {
            public float length;
            public const int RESOLUTION = 5;
            public fixed float table[RESOLUTION];
        }
        
        public static BaconStruct foo;
        
        public unsafe static BaconStruct GetBacon()
        {
            const int resolution = BaconStruct.RESOLUTION;
            var length = 1;
            BaconStruct lut = new BaconStruct()
            {
                length = length
            };
            foo = new BaconStruct(){length = length};
		
            float[] bacon = new float[] { 1.1f, 1.2f, 1.3f, 1.4f, 1.5f };
            for (int x = 0; x < BaconStruct.RESOLUTION; x++)
            {
                lut.table[x] = bacon[x];
                foo.table[x] = bacon[x];
            }
            return lut;
        }
    }
}
