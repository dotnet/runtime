// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public static class AddStaticFieldCollectible
    {
        private static int s_count;

        private static string s_name;

        public static int Next() => s_count += 2;

        public static string Name() => s_name ??= "added";
    }

    public static class AddStaticFieldCollectibleInit
    {
        public static int s_value;

        static AddStaticFieldCollectibleInit() { s_value = 42; }
    }

    public static class AddStaticFieldCollectibleReader
    {
        // The first access to AddStaticFieldCollectibleInit, after the update: it has to run the updated static
        // constructor before reading the added field.
        public static int Read() => AddStaticFieldCollectibleInit.s_value;
    }
}
