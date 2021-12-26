// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.TypeLoading;
using CultureInfo = System.Globalization.CultureInfo;

namespace System
{
    internal sealed partial class DefaultBinder : Binder
    {
        private readonly MetadataLoadContext _loader;
        private readonly Type? _objectType;

        internal DefaultBinder(MetadataLoadContext loader)
        {
            _loader = loader;
            _objectType = loader.TryGetCoreType(CoreType.Object);
        }

        private bool IsImplementedByMetadataLoadContext(Type type) => type is RoType roType && roType.Loader == _loader;

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
        public sealed override MethodBase BindToMethod(
            BindingFlags bindingAttr, MethodBase[] match, ref object?[] args,
            ParameterModifier[]? modifiers, CultureInfo? cultureInfo, string[]? names, out object state) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        // Given a set of fields that match the base criteria, select a field.
        // if value is null then we have no way to select a field
        public sealed override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo? cultureInfo) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        // Given a set of methods that match the base criteria, select a method based upon an array of types.
        // This method should return null if no method matches the criteria.
        public sealed override MethodBase? SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[]? modifiers)
        {
            int i;
            int j;

            Type[] realTypes = new Type[types.Length];
            for (i = 0; i < types.Length; i++)
            {
                realTypes[i] = types[i].UnderlyingSystemType;
                if (!(IsImplementedByMetadataLoadContext(realTypes[i]) || realTypes[i].IsSignatureType()))
                    throw new ArgumentException(SR.Arg_MustBeType, nameof(types));
            }
            types = realTypes;

            // We don't automatically jump out on exact match.
            if (match == null || match.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(match));

            MethodBase[] candidates = (MethodBase[])match.Clone();

            // Find all the methods that can be described by the types parameter.
            // Remove all of them that cannot.
            int curIdx = 0;
            for (i = 0; i < candidates.Length; i++)
            {
                ParameterInfo[] par = candidates[i].GetParametersNoCopy();
                if (par.Length != types.Length)
                    continue;
                for (j = 0; j < types.Length; j++)
                {
                    Type pCls = par[j].ParameterType;
                    if (types[j].MatchesParameterTypeExactly(par[j]))
                        continue;
                    if (pCls == _objectType)
                        continue;

                    Type? type = types[j];
                    if (type.IsSignatureType())
                    {
                        if (!(candidates[i] is MethodInfo methodInfo))
                            break;
                        type = type.TryResolveAgainstGenericMethod(methodInfo);
                        if (type == null)
                            break;
                    }

                    if (pCls.IsPrimitive)
                    {
                        if (!(IsImplementedByMetadataLoadContext(type.UnderlyingSystemType)) ||
                            !CanChangePrimitive(type.UnderlyingSystemType, pCls.UnderlyingSystemType))
                            break;
                    }
                    else
                    {
                        if (!pCls.IsAssignableFrom(type))
                            break;
                    }
                }
                if (j == types.Length)
                    candidates[curIdx++] = candidates[i];
            }
            if (curIdx == 0)
                return null;
            if (curIdx == 1)
                return candidates[0];

            // Walk all of the methods looking the most specific method to invoke
            int currentMin = 0;
            bool ambig = false;
            int[] paramOrder = new int[types.Length];
            for (i = 0; i < types.Length; i++)
                paramOrder[i] = i;
            for (i = 1; i < curIdx; i++)
            {
                int newMin = FindMostSpecificMethod(
                    candidates[currentMin],
                    paramOrder,
                    paramArrayType1: null,
                    candidates[i],
                    paramOrder,
                    paramArrayType2: null,
                    types, args: null);

                if (newMin == 0)
                {
                    ambig = true;
                }
                else if (newMin == 2)
                {
                    currentMin = i;
                    ambig = false;
                }
            }
            if (ambig)
                throw new AmbiguousMatchException();
            return candidates[currentMin];
        }

        // Given a set of properties that match the base criteria, select one.
        public sealed override PropertyInfo? SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type? returnType,
                    Type[]? indexes, ParameterModifier[]? modifiers)
        {
            // Allow a null indexes array. But if it is not null, every element must be non-null as well.
            if (indexes != null)
            {
                foreach (Type index in indexes)
                {
                    if (index == null)
                        throw new ArgumentNullException(nameof(indexes));
                }
            }

            if (match == null || match.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(match));

            PropertyInfo[] candidates = (PropertyInfo[])match.Clone();

            int i, j = 0;

            // Find all the properties that can be described by type indexes parameter
            int curIdx = 0;
            int indexesLength = (indexes != null) ? indexes.Length : 0;
            for (i = 0; i < candidates.Length; i++)
            {
                if (indexes != null)
                {
                    ParameterInfo[] par = candidates[i].GetIndexParameters();
                    if (par.Length != indexesLength)
                        continue;

                    for (j = 0; j < indexesLength; j++)
                    {
                        Type pCls = par[j].ParameterType;

                        // If the classes exactly match continue
                        if (pCls == indexes[j])
                            continue;
                        if (pCls == _objectType)
                            continue;

                        if (pCls.IsPrimitive)
                        {
                            if (!(IsImplementedByMetadataLoadContext(indexes[j].UnderlyingSystemType)) ||
                                !CanChangePrimitive(indexes[j].UnderlyingSystemType, pCls.UnderlyingSystemType))
                                break;
                        }
                        else
                        {
                            if (!pCls.IsAssignableFrom(indexes[j]))
                                break;
                        }
                    }
                }

                if (j == indexesLength)
                {
                    if (returnType != null)
                    {
                        if (candidates[i].PropertyType.IsPrimitive)
                        {
                            if (!(IsImplementedByMetadataLoadContext(returnType.UnderlyingSystemType)) ||
                                !CanChangePrimitive(returnType.UnderlyingSystemType, candidates[i].PropertyType.UnderlyingSystemType))
                                continue;
                        }
                        else
                        {
                            if (!candidates[i].PropertyType.IsAssignableFrom(returnType))
                                continue;
                        }
                    }
                    candidates[curIdx++] = candidates[i];
                }
            }
            if (curIdx == 0)
                return null;
            if (curIdx == 1)
                return candidates[0];

            // Walk all of the properties looking for the most specific method to invoke
            int currentMin = 0;
            bool ambig = false;
            int[] paramOrder = new int[indexesLength];
            for (i = 0; i < indexesLength; i++)
                paramOrder[i] = i;
            for (i = 1; i < curIdx; i++)
            {
                Debug.Assert(returnType != null);
                int newMin = FindMostSpecificType(candidates[currentMin].PropertyType, candidates[i].PropertyType, returnType);
                if (newMin == 0 && indexes != null)
                    newMin = FindMostSpecific(
                        candidates[currentMin].GetIndexParameters(),
                        paramOrder,
                        paramArrayType1: null,
                        candidates[i].GetIndexParameters(),
                        paramOrder,
                        paramArrayType2: null,
                        indexes,
                        args: null);

                if (newMin == 0)
                {
                    newMin = FindMostSpecificProperty(candidates[currentMin], candidates[i]);
                    if (newMin == 0)
                        ambig = true;
                }
                if (newMin == 2)
                {
                    ambig = false;
                    currentMin = i;
                }
            }

            if (ambig)
                throw new AmbiguousMatchException();
            return candidates[currentMin];
        }

        // The default binder doesn't support any change type functionality.
        // This is because the default is built into the low level invoke code.
        public override object ChangeType(object value, Type type, CultureInfo? cultureInfo) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        public sealed override void ReorderArgumentArray(ref object?[] args, object state) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        // Return any exact bindings that may exist. (This method is not defined on the
        // Binder and is used by RuntimeType.)
        public static MethodBase? ExactBinding(MethodBase[] match, Type[] types, ParameterModifier[]? modifiers)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            MethodBase[] aExactMatches = new MethodBase[match.Length];
            int cExactMatches = 0;

            for (int i = 0; i < match.Length; i++)
            {
                ParameterInfo[] par = match[i].GetParametersNoCopy();
                if (par.Length == 0)
                {
                    continue;
                }
                int j;
                for (j = 0; j < types.Length; j++)
                {
                    Type pCls = par[j].ParameterType;

                    // If the classes  exactly match continue
                    if (!pCls.Equals(types[j]))
                        break;
                }
                if (j < types.Length)
                    continue;

                // Add the exact match to the array of exact matches.
                aExactMatches[cExactMatches] = match[i];
                cExactMatches++;
            }

            if (cExactMatches == 0)
                return null;

            if (cExactMatches == 1)
                return aExactMatches[0];

            return FindMostDerivedNewSlotMeth(aExactMatches, cExactMatches);
        }

        // Return any exact bindings that may exist. (This method is not defined on the
        //  Binder and is used by RuntimeType.)
        public static PropertyInfo? ExactPropertyBinding(PropertyInfo[] match, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            PropertyInfo? bestMatch = null;
            int typesLength = (types != null) ? types.Length : 0;
            for (int i = 0; i < match.Length; i++)
            {
                ParameterInfo[] par = match[i].GetIndexParameters();
                int j;
                for (j = 0; j < typesLength; j++)
                {
                    Type pCls = par[j].ParameterType;

                    // If the classes  exactly match continue
                    if (pCls != types![j])
                        break;
                }
                if (j < typesLength)
                    continue;
                if (returnType != null && returnType != match[i].PropertyType)
                    continue;

                if (bestMatch != null)
                    throw new AmbiguousMatchException();

                bestMatch = match[i];
            }
            return bestMatch;
        }

        private static int FindMostSpecific(ParameterInfo[] p1, int[] paramOrder1, Type? paramArrayType1,
                                            ParameterInfo[] p2, int[] paramOrder2, Type? paramArrayType2,
                                            Type[] types, object[]? args)
        {
            // A method using params is always less specific than one not using params
            if (paramArrayType1 != null && paramArrayType2 == null) return 2;
            if (paramArrayType2 != null && paramArrayType1 == null) return 1;

            // now either p1 and p2 both use params or neither does.

            bool param1Less = false;
            bool param2Less = false;

            for (int i = 0; i < types.Length; i++)
            {
                if (args != null && args[i] == Type.Missing)
                    continue;

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
                    c1 = paramArrayType1;
                else
                    c1 = p1[paramOrder1[i]].ParameterType;

                if (paramArrayType2 != null && paramOrder2[i] >= p2.Length - 1)
                    c2 = paramArrayType2;
                else
                    c2 = p2[paramOrder2[i]].ParameterType;

                if (c1 == c2) continue;

                switch (FindMostSpecificType(c1, c2, types[i]))
                {
                    case 0: return 0;
                    case 1: param1Less = true; break;
                    case 2: param2Less = true; break;
                }
            }

            // Two ways param1Less and param2Less can be equal: all the arguments are the
            //  same they both equal false, otherwise there were things that both
            //  were the most specific type on.
            if (param1Less == param2Less)
            {
                // If we cannot tell which is a better match based on parameter types (param1Less == param2Less),
                // let's see which one has the most matches without using the params array (the longer one wins).
                if (!param1Less && args != null)
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
                return (param1Less == true) ? 1 : 2;
            }
        }

        private static int FindMostSpecificType(Type c1, Type c2, Type t)
        {
            // If the two types are exact move on...
            if (c1 == c2)
                return 0;

            if (t.IsSignatureType())
            {
                if (t.MatchesExactly(c1))
                    return 1;

                if (t.MatchesExactly(c2))
                    return 2;
            }
            else
            {
                if (c1 == t)
                    return 1;

                if (c2 == t)
                    return 2;
            }

            bool c1FromC2;
            bool c2FromC1;

            if (c1.IsByRef || c2.IsByRef)
            {
                if (c1.IsByRef && c2.IsByRef)
                {
                    c1 = c1.GetElementType()!;
                    c2 = c2.GetElementType()!;
                }
                else if (c1.IsByRef)
                {
                    if (c1.GetElementType() == c2)
                        return 2;

                    c1 = c1.GetElementType()!;
                }
                else
                {
                    if (c2.GetElementType() == c1)
                        return 1;

                    c2 = c2.GetElementType()!;
                }
            }


            if (c1.IsPrimitive && c2.IsPrimitive)
            {
                c1FromC2 = CanChangePrimitive(c2, c1);
                c2FromC1 = CanChangePrimitive(c1, c2);
            }
            else
            {
                c1FromC2 = c1.IsAssignableFrom(c2);
                c2FromC1 = c2.IsAssignableFrom(c1);
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

        private static int FindMostSpecificMethod(MethodBase m1, int[] paramOrder1, Type? paramArrayType1,
                                                  MethodBase m2, int[] paramOrder2, Type? paramArrayType2,
                                                  Type[] types, object[]? args)
        {
            // Find the most specific method based on the parameters.
            int res = FindMostSpecific(m1.GetParametersNoCopy(), paramOrder1, paramArrayType1,
                                       m2.GetParametersNoCopy(), paramOrder2, paramArrayType2, types, args);

            // If the match was not ambigous then return the result.
            if (res != 0)
                return res;

            // Check to see if the methods have the exact same name and signature.
            if (CompareMethodSig(m1, m2))
            {
                // Determine the depth of the declaring types for both methods.
                int hierarchyDepth1 = GetHierarchyDepth(m1.DeclaringType!);
                int hierarchyDepth2 = GetHierarchyDepth(m2.DeclaringType!);

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
                int hierarchyDepth1 = GetHierarchyDepth(cur1.DeclaringType!);
                int hierarchyDepth2 = GetHierarchyDepth(cur2.DeclaringType!);

                if (hierarchyDepth1 == hierarchyDepth2)
                {
                    return 0;
                }
                else if (hierarchyDepth1 < hierarchyDepth2)
                    return 2;
                else
                    return 1;
            }

            // The match is ambigous.
            return 0;
        }

        public static bool CompareMethodSig(MethodBase m1, MethodBase m2)
        {
            ParameterInfo[] params1 = m1.GetParametersNoCopy();
            ParameterInfo[] params2 = m2.GetParametersNoCopy();

            if (params1.Length != params2.Length)
                return false;

            int numParams = params1.Length;
            for (int i = 0; i < numParams; i++)
            {
                if (params1[i].ParameterType != params2[i].ParameterType)
                    return false;
            }

            return true;
        }

        private static int GetHierarchyDepth(Type t)
        {
            int depth = 0;

            Type? currentType = t;
            do
            {
                depth++;
                currentType = currentType.BaseType;
            } while (currentType != null);

            return depth;
        }

        internal static MethodBase? FindMostDerivedNewSlotMeth(MethodBase[] match, int cMatches)
        {
            int deepestHierarchy = 0;
            MethodBase? methWithDeepestHierarchy = null;

            for (int i = 0; i < cMatches; i++)
            {
                // Calculate the depth of the hierarchy of the declaring type of the
                // current method.
                int currentHierarchyDepth = GetHierarchyDepth(match[i].DeclaringType!);

                // The two methods have the same name, signature, and hierarchy depth.
                // This can only happen if at least one is vararg or generic.
                if (currentHierarchyDepth == deepestHierarchy)
                {
                    throw new AmbiguousMatchException();
                }

                // Check to see if this method is on the most derived class.
                if (currentHierarchyDepth > deepestHierarchy)
                {
                    deepestHierarchy = currentHierarchyDepth;
                    methWithDeepestHierarchy = match[i];
                }
            }

            return methWithDeepestHierarchy;
        }
    }
}
