// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public static class ClassWithCustomAttributesHelper {
        public static Type GetAttributedClass () {
#pragma warning disable CS0612
            return typeof(ClassWithCustomAttributes);
#pragma warning restore CS0612
        }
    }

    [Obsolete]
    public class ClassWithCustomAttributes {
        [Obsolete]
        public static string Method () {
            return null;
        }
    }
}


    
