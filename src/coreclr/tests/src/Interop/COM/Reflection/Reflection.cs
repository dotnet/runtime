// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Text;
using System.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

public class Reflection
{
    /// <summary>
    /// Reflect load ComImport Types amd enumerate them
    /// </summary>
    static bool ReflectionLoad()
    {
        try
        {
            Console.WriteLine("Scenario: ReflectionLoad");
            var asm = Assembly.LoadFrom("NETServer.dll");
            foreach (Type t in asm.GetTypes())
            {
                Console.WriteLine(t.Name);
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Caught unexpected exception: {e}");
            return false;
        }
    }

    /// <summary>
    /// Type.IsCOMObject
    /// </summary>
    static bool TypeIsComObject()
    {
        try
        {
            Console.WriteLine("Scenario: TypeIsComObject");
            Type classType = typeof(NETServer.ContextMenu);
            if (!classType.IsCOMObject)
            {
                Console.WriteLine("ComImport Class's IsCOMObject should return true");
                return false;
            }

            Type interfaceType = typeof(NETServer.IEnumVARIANT);
            if (interfaceType.IsCOMObject)
            {
                Console.WriteLine("ComImport interface's IsCOMObject should return false");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Caught unexpected exception: {e}");
            return false;
        }
    }

    /// <summary>
    /// Create COM instance via Activator
    /// </summary>
    static bool ActivateCOMType()
    {
        try
        {
            Console.WriteLine("Scenario: ActivateCOMType");
            var contextMenu = (NETServer.ContextMenu)Activator.CreateInstance(typeof(NETServer.ContextMenu));

            // Non-Windows should throw PlatformNotSupportedException
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }
            
            if (contextMenu == null)
            {
                Console.WriteLine("ActivateCOMType failed");
                return false;
            }

            return true;
        }
        catch (TargetInvocationException e)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && e.InnerException is PlatformNotSupportedException)
            {
                return true;
            }
            
            Console.WriteLine($"Caught unexpected {nameof(PlatformNotSupportedException)}: {e}");
            return false;
        }
        catch(COMException e)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true;
            }
            
            Console.WriteLine($"Caught unexpected {nameof(COMException)}: {e}");
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Caught unexpected exception: {e}");
            return false;
        }
    }

    [System.Security.SecuritySafeCritical]
    static int Main()
    {
        int failures = 0;
        if (!ReflectionLoad())
        {
            Console.WriteLine("ReflectionLoad Failed");
            failures++;
        }

        if (!TypeIsComObject())
        {
            Console.WriteLine("TypeIsComObject Failed");
            failures++;
        }

        if (!ActivateCOMType())
        {
            Console.WriteLine("ActivateCOMType Failed");
            failures++;
        }

        return failures > 0 ? 101 : 100;
    }
}