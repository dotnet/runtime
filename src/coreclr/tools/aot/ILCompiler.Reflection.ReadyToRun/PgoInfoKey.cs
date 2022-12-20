// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Internal.Pgo;

namespace ILCompiler.Reflection.ReadyToRun
{
    public class PgoInfoKey : IEquatable<PgoInfoKey>
    {
        /// <summary>
        /// The type that the method belongs to
        /// </summary>
        public string DeclaringType { get; }

        public string Name { get; }

        /// <summary>
        /// The method metadata handle
        /// </summary>
        public EntityHandle MethodHandle { get; }

        public IAssemblyMetadata ComponentReader { get; }

        public MethodSignature<string> Signature { get; }

        /// <summary>
        /// The signature with format: namespace.class.methodName<S, T, ...>(S, T, ...)
        /// </summary>
        public string SignatureString { get; }

        public PgoInfoKey(IAssemblyMetadata componentReader, string owningType, EntityHandle methodHandle, string[] instanceArgs)
        {
            ComponentReader = componentReader;
            EntityHandle owningTypeHandle;
            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(typeParameters: Array.Empty<string>(), methodParameters: instanceArgs);
            DisassemblingTypeProvider typeProvider = new DisassemblingTypeProvider();
            MethodHandle = methodHandle;

            // get the method signature from the method handle
            switch (MethodHandle.Kind)
            {
                case HandleKind.MethodDefinition:
                    {
                        MethodDefinition methodDef = componentReader.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)MethodHandle);
                        Name = componentReader.MetadataReader.GetString(methodDef.Name);
                        Signature = methodDef.DecodeSignature<string, DisassemblingGenericContext>(typeProvider, genericContext);
                        owningTypeHandle = methodDef.GetDeclaringType();
                    }
                    break;

                case HandleKind.MemberReference:
                    {
                        MemberReference memberRef = componentReader.MetadataReader.GetMemberReference((MemberReferenceHandle)MethodHandle);
                        Name = componentReader.MetadataReader.GetString(memberRef.Name);
                        Signature = memberRef.DecodeMethodSignature<string, DisassemblingGenericContext>(typeProvider, genericContext);
                        owningTypeHandle = memberRef.Parent;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (owningType != null)
            {
                DeclaringType = owningType;
            }
            else
            {
                DeclaringType = MetadataNameFormatter.FormatHandle(componentReader.MetadataReader, owningTypeHandle);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(Signature.ReturnType);
            sb.Append(" ");
            sb.Append(DeclaringType);
            sb.Append(".");
            sb.Append(Name);

            if (Signature.GenericParameterCount != 0)
            {
                sb.Append("<");
                for (int i = 0; i < Signature.GenericParameterCount; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    if (instanceArgs != null && instanceArgs.Length > i)
                    {
                        sb.Append(instanceArgs[i]);
                    }
                    else
                    {
                        sb.Append("!");
                        sb.Append(i);
                    }
                }
                sb.Append(">");
            }

            sb.Append("(");
            for (int i = 0; i < Signature.ParameterTypes.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append($"{Signature.ParameterTypes[i]}");
            }
            sb.Append(")");

            SignatureString = sb.ToString();
        }

        public override string ToString() => SignatureString;
        public override int GetHashCode() => SignatureString.GetHashCode();
        public override bool Equals(object obj) => obj is PgoInfoKey other && other.Equals(this);

        // Equality check. This isn't precisely accurate, but it should be good enough
        public bool Equals(PgoInfoKey other)
        {
            return SignatureString.Equals(other.SignatureString);
        }

        public static PgoInfoKey FromReadyToRunMethod(ReadyToRunMethod method)
        {
            var key = new PgoInfoKey(method.ComponentReader, method.DeclaringType, method.MethodHandle, method.InstanceArgs);
            Debug.Assert(key.SignatureString == method.SignatureString);
            return key;
        }
    }
}
