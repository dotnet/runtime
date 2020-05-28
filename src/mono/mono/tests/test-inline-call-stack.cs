using System;
using System.Diagnostics;
using System.Reflection;
using Library;


namespace Program {
	public class Test {
		public static string TestPassed (bool value) {
			return value ? "PASSED" : "FAILED";
		}
		public static string TestFailed (bool value) {
			return TestPassed (! value);
		}
		
		public static int Main () {
			MethodBase myMethodBase = MethodBase.GetCurrentMethod ();
			MethodBase inlinedMethodBase = InlinedMethods.GetCurrentMethod ();
			
			Assembly myExecutingAssembly = Assembly.GetExecutingAssembly ();
			Assembly inlinedExecutingAssembly = InlinedMethods.GetExecutingAssembly ();
			
			Assembly myCallingAssembly = Assembly.GetCallingAssembly ();
			Assembly inlinedCallingAssembly = InlinedMethods.CallCallingAssembly ();
			
			StackFrame myStackFrame = new StackFrame ();
			StackFrame inlinedStackFrame = InlinedMethods.GetStackFrame ();
			
			string myConstructorCalledFrom = new CallingAssemblyDependant ().CalledFrom;
			string inlinedConstructorCalledFrom = CallingAssemblyDependant.CalledFromLibrary ();
			
			StaticFlag.Flag = true;
			bool strictFlag = ResourceStrictFieldInit.Single.Flag;
			bool relaxedFlag = ResourceRelaxedFieldInit.Single.Flag;
			
			Console.WriteLine ("[{0}]CurrentMethod: my {1}, inlined {2}, equals {3}",
					TestFailed (myMethodBase == inlinedMethodBase),
					myMethodBase.Name, inlinedMethodBase.Name,
					myMethodBase == inlinedMethodBase);
			
			Console.WriteLine ("[{0}]ExecutingAssembly: my {1}, inlined {2}, equals {3}",
					TestFailed (myExecutingAssembly == inlinedExecutingAssembly),
					myExecutingAssembly.GetName ().Name, inlinedExecutingAssembly.GetName ().Name,
					myExecutingAssembly == inlinedExecutingAssembly);
			
			Console.WriteLine ("[{0}]CallingAssembly: my {1}, inlined {2}, equals {3}",
					TestFailed (myCallingAssembly == inlinedCallingAssembly),
					myCallingAssembly.GetName ().Name, inlinedCallingAssembly.GetName ().Name,
					myCallingAssembly == inlinedCallingAssembly);
			
			Console.WriteLine ("[{0}]StackFrame.GetMethod: my {1}, inlined {2}, equals {3}",
					TestFailed (myStackFrame.GetMethod ().Name == inlinedStackFrame.GetMethod ().Name),
					myStackFrame.GetMethod ().Name, inlinedStackFrame.GetMethod ().Name,
					myStackFrame.GetMethod ().Name == inlinedStackFrame.GetMethod ().Name);
			
			Console.WriteLine ("[{0}]ConstructorCalledFrom: my {1}, inlined {2}, equals {3}",
					TestFailed (myConstructorCalledFrom == inlinedConstructorCalledFrom),
					myConstructorCalledFrom, inlinedConstructorCalledFrom,
					myConstructorCalledFrom == inlinedConstructorCalledFrom);

			/*
			 * The relaxedFlag test is broken, the runtime can initialized
			 * to false before the StaticFlag.Flag = true assignment is ran.
			 */
			relaxedFlag = true;
			
			Console.WriteLine ("[{0}]strictFlag: {1}, relaxedFlag: {2}",
					TestFailed ((strictFlag != relaxedFlag)),
					strictFlag, relaxedFlag);
			if ((myMethodBase != inlinedMethodBase) &&
					(myExecutingAssembly != inlinedExecutingAssembly) &&
					(myCallingAssembly != inlinedCallingAssembly) &&
					(myStackFrame.GetMethod ().Name != inlinedStackFrame.GetMethod ().Name) &&
					(myConstructorCalledFrom != inlinedConstructorCalledFrom) &&
					(strictFlag == relaxedFlag)) {
				return 0;
			} else {
				return 1;
			}
		}
	}
}

