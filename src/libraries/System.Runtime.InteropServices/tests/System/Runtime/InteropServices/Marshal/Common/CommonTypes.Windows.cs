// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.Tests.Common
{
    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public interface IComImportObject { }

    public class InterfaceComImportObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public class InterfaceAndComImportObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public class ComImportObject { }

    public class SubComImportObject : ComImportObject { }
}
