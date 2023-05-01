// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Internal.TypeSystem;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;
using Debug = System.Diagnostics.Debug;
using Internal.NativeFormat;
using System.Collections.Generic;
using Internal.TypeSystem.NoMetadata;
using System.Reflection.Runtime.General;

namespace Internal.TypeSystem
{
    public abstract partial class TypeDesc
    {
        private RuntimeTypeHandle _runtimeTypeHandle;
        public RuntimeTypeHandle RuntimeTypeHandle
        {
            get
            {
                return _runtimeTypeHandle;
            }
        }

        /// <summary>
        ///  Setter for RuntimeTypeHandle. Separate from normal property as all uses should be done with great care.
        ///  Must not be set with partially constructed type handles
        /// </summary>
        public void SetRuntimeTypeHandleUnsafe(RuntimeTypeHandle runtimeTypeHandle)
        {
            Debug.Assert(!runtimeTypeHandle.IsNull());
            Debug.Assert(_runtimeTypeHandle.IsNull() || runtimeTypeHandle.Equals(_runtimeTypeHandle));
            Debug.Assert(runtimeTypeHandle.GetHashCode() == GetHashCode());
            _runtimeTypeHandle = runtimeTypeHandle;
        }

        /// <summary>
        /// Get the RuntimeTypeHandle if possible and return it. Otherwise, return a null RuntimeTypeHandle
        /// </summary>
        public RuntimeTypeHandle GetRuntimeTypeHandle()
        {
            RetrieveRuntimeTypeHandleIfPossible();
            return RuntimeTypeHandle;
        }

        internal TypeBuilderState TypeBuilderState { get; set; }

#if DEBUG
        public string DebugName { get; set; }
#endif

        // Todo: This is looking up the hierarchy to DefType and ParameterizedType. It should really
        // call a virtual or an outside type to handle those parts
        internal bool RetrieveRuntimeTypeHandleIfPossible()
        {
            TypeDesc type = this;
            if (!type.RuntimeTypeHandle.IsNull())
                return true;

            TypeBuilderState state = GetTypeBuilderStateIfExist();
            if (state != null && state.AttemptedAndFailedToRetrieveTypeHandle)
                return false;

            if (type is DefType typeAsDefType)
            {
                TypeDesc typeDefinition = typeAsDefType.GetTypeDefinition();
                RuntimeTypeHandle typeDefHandle = typeDefinition.RuntimeTypeHandle;
                if (!typeDefHandle.IsNull())
                {
                    Instantiation instantiation = typeAsDefType.Instantiation;

                    if ((instantiation.Length > 0) && !typeAsDefType.IsGenericDefinition)
                    {
                        // Generic type. First make sure we have type handles for the arguments, then check
                        // the instantiation.
                        bool argumentsRegistered = true;
                        for (int i = 0; i < instantiation.Length; i++)
                        {
                            if (!instantiation[i].RetrieveRuntimeTypeHandleIfPossible())
                            {
                                argumentsRegistered = false;
                            }
                        }

                        RuntimeTypeHandle rtth;
                        if (argumentsRegistered && TypeLoaderEnvironment.Instance.TryLookupConstructedGenericTypeForComponents(new TypeLoaderEnvironment.GenericTypeLookupData(typeAsDefType), out rtth))
                        {
                            typeAsDefType.SetRuntimeTypeHandleUnsafe(rtth);
                            return true;
                        }
                    }
                    else
                    {
                        // Nongeneric, or generic type def types are just the type handle of the type definition as found above
                        type.SetRuntimeTypeHandleUnsafe(typeDefHandle);
                        return true;
                    }
                }
            }
            else if (type is ParameterizedType typeAsParameterType)
            {
                if (typeAsParameterType.ParameterType.RetrieveRuntimeTypeHandleIfPossible())
                {
                    RuntimeTypeHandle rtth;
                    if ((type is ArrayType &&
                          TypeLoaderEnvironment.TryGetArrayTypeForElementType_LookupOnly(typeAsParameterType.ParameterType.RuntimeTypeHandle, type.IsMdArray, type.IsMdArray ? ((ArrayType)type).Rank : -1, out rtth))
                           ||
                        (type is PointerType && TypeLoaderEnvironment.TryGetPointerTypeForTargetType_LookupOnly(typeAsParameterType.ParameterType.RuntimeTypeHandle, out rtth))
                           ||
                        (type is ByRefType && TypeLoaderEnvironment.TryGetByRefTypeForTargetType_LookupOnly(typeAsParameterType.ParameterType.RuntimeTypeHandle, out rtth)))
                    {
                        typeAsParameterType.SetRuntimeTypeHandleUnsafe(rtth);
                        return true;
                    }
                }
            }
            else if (type is SignatureVariable)
            {
                // SignatureVariables do not have RuntimeTypeHandles
            }
            else if (type is FunctionPointerType functionPointerType)
            {
                MethodSignature sig = functionPointerType.Signature;
                if (sig.ReturnType.RetrieveRuntimeTypeHandleIfPossible())
                {
                    RuntimeTypeHandle[] parameterHandles = new RuntimeTypeHandle[sig.Length];
                    bool handlesAvailable = true;
                    for (int i = 0; i < parameterHandles.Length; i++)
                    {
                        if (sig[i].RetrieveRuntimeTypeHandleIfPossible())
                        {
                            parameterHandles[i] = sig[i].RuntimeTypeHandle;
                        }
                        else
                        {
                            handlesAvailable = false;
                            break;
                        }
                    }

                    if (handlesAvailable
                        && TypeLoaderEnvironment.Instance.TryLookupFunctionPointerTypeForComponents(
                            sig.ReturnType.RuntimeTypeHandle, parameterHandles,
                            isUnmanaged: (sig.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) != 0,
                            out RuntimeTypeHandle rtth))
                    {
                        functionPointerType.SetRuntimeTypeHandleUnsafe(rtth);
                        return true;
                    }
                }
            }
            else
            {
                Debug.Assert(false);
            }

            // Make a note on the type build state that we have attempted to retrieve RuntimeTypeHandle but there is not one
            GetOrCreateTypeBuilderState().AttemptedAndFailedToRetrieveTypeHandle = true;

            return false;
        }

