// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace BadBox1
{


    public struct BytearrayHolder
    {
        public Byte[] m_value;
    }


    public class BoxedObjectHolder
    {
        public object m_boxedObject;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void SetBoxedObject(BytearrayHolder holder)
        {
            this.m_boxedObject = holder;
            return;
        }
    }


    internal static class App
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunScenario()
        {
            BytearrayHolder arrayHolder;
            BoxedObjectHolder boxedObjectHolder;

            arrayHolder.m_value = new Byte[10];
            boxedObjectHolder = new BoxedObjectHolder();
            boxedObjectHolder.SetBoxedObject(arrayHolder);

            return;
        }


        private static int Main()
        {
            try
            {
                App.RunScenario();
            }
            catch (Exception e)
            {
                Console.WriteLine("FAILED: Exception occurred ({0}).", e.GetType().ToString());
                return 101;
            }

            Console.WriteLine("PASSED: No exceptions occurred.");
            return 100;
        }
    }
}
