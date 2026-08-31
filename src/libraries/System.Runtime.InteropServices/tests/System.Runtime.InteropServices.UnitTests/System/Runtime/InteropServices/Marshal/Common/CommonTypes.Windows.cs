// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0618 // Type or member is obsolete

using System.Runtime.CompilerServices;
using System.Threading;

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

    /// <summary>
    /// Well-known COM constants
    /// </summary>
    public static class ComConstants
    {
        public static readonly Guid IID_IUnknown = new("00000000-0000-0000-c000-000000000046");
        public static readonly Guid IID_IDispatch = new("00020400-0000-0000-c000-000000000046");
        public static readonly Guid IID_IClassFactory = new("00000001-0000-0000-c000-000000000046");

        public const int S_OK = 0;
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_NOINTERFACE = unchecked((int)0x80004002);
    }

    /// <summary>
    /// Class factory used to provide a unmanaged IClassFactory instance for testing with <see cref="ComObject"/>.
    /// </summary>
    internal unsafe struct ComObjectFactory
    {
        public const string CLSID = "3DCAAC37-F484-49E9-9257-273BA9618162";

        [ModuleInitializer]
        internal static unsafe void RegisterInProcCOMServer()
        {
            var clsid = new Guid(ComObjectFactory.CLSID);
            const int CLSCTX_INPROC_SERVER = 1;
            const int REGCLS_MULTIPLEUSE = 1;
            void* classFactory = ComObjectFactory.Create();
            int res = CoRegisterClassObject(in clsid, classFactory, CLSCTX_INPROC_SERVER, REGCLS_MULTIPLEUSE, out int cookie);
            Xunit.Assert.Equal(ComConstants.S_OK, res);
            Marshal.Release((IntPtr)classFactory);

            [DllImport("Ole32")]
            static extern int CoRegisterClassObject(in Guid clsid, void* factory, int clsContext, int flags, out int registerCookie);
        }

        // Create an instance of a COM object class factory
        public static void* Create()
        {
            var vtable = (VTable*)NativeMemory.Alloc((nuint)sizeof(VTable));
            vtable->QueryInterface = &QueryInterface;
            vtable->AddRef = &AddRef;
            vtable->Release = &Release;

            vtable->CreateInstance = &CreateInstance;
            vtable->LockServer = &LockServer;

            var instance = (ComObjectFactory*)NativeMemory.Alloc((nuint)sizeof(ComObjectFactory));
            instance->_vtable = vtable;
            instance->_refCount = 1;
            return instance;
        }

        // The COM ABI for a vtable
        private struct VTable
        {
            // IUnknown
            public delegate* unmanaged<void*, Guid*, void**, int> QueryInterface;
            public delegate* unmanaged<void*, uint> AddRef;
            public delegate* unmanaged<void*, uint> Release;

            // IClassFactory
            public delegate* unmanaged<void*, void*, Guid*, void**, int> CreateInstance;
            public delegate* unmanaged<void*, int, int> LockServer;
        }

        // The COM ABI requires the first pointer field to be the vtable
        private VTable* _vtable;

        // Additional instance fields for this COM object
        private uint _refCount;

        [UnmanagedCallersOnly]
        private static int QueryInterface(void* instance, Guid* iid, void** obj)
        {
            if (ComConstants.IID_IUnknown == *iid || ComConstants.IID_IClassFactory == *iid)
            {
                *obj = instance;
            }
            else
            {
                return ComConstants.E_NOINTERFACE;
            }

            _AddRef(instance);
            return ComConstants.S_OK;
        }

        [UnmanagedCallersOnly]
        private static uint AddRef(void* instance) => _AddRef(instance);

        private static uint _AddRef(void* instance)
        {
            var inst = (ComObjectFactory*)instance;
            return Interlocked.Increment(ref inst->_refCount);
        }

        [UnmanagedCallersOnly]
        private static uint Release(void* instance)
        {
            var inst = (ComObjectFactory*)instance;
            uint c = Interlocked.Decrement(ref inst->_refCount);
            if (c == 0)
            {
                NativeMemory.Free(inst->_vtable);
                NativeMemory.Free(inst);
            }
            return c;
        }

        [UnmanagedCallersOnly]
        private static int CreateInstance(void* instance, void* outer, Guid* riid, void** obj)
        {
            *obj = ComObject.Create();
            return ComConstants.S_OK;
        }

        [UnmanagedCallersOnly]
        private static int LockServer(void* instance, int shouldLock) => ComConstants.S_OK;
    }

    /// <summary>
    /// ComObject defined entirely in managed code to avoid requiring a known registered COM server.
    /// </summary>
    internal unsafe struct ComObject
    {
        // Create an instance of a COM object
        public static void* Create()
        {
            var vtable = (VTable*)NativeMemory.Alloc((nuint)sizeof(VTable));
            vtable->QueryInterface = &QueryInterface;
            vtable->AddRef = &AddRef;
            vtable->Release = &Release;

            vtable->GetTypeInfoCount = &GetTypeInfoCount;
            vtable->GetTypeInfo = &GetTypeInfo;
            vtable->GetIDsOfNames = &GetIDsOfNames;
            vtable->Invoke = &Invoke;

            var instance = (ComObject*)NativeMemory.Alloc((nuint)sizeof(ComObject));
            instance->_vtable = vtable;
            instance->_refCount = 1;
            return instance;
        }

        // The COM ABI for a vtable
        private struct VTable
        {
            // IUnknown
            public delegate* unmanaged<void*, Guid*, void**, int> QueryInterface;
            public delegate* unmanaged<void*, uint> AddRef;
            public delegate* unmanaged<void*, uint> Release;

            // IDispatch
            public delegate* unmanaged<void*, uint*, int> GetTypeInfoCount;
            public delegate* unmanaged<void*, int, int, void**, int> GetTypeInfo;
            public delegate* unmanaged<void*, Guid*, void**, uint, uint, int*, int> GetIDsOfNames;
            public delegate* unmanaged<void*, int, Guid*, uint, short, void*, void*, void*, uint*, int> Invoke;
        }

        // The COM ABI requires the first pointer field to be the vtable
        private VTable* _vtable;

        // Additional instance fields for this COM object
        private uint _refCount;

        [UnmanagedCallersOnly]
        private static int QueryInterface(void* instance, Guid* iid, void** obj)
        {
            if (ComConstants.IID_IUnknown == *iid || ComConstants.IID_IDispatch == *iid)
            {
                *obj = instance;
            }
            else
            {
                return ComConstants.E_NOINTERFACE;
            }

            _AddRef(instance);
            return ComConstants.S_OK;
        }

        [UnmanagedCallersOnly]
        private static uint AddRef(void* instance) => _AddRef(instance);

        private static uint _AddRef(void* instance)
        {
            var inst = (ComObject*)instance;
            return Interlocked.Increment(ref inst->_refCount);
        }

        [UnmanagedCallersOnly]
        private static uint Release(void* instance)
        {
            var inst = (ComObject*)instance;
            uint c = Interlocked.Decrement(ref inst->_refCount);
            if (c == 0)
            {
                NativeMemory.Free(inst->_vtable);
                NativeMemory.Free(inst);
            }
            return c;
        }

        [UnmanagedCallersOnly]
        private static int GetTypeInfoCount(void* instance, uint* i) => ComConstants.E_NOTIMPL;

        [UnmanagedCallersOnly]
        private static int GetTypeInfo(void* instance, int itinfo, int lcid, void** i) => ComConstants.E_NOTIMPL;

        [UnmanagedCallersOnly]
        private static int GetIDsOfNames(
            void* instance,
            Guid* iid,
            void** namesRaw,
            uint namesCount,
            uint lcid,
            int* dispIdsRaw) => ComConstants.E_NOTIMPL;

        [UnmanagedCallersOnly]
        private static int Invoke(
            void* instance,
            int dispIdMember,
            Guid* riid,
            uint lcid,
            short wFlags,
            void* pDispParams,
            void* VarResult,
            void* pExcepInfo,
            void* puArgErr) => ComConstants.E_NOTIMPL;
    }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    public class InterfaceOnComImportObject : IComImportObject { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    public class ComImportObject { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.None)]
    public class DualComObject : DualInterface { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.None)]
    public class IUnknownComObject : IUnknownInterface { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.None)]
    public class IDispatchComObject : IDispatchInterface { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.None)]
    public class IInspectableComObject : IInspectableInterface { }

    public class IInspectableManagedObject : IInspectableInterface {}

    public class SubComImportObject : ComImportObject { }

    public class GenericSubComImportObject<T> : ComImportObject { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.None)]
    public class NonDualComObject : IComImportObject { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.None)]
    public class NonDualComObjectEmpty { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class AutoDispatchComObject : IComImportObject { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class AutoDispatchComObjectEmpty { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class AutoDualComObject : IComImportObject { }

    [ComImport]
    [Guid(ComObjectFactory.CLSID)]
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
