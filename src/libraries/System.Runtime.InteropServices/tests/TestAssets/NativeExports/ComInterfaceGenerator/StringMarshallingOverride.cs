// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using SharedTypes.ComInterfaces;
using static System.Runtime.InteropServices.ComWrappers;

namespace NativeExports.ComInterfaceGenerator
{
    public unsafe partial class StringMarshallingOverride
    {
        [UnmanagedCallersOnly(EntryPoint = "new_string_marshalling_override")]
        public static void* CreateStringMarshallingOverrideObject()
        {
            MyComWrapper cw = new();
            var myObject = new Implementation();
            nint ptr = cw.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);
            return (void*)ptr;
        }

        class MyComWrapper : ComWrappers
        {
            static void* _s_comInterfaceVTable = null;
            static void* S_VTable
            {
                get
                {
                    if (_s_comInterfaceVTable != null)
                        return _s_comInterfaceVTable;
                    void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(GetAndSetInt), sizeof(void*) * 6);
                    GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                    vtable[0] = (void*)fpQueryInterface;
                    vtable[1] = (void*)fpAddReference;
                    vtable[2] = (void*)fpRelease;
                    vtable[3] = (delegate* unmanaged<void*, byte*, byte**, int>)&Implementation.ABI.StringMarshallingUtf8;
                    vtable[4] = (delegate* unmanaged<void*, ushort*, ushort**, int>)&Implementation.ABI.MarshalAsLPWStr;
                    vtable[5] = (delegate* unmanaged<void*, ushort*, ushort**, int>)&Implementation.ABI.MarshalUsingUtf16;
                    _s_comInterfaceVTable = vtable;
                    return _s_comInterfaceVTable;
                }
            }

