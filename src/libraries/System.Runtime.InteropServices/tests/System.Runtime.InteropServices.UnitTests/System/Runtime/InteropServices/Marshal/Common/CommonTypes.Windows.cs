// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests.Common
{
    [ComImport]
    [Guid("293E13A4-2791-4121-9714-14D37CF5DCD4")]
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

    public static class ComServers
    {
        // Represents an IUnknown only server that exists on all Windows SKUs.
        public const string IUnknownOnlyComServer = "0340F119-A598-4ed9-B0AC-6F6A12D3E755";

        // Represents a COM server that exists on all Windows skus that support IDispatch.
        // See System.DirectoryServices.AccountManagement.ADsLargeInteger.
        public const string IDispatchSupportedComServer = "927971f5-0939-11d1-8be1-00c04fd8d503";
        public const string IDispatchSupportedComServerProgId = "LargeInteger";
    }

    [ComImport]
    [Guid(ComServers.IUnknownOnlyComServer)]
    public class InterfaceOnComImportObject : IComImportObject { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    public class ComImportObject { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.None)]
    public class DualComObject : DualInterface { }

    [ComImport]
    [Guid(ComServers.IUnknownOnlyComServer)]
    [ClassInterface(ClassInterfaceType.None)]
    public class IUnknownComObject : IUnknownInterface { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.None)]
    public class IDispatchComObject : IDispatchInterface { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.None)]
    public class IInspectableComObject : IInspectableInterface { }

    public class IInspectableManagedObject : IInspectableInterface {}

    public class SubComImportObject : ComImportObject { }

    public class GenericSubComImportObject<T> : ComImportObject { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.None)]
    public class NonDualComObject : IComImportObject { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.None)]
    public class NonDualComObjectEmpty { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class AutoDispatchComObject : IComImportObject { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class AutoDispatchComObjectEmpty { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class AutoDualComObject : IComImportObject { }

    [ComImport]
    [Guid(ComServers.IDispatchSupportedComServer)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class AutoDualComObjectEmpty { }

    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class ManagedAutoDispatchClass { }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ManagedAutoDualClass{ }

    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("710D252E-22BF-4A33-9544-40D8D03C29FF")]
    public interface ManagedInterfaceSupportIUnknown { }

    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("B68116E6-B341-4596-951F-F95262CA5612")]
    public interface ManagedInterfaceSupportIUnknownWithMethods
    {
        void M1();
        void M2();
    }

    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
    [Guid("E0B128C2-C560-42B7-9824-BE753F321B09")]
    public interface ManagedInterfaceSupportIDispatch { }

    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
    [Guid("857A6ACD-E462-4379-8314-44B9C0078217")]
    public interface ManagedInterfaceSupportIDispatchWithMethods
    {
        void M1();
        void M2();
    }

    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    [Guid("D235E3E7-344F-4645-BBB6-6A82C2B34C34")]
    public interface ManagedInterfaceSupportDualInterfaceWithMethods
    {
        void M1();
        void M2();
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
