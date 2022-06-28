// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Collections.Generic;

namespace System.Reflection.Runtime.MethodInfos
{
    internal static partial class CustomMethodMapper
    {
        //
        // String constructors require special casing down the stack, being the only variable-sized objects created via a constructor.
        //
        private static class StringActions
        {
            public static Dictionary<MethodBase, CustomMethodInvokerAction> Map
            {
                get
                {
                    if (s_lazyMap == null)
                    {
                        Dictionary<MethodBase, CustomMethodInvokerAction> map = new Dictionary<MethodBase, CustomMethodInvokerAction>();

                        Type type = typeof(string);

                        unsafe
                        {
                            map.AddConstructor(type, new Type[] { typeof(char), typeof(int) },
                                (object thisObject, object[] args, Type thisType) =>
                                {
                                    return new string((char)(args[0]), (int)(args[1]));
                                }
                            );

                            map.AddConstructor(type, new Type[] { typeof(char[]) },
                                (object thisObject, object[] args, Type thisType) =>
                                {
                                    return new string((char[])(args[0]));
                                }
                            );

                            map.AddConstructor(type, new Type[] { typeof(char[]), typeof(int), typeof(int) },
                                (object thisObject, object[] args, Type thisType) =>
                                {
                                    return new string((char[])(args[0]), (int)(args[1]), (int)(args[2]));
                                }
                            );

                            map.AddConstructor(type, new Type[] { typeof(char*) },
                                (object thisObject, object[] args, Type thisType) =>
                                {
                                    return new string((char*)(IntPtr)(args[0]));
                                }
                            );

                            map.AddConstructor(type, new Type[] { typeof(char*), typeof(int), typeof(int) },
                                (object thisObject, object[] args, Type thisType) =>
                                {
                                    return new string((char*)(IntPtr)(args[0]), (int)(args[1]), (int)(args[2]));
                                }
                            );

                            map.AddConstructor(type, new Type[] { typeof(sbyte*) },
                                (object thisObject, object[] args, Type thisType) =>
                                {
                                    return new string((sbyte*)(IntPtr)(args[0]));
                                }
                            );

                            map.AddConstructor(type, new Type[] { typeof(sbyte*), typeof(int), typeof(int) },
                                (object thisObject, object[] args, Type thisType) =>
                                {
                                    return new string((sbyte*)(IntPtr)(args[0]), (int)(args[1]), (int)(args[2]));
                                }
                            );

                            map.AddConstructor(type, new Type[] { typeof(sbyte*), typeof(int), typeof(int), typeof(Encoding) },
                                (object thisObject, object[] args, Type thisType) =>
                                {
                                    return new string((sbyte*)(IntPtr)(args[0]), (int)(args[1]), (int)(args[2]), (Encoding)(args[3]));
                                }
                            );
                        }

                        s_lazyMap = map;
                    }

                    return s_lazyMap;
                }
            }
            private static volatile Dictionary<MethodBase, CustomMethodInvokerAction> s_lazyMap;
        }
    }
}
