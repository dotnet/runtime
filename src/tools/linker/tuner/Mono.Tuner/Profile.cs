using System;
using System.Collections.Generic;

using Mono.Cecil;

namespace Mono.Tuner {

	abstract class Profile {

		static Profile current;

		static Profile Current {
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

		public static bool IsProductAssembly (AssemblyDefinition assembly)
		{
			return Current.IsProduct (assembly);
		}

		protected abstract bool IsSdk (AssemblyDefinition assembly);
		protected abstract bool IsProduct (AssemblyDefinition assembly);
	}
}
