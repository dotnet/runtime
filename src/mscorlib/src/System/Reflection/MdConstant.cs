// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection
{
    using System;

    internal static class MdConstant
    {
        [System.Security.SecurityCritical]  // auto-generated
        public static unsafe Object GetValue(MetadataImport scope, int token, RuntimeTypeHandle fieldTypeHandle, bool raw)
        {
            CorElementType corElementType = 0;
            long buffer = 0;
            int length;
            String stringVal;

            stringVal = scope.GetDefaultValue(token, out buffer, out length, out corElementType);

            RuntimeType fieldType = fieldTypeHandle.GetRuntimeType();

            if (fieldType.IsEnum && raw == false)
            {
                long defaultValue = 0;

                switch (corElementType)
                {
                    #region Switch

                    case CorElementType.Void:
                        return DBNull.Value;

                    case CorElementType.Char:
                        defaultValue = *(char*)&buffer;
                        break;

                    case CorElementType.I1:
                        defaultValue = *(sbyte*)&buffer;
                        break;

                    case CorElementType.U1:
                        defaultValue = *(byte*)&buffer;
                        break;

                    case CorElementType.I2:
                        defaultValue = *(short*)&buffer;
                        break;

                    case CorElementType.U2:
                        defaultValue = *(ushort*)&buffer;
                        break;

                    case CorElementType.I4:
                        defaultValue = *(int*)&buffer;
                        break;

                    case CorElementType.U4:
                        defaultValue = *(uint*)&buffer;
                        break;

                    case CorElementType.I8:
                        defaultValue = buffer;
                        break;

                    case CorElementType.U8:
                        defaultValue = buffer;
                        break;
                
                    default:
                        throw new FormatException(Environment.GetResourceString("Arg_BadLiteralFormat"));
                    #endregion
                }

                return RuntimeType.CreateEnum(fieldType, defaultValue);                       
            }
            else if (fieldType == typeof(DateTime))
            {
                long defaultValue = 0;

                switch (corElementType)
                {
                    #region Switch

                    case CorElementType.Void:
                        return DBNull.Value;
                        
                    case CorElementType.I8:
                        defaultValue = buffer;
                        break;

                    case CorElementType.U8:
                        defaultValue = buffer;
                        break;
                
                    default:
                        throw new FormatException(Environment.GetResourceString("Arg_BadLiteralFormat"));
                    #endregion  
                }

                return new DateTime(defaultValue);
            }
            else
            {
                switch (corElementType)
                {
                    #region Switch

                    case CorElementType.Void:
                        return DBNull.Value;

                    case CorElementType.Char:
                        return *(char*)&buffer;

                    case CorElementType.I1:
                        return *(sbyte*)&buffer;

                    case CorElementType.U1:
                        return *(byte*)&buffer;

                    case CorElementType.I2:
                        return *(short*)&buffer;

                    case CorElementType.U2:
                        return *(ushort*)&buffer;

                    case CorElementType.I4:
                        return *(int*)&buffer;

                    case CorElementType.U4:
                        return *(uint*)&buffer;

                    case CorElementType.I8:
                        return buffer;

                    case CorElementType.U8:
                        return (ulong)buffer;
                
                    case CorElementType.Boolean :
                        // The boolean value returned from the metadata engine is stored as a
                        // BOOL, which actually maps to an int. We need to read it out as an int
                        // to avoid problems on big-endian machines.
                        return (*(int*)&buffer != 0);

                    case CorElementType.R4 :
                        return *(float*)&buffer;

                    case CorElementType.R8:
                        return *(double*)&buffer;

                    case CorElementType.String:
                        // A string constant can be empty but never null.
                        // A nullref constant can only be type CorElementType.Class.
                        return stringVal == null ? String.Empty : stringVal;

                    case CorElementType.Class:
                        return null;
                    
                    default:
                        throw new FormatException(Environment.GetResourceString("Arg_BadLiteralFormat"));
                    #endregion
                }
            }
        } 
    }

}
