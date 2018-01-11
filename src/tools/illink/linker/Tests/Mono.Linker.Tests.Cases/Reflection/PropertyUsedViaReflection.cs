using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class PropertyUsedViaReflection {
		public static void Main ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("OnlyUsedViaReflection");
			property.GetValue (null, new object[] { });

			property = typeof (PropertyUsedViaReflection).GetProperty ("SetterOnly");
			property.SetValue (null, 42, new object[] { });

			property = typeof (PropertyUsedViaReflection).GetProperty ("GetterOnly");
			property.GetValue (null, new object[] { });
		}

		[Kept]
		static int _field;

		[Kept]
		static int OnlyUsedViaReflection {
			[Kept]
			get { return _field; }
			[Kept]
			set { _field = value; }
		}

		[Kept]
		static int SetterOnly {
			[Kept]
			set { _field = value; }
		}

		[Kept]
		static int GetterOnly {
			[Kept]
			get { return _field; }
		}
	}
}
