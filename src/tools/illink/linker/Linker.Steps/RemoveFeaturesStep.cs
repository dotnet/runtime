using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class RemoveFeaturesStep : BaseStep
	{
		public bool FeatureCOM { get; set; }

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) == AssemblyAction.Link) {
				foreach (var type in assembly.MainModule.Types)
					ProcessType (type);
			}
		}

		void ProcessType (TypeDefinition type)
		{
			if (RemoveCustomAttributes (type)) {
				if (FeatureCOM && type.IsImport) {
					type.IsImport = false;
				}
			}

			foreach (var field in type.Fields)
				RemoveCustomAttributes (field);

			foreach (var method in type.Methods) {
				RemoveCustomAttributes (method);
			}

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		bool RemoveCustomAttributes (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return false;

			var attrsToRemove = provider.CustomAttributes.Where (IsCustomAttributeExcluded).ToArray ();
			foreach (var remove in attrsToRemove)
				provider.CustomAttributes.Remove (remove);
			
			return attrsToRemove.Length > 0;
		}

		bool IsCustomAttributeExcluded (CustomAttribute attr)
		{
			var type = attr.AttributeType;

			switch (type.Name) {
			default:
				return false;

			case "ComDefaultInterfaceAttribute":
			case "ComVisibleAttribute":
			case "ClassInterfaceAttribute":
			case "InterfaceTypeAttribute":
			case "DispIdAttribute":
			case "TypeLibImportClassAttribute":
			case "ComRegisterFunctionAttribute":
			case "ComUnregisterFunctionAttribute":
			case "ProgIdAttribute":
			case "ImportedFromTypeLibAttribute":
			case "IDispatchImplAttribute":
			case "ComSourceInterfacesAttribute":
			case "ComConversionLossAttribute":
			case "TypeLibTypeAttribute":
			case "TypeLibFuncAttribute":
			case "TypeLibVarAttribute":
			case "ComImportAttribute":
			case "GuidAttribute":
			case "ComAliasNameAttribute":
			case "AutomationProxyAttribute":
			case "PrimaryInteropAssemblyAttribute":
			case "CoClassAttribute":
			case "ComEventInterfaceAttribute":
			case "TypeLibVersionAttribute":
			case "ComCompatibleVersionAttribute":
			case "SetWin32ContextInIDispatchAttribute":
			case "ManagedToNativeComInteropStubAttribute":
				if (!FeatureCOM || type.Namespace != "System.Runtime.InteropServices")
					return false;

				break;
			}

			var definition = type.Resolve ();
			if (!Annotations.IsPreserved (definition))
				return true;

			//
			// We allow xml descriptor to override feature attributes which should be
			// removed
			//
			return Annotations.GetPreserve (definition) != TypePreserve.All;
		}
	}
}
