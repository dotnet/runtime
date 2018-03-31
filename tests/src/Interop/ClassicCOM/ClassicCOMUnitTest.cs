// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
//  Adding tests for Classic COM code coverage
//

using System;
using System.Text;
using System.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using TestLibrary;

public class ClassicCOMUnitTest
{
    /// <summary>
    /// Try to reflect load ComImport Types by enumerate
    /// </summary>
    /// <returns></returns>
    static bool RelectionLoad()
    {
        try
        {
            Console.WriteLine("Scenario: RelectionLoad");
            var asm = Assembly.LoadFrom("COMLib.dll");
            foreach (Type t in asm.GetTypes())
            {
                Console.WriteLine(t.Name);
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: " + e);
            return false;
        }
    }

    /// <summary>
    /// Try to test Type.IsCOMObject
    /// </summary>
    /// <returns></returns>
    static bool TypeIsComObject()
    {
        try
        {
            Console.WriteLine("Scenario: TypeIsComObject");
            Type classType = typeof(COMLib2.ContextMenu);
            if (!classType.IsCOMObject)
            {
                Console.WriteLine("ComImport Class's IsCOMObject should return true");
                return false;
            }

            Type interfaceType = typeof(COMLib2.IEnumVARIANT);
            if (interfaceType.IsCOMObject)
            {
                Console.WriteLine("ComImport interface's IsCOMObject should return false");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: " + e);
            return false;
        }
    }

    /// <summary>
    /// Try to create COM instance
    /// </summary>
    /// <returns></returns>
    static bool AcivateCOMType()
    {
        try
        {
            Console.WriteLine("Scenario: AcivateCOMType");
            COMLib2.ContextMenu contextMenu = (COMLib2.ContextMenu)Activator.CreateInstance(typeof(COMLib2.ContextMenu));
            
            // Linux should throw PlatformNotSupportedException
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }
            
            if (contextMenu == null)
            {
                Console.WriteLine("AcivateCOMType failed");
                return false;
            }

            return true;
        }
        catch (System.Reflection.TargetInvocationException e)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && e.InnerException is PlatformNotSupportedException)
            {
                return true;
            }
            
            Console.WriteLine("Caught unexpected PlatformNotSupportedException: " + e);
            return false;
        }
        catch(System.Runtime.InteropServices.COMException e)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true;
            }
            
            Console.WriteLine("Caught unexpected COMException: " + e);
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: " + e);
            return false;
        }
    }

    [DllImport("ClassicCOMNative.dll")]
    extern static void PassObjectToNative([In, MarshalAs( UnmanagedType.Interface)] object o);
    
    [DllImport("ClassicCOMNative.dll")]
    extern static void PassObjectArrayToNative([In,Out] object[] o);
    
    [DllImport("ClassicCOMNative.dll")]
    extern static void GetObjectFromNative(out object o);
    
    [DllImport("ClassicCOMNative.dll")]
    extern static void GetObjectFromNativeAsRef(ref object o);
    
    /// <summary>
    /// Try to Marshal COM Type across managed-native boundary
    /// </summary>
    /// <returns></returns>
    static bool MarshalCOMType()
    {
        Console.WriteLine("Scenario: MarshalCOMType");
        try
        {
            object o = new object();
            PassObjectToNative(o);
        }
        catch (System.Runtime.InteropServices.MarshalDirectiveException e) 
        { 
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            { 
                return true; 
            } 
            Console.WriteLine("Caught unexpected MarshalDirectiveException: " + e); 
            return false; 
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception in PassObjectToNative: " + e);
            return false;
        }
        
        try
        {
            object [] oa = new object[2];
            PassObjectArrayToNative(oa);
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception in GetObjectFromNative: " + e);
            return false;
        }
        
        
        try
        {
            object o; 
            GetObjectFromNative(out o);
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception in GetObjectFromNative: " + e);
            return false;
        }
        
        try
        {
            object o = new object(); 
            GetObjectFromNativeAsRef(ref o);
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception in GetObjectFromNativeAsRef: " + e);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Try to call Marshal API for COM Types
    /// </summary>
    /// <returns></returns>
    static bool MarshalAPI()
    {
        Console.WriteLine("Scenario: MarshalAPI");
        // MarshalAPI
        if (Marshal.AreComObjectsAvailableForCleanup())
        {
            Console.WriteLine("AreComObjectsAvailableForCleanup should return false");
            return false;
        }
        return true;
    }

    [System.Security.SecuritySafeCritical]
    static int Main()
    {
        int failures = 0;
        if (!RelectionLoad())
        {
            Console.WriteLine("RelectionLoad Failed");
            failures++;
        }

        if (!TypeIsComObject())
        {
            Console.WriteLine("TypeIsComObject Failed");
            failures++;
        }

        if (!AcivateCOMType())
        {
            Console.WriteLine("AcivateCOMType Failed");
            failures++;
        }

        if (!MarshalCOMType())
        {
            Console.WriteLine("MarshalCOMType Failed");
            failures++;
        }

        if (!MarshalAPI())
        {
            Console.WriteLine("MarshalAPI Failed");
            failures++;
        }

        return failures > 0 ? 101 : 100;
    }
}
