using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SetupLinkerTrimMode ("link")]
	[SkipPeVerify]

	[KeptInterface (typeof (IUserData))]
	public class MarshalAsCustomMarshalerInterface : IUserData
	{
		[Kept]
		public MarshalAsCustomMarshalerInterface ()
		{
		}

		public static void Main ()
		{
			IUserData icm = new MarshalAsCustomMarshalerInterface ();
			icm.TestM1 (null);

			CustomMarhaler2.StaticMethod ();
		}

		[Kept]
		public void TestM1 (object o)
		{
		}
	}

	[Kept]
	interface IUserData
	{
		[Kept]
		void TestM1 ([MarshalAs (UnmanagedType.CustomMarshaler, MarshalType = "Mono.Linker.Tests.Cases.Attributes.CustomMarhaler1")] object o);
	}

	[Kept]
	[KeptInterface (typeof (ICustomMarshaler))]
	class CustomMarhaler1 : ICustomMarshaler
	{
		[Kept]
		private CustomMarhaler1 ()
		{
		}

		[Kept]
		public static ICustomMarshaler GetInstance (string cookie)
		{
			return new CustomMarhaler1 ();
		}

		[Kept]
		public Object MarshalNativeToManaged (IntPtr pNativeData) => throw new NotImplementedException ();

		[Kept]
		public IntPtr MarshalManagedToNative (Object ManagedObj) => throw new NotImplementedException ();

		[Kept]
		public void CleanUpNativeData (IntPtr pNativeData) => throw new NotImplementedException ();

		[Kept]
		void ICustomMarshaler.CleanUpManagedData (Object ManagedObj) => throw new NotImplementedException ();

		[Kept]
		public int GetNativeDataSize () => throw new NotImplementedException ();

		public void ExtraMethod ()
		{
		}
	}

	[Kept]
	class CustomMarhaler2 : ICustomMarshaler
	{
		public CustomMarhaler2 ()
		{
		}

		public static ICustomMarshaler GetInstance (string cookie)
		{
			return new CustomMarhaler2 ();
		}

		public Object MarshalNativeToManaged (IntPtr pNativeData) => throw new NotImplementedException ();

		public IntPtr MarshalManagedToNative (Object ManagedObj) => throw new NotImplementedException ();

		public void CleanUpNativeData (IntPtr pNativeData) => throw new NotImplementedException ();

		void ICustomMarshaler.CleanUpManagedData (Object ManagedObj) => throw new NotImplementedException ();

		public int GetNativeDataSize () => throw new NotImplementedException ();

		[Kept]
		public static void StaticMethod ()
		{
		}
	}
}
