// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Dynamic
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using TestLibrary;

    /*
       Types:
       - call method with type (in and return)
       - Set property
       - get property
       
       boolean
       string
       byte - I1
       short - I2
       LCID
       int - I4
       variant
       variant (optional)
       single - R4
       double - R8

       Enum
         - call method with ints instead of enum
       DateTime

       BStrWrapper
       CurrencyWrapper
       DispatchWrapper
       ErrorWrapper
       UnknownWrapper
       UnknownWrapper(None)

       Structs
       - call method returning struct, get properties on struct : GetPoint
       - call method with inout struct : TakePoint

       - call method returning variant (struct, Point) : GetVariantPoint
       - call method taking variant (struct, Point)

       - set object poperty to COM object
       - get property on object property

       VariantWrapper
       - take in wrapper, return variant : ByRefVariant

       Collections
       - method returns collection
       - get count
       - index by string
       - index by number
       - .Item[<stringIndex>]
       - .Item[<numberIndex>]
       - iterate over collection
       
       Array
       - method returns array
       - iterate over array
       - index by number
       - method takes ref array (inout)

       Exceptions
       - RaiseUserError
       - RaiseInternalError
       - call property as method

       GetVarType

       indexed default property??

       COM class that is a user-defined collection
       - Add
       - iterate
       - get enumerator
       - index

       SpecialVariantCases
       - by ref: Null, Empty, vbNullString, CVErr(50), emptyArray
         - object, VariantWrapper
    
       ByRef properties

       Named arguments
       - bad arg name
       - missing arg name
       - all named
       - mixed named and not named

       Optional arguments
       - specify all
       - omit all
       - named one optional

       Param array : ParamArrayArgs
     */

    class Program
    {
        static int Main(string[] doNotUse)
        {
            // RegFree COM is not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                new CollectionTest().Run();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
