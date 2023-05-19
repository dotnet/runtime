// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        ConcurrentBag<(TypeDesc type, string reason)> _typeLoadValidationErrors = new ConcurrentBag<(TypeDesc type, string reason)>();

        public void LogErrors(Action<string> loggingFunction)
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

        public async Task<bool> CanSkipValidation(EcmaModule module)
        {
            // The system module can always skip type validation
#if !DEBUG
            if (module == module.Context.SystemModule)
                return true;
#endif

            List<Task<bool>> typesInModuleSkipValidationChecking = new List<Task<bool>>();
            foreach (var type in module.GetAllTypes())
            {
                if (type is EcmaType ecmaType)
                    typesInModuleSkipValidationChecking.Add(CanSkipValidation(ecmaType));
            }
            typesInModuleSkipValidationChecking.Add(CanSkipValidation((EcmaType)module.GetGlobalModuleType()));

            await Task.WhenAll(typesInModuleSkipValidationChecking);
            foreach (var type in typesInModuleSkipValidationChecking)
            {
                if (!type.Result)
                    return false;
            }
            return true;
        }

        private ConcurrentDictionary<TypeDesc, Task<bool>> _skipValidationDict = new ConcurrentDictionary<TypeDesc, Task<bool>>();
        private Task<bool> CanSkipValidation(EcmaType type)
        {
            if (_skipValidationDict.TryGetValue(type, out var result)) return result;
            Task<bool> skipValidatorForType = Task.Run(() => CanSkipValidationWorker(type));
            _skipValidationDict.TryAdd(type, skipValidatorForType);
            return skipValidatorForType;
        }

        public Task<bool> CanSkipValidation(TypeDesc type)
        {
            if (type == null)
                return Task.FromResult(true);

            if (type is EcmaType ecmaType)
                return CanSkipValidation(ecmaType);
            else if (type is InstantiatedType instantiatedType)
                return CanSkipValidation(instantiatedType);
            else if (type is ParameterizedType parameterizedType)
                return CanSkipValidation(parameterizedType.ParameterType);
            else if (type is FunctionPointerType functionPointerType)
                return CanSkipValidation(functionPointerType);
            return Task.FromResult(true);
        }

        private Task<bool> CanSkipValidation(InstantiatedType instantiatedType)
        {
            try
            {
                // Constraints should be satisfied
                if (!instantiatedType.CheckConstraints())
                {
                    _typeLoadValidationErrors.Add((instantiatedType, "Constraint check failed"));
                    return Task.FromResult(false);
                }

                return CanSkipValidation(instantiatedType.GetTypeDefinition());
            }
            catch (Exception ex)
            {
                _typeLoadValidationErrors.Add((instantiatedType, $"due to exception '{ex}'"));
                return Task.FromResult(false);
            }
/*
            if (_skipValidationDict.TryGetValue(instantiatedType, out var result)) return result;
            Task<bool> skipValidatorForType = Task.Run(() => CanSkipValidationWorker(instantiatedType));
            _skipValidationDict.TryAdd(instantiatedType, skipValidatorForType);
            return skipValidatorForType;*/
        }

        private async Task<bool> CanSkipValidationWorker(InstantiatedType instantiatedType)
        {
            try
            {
                // Constraints should be satisfied
                if (!instantiatedType.CheckConstraints())
                {
                    _typeLoadValidationErrors.Add((instantiatedType, "Constraint check failed"));
                    return false;
                }

                return await CanSkipValidation(instantiatedType.GetTypeDefinition());
            }
            catch (Exception ex)
            {
                _typeLoadValidationErrors.Add((instantiatedType, $"due to exception '{ex}'"));
                return false;
            }
        }

        private async Task<bool> CanSkipValidation(FunctionPointerType functionPointerType)
        {
            if (!await CanSkipValidation(functionPointerType.Signature.ReturnType))
                return false;
            foreach (var type in functionPointerType.Signature)
            {
                if (!await CanSkipValidation(type))
                    return false;
            }
            return true;
        }

        private async Task<bool> CanSkipValidationWorker(EcmaType type)
        {
            Task<bool> CanSkipValidationWorkerHelper(TypeDesc typeToCheckForSkipValidation)
            {
                return CanSkipValidation(typeToCheckForSkipValidation.InstantiateSignature(type.Instantiation, new Instantiation()));
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
                    if (!await CanSkipValidationWorkerHelper(type.BaseType))
                    {
                        _typeLoadValidationErrors.Add((type, "BaseType failed validation"));
                        return false;
                    }
                }

                // Validate that the base type is accessible to this type -- UNIMPLEMENTED

                foreach (var interfaceType in type.RuntimeInterfaces)
                {
                    // Validate that the all referenced interface types are loadable
                    if (!await CanSkipValidationWorkerHelper(interfaceType))
                    {
                        _typeLoadValidationErrors.Add((type, $"Interface type {interfaceType} failed validation"));
                        return false;
                    }
                }

                // Validate that each interface type explicitly implemented on this type is accessible to this type -- UNIMPLEMENTED

                foreach (var field in type.GetFields())
                {
                    // Validate that all fields on the type are both loadable
                    if (!await CanSkipValidationWorkerHelper(field.FieldType))
                    {
                        _typeLoadValidationErrors.Add((type, $"Field {field.Name}'s type failed validation"));
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
                        _typeLoadValidationErrors.Add((type, $"Signature could not be loaded for method {method.Name}"));
                        return false;
                    }

                    // Validate that enums have no methods
                    if (type.IsEnum)
                    {
                        _typeLoadValidationErrors.Add((type, $"Is enum type with a method"));
                        return false;
                    }

                    // Validate that if the method has an RVA that (the Class is not a ComImport class, it is not abstract, it is not marked with the miRuntime flag, and is not marked as InternalCall)
                    if (methodDef.RelativeVirtualAddress != 0)
                    {
                        // Validate that if the method has an RVA that the Class is not a ComImport class -- UNIMPLEMENTED
                        // Validate that if the method has an RVA that the method is not abstract
                        if (methodDef.Attributes.HasFlag(System.Reflection.MethodAttributes.Abstract))
                        {
                            _typeLoadValidationErrors.Add((type, $"{method} is an abstract method with a non-zero RVA"));
                            return false;
                        }
                        // Validate that if the method has an RVA is not marked with the miRuntime flag
                        if (methodDef.ImplAttributes.HasFlag(System.Reflection.MethodImplAttributes.Runtime))
                        {
                            _typeLoadValidationErrors.Add((type, $"{method} is an miRuntime method with a non-zero RVA"));
                            return false;
                        }
                        // Validate that if the method has an RVA is not marked as InternalCall
                        if (methodDef.ImplAttributes.HasFlag(System.Reflection.MethodImplAttributes.InternalCall))
                        {
                            _typeLoadValidationErrors.Add((type, $"{method} is an internal call method with a non-zero RVA"));
                            return false;
                        }
                    }
                    // Validate that abstract methods cannot exist on non-abstract classes
                    if (method.IsAbstract && !type.IsAbstract)
                    {
                        _typeLoadValidationErrors.Add((type, $"abstract method {method} defined on non-abstract type"));
                        return false;
                    }
                    // Validate that for instance methods, the abstract flag can only be set on a virtual method.
                    if (!methodDef.Attributes.HasFlag(MethodAttributes.Static) && method.IsAbstract)
                    {
                        if (!method.IsVirtual)
                        {
                            _typeLoadValidationErrors.Add((type, $"instance abstract method {method} not marked as virtual"));
                            return false;
                        }
                    }
                    // Validate that interfaces can only have rtSpecialName methods which are "_VtblGap" or ".cctor" methods
                    if (type.IsInterface)
                    {
                        if (method.IsSpecialName || methodDef.Attributes.HasFlag(MethodAttributes.RTSpecialName))
                        {
                            if ((method.Name != ".cctor") && !method.Name.StartsWith("_VtblGap"))
                            {
                                _typeLoadValidationErrors.Add((type, $"Special name method {method} defined on interface"));
                                return false;
                            }
                        }
                    }
                    if (method.IsVirtual)
                    {
                        // Validate that if a method is virtual that it cannot be a p/invoke
                        if (method.IsPInvoke)
                        {
                            _typeLoadValidationErrors.Add((type, $"'{method}' is both virtual and a p/invoke"));
                            return false;
                        }
                        // Validate that if a method is virtual and static it can only exist on an interface
                        if (methodDef.Attributes.HasFlag(MethodAttributes.Static) && !type.IsInterface)
                        {
                            _typeLoadValidationErrors.Add((type, $"'{method}' is a virtual static method not defined on an interface"));
                            return false;
                        }
                        // Validate that constructors cannot be marked as virtual
                        if (method.IsConstructor || method.IsStaticConstructor)
                        {
                            _typeLoadValidationErrors.Add((type, $"'{method}' is a virtual constructor"));
                            return false;
                        }
                    }
                    // Validate that no synchronized methods may exist on a value type
                    if (type.IsValueType)
                    {
                        if (method.IsSynchronized)
                        {
                            _typeLoadValidationErrors.Add((type, $"'{method}' is synchronized method on a value type"));
                            return false;
                        }
                    }
                    // validate that the global class cannot have instance methods
                    if (type.EcmaModule.GetGlobalModuleType() == type && !methodDef.Attributes.HasFlag(MethodAttributes.Static))
                    {
                        _typeLoadValidationErrors.Add((type, $"'{method}' is an instance method defined on the global <module> type"));
                        return false;
                    }
                    // Validate that a generic method cannot be on a ComImport class, or a ComEventInterface  -- UNIMPLEMENTED
                    // Validate that a generic method cannot be a p/invoke
                    if (method.IsPInvoke)
                    {
                        if (type.HasInstantiation)
                        {
                            _typeLoadValidationErrors.Add((type, $"'{method}' is an pinvoke defined on a generic type"));
                            return false;
                        }
                        if (method.HasInstantiation)
                        {
                            _typeLoadValidationErrors.Add((type, $"'{method}' is an pinvoke defined as a generic method"));
                            return false;
                        }
                    }
                    // Validate that outside of CoreLib, that a generic method cannot be an internal call method
                    if (type.Context.SystemModule != type.Module && method.IsInternalCall)
                    {
                        if (method.HasInstantiation)
                        {
                            _typeLoadValidationErrors.Add((type, $"'{method}' is an internal call generic method"));
                            return false;
                        }
                        if (type.HasInstantiation)
                        {
                            _typeLoadValidationErrors.Add((type, $"'{method}' is an internal call method on generic type"));
                            return false;
                        }
                    }
                    // Validate that a generic method cannot be marked as runtime
                    if (method.HasInstantiation && method.IsRuntimeImplemented)
                    {
                        _typeLoadValidationErrors.Add((type, $"'{method}' is an runtime-impl generic method"));
                        return false;
                    }
                    // Validate that generic variance is properly respected in method signatures -- UNIMPLEMENTED
                    // Validate that there are no cyclical method constraints -- UNIMPLEMENTED
                    // Validate that constraints are all acccessible to the method using them -- UNIMPLEMENTED

                }

                // Generic class special rules
                // Validate that a generic class cannot be a ComImport class, or a ComEventInterface class -- UNIMPLEMENTED
                // Validate that there are no cyclical class or method constraints, and that constraints are all acccessible to the type using them -- UNIMPLEMENTED

                // Override rules
                // Validate that for every override involving generic methods that the generic method constraints are matching. -- UNIMPLEMENTED
                // Validate that each override results does not violate accessibility rules
                
                foreach (var methodImplHandle in typeDef.GetMethodImplementations())
                {
                    var methodImpl = type.MetadataReader.GetMethodImplementation(methodImplHandle);
                    var methodBody = type.EcmaModule.GetMethod(methodImpl.MethodBody);
                    var methodDecl = type.EcmaModule.GetMethod(methodImpl.MethodDeclaration);

                    // Validate that all MethodImpls actually match signatures closely enough
                    if (!methodDecl.Signature.EqualsWithCovariantReturnType(methodBody.Signature))
                    {
                        _typeLoadValidationErrors.Add((type, $"MethodImpl with Body '{methodBody}' and Decl '{methodDecl}' do not have matching signatures"));
                        return false;
                    }

                    if (!methodDecl.IsVirtual)
                    {
                        _typeLoadValidationErrors.Add((type, $"MethodImpl with Decl '{methodDecl}' points at non-virtual decl method"));
                        return false;
                    }

                    if (!methodDecl.IsFinal)
                    {
                        _typeLoadValidationErrors.Add((type, $"MethodImpl with Decl '{methodDecl}' points at sealed decl method"));
                        return false;
                    }

                    bool isStatic = methodBody.Signature.IsStatic;
                    if (methodBody.OwningType.IsInterface && !isStatic && !methodBody.IsFinal)
                    {
                        _typeLoadValidationErrors.Add((type, $"MethodImpl with Body '{methodBody}' and Decl '{methodDecl}' implements interface on another interface with a non-sealed method"));
                        return false;
                    }

                    if (isStatic && methodBody.IsVirtual)
                    {
                        _typeLoadValidationErrors.Add((type, $"MethodImpl with Body '{methodBody}' and Decl '{methodDecl}' implements a static virtual method with a virtual static method"));
                        return false;
                    }

                    if (!isStatic && !methodBody.IsVirtual)
                    {
                        _typeLoadValidationErrors.Add((type, $"MethodImpl with Body '{methodBody}' and Decl '{methodDecl}' implements a instance virtual method with a non-virtual instance method"));
                        return false;
                    }

                    // Validate that the MethodImpl follows MethodImpl accessibility rules -- UNIMPLEMENTED
                }

                // Validate that all virtual static methods are actually implemented if the type is not abstract
                // Validate that all virtual instance methods are actually implemented if the type is concrete (Not abstract or interface)
                bool isConcrete = !type.IsInterface && !type.IsAbstract;
                var virtualMethodAlgorithm = type.Context.GetVirtualMethodAlgorithmForType(type);
                foreach (var interfaceImplemented in type.RuntimeInterfaces)
                {
                    foreach (var interfaceMethod in interfaceImplemented.GetVirtualMethods())
                    {
                        if (interfaceMethod.Signature.IsStatic)
                        {
                            var resolvedMethod = virtualMethodAlgorithm.ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod, type);
                            if (null == resolvedMethod || resolvedMethod.IsAbstract)
                            {
                                if (virtualMethodAlgorithm.ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethod, type, out var impl) != DefaultInterfaceMethodResolution.DefaultImplementation || impl.IsAbstract)
                                {
                                    _typeLoadValidationErrors.Add((type, $"Interface method '{interfaceMethod}' does not have implementation"));
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            if (null == virtualMethodAlgorithm.ResolveInterfaceMethodToVirtualMethodOnType(interfaceMethod, type))
                            {
                                _typeLoadValidationErrors.Add((type, $"Interface method '{interfaceMethod}' does not have implementation"));
                                return false;
                            }
                        }
                    }
                }

                foreach (var virtualMethod in type.GetAllVirtualMethods())
                {
                    var implementationMethod = virtualMethodAlgorithm.FindVirtualFunctionTargetMethodOnObjectType(virtualMethod, type);
                    if (implementationMethod == null ||  implementationMethod.IsAbstract)
                    {
                        _typeLoadValidationErrors.Add((type, $"Interface method '{virtualMethod}' does not have implementation"));
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // If we throw an exception, clearly type validation skipping was not to be
                _typeLoadValidationErrors.Add((type, ex.ToString()));
                return false;
            }
        }
    }
}
