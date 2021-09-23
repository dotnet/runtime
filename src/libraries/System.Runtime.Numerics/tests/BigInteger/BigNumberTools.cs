// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;

namespace BigNumberTools
{
    public class Utils
    {
        private static TypeInfo InternalNumber
        {
            get
            {
                if (s_lazyInternalNumber == null)
                {
                    Type t = typeof(BigInteger).Assembly.GetType("System.Numerics.BigNumber");
                    if (t != null)
                    {
                        s_lazyInternalNumber = t.GetTypeInfo();
                    }
                }
                return s_lazyInternalNumber;
            }
        }

        private static volatile TypeInfo s_lazyInternalNumber;

        public static void RunWithFakeThreshold(string name, int value, Action action)
        {
            TypeInfo internalNumber = InternalNumber;
            if (internalNumber == null)
                return; // Internal frame types are not reflectable on AoT platforms. Skip the test.

            FieldInfo field = internalNumber.GetDeclaredField(name);
            int lastValue = (int)field.GetValue(null);
            field.SetValue(null, value);
            try
            {
                action();
            }
            finally
            {
                field.SetValue(null, lastValue);
            }
        }
    }
}
