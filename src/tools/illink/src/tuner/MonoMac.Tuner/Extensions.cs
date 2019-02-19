using System;
using System.Collections.Generic;

using Mono.Cecil;

using Mono.Linker;

using Mono.Tuner;

namespace MonoMac.Tuner {

	static class Extensions {

		const string NSObject = "MonoMac.Foundation.NSObject";
		const string INativeObject = "MonoMac.ObjCRuntime.INativeObject";

		public static bool IsNSObject (this TypeDefinition type)
		{
			return type.Inherits (NSObject);
		}

		public static bool IsNativeObject (this TypeDefinition type)
		{
			return type.Implements (INativeObject);
		}
	}
}
