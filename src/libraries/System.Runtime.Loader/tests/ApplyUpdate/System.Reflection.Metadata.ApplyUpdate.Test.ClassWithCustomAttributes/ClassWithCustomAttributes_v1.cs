// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    [Obsolete]
    public class ClassWithCustomAttributes {
        [Obsolete]
        public static string Method () {
            return null;
        }
    }

    [Obsolete]
    public class ClassWithCustomAttributes2 {
        [Obsolete]
        public static string Method2 () {
            return null;
        }
    }
}


    
