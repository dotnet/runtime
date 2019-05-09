// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    internal static class MdConstant
    {
        public static unsafe object? GetValue(MetadataImport scope, int token, RuntimeTypeHandle fieldTypeHandle, bool raw)
        {
            CorElementType corElementType = 0;
            long buffer = 0;
            int length;
            string? stringVal;

            stringVal = scope.GetDefaultValue(token, out buffer, out length, out corElementType);

            RuntimeType fieldType = fieldTypeHandle.GetRuntimeType();

            if (fieldType.IsEnum && raw == false)
            {
                // NOTE: Unlike in `TypeBuilder.SetConstantValue`, if `fieldType` describes
                // a nullable enum type `Nullable<TEnum>`, we do not unpack it to `TEnum` to
                // successfully enter this `if` clause. Default values of `TEnum?`-typed
                // parameters have been reported as values of the underlying type, changing
                // this now might be a breaking change.

                long defaultValue = 0;

                switch (corElementType)
                {
                    #region Switch

                    case CorElementType.ELEMENT_TYPE_VOID:
                        return DBNull.Value;

                    case CorElementType.ELEMENT_TYPE_CHAR:
                        defaultValue = *(char*)&buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_I1:
                        defaultValue = *(sbyte*)&buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_U1:
                        defaultValue = *(byte*)&buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_I2:
                        defaultValue = *(short*)&buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_U2:
                        defaultValue = *(ushort*)&buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_I4:
                        defaultValue = *(int*)&buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_U4:
                        defaultValue = *(uint*)&buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_I8:
                        defaultValue = buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_U8:
                        defaultValue = buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_CLASS:
                        return null;

                    default:
                        throw new FormatException(SR.Arg_BadLiteralFormat);
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

                    case CorElementType.ELEMENT_TYPE_VOID:
                        return DBNull.Value;

                    case CorElementType.ELEMENT_TYPE_I8:
                        defaultValue = buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_U8:
                        defaultValue = buffer;
                        break;

                    case CorElementType.ELEMENT_TYPE_CLASS:
                        return null;

                    default:
                        throw new FormatException(SR.Arg_BadLiteralFormat);
                        #endregion
                }

                return new DateTime(defaultValue);
            }
            else
            {
                switch (corElementType)
                {
                    #region Switch

                    case CorElementType.ELEMENT_TYPE_VOID:
                        return DBNull.Value;

                    case CorElementType.ELEMENT_TYPE_CHAR:
                        return *(char*)&buffer;

                    case CorElementType.ELEMENT_TYPE_I1:
                        return *(sbyte*)&buffer;

                    case CorElementType.ELEMENT_TYPE_U1:
                        return *(byte*)&buffer;

                    case CorElementType.ELEMENT_TYPE_I2:
                        return *(short*)&buffer;

                    case CorElementType.ELEMENT_TYPE_U2:
                        return *(ushort*)&buffer;

                    case CorElementType.ELEMENT_TYPE_I4:
                        return *(int*)&buffer;

                    case CorElementType.ELEMENT_TYPE_U4:
                        return *(uint*)&buffer;

                    case CorElementType.ELEMENT_TYPE_I8:
                        return buffer;

                    case CorElementType.ELEMENT_TYPE_U8:
                        return (ulong)buffer;

                    case CorElementType.ELEMENT_TYPE_BOOLEAN:
                        // The boolean value returned from the metadata engine is stored as a
                        // BOOL, which actually maps to an int. We need to read it out as an int
                        // to avoid problems on big-endian machines.
                        return (*(int*)&buffer != 0);

                    case CorElementType.ELEMENT_TYPE_R4:
                        return *(float*)&buffer;

                    case CorElementType.ELEMENT_TYPE_R8:
                        return *(double*)&buffer;

                    case CorElementType.ELEMENT_TYPE_STRING:
                        // A string constant can be empty but never null.
                        // A nullref constant can only be type CorElementType.ELEMENT_TYPE_CLASS.
                        return stringVal == null ? string.Empty : stringVal;

                    case CorElementType.ELEMENT_TYPE_CLASS:
                        return null;

                    default:
                        throw new FormatException(SR.Arg_BadLiteralFormat);
                        #endregion
                }
            }
        }
    }
}
