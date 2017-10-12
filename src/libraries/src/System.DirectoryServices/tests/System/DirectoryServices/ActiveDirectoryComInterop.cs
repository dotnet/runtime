// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace ActiveDirectoryComInterop
{
    // , TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FDispatchable)
	[Guid("9068270B-0939-11D1-8BE1-00C04FD8D503")]
	[ComImport]
	public interface IADsLargeInteger
	{
		[DispId(2)]
		int HighPart
		{
			[DispId(2)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[DispId(2)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[DispId(3)]
		int LowPart
		{
			[DispId(3)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[DispId(3)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}
	}

    // , TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FDispatchable)
	[Guid("FD8256D0-FD15-11CE-ABC4-02608C9E7553")]
	[ComImport]
	public interface IADs
	{
		[DispId(2)]
		string Name
		{
			[DispId(2)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		[DispId(3)]
		string Class
		{
			[DispId(3)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		[DispId(4)]
		string GUID
		{
			[DispId(4)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		[DispId(5)]
		string ADsPath
		{
			[DispId(5)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		[DispId(6)]
		string Parent
		{
			[DispId(6)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		[DispId(7)]
		string Schema
		{
			[DispId(7)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		[DispId(8)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		void GetInfo();

		[DispId(9)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		void SetInfo();

		[DispId(10)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.Struct)]
		object Get([MarshalAs(UnmanagedType.BStr)] [In] string bstrName);

		[DispId(11)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		void Put([MarshalAs(UnmanagedType.BStr)] [In] string bstrName, [MarshalAs(UnmanagedType.Struct)] [In] object vProp);

		[DispId(12)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetEx([MarshalAs(UnmanagedType.BStr)] [In] string bstrName);

		[DispId(13)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		void PutEx([In] int lnControlCode, [MarshalAs(UnmanagedType.BStr)] [In] string bstrName, [MarshalAs(UnmanagedType.Struct)] [In] object vProp);

		[DispId(14)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		void GetInfoEx([MarshalAs(UnmanagedType.Struct)] [In] object vProperties, [In] int lnReserved);
	}

    // , TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FDispatchable)
	[Guid("B8C787CA-9BDD-11D0-852C-00C04FD8D503")]
	[ComImport]
	public interface IADsSecurityDescriptor
	{
		[DispId(2)]
		int Revision
		{
			[DispId(2)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[DispId(2)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[DispId(3)]
		int Control
		{
			[DispId(3)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[DispId(3)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[DispId(4)]
		string Owner
		{
			[DispId(4)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			[DispId(4)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[param: MarshalAs(UnmanagedType.BStr)]
			set;
		}

		[DispId(5)]
		bool OwnerDefaulted
		{
			[DispId(5)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[DispId(5)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[DispId(6)]
		string Group
		{
			[DispId(6)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			[DispId(6)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[param: MarshalAs(UnmanagedType.BStr)]
			set;
		}

		[DispId(7)]
		bool GroupDefaulted
		{
			[DispId(7)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[DispId(7)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[DispId(8)]
		object DiscretionaryAcl
		{
			[DispId(8)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.IDispatch)]
			get;
			[DispId(8)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[param: MarshalAs(UnmanagedType.IDispatch)]
			set;
		}

		[DispId(9)]
		bool DaclDefaulted
		{
			[DispId(9)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[DispId(9)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[DispId(10)]
		object SystemAcl
		{
			[DispId(10)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[return: MarshalAs(UnmanagedType.IDispatch)]
			get;
			[DispId(10)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			[param: MarshalAs(UnmanagedType.IDispatch)]
			set;
		}

		[DispId(11)]
		bool SaclDefaulted
		{
			[DispId(11)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[DispId(11)]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[DispId(12)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.IDispatch)]
		object CopySecurityDescriptor();
	}
}
