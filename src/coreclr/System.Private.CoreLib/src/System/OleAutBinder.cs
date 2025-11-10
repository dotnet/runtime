// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This class represents the Ole Automation binder.

using System.Runtime.InteropServices;
using Microsoft.Win32;
using CultureInfo = System.Globalization.CultureInfo;

namespace System
{
    internal sealed class OleAutBinder : DefaultBinder
    {
        // ChangeType
        // This binder uses OLEAUT to change the type of the variant.
        public override object ChangeType(object value, Type type, CultureInfo? cultureInfo)
        {
            cultureInfo ??= CultureInfo.CurrentCulture;

            if (type.IsByRef)
            {
                type = type.GetElementType()!;
            }

            // If we are trying to convert from an object to another type then we don't
            // need the OLEAUT change type, we can just use the normal CLR mechanisms.
            if (!type.IsPrimitive && type.IsInstanceOfType(value))
            {
                return value;
            }

            Type srcType = value.GetType();

            // Handle converting primitives to enums.
            if (type.IsEnum && srcType.IsPrimitive)
            {
                return Enum.Parse(type, value.ToString()!);
            }

            // Special case the conversion from DBNull.
            if (srcType == typeof(DBNull))
            {
                // The requested type is a DBNull so no conversion is required.
                if (type == typeof(DBNull))
                    return value;

                // The DBNull to null conversion is for compat with .NET Framework.
                // We don't allow this when converting to a value class, since null
                // doesn't make sense for these, or to object since this would change
                // existing semantics.
                if ((type.IsClass && type != typeof(object))
                    || type.IsInterface)
                {
                    return null!;
                }
            }

            // Use the OA variant lib to convert primitive types.
            try
            {
                return OAVariantLib.ChangeType(value, type, cultureInfo)!;
            }
            catch (NotSupportedException)
            {
                const int DISP_E_TYPEMISMATCH = unchecked((int)0x80020005);
                throw new COMException(SR.Interop_COM_TypeMismatch, DISP_E_TYPEMISMATCH);
            }
        }
    }
}
