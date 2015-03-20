// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Linq.Expressions.Compiler
{
    internal sealed class AssemblyGen
    {
        private static AssemblyGen s_assembly;

        private readonly AssemblyBuilder _myAssembly;
        private readonly ModuleBuilder _myModule;

        private int _index;

        private static AssemblyGen Assembly
        {
            get
            {
                if (s_assembly == null)
                {
                    Interlocked.CompareExchange(ref s_assembly, new AssemblyGen(), null);
                }
                return s_assembly;
            }
        }

        private AssemblyGen()
        {
            var name = new AssemblyName("Snippets");

            // mark the assembly transparent so that it works in partial trust:
            var attributes = new[] {
                new CustomAttributeBuilder(typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes), new object[0])
            };

            _myAssembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run, attributes);
            _myModule = _myAssembly.DefineDynamicModule(name.Name);
        }

        private TypeBuilder DefineType(string name, Type parent, TypeAttributes attr)
        {
            ContractUtils.RequiresNotNull(name, "name");
            ContractUtils.RequiresNotNull(parent, "parent");

            StringBuilder sb = new StringBuilder(name);

            int index = Interlocked.Increment(ref _index);
            sb.Append("$");
            sb.Append(index);

            // An unhandled Exception: System.Runtime.InteropServices.COMException (0x80131130): Record not found on lookup.
            // is thrown if there is any of the characters []*&+,\ in the type name and a method defined on the type is called.
            sb.Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('*', '_').Replace('&', '_').Replace(',', '_').Replace('\\', '_');

            name = sb.ToString();

            return _myModule.DefineType(name, attr, parent);
        }

        internal static TypeBuilder DefineDelegateType(string name)
        {
            return Assembly.DefineType(
                name,
                typeof(MulticastDelegate),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass
            );
        }
    }
}

