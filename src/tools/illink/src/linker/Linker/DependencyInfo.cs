// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker
{
	public enum DependencyKind
	{
		// For tracking any other kinds of dependencies in extensions to the core logic
		Custom = -1, // the source is reserved to carry any dependency information tracked by the extender

		// For use when we don't care about tracking a particular dependency
		Unspecified = 0, // currently unused

		// Entry points to the analysis
		AssemblyAction = 1, // assembly action -> entry assembly
		RootAssembly = 2, // assembly -> entry type
		XmlDescriptor = 3, // xml document -> entry member
						   // Attributes on assemblies are marked whether or not we keep
						   // the assembly, so mark these as entry points.
		AssemblyOrModuleAttribute = 4, // assembly/module -> entry attribute

		// Membership and containment relationships
		NestedType = 5, // parent type -> nested type
		MemberOfType = 6, // type -> member
		DeclaringType = 7, // member -> type
		FieldOnGenericInstance = 8, // fieldref on instantiated generic -> field on generic typedef
		MethodOnGenericInstance = 9, // methodref on instantiated generic -> method on generic typedef

		// Type relationships
		BaseType = 10, // type -> its base type
		FieldType = 11, // field -> its type
		ParameterType = 12, // method -> types of its parameters
		ReturnType = 13, // method -> type it returns
		VariableType = 14, // method -> types of its variables
		CatchType = 15, // method -> types of its exception handlers

		// Override relationships
		BaseMethod = 16, // override -> base method
		Override = 17, // base method -> override
		MethodImplOverride = 18, // method -> .override on the method
		VirtualNeededDueToPreservedScope = 19, // type -> virtuals kept because scope requires it
		MethodForInstantiatedType = 20, // type -> methods kept because type is instantiated or scope requires it
		BaseDefaultCtorForStubbedMethod = 21, // stubbed method -> default ctor of base type

		// Generic type relationships
		GenericArgumentType = 22, // generic instance -> argument type
		GenericParameterConstraintType = 23, // generic typedef/methoddef -> parameter constraint type
		DefaultCtorForNewConstrainedGenericArgument = 24, // generic instance -> argument ctor
		ElementType = 25, // generic type instantiation -> generic typedef
		ElementMethod = 26, // generic method instantiation -> generic methoddef
		ModifierType = 27, // modified type -> type modifier

		// Modules and assemblies
		ScopeOfType = 28, // type -> module/assembly
		AssemblyOfModule = 81, // module -> assembly
		TypeInAssembly = 29, // assembly -> type
		ModuleOfExportedType = 30, // exported type -> module
		ExportedType = 31, // type -> exported type

		// Grouping of property/event methods
		PropertyOfPropertyMethod = 32, // property method -> its property
		EventOfEventMethod = 33, // event method -> its event
		EventMethod = 34, // event -> its event methods
						  // PropertyMethod doesn't exist because property methods aren't always marked for a property

		// Interface implementations
		InterfaceImplementationInterfaceType = 35, // interfaceimpl -> interface type
		InterfaceImplementationOnType = 36, // type -> interfaceimpl on it

		// Interop methods
		ReturnTypeMarshalSpec = 37, // interop method -> marshal spec of its return type
		ParameterMarshalSpec = 38, // interop method -> marshal spec of its parameters
		FieldMarshalSpec = 39, // field -> its marshal spec
		InteropMethodDependency = 40, // interop method -> required members of its parameters, return type, declaring type

		// Dependencies created by instructions
		DirectCall = 41, // method -> method
		VirtualCall = 42, // method -> method
		Ldvirtftn = 43, // method -> method
		Ldftn = 44, // method -> method
		Newobj = 45, // method -> method
		Ldtoken = 46, // method -> member referenced
		FieldAccess = 47, // method -> field (for instructions that load/store fields)
		InstructionTypeRef = 48, // other instructions that have an inline type token (method -> type)

		// Custom attributes on various providers
		CustomAttribute = 49, // attribute provider (type/field/method/etc...) -> attribute on it
		ParameterAttribute = 50, // method parameter -> attribute on it
		ReturnTypeAttribute = 51, // method return type -> attribute on it
		GenericParameterCustomAttribute = 52, // generic parameter -> attribute on it
		GenericParameterConstraintCustomAttribute = 53, // generic parameter constraint -> attribute on it

		// Dependencies of custom attributes
		AttributeConstructor = 54, // attribute -> its ctor
								   // used for security attributes, where we mark the type/properties directly
		AttributeType = 55, // attribute -> attribute type
		AttributeProperty = 56, // attribute -> attribute property
		CustomAttributeArgumentType = 57, // attribute -> type of an argument to the attribute ctor
		CustomAttributeArgumentValue = 58, // attribute -> type passed as an argument to the attribute ctor
		CustomAttributeField = 59, // attribute -> field on the attribute

		// Tracking cctors
		TriggersCctorThroughFieldAccess = 60, // field-accessing method -> cctor of field's declaring type
		TriggersCctorForCalledMethod = 61, // caller method -> callee method type cctor
		DeclaringTypeOfCalledMethod = 62, // called method -> its declaring type (used to track when a cctor is triggered by a method call)
		CctorForType = 63, // type -> cctor of type
		CctorForField = 64, // field -> cctor of field's declaring type

		// Tracking instantiations
		InstantiatedByCtor = 65, // ctor -> its declaring type (indicating that it was marked instantiated due to the ctor)
		OverrideOnInstantiatedType = 66, // instantiated type -> override method on the type

		// Trimming-specific behavior (preservation hints, patterns, user inputs, trimming outputs, etc.)
		DynamicDependency = 67, // DynamicDependency attribute -> member
		PreservedDependency = 67, // PreserveDependency attribute -> member
		AccessedViaReflection = 68, // method -> detected member accessed via reflection from that method
		PreservedMethod = 69, // type/method -> preserved method (explicitly preserved in Annotations by XML or other steps)
		TypePreserve = 70, // type -> field/method preserved for the type (explicitly set in Annotations by XML or other steps)
		DisablePrivateReflection = 71, // type/method -> DisablePrivateReflection attribute added by trimming
		DynamicallyAccessedMember = 72, // DynamicallyAccessedMember attribute -> member

		// Built-in knowledge of special runtime/diagnostic subsystems
		// XmlSchemaProvider, DebuggerDisplay, DebuggerTypeProxy, SoapHeader, TypeDescriptionProvider
		ReferencedBySpecialAttribute = 73, // attribute -> referenced members
		KeptForSpecialAttribute = 74, // attribute -> kept members (used when the members are not explicitly referenced)
		SerializationMethodForType = 75, // type -> method required for serialization
		EventSourceProviderField = 76, // EventSource derived type -> fields on nested Keywords/Tasks/Opcodes provider classes
		MethodForSpecialType = 77, // type -> methods kept (currently used for MulticastDelegate)

		// Trimming internals, requirements for certain optimizations
		UnreachableBodyRequirement = 78, // method -> well-known type required for unreachable bodies optimization
		DisablePrivateReflectionRequirement = 79, // null -> DisablePrivateReflectionAttribute type/methods (note that no specific source is reported)
		DynamicInterfaceCastableImplementation = 80, // type -> type is marked with IDynamicInterfaceCastableImplementationAttribute and implements the provided interface
		AlreadyMarked = 82, // null -> member that has already been marked for a particular reason (used to propagate reasons internally, not reported)

		DataContractSerialized = 83, // entry type or member for DataContract serialization
		XmlSerialized = 84, // entry type or member for XML serialization
		SerializedRecursiveType = 85, // recursive type kept due to serialization handling
		SerializedMember = 86, // field or property kept on a type for serialization

		PreservedOperator = 87, // operator method preserved on a type

		DynamicallyAccessedMemberOnType = 88, // type with DynamicallyAccessedMembers annotations (including those inherited from base types and interfaces)

		UnsafeAccessorTarget = 89, // the member is referenced via UnsafeAccessor attribute
	}

	public readonly struct DependencyInfo : IEquatable<DependencyInfo>
	{
		public DependencyKind Kind { get; }
		public object? Source { get; }
		public DependencyInfo (DependencyKind kind, object? source) => (Kind, Source) = (kind, source);
		public static readonly DependencyInfo Unspecified = new DependencyInfo (DependencyKind.Unspecified, null);
		public static readonly DependencyInfo AlreadyMarked = new DependencyInfo (DependencyKind.AlreadyMarked, null);
		public static readonly DependencyInfo DisablePrivateReflectionRequirement = new DependencyInfo (DependencyKind.DisablePrivateReflectionRequirement, null);
		public bool Equals (DependencyInfo other) => (Kind, Source) == (other.Kind, other.Source);
		public override bool Equals (object? obj) => obj is DependencyInfo info && this.Equals (info);
		public override int GetHashCode () => (Kind, Source).GetHashCode ();
		public static bool operator == (DependencyInfo lhs, DependencyInfo rhs) => lhs.Equals (rhs);
		public static bool operator != (DependencyInfo lhs, DependencyInfo rhs) => !lhs.Equals (rhs);
	}
}
