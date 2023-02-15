using System;
using System.Reflection;

// Disable warning about ambigous types - linker only looks at names, not definition assemblies
#pragma warning disable 0436

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
{
	public class CoreLibEmulator
	{
		public static void Test ()
		{
#if INCLUDE_CORELIB_IMPL
			Type t = new Type();
			t.GetConstructor (BindingFlags.Default, null, new Type[] {}, null);
			t.GetMethod ("name", new Type[] {});
			t.GetProperty ("name", new Type[] {});
			t.GetField ("name");
			t.GetEvent ("name");
#endif
		}
	}
}

#if INCLUDE_CORELIB_IMPL
namespace System
{
	public class Type
	{
		public ConstructorInfo GetConstructor (BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) => GetConstructor (bindingAttr, binder, CallingConventions.Any, types, modifiers);

		public ConstructorInfo GetConstructor (BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			return null;
		}

		public MethodInfo GetMethod (string name, Type [] types) => GetMethod (name, types, null);
		public MethodInfo GetMethod (string name, Type [] types, ParameterModifier [] modifiers)
		{
			return null;
		}

		public PropertyInfo GetProperty (string name, Type [] types) => GetProperty (name, null, types);
		public PropertyInfo GetProperty (string name, Type returnType, Type [] types)
		{
			return null;
		}

		public FieldInfo GetField (string name) => GetField (name, BindingFlags.Default);
		public FieldInfo GetField (string name, BindingFlags bindingAttr)
		{
			return null;
		}

		public EventInfo GetEvent (string name) => GetEvent (name, BindingFlags.Default);
		public EventInfo GetEvent (string name, BindingFlags bindingAttr)
		{
			return null;
		}
	}
}
#endif
