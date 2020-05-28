// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace SharedLibrary
{
    public class SharedType
    {
        // This is injected into a portable application by a startup
        // hook.
        public static int Value = 2;
    }
}
