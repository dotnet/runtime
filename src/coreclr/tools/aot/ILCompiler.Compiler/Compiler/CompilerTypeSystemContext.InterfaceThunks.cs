// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Internal.TypeSystem;
using Internal.IL;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

// Default interface method implementation thunks
//
// The problem with default interface methods and shared generic code is that for:
//
// interface IFoo<T>
// {
//     Type GetTheType() => typeof(T);
// }
//
// The actual generated code when instantiated over a shareable instance (like object) is
// just IFoo<__Canon>.GetTheType, for any shareable argument (there's no unique code
// generated for IFoo<Object>/IFoo<string>/... - we just have IFoo<__Canon>).
//
// For the canonical code to know what the actual T is, we need to provide the instantiation
// context somehow. We can't easily get it from `this` like we do for reference types
// since the type might implement multiple instantiations of IFoo (`class Abc : IFoo<object>, IFoo<string> { }`)
// and we wouldn't know which one we are executing for within the method body.
//
// So we end up passing the context same as for shared valuetype code (that also cannot
// determine context from just `this`) - by adding an extra instantiation
// argument. The actual code for IFoo<__Canon>.GetTheType looks something like this:
//
// Type IFoo__Canon__GetTheType(IFoo<__Canon> instance, MethodTable* context)
// {
//     return Type.GetTypeFromHandle(GetTypeHandleOfTInIFooCanon(context));
// }
//
// Now we have a problem because this method expects an extra `context` argument
// that will not be provided at the callsite, since the callsite doesn't know
// where it will dispatch to (could be a non-default-interface-method).
//
// We solve this with an instantiating thunk. The instantiating thunk is the thing
// we place in the vtable of the implementing type. The thunk looks like this:
//
// Type Abc_IFoo__Canon__GetTheType_Thunk(IFoo<__Canon> instance)
// {
//     return IFoo__Canon__GetTheType(instance, GetOrdinalInterface(instance.m_pEEType, 0));
// }
//
// Notice the thunk now has the expected signature, and some code to compute the context.
//
// The GetOrdinalInterface method retrieves the specified interface MethodTable off the MethodTable's interface list.
// The thunks are per-type (since the position in the interface list is different).
//
// We hardcode the position in the interface list instead of just hardcoding the interface type
// itself so that we don't require runtime code generation when a new type is loaded
// (e.g. "class Abc<T> : IFoo<T> { }" and we MakeGenericType's a new Abc at runtime) -
// the instantiating thunk in this shape can be shared.
namespace ILCompiler
{
    // Contains functionality related to instantiating thunks for default interface methods
    partial class CompilerTypeSystemContext
    {
        private const int UseContextFromRuntime = -1;

        /// <summary>
        /// For a shared (canonical) default interface method, gets a method that can be used to call the
        /// method on a specific implementing class.
        /// </summary>
        public MethodDesc GetDefaultInterfaceMethodImplementationThunk(MethodDesc targetMethod, TypeDesc implementingClass, DefType interfaceOnDefinition)
        {
            Debug.Assert(targetMethod.IsSharedByGenericInstantiations);
            Debug.Assert(!targetMethod.Signature.IsStatic);
            Debug.Assert(!targetMethod.HasInstantiation);
            Debug.Assert(interfaceOnDefinition.GetTypeDefinition() == targetMethod.OwningType.GetTypeDefinition());
            Debug.Assert(targetMethod.OwningType.IsInterface);

            int interfaceIndex;
            if (implementingClass.IsInterface)
            {
                Debug.Assert(((MetadataType)implementingClass).IsDynamicInterfaceCastableImplementation());
                interfaceIndex = UseContextFromRuntime;
            }
            else
            {
                interfaceIndex = Array.IndexOf(implementingClass.GetTypeDefinition().RuntimeInterfaces, interfaceOnDefinition);
                Debug.Assert(interfaceIndex >= 0);
            }

            // Get a method that will inject the appropriate instantiation context to the
            // target default interface method.
            var methodKey = new DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey(targetMethod, interfaceIndex);
            MethodDesc thunk = _dimThunkHashtable.GetOrCreateValue(methodKey);

            return thunk;
        }

