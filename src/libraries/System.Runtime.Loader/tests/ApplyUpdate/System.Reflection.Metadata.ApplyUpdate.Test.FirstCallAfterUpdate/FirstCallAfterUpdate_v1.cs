// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class FirstCallAfterUpdate {
        public FirstCallAfterUpdate() {}
        public string Method1(string s) {
            return "NEW " + s;
        }
    }
}
