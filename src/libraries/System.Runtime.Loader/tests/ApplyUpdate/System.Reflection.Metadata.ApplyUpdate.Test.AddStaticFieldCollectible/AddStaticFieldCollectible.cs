// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public static class AddStaticFieldCollectible
    {
        public static int Next() => 1;

        public static string Name() => "none";
    }

    public static class AddStaticFieldCollectibleInit
    {
        // An explicit static constructor makes the type precise-init (not beforefieldinit): it has to run before
        // any of its static fields is accessed.
        static AddStaticFieldCollectibleInit() { }
    }

    public static class AddStaticFieldCollectibleReader
    {
        public static int Read() => 0;
    }
}
