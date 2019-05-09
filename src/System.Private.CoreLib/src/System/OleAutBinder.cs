// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This class represents the Ole Automation binder.

// #define DISPLAY_DEBUG_INFO

using System.Runtime.InteropServices;
using Microsoft.Win32;
using CultureInfo = System.Globalization.CultureInfo;

namespace System
{
    // Made serializable in anticipation of this class eventually having state.
    internal class OleAutBinder : DefaultBinder
    {
        // ChangeType
        // This binder uses OLEAUT to change the type of the variant.
        public override object ChangeType(object value, Type type, CultureInfo? cultureInfo)
        {
            Variant myValue = new Variant(value);
            if (cultureInfo == null)
                cultureInfo = CultureInfo.CurrentCulture;

#if DISPLAY_DEBUG_INFO
            Console.WriteLine("In OleAutBinder::ChangeType converting variant of type: {0} to type: {1}", myValue.VariantType, type.Name);
#endif

            if (type.IsByRef)
            {
#if DISPLAY_DEBUG_INFO
                Console.WriteLine("Stripping byref from the type to convert to.");
#endif
                type = type.GetElementType()!;
            }

            // If we are trying to convert from an object to another type then we don't
            // need the OLEAUT change type, we can just use the normal COM+ mechanisms.
            if (!type.IsPrimitive && type.IsInstanceOfType(value))
            {
#if DISPLAY_DEBUG_INFO
                Console.WriteLine("Source variant can be assigned to destination type");
#endif
                return value;
            }

            Type srcType = value.GetType();

            // Handle converting primitives to enums.
            if (type.IsEnum && srcType.IsPrimitive)
            {
#if DISPLAY_DEBUG_INFO
                Console.WriteLine("Converting primitive to enum");
#endif
                return Enum.Parse(type, value.ToString()!);
            }

            // Use the OA variant lib to convert primitive types.
            try
            {
#if DISPLAY_DEBUG_INFO      
                Console.WriteLine("Using OAVariantLib.ChangeType() to do the conversion");
#endif      
                // Specify the LocalBool flag to have BOOL values converted to local language rather
                // than 0 or -1.
                object RetObj = OAVariantLib.ChangeType(myValue, type, OAVariantLib.LocalBool, cultureInfo).ToObject()!;

#if DISPLAY_DEBUG_INFO      
                Console.WriteLine("Object returned from ChangeType is of type: " + RetObj.GetType().Name);
#endif

                return RetObj;
            }
#if DISPLAY_DEBUG_INFO      
            catch(NotSupportedException e)
#else
            catch (NotSupportedException)
#endif      
            {
#if DISPLAY_DEBUG_INFO      
                Console.Write("Exception thrown: ");
                Console.WriteLine(e);
#endif      
                throw new COMException(SR.Interop_COM_TypeMismatch, unchecked((int)0x80020005));
            }
        }
    }
}
