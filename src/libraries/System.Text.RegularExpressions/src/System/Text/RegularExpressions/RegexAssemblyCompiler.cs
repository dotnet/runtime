// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

#if DEBUG // until it can be fully implemented
namespace System.Text.RegularExpressions
{
    /// <summary>Compiles a Regex to an assembly that can be saved to disk.</summary>
    internal sealed class RegexAssemblyCompiler : RegexCompiler
    {
        /// <summary>Type count used to augment generated type names to create unique names.</summary>
        private static int s_typeCount;

        private AssemblyBuilder _assembly;
        private ModuleBuilder _module;

        internal RegexAssemblyCompiler(AssemblyName an, CustomAttributeBuilder[]? attribs, string? resourceFile) :
            base(persistsAssembly: true)
        {
            if (resourceFile != null)
            {
                // Unmanaged resources are not supported: _assembly.DefineUnmanagedResource(resourceFile);
                throw new PlatformNotSupportedException();
            }

            _assembly = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run); // TODO https://github.com/dotnet/runtime/issues/30153: AssemblyBuilderAccess.Save
            _module = _assembly.DefineDynamicModule(an.Name + ".dll");
            if (attribs != null)
            {
                foreach (CustomAttributeBuilder attr in attribs)
                {
                    _assembly.SetCustomAttribute(attr);
                }
            }
        }