        /// <summary>
        /// Returns true of <paramref name="method"/> is a standin method for instantiating thunk target.
        /// </summary>
        public bool IsDefaultInterfaceMethodImplementationThunkTargetMethod(MethodDesc method)
        {
            return method.GetTypicalMethodDefinition().GetType() == typeof(DefaultInterfaceMethodImplementationWithHiddenParameter);
        }

        /// <summary>
        /// Returns the real target method of an instantiating thunk.
        /// </summary>
        public MethodDesc GetRealDefaultInterfaceMethodImplementationThunkTargetMethod(MethodDesc method)
        {
            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();
            return ((DefaultInterfaceMethodImplementationWithHiddenParameter)typicalMethod).MethodRepresented;
        }

        private struct DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey
        {
            public readonly MethodDesc TargetMethod;
            public readonly int InterfaceIndex;

            public DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey(MethodDesc targetMethod, int interfaceIndex)
            {
                TargetMethod = targetMethod;
                InterfaceIndex = interfaceIndex;
            }
        }

        private class DefaultInterfaceMethodImplementationInstantiationThunkHashtable : LockFreeReaderHashtable<DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey, DefaultInterfaceMethodImplementationInstantiationThunk>
        {
            protected override int GetKeyHashCode(DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey key)
            {
                return key.TargetMethod.GetHashCode() ^ key.InterfaceIndex;
            }
            protected override int GetValueHashCode(DefaultInterfaceMethodImplementationInstantiationThunk value)
            {
                return value.TargetMethod.GetHashCode() ^ value.InterfaceIndex;
            }
            protected override bool CompareKeyToValue(DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey key, DefaultInterfaceMethodImplementationInstantiationThunk value)
            {
                return Object.ReferenceEquals(key.TargetMethod, value.TargetMethod) &&
                    key.InterfaceIndex == value.InterfaceIndex;
            }
            protected override bool CompareValueToValue(DefaultInterfaceMethodImplementationInstantiationThunk value1, DefaultInterfaceMethodImplementationInstantiationThunk value2)
            {
                return Object.ReferenceEquals(value1.TargetMethod, value2.TargetMethod) &&
                    value1.InterfaceIndex == value2.InterfaceIndex;
            }
            protected override DefaultInterfaceMethodImplementationInstantiationThunk CreateValueFromKey(DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey key)
            {
                TypeDesc owningTypeOfThunks = ((CompilerTypeSystemContext)key.TargetMethod.Context).GeneratedAssembly.GetGlobalModuleType();
                return new DefaultInterfaceMethodImplementationInstantiationThunk(owningTypeOfThunks, key.TargetMethod, key.InterfaceIndex);
            }
        }
        private DefaultInterfaceMethodImplementationInstantiationThunkHashtable _dimThunkHashtable = new DefaultInterfaceMethodImplementationInstantiationThunkHashtable();

        /// <summary>
        /// Represents a thunk to call shared instance method on generic interfaces.
        /// </summary>
        private partial class DefaultInterfaceMethodImplementationInstantiationThunk : ILStubMethod, IPrefixMangledMethod
        {
            private readonly MethodDesc _targetMethod;
            private readonly DefaultInterfaceMethodImplementationWithHiddenParameter _nakedTargetMethod;
            private readonly TypeDesc _owningType;
            private readonly int _interfaceIndex;

            public DefaultInterfaceMethodImplementationInstantiationThunk(TypeDesc owningType, MethodDesc targetMethod, int interfaceIndex)
            {
                Debug.Assert(targetMethod.OwningType.IsInterface);
                Debug.Assert(!targetMethod.Signature.IsStatic);

                _owningType = owningType;
                _targetMethod = targetMethod;
                _nakedTargetMethod = new DefaultInterfaceMethodImplementationWithHiddenParameter(targetMethod, owningType);
                _interfaceIndex = interfaceIndex;
            }

            public override TypeSystemContext Context => _targetMethod.Context;

            public override TypeDesc OwningType => _owningType;

            public int InterfaceIndex => _interfaceIndex;

            public override MethodSignature Signature => _targetMethod.Signature;

            public MethodDesc TargetMethod => _targetMethod;

            public override string Name
            {
                get
                {
                    return _targetMethod.Name;
                }
            }

