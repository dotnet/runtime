// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using static System.Runtime.InteropServices.ComWrappers;

namespace NativeExports.ComInterfaceGenerator
{
    public static unsafe class IndexersExports
    {
        [UnmanagedCallersOnly(EntryPoint = "new_indexers")]
        public static void* CreateComObject()
        {
            MyComWrapper cw = new();
            var myObject = new IndexersImplementation();
            nint ptr = cw.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);

            return (void*)ptr;
        }

        private sealed class MyComWrapper : ComWrappers
        {
            private static void* _vtable;
            private static void* Vtable
            {
                get
                {
                    if (_vtable is not null)
                    {
                        return _vtable;
                    }

                    // 3 IUnknown slots + 8 indexer-accessor slots, matching the source-declaration
                    // order baked into the generator's InterfaceImplementationVtable for IIndexers:
                    //   3: get_Item(int)
                    //   4: set_Item(int)
                    //   5: get_Item(int, int)
                    //   6: set_Item(int, int)
                    //   7: get_Item(long)            -- read-only
                    //   8: set_Item(short)           -- write-only
                    //   9: get_Item(string)
                    //  10: set_Item(string)
                    void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IndexersExports), sizeof(void*) * 11);
                    GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                    vtable[0] = (void*)fpQueryInterface;
                    vtable[1] = (void*)fpAddReference;
                    vtable[2] = (void*)fpRelease;
                    vtable[3] = (delegate* unmanaged<void*, int, int*, int>)&IndexersImplementation.ABI.GetItemInt;
                    vtable[4] = (delegate* unmanaged<void*, int, int, int>)&IndexersImplementation.ABI.SetItemInt;
                    vtable[5] = (delegate* unmanaged<void*, int, int, int*, int>)&IndexersImplementation.ABI.GetItemIntInt;
                    vtable[6] = (delegate* unmanaged<void*, int, int, int, int>)&IndexersImplementation.ABI.SetItemIntInt;
                    vtable[7] = (delegate* unmanaged<void*, long, int*, int>)&IndexersImplementation.ABI.GetItemLong;
                    vtable[8] = (delegate* unmanaged<void*, short, int, int>)&IndexersImplementation.ABI.SetItemShort;
                    vtable[9] = (delegate* unmanaged<void*, ushort*, ushort**, int>)&IndexersImplementation.ABI.GetItemString;
                    vtable[10] = (delegate* unmanaged<void*, ushort*, ushort*, int>)&IndexersImplementation.ABI.SetItemString;
                    _vtable = vtable;
                    return _vtable;
                }
            }

            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                if (obj is IndexersImplementation)
                {
                    ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IndexersImplementation), sizeof(ComInterfaceEntry));
                    comInterfaceEntry->IID = new Guid(IIndexers.IID);
                    comInterfaceEntry->Vtable = (nint)Vtable;
                    count = 1;
                    return comInterfaceEntry;
                }
                count = 0;
                return null;
            }

            protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags) => throw new NotImplementedException();
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }

        private sealed class IndexersImplementation : IIndexers
        {
            // Matches the semantics of SharedTypes.ComInterfaces.Indexers so the tests
            // observe identical behavior regardless of whether the CCW is generator-
            // produced or native-shim produced.
            private int _singleValue;
            private int _twoParamValue;
            private int _writeOnlyShortSink;
            private readonly Dictionary<string, string> _stringMap = new();

            int IIndexers.this[int i]
            {
                get => _singleValue + i;
                set => _singleValue = value - i;
            }

            int IIndexers.this[int i, int j]
            {
                get => _twoParamValue + (i * 100) + j;
                set => _twoParamValue = value - (i * 100) - j;
            }

            int IIndexers.this[long l] => unchecked((int)(l * 7));

            int IIndexers.this[short s]
            {
                set => _writeOnlyShortSink = value + s;
            }

            string IIndexers.this[string key]
            {
                get => _stringMap.TryGetValue(key, out string? v) ? v : string.Empty;
                set => _stringMap[key] = value;
            }

            // Hand-rolled COM ABI thunks. Each one matches the unmanaged signature the
            // generated RCW expects on the corresponding vtable slot.
            public static class ABI
            {
                [UnmanagedCallersOnly]
                public static int GetItemInt(void* @this, int i, int* value)
                {
                    try
                    {
                        IIndexers impl = ComInterfaceDispatch.GetInstance<IIndexers>((ComInterfaceDispatch*)@this);
                        *value = impl[i];
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetItemInt(void* @this, int i, int newValue)
                {
                    try
                    {
                        IIndexers impl = ComInterfaceDispatch.GetInstance<IIndexers>((ComInterfaceDispatch*)@this);
                        impl[i] = newValue;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int GetItemIntInt(void* @this, int i, int j, int* value)
                {
                    try
                    {
                        IIndexers impl = ComInterfaceDispatch.GetInstance<IIndexers>((ComInterfaceDispatch*)@this);
                        *value = impl[i, j];
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetItemIntInt(void* @this, int i, int j, int newValue)
                {
                    try
                    {
                        IIndexers impl = ComInterfaceDispatch.GetInstance<IIndexers>((ComInterfaceDispatch*)@this);
                        impl[i, j] = newValue;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int GetItemLong(void* @this, long l, int* value)
                {
                    try
                    {
                        IIndexers impl = ComInterfaceDispatch.GetInstance<IIndexers>((ComInterfaceDispatch*)@this);
                        *value = impl[l];
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetItemShort(void* @this, short s, int newValue)
                {
                    try
                    {
                        IIndexers impl = ComInterfaceDispatch.GetInstance<IIndexers>((ComInterfaceDispatch*)@this);
                        impl[s] = newValue;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int GetItemString(void* @this, ushort* key, ushort** value)
                {
                    try
                    {
                        string managedKey = Utf16StringMarshaller.ConvertToManaged(key);
                        IIndexers impl = ComInterfaceDispatch.GetInstance<IIndexers>((ComInterfaceDispatch*)@this);
                        string result = impl[managedKey];
                        // Ownership of the returned buffer transfers to the caller; the
                        // generated RCW frees it via Utf16StringMarshaller.Free.
                        *value = Utf16StringMarshaller.ConvertToUnmanaged(result);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetItemString(void* @this, ushort* key, ushort* newValue)
                {
                    try
                    {
                        string managedKey = Utf16StringMarshaller.ConvertToManaged(key);
                        string managedValue = Utf16StringMarshaller.ConvertToManaged(newValue);
                        IIndexers impl = ComInterfaceDispatch.GetInstance<IIndexers>((ComInterfaceDispatch*)@this);
                        impl[managedKey] = managedValue;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }
            }
        }
    }
}