        internal void GenerateRegexType(string pattern, RegexOptions options, string name, bool isPublic, RegexCode code, TimeSpan matchTimeout)
        {
            // Store arguments into the base type's fields
            _options = options;
            _code = code;
            _codes = code.Codes;
            _strings = code.Strings;
            _leadingCharClasses = code.LeadingCharClasses;
            _boyerMoorePrefix = code.BoyerMoorePrefix;
            _leadingAnchor = code.LeadingAnchor;
            _trackcount = code.TrackCount;

            // Pick a name for the class.
            string typenumString = ((uint)Interlocked.Increment(ref s_typeCount)).ToString();

            // Generate the RegexRunner-derived type.
            TypeBuilder regexRunnerTypeBuilder = DefineType(_module, $"{name}Runner{typenumString}", isPublic: false, isSealed: true, typeof(RegexRunner));
            _ilg = DefineMethod(regexRunnerTypeBuilder, "Go", null);
            GenerateGo();
            _ilg = DefineMethod(regexRunnerTypeBuilder, "FindFirstChar", typeof(bool));
            GenerateFindFirstChar();
            _ilg = DefineMethod(regexRunnerTypeBuilder, "InitTrackCount", null);
            GenerateInitTrackCount();
            Type runnerType = regexRunnerTypeBuilder.CreateType()!;

            // Generate the RegexRunnerFactory-derived type.
            TypeBuilder regexRunnerFactoryTypeBuilder = DefineType(_module, $"{name}Factory{typenumString}", isPublic: false, isSealed: true, typeof(RegexRunnerFactory));
            _ilg = DefineMethod(regexRunnerFactoryTypeBuilder, "CreateInstance", typeof(RegexRunner));
            GenerateCreateInstance(runnerType);
            Type regexRunnerFactoryType = regexRunnerFactoryTypeBuilder.CreateType()!;

            // Generate the Regex-derived type.
            TypeBuilder regexTypeBuilder = DefineType(_module, name, isPublic, isSealed: false, typeof(Regex));
            ConstructorBuilder defaultCtorBuilder = regexTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            _ilg = defaultCtorBuilder.GetILGenerator();
            GenerateRegexDefaultCtor(pattern, options, regexRunnerFactoryType, code, matchTimeout);
            if (matchTimeout != Regex.InfiniteMatchTimeout)
            {
                // We only generate a constructor with a timeout parameter if the regex information supplied has a non-infinite timeout.
                // If it has an infinite timeout, then the generated code is not going to respect the timeout. This is a difference from netfx,
                // due to the fact that we now special-case an infinite timeout in the code generator to avoid spitting unnecessary code
                // and paying for the checks at run time.
                _ilg = regexTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(TimeSpan) }).GetILGenerator();
                GenerateRegexTimeoutCtor(defaultCtorBuilder, regexTypeBuilder);
            }
            regexTypeBuilder.CreateType();
        }

        private void GenerateInitTrackCount()
        {
            // this.runtrackcount = _trackcount;
            // return;
            Ldthis();
            Ldc(_trackcount);
            Stfld(s_runtrackcountField);
            Ret();
        }

        /// <summary>Generates a very simple factory method.</summary>
        private void GenerateCreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
        {
            // return new Type();
            Newobj(type.GetConstructor(Type.EmptyTypes)!);
            Ret();
        }

        private void GenerateRegexDefaultCtor(
            string pattern,
            RegexOptions options,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type regexRunnerFactoryType,
            RegexCode code,
            TimeSpan matchTimeout)
        {
            // Call the base ctor and store pattern, options, and factory.
            // base.ctor();
            // base.pattern = pattern;
            // base.options = options;
            // base.factory = new DerivedRegexRunnerFactory();
            Ldthis();
            _ilg!.Emit(OpCodes.Call, typeof(Regex).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, Array.Empty<ParameterModifier>())!);
            Ldthis();
            Ldstr(pattern);
            Stfld(RegexField(nameof(Regex.pattern)));
            Ldthis();
            Ldc((int)options);
            Stfld(RegexField(nameof(Regex.roptions)));
            Ldthis();
            Newobj(regexRunnerFactoryType.GetConstructor(Type.EmptyTypes)!);
            Stfld(RegexField(nameof(Regex.factory)));

            // Store the timeout (no need to validate as it should have happened in RegexCompilationInfo)
            if (matchTimeout == Regex.InfiniteMatchTimeout)
            {
                // base.internalMatchTimeout = Regex.InfiniteMatchTimeout;
                _ilg.Emit(OpCodes.Ldsfld, RegexField(nameof(Regex.InfiniteMatchTimeout)));
            }
            else
            {
                // base.internalMatchTimeout = TimeSpan.FromTick(matchTimeout.Ticks);
                Ldthis();
                LdcI8(matchTimeout.Ticks);
                Call(typeof(TimeSpan).GetMethod(nameof(TimeSpan.FromTicks), BindingFlags.Public | BindingFlags.Static)!);
            }
            Stfld(RegexField(nameof(Regex.internalMatchTimeout)));

            // Set capsize, caps, capnames, capslist.
            Ldthis();
            Ldc(code.CapSize);
            Stfld(RegexField(nameof(Regex.capsize)));
            if (code.Caps != null)
            {
                // Caps = new Hashtable {{0, 0}, {1, 1}, ... };
                GenerateCreateHashtable(RegexField(nameof(Regex.caps)), code.Caps);
            }
            if (code.Tree.CapNames != null)
            {
                // CapNames = new Hashtable {{"0", 0}, {"1", 1}, ...};
                GenerateCreateHashtable(RegexField(nameof(Regex.capnames)), code.Tree.CapNames);
            }
            if (code.Tree.CapsList != null)
            {
                // capslist = new string[...];
                // capslist[0] = "0";
                // capslist[1] = "1";
                // ...
                Ldthis();
                Ldc(code.Tree.CapsList.Length);
                _ilg.Emit(OpCodes.Newarr, typeof(string));  // create new string array
                FieldInfo capslistField = RegexField(nameof(Regex.capslist));
                Stfld(capslistField);
                for (int i = 0; i < code.Tree.CapsList.Length; i++)
                {
                    Ldthisfld(capslistField);
                    Ldc(i);
                    Ldstr(code.Tree.CapsList[i]);
                    _ilg.Emit(OpCodes.Stelem_Ref);
                }
            }

            // InitializeReferences();
            // return;
            Ldthis();
            Call(typeof(Regex).GetMethod("InitializeReferences", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!);
            Ret();
        }

        private void GenerateRegexTimeoutCtor(ConstructorBuilder defaultCtorBuilder, TypeBuilder regexTypeBuilder)
        {
            // base.ctor();
            // ValidateMatchTimeout(timeSpan);
            // base.internalMatchTimeout = timeSpan;
            Ldthis();
            _ilg!.Emit(OpCodes.Call, defaultCtorBuilder);
            _ilg.Emit(OpCodes.Ldarg_1);
            Call(typeof(Regex).GetMethod(nameof(Regex.ValidateMatchTimeout), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!);
            Ldthis();
            _ilg.Emit(OpCodes.Ldarg_1);
            Stfld(RegexField(nameof(Regex.internalMatchTimeout)));
            Ret();
        }

        internal void GenerateCreateHashtable(FieldInfo field, Hashtable ht)
        {
            // hashtable = new Hashtable();
            Ldthis();
            Newobj(typeof(Hashtable).GetConstructor(Type.EmptyTypes)!);
            Stfld(field);

            // hashtable.Add(key1, value1);
            // hashtable.Add(key2, value2);
            // ...
            MethodInfo addMethod = typeof(Hashtable).GetMethod(nameof(Hashtable.Add), BindingFlags.Public | BindingFlags.Instance)!;
            IDictionaryEnumerator en = ht.GetEnumerator();
            while (en.MoveNext())
            {
                Ldthisfld(field);

                if (en.Key is int key)
                {
                    Ldc(key);
                    _ilg!.Emit(OpCodes.Box, typeof(int));
                }
                else
                {
                    Ldstr((string)en.Key);
                }

                Ldc((int)en.Value!);
                _ilg!.Emit(OpCodes.Box, typeof(int));
                Callvirt(addMethod);
            }
        }

        /// <summary>Gets the named instance field from the Regex type.</summary>
        private static FieldInfo RegexField(string fieldname) =>
            typeof(Regex).GetField(fieldname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;

        /// <summary>Saves the assembly to a file in the current directory based on the assembly's name.</summary>
        internal void Save()
        {
            // Save the assembly to the current directory.
            string fileName = _assembly.GetName().Name + ".dll";

            // TODO https://github.com/dotnet/runtime/issues/30153: _assembly.Save(fileName)
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
        }

        /// <summary>Begins the definition of a new type with a specified base class</summary>
        private static TypeBuilder DefineType(
            ModuleBuilder moduleBuilder,
            string typeName,
            bool isPublic,
            bool isSealed,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type inheritFromClass)
        {
            TypeAttributes attrs = TypeAttributes.Class | TypeAttributes.BeforeFieldInit | (isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic);
            if (isSealed)
            {
                attrs |= TypeAttributes.Sealed;
            }

            return moduleBuilder.DefineType(typeName, attrs, inheritFromClass);
        }

        /// <summary>Begins the definition of a new method (no args) with a specified return value.</summary>
        private static ILGenerator DefineMethod(TypeBuilder typeBuilder, string methname, Type? returnType) =>
            typeBuilder.DefineMethod(methname, MethodAttributes.Family | MethodAttributes.Virtual, returnType, null).GetILGenerator();
    }
}
#endif
