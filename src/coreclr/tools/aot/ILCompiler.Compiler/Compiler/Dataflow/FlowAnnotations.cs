// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

using ILCompiler;
using ILCompiler.Dataflow;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;
using WellKnownType = Internal.TypeSystem.WellKnownType;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// Caches dataflow annotations for type members.
    /// </summary>
    public sealed partial class FlowAnnotations
    {
        private readonly TypeAnnotationsHashtable _hashtable;
        private readonly Logger _logger;

        public ILProvider ILProvider { get; }
        public CompilerGeneratedState CompilerGeneratedState { get; }

        public FlowAnnotations(Logger logger, ILProvider ilProvider, CompilerGeneratedState compilerGeneratedState)
        {
            _hashtable = new TypeAnnotationsHashtable(logger, ilProvider, compilerGeneratedState);
            _logger = logger;
            ILProvider = ilProvider;
            CompilerGeneratedState = compilerGeneratedState;
        }

        /// <summary>
        /// Determines if the method has any annotations on its parameters or return values.
        /// </summary>
        public bool RequiresDataflowAnalysisDueToSignature(MethodDesc method)
        {
            try
            {
                method = method.GetTypicalMethodDefinition();
                TypeAnnotations typeAnnotations = GetAnnotations(method.OwningType);
                // This will return true even if the method has annotations on generic parameters,
                // but the callers don't rely on that - it's just easier to leave it this way (and it makes very little difference)
                return typeAnnotations.TryGetAnnotation(method, out _);
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public bool RequiresVirtualMethodDataflowAnalysis(MethodDesc method)
        {
            try
            {
                method = method.GetTypicalMethodDefinition();
                return GetAnnotations(method.OwningType).TryGetAnnotation(method, out _);
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if the field has any annotations on itself (not looking at owning type).
        /// </summary>
        public bool RequiresDataflowAnalysisDueToSignature(FieldDesc field)
        {
            try
            {
                field = field.GetTypicalFieldDefinition();
                TypeAnnotations typeAnnotations = GetAnnotations(field.OwningType);
                return typeAnnotations.TryGetAnnotation(field, out _);
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public bool HasGenericParameterAnnotation(TypeDesc type)
        {
            try
            {
                return GetAnnotations(type.GetTypeDefinition()).HasGenericParameterAnnotation();
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public bool HasGenericParameterAnnotation(MethodDesc method)
        {
            try
            {
                method = method.GetTypicalMethodDefinition();
                return GetAnnotations(method.OwningType).TryGetAnnotation(method, out var annotation) && annotation.GenericParameterAnnotations != null;
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        internal DynamicallyAccessedMemberTypes GetParameterAnnotation(ParameterProxy param)
        {
            MethodDesc method = param.Method.Method.GetTypicalMethodDefinition();

            if (GetAnnotations(method.OwningType).TryGetAnnotation(method, out var annotation) && annotation.ParameterAnnotations != null)
            {
                return annotation.ParameterAnnotations[(int)param.Index];
            }

            return DynamicallyAccessedMemberTypes.None;
        }

        public DynamicallyAccessedMemberTypes GetReturnParameterAnnotation(MethodDesc method)
        {
            method = method.GetTypicalMethodDefinition();

            if (GetAnnotations(method.OwningType).TryGetAnnotation(method, out var annotation))
            {
                return annotation.ReturnParameterAnnotation;
            }

            return DynamicallyAccessedMemberTypes.None;
        }

        public DynamicallyAccessedMemberTypes GetFieldAnnotation(FieldDesc field)
        {
            field = field.GetTypicalFieldDefinition();

            if (GetAnnotations(field.OwningType).TryGetAnnotation(field, out var annotation))
            {
                return annotation.Annotation;
            }

            return DynamicallyAccessedMemberTypes.None;
        }

        public DynamicallyAccessedMemberTypes GetTypeAnnotation(TypeDesc type)
        {
            return GetAnnotations(type.GetTypeDefinition()).TypeAnnotation;
        }

        public bool ShouldWarnWhenAccessedForReflection(TypeSystemEntity entity) =>
            entity switch
            {
                MethodDesc method => ShouldWarnWhenAccessedForReflection(method),
                FieldDesc field => ShouldWarnWhenAccessedForReflection(field),
                _ => false
            };

        public DynamicallyAccessedMemberTypes GetGenericParameterAnnotation(GenericParameterDesc genericParameter)
        {
            if (genericParameter is not EcmaGenericParameter ecmaGenericParameter)
                return DynamicallyAccessedMemberTypes.None;

            GenericParameter paramDef = ecmaGenericParameter.MetadataReader.GetGenericParameter(ecmaGenericParameter.Handle);

            if (ecmaGenericParameter.Kind == GenericParameterKind.Type)
            {
                TypeDesc parent = ecmaGenericParameter.Module.GetType(paramDef.Parent);
                if (GetAnnotations(parent).TryGetAnnotation(ecmaGenericParameter, out var annotation))
                {
                    return annotation;
                }
            }
            else
            {
                Debug.Assert(ecmaGenericParameter.Kind == GenericParameterKind.Method);
                MethodDesc parent = ecmaGenericParameter.Module.GetMethod(paramDef.Parent);
                if (GetAnnotations(parent.OwningType).TryGetAnnotation(parent, out var methodAnnotation)
                    && methodAnnotation.TryGetAnnotation(genericParameter, out var annotation))
                {
                    return annotation;
                }
            }

            return DynamicallyAccessedMemberTypes.None;
        }

        public bool ShouldWarnWhenAccessedForReflection(MethodDesc method)
        {
            method = method.GetTypicalMethodDefinition();

            if (!GetAnnotations(method.OwningType).TryGetAnnotation(method, out var annotation))
                return false;

            if (annotation.ParameterAnnotations == null && annotation.ReturnParameterAnnotation == DynamicallyAccessedMemberTypes.None)
                return false;

            // If the method only has annotation on the return value and it's not virtual avoid warning.
            // Return value annotations are "consumed" by the caller of a method, and as such there is nothing
            // wrong calling these dynamically. The only problem can happen if something overrides a virtual
            // method with annotated return value at runtime - in this case the trimmer can't validate
            // that the method will return only types which fulfill the annotation's requirements.
            // For example:
            //   class BaseWithAnnotation
            //   {
            //       [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            //       public abstract Type GetTypeWithFields();
            //   }
            //
            //   class UsingTheBase
            //   {
            //       public void PrintFields(Base base)
            //       {
            //            // No warning here - GetTypeWithFields is correctly annotated to allow GetFields on the return value.
            //            Console.WriteLine(string.Join(" ", base.GetTypeWithFields().GetFields().Select(f => f.Name)));
            //       }
            //   }
            //
            // If at runtime (through ref emit) something generates code like this:
            //   class DerivedAtRuntimeFromBase
            //   {
            //       // No point in adding annotation on the return value - nothing will look at it anyway
            //       // ILLink will not see this code, so there are no checks
            //       public override Type GetTypeWithFields() { return typeof(TestType); }
            //   }
            //
            // If TestType from above is trimmed, it may note have all its fields, and there would be no warnings generated.
            // But there has to be code like this somewhere in the app, in order to generate the override:
            //   class RuntimeTypeGenerator
            //   {
            //       public MethodInfo GetBaseMethod()
            //       {
            //            // This must warn - that the GetTypeWithFields has annotation on the return value
            //            return typeof(BaseWithAnnotation).GetMethod("GetTypeWithFields");
            //       }
            //   }
            return method.IsVirtual || annotation.ParameterAnnotations != null;
        }

        public bool ShouldWarnWhenAccessedForReflection(FieldDesc field)
        {
            field = field.GetTypicalFieldDefinition();
            return GetAnnotations(field.OwningType).TryGetAnnotation(field, out _);
        }

        public static bool IsTypeInterestingForDataflow(TypeDesc type)
        {
            // NOTE: this method is not particulary fast. It's assumed that the caller limits
            // calls to this method as much as possible.

            if (type.IsWellKnownType(WellKnownType.String))
                return true;

            // ByRef over an interesting type is itself interesting
            if (type is ByRefType byRefType)
                type = byRefType.ParameterType;

            if (!type.IsDefType)
                return false;

            var metadataType = (MetadataType)type;

            foreach (var intf in type.RuntimeInterfaces)
            {
                if (intf.Name == "IReflect" && intf.Namespace == "System.Reflection")
                    return true;
            }

            if (metadataType.Name == "IReflect" && metadataType.Namespace == "System.Reflection")
                return true;

            do
            {
                if (metadataType.Name == "Type" && metadataType.Namespace == "System")
                    return true;
            } while ((metadataType = metadataType.MetadataBaseType) != null);

            return false;
        }

        private TypeAnnotations GetAnnotations(TypeDesc type)
        {
            return _hashtable.GetOrCreateValue(type);
        }

        private sealed class TypeAnnotationsHashtable : LockFreeReaderHashtable<TypeDesc, TypeAnnotations>
        {
            private readonly ILProvider _ilProvider;
            private readonly Logger _logger;
            private readonly CompilerGeneratedState _compilerGeneratedState;

            public TypeAnnotationsHashtable(Logger logger, ILProvider ilProvider, CompilerGeneratedState compilerGeneratedState) => (_logger, _ilProvider, _compilerGeneratedState) = (logger, ilProvider, compilerGeneratedState);

            private static DynamicallyAccessedMemberTypes GetMemberTypesForDynamicallyAccessedMembersAttribute(MetadataReader reader, CustomAttributeHandleCollection customAttributeHandles)
            {
                CustomAttributeHandle ca = reader.GetCustomAttributeHandle(customAttributeHandles, "System.Diagnostics.CodeAnalysis", "DynamicallyAccessedMembersAttribute");
                if (ca.IsNil)
                    return DynamicallyAccessedMemberTypes.None;

                BlobReader blobReader = reader.GetBlobReader(reader.GetCustomAttribute(ca).Value);
                Debug.Assert(blobReader.Length == 8);
                if (blobReader.Length != 8)
                    return DynamicallyAccessedMemberTypes.None;

                blobReader.ReadUInt16(); // Prolog
                return (DynamicallyAccessedMemberTypes)blobReader.ReadUInt32();
            }

            private static DynamicallyAccessedMemberTypes GetMemberTypesForConstraints(GenericParameterDesc genericParameter)
                => genericParameter.HasDefaultConstructorConstraint ?
                    DynamicallyAccessedMemberTypes.PublicParameterlessConstructor :
                    DynamicallyAccessedMemberTypes.None;

            protected override bool CompareKeyToValue(TypeDesc key, TypeAnnotations value) => key == value.Type;
            protected override bool CompareValueToValue(TypeAnnotations value1, TypeAnnotations value2) => value1.Type == value2.Type;
            protected override int GetKeyHashCode(TypeDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(TypeAnnotations value) => value.Type.GetHashCode();

            protected override TypeAnnotations CreateValueFromKey(TypeDesc key)
            {
                // We scan the entire type at this point; the reason for doing that is properties.
                //
                // We allow annotating properties, but those annotations need to flow onto individual get/set methods
                // and backing fields. Without scanning all properties, we can't answer questions about fields/methods.
                // And if we're going over all properties, we might as well go over everything else to keep things simple.

                Debug.Assert(key.IsTypeDefinition);
                if (key is not EcmaType ecmaType)
                    return new TypeAnnotations(key, DynamicallyAccessedMemberTypes.None, null, null, null);

                MetadataReader reader = ecmaType.MetadataReader;

                // class, interface, struct can have annotations
                TypeDefinition typeDef = reader.GetTypeDefinition(ecmaType.Handle);
                DynamicallyAccessedMemberTypes typeAnnotation = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, typeDef.GetCustomAttributes());

                try
                {
                    // Also inherit annotation from bases
                    DefType baseType = key.BaseType;
                    while (baseType != null)
                    {
                        var ecmaBaseType = (EcmaType)baseType.GetTypeDefinition();
                        TypeDefinition baseTypeDef = ecmaBaseType.MetadataReader.GetTypeDefinition(ecmaBaseType.Handle);
                        typeAnnotation |= GetMemberTypesForDynamicallyAccessedMembersAttribute(ecmaBaseType.MetadataReader, baseTypeDef.GetCustomAttributes());
                        baseType = baseType.BaseType;
                    }

                    // And inherit them from interfaces
                    foreach (DefType runtimeInterface in key.RuntimeInterfaces)
                    {
                        var ecmaInterface = (EcmaType)runtimeInterface.GetTypeDefinition();
                        TypeDefinition interfaceTypeDef = ecmaInterface.MetadataReader.GetTypeDefinition(ecmaInterface.Handle);
                        typeAnnotation |= GetMemberTypesForDynamicallyAccessedMembersAttribute(ecmaInterface.MetadataReader, interfaceTypeDef.GetCustomAttributes());
                    }
                }
                catch (TypeSystemException)
                {
                    // If the class hierarchy is not walkable, just stop collecting the annotations.
                }

                var annotatedFields = default(ArrayBuilder<FieldAnnotation>);

                // First go over all fields with an explicit annotation
                foreach (EcmaField field in ecmaType.GetFields())
                {
                    FieldDefinition fieldDef = reader.GetFieldDefinition(field.Handle);
                    DynamicallyAccessedMemberTypes annotation =
                        GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, fieldDef.GetCustomAttributes());
                    if (annotation == DynamicallyAccessedMemberTypes.None)
                    {
                        continue;
                    }

                    if (!IsTypeInterestingForDataflow(field.FieldType))
                    {
                        // Already know that there's a non-empty annotation on a field which is not System.Type/String and we're about to ignore it
                        _logger.LogWarning(field, DiagnosticId.DynamicallyAccessedMembersOnFieldCanOnlyApplyToTypesOrStrings, field.GetDisplayName());
                        continue;
                    }

                    annotatedFields.Add(new FieldAnnotation(field, annotation));
                }

                var annotatedMethods = new List<MethodAnnotations>();

                // Next go over all methods with an explicit annotation
                foreach (EcmaMethod method in ecmaType.GetMethods())
                {
                    DynamicallyAccessedMemberTypes[]? paramAnnotations = null;

                    DynamicallyAccessedMemberTypes methodMemberTypes = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, reader.GetMethodDefinition(method.Handle).GetCustomAttributes());

                    MethodSignature signature;
                    try
                    {
                        signature = method.Signature;
                    }
                    catch (TypeSystemException)
                    {
                        // If we cannot resolve things in the signature, just move along.
                        continue;
                    }

                    // If there's an annotation on the method itself and it's one of the special types (System.Type for example)
                    // treat that annotation as annotating the "this" parameter.
                    if (methodMemberTypes != DynamicallyAccessedMemberTypes.None)
                    {
                        if (IsTypeInterestingForDataflow(method.OwningType) && !signature.IsStatic)
                        {
                            paramAnnotations = new DynamicallyAccessedMemberTypes[method.GetParametersCount()];
                            paramAnnotations[0] = methodMemberTypes;
                        }
                        else
                        {
                            _logger.LogWarning(method, DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods);
                        }
                    }

                    MethodDefinition methodDef = reader.GetMethodDefinition(method.Handle);
                    ParameterHandleCollection parameterHandles = methodDef.GetParameters();

                    DynamicallyAccessedMemberTypes returnAnnotation = DynamicallyAccessedMemberTypes.None;

                    foreach (ParameterHandle parameterHandle in parameterHandles)
                    {
                        Parameter parameter = reader.GetParameter(parameterHandle);

                        if (parameter.SequenceNumber == 0)
                        {
                            // this is the return parameter
                            returnAnnotation = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, parameter.GetCustomAttributes());
                            if (returnAnnotation != DynamicallyAccessedMemberTypes.None && !IsTypeInterestingForDataflow(signature.ReturnType))
                            {
                                _logger.LogWarning(method, DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings, method.GetDisplayName());
                            }
                        }
                        else
                        {
                            DynamicallyAccessedMemberTypes pa = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, parameter.GetCustomAttributes());
                            if (pa == DynamicallyAccessedMemberTypes.None)
                                continue;

                            if (!IsTypeInterestingForDataflow(signature[parameter.SequenceNumber - 1]))
                            {
                                _logger.LogWarning(method, DiagnosticId.DynamicallyAccessedMembersOnMethodParameterCanOnlyApplyToTypesOrStrings, DiagnosticUtilities.GetParameterNameForErrorMessage(method, parameter.SequenceNumber - 1), method.GetDisplayName());
                                continue;
                            }

                            paramAnnotations ??= new DynamicallyAccessedMemberTypes[method.GetParametersCount()];
                            paramAnnotations[parameter.SequenceNumber - 1 + (signature.IsStatic ? 0 : 1)] = pa;
                        }
                    }

                    DynamicallyAccessedMemberTypes[]? genericParameterAnnotations = null;
                    foreach (EcmaGenericParameter genericParameter in method.Instantiation)
                    {
                        GenericParameter genericParameterDef = reader.GetGenericParameter(genericParameter.Handle);
                        var annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, genericParameterDef.GetCustomAttributes());
                        annotation |= GetMemberTypesForConstraints(genericParameter);
                        if (annotation != DynamicallyAccessedMemberTypes.None)
                        {
                            genericParameterAnnotations ??= new DynamicallyAccessedMemberTypes[method.Instantiation.Length];
                            genericParameterAnnotations[genericParameter.Index] = annotation;
                        }
                    }

                    if (returnAnnotation != DynamicallyAccessedMemberTypes.None || paramAnnotations != null || genericParameterAnnotations != null)
                    {
                        annotatedMethods.Add(new MethodAnnotations(method, paramAnnotations, returnAnnotation, genericParameterAnnotations));
                    }
                }

                // Next up are properties. Annotations on properties are kind of meta because we need to
                // map them to annotations on methods/fields. They're syntactic sugar - what they do is expressible
                // by placing attribute on the accessor/backing field. For complex properties, that's what people
                // will need to do anyway. Like so:
                //
                // [field: Attribute]
                // Type MyProperty
                // {
                //     [return: Attribute]
                //     get;
                //     [value: Attribute]
                //     set;
                //  }

                foreach (PropertyDefinitionHandle propertyHandle in reader.GetTypeDefinition(ecmaType.Handle).GetProperties())
                {
                    DynamicallyAccessedMemberTypes annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute(
                        reader, reader.GetPropertyDefinition(propertyHandle).GetCustomAttributes());
                    if (annotation == DynamicallyAccessedMemberTypes.None)
                        continue;

                    PropertyPseudoDesc property = new PropertyPseudoDesc(ecmaType, propertyHandle);

                    if (!IsTypeInterestingForDataflow(property.Signature.ReturnType))
                    {
                        _logger.LogWarning(property, DiagnosticId.DynamicallyAccessedMembersOnPropertyCanOnlyApplyToTypesOrStrings, property.GetDisplayName());
                        continue;
                    }

                    FieldDesc? backingFieldFromSetter = null;

                    // Propagate the annotation to the setter method
                    MethodDesc setMethod = property.SetMethod;
                    if (setMethod != null)
                    {
                        // Abstract property backing field propagation doesn't make sense, and any derived property will be validated
                        // to have the exact same annotations on getter/setter, and thus if it has a detectable backing field that will be validated as well.
                        MethodIL methodBody = _ilProvider.GetMethodIL(setMethod);
                        if (methodBody != null)
                        {
                            // Look for the compiler generated backing field. If it doesn't work out simply move on. In such case we would still
                            // propagate the annotation to the setter/getter and later on when analyzing the setter/getter we will warn
                            // that the field (which ever it is) must be annotated as well.
                            ScanMethodBodyForFieldAccess(methodBody, write: true, out backingFieldFromSetter);
                        }

                        MethodAnnotations? setterAnnotation = null;
                        foreach (var annotatedMethod in annotatedMethods)
                        {
                            if (annotatedMethod.Method == setMethod)
                                setterAnnotation = annotatedMethod;
                        }

                        // If 'value' parameter is annotated, then warn. Other parameters can be annotated for indexable properties
                        if (setterAnnotation?.ParameterAnnotations?[^1] is not (null or DynamicallyAccessedMemberTypes.None))
                        {
                            _logger.LogWarning(setMethod, DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor, property.GetDisplayName(), setMethod.GetDisplayName());
                        }
                        else
                        {
                            if (setterAnnotation is not null)
                                annotatedMethods.Remove(setterAnnotation.Value);

                            DynamicallyAccessedMemberTypes[] paramAnnotations;
                            if (setterAnnotation?.ParameterAnnotations is null)
                                paramAnnotations = new DynamicallyAccessedMemberTypes[setMethod.GetParametersCount()];
                            else
                                paramAnnotations = setterAnnotation.Value.ParameterAnnotations;

                            paramAnnotations[paramAnnotations.Length - 1] = annotation;
                            annotatedMethods.Add(new MethodAnnotations(setMethod, paramAnnotations, DynamicallyAccessedMemberTypes.None, null));
                        }
                    }

                    FieldDesc? backingFieldFromGetter = null;

                    // Propagate the annotation to the getter method
                    MethodDesc getMethod = property.GetMethod;
                    if (getMethod != null)
                    {

                        // Abstract property backing field propagation doesn't make sense, and any derived property will be validated
                        // to have the exact same annotations on getter/setter, and thus if it has a detectable backing field that will be validated as well.
                        MethodIL methodBody = _ilProvider.GetMethodIL(getMethod);
                        if (methodBody != null)
                        {
                            // Look for the compiler generated backing field. If it doesn't work out simply move on. In such case we would still
                            // propagate the annotation to the setter/getter and later on when analyzing the setter/getter we will warn
                            // that the field (which ever it is) must be annotated as well.
                            ScanMethodBodyForFieldAccess(methodBody, write: false, out backingFieldFromGetter);
                        }

                        MethodAnnotations? getterAnnotation = null;
                        foreach (var annotatedMethod in annotatedMethods)
                        {
                            if (annotatedMethod.Method == getMethod)
                                getterAnnotation = annotatedMethod;
                        }

                        // If return value is annotated, then warn. Otherwise, parameters can be annotated for indexable properties
                        if (getterAnnotation?.ReturnParameterAnnotation is not (null or DynamicallyAccessedMemberTypes.None))
                        {
                            _logger.LogWarning(getMethod, DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor, property.GetDisplayName(), getMethod.GetDisplayName());
                        }
                        else
                        {
                            if (getterAnnotation is not null)
                                annotatedMethods.Remove(getterAnnotation.Value);

                            annotatedMethods.Add(new MethodAnnotations(getMethod, getterAnnotation?.ParameterAnnotations, annotation, null));
                        }
                    }

                    FieldDesc? backingField;
                    if (backingFieldFromGetter != null && backingFieldFromSetter != null &&
                        backingFieldFromGetter != backingFieldFromSetter)
                    {
                        _logger.LogWarning(property, DiagnosticId.DynamicallyAccessedMembersCouldNotFindBackingField, property.GetDisplayName());
                        backingField = null;
                    }
                    else
                    {
                        backingField = backingFieldFromGetter ?? backingFieldFromSetter;
                    }

                    if (backingField != null)
                    {
                        if (annotatedFields.Any(a => a.Field == backingField))
                        {
                            _logger.LogWarning(backingField, DiagnosticId.DynamicallyAccessedMembersOnPropertyConflictsWithBackingField, property.GetDisplayName(), backingField.GetDisplayName());
                        }
                        else
                        {
                            annotatedFields.Add(new FieldAnnotation(backingField, annotation));
                        }
                    }
                }

                DynamicallyAccessedMemberTypes[]? typeGenericParameterAnnotations = null;
                if (ecmaType.Instantiation.Length > 0)
                {
                    var attrs = GetGeneratedTypeAttributes(ecmaType);
                    for (int genericParameterIndex = 0; genericParameterIndex < ecmaType.Instantiation.Length; genericParameterIndex++)
                    {
                        EcmaGenericParameter genericParameter = (EcmaGenericParameter)ecmaType.Instantiation[genericParameterIndex];
                        genericParameter = (attrs?[genericParameterIndex] as EcmaGenericParameter) ?? genericParameter;
                        GenericParameter genericParameterDef = reader.GetGenericParameter(genericParameter.Handle);
                        var annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, genericParameterDef.GetCustomAttributes());
                        annotation |= GetMemberTypesForConstraints(genericParameter);
                        if (annotation != DynamicallyAccessedMemberTypes.None)
                        {
                            typeGenericParameterAnnotations ??= new DynamicallyAccessedMemberTypes[ecmaType.Instantiation.Length];
                            typeGenericParameterAnnotations[genericParameterIndex] = annotation;
                        }
                    }
                }

                return new TypeAnnotations(ecmaType, typeAnnotation, annotatedMethods.ToArray(), annotatedFields.ToArray(), typeGenericParameterAnnotations);
            }

            private IReadOnlyList<GenericParameterDesc?>? GetGeneratedTypeAttributes(EcmaType typeDef)
            {
                if (!CompilerGeneratedNames.IsGeneratedType(typeDef.Name))
                {
                    return null;
                }
                var attrs = _compilerGeneratedState.GetGeneratedTypeAttributes(typeDef);
                Debug.Assert(attrs is null || attrs.Count == typeDef.Instantiation.Length);
                return attrs;
            }

            private static bool ScanMethodBodyForFieldAccess(MethodIL body, bool write, out FieldDesc? found)
            {
                // Tries to find the backing field for a property getter/setter.
                // Returns true if this is a method body that we can unambiguously analyze.
                // The found field could still be null if there's no backing store.
                found = null;

                ILReader ilReader = new ILReader(body.GetILBytes());

                while (ilReader.HasNext)
                {
                    ILOpcode opcode = ilReader.ReadILOpcode();
                    switch (opcode)
                    {
                        case ILOpcode.ldsfld when !write:
                        case ILOpcode.ldfld when !write:
                        case ILOpcode.stsfld when write:
                        case ILOpcode.stfld when write:
                            {
                                // This writes/reads multiple fields - can't guess which one is the backing store.
                                // Return failure.
                                if (found != null)
                                {
                                    found = null;
                                    return false;
                                }
                                found = (FieldDesc)body.GetObject(ilReader.ReadILToken());
                            }
                            break;
                        default:
                            ilReader.Skip(opcode);
                            break;
                    }
                }

                if (found == null)
                {
                    // Doesn't access any fields. Could be e.g. "Type Foo => typeof(Bar);"
                    // Return success.
                    return true;
                }

                if (found.OwningType != body.OwningMethod.OwningType ||
                    found.IsStatic != body.OwningMethod.Signature.IsStatic ||
                    !found.HasCustomAttribute("System.Runtime.CompilerServices", "CompilerGeneratedAttribute"))
                {
                    // A couple heuristics to make sure we got the right field.
                    // Return failure.
                    found = null;
                    return false;
                }

                return true;
            }
        }

        internal void ValidateMethodAnnotationsAreSame(MethodDesc method, MethodDesc baseMethod)
        {
            method = method.GetTypicalMethodDefinition();
            baseMethod = baseMethod.GetTypicalMethodDefinition();

            GetAnnotations(method.OwningType).TryGetAnnotation(method, out var methodAnnotations);
            GetAnnotations(baseMethod.OwningType).TryGetAnnotation(baseMethod, out var baseMethodAnnotations);

            if (methodAnnotations.ReturnParameterAnnotation != baseMethodAnnotations.ReturnParameterAnnotation)
                LogValidationWarning(method.Signature.ReturnType, baseMethod, method);

            if (methodAnnotations.ParameterAnnotations != null || baseMethodAnnotations.ParameterAnnotations != null)
            {
                if (methodAnnotations.ParameterAnnotations == null)
                    ValidateMethodParametersHaveNoAnnotations(baseMethodAnnotations.ParameterAnnotations!, method, baseMethod, method);
                else if (baseMethodAnnotations.ParameterAnnotations == null)
                    ValidateMethodParametersHaveNoAnnotations(methodAnnotations.ParameterAnnotations, method, baseMethod, method);
                else
                {
                    if (methodAnnotations.ParameterAnnotations.Length != baseMethodAnnotations.ParameterAnnotations.Length)
                        return;

                    for (int parameterIndex = 0; parameterIndex < methodAnnotations.ParameterAnnotations.Length; parameterIndex++)
                    {
                        if (methodAnnotations.ParameterAnnotations[parameterIndex] != baseMethodAnnotations.ParameterAnnotations[parameterIndex])
                            LogValidationWarning(
                                (new MethodProxy(method)).GetParameter((ParameterIndex)parameterIndex),
                                (new MethodProxy(baseMethod)).GetParameter((ParameterIndex)parameterIndex),
                                method);
                    }
                }
            }

            if (methodAnnotations.GenericParameterAnnotations != null || baseMethodAnnotations.GenericParameterAnnotations != null)
            {
                if (methodAnnotations.GenericParameterAnnotations == null)
                    ValidateMethodGenericParametersHaveNoAnnotations(baseMethodAnnotations.GenericParameterAnnotations!, method, baseMethod, method);
                else if (baseMethodAnnotations.GenericParameterAnnotations == null)
                    ValidateMethodGenericParametersHaveNoAnnotations(methodAnnotations.GenericParameterAnnotations, method, baseMethod, method);
                else
                {
                    if (methodAnnotations.GenericParameterAnnotations.Length != baseMethodAnnotations.GenericParameterAnnotations.Length)
                        return;

                    for (int genericParameterIndex = 0; genericParameterIndex < methodAnnotations.GenericParameterAnnotations.Length; genericParameterIndex++)
                    {
                        if (methodAnnotations.GenericParameterAnnotations[genericParameterIndex] != baseMethodAnnotations.GenericParameterAnnotations[genericParameterIndex])
                        {
                            LogValidationWarning(
                                method.Instantiation[genericParameterIndex],
                                baseMethod.Instantiation[genericParameterIndex],
                                method);
                        }
                    }
                }
            }
        }

        private void ValidateMethodParametersHaveNoAnnotations(DynamicallyAccessedMemberTypes[] parameterAnnotations, MethodDesc method, MethodDesc baseMethod, MethodDesc origin)
        {
            for (int parameterIndex = 0; parameterIndex < parameterAnnotations.Length; parameterIndex++)
            {
                var annotation = parameterAnnotations[parameterIndex];
                if (annotation != DynamicallyAccessedMemberTypes.None)
                    LogValidationWarning(
                        (new MethodProxy(method)).GetParameter((ParameterIndex)parameterIndex),
                        (new MethodProxy(baseMethod)).GetParameter((ParameterIndex)parameterIndex),
                        origin);
            }
        }

        private void ValidateMethodGenericParametersHaveNoAnnotations(DynamicallyAccessedMemberTypes[] genericParameterAnnotations, MethodDesc method, MethodDesc baseMethod, MethodDesc origin)
        {
            for (int genericParameterIndex = 0; genericParameterIndex < genericParameterAnnotations.Length; genericParameterIndex++)
            {
                if (genericParameterAnnotations[genericParameterIndex] != DynamicallyAccessedMemberTypes.None)
                {
                    LogValidationWarning(
                        method.Instantiation[genericParameterIndex],
                        baseMethod.Instantiation[genericParameterIndex],
                        origin);
                }
            }
        }

        private void LogValidationWarning(object provider, object baseProvider, MethodDesc origin)
        {
            switch (provider)
            {
                case ParameterProxy parameter:
                    ParameterProxy baseParameter = (ParameterProxy)baseProvider;
                    if (parameter.IsImplicitThis)
                        _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersMismatchOnImplicitThisBetweenOverrides,
                            DiagnosticUtilities.GetMethodSignatureDisplayName(parameter.Method.Method), DiagnosticUtilities.GetMethodSignatureDisplayName(baseParameter.Method.Method));
                    else
                        _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides,
                            DiagnosticUtilities.GetParameterNameForErrorMessage(parameter.Method.Method, parameter.MetadataIndex), DiagnosticUtilities.GetMethodSignatureDisplayName(parameter.Method.Method),
                            DiagnosticUtilities.GetParameterNameForErrorMessage(baseParameter.Method.Method, baseParameter.MetadataIndex), DiagnosticUtilities.GetMethodSignatureDisplayName(baseParameter.Method.Method));
                    break;
                case GenericParameterDesc genericParameterOverride:
                    _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides,
                        genericParameterOverride.Name, DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(genericParameterOverride),
                        ((GenericParameterDesc)baseProvider).Name, DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName((GenericParameterDesc)baseProvider));
                    break;
                case TypeDesc:
                    _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides,
                        DiagnosticUtilities.GetMethodSignatureDisplayName(origin), DiagnosticUtilities.GetMethodSignatureDisplayName((MethodDesc)baseProvider));
                    break;
                // No fields - it's not possible to have a virtual field and override it
                default:
                    throw new NotImplementedException($"Unsupported provider type {provider.GetType()}");
            }
        }

        private sealed class TypeAnnotations
        {
            public readonly TypeDesc Type;
            public readonly DynamicallyAccessedMemberTypes TypeAnnotation;
            private readonly MethodAnnotations[]? _annotatedMethods;
            private readonly FieldAnnotation[]? _annotatedFields;
            private readonly DynamicallyAccessedMemberTypes[]? _genericParameterAnnotations;

            public bool IsDefault => _annotatedMethods == null && _annotatedFields == null && _genericParameterAnnotations == null;

            public TypeAnnotations(
                TypeDesc type,
                DynamicallyAccessedMemberTypes typeAnnotations,
                MethodAnnotations[]? annotatedMethods,
                FieldAnnotation[]? annotatedFields,
                DynamicallyAccessedMemberTypes[]? genericParameterAnnotations)
                => (Type, TypeAnnotation, _annotatedMethods, _annotatedFields, _genericParameterAnnotations)
                 = (type, typeAnnotations, annotatedMethods, annotatedFields, genericParameterAnnotations);

            public bool TryGetAnnotation(MethodDesc method, out MethodAnnotations annotations)
            {
                annotations = default;

                if (_annotatedMethods == null)
                {
                    return false;
                }

                foreach (var m in _annotatedMethods)
                {
                    if (m.Method == method)
                    {
                        annotations = m;
                        return true;
                    }
                }

                return false;
            }

            public bool TryGetAnnotation(FieldDesc field, out FieldAnnotation annotation)
            {
                annotation = default;

                if (_annotatedFields == null)
                {
                    return false;
                }

                foreach (var f in _annotatedFields)
                {
                    if (f.Field == field)
                    {
                        annotation = f;
                        return true;
                    }
                }

                return false;
            }

            public bool TryGetAnnotation(GenericParameterDesc genericParameter, out DynamicallyAccessedMemberTypes annotation)
            {
                annotation = default;

                if (_genericParameterAnnotations == null)
                    return false;

                for (int genericParameterIndex = 0; genericParameterIndex < _genericParameterAnnotations.Length; genericParameterIndex++)
                {
                    if (Type.Instantiation[genericParameterIndex] == genericParameter)
                    {
                        annotation = _genericParameterAnnotations[genericParameterIndex];
                        return true;
                    }
                }

                return false;
            }

            public bool HasGenericParameterAnnotation() => _genericParameterAnnotations != null;
        }

        private readonly struct MethodAnnotations
        {
            public readonly MethodDesc Method;
            public readonly DynamicallyAccessedMemberTypes[]? ParameterAnnotations;
            public readonly DynamicallyAccessedMemberTypes ReturnParameterAnnotation;
            public readonly DynamicallyAccessedMemberTypes[]? GenericParameterAnnotations;

            public MethodAnnotations(
                MethodDesc method,
                DynamicallyAccessedMemberTypes[]? paramAnnotations,
                DynamicallyAccessedMemberTypes returnParamAnnotations,
                DynamicallyAccessedMemberTypes[]? genericParameterAnnotations)
                => (Method, ParameterAnnotations, ReturnParameterAnnotation, GenericParameterAnnotations) =
                    (method, paramAnnotations, returnParamAnnotations, genericParameterAnnotations);

            public bool TryGetAnnotation(GenericParameterDesc genericParameter, out DynamicallyAccessedMemberTypes annotation)
            {
                annotation = default;

                if (GenericParameterAnnotations == null)
                    return false;

                for (int genericParameterIndex = 0; genericParameterIndex < GenericParameterAnnotations.Length; genericParameterIndex++)
                {
                    if (Method.Instantiation[genericParameterIndex] == genericParameter)
                    {
                        annotation = GenericParameterAnnotations[genericParameterIndex];
                        return true;
                    }
                }

                return false;
            }
        }

        private readonly struct FieldAnnotation
        {
            public readonly FieldDesc Field;
            public readonly DynamicallyAccessedMemberTypes Annotation;

            public FieldAnnotation(FieldDesc field, DynamicallyAccessedMemberTypes annotation)
                => (Field, Annotation) = (field, annotation);
        }

        internal partial bool MethodRequiresDataFlowAnalysis(MethodProxy method)
            => RequiresDataflowAnalysisDueToSignature(method.Method);

#pragma warning disable CA1822 // Other partial implementations are not in the ilc project
        internal partial MethodReturnValue GetMethodReturnValue(MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
#pragma warning restore CA1822 // Mark members as static
            => new MethodReturnValue(method.Method, dynamicallyAccessedMemberTypes);

        internal partial MethodReturnValue GetMethodReturnValue(MethodProxy method)
            => GetMethodReturnValue(method, GetReturnParameterAnnotation(method.Method));

        internal partial GenericParameterValue GetGenericParameterValue(GenericParameterProxy genericParameter)
            => new GenericParameterValue(genericParameter.GenericParameter, GetGenericParameterAnnotation(genericParameter.GenericParameter));

#pragma warning disable CA1822 // Mark members as static - Should be an instance method for consistency
        internal partial MethodParameterValue GetMethodParameterValue(ParameterProxy param, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
            => new(param, dynamicallyAccessedMemberTypes);
#pragma warning restore CA1822 // Mark members as static

        internal partial MethodParameterValue GetMethodParameterValue(ParameterProxy param)
            => GetMethodParameterValue(param, GetParameterAnnotation(param));

#pragma warning disable CA1822 // Mark members as static - Should be an instance method for consistency
        // overrideIsThis is needed for backwards compatibility with MakeGenericType/Method https://github.com/dotnet/linker/issues/2428
        internal MethodParameterValue GetMethodThisParameterValue(MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, bool overrideIsThis = false)
        {
            if (!method.HasImplicitThis() && !overrideIsThis)
                throw new InvalidOperationException($"Cannot get 'this' parameter of method {method.GetDisplayName()} with no 'this' parameter.");
            return new MethodParameterValue(new ParameterProxy(method, (ParameterIndex)0), dynamicallyAccessedMemberTypes, overrideIsThis);
        }
#pragma warning restore CA1822 // Mark members as static

        internal partial MethodParameterValue GetMethodThisParameterValue(MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
            => GetMethodThisParameterValue(method, dynamicallyAccessedMemberTypes, false);

        internal partial MethodParameterValue GetMethodThisParameterValue(MethodProxy method)
        {
            if (!method.HasImplicitThis())
                throw new InvalidOperationException($"Cannot get 'this' parameter of method {method.GetDisplayName()} with no 'this' parameter.");
            ParameterProxy param = new(method, (ParameterIndex)0);
            var damt = GetParameterAnnotation(param);
            return GetMethodParameterValue(new ParameterProxy(method, (ParameterIndex)0), damt);
        }

        internal SingleValue GetFieldValue(FieldDesc field)
            => field.Name switch
            {
                "EmptyTypes" when field.OwningType.IsTypeOf(ILLink.Shared.TypeSystemProxy.WellKnownType.System_Type) => ArrayValue.Create(0, field.OwningType),
                "Empty" when field.OwningType.IsTypeOf(ILLink.Shared.TypeSystemProxy.WellKnownType.System_String) => new KnownStringValue(string.Empty),
                _ => new FieldValue(field, GetFieldAnnotation(field))
            };

        internal SingleValue GetTypeValueFromGenericArgument(TypeDesc genericArgument)
        {
            if (genericArgument is GenericParameterDesc inputGenericParameter)
            {
                return GetGenericParameterValue(inputGenericParameter);
            }
            else if (genericArgument is MetadataType genericArgumentType)
            {
                if (genericArgumentType.IsTypeOf(ILLink.Shared.TypeSystemProxy.WellKnownType.System_Nullable_T))
                {
                    var innerGenericArgument = genericArgumentType.Instantiation.Length == 1 ? genericArgumentType.Instantiation[0] : null;
                    switch (innerGenericArgument)
                    {
                        case GenericParameterDesc gp:
                            return new NullableValueWithDynamicallyAccessedMembers(genericArgumentType,
                                new GenericParameterValue(gp, GetGenericParameterAnnotation(gp)));

                        case TypeDesc underlyingType:
                            return new NullableSystemTypeValue(genericArgumentType, new SystemTypeValue(underlyingType));
                    }
                }
                // All values except for Nullable<T>, including Nullable<> (with no type arguments)
                return new SystemTypeValue(genericArgumentType);
            }
            else if (genericArgument is ArrayType arrayType)
            {
                return new SystemTypeValue(arrayType);
            }
            else
            {
                return UnknownValue.Instance;
            }
        }
    }
}
