// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // this type is not used anywhere, but it exists so the containing `.dll` is not empty and
    // can be copied to the output folder and be dynamically loaded during test execution
    public class ClassDefinedInAssemblyWithAVeryLongPath
    {
    }
}
