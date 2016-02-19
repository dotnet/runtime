// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Ported from desktop (BCL\System\DefaultBinder.cs)

using System;
using System.Reflection;
using System.Diagnostics.Contracts;

namespace Internal.Reflection.Core.Execution.Binder
{
    internal static class DefaultBinder
    {
        // This method is passed a set of methods and must choose the best
        // fit.  The methods all have the same number of arguments and the object
        // array args.  On exit, this method will choice the best fit method
        // and coerce the args to match that method.  By match, we mean all primitive
        // arguments are exact matchs and all object arguments are exact or subclasses
        // of the target.  If the target OR is an interface, the object must implement
        // that interface.  There are a couple of exceptions
        // thrown when a method cannot be returned.  If no method matchs the args and
        // ArgumentException is thrown.  If multiple methods match the args then 
        // an AmbiguousMatchException is thrown.
        // 
        // The most specific match will be selected.  
        // 
        public static MethodBase BindToMethod(MethodBase[] match, ref object[] args)
        {
            if (match == null || match.Length == 0)
            {
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(match));
            }

            MethodBase[] candidates = (MethodBase[])match.Clone();

            int i;
            int j;

            #region Map named parameters to candidate parameter postions
            // We are creating an paramOrder array to act as a mapping
            //  between the order of the args and the actual order of the
            //  parameters in the method.  This order may differ because
            //  named parameters (names) may change the order.  If names
            //  is not provided, then we assume the default mapping (0,1,...)
            int[][] paramOrder = new int[candidates.Length][];

            for (i = 0; i < candidates.Length; i++)
            {
                ParameterInfo[] par = candidates[i].GetParameters();

                // args.Length + 1 takes into account the possibility of a last paramArray that can be omitted
                paramOrder[i] = new int[(par.Length > args.Length) ? par.Length : args.Length];

                // Default mapping
                for (j = 0; j < args.Length; j++)
                {
                    paramOrder[i][j] = j;
                }
            }
            #endregion

            Type[] paramArrayTypes = new Type[candidates.Length];

            Type[] argTypes = new Type[args.Length];

            #region Cache the type of the provided arguments
            // object that contain a null are treated as if they were typeless (but match either object 
            // references or value classes).  We mark this condition by placing a null in the argTypes array.
            for (i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                {
                    argTypes[i] = args[i].GetType();
                }
            }
            #endregion


            // Find the method that matches...
            int CurIdx = 0;

            Type paramArrayType = null;

