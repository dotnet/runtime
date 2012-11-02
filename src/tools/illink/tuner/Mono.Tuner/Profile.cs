using System;
using System.Collections.Generic;

using Mono.Cecil;

namespace Mono.Tuner {

	public abstract class Profile {

		static Profile current;

		public static Profile Current {
			get {
				if (current != null)
					return current;

				current = CreateProfile ("MonoTouch");
				if (current != null)
					return current;

				current = CreateProfile ("MonoDroid");
				if (current != null)
					return current;

				current = CreateProfile ("MonoMac");
				if (current != null)
					return current;

				throw new NotSupportedException ("No active profile");
			}
			set {
				current = value;
			}
		}

		static Profile CreateProfile (string name)
		{
			var type = Type.GetType (string.Format ("{0}.Tuner.{0}Profile", name));
			if (type == null)
				return null;

			return (Profile) Activator.CreateInstance (type);
		}

		public static bool IsSdkAssembly (AssemblyDefinition assembly)
		{
			return Current.IsSdk (assembly);
		}

		public static bool IsSdkAssembly (string assemblyName)
		{
			return Current.IsSdk (assemblyName);
		}

		public static bool IsProductAssembly (AssemblyDefinition assembly)
		{
			return Current.IsProduct (assembly);
		}

		public static bool IsProductAssembly (string assemblyName)
		{
			return Current.IsProduct (assemblyName);
		}

		protected virtual bool IsSdk (AssemblyDefinition assembly)
		{
			return IsSdk (assembly.Name.Name);
		}
		
		protected virtual bool IsProduct (AssemblyDefinition assembly)
		{
			return IsProduct (assembly.Name.Name);
		}

		protected abstract bool IsSdk (string assemblyName);
		protected abstract bool IsProduct (string assemblyName);
	}
}
