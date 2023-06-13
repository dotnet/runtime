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
	public unsafe class StringMarshalling
	{
		[UnmanagedCallersOnly(EntryPoint = "new_utf8_marshalling")]
		public static void* CreateUtf8ComObject()
		{
			MyComWrapper cw = new();
			var myObject = new Utf8Implementation();
			nint ptr = cw.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);

			return (void*)ptr;
		}

		[UnmanagedCallersOnly(EntryPoint = "new_utf16_marshalling")]
		public static void* CreateUtf16ComObject()
		{
			MyComWrapper cw = new();
			var myObject = new Utf16Implementation();
			nint ptr = cw.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);

			return (void*)ptr;
		}

		class MyComWrapper : ComWrappers
		{
			static void* _s_comInterface1VTable = null;
			static void* _s_comInterface2VTable = null;
			static void* S_Utf8VTable
			{
				get
				{
					if (_s_comInterface1VTable != null)
						return _s_comInterface1VTable;
					void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(GetAndSetInt), sizeof(void*) * 5);
					GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
					vtable[0] = (void*)fpQueryInterface;
					vtable[1] = (void*)fpAddReference;
					vtable[2] = (void*)fpRelease;
					vtable[3] = (delegate* unmanaged<void*, byte**, int>)&Utf8Implementation.ABI.GetStringUtf8;
					vtable[4] = (delegate* unmanaged<void*, byte*, int>)&Utf8Implementation.ABI.SetStringUtf8;
					_s_comInterface1VTable = vtable;
					return _s_comInterface1VTable;
				}
			}
			static void* S_Utf16VTable
			{
				get
				{
					if (_s_comInterface2VTable != null)
						return _s_comInterface2VTable;
					void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(GetAndSetInt), sizeof(void*) * 5);
					GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
					vtable[0] = (void*)fpQueryInterface;
					vtable[1] = (void*)fpAddReference;
					vtable[2] = (void*)fpRelease;
					vtable[3] = (delegate* unmanaged<void*, ushort**, int>)&Utf16Implementation.ABI.GetStringUtf16;
					vtable[4] = (delegate* unmanaged<void*, ushort*, int>)&Utf16Implementation.ABI.SetStringUtf16;
					_s_comInterface2VTable = vtable;
					return _s_comInterface2VTable;
				}
			}

			protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
			{
				if (obj is IUTF8Marshalling)
				{
					ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Utf8Implementation), sizeof(ComInterfaceEntry));
					comInterfaceEntry->IID = new Guid(IUTF8Marshalling._guid);
					comInterfaceEntry->Vtable = (nint)S_Utf8VTable;
					count = 1;
					return comInterfaceEntry;
				}
				else if (obj is IUTF16Marshalling)
				{
					ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Utf16Implementation), sizeof(ComInterfaceEntry));
					comInterfaceEntry->IID = new Guid(IUTF16Marshalling._guid);
					comInterfaceEntry->Vtable = (nint)S_Utf16VTable;
					count = 1;
					return comInterfaceEntry;
				}
				count = 0;
				return null;
			}

			protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags) => throw new NotImplementedException();
			protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
		}

		class Utf8Implementation : IUTF8Marshalling
		{
			string _data = "Hello, World!";

			string IUTF8Marshalling.GetString()
			{
				return _data;
			}
			void IUTF8Marshalling.SetString(string x)
			{
				_data = x;
			}

			// Provides function pointers in the COM format to use in COM VTables
			public static class ABI
			{
				[UnmanagedCallersOnly]
				public static int GetStringUtf8(void* @this, byte** value)
				{
					try
					{
						string currValue = ComInterfaceDispatch.GetInstance<IUTF8Marshalling>((ComInterfaceDispatch*)@this).GetString();
						*value = Utf8StringMarshaller.ConvertToUnmanaged(currValue);
						return 0;
					}
					catch (Exception e)
					{
						return e.HResult;
					}
				}

				[UnmanagedCallersOnly]
				public static int SetStringUtf8(void* @this, byte* newValue)
				{
					try
					{
						string value = Utf8StringMarshaller.ConvertToManaged(newValue);
						ComInterfaceDispatch.GetInstance<IUTF8Marshalling>((ComInterfaceDispatch*)@this).SetString(value);
						return 0;
					}
					catch (Exception e)
					{
						return e.HResult;
					}
				}
			}
		}

		class Utf16Implementation : IUTF16Marshalling
		{
			string _data = "Hello, World!";

			string IUTF16Marshalling.GetString()
			{
				return _data;
			}
			void IUTF16Marshalling.SetString(string x)
			{
				_data = x;
			}

			// Provides function pointers in the COM format to use in COM VTables
			public static class ABI
			{
				[UnmanagedCallersOnly]
				public static int GetStringUtf16(void* @this, ushort** value)
				{
					try
					{
						string currValue = ComInterfaceDispatch.GetInstance<IUTF16Marshalling>((ComInterfaceDispatch*)@this).GetString();
						*value = Utf16StringMarshaller.ConvertToUnmanaged(currValue);
						return 0;
					}
					catch (Exception e)
					{
						return e.HResult;
					}
				}

				[UnmanagedCallersOnly]
				public static int SetStringUtf16(void* @this, ushort* newValue)
				{
					try
					{
						string value = Utf16StringMarshaller.ConvertToManaged(newValue);
						ComInterfaceDispatch.GetInstance<IUTF16Marshalling>((ComInterfaceDispatch*)@this).SetString(value);
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
