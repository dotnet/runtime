// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool failed = false;

        {
            var map = typeof(Fooer).GetInterfaceMap(typeof(IFoo<Fooer>));

            int foundMatchMask = 0;

            MethodInfo ifooDefaultMethod = typeof(IFoo<Fooer>).GetMethod("DefaultMethod");
            MethodInfo ifooOtherMethod = typeof(IFoo<Fooer>).GetMethod("OtherMethod");
            MethodInfo ibarOtherMethod = typeof(IBar<Fooer>).GetMethod("OtherMethod");

            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                MethodInfo declMethod = map.InterfaceMethods[i];
                MethodInfo implMethod = map.TargetMethods[i];

                Console.Write("{0} ({1}) - {2} ({3}) - ", declMethod, declMethod.DeclaringType, implMethod, implMethod.DeclaringType);

                if (declMethod.Equals(ifooDefaultMethod))
                {
                    foundMatchMask |= 1;
                    CheckEqual(ref failed, implMethod, ifooDefaultMethod);
                }
                else if (declMethod.Equals(ifooOtherMethod))
                {
                    foundMatchMask |= 2;
                    CheckEqual(ref failed, implMethod, ibarOtherMethod);
                }
                else
                {
                    Console.WriteLine("UNEXPECTED");
                    failed = true;
                }
            }

            if (foundMatchMask != 3)
                return 10;
        }

        {
            var map = typeof(Fooer).GetInterfaceMap(typeof(IFoo));

            int foundMatchMask = 0;

            MethodInfo ifooDefaultMethod = typeof(IFoo).GetMethod("DefaultMethod");
            MethodInfo ifooOtherMethod = typeof(IFoo).GetMethod("OtherMethod");
            MethodInfo ibarOtherMethod = typeof(IBar).GetMethod("OtherMethod");

            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                MethodInfo declMethod = map.InterfaceMethods[i];
                MethodInfo implMethod = map.TargetMethods[i];

                Console.Write("{0} ({1}) - {2} ({3}) - ", declMethod, declMethod.DeclaringType, implMethod, implMethod.DeclaringType);

                if (declMethod.Equals(ifooDefaultMethod))
                {
                    foundMatchMask |= 1;
                    CheckEqual(ref failed, implMethod, ifooDefaultMethod);
                }
                else if (declMethod.Equals(ifooOtherMethod))
                {
                    foundMatchMask |= 2;
                    CheckEqual(ref failed, implMethod, ibarOtherMethod);
                }
                else
                {
                    Console.WriteLine("UNEXPECTED");
                    failed = true;
                }
            }

            if (foundMatchMask != 3)
                return 10;
        }


        {
            var map = typeof(Reabstractor).GetInterfaceMap(typeof(IFoo));

            int foundMatchMask = 0;

            MethodInfo ifooDefaultMethod = typeof(IFoo).GetMethod("DefaultMethod");
            MethodInfo ifooOtherMethod = typeof(IFoo).GetMethod("OtherMethod");

            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                MethodInfo declMethod = map.InterfaceMethods[i];
                MethodInfo implMethod = map.TargetMethods[i];

                Console.Write("{0} ({1}) - {2} ({3}) - ", declMethod, declMethod.DeclaringType, implMethod, implMethod?.DeclaringType);

                if (declMethod.Equals(ifooDefaultMethod))
                {
                    foundMatchMask |= 1;
                    CheckEqual(ref failed, implMethod, null);
                }
                else if (declMethod.Equals(ifooOtherMethod))
                {
                    foundMatchMask |= 2;
                    CheckEqual(ref failed, implMethod, null);
                }
                else
                {
                    Console.WriteLine("UNEXPECTED");
                    failed = true;
                }
            }

            if (foundMatchMask != 3)
                return 10;
        }

        {
            var map = typeof(Diamond).GetInterfaceMap(typeof(IFoo));

            int foundMatchMask = 0;

            MethodInfo ifooDefaultMethod = typeof(IFoo).GetMethod("DefaultMethod");
            MethodInfo ifooOtherMethod = typeof(IFoo).GetMethod("OtherMethod");

            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                MethodInfo declMethod = map.InterfaceMethods[i];
                MethodInfo implMethod = map.TargetMethods[i];

                Console.Write("{0} ({1}) - {2} ({3}) - ", declMethod, declMethod.DeclaringType, implMethod, implMethod?.DeclaringType);

                if (declMethod.Equals(ifooDefaultMethod))
                {
                    foundMatchMask |= 1;
                    CheckEqual(ref failed, implMethod, ifooDefaultMethod);
                }
                else if (declMethod.Equals(ifooOtherMethod))
                {
                    foundMatchMask |= 2;
                    CheckEqual(ref failed, implMethod, null);
                }
                else
                {
                    Console.WriteLine("UNEXPECTED");
                    failed = true;
                }
            }

            if (foundMatchMask != 3)
                return 10;
        }


        return failed ? -1 : 100;
    }

    static void CheckEqual(ref bool failed, MethodInfo method1, MethodInfo method2)
    {
        if (Object.Equals(method1, method2))
            Console.WriteLine("OK");
        else
        {
            Console.WriteLine("FAIL");
            failed = true;
        }
    }
}
