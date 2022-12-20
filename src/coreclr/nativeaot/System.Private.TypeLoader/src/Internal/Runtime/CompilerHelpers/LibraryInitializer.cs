// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Runtime.TypeLoader;

namespace Internal.Runtime.CompilerHelpers
{
    public class LibraryInitializer
    {
        public static void InitializeLibrary()
        {
            TypeLoaderEnvironment.Initialize();
        }
    }
}
