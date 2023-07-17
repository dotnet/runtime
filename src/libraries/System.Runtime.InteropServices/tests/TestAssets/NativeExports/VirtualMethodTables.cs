// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
                public delegate* unmanaged<NativeObjectInterface*, int*, void> exchangeData;
                public delegate* unmanaged<NativeObjectInterface*, int*, int, int*, void> sumAndSetData;
                public delegate* unmanaged<NativeObjectInterface*, int**, int, int*, void> sumAndSetDataWithRef;
                public delegate* unmanaged<NativeObjectInterface*, int*, int, void> multiplyWithData;
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
                public delegate* unmanaged<NativeObject*, int*, void> exchangeData;
                public delegate* unmanaged<NativeObject*, int*, int, int*, void> sumAndSetData;
                public delegate* unmanaged<NativeObject*, int**, int, int*, void> sumAndSetDataWithRef;
                public delegate* unmanaged<NativeObject*, int*, int, void> multiplyWithData;
            }
            static NativeObject()
            {
                VTablePointer = (VirtualFunctionTable*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NativeObject), sizeof(VirtualFunctionTable));
                VTablePointer->getData = &GetData;
                VTablePointer->setData = &SetData;
                VTablePointer->exchangeData = &ExchangeData;
                VTablePointer->sumAndSetData = &SumAndSetData;
                VTablePointer->sumAndSetDataWithRef = &SumAndSetData;
                VTablePointer->multiplyWithData = &MultiplyWithData;
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

            [UnmanagedCallersOnly]
            private static void ExchangeData(NativeObject* obj, int* value)
            {
                var temp = obj->Data;
                obj->Data = *value;
                *value = temp;
            }

            [UnmanagedCallersOnly]
            private static void SumAndSetData(NativeObject* obj, int** values, int numValues, int* oldValue)
            {
                *oldValue = obj->Data;

                Span<int> arr = new(*values, numValues);
                int sum = 0;
                foreach (int value in arr)
                {
                    sum += value;
                }
                obj->Data = sum;
            }

            [UnmanagedCallersOnly]
            private static void SumAndSetData(NativeObject* obj, int* values, int numValues, int* oldValue)
            {
                *oldValue = obj->Data;

                Span<int> arr = new(values, numValues);
                int sum = 0;
                foreach (int value in arr)
                {
                    sum += value;
                }
                obj->Data = sum;
            }

            [UnmanagedCallersOnly]
            private static void MultiplyWithData(NativeObject* obj, int* values, int numValues)
            {
                Span<int> arr = new(values, numValues);
                foreach (ref int value in arr)
                {
                    value *= obj->Data;
                }
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

        [UnmanagedCallersOnly(EntryPoint = "exchange_native_object_data")]
        [DNNE.C99DeclCode("struct INativeObject;")]
        public static void ExchangeNativeObjectData([DNNE.C99Type("struct INativeObject*")] NativeObjectInterface* obj, int* x)
        {
            obj->VTable->exchangeData(obj, x);
        }

        [UnmanagedCallersOnly(EntryPoint = "sum_and_set_native_object_data")]
        [DNNE.C99DeclCode("struct INativeObject;")]
        public static void SumAndSetData([DNNE.C99Type("struct INativeObject*")] NativeObjectInterface* obj, int* values, int numValues, int* oldValue)
        {
            obj->VTable->sumAndSetData(obj, values, numValues, oldValue);
        }

        [UnmanagedCallersOnly(EntryPoint = "sum_and_set_native_object_data_with_ref")]
        [DNNE.C99DeclCode("struct INativeObject;")]
        public static void SumAndSetDataWithRef([DNNE.C99Type("struct INativeObject*")] NativeObjectInterface* obj, int** values, int numValues, int* oldValue)
        {
            obj->VTable->sumAndSetDataWithRef(obj, values, numValues, oldValue);
        }

        [UnmanagedCallersOnly(EntryPoint = "multiply_with_native_object_data")]
        [DNNE.C99DeclCode("struct INativeObject;")]
        public static void MultiplyWithData([DNNE.C99Type("struct INativeObject*")] NativeObjectInterface* obj, int* values, int numValues)
        {
            obj->VTable->multiplyWithData(obj, values, numValues);
        }
    }
}
