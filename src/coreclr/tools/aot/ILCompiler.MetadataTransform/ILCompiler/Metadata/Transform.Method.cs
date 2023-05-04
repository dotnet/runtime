// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;
using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;
using SignatureCallingConvention = Internal.Metadata.NativeFormat.SignatureCallingConvention;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        internal EntityMap<Cts.MethodDesc, MetadataRecord> _methods
            = new EntityMap<Cts.MethodDesc, MetadataRecord>(EqualityComparer<Cts.MethodDesc>.Default);

        private Action<Cts.MethodDesc, Method> _initMethodDef;
        private Action<Cts.MethodDesc, MemberReference> _initMethodRef;
        private Action<Cts.MethodDesc, MethodInstantiation> _initMethodInst;

        public override MetadataRecord HandleQualifiedMethod(Cts.MethodDesc method)
        {
            MetadataRecord rec;

            if (method is Cts.InstantiatedMethod)
            {
                rec = HandleMethodInstantiation(method);
            }
            else if (method.IsTypicalMethodDefinition && _policy.GeneratesMetadata(method))
            {
                rec = new QualifiedMethod
                {
                    EnclosingType = (TypeDefinition)HandleType(method.OwningType),
                    Method = HandleMethodDefinition(method),
                };
            }
            else
            {
                rec = HandleMethodReference(method);
            }

            Debug.Assert(rec is QualifiedMethod || rec is MemberReference || rec is MethodInstantiation);

            return rec;
        }

        private Method HandleMethodDefinition(Cts.MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            Debug.Assert(_policy.GeneratesMetadata(method));
            return (Method)_methods.GetOrCreate(method, _initMethodDef ??= InitializeMethodDefinition);
        }

        private void InitializeMethodDefinition(Cts.MethodDesc entity, Method record)
        {
            record.Name = HandleString(entity.Name);
            record.Signature = HandleMethodSignature(entity.Signature);

            if (entity.HasInstantiation)
            {
                record.GenericParameters.Capacity = entity.Instantiation.Length;
                foreach (var p in entity.Instantiation)
                    record.GenericParameters.Add(HandleGenericParameter((Cts.GenericParameterDesc)p));
            }

            var ecmaEntity = entity as Cts.Ecma.EcmaMethod;
            if (ecmaEntity != null)
            {
                Ecma.MetadataReader reader = ecmaEntity.MetadataReader;
                Ecma.MethodDefinition methodDef = reader.GetMethodDefinition(ecmaEntity.Handle);
                Ecma.ParameterHandleCollection paramHandles = methodDef.GetParameters();

                record.Parameters.Capacity = paramHandles.Count;
                foreach (var paramHandle in paramHandles)
                {
                    Ecma.Parameter param = reader.GetParameter(paramHandle);
                    Parameter paramRecord = new Parameter
                    {
                        Flags = param.Attributes,
                        Name = HandleString(reader.GetString(param.Name)),
                        Sequence = checked((ushort)param.SequenceNumber)
                    };

                    Ecma.ConstantHandle defaultValue = param.GetDefaultValue();
                    if (!defaultValue.IsNil)
                    {
                        paramRecord.DefaultValue = HandleConstant(ecmaEntity.Module, defaultValue);
                    }

                    Ecma.CustomAttributeHandleCollection paramAttributes = param.GetCustomAttributes();
                    if (paramAttributes.Count > 0)
                    {
                        paramRecord.CustomAttributes = HandleCustomAttributes(ecmaEntity.Module, paramAttributes);
                    }

                    record.Parameters.Add(paramRecord);
                }

                Ecma.CustomAttributeHandleCollection attributes = methodDef.GetCustomAttributes();
                if (attributes.Count > 0)
                {
                    record.CustomAttributes = HandleCustomAttributes(ecmaEntity.Module, attributes);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            record.Flags = GetMethodAttributes(entity);
            record.ImplFlags = GetMethodImplAttributes(entity);

            //TODO: RVA
        }

        private MemberReference HandleMethodReference(Cts.MethodDesc method)
        {
            Debug.Assert(method.IsMethodDefinition);
            return (MemberReference)_methods.GetOrCreate(method, _initMethodRef ??= InitializeMethodReference);
        }

        private void InitializeMethodReference(Cts.MethodDesc entity, MemberReference record)
        {
            record.Name = HandleString(entity.Name);
            record.Parent = HandleType(entity.OwningType);
            record.Signature = HandleMethodSignature(entity.GetTypicalMethodDefinition().Signature);
        }

        private MethodInstantiation HandleMethodInstantiation(Cts.MethodDesc method)
        {
            return (MethodInstantiation)_methods.GetOrCreate(method, _initMethodInst ??= InitializeMethodInstantiation);
        }

        private void InitializeMethodInstantiation(Cts.MethodDesc entity, MethodInstantiation record)
        {
            Cts.InstantiatedMethod instantiation = (Cts.InstantiatedMethod)entity;
            record.Method = HandleQualifiedMethod(instantiation.GetMethodDefinition());
            record.GenericTypeArguments.Capacity = instantiation.Instantiation.Length;
            foreach (Cts.TypeDesc typeArgument in instantiation.Instantiation)
            {
                record.GenericTypeArguments.Add(HandleType(typeArgument));
            }
        }

        public override MethodSignature HandleMethodSignature(Cts.MethodSignature signature)
        {
            // TODO: if Cts.MethodSignature implements Equals/GetHashCode, we could enable pooling here.

            var result = new MethodSignature
            {
                CallingConvention = GetSignatureCallingConvention(signature),
                GenericParameterCount = signature.GenericParameterCount,
                ReturnType = HandleType(signature.ReturnType),
                // TODO-NICE: VarArgParameters
            };

            result.Parameters.Capacity = signature.Length;
            for (int i = 0; i < signature.Length; i++)
            {
                result.Parameters.Add(HandleType(signature[i]));
            }

            return result;
        }

        private static MethodAttributes GetMethodAttributes(Cts.MethodDesc method)
        {
            var ecmaMethod = method as Cts.Ecma.EcmaMethod;
            if (ecmaMethod != null)
            {
                Ecma.MetadataReader reader = ecmaMethod.MetadataReader;
                Ecma.MethodDefinition methodDef = reader.GetMethodDefinition(ecmaMethod.Handle);
                return methodDef.Attributes;
            }
            else
                throw new NotImplementedException();
        }

        private static MethodImplAttributes GetMethodImplAttributes(Cts.MethodDesc method)
        {
            var ecmaMethod = method as Cts.Ecma.EcmaMethod;
            if (ecmaMethod != null)
            {
                Ecma.MetadataReader reader = ecmaMethod.MetadataReader;
                Ecma.MethodDefinition methodDef = reader.GetMethodDefinition(ecmaMethod.Handle);
                return methodDef.ImplAttributes;
            }
            else
                throw new NotImplementedException();
        }

        private static SignatureCallingConvention GetSignatureCallingConvention(Cts.MethodSignature signature)
        {
            Debug.Assert((int)Cts.MethodSignatureFlags.UnmanagedCallingConventionCdecl == (int)SignatureCallingConvention.Cdecl);
            Debug.Assert((int)Cts.MethodSignatureFlags.UnmanagedCallingConventionThisCall == (int)SignatureCallingConvention.ThisCall);
            Debug.Assert((int)Cts.MethodSignatureFlags.UnmanagedCallingConventionMask == (int)SignatureCallingConvention.UnmanagedCallingConventionMask);
            SignatureCallingConvention callingConvention = (SignatureCallingConvention)(signature.Flags & Cts.MethodSignatureFlags.UnmanagedCallingConventionMask);
            if ((signature.Flags & Cts.MethodSignatureFlags.Static) == 0)
            {
                callingConvention |= SignatureCallingConvention.HasThis;
            }
            return callingConvention;
        }
    }
}
