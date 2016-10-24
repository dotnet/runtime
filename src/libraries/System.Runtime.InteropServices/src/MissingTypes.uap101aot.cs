// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Temporary type definitions to quiet GenFacades
namespace System.IO
{
    public class UnmanagedMemoryAccessor { }
    public class UnmanagedMemoryStream { }
}

namespace System.Runtime.InteropServices
{
    public class AllowReversePInvokeCallsAttribute { }
    public struct HandleRef { }
    public class RuntimeEnvironment {}
}