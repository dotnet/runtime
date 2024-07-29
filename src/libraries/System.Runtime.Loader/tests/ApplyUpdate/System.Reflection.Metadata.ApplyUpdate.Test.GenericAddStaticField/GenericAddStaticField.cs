// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class GenericAddStaticField<T>
    {
        public GenericAddStaticField () {
        }

        public T GetField () => s_field;

        private static T s_field;

        public void TestMethod () {
            s_field = (T)(object)"abcd";
        }

    }
}
