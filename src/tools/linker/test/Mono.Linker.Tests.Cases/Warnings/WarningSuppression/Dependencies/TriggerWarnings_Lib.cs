using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression.Dependencies
{
	public class TriggerWarnings_Lib
	{
		public static Type UnrecognizedPattern ()
		{
			return typeof (TriggerWarnings_Lib);
		}

		public static void Main ()
		{
			Expression.Call (UnrecognizedPattern (), "", Type.EmptyTypes);
		}
	}
}
