using System;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps {
	public class RemoveSecurityStep : BaseStep {
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) == AssemblyAction.Link) {
				ClearSecurityDeclarations (assembly);
				RemoveCustomAttributesThatAreForSecurity (assembly);

				foreach (var type in assembly.MainModule.Types)
					ProcessType (type);
			}
		}

		static void ProcessType (TypeDefinition type)
		{
			RemoveCustomAttributesThatAreForSecurity (type);
			ClearSecurityDeclarations (type);
			type.HasSecurity = false;

			foreach (var field in type.Fields)
				RemoveCustomAttributesThatAreForSecurity (field);

			foreach (var method in type.Methods) {
				ClearSecurityDeclarations (method);
				RemoveCustomAttributesThatAreForSecurity (method);
				method.HasSecurity = false;
			}

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		static void ClearSecurityDeclarations (ISecurityDeclarationProvider provider)
		{
			provider.SecurityDeclarations.Clear ();
		}

		/// <summary>
		/// We have to remove some security attributes, otherwise pe verify will complain that a type has HasSecurity = false
		/// </summary>
		/// <param name="provider"></param>
		static void RemoveCustomAttributesThatAreForSecurity (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			var attrsToRemove = provider.CustomAttributes.Where (IsCustomAttributeForSecurity).ToArray ();
			foreach (var remove in attrsToRemove)
				provider.CustomAttributes.Remove (remove);
		}

		static bool IsCustomAttributeForSecurity (CustomAttribute attr)
		{
			switch (attr.AttributeType.FullName) {
			case "System.Security.SecurityCriticalAttribute":
			case "System.Security.SecuritySafeCriticalAttribute":
			case "System.Security.SuppressUnmanagedCodeSecurityAttribute":
				return true;
			}

			return false;
		}
	}
}