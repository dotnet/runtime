//
// MarkStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Mono.Linker.Dataflow;

namespace Mono.Linker.Steps {

	public partial class MarkStep : IStep {

		protected LinkContext _context;
		protected Queue<(MethodDefinition, DependencyInfo)> _methods;
		protected List<MethodDefinition> _virtual_methods;
		protected Queue<AttributeProviderPair> _assemblyLevelAttributes;
		protected Queue<(AttributeProviderPair, DependencyInfo)> _lateMarkedAttributes;
		protected List<TypeDefinition> _typesWithInterfaces;
		protected List<MethodBody> _unreachableBodies;

#if DEBUG
		static DependencyKind [] _entireTypeReasons = new DependencyKind [] {
			DependencyKind.NestedType,
			DependencyKind.PreservedDependency,
			DependencyKind.TypeInAssembly,
		};

		static DependencyKind [] _fieldReasons = new DependencyKind [] {
			DependencyKind.AccessedViaReflection,
			DependencyKind.AlreadyMarked,
			DependencyKind.Custom,
			DependencyKind.CustomAttributeField,
			DependencyKind.EventSourceProviderField,
			DependencyKind.FieldAccess,
			DependencyKind.FieldOnGenericInstance,
			DependencyKind.InteropMethodDependency,
			DependencyKind.Ldtoken,
			DependencyKind.MemberOfType,
			DependencyKind.PreservedDependency,
			DependencyKind.ReferencedBySpecialAttribute,
			DependencyKind.TypePreserve,
		};

		static DependencyKind [] _typeReasons = new DependencyKind [] {
			DependencyKind.AccessedViaReflection,
			DependencyKind.AlreadyMarked,
			DependencyKind.AttributeType,
			DependencyKind.BaseType,
			DependencyKind.CatchType,
			DependencyKind.Custom,
			DependencyKind.CustomAttributeArgumentType,
			DependencyKind.CustomAttributeArgumentValue,
			DependencyKind.DeclaringType,
			DependencyKind.DeclaringTypeOfCalledMethod,
			DependencyKind.ElementType,
			DependencyKind.FieldType,
			DependencyKind.GenericArgumentType,
			DependencyKind.GenericParameterConstraintType,
			DependencyKind.InterfaceImplementationInterfaceType,
			DependencyKind.Ldtoken,
			DependencyKind.ModifierType,
			DependencyKind.InstructionTypeRef,
			DependencyKind.ParameterType,
			DependencyKind.ReferencedBySpecialAttribute,
			DependencyKind.ReturnType,
			DependencyKind.UnreachableBodyRequirement,
			DependencyKind.VariableType,
		};

		static DependencyKind [] _methodReasons = new DependencyKind [] {
			DependencyKind.AccessedViaReflection,
			DependencyKind.AlreadyMarked,
			DependencyKind.AttributeConstructor,
			DependencyKind.AttributeProperty,
			DependencyKind.BaseDefaultCtorForStubbedMethod,
			DependencyKind.BaseMethod,
			DependencyKind.CctorForType,
			DependencyKind.CctorForField,
			DependencyKind.Custom,
			DependencyKind.DefaultCtorForNewConstrainedGenericArgument,
			DependencyKind.DirectCall,
			DependencyKind.ElementMethod,
			DependencyKind.EventMethod,
			DependencyKind.EventOfEventMethod,
			DependencyKind.InteropMethodDependency,
			DependencyKind.KeptForSpecialAttribute,
			DependencyKind.Ldftn,
			DependencyKind.Ldtoken,
			DependencyKind.Ldvirtftn,
			DependencyKind.MemberOfType,
			DependencyKind.MethodForInstantiatedType,
			DependencyKind.MethodForSpecialType,
			DependencyKind.MethodImplOverride,
			DependencyKind.MethodOnGenericInstance,
			DependencyKind.Newobj,
			DependencyKind.Override,
			DependencyKind.OverrideOnInstantiatedType,
			DependencyKind.PreservedDependency,
			DependencyKind.ReferencedBySpecialAttribute,
			DependencyKind.SerializationMethodForType,
			DependencyKind.TriggersCctorForCalledMethod,
			DependencyKind.TriggersCctorThroughFieldAccess,
			DependencyKind.TypePreserve,
			DependencyKind.UnreachableBodyRequirement,
			DependencyKind.VirtualCall,
			DependencyKind.VirtualNeededDueToPreservedScope,
		};
#endif

		readonly FlowAnnotations _flowAnnotations;

		public MarkStep ()
		{
			_methods = new Queue<(MethodDefinition, DependencyInfo)> ();
			_virtual_methods = new List<MethodDefinition> ();
			_assemblyLevelAttributes = new Queue<AttributeProviderPair> ();
			_lateMarkedAttributes = new Queue<(AttributeProviderPair, DependencyInfo)> ();
			_typesWithInterfaces = new List<TypeDefinition> ();
			_unreachableBodies = new List<MethodBody> ();

			_flowAnnotations = new FlowAnnotations (new AttributeFlowAnnotationSource (), _context);
		}

		public AnnotationStore Annotations => _context.Annotations;
		public Tracer Tracer => _context.Tracer;

		public virtual void Process (LinkContext context)
		{
			_context = context;

			Initialize ();
			Process ();
			Complete ();
		}

		void Initialize ()
		{
			foreach (AssemblyDefinition assembly in _context.GetAssemblies ())
				InitializeAssembly (assembly);
		}

		protected virtual void InitializeAssembly (AssemblyDefinition assembly)
		{
			var action = _context.Annotations.GetAction (assembly);
			switch (action) {
			case AssemblyAction.Copy:
			case AssemblyAction.Save:
				Tracer.AddDirectDependency (assembly, new DependencyInfo (DependencyKind.AssemblyAction, action), marked: false);
				MarkEntireAssembly (assembly);
				break;
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
				MarkAssembly (assembly);

				foreach (TypeDefinition type in assembly.MainModule.Types)
					InitializeType (type);

				break;
			}
		}

		void Complete ()
		{
			foreach (var body in _unreachableBodies) {
				Annotations.SetAction (body.Method, MethodAction.ConvertToThrow);
			}
		}

		void InitializeType (TypeDefinition type)
		{
			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					InitializeType (nested);
			}

			if (!Annotations.IsMarked (type))
				return;

			// We may get here for a type marked by an earlier step, or by a type
			// marked indirectly as the result of some other InitializeType call.
			// Just track this as already marked, and don't include a new source.
			MarkType (type, DependencyInfo.AlreadyMarked);

			if (type.HasFields)
				InitializeFields (type);
			if (type.HasMethods)
				InitializeMethods (type.Methods);
		}

		protected bool IsFullyPreserved (TypeDefinition type)
		{
			if (Annotations.TryGetPreserve (type, out TypePreserve preserve) && preserve == TypePreserve.All)
				return true;

			switch (Annotations.GetAction (type.Module.Assembly)) {
			case AssemblyAction.Save:
			case AssemblyAction.Copy:
			case AssemblyAction.CopyUsed:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
				return true;
			}

			return false;
		}

		void InitializeFields (TypeDefinition type)
		{
			foreach (FieldDefinition field in type.Fields)
				if (Annotations.IsMarked (field))
					MarkField (field, DependencyInfo.AlreadyMarked);
		}

		void InitializeMethods (Collection<MethodDefinition> methods)
		{
			foreach (MethodDefinition method in methods)
				if (Annotations.IsMarked (method))
					EnqueueMethod (method, DependencyInfo.AlreadyMarked);
		}

		void MarkEntireType (TypeDefinition type, in DependencyInfo reason)
		{
#if DEBUG
			if (!_entireTypeReasons.Contains (reason.Kind))
				throw new ArgumentOutOfRangeException ($"Internal error: unsupported type dependency {reason.Kind}");
#endif

			if (type.HasNestedTypes) {
				foreach (TypeDefinition nested in type.NestedTypes)
					MarkEntireType (nested, new DependencyInfo (DependencyKind.NestedType, type));
			}

			Annotations.Mark (type, reason);
			MarkCustomAttributes (type, new DependencyInfo (DependencyKind.CustomAttribute, type));
			MarkTypeSpecialCustomAttributes (type);

			if (type.HasInterfaces) {
				foreach (InterfaceImplementation iface in type.Interfaces) {
					MarkInterfaceImplementation (iface, type);
				}
			}

			MarkGenericParameterProvider (type);

			if (type.HasFields) {
				foreach (FieldDefinition field in type.Fields) {
					MarkField (field, new DependencyInfo (DependencyKind.MemberOfType, type));
				}
			}

			if (type.HasMethods) {
				foreach (MethodDefinition method in type.Methods) {
					// Probably redundant since we EnqueueMethod below anyway.
					Annotations.Mark (method, new DependencyInfo (DependencyKind.MemberOfType, type));
					Annotations.SetAction (method, MethodAction.ForceParse);
					EnqueueMethod (method, new DependencyInfo (DependencyKind.MemberOfType, type));
				}
			}

			if (type.HasProperties) {
				foreach (var property in type.Properties) {
					MarkProperty (property, new DependencyInfo (DependencyKind.MemberOfType, type));
				}
			}

			if (type.HasEvents) {
				foreach (var ev in type.Events) {
					MarkEvent (ev, new DependencyInfo (DependencyKind.MemberOfType, type));
				}
			}
		}

		void Process ()
		{
			while (ProcessPrimaryQueue () || ProcessLazyAttributes () || ProcessLateMarkedAttributes ())

			// deal with [TypeForwardedTo] pseudo-attributes
			foreach (AssemblyDefinition assembly in _context.GetAssemblies ()) {
				if (!assembly.MainModule.HasExportedTypes)
					continue;

				foreach (var exported in assembly.MainModule.ExportedTypes) {
					bool isForwarder = exported.IsForwarder;
					var declaringType = exported.DeclaringType;
					while (!isForwarder && (declaringType != null)) {
						isForwarder = declaringType.IsForwarder;
						declaringType = declaringType.DeclaringType;
					}

					if (!isForwarder)
						continue;
					TypeDefinition type = exported.Resolve ();
					if (type == null)
						continue;
					if (!Annotations.IsMarked (type))
						continue;
					_context.MarkingHelpers.MarkExportedType (exported, assembly.MainModule, new DependencyInfo (DependencyKind.ExportedType, type));
				}
			}
		}

		bool ProcessPrimaryQueue ()
		{
			if (QueueIsEmpty ())
				return false;

			while (!QueueIsEmpty ()) {
				ProcessQueue ();
				ProcessVirtualMethods ();
				ProcessMarkedTypesWithInterfaces ();
				ProcessPendingBodies ();
				DoAdditionalProcessing ();
			}

			return true;
		}

		void ProcessQueue ()
		{
			while (!QueueIsEmpty ()) {
				(MethodDefinition method, DependencyInfo reason) = _methods.Dequeue ();
				try {
					ProcessMethod (method, reason);
				} catch (Exception e) {
					throw new MarkException (string.Format ("Error processing method: '{0}' in assembly: '{1}'", method.FullName, method.Module.Name), e, method);
				}
			}
		}

		bool QueueIsEmpty ()
		{
			return _methods.Count == 0;
		}

		protected virtual void EnqueueMethod (MethodDefinition method, in DependencyInfo reason)
		{
			_methods.Enqueue ((method, reason));
		}

		void ProcessVirtualMethods ()
		{
			foreach (MethodDefinition method in _virtual_methods) {
				ProcessVirtualMethod (method);
			}
		}

		void ProcessMarkedTypesWithInterfaces ()
		{
			// We may mark an interface type later on.  Which means we need to reprocess any time with one or more interface implementations that have not been marked
			// and if an interface type is found to be marked and implementation is not marked, then we need to mark that implementation

			// copy the data to avoid modified while enumerating error potential, which can happen under certain conditions.
			var typesWithInterfaces = _typesWithInterfaces.ToArray ();

			foreach (var type in typesWithInterfaces) {
				// Exception, types that have not been flagged as instantiated yet.  These types may not need their interfaces even if the
				// interface type is marked
				if (!Annotations.IsInstantiated (type))
					continue;

				MarkInterfaceImplementations (type);
			}
		}

		void ProcessPendingBodies ()
		{
			for (int i = 0; i < _unreachableBodies.Count; i++) {
				var body = _unreachableBodies [i];
				if (Annotations.IsInstantiated (body.Method.DeclaringType)) {
					MarkMethodBody (body);
					_unreachableBodies.RemoveAt (i--);
				}
			}
		}

		void ProcessVirtualMethod (MethodDefinition method)
		{
			var overrides = Annotations.GetOverrides (method);
			if (overrides == null)
				return;

			foreach (OverrideInformation @override in overrides)
				ProcessOverride (@override);
		}

		void ProcessOverride (OverrideInformation overrideInformation)
		{
			var method = overrideInformation.Override;
			var @base = overrideInformation.Base;
			if (!Annotations.IsMarked (method.DeclaringType))
				return;

			if (Annotations.IsProcessed (method))
				return;

			if (Annotations.IsMarked (method))
				return;

			var isInstantiated = Annotations.IsInstantiated (method.DeclaringType);

			// We don't need to mark overrides until it is possible that the type could be instantiated
			// Note : The base type is interface check should be removed once we have base type sweeping
			if (IsInterfaceOverrideThatDoesNotNeedMarked (overrideInformation, isInstantiated))
				return;

			if (!isInstantiated && !@base.IsAbstract && _context.IsOptimizationEnabled (CodeOptimizations.OverrideRemoval, method))
				return;

			// Only track instantiations if override removal is enabled and the type is instantiated.
			// If it's disabled, all overrides are kept, so there's no instantiation site to blame.
			if (_context.IsOptimizationEnabled (CodeOptimizations.OverrideRemoval, method) && isInstantiated) {
				MarkMethod (method, new DependencyInfo (DependencyKind.OverrideOnInstantiatedType, method.DeclaringType));
			} else {
				// If the optimization is disabled or it's an abstract type, we just mark it as a normal override.
				Debug.Assert (!_context.IsOptimizationEnabled (CodeOptimizations.OverrideRemoval, method) || @base.IsAbstract);
				MarkMethod (method, new DependencyInfo (DependencyKind.Override, @base));
			}

			ProcessVirtualMethod (method);
		}

		bool IsInterfaceOverrideThatDoesNotNeedMarked (OverrideInformation overrideInformation, bool isInstantiated)
		{
			if (!overrideInformation.IsOverrideOfInterfaceMember || isInstantiated)
				return false;

			if (overrideInformation.MatchingInterfaceImplementation != null)
				return !Annotations.IsMarked (overrideInformation.MatchingInterfaceImplementation);

			var interfaceType = overrideInformation.InterfaceType;
			var overrideDeclaringType = overrideInformation.Override.DeclaringType;

			if (!IsInterfaceImplementationMarked (overrideDeclaringType, interfaceType)) {
				var derivedInterfaceTypes = Annotations.GetDerivedInterfacesForInterface (interfaceType);

				// There are no derived interface types that could be marked, it's safe to skip marking this override
				if (derivedInterfaceTypes == null)
					return true;

				// If none of the other interfaces on the type that implement the interface from the @base type are marked, then it's safe to skip
				// marking this override
				if (!derivedInterfaceTypes.Any (d => IsInterfaceImplementationMarked (overrideDeclaringType, d)))
					return true;
			}

			return false;
		}

		bool IsInterfaceImplementationMarked (TypeDefinition type, TypeDefinition interfaceType)
		{
			return type.HasInterface (@interfaceType, out InterfaceImplementation implementation) && Annotations.IsMarked (implementation);
		}

		void MarkMarshalSpec (IMarshalInfoProvider spec, in DependencyInfo reason)
		{
			if (!spec.HasMarshalInfo)
				return;

			if (spec.MarshalInfo is CustomMarshalInfo marshaler)
				MarkType (marshaler.ManagedType, reason);
		}

		void MarkCustomAttributes (ICustomAttributeProvider provider, in DependencyInfo reason)
		{
			if (!provider.HasCustomAttributes)
				return;

			bool markOnUse = _context.KeepUsedAttributeTypesOnly && Annotations.GetAction (GetAssemblyFromCustomAttributeProvider (provider)) == AssemblyAction.Link;

			foreach (CustomAttribute ca in provider.CustomAttributes) {
				if (ProcessLinkerSpecialAttribute (ca, provider, reason)) {
					continue;
				}

				if (markOnUse) {
					_lateMarkedAttributes.Enqueue ((new AttributeProviderPair (ca, provider), reason));
					continue;
				}

				MarkCustomAttribute (ca, reason);
				MarkSpecialCustomAttributeDependencies (ca, provider);
			}
		}

		protected virtual bool ProcessLinkerSpecialAttribute (CustomAttribute ca, ICustomAttributeProvider provider, in DependencyInfo reason)
		{
			if (IsUserDependencyMarker (ca.AttributeType) && provider is MemberReference mr) {
				MarkUserDependency (mr, ca);

				if (_context.KeepDependencyAttributes || Annotations.GetAction (mr.Module.Assembly) != AssemblyAction.Link) {
					MarkCustomAttribute (ca, reason);
				} else {
					// Record the custom attribute so that it has a reason, without actually marking it.
					Tracer.AddDirectDependency (ca, reason, marked: false);
				}

				return true;
			}

			return false;
		}

