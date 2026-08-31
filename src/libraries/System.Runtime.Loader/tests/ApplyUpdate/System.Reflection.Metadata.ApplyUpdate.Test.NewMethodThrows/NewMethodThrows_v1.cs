// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class NewMethodThrows
    {
        public string ExistingMethod(string x)
	{
            return NewMethod(x);
        }

        public string NewMethod(string x)
        {
            throw new InvalidOperationException (x);
        }

    }
}
