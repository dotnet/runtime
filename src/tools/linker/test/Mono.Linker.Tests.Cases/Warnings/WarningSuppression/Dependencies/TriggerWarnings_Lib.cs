using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression.Dependencies
{
	public class TriggerWarnings_Lib
	{
		public static void Main ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			Warning1 ();
			var getProperty = Warning2;
			NestedType.Warning3 ();
			var list = new List<int> ();
			NestedType.Warning4 (ref list);
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (TriggerWarnings_Lib);
		}

		public static void Warning1 ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public static int Warning2 {
			get {
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				return 0;
			}
		}

		public class NestedType
		{
			public static void Warning3 ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}

			public static void Warning4<T> (ref List<T> p)
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}
	}
}
