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
}
