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
    public static unsafe class DerivedPropertiesExports
    {
        [UnmanagedCallersOnly(EntryPoint = "new_derived_properties")]
        public static void* CreateComObject()
        {
            MyComWrapper cw = new();
            var myObject = new DerivedPropertiesImplementation();
            nint ptr = cw.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);

            return (void*)ptr;
        }

        private sealed class MyComWrapper : ComWrappers
        {
            private static void* _basePropertiesVtable;
            private static void* BasePropertiesVtable
            {
                get
                {
                    if (_basePropertiesVtable is not null)
                    {
                        return _basePropertiesVtable;
                    }

                    void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(DerivedPropertiesExports), sizeof(void*) * 13);
                    GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                    vtable[0] = (void*)fpQueryInterface;
                    vtable[1] = (void*)fpAddReference;
                    vtable[2] = (void*)fpRelease;
                    vtable[3] = (delegate* unmanaged<void*, int*, int>)&DerivedPropertiesImplementation.ABI.GetIntProperty;
                    vtable[4] = (delegate* unmanaged<void*, int, int>)&DerivedPropertiesImplementation.ABI.SetIntProperty;
                    vtable[5] = (delegate* unmanaged<void*, int*, int>)&DerivedPropertiesImplementation.ABI.GetReadOnlyInt;
                    vtable[6] = (delegate* unmanaged<void*, int, int>)&DerivedPropertiesImplementation.ABI.SetWriteOnlyInt;
                    vtable[7] = (delegate* unmanaged<void*, Guid*, int>)&DerivedPropertiesImplementation.ABI.GetGuidProperty;
                    vtable[8] = (delegate* unmanaged<void*, Guid, int>)&DerivedPropertiesImplementation.ABI.SetGuidProperty;
                    vtable[9] = (delegate* unmanaged<void*, ushort**, int>)&DerivedPropertiesImplementation.ABI.GetStringProperty;
                    vtable[10] = (delegate* unmanaged<void*, ushort*, int>)&DerivedPropertiesImplementation.ABI.SetStringProperty;
                    vtable[11] = (delegate* unmanaged<void*, void**, int>)&DerivedPropertiesImplementation.ABI.GetSelf;
                    vtable[12] = (delegate* unmanaged<void*, void*, int>)&DerivedPropertiesImplementation.ABI.SetSelf;
                    _basePropertiesVtable = vtable;
                    return _basePropertiesVtable;
                }
            }

            private static void* _derivedPropertiesVtable;
            private static void* DerivedPropertiesVtable
            {
                get
                {
                    if (_derivedPropertiesVtable is not null)
                    {
                        return _derivedPropertiesVtable;
                    }

                    void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(DerivedPropertiesExports), sizeof(void*) * 18);
                    GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                    vtable[0] = (void*)fpQueryInterface;
                    vtable[1] = (void*)fpAddReference;
                    vtable[2] = (void*)fpRelease;
                    // Inherited slots — must match the IProperties vtable layout above byte-for-byte
                    // so the IDerivedProperties RCW can dispatch base accessors through this pointer.
                    vtable[3] = (delegate* unmanaged<void*, int*, int>)&DerivedPropertiesImplementation.ABI.GetIntProperty;
                    vtable[4] = (delegate* unmanaged<void*, int, int>)&DerivedPropertiesImplementation.ABI.SetIntProperty;
                    vtable[5] = (delegate* unmanaged<void*, int*, int>)&DerivedPropertiesImplementation.ABI.GetReadOnlyInt;
                    vtable[6] = (delegate* unmanaged<void*, int, int>)&DerivedPropertiesImplementation.ABI.SetWriteOnlyInt;
                    vtable[7] = (delegate* unmanaged<void*, Guid*, int>)&DerivedPropertiesImplementation.ABI.GetGuidProperty;
                    vtable[8] = (delegate* unmanaged<void*, Guid, int>)&DerivedPropertiesImplementation.ABI.SetGuidProperty;
                    vtable[9] = (delegate* unmanaged<void*, ushort**, int>)&DerivedPropertiesImplementation.ABI.GetStringProperty;
                    vtable[10] = (delegate* unmanaged<void*, ushort*, int>)&DerivedPropertiesImplementation.ABI.SetStringProperty;
                    vtable[11] = (delegate* unmanaged<void*, void**, int>)&DerivedPropertiesImplementation.ABI.GetSelf;
                    vtable[12] = (delegate* unmanaged<void*, void*, int>)&DerivedPropertiesImplementation.ABI.SetSelf;
                    // Derived-only slots.
                    vtable[13] = (delegate* unmanaged<void*, int*, int>)&DerivedPropertiesImplementation.ABI.GetDerivedIntProperty;
                    vtable[14] = (delegate* unmanaged<void*, int, int>)&DerivedPropertiesImplementation.ABI.SetDerivedIntProperty;
                    vtable[15] = (delegate* unmanaged<void*, ushort**, int>)&DerivedPropertiesImplementation.ABI.GetDerivedStringProperty;
                    vtable[16] = (delegate* unmanaged<void*, ushort*, int>)&DerivedPropertiesImplementation.ABI.SetDerivedStringProperty;
                    vtable[17] = (delegate* unmanaged<void*, int*, int>)&DerivedPropertiesImplementation.ABI.GetDerivedReadOnlyInt;
                    _derivedPropertiesVtable = vtable;
                    return _derivedPropertiesVtable;
                }
            }

            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                if (obj is DerivedPropertiesImplementation)
                {
                    ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(DerivedPropertiesImplementation), sizeof(ComInterfaceEntry) * 2);
                    // Listing the derived entry first means the default IUnknown for the CCW exposes
                    // the derived vtable, so a QI for IID_IDerivedProperties resolves directly.
                    comInterfaceEntry[0].IID = new Guid(IDerivedProperties.IID);
                    comInterfaceEntry[0].Vtable = (nint)DerivedPropertiesVtable;
                    comInterfaceEntry[1].IID = new Guid(IProperties.IID);
                    comInterfaceEntry[1].Vtable = (nint)BasePropertiesVtable;
                    count = 2;
                    return comInterfaceEntry;
                }
                count = 0;
                return null;
            }

            protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags) => throw new NotImplementedException();
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }

        private sealed class DerivedPropertiesImplementation : IProperties, IDerivedProperties
        {
            private int _int;
            private int _writeOnlyIntSink;
            private Guid _guid;
            private string _string = string.Empty;
            private int _derivedInt;
            private string _derivedString = string.Empty;

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
            IProperties? IProperties.Self
            {
                get => null;
                set { }
            }

            int IDerivedProperties.DerivedIntProperty
            {
                get => _derivedInt;
                set => _derivedInt = value;
            }

            string IDerivedProperties.DerivedStringProperty
            {
                get => _derivedString;
                set => _derivedString = value;
            }

            int IDerivedProperties.DerivedReadOnlyInt => 2222;

            // Hand-rolled COM ABI thunks. Each one matches the unmanaged signature the
            // generated RCW expects on the corresponding vtable slot. Thunks for the
            // inherited (IProperties) slots are referenced from both the base and
            // derived vtables; ComInterfaceDispatch.GetInstance recovers the same
            // managed object regardless of which vtable entry was invoked.
            //
            // Every [UnmanagedCallersOnly] method in this assembly is also picked up by
            // DNNE as a C export; the entry-point names below are explicitly unique to
            // avoid clashing with the IProperties thunks of the same shape declared in
            // Properties.cs. These thunks are never invoked through their DNNE C entry
            // point; only their function-pointer addresses (used as vtable slots) matter.
            public static class ABI
            {
                [UnmanagedCallersOnly(EntryPoint = "dprops_get_IntProperty")]
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

                [UnmanagedCallersOnly(EntryPoint = "dprops_set_IntProperty")]
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

                [UnmanagedCallersOnly(EntryPoint = "dprops_get_ReadOnlyInt")]
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

                [UnmanagedCallersOnly(EntryPoint = "dprops_set_WriteOnlyInt")]
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

                // dnne_guid is declared by Properties.cs for the whole assembly; do not redeclare it here.
                [UnmanagedCallersOnly(EntryPoint = "dprops_get_GuidProperty")]
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

                [UnmanagedCallersOnly(EntryPoint = "dprops_set_GuidProperty")]
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

                [UnmanagedCallersOnly(EntryPoint = "dprops_get_StringProperty")]
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

                [UnmanagedCallersOnly(EntryPoint = "dprops_set_StringProperty")]
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

                [UnmanagedCallersOnly(EntryPoint = "dprops_get_Self")]
                public static int GetSelf(void* @this, void** value)
                {
                    *value = null;
                    return 0;
                }

                [UnmanagedCallersOnly(EntryPoint = "dprops_set_Self")]
                public static int SetSelf(void* @this, void* value)
                {
                    return 0;
                }

                [UnmanagedCallersOnly(EntryPoint = "dprops_get_DerivedIntProperty")]
                public static int GetDerivedIntProperty(void* @this, int* value)
                {
                    try
                    {
                        *value = ComInterfaceDispatch.GetInstance<IDerivedProperties>((ComInterfaceDispatch*)@this).DerivedIntProperty;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly(EntryPoint = "dprops_set_DerivedIntProperty")]
                public static int SetDerivedIntProperty(void* @this, int newValue)
                {
                    try
                    {
                        ComInterfaceDispatch.GetInstance<IDerivedProperties>((ComInterfaceDispatch*)@this).DerivedIntProperty = newValue;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly(EntryPoint = "dprops_get_DerivedStringProperty")]
                public static int GetDerivedStringProperty(void* @this, ushort** value)
                {
                    try
                    {
                        string currValue = ComInterfaceDispatch.GetInstance<IDerivedProperties>((ComInterfaceDispatch*)@this).DerivedStringProperty;
                        *value = Utf16StringMarshaller.ConvertToUnmanaged(currValue);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly(EntryPoint = "dprops_set_DerivedStringProperty")]
                public static int SetDerivedStringProperty(void* @this, ushort* newValue)
                {
                    try
                    {
                        string value = Utf16StringMarshaller.ConvertToManaged(newValue);
                        ComInterfaceDispatch.GetInstance<IDerivedProperties>((ComInterfaceDispatch*)@this).DerivedStringProperty = value;
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly(EntryPoint = "dprops_get_DerivedReadOnlyInt")]
                public static int GetDerivedReadOnlyInt(void* @this, int* value)
                {
                    try
                    {
                        *value = ComInterfaceDispatch.GetInstance<IDerivedProperties>((ComInterfaceDispatch*)@this).DerivedReadOnlyInt;
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
