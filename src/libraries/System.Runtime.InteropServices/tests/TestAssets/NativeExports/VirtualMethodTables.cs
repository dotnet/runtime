// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NativeExports
{
    public unsafe static class VirtualMethodTables
    {
        struct StaticFunctionTable
        {
            public delegate* unmanaged<int, int, int> Add;
            public delegate* unmanaged<int, int, int> Multiply;
        }

        [UnmanagedCallersOnly]
        private static int Add(int x, int y) => x + y;

        [UnmanagedCallersOnly]
        private static int Multiply(int x, int y) => x * y;

        private static readonly StaticFunctionTable* StaticTable;

        static VirtualMethodTables()
        {
            StaticTable = (StaticFunctionTable*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(VirtualMethodTables), sizeof(StaticFunctionTable));

            StaticTable->Add = &Add;
            StaticTable->Multiply = &Multiply;
        }

        [UnmanagedCallersOnly(EntryPoint = "get_static_function_table")]
        public static void* GetStaticFunctionTable()
        {
            return StaticTable;
        }

        public readonly struct NativeObjectInterface
        {
            public struct VirtualFunctionTable
            {
                public delegate* unmanaged<NativeObjectInterface*, int> getData;
                public delegate* unmanaged<NativeObjectInterface*, int, void> setData;
            }

            public readonly VirtualFunctionTable* VTable;

            public NativeObjectInterface()
            {
                throw new UnreachableException("This type should only be accessed through a pointer as it represents an arbitrary implementation of a type that has a NativeObject virtual method table.");
            }
        }

        public struct NativeObject
        {

            public struct VirtualFunctionTable
            {
                // The order of functions here should match NativeObjectInterface.VirtualFunctionTable's members.
                public delegate* unmanaged<NativeObject*, int> getData;
                public delegate* unmanaged<NativeObject*, int, void> setData;
            }
            static NativeObject()
            {
                VTablePointer = (VirtualFunctionTable*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NativeObject), sizeof(VirtualFunctionTable));
                VTablePointer->getData = &GetData;
                VTablePointer->setData = &SetData;
            }

            private static readonly VirtualFunctionTable* VTablePointer;

            private readonly VirtualFunctionTable* vtable = VTablePointer;

            public NativeObject()
            {
            }

            public int Data { get; set; } = 0;

            [UnmanagedCallersOnly]
            private static int GetData(NativeObject* obj)
            {
                return obj->Data;
            }

            [UnmanagedCallersOnly]
            private static void SetData(NativeObject* obj, int value)
            {
                obj->Data = value;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "new_native_object")]
        [DNNE.C99DeclCode("struct NativeObject;")]
        [return: DNNE.C99Type("struct NativeObject*")]
        public static NativeObject* NewNativeObject()
        {
            NativeObject* memory = (NativeObject*)NativeMemory.Alloc((nuint)sizeof(NativeObject));
            *memory = new NativeObject();
            return memory;
        }

        [UnmanagedCallersOnly(EntryPoint = "delete_native_object")]
        [DNNE.C99DeclCode("struct NativeObject;")]
        public static void DeleteNativeObject([DNNE.C99Type("struct NativeObject*")] NativeObject* obj)
        {
            NativeMemory.Free(obj);
        }

        [UnmanagedCallersOnly(EntryPoint = "set_native_object_data")]
        [DNNE.C99DeclCode("struct INativeObject;")]
        public static void SetNativeObjectData([DNNE.C99Type("struct INativeObject*")] NativeObjectInterface* obj, int x)
        {
            obj->VTable->setData(obj, x);
        }

        [UnmanagedCallersOnly(EntryPoint = "get_native_object_data")]
        [DNNE.C99DeclCode("struct INativeObject;")]
        public static int GetNativeObjectData([DNNE.C99Type("struct INativeObject*")] NativeObjectInterface* obj)
        {
            return obj->VTable->getData(obj);
        }
    }
}
