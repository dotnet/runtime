// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection.Runtime.BindingFlagSupport;

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeTypeInfo
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public sealed override object? InvokeMember(
            string name, BindingFlags bindingFlags, Binder? binder, object? target,
            object?[]? providedArgs, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParams)
        {
            const BindingFlags MemberBindingMask = (BindingFlags)0x000000FF;
            const BindingFlags InvocationMask = (BindingFlags)0x0000FF00;
            const BindingFlags BinderGetSetProperty = BindingFlags.GetProperty | BindingFlags.SetProperty;
            const BindingFlags BinderGetSetField = BindingFlags.GetField | BindingFlags.SetField;
            const BindingFlags BinderNonFieldGetSet = (BindingFlags)0x00FFF300;
            const BindingFlags BinderNonCreateInstance = BindingFlags.InvokeMethod | BinderGetSetField | BinderGetSetProperty;

            if (IsGenericParameter)
                throw new InvalidOperationException(SR.Arg_GenericParameter);

            #region Preconditions
            if ((bindingFlags & InvocationMask) == 0)
                // "Must specify binding flags describing the invoke operation required."
                throw new ArgumentException(SR.Arg_NoAccessSpec, nameof(bindingFlags));

            // Provide a default binding mask if none is provided
            if ((bindingFlags & MemberBindingMask) == 0)
            {
                bindingFlags |= BindingFlags.Instance | BindingFlags.Public;

                if ((bindingFlags & BindingFlags.CreateInstance) == 0)
                    bindingFlags |= BindingFlags.Static;
            }

            // There must not be more named parameters than provided arguments
            if (namedParams != null)
            {
                if (providedArgs != null)
                {
                    if (namedParams.Length > providedArgs.Length)
                        // "Named parameter array can not be bigger than argument array."
                        throw new ArgumentException(SR.Arg_NamedParamTooBig, nameof(namedParams));
                }
                else
                {
                    if (namedParams.Length != 0)
                        // "Named parameter array can not be bigger than argument array."
                        throw new ArgumentException(SR.Arg_NamedParamTooBig, nameof(namedParams));
                }
            }
            #endregion

            #region COM Interop
            if (target != null && target.GetType().IsCOMObject)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
            #endregion

            #region Check that any named paramters are not null
            if (namedParams != null && Array.IndexOf(namedParams, null) != -1)
                // "Named parameter value must not be null."
                throw new ArgumentException(SR.Arg_NamedParamNull, nameof(namedParams));
            #endregion

            int argCnt = (providedArgs != null) ? providedArgs.Length : 0;

            #region Get a Binder
            if (binder == null)
                binder = DefaultBinder;

            #endregion

            #region Delegate to Activator.CreateInstance
            if ((bindingFlags & BindingFlags.CreateInstance) != 0)
            {
                if ((bindingFlags & BindingFlags.CreateInstance) != 0 && (bindingFlags & BinderNonCreateInstance) != 0)
                    // "Can not specify both CreateInstance and another access type."
                    throw new ArgumentException(SR.Arg_CreatInstAccess, nameof(bindingFlags));

                return Activator.CreateInstance(this, bindingFlags, binder, providedArgs, culture);
            }
            #endregion

            // PutDispProperty and\or PutRefDispProperty ==> SetProperty.
            if ((bindingFlags & (BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) != 0)
                bindingFlags |= BindingFlags.SetProperty;

            #region Name
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0 || name.Equals(@"[DISPID=0]"))
            {
                // in InvokeMember we always pretend there is a default member if none is provided and we make it ToString
                name = GetDefaultMemberName() ?? "ToString";
            }
            #endregion

            #region GetField or SetField
            bool IsGetField = (bindingFlags & BindingFlags.GetField) != 0;
            bool IsSetField = (bindingFlags & BindingFlags.SetField) != 0;

            if (IsGetField || IsSetField)
            {
                #region Preconditions
                if (IsGetField)
                {
                    if (IsSetField)
                        // "Can not specify both Get and Set on a field."
                        throw new ArgumentException(SR.Arg_FldSetGet, nameof(bindingFlags));

                    if ((bindingFlags & BindingFlags.SetProperty) != 0)
                        // "Can not specify both GetField and SetProperty."
                        throw new ArgumentException(SR.Arg_FldGetPropSet, nameof(bindingFlags));
                }
                else
                {
                    Debug.Assert(IsSetField);

                    if (providedArgs == null)
                        throw new ArgumentNullException(nameof(providedArgs));

                    if ((bindingFlags & BindingFlags.GetProperty) != 0)
                        // "Can not specify both SetField and GetProperty."
                        throw new ArgumentException(SR.Arg_FldSetPropGet, nameof(bindingFlags));

                    if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
                        // "Can not specify Set on a Field and Invoke on a method."
                        throw new ArgumentException(SR.Arg_FldSetInvoke, nameof(bindingFlags));
                }
                #endregion

                #region Lookup Field
                FieldInfo? selFld = null;
                FieldInfo[] flds = (FieldInfo[])GetMember(name, MemberTypes.Field, bindingFlags);

                if (flds.Length == 1)
                {
                    selFld = flds[0];
                }
                else if (flds.Length > 0)
                {
                    selFld = binder.BindToField(bindingFlags, flds, IsGetField ? Empty.Value : providedArgs[0], culture);
                }
                #endregion

                if (selFld != null)
                {
                    #region Invocation on a field
                    if (selFld.FieldType.IsArray || object.ReferenceEquals(selFld.FieldType, typeof(Array)))
                    {
                        #region Invocation of an array Field
                        int idxCnt;

                        if ((bindingFlags & BindingFlags.GetField) != 0)
                        {
                            idxCnt = argCnt;
                        }
                        else
                        {
                            idxCnt = argCnt - 1;
                        }

                        if (idxCnt > 0)
                        {
                            Debug.Assert(providedArgs != null);

                            // Verify that all of the index values are ints
                            int[] idx = new int[idxCnt];
                            for (int i = 0; i < idxCnt; i++)
                            {
                                try
                                {
                                    idx[i] = ((IConvertible)providedArgs[i]!).ToInt32(null);
                                }
                                catch (InvalidCastException)
                                {
                                    throw new ArgumentException(SR.Arg_IndexMustBeInt);
                                }
                            }

                            // Set or get the value...
                            Array a = (Array)selFld.GetValue(target)!;

                            // Set or get the value in the array
                            if ((bindingFlags & BindingFlags.GetField) != 0)
                            {
                                return a.GetValue(idx);
                            }
                            else
                            {
                                a.SetValue(providedArgs[idxCnt], idx);
                                return null;
                            }
                        }
                        #endregion
                    }

                    if (IsGetField)
                    {
                        #region Get the field value
                        if (argCnt != 0)
                            throw new ArgumentException(SR.Arg_FldGetArgErr, nameof(bindingFlags));

                        return selFld.GetValue(target);
                        #endregion
                    }
                    else
                    {
                        #region Set the field Value
                        if (argCnt != 1)
                            throw new ArgumentException(SR.Arg_FldSetArgErr, nameof(bindingFlags));

                        selFld.SetValue(target, providedArgs[0], bindingFlags, binder, culture);

                        return null;
                        #endregion
                    }
                    #endregion
                }

                if ((bindingFlags & BinderNonFieldGetSet) == 0)
                    throw new MissingFieldException(FullName, name);
            }
            #endregion

            #region Property PreConditions
            // @Legacy - This is RTM behavior
            bool isGetProperty = (bindingFlags & BindingFlags.GetProperty) != 0;
            bool isSetProperty = (bindingFlags & BindingFlags.SetProperty) != 0;

            if (isGetProperty || isSetProperty)
            {
                #region Preconditions
                if (isGetProperty)
                {
                    Debug.Assert(!IsSetField);

                    if (isSetProperty)
                        throw new ArgumentException(SR.Arg_PropSetGet, nameof(bindingFlags));
                }
                else
                {
                    Debug.Assert(isSetProperty);

                    Debug.Assert(!IsGetField);

                    if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
                        throw new ArgumentException(SR.Arg_PropSetInvoke, nameof(bindingFlags));
                }
                #endregion
            }
            #endregion

            MethodInfo[]? finalists = null;
            MethodInfo? finalist = null;

            #region BindingFlags.InvokeMethod
            if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
            {
                #region Lookup Methods
                MethodInfo[] semiFinalists = (MethodInfo[])GetMember(name, MemberTypes.Method, bindingFlags);
                LowLevelListWithIList<MethodInfo>? results = null;

                for (int i = 0; i < semiFinalists.Length; i++)
                {
                    MethodInfo semiFinalist = semiFinalists[i];
                    Debug.Assert(semiFinalist != null);

                    if (!semiFinalist.QualifiesBasedOnParameterCount(bindingFlags, CallingConventions.Any, new Type[argCnt]))
                        continue;

                    if (finalist == null)
                    {
                        finalist = semiFinalist;
                    }
                    else
                    {
                        if (results == null)
                        {
                            results = new LowLevelListWithIList<MethodInfo>(semiFinalists.Length);
                            results.Add(finalist);
                        }

                        results.Add(semiFinalist);
                    }
                }

                if (results != null)
                {
                    Debug.Assert(results.Count > 1);
                    finalists = new MethodInfo[results.Count];
                    results.CopyTo(finalists, 0);
                }
                #endregion
            }
            #endregion

            Debug.Assert(finalists == null || finalist != null);

            #region BindingFlags.GetProperty or BindingFlags.SetProperty
            if (finalist == null && isGetProperty || isSetProperty)
            {
                #region Lookup Property
                PropertyInfo[] semiFinalists = (PropertyInfo[])GetMember(name, MemberTypes.Property, bindingFlags);
                LowLevelListWithIList<MethodInfo>? results = null;

                for (int i = 0; i < semiFinalists.Length; i++)
                {
                    MethodInfo? semiFinalist;

                    if (isSetProperty)
                    {
                        semiFinalist = semiFinalists[i].GetSetMethod(true);
                    }
                    else
                    {
                        semiFinalist = semiFinalists[i].GetGetMethod(true);
                    }

                    if (semiFinalist == null)
                        continue;

                    BindingFlags expectedBindingFlags = semiFinalist.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
                    if ((bindingFlags & expectedBindingFlags) != expectedBindingFlags)
                        continue;

                    if (!semiFinalist.QualifiesBasedOnParameterCount(bindingFlags, CallingConventions.Any, new Type[argCnt]))
                        continue;

                    if (finalist == null)
                    {
                        finalist = semiFinalist;
                    }
                    else
                    {
                        if (results == null)
                        {
                            results = new LowLevelListWithIList<MethodInfo>(semiFinalists.Length);
                            results.Add(finalist);
                        }

                        results.Add(semiFinalist);
                    }
                }

                if (results != null)
                {
                    Debug.Assert(results.Count > 1);
                    finalists = new MethodInfo[results.Count];
                    results.CopyTo(finalists, 0);
                }
                #endregion
            }
            #endregion

            if (finalist != null)
            {
                #region Invoke
                if (finalists == null &&
                    argCnt == 0 &&
                    finalist.GetParametersNoCopy().Length == 0 &&
                    (bindingFlags & BindingFlags.OptionalParamBinding) == 0)
                {
                    //if (useCache && argCnt == props[0].GetParameters().Length)
                    //    AddMethodToCache(name, bindingFlags, argCnt, providedArgs, props[0]);

                    return finalist.Invoke(target, bindingFlags, binder, providedArgs, culture);
                }

                if (finalists == null)
                    finalists = new MethodInfo[] { finalist };

                if (providedArgs == null)
                    providedArgs = Array.Empty<object>();

                object? state = null;


                MethodBase? invokeMethod = null;

                try { invokeMethod = binder.BindToMethod(bindingFlags, finalists, ref providedArgs, modifiers, culture, namedParams, out state); }
                catch (MissingMethodException) { }

                if (invokeMethod == null)
                    throw new MissingMethodException(FullName, name);

                //if (useCache && argCnt == invokeMethod.GetParameters().Length)
                //    AddMethodToCache(name, bindingFlags, argCnt, providedArgs, invokeMethod);

                object? result = ((MethodInfo)invokeMethod).Invoke(target, bindingFlags, binder, providedArgs, culture);

                if (state != null)
                    binder.ReorderArgumentArray(ref providedArgs, state);

                return result;
                #endregion
            }

            throw new MissingMethodException(FullName, name);
        }
    }
}