            #region Filter methods by parameter count and type
            for (i = 0; i < candidates.Length; i++)
            {
                paramArrayType = null;

                // If we have named parameters then we may have a hole in the candidates array.
                if (candidates[i] == null)
                {
                    continue;
                }

                // Validate the parameters.
                ParameterInfo[] par = candidates[i].GetParameters();

                #region Match method by parameter count
                if (par.Length == 0)
                {
                    #region No formal parameters
                    if (args.Length != 0)
                    {
                        if ((candidates[i].CallingConvention & CallingConventions.VarArgs) == 0)
                        {
                            continue;
                        }
                    }

                    // This is a valid routine so we move it up the candidates list.
                    paramOrder[CurIdx] = paramOrder[i];
                    candidates[CurIdx++] = candidates[i];

                    continue;
                    #endregion
                }
                else if (par.Length > args.Length)
                {
                    #region Shortage of provided parameters
                    // If the number of parameters is greater than the number of args then 
                    // we are in the situation were we may be using default values.
                    for (j = args.Length; j < par.Length - 1; j++)
                    {
                        if (!par[j].HasDefaultValue)
                        {
                            break;
                        }
                    }

                    if (j != par.Length - 1)
                    {
                        continue;
                    }

                    if (!par[j].HasDefaultValue)
                    {
                        if (!par[j].ParameterType.IsArray)
                        {
                            continue;
                        }

                        if (!HasParamArrayAttribute(par[j]))
                        {
                            continue;
                        }

                        paramArrayType = par[j].ParameterType.GetElementType();
                    }
                    #endregion
                }
                else if (par.Length < args.Length)
                {
                    #region Excess provided parameters
                    // test for the ParamArray case
                    int lastArgPos = par.Length - 1;

                    if (!par[lastArgPos].ParameterType.IsArray)
                    {
                        continue;
                    }

                    if (!HasParamArrayAttribute(par[lastArgPos]))
                    {
                        continue;
                    }

                    if (paramOrder[i][lastArgPos] != lastArgPos)
                    {
                        continue;
                    }

                    paramArrayType = par[lastArgPos].ParameterType.GetElementType();
                    #endregion
                }
                else
                {
                    #region Test for paramArray, save paramArray type
                    int lastArgPos = par.Length - 1;

                    if (par[lastArgPos].ParameterType.IsArray
                        && HasParamArrayAttribute(par[lastArgPos])
                        && paramOrder[i][lastArgPos] == lastArgPos)
                    {
                        if (!par[lastArgPos].ParameterType.GetTypeInfo().IsAssignableFrom(argTypes[lastArgPos].GetTypeInfo()))
                        {
                            paramArrayType = par[lastArgPos].ParameterType.GetElementType();
                        }
                    }
                    #endregion
                }
                #endregion

                Type pCls = null;
                int argsToCheck = (paramArrayType != null) ? par.Length - 1 : args.Length;

                #region Match method by parameter type
                for (j = 0; j < argsToCheck; j++)
                {
                    #region Classic argument coersion checks
                    // get the formal type
                    pCls = par[j].ParameterType;

                    if (pCls.IsByRef)
                    {
                        pCls = pCls.GetElementType();
                    }

                    // the type is the same
                    if (pCls == argTypes[paramOrder[i][j]])
                    {
                        continue;
                    }

                    // the argument was null, so it matches with everything
                    if (args[paramOrder[i][j]] == null)
                    {
                        continue;
                    }

                    // the type is Object, so it will match everything
                    if (pCls == typeof(Object))
                    {
                        continue;
                    }

                    // now do a "classic" type check
                    if (pCls.GetTypeInfo().IsPrimitive)
                    {
                        if (argTypes[paramOrder[i][j]] == null || !CanConvertPrimitiveObjectToType(args[paramOrder[i][j]], pCls))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (argTypes[paramOrder[i][j]] == null)
                        {
                            continue;
                        }

                        if (!pCls.GetTypeInfo().IsAssignableFrom(argTypes[paramOrder[i][j]].GetTypeInfo()))
                        {
                            break;
                        }
                    }
                    #endregion
                }

                if (paramArrayType != null && j == par.Length - 1)
                {
                    #region Check that excess arguments can be placed in the param array

                    // Legacy: It's so pathetic that we go to all this trouble let "widening-compatible" params arguments through this screen 
                    // only to end up blowing up with an InvalidCastException anyway because we use Array.Copy() to do the actual copy down below.
                    // Ah, the joys of backward compatibility...
                    for (; j < args.Length; j++)
                    {
                        if (paramArrayType.GetTypeInfo().IsPrimitive)
                        {
                            if (argTypes[j] == null || !CanConvertPrimitiveObjectToType(args[j], paramArrayType))
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (argTypes[j] == null)
                            {
                                continue;
                            }

                            if (!paramArrayType.GetTypeInfo().IsAssignableFrom(argTypes[j].GetTypeInfo()))
                            {
                                break;
                            }
                        }
                    }
                    #endregion
                }
                #endregion

                if (j == args.Length)
                {
                    #region This is a valid routine so we move it up the candidates list
                    paramOrder[CurIdx] = paramOrder[i];
                    paramArrayTypes[CurIdx] = paramArrayType;
                    candidates[CurIdx++] = candidates[i];
                    #endregion
                }
            }
            #endregion

            // If we didn't find a method 
            if (CurIdx == 0)
            {
                throw new MissingMethodException(SR.MissingMember);
            }

