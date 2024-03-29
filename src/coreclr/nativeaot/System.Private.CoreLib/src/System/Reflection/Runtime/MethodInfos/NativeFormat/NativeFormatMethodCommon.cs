// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Runtime.CustomAttributes;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.ParameterInfos.NativeFormat;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core.Execution;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System.Reflection.Runtime.MethodInfos.NativeFormat
{
    //
    // Implements methods and properties common to RuntimeMethodInfo and RuntimeConstructorInfo.
    //
    internal struct NativeFormatMethodCommon : IRuntimeMethodCommon<NativeFormatMethodCommon>, IEquatable<NativeFormatMethodCommon>
    {
        public bool IsGenericMethodDefinition => GenericParameterCount != 0;

        public MethodBaseInvoker GetUncachedMethodInvoker(RuntimeTypeInfo[] methodArguments, MemberInfo exceptionPertainant, out Exception exception)
        {
            return ReflectionCoreExecution.ExecutionEnvironment.GetMethodInvoker(DeclaringType, new QMethodDefinition(Reader, MethodHandle), methodArguments, exceptionPertainant, out exception);
        }

        public QSignatureTypeHandle[] QualifiedMethodSignature
        {
            get
            {
                MethodSignature methodSignature = this.MethodSignature;

                QSignatureTypeHandle[] typeSignatures = new QSignatureTypeHandle[methodSignature.Parameters.Count + 1];
                typeSignatures[0] = new QSignatureTypeHandle(_reader, methodSignature.ReturnType, true);
                int paramIndex = 1;
                foreach (Handle parameterTypeSignatureHandle in methodSignature.Parameters)
                {
                    typeSignatures[paramIndex++] = new QSignatureTypeHandle(_reader, parameterTypeSignatureHandle, true);
                }

                return typeSignatures;
            }
        }

        public NativeFormatMethodCommon RuntimeMethodCommonOfUninstantiatedMethod
        {
            get
            {
                return new NativeFormatMethodCommon(MethodHandle, _definingTypeInfo, _definingTypeInfo);
            }
        }

        public void FillInMetadataDescribedParameters(ref VirtualRuntimeParameterInfoArray result, QSignatureTypeHandle[] typeSignatures, MethodBase contextMethod, TypeContext typeContext)
        {
            foreach (ParameterHandle parameterHandle in _method.Parameters)
            {
                Parameter parameterRecord = parameterHandle.GetParameter(_reader);
                int index = parameterRecord.Sequence;
                result[index] =
                    NativeFormatMethodParameterInfo.GetNativeFormatMethodParameterInfo(
                        contextMethod,
                        index - 1,
                        parameterHandle,
                        typeSignatures[index],
                        typeContext);
            }
        }

        public int GenericParameterCount => MethodHandle.GetMethod(Reader).GenericParameters.Count;

        public RuntimeTypeInfo[] GetGenericTypeParametersWithSpecifiedOwningMethod(RuntimeNamedMethodInfo<NativeFormatMethodCommon> owningMethod)
        {
            Method method = MethodHandle.GetMethod(Reader);
            int genericParametersCount = method.GenericParameters.Count;
            if (genericParametersCount == 0)
                return Array.Empty<RuntimeTypeInfo>();

            RuntimeTypeInfo[] genericTypeParameters = new RuntimeTypeInfo[genericParametersCount];
            int i = 0;
            foreach (GenericParameterHandle genericParameterHandle in method.GenericParameters)
            {
                RuntimeTypeInfo genericParameterType = NativeFormatRuntimeGenericParameterTypeInfoForMethods.GetRuntimeGenericParameterTypeInfoForMethods(owningMethod, Reader, genericParameterHandle);
                genericTypeParameters[i++] = genericParameterType;
            }
            return genericTypeParameters;
        }

        //
        // methodHandle    - the "tkMethodDef" that identifies the method.
        // definingType   - the "tkTypeDef" that defined the method (this is where you get the metadata reader that created methodHandle.)
        // contextType    - the type that supplies the type context (i.e. substitutions for generic parameters.) Though you
        //                  get your raw information from "definingType", you report "contextType" as your DeclaringType property.
        //
        //  For example:
        //
        //       typeof(Foo<>).GetTypeInfo().DeclaredMembers
        //
        //           The definingType and contextType are both Foo<>
        //
        //       typeof(Foo<int,String>).GetTypeInfo().DeclaredMembers
        //
        //          The definingType is "Foo<,>"
        //          The contextType is "Foo<int,String>"
        //
        //  We don't report any DeclaredMembers for arrays or generic parameters so those don't apply.
        //
        public NativeFormatMethodCommon(MethodHandle methodHandle, NativeFormatRuntimeNamedTypeInfo definingTypeInfo, RuntimeTypeInfo contextTypeInfo)
        {
            _definingTypeInfo = definingTypeInfo;
            _methodHandle = methodHandle;
            _contextTypeInfo = contextTypeInfo;
            _reader = definingTypeInfo.Reader;
            _method = methodHandle.GetMethod(_reader);
        }

        public MethodAttributes Attributes
        {
            get
            {
                return _method.Flags;
            }
        }

        public CallingConventions CallingConvention
        {
            get
            {
                var sigCallingConvention = MethodSignature.CallingConvention;
                return (sigCallingConvention & Internal.Metadata.NativeFormat.SignatureCallingConvention.HasThis) != 0
                    ? CallingConventions.HasThis : 0;
            }
        }

        public RuntimeTypeInfo ContextTypeInfo
        {
            get
            {
                return _contextTypeInfo;
            }
        }

        public RuntimeTypeInfo DeclaringType
        {
            get
            {
                return _contextTypeInfo;
            }
        }

        public RuntimeNamedTypeInfo DefiningTypeInfo
        {
            get
            {
                return _definingTypeInfo;
            }
        }

        public MethodImplAttributes MethodImplementationFlags
        {
            get
            {
                return _method.ImplFlags;
            }
        }

        public Module Module
        {
            get
            {
                return _definingTypeInfo.Module;
            }
        }

        public int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        public RuntimeMethodHandle GetRuntimeMethodHandle(Type[] genericArgs)
        {
            Debug.Assert(genericArgs == null || genericArgs.Length > 0);

            RuntimeTypeHandle[]? genericArgHandles;
            if (genericArgs != null)
            {
                genericArgHandles = new RuntimeTypeHandle[genericArgs.Length];
                for (int i = 0; i < genericArgHandles.Length; i++)
                    genericArgHandles[i] = genericArgs[i].TypeHandle;
            }
            else
            {
                genericArgHandles = null;
            }

            TypeManagerHandle typeManager = RuntimeAugments.TypeLoaderCallbacks.GetModuleForMetadataReader(Reader);

            return RuntimeAugments.TypeLoaderCallbacks.GetRuntimeMethodHandleForComponents(
                DeclaringType.TypeHandle,
                Name,
                RuntimeSignature.CreateFromMethodHandle(typeManager, MethodHandle.AsInt()),
                genericArgHandles);
        }

        public string Name
        {
            get
            {
                return _method.Name.GetString(_reader);
            }
        }

        public MetadataReader Reader
        {
            get
            {
                return _reader;
            }
        }

        public MethodHandle MethodHandle
        {
            get
            {
                return _methodHandle;
            }
        }

        public bool HasSameMetadataDefinitionAs(NativeFormatMethodCommon other)
        {
            if (!(_reader == other._reader))
                return false;
            if (!(_methodHandle.Equals(other._methodHandle)))
                return false;
            if (!(_definingTypeInfo.Equals(other._definingTypeInfo)))
                return false;
            return true;
        }

        public IEnumerable<CustomAttributeData> TrueCustomAttributes => RuntimeCustomAttributeData.GetCustomAttributes(_reader, _method.CustomAttributes);

        public override bool Equals(object obj)
        {
            if (!(obj is NativeFormatMethodCommon other))
                return false;
            return Equals(other);
        }

        public bool Equals(NativeFormatMethodCommon other)
        {
            if (!(_reader == other._reader))
                return false;
            if (!(_methodHandle.Equals(other._methodHandle)))
                return false;
            if (!(_contextTypeInfo.Equals(other._contextTypeInfo)))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return _methodHandle.GetHashCode() ^ _contextTypeInfo.GetHashCode();
        }

        private MethodSignature MethodSignature
        {
            get
            {
                return _method.Signature.GetMethodSignature(_reader);
            }
        }

        private readonly NativeFormatRuntimeNamedTypeInfo _definingTypeInfo;
        private readonly MethodHandle _methodHandle;
        private readonly RuntimeTypeInfo _contextTypeInfo;

        private readonly MetadataReader _reader;

        private readonly Method _method;
    }
}
