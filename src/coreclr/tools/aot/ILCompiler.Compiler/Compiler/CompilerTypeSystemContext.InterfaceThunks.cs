// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
    public partial class CompilerTypeSystemContext
    {
        /// <summary>
        /// For a shared (canonical) default interface method, gets a method that can be used to call the
        /// method on a specific implementing class.
        /// </summary>
        public MethodDesc GetDefaultInterfaceMethodImplementationThunk(MethodDesc targetMethod, TypeDesc implementingClass, DefType interfaceOnDefinition, out int interfaceIndex)
        {
            Debug.Assert(targetMethod.IsSharedByGenericInstantiations);
            Debug.Assert(!targetMethod.Signature.IsStatic);
            Debug.Assert(!targetMethod.HasInstantiation);
            Debug.Assert(interfaceOnDefinition.GetTypeDefinition() == targetMethod.OwningType.GetTypeDefinition());
            Debug.Assert(targetMethod.OwningType.IsInterface);

            bool useContextFromRuntime = false;
            if (implementingClass.IsInterface)
            {
                Debug.Assert(((MetadataType)implementingClass).IsDynamicInterfaceCastableImplementation());
                useContextFromRuntime = true;
            }

            if (useContextFromRuntime && targetMethod.OwningType == implementingClass)
            {
                interfaceIndex = -1;
            }
            else
            {
                interfaceIndex = Array.IndexOf(implementingClass.GetTypeDefinition().RuntimeInterfaces, interfaceOnDefinition);
                Debug.Assert(interfaceIndex >= 0);
            }

            // Get a method that will inject the appropriate instantiation context to the
            // target default interface method.
            var methodKey = new DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey(targetMethod, interfaceIndex, useContextFromRuntime);
            MethodDesc thunk = _dimThunkHashtable.GetOrCreateValue(methodKey);

            return thunk;
        }

        private struct DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey
        {
            public readonly MethodDesc TargetMethod;
            public readonly int InterfaceIndex;
            public bool UseContextFromRuntime;

            public DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey(MethodDesc targetMethod, int interfaceIndex, bool useContextFromRuntime)
            {
                TargetMethod = targetMethod;
                InterfaceIndex = interfaceIndex;
                UseContextFromRuntime = useContextFromRuntime;
            }
        }

        private sealed class DefaultInterfaceMethodImplementationInstantiationThunkHashtable : LockFreeReaderHashtable<DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey, DefaultInterfaceMethodImplementationInstantiationThunk>
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
                return ReferenceEquals(key.TargetMethod, value.TargetMethod) &&
                    key.InterfaceIndex == value.InterfaceIndex &&
                    key.UseContextFromRuntime == value.UseContextFromRuntime;
            }
            protected override bool CompareValueToValue(DefaultInterfaceMethodImplementationInstantiationThunk value1, DefaultInterfaceMethodImplementationInstantiationThunk value2)
            {
                return ReferenceEquals(value1.TargetMethod, value2.TargetMethod) &&
                    value1.InterfaceIndex == value2.InterfaceIndex &&
                    value1.UseContextFromRuntime == value2.UseContextFromRuntime;
            }
            protected override DefaultInterfaceMethodImplementationInstantiationThunk CreateValueFromKey(DefaultInterfaceMethodImplementationInstantiationThunkHashtableKey key)
            {
                TypeDesc owningTypeOfThunks = ((CompilerTypeSystemContext)key.TargetMethod.Context).GeneratedAssembly.GetGlobalModuleType();
                return new DefaultInterfaceMethodImplementationInstantiationThunk(owningTypeOfThunks, key.TargetMethod, key.InterfaceIndex, key.UseContextFromRuntime);
            }
        }
        private DefaultInterfaceMethodImplementationInstantiationThunkHashtable _dimThunkHashtable = new DefaultInterfaceMethodImplementationInstantiationThunkHashtable();

        /// <summary>
        /// Represents a thunk to call shared instance method on generic interfaces.
        /// </summary>
        private sealed partial class DefaultInterfaceMethodImplementationInstantiationThunk : ILStubMethod, IPrefixMangledMethod
        {
            private readonly MethodDesc _targetMethod;
            private readonly TypeDesc _owningType;
            private readonly int _interfaceIndex;
            private readonly bool _useContextFromRuntime;
            private readonly byte[] _prefix;

            public DefaultInterfaceMethodImplementationInstantiationThunk(TypeDesc owningType, MethodDesc targetMethod, int interfaceIndex, bool useContextFromRuntime)
            {
                Debug.Assert(targetMethod.OwningType.IsInterface);
                Debug.Assert(!targetMethod.Signature.IsStatic);

                _owningType = owningType;
                _targetMethod = targetMethod;
                _interfaceIndex = interfaceIndex;
                _useContextFromRuntime = useContextFromRuntime;

                string prefixString = $"__InstantiatingStub_{(uint)interfaceIndex}_{(useContextFromRuntime ? "_FromRuntime" : "")}_";
                _prefix = System.Text.Encoding.UTF8.GetBytes(prefixString);
            }

            public override TypeSystemContext Context => _targetMethod.Context;

            public override TypeDesc OwningType => _owningType;

            public int InterfaceIndex => _interfaceIndex;

            public bool UseContextFromRuntime => _useContextFromRuntime;

            public override MethodSignature Signature => _targetMethod.Signature;

            public MethodDesc TargetMethod => _targetMethod;

            public override ReadOnlySpan<byte> Name
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

            public ReadOnlySpan<byte> Prefix => _prefix;

            public override MethodIL EmitIL()
            {
                // TODO: (async) https://github.com/dotnet/runtime/issues/121781
                if (_targetMethod.IsAsyncCall())
                {
                    ILEmitter e = new ILEmitter();
                    ILCodeStream c = e.NewCodeStream();

                    c.EmitCallThrowHelper(e, Context.GetCoreLibEntryPoint("System.Runtime"u8, "InternalCalls"u8, "RhpFallbackFailFast"u8, null));
                    return e.Link(this);
                }

                // Generate the instantiating stub. This loosely corresponds to following C#:
                // return Interface.Method(this, GetOrdinalInterface(this.m_pEEType, Index), [rest of parameters])

                ILEmitter emit = new ILEmitter();
                ILCodeStream codeStream = emit.NewCodeStream();

                FieldDesc eeTypeField = Context.GetWellKnownType(WellKnownType.Object).GetKnownField("m_pEEType"u8);
                MethodDesc getOrdinalInterfaceMethod = Context.GetHelperEntryPoint("SharedCodeHelpers"u8, "GetOrdinalInterface"u8);
                MethodDesc getCurrentContext = Context.GetHelperEntryPoint("SharedCodeHelpers"u8, "GetCurrentSharedThunkContext"u8);

                // Load "this"
                codeStream.EmitLdArg(0);

                // Load the instantiating argument.
                if (_useContextFromRuntime)
                {
                    codeStream.Emit(ILOpcode.call, emit.NewToken(getCurrentContext));
                }
                else
                {
                    codeStream.EmitLdArg(0);
                    codeStream.Emit(ILOpcode.ldfld, emit.NewToken(eeTypeField));
                }

                if (_interfaceIndex >= 0)
                {
                    codeStream.EmitLdc(_interfaceIndex);
                    codeStream.Emit(ILOpcode.call, emit.NewToken(getOrdinalInterfaceMethod));
                }

                codeStream.Emit(ILOpcode.call, emit.NewToken(Context.GetCoreLibEntryPoint("System.Runtime.CompilerServices"u8, "RuntimeHelpers"u8, "SetNextCallGenericContext"u8, null)));

                // Load rest of the arguments
                for (int i = 0; i < _targetMethod.Signature.Length; i++)
                {
                    codeStream.EmitLdArg(i + 1);
                }

                codeStream.Emit(ILOpcode.call, emit.NewToken(_targetMethod));
                codeStream.Emit(ILOpcode.ret);

                return emit.Link(this);
            }
        }
    }
}