            if (CurIdx == 1)
            {
                #region Found only one method
                // If the parameters and the args are not the same length or there is a paramArray
                //  then we need to create a argument array.
                ParameterInfo[] parms = candidates[0].GetParameters();

                if (parms.Length == args.Length)
                {
                    if (paramArrayTypes[0] != null)
                    {
                        Object[] objs = new Object[parms.Length];
                        int lastPos = parms.Length - 1;
                        Array.Copy(args, 0, objs, 0, lastPos);
                        objs[lastPos] = Array.CreateInstance(paramArrayTypes[0], 1);
                        ((Array)objs[lastPos]).SetValue(args[lastPos], 0);
                        args = objs;
                    }
                }
                else if (parms.Length > args.Length)
                {
                    Object[] objs = new Object[parms.Length];

                    for (i = 0; i < args.Length; i++)
                    {
                        objs[i] = args[i];
                    }

                    for (; i < parms.Length - 1; i++)
                    {
                        objs[i] = parms[i].DefaultValue;
                    }

                    if (paramArrayTypes[0] != null)
                    {
                        objs[i] = Array.CreateInstance(paramArrayTypes[0], 0); // create an empty array for the 
                    }

                    else
                    {
                        objs[i] = parms[i].DefaultValue;
                    }

                    args = objs;
                }
                else
                {
                    if ((candidates[0].CallingConvention & CallingConventions.VarArgs) == 0)
                    {
                        Object[] objs = new Object[parms.Length];
                        int paramArrayPos = parms.Length - 1;
                        Array.Copy(args, 0, objs, 0, paramArrayPos);
                        objs[paramArrayPos] = Array.CreateInstance(paramArrayTypes[0], args.Length - paramArrayPos);
                        Array.Copy(args, paramArrayPos, (System.Array)objs[paramArrayPos], 0, args.Length - paramArrayPos);
                        args = objs;
                    }
                }
                #endregion

                return candidates[0];
            }

            int currentMin = 0;
            bool ambig = false;
            for (i = 1; i < CurIdx; i++)
            {
                #region Walk all of the methods looking the most specific method to invoke
                int newMin = FindMostSpecificMethod(candidates[currentMin], paramOrder[currentMin], paramArrayTypes[currentMin],
                                                    candidates[i], paramOrder[i], paramArrayTypes[i], argTypes, args);

                if (newMin == 0)
                {
                    ambig = true;
                }
                else if (newMin == 2)
                {
                    currentMin = i;
                    ambig = false;
                }
                #endregion
            }

            if (ambig)
            {
                throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
            }

            // If the parameters and the args are not the same length or there is a paramArray
            //  then we need to create a argument array.
            ParameterInfo[] parameters = candidates[currentMin].GetParameters();
            if (parameters.Length == args.Length)
            {
                if (paramArrayTypes[currentMin] != null)
                {
                    Object[] objs = new Object[parameters.Length];
                    int lastPos = parameters.Length - 1;
                    Array.Copy(args, 0, objs, 0, lastPos);
                    objs[lastPos] = Array.CreateInstance(paramArrayTypes[currentMin], 1);
                    ((Array)objs[lastPos]).SetValue(args[lastPos], 0);
                    args = objs;
                }
            }
            else if (parameters.Length > args.Length)
            {
                Object[] objs = new Object[parameters.Length];

                for (i = 0; i < args.Length; i++)
                {
                    objs[i] = args[i];
                }

                for (; i < parameters.Length - 1; i++)
                {
                    objs[i] = parameters[i].DefaultValue;
                }

                if (paramArrayTypes[currentMin] != null)
                {
                    objs[i] = Array.CreateInstance(paramArrayTypes[currentMin], 0);
                }
                else
                {
                    objs[i] = parameters[i].DefaultValue;
                }

                args = objs;
            }
            else
            {
                if ((candidates[currentMin].CallingConvention & CallingConventions.VarArgs) == 0)
                {
                    Object[] objs = new Object[parameters.Length];
                    int paramArrayPos = parameters.Length - 1;
                    Array.Copy(args, 0, objs, 0, paramArrayPos);
                    objs[paramArrayPos] = Array.CreateInstance(paramArrayTypes[currentMin], args.Length - paramArrayPos);
                    Array.Copy(args, paramArrayPos, (System.Array)objs[paramArrayPos], 0, args.Length - paramArrayPos);
                    args = objs;
                }
            }

            return candidates[currentMin];
        }


