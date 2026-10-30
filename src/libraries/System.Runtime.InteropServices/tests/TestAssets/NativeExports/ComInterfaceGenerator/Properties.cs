// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using static System.Runtime.InteropServices.ComWrappers;

namespace NativeExports.ComInterfaceGenerator
{
    public static unsafe class PropertiesExports
    {
        [UnmanagedCallersOnly(EntryPoint = "new_properties")]
        public static void* CreateComObject()
        {
            MyComWrapper cw = new();
            var myObject = new PropertiesImplementation();
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

                    void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(PropertiesExports), sizeof(void*) * 13);
                    GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                    vtable[0] = (void*)fpQueryInterface;
                    vtable[1] = (void*)fpAddReference;
                    vtable[2] = (void*)fpRelease;
                    vtable[3] = (delegate* unmanaged<void*, int*, int>)&PropertiesImplementation.ABI.GetIntProperty;
                    vtable[4] = (delegate* unmanaged<void*, int, int>)&PropertiesImplementation.ABI.SetIntProperty;
                    vtable[5] = (delegate* unmanaged<void*, int*, int>)&PropertiesImplementation.ABI.GetReadOnlyInt;
                    vtable[6] = (delegate* unmanaged<void*, int, int>)&PropertiesImplementation.ABI.SetWriteOnlyInt;
                    vtable[7] = (delegate* unmanaged<void*, Guid*, int>)&PropertiesImplementation.ABI.GetGuidProperty;
                    vtable[8] = (delegate* unmanaged<void*, Guid, int>)&PropertiesImplementation.ABI.SetGuidProperty;
                    vtable[9] = (delegate* unmanaged<void*, ushort**, int>)&PropertiesImplementation.ABI.GetStringProperty;
                    vtable[10] = (delegate* unmanaged<void*, ushort*, int>)&PropertiesImplementation.ABI.SetStringProperty;
                    vtable[11] = (delegate* unmanaged<void*, void**, int>)&PropertiesImplementation.ABI.GetSelf;
                    vtable[12] = (delegate* unmanaged<void*, void*, int>)&PropertiesImplementation.ABI.SetSelf;
                    _vtable = vtable;
                    return _vtable;
                }
            }

            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                if (obj is PropertiesImplementation)
                {
                    ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(PropertiesImplementation), sizeof(ComInterfaceEntry));
                    comInterfaceEntry->IID = new Guid(IProperties.IID);
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

        private sealed class PropertiesImplementation : IProperties
        {
            private int _int;
            private int _writeOnlyIntSink;
            private Guid _guid;
            private string _string = string.Empty;

            int IProperties.IntProperty
            {
                get => _int;
                set => _int = value;
            }

            int IProperties.ReadOnlyInt => 111;

            int IProperties.WriteOnlyInt
            {
                set => _writeOnlyIntSink = value;
            }

            Guid IProperties.GuidProperty
            {
                get => _guid;
                set => _guid = value;
            }

            string IProperties.StringProperty
            {
                get => _string;
                set => _string = value;
            }

            // Self is not implemented through this shim; the ABI thunks below short-circuit it.
            // The Self round-trip is covered by the in-process CCW-around-RCW test.
            IProperties? IProperties.Self
            {
                get => null;
                set { }
            }

            // Hand-rolled COM ABI thunks. Each one matches the unmanaged signature the
            // generated RCW expects on the corresponding vtable slot.
            public static class ABI
            {
                [UnmanagedCallersOnly]
                public static int GetIntProperty(void* @this, int* value)
                {
                    try
                    {
                        *value = ComInterfaceDispatch.GetInstance<IProperties>((ComInterfaceDispatch*)@this).IntProperty;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetIntProperty(void* @this, int newValue)
                {
                    try
                    {
                        ComInterfaceDispatch.GetInstance<IProperties>((ComInterfaceDispatch*)@this).IntProperty = newValue;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int GetReadOnlyInt(void* @this, int* value)
                {
                    try
                    {
                        *value = ComInterfaceDispatch.GetInstance<IProperties>((ComInterfaceDispatch*)@this).ReadOnlyInt;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetWriteOnlyInt(void* @this, int newValue)
                {
                    try
                    {
                        ComInterfaceDispatch.GetInstance<IProperties>((ComInterfaceDispatch*)@this).WriteOnlyInt = newValue;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [DNNE.C99DeclCode("struct dnne_guid { uint8_t b[16]; };")]
                [UnmanagedCallersOnly]
                public static int GetGuidProperty(void* @this, [DNNE.C99Type("struct dnne_guid*")] Guid* value)
                {
                    try
                    {
                        *value = ComInterfaceDispatch.GetInstance<IProperties>((ComInterfaceDispatch*)@this).GuidProperty;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetGuidProperty(void* @this, [DNNE.C99Type("struct dnne_guid")] Guid newValue)
                {
                    try
                    {
                        ComInterfaceDispatch.GetInstance<IProperties>((ComInterfaceDispatch*)@this).GuidProperty = newValue;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int GetStringProperty(void* @this, ushort** value)
                {
                    try
                    {
                        string currValue = ComInterfaceDispatch.GetInstance<IProperties>((ComInterfaceDispatch*)@this).StringProperty;
                        *value = Utf16StringMarshaller.ConvertToUnmanaged(currValue);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetStringProperty(void* @this, ushort* newValue)
                {
                    try
                    {
                        string value = Utf16StringMarshaller.ConvertToManaged(newValue);
                        ComInterfaceDispatch.GetInstance<IProperties>((ComInterfaceDispatch*)@this).StringProperty = value;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int GetSelf(void* @this, void** value)
                {
                    *value = null;
                    return 0;
                }

                [UnmanagedCallersOnly]
                public static int SetSelf(void* @this, void* value)
                {
                    return 0;
                }
            }
        }
    }
}
