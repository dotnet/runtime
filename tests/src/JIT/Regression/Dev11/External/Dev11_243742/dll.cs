// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace BadOverride1.Dll
{
    public static class Apis
    {
        public static void RunDllCode()
        {
            Console.Write("In the DLL.\r\n");
            return;
        }
    }

    public class ParameterizedBase<TDerivedType>
    {
        public virtual void RunGenericMethod<T1>(T1 value)
        {
            Console.Write(
                "ParameterizedBase<{0}>.RunGenericMethod<{1}>({2})\r\n",
                typeof(TDerivedType),
                typeof(T1),
                value
            );

            return;
        }

        public virtual void RunGenericMethod<T1, T2>(T1 value)
        {
            Console.Write(
                "ParameterizedBase<{0}>.RunGenericMethod<{1},{2}>({3})\r\n",
                typeof(TDerivedType),
                typeof(T1),
                typeof(T2),
                value
            );

            return;
        }
    }
}
