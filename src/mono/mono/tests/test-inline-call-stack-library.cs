using System;
using System.Diagnostics;
using System.Reflection;

namespace Library {
	public class StaticFlag {
		private static bool _flag = false;
		public static bool Flag {
			get {
				return _flag;
			}
			set {
				_flag = value;
			}
		}
		
	}
	
	public class CallingAssemblyDependant {
		private string _calledFrom;
		public string CalledFrom {
			get {
				return _calledFrom;
			}
		}
		
		public CallingAssemblyDependant () {
			_calledFrom = Assembly.GetCallingAssembly ().GetName ().Name;
		}
		
		public static string CalledFromLibrary () {
			return new CallingAssemblyDependant ().CalledFrom;
		}
	}
	
	public class ResourceRelaxedFieldInit {		
		private static ResourceRelaxedFieldInit _singleResource = new ResourceRelaxedFieldInit ();
		public static ResourceRelaxedFieldInit Single {
			get {
				return _singleResource;
			}
		}
		
		private bool _flag;
		public bool Flag {
			get {
				return _flag;
			}
		}
		
		public ResourceRelaxedFieldInit () {
			_flag = StaticFlag.Flag;
		}
	}
	
	public class ResourceStrictFieldInit {		
		private static ResourceStrictFieldInit _singleResource = new ResourceStrictFieldInit ();
		public static ResourceStrictFieldInit Single {
			get {
				return _singleResource;
			}
		}
		
		private bool _flag;
		public bool Flag {
			get {
				return _flag;
			}
		}
		
		public ResourceStrictFieldInit () {
			_flag = StaticFlag.Flag;
		}
		
		static ResourceStrictFieldInit () {
		}
	}
	
	public class InlinedMethods {
		public static MethodBase GetCurrentMethod () {
			return MethodBase.GetCurrentMethod ();
		}
		public static Assembly GetExecutingAssembly () {
			return Assembly.GetExecutingAssembly ();
		}
		public static Assembly GetCallingAssembly () {
			return Assembly.GetCallingAssembly ();
		}
		public static Assembly CallCallingAssembly () {
			return GetCallingAssembly ();
		}
		public static StackFrame GetStackFrame () {
			return new StackFrame ();
		}
		public static ResourceRelaxedFieldInit GetResourceRelaxedFieldInit () {
			return ResourceRelaxedFieldInit.Single;
		}
		public static ResourceStrictFieldInit GetResourceStrictFieldInit () {
			return ResourceStrictFieldInit.Single;
		}
	}
}