        internal TypeBuilderState GetTypeBuilderStateIfExist()
        {
            return (TypeBuilderState)TypeBuilderState;
        }

        //
        // Get existing type builder state. This method should be only called during final phase of type building.
        //
        internal TypeBuilderState GetTypeBuilderState()
        {
            TypeBuilderState state = (TypeBuilderState)TypeBuilderState;
            Debug.Assert(state != null);
            return state;
        }

        //
        // Get or create existing type builder state. This method should not be called during final phase of type building.
        //
        internal TypeBuilderState GetOrCreateTypeBuilderState()
        {
            TypeBuilderState state = (TypeBuilderState)TypeBuilderState;
            if (state == null)
            {
                state = new TypeBuilderState(this);
                TypeBuilderState = state;
                Context.RegisterTypeForTypeSystemStateFlushing(this);
            }
            return state;
        }

        /// Parse the native layout to ensure that the type has proper base type setup.
        /// This is used to generalize out some behavior of NoMetadataTypes which actually use this information
        internal virtual void ParseBaseType(NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, NativeParser baseTypeParser)
        {
            return;
        }

        internal TypeDesc ComputeTemplate(bool templateRequired = true)
        {
            return ComputeTemplate(GetOrCreateTypeBuilderState(), templateRequired);
        }

        internal static TypeDesc ComputeTemplate(TypeBuilderState state, bool templateRequired = true)
        {
            TypeDesc templateType = state.TemplateType;

            if (templateRequired && (templateType == null))
            {
                throw new TypeBuilder.MissingTemplateException();
            }

            return templateType;
        }

        internal bool IsTemplateUniversal()
        {
            TypeDesc templateType = ComputeTemplate(false);
            if (templateType == null)
                return false;
            else
                return templateType.IsCanonicalSubtype(CanonicalFormKind.Universal);
        }

        internal bool IsTemplateCanonical()
        {
            TypeDesc templateType = ComputeTemplate(false);
            if (templateType == null)
                return false;
            else
                return !templateType.IsCanonicalSubtype(CanonicalFormKind.Universal);
        }
    }
}
