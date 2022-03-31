// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace JavaScript.MarshalerGenerator
{
    internal class MarshalerSelector
    {
        private JSMarshalerMetadata Void;
        private JSMarshalerMetadata Object;
        private JSMarshalerMetadata JSObject;
        private JSMarshalerMetadata Exception;
        private JSMarshalerMetadata Task;

        private Compilation Compilation;


        internal JSMarshalerSig GetArgumentSignature(StringBuilder prolog, JSMarshalerMetadata[] customMarshalers, ITypeSymbol argType)
        {
            foreach (var marshaler in marshalers)
            {
                if (marshaler.IsExactMatch(argType))
                {
                    return marshaler.ToSignature(false);
                };
            }

            if (customMarshalers != null) foreach (var custom in customMarshalers)
                {
                    if (custom.IsAssignableFrom(Compilation, argType))
                    {
                        return custom.ToSignature(!custom.IsExactMatch(argType));
                    }
                }

            if (JSObject.IsAssignableFrom(Compilation, argType)) return JSObject.ToSignature(!JSObject.IsExactMatch(argType));
            if (Exception.IsAssignableFrom(Compilation, argType)) return Exception.ToSignature(!Exception.IsExactMatch(argType));
            if (Task.IsAssignableFrom(Compilation, argType)) return Task.ToSignature(!Task.IsExactMatch(argType));

            // TODO check if it has MarshalAs or StructLayout
            // TODO test enums, ...
            // TODO test Nullable<struct>
            if (argType.IsValueType)
            {
                throw new NotSupportedException("TODO, struct is not supported: "+ argType.AsTypeSyntax());
            }

            // classes via System.Object reference
            return Object.ToSignature(!Object.IsExactMatch(argType));
        }

        private List<JSMarshalerMetadata> marshalers = new List<JSMarshalerMetadata>();
        private JSMarshalerMetadata AddMarshaler(JSMarshalerMetadata meta)
        {
            marshalers.Add(meta);
            return meta;
        }

        public MarshalerSelector(Compilation compilation)
        {
            Compilation = compilation;

            AddPrimitive(compilation);
            AddNullable(compilation);

            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_String),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedString",
                ToJsMethod = "MarshalStringToJs",
            });

            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_DateTime),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedDateTime",
                ToJsMethod = "MarshalDateTimeToJs",
            });

            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.DateTimeOffset"),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedDateTimeOffset",
                ToJsMethod = "MarshalDateTimeOffsetToJs",
            });

            Task = AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task"),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedTask",
                ToJsMethod = "MarshalTaskToJs",
                AfterToJsMethod = "AfterMarshalTaskToJs",
            });

            JSObject = AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".IJSObject"),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedIJSObject",
                ToJsMethod = "MarshalIJSObjectToJs",
            });

            Exception = AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Exception"),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedException",
                ToJsMethod = "MarshalExceptionToJs",
            });

            Object = AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Object),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedObject",
                ToJsMethod = "MarshalObjectToJs",
            });

            Void = AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Void),
                MarshalerType = null,
                ToManagedMethod = null,
                ToJsMethod = null,
            });
        }

        private void AddNullable(Compilation compilation) {
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_Boolean)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_Boolean)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_Byte)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_Byte)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_Int16)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_Int16)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_Int32)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_Int32)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_Int64)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_Int64)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_Single)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_Single)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_Double)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_Double)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_IntPtr)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_IntPtr)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetSpecialType(SpecialType.System_DateTime)),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetSpecialType(SpecialType.System_DateTime)),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(compilation.GetTypeByMetadataName("System.DateTimeOffset")),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptPublic + ".NullableMarshaler`1").Construct(compilation.GetTypeByMetadataName("System.DateTimeOffset")),
                ToManagedMethod = "MarshalToManaged",
                ToJsMethod = "MarshalToJs",
                IsAuto = true
            });
        }

        private void AddPrimitive(Compilation compilation)
        {
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Boolean),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedBoolean",
                ToJsMethod = "MarshalBooleanToJs",
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Byte),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedByte",
                ToJsMethod = "MarshalByteToJs",
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Int16),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedInt16",
                ToJsMethod = "MarshalInt16ToJs",
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Int32),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedInt32",
                ToJsMethod = "MarshalInt32ToJs",
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Int64),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedInt64",
                ToJsMethod = "MarshalInt64ToJs",
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Single),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedSingle",
                ToJsMethod = "MarshalSingleToJs",
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_Double),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedDouble",
                ToJsMethod = "MarshalDoubleToJs",
            });
            AddMarshaler(new JSMarshalerMetadata
            {
                MarshaledType = compilation.GetSpecialType(SpecialType.System_IntPtr),
                MarshalerType = compilation.GetTypeByMetadataName(Constants.JavaScriptMarshal),
                ToManagedMethod = "MarshalToManagedIntPtr",
                ToJsMethod = "MarshalIntPtrToJs",
            });
        }
    }
}
