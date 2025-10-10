// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ILCompiler.Dataflow;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal class TypeValidationChecker
    {
        private ConcurrentBag<(TypeDesc type, string reason)> _typeLoadValidationErrors = new ConcurrentBag<(TypeDesc type, string reason)>();
        private ConcurrentDictionary<TypeDesc, Task<bool>> _firstStageValidationChecks = new ConcurrentDictionary<TypeDesc, Task<bool>>();
        private ConcurrentDictionary<TypeDesc, (TypeDesc dependendOnType, string reason)[]> _crossTypeValidationDependencies = new ConcurrentDictionary<TypeDesc, (TypeDesc dependendOnType, string reason)[]>();
        private ConcurrentQueue<Task<bool>> _tasksThatMustFinish = new ConcurrentQueue<Task<bool>>();

        private TypeValidationChecker() { }

        private void LogErrors(Action<string> loggingFunction)
        {
            var typeLoadValidationErrors = _typeLoadValidationErrors.ToArray();
            Array.Sort(typeLoadValidationErrors, (ValueTuple<TypeDesc, string> left, ValueTuple<TypeDesc, string> right) =>
            {
                return TypeSystemComparer.Instance.Compare(left.Item1, right.Item1);
            });
            loggingFunction($"{_typeLoadValidationErrors.Count} type validation errors");
            loggingFunction("------");
            foreach (var reason in typeLoadValidationErrors)
            {
                loggingFunction($"{reason.type}: {reason.reason}");
            }
            loggingFunction("------");
        }

        private void AddTypeValidationError(TypeDesc type, string error)
        {
            _typeLoadValidationErrors.Add((type, error));
        }

        private async Task<bool> CanSkipValidationInstance(EcmaModule module)
        {
            // The system module can always skip type validation
#if !DEBUG
            if (module == module.Context.SystemModule)
                return true;
#endif

            foreach (var type in module.GetAllTypes())
            {
                if (type is EcmaType ecmaType)
                    _tasksThatMustFinish.Enqueue(ValidateType(this, ecmaType));
            }
            _tasksThatMustFinish.Enqueue(ValidateType(this, (EcmaType)module.GetGlobalModuleType()));

            bool failAtEnd = false;
            while (_tasksThatMustFinish.TryDequeue(out var taskToComplete))
            {
                if (!await taskToComplete)
                    failAtEnd = true;
            }

#if DEBUG
            if (module == module.Context.SystemModule)
            {
                // Spot check that the system module ALWAYS succeeds
                if (failAtEnd)
                {
                    throw new InternalCompilerErrorException("System module failed to validate all types");
                }
            }
#endif

            return !failAtEnd;
        }

        public static async Task<(bool canSkipValidation, string[] reasonsWhyItFailed)> CanSkipValidation(EcmaModule module)
        {
            TypeValidationChecker checker = new TypeValidationChecker();
            bool canSkipValidation = await checker.CanSkipValidationInstance(module);
            List<string> reasons = new List<string>();
            checker.LogErrors(reasons.Add);
            return (canSkipValidation, reasons.ToArray());
        }

        private static Task<bool> ValidateType(TypeValidationChecker checker, EcmaType type)
        {
            if (checker._firstStageValidationChecks.TryGetValue(type, out var result)) return result;
            Task<bool> skipValidatorForType = Task.Run(() => checker.ValidateTypeWorker(type));
            checker._firstStageValidationChecks.TryAdd(type, skipValidatorForType);
            return skipValidatorForType;
        }


        private async Task<bool> ValidateTypeWorker(EcmaType type)
        {
            Task<bool> ValidateTypeWorkerHelper(TypeDesc typeToCheckForSkipValidation)
            {
                return ValidateTypeHelper(typeToCheckForSkipValidation.InstantiateSignature(type.Instantiation, new Instantiation()));
            }
            // The runtime has a number of checks in the type loader which it will skip running if the SkipValidation flag is set
            // This function attempts to document all of them, and implement *some* of them.

            // This function performs a portion of the validation skipping that has been found to have some importance, or to serve as
            // In addition, there are comments about all validation skipping activities that the runtime will perform.
            try
            {
                var typeDef = type.MetadataReader.GetTypeDefinition(type.Handle);

                // Validate that the base type is loadable
                if (type.BaseType != null)
                {
                    if (!await ValidateTypeWorkerHelper(type.BaseType))
                    {
                        AddTypeValidationError(type, "BaseType failed validation");
                        return false;
                    }
                }

                // Validate that the base type is accessible to this type -- UNIMPLEMENTED

                foreach (var interfaceType in type.RuntimeInterfaces)
                {
                    // Validate that the all referenced interface types are loadable
                    if (!await ValidateTypeWorkerHelper(interfaceType))
                    {
                        AddTypeValidationError(type, $"Interface type {interfaceType} failed validation");
                        return false;
                    }

                    if (!interfaceType.IsInterface)
                    {
                        AddTypeValidationError(type, $"Runtime interface {interfaceType} is not an interface");
                        return false;
                    }

                    if (interfaceType.HasInstantiation)
                    {
                        // Interfaces behave covariantly
                        ValidateVarianceInType(type.Instantiation, interfaceType, Internal.TypeSystem.GenericVariance.Covariant);
                    }
                }

                // Validate that each interface type explicitly implemented on this type is accessible to this type -- UNIMPLEMENTED
                foreach (var field in type.GetFields())
                {
                    // Validate that all fields on the type are both loadable
                    if (!await ValidateTypeWorkerHelper(field.FieldType))
                    {
                        AddTypeValidationError(type, $"Field {field.GetName()}'s type failed validation");
                        return false;
                    }

                    // Validate that all fields on the type are accessible -- UNIMPLEMENTED
                }

                // Per method rules
                foreach (var methodDesc in type.GetMethods())
                {
                    var method = (EcmaMethod)methodDesc;
                    var methodDef = method.MetadataReader.GetMethodDefinition(method.Handle);
                    // Validate that the validateTokenSig algorithm on all methods defined on the type
                    // The validateTokenSig algorithm simply validates the phyical structure of the signature. Getting a MethodSignature object is a more complete check
                    try
                    {
                        var getSignature = method.Signature;
                    }
                    catch
                    {
                        AddTypeValidationError(type, $"Signature could not be loaded for method {method.GetName()}");
                        return false;
                    }

                    // Validate that enums have no methods
                    if (type.IsEnum)
                    {
                        AddTypeValidationError(type, $"Is enum type with a method");
                        return false;
                    }

                    // Validate that if the method has an RVA that (the Class is not a ComImport class, it is not abstract, it is not marked with the miRuntime flag, and is not marked as InternalCall)
                    if (methodDef.RelativeVirtualAddress != 0)
                    {
                        // Validate that if the method has an RVA that the Class is not a ComImport class -- UNIMPLEMENTED
                        // Validate that if the method has an RVA that the method is not abstract
                        if (methodDef.Attributes.HasFlag(System.Reflection.MethodAttributes.Abstract))
                        {
                            AddTypeValidationError(type, $"{method} is an abstract method with a non-zero RVA");
                            return false;
                        }
                        // Validate that if the method has an RVA is not marked with the miRuntime flag
                        if (methodDef.ImplAttributes.HasFlag(System.Reflection.MethodImplAttributes.Runtime))
                        {
                            AddTypeValidationError(type, $"{method} is an miRuntime method with a non-zero RVA");
                            return false;
                        }
                        // Validate that if the method has an RVA is not marked as InternalCall
                        if (methodDef.ImplAttributes.HasFlag(System.Reflection.MethodImplAttributes.InternalCall))
                        {
                            AddTypeValidationError(type, $"{method} is an internal call method with a non-zero RVA");
                            return false;
                        }
                    }
                    // Validate that abstract methods cannot exist on non-abstract classes
                    if (method.IsAbstract && !type.IsAbstract)
                    {
                        AddTypeValidationError(type, $"abstract method {method} defined on non-abstract type");
                        return false;
                    }
                    // Validate that for instance methods, the abstract flag can only be set on a virtual method.
                    if (!methodDef.Attributes.HasFlag(MethodAttributes.Static) && method.IsAbstract)
                    {
                        if (!method.IsVirtual)
                        {
                            AddTypeValidationError(type, $"instance abstract method {method} not marked as virtual");
                            return false;
                        }
                    }
                    // Validate that interfaces can only have rtSpecialName methods which are "_VtblGap" or ".cctor" methods
                    if (type.IsInterface)
                    {
                        if (methodDef.Attributes.HasFlag(MethodAttributes.RTSpecialName))
                        {
                            if (!method.Name.SequenceEqual(".cctor"u8) && !method.Name.StartsWith("_VtblGap"u8))
                            {
                                AddTypeValidationError(type, $"Special name method {method} defined on interface");
                                return false;
                            }
                        }
                    }
                    if (method.IsVirtual)
                    {
                        // Validate that if a method is virtual that it cannot be a p/invoke
                        if (method.IsPInvoke)
                        {
                            AddTypeValidationError(type, $"'{method}' is both virtual and a p/invoke");
                            return false;
                        }
                        // Validate that if a method is virtual and static it can only exist on an interface
                        if (methodDef.Attributes.HasFlag(MethodAttributes.Static) && !type.IsInterface)
                        {
                            AddTypeValidationError(type, $"'{method}' is a virtual static method not defined on an interface");
                            return false;
                        }
                        // Validate that constructors cannot be marked as virtual
                        if (method.IsConstructor || method.IsStaticConstructor)
                        {
                            AddTypeValidationError(type, $"'{method}' is a virtual constructor");
                            return false;
                        }
                    }
                    // Validate that no synchronized methods may exist on a value type
                    if (type.IsValueType)
                    {
                        if (method.IsSynchronized)
                        {
                            AddTypeValidationError(type, $"'{method}' is synchronized method on a value type");
                            return false;
                        }
                    }
                    // validate that the global class cannot have instance methods
                    if (type.EcmaModule.GetGlobalModuleType() == type && !methodDef.Attributes.HasFlag(MethodAttributes.Static))
                    {
                        AddTypeValidationError(type, $"'{method}' is an instance method defined on the global <module> type");
                        return false;
                    }
                    // Validate that a generic method cannot be on a ComImport class, or a ComEventInterface  -- UNIMPLEMENTED
                    // Validate that a generic method cannot be a p/invoke
                    if (method.IsPInvoke)
                    {
                        if (type.HasInstantiation)
                        {
                            AddTypeValidationError(type, $"'{method}' is an pinvoke defined on a generic type");
                            return false;
                        }
                        if (method.HasInstantiation)
                        {
                            AddTypeValidationError(type, $"'{method}' is an pinvoke defined as a generic method");
                            return false;
                        }
                    }
                    // Validate that outside of CoreLib, that a generic method cannot be an internal call method
                    if (type.Context.SystemModule != type.Module && method.IsInternalCall)
                    {
                        if (method.HasInstantiation)
                        {
                            AddTypeValidationError(type, $"'{method}' is an internal call generic method");
                            return false;
                        }
                        if (type.HasInstantiation)
                        {
                            AddTypeValidationError(type, $"'{method}' is an internal call method on generic type");
                            return false;
                        }
                    }
                    // Validate that a generic method cannot be marked as runtime
                    if (method.HasInstantiation && method.IsRuntimeImplemented)
                    {
                        AddTypeValidationError(type, $"'{method}' is an runtime-impl generic method");
                        return false;
                    }


                    // Validate that generic variance is properly respected in method signatures
                    if (type.IsInterface && method.IsVirtual && !method.Signature.IsStatic && type.HasInstantiation)
                    {
                        if (!ValidateVarianceInSignature(type.Instantiation, method.Signature, Internal.TypeSystem.GenericVariance.Covariant, Internal.TypeSystem.GenericVariance.Contravariant))
                        {
                            AddTypeValidationError(type, $"'{method}' has invalid variance in signature");
                            return false;
                        }
                    }

                    foreach (var genericParameterType in method.Instantiation)
                    {
                        foreach (var constraint in ((GenericParameterDesc)genericParameterType).TypeConstraints)
                        {
                            if (!ValidateVarianceInType(type.Instantiation, constraint, Internal.TypeSystem.GenericVariance.Contravariant))
                            {
                                AddTypeValidationError(type, $"'{method}' has invalid variance in generic parameter constraint {constraint} on type {genericParameterType}");
                                return false;
                            }
                        }
                    }

                    if (!BoundedCheckForInstantiation(method.Instantiation))
                    {
                        AddTypeValidationError(type, $"'{method}' has cyclical generic parameter constraints");
                        return false;
                    }
                    // Validate that constraints are all acccessible to the method using them -- UNIMPLEMENTED
                }

                // Generic class special rules
                // Validate that a generic class cannot be a ComImport class, or a ComEventInterface class -- UNIMPLEMENTED

                foreach (var genericParameterType in type.Instantiation)
                {
                    foreach (var constraint in ((GenericParameterDesc)genericParameterType).TypeConstraints)
                    {
                        if (!ValidateVarianceInType(type.Instantiation, constraint, Internal.TypeSystem.GenericVariance.Contravariant))
                        {
                            AddTypeValidationError(type, $"'{genericParameterType}' has invalid variance in generic parameter constraint {constraint}");
                            return false;
                        }
                    }
                }

                // Validate that there are no cyclical class or method constraints, and that constraints are all acccessible to the type using them
                if (!BoundedCheckForInstantiation(type.Instantiation))
                {
                    AddTypeValidationError(type, $"'{type}' has cyclical generic parameter constraints");
                    return false;
                }

                // Override rules
                // Validate that each override results does not violate accessibility rules -- UNIMPLEMENTED

                HashSet<MethodDesc> overriddenDeclMethods = new HashSet<MethodDesc>();

                foreach (var methodImplHandle in typeDef.GetMethodImplementations())
                {
                    var methodImpl = type.MetadataReader.GetMethodImplementation(methodImplHandle);
                    var methodBody = type.EcmaModule.GetMethod(methodImpl.MethodBody);
                    var methodDecl = type.EcmaModule.GetMethod(methodImpl.MethodDeclaration);

                    // Validate that all MethodImpls actually match signatures closely enough
                    if (!methodBody.Signature.ApplySubstitution(type.Instantiation).EquivalentWithCovariantReturnType(methodDecl.Signature.ApplySubstitution(type.Instantiation)))
                    {
                        AddTypeValidationError(type, $"MethodImpl with Body '{methodBody}' and Decl '{methodDecl}' do not have matching signatures");
                        return false;
                    }

                    if (!methodDecl.IsVirtual)
                    {
                        AddTypeValidationError(type, $"MethodImpl with Decl '{methodDecl}' points at non-virtual decl method");
                        return false;
                    }

                    if (methodDecl.IsFinal)
                    {
                        AddTypeValidationError(type, $"MethodImpl with Decl '{methodDecl}' points at sealed decl method");
                        return false;
                    }

                    bool isStatic = methodBody.Signature.IsStatic;
                    if (methodBody.OwningType.IsInterface && !isStatic && !methodBody.IsFinal)
                    {
                        AddTypeValidationError(type, $"MethodImpl with Body '{methodBody}' and Decl '{methodDecl}' implements interface on another interface with a non-sealed method");
                        return false;
                    }

                    if (isStatic && methodBody.IsVirtual)
                    {
                        AddTypeValidationError(type, $"MethodImpl with Body '{methodBody}' and Decl '{methodDecl}' implements a static virtual method with a virtual static method");
                        return false;
                    }

                    if (!isStatic && !methodBody.IsVirtual)
                    {
                        AddTypeValidationError(type, $"MethodImpl with Body '{methodBody}' and Decl '{methodDecl}' implements a instance virtual method with a non-virtual instance method");
                        return false;
                    }

                    // Validate that multiple MethodImpls don't override the same method
                    if (!overriddenDeclMethods.Add(methodDecl))
                    {
                        AddTypeValidationError(type, $"Multiple MethodImpl records override '{methodDecl}'");
                        return false;
                    }

                    // Validate that the MethodImpl follows MethodImpl accessibility rules -- UNIMPLEMENTED
                }

                var virtualMethodAlgorithm = type.Context.GetVirtualMethodAlgorithmForType(type);
                VirtualMethodAlgorithm baseTypeVirtualMethodAlgorithm = null;
                if (type.BaseType != null && !type.IsInterface && !type.IsValueType)
                {
                    baseTypeVirtualMethodAlgorithm = type.Context.GetVirtualMethodAlgorithmForType(type.BaseType);
                }

                foreach (var interfaceImplemented in type.RuntimeInterfaces)
                {
                    foreach (var interfaceMethod in interfaceImplemented.GetVirtualMethods())
                    {
                        MethodDesc resolvedMethod;
                        bool staticInterfaceMethod = interfaceMethod.Signature.IsStatic;
                        if (staticInterfaceMethod)
                        {
                            resolvedMethod = virtualMethodAlgorithm.ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod, type);
                        }
                        else
                        {
                            resolvedMethod = type.ResolveInterfaceMethodTarget(interfaceMethod);
                        }

                        if (resolvedMethod != null)
                        {
                            // Validate that for every override involving generic methods that the generic method constraints are matching
                            if (!CompareMethodConstraints(interfaceMethod, resolvedMethod))
                            {
                                AddTypeValidationError(type, $"Interface method '{interfaceMethod}' overridden by method '{resolvedMethod}' which does not have matching generic constraints");
                                return false;
                            }
                        }

                        // Validate that all virtual static methods are actually implemented if the type is not abstract
                        // Validate that all virtual instance methods are actually implemented if the type is not abstract
                        if (!type.IsAbstract)
                        {
                            if (null == resolvedMethod || (staticInterfaceMethod && resolvedMethod.IsAbstract))
                            {
                                if (virtualMethodAlgorithm.ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethod, type, out var impl) != DefaultInterfaceMethodResolution.DefaultImplementation || impl.IsAbstract)
                                {
                                    AddTypeValidationError(type, $"Interface method '{interfaceMethod}' does not have implementation");
                                    return false;
                                }

                                if (impl != null)
                                {
                                    // Validate that for every override involving generic methods that the generic method constraints are matching
                                    if (!CompareMethodConstraints(interfaceMethod, impl))
                                    {
                                        AddTypeValidationError(type, $"Interface method '{interfaceMethod}' overridden by method '{impl}' which does not have matching generic constraints");
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var virtualMethod in type.EnumAllVirtualSlots())
                {
                    var implementationMethod = virtualMethodAlgorithm.FindVirtualFunctionTargetMethodOnObjectType(virtualMethod, type);

                    if (implementationMethod != null)
                    {
                        // Validate that for every override involving generic methods that the generic method constraints are matching
                        if (!CompareMethodConstraints(virtualMethod, implementationMethod))
                        {
                            AddTypeValidationError(type, $"Virtual method '{virtualMethod}' overridden by method '{implementationMethod}' which does not have matching generic constraints");
                            return false;
                        }

                        // Validate that if the decl method for the virtual is not on the immediate base type, that the intermediate type did not establish a
                        // covariant return type which requires the implementation method to specify a more specific base type
                        if ((virtualMethod.OwningType != type.BaseType) && (virtualMethod.OwningType != type) && (baseTypeVirtualMethodAlgorithm != null))
                        {
                            var implementationOnBaseType = baseTypeVirtualMethodAlgorithm.FindVirtualFunctionTargetMethodOnObjectType(virtualMethod, type.BaseType);
                            if (!implementationMethod.Signature.ApplySubstitution(type.Instantiation).EquivalentWithCovariantReturnType(implementationOnBaseType.Signature.ApplySubstitution(type.Instantiation)))
                            {
                                AddTypeValidationError(type, $"Virtual method '{virtualMethod}' overridden by method '{implementationMethod}' does not satisfy the covariant return type introduced with '{implementationOnBaseType}'");
                                return false;
                            }
                        }
                    }

                    // Validate that all virtual static methods are actually implemented if the type is not abstract
                    // Validate that all virtual instance methods are actually implemented if the type is not abstract
                    if (!type.IsAbstract)
                    {
                        if (implementationMethod == null || implementationMethod.IsAbstract)
                        {
                            AddTypeValidationError(type, $"Interface method '{virtualMethod}' does not have implementation");
                            return false;
                        }
                    }
                }

                if (type.TypeIdentifierData != null)
                {
                    if (!type.TypeHasCharacteristicsRequiredToBeLoadableTypeEquivalentType)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // If we throw an exception, clearly type validation skipping was not to be
                AddTypeValidationError(type, $"due to exception '{ex.ToString()}'");
                return false;
            }

            static bool CompareGenericParameterConstraint(MethodDesc declMethod, GenericParameterDesc parameterOfDecl, MethodDesc implMethod, GenericParameterDesc parameterOfImpl)
            {
                if (parameterOfImpl.HasDefaultConstructorConstraint)
                    if (!parameterOfDecl.HasDefaultConstructorConstraint && !parameterOfDecl.HasNotNullableValueTypeConstraint)
                        return false;

                if (parameterOfImpl.HasNotNullableValueTypeConstraint)
                    if (!parameterOfDecl.HasNotNullableValueTypeConstraint)
                        return false;

                if (parameterOfImpl.HasReferenceTypeConstraint)
                    if (!parameterOfDecl.HasReferenceTypeConstraint)
                        return false;

                // Constraints that 'allow' must check the impl first
                if (parameterOfImpl.HasAllowByRefLikeConstraint)
                    if (!parameterOfDecl.HasAllowByRefLikeConstraint)
                        return false;

                HashSet<TypeDesc> constraintsOnDecl = new HashSet<TypeDesc>();
                foreach (var constraint in parameterOfDecl.TypeConstraints)
                {
                    constraintsOnDecl.Add(constraint.InstantiateSignature(declMethod.OwningType.Instantiation, implMethod.Instantiation).InstantiateSignature(implMethod.OwningType.Instantiation, new Instantiation()));
                }

                foreach (var constraint in parameterOfImpl.TypeConstraints)
                {
                    if (!constraintsOnDecl.Contains(constraint.InstantiateSignature(implMethod.OwningType.Instantiation, implMethod.Instantiation)))
                        return false;
                }

                return true;
            }

            static bool CompareMethodConstraints(MethodDesc methodDecl, MethodDesc methodImpl)
            {
                // Validate that methodDecl's method constraints are at least as stringent as methodImpls
                // The Decl is permitted to be more stringent.

                if (methodDecl.Instantiation.Length != methodImpl.Instantiation.Length)
                    return false;
                for (int i = 0; i < methodDecl.Instantiation.Length; i++)
                {
                    var genericParameterDescOnImpl = (GenericParameterDesc)methodImpl.GetTypicalMethodDefinition().Instantiation[i];
                    var genericParameterDescOnDecl = (GenericParameterDesc)methodDecl.GetTypicalMethodDefinition().Instantiation[i];
                    if (!CompareGenericParameterConstraint(methodDecl, genericParameterDescOnDecl, methodImpl, genericParameterDescOnImpl))
                    {
                        return false;
                    }
                }

                return true;
            }

            Task<bool> ValidateTypeHelper(TypeDesc typeDesc)
            {
                if (typeDesc == null)
                    return Task.FromResult(true);

                if (typeDesc is EcmaType ecmaType)
                {
                    // Trigger the ecmaType to have its type checked, but do not check the task immediately. Unfortunately this can be recursive.
                    _tasksThatMustFinish.Enqueue(ValidateType(this, ecmaType));
                    return Task.FromResult(true);
                }
                else if (typeDesc is InstantiatedType instantiatedType)
                    return ValidateTypeHelperInstantiatedType(instantiatedType);
                else if (typeDesc is ParameterizedType parameterizedType)
                    return ValidateTypeHelper(parameterizedType.ParameterType);
                else if (typeDesc is FunctionPointerType functionPointerType)
                    return ValidateTypeHelperFunctionPointerType(functionPointerType);
                return Task.FromResult(true);
            }

            Task<bool> ValidateTypeHelperInstantiatedType(InstantiatedType instantiatedType)
            {
                try
                {
                    // Constraints should be satisfied
                    if (!instantiatedType.CheckConstraints())
                    {
                        AddTypeValidationError(type, $"Constraint check failed validating {instantiatedType}");
                        return Task.FromResult(false);
                    }

                    return ValidateTypeHelper(instantiatedType.GetTypeDefinition());
                }
                catch (Exception ex)
                {
                    AddTypeValidationError(instantiatedType, $"due to exception '{ex}'");
                    return Task.FromResult(false);
                }
            }

            async Task<bool> ValidateTypeHelperFunctionPointerType(FunctionPointerType functionPointerType)
            {
                if (!await ValidateTypeHelper(functionPointerType.Signature.ReturnType))
                    return false;
                foreach (var type in functionPointerType.Signature)
                {
                    if (!await ValidateTypeHelper(type))
                        return false;
                }
                return true;
            }

            static bool BoundedCheckForInstantiation(Instantiation instantiation)
            {
                foreach (var type in instantiation)
                {
                    Debug.Assert(type.IsGenericParameter);
                    GenericParameterDesc genericParameter = (GenericParameterDesc)type;

                    if (!BoundedCheckForType(genericParameter, instantiation.Length))
                        return false;
                }
                return true;
            }

            static bool BoundedCheckForType(GenericParameterDesc genericParameter, int maxDepth)
            {
                if (maxDepth <= 0)
                    return false;
                foreach (var type in genericParameter.TypeConstraints)
                {
                    if (type is GenericParameterDesc possiblyCircularGenericParameter)
                    {
                        if (possiblyCircularGenericParameter.Kind == genericParameter.Kind)
                        {
                            if (!BoundedCheckForType(possiblyCircularGenericParameter, maxDepth - 1))
                                return false;
                        }
                    }
                }
                return true;
            }

            static bool ValidateVarianceInSignature(Instantiation associatedTypeInstantiation, MethodSignature signature, Internal.TypeSystem.GenericVariance returnTypePosition, Internal.TypeSystem.GenericVariance argTypePosition)
            {
                for (int i = 0; i < signature.Length; i++)
                {
                    if (!ValidateVarianceInType(associatedTypeInstantiation, signature[i], argTypePosition))
                        return false;
                }

                if (!ValidateVarianceInType(associatedTypeInstantiation, signature.ReturnType, returnTypePosition))
                    return false;

                return true;
            }

            static bool ValidateVarianceInType(Instantiation associatedTypeInstantiation, TypeDesc type, Internal.TypeSystem.GenericVariance position)
            {
                if (type is SignatureTypeVariable signatureTypeVar)
                {
                    GenericParameterDesc gp = (GenericParameterDesc)associatedTypeInstantiation[signatureTypeVar.Index];
                    if (gp.Variance != Internal.TypeSystem.GenericVariance.None)
                    {
                        if (position != gp.Variance)
                        {
                            // Covariant and contravariant parameters can *only* appear in resp. covariant and contravariant positions
                            return false;
                        }
                    }
                }
                else if (type is InstantiatedType instantiatedType)
                {
                    var typeDef = instantiatedType.GetTypeDefinition();
                    if (typeDef.IsValueType || position == Internal.TypeSystem.GenericVariance.None)
                    {
                        // Value types and non-variant positions require all parameters to be non-variant
                        foreach (TypeDesc instType in instantiatedType.Instantiation)
                        {
                            if (!ValidateVarianceInType(associatedTypeInstantiation, instType, Internal.TypeSystem.GenericVariance.None))
                                return false;
                        }
                    }
                    else
                    {
                        int index = 0;
                        for (index = 0; index < typeDef.Instantiation.Length; index++)
                        {
                            Internal.TypeSystem.GenericVariance positionForParameter = ((GenericParameterDesc)typeDef.Instantiation[index]).Variance;
                            // If the surrounding context is contravariant then we need to flip the variance of this parameter
                            if (position == Internal.TypeSystem.GenericVariance.Contravariant)
                            {
                                if (positionForParameter == Internal.TypeSystem.GenericVariance.Covariant)
                                    positionForParameter = Internal.TypeSystem.GenericVariance.Contravariant;
                                else if (positionForParameter == Internal.TypeSystem.GenericVariance.Contravariant)
                                    positionForParameter = Internal.TypeSystem.GenericVariance.Covariant;
                                else
                                    positionForParameter = Internal.TypeSystem.GenericVariance.None;
                            }

                            if (!ValidateVarianceInType(associatedTypeInstantiation, instantiatedType.Instantiation[index], positionForParameter))
                                return false;
                        }
                    }
                }
                else if (type is ParameterizedType parameterizedType)
                {
                    // Arrays behave as covariant, byref and pointer types are non-variant
                    if (!ValidateVarianceInType(associatedTypeInstantiation, parameterizedType.ParameterType, (parameterizedType.IsByRef || parameterizedType.IsPointer) ? Internal.TypeSystem.GenericVariance.None : position))
                        return false;
                }
                else if (type is FunctionPointerType functionPointerType)
                {
                    if (!ValidateVarianceInSignature(associatedTypeInstantiation, functionPointerType.Signature, Internal.TypeSystem.GenericVariance.None, Internal.TypeSystem.GenericVariance.None))
                        return false;
                }

                return true;
            }
        }
    }
}
