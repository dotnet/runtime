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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Runtime.TypeParsing;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Mono.Linker.Dataflow;

namespace Mono.Linker.Steps
{

	public partial class MarkStep : IStep
	{
		protected LinkContext _context;
		protected Queue<(MethodDefinition, DependencyInfo, MarkScopeStack.Scope)> _methods;
		protected List<(MethodDefinition, MarkScopeStack.Scope)> _virtual_methods;
		protected Queue<AttributeProviderPair> _assemblyLevelAttributes;
		readonly List<AttributeProviderPair> _ivt_attributes;
		protected Queue<(AttributeProviderPair, DependencyInfo, MarkScopeStack.Scope)> _lateMarkedAttributes;
		protected List<(TypeDefinition, MarkScopeStack.Scope)> _typesWithInterfaces;
		protected HashSet<AssemblyDefinition> _dynamicInterfaceCastableImplementationTypesDiscovered;
		protected List<TypeDefinition> _dynamicInterfaceCastableImplementationTypes;
		protected List<(MethodBody, MarkScopeStack.Scope)> _unreachableBodies;

		readonly List<(TypeDefinition Type, MethodBody Body, Instruction Instr)> _pending_isinst_instr;
		UnreachableBlocksOptimizer _unreachableBlocksOptimizer;
		MarkStepContext _markContext;
		readonly Dictionary<TypeDefinition, bool> _entireTypesMarked; // The value is markBaseAndInterfaceTypes flag used to mark the type
		DynamicallyAccessedMembersTypeHierarchy _dynamicallyAccessedMembersTypeHierarchy;
		MarkScopeStack _scopeStack;

		internal DynamicallyAccessedMembersTypeHierarchy DynamicallyAccessedMembersTypeHierarchy {
			get => _dynamicallyAccessedMembersTypeHierarchy;
		}

#if DEBUG
		static readonly DependencyKind[] _entireTypeReasons = new DependencyKind[] {
			DependencyKind.AccessedViaReflection,
			DependencyKind.BaseType,
			DependencyKind.DynamicallyAccessedMember,
			DependencyKind.DynamicDependency,
			DependencyKind.NestedType,
			DependencyKind.TypeInAssembly,
			DependencyKind.Unspecified,
		};

		static readonly DependencyKind[] _fieldReasons = new DependencyKind[] {
			DependencyKind.Unspecified,
			DependencyKind.AccessedViaReflection,
			DependencyKind.AlreadyMarked,
			DependencyKind.Custom,
			DependencyKind.CustomAttributeField,
			DependencyKind.DynamicallyAccessedMember,
			DependencyKind.EventSourceProviderField,
			DependencyKind.FieldAccess,
			DependencyKind.FieldOnGenericInstance,
			DependencyKind.InteropMethodDependency,
			DependencyKind.Ldtoken,
			DependencyKind.MemberOfType,
			DependencyKind.DynamicDependency,
			DependencyKind.ReferencedBySpecialAttribute,
			DependencyKind.TypePreserve,
			DependencyKind.XmlDescriptor,
		};

		static readonly DependencyKind[] _typeReasons = new DependencyKind[] {
			DependencyKind.Unspecified,
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
			DependencyKind.DynamicallyAccessedMember,
			DependencyKind.DynamicDependency,
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
			DependencyKind.TypeInAssembly,
			DependencyKind.UnreachableBodyRequirement,
			DependencyKind.VariableType,
			DependencyKind.ParameterMarshalSpec,
			DependencyKind.FieldMarshalSpec,
			DependencyKind.ReturnTypeMarshalSpec,
			DependencyKind.DynamicInterfaceCastableImplementation,
			DependencyKind.XmlDescriptor,
		};

		static readonly DependencyKind[] _methodReasons = new DependencyKind[] {
			DependencyKind.Unspecified,
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
			DependencyKind.DynamicallyAccessedMember,
			DependencyKind.DynamicDependency,
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
			DependencyKind.DynamicDependency,
			DependencyKind.PreservedMethod,
			DependencyKind.ReferencedBySpecialAttribute,
			DependencyKind.SerializationMethodForType,
			DependencyKind.TriggersCctorForCalledMethod,
			DependencyKind.TriggersCctorThroughFieldAccess,
			DependencyKind.TypePreserve,
			DependencyKind.UnreachableBodyRequirement,
			DependencyKind.VirtualCall,
			DependencyKind.VirtualNeededDueToPreservedScope,
			DependencyKind.ParameterMarshalSpec,
			DependencyKind.FieldMarshalSpec,
			DependencyKind.ReturnTypeMarshalSpec,
			DependencyKind.XmlDescriptor,
		};
#endif

		public MarkStep ()
		{
			_methods = new Queue<(MethodDefinition, DependencyInfo, MarkScopeStack.Scope)> ();
			_virtual_methods = new List<(MethodDefinition, MarkScopeStack.Scope)> ();
			_assemblyLevelAttributes = new Queue<AttributeProviderPair> ();
			_ivt_attributes = new List<AttributeProviderPair> ();
			_lateMarkedAttributes = new Queue<(AttributeProviderPair, DependencyInfo, MarkScopeStack.Scope)> ();
			_typesWithInterfaces = new List<(TypeDefinition, MarkScopeStack.Scope)> ();
			_dynamicInterfaceCastableImplementationTypesDiscovered = new HashSet<AssemblyDefinition> ();
			_dynamicInterfaceCastableImplementationTypes = new List<TypeDefinition> ();
			_unreachableBodies = new List<(MethodBody, MarkScopeStack.Scope)> ();
			_pending_isinst_instr = new List<(TypeDefinition, MethodBody, Instruction)> ();
			_entireTypesMarked = new Dictionary<TypeDefinition, bool> ();
		}

		public AnnotationStore Annotations => _context.Annotations;
		public MarkingHelpers MarkingHelpers => _context.MarkingHelpers;
		public Tracer Tracer => _context.Tracer;

		public virtual void Process (LinkContext context)
		{
			_context = context;
			_unreachableBlocksOptimizer = new UnreachableBlocksOptimizer (_context);
			_markContext = new MarkStepContext ();
			_scopeStack = new MarkScopeStack (_context);
			_dynamicallyAccessedMembersTypeHierarchy = new DynamicallyAccessedMembersTypeHierarchy (_context, this, _scopeStack);

			Initialize ();
			Process ();
			Complete ();
		}

		void Initialize ()
		{
			InitializeCorelibAttributeXml ();
			_context.Pipeline.InitializeMarkHandlers (_context, _markContext);

			ProcessMarkedPending ();
		}

		void InitializeCorelibAttributeXml ()
		{
			// Pre-load corelib and process its attribute XML first. This is necessary because the
			// corelib attribute XML can contain modifications to other assemblies.
			// We could just mark it here, but the attribute processing isn't necessarily tied to marking,
			// so this would rely on implementation details of corelib.
			var coreLib = _context.TryResolve (PlatformAssemblies.CoreLib);
			if (coreLib == null)
				return;

			var xmlInfo = EmbeddedXmlInfo.ProcessAttributes (coreLib, _context);
			if (xmlInfo == null)
				return;

			// Because the attribute XML can reference other assemblies, they must go in the global store,
			// instead of the per-assembly stores.
			foreach (var (provider, annotations) in xmlInfo.CustomAttributes)
				_context.CustomAttributes.PrimaryAttributeInfo.AddCustomAttributes (provider, annotations);
		}

		void Complete ()
		{
			foreach ((var body, var _) in _unreachableBodies) {
				Annotations.SetAction (body.Method, MethodAction.ConvertToThrow);
			}
		}

		bool ProcessInternalsVisibleAttributes ()
		{
			using var nullScope = _scopeStack.PushScope (new MessageOrigin (null));

			bool marked_any = false;
			foreach (var attr in _ivt_attributes) {
				if (!Annotations.IsMarked (attr.Attribute) && IsInternalsVisibleAttributeAssemblyMarked (attr.Attribute)) {
					MarkCustomAttribute (attr.Attribute, new DependencyInfo (DependencyKind.AssemblyOrModuleAttribute, attr.Provider));
					marked_any = true;
				}
			}

			return marked_any;

			bool IsInternalsVisibleAttributeAssemblyMarked (CustomAttribute ca)
			{
				System.Reflection.AssemblyName an;
				try {
					an = new System.Reflection.AssemblyName ((string) ca.ConstructorArguments[0].Value);
				} catch {
					return false;
				}

				var assembly = _context.GetLoadedAssembly (an.Name);
				if (assembly == null)
					return false;

				return Annotations.IsMarked (assembly.MainModule);
			}
		}

