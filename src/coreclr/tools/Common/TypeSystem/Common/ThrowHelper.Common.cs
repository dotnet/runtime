// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public static partial class ThrowHelper
    {
        [System.Diagnostics.DebuggerHidden]
        public static void ThrowTypeLoadException(string nestedTypeName, ModuleDesc module)
        {
            ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, nestedTypeName, Format.Module(module));
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowTypeLoadException(string @namespace, string name, ModuleDesc module)
        {
            ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, Format.Type(@namespace, name), Format.Module(module));
        }

        public static void ThrowTypeLoadException(string @namespace, string name, string moduleName)
        {
            ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, Format.Type(@namespace, name), moduleName);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowTypeLoadException(TypeDesc type)
        {
            ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, Format.Type(type), Format.OwningModule(type));
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowTypeLoadException(ExceptionStringID id, MethodDesc method)
        {
            ThrowTypeLoadException(id, Format.Type(method.OwningType), Format.OwningModule(method), Format.Method(method));
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowTypeLoadException(ExceptionStringID id, TypeDesc type, string messageArg)
        {
            ThrowTypeLoadException(id, Format.Type(type), Format.OwningModule(type), messageArg);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowTypeLoadException(ExceptionStringID id, TypeDesc type)
        {
            ThrowTypeLoadException(id, Format.Type(type), Format.OwningModule(type));
        }

        private static partial class Format
        {
            public static string OwningModule(MethodDesc method)
            {
                return OwningModule(method.OwningType);
            }

            public static string Module(ModuleDesc module)
            {
                if (module == null)
                    return "?";

                IAssemblyDesc assembly = module as IAssemblyDesc;
                if (assembly != null)
                {
                    return assembly.GetName().FullName;
                }
                else
                {
                    Debug.Fail("Multi-module assemblies");
                    return module.ToString();
                }
            }

            public static string Type(TypeDesc type)
            {
                return ExceptionTypeNameFormatter.Instance.FormatName(type);
            }

            public static string Type(string @namespace, string name)
            {
                return string.IsNullOrEmpty(@namespace) ? name : @namespace + "." + name;
            }

            public static string Field(TypeDesc owningType, string fieldName)
            {
                return Type(owningType) + "." + fieldName;
            }

            public static string Method(MethodDesc method)
            {
                return Method(method.OwningType, method.Name, method.Signature);
            }

            public static string Method(TypeDesc owningType, string methodName, MethodSignature signature)
            {
                StringBuilder sb = new StringBuilder();

                if (signature != null)
                {
                    sb.Append(ExceptionTypeNameFormatter.Instance.FormatName(signature.ReturnType));
                    sb.Append(' ');
                }

                sb.Append(ExceptionTypeNameFormatter.Instance.FormatName(owningType));
                sb.Append('.');
                sb.Append(methodName);

                if (signature != null)
                {
                    sb.Append('(');
                    for (int i = 0; i < signature.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(ExceptionTypeNameFormatter.Instance.FormatName(signature[i]));
                    }
                    sb.Append(')');
                }

                return sb.ToString();
            }
        }
    }
}