        // Given a set of methods that match the base criteria, select a method based
        // upon an array of types.  This method should return null if no method matchs
        // the criteria.
        public static MethodBase SelectMethod(MethodBase[] match, Type[] types)
        {
            int i;
            int j;

            MethodBase[] candidates = (MethodBase[])match.Clone();

            // Find all the methods that can be described by the types parameter. 
            //  Remove all of them that cannot.
            int CurIdx = 0;
            for (i = 0; i < candidates.Length; i++)
            {
                ParameterInfo[] par = candidates[i].GetParameters();
                if (par.Length != types.Length)
                    continue;
                for (j = 0; j < types.Length; j++)
                {
                    Type pCls = par[j].ParameterType;
                    if (pCls == types[j])
                    {
                        continue;
                    }
                    if (pCls == typeof(Object))
                    {
                        continue;
                    }
                    if (pCls.GetTypeInfo().IsPrimitive)
                    {
                        if (!CanConvertPrimitive(types[j], pCls))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (!pCls.GetTypeInfo().IsAssignableFrom(types[j].GetTypeInfo()))
                        {
                            break;
                        }
                    }
                }
                if (j == types.Length)
                {
                    candidates[CurIdx++] = candidates[i];
                }
            }
            if (CurIdx == 0)
            {
                return null;
            }
            if (CurIdx == 1)
            {
                return candidates[0];
            }

            // Walk all of the methods looking the most specific method to invoke
            int currentMin = 0;
            bool ambig = false;
            int[] paramOrder = new int[types.Length];
            for (i = 0; i < types.Length; i++)
            {
                paramOrder[i] = i;
            }
            for (i = 1; i < CurIdx; i++)
            {
                int newMin = FindMostSpecificMethod(candidates[currentMin], paramOrder, null, candidates[i], paramOrder, null, types, null);
                if (newMin == 0)
                {
                    ambig = true;
                }
                else
                {
                    if (newMin == 2)
                    {
                        currentMin = i;
                        ambig = false;
                        currentMin = i;
                    }
                }
            }
            if (ambig)
            {
                throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
            }

            return candidates[currentMin];
        }

