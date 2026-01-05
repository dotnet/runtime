// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public static class IncreaseMetadataRowSize
    {
        public static void Main(string[] args) { }
        public static int VeryLooooooooooooooooooooooooooooooooongMethodNameToPushTheStringBlobOver64k_1()
        {
            return 0;
        }
        public static void VeryLooooooooooooooooooooooooooooooooongMethodNameToPushTheStringBlobOver64k_2(int x2) {}
        public static void VeryLooooooooooooooooooooooooooooooooongMethodNameToPushTheStringBlobOver64k_3(int x3) {}
        public static void VeryLooooooooooooooooooooooooooooooooongMethodNameToPushTheStringBlobOver64k_4(int x4) {}
        public static void VeryLooooooooooooooooooooooooooooooooongMethodNameToPushTheStringBlobOver64k_5(int x5) {}
    }

}
