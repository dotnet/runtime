// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // Types decorated with this attribute are treated specially by the compiler. A "windowsruntime" bit is set in their metadata.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Delegate, Inherited = false)]
    internal sealed class WindowsRuntimeImportAttribute : Attribute
    {
        public WindowsRuntimeImportAttribute() { }
    }
}