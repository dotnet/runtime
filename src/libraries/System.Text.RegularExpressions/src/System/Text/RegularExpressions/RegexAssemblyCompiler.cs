// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

#if DEBUG // until it can be fully implemented
namespace System.Text.RegularExpressions
{
    /// <summary>Compiles a Regex to an assembly that can be saved to disk.</summary>
    internal sealed class RegexAssemblyCompiler : RegexCompiler
    {
        /// <summary>Type count used to augment generated type names to create unique names.</summary>
        private static int s_typeCount = 0;

        private AssemblyBuilder _assembly;
        private ModuleBuilder _module;
        private TypeBuilder? _type;
        private MethodBuilder? _method;

        internal RegexAssemblyCompiler(AssemblyName an, CustomAttributeBuilder[]? attribs, string? resourceFile) :
            base(persistsAssembly: true)
        {
            if (resourceFile != null)
            {
                // Unmanaged resources are not supported: _assembly.DefineUnmanagedResource(resourceFile);
                throw new PlatformNotSupportedException();
            }

            _assembly = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run); // TODO https://github.com/dotnet/corefx/issues/39227: AssemblyBuilderAccess.Save
            _module = _assembly.DefineDynamicModule(an.Name + ".dll");
            if (attribs != null)
            {
                foreach (CustomAttributeBuilder attr in attribs)
                {
                    _assembly.SetCustomAttribute(attr);
                }
            }
        }

        internal void GenerateRegexType(string pattern, RegexOptions options, string name, bool isPublic, RegexCode code, RegexTree tree, TimeSpan matchTimeout)
        {
            _code = code;
            _codes = code.Codes;
            _strings = code.Strings;
            _fcPrefix = code.FCPrefix;
            _bmPrefix = code.BMPrefix;
            _anchors = code.Anchors;
            _trackcount = code.TrackCount;
            _options = options;

            // Pick a name for the class.
            string typenumString = ((uint)Interlocked.Increment(ref s_typeCount)).ToString();

            // Generate the RegexRunner-derived type.
            DefineType($"{name}Runner{typenumString}", false, typeof(RegexRunner));

            DefineMethod("Go", null);
            GenerateGo();
            BakeMethod();

            DefineMethod("FindFirstChar", typeof(bool));
            GenerateFindFirstChar();
            BakeMethod();

            DefineMethod("InitTrackCount", null);
            GenerateInitTrackCount();
            BakeMethod();

            Type runnertype = BakeType();

            // Generate a RegexRunnerFactory-derived type.
            DefineType($"{name}Factory{typenumString}", false, typeof(RegexRunnerFactory));
            DefineMethod("CreateInstance", typeof(RegexRunner));
            GenerateCreateInstance(runnertype);
            BakeMethod();
            Type factory = BakeType();

            FieldInfo internalMatchTimeoutField = RegexField(nameof(Regex.internalMatchTimeout));
            ConstructorBuilder defaultCtor, timeoutCtor;

            DefineType(name, isPublic, typeof(Regex));
            {
                // Define default constructor:
                _method = null;
                defaultCtor = _type!.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                _ilg = defaultCtor.GetILGenerator();
                {
                    // call base constructor
                    Ldthis();
                    _ilg.Emit(OpCodes.Call, typeof(Regex).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, Array.Empty<ParameterModifier>())!);

                    // set pattern
                    Ldthis();
                    Ldstr(pattern);
                    Stfld(RegexField(nameof(Regex.pattern)));

                    // set options
                    Ldthis();
                    Ldc((int)options);
                    Stfld(RegexField(nameof(Regex.roptions)));

                    // set timeout (no need to validate as it should have happened in RegexCompilationInfo)
                    Ldthis();
                    LdcI8(matchTimeout.Ticks);
                    Call(typeof(TimeSpan).GetMethod(nameof(TimeSpan.FromTicks), BindingFlags.Static | BindingFlags.Public)!);
                    Stfld(internalMatchTimeoutField);

                    // set factory
                    Ldthis();
                    Newobj(factory.GetConstructor(Type.EmptyTypes)!);
                    Stfld(RegexField(nameof(Regex.factory)));

                    // set caps
                    if (code.Caps != null)
                    {
                        GenerateCreateHashtable(RegexField(nameof(Regex.caps)), code.Caps);
                    }

                    // set capnames
                    if (tree.CapNames != null)
                    {
                        GenerateCreateHashtable(RegexField(nameof(Regex.capnames)), tree.CapNames);
                    }

                    // set capslist
                    if (tree.CapsList != null)
                    {
                        Ldthis();
                        Ldc(tree.CapsList.Length);
                        _ilg.Emit(OpCodes.Newarr, typeof(string));  // create new string array
                        FieldInfo capslistField = RegexField(nameof(Regex.capslist));
                        Stfld(capslistField);
                        for (int i = 0; i < tree.CapsList.Length; i++)
                        {
                            Ldthisfld(capslistField);

                            Ldc(i);
                            Ldstr(tree.CapsList[i]);
                            _ilg.Emit(OpCodes.Stelem_Ref);
                        }
                    }

                    // set capsize
                    Ldthis();
                    Ldc(code.CapSize);
                    Stfld(RegexField(nameof(Regex.capsize)));

                    // set runnerref and replref by calling InitializeReferences()
                    Ldthis();
                    Call(typeof(Regex).GetMethod("InitializeReferences", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!);

                    Ret();
                }

                // Constructor with the timeout parameter:
                // We only generate a constructor with a timeout parameter if the regex information supplied has a non-infinite timeout.
                // If it has an infinite timeout, then the generated code is not going to respect the timeout.
                if (matchTimeout != Regex.InfiniteMatchTimeout)
                {
                    _method = null;
                    timeoutCtor = _type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(TimeSpan) });
                    _ilg = timeoutCtor.GetILGenerator();
                    {
                        // Call the default constructor:
                        Ldthis();
                        _ilg.Emit(OpCodes.Call, defaultCtor);

                        // Validate timeout:
                        _ilg.Emit(OpCodes.Ldarg_1);
                        Call(typeof(Regex).GetMethod(nameof(Regex.ValidateMatchTimeout), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!);

                        // Set timeout:
                        Ldthis();
                        _ilg.Emit(OpCodes.Ldarg_1);
                        Stfld(internalMatchTimeoutField);

                        Ret();
                    }
                }
            }