		protected static AssemblyDefinition GetAssemblyFromCustomAttributeProvider (ICustomAttributeProvider provider)
		{
			return provider switch {
				MemberReference mr => mr.Module.Assembly,
				AssemblyDefinition ad => ad,
				ModuleDefinition md => md.Assembly,
				InterfaceImplementation ii => ii.InterfaceType.Module.Assembly,
				GenericParameterConstraint gpc => gpc.ConstraintType.Module.Assembly,
				ParameterDefinition pd => pd.ParameterType.Module.Assembly,
				MethodReturnType mrt => mrt.ReturnType.Module.Assembly,
				_ => throw new NotImplementedException (provider.GetType ().ToString ()),
			};
		}

		protected virtual bool IsUserDependencyMarker (TypeReference type)
		{
			return PreserveDependencyLookupStep.IsPreserveDependencyAttribute (type);
		}

		protected virtual void MarkUserDependency (MemberReference context, CustomAttribute ca)
		{
			if (ca.HasProperties && ca.Properties [0].Name == "Condition") {
				var condition = ca.Properties [0].Argument.Value as string;
				switch (condition) {
				case "":
				case null:
					break;
				case "DEBUG":
					if (!_context.KeepMembersForDebugger)
						return;

					break;
				default:
					// Don't have yet a way to match the general condition so everything is excluded
					return;
				}
			}

			AssemblyDefinition assembly;
			var args = ca.ConstructorArguments;
			if (args.Count >= 3 && args [2].Value is string assemblyName) {
				if (!_context.Resolver.AssemblyCache.TryGetValue (assemblyName, out assembly)) {
					_context.LogMessage (MessageImportance.Low, $"Could not resolve '{assemblyName}' assembly dependency");
					return;
				}
			} else {
				assembly = null;
			}

			TypeDefinition td;
			if (args.Count >= 2 && args [1].Value is string typeName) {
				td = FindType (assembly ?? context.Module.Assembly, typeName);

				if (td == null) {
					_context.LogMessage (MessageImportance.Low, $"Could not resolve '{typeName}' type dependency");
					return;
				}
			} else {
				td = context.DeclaringType.Resolve ();
			}

			string member = null;
			string[] signature = null;
			if (args.Count >= 1 && args [0].Value is string memberSignature) {
				memberSignature = memberSignature.Replace (" ", "");
				var sign_start = memberSignature.IndexOf ('(');
				var sign_end = memberSignature.LastIndexOf (')');
				if (sign_start > 0 && sign_end > sign_start) {
					var parameters = memberSignature.Substring (sign_start + 1, sign_end - sign_start - 1);
					signature = string.IsNullOrEmpty (parameters) ? Array.Empty<string> () : parameters.Split (',');
					member = memberSignature.Substring (0, sign_start);
				} else {
					member = memberSignature;
				}
			}

			if (member == "*") {
				MarkEntireType (td, new DependencyInfo (DependencyKind.PreservedDependency, ca));
				return;
			}

			if (MarkDependencyMethod (td, member, signature, new DependencyInfo (DependencyKind.PreservedDependency, ca)))
				return;

			if (MarkDependencyField (td, member, new DependencyInfo (DependencyKind.PreservedDependency, ca)))
				return;

			_context.LogMessage (MessageImportance.High, $"Could not resolve dependency member '{member}' declared in type '{td.FullName}'");
		}

		static TypeDefinition FindType (AssemblyDefinition assembly, string fullName)
		{
			fullName = fullName.ToCecilName ();

			var type = assembly.MainModule.GetType (fullName);
			return type?.Resolve ();
		}

		bool MarkDependencyMethod (TypeDefinition type, string name, string[] signature, in DependencyInfo reason)
		{
			bool marked = false;

			int arity_marker = name.IndexOf ('`');
			if (arity_marker < 1 || !int.TryParse (name.Substring (arity_marker + 1), out int arity)) {
				arity = 0;
			} else {
				name = name.Substring (0, arity_marker);
			}
			
			foreach (var m in type.Methods) {
				if (m.Name != name)
					continue;

				if (m.GenericParameters.Count != arity)
					continue;

				if (signature == null) {
					MarkIndirectlyCalledMethod (m, reason);
					marked = true;
					continue;
				}

				var mp = m.Parameters;
				if (mp.Count != signature.Length)
					continue;

				int i = 0;
				for (; i < signature.Length; ++i) {
					if (mp [i].ParameterType.FullName != signature [i].Trim ().ToCecilName ()) {
						i = -1;
						break;
					}
				}

				if (i < 0)
					continue;

				MarkIndirectlyCalledMethod (m, reason);
				marked = true;
			}

			return marked;
		}

		bool MarkDependencyField (TypeDefinition type, string name, in DependencyInfo reason)
		{
			foreach (var f in type.Fields) {
				if (f.Name == name) {
					MarkField (f, reason);
					return true;
				}
			}

			return false;
		}

		void LazyMarkCustomAttributes (ICustomAttributeProvider provider, ModuleDefinition module)
		{
			if (!provider.HasCustomAttributes)
				return;

			foreach (CustomAttribute ca in provider.CustomAttributes)
				_assemblyLevelAttributes.Enqueue (new AttributeProviderPair (ca, module));
		}

		protected virtual void MarkCustomAttribute (CustomAttribute ca, in DependencyInfo reason)
		{
			Annotations.Mark (ca, reason);
			MarkMethod (ca.Constructor, new DependencyInfo (DependencyKind.AttributeConstructor, ca));

			MarkCustomAttributeArguments (ca);

			TypeReference constructor_type = ca.Constructor.DeclaringType;
			TypeDefinition type = constructor_type.Resolve ();

			if (type == null) {
				HandleUnresolvedType (constructor_type);
				return;
			}

			MarkCustomAttributeProperties (ca, type);
			MarkCustomAttributeFields (ca, type);
		}

		protected virtual bool ShouldMarkCustomAttribute (CustomAttribute ca, ICustomAttributeProvider provider)
		{
			var attr_type = ca.AttributeType;

			if (_context.KeepUsedAttributeTypesOnly) {
				switch (attr_type.FullName) {
				// These are required by the runtime
				case "System.ThreadStaticAttribute":
				case "System.ContextStaticAttribute":
				case "System.Runtime.CompilerServices.IsByRefLikeAttribute":
					return true;
				// Attributes related to `fixed` keyword used to declare fixed length arrays
				case "System.Runtime.CompilerServices.FixedBufferAttribute":
					return true;
				case "System.Runtime.InteropServices.InterfaceTypeAttribute":
				case "System.Runtime.InteropServices.GuidAttribute":
				case "System.Runtime.CompilerServices.InternalsVisibleToAttribute":
					return true;
				}
				
				if (!Annotations.IsMarked (attr_type.Resolve ()))
					return false;
			}

			return true;
		}

		protected virtual bool ShouldMarkTypeStaticConstructor (TypeDefinition type)
		{
			if (Annotations.HasPreservedStaticCtor (type))
				return false;
			
			if (type.IsBeforeFieldInit && _context.IsOptimizationEnabled (CodeOptimizations.BeforeFieldInit, type))
				return false;

			return true;
		}

		protected void MarkStaticConstructor (TypeDefinition type, in DependencyInfo reason)
		{
			if (MarkMethodIf (type.Methods, IsNonEmptyStaticConstructor, reason) != null)
				Annotations.SetPreservedStaticCtor (type);
		}

		protected virtual bool ShouldMarkTopLevelCustomAttribute (AttributeProviderPair app, MethodDefinition resolvedConstructor)
		{
			var ca = app.Attribute;

			if (!ShouldMarkCustomAttribute (app.Attribute, app.Provider))
				return false;

			// If an attribute's module has not been marked after processing all types in all assemblies and the attribute itself has not been marked,
			// then surely nothing is using this attribute and there is no need to mark it
			if (!Annotations.IsMarked (resolvedConstructor.Module) &&
				!Annotations.IsMarked (ca.AttributeType) &&
				Annotations.GetAction (resolvedConstructor.Module.Assembly) == AssemblyAction.Link)
				return false;

			if (ca.Constructor.DeclaringType.Namespace == "System.Diagnostics") {
				string attributeName = ca.Constructor.DeclaringType.Name;
				if (attributeName == "DebuggerDisplayAttribute" || attributeName == "DebuggerTypeProxyAttribute") {
					var displayTargetType = GetDebuggerAttributeTargetType (app.Attribute, (AssemblyDefinition) app.Provider);
					if (displayTargetType == null || !Annotations.IsMarked (displayTargetType))
						return false;
				}
			}
			
			return true;
		}

		protected void MarkSecurityDeclarations (ISecurityDeclarationProvider provider, in DependencyInfo reason)
		{
			// most security declarations are removed (if linked) but user code might still have some
			// and if the attributes references types then they need to be marked too
			if ((provider == null) || !provider.HasSecurityDeclarations)
				return;

			foreach (var sd in provider.SecurityDeclarations)
				MarkSecurityDeclaration (sd, reason);
		}

		protected virtual void MarkSecurityDeclaration (SecurityDeclaration sd, in DependencyInfo reason)
		{
			if (!sd.HasSecurityAttributes)
				return;
			
			foreach (var sa in sd.SecurityAttributes)
				MarkSecurityAttribute (sa, reason);
		}

		protected virtual void MarkSecurityAttribute (SecurityAttribute sa, in DependencyInfo reason)
		{
			TypeReference security_type = sa.AttributeType;
			TypeDefinition type = security_type.Resolve ();
			if (type == null) {
				HandleUnresolvedType (security_type);
				return;
			}

			// Security attributes participate in inference logic without being marked.
			Tracer.AddDirectDependency (sa, reason, marked: false);
			MarkType (security_type, new DependencyInfo (DependencyKind.AttributeType, sa));
			MarkCustomAttributeProperties (sa, type);
			MarkCustomAttributeFields (sa, type);
		}

		protected void MarkCustomAttributeProperties (ICustomAttribute ca, TypeDefinition attribute)
		{
			if (!ca.HasProperties)
				return;

			foreach (var named_argument in ca.Properties)
				MarkCustomAttributeProperty (named_argument, attribute, ca, new DependencyInfo (DependencyKind.AttributeProperty, ca));
		}

		protected void MarkCustomAttributeProperty (CustomAttributeNamedArgument namedArgument, TypeDefinition attribute, ICustomAttribute ca, in DependencyInfo reason)
		{
			PropertyDefinition property = GetProperty (attribute, namedArgument.Name);
			if (property != null)
				MarkMethod (property.SetMethod, reason);

			MarkCustomAttributeArgument (namedArgument.Argument, ca);
		}

		PropertyDefinition GetProperty (TypeDefinition type, string propertyname)
		{
			while (type != null) {
				PropertyDefinition property = type.Properties.FirstOrDefault (p => p.Name == propertyname);
				if (property != null)
					return property;

				// This would neglect to mark parameters for generic instances.
				Debug.Assert (!(type.BaseType is GenericInstanceType));
				type = type.BaseType?.Resolve ();
			}

			return null;
		}

		protected void MarkCustomAttributeFields (ICustomAttribute ca, TypeDefinition attribute)
		{
			if (!ca.HasFields)
				return;

			foreach (var named_argument in ca.Fields)
				MarkCustomAttributeField (named_argument, attribute, ca);
		}

		protected void MarkCustomAttributeField (CustomAttributeNamedArgument namedArgument, TypeDefinition attribute, ICustomAttribute ca)
		{
			FieldDefinition field = GetField (attribute, namedArgument.Name);
			if (field != null)
				MarkField (field, new DependencyInfo (DependencyKind.CustomAttributeField, ca));

			MarkCustomAttributeArgument (namedArgument.Argument, ca);
		}

		FieldDefinition GetField (TypeDefinition type, string fieldname)
		{
			while (type != null) {
				FieldDefinition field = type.Fields.FirstOrDefault (f => f.Name == fieldname);
				if (field != null)
					return field;

				// This would neglect to mark parameters for generic instances.
				Debug.Assert (!(type.BaseType is GenericInstanceType));
				type = type.BaseType?.Resolve ();
			}

			return null;
		}

		MethodDefinition GetMethodWithNoParameters (TypeDefinition type, string methodname)
		{
			while (type != null) {
				MethodDefinition method = type.Methods.FirstOrDefault (m => m.Name == methodname && !m.HasParameters);
				if (method != null)
					return method;

				// This would neglect to mark parameters for generic instances.
				Debug.Assert (!(type.BaseType is GenericInstanceType));
				type = type.BaseType.Resolve ();
			}

			return null;
		}

		void MarkCustomAttributeArguments (CustomAttribute ca)
		{
			if (!ca.HasConstructorArguments)
				return;

			foreach (var argument in ca.ConstructorArguments)
				MarkCustomAttributeArgument (argument, ca);
		}

		void MarkCustomAttributeArgument (CustomAttributeArgument argument, ICustomAttribute ca)
		{
			var at = argument.Type;

			if (at.IsArray) {
				var et = at.GetElementType ();

				MarkType (et, new DependencyInfo (DependencyKind.CustomAttributeArgumentType, ca));
				if (argument.Value == null)
					return;

				// Array arguments are modeled as a CustomAttributeArgument [], and will mark the
				// Type once for each element in the array.
				foreach (var caa in (CustomAttributeArgument [])argument.Value)
					MarkCustomAttributeArgument (caa, ca);

				return;
			}

			if (at.Namespace == "System") {
				switch (at.Name) {
				case "Type":
					MarkType (argument.Type, new DependencyInfo (DependencyKind.CustomAttributeArgumentType, ca));
					MarkType ((TypeReference)argument.Value, new DependencyInfo (DependencyKind.CustomAttributeArgumentValue, ca));
					return;

				case "Object":
					var boxed_value = (CustomAttributeArgument)argument.Value;
					MarkType (boxed_value.Type, new DependencyInfo (DependencyKind.CustomAttributeArgumentType, ca));
					MarkCustomAttributeArgument (boxed_value, ca);
					return;
				}
			}
		}

		protected bool CheckProcessed (IMetadataTokenProvider provider)
		{
			if (Annotations.IsProcessed (provider))
				return true;

			Annotations.Processed (provider);
			return false;
		}

		protected void MarkAssembly (AssemblyDefinition assembly)
		{
			if (CheckProcessed (assembly))
				return;

			ProcessModule (assembly);

			MarkAssemblyCustomAttributes (assembly);

			MarkSecurityDeclarations (assembly, new DependencyInfo (DependencyKind.AssemblyOrModuleAttribute, assembly));

			foreach (ModuleDefinition module in assembly.Modules)
				LazyMarkCustomAttributes (module, module);
		}

		void MarkEntireAssembly (AssemblyDefinition assembly)
		{
			MarkCustomAttributes (assembly, new DependencyInfo (DependencyKind.AssemblyOrModuleAttribute, assembly));
			MarkCustomAttributes (assembly.MainModule, new DependencyInfo (DependencyKind.AssemblyOrModuleAttribute, assembly.MainModule));

			if (assembly.MainModule.HasExportedTypes) {
				// TODO: This needs more work accross all steps
			}

			foreach (TypeDefinition type in assembly.MainModule.Types)
				MarkEntireType (type, new DependencyInfo (DependencyKind.TypeInAssembly, assembly));
		}

		void ProcessModule (AssemblyDefinition assembly)
		{
			// Pre-mark <Module> if there is any methods as they need to be executed 
			// at assembly load time
			foreach (TypeDefinition type in assembly.MainModule.Types)
			{
				if (type.Name == "<Module>" && type.HasMethods)
				{
					MarkType (type, new DependencyInfo (DependencyKind.TypeInAssembly, assembly));
					break;
				}
			}
		}

		bool ProcessLazyAttributes ()
		{
			if (Annotations.HasMarkedAnyIndirectlyCalledMethods () && MarkDisablePrivateReflectionAttribute ())
				return true;

			var startingQueueCount = _assemblyLevelAttributes.Count;
			if (startingQueueCount == 0)
				return false;

			var skippedItems = new List<AttributeProviderPair> ();
			var markOccurred = false;

			while (_assemblyLevelAttributes.Count != 0) {
				var assemblyLevelAttribute = _assemblyLevelAttributes.Dequeue ();
				var customAttribute = assemblyLevelAttribute.Attribute;

				var resolved = customAttribute.Constructor.Resolve ();
				if (resolved == null) {
					HandleUnresolvedMethod (customAttribute.Constructor);
					continue;
				}

				if (!ShouldMarkTopLevelCustomAttribute (assemblyLevelAttribute, resolved)) {
					skippedItems.Add (assemblyLevelAttribute);
					continue;
				}


				markOccurred = true;
				MarkCustomAttribute (customAttribute, new DependencyInfo (DependencyKind.AssemblyOrModuleAttribute, assemblyLevelAttribute.Provider));

				string attributeFullName = customAttribute.Constructor.DeclaringType.FullName;
				switch (attributeFullName) {
				case "System.Diagnostics.DebuggerDisplayAttribute":
					MarkTypeWithDebuggerDisplayAttribute (GetDebuggerAttributeTargetType (assemblyLevelAttribute.Attribute, (AssemblyDefinition) assemblyLevelAttribute.Provider), customAttribute);
					break;
				case "System.Diagnostics.DebuggerTypeProxyAttribute":
					MarkTypeWithDebuggerTypeProxyAttribute (GetDebuggerAttributeTargetType (assemblyLevelAttribute.Attribute, (AssemblyDefinition) assemblyLevelAttribute.Provider), customAttribute);
					break;
				}
			}

			// requeue the items we skipped in case we need to make another pass
			foreach (var item in skippedItems)
				_assemblyLevelAttributes.Enqueue (item);

			return markOccurred;
		}

