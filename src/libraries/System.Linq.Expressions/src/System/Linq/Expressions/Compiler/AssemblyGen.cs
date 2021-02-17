// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace System.Linq.Expressions.Compiler
{
    internal sealed class AssemblyGen
    {
        private static AssemblyGen? s_assembly;

        private readonly ModuleBuilder _myModule;

        private int _index;

        private static AssemblyGen Assembly
        {
            get
            {
                if (s_assembly == null)
                {
                    Interlocked.CompareExchange(ref s_assembly, new AssemblyGen(), comparand: null);
                }
                return s_assembly;
            }
        }

        private AssemblyGen()
        {
            var name = new AssemblyName("Snippets");

            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            _myModule = myAssembly.DefineDynamicModule(name.Name!);
        }

        private TypeBuilder DefineType(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, TypeAttributes attr)
        {
            ContractUtils.RequiresNotNull(name, nameof(name));
            ContractUtils.RequiresNotNull(parent, nameof(parent));

            StringBuilder sb = new StringBuilder(name);

            int index = Interlocked.Increment(ref _index);
            sb.Append('$');
            sb.Append(index);

            // An unhandled Exception: System.Runtime.InteropServices.COMException (0x80131130): Record not found on lookup.
            // is thrown if there is any of the characters []*&+,\ in the type name and a method defined on the type is called.
            sb.Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('*', '_').Replace('&', '_').Replace(',', '_').Replace('\\', '_');

            name = sb.ToString();

            return _myModule.DefineType(name, attr, parent);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "MulticastDelegate has a ctor with RequiresUnreferencedCode, but the generated derived type doesn't reference this ctor, so this is trim compatible.")]
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
