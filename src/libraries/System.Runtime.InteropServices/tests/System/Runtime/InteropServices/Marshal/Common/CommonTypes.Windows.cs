// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests.Common
{
    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public interface IComImportObject { }

    [ComImport]
    [Guid("BF46F910-6B9B-4FBF-BC81-87CDACD2BD83")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface DualInterface { }

    [ComImport]
    [Guid("8DCD4DCE-778A-4261-A812-F4595C2F2614")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IUnknownInterface { }

    [ComImport]
    [Guid("9323D453-BA36-4459-92AA-ECEC2F916FED")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IDispatchInterface { }

    [ComImport]
    [Guid("E7AA81A5-36A2-4CEC-A629-13B6A26865D1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    public interface IInspectableInterface { }

    public class InterfaceComImportObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public class InterfaceAndComImportObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    public class ComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.None)]
    public class DualComObject : DualInterface { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.None)]
    public class IUnknownComObject : IUnknownInterface { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.None)]
    public class IDispatchComObject : IDispatchInterface { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.None)]
    public class IInspectableComObject : IInspectableInterface { }

    public class SubComImportObject : ComImportObject { }

    public class GenericSubComImportObject<T> : ComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.None)]
    public class NonDualComObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.None)]
    public class NonDualComObjectEmpty { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class AutoDispatchComObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class AutoDispatchComObjectEmpty { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class AutoDualComObject : IComImportObject { }

    [ComImport]
    [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class AutoDualComObjectEmpty { }
}

#pragma warning restore CS0618 // Type or member is obsolete