		bool ProcessLateMarkedAttributes ()
		{
			var startingQueueCount = _lateMarkedAttributes.Count;
			if (startingQueueCount == 0)
				return false;

			var skippedItems = new List<(AttributeProviderPair, DependencyInfo)> ();
			var markOccurred = false;

			while (_lateMarkedAttributes.Count != 0) {
				var (attributeProviderPair, reason) = _lateMarkedAttributes.Dequeue ();
				var customAttribute = attributeProviderPair.Attribute;
				var provider = attributeProviderPair.Provider;

				var resolved = customAttribute.Constructor.Resolve ();
				if (resolved == null) {
					HandleUnresolvedMethod (customAttribute.Constructor);
					continue;
				}

				if (!ShouldMarkCustomAttribute (customAttribute, provider)) {
					skippedItems.Add ((attributeProviderPair, reason));
					continue;
				}

				markOccurred = true;
				MarkCustomAttribute (customAttribute, reason);
				MarkSpecialCustomAttributeDependencies (customAttribute, provider);
			}

			// requeue the items we skipped in case we need to make another pass
			foreach (var item in skippedItems)
				_lateMarkedAttributes.Enqueue (item);

			return markOccurred;
		}

		protected void MarkField (FieldReference reference, DependencyInfo reason)
		{
			if (reference.DeclaringType is GenericInstanceType) {
				Debug.Assert (reason.Kind == DependencyKind.FieldAccess || reason.Kind == DependencyKind.Ldtoken);
				// Blame the field reference (without actually marking) on the original reason.
				Tracer.AddDirectDependency (reference, reason, marked: false);
				MarkType (reference.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, reference));

				// Blame the field definition that we will resolve on the field reference.
				reason = new DependencyInfo (DependencyKind.FieldOnGenericInstance, reference);
			}

			FieldDefinition field = reference.Resolve ();

			if (field == null) {
				HandleUnresolvedField (reference);
				return;
			}