            // bake the type
            _type.CreateType();
            _type = null;
            _method = null;
            _ilg = null;
        }

        private void GenerateInitTrackCount()
        {
            Ldthis();
            Ldc(_trackcount);
            Stfld(s_runtrackcountField);
            Ret();
        }

        internal void GenerateCreateHashtable(FieldInfo field, Hashtable ht)
        {
            MethodInfo addMethod = typeof(Hashtable).GetMethod(nameof(Hashtable.Add), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

            Ldthis();
            Newobj(typeof(Hashtable).GetConstructor(Type.EmptyTypes)!);

            Stfld(field);

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

        private FieldInfo RegexField(string fieldname) =>
            typeof(Regex).GetField(fieldname, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;

        internal void Save()
        {
            // Save the assembly to the current directory.
            string fileName = _assembly.GetName().Name + ".dll";

            // TODO https://github.com/dotnet/corefx/issues/39227: _assembly.Save(fileName)
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_CompileToAssembly);
        }

        /// <summary>Generates a very simple factory method.</summary>
        internal void GenerateCreateInstance(Type newtype)
        {
            Newobj(newtype.GetConstructor(Type.EmptyTypes)!);
            Ret();
        }

        /// <summary>Begins the definition of a new type with a specified base class</summary>
        internal void DefineType(string typename, bool isPublic, Type inheritfromclass) =>
            _type = _module.DefineType(typename, TypeAttributes.Class | (isPublic ? TypeAttributes.Public : TypeAttributes.NotPublic), inheritfromclass);

        /// <summary>Begins the definition of a new method (no args) with a specified return value.</summary>
        internal void DefineMethod(string methname, Type? returntype)
        {
            _method = _type!.DefineMethod(methname, MethodAttributes.Public | MethodAttributes.Virtual, returntype, null);
            _ilg = _method.GetILGenerator();
        }

        /// <summary>Ends the definition of a method</summary>
        internal void BakeMethod() => _method = null;

        /// <summary>Ends the definition of a class and returns the type</summary>
        internal Type BakeType()
        {
            Type retval = _type!.CreateType()!;
            _type = null;
            return retval!;
        }
    }
}
#endif
