// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection.Runtime.General;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Runtime.MethodInfos
{
    internal static partial class CustomMethodMapper
    {
        //
        // Nullables are another edge case.
        //
        private static class NullableActions
        {
            public static Dictionary<MethodBase, CustomMethodInvokerAction> Map
            {
                get
                {
                    if (s_lazyMap == null)
                    {
                        Dictionary<MethodBase, CustomMethodInvokerAction> map = new Dictionary<MethodBase, CustomMethodInvokerAction>();

                        Type type = typeof(Nullable<>);
                        Type theT = type.GetGenericTypeParameters()[0];

                        map.AddMethod(type, nameof(Nullable<int>.ToString), Array.Empty<Type>(),
                            (object thisObject, object[] args, Type thisType) =>
                            {
                                return thisObject == null ? string.Empty : thisObject.ToString();
                            }
                        );

                        map.AddMethod(type, nameof(Nullable<int>.Equals), new Type[] { typeof(object) },
                            (object thisObject, object[] args, Type thisType) =>
                            {
                                object other = args[0];
                                if (thisObject == null)
                                    return other == null;
                                if (other == null)
                                    return false;
                                return thisObject.Equals(other);
                            }
                        );

                        map.AddMethod(type, nameof(Nullable<int>.GetHashCode), Array.Empty<Type>(),
                            (object thisObject, object[] args, Type thisType) =>
                            {
                                return thisObject == null ? 0 : thisObject.GetHashCode();
                            }
                        );

                        map.AddConstructor(type, new Type[] { theT },
                            (object thisObject, object[] args, Type thisType) =>
                            {
                                return args[0];
                            }
                         );

                        map.AddMethod(type, "get_" + nameof(Nullable<int>.HasValue), Array.Empty<Type>(),
                            (object thisObject, object[] args, Type thisType) =>
                            {
                                return thisObject != null;
                            }
                        );

                        map.AddMethod(type, "get_" + nameof(Nullable<int>.Value), Array.Empty<Type>(),
                            (object thisObject, object[] args, Type thisType) =>
                            {
                                if (thisObject == null)
                                    throw new InvalidOperationException(SR.InvalidOperation_NoValue);
                                return thisObject;
                            }
                        );

                        map.AddMethod(type, nameof(Nullable<int>.GetValueOrDefault), Array.Empty<Type>(), NullableGetValueOrDefault);

                        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:ParameterDoesntMeetParameterRequirements",
                            Justification = "Constructed MethodTable of a Nullable forces a constructed MethodTable of the element type")]
                        static object NullableGetValueOrDefault(object thisObject, object[] args,
                            Type thisType)
                        {
                            if (thisObject == null)
                                return RuntimeHelpers.GetUninitializedObject(thisType);

                            return thisObject;
                        }

                        map.AddMethod(type, nameof(Nullable<int>.GetValueOrDefault), new Type[] { theT },
                            (object thisObject, object[] args, Type thisType) =>
                            {
                                if (thisObject == null)
                                    return args[0];
                                return thisObject;
                            }
                        );

                        s_lazyMap = map;
                    }

                    return s_lazyMap;
                }
            }

            private static volatile Dictionary<MethodBase, CustomMethodInvokerAction> s_lazyMap;
        }
    }
}
