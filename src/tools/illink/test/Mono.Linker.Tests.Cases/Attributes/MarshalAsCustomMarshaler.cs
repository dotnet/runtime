using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[KeptModuleReference ("lib")]
	class MarshalAsCustomMarshaler
	{
		static void Main ()
		{
			KeepParamMarshal (null);
			KeepReturnParamMarshal ();
			var k = new KeepFieldMarshaler ();
		}

		[Kept]
		class ParamMarshal
		{
			[Kept]
			public static ICustomMarshaler GetInstance (string s) => null;
		}

		[Kept]
		[DllImport ("lib")]
		static extern void KeepParamMarshal ([MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (ParamMarshal))] string s);

		[Kept]
		class RetParamMarshal
		{
			[Kept]
			public static ICustomMarshaler GetInstance (string s) => null;
		}

		[Kept]
		[DllImport ("lib")]
		[return: MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (RetParamMarshal))]
		static extern int KeepReturnParamMarshal ();

		[Kept]
		class FieldMarshal
		{
			[Kept]
			public static ICustomMarshaler GetInstance (string s) => null;
		}

		[Kept]
		struct KeepFieldMarshaler
		{
			[Kept]
			[MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (FieldMarshal))]
			int _f;
		}
	}
}