			MarkField (field, reason);
		}

		void MarkField (FieldDefinition field, in DependencyInfo reason)
		{
#if DEBUG
			if (!_fieldReasons.Contains (reason.Kind))
				throw new ArgumentOutOfRangeException ($"Internal error: unsupported field dependency {reason.Kind}");
#endif

			if (CheckProcessed (field))
				return;

			MarkType (field.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, field));
			MarkType (field.FieldType, new DependencyInfo (DependencyKind.FieldType, field));
			MarkCustomAttributes (field, new DependencyInfo (DependencyKind.CustomAttribute, field));
			MarkMarshalSpec (field, new DependencyInfo (DependencyKind.FieldMarshalSpec, field));
			DoAdditionalFieldProcessing (field);

			var parent = field.DeclaringType;
			if (!Annotations.HasPreservedStaticCtor (parent)) {
				var cctorReason = reason.Kind switch {
					// Report an edge directly from the method accessing the field to the static ctor it triggers
					DependencyKind.FieldAccess => new DependencyInfo (DependencyKind.TriggersCctorThroughFieldAccess, reason.Source),
					_ => new DependencyInfo (DependencyKind.CctorForField, field)
				};
				MarkStaticConstructor (parent, cctorReason);
			}

			if (Annotations.HasSubstitutedInit (field)) {
				Annotations.SetPreservedStaticCtor (parent);
				Annotations.SetSubstitutedInit (parent);
			}

			if (reason.Kind == DependencyKind.AlreadyMarked) {
				Debug.Assert (Annotations.IsMarked (field));
				return;
			}

			Annotations.Mark (field, reason);
		}

		protected virtual bool IgnoreScope (IMetadataScope scope)
		{
			AssemblyDefinition assembly = ResolveAssembly (scope);
			return Annotations.GetAction (assembly) != AssemblyAction.Link;
		}

		void MarkScope (IMetadataScope scope, TypeDefinition type)
		{
			Annotations.Mark (scope, new DependencyInfo (DependencyKind.ScopeOfType, type));
		}

		protected virtual void MarkSerializable (TypeDefinition type)
		{
			// Keep default ctor for XmlSerializer support. See https://github.com/mono/linker/issues/957
			MarkDefaultConstructor (type, new DependencyInfo (DependencyKind.SerializationMethodForType, type));
			if (!_context.IsFeatureExcluded ("deserialization"))
				MarkMethodsIf (type.Methods, IsSpecialSerializationConstructor, new DependencyInfo (DependencyKind.SerializationMethodForType, type));
		}

		protected virtual TypeDefinition MarkType (TypeReference reference, DependencyInfo reason)
		{
#if DEBUG
			if (!_typeReasons.Contains (reason.Kind))
				throw new ArgumentOutOfRangeException ($"Internal error: unsupported type dependency {reason.Kind}");
#endif
			if (reference == null)
				return null;

			(reference, reason) = GetOriginalType (reference, reason);

			if (reference is FunctionPointerType)
				return null;

			if (reference is GenericParameter)
				return null;

//			if (IgnoreScope (reference.Scope))
//				return null;

			TypeDefinition type = reference.Resolve ();

			if (type == null) {
				HandleUnresolvedType (reference);
				return null;
			}

			// Track a mark reason for each call to MarkType.
			switch (reason.Kind) {
			case DependencyKind.AlreadyMarked:
				Debug.Assert (Annotations.IsMarked (type));
				break;
			default:
				Annotations.Mark (type, reason);
				break;
			}

			// Treat cctors triggered by a called method specially and mark this case up-front.
			if (type.HasMethods && ShouldMarkTypeStaticConstructor (type) && reason.Kind == DependencyKind.DeclaringTypeOfCalledMethod)
				MarkStaticConstructor (type, new DependencyInfo (DependencyKind.TriggersCctorForCalledMethod, reason.Source));

			if (CheckProcessed (type))
				return null;

			MarkScope (type.Scope, type);
			MarkType (type.BaseType, new DependencyInfo (DependencyKind.BaseType, type));
			MarkType (type.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, type));
			MarkCustomAttributes (type, new DependencyInfo (DependencyKind.CustomAttribute, type));
			MarkSecurityDeclarations (type, new DependencyInfo (DependencyKind.CustomAttribute, type));

			if (type.IsMulticastDelegate ()) {
				MarkMulticastDelegate (type);
			}

			if (type.IsSerializable ())
				MarkSerializable (type);

			// TODO: This needs work to ensure we handle EventSource appropriately.
			// This marks static fields of KeyWords/OpCodes/Tasks subclasses of an EventSource type.
			if (!_context.IsFeatureExcluded ("etw") && BCL.EventTracingForWindows.IsEventSourceImplementation (type, _context)) {
				MarkEventSourceProviders (type);
			}

			// This marks properties for [EventData] types as well as other attribute dependencies.
			MarkTypeSpecialCustomAttributes (type);

			MarkGenericParameterProvider (type);

			// keep fields for value-types and for classes with LayoutKind.Sequential or Explicit
			if (type.IsValueType || !type.IsAutoLayout)
				MarkFields (type, includeStatic: type.IsEnum, reason: new DependencyInfo (DependencyKind.MemberOfType, type));

			// There are a number of markings we can defer until later when we know it's possible a reference type could be instantiated
			// For example, if no instance of a type exist, then we don't need to mark the interfaces on that type
			// However, for some other types there is no benefit to deferring
			if (type.IsInterface) {
				// There's no benefit to deferring processing of an interface type until we know a type implementing that interface is marked
				MarkRequirementsForInstantiatedTypes (type);
			} else if (type.IsValueType) {
				// Note : Technically interfaces could be removed from value types in some of the same cases as reference types, however, it's harder to know when
				// a value type instance could exist.  You'd have to track initobj and maybe locals types.  Going to punt for now.
				MarkRequirementsForInstantiatedTypes (type);
			} else if (IsFullyPreserved (type)) {
				// Here for a couple reasons:
				// * Edge case to cover a scenario where a type has preserve all, implements interfaces, but does not have any instance ctors.
				//    Normally TypePreserve.All would cause an instance ctor to be marked and that would in turn lead to MarkInterfaceImplementations being called
				//    Without an instance ctor, MarkInterfaceImplementations is not called and then TypePreserve.All isn't truly respected.
				// * If an assembly has the action Copy and had ResolveFromAssemblyStep ran for the assembly, then InitializeType will have led us here
				//    When the entire assembly is preserved, then all interfaces, base, etc will be preserved on the type, so we need to make sure
				//    all of these types are marked.  For example, if an interface implementation is of a type in another assembly that is linked,
				//    and there are no other usages of that interface type, then we need to make sure the interface type is still marked because
				//    this type is going to retain the interface implementation
				MarkRequirementsForInstantiatedTypes (type);
			} else if (AlwaysMarkTypeAsInstantiated (type)) {
				MarkRequirementsForInstantiatedTypes (type);
			}

			if (type.HasInterfaces)
				_typesWithInterfaces.Add (type);

			if (type.HasMethods) {
				// For virtuals that must be preserved, blame the declaring type.
				MarkMethodsIf (type.Methods, IsVirtualNeededByTypeDueToPreservedScope, new DependencyInfo (DependencyKind.VirtualNeededDueToPreservedScope, type));
				if (ShouldMarkTypeStaticConstructor (type) && reason.Kind != DependencyKind.TriggersCctorForCalledMethod)
					MarkStaticConstructor (type, new DependencyInfo (DependencyKind.CctorForType, type));

				if (_context.IsFeatureExcluded ("deserialization"))
					MarkMethodsIf (type.Methods, HasOnSerializeAttribute, new DependencyInfo (DependencyKind.SerializationMethodForType, type));
				else
					MarkMethodsIf (type.Methods, HasOnSerializeOrDeserializeAttribute, new DependencyInfo (DependencyKind.SerializationMethodForType, type));
			}

			DoAdditionalTypeProcessing (type);

			ApplyPreserveInfo (type);

			return type;
		}

		// Allow subclassers to mark additional things in the main processing loop
		protected virtual void DoAdditionalProcessing ()
		{
		}

		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalTypeProcessing (TypeDefinition type)
		{
		}
		
		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalFieldProcessing (FieldDefinition field)
		{
		}

		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalPropertyProcessing (PropertyDefinition property)
		{
		}

		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalEventProcessing (EventDefinition evt)
		{
		}

		// Allow subclassers to mark additional things
		protected virtual void DoAdditionalInstantiatedTypeProcessing (TypeDefinition type)
		{
		}

		void MarkAssemblyCustomAttributes (AssemblyDefinition assembly)
		{
			if (!assembly.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in assembly.CustomAttributes)
				_assemblyLevelAttributes.Enqueue (new AttributeProviderPair (attribute, assembly));
		}

		TypeDefinition GetDebuggerAttributeTargetType (CustomAttribute ca, AssemblyDefinition asm)
		{
			TypeReference targetTypeReference = null;
			foreach (var property in ca.Properties) {
				if (property.Name == "Target") {
					targetTypeReference = (TypeReference) property.Argument.Value;
					break;
				}

				if (property.Name == "TargetTypeName") {
					if (TypeNameParser.TryParseTypeAssemblyQualifiedName ((string) property.Argument.Value, out string typeName, out string assemblyName)) {
						if (string.IsNullOrEmpty (assemblyName))
							targetTypeReference = asm.MainModule.GetType (typeName);
						else
							targetTypeReference = _context.GetAssemblies ().FirstOrDefault (a => a.Name.Name == assemblyName)?.MainModule.GetType (typeName);
					}
					break;
				}
			}

			return targetTypeReference?.Resolve ();
		}
		
		void MarkTypeSpecialCustomAttributes (TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in type.CustomAttributes) {
				var attrType = attribute.Constructor.DeclaringType;
				switch (attrType.Name) {
				case "XmlSchemaProviderAttribute" when attrType.Namespace == "System.Xml.Serialization":
					MarkXmlSchemaProvider (type, attribute);
					break;
				case "DebuggerDisplayAttribute" when attrType.Namespace == "System.Diagnostics":
					MarkTypeWithDebuggerDisplayAttribute (type, attribute);
					break;
				case "DebuggerTypeProxyAttribute" when attrType.Namespace == "System.Diagnostics":
					MarkTypeWithDebuggerTypeProxyAttribute (type, attribute);
					break;
				case "EventDataAttribute" when attrType.Namespace == "System.Diagnostics.Tracing":
					if (MarkMethodsIf (type.Methods, MethodDefinitionExtensions.IsPublicInstancePropertyMethod, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, type)))
						Tracer.AddDirectDependency (attribute, new DependencyInfo (DependencyKind.CustomAttribute, type), marked: false);
					break;
				case "TypeDescriptionProviderAttribute" when attrType.Namespace == "System.ComponentModel":
					MarkTypeConverterLikeDependency (attribute, l => l.IsDefaultConstructor (), type);
					break;
				}
			}
		}

		//
		// Used for known framework attributes which can be applied to any element
		//
		bool MarkSpecialCustomAttributeDependencies (CustomAttribute ca, ICustomAttributeProvider provider)
		{
			var dt = ca.Constructor.DeclaringType;
			if (dt.Name == "TypeConverterAttribute" && dt.Namespace == "System.ComponentModel") {
				MarkTypeConverterLikeDependency (ca, l =>
					l.IsDefaultConstructor () ||
					l.Parameters.Count == 1 && l.Parameters [0].ParameterType.IsTypeOf ("System", "Type"),
					provider);
				return true;
			}

			return false;
		}

		void MarkMethodSpecialCustomAttributes (MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in method.CustomAttributes) {
				switch (attribute.Constructor.DeclaringType.FullName) {
				case "System.Web.Services.Protocols.SoapHeaderAttribute":
					MarkSoapHeader (method, attribute);
					break;
				}
			}
		}

		void MarkXmlSchemaProvider (TypeDefinition type, CustomAttribute attribute)
		{
			if (TryGetStringArgument (attribute, out string name)) {
				Tracer.AddDirectDependency (attribute, new DependencyInfo (DependencyKind.CustomAttribute, type), marked: false);
				MarkNamedMethod (type, name, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
			}
		}

		protected virtual void MarkTypeConverterLikeDependency (CustomAttribute attribute, Func<MethodDefinition, bool> predicate, ICustomAttributeProvider provider)
		{
			var args = attribute.ConstructorArguments;
			if (args.Count < 1)
				return;

			TypeDefinition tdef = null;
			switch (attribute.ConstructorArguments [0].Value) {
			case string s:
				tdef = ResolveFullyQualifiedTypeName (s);
				break;
			case TypeReference type:
				tdef = type.Resolve ();
				break;
			}

			if (tdef == null)
				return;

			Tracer.AddDirectDependency (attribute, new DependencyInfo (DependencyKind.CustomAttribute, provider), marked: false);
			MarkMethodsIf (tdef.Methods, predicate, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
		}

		void MarkTypeWithDebuggerDisplayAttribute (TypeDefinition type, CustomAttribute attribute)
		{
			if (_context.KeepMembersForDebugger) {

				// Members referenced by the DebuggerDisplayAttribute are kept even if the attribute may not be.
				// Record a logical dependency on the attribute so that we can blame it for the kept members below.
				Tracer.AddDirectDependency (attribute, new DependencyInfo (DependencyKind.CustomAttribute, type), marked: false);

				string displayString = (string) attribute.ConstructorArguments[0].Value;

				Regex regex = new Regex ("{[^{}]+}", RegexOptions.Compiled);

				foreach (Match match in regex.Matches (displayString)) {
					// Remove '{' and '}'
					string realMatch = match.Value.Substring (1, match.Value.Length - 2);

					// Remove ",nq" suffix if present
					// (it asks the expression evaluator to remove the quotes when displaying the final value)
					if (Regex.IsMatch(realMatch, @".+,\s*nq")) {
						realMatch = realMatch.Substring (0, realMatch.LastIndexOf (','));
					}

					if (realMatch.EndsWith ("()")) {
						string methodName = realMatch.Substring (0, realMatch.Length - 2);
						MethodDefinition method = GetMethodWithNoParameters (type, methodName);
						if (method != null) {
							MarkMethod (method, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
							continue;
						}
					} else {
						FieldDefinition field = GetField (type, realMatch);
						if (field != null) {
							MarkField (field, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
							continue;
						}

						PropertyDefinition property = GetProperty (type, realMatch);
						if (property != null) {
							if (property.GetMethod != null) {
								MarkMethod (property.GetMethod, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
							}
							if (property.SetMethod != null) {
								MarkMethod (property.SetMethod, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
							}
							continue;
						}
					}

					while (type != null) {
						// TODO: Non-understood DebuggerDisplayAttribute causes us to keep everything. Should this be a warning?
						MarkMethods (type, new DependencyInfo (DependencyKind.KeptForSpecialAttribute, attribute));
						MarkFields (type, includeStatic: true, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
						// This logic would miss generic parameters used in methods/fields for generic types
						Debug.Assert (!(type.BaseType is GenericInstanceType));
						type = type.BaseType?.Resolve ();
					}
					return;
				}
			}
		}

		void MarkTypeWithDebuggerTypeProxyAttribute (TypeDefinition type, CustomAttribute attribute)
		{
			if (_context.KeepMembersForDebugger) {
				object constructorArgument = attribute.ConstructorArguments[0].Value;
				TypeReference proxyTypeReference = constructorArgument as TypeReference;
				if (proxyTypeReference == null) {
					if (constructorArgument is string proxyTypeReferenceString) {
						proxyTypeReference = type.Module.GetType (proxyTypeReferenceString, runtimeName: true);
					}
				}

				if (proxyTypeReference == null) {
					return;
				}

				Tracer.AddDirectDependency (attribute, new DependencyInfo (DependencyKind.CustomAttribute, type), marked: false);
				MarkType (proxyTypeReference, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));

				TypeDefinition proxyType = proxyTypeReference.Resolve ();
				if (proxyType != null) {
					MarkMethods (proxyType, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
					MarkFields (proxyType, includeStatic: true, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
				}
			}
		}

		static bool TryGetStringArgument (CustomAttribute attribute, out string argument)
		{
			argument = null;

			if (attribute.ConstructorArguments.Count < 1)
				return false;

			argument = attribute.ConstructorArguments [0].Value as string;

			return argument != null;
		}

		protected int MarkNamedMethod (TypeDefinition type, string method_name, in DependencyInfo reason)
		{
			if (!type.HasMethods)
				return 0;

			int count = 0;
			foreach (MethodDefinition method in type.Methods) {
				if (method.Name != method_name)
					continue;

				MarkMethod (method, reason);
				count++;
			}

			return count;
		}

		void MarkSoapHeader (MethodDefinition method, CustomAttribute attribute)
		{
			if (!TryGetStringArgument (attribute, out string member_name))
				return;

			MarkNamedField (method.DeclaringType, member_name, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
			MarkNamedProperty (method.DeclaringType, member_name, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
		}

		// TODO: combine with MarkDependencyField?
		void MarkNamedField (TypeDefinition type, string field_name, in DependencyInfo reason)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.Name != field_name)
					continue;

				MarkField (field, reason);
			}
		}

		void MarkNamedProperty (TypeDefinition type, string property_name, in DependencyInfo reason)
		{
			if (!type.HasProperties)
				return;

			foreach (PropertyDefinition property in type.Properties) {
				if (property.Name != property_name)
					continue;

				// This marks methods directly without reporting the property.
				MarkMethod (property.GetMethod, reason);
				MarkMethod (property.SetMethod, reason);
			}
		}

		void MarkInterfaceImplementations (TypeDefinition type)
		{
			if (!type.HasInterfaces)
				return;

			foreach (var iface in type.Interfaces) {
				// Only mark interface implementations of interface types that have been marked.
				// This enables stripping of interfaces that are never used
				var resolvedInterfaceType = iface.InterfaceType.Resolve ();
				if (resolvedInterfaceType == null) {
					HandleUnresolvedType (iface.InterfaceType);
					continue;
				}
				
				if (ShouldMarkInterfaceImplementation (type, iface, resolvedInterfaceType))
					MarkInterfaceImplementation (iface, type);
			}
		}

		void MarkGenericParameterProvider (IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			foreach (GenericParameter parameter in provider.GenericParameters)
				MarkGenericParameter (parameter);
		}

		void MarkGenericParameter (GenericParameter parameter)
		{
			MarkCustomAttributes (parameter, new DependencyInfo (DependencyKind.GenericParameterCustomAttribute, parameter.Owner));
			if (!parameter.HasConstraints)
				return;

			foreach (var constraint in parameter.Constraints) {
				MarkCustomAttributes (constraint, new DependencyInfo (DependencyKind.GenericParameterConstraintCustomAttribute, parameter.Owner));
				MarkType (constraint.ConstraintType, new DependencyInfo (DependencyKind.GenericParameterConstraintType, parameter.Owner));
			}
		}

		bool IsVirtualNeededByTypeDueToPreservedScope (MethodDefinition method)
		{
			if (!method.IsVirtual)
				return false;

			var base_list = Annotations.GetBaseMethods (method);
			if (base_list == null)
				return false;

			foreach (MethodDefinition @base in base_list) {
				// Just because the type is marked does not mean we need interface methods.
				// if the type is never instantiated, interfaces will be removed
				if (@base.DeclaringType.IsInterface)
					continue;
				
				// If the type is marked, we need to keep overrides of abstract members defined in assemblies
				// that are copied.  However, if the base method is virtual, then we don't need to keep the override
				// until the type could be instantiated
				if (!@base.IsAbstract)
					continue;

				if (IgnoreScope (@base.DeclaringType.Scope))
					return true;

				if (IsVirtualNeededByTypeDueToPreservedScope (@base))
					return true;
			}

			return false;
		}
		
		bool IsVirtualNeededByInstantiatedTypeDueToPreservedScope (MethodDefinition method)
		{
			if (!method.IsVirtual)
				return false;

			var base_list = Annotations.GetBaseMethods (method);
			if (base_list == null)
				return false;

			foreach (MethodDefinition @base in base_list) {
				if (IgnoreScope (@base.DeclaringType.Scope))
					return true;

				if (IsVirtualNeededByTypeDueToPreservedScope (@base))
					return true;
			}

			return false;
		}

		static bool IsSpecialSerializationConstructor (MethodDefinition method)
		{
			if (!method.IsInstanceConstructor ())
				return false;

			var parameters = method.Parameters;
			if (parameters.Count != 2)
				return false;

			return parameters [0].ParameterType.Name == "SerializationInfo" &&
				parameters [1].ParameterType.Name == "StreamingContext";
		}

		protected bool MarkMethodsIf (Collection<MethodDefinition> methods, Func<MethodDefinition, bool> predicate, in DependencyInfo reason)
		{
			bool marked = false;
			foreach (MethodDefinition method in methods) {
				if (predicate (method)) {
					MarkMethod (method, reason);
					marked = true;
				}
			}
			return marked;
		}

		protected MethodDefinition MarkMethodIf (Collection<MethodDefinition> methods, Func<MethodDefinition, bool> predicate, in DependencyInfo reason)
		{
			foreach (MethodDefinition method in methods) {
				if (predicate (method)) {
					return MarkMethod (method, reason);
				}
			}

			return null;
		}

		protected bool MarkDefaultConstructor (TypeDefinition type, in DependencyInfo reason)
		{
			if (type?.HasMethods != true)
				return false;

			return MarkMethodIf (type.Methods, MethodDefinitionExtensions.IsDefaultConstructor, reason) != null;
		}

		static bool IsNonEmptyStaticConstructor (MethodDefinition method)
		{
			if (!method.IsStaticConstructor ())
				return false;

			if (!method.HasBody || !method.IsIL)
				return true;

			if (method.Body.CodeSize != 1)
				return true;

			return method.Body.Instructions [0].OpCode.Code != Code.Ret;
		}

		static bool HasOnSerializeAttribute (MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return false;
			foreach (var ca in method.CustomAttributes) {
				var cat = ca.AttributeType;
				if (cat.Namespace != "System.Runtime.Serialization")
					continue;
				switch (cat.Name) {
				case "OnSerializedAttribute":
				case "OnSerializingAttribute":
					return true;
				}
			}
			return false;
		}

		static bool HasOnSerializeOrDeserializeAttribute (MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return false;
			foreach (var ca in method.CustomAttributes) {
				var cat = ca.AttributeType;
				if (cat.Namespace != "System.Runtime.Serialization")
					continue;
				switch (cat.Name) {
				case "OnDeserializedAttribute":
				case "OnDeserializingAttribute":
				case "OnSerializedAttribute":
				case "OnSerializingAttribute":
					return true;
				}
			}
			return false;
		}

		protected virtual bool AlwaysMarkTypeAsInstantiated (TypeDefinition td)
		{
			switch (td.Name) {
				// These types are created from native code which means we are unable to track when they are instantiated
				// Since these are such foundational types, let's take the easy route and just always assume an instance of one of these
				// could exist
				case "Delegate":
				case "MulticastDelegate":
				case "ValueType":
				case "Enum":
					return td.Namespace == "System";
			}

			return false;
		}

		void MarkEventSourceProviders (TypeDefinition td)
		{
			foreach (var nestedType in td.NestedTypes) {
				if (BCL.EventTracingForWindows.IsProviderName (nestedType.Name))
					MarkStaticFields (nestedType, new DependencyInfo (DependencyKind.EventSourceProviderField, td));
			}
		}

		protected virtual void MarkMulticastDelegate (TypeDefinition type)
		{
			MarkMethodCollection (type.Methods, new DependencyInfo (DependencyKind.MethodForSpecialType, type));
		}

		TypeDefinition ResolveFullyQualifiedTypeName (string name)
		{
			if (!TypeNameParser.TryParseTypeAssemblyQualifiedName (name, out string typeName, out string assemblyName))
				return null;

			foreach (var assemblyDefinition in _context.GetAssemblies ()) {
				if (assemblyName != null && assemblyDefinition.Name.Name != assemblyName)
					continue;

				var foundType = assemblyDefinition.MainModule.GetType (typeName);
				if (foundType == null)
					continue;

				return foundType;
			}

			return null;
		}

		protected (TypeReference, DependencyInfo) GetOriginalType (TypeReference type, DependencyInfo reason)
		{
			while (type is TypeSpecification specification) {
				if (type is GenericInstanceType git) {
					MarkGenericArguments (git);
					Debug.Assert (!(specification.ElementType is TypeSpecification));
				}

				if (type is IModifierType mod)
					MarkModifierType (mod);

				if (type is FunctionPointerType fnptr) {
					MarkParameters (fnptr);
					MarkType (fnptr.ReturnType, new DependencyInfo (DependencyKind.ReturnType, fnptr));
					break; // FunctionPointerType is the original type
				}

				// Blame the type reference (which isn't marked) on the original reason.
				Tracer.AddDirectDependency (specification, reason, marked: false);
				// Blame the outgoing element type on the specification.
				(type, reason) = (specification.ElementType, new DependencyInfo (DependencyKind.ElementType, specification));
			}

			return (type, reason);
		}

		void MarkParameters (FunctionPointerType fnptr)
		{
			if (!fnptr.HasParameters)
				return;

			for (int i = 0; i < fnptr.Parameters.Count; i++)
			{
				MarkType (fnptr.Parameters[i].ParameterType, new DependencyInfo (DependencyKind.ParameterType, fnptr));
			}
		}

		void MarkModifierType (IModifierType mod)
		{
			MarkType (mod.ModifierType, new DependencyInfo (DependencyKind.ModifierType, mod));
		}

		void MarkGenericArguments (IGenericInstance instance)
		{
			foreach (TypeReference argument in instance.GenericArguments)
				MarkType (argument, new DependencyInfo (DependencyKind.GenericArgumentType, instance));

			MarkGenericArgumentConstructors (instance);
		}

		void MarkGenericArgumentConstructors (IGenericInstance instance)
		{
			var arguments = instance.GenericArguments;

			var generic_element = GetGenericProviderFromInstance (instance);
			if (generic_element == null)
				return;

			var parameters = generic_element.GenericParameters;

			if (arguments.Count != parameters.Count)
				return;

			for (int i = 0; i < arguments.Count; i++) {
				var argument = arguments [i];
				var parameter = parameters [i];

				if (!parameter.HasDefaultConstructorConstraint)
					continue;

				var argument_definition = argument.Resolve ();
				MarkDefaultConstructor (argument_definition, new DependencyInfo (DependencyKind.DefaultCtorForNewConstrainedGenericArgument, instance));
			}
		}

		static IGenericParameterProvider GetGenericProviderFromInstance (IGenericInstance instance)
		{
			if (instance is GenericInstanceMethod method)
				return method.ElementMethod.Resolve ();

			if (instance is GenericInstanceType type)
				return type.ElementType.Resolve ();

			return null;
		}

		void ApplyPreserveInfo (TypeDefinition type)
		{
			ApplyPreserveMethods (type);

			if (!Annotations.TryGetPreserve (type, out TypePreserve preserve))
				return;

			switch (preserve) {
			case TypePreserve.All:
				// TODO: it seems like PreserveAll on a type won't necessarily keep nested types,
				// but PreserveAll on an assembly will. Is this correct?
				MarkFields (type, true, new DependencyInfo (DependencyKind.TypePreserve, type));
				MarkMethods (type, new DependencyInfo (DependencyKind.TypePreserve, type));
				break;
			case TypePreserve.Fields:
				if (!MarkFields (type, true, new DependencyInfo (DependencyKind.TypePreserve, type), true))
					_context.LogMessage ($"Type {type.FullName} has no fields to preserve");
				break;
			case TypePreserve.Methods:
				if (!MarkMethods (type, new DependencyInfo (DependencyKind.TypePreserve, type)))
					_context.LogMessage ($"Type {type.FullName} has no methods to preserve");
				break;
			}
		}

		void ApplyPreserveMethods (TypeDefinition type)
		{
			var list = Annotations.GetPreservedMethods (type);
			if (list == null)
				return;

			MarkMethodCollection (list, new DependencyInfo (DependencyKind.PreservedMethod, type));
		}

		void ApplyPreserveMethods (MethodDefinition method)
		{
			var list = Annotations.GetPreservedMethods (method);
			if (list == null)
				return;

			MarkMethodCollection (list, new DependencyInfo (DependencyKind.PreservedMethod, method));
		}

		protected bool MarkFields (TypeDefinition type, bool includeStatic, in DependencyInfo reason, bool markBackingFieldsOnlyIfPropertyMarked = false)
		{
			if (!type.HasFields)
				return false;

			foreach (FieldDefinition field in type.Fields) {
				if (!includeStatic && field.IsStatic)
					continue;

				if (markBackingFieldsOnlyIfPropertyMarked && field.Name.EndsWith (">k__BackingField", StringComparison.Ordinal)) {
					// We can't reliably construct the expected property name from the backing field name for all compilers
					// because csc shortens the name of the backing field in some cases
					// For example:
					// Field Name = <IFoo<int>.Bar>k__BackingField
					// Property Name = IFoo<System.Int32>.Bar
					//
					// instead we will search the properties and find the one that makes use of the current backing field
					var propertyDefinition = SearchPropertiesForMatchingFieldDefinition (field);
					if (propertyDefinition != null && !Annotations.IsMarked (propertyDefinition))
						continue;
				}
				MarkField (field, reason);
			}

			return true;
		}

		static PropertyDefinition SearchPropertiesForMatchingFieldDefinition (FieldDefinition field)
		{
			foreach (var property in field.DeclaringType.Properties) {
				var instr = property.GetMethod?.Body?.Instructions;
				if (instr == null)
					continue;

				foreach (var ins in instr) {
					if (ins?.Operand == field)
						return property;
				}
			}

			return null;
		}

		protected void MarkStaticFields (TypeDefinition type, in DependencyInfo reason)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.IsStatic)
					MarkField (field, reason);
			}
		}

		protected virtual bool MarkMethods (TypeDefinition type, in DependencyInfo reason)
		{
			if (!type.HasMethods)
				return false;

			MarkMethodCollection (type.Methods, reason);
			return true;
		}

		void MarkMethodCollection (IList<MethodDefinition> methods, in DependencyInfo reason)
		{
			foreach (MethodDefinition method in methods)
				MarkMethod (method, reason);
		}

		protected void MarkIndirectlyCalledMethod (MethodDefinition method, in DependencyInfo reason)
		{
			MarkMethod (method, reason);
			Annotations.MarkIndirectlyCalledMethod (method);
		}

		protected virtual MethodDefinition MarkMethod (MethodReference reference, DependencyInfo reason)
		{
			(reference, reason) = GetOriginalMethod (reference, reason);

			if (reference.DeclaringType is ArrayType)
				return null;

			if (reference.DeclaringType is GenericInstanceType) {
				// Blame the method reference on the original reason without marking it.
				Tracer.AddDirectDependency (reference, reason, marked: false);
				MarkType (reference.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, reference));
				// Mark the resolved method definition as a dependency of the reference.
				reason = new DependencyInfo (DependencyKind.MethodOnGenericInstance, reference);
			}

//			if (IgnoreScope (reference.DeclaringType.Scope))
//				return;

			MethodDefinition method = reference.Resolve ();

			if (method == null) {
				HandleUnresolvedMethod (reference);
				return null;
			}

			if (Annotations.GetAction (method) == MethodAction.Nothing)
				Annotations.SetAction (method, MethodAction.Parse);

			EnqueueMethod (method, reason);

			return method;
		}

		AssemblyDefinition ResolveAssembly (IMetadataScope scope)
		{
			AssemblyDefinition assembly = _context.Resolve (scope);
			MarkAssembly (assembly);
			return assembly;
		}

		protected (MethodReference, DependencyInfo) GetOriginalMethod (MethodReference method, DependencyInfo reason)
		{
			while (method is MethodSpecification specification) {
				// Blame the method reference (which isn't marked) on the original reason.
				Tracer.AddDirectDependency (specification, reason, marked: false);
				// Blame the outgoing element method on the specification.
				if (method is GenericInstanceMethod gim)
					MarkGenericArguments (gim);

				(method, reason) = (specification.ElementMethod, new DependencyInfo (DependencyKind.ElementMethod, specification));
				Debug.Assert (!(method is MethodSpecification));
			}

			return (method, reason);
		}

		protected virtual void ProcessMethod (MethodDefinition method, in DependencyInfo reason)
		{
#if DEBUG
			if (!_methodReasons.Contains (reason.Kind))
				throw new ArgumentOutOfRangeException ($"Internal error: unsupported method dependency {reason.Kind}");
#endif

			// Record the reason for marking a method on each call. The logic under CheckProcessed happens
			// only once per method.
			switch (reason.Kind) {
			case DependencyKind.AlreadyMarked:
				Debug.Assert (Annotations.IsMarked (method));
				break;
			default:
				Annotations.Mark (method, reason);
				break;
			}

			bool markedForCall = (
				reason.Kind == DependencyKind.DirectCall ||
				reason.Kind == DependencyKind.VirtualCall ||
				reason.Kind == DependencyKind.Newobj
			);
			if (markedForCall) {
				// Record declaring type of a called method up-front as a special case so that we may
				// track at least some method calls that trigger a cctor.
				MarkType (method.DeclaringType, new DependencyInfo (DependencyKind.DeclaringTypeOfCalledMethod, method));
			}

			if (CheckProcessed (method))
				return;

			if (!markedForCall)
				MarkType (method.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, method));
			MarkCustomAttributes (method, new DependencyInfo (DependencyKind.CustomAttribute, method));
			MarkSecurityDeclarations (method, new DependencyInfo (DependencyKind.CustomAttribute, method));

			MarkGenericParameterProvider (method);

			if (method.IsInstanceConstructor ()) {
				MarkRequirementsForInstantiatedTypes (method.DeclaringType);
				Tracer.AddDirectDependency (method.DeclaringType, new DependencyInfo (DependencyKind.InstantiatedByCtor, method), marked: false);
			}

			if (method.IsConstructor) {
				if (!Annotations.ProcessSatelliteAssemblies && KnownMembers.IsSatelliteAssemblyMarker (method))
					Annotations.ProcessSatelliteAssemblies = true;
			} else if (method.IsPropertyMethod ())
				MarkProperty (method.GetProperty (), new DependencyInfo (DependencyKind.PropertyOfPropertyMethod, method));
			else if (method.IsEventMethod ())
				MarkEvent (method.GetEvent (), new DependencyInfo (DependencyKind.EventOfEventMethod, method));

			if (method.HasParameters) {
				foreach (ParameterDefinition pd in method.Parameters) {
					MarkType (pd.ParameterType, new DependencyInfo (DependencyKind.ParameterType, method));
					MarkCustomAttributes (pd, new DependencyInfo (DependencyKind.ParameterAttribute, method));
					MarkMarshalSpec (pd, new DependencyInfo (DependencyKind.ParameterMarshalSpec, method));
				}
			}

			if (method.HasOverrides) {
				foreach (MethodReference ov in method.Overrides) {
					MarkMethod (ov, new DependencyInfo (DependencyKind.MethodImplOverride, method));
					MarkExplicitInterfaceImplementation (method, ov);
				}
			}

			MarkMethodSpecialCustomAttributes (method);

			if (method.IsVirtual)
				_virtual_methods.Add (method);

			MarkNewCodeDependencies (method);

			MarkBaseMethods (method);

			MarkType (method.ReturnType, new DependencyInfo (DependencyKind.ReturnType, method));
			MarkCustomAttributes (method.MethodReturnType, new DependencyInfo (DependencyKind.ReturnTypeAttribute, method));
			MarkMarshalSpec (method.MethodReturnType, new DependencyInfo (DependencyKind.ReturnTypeMarshalSpec, method));

			if (method.IsPInvokeImpl || method.IsInternalCall) {
				ProcessInteropMethod (method);
			}

			if (ShouldParseMethodBody (method))
				MarkMethodBody (method.Body);

			DoAdditionalMethodProcessing (method);

			ApplyPreserveMethods (method);
		}

		// Allow subclassers to mark additional things when marking a method
		protected virtual void DoAdditionalMethodProcessing (MethodDefinition method)
		{
		}

		protected virtual void MarkRequirementsForInstantiatedTypes (TypeDefinition type)
		{
			if (Annotations.IsInstantiated (type))
				return;

			Annotations.MarkInstantiated (type);

			MarkInterfaceImplementations (type);

			foreach (var method in GetRequiredMethodsForInstantiatedType (type))
				MarkMethod (method, new DependencyInfo (DependencyKind.MethodForInstantiatedType, type));

			DoAdditionalInstantiatedTypeProcessing (type);
		}

		/// <summary>
		/// Collect methods that must be marked once a type is determined to be instantiated.
		///
		/// This method is virtual in order to give derived mark steps an opportunity to modify the collection of methods that are needed 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		protected virtual IEnumerable<MethodDefinition> GetRequiredMethodsForInstantiatedType (TypeDefinition type)
		{
			foreach (var method in type.Methods) {
				if (method.IsFinalizer () || IsVirtualNeededByInstantiatedTypeDueToPreservedScope (method))
					yield return method;
			}
		}

		void MarkExplicitInterfaceImplementation (MethodDefinition method, MethodReference ov)
		{
			var resolvedOverride = ov.Resolve ();
			
			if (resolvedOverride == null) {
				HandleUnresolvedMethod (ov);
				return;
			}

			if (resolvedOverride.DeclaringType.IsInterface) {
				foreach (var ifaceImpl in method.DeclaringType.Interfaces) {
					var resolvedInterfaceType = ifaceImpl.InterfaceType.Resolve ();
					if (resolvedInterfaceType == null) {
						HandleUnresolvedType (ifaceImpl.InterfaceType);
						continue;
					}

					if (resolvedInterfaceType == resolvedOverride.DeclaringType) {
						MarkInterfaceImplementation (ifaceImpl, method.DeclaringType);
						return;
					}
				}
			}
		}

		void MarkNewCodeDependencies (MethodDefinition method)
		{
			switch (Annotations.GetAction (method)) {
			case MethodAction.ConvertToStub:
				if (!method.IsInstanceConstructor ())
					return;

				var baseType = method.DeclaringType.BaseType.Resolve ();
				if (!MarkDefaultConstructor (baseType, new DependencyInfo (DependencyKind.BaseDefaultCtorForStubbedMethod, method)))
					throw new NotSupportedException ($"Cannot stub constructor on '{method.DeclaringType}' when base type does not have default constructor");

				break;

			case MethodAction.ConvertToThrow:
				MarkAndCacheConvertToThrowExceptionCtor (new DependencyInfo (DependencyKind.UnreachableBodyRequirement, method));
				break;
			}
		}

		protected virtual void MarkAndCacheConvertToThrowExceptionCtor (DependencyInfo reason)
		{
			if (_context.MarkedKnownMembers.NotSupportedExceptionCtorString != null)
				return;

			var nse = BCL.FindPredefinedType ("System", "NotSupportedException", _context);
			if (nse == null)
				throw new NotSupportedException ("Missing predefined 'System.NotSupportedException' type");

			MarkType (nse, reason);

			var nseCtor = MarkMethodIf (nse.Methods, KnownMembers.IsNotSupportedExceptionCtorString, reason);
			_context.MarkedKnownMembers.NotSupportedExceptionCtorString = nseCtor ?? throw new MarkException ($"Could not find constructor on '{nse.FullName}'");

			var objectType = BCL.FindPredefinedType ("System", "Object", _context);
			if (objectType == null)
				throw new NotSupportedException ("Missing predefined 'System.Object' type");

			MarkType (objectType, reason);

			var objectCtor = MarkMethodIf (objectType.Methods, MethodDefinitionExtensions.IsDefaultConstructor, reason);
			_context.MarkedKnownMembers.ObjectCtor = objectCtor ?? throw new MarkException ($"Could not find constructor on '{objectType.FullName}'");
		}

		bool MarkDisablePrivateReflectionAttribute ()
		{
			if (_context.MarkedKnownMembers.DisablePrivateReflectionAttributeCtor != null)
				return false;

			var disablePrivateReflection = BCL.FindPredefinedType ("System.Runtime.CompilerServices", "DisablePrivateReflectionAttribute", _context);
			if (disablePrivateReflection == null)
				throw new NotSupportedException ("Missing predefined 'System.Runtime.CompilerServices.DisablePrivateReflectionAttribute' type");

			MarkType (disablePrivateReflection, DependencyInfo.DisablePrivateReflectionRequirement);

			var ctor = MarkMethodIf (disablePrivateReflection.Methods, MethodDefinitionExtensions.IsDefaultConstructor, new DependencyInfo (DependencyKind.DisablePrivateReflectionRequirement, disablePrivateReflection));
			_context.MarkedKnownMembers.DisablePrivateReflectionAttributeCtor = ctor ?? throw new MarkException ($"Could not find constructor on '{disablePrivateReflection.FullName}'");
			return true;
		}

		void MarkBaseMethods (MethodDefinition method)
		{
			var base_methods = Annotations.GetBaseMethods (method);
			if (base_methods == null)
				return;

			foreach (MethodDefinition base_method in base_methods) {
				if (base_method.DeclaringType.IsInterface && !method.DeclaringType.IsInterface)
					continue;

				MarkMethod (base_method, new DependencyInfo (DependencyKind.BaseMethod, method));
				MarkBaseMethods (base_method);
			}
		}

		void ProcessInteropMethod(MethodDefinition method)
		{
			TypeDefinition returnTypeDefinition = method.ReturnType.Resolve ();

			if (!string.IsNullOrEmpty(_context.PInvokesListFile) && method.IsPInvokeImpl) {
				_context.PInvokes.Add (new PInvokeInfo {
					AssemblyName = method.DeclaringType.Module.Name,
					EntryPoint = method.PInvokeInfo.EntryPoint,
					FullName = method.FullName,
					ModuleName = method.PInvokeInfo.Module.Name
				});
			}

			const bool includeStaticFields = false;
			if (returnTypeDefinition != null && !returnTypeDefinition.IsImport) {
				MarkDefaultConstructor (returnTypeDefinition, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
				MarkFields (returnTypeDefinition, includeStaticFields, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
			}

			if (method.HasThis && !method.DeclaringType.IsImport) {
				MarkFields (method.DeclaringType, includeStaticFields, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
			}

			foreach (ParameterDefinition pd in method.Parameters) {
				TypeReference paramTypeReference = pd.ParameterType;
				if (paramTypeReference is TypeSpecification) {
					paramTypeReference = (paramTypeReference as TypeSpecification).ElementType;
				}
				TypeDefinition paramTypeDefinition = paramTypeReference.Resolve ();
				if (paramTypeDefinition != null && !paramTypeDefinition.IsImport) {
					MarkFields (paramTypeDefinition, includeStaticFields, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
					if (pd.ParameterType.IsByReference) {
						MarkDefaultConstructor (paramTypeDefinition, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
					}
				}
			}
		}

		protected virtual bool ShouldParseMethodBody (MethodDefinition method)
		{
			if (!method.HasBody)
				return false;

			switch (Annotations.GetAction (method)) {
			case MethodAction.ForceParse:
				return true;
			case MethodAction.Parse:
				AssemblyDefinition assembly = ResolveAssembly (method.DeclaringType.Scope);
				switch (Annotations.GetAction (assembly)) {
				case AssemblyAction.Link:
				case AssemblyAction.Copy:
				case AssemblyAction.CopyUsed:
				case AssemblyAction.AddBypassNGen:
				case AssemblyAction.AddBypassNGenUsed:
					return true;
				default:
					return false;
				}
			default:
				return false;
			}
		}

		protected void MarkProperty (PropertyDefinition prop, in DependencyInfo reason)
		{
			Tracer.AddDirectDependency (prop, reason, marked: false);
			// Consider making this more similar to MarkEvent method?
			MarkCustomAttributes (prop, new DependencyInfo (DependencyKind.CustomAttribute, prop));
			DoAdditionalPropertyProcessing (prop);
		}

		protected virtual void MarkEvent (EventDefinition evt, in DependencyInfo reason)
		{
			// Record the event without marking it in Annotations.
			Tracer.AddDirectDependency (evt, reason, marked: false);
			MarkCustomAttributes (evt, new DependencyInfo (DependencyKind.CustomAttribute, evt));
			MarkMethodIfNotNull (evt.AddMethod, new DependencyInfo (DependencyKind.EventMethod, evt));
			MarkMethodIfNotNull (evt.InvokeMethod, new DependencyInfo (DependencyKind.EventMethod, evt));
			MarkMethodIfNotNull (evt.RemoveMethod, new DependencyInfo (DependencyKind.EventMethod, evt));
			DoAdditionalEventProcessing (evt);
		}

		void MarkMethodIfNotNull (MethodReference method, in DependencyInfo reason)
		{
			if (method == null)
				return;

			MarkMethod (method, reason);
		}

		protected virtual void MarkMethodBody (MethodBody body)
		{
			if (_context.IsOptimizationEnabled (CodeOptimizations.UnreachableBodies, body.Method) && IsUnreachableBody (body)) {
				MarkAndCacheConvertToThrowExceptionCtor (new DependencyInfo (DependencyKind.UnreachableBodyRequirement, body.Method));
				_unreachableBodies.Add (body);
				return;
			}

			foreach (VariableDefinition var in body.Variables)
				MarkType (var.VariableType, new DependencyInfo (DependencyKind.VariableType, body.Method));

			foreach (ExceptionHandler eh in body.ExceptionHandlers)
				if (eh.HandlerType == ExceptionHandlerType.Catch)
					MarkType (eh.CatchType, new DependencyInfo (DependencyKind.CatchType, body.Method));

			foreach (Instruction instruction in body.Instructions)
				MarkInstruction (instruction, body.Method);

			MarkInterfacesNeededByBodyStack (body);

			MarkReflectionLikeDependencies (body);

			PostMarkMethodBody (body);
		}

		bool IsUnreachableBody (MethodBody body)
		{
			return !body.Method.IsStatic
				&& !Annotations.IsInstantiated (body.Method.DeclaringType)
				&& MethodBodyScanner.IsWorthConvertingToThrow (body);
		}
		

		partial void PostMarkMethodBody (MethodBody body);

		void MarkInterfacesNeededByBodyStack (MethodBody body)
		{
			// If a type could be on the stack in the body and an interface it implements could be on the stack on the body
			// then we need to mark that interface implementation.  When this occurs it is not safe to remove the interface implementation from the type
			// even if the type is never instantiated
			var implementations = MethodBodyScanner.GetReferencedInterfaces (_context.Annotations, body);
			if (implementations == null)
				return;

			foreach (var (implementation, type) in implementations)
				MarkInterfaceImplementation (implementation, type);
		}

		protected virtual void MarkInstruction (Instruction instruction, MethodDefinition method)
		{
			switch (instruction.OpCode.OperandType) {
			case OperandType.InlineField:
				MarkField ((FieldReference) instruction.Operand, new DependencyInfo (DependencyKind.FieldAccess, method));
				break;
			case OperandType.InlineMethod:
			{
				DependencyKind dependencyKind = instruction.OpCode.Code switch {
					Code.Jmp => DependencyKind.DirectCall,
					Code.Call => DependencyKind.DirectCall,
					Code.Callvirt => DependencyKind.VirtualCall,
					Code.Newobj => DependencyKind.Newobj,
					Code.Ldvirtftn => DependencyKind.Ldvirtftn,
					Code.Ldftn => DependencyKind.Ldftn,
					_ => throw new Exception ($"unexpected opcode {instruction.OpCode}")
				};
				MarkMethod ((MethodReference) instruction.Operand, new DependencyInfo (dependencyKind, method));
				break;
			}
			case OperandType.InlineTok:
			{
				object token = instruction.Operand;
				Debug.Assert (instruction.OpCode.Code == Code.Ldtoken);
				var reason = new DependencyInfo (DependencyKind.Ldtoken, method);
				if (token is TypeReference typeReference)
					MarkType (typeReference, reason);
				else if (token is MethodReference methodReference)
					MarkMethod (methodReference, reason);
				else
					MarkField ((FieldReference) token, reason);
				break;
			}
			case OperandType.InlineType:
				MarkType ((TypeReference) instruction.Operand, new DependencyInfo (DependencyKind.InstructionTypeRef, method));
				break;
			default:
				break;
			}
		}

		protected virtual void HandleUnresolvedType (TypeReference reference)
		{
			if (!_context.IgnoreUnresolved) {
				throw new ResolutionException (reference);
			}
		}

		protected virtual void HandleUnresolvedMethod (MethodReference reference)
		{
			if (!_context.IgnoreUnresolved) {
				throw new ResolutionException (reference);
			}
		}

		protected virtual void HandleUnresolvedField (FieldReference reference)
		{
			if (!_context.IgnoreUnresolved) {
				throw new ResolutionException (reference);
			}
		}

		protected virtual bool ShouldMarkInterfaceImplementation (TypeDefinition type, InterfaceImplementation iface, TypeDefinition resolvedInterfaceType)
		{
			if (Annotations.IsMarked (iface))
				return false;

			if (Annotations.IsMarked (resolvedInterfaceType))
				return true;

			if (!_context.IsOptimizationEnabled (CodeOptimizations.UnusedInterfaces, type))
				return true;

			// It's hard to know if a com or windows runtime interface will be needed from managed code alone,
			// so as a precaution we will mark these interfaces once the type is instantiated
			if (resolvedInterfaceType.IsImport || resolvedInterfaceType.IsWindowsRuntime)
				return true;

			return IsFullyPreserved (type);
		}

		protected virtual void MarkInterfaceImplementation (InterfaceImplementation iface, TypeDefinition type)
		{
			// Blame the type that has the interfaceimpl, expecting the type itself to get marked for other reasons.
			MarkCustomAttributes (iface, new DependencyInfo (DependencyKind.CustomAttribute, iface));
			// Blame the interface type on the interfaceimpl itself.
			MarkType (iface.InterfaceType, new DependencyInfo (DependencyKind.InterfaceImplementationInterfaceType, iface));
			Annotations.Mark (iface, new DependencyInfo (DependencyKind.InterfaceImplementationOnType, type));
		}

		bool HasManuallyTrackedDependency (MethodBody methodBody)
		{
			return PreserveDependencyLookupStep.HasPreserveDependencyAttribute (methodBody.Method);
		}

		//
		// Extension point for reflection logic handling customization
		//
		protected virtual bool ProcessReflectionDependency (MethodBody body, Instruction instruction)
		{
			return false;
		}

		//
		// Tries to mark additional dependencies used in reflection like calls (e.g. typeof (MyClass).GetField ("fname"))
		//
		protected virtual void MarkReflectionLikeDependencies (MethodBody body)
		{
			if (HasManuallyTrackedDependency (body))
				return;

			var scanner = new ReflectionMethodBodyScanner (this);
			scanner.ScanAndProcessReturnValue (body);

			var instructions = body.Instructions;
			ReflectionPatternDetector detector = new ReflectionPatternDetector (this, body.Method);

			//
			// Starting at 1 because all patterns require at least 1 instruction backward lookup
			//
			for (var i = 1; i < instructions.Count; i++) {
				var instruction = instructions [i];

				if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt)
					continue;

				if (ProcessReflectionDependency (body, instruction))
					continue;

				if (!(instruction.Operand is MethodReference methodCalled))
					continue;

				var methodCalledDefinition = methodCalled.Resolve ();
				if (methodCalledDefinition == null)
					continue;

				ReflectionPatternContext reflectionContext = new ReflectionPatternContext (_context, body.Method, methodCalledDefinition, i);
				try {
					detector.Process (ref reflectionContext);
				}
				finally {
					reflectionContext.Dispose ();
				}
			}
		}

		/// <summary>
		/// Helper struct to pass around context information about reflection pattern
		/// as a single parameter (and have a way to extend this in the future if we need to easily).
		/// Also implements a simple validation mechanism to check that the code does report patter recognition
		/// results for all methods it works on.
		/// The promise of the pattern recorder is that for a given reflection method, it will either not talk
		/// about it ever, or it will always report recognized/unrecognized.
		/// </summary>
		struct ReflectionPatternContext : IDisposable
		{
			readonly LinkContext _context;
#if DEBUG
			bool _patternAnalysisAttempted;
			bool _patternReported;
#endif

			public MethodDefinition MethodCalling { get; private set; }
			public MethodDefinition MethodCalled { get; private set; }
			public int InstructionIndex { get; private set; }

			public ReflectionPatternContext (LinkContext context, MethodDefinition methodCalling, MethodDefinition methodCalled, int instructionIndex)
			{
				_context = context;
				MethodCalling = methodCalling;
				MethodCalled = methodCalled;
				InstructionIndex = instructionIndex;

#if DEBUG
				_patternAnalysisAttempted = false;
				_patternReported = false;
#endif
			}

			[Conditional("DEBUG")]
			public void AnalyzingPattern ()
			{
#if DEBUG
				_patternAnalysisAttempted = true;
#endif
			}

			[Conditional ("DEBUG")]
			public void RecordHandledPattern ()
			{
#if DEBUG
				_patternReported = true;
#endif
			}

			public void RecordRecognizedPattern<T> (T accessedItem, Action mark)
				where T : IMemberDefinition
			{
#if DEBUG
				if (!_patternAnalysisAttempted)
					throw new InvalidOperationException ($"Internal error: To correctly report all patterns, when starting to analyze a pattern the AnalyzingPattern must be called first. {MethodCalling} -> {MethodCalled}");

				_patternReported = true;
#endif

				mark ();
				_context.ReflectionPatternRecorder.RecognizedReflectionAccessPattern (MethodCalling, MethodCalled, accessedItem);
			}

			public void RecordUnrecognizedPattern (string message)
			{
#if DEBUG
				if (!_patternAnalysisAttempted)
					throw new InvalidOperationException ($"Internal error: To correctly report all patterns, when starting to analyze a pattern the AnalyzingPattern must be called first. {MethodCalling} -> {MethodCalled}");

				_patternReported = true;
#endif

				_context.ReflectionPatternRecorder.UnrecognizedReflectionAccessPattern (MethodCalling, MethodCalled, message);
			}

			public void Dispose ()
			{
#if DEBUG
				if (_patternAnalysisAttempted && !_patternReported)
					throw new InvalidOperationException ($"Internal error: A reflection pattern was analyzed, but no result was reported. {MethodCalling} -> {MethodCalled}");
#endif
			}
		}

		class ReflectionPatternDetector
		{
			readonly MarkStep _markStep;
			readonly MethodDefinition _methodCalling;
			readonly Collection<Instruction> _instructions;

			public ReflectionPatternDetector (MarkStep markStep, MethodDefinition callingMethod)
			{
				_markStep = markStep;
				_methodCalling = callingMethod;
				_instructions = _methodCalling.Body.Instructions;
			}

			public void Process (ref ReflectionPatternContext reflectionContext)
			{
				var methodCalled = reflectionContext.MethodCalled;
				var instructionIndex = reflectionContext.InstructionIndex;
				var methodCalledType = methodCalled.DeclaringType;

				switch (methodCalledType.Name) {
					//
					// System.Type
					//
					case "Type" when methodCalledType.Namespace == "System":

						// Some of the overloads are implemented by calling another overload of the same name.
						// These "internal" calls are not interesting to analyze, the outermost call is the one
						// which needs to be analyzed. The assumption is that all overloads have the same semantics.
						// (for example that all overload of GetConstructor if used require the specified type to have a .ctor).
						if (_methodCalling.DeclaringType == methodCalled.DeclaringType && _methodCalling.Name == methodCalled.Name)
							break;

						switch (methodCalled.Name) {
							//
							// GetField (string)
							// GetField (string, BindingFlags)
							//
							case "GetField":
								if (!methodCalled.IsStatic)
									ProcessSystemTypeGetMemberLikeCall (ref reflectionContext, System.Reflection.MemberTypes.Field, instructionIndex - 1);

								break;

							//
							// GetEvent (string)
							// GetEvent (string, BindingFlags)
							//
							case "GetEvent":
								if (!methodCalled.IsStatic)
									ProcessSystemTypeGetMemberLikeCall (ref reflectionContext, System.Reflection.MemberTypes.Event, instructionIndex - 1);

								break;

							//
							// GetProperty (string)
							// GetProperty (string, BindingFlags)
							// GetProperty (string, Type)
							// GetProperty (string, Type[])
							// GetProperty (string, Type, Type[])
							// GetProperty (string, Type, Type[], ParameterModifier[])
							// GetProperty (string, BindingFlags, Binder, Type, Type[], ParameterModifier[])
							//
							case "GetProperty":
								if (!methodCalled.IsStatic)
									ProcessSystemTypeGetMemberLikeCall (ref reflectionContext, System.Reflection.MemberTypes.Property, instructionIndex - 1);

								break;

							//
							// GetType (string)
							// GetType (string, Boolean)
							// GetType (string, Boolean, Boolean)
							// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>)
							// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean)
							// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)
							//
							case "GetType":
								if (!methodCalled.IsStatic) {
									break;
								} else {
									reflectionContext.AnalyzingPattern ();
									
									var first_arg_instr = GetInstructionAtStackDepth (_instructions, instructionIndex - 1, methodCalled.Parameters.Count);
									if (first_arg_instr < 0) {
										reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' couldn't be decomposed");
										break;
									}

									//
									// The next value must be string constant (we don't handle anything else)
									//
									var first_arg = _instructions [first_arg_instr];
									if (first_arg.OpCode != OpCodes.Ldstr) {
										reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with argument which cannot be analyzed");
										break;
									}

									string typeName = (string)first_arg.Operand;
									TypeDefinition foundType = _markStep.ResolveFullyQualifiedTypeName (typeName);
									if (foundType == null) {
										reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with type name `{typeName}` which can't be resolved.");
										break;
									}

									var methodCalling = reflectionContext.MethodCalling;
									reflectionContext.RecordRecognizedPattern (foundType, () => _markStep.MarkType (foundType, new DependencyInfo (DependencyKind.AccessedViaReflection, methodCalling)));
								}
								break;
						}

						break;

					//
					// System.Linq.Expressions.Expression
					//
					case "Expression" when methodCalledType.Namespace == "System.Linq.Expressions":
						Instruction second_argument;
						TypeDefinition declaringType;

						if (!methodCalled.IsStatic)
							break;

						switch (methodCalled.Name) {

							//
							// static Call (Type, String, Type[], Expression[])
							//
							case "Call": {
									reflectionContext.AnalyzingPattern ();

									var first_arg_instr = GetInstructionAtStackDepth (_instructions, instructionIndex - 1, 4);
									if (first_arg_instr < 0) {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' couldn't be decomposed");
										break;
									}

									var first_arg = _instructions [first_arg_instr];
									if (first_arg.OpCode == OpCodes.Ldtoken)
										first_arg_instr++;

									declaringType = FindReflectionTypeForLookup (_instructions, first_arg_instr);
									if (declaringType == null) {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with 1st argument which cannot be analyzed");
										break;
									}

									var second_arg_instr = GetInstructionAtStackDepth (_instructions, instructionIndex - 1, 3);
									second_argument = _instructions [second_arg_instr];
									if (second_argument.OpCode != OpCodes.Ldstr) {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with 2nd argument which cannot be analyzed");
										break;
									}

									var name = (string)second_argument.Operand;

									MarkMethodsFromReflectionCall (ref reflectionContext, declaringType, name, null, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
								}

								break;

							//
							// static Property(Expression, Type, String)
							// static Field (Expression, Type, String)
							//
							case "Property":
							case "Field": {
									reflectionContext.AnalyzingPattern ();

									var second_arg_instr = GetInstructionAtStackDepth (_instructions, instructionIndex - 1, 2);
									if (second_arg_instr < 0) {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' couldn't be decomposed");
										break;
									}

									var second_arg = _instructions [second_arg_instr];
									if (second_arg.OpCode == OpCodes.Ldtoken)
										second_arg_instr++;

									declaringType = FindReflectionTypeForLookup (_instructions, second_arg_instr);
									if (declaringType == null) {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with 2nd argument which cannot be analyzed");
										break;
									}

									var third_arg_inst = GetInstructionAtStackDepth (_instructions, instructionIndex - 1, 1);
									var third_argument = _instructions [third_arg_inst];
									if (third_argument.OpCode != OpCodes.Ldstr) {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with the 3rd argument which cannot be analyzed");
										break;
									}

									var name = (string)third_argument.Operand;

									//
									// The first argument can be any expression but we are looking only for simple null
									// which we can convert to static only field lookup
									//
									var first_arg_instr = GetInstructionAtStackDepth (_instructions, instructionIndex - 1, 3);
									bool staticOnly = false;

									if (first_arg_instr >= 0) {
										var first_arg = _instructions [first_arg_instr];
										if (first_arg.OpCode == OpCodes.Ldnull)
											staticOnly = true;
									}

									if (methodCalled.Name [0] == 'P')
										MarkPropertiesFromReflectionCall (ref reflectionContext, declaringType, name, staticOnly);
									else
										MarkFieldsFromReflectionCall (ref reflectionContext, declaringType, name, staticOnly);
								}

								break;

							//
							// static New (Type)
							//
							case "New": {
									reflectionContext.AnalyzingPattern ();

									var first_arg_instr = GetInstructionAtStackDepth (_instructions, instructionIndex - 1, 1);
									if (first_arg_instr < 0) {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' couldn't be decomposed");
										break;
									}

									var first_arg = _instructions [first_arg_instr];
									if (first_arg.OpCode == OpCodes.Ldtoken)
										first_arg_instr++;

									declaringType = FindReflectionTypeForLookup (_instructions, first_arg_instr);
									if (declaringType == null) {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with 1st argument which cannot be analyzed");
										break;
									}

									MarkMethodsFromReflectionCall (ref reflectionContext, declaringType, ".ctor", 0, BindingFlags.Instance, parametersCount: 0);
								}
								break;
						}

						break;

					//
					// System.Reflection.RuntimeReflectionExtensions
					//
					case "RuntimeReflectionExtensions" when methodCalledType.Namespace == "System.Reflection":
						switch (methodCalled.Name) {
							//
							// static GetRuntimeField (this Type type, string name)
							//
							case "GetRuntimeField":
								ProcessSystemTypeGetMemberLikeCall (ref reflectionContext, System.Reflection.MemberTypes.Field, instructionIndex - 1, thisExtension: true);
								break;

							//
							// static GetRuntimeMethod (this Type type, string name, Type[] parameters)
							//
							case "GetRuntimeMethod":
								ProcessSystemTypeGetMemberLikeCall (ref reflectionContext, System.Reflection.MemberTypes.Method, instructionIndex - 1, thisExtension: true);
								break;

							//
							// static GetRuntimeProperty (this Type type, string name)
							//
							case "GetRuntimeProperty":
								ProcessSystemTypeGetMemberLikeCall (ref reflectionContext, System.Reflection.MemberTypes.Property, instructionIndex - 1, thisExtension: true);
								break;

							//
							// static GetRuntimeEvent (this Type type, string name)
							//
							case "GetRuntimeEvent":
								ProcessSystemTypeGetMemberLikeCall (ref reflectionContext, System.Reflection.MemberTypes.Event, instructionIndex - 1, thisExtension: true);
								break;
						}

						break;

					//
					// System.AppDomain
					//
					case "AppDomain" when methodCalledType.Namespace == "System":
						//
						// CreateInstance (string assemblyName, string typeName)
						// CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
						// CreateInstance (string assemblyName, string typeName, object? []? activationAttributes)
						//
						// CreateInstanceAndUnwrap (string assemblyName, string typeName)
						// CreateInstanceAndUnwrap (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
						// CreateInstanceAndUnwrap (string assemblyName, string typeName, object? []? activationAttributes)
						//
						// CreateInstanceFrom (string assemblyFile, string typeName)
						// CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
						// CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
						//
						// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName)
						// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
						// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, object? []? activationAttributes)
						//
						switch (methodCalled.Name) {
							case "CreateInstance":
							case "CreateInstanceAndUnwrap":
							case "CreateInstanceFrom":
							case "CreateInstanceFromAndUnwrap":
								ProcessActivatorCallWithStrings (ref reflectionContext, instructionIndex - 1, methodCalled.Parameters.Count < 4);
								break;
						}

						break;

					//
					// System.Reflection.Assembly
					//
					case "Assembly" when methodCalledType.Namespace == "System.Reflection":
						//
						// CreateInstance (string typeName)
						// CreateInstance (string typeName, bool ignoreCase)
						// CreateInstance (string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object []? args, CultureInfo? culture, object []? activationAttributes)
						//
						if (methodCalled.Name == "CreateInstance") {
							//
							// TODO: This could be supported for `this` only calls
							//
							reflectionContext.AnalyzingPattern ();
							reflectionContext.RecordUnrecognizedPattern ($"Activator call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' is not yet supported");
							break;
						}

						break;

					//
					// System.Activator
					//
					case "Activator" when methodCalledType.Namespace == "System":
						if (!methodCalled.IsStatic)
							break;

						switch (methodCalled.Name) {
							//
							// static T CreateInstance<T> ()
							//
							case "CreateInstance" when methodCalled.ContainsGenericParameter:
								// Not sure it's worth implementing as we cannot expant T and simple cases can be rewritten
								reflectionContext.AnalyzingPattern ();
								reflectionContext.RecordUnrecognizedPattern ($"Activator call '{methodCalled.FullName}' inside '{_methodCalling.FullName}' is not supported");
								break;

							//
							// static CreateInstance (string assemblyName, string typeName)
							// static CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes)
							// static CreateInstance (string assemblyName, string typeName, object?[]? activationAttributes)
							case "CreateInstance": {

									var parameters = methodCalled.Parameters;
									if (parameters.Count < 1)
										break;

									if (parameters [0].ParameterType.MetadataType == MetadataType.String) {
										reflectionContext.AnalyzingPattern ();
										ProcessActivatorCallWithStrings (ref reflectionContext, instructionIndex - 1, parameters.Count < 4);
										break;
									}
								}

								break;

							//
							// static CreateInstanceFrom (string assemblyFile, string typeName)
							// static CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
							// static CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
							//
							case "CreateInstanceFrom":
								ProcessActivatorCallWithStrings (ref reflectionContext, instructionIndex - 1, methodCalled.Parameters.Count < 4);
								break;
						}

						break;
				}

			}

			//
			// Handles static method calls in form of Create (string assemblyFile, string typeName, ......)
			//
			void ProcessActivatorCallWithStrings (ref ReflectionPatternContext reflectionContext, int startIndex, bool defaultCtorOnly)
			{
				reflectionContext.AnalyzingPattern ();

				var parameters = reflectionContext.MethodCalled.Parameters;
				if (parameters.Count < 2) {
					reflectionContext.RecordUnrecognizedPattern ($"Activator call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' is not supported");
					return;
				}

				if (parameters [0].ParameterType.MetadataType != MetadataType.String && parameters [1].ParameterType.MetadataType != MetadataType.String) {
					reflectionContext.RecordUnrecognizedPattern ($"Activator call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' is not supported");
					return;
				}

				var first_arg_instr = GetInstructionAtStackDepth (_instructions, startIndex, reflectionContext.MethodCalled.Parameters.Count);
				if (first_arg_instr < 0) {
					reflectionContext.RecordUnrecognizedPattern ($"Activator call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' couldn't be decomposed");
					return;
				}

				var first_arg = _instructions [first_arg_instr];
				if (first_arg.OpCode != OpCodes.Ldstr) {
					reflectionContext.RecordUnrecognizedPattern ($"Activator call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with the 1st argument which cannot be analyzed");
					return;
				}

				var second_arg_instr = GetInstructionAtStackDepth (_instructions, startIndex, reflectionContext.MethodCalled.Parameters.Count - 1);
				if (second_arg_instr < 0) {
					reflectionContext.RecordUnrecognizedPattern ($"Activator call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' couldn't be decomposed");
					return;
				}

				var second_arg = _instructions [second_arg_instr];
				if (second_arg.OpCode != OpCodes.Ldstr) {
					reflectionContext.RecordUnrecognizedPattern ($"Activator call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with the 2nd argument which cannot be analyzed");
					return;
				}

				string assembly_name = (string)first_arg.Operand;
				if (!_markStep._context.Resolver.AssemblyCache.TryGetValue (assembly_name, out var assembly)) {
					reflectionContext.RecordUnrecognizedPattern ($"Activator call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' references assembly '{assembly_name}' which could not be found");
					return;
				}

				string type_name = (string)second_arg.Operand;
				var declaringType = FindType (assembly, type_name);

				if (declaringType == null) {
					reflectionContext.RecordUnrecognizedPattern ($"Activator call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' references type '{type_name}' which could not be found");
					return;
				}

				MarkMethodsFromReflectionCall (ref reflectionContext, declaringType, ".ctor", 0, null, defaultCtorOnly ? 0 : (int?)null);
			}

			//
			// Handles instance methods called over typeof (Foo) with string name as the first argument
			//
			void ProcessSystemTypeGetMemberLikeCall (ref ReflectionPatternContext reflectionContext, System.Reflection.MemberTypes memberTypes, int startIndex, bool thisExtension = false)
			{
				reflectionContext.AnalyzingPattern ();

				int first_instance_arg = reflectionContext.MethodCalled.Parameters.Count;
				if (thisExtension)
					--first_instance_arg;

				var first_arg_instr = GetInstructionAtStackDepth (_instructions, startIndex, first_instance_arg);
				if (first_arg_instr < 0) {
					reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' couldn't be decomposed");
					return;
				}

				var first_arg = _instructions [first_arg_instr];
				BindingFlags? bindingFlags = default;
				string name = default;

				if (memberTypes == System.Reflection.MemberTypes.Constructor) {
					if (first_arg.OpCode == OpCodes.Ldc_I4_S && reflectionContext.MethodCalled.Parameters.Count > 0 && reflectionContext.MethodCalled.Parameters [0].ParameterType.IsTypeOf ("System.Reflection", "BindingFlags")) {
						bindingFlags = (BindingFlags)(sbyte)first_arg.Operand;
					}
				} else {
					//
					// The next value must be string constant (we don't handle anything else)
					//
					if (first_arg.OpCode != OpCodes.Ldstr) {
						reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' was detected with argument which cannot be analyzed");
						return;
					}

					name = (string)first_arg.Operand;

					var pos_arg = _instructions [first_arg_instr + 1];
					if (pos_arg.OpCode == OpCodes.Ldc_I4_S && reflectionContext.MethodCalled.Parameters.Count > 1 && reflectionContext.MethodCalled.Parameters [1].ParameterType.IsTypeOf ("System.Reflection", "BindingFlags")) {
						bindingFlags = (BindingFlags)(sbyte)pos_arg.Operand;
					}
				}

				var declaringType = FindReflectionTypeForLookup (_instructions, first_arg_instr - 1);
				if (declaringType == null) {
					reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' does not use detectable instance type extraction");
					return;
				}

				switch (memberTypes) {
					case System.Reflection.MemberTypes.Constructor:
						MarkMethodsFromReflectionCall (ref reflectionContext, declaringType, ".ctor", 0, bindingFlags);
						break;
					case System.Reflection.MemberTypes.Method:
						MarkMethodsFromReflectionCall (ref reflectionContext, declaringType, name, 0, bindingFlags);
						break;
					case System.Reflection.MemberTypes.Field:
						MarkFieldsFromReflectionCall (ref reflectionContext, declaringType, name);
						break;
					case System.Reflection.MemberTypes.Property:
						MarkPropertiesFromReflectionCall (ref reflectionContext, declaringType, name);
						break;
					case System.Reflection.MemberTypes.Event:
						MarkEventsFromReflectionCall (ref reflectionContext, declaringType, name);
						break;
					default:
						Debug.Fail ("Unsupported member type");
						reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{_methodCalling.FullName}' is of unexpected member type.");
						break;
				}
			}

			//
			// arity == null for name match regardless of arity
			//
			void MarkMethodsFromReflectionCall (ref ReflectionPatternContext reflectionContext, TypeDefinition declaringType, string name, int? arity, BindingFlags? bindingFlags, int? parametersCount = null)
			{
				bool foundMatch = false;
				foreach (var method in declaringType.Methods) {
					var mname = method.Name;

					// Either exact match or generic method with any arity when unspecified
					if (mname != name && !(arity == null && mname.StartsWith (name, StringComparison.Ordinal) && mname.Length > name.Length + 2 && mname [name.Length + 1] == '`')) {
						continue;
					}

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic)
						continue;

					if (parametersCount != null && parametersCount != method.Parameters.Count)
						continue;

					foundMatch = true;
					var methodCalling = reflectionContext.MethodCalling;
					reflectionContext.RecordRecognizedPattern (method, () => _markStep.MarkIndirectlyCalledMethod (method, new DependencyInfo (DependencyKind.AccessedViaReflection, methodCalling)));
				}

				if (!foundMatch)
					reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{reflectionContext.MethodCalling.FullName}' could not resolve method `{name}` on type `{declaringType.FullName}`.");
			}

			void MarkPropertiesFromReflectionCall (ref ReflectionPatternContext reflectionContext, TypeDefinition declaringType, string name, bool staticOnly = false)
			{
				bool foundMatch = false;
				var methodCalling = reflectionContext.MethodCalling;
				foreach (var property in declaringType.Properties) {
					if (property.Name != name)
						continue;

					bool markedAny = false;

					// It is not easy to reliably detect in the IL code whether the getter or setter (or both) are used.
					// Be conservative and mark everything for the property.
					var getter = property.GetMethod;
					if (getter != null && (!staticOnly || staticOnly && getter.IsStatic)) {
						reflectionContext.RecordRecognizedPattern (getter, () => _markStep.MarkIndirectlyCalledMethod (getter, new DependencyInfo (DependencyKind.AccessedViaReflection, methodCalling)));
						markedAny = true;
					}

					var setter = property.SetMethod;
					if (setter != null && (!staticOnly || staticOnly && setter.IsStatic)) {
						reflectionContext.RecordRecognizedPattern (setter, () => _markStep.MarkIndirectlyCalledMethod (setter, new DependencyInfo (DependencyKind.AccessedViaReflection, methodCalling)));
						markedAny = true;
					}

					if (markedAny) {
						foundMatch = true;
						reflectionContext.RecordRecognizedPattern (property, () => _markStep.MarkProperty (property, new DependencyInfo (DependencyKind.AccessedViaReflection, methodCalling)));
					}
				}

				if (!foundMatch)
					reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{reflectionContext.MethodCalling.FullName}' could not resolve property `{name}` on type `{declaringType.FullName}`.");
			}

			void MarkFieldsFromReflectionCall (ref ReflectionPatternContext reflectionContext, TypeDefinition declaringType, string name, bool staticOnly = false)
			{
				bool foundMatch = false;
				var methodCalling = reflectionContext.MethodCalling;
				foreach (var field in declaringType.Fields) {
					if (field.Name != name)
						continue;

					if (staticOnly && !field.IsStatic)
						continue;

					foundMatch = true;
					reflectionContext.RecordRecognizedPattern (field, () => _markStep.MarkField (field, new DependencyInfo (DependencyKind.AccessedViaReflection, methodCalling)));
					break;
				}

				if (!foundMatch)
					reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{reflectionContext.MethodCalling.FullName}' could not resolve field `{name}` on type `{declaringType.FullName}`.");
			}

			void MarkEventsFromReflectionCall (ref ReflectionPatternContext reflectionContext, TypeDefinition declaringType, string name)
			{
				bool foundMatch = false;
				var methodCalling = reflectionContext.MethodCalling;
				foreach (var eventInfo in declaringType.Events) {
					if (eventInfo.Name != name)
						continue;

					foundMatch = true;
					reflectionContext.RecordRecognizedPattern (eventInfo, () => _markStep.MarkEvent (eventInfo, new DependencyInfo (DependencyKind.AccessedViaReflection, methodCalling)));
				}

				if (!foundMatch)
					reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{reflectionContext.MethodCalling.FullName}' could not resolve event `{name}` on type `{declaringType.FullName}`.");
			}
		}

		static int GetInstructionAtStackDepth (Collection<Instruction> instructions, int startIndex, int stackSizeToBacktrace)
		{
			for (int i = startIndex; i >= 0; --i) {
				var instruction = instructions [i];

				switch (instruction.OpCode.StackBehaviourPop) {
				case StackBehaviour.Pop0:
					break;
				case StackBehaviour.Pop1:
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
					stackSizeToBacktrace++;
					break;
				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					stackSizeToBacktrace += 2;
					break;
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					stackSizeToBacktrace += 3;
					break;
				case StackBehaviour.Varpop:
					switch (instruction.OpCode.Code) {
					case Code.Call:
					case Code.Calli:
					case Code.Callvirt:
						if (instruction.Operand is MethodReference mr) {
							stackSizeToBacktrace += mr.Parameters.Count;
							if (mr.Resolve ()?.IsStatic == false)
								stackSizeToBacktrace++;
						}

						break;
					case Code.Newobj:
						if (instruction.Operand is MethodReference ctor) {
							stackSizeToBacktrace += ctor.Parameters.Count;
						}
						break;
					case Code.Ret:
						// TODO: Need method return type for correct stack size but this path should not be hit yet
						break;
					default:
						return -3;
					}
					break;
				}

				switch (instruction.OpCode.StackBehaviourPush) {
				case StackBehaviour.Push0:
					break;
				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
					stackSizeToBacktrace--;
					break;
				case StackBehaviour.Push1_push1:
					stackSizeToBacktrace -= 2;
					break;
				case StackBehaviour.Varpush:
					//
					// Only call, calli, callvirt will hit this
					//
					if (instruction.Operand is MethodReference mr && mr.ReturnType.MetadataType != MetadataType.Void) {
						stackSizeToBacktrace--;
					}
					break;
				}

				if (stackSizeToBacktrace == 0)
					return i;

				if (stackSizeToBacktrace < 0)
					return -1;
			}

			return -2;
		}

		static TypeDefinition FindReflectionTypeForLookup (Collection<Instruction> instructions, int startIndex)
		{
			while (startIndex >= 1) {
				int storeIndex = -1;
				var instruction = instructions [startIndex];
				switch (instruction.OpCode.Code) {
				//
				// Pattern #1
				//
				// typeof (Foo).ReflectionCall ()
				//
				case Code.Call:
					if (!(instruction.Operand is MethodReference mr) || mr.Name != "GetTypeFromHandle")
						return null;

					var ldtoken = instructions [startIndex - 1];

					if (ldtoken.OpCode != OpCodes.Ldtoken)
						return null;

					return (ldtoken.Operand as TypeReference).Resolve ();

				//
				// Patern #2
				//
				// var temp = typeof (Foo);
				// temp.ReflectionCall ()
				//
				case Code.Ldloc_0:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_0, startIndex - 1);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc_1:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_1, startIndex - 1);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc_2:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_2, startIndex - 1);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc_3:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_3, startIndex - 1);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc_S:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc_S, startIndex - 1, l => (VariableReference)l.Operand == (VariableReference)instruction.Operand);
					startIndex = storeIndex - 1;
					break;
				case Code.Ldloc:
					storeIndex = GetIndexOfInstruction (instructions, OpCodes.Stloc, startIndex - 1, l => (VariableReference)l.Operand == (VariableReference)instruction.Operand);
					startIndex = storeIndex - 1;
					break;

				case Code.Nop:
					startIndex--;
					break;

				default:
					return null;
				}
			}

			return null;
		}

		static int GetIndexOfInstruction (Collection<Instruction> instructions, OpCode opcode, int startIndex, Predicate<Instruction> comparer = null)
		{
			while (startIndex >= 0) {
				var instr = instructions [startIndex];
				if (instr.OpCode == opcode && (comparer == null || comparer (instr)))
					return startIndex;

				startIndex--;
			}

			return -1;
		}

		protected class AttributeProviderPair {
			public AttributeProviderPair (CustomAttribute attribute, ICustomAttributeProvider provider)
			{
				Attribute = attribute;
				Provider = provider;
			}

			public CustomAttribute Attribute { get; private set; }
			public ICustomAttributeProvider Provider { get; private set; }
		}

		private class ReflectionMethodBodyScanner : Dataflow.MethodBodyScanner
		{
			private readonly MarkStep _markStep;
			private readonly FlowAnnotations _flowAnnotations;

			public ReflectionMethodBodyScanner(MarkStep parent)
			{
				_markStep = parent;
				_flowAnnotations = _markStep._flowAnnotations;
			}

			public void ScanAndProcessReturnValue (MethodBody methodBody)
			{
				Scan (methodBody);

				if (MethodReturnValue != null) {
					var requiredMemberKinds = _flowAnnotations.GetReturnParameterAnnotation (methodBody.Method);
					if (requiredMemberKinds != 0) {
						var reflectionContext = new ReflectionPatternContext (_markStep._context, methodBody.Method, methodBody.Method, 0);
						reflectionContext.AnalyzingPattern ();
						RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberKinds, MethodReturnValue, methodBody.Method.MethodReturnType);
					}
				}
			}

			protected override void WarnAboutInvalidILInMethod (MethodBody method, int ilOffset)
			{
				// TODO: remove once we're ready to scan actual invalid IL
				// Serves as a debug helper for now to make sure valid IL is not considered invalid.
				throw new Exception ();
			}

			protected override ValueNode GetMethodParameterValue (MethodDefinition method, int parameterIndex)
			{
				DynamicallyAccessedMemberKinds memberKinds = _flowAnnotations.GetParameterAnnotation (method, parameterIndex);
				return new MethodParameterValue (parameterIndex, memberKinds) {
					SourceContext = method
				};
			}

			protected override ValueNode GetFieldValue (MethodDefinition method, FieldDefinition field)
			{
				DynamicallyAccessedMemberKinds memberKinds = _flowAnnotations.GetFieldAnnotation (field);
				return new LoadFieldValue (field, memberKinds) {
					SourceContext = method
				};
			}

			protected override void HandleStoreField (MethodDefinition method, FieldDefinition field, Instruction operation, ValueNode valueToStore)
			{
				var requiredMemberKinds = _flowAnnotations.GetFieldAnnotation (field);
				if (requiredMemberKinds != 0) {
					// TODO: There's no way to represent store to a field given current ReflectionPatternContext (and the underlying IReflectionPatterRecorder)
					var reflectionContext = new ReflectionPatternContext (_markStep._context, method, method, operation.Offset);
					reflectionContext.AnalyzingPattern ();
					RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberKinds, valueToStore, field);
				}
			}

			public override bool HandleCall (MethodBody callingMethodBody, MethodReference calledMethod, Instruction operation, ValueNodeList methodParams, out ValueNode methodReturnValue)
			{
				var reflectionContext = new ReflectionPatternContext (_markStep._context, callingMethodBody.Method, calledMethod.Resolve (), operation.Offset);

				DynamicallyAccessedMemberKinds returnValueDynamicallyAccessedMemberKinds = 0;

				try {

					methodReturnValue = null;

					var calledMethodDefinition = calledMethod.Resolve ();
					if (calledMethodDefinition == null)
						return false;

					bool requiresDataFlowAnalysis = _flowAnnotations.RequiresDataFlowAnalysis (calledMethodDefinition);
					returnValueDynamicallyAccessedMemberKinds =  requiresDataFlowAnalysis ?
						_flowAnnotations.GetReturnParameterAnnotation (calledMethodDefinition) : 0;

					switch (calledMethod.Name) {
						case "GetTypeInfo" when calledMethod.DeclaringType.Name == "IntrospectionExtensions": {
								// typeof(Foo).GetTypeInfo()... will be commonly present in code targeting
								// the dead-end reflection refactoring. The call doesn't do anything and we
								// don't want to lose the annotation.
								methodReturnValue = methodParams [0];
							}
							break;

						case "GetTypeFromHandle" when calledMethod.DeclaringType.Name == "Type": {
								// Infrastructure piece to support "typeof(Foo)"
								if (methodParams[0] is RuntimeTypeHandleValue typeHandle)
									methodReturnValue = new SystemTypeValue (typeHandle.TypeRepresented);
							}
							break;

						case "MakeGenericType" when calledMethod.DeclaringType.Name == "Type": {
								// Don't care about the actual arguments, but we don't want to lose track of the type
								// in case this is e.g. Activator.CreateInstance(typeof(Foo<>).MakeGenericType(...));
								methodReturnValue = methodParams [0];
							}
							break;

						//
						// static CreateInstance (System.Type type)
						// static CreateInstance (System.Type type, bool nonPublic)
						// static CreateInstance (System.Type type, params object?[]? args)
						// static CreateInstance (System.Type type, object?[]? args, object?[]? activationAttributes)
						// static CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture)
						// static CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes) { throw null; }
						//
						case "CreateInstance" when !calledMethod.ContainsGenericParameter
							&& calledMethod.DeclaringType.Name == "Activator"
							&& calledMethod.Parameters.Count >= 1
							&& calledMethod.DeclaringType.Namespace == "System"
							&& calledMethod.Parameters [0].ParameterType.MetadataType != MetadataType.String: {

								var parameters = calledMethod.Parameters;

								reflectionContext.AnalyzingPattern ();

								int? ctorParameterCount = null;
								BindingFlags bindingFlags = BindingFlags.Instance;
								if (parameters.Count > 1) {									
									if (parameters [1].ParameterType.MetadataType == MetadataType.Boolean) {
										// The overload that takes a "nonPublic" bool
										bool nonPublic = true;
										if (methodParams [1] is ConstIntValue constInt) {
											nonPublic = constInt.Value != 0;
										}

										if (nonPublic)
											bindingFlags |= BindingFlags.NonPublic | BindingFlags.Public;
										else
											bindingFlags |= BindingFlags.Public;
										ctorParameterCount = 0;
									} else {
										// Overload that has the parameters as the second or fourth argument
										int argsParam = parameters.Count == 2 || parameters.Count == 3 ? 1 : 3;
										
										if (methodParams.Count > argsParam &&
											methodParams [argsParam] is ArrayValue arrayValue &&
											arrayValue.Size.AsConstInt () != null) {
											ctorParameterCount = arrayValue.Size.AsConstInt ();
										}

										if (parameters.Count > 3) {
											if (methodParams [1].AsConstInt () != null)
												bindingFlags |= (BindingFlags)methodParams [1].AsConstInt ();
											else
												bindingFlags |= BindingFlags.NonPublic | BindingFlags.Public;
										} else {
											bindingFlags |= BindingFlags.Public;
										}
									}
								}
								else {
									// The overload with a single System.Type argument
									ctorParameterCount = 0;
									bindingFlags |= BindingFlags.Public;
								}

								// Go over all types we've seen
								foreach (var value in methodParams [0].UniqueValues ()) {
									if (value is SystemTypeValue systemTypeValue) {
										// Special case known type values as we can do better by applying exact binding flags and parameter count.
										MarkMethodsFromReflectionCall (ref reflectionContext, systemTypeValue.TypeRepresented, ".ctor", bindingFlags, ctorParameterCount);
									} else {
										// Otherwise fall back to the bitfield requirements
										var requiredMemberKinds = ctorParameterCount == 0
											? DynamicallyAccessedMemberKinds.DefaultConstructor
											: ((bindingFlags & BindingFlags.NonPublic) == 0)
												? DynamicallyAccessedMemberKinds.PublicConstructors
												: DynamicallyAccessedMemberKinds.Constructors;
										RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberKinds, value, calledMethod.Parameters [0]);
									}
								}
							}
							break;
						//
						// GetConstructor (Type[])
						// GetConstructor (BindingFlags, Binder, Type[], ParameterModifier [])
						// GetConstructor (BindingFlags, Binder, CallingConventions, Type[], ParameterModifier [])
						//
						case "GetConstructor" when calledMethod.DeclaringType.Name == "Type"
						  && calledMethod.Parameters.Count >= 1
						  && calledMethod.DeclaringType.Namespace == "System": {

								reflectionContext.AnalyzingPattern ();
								var parameters = calledMethod.Parameters;
								int? ctorParameterCount = null;
								BindingFlags bindingFlags = BindingFlags.Default;
								if (parameters.Count > 1) {
									if (methodParams [1].AsConstInt () != null)
										bindingFlags |= (BindingFlags)methodParams [1].AsConstInt ();
								}
								// Go over all types we've seen
								foreach (var value in methodParams [0].UniqueValues ()) {
									if (value is SystemTypeValue systemTypeValue) {
										MarkMethodsFromReflectionCall (ref reflectionContext, systemTypeValue.TypeRepresented, ".ctor", bindingFlags, ctorParameterCount);
									} else if (value == NullValue.Instance) {
										// Nothing to report. This is likely just a value on some unreachable branch.
										reflectionContext.RecordHandledPattern ();
									} else if (value is MethodParameterValue methodParameterValue) {
										// This is the case where the value comes from a method parameter.
										// TODO: If the parameter is annotated, we're good. If it's not annotated, we shold warn.
										reflectionContext.RecordUnrecognizedPattern ($"Activator call '{calledMethod.FullName}' inside '{callingMethodBody.Method.FullName}' was detected with 1st argument expression which cannot be analyzed");
									} else {
										// Not known where the value is coming from
										reflectionContext.RecordUnrecognizedPattern ($"Activator call '{calledMethod.FullName}' inside '{callingMethodBody.Method.FullName}' was detected with 1st argument expression which cannot be analyzed");
									}
								}
							}
							break;
						//
						// GetMethod (string)
						// GetMethod (string, BindingFlags)
						// GetMethod (string, Type[])
						// GetMethod (string, Type[], ParameterModifier[])
						// GetMethod (string, BindingFlags, Binder, Type[], ParameterModifier[]) 6
						// GetMethod (string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[]) 7
						// GetMethod (string, int, Type[])
						// GetMethod (string, int, Type[], ParameterModifier[]?)
						// GetMethod (string, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)
						// GetMethod (string, int, BindingFlags, Binder?, CallingConventions, Type[], ParameterModifier[]?)
						//
						case "GetMethod" when calledMethod.DeclaringType.Name == "Type"
						  && calledMethod.Parameters.Count >= 1
						  && calledMethod.DeclaringType.Namespace == "System": {
								reflectionContext.AnalyzingPattern ();
								BindingFlags bindingFlags = BindingFlags.Default;
								if (calledMethod.Parameters.Count > 1 && calledMethod.Parameters [1].ParameterType.Name == "BindingFlags") {
									bindingFlags |= (BindingFlags)methodParams [2].AsConstInt ();
								}
								else if(calledMethod.Parameters.Count > 2 && calledMethod.Parameters[2].ParameterType.Name == "BindingFlags") {
									bindingFlags |= (BindingFlags)methodParams [3].AsConstInt ();
								}

								foreach (var value in methodParams [0].UniqueValues ()) {
									if (value is SystemTypeValue systemTypeValue) {
										foreach (var stringParam in methodParams [1].UniqueValues ()) {
											if (stringParam is KnownStringValue stringValue) {
												MarkMethodsFromReflectionCall (ref reflectionContext, systemTypeValue.TypeRepresented, stringValue.Contents, bindingFlags);
											} else if (stringParam is NullValue) {
												reflectionContext.RecordHandledPattern ();
											} else if (stringParam is MethodParameterValue) {
												// TODO: Check if parameter is annotated.
												reflectionContext.RecordUnrecognizedPattern ($"Expression call '{calledMethod.FullName}' inside '{callingMethodBody.Method.FullName}' was detected with 2nd argument which cannot be analyzed");
											} else {
												reflectionContext.RecordUnrecognizedPattern ($"Expression call '{calledMethod.FullName}' inside '{callingMethodBody.Method.FullName}' was detected with 2nd argument which cannot be analyzed");
											}
										}
									} else if (value == NullValue.Instance) {
										reflectionContext.RecordHandledPattern ();
									} else if (value is MethodParameterValue) {
										// TODO: Check if parameter is annotated.
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{calledMethod.FullName}' inside '{callingMethodBody.Method.FullName}' was detected with 1st argument which cannot be analyzed");
									} else {
										reflectionContext.RecordUnrecognizedPattern ($"Expression call '{calledMethod.FullName}' inside '{callingMethodBody.Method.FullName}' was detected with 1st argument which cannot be analyzed");
									}
								}
							}
							break;
						default:
							if (requiresDataFlowAnalysis) {
								reflectionContext.AnalyzingPattern ();
								for (int parameterIndex = 0; parameterIndex < methodParams.Count; parameterIndex ++) {
									var requiredMemberKinds = _flowAnnotations.GetParameterAnnotation (calledMethodDefinition, parameterIndex);
									if (requiredMemberKinds != 0) {
										var targetParameter = calledMethodDefinition.Parameters [parameterIndex - (calledMethodDefinition.HasImplicitThis () ? 1 : 0)];
										RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberKinds, methodParams [parameterIndex], targetParameter);
									}
								}

								reflectionContext.RecordHandledPattern ();
							}

							// To get good reporting of errors we need to track the origin of the value for all method calls
							// but except Newobj as those are special.
							if (calledMethodDefinition.ReturnType.MetadataType != MetadataType.Void) {
								methodReturnValue = new MethodReturnValue (returnValueDynamicallyAccessedMemberKinds) {
									SourceContext = calledMethodDefinition
								};

								return true;
							}

							return false;
					}
				}
				finally {
					reflectionContext.Dispose ();
				}

				// If we get here, we handled this as an intrinsic.  As a convenience, if the code above
				// didn't set the return value (and the method has a return value), we will set it to be an
				// unknown value with the return type of the method.
				if (methodReturnValue == null) {
					if (calledMethod.ReturnType.MetadataType != MetadataType.Void) {
						methodReturnValue = new MethodReturnValue(returnValueDynamicallyAccessedMemberKinds);
					}
				}

				// Validate that the return value has the correct annotations as per the method return value annotations
				if (returnValueDynamicallyAccessedMemberKinds != 0 && methodReturnValue != null) {
					if (methodReturnValue is LeafValueWithDynamicallyAccessedMemberNode methodReturnValueWithMemberKinds) {
						if (!methodReturnValueWithMemberKinds.DynamicallyAccessedMemberKinds.HasFlag (returnValueDynamicallyAccessedMemberKinds))
							throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodBody.Method} to {calledMethod} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					}
					else if (methodReturnValue is SystemTypeValue) {
						// SystemTypeValue can fullfill any requirement, so it's always valid
					}
					else {
						throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodBody.Method} to {calledMethod} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					}
				}

				return true;
			}

			void RequireDynamicallyAccessedMembers (ref ReflectionPatternContext reflectionContext, DynamicallyAccessedMemberKinds requiredMemberKinds, ValueNode value, IMetadataTokenProvider targetContext)
			{
				foreach (var uniqueValue in value.UniqueValues ()) {
					if (uniqueValue is LeafValueWithDynamicallyAccessedMemberNode valueWithDynamicallyAccessedMember) {
						if (!valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberKinds.HasFlag (requiredMemberKinds)) {
							reflectionContext.RecordUnrecognizedPattern ($"The {GetValueDescriptionForErrorMessage (valueWithDynamicallyAccessedMember)} " +
								$"with dynamically accessed member kinds '{GetDynamicallyAccessedMemberKindsDescription (valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberKinds)}' " +
								$"is passed into the {GetMetadataTokenDescriptionForErrorMessage (targetContext)} " +
								$"which requires dynamically accessed member kinds `{GetDynamicallyAccessedMemberKindsDescription (requiredMemberKinds)}`. " +
								$"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds '{GetDynamicallyAccessedMemberKindsDescription (requiredMemberKinds)}'.");
						} else {
							reflectionContext.RecordHandledPattern ();
						}
					} else if (uniqueValue is SystemTypeValue systemTypeValue) {
						// Note that it's important to first test for the widest selector (Constructors > PublicConstructors > DefaultConstructor)
						// as the wider ones include the narrower ones in the bitfield values.
						if (requiredMemberKinds.HasFlag(DynamicallyAccessedMemberKinds.Constructors)) {
							MarkMethodsFromReflectionCall (ref reflectionContext, systemTypeValue.TypeRepresented, ".ctor", bindingFlags: null);
						} else if (requiredMemberKinds.HasFlag(DynamicallyAccessedMemberKinds.PublicConstructors)) {
							MarkMethodsFromReflectionCall (ref reflectionContext, systemTypeValue.TypeRepresented, ".ctor", BindingFlags.Public);
						} else if (requiredMemberKinds.HasFlag(DynamicallyAccessedMemberKinds.DefaultConstructor)) {
							MarkMethodsFromReflectionCall (ref reflectionContext, systemTypeValue.TypeRepresented, ".ctor", bindingFlags: null, parametersCount: 0);
						} else {
							throw new NotImplementedException ();
						}
					} else if (uniqueValue == NullValue.Instance) {
						// Ignore - probably unreachable path as it would fail at runtime anyway.
					} else {
						reflectionContext.RecordUnrecognizedPattern ($"A {GetValueDescriptionForErrorMessage (uniqueValue)} " +
							$"is passed into the {GetMetadataTokenDescriptionForErrorMessage (targetContext)} " +
							$"which requires dynamically accessed member kinds `{GetDynamicallyAccessedMemberKindsDescription (requiredMemberKinds)}`. " +
							$"It's not possible to guarantee that these requirements are met by the application.");
					}
				}

				reflectionContext.RecordHandledPattern ();
			}

			void MarkMethodsFromReflectionCall (ref ReflectionPatternContext reflectionContext, TypeDefinition declaringType, string name, BindingFlags? bindingFlags, int? parametersCount = null)
			{
				bool foundMatch = false;
				foreach (var method in declaringType.Methods) {
					var mname = method.Name;

					if (mname != name) {
						continue;
					}

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic)
						continue;

					if (parametersCount != null && parametersCount != method.Parameters.Count)
						continue;

					foundMatch = true;
					var methodCalling = reflectionContext.MethodCalling;
					reflectionContext.RecordRecognizedPattern (method, () => _markStep.MarkIndirectlyCalledMethod (method, new DependencyInfo (DependencyKind.AccessedViaReflection, methodCalling)));
				}

				if (!foundMatch) {
					bool publicOnly = (bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public;
					reflectionContext.RecordUnrecognizedPattern ($"Reflection call '{reflectionContext.MethodCalled.FullName}' inside '{reflectionContext.MethodCalling.FullName}' could not resolve {(publicOnly ? "public" : "")} method `{name}` on type `{declaringType.FullName}`.");
				}
			}

			string GetValueDescriptionForErrorMessage (ValueNode value)
			{
				switch (value) {
					case MethodParameterValue methodParameterValue: {
							if (methodParameterValue.SourceContext is MethodDefinition method) {
								int declaredParameterIndex = methodParameterValue.ParameterIndex - (method.HasImplicitThis () ? 1 : 0);
								if (declaredParameterIndex >= 0 && declaredParameterIndex < method.Parameters.Count)
									return GetMetadataTokenDescriptionForErrorMessage (method.Parameters [declaredParameterIndex]);
							}

							return $"method prameter #{methodParameterValue.ParameterIndex}";
						}

					case MethodReturnValue methodReturnValue: {
							if (methodReturnValue.SourceContext is MethodDefinition method) {
								return GetMetadataTokenDescriptionForErrorMessage (method.MethodReturnType);
							}

							return "method return value";
						}

					case LoadFieldValue loadFieldValue:
						return GetMetadataTokenDescriptionForErrorMessage (loadFieldValue.Field);

					default:
						return $"value from unknown source";
				}
			}

			string GetMetadataTokenDescriptionForErrorMessage (IMetadataTokenProvider targetContext)
			{
				switch (targetContext) {
					case ParameterDefinition parameterDefinition: return $"parameter '{parameterDefinition.Name}' of method '{parameterDefinition.Method}'";
					case MethodReturnType methodReturnType: return $"return value of method '{methodReturnType.Method}'";
					case FieldDefinition fieldDefinition: return $"field '{fieldDefinition}'";
					default: return targetContext.ToString ();
				};
			}

			string GetDynamicallyAccessedMemberKindsDescription (DynamicallyAccessedMemberKinds memberKinds)
			{
				var results = new List<DynamicallyAccessedMemberKinds> ();
				if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.Constructors))
					results.Add (DynamicallyAccessedMemberKinds.Constructors);
				else if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.PublicConstructors))
					results.Add (DynamicallyAccessedMemberKinds.PublicConstructors);
				else if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.DefaultConstructor))
					results.Add (DynamicallyAccessedMemberKinds.DefaultConstructor);
				
				if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.Methods))
					results.Add (DynamicallyAccessedMemberKinds.Methods);
				else if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.PublicMethods))
					results.Add (DynamicallyAccessedMemberKinds.PublicMethods);
				
				if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.Properties))
					results.Add (DynamicallyAccessedMemberKinds.Properties);
				else if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.PublicProperties))
					results.Add (DynamicallyAccessedMemberKinds.PublicProperties);
				
				if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.Fields))
					results.Add (DynamicallyAccessedMemberKinds.Fields);
				else if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.PublicFields))
					results.Add (DynamicallyAccessedMemberKinds.PublicFields);
				
				if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.Events))
					results.Add (DynamicallyAccessedMemberKinds.Events);
				else if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.PublicEvents))
					results.Add (DynamicallyAccessedMemberKinds.PublicEvents);
				
				if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.NestedTypes))
					results.Add (DynamicallyAccessedMemberKinds.NestedTypes);
				else if (memberKinds.HasFlag (DynamicallyAccessedMemberKinds.PublicNestedTypes))
					results.Add (DynamicallyAccessedMemberKinds.PublicNestedTypes);

				if (results.Count == 0)
					return "None";

				return string.Join (" | ", results.Select (r => r.ToString ()));
			}
		}
	}

	// Make our own copy of the BindingFlags enum, so that we don't depend on System.Reflection.
	[Flags]
	enum BindingFlags
	{
		Default = 0,
		IgnoreCase = 1,
		DeclaredOnly = 2,
		Instance = 4,
		Static = 8,
		Public = 16,
		NonPublic = 32,
		FlattenHierarchy = 64,
		InvokeMethod = 256,
		CreateInstance = 512,
		GetField = 1024,
		SetField = 2048,
		GetProperty = 4096,
		SetProperty = 8192,
		PutDispProperty = 16384,
		PutRefDispProperty = 32768,
		ExactBinding = 65536,
		SuppressChangeType = 131072,
		OptionalParamBinding = 262144,
		IgnoreReturn = 16777216
	}
}
