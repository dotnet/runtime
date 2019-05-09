// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System.Text;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Reflection.Emit
{
    public sealed class SignatureHelper
    {
        #region Consts Fields
        private const int NO_SIZE_IN_SIG = -1;
        #endregion

        #region Static Members
        public static SignatureHelper GetMethodSigHelper(Module? mod, Type? returnType, Type[]? parameterTypes)
        {
            return GetMethodSigHelper(mod, CallingConventions.Standard, returnType, null, null, parameterTypes, null, null);
        }

        internal static SignatureHelper GetMethodSigHelper(Module? mod, CallingConventions callingConvention, Type? returnType, int cGenericParam)
        {
            return GetMethodSigHelper(mod, callingConvention, cGenericParam, returnType, null, null, null, null, null);
        }

        public static SignatureHelper GetMethodSigHelper(Module? mod, CallingConventions callingConvention, Type? returnType)
        {
            return GetMethodSigHelper(mod, callingConvention, returnType, null, null, null, null, null);
        }

        internal static SignatureHelper GetMethodSpecSigHelper(Module? scope, Type[] inst)
        {
            SignatureHelper sigHelp = new SignatureHelper(scope, MdSigCallingConvention.GenericInst);
            sigHelp.AddData(inst.Length);
            foreach (Type t in inst)
                sigHelp.AddArgument(t);
            return sigHelp;
        }

        internal static SignatureHelper GetMethodSigHelper(
            Module? scope, CallingConventions callingConvention,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
        {
            return GetMethodSigHelper(scope, callingConvention, 0, returnType, requiredReturnTypeCustomModifiers,
                optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
        }

        internal static SignatureHelper GetMethodSigHelper(
            Module? scope, CallingConventions callingConvention, int cGenericParam,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
        {
            SignatureHelper sigHelp;
            MdSigCallingConvention intCall;

            if (returnType == null)
            {
                returnType = typeof(void);
            }

            intCall = MdSigCallingConvention.Default;

            if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                intCall = MdSigCallingConvention.Vararg;

            if (cGenericParam > 0)
            {
                intCall |= MdSigCallingConvention.Generic;
            }

            if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
                intCall |= MdSigCallingConvention.HasThis;

            sigHelp = new SignatureHelper(scope, intCall, cGenericParam, returnType,
                                            requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers);
            sigHelp.AddArguments(parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);

            return sigHelp;
        }

        public static SignatureHelper GetMethodSigHelper(Module? mod, CallingConvention unmanagedCallConv, Type? returnType)
        {
            SignatureHelper sigHelp;
            MdSigCallingConvention intCall;

            if (returnType == null)
                returnType = typeof(void);

            if (unmanagedCallConv == CallingConvention.Cdecl)
            {
                intCall = MdSigCallingConvention.C;
            }
            else if (unmanagedCallConv == CallingConvention.StdCall || unmanagedCallConv == CallingConvention.Winapi)
            {
                intCall = MdSigCallingConvention.StdCall;
            }
            else if (unmanagedCallConv == CallingConvention.ThisCall)
            {
                intCall = MdSigCallingConvention.ThisCall;
            }
            else if (unmanagedCallConv == CallingConvention.FastCall)
            {
                intCall = MdSigCallingConvention.FastCall;
            }
            else
            {
                throw new ArgumentException(SR.Argument_UnknownUnmanagedCallConv, nameof(unmanagedCallConv));
            }

            sigHelp = new SignatureHelper(mod, intCall, returnType, null, null);

            return sigHelp;
        }

        public static SignatureHelper GetLocalVarSigHelper()
        {
            return GetLocalVarSigHelper(null);
        }

        public static SignatureHelper GetMethodSigHelper(CallingConventions callingConvention, Type? returnType)
        {
            return GetMethodSigHelper(null, callingConvention, returnType);
        }

        public static SignatureHelper GetMethodSigHelper(CallingConvention unmanagedCallingConvention, Type? returnType)
        {
            return GetMethodSigHelper(null, unmanagedCallingConvention, returnType);
        }

        public static SignatureHelper GetLocalVarSigHelper(Module? mod)
        {
            return new SignatureHelper(mod, MdSigCallingConvention.LocalSig);
        }

        public static SignatureHelper GetFieldSigHelper(Module? mod)
        {
            return new SignatureHelper(mod, MdSigCallingConvention.Field);
        }

        public static SignatureHelper GetPropertySigHelper(Module? mod, Type? returnType, Type[]? parameterTypes)
        {
            return GetPropertySigHelper(mod, returnType, null, null, parameterTypes, null, null);
        }

        public static SignatureHelper GetPropertySigHelper(Module? mod,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
        {
            return GetPropertySigHelper(mod, (CallingConventions)0, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers,
                parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
        }
        public static SignatureHelper GetPropertySigHelper(Module? mod, CallingConventions callingConvention,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
        {
            SignatureHelper sigHelp;

            if (returnType == null)
            {
                returnType = typeof(void);
            }

            MdSigCallingConvention intCall = MdSigCallingConvention.Property;

            if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
                intCall |= MdSigCallingConvention.HasThis;

            sigHelp = new SignatureHelper(mod, intCall,
                returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers);
            sigHelp.AddArguments(parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);

            return sigHelp;
        }

        internal static SignatureHelper GetTypeSigToken(Module module, Type type)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return new SignatureHelper(module, type);
        }
        #endregion

        #region Private Data Members
        private byte[] m_signature = null!;
        private int m_currSig; // index into m_signature buffer for next available byte
        private int m_sizeLoc; // index into m_signature buffer to put m_argCount (will be NO_SIZE_IN_SIG if no arg count is needed)
        private ModuleBuilder? m_module;
        private bool m_sigDone;
        private int m_argCount; // tracking number of arguments in the signature
        #endregion

        #region Constructor
        private SignatureHelper(Module? mod, MdSigCallingConvention callingConvention)
        {
            // Use this constructor to instantiate a local var sig  or Field where return type is not applied.
            Init(mod, callingConvention);
        }

        private SignatureHelper(Module? mod, MdSigCallingConvention callingConvention, int cGenericParameters,
            Type returnType, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers)
        {
            // Use this constructor to instantiate a any signatures that will require a return type.
            Init(mod, callingConvention, cGenericParameters);

            if (callingConvention == MdSigCallingConvention.Field)
                throw new ArgumentException(SR.Argument_BadFieldSig);

            AddOneArgTypeHelper(returnType, requiredCustomModifiers, optionalCustomModifiers);
        }

        private SignatureHelper(Module? mod, MdSigCallingConvention callingConvention,
            Type returnType, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers)
            : this(mod, callingConvention, 0, returnType, requiredCustomModifiers, optionalCustomModifiers)
        {
        }

        private SignatureHelper(Module mod, Type type)
        {
            Init(mod);

            AddOneArgTypeHelper(type);
        }

        private void Init(Module? mod)
        {
            m_signature = new byte[32];
            m_currSig = 0;
            m_module = mod as ModuleBuilder;
            m_argCount = 0;
            m_sigDone = false;
            m_sizeLoc = NO_SIZE_IN_SIG;

            if (m_module == null && mod != null)
                throw new ArgumentException(SR.NotSupported_MustBeModuleBuilder);
        }

        private void Init(Module? mod, MdSigCallingConvention callingConvention)
        {
            Init(mod, callingConvention, 0);
        }

        private void Init(Module? mod, MdSigCallingConvention callingConvention, int cGenericParam)
        {
            Init(mod);

            AddData((byte)callingConvention);

            if (callingConvention == MdSigCallingConvention.Field ||
                callingConvention == MdSigCallingConvention.GenericInst)
            {
                m_sizeLoc = NO_SIZE_IN_SIG;
            }
            else
            {
                if (cGenericParam > 0)
                    AddData(cGenericParam);

                m_sizeLoc = m_currSig++;
            }
        }

        #endregion

        #region Private Members
        private void AddOneArgTypeHelper(Type argument, bool pinned)
        {
            if (pinned)
                AddElementType(CorElementType.ELEMENT_TYPE_PINNED);

            AddOneArgTypeHelper(argument);
        }

        private void AddOneArgTypeHelper(Type clsArgument, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers)
        {
            // This function will not increase the argument count. It only fills in bytes 
            // in the signature based on clsArgument. This helper is called for return type.

            Debug.Assert(clsArgument != null);

            if (optionalCustomModifiers != null)
            {
                for (int i = 0; i < optionalCustomModifiers.Length; i++)
                {
                    Type t = optionalCustomModifiers[i];

                    if (t == null)
                        throw new ArgumentNullException(nameof(optionalCustomModifiers));

                    if (t.HasElementType)
                        throw new ArgumentException(SR.Argument_ArraysInvalid, nameof(optionalCustomModifiers));

                    if (t.ContainsGenericParameters)
                        throw new ArgumentException(SR.Argument_GenericsInvalid, nameof(optionalCustomModifiers));

                    AddElementType(CorElementType.ELEMENT_TYPE_CMOD_OPT);

                    int token = m_module!.GetTypeToken(t).Token;
                    Debug.Assert(!MetadataToken.IsNullToken(token));
                    AddToken(token);
                }
            }

            if (requiredCustomModifiers != null)
            {
                for (int i = 0; i < requiredCustomModifiers.Length; i++)
                {
                    Type t = requiredCustomModifiers[i];

                    if (t == null)
                        throw new ArgumentNullException(nameof(requiredCustomModifiers));

                    if (t.HasElementType)
                        throw new ArgumentException(SR.Argument_ArraysInvalid, nameof(requiredCustomModifiers));

                    if (t.ContainsGenericParameters)
                        throw new ArgumentException(SR.Argument_GenericsInvalid, nameof(requiredCustomModifiers));

                    AddElementType(CorElementType.ELEMENT_TYPE_CMOD_REQD);

                    int token = m_module!.GetTypeToken(t).Token;
                    Debug.Assert(!MetadataToken.IsNullToken(token));
                    AddToken(token);
                }
            }

            AddOneArgTypeHelper(clsArgument);
        }

        private void AddOneArgTypeHelper(Type clsArgument) { AddOneArgTypeHelperWorker(clsArgument, false); }
        private void AddOneArgTypeHelperWorker(Type clsArgument, bool lastWasGenericInst)
        {
            if (clsArgument.IsGenericParameter)
            {
                if (clsArgument.DeclaringMethod != null)
                    AddElementType(CorElementType.ELEMENT_TYPE_MVAR);
                else
                    AddElementType(CorElementType.ELEMENT_TYPE_VAR);

                AddData(clsArgument.GenericParameterPosition);
            }
            else if (clsArgument.IsGenericType && (!clsArgument.IsGenericTypeDefinition || !lastWasGenericInst))
            {
                AddElementType(CorElementType.ELEMENT_TYPE_GENERICINST);

                AddOneArgTypeHelperWorker(clsArgument.GetGenericTypeDefinition(), true);

                Type[] args = clsArgument.GetGenericArguments();

                AddData(args.Length);

                foreach (Type t in args)
                    AddOneArgTypeHelper(t);
            }
            else if (clsArgument is TypeBuilder)
            {
                TypeBuilder clsBuilder = (TypeBuilder)clsArgument;
                TypeToken tkType;

                if (clsBuilder.Module.Equals(m_module))
                {
                    tkType = clsBuilder.TypeToken;
                }
                else
                {
                    tkType = m_module!.GetTypeToken(clsArgument);
                }

                if (clsArgument.IsValueType)
                {
                    InternalAddTypeToken(tkType, CorElementType.ELEMENT_TYPE_VALUETYPE);
                }
                else
                {
                    InternalAddTypeToken(tkType, CorElementType.ELEMENT_TYPE_CLASS);
                }
            }
            else if (clsArgument is EnumBuilder)
            {
                TypeBuilder clsBuilder = ((EnumBuilder)clsArgument).m_typeBuilder;
                TypeToken tkType;

                if (clsBuilder.Module.Equals(m_module))
                {
                    tkType = clsBuilder.TypeToken;
                }
                else
                {
                    tkType = m_module!.GetTypeToken(clsArgument);
                }

                if (clsArgument.IsValueType)
                {
                    InternalAddTypeToken(tkType, CorElementType.ELEMENT_TYPE_VALUETYPE);
                }
                else
                {
                    InternalAddTypeToken(tkType, CorElementType.ELEMENT_TYPE_CLASS);
                }
            }
            else if (clsArgument.IsByRef)
            {
                AddElementType(CorElementType.ELEMENT_TYPE_BYREF);
                clsArgument = clsArgument.GetElementType()!;
                AddOneArgTypeHelper(clsArgument);
            }
            else if (clsArgument.IsPointer)
            {
                AddElementType(CorElementType.ELEMENT_TYPE_PTR);
                AddOneArgTypeHelper(clsArgument.GetElementType()!);
            }
            else if (clsArgument.IsArray)
            {
                if (clsArgument.IsSZArray)
                {
                    AddElementType(CorElementType.ELEMENT_TYPE_SZARRAY);

                    AddOneArgTypeHelper(clsArgument.GetElementType()!);
                }
                else
                {
                    AddElementType(CorElementType.ELEMENT_TYPE_ARRAY);

                    AddOneArgTypeHelper(clsArgument.GetElementType()!);

                    // put the rank information
                    int rank = clsArgument.GetArrayRank();
                    AddData(rank);     // rank
                    AddData(0);     // upper bounds
                    AddData(rank);  // lower bound
                    for (int i = 0; i < rank; i++)
                        AddData(0);
                }
            }
            else
            {
                CorElementType type = CorElementType.ELEMENT_TYPE_MAX;

                if (clsArgument is RuntimeType)
                {
                    type = RuntimeTypeHandle.GetCorElementType((RuntimeType)clsArgument);

                    //GetCorElementType returns CorElementType.ELEMENT_TYPE_CLASS for both object and string
                    if (type == CorElementType.ELEMENT_TYPE_CLASS)
                    {
                        if (clsArgument == typeof(object))
                            type = CorElementType.ELEMENT_TYPE_OBJECT;
                        else if (clsArgument == typeof(string))
                            type = CorElementType.ELEMENT_TYPE_STRING;
                    }
                }

                if (IsSimpleType(type))
                {
                    AddElementType(type);
                }
                else if (m_module == null)
                {
                    InternalAddRuntimeType(clsArgument);
                }
                else if (clsArgument.IsValueType)
                {
                    InternalAddTypeToken(m_module.GetTypeToken(clsArgument), CorElementType.ELEMENT_TYPE_VALUETYPE);
                }
                else
                {
                    InternalAddTypeToken(m_module.GetTypeToken(clsArgument), CorElementType.ELEMENT_TYPE_CLASS);
                }
            }
        }

        private void AddData(int data)
        {
            if (m_currSig + 4 > m_signature.Length)
            {
                m_signature = ExpandArray(m_signature);
            }

            if (data <= 0x7F)
            {
                m_signature[m_currSig++] = (byte)data;
            }
            else if (data <= 0x3F_FF)
            {
                BinaryPrimitives.WriteInt16BigEndian(m_signature.AsSpan(m_currSig), (short)(data | 0x80_00));
                m_currSig += 2;
            }
            else if (data <= 0x1F_FF_FF_FF)
            {
                BinaryPrimitives.WriteInt32BigEndian(m_signature.AsSpan(m_currSig), (int)(data | 0xC0_00_00_00));
                m_currSig += 4;
            }
            else
            {
                throw new ArgumentException(SR.Argument_LargeInteger);
            }
        }

        private void AddElementType(CorElementType cvt)
        {
            // Adds an element to the signature.  A managed represenation of CorSigCompressElement
            if (m_currSig + 1 > m_signature.Length)
                m_signature = ExpandArray(m_signature);

            m_signature[m_currSig++] = (byte)cvt;
        }

        private void AddToken(int token)
        {
            // A managed represenation of CompressToken
            // Pulls the token appart to get a rid, adds some appropriate bits
            // to the token and then adds this to the signature.

            int rid = (token & 0x00FFFFFF); //This is RidFromToken;
            MetadataTokenType type = (MetadataTokenType)(token & unchecked((int)0xFF000000)); //This is TypeFromToken;

            if (rid > 0x3FFFFFF)
            {
                // token is too big to be compressed    
                throw new ArgumentException(SR.Argument_LargeInteger);
            }

            rid = (rid << 2);

            // TypeDef is encoded with low bits 00  
            // TypeRef is encoded with low bits 01  
            // TypeSpec is encoded with low bits 10    
            if (type == MetadataTokenType.TypeRef)
            {
                //if type is mdtTypeRef
                rid |= 0x1;
            }
            else if (type == MetadataTokenType.TypeSpec)
            {
                //if type is mdtTypeSpec
                rid |= 0x2;
            }

            AddData(rid);
        }

        private void InternalAddTypeToken(TypeToken clsToken, CorElementType CorType)
        {
            // Add a type token into signature. CorType will be either CorElementType.ELEMENT_TYPE_CLASS or CorElementType.ELEMENT_TYPE_VALUETYPE
            AddElementType(CorType);
            AddToken(clsToken.Token);
        }

        private unsafe void InternalAddRuntimeType(Type type)
        {
            // Add a runtime type into the signature. 

            AddElementType(CorElementType.ELEMENT_TYPE_INTERNAL);

            IntPtr handle = type.GetTypeHandleInternal().Value;

            // Internal types must have their pointer written into the signature directly (we don't
            // want to convert to little-endian format on big-endian machines because the value is
            // going to be extracted and used directly as a pointer (and only within this process)).

            if (m_currSig + sizeof(void*) > m_signature.Length)
                m_signature = ExpandArray(m_signature);

            byte* phandle = (byte*)&handle;
            for (int i = 0; i < sizeof(void*); i++)
                m_signature[m_currSig++] = phandle[i];
        }

        private byte[] ExpandArray(byte[] inArray)
        {
            // Expand the signature buffer size
            return ExpandArray(inArray, inArray.Length * 2);
        }

        private byte[] ExpandArray(byte[] inArray, int requiredLength)
        {
            // Expand the signature buffer size

            if (requiredLength < inArray.Length)
                requiredLength = inArray.Length * 2;

            byte[] outArray = new byte[requiredLength];
            Buffer.BlockCopy(inArray, 0, outArray, 0, inArray.Length);
            return outArray;
        }

        private void IncrementArgCounts()
        {
            if (m_sizeLoc == NO_SIZE_IN_SIG)
            {
                //We don't have a size if this is a field.
                return;
            }

            m_argCount++;
        }

        private void SetNumberOfSignatureElements(bool forceCopy)
        {
            // For most signatures, this will set the number of elements in a byte which we have reserved for it.
            // However, if we have a field signature, we don't set the length and return.
            // If we have a signature with more than 128 arguments, we can't just set the number of elements,
            // we actually have to allocate more space (e.g. shift everything in the array one or more spaces to the
            // right.  We do this by making a copy of the array and leaving the correct number of blanks.  This new
            // array is now set to be m_signature and we use the AddData method to set the number of elements properly.
            // The forceCopy argument can be used to force SetNumberOfSignatureElements to make a copy of
            // the array.  This is useful for GetSignature which promises to trim the array to be the correct size anyway.

            byte[] temp;
            int newSigSize;
            int currSigHolder = m_currSig;

            if (m_sizeLoc == NO_SIZE_IN_SIG)
                return;

            //If we have fewer than 128 arguments and we haven't been told to copy the
            //array, we can just set the appropriate bit and return.
            if (m_argCount < 0x80 && !forceCopy)
            {
                m_signature[m_sizeLoc] = (byte)m_argCount;
                return;
            }

            //We need to have more bytes for the size.  Figure out how many bytes here.
            //Since we need to copy anyway, we're just going to take the cost of doing a
            //new allocation.
            if (m_argCount < 0x80)
            {
                newSigSize = 1;
            }
            else if (m_argCount < 0x4000)
            {
                newSigSize = 2;
            }
            else
            {
                newSigSize = 4;
            }

            //Allocate the new array.
            temp = new byte[m_currSig + newSigSize - 1];

            //Copy the calling convention.  The calling convention is always just one byte
            //so we just copy that byte.  Then copy the rest of the array, shifting everything
            //to make room for the new number of elements.
            temp[0] = m_signature[0];
            Buffer.BlockCopy(m_signature, m_sizeLoc + 1, temp, m_sizeLoc + newSigSize, currSigHolder - (m_sizeLoc + 1));
            m_signature = temp;

            //Use the AddData method to add the number of elements appropriately compressed.
            m_currSig = m_sizeLoc;
            AddData(m_argCount);
            m_currSig = currSigHolder + (newSigSize - 1);
        }

        #endregion

        #region Internal Members
        internal int ArgumentCount
        {
            get
            {
                return m_argCount;
            }
        }

        internal static bool IsSimpleType(CorElementType type)
        {
            if (type <= CorElementType.ELEMENT_TYPE_STRING)
                return true;

            if (type == CorElementType.ELEMENT_TYPE_TYPEDBYREF || type == CorElementType.ELEMENT_TYPE_I || type == CorElementType.ELEMENT_TYPE_U || type == CorElementType.ELEMENT_TYPE_OBJECT)
                return true;

            return false;
        }

        internal byte[] InternalGetSignature(out int length)
        {
            // An internal method to return the signature.  Does not trim the
            // array, but passes out the length of the array in an out parameter.
            // This is the actual array -- not a copy -- so the callee must agree
            // to not copy it.
            //
            // param length : an out param indicating the length of the array.
            // return : A reference to the internal ubyte array.

            if (!m_sigDone)
            {
                m_sigDone = true;

                // If we have more than 128 variables, we can't just set the length, we need 
                // to compress it.  Unfortunately, this means that we need to copy the entire 
                // array.
                SetNumberOfSignatureElements(false);
            }

            length = m_currSig;
            return m_signature;
        }




        internal byte[] InternalGetSignatureArray()
        {
            int argCount = m_argCount;
            int currSigLength = m_currSig;
            int newSigSize = currSigLength;

            //Allocate the new array.
            if (argCount < 0x7F)
                newSigSize += 1;
            else if (argCount < 0x3FFF)
                newSigSize += 2;
            else
                newSigSize += 4;
            byte[] temp = new byte[newSigSize];

            // copy the sig
            int sigCopyIndex = 0;
            // calling convention
            temp[sigCopyIndex++] = m_signature[0];
            // arg size
            if (argCount <= 0x7F)
                temp[sigCopyIndex++] = (byte)(argCount & 0xFF);
            else if (argCount <= 0x3FFF)
            {
                temp[sigCopyIndex++] = (byte)((argCount >> 8) | 0x80);
                temp[sigCopyIndex++] = (byte)(argCount & 0xFF);
            }
            else if (argCount <= 0x1FFFFFFF)
            {
                temp[sigCopyIndex++] = (byte)((argCount >> 24) | 0xC0);
                temp[sigCopyIndex++] = (byte)((argCount >> 16) & 0xFF);
                temp[sigCopyIndex++] = (byte)((argCount >> 8) & 0xFF);
                temp[sigCopyIndex++] = (byte)((argCount) & 0xFF);
            }
            else
                throw new ArgumentException(SR.Argument_LargeInteger);
            // copy the sig part of the sig
            Buffer.BlockCopy(m_signature, 2, temp, sigCopyIndex, currSigLength - 2);
            // mark the end of sig
            temp[newSigSize - 1] = (byte)CorElementType.ELEMENT_TYPE_END;

            return temp;
        }

        #endregion

        #region Public Methods
        public void AddArgument(Type clsArgument)
        {
            AddArgument(clsArgument, null, null);
        }

        public void AddArgument(Type argument, bool pinned)
        {
            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            IncrementArgCounts();
            AddOneArgTypeHelper(argument, pinned);
        }

        public void AddArguments(Type[]? arguments, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
        {
            if (requiredCustomModifiers != null && (arguments == null || requiredCustomModifiers.Length != arguments.Length))
                throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, nameof(requiredCustomModifiers), nameof(arguments)));

            if (optionalCustomModifiers != null && (arguments == null || optionalCustomModifiers.Length != arguments.Length))
                throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, nameof(optionalCustomModifiers), nameof(arguments)));

            if (arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    AddArgument(arguments[i],
                        requiredCustomModifiers == null ? null : requiredCustomModifiers[i],
                        optionalCustomModifiers == null ? null : optionalCustomModifiers[i]);
                }
            }
        }

        public void AddArgument(Type argument, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers)
        {
            if (m_sigDone)
                throw new ArgumentException(SR.Argument_SigIsFinalized);

            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            IncrementArgCounts();

            // Add an argument to the signature. Takes a Type and determines whether it
            // is one of the primitive types of which we have special knowledge or a more
            // general class.  In the former case, we only add the appropriate short cut encoding, 
            // otherwise we will calculate proper description for the type.
            AddOneArgTypeHelper(argument, requiredCustomModifiers, optionalCustomModifiers);
        }

        public void AddSentinel()
        {
            AddElementType(CorElementType.ELEMENT_TYPE_SENTINEL);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is SignatureHelper))
            {
                return false;
            }

            SignatureHelper temp = (SignatureHelper)obj;

            if (!temp.m_module!.Equals(m_module) || temp.m_currSig != m_currSig || temp.m_sizeLoc != m_sizeLoc || temp.m_sigDone != m_sigDone)
            {
                return false;
            }

            for (int i = 0; i < m_currSig; i++)
            {
                if (m_signature[i] != temp.m_signature[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            // Start the hash code with the hash code of the module and the values of the member variables.
            int HashCode = m_module!.GetHashCode() + m_currSig + m_sizeLoc;

            // Add one if the sig is done.
            if (m_sigDone)
                HashCode += 1;

            // Then add the hash code of all the arguments.
            for (int i = 0; i < m_currSig; i++)
                HashCode += m_signature[i].GetHashCode();

            return HashCode;
        }

        public byte[] GetSignature()
        {
            return GetSignature(false);
        }

        internal byte[] GetSignature(bool appendEndOfSig)
        {
            // Chops the internal signature to the appropriate length.  Adds the 
            // end token to the signature and marks the signature as finished so that
            // no further tokens can be added. Return the full signature in a trimmed array.
            if (!m_sigDone)
            {
                if (appendEndOfSig)
                    AddElementType(CorElementType.ELEMENT_TYPE_END);
                SetNumberOfSignatureElements(true);
                m_sigDone = true;
            }

            // This case will only happen if the user got the signature through 
            // InternalGetSignature first and then called GetSignature.
            if (m_signature.Length > m_currSig)
            {
                byte[] temp = new byte[m_currSig];
                Array.Copy(m_signature, 0, temp, 0, m_currSig);
                m_signature = temp;
            }

            return m_signature;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Length: " + m_currSig + Environment.NewLine);

            if (m_sizeLoc != -1)
            {
                sb.Append("Arguments: " + m_signature[m_sizeLoc] + Environment.NewLine);
            }
            else
            {
                sb.Append("Field Signature" + Environment.NewLine);
            }

            sb.Append("Signature: " + Environment.NewLine);
            for (int i = 0; i <= m_currSig; i++)
            {
                sb.Append(m_signature[i] + "  ");
            }

            sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        #endregion
    }
}

























