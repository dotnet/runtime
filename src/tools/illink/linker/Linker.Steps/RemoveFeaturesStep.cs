using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class RemoveFeaturesStep : BaseStep
	{
		public bool FeatureCOM { get; set; }
		public bool FeatureETW { get; set; }

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);
		}

		void ProcessType (TypeDefinition type)
		{
			if (FeatureETW) {
				//
				// The pattern here is that EventSource has IsEnabled method(s) which are
				// always called before accessing any other members. We stub them which should
				// make all other members unreachable
				//
				if (BCL.EventTracingForWindows.IsEventSourceType (type))
					ExcludeEventSource (type);
				else if (BCL.EventTracingForWindows.IsEventSourceImplementation (type))
					ExcludeEventSourceImplementation (type);
			}

			if (RemoveCustomAttributes (type)) {
				if (FeatureCOM && type.IsImport) {
					type.IsImport = false;
				}
			}

			foreach (var field in type.Fields)
				RemoveCustomAttributes (field);
				
			foreach (var method in type.Methods)
				RemoveCustomAttributes (method);

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		void ExcludeEventSource (TypeDefinition type)
		{
			var annotations = Context.Annotations;

			foreach (var method in type.Methods) {
				if (annotations.GetAction (method) != MethodAction.Nothing)
					continue;

				if (method.IsStatic)
					continue;

				if (method.HasCustomAttributes)
					method.CustomAttributes.Clear ();

				if (method.IsDefaultConstructor ()) {
					annotations.SetAction (method, MethodAction.ConvertToStub);
					continue;
				}

				if (method.Name == "IsEnabled" || BCL.IsIDisposableImplementation (method) || method.IsFinalizer ()) {
					annotations.SetAction (method, MethodAction.ConvertToStub);
					continue;
				}

				annotations.SetAction (method, MethodAction.ConvertToThrow);
			}
		}

		void ExcludeEventSourceImplementation (TypeDefinition type)
		{
			var annotations = Context.Annotations;

			foreach (var method in type.Methods) {
				if (annotations.GetAction (method) != MethodAction.Nothing)
					continue;

				if (method.IsStatic)
					continue;

				bool skip = false;
				if (method.HasCustomAttributes) {
					if (!method.IsPrivate) {
						foreach (var attr in method.CustomAttributes) {
							//
							// [NonEvent] attribute is commonly used to mark code which calls
							// IsEnabled we could check for that as well to be more aggressive
							// but for now I haven't seen such code in wild
							//
							if (BCL.EventTracingForWindows.IsNonEventAtribute (attr.AttributeType)) {
								skip = true;
								break;
							}
						}
					}

					method.CustomAttributes.Clear ();
				}

				if (method.IsFinalizer ()) {
					annotations.SetAction (method, MethodAction.ConvertToStub);
					continue;
				}

				if (method.IsConstructor) {
					//
					// Skip when it cannot be easily stubbed 
					//
					if (type.BaseType.HasDefaultConstructor ())
						annotations.SetAction (method, MethodAction.ConvertToStub);

					continue;
				}

				if (!skip)
					annotations.SetAction (method, MethodAction.ConvertToThrow);
			}
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

			case "EventSourceAttribute":
			case "EventAttribute":
			case "EventDataAttribute":
			case "EventFieldAttribute":
			case "EventIgnoreAttribute":
			case "NonEventAttribute":
				if (!FeatureETW || type.Namespace != "System.Diagnostics.Tracing")
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