            static void* _s_derivedVTable = null;
            static void* S_DerivedVTable
            {
                get
                {
                    if (_s_comInterfaceVTable != null)
                        return _s_comInterfaceVTable;
                    void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(GetAndSetInt), sizeof(void*) * 9);
                    GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                    vtable[0] = (void*)fpQueryInterface;
                    vtable[1] = (void*)fpAddReference;
                    vtable[2] = (void*)fpRelease;
                    vtable[3] = (delegate* unmanaged<void*, byte*, byte**, int>)&Implementation.ABI.StringMarshallingUtf8;
                    vtable[4] = (delegate* unmanaged<void*, ushort*, ushort**, int>)&Implementation.ABI.MarshalAsLPWStr;
                    vtable[5] = (delegate* unmanaged<void*, ushort*, ushort**, int>)&Implementation.ABI.MarshalUsingUtf16;
                    vtable[6] = (delegate* unmanaged<void*, byte*, byte**, int>)&Implementation.ABI.StringMarshallingUtf8_2;
                    vtable[7] = (delegate* unmanaged<void*, ushort*, ushort**, int>)&Implementation.ABI.MarshalAsLPWStr_2;
                    vtable[8] = (delegate* unmanaged<void*, ushort*, ushort**, int>)&Implementation.ABI.MarshalUsingUtf16_2;
                    _s_comInterfaceVTable = vtable;
                    return _s_comInterfaceVTable;
                }
            }

            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                if (obj is IStringMarshallingOverrideDerived)
                {
                    ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Implementation), sizeof(ComInterfaceEntry) * 2);
                    comInterfaceEntry[0].IID = new Guid(IStringMarshallingOverrideDerived._guid);
                    comInterfaceEntry[0].Vtable = (nint)S_DerivedVTable;
                    comInterfaceEntry[1].IID = new Guid(IStringMarshallingOverride._guid);
                    comInterfaceEntry[1].Vtable = (nint)S_VTable;
                    count = 2;
                    return comInterfaceEntry;
                }
                if (obj is IStringMarshallingOverride)
                {
                    ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Implementation), sizeof(ComInterfaceEntry));
                    comInterfaceEntry->IID = new Guid(IStringMarshallingOverride._guid);
                    comInterfaceEntry->Vtable = (nint)S_VTable;
                    count = 1;
                    return comInterfaceEntry;
                }
                count = 0;
                return null;
            }

            protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags) => throw new NotImplementedException();
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }

        partial class Implementation : IStringMarshallingOverride, IStringMarshallingOverrideDerived
        {
            string _data = "Your string: ";
            string IStringMarshallingOverride.StringMarshallingUtf8(string input) => _data + input;
            string IStringMarshallingOverride.MarshalAsLPWString(string input) => _data + input;
            string IStringMarshallingOverride.MarshalUsingUtf16(string input) => _data + input;

            string _data2 = "Your string 2: ";
            string IStringMarshallingOverrideDerived.StringMarshallingUtf8_2(string input) => _data2 + input;
            string IStringMarshallingOverrideDerived.MarshalAsLPWString_2(string input) => _data2 + input;
            string IStringMarshallingOverrideDerived.MarshalUsingUtf16_2(string input) => _data2 + input;

            // Provides function pointers in the COM format to use in COM VTables
            public static class ABI
            {
                [UnmanagedCallersOnly]
                public static int StringMarshallingUtf8(void* @this, byte* input, byte** output)
                {
                    try
                    {
                        string inputStr = Utf8StringMarshaller.ConvertToManaged(input);
                        string currValue = ComInterfaceDispatch.GetInstance<IStringMarshallingOverride>((ComInterfaceDispatch*)@this).StringMarshallingUtf8(inputStr);
                        *output = Utf8StringMarshaller.ConvertToUnmanaged(currValue);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int MarshalAsLPWStr(void* @this, ushort* input, ushort** output)
                {
                    try
                    {
                        string inputStr = Utf16StringMarshaller.ConvertToManaged(input);
                        string currValue = ComInterfaceDispatch.GetInstance<IStringMarshallingOverride>((ComInterfaceDispatch*)@this).MarshalAsLPWString(inputStr);
                        *output = Utf16StringMarshaller.ConvertToUnmanaged(currValue);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int MarshalUsingUtf16(void* @this, ushort* input, ushort** output)
                {
                    try
                    {
                        string inputStr = Utf16StringMarshaller.ConvertToManaged(input);
                        string currValue = ComInterfaceDispatch.GetInstance<IStringMarshallingOverride>((ComInterfaceDispatch*)@this).MarshalUsingUtf16(inputStr);
                        *output = Utf16StringMarshaller.ConvertToUnmanaged(currValue);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int StringMarshallingUtf8_2(void* @this, byte* input, byte** output)
                {
                    try
                    {
                        string inputStr = Utf8StringMarshaller.ConvertToManaged(input);
                        string currValue = ComInterfaceDispatch.GetInstance<IStringMarshallingOverrideDerived>((ComInterfaceDispatch*)@this).StringMarshallingUtf8_2(inputStr);
                        *output = Utf8StringMarshaller.ConvertToUnmanaged(currValue);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int MarshalAsLPWStr_2(void* @this, ushort* input, ushort** output)
                {
                    try
                    {
                        string inputStr = Utf16StringMarshaller.ConvertToManaged(input);
                        string currValue = ComInterfaceDispatch.GetInstance<IStringMarshallingOverrideDerived>((ComInterfaceDispatch*)@this).MarshalAsLPWString_2(inputStr);
                        *output = Utf16StringMarshaller.ConvertToUnmanaged(currValue);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int MarshalUsingUtf16_2(void* @this, ushort* input, ushort** output)
                {
                    try
                    {
                        string inputStr = Utf16StringMarshaller.ConvertToManaged(input);
                        string currValue = ComInterfaceDispatch.GetInstance<IStringMarshallingOverrideDerived>((ComInterfaceDispatch*)@this).MarshalUsingUtf16_2(inputStr);
                        *output = Utf16StringMarshaller.ConvertToUnmanaged(currValue);
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
