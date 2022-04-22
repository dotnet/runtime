// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

//
// System.Reflection.Emit/SignatureHelper.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed class SignatureHelper
    {
        internal enum SignatureHelperType
        {
            HELPER_FIELD,
            HELPER_LOCAL,
            HELPER_METHOD,
            HELPER_PROPERTY
        }

#region Sync with MonoReflectionSigHelper in object-internals.h
        private ModuleBuilder? module;
        private Type[]? arguments;
        private SignatureHelperType type;
        private Type? returnType;
        private CallingConventions callConv;
        private CallingConvention unmanagedCallConv;
        private Type[][]? modreqs;
        private Type[][]? modopts;
#endregion

        [DynamicDependency(nameof(modopts))]  // Automatically keeps all previous fields too due to StructLayout
        internal SignatureHelper(ModuleBuilder? module, SignatureHelperType type)
        {
            this.type = type;
            this.module = module;
        }

        public static SignatureHelper GetFieldSigHelper(Module? mod)
        {
            if (mod != null && !(mod is ModuleBuilder))
                throw new ArgumentException("ModuleBuilder is expected");

            return new SignatureHelper((ModuleBuilder?)mod, SignatureHelperType.HELPER_FIELD);
        }

        public static SignatureHelper GetLocalVarSigHelper(Module? mod)
        {
            if (mod != null && !(mod is ModuleBuilder))
                throw new ArgumentException("ModuleBuilder is expected");

            return new SignatureHelper((ModuleBuilder?)mod, SignatureHelperType.HELPER_LOCAL);
        }

        public static SignatureHelper GetLocalVarSigHelper()
        {
            return new SignatureHelper(null, SignatureHelperType.HELPER_LOCAL);
        }

        public static SignatureHelper GetMethodSigHelper(CallingConventions callingConvention, Type? returnType)
        {
            return GetMethodSigHelper(null, callingConvention, (CallingConvention)0, returnType, null);
        }

        internal static SignatureHelper GetMethodSigHelper(CallingConvention unmanagedCallingConvention, Type? returnType)
        {
            return GetMethodSigHelper(null, CallingConventions.Standard, unmanagedCallingConvention, returnType, null);
        }

        public static SignatureHelper GetMethodSigHelper(Module? mod, CallingConventions callingConvention, Type? returnType)
        {
            return GetMethodSigHelper(mod, callingConvention, (CallingConvention)0, returnType, null);
        }

        internal static SignatureHelper GetMethodSigHelper(Module? mod, CallingConvention unmanagedCallConv, Type? returnType)
        {
            return GetMethodSigHelper(mod, CallingConventions.Standard, unmanagedCallConv, returnType, null);
        }

        public static SignatureHelper GetMethodSigHelper(Module? mod, Type? returnType, Type[]? parameterTypes)
        {
            return GetMethodSigHelper(mod, CallingConventions.Standard, (CallingConvention)0, returnType, parameterTypes);
        }

        // FIXME: "Not implemented"
        public static SignatureHelper GetPropertySigHelper(Module? mod, Type? returnType, Type[]? parameterTypes)
        {
            throw new NotImplementedException();
        }

        // FIXME: "Not implemented"
        public static SignatureHelper GetPropertySigHelper(Module? mod, Type? returnType,
                                    Type[]? requiredReturnTypeCustomModifiers,
                                    Type[]? optionalReturnTypeCustomModifiers,
                                    Type[]? parameterTypes,
                                    Type[][]? requiredParameterTypeCustomModifiers,
                                    Type[][]? optionalParameterTypeCustomModifiers)
        {
            throw new NotImplementedException();
        }

        // FIXME: "Not implemented"
        public static SignatureHelper GetPropertySigHelper(Module? mod,
                                    CallingConventions callingConvention,
                                    Type? returnType,
                                    Type[]? requiredReturnTypeCustomModifiers,
                                    Type[]? optionalReturnTypeCustomModifiers,
                                    Type[]? parameterTypes,
                                    Type[][]? requiredParameterTypeCustomModifiers,
                                    Type[][]? optionalParameterTypeCustomModifiers)
        {
            throw new NotImplementedException();
        }

        //
        // Grows the given array, and returns the index where the element
        // was added
        //
        private static int AppendArray([NotNull] ref Type[]? array, Type t)
        {
            if (array != null)
            {
                Type[] new_a = new Type[array.Length + 1];
                Array.Copy(array, new_a, array.Length);
                new_a[array.Length] = t;
                array = new_a;
                return array.Length - 1;
            }
            else
            {
                array = new Type[1];
                array[0] = t;
                return 0;
            }
        }

        //
        // Appends the given type array @t into the @array passed at
        // position @pos.   If there is no array, it gets created
        //
        // This allows adding data to a null array at position 5 for
        // example, creating 4 empty slots before the slot where @t
        // is stored.
        //
        //
        private static void AppendArrayAt([NotNull] ref Type[][]? array, Type[] t, int pos)
        {
            int top = Math.Max(pos, array == null ? 0 : array.Length);
            Type[][] new_a = new Type[top + 1][];
            if (array != null)
                Array.Copy(array, new_a, top);
            new_a[pos] = t;
            array = new_a;
        }

        private static void ValidateParameterModifiers(string name, Type[] parameter_modifiers)
        {
            foreach (Type modifier in parameter_modifiers)
            {
                ArgumentNullException.ThrowIfNull(modifier, name);
                if (modifier.IsArray)
                    throw new ArgumentException("Array type not permitted", name);
                if (modifier.ContainsGenericParameters)
                    throw new ArgumentException("Open Generic Type not permitted", name);
            }
        }

        private static void ValidateCustomModifier(int n, Type[][]? custom_modifiers, string name)
        {
            if (custom_modifiers == null)
                return;

            if (custom_modifiers.Length != n)
                throw new ArgumentException(string.Format("Custom modifiers length `{0}' does not match the size of the arguments", custom_modifiers.Length));

            foreach (Type[] parameter_modifiers in custom_modifiers)
            {
                if (parameter_modifiers == null)
                    continue;

                ValidateParameterModifiers(name, parameter_modifiers);
            }
        }

        private static Exception MissingFeature()
        {
            throw new NotImplementedException("Mono does not currently support setting modOpt/modReq through SignatureHelper");
        }

        // FIXME: "Currently we ignore requiredCustomModifiers and optionalCustomModifiers"
        public void AddArguments(Type[]? arguments, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
        {
            ArgumentNullException.ThrowIfNull(arguments);

            // For now
            if (requiredCustomModifiers != null || optionalCustomModifiers != null)
            {
                throw MissingFeature();
            }

            ValidateCustomModifier(arguments.Length, requiredCustomModifiers, "requiredCustomModifiers");
            ValidateCustomModifier(arguments.Length, optionalCustomModifiers, "optionalCustomModifiers");

            for (int i = 0; i < arguments.Length; i++)
            {
                AddArgument(arguments[i], requiredCustomModifiers?[i], optionalCustomModifiers?[i]);
            }
        }

        // FIXME: "pinned is ignored"
        public void AddArgument(Type argument, bool pinned)
        {
            AddArgument(argument);
        }

        public void AddArgument(Type argument, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers)
        {
            ArgumentNullException.ThrowIfNull(argument);

            if (requiredCustomModifiers != null)
                ValidateParameterModifiers("requiredCustomModifiers", requiredCustomModifiers);
            if (optionalCustomModifiers != null)
                ValidateParameterModifiers("optionalCustomModifiers", optionalCustomModifiers);

            int p = AppendArray(ref arguments, argument);
            if (requiredCustomModifiers != null)
                AppendArrayAt(ref modreqs, requiredCustomModifiers, p);
            if (optionalCustomModifiers != null)
                AppendArrayAt(ref modopts, optionalCustomModifiers, p);
        }

        public void AddArgument(Type clsArgument)
        {
            ArgumentNullException.ThrowIfNull(clsArgument);

            AppendArray(ref arguments, clsArgument);
        }

        // FIXME: "Not implemented"
        public void AddSentinel()
        {
            throw new NotImplementedException();
        }

        private static bool CompareOK(Type[][]? one, Type[][]? two)
        {
            if (one == null)
            {
                if (two == null)
                    return true;
                return false;
            }
            else if (two == null)
                return false;

            if (one.Length != two.Length)
                return false;

            for (int i = 0; i < one.Length; i++)
            {
                Type[] tone = one[i];
                Type[] ttwo = two[i];

                if (tone == null)
                {
                    if (ttwo == null)
                        continue;
                }
                else if (ttwo == null)
                    return false;

                if (tone!.Length != ttwo.Length)
                    return false;

                for (int j = 0; j < tone.Length; j++)
                {
                    Type uone = tone[j];
                    Type utwo = ttwo[j];

                    if (uone == null)
                    {
                        if (utwo == null)
                            continue;
                        return false;
                    }
                    else if (utwo == null)
                        return false;

                    if (!uone.Equals(utwo))
                        return false;
                }
            }
            return true;
        }

        public override bool Equals(object? obj)
        {
            SignatureHelper? other = obj as SignatureHelper;
            if (other == null)
                return false;

            if (other.module != module ||
                other.returnType != returnType ||
                other.callConv != callConv ||
                other.unmanagedCallConv != unmanagedCallConv)
                return false;

            if (arguments != null)
            {
                if (other.arguments == null)
                    return false;
                if (arguments.Length != other.arguments.Length)
                    return false;

                for (int i = 0; i < arguments.Length; i++)
                    if (!other.arguments[i].Equals(arguments[i]))
                        return false;
            }
            else if (other.arguments != null)
                return false;

            return CompareOK(other.modreqs, modreqs) && CompareOK(other.modopts, modopts);
        }

        public override int GetHashCode()
        {
            // Lame, but easy, and will work, and chances are
            // you will only need a few of these.
            return 0;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern byte[] get_signature_local();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern byte[] get_signature_field();

        public byte[] GetSignature()
        {
            TypeBuilder.ResolveUserTypes(arguments);

            return type switch
            {
                SignatureHelperType.HELPER_LOCAL => get_signature_local(),
                SignatureHelperType.HELPER_FIELD => get_signature_field(),
                _ => throw new NotImplementedException(),
            };
        }

        public override string ToString()
        {
            return "SignatureHelper";
        }

        internal static SignatureHelper GetMethodSigHelper(Module? mod, CallingConventions callingConvention, CallingConvention unmanagedCallingConvention, Type? returnType,
                                                           Type[]? parameters)
        {
            if (mod != null && !(mod is ModuleBuilder))
                throw new ArgumentException("ModuleBuilder is expected");

            if (returnType == null)
                returnType = typeof(void);

            if (returnType.IsUserType)
                throw new NotSupportedException("User defined subclasses of System.Type are not yet supported.");
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; ++i)
                    if (parameters[i].IsUserType)
                        throw new NotSupportedException("User defined subclasses of System.Type are not yet supported.");

            }

            SignatureHelper helper = new SignatureHelper((ModuleBuilder?)mod, SignatureHelperType.HELPER_METHOD);
            helper.returnType = returnType;
            helper.callConv = callingConvention;
            helper.unmanagedCallConv = unmanagedCallingConvention;

            if (parameters != null)
            {
                helper.arguments = new Type[parameters.Length];
                for (int i = 0; i < parameters.Length; ++i)
                    helper.arguments[i] = parameters[i];
            }

            return helper;
        }
    }
}
#endif
