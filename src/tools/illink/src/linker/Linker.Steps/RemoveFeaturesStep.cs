using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class RemoveFeaturesStep : BaseStep
	{
		//
		// When any of the features bellow is set, the linker will remove the code and change
		// the behaviour of the program but it should not cause it to crash
		//

		public bool FeatureCOM { get; set; }
		public bool FeatureETW { get; set; }
		public bool FeatureSRE { get; set; }

		//
		// Manually overrides System.Globalization.Invariant mode
		// https://github.com/dotnet/corefx/blob/master/Documentation/architecture/globalization-invariant-mode.md
		//
		public bool FeatureGlobalization { get; set; }

		readonly static string[] MonoCollationResources = new [] {
			"collation.cjkCHS.bin",
			"collation.cjkCHT.bin",
			"collation.cjkJA.bin",
			"collation.cjkKO.bin",
			"collation.cjkKOlv2.bin",
			"collation.core.bin",
			"collation.tailoring.bin"
		};

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);

			if (FeatureGlobalization) {
				foreach (var res in MonoCollationResources)
					Context.Annotations.AddResourceToRemove (assembly, res);
			}
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

			if (FeatureSRE) {
				if (type.Namespace == "System" && type.Name == "RuntimeType") {
					foreach (var method in type.Methods) {
						if (method.Name == "MakeTypeBuilderInstantiation") {
							Annotations.SetAction (method, MethodAction.ConvertToThrow);
							break;
						}
					}
				}
			}

			if (FeatureGlobalization)
				ExcludeGlobalization (type);

			if (RemoveCustomAttributes (type)) {
				if (FeatureCOM && type.IsImport) {
					type.IsImport = false;
				}
			}

			if (FeatureGlobalization)
				ExcludeMonoCollation (type);

			foreach (var field in type.Fields)
				RemoveCustomAttributes (field);
				
			foreach (var method in type.Methods)
				RemoveCustomAttributes (method);

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		void ExcludeGlobalization (TypeDefinition type)
		{
			switch (type.Namespace) {
			case "System.Globalization":
				switch (type.Name) {
				case "CalendarData":
					foreach (var method in type.Methods) {
						switch (method.Name) {
						case "GetJapaneseEraNames":
						case "GetJapaneseEnglishEraNames":
							Annotations.SetAction (method, MethodAction.ConvertToThrow);
							break;
						}
					}
					break;
				case "DateTimeFormatInfo":
					foreach (var method in type.Methods) {
						switch (method.Name) {
						case "PopulateSpecialTokenHashTable":
						case "GetJapaneseCalendarDTFI":
						case "GetTaiwanCalendarDTFI":
						case "IsJapaneseCalendar":
						case "TryParseHebrewNumber":
							Annotations.SetAction (method, MethodAction.ConvertToThrow);
							break;
						}
					}
					break;
				}
				break;
			case "System":
				switch (type.Name) {
				case "DateTimeFormat":
					foreach (var method in type.Methods) {
						switch (method.Name) {
						case "HebrewFormatDigits":
						case "FormatHebrewMonthName":
							Annotations.SetAction (method, MethodAction.ConvertToThrow);
							break;
						}
					}
					break;
				case "DateTimeParse":
					foreach (var method in type.Methods) {
						switch (method.Name) {
						case "GetJapaneseCalendarDefaultInstance":
						case "GetTaiwanCalendarDefaultInstance":
						case "ProcessHebrewTerminalState":
						case "GetHebrewDayOfNM":
						case "MatchHebrewDigits":
							Annotations.SetAction (method, MethodAction.ConvertToThrow);
							break;
						}
					}
					break;
				}
				break;
			}
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

				if (MethodBodyScanner.IsWorthConvertingToThrow (method.Body))
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

				if (!skip && MethodBodyScanner.IsWorthConvertingToThrow (method.Body))
					annotations.SetAction (method, MethodAction.ConvertToThrow);
			}
		}

		void ExcludeMonoCollation (TypeDefinition type)
		{
			var annotations = Context.Annotations;


			switch (type.Name)
			{
				case "SimpleCollator":
					if (type.Namespace == "Mono.Globalization.Unicode")
					{
						foreach (var method in type.Methods)
						{
							if (MethodBodyScanner.IsWorthConvertingToThrow (method.Body))
								annotations.SetAction(method, MethodAction.ConvertToThrow);
						}
					}

					break;
				case "CompareInfo":
					if (type.Namespace == "System.Globalization")
					{
						foreach (var method in type.Methods)
						{
							if (method.Name == "get_UseManagedCollation")
							{
								annotations.SetAction(method, MethodAction.ConvertToFalse);
								break;
							}
						}
					}

					break;
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