        private static bool HasParamArrayAttribute(ParameterInfo parameterInfo)
        {
            foreach (CustomAttributeData cad in parameterInfo.CustomAttributes)
            {
                if (cad.AttributeType.Equals(typeof(ParamArrayAttribute)))
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindMostSpecific(ParameterInfo[] p1, int[] paramOrder1, Type paramArrayType1,
                                            ParameterInfo[] p2, int[] paramOrder2, Type paramArrayType2,
                                            Type[] types, Object[] args)
        {
            // A method using params is always less specific than one not using params
            if (paramArrayType1 != null && paramArrayType2 == null)
            {
                return 2;
            }
            if (paramArrayType2 != null && paramArrayType1 == null)
            {
                return 1;
            }

            // now either p1 and p2 both use params or neither does.

            bool p1Less = false;
            bool p2Less = false;

            for (int i = 0; i < types.Length; i++)
            {
                if (args != null && args[i] == Type.Missing)
                {
                    continue;
                }

                Type c1, c2;

                //  If a param array is present, then either
                //      the user re-ordered the parameters in which case
                //          the argument to the param array is either an array
                //              in which case the params is conceptually ignored and so paramArrayType1 == null
                //          or the argument to the param array is a single element
                //              in which case paramOrder[i] == p1.Length - 1 for that element
                //      or the user did not re-order the parameters in which case
                //          the paramOrder array could contain indexes larger than p.Length - 1 (see VSW 577286)
                //          so any index >= p.Length - 1 is being put in the param array

                if (paramArrayType1 != null && paramOrder1[i] >= p1.Length - 1)
                {
                    c1 = paramArrayType1;
                }
                else
                {
                    c1 = p1[paramOrder1[i]].ParameterType;
                }

                if (paramArrayType2 != null && paramOrder2[i] >= p2.Length - 1)
                {
                    c2 = paramArrayType2;
                }
                else
                {
                    c2 = p2[paramOrder2[i]].ParameterType;
                }

                if (c1 == c2)
                {
                    continue;
                }

                switch (FindMostSpecificType(c1, c2, types[i]))
                {
                    case 0: return 0;
                    case 1: p1Less = true; break;
                    case 2: p2Less = true; break;
                }
            }

            // Two way p1Less and p2Less can be equal.  All the arguments are the
            //  same they both equal false, otherwise there were things that both
            //  were the most specific type on....
            if (p1Less == p2Less)
            {
                // if we cannot tell which is a better match based on parameter types (p1Less == p2Less),
                // let's see which one has the most matches without using the params array (the longer one wins).
                if (!p1Less && args != null)
                {
                    if (p1.Length > p2.Length)
                    {
                        return 1;
                    }
                    else if (p2.Length > p1.Length)
                    {
                        return 2;
                    }
                }

                return 0;
            }
            else
            {
                return (p1Less == true) ? 1 : 2;
            }
        }

        private static int FindMostSpecificType(Type c1, Type c2, Type t)
        {
            // If the two types are exact move on...
            if (c1 == c2)
            {
                return 0;
            }

            if (c1 == t)
            {
                return 1;
            }

            if (c2 == t)
            {
                return 2;
            }

            bool c1FromC2;
            bool c2FromC1;

            if (c1.IsByRef || c2.IsByRef)
            {
                if (c1.IsByRef && c2.IsByRef)
                {
                    c1 = c1.GetElementType();
                    c2 = c2.GetElementType();
                }
                else if (c1.IsByRef)
                {
                    if (c1.GetElementType() == c2)
                    {
                        return 2;
                    }

                    c1 = c1.GetElementType();
                }
                else
                {
                    if (c2.GetElementType() == c1)
                    {
                        return 1;
                    }

                    c2 = c2.GetElementType();
                }
            }


            if (c1.GetTypeInfo().IsPrimitive && c2.GetTypeInfo().IsPrimitive)
            {
                c1FromC2 = CanConvertPrimitive(c2, c1);
                c2FromC1 = CanConvertPrimitive(c1, c2);
            }
            else
            {
                c1FromC2 = c1.GetTypeInfo().IsAssignableFrom(c2.GetTypeInfo());
                c2FromC1 = c2.GetTypeInfo().IsAssignableFrom(c1.GetTypeInfo());
            }

            if (c1FromC2 == c2FromC1)
                return 0;

            if (c1FromC2)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        public static PropertyInfo SelectProperty(PropertyInfo[] match, Type returnType, Type[] indexes)
        {
            // Allow a null indexes array. But if it is not null, every element must be non-null as well.
            if (indexes != null && !Contract.ForAll(indexes, delegate (Type t) { return t != null; }))
            {
                Exception e;  // Written this way to pass the Code Contracts style requirements.
                e = new ArgumentNullException(nameof(indexes));
                throw e;
            }
            if (match == null || match.Length == 0)
            {
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(match));
            }

            PropertyInfo[] candidates = (PropertyInfo[])match.Clone();

            int i, j = 0;

            // Find all the properties that can be described by type indexes parameter
            int CurIdx = 0;
            int indexesLength = (indexes != null) ? indexes.Length : 0;
            for (i = 0; i < candidates.Length; i++)
            {
                if (indexes != null)
                {
                    ParameterInfo[] par = candidates[i].GetIndexParameters();
                    if (par.Length != indexesLength)
                    {
                        continue;
                    }

                    for (j = 0; j < indexesLength; j++)
                    {
                        Type pCls = par[j].ParameterType;

                        // If the classes  exactly match continue
                        if (pCls == indexes[j])
                        {
                            continue;
                        }
                        if (pCls == typeof(Object))
                        {
                            continue;
                        }

                        if (pCls.GetTypeInfo().IsPrimitive)
                        {
                            if (!CanConvertPrimitive(indexes[j], pCls))
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (!pCls.GetTypeInfo().IsAssignableFrom(indexes[j].GetTypeInfo()))
                            {
                                break;
                            }
                        }
                    }
                }

                if (j == indexesLength)
                {
                    if (returnType != null)
                    {
                        if (candidates[i].PropertyType.GetTypeInfo().IsPrimitive)
                        {
                            if (!CanConvertPrimitive(returnType, candidates[i].PropertyType))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!candidates[i].PropertyType.GetTypeInfo().IsAssignableFrom(returnType.GetTypeInfo()))
                            {
                                continue;
                            }
                        }
                    }
                    candidates[CurIdx++] = candidates[i];
                }
            }
            if (CurIdx == 0)
            {
                return null;
            }
            if (CurIdx == 1)
            {
                return candidates[0];
            }

            // Walk all of the properties looking the most specific method to invoke
            int currentMin = 0;
            bool ambig = false;
            int[] paramOrder = new int[indexesLength];
            for (i = 0; i < indexesLength; i++)
            {
                paramOrder[i] = i;
            }
            for (i = 1; i < CurIdx; i++)
            {
                int newMin = FindMostSpecificType(candidates[currentMin].PropertyType, candidates[i].PropertyType, returnType);
                if (newMin == 0 && indexes != null)
                    newMin = FindMostSpecific(candidates[currentMin].GetIndexParameters(),
                                              paramOrder,
                                              null,
                                              candidates[i].GetIndexParameters(),
                                              paramOrder,
                                              null,
                                              indexes,
                                              null);
                if (newMin == 0)
                {
                    newMin = FindMostSpecificProperty(candidates[currentMin], candidates[i]);
                    if (newMin == 0)
                    {
                        ambig = true;
                    }
                }
                if (newMin == 2)
                {
                    ambig = false;
                    currentMin = i;
                }
            }

            if (ambig)
            {
                throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
            }

            return candidates[currentMin];
        }

        private static int FindMostSpecificMethod(MethodBase m1, int[] paramOrder1, Type paramArrayType1,
                                                  MethodBase m2, int[] paramOrder2, Type paramArrayType2,
                                                  Type[] types, Object[] args)
        {
            // Find the most specific method based on the parameters.
            int res = FindMostSpecific(m1.GetParameters(), paramOrder1, paramArrayType1,
                                       m2.GetParameters(), paramOrder2, paramArrayType2, types, args);

            // If the match was not ambigous then return the result.
            if (res != 0)
            {
                return res;
            }

            // Check to see if the methods have the exact same name and signature.
            if (CompareMethodSigAndName(m1, m2))
            {
                // Determine the depth of the declaring types for both methods.
                int hierarchyDepth1 = GetHierarchyDepth(m1.DeclaringType);
                int hierarchyDepth2 = GetHierarchyDepth(m2.DeclaringType);

                // The most derived method is the most specific one.
                if (hierarchyDepth1 == hierarchyDepth2)
                {
                    return 0;
                }
                else if (hierarchyDepth1 < hierarchyDepth2)
                {
                    return 2;
                }
                else
                {
                    return 1;
                }
            }

            // The match is ambigous.
            return 0;
        }

        private static int FindMostSpecificProperty(PropertyInfo cur1, PropertyInfo cur2)
        {
            // Check to see if the fields have the same name.
            if (cur1.Name == cur2.Name)
            {
                int hierarchyDepth1 = GetHierarchyDepth(cur1.DeclaringType);
                int hierarchyDepth2 = GetHierarchyDepth(cur2.DeclaringType);

                if (hierarchyDepth1 == hierarchyDepth2)
                {
                    return 0;
                }
                else if (hierarchyDepth1 < hierarchyDepth2)
                {
                    return 2;
                }
                else
                {
                    return 1;
                }
            }

            // The match is ambigous.
            return 0;
        }

        private static bool CompareMethodSigAndName(MethodBase m1, MethodBase m2)
        {
            ParameterInfo[] params1 = m1.GetParameters();
            ParameterInfo[] params2 = m2.GetParameters();

            if (params1.Length != params2.Length)
            {
                return false;
            }

            int numParams = params1.Length;
            for (int i = 0; i < numParams; i++)
            {
                if (params1[i].ParameterType != params2[i].ParameterType)
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetHierarchyDepth(Type t)
        {
            int depth = 0;

            Type currentType = t;
            do
            {
                depth++;
                currentType = currentType.GetTypeInfo().BaseType;
            } while (currentType != null);

            return depth;
        }

        // CanConvertPrimitive
        // This will determine if the source can be converted to the target type
        private static bool CanConvertPrimitive(Type source, Type target)
        {
            return CanPrimitiveWiden(source, target);
        }

        // CanConvertPrimitiveObjectToType
        private static bool CanConvertPrimitiveObjectToType(Object source, Type type)
        {
            return CanConvertPrimitive(source.GetType(), type);
        }

        #region Portable Runtime Augments Methods
        private static bool CanPrimitiveWiden(Type source, Type target)
        {
            Primitives widerCodes = _primitiveConversions[(int)GetTypeCode(source)];
            Primitives targetCode = (Primitives)(1 << (int)GetTypeCode(target));

            return 0 != (widerCodes & targetCode);
        }

        [Flags]
        private enum Primitives
        {
            Boolean = 1 << (int)TypeCode.Boolean,
            Char = 1 << (int)TypeCode.Char,
            SByte = 1 << (int)TypeCode.SByte,
            Byte = 1 << (int)TypeCode.Byte,
            Int16 = 1 << (int)TypeCode.Int16,
            UInt16 = 1 << (int)TypeCode.UInt16,
            Int32 = 1 << (int)TypeCode.Int32,
            UInt32 = 1 << (int)TypeCode.UInt32,
            Int64 = 1 << (int)TypeCode.Int64,
            UInt64 = 1 << (int)TypeCode.UInt64,
            Single = 1 << (int)TypeCode.Single,
            Double = 1 << (int)TypeCode.Double,
            Decimal = 1 << (int)TypeCode.Decimal,
            DateTime = 1 << (int)TypeCode.DateTime,
            String = 1 << (int)TypeCode.String,
        }


        private static Primitives[] _primitiveConversions = new Primitives[]
        {
            /* Empty    */  0, // not primitive
            /* Object   */  0, // not primitive
            /* DBNull   */  0, // not exposed.
            /* Boolean  */  Primitives.Boolean,
            /* Char     */  Primitives.Char    | Primitives.UInt16 | Primitives.UInt32 | Primitives.Int32  | Primitives.UInt64 | Primitives.Int64  | Primitives.Single |  Primitives.Double,
            /* SByte    */  Primitives.SByte   | Primitives.Int16  | Primitives.Int32  | Primitives.Int64  | Primitives.Single | Primitives.Double,
            /* Byte     */  Primitives.Byte    | Primitives.Char   | Primitives.UInt16 | Primitives.Int16  | Primitives.UInt32 | Primitives.Int32  | Primitives.UInt64 |  Primitives.Int64 |  Primitives.Single |  Primitives.Double,
            /* Int16    */  Primitives.Int16   | Primitives.Int32  | Primitives.Int64  | Primitives.Single | Primitives.Double,
            /* UInt16   */  Primitives.UInt16  | Primitives.UInt32 | Primitives.Int32  | Primitives.UInt64 | Primitives.Int64  | Primitives.Single | Primitives.Double,
            /* Int32    */  Primitives.Int32   | Primitives.Int64  | Primitives.Single | Primitives.Double |
            /* UInt32   */  Primitives.UInt32  | Primitives.UInt64 | Primitives.Int64  | Primitives.Single | Primitives.Double,
            /* Int64    */  Primitives.Int64   | Primitives.Single | Primitives.Double,
            /* UInt64   */  Primitives.UInt64  | Primitives.Single | Primitives.Double,
            /* Single   */  Primitives.Single  | Primitives.Double,
            /* Double   */  Primitives.Double,
            /* Decimal  */  Primitives.Decimal,
            /* DateTime */  Primitives.DateTime,
            /* [Unused] */  0,
            /* String   */  Primitives.String,
        };

        private static TypeCode GetTypeCode(Type type)
        {
            if (type == typeof(Boolean))
            {
                return TypeCode.Boolean;
            }

            if (type == typeof(Char))
            {
                return TypeCode.Char;
            }

            if (type == typeof(SByte))
            {
                return TypeCode.SByte;
            }

            if (type == typeof(Byte))
            {
                return TypeCode.Byte;
            }

            if (type == typeof(Int16))
            {
                return TypeCode.Int16;
            }

            if (type == typeof(UInt16))
            {
                return TypeCode.UInt16;
            }

            if (type == typeof(Int32))
            {
                return TypeCode.Int32;
            }

            if (type == typeof(UInt32))
            {
                return TypeCode.UInt32;
            }

            if (type == typeof(Int64))
            {
                return TypeCode.Int64;
            }

            if (type == typeof(UInt64))
            {
                return TypeCode.UInt64;
            }

            if (type == typeof(Single))
            {
                return TypeCode.Single;
            }

            if (type == typeof(Double))
            {
                return TypeCode.Double;
            }

            if (type == typeof(Decimal))
            {
                return TypeCode.Decimal;
            }

            if (type == typeof(DateTime))
            {
                return TypeCode.DateTime;
            }

            if (type.GetTypeInfo().IsEnum)
            {
                return GetTypeCode(Enum.GetUnderlyingType(type));
            }

            return TypeCode.Object;
        }
        #endregion Portable Runtime Augments Methods
    }
}
