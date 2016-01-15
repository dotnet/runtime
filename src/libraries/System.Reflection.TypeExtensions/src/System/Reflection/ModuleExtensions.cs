// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection
{
    public static class ModuleExtensions
    {
        public static bool HasModuleVersionId(this Module module)
        {
            Requires.NotNull(module, "module");
            return false; // never available on .NET Native
        }
        
        public static Guid GetModuleVersionId(this Module module)
        {
            Requires.NotNull(module, "module");
            throw new InvalidOperationException(SR.ModuleVersionIdNotSupported);
        }
    }
}
