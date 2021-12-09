﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Dataflow
{
    /// <summary>
    /// Caches dataflow annotations for type members.
    /// </summary>
    public class FlowAnnotations
    {
        private readonly TypeAnnotationsHashtable _hashtable;
        private readonly Logger _logger;

        public FlowAnnotations(Logger logger, ILProvider ilProvider)
        {
            _hashtable = new TypeAnnotationsHashtable(logger, ilProvider);
            _logger = logger;
        }

        public bool RequiresDataflowAnalysis(MethodDesc method)
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

        public bool RequiresDataflowAnalysis(FieldDesc field)
        {
            try
            {
                field = field.GetTypicalFieldDefinition();
                return GetAnnotations(field.OwningType).TryGetAnnotation(field, out _);
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public bool HasAnyAnnotations(TypeDesc type)
        {
            try
            {
                return !GetAnnotations(type.GetTypeDefinition()).IsDefault;
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the annotations for the given parameter.
        /// </summary>
        /// <param name="parameterIndex">Parameter index in the IL sense. Parameter 0 on instance methods is `this`.</param>
        public DynamicallyAccessedMemberTypes GetParameterAnnotation(MethodDesc method, int parameterIndex)
        {
            method = method.GetTypicalMethodDefinition();

            if (GetAnnotations(method.OwningType).TryGetAnnotation(method, out var annotation) && annotation.ParameterAnnotations != null)
            {
                return annotation.ParameterAnnotations[parameterIndex];
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
            //       // Linker will not see this code, so there are no checks
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

        private TypeAnnotations GetAnnotations(TypeDesc type)
        {
            return _hashtable.GetOrCreateValue(type);
        }

        private class TypeAnnotationsHashtable : LockFreeReaderHashtable<TypeDesc, TypeAnnotations>
        {
            private readonly ILProvider _ilProvider;
            private readonly Logger _logger;

            public TypeAnnotationsHashtable(Logger logger, ILProvider ilProvider) => (_logger, _ilProvider) = (logger, ilProvider);

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
                    TypeDesc baseType = key.BaseType;
                    while (baseType != null)
                    {
                        TypeDefinition baseTypeDef = reader.GetTypeDefinition(((EcmaType)baseType.GetTypeDefinition()).Handle);
                        typeAnnotation |= GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, baseTypeDef.GetCustomAttributes());
                        baseType = baseType.BaseType;
                    }

                    // And inherit them from interfaces
                    foreach (DefType runtimeInterface in key.RuntimeInterfaces)
                    {
                        TypeDefinition interfaceTypeDef = reader.GetTypeDefinition(((EcmaType)runtimeInterface.GetTypeDefinition()).Handle);
                        typeAnnotation |= GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, interfaceTypeDef.GetCustomAttributes());
                    }
                }
                catch (TypeSystemException)
                {
                    // If the class hierarchy is not walkable, just stop collecting the annotations.
                }

                var annotatedFields = new ArrayBuilder<FieldAnnotation>();

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
                        _logger.LogWarning(
                            $"Field '{field.GetDisplayName()}' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to fields of type 'System.Type' or 'System.String'.",
                            2097, field, subcategory: MessageSubCategory.TrimAnalysis);
                        continue;
                    }

                    annotatedFields.Add(new FieldAnnotation(field, annotation));
                }

                var annotatedMethods = new ArrayBuilder<MethodAnnotations>();

                // Next go over all methods with an explicit annotation
                foreach (EcmaMethod method in ecmaType.GetMethods())
                {
                    DynamicallyAccessedMemberTypes[] paramAnnotations = null;

                    // We convert indices from metadata space to IL space here.
                    // IL space assigns index 0 to the `this` parameter on instance methods.

                    DynamicallyAccessedMemberTypes methodMemberTypes =
                        GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, reader.GetMethodDefinition(method.Handle).GetCustomAttributes());
                    
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

                    int offset;
                    if (!signature.IsStatic)
                    {
                        offset = 1;
                    }
                    else
                    {
                        offset = 0;
                    }

                    // If there's an annotation on the method itself and it's one of the special types (System.Type for example)
                    // treat that annotation as annotating the "this" parameter.
                    if (methodMemberTypes != DynamicallyAccessedMemberTypes.None)
                    {
                        if (IsTypeInterestingForDataflow(method.OwningType) && !signature.IsStatic)
                        {
                            paramAnnotations = new DynamicallyAccessedMemberTypes[signature.Length + offset];
                            paramAnnotations[0] = methodMemberTypes;
                        }
                        else
                        {
                            _logger.LogWarning(
                                $"The 'DynamicallyAccessedMembersAttribute' is not allowed on methods. It is allowed on method return value or method parameters though.",
                                2041, method, subcategory: MessageSubCategory.TrimAnalysis);
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
                                _logger.LogWarning(
                                    $"Return type of method '{method.GetDisplayName()}' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to parameters of type 'System.Type' or 'System.String'.",
                                    2106, method, subcategory: MessageSubCategory.TrimAnalysis);
                            }
                        }
                        else
                        {
                            DynamicallyAccessedMemberTypes pa = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, parameter.GetCustomAttributes());
                            if (pa == DynamicallyAccessedMemberTypes.None)
                                continue;

                            if (!IsTypeInterestingForDataflow(signature[parameter.SequenceNumber - 1]))
                            {
                                _logger.LogWarning(
                                    $"Parameter #{parameter.SequenceNumber} of method '{method.GetDisplayName()}' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to parameters of type 'System.Type' or 'System.String'.",
                                    2098, method, subcategory: MessageSubCategory.TrimAnalysis);
                                continue;
                            }

                            if (paramAnnotations == null)
                            {
                                paramAnnotations = new DynamicallyAccessedMemberTypes[signature.Length + offset];
                            }
                            paramAnnotations[parameter.SequenceNumber - 1 + offset] = pa;
                        }
                    }

                    DynamicallyAccessedMemberTypes[] genericParameterAnnotations = null;
                    foreach (EcmaGenericParameter genericParameter in method.Instantiation)
                    {
                        GenericParameter genericParameterDef = reader.GetGenericParameter(genericParameter.Handle);
                        var annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, genericParameterDef.GetCustomAttributes());
                        if (annotation != DynamicallyAccessedMemberTypes.None)
                        {
                            if (genericParameterAnnotations == null)
                                genericParameterAnnotations = new DynamicallyAccessedMemberTypes[method.Instantiation.Length];
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
                        _logger.LogWarning(
                            $"Property '{property.GetDisplayName()}' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to properties of type 'System.Type' or 'System.String'.",
                            2099, property, subcategory: MessageSubCategory.TrimAnalysis);
                        continue;
                    }

                    FieldDesc backingFieldFromSetter = null;

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

                        if (annotatedMethods.Any(a => a.Method == setMethod))
                        {
                            
                            _logger.LogWarning(
                                $"'DynamicallyAccessedMembersAttribute' on property '{property.GetDisplayName()}' conflicts with the same attribute on its accessor '{setMethod.GetDisplayName()}'.",
                                2043, setMethod, subcategory: MessageSubCategory.TrimAnalysis);
                        }
                        else
                        {
                            int offset = setMethod.Signature.IsStatic ? 0 : 1;
                            if (setMethod.Signature.Length > 0)
                            {
                                DynamicallyAccessedMemberTypes[] paramAnnotations = new DynamicallyAccessedMemberTypes[setMethod.Signature.Length + offset];
                                paramAnnotations[paramAnnotations.Length - 1] = annotation;
                                annotatedMethods.Add(new MethodAnnotations(setMethod, paramAnnotations, DynamicallyAccessedMemberTypes.None, null));
                            }
                        }
                    }

                    FieldDesc backingFieldFromGetter = null;

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

                        if (annotatedMethods.Any(a => a.Method == getMethod))
                        {
                            _logger.LogWarning(
                                $"'DynamicallyAccessedMembersAttribute' on property '{property.GetDisplayName()}' conflicts with the same attribute on its accessor '{getMethod.GetDisplayName()}'.",
                                2043, getMethod, subcategory: MessageSubCategory.TrimAnalysis);
                        }
                        else
                        {
                            annotatedMethods.Add(new MethodAnnotations(getMethod, null, annotation, null));
                        }
                    }

                    FieldDesc backingField;
                    if (backingFieldFromGetter != null && backingFieldFromSetter != null &&
                        backingFieldFromGetter != backingFieldFromSetter)
                    {
                        _logger.LogWarning(
                            $"Could not find a unique backing field for property '{property.GetDisplayName()}' to propagate 'DynamicallyAccessedMembersAttribute'.",
                            2042, property, subcategory: MessageSubCategory.TrimAnalysis);
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
                            _logger.LogWarning(
                                $"'DynamicallyAccessedMemberAttribute' on property '{property.GetDisplayName()}' conflicts with the same attribute on its backing field '{backingField.GetDisplayName()}'.",
                                2056, backingField, subcategory: MessageSubCategory.TrimAnalysis);
                        }
                        else
                        {
                            annotatedFields.Add(new FieldAnnotation(backingField, annotation));
                        }
                    }
                }

                DynamicallyAccessedMemberTypes[] typeGenericParameterAnnotations = null;
                foreach (EcmaGenericParameter genericParameter in ecmaType.Instantiation)
                {
                    GenericParameter genericParameterDef = reader.GetGenericParameter(genericParameter.Handle);

                    var annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute(reader, genericParameterDef.GetCustomAttributes());
                    if (annotation != DynamicallyAccessedMemberTypes.None)
                    {
                        if (typeGenericParameterAnnotations == null)
                            typeGenericParameterAnnotations = new DynamicallyAccessedMemberTypes[ecmaType.Instantiation.Length];
                        typeGenericParameterAnnotations[genericParameter.Index] = annotation;
                    }
                }

                return new TypeAnnotations(ecmaType, typeAnnotation, annotatedMethods.ToArray(), annotatedFields.ToArray(), typeGenericParameterAnnotations);
            }

            private static bool ScanMethodBodyForFieldAccess(MethodIL body, bool write, out FieldDesc found)
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

            private bool IsTypeInterestingForDataflow(TypeDesc type)
            {
                // NOTE: this method is not particulary fast. It's assumed that the caller limits
                // calls to this method as much as possible.

                if (type.IsWellKnownType(WellKnownType.String))
                    return true;

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
                    ValidateMethodParametersHaveNoAnnotations(baseMethodAnnotations.ParameterAnnotations, method, baseMethod, method);
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
                                DiagnosticUtilities.GetMethodParameterFromIndex(method, parameterIndex),
                                DiagnosticUtilities.GetMethodParameterFromIndex(baseMethod, parameterIndex),
                                method);
                    }
                }
            }

            if (methodAnnotations.GenericParameterAnnotations != null || baseMethodAnnotations.GenericParameterAnnotations != null)
            {
                if (methodAnnotations.GenericParameterAnnotations == null)
                    ValidateMethodGenericParametersHaveNoAnnotations(baseMethodAnnotations.GenericParameterAnnotations, method, baseMethod, method);
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

        void ValidateMethodParametersHaveNoAnnotations(DynamicallyAccessedMemberTypes[] parameterAnnotations, MethodDesc method, MethodDesc baseMethod, MethodDesc origin)
        {
            for (int parameterIndex = 0; parameterIndex < parameterAnnotations.Length; parameterIndex++)
            {
                var annotation = parameterAnnotations[parameterIndex];
                if (annotation != DynamicallyAccessedMemberTypes.None)
                    LogValidationWarning(
                        parameterIndex,
                        baseMethod,
                        origin);
            }
        }

        void ValidateMethodGenericParametersHaveNoAnnotations(DynamicallyAccessedMemberTypes[] genericParameterAnnotations, MethodDesc method, MethodDesc baseMethod, MethodDesc origin)
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

        void LogValidationWarning(object provider, object baseProvider, MethodDesc origin)
        {
            switch (provider)
            {
                case int parameterNumber:
                    _logger.LogWarning(
                        $"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter #{parameterNumber} of method '{DiagnosticUtilities.GetMethodSignatureDisplayName(origin)}' " +
                        $"don't match overridden parameter #{parameterNumber} of method '{DiagnosticUtilities.GetMethodSignatureDisplayName((MethodDesc)baseProvider)}'. " +
                        $"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.",
                        2092, origin, subcategory: MessageSubCategory.TrimAnalysis);
                    break;
                case GenericParameterDesc genericParameterOverride:
                    _logger.LogWarning(
                        $"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the generic parameter '{genericParameterOverride.Name}' of '{DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(new GenericParameterOrigin(genericParameterOverride))}' " +
                        $"don't match overridden generic parameter '{((GenericParameterDesc)baseProvider).Name}' of '{DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName(new GenericParameterOrigin((GenericParameterDesc)baseProvider))}'. " +
                        $"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.",
                        2095, origin, subcategory: MessageSubCategory.TrimAnalysis);
                    break;
                case TypeDesc methodReturnType:
                    _logger.LogWarning(
                        $"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method '{DiagnosticUtilities.GetMethodSignatureDisplayName(origin)}' " +
                        $"don't match overridden return value of method '{DiagnosticUtilities.GetMethodSignatureDisplayName((MethodDesc)baseProvider)}'. " +
                        $"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.",
                        2093, origin, subcategory: MessageSubCategory.TrimAnalysis);
                    break;
                // No fields - it's not possible to have a virtual field and override it
                case MethodDesc methodDefinition:
                    _logger.LogWarning(
                        $"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the implicit 'this' parameter of method '{DiagnosticUtilities.GetMethodSignatureDisplayName(methodDefinition)}' " +
                        $"don't match overridden implicit 'this' parameter of method '{DiagnosticUtilities.GetMethodSignatureDisplayName((MethodDesc)baseProvider)}'. " +
                        $"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.",
                        2094, origin, subcategory: MessageSubCategory.TrimAnalysis);
                    break;
                default:
                    throw new NotImplementedException($"Unsupported provider type {provider.GetType()}");
            }
        }

        private class TypeAnnotations
        {
            public readonly TypeDesc Type;
            public readonly DynamicallyAccessedMemberTypes TypeAnnotation;
            private readonly MethodAnnotations[] _annotatedMethods;
            private readonly FieldAnnotation[] _annotatedFields;
            private readonly DynamicallyAccessedMemberTypes[] _genericParameterAnnotations;

            public bool IsDefault => _annotatedMethods == null && _annotatedFields == null && _genericParameterAnnotations == null;

            public TypeAnnotations(
                TypeDesc type,
                DynamicallyAccessedMemberTypes typeAnnotations,
                MethodAnnotations[] annotatedMethods,
                FieldAnnotation[] annotatedFields,
                DynamicallyAccessedMemberTypes[] genericParameterAnnotations)
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
        }

        private readonly struct MethodAnnotations
        {
            public readonly MethodDesc Method;
            public readonly DynamicallyAccessedMemberTypes[] ParameterAnnotations;
            public readonly DynamicallyAccessedMemberTypes ReturnParameterAnnotation;
            public readonly DynamicallyAccessedMemberTypes[] GenericParameterAnnotations;

            public MethodAnnotations(
                MethodDesc method,
                DynamicallyAccessedMemberTypes[] paramAnnotations,
                DynamicallyAccessedMemberTypes returnParamAnnotations,
                DynamicallyAccessedMemberTypes[] genericParameterAnnotations)
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
    }
}