            public override string DiagnosticName
            {
                get
                {
                    return _targetMethod.DiagnosticName;
                }
            }

            public MethodDesc BaseMethod => _targetMethod;

            public string Prefix => $"__InstantiatingStub_{_interfaceIndex}_";

            public override MethodIL EmitIL()
            {
                // Generate the instantiating stub. This loosely corresponds to following C#:
                // return Interface.Method(this, GetOrdinalInterface(this.m_pEEType, Index), [rest of parameters])

                ILEmitter emit = new ILEmitter();
                ILCodeStream codeStream = emit.NewCodeStream();

                FieldDesc eeTypeField = Context.GetWellKnownType(WellKnownType.Object).GetKnownField("m_pEEType");
                MethodDesc getOrdinalInterfaceMethod = Context.GetHelperEntryPoint("SharedCodeHelpers", "GetOrdinalInterface");
                MethodDesc getCurrentContext = Context.GetHelperEntryPoint("SharedCodeHelpers", "GetCurrentSharedThunkContext");

                // Load "this"
                codeStream.EmitLdArg(0);

                // Load the instantiating argument.
                if (_interfaceIndex == UseContextFromRuntime)
                {
                    codeStream.Emit(ILOpcode.call, emit.NewToken(getCurrentContext));
                }
                else
                {
                    codeStream.EmitLdArg(0);
                    codeStream.Emit(ILOpcode.ldfld, emit.NewToken(eeTypeField));
                    codeStream.EmitLdc(_interfaceIndex);
                    codeStream.Emit(ILOpcode.call, emit.NewToken(getOrdinalInterfaceMethod));
                }

                // Load rest of the arguments
                for (int i = 0; i < _targetMethod.Signature.Length; i++)
                {
                    codeStream.EmitLdArg(i + 1);
                }

                // Call an instance method on the target interface that has a fake instantiation parameter
                // in it's signature. This will be swapped by the actual instance method after codegen is done.
                codeStream.Emit(ILOpcode.call, emit.NewToken(_nakedTargetMethod));
                codeStream.Emit(ILOpcode.ret);

                return emit.Link(this);
            }
        }

        /// <summary>
        /// Represents an instance method on a generic interface with an explicit instantiation parameter in the
        /// signature. This is so that we can refer to the parameter from IL. References to this method will
        /// be replaced by the actual instance method after codegen is done.
        /// </summary>
        internal partial class DefaultInterfaceMethodImplementationWithHiddenParameter : MethodDesc
        {
            private readonly MethodDesc _methodRepresented;
            private readonly TypeDesc _owningType;
            private MethodSignature _signature;

            public DefaultInterfaceMethodImplementationWithHiddenParameter(MethodDesc methodRepresented, TypeDesc owningType)
            {
                Debug.Assert(methodRepresented.OwningType.IsInterface);
                Debug.Assert(!methodRepresented.Signature.IsStatic);

                _methodRepresented = methodRepresented;
                _owningType = owningType;
            }

            public MethodDesc MethodRepresented => _methodRepresented;

            // We really don't want this method to be inlined.
            public override bool IsNoInlining => true;

            public override bool IsInternalCall => true;

            public override bool IsIntrinsic => true;

            public override TypeSystemContext Context => _methodRepresented.Context;
            public override TypeDesc OwningType => _owningType;

            public override string Name => _methodRepresented.Name;
            public override string DiagnosticName => _methodRepresented.DiagnosticName;

            public override MethodSignature Signature
            {
                get
                {
                    if (_signature == null)
                    {
                        TypeDesc[] parameters = new TypeDesc[_methodRepresented.Signature.Length + 1];

                        // Shared instance methods on generic interfaces have a hidden parameter with the generic context.
                        // We add it to the signature so that we can refer to it from IL.
                        parameters[0] = Context.GetWellKnownType(WellKnownType.IntPtr);
                        for (int i = 0; i < _methodRepresented.Signature.Length; i++)
                            parameters[i + 1] = _methodRepresented.Signature[i];

                        _signature = new MethodSignature(_methodRepresented.Signature.Flags,
                            _methodRepresented.Signature.GenericParameterCount,
                            _methodRepresented.Signature.ReturnType,
                            parameters);
                    }

                    return _signature;
                }
            }

            public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;
        }
    }
}