		static bool TypeIsDynamicInterfaceCastableImplementation (TypeDefinition type)
		{
			if (!type.IsInterface || !type.HasInterfaces || !type.HasCustomAttributes)
				return false;

			foreach (var ca in type.CustomAttributes) {
				if (ca.AttributeType.IsTypeOf ("System.Runtime.InteropServices", "DynamicInterfaceCastableImplementationAttribute"))
					return true;
			}
			return false;
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

		internal void MarkEntireType (TypeDefinition type, bool includeBaseAndInterfaceTypes, in DependencyInfo reason)
		{
			MarkEntireTypeInternal (type, includeBaseAndInterfaceTypes, reason);
		}

		void MarkEntireTypeInternal (TypeDefinition type, bool includeBaseAndInterfaceTypes, in DependencyInfo reason)
		{
#if DEBUG
			if (!_entireTypeReasons.Contains (reason.Kind))
				throw new InternalErrorException ($"Unsupported type dependency '{reason.Kind}'.");
#endif

			if (_entireTypesMarked.TryGetValue (type, out bool alreadyIncludedBaseAndInterfaceTypes) &&
				(!includeBaseAndInterfaceTypes || alreadyIncludedBaseAndInterfaceTypes))
				return;

			_entireTypesMarked[type] = includeBaseAndInterfaceTypes;

			if (type.HasNestedTypes) {
				foreach (TypeDefinition nested in type.NestedTypes)
					MarkEntireTypeInternal (nested, includeBaseAndInterfaceTypes, new DependencyInfo (DependencyKind.NestedType, type));
			}

			Annotations.Mark (type, reason);
			var baseTypeDefinition = _context.Resolve (type.BaseType);
			if (includeBaseAndInterfaceTypes && baseTypeDefinition != null) {
				MarkEntireTypeInternal (baseTypeDefinition, true, new DependencyInfo (DependencyKind.BaseType, type));
			}
			MarkCustomAttributes (type, new DependencyInfo (DependencyKind.CustomAttribute, type));
			MarkTypeSpecialCustomAttributes (type);

			if (type.HasInterfaces) {
				foreach (InterfaceImplementation iface in type.Interfaces) {
					var interfaceTypeDefinition = _context.Resolve (iface.InterfaceType);
					if (includeBaseAndInterfaceTypes && interfaceTypeDefinition != null)
						MarkEntireTypeInternal (interfaceTypeDefinition, true, new DependencyInfo (reason.Kind, type));

					MarkInterfaceImplementation (iface, new MessageOrigin (type));
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
					Annotations.SetAction (method, MethodAction.ForceParse);
					DependencyKind dependencyKind = (reason.Kind == DependencyKind.DynamicallyAccessedMember || reason.Kind == DependencyKind.DynamicDependency) ? reason.Kind : DependencyKind.MemberOfType;
					MarkMethod (method, new DependencyInfo (dependencyKind, type));
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
			while (ProcessPrimaryQueue () ||
				ProcessMarkedPending () ||
				ProcessLazyAttributes () ||
				ProcessLateMarkedAttributes () ||
				MarkFullyPreservedAssemblies () ||
				ProcessInternalsVisibleAttributes ()) ;

			ProcessPendingTypeChecks ();
		}

		static bool IsFullyPreservedAction (AssemblyAction action) => action == AssemblyAction.Copy || action == AssemblyAction.Save;

		bool MarkFullyPreservedAssemblies ()
		{
			// Fully mark any assemblies with copy/save action.

			// Unresolved references could get the copy/save action if this is the default action.
			bool scanReferences = IsFullyPreservedAction (_context.TrimAction) || IsFullyPreservedAction (_context.DefaultAction);

			if (!scanReferences) {
				// Unresolved references could get the copy/save action if it was set explicitly
				// for some referenced assembly that has not been resolved yet
				foreach (var (assemblyName, action) in _context.Actions) {
					if (!IsFullyPreservedAction (action))
						continue;

					var assembly = _context.GetLoadedAssembly (assemblyName);
					if (assembly == null) {
						scanReferences = true;
						break;
					}

					// The action should not change from the explicit command-line action
					Debug.Assert (_context.Annotations.GetAction (assembly) == action);
				}
			}

			// Beware: this works on loaded assemblies, not marked assemblies, so it should not be tied to marking.
			// We could further optimize this to only iterate through assemblies if the last mark iteration loaded
			// a new assembly, since this is the only way that the set we need to consider could have changed.
			var assembliesToCheck = scanReferences ? _context.GetReferencedAssemblies ().ToArray () : _context.GetAssemblies ();
			bool markedNewAssembly = false;
			foreach (var assembly in assembliesToCheck) {
				var action = _context.Annotations.GetAction (assembly);
				if (!IsFullyPreservedAction (action))
					continue;
				if (!Annotations.IsProcessed (assembly))
					markedNewAssembly = true;
				MarkAssembly (assembly, new DependencyInfo (DependencyKind.AssemblyAction, null));
			}
			return markedNewAssembly;
		}

		bool ProcessPrimaryQueue ()
		{
			if (QueueIsEmpty ())
				return false;

			while (!QueueIsEmpty ()) {
				ProcessQueue ();
				ProcessVirtualMethods ();
				ProcessMarkedTypesWithInterfaces ();
				ProcessDynamicCastableImplementationInterfaces ();
				ProcessPendingBodies ();
				DoAdditionalProcessing ();
			}

			return true;
		}

		bool ProcessMarkedPending ()
		{
			using var emptyScope = _scopeStack.PushScope (new MessageOrigin (null));

			bool marked = false;
			foreach (var pending in Annotations.GetMarkedPending ()) {
				marked = true;

				// Some pending items might be processed by the time we get to them.
				if (Annotations.IsProcessed (pending))
					continue;

				switch (pending) {
				case TypeDefinition type:
					MarkType (type, DependencyInfo.AlreadyMarked);
					break;
				case MethodDefinition method:
					MarkMethod (method, DependencyInfo.AlreadyMarked);
					// Methods will not actually be processed until we drain the method queue.
					break;
				case FieldDefinition field:
					MarkField (field, DependencyInfo.AlreadyMarked);
					break;
				case ModuleDefinition module:
					MarkModule (module, DependencyInfo.AlreadyMarked);
					break;
				case ExportedType exportedType:
					Annotations.SetProcessed (exportedType);
					// No additional processing is done for exported types.
					break;
				default:
					throw new NotImplementedException (pending.GetType ().ToString ());
				}
			}

			foreach (var type in Annotations.GetPendingPreserve ()) {
				marked = true;
				Debug.Assert (Annotations.IsProcessed (type));
				ApplyPreserveInfo (type);
			}

			return marked;
		}

		void ProcessPendingTypeChecks ()
		{
			for (int i = 0; i < _pending_isinst_instr.Count; ++i) {
				var item = _pending_isinst_instr[i];
				TypeDefinition type = item.Type;
				if (Annotations.IsInstantiated (type))
					continue;

				Instruction instr = item.Instr;
				ILProcessor ilProcessor = item.Body.GetILProcessor ();

				ilProcessor.InsertAfter (instr, Instruction.Create (OpCodes.Ldnull));
				Instruction new_instr = Instruction.Create (OpCodes.Pop);
				ilProcessor.Replace (instr, new_instr);
				UpdateBranchTarget (item.Body, instr, new_instr);

				_context.LogMessage ($"Removing typecheck of '{type.FullName}' inside {item.Body.Method.GetDisplayName ()} method");
			}

			static void UpdateBranchTarget (MethodBody body, Instruction oldTarget, Instruction newTarget)
			{
				foreach (var instr in body.Instructions) {
					switch (instr.OpCode.FlowControl) {
					case FlowControl.Branch:
					case FlowControl.Cond_Branch:
						if (instr.Operand == oldTarget)
							instr.Operand = newTarget;
						break;
					}
				}
			}
		}

		void ProcessQueue ()
		{
			while (!QueueIsEmpty ()) {
				(MethodDefinition method, DependencyInfo reason, MarkScopeStack.Scope scope) = _methods.Dequeue ();
				try {
					ProcessMethod (method, reason, scope);
				} catch (Exception e) when (!(e is LinkerFatalErrorException)) {
					throw new LinkerFatalErrorException (
						MessageContainer.CreateErrorMessage ($"Error processing method '{method.GetDisplayName ()}' in assembly '{method.Module.Name}'", 1005,
						origin: scope.Origin), e);
				}
			}
		}

		bool QueueIsEmpty ()
		{
			return _methods.Count == 0;
		}

		protected virtual void EnqueueMethod (MethodDefinition method, in DependencyInfo reason, in MarkScopeStack.Scope scope)
		{
			_methods.Enqueue ((method, reason, scope));
		}

		void ProcessVirtualMethods ()
		{
			foreach ((MethodDefinition method, MarkScopeStack.Scope scope) in _virtual_methods) {
				using (_scopeStack.PushScope (scope))
					ProcessVirtualMethod (method);
			}
		}

		void ProcessMarkedTypesWithInterfaces ()
		{
			// We may mark an interface type later on.  Which means we need to reprocess any time with one or more interface implementations that have not been marked
			// and if an interface type is found to be marked and implementation is not marked, then we need to mark that implementation

			// copy the data to avoid modified while enumerating error potential, which can happen under certain conditions.
			var typesWithInterfaces = _typesWithInterfaces.ToArray ();

			foreach ((var type, var scope) in typesWithInterfaces) {
				// Exception, types that have not been flagged as instantiated yet.  These types may not need their interfaces even if the
				// interface type is marked
				if (!Annotations.IsInstantiated (type) && !Annotations.IsRelevantToVariantCasting (type))
					continue;

				using (_scopeStack.PushScope (scope))
					MarkInterfaceImplementations (type);
			}
		}

		void DiscoverDynamicCastableImplementationInterfaces ()
		{
			// We could potentially avoid loading all references here: https://github.com/mono/linker/issues/1788
			foreach (var assembly in _context.GetReferencedAssemblies ().ToArray ()) {
				switch (Annotations.GetAction (assembly)) {
				// We only need to search assemblies where we don't mark everything
				// Assemblies that are fully marked already mark these types.
				case AssemblyAction.Link:
				case AssemblyAction.AddBypassNGen:
				case AssemblyAction.AddBypassNGenUsed:
					if (!_dynamicInterfaceCastableImplementationTypesDiscovered.Add (assembly))
						continue;

					foreach (TypeDefinition type in assembly.MainModule.Types)
						CheckIfTypeOrNestedTypesIsDynamicCastableImplementation (type);

					break;
				}
			}

			void CheckIfTypeOrNestedTypesIsDynamicCastableImplementation (TypeDefinition type)
			{
				if (!Annotations.IsMarked (type) && TypeIsDynamicInterfaceCastableImplementation (type))
					_dynamicInterfaceCastableImplementationTypes.Add (type);

				if (type.HasNestedTypes) {
					foreach (var nestedType in type.NestedTypes)
						CheckIfTypeOrNestedTypesIsDynamicCastableImplementation (nestedType);
				}
			}
		}

		void ProcessDynamicCastableImplementationInterfaces ()
		{
			DiscoverDynamicCastableImplementationInterfaces ();

			// We may mark an interface type later on.  Which means we need to reprocess any time with one or more interface implementations that have not been marked
			// and if an interface type is found to be marked and implementation is not marked, then we need to mark that implementation

			for (int i = 0; i < _dynamicInterfaceCastableImplementationTypes.Count; i++) {
				var type = _dynamicInterfaceCastableImplementationTypes[i];

				Debug.Assert (TypeIsDynamicInterfaceCastableImplementation (type));

				// If the type has already been marked, we can remove it from this list.
				if (Annotations.IsMarked (type)) {
					_dynamicInterfaceCastableImplementationTypes.RemoveAt (i--);
					continue;
				}

				foreach (var iface in type.Interfaces) {
					if (Annotations.IsMarked (iface.InterfaceType)) {
						// We only need to mark the type definition because the linker will ensure that all marked implemented interfaces and used method implementations
						// will be marked on this type as well.
						MarkType (type, new DependencyInfo (DependencyKind.DynamicInterfaceCastableImplementation, iface.InterfaceType), new MessageOrigin (_context.TryResolve (iface.InterfaceType)));

						_dynamicInterfaceCastableImplementationTypes.RemoveAt (i--);
						break;
					}
				}
			}
		}

		void ProcessPendingBodies ()
		{
			for (int i = 0; i < _unreachableBodies.Count; i++) {
				(var body, var scope) = _unreachableBodies[i];
				if (Annotations.IsInstantiated (body.Method.DeclaringType)) {
					using (_scopeStack.PushScope (scope))
						MarkMethodBody (body);

					_unreachableBodies.RemoveAt (i--);
				}
			}
		}

		void ProcessVirtualMethod (MethodDefinition method)
		{
			_context.Annotations.EnqueueVirtualMethod (method);

			var overrides = Annotations.GetOverrides (method);
			if (overrides != null) {
				foreach (OverrideInformation @override in overrides)
					ProcessOverride (@override);
			}

			var defaultImplementations = Annotations.GetDefaultInterfaceImplementations (method);
			if (defaultImplementations != null) {
				foreach (var defaultImplementationInfo in defaultImplementations) {
					ProcessDefaultImplementation (defaultImplementationInfo.InstanceType, defaultImplementationInfo.ProvidingInterface);
				}
			}
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

			// Interface static veitual methods will be abstract and will also by pass this check to get marked
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

			if (method.IsVirtual)
				ProcessVirtualMethod (method);
		}

		bool IsInterfaceOverrideThatDoesNotNeedMarked (OverrideInformation overrideInformation, bool isInstantiated)
		{
			if (!overrideInformation.IsOverrideOfInterfaceMember || isInstantiated)
				return false;

			// This is a static interface method and these checks should all be true
			if (overrideInformation.Override.IsStatic && overrideInformation.Base.IsStatic && overrideInformation.Base.IsAbstract && !overrideInformation.Override.IsVirtual)
				return false;

			if (overrideInformation.MatchingInterfaceImplementation != null)
				return !Annotations.IsMarked (overrideInformation.MatchingInterfaceImplementation);

			var interfaceType = overrideInformation.InterfaceType;
			var overrideDeclaringType = overrideInformation.Override.DeclaringType;

			if (!IsInterfaceImplementationMarkedRecursively (overrideDeclaringType, interfaceType))
				return true;

			return false;
		}

		bool IsInterfaceImplementationMarkedRecursively (TypeDefinition type, TypeDefinition interfaceType)
		{
			if (type.HasInterfaces) {
				foreach (var intf in type.Interfaces) {
					TypeDefinition resolvedInterface = _context.Resolve (intf.InterfaceType);
					if (resolvedInterface == null)
						continue;

					if (Annotations.IsMarked (intf) && RequiresInterfaceRecursively (resolvedInterface, interfaceType))
						return true;
				}
			}

			return false;
		}

		bool RequiresInterfaceRecursively (TypeDefinition typeToExamine, TypeDefinition interfaceType)
		{
			if (typeToExamine == interfaceType)
				return true;

			if (typeToExamine.HasInterfaces) {
				foreach (var iface in typeToExamine.Interfaces) {
					var resolved = _context.TryResolve (iface.InterfaceType);
					if (resolved == null)
						continue;

					if (RequiresInterfaceRecursively (resolved, interfaceType))
						return true;
				}
			}

			return false;
		}

		void ProcessDefaultImplementation (TypeDefinition typeWithDefaultImplementedInterfaceMethod, InterfaceImplementation implementation)
		{
			if (!Annotations.IsInstantiated (typeWithDefaultImplementedInterfaceMethod))
				return;

			MarkInterfaceImplementation (implementation);
		}

		void MarkMarshalSpec (IMarshalInfoProvider spec, in DependencyInfo reason)
		{
			if (!spec.HasMarshalInfo)
				return;

			if (spec.MarshalInfo is CustomMarshalInfo marshaler) {
				MarkType (marshaler.ManagedType, reason);
				TypeDefinition type = _context.Resolve (marshaler.ManagedType);
				if (type != null) {
					MarkICustomMarshalerMethods (type, in reason);
					MarkCustomMarshalerGetInstance (type, in reason);
				}
			}
		}

		void MarkCustomAttributes (ICustomAttributeProvider provider, in DependencyInfo reason)
		{
			if (provider.HasCustomAttributes) {
				bool providerInLinkedAssembly = Annotations.GetAction (CustomAttributeSource.GetAssemblyFromCustomAttributeProvider (provider)) == AssemblyAction.Link;
				bool markOnUse = _context.KeepUsedAttributeTypesOnly && providerInLinkedAssembly;

				foreach (CustomAttribute ca in provider.CustomAttributes) {
					if (ProcessLinkerSpecialAttribute (ca, provider, reason))
						continue;

					if (markOnUse) {
						_lateMarkedAttributes.Enqueue ((new AttributeProviderPair (ca, provider), reason, _scopeStack.CurrentScope));
						continue;
					}

					if (UnconditionalSuppressMessageAttributeState.TypeRefHasUnconditionalSuppressions (ca.Constructor.DeclaringType))
						_context.Suppressions.AddSuppression (ca, provider);

					var resolvedAttributeType = _context.Resolve (ca.AttributeType);
					if (resolvedAttributeType == null) {
						continue;
					}

					if (providerInLinkedAssembly && IsAttributeRemoved (ca, resolvedAttributeType))
						continue;

					MarkCustomAttribute (ca, reason);
					MarkSpecialCustomAttributeDependencies (ca, provider);
				}
			}

			if (!(provider is MethodDefinition || provider is FieldDefinition))
				return;

			IMemberDefinition providerMember = (IMemberDefinition) provider; ;
			using (_scopeStack.PushScope (new MessageOrigin (providerMember)))
				foreach (var dynamicDependency in _context.Annotations.GetLinkerAttributes<DynamicDependency> (providerMember))
					MarkDynamicDependency (dynamicDependency, providerMember);
		}

		bool IsAttributeRemoved (CustomAttribute ca, TypeDefinition attributeType)
		{
			foreach (var attr in _context.Annotations.GetLinkerAttributes<RemoveAttributeInstancesAttribute> (attributeType)) {
				var args = attr.Arguments;
				if (args.Length == 0)
					return true;

				if (args.Length > ca.ConstructorArguments.Count)
					continue;

				if (HasMatchingArguments (args, ca.ConstructorArguments))
					return true;
			}

			return false;

			static bool HasMatchingArguments (CustomAttributeArgument[] argsA, Collection<CustomAttributeArgument> argsB)
			{
				for (int i = 0; i < argsA.Length; ++i) {
					object argB = argsB[i].Value;

					// The internal attribute has only object overloads which does not allow
					// to distinguish between boxed/converted and exact candidates. This
					// allows simpler data entering and for now it does not like problem.
					if (argB is CustomAttributeArgument caa)
						argB = caa.Value;

					if (!argsA[i].Value.Equals (argB))
						return false;
				}

				return true;
			}
		}

		protected virtual bool ProcessLinkerSpecialAttribute (CustomAttribute ca, ICustomAttributeProvider provider, in DependencyInfo reason)
		{
			var isPreserveDependency = IsUserDependencyMarker (ca.AttributeType);
			var isDynamicDependency = ca.AttributeType.IsTypeOf<DynamicDependencyAttribute> ();

			if (!((isPreserveDependency || isDynamicDependency) && provider is IMemberDefinition member))
				return false;

			if (isPreserveDependency)
				MarkUserDependency (member, ca);

			if (_context.CanApplyOptimization (CodeOptimizations.RemoveDynamicDependencyAttribute, member.DeclaringType.Module.Assembly)) {
				// Record the custom attribute so that it has a reason, without actually marking it.
				Tracer.AddDirectDependency (ca, reason, marked: false);
			} else {
				MarkCustomAttribute (ca, reason);
			}

			return true;
		}

		void MarkDynamicDependency (DynamicDependency dynamicDependency, IMemberDefinition context)
		{
			Debug.Assert (context is MethodDefinition || context is FieldDefinition);
			AssemblyDefinition assembly;
			if (dynamicDependency.AssemblyName != null) {
				assembly = _context.TryResolve (dynamicDependency.AssemblyName);
				if (assembly == null) {
					_context.LogWarning ($"Unresolved assembly '{dynamicDependency.AssemblyName}' in 'DynamicDependencyAttribute'", 2035, _scopeStack.CurrentScope.Origin);
					return;
				}
			} else {
				assembly = context.DeclaringType.Module.Assembly;
				Debug.Assert (assembly != null);
			}

			TypeDefinition type;
			if (dynamicDependency.TypeName is string typeName) {
				type = DocumentationSignatureParser.GetTypeByDocumentationSignature (assembly, typeName);
				if (type == null) {
					_context.LogWarning ($"Unresolved type '{typeName}' in DynamicDependencyAttribute", 2036, _scopeStack.CurrentScope.Origin);
					return;
				}

				MarkingHelpers.MarkMatchingExportedType (type, assembly, new DependencyInfo (DependencyKind.DynamicDependency, type));
			} else if (dynamicDependency.Type is TypeReference typeReference) {
				type = _context.TryResolve (typeReference);
				if (type == null) {
					_context.LogWarning ($"Unresolved type '{typeReference}' in DynamicDependencyAtribute", 2036, _scopeStack.CurrentScope.Origin);
					return;
				}
			} else {
				type = _context.TryResolve (context.DeclaringType);
				if (type == null) {
					_context.LogWarning ($"Unresolved type '{context.DeclaringType}' in DynamicDependencyAttribute", 2036, context);
					return;
				}
			}

			IEnumerable<IMetadataTokenProvider> members;
			if (dynamicDependency.MemberSignature is string memberSignature) {
				members = DocumentationSignatureParser.GetMembersByDocumentationSignature (type, memberSignature, acceptName: true);
				if (!members.Any ()) {
					_context.LogWarning ($"No members were resolved for '{memberSignature}'.", 2037, _scopeStack.CurrentScope.Origin);
					return;
				}
			} else {
				var memberTypes = dynamicDependency.MemberTypes;
				members = type.GetDynamicallyAccessedMembers (_context, memberTypes);
				if (!members.Any ()) {
					_context.LogWarning ($"No members were resolved for '{memberTypes}'.", 2037, _scopeStack.CurrentScope.Origin);
					return;
				}
			}

			MarkMembers (type, members, new DependencyInfo (DependencyKind.DynamicDependency, dynamicDependency.OriginalAttribute));
		}

		void MarkMembers (TypeDefinition typeDefinition, IEnumerable<IMetadataTokenProvider> members, in DependencyInfo reason)
		{
			foreach (var member in members) {
				switch (member) {
				case TypeDefinition type:
					MarkType (type, reason);
					break;
				case MethodDefinition method:
					MarkMethod (method, reason);
					break;
				case FieldDefinition field:
					MarkField (field, reason);
					break;
				case PropertyDefinition property:
					MarkPropertyVisibleToReflection (property, reason);
					break;
				case EventDefinition @event:
					MarkEventVisibleToReflection (@event, reason);
					break;
				case InterfaceImplementation interfaceType:
					MarkInterfaceImplementation (interfaceType, null, reason);
					break;
				case null:
					MarkEntireType (typeDefinition, includeBaseAndInterfaceTypes: true, reason);
					break;
				}
			}
		}


		protected virtual bool IsUserDependencyMarker (TypeReference type)
		{
			return type.Name == "PreserveDependencyAttribute" && type.Namespace == "System.Runtime.CompilerServices";
		}

		protected virtual void MarkUserDependency (IMemberDefinition context, CustomAttribute ca)
		{
			_context.LogWarning ($"'PreserveDependencyAttribute' is deprecated. Use 'DynamicDependencyAttribute' instead.", 2033, context);

			if (!DynamicDependency.ShouldProcess (_context, ca))
				return;

			AssemblyDefinition assembly;
			var args = ca.ConstructorArguments;
			if (args.Count >= 3 && args[2].Value is string assemblyName) {
				assembly = _context.TryResolve (assemblyName);
				if (assembly == null) {
					_context.LogWarning (
						$"Could not resolve dependency assembly '{assemblyName}' specified in a 'PreserveDependency' attribute", 2003, context);
					return;
				}
			} else {
				assembly = null;
			}

			TypeDefinition td;
			if (args.Count >= 2 && args[1].Value is string typeName) {
				AssemblyDefinition assemblyDef = assembly ?? (context as MemberReference).Module.Assembly;
				td = _context.TryResolve (assemblyDef, typeName);

				if (td == null) {
					_context.LogWarning (
						$"Could not resolve dependency type '{typeName}' specified in a `PreserveDependency` attribute", 2004, context);
					return;
				}

				MarkingHelpers.MarkMatchingExportedType (td, assemblyDef, new DependencyInfo (DependencyKind.PreservedDependency, ca));
			} else {
				td = _context.TryResolve (context.DeclaringType);
			}

			string member = null;
			string[] signature = null;
			if (args.Count >= 1 && args[0].Value is string memberSignature) {
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
				MarkEntireType (td, includeBaseAndInterfaceTypes: false, new DependencyInfo (DependencyKind.PreservedDependency, ca));
				return;
			}

			if (MarkDependencyMethod (td, member, signature, new DependencyInfo (DependencyKind.PreservedDependency, ca)))
				return;

			if (MarkNamedField (td, member, new DependencyInfo (DependencyKind.PreservedDependency, ca)))
				return;

			_context.LogWarning (
				$"Could not resolve dependency member '{member}' declared in type '{td.GetDisplayName ()}' specified in a `PreserveDependency` attribute", 2005, context);
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
					if (mp[i].ParameterType.FullName != signature[i].Trim ().ToCecilName ()) {
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

		void LazyMarkCustomAttributes (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			foreach (CustomAttribute ca in provider.CustomAttributes) {
				_assemblyLevelAttributes.Enqueue (new AttributeProviderPair (ca, provider));
			}
		}

		protected virtual void MarkCustomAttribute (CustomAttribute ca, in DependencyInfo reason)
		{
			Annotations.Mark (ca, reason);
			MarkMethod (ca.Constructor, new DependencyInfo (DependencyKind.AttributeConstructor, ca));

			MarkCustomAttributeArguments (ca);

			TypeReference constructor_type = ca.Constructor.DeclaringType;
			TypeDefinition type = _context.Resolve (constructor_type);

			if (type == null) {
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
					return true;
				}

				TypeDefinition type = _context.Resolve (attr_type);
				if (type is null || !Annotations.IsMarked (type))
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

		protected internal void MarkStaticConstructor (TypeDefinition type, in DependencyInfo reason)
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
			TypeDefinition type = _context.Resolve (security_type);
			if (type == null) {
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

			if (property != null && _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (property.SetMethod)) {
				var scanner = new ReflectionMethodBodyScanner (_context, this, _scopeStack);
				scanner.ProcessAttributeDataflow (property.SetMethod, new List<CustomAttributeArgument> { namedArgument.Argument });
			}
		}

		PropertyDefinition GetProperty (TypeDefinition type, string propertyname)
		{
			while (type != null) {
				PropertyDefinition property = type.Properties.FirstOrDefault (p => p.Name == propertyname);
				if (property != null)
					return property;

				type = _context.TryResolve (type.BaseType);
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

			if (field != null && _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (field)) {
				var scanner = new ReflectionMethodBodyScanner (_context, this, _scopeStack);
				scanner.ProcessAttributeDataflow (field, namedArgument.Argument);
			}
		}

		FieldDefinition GetField (TypeDefinition type, string fieldname)
		{
			while (type != null) {
				FieldDefinition field = type.Fields.FirstOrDefault (f => f.Name == fieldname);
				if (field != null)
					return field;

				type = _context.TryResolve (type.BaseType);
			}

			return null;
		}

		MethodDefinition GetMethodWithNoParameters (TypeDefinition type, string methodname)
		{
			while (type != null) {
				MethodDefinition method = type.Methods.FirstOrDefault (m => m.Name == methodname && !m.HasParameters);
				if (method != null)
					return method;

				type = _context.TryResolve (type.BaseType);
			}

			return null;
		}

		void MarkCustomAttributeArguments (CustomAttribute ca)
		{
			if (!ca.HasConstructorArguments)
				return;

			foreach (var argument in ca.ConstructorArguments)
				MarkCustomAttributeArgument (argument, ca);

			var resolvedConstructor = _context.TryResolve (ca.Constructor);
			if (resolvedConstructor != null && _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (resolvedConstructor)) {
				var scanner = new ReflectionMethodBodyScanner (_context, this, _scopeStack);
				scanner.ProcessAttributeDataflow (resolvedConstructor, ca.ConstructorArguments);
			}
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
				foreach (var caa in (CustomAttributeArgument[]) argument.Value)
					MarkCustomAttributeArgument (caa, ca);

				return;
			}

			if (at.Namespace == "System") {
				switch (at.Name) {
				case "Type":
					MarkType (argument.Type, new DependencyInfo (DependencyKind.CustomAttributeArgumentType, ca));
					MarkType ((TypeReference) argument.Value, new DependencyInfo (DependencyKind.CustomAttributeArgumentValue, ca));
					return;

				case "Object":
					var boxed_value = (CustomAttributeArgument) argument.Value;
					MarkType (boxed_value.Type, new DependencyInfo (DependencyKind.CustomAttributeArgumentType, ca));
					MarkCustomAttributeArgument (boxed_value, ca);
					return;
				}
			}
		}

		protected bool CheckProcessed (IMetadataTokenProvider provider)
		{
			return !Annotations.SetProcessed (provider);
		}

		protected void MarkAssembly (AssemblyDefinition assembly, DependencyInfo reason)
		{
			Annotations.Mark (assembly, reason);
			if (CheckProcessed (assembly))
				return;

			// We don't have a good origin for "assembly" level, so just use null for now
			using var assemblyScope = _scopeStack.PushScope (new MessageOrigin (null));

			EmbeddedXmlInfo.ProcessDescriptors (assembly, _context);

			foreach (Action<AssemblyDefinition> handleMarkAssembly in _markContext.MarkAssemblyActions)
				handleMarkAssembly (assembly);

			// Security attributes do not respect the attributes XML
			if (_context.StripSecurity)
				RemoveSecurity.ProcessAssembly (assembly, _context);

			MarkExportedTypesTarget.ProcessAssembly (assembly, _context);

			if (ProcessReferencesStep.IsFullyPreservedAction (_context.Annotations.GetAction (assembly))) {
				MarkEntireAssembly (assembly);
				return;
			}

			ProcessModuleType (assembly);

			LazyMarkCustomAttributes (assembly);

			MarkSecurityDeclarations (assembly, new DependencyInfo (DependencyKind.AssemblyOrModuleAttribute, assembly));

			foreach (ModuleDefinition module in assembly.Modules)
				LazyMarkCustomAttributes (module);
		}

		void MarkEntireAssembly (AssemblyDefinition assembly)
		{
			Debug.Assert (Annotations.IsProcessed (assembly));

			ModuleDefinition module = assembly.MainModule;

			MarkCustomAttributes (assembly, new DependencyInfo (DependencyKind.AssemblyOrModuleAttribute, assembly));
			MarkCustomAttributes (module, new DependencyInfo (DependencyKind.AssemblyOrModuleAttribute, module));

			foreach (TypeDefinition type in module.Types)
				MarkEntireType (type, includeBaseAndInterfaceTypes: false, new DependencyInfo (DependencyKind.TypeInAssembly, assembly));

			foreach (ExportedType exportedType in module.ExportedTypes) {
				MarkingHelpers.MarkExportedType (exportedType, module, new DependencyInfo (DependencyKind.ExportedType, assembly));
				MarkingHelpers.MarkForwardedScope (new TypeReference (exportedType.Namespace, exportedType.Name, module, exportedType.Scope));
			}

			foreach (TypeReference typeReference in module.GetTypeReferences ())
				MarkingHelpers.MarkForwardedScope (typeReference);
		}

		void ProcessModuleType (AssemblyDefinition assembly)
		{
			// The <Module> type may have an initializer, in which case we want to keep it.
			TypeDefinition moduleType = assembly.MainModule.Types.FirstOrDefault (t => t.MetadataToken.RID == 1);
			if (moduleType != null && moduleType.HasMethods)
				MarkType (moduleType, new DependencyInfo (DependencyKind.TypeInAssembly, assembly));
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

			// We don't have a good message origin for assembly level attributes - so set it to null
			using var assemblyScope = _scopeStack.PushScope (new MessageOrigin (null));
			while (_assemblyLevelAttributes.Count != 0) {
				var assemblyLevelAttribute = _assemblyLevelAttributes.Dequeue ();
				var customAttribute = assemblyLevelAttribute.Attribute;

				var resolved = _context.Resolve (customAttribute.Constructor);
				if (resolved == null) {
					continue;
				}

				if (IsAttributeRemoved (customAttribute, resolved.DeclaringType) && Annotations.GetAction (CustomAttributeSource.GetAssemblyFromCustomAttributeProvider (assemblyLevelAttribute.Provider)) == AssemblyAction.Link)
					continue;

				if (customAttribute.AttributeType.IsTypeOf ("System.Runtime.CompilerServices", "InternalsVisibleToAttribute") && !Annotations.IsMarked (customAttribute)) {
					_ivt_attributes.Add (assemblyLevelAttribute);
					continue;
				} else if (!ShouldMarkTopLevelCustomAttribute (assemblyLevelAttribute, resolved)) {
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

			var skippedItems = new List<(AttributeProviderPair, DependencyInfo, MarkScopeStack.Scope)> ();
			var markOccurred = false;

			while (_lateMarkedAttributes.Count != 0) {
				var (attributeProviderPair, reason, scope) = _lateMarkedAttributes.Dequeue ();
				var customAttribute = attributeProviderPair.Attribute;
				var provider = attributeProviderPair.Provider;

				var resolved = _context.Resolve (customAttribute.Constructor);
				if (resolved == null) {
					continue;
				}

				if (!ShouldMarkCustomAttribute (customAttribute, provider)) {
					skippedItems.Add ((attributeProviderPair, reason, scope));
					continue;
				}

				markOccurred = true;
				using (_scopeStack.PushScope (scope)) {
					MarkCustomAttribute (customAttribute, reason);
					MarkSpecialCustomAttributeDependencies (customAttribute, provider);
				}
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
				MarkType (reference.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, reference), new MessageOrigin (_context.TryResolve (reference)));

				// Blame the field definition that we will resolve on the field reference.
				reason = new DependencyInfo (DependencyKind.FieldOnGenericInstance, reference);
			}

			FieldDefinition field = _context.Resolve (reference);

			if (field == null) {
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

			if (reason.Kind == DependencyKind.AlreadyMarked) {
				Debug.Assert (Annotations.IsMarked (field));
			} else {
				Annotations.Mark (field, reason);
			}

			if (CheckProcessed (field))
				return;

			// Use the original scope for marking the declaring type - it provides better warning message location
			MarkType (field.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, field));

			using var fieldScope = _scopeStack.PushScope (new MessageOrigin (field));
			MarkType (field.FieldType, new DependencyInfo (DependencyKind.FieldType, field));
			MarkCustomAttributes (field, new DependencyInfo (DependencyKind.CustomAttribute, field));
			MarkMarshalSpec (field, new DependencyInfo (DependencyKind.FieldMarshalSpec, field));
			DoAdditionalFieldProcessing (field);

			// If we accessed a field on a type and the type has explicit/sequential layout, make sure to keep
			// all the other fields.
			//
			// We normally do this when the type is seen as instantiated, but one can get into a situation
			// where the type is not seen as instantiated and the offsets still matter (usually when type safety
			// is violated with Unsafe.As).
			//
			// This won't do too much work because classes are rarely tagged for explicit/sequential layout.
			if (!field.DeclaringType.IsValueType && !field.DeclaringType.IsAutoLayout) {
				// We also need to walk the base hierarchy because the offset of the field depends on the
				// layout of the base.
				TypeDefinition typeWithFields = field.DeclaringType;
				while (typeWithFields != null) {
					MarkImplicitlyUsedFields (typeWithFields);
					typeWithFields = _context.TryResolve (typeWithFields.BaseType);
				}
			}

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
		}

		protected virtual bool IgnoreScope (IMetadataScope scope)
		{
			AssemblyDefinition assembly = _context.Resolve (scope);
			return Annotations.GetAction (assembly) != AssemblyAction.Link;
		}

		void MarkModule (ModuleDefinition module, DependencyInfo reason)
		{
			if (reason.Kind == DependencyKind.AlreadyMarked) {
				Debug.Assert (Annotations.IsMarked (module));
			} else {
				Annotations.Mark (module, reason);
			}
			if (CheckProcessed (module))
				return;
			MarkAssembly (module.Assembly, new DependencyInfo (DependencyKind.AssemblyOfModule, module));
		}

		protected virtual void MarkSerializable (TypeDefinition type)
		{
			if (!type.HasMethods)
				return;

			if (_context.GetTargetRuntimeVersion () > TargetRuntimeVersion.NET5)
				return;

			if (type.IsSerializable ()) {
				MarkDefaultConstructor (type, new DependencyInfo (DependencyKind.SerializationMethodForType, type));
				MarkMethodsIf (type.Methods, IsSpecialSerializationConstructor, new DependencyInfo (DependencyKind.SerializationMethodForType, type));
			}

			MarkMethodsIf (type.Methods, HasOnSerializeOrDeserializeAttribute, new DependencyInfo (DependencyKind.SerializationMethodForType, type));
		}

		protected internal virtual TypeDefinition MarkTypeVisibleToReflection (TypeReference type, TypeDefinition definition, in DependencyInfo reason)
		{
			// If a type is visible to reflection, we need to stop doing optimization that could cause observable difference
			// in reflection APIs. This includes APIs like MakeGenericType (where variant castability of the produced type
			// could be incorrect) or IsAssignableFrom (where assignability of unconstructed types might change).
			Annotations.MarkRelevantToVariantCasting (definition);

			Annotations.MarkReflectionUsed (definition);

			MarkImplicitlyUsedFields (definition);

			return MarkType (type, reason);
		}

		internal void MarkMethodVisibleToReflection (MethodDefinition method, in DependencyInfo reason)
		{
			MarkIndirectlyCalledMethod (method, reason);
			Annotations.MarkReflectionUsed (method);
		}

		internal void MarkFieldVisibleToReflection (FieldDefinition field, in DependencyInfo reason)
		{
			MarkField (field, reason);
		}

		internal void MarkPropertyVisibleToReflection (PropertyDefinition property, in DependencyInfo reason)
		{
			// Marking the property itself actually doesn't keep it (it only marks its attributes and records the dependency), we have to mark the methods on it
			MarkProperty (property, reason);
			// We don't track PropertyInfo, so we can't tell if any accessor is needed by the app, so include them both.
			// With better tracking it might be possible to be more precise here: mono/linker/issues/1948
			MarkMethodIfNotNull (property.GetMethod, reason);
			MarkMethodIfNotNull (property.SetMethod, reason);
			MarkMethodsIf (property.OtherMethods, m => true, reason);
		}

		internal void MarkEventVisibleToReflection (EventDefinition @event, in DependencyInfo reason)
		{
			// MarkEvent actually marks the add/remove/invoke methods as well, so no need to mark those explicitly
			MarkEvent (@event, reason);
			MarkMethodsIf (@event.OtherMethods, m => true, reason);
		}

		internal void MarkStaticConstructorVisibleToReflection (TypeDefinition type, in DependencyInfo reason)
		{
			MarkStaticConstructor (type, reason);
		}

		/// <summary>
		/// Marks the specified <paramref name="reference"/> as referenced.
		/// </summary>
		/// <param name="reference">The type reference to mark.</param>
		/// <param name="reason">The reason why the marking is occuring</param>
		/// <returns>The resolved type definition if the reference can be resolved</returns>
		protected internal virtual TypeDefinition MarkType (TypeReference reference, DependencyInfo reason, MessageOrigin? origin = null)
		{
#if DEBUG
			if (!_typeReasons.Contains (reason.Kind))
				throw new ArgumentOutOfRangeException ($"Internal error: unsupported type dependency {reason.Kind}");
#endif
			if (reference == null)
				return null;

			using var localScope = origin.HasValue ? _scopeStack.PushScope (origin.Value) : null;

			(reference, reason) = GetOriginalType (reference, reason);

			if (reference is FunctionPointerType)
				return null;

			if (reference is GenericParameter)
				return null;

			TypeDefinition type = _context.Resolve (reference);

			if (type == null)
				return null;

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

			if (_context.Annotations.HasLinkerAttribute<RemoveAttributeInstancesAttribute> (type)) {
				// Don't warn about references from the removed attribute itself (for example the .ctor on the attribute
				// will call MarkType on the attribute type itself). 
				// If for some reason we do keep the attribute type (could be because of previous reference which would cause IL2045
				// or because of a copy assembly with a reference and so on) then we should not spam the warnings due to the type itself.
				if (!(reason.Source is IMemberDefinition sourceMemberDefinition && sourceMemberDefinition.DeclaringType == type))
					_context.LogWarning (
						$"Attribute '{type.GetDisplayName ()}' is being referenced in code but the linker was " +
						$"instructed to remove all instances of this attribute. If the attribute instances are necessary make sure to " +
						$"either remove the linker attribute XML portion which removes the attribute instances, " +
						$"or override the removal by using the linker XML descriptor to keep the attribute type " +
						$"(which in turn keeps all of its instances).",
						2045, _scopeStack.CurrentScope.Origin, subcategory: MessageSubCategory.TrimAnalysis);
			}

			if (CheckProcessed (type))
				return type;

			MarkModule (type.Scope as ModuleDefinition, new DependencyInfo (DependencyKind.ScopeOfType, type));

			using var typeScope = _scopeStack.PushScope (new MessageOrigin (type));

			foreach (Action<TypeDefinition> handleMarkType in _markContext.MarkTypeActions)
				handleMarkType (type);

			MarkType (type.BaseType, new DependencyInfo (DependencyKind.BaseType, type));

			// The DynamicallyAccessedMembers hiearchy processing must be done after the base type was marked
			// (to avoid inconsistencies in the cache), but before anything else as work done below
			// might need the results of the processing here.
			_dynamicallyAccessedMembersTypeHierarchy.ProcessMarkedTypeForDynamicallyAccessedMembersHierarchy (type);

			if (type.DeclaringType != null)
				MarkType (type.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, type));
			MarkCustomAttributes (type, new DependencyInfo (DependencyKind.CustomAttribute, type));
			MarkSecurityDeclarations (type, new DependencyInfo (DependencyKind.CustomAttribute, type));

			if (type.IsMulticastDelegate ()) {
				MarkMulticastDelegate (type);
			}

			if (type.IsClass && type.BaseType == null && type.Name == "Object" && ShouldMarkSystemObjectFinalize)
				MarkMethodIf (type.Methods, m => m.Name == "Finalize", new DependencyInfo (DependencyKind.MethodForSpecialType, type));

			MarkSerializable (type);

			// This marks static fields of KeyWords/OpCodes/Tasks subclasses of an EventSource type.
			if (BCL.EventTracingForWindows.IsEventSourceImplementation (type, _context)) {
				MarkEventSourceProviders (type);
			}

			// This marks properties for [EventData] types as well as other attribute dependencies.
			MarkTypeSpecialCustomAttributes (type);

			MarkGenericParameterProvider (type);

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
				_typesWithInterfaces.Add ((type, _scopeStack.CurrentScope));

			if (type.HasMethods) {
				// For virtuals that must be preserved, blame the declaring type.
				MarkMethodsIf (type.Methods, IsVirtualNeededByTypeDueToPreservedScope, new DependencyInfo (DependencyKind.VirtualNeededDueToPreservedScope, type));
				if (ShouldMarkTypeStaticConstructor (type) && reason.Kind != DependencyKind.TriggersCctorForCalledMethod) {
					using (_scopeStack.PopToParent ())
						MarkStaticConstructor (type, new DependencyInfo (DependencyKind.CctorForType, type));
				}
			}

			DoAdditionalTypeProcessing (type);

			ApplyPreserveInfo (type);
			ApplyPreserveMethods (type);

			return type;
		}

		/// <summary>
		/// Allow subclasses to disable marking of System.Object.Finalize()
		/// </summary>
		protected virtual bool ShouldMarkSystemObjectFinalize => true;

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

		TypeDefinition GetDebuggerAttributeTargetType (CustomAttribute ca, AssemblyDefinition asm)
		{
			foreach (var property in ca.Properties) {
				if (property.Name == "Target")
					return _context.TryResolve ((TypeReference) property.Argument.Value);

				if (property.Name == "TargetTypeName") {
					string targetTypeName = (string) property.Argument.Value;
					TypeName typeName = TypeParser.ParseTypeName (targetTypeName);
					if (typeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
						AssemblyDefinition assembly = _context.TryResolve (assemblyQualifiedTypeName.AssemblyName.Name);
						return _context.TryResolve (assembly, targetTypeName);
					}

					return _context.TryResolve (asm, targetTypeName);
				}
			}

			return null;
		}

		void MarkTypeSpecialCustomAttributes (TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in type.CustomAttributes) {
				var attrType = attribute.Constructor.DeclaringType;
				var resolvedAttributeType = _context.Resolve (attrType);
				if (resolvedAttributeType == null) {
					continue;
				}

				if (_context.Annotations.HasLinkerAttribute<RemoveAttributeInstancesAttribute> (resolvedAttributeType) && Annotations.GetAction (type.Module.Assembly) == AssemblyAction.Link)
					continue;

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
					l.Parameters.Count == 1 && l.Parameters[0].ParameterType.IsTypeOf ("System", "Type"),
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

			TypeDefinition typeDefinition = null;
			switch (attribute.ConstructorArguments[0].Value) {
			case string s:
				typeDefinition = _context.TypeNameResolver.ResolveTypeName (s, _scopeStack.CurrentScope.Origin.MemberDefinition, out AssemblyDefinition assemblyDefinition)?.Resolve ();
				if (typeDefinition != null)
					MarkingHelpers.MarkMatchingExportedType (typeDefinition, assemblyDefinition, new DependencyInfo (DependencyKind.CustomAttribute, provider));

				break;
			case TypeReference type:
				typeDefinition = _context.Resolve (type);
				break;
			}

			if (typeDefinition == null)
				return;

			Tracer.AddDirectDependency (attribute, new DependencyInfo (DependencyKind.CustomAttribute, provider), marked: false);
			MarkMethodsIf (typeDefinition.Methods, predicate, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
		}

		void MarkTypeWithDebuggerDisplayAttribute (TypeDefinition type, CustomAttribute attribute)
		{
			if (_context.KeepMembersForDebugger) {

				// Members referenced by the DebuggerDisplayAttribute are kept even if the attribute may not be.
				// Record a logical dependency on the attribute so that we can blame it for the kept members below.
				Tracer.AddDirectDependency (attribute, new DependencyInfo (DependencyKind.CustomAttribute, type), marked: false);

				string displayString = (string) attribute.ConstructorArguments[0].Value;
				if (string.IsNullOrEmpty (displayString))
					return;

				Regex regex = new Regex ("{[^{}]+}", RegexOptions.Compiled);

				foreach (Match match in regex.Matches (displayString)) {
					// Remove '{' and '}'
					string realMatch = match.Value.Substring (1, match.Value.Length - 2);

					// Remove ",nq" suffix if present
					// (it asks the expression evaluator to remove the quotes when displaying the final value)
					if (Regex.IsMatch (realMatch, @".+,\s*nq")) {
						realMatch = realMatch.Substring (0, realMatch.LastIndexOf (','));
					}

					if (realMatch.EndsWith ("()")) {
						string methodName = realMatch.Substring (0, realMatch.Length - 2);

						// It's a call to a method on some member.  Handling this scenario robustly would be complicated and a decent bit of work.
						// 
						// We could implement support for this at some point, but for now it's important to make sure at least we don't crash trying to find some
						// method on the current type when it exists on some other type
						if (methodName.Contains ("."))
							continue;

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
						// Currently if we don't understand the DebuggerDisplayAttribute we mark everything on the type
						// This can be improved: mono/linker/issues/1873
						MarkMethods (type, new DependencyInfo (DependencyKind.KeptForSpecialAttribute, attribute));
						MarkFields (type, includeStatic: true, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, attribute));
						type = _context.TryResolve (type.BaseType);
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

				TypeDefinition proxyType = _context.TryResolve (proxyTypeReference);
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

			argument = attribute.ConstructorArguments[0].Value as string;

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

		bool MarkNamedField (TypeDefinition type, string field_name, in DependencyInfo reason)
		{
			if (!type.HasFields)
				return false;

			foreach (FieldDefinition field in type.Fields) {
				if (field.Name != field_name)
					continue;

				MarkField (field, reason);
				return true;
			}

			return false;
		}

		void MarkNamedProperty (TypeDefinition type, string property_name, in DependencyInfo reason)
		{
			if (!type.HasProperties)
				return;

			foreach (PropertyDefinition property in type.Properties) {
				if (property.Name != property_name)
					continue;

				using (_scopeStack.PushScope (new MessageOrigin (property))) {
					// This marks methods directly without reporting the property.
					MarkMethod (property.GetMethod, reason);
					MarkMethod (property.SetMethod, reason);
				}
			}
		}

		void MarkInterfaceImplementations (TypeDefinition type)
		{
			if (!type.HasInterfaces)
				return;

			foreach (var iface in type.Interfaces) {
				// Only mark interface implementations of interface types that have been marked.
				// This enables stripping of interfaces that are never used
				var resolvedInterfaceType = _context.Resolve (iface.InterfaceType);
				if (resolvedInterfaceType == null) {
					continue;
				}

				if (ShouldMarkInterfaceImplementation (type, iface, resolvedInterfaceType))
					MarkInterfaceImplementation (iface, new MessageOrigin (type));
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

			return parameters[0].ParameterType.Name == "SerializationInfo" &&
				parameters[1].ParameterType.Name == "StreamingContext";
		}

		protected internal bool MarkMethodsIf (Collection<MethodDefinition> methods, Func<MethodDefinition, bool> predicate, in DependencyInfo reason)
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

		void MarkCustomMarshalerGetInstance (TypeDefinition type, in DependencyInfo reason)
		{
			if (!type.HasMethods)
				return;

			MarkMethodIf (type.Methods, m =>
				m.Name == "GetInstance" && m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String,
				reason);
		}

		void MarkICustomMarshalerMethods (TypeDefinition type, in DependencyInfo reason)
		{
			do {
				if (!type.HasInterfaces)
					continue;

				foreach (var iface in type.Interfaces) {
					var iface_type = iface.InterfaceType;
					if (!iface_type.IsTypeOf ("System.Runtime.InteropServices", "ICustomMarshaler"))
						continue;

					//
					// Instead of trying to guess where to find the interface declaration linker walks
					// the list of implemented interfaces and resolve the declaration from there
					//
					var tdef = _context.Resolve (iface_type);
					if (tdef == null) {
						return;
					}

					MarkMethodsIf (tdef.Methods, m => !m.IsStatic, reason);

					MarkInterfaceImplementation (iface, new MessageOrigin (type));
					return;
				}
			} while ((type = _context.TryResolve (type.BaseType)) != null);
		}

		static bool IsNonEmptyStaticConstructor (MethodDefinition method)
		{
			if (!method.IsStaticConstructor ())
				return false;

			if (!method.HasBody || !method.IsIL)
				return true;

			if (method.Body.CodeSize != 1)
				return true;

			return method.Body.Instructions[0].OpCode.Code != Code.Ret;
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
			MarkMethodsIf (type.Methods, m => m.Name == ".ctor" || m.Name == "Invoke", new DependencyInfo (DependencyKind.MethodForSpecialType, type));
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

			for (int i = 0; i < fnptr.Parameters.Count; i++) {
				MarkType (fnptr.Parameters[i].ParameterType, new DependencyInfo (DependencyKind.ParameterType, fnptr));
			}
		}

		void MarkModifierType (IModifierType mod)
		{
			MarkType (mod.ModifierType, new DependencyInfo (DependencyKind.ModifierType, mod));
		}

		void MarkGenericArguments (IGenericInstance instance)
		{
			var arguments = instance.GenericArguments;

			var generic_element = GetGenericProviderFromInstance (instance);
			if (generic_element == null)
				return;

			var parameters = generic_element.GenericParameters;

			if (arguments.Count != parameters.Count)
				return;

			for (int i = 0; i < arguments.Count; i++) {
				var argument = arguments[i];
				var parameter = parameters[i];

				TypeDefinition argumentTypeDef = MarkType (argument, new DependencyInfo (DependencyKind.GenericArgumentType, instance));

				if (_context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (parameter)) {
					// The only two implementations of IGenericInstance both derive from MemberReference
					Debug.Assert (instance is MemberReference);

					using var _ = _scopeStack.CurrentScope.Origin.MemberDefinition == null ? _scopeStack.PushScope (new MessageOrigin ((instance as MemberReference).Resolve ())) : null;
					var scanner = new ReflectionMethodBodyScanner (_context, this, _scopeStack);
					scanner.ProcessGenericArgumentDataFlow (parameter, argument);
				}

				if (argumentTypeDef == null)
					continue;

				Annotations.MarkRelevantToVariantCasting (argumentTypeDef);

				if (parameter.HasDefaultConstructorConstraint)
					MarkDefaultConstructor (argumentTypeDef, new DependencyInfo (DependencyKind.DefaultCtorForNewConstrainedGenericArgument, instance));
			}
		}

		IGenericParameterProvider GetGenericProviderFromInstance (IGenericInstance instance)
		{
			if (instance is GenericInstanceMethod method)
				return _context.TryResolve (method.ElementMethod);

			if (instance is GenericInstanceType type)
				return _context.TryResolve (type.ElementType);

			return null;
		}

		void ApplyPreserveInfo (TypeDefinition type)
		{
			using var typeScope = _scopeStack.PushScope (new MessageOrigin (type));

			if (Annotations.TryGetPreserve (type, out TypePreserve preserve)) {
				if (!Annotations.SetAppliedPreserve (type, preserve))
					throw new InternalErrorException ($"Type {type} already has applied {preserve}.");

				var di = new DependencyInfo (DependencyKind.TypePreserve, type);

				switch (preserve) {
				case TypePreserve.All:
					MarkFields (type, true, di);
					MarkMethods (type, di);
					return;

				case TypePreserve.Fields:
					if (!MarkFields (type, true, di, true))
						_context.LogWarning ($"Type {type.GetDisplayName ()} has no fields to preserve", 2001, type);
					break;
				case TypePreserve.Methods:
					if (!MarkMethods (type, di))
						_context.LogWarning ($"Type {type.GetDisplayName ()} has no methods to preserve", 2002, type);
					break;
				}
			}

			if (Annotations.TryGetPreservedMembers (type, out TypePreserveMembers members)) {
				var di = new DependencyInfo (DependencyKind.TypePreserve, type);

				if (type.HasMethods) {
					foreach (var m in type.Methods) {
						if ((members & TypePreserveMembers.Visible) != 0 && IsMethodVisible (m)) {
							MarkMethod (m, di);
							continue;
						}

						if ((members & TypePreserveMembers.Internal) != 0 && IsMethodInternal (m)) {
							MarkMethod (m, di);
							continue;
						}

						if ((members & TypePreserveMembers.Library) != 0) {
							if (IsSpecialSerializationConstructor (m) || HasOnSerializeOrDeserializeAttribute (m)) {
								MarkMethod (m, di);
								continue;
							}
						}
					}
				}

				if (type.HasFields) {
					foreach (var f in type.Fields) {
						if ((members & TypePreserveMembers.Visible) != 0 && IsFieldVisible (f)) {
							MarkField (f, di);
							continue;
						}

						if ((members & TypePreserveMembers.Internal) != 0 && IsFieldInternal (f)) {
							MarkField (f, di);
							continue;
						}
					}
				}
			}
		}

		static bool IsMethodVisible (MethodDefinition method)
		{
			return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
		}

		static bool IsMethodInternal (MethodDefinition method)
		{
			return method.IsAssembly || method.IsFamilyAndAssembly;
		}

		static bool IsFieldVisible (FieldDefinition field)
		{
			return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
		}

		static bool IsFieldInternal (FieldDefinition field)
		{
			return field.IsAssembly || field.IsFamilyAndAssembly;
		}

		void ApplyPreserveMethods (TypeDefinition type)
		{
			var list = Annotations.GetPreservedMethods (type);
			if (list == null)
				return;

			Annotations.ClearPreservedMethods (type);
			MarkMethodCollection (list, new DependencyInfo (DependencyKind.PreservedMethod, type));
		}

		void ApplyPreserveMethods (MethodDefinition method)
		{
			var list = Annotations.GetPreservedMethods (method);
			if (list == null)
				return;

			Annotations.ClearPreservedMethods (method);
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

		protected internal void MarkIndirectlyCalledMethod (MethodDefinition method, in DependencyInfo reason)
		{
			MarkMethod (method, reason);
			Annotations.MarkIndirectlyCalledMethod (method);
		}

		protected virtual MethodDefinition MarkMethod (MethodReference reference, DependencyInfo reason)
		{
			(reference, reason) = GetOriginalMethod (reference, reason);

			if (reference.DeclaringType is ArrayType arrayType) {
				MarkType (reference.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, reference));

				if (reference.Name == ".ctor") {
					Annotations.MarkRelevantToVariantCasting (_context.TryResolve (arrayType));
				}
				return null;
			}

			if (reference.DeclaringType is GenericInstanceType) {
				// Blame the method reference on the original reason without marking it.
				Tracer.AddDirectDependency (reference, reason, marked: false);
				MarkType (reference.DeclaringType, new DependencyInfo (DependencyKind.DeclaringType, reference));
				// Mark the resolved method definition as a dependency of the reference.
				reason = new DependencyInfo (DependencyKind.MethodOnGenericInstance, reference);
			}

			MethodDefinition method = _context.Resolve (reference);
			if (method == null)
				return null;

			if (Annotations.GetAction (method) == MethodAction.Nothing)
				Annotations.SetAction (method, MethodAction.Parse);

			EnqueueMethod (method, reason, _scopeStack.CurrentScope);

			// All override methods should have the same annotations as their base methods (else we will produce warning IL2046.)
			// When marking override methods with RequiresUnreferencedCode on a type annotated with DynamicallyAccessedMembers,
			// we should only issue a warning for the base method.
			if (reason.Kind != DependencyKind.DynamicallyAccessedMember || !method.IsVirtual || Annotations.GetBaseMethods (method) == null)
				ProcessRequiresUnreferencedCode (method, reason.Kind);

			return method;
		}

		void ProcessRequiresUnreferencedCode (MethodDefinition method, DependencyKind dependencyKind)
		{
			switch (dependencyKind) {
			case DependencyKind.AccessedViaReflection:
			case DependencyKind.CctorForType:
			case DependencyKind.DynamicallyAccessedMember:
			case DependencyKind.DynamicDependency:
			case DependencyKind.ElementMethod:
			case DependencyKind.Ldftn:
			case DependencyKind.Ldvirtftn:
			case DependencyKind.TriggersCctorForCalledMethod:
			case DependencyKind.AttributeConstructor:
				break;

			// DirectCall, VirtualCall and NewObj are handled by ReflectionMethodBodyScanner
			// This is necessary since the ReflectionMethodBodyScanner has intrinsic handling for some
			// of the methods annotated with the attribute (for example Type.GetType)
			// and it know when it's OK and when it needs a warning. In this place we don't know
			// and would have to warn every time.

			default:
				return;
			}

			CheckAndReportRequiresUnreferencedCode (method);
		}

		internal bool ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode ()
		{
			// Check if the current scope method has RequiresUnreferencedCode on it
			// since that attribute automatically suppresses all trim analysis warnings.
			// Check both the immediate origin method as well as suppression context method
			// since that will be different for compiler generated code.
			var currentOrigin = _scopeStack.CurrentScope.Origin;

			IMemberDefinition suppressionContextMember = currentOrigin.SuppressionContextMember;
			if (suppressionContextMember != null &&
				Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (suppressionContextMember))
				return true;

			IMemberDefinition originMember = currentOrigin.MemberDefinition;
			if (suppressionContextMember != originMember && originMember != null &&
				Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (originMember))
				return true;

			return false;
		}

		internal void CheckAndReportRequiresUnreferencedCode (MethodDefinition method)
		{
			var currentOrigin = _scopeStack.CurrentScope.Origin;

			// If the caller of a method is already marked with `RequiresUnreferencedCodeAttribute` a new warning should not
			// be produced for the callee.
			if (ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode ())
				return;

			if (Annotations.TryGetLinkerAttribute (method, out RequiresUnreferencedCodeAttribute requiresUnreferencedCode)) {
				string message = $"Using method '{method.GetDisplayName ()}' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.";
				if (!string.IsNullOrEmpty (requiresUnreferencedCode.Message))
					message += $" {requiresUnreferencedCode.Message}{(requiresUnreferencedCode.Message.TrimEnd ().EndsWith ('.') ? "" : ".")}";

				if (!string.IsNullOrEmpty (requiresUnreferencedCode.Url))
					message += " " + requiresUnreferencedCode.Url;

				_context.LogWarning (message, 2026, currentOrigin, MessageSubCategory.TrimAnalysis);
			}
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

		protected virtual void ProcessMethod (MethodDefinition method, in DependencyInfo reason, in MarkScopeStack.Scope scope)
		{
#if DEBUG
			if (!_methodReasons.Contains (reason.Kind))
				throw new InternalErrorException ($"Unsupported method dependency {reason.Kind}");
#endif

			_scopeStack.AssertIsEmpty ();
			using var parentScope = _scopeStack.PushScope (scope);
			using var methodScope = _scopeStack.PushScope (new MessageOrigin (method));

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

			bool markedForCall =
				reason.Kind == DependencyKind.DirectCall ||
				reason.Kind == DependencyKind.VirtualCall ||
				reason.Kind == DependencyKind.Newobj;
			if (markedForCall) {
				// Record declaring type of a called method up-front as a special case so that we may
				// track at least some method calls that trigger a cctor.
				// Temporarily switch to the original source for marking this method
				// this is for the same reason as for tracking, but this time so that we report potential
				// warnings from a better place.
				MarkType (method.DeclaringType, new DependencyInfo (DependencyKind.DeclaringTypeOfCalledMethod, method), new MessageOrigin (reason.Source as IMemberDefinition ?? method));
			}

			if (CheckProcessed (method))
				return;

			_unreachableBlocksOptimizer.ProcessMethod (method);

			foreach (Action<MethodDefinition> handleMarkMethod in _markContext.MarkMethodActions)
				handleMarkMethod (method);

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
				_virtual_methods.Add ((method, _scopeStack.CurrentScope));

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

			if (method.DeclaringType.IsMulticastDelegate ()) {
				string methodPair = null;
				if (method.Name == "BeginInvoke")
					methodPair = "EndInvoke";
				else if (method.Name == "EndInvoke")
					methodPair = "BeginInvoke";

				if (methodPair != null) {
					TypeDefinition declaringType = method.DeclaringType;
					MarkMethodIf (declaringType.Methods, m => m.Name == methodPair, new DependencyInfo (DependencyKind.MethodForSpecialType, declaringType));
				}
			}

			DoAdditionalMethodProcessing (method);

			ApplyPreserveMethods (method);
		}

		// Allow subclassers to mark additional things when marking a method
		protected virtual void DoAdditionalMethodProcessing (MethodDefinition method)
		{
		}

		void MarkImplicitlyUsedFields (TypeDefinition type)
		{
			if (type?.HasFields != true)
				return;

			// keep fields for types with explicit layout and for enums
			if (!type.IsAutoLayout || type.IsEnum)
				MarkFields (type, includeStatic: type.IsEnum, reason: new DependencyInfo (DependencyKind.MemberOfType, type));
		}

		protected virtual void MarkRequirementsForInstantiatedTypes (TypeDefinition type)
		{
			if (Annotations.IsInstantiated (type))
				return;

			Annotations.MarkInstantiated (type);

			using var typeScope = _scopeStack.PushScope (new MessageOrigin (type));

			MarkInterfaceImplementations (type);

			foreach (var method in GetRequiredMethodsForInstantiatedType (type))
				MarkMethod (method, new DependencyInfo (DependencyKind.MethodForInstantiatedType, type));

			MarkImplicitlyUsedFields (type);

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
				if (IsVirtualNeededByInstantiatedTypeDueToPreservedScope (method))
					yield return method;
			}
		}

		void MarkExplicitInterfaceImplementation (MethodDefinition method, MethodReference ov)
		{
			MethodDefinition resolvedOverride = _context.Resolve (ov);

			if (resolvedOverride == null)
				return;

			if (resolvedOverride.DeclaringType.IsInterface) {
				foreach (var ifaceImpl in method.DeclaringType.Interfaces) {
					var resolvedInterfaceType = _context.Resolve (ifaceImpl.InterfaceType);
					if (resolvedInterfaceType == null) {
						continue;
					}

					if (resolvedInterfaceType == resolvedOverride.DeclaringType) {
						MarkInterfaceImplementation (ifaceImpl, new MessageOrigin (method.DeclaringType));
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

				var baseType = _context.Resolve (method.DeclaringType.BaseType);
				if (!MarkDefaultConstructor (baseType, new DependencyInfo (DependencyKind.BaseDefaultCtorForStubbedMethod, method)))
					throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Cannot stub constructor on '{method.DeclaringType}' when base type does not have default constructor",
						1006, origin: _scopeStack.CurrentScope.Origin));

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
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ("Missing predefined 'System.NotSupportedException' type", 1007));

			MarkType (nse, reason);

			var nseCtor = MarkMethodIf (nse.Methods, KnownMembers.IsNotSupportedExceptionCtorString, reason);
			_context.MarkedKnownMembers.NotSupportedExceptionCtorString = nseCtor ??
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Could not find constructor on '{nse.GetDisplayName ()}'", 1008));

			var objectType = BCL.FindPredefinedType ("System", "Object", _context);
			if (objectType == null)
				throw new NotSupportedException ("Missing predefined 'System.Object' type");

			MarkType (objectType, reason);

			var objectCtor = MarkMethodIf (objectType.Methods, MethodDefinitionExtensions.IsDefaultConstructor, reason);
			_context.MarkedKnownMembers.ObjectCtor = objectCtor ??
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Could not find constructor on '{objectType.GetDisplayName ()}'", 1008));
		}

		bool MarkDisablePrivateReflectionAttribute ()
		{
			if (_context.MarkedKnownMembers.DisablePrivateReflectionAttributeCtor != null)
				return false;

			var disablePrivateReflection = BCL.FindPredefinedType ("System.Runtime.CompilerServices", "DisablePrivateReflectionAttribute", _context);
			if (disablePrivateReflection == null)
				throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ("Missing predefined 'System.Runtime.CompilerServices.DisablePrivateReflectionAttribute' type", 1007));

			using (_scopeStack.PushScope (new MessageOrigin (null))) {
				MarkType (disablePrivateReflection, DependencyInfo.DisablePrivateReflectionRequirement);

				var ctor = MarkMethodIf (disablePrivateReflection.Methods, MethodDefinitionExtensions.IsDefaultConstructor, new DependencyInfo (DependencyKind.DisablePrivateReflectionRequirement, disablePrivateReflection));
				_context.MarkedKnownMembers.DisablePrivateReflectionAttributeCtor = ctor ??
					throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ($"Could not find constructor on '{disablePrivateReflection.GetDisplayName ()}'", 1010));
			}

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

		void ProcessInteropMethod (MethodDefinition method)
		{
			if (method.IsPInvokeImpl && method.PInvokeInfo != null) {
				var pii = method.PInvokeInfo;
				Annotations.MarkProcessed (pii.Module, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
				if (!string.IsNullOrEmpty (_context.PInvokesListFile)) {
					_context.PInvokes.Add (new PInvokeInfo {
						AssemblyName = method.DeclaringType.Module.Name,
						EntryPoint = pii.EntryPoint,
						FullName = method.FullName,
						ModuleName = pii.Module.Name
					});
				}
			}

			TypeDefinition returnTypeDefinition = _context.TryResolve (method.ReturnType);

			const bool includeStaticFields = false;
			if (returnTypeDefinition != null) {
				if (!returnTypeDefinition.IsImport) {
					// What we keep here is correct most of the time, but not every time. Fine for now.
					MarkDefaultConstructor (returnTypeDefinition, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
					MarkFields (returnTypeDefinition, includeStaticFields, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
				}
			}

			if (method.HasThis && !method.DeclaringType.IsImport) {
				// This is probably Mono-specific. One can't have InternalCall or P/invoke instance methods in CoreCLR or .NET.
				MarkFields (method.DeclaringType, includeStaticFields, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
			}

			foreach (ParameterDefinition pd in method.Parameters) {
				TypeReference paramTypeReference = pd.ParameterType;
				if (paramTypeReference is TypeSpecification) {
					paramTypeReference = (paramTypeReference as TypeSpecification).ElementType;
				}
				TypeDefinition paramTypeDefinition = _context.TryResolve (paramTypeReference);
				if (paramTypeDefinition != null) {
					if (!paramTypeDefinition.IsImport) {
						// What we keep here is correct most of the time, but not every time. Fine for now.
						MarkFields (paramTypeDefinition, includeStaticFields, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
						if (pd.ParameterType.IsByReference) {
							MarkDefaultConstructor (paramTypeDefinition, new DependencyInfo (DependencyKind.InteropMethodDependency, method));
						}
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
				AssemblyDefinition assembly = _context.Resolve (method.DeclaringType.Scope);
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

		protected internal void MarkProperty (PropertyDefinition prop, in DependencyInfo reason)
		{
			Tracer.AddDirectDependency (prop, reason, marked: false);

			using var propertyScope = _scopeStack.PushScope (new MessageOrigin (prop));

			// Consider making this more similar to MarkEvent method?
			MarkCustomAttributes (prop, new DependencyInfo (DependencyKind.CustomAttribute, prop));
			DoAdditionalPropertyProcessing (prop);
		}

		protected internal virtual void MarkEvent (EventDefinition evt, in DependencyInfo reason)
		{
			// Record the event without marking it in Annotations.
			Tracer.AddDirectDependency (evt, reason, marked: false);

			using var eventScope = _scopeStack.PushScope (new MessageOrigin (evt));

			MarkCustomAttributes (evt, new DependencyInfo (DependencyKind.CustomAttribute, evt));
			MarkMethodIfNotNull (evt.AddMethod, new DependencyInfo (DependencyKind.EventMethod, evt));
			MarkMethodIfNotNull (evt.InvokeMethod, new DependencyInfo (DependencyKind.EventMethod, evt));
			MarkMethodIfNotNull (evt.RemoveMethod, new DependencyInfo (DependencyKind.EventMethod, evt));
			DoAdditionalEventProcessing (evt);
		}

		internal void MarkMethodIfNotNull (MethodReference method, in DependencyInfo reason)
		{
			if (method == null)
				return;

			MarkMethod (method, reason);
		}

		protected virtual void MarkMethodBody (MethodBody body)
		{
			if (_context.IsOptimizationEnabled (CodeOptimizations.UnreachableBodies, body.Method) && IsUnreachableBody (body)) {
				MarkAndCacheConvertToThrowExceptionCtor (new DependencyInfo (DependencyKind.UnreachableBodyRequirement, body.Method));
				_unreachableBodies.Add ((body, _scopeStack.CurrentScope));
				return;
			}

			foreach (VariableDefinition var in body.Variables)
				MarkType (var.VariableType, new DependencyInfo (DependencyKind.VariableType, body.Method));

			foreach (ExceptionHandler eh in body.ExceptionHandlers)
				if (eh.HandlerType == ExceptionHandlerType.Catch)
					MarkType (eh.CatchType, new DependencyInfo (DependencyKind.CatchType, body.Method));

			bool requiresReflectionMethodBodyScanner =
				ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForMethodBody (_context.Annotations.FlowAnnotations, body.Method);
			foreach (Instruction instruction in body.Instructions)
				MarkInstruction (instruction, body.Method, ref requiresReflectionMethodBodyScanner);

			MarkInterfacesNeededByBodyStack (body);

			MarkReflectionLikeDependencies (body, requiresReflectionMethodBodyScanner);

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
			var implementations = new InterfacesOnStackScanner (_context).GetReferencedInterfaces (body);
			if (implementations == null)
				return;

			foreach (var (implementation, type) in implementations)
				MarkInterfaceImplementation (implementation, new MessageOrigin (type));
		}

		protected virtual void MarkInstruction (Instruction instruction, MethodDefinition method, ref bool requiresReflectionMethodBodyScanner)
		{
			switch (instruction.OpCode.OperandType) {
			case OperandType.InlineField:
				switch (instruction.OpCode.Code) {
				case Code.Stfld: // Field stores (Storing value to annotated field must be checked)
				case Code.Stsfld:
				case Code.Ldflda: // Field address loads (as those can be used to store values to annotated field and thus must be checked)
				case Code.Ldsflda:
					requiresReflectionMethodBodyScanner |=
						ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForAccess (_context, (FieldReference) instruction.Operand);
					break;

				default: // Other field operations are not interesting as they don't need to be checked
					break;
				}

				_scopeStack.UpdateCurrentScopeInstructionOffset (instruction.Offset);
				MarkField ((FieldReference) instruction.Operand, new DependencyInfo (DependencyKind.FieldAccess, method));
				break;

			case OperandType.InlineMethod: {
					DependencyKind dependencyKind = instruction.OpCode.Code switch {
						Code.Jmp => DependencyKind.DirectCall,
						Code.Call => DependencyKind.DirectCall,
						Code.Callvirt => DependencyKind.VirtualCall,
						Code.Newobj => DependencyKind.Newobj,
						Code.Ldvirtftn => DependencyKind.Ldvirtftn,
						Code.Ldftn => DependencyKind.Ldftn,
						_ => throw new InvalidOperationException ($"unexpected opcode {instruction.OpCode}")
					};

					requiresReflectionMethodBodyScanner |=
						ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForCallSite (_context, (MethodReference) instruction.Operand);

					_scopeStack.UpdateCurrentScopeInstructionOffset (instruction.Offset);
					MarkMethod ((MethodReference) instruction.Operand, new DependencyInfo (dependencyKind, method));
					break;
				}

			case OperandType.InlineTok: {
					object token = instruction.Operand;
					Debug.Assert (instruction.OpCode.Code == Code.Ldtoken);
					var reason = new DependencyInfo (DependencyKind.Ldtoken, method);
					_scopeStack.UpdateCurrentScopeInstructionOffset (instruction.Offset);

					if (token is TypeReference typeReference) {
						// Error will be reported as part of MarkType
						TypeDefinition type = _context.TryResolve (typeReference);
						MarkTypeVisibleToReflection (typeReference, type, reason);
					} else if (token is MethodReference methodReference) {
						MarkMethod (methodReference, reason);
					} else {
						MarkField ((FieldReference) token, reason);
					}
					break;
				}

			case OperandType.InlineType:
				var operand = (TypeReference) instruction.Operand;
				switch (instruction.OpCode.Code) {
				case Code.Newarr:
					Annotations.MarkRelevantToVariantCasting (_context.TryResolve (operand));
					break;
				case Code.Isinst:
					if (operand is TypeSpecification || operand is GenericParameter)
						break;

					if (!_context.CanApplyOptimization (CodeOptimizations.UnusedTypeChecks, method.DeclaringType.Module.Assembly))
						break;

					TypeDefinition type = _context.Resolve (operand);
					if (type == null)
						return;

					if (type.IsInterface)
						break;

					if (!Annotations.IsInstantiated (type)) {
						_pending_isinst_instr.Add ((type, method.Body, instruction));
						return;
					}

					break;
				}

				_scopeStack.UpdateCurrentScopeInstructionOffset (instruction.Offset);
				MarkType (operand, new DependencyInfo (DependencyKind.InstructionTypeRef, method));
				break;
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

		protected internal virtual void MarkInterfaceImplementation (InterfaceImplementation iface, MessageOrigin? origin = null, DependencyInfo? reason = null)
		{
			if (Annotations.IsMarked (iface))
				return;

			using var localScope = origin.HasValue ? _scopeStack.PushScope (origin.Value) : null;

			// Blame the type that has the interfaceimpl, expecting the type itself to get marked for other reasons.
			MarkCustomAttributes (iface, new DependencyInfo (DependencyKind.CustomAttribute, iface));
			// Blame the interface type on the interfaceimpl itself.
			MarkType (iface.InterfaceType, reason ?? new DependencyInfo (DependencyKind.InterfaceImplementationInterfaceType, iface));
			Annotations.MarkProcessed (iface, reason ?? new DependencyInfo (DependencyKind.InterfaceImplementationOnType, _scopeStack.CurrentScope.Origin.MemberDefinition));
		}

		//
		// Extension point for reflection logic handling customization
		//
		protected internal virtual bool ProcessReflectionDependency (MethodBody body, Instruction instruction)
		{
			return false;
		}

		//
		// Tries to mark additional dependencies used in reflection like calls (e.g. typeof (MyClass).GetField ("fname"))
		//
		protected virtual void MarkReflectionLikeDependencies (MethodBody body, bool requiresReflectionMethodBodyScanner)
		{
			if (requiresReflectionMethodBodyScanner) {
				var scanner = new ReflectionMethodBodyScanner (_context, this, _scopeStack);
				scanner.ScanAndProcessReturnValue (body);
			}
		}

		protected class AttributeProviderPair
		{
			public AttributeProviderPair (CustomAttribute attribute, ICustomAttributeProvider provider)
			{
				Attribute = attribute;
				Provider = provider;
			}

			public CustomAttribute Attribute { get; private set; }
			public ICustomAttributeProvider Provider { get; private set; }
		}
	}
}
