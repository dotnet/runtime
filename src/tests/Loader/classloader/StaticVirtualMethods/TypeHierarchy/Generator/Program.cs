// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace VirtualStaticInterfaceMethodTestGen
{
    class Program
    {
        public enum InterfaceImplementationApproach
        {
            OnBaseType,
            OnBothBaseAndDerived,
            OnBothBaseAndDerivedBaseIsAbstract
        }

        public struct TestScenario
        {
            public TestScenario(int scenarioIndex,
                string InterfaceReturnType,
            string InterfaceTypeGenericParams,
            string BaseTypeGenericParams,
            string BaseTypeReturnType,
            string InterfaceTypeInstantiationOnBaseType,
            string DerivedTypeGenericParams,
            string DerivedTypeReturnType,
            string InterfaceTypeInstantiationOnDerivedType,
            string BaseTypeInstantiationOnDerivedType,
            string DerivedTypeInstantiation,
            string CallReturnType,
            string CallInterfaceTypeInstantiation,
            InterfaceImplementationApproach InterfaceImplementationApproach
                )
            {
                ScenarioName = $"Scenario{scenarioIndex}";

                this.InterfaceReturnType = InterfaceReturnType;
                this.InterfaceTypeGenericParams = InterfaceTypeGenericParams;
                this.BaseTypeGenericParams = BaseTypeGenericParams;
                this.BaseTypeReturnType = BaseTypeReturnType;
                this.InterfaceTypeInstantiationOnBaseType = InterfaceTypeInstantiationOnBaseType;
                this.DerivedTypeGenericParams = DerivedTypeGenericParams;
                this.DerivedTypeReturnType = DerivedTypeReturnType;
                this.InterfaceTypeInstantiationOnDerivedType = InterfaceTypeInstantiationOnDerivedType;
                this.BaseTypeInstantiationOnDerivedType = BaseTypeInstantiationOnDerivedType;
                this.DerivedTypeInstantiation = DerivedTypeInstantiation;
                this.CallReturnType = CallReturnType;
                this.CallInterfaceTypeInstantiation = CallInterfaceTypeInstantiation;
                this.InterfaceImplementationApproach = InterfaceImplementationApproach;
            }
            public readonly string ScenarioName;
            public readonly string InterfaceReturnType;
            public readonly string InterfaceTypeGenericParams;
            public readonly string BaseTypeGenericParams;
            public readonly string BaseTypeReturnType;
            public readonly string InterfaceTypeInstantiationOnBaseType;
            public readonly string DerivedTypeGenericParams;
            public readonly string DerivedTypeReturnType;
            public readonly string InterfaceTypeInstantiationOnDerivedType;
            public readonly string BaseTypeInstantiationOnDerivedType;
            public readonly string DerivedTypeInstantiation;
            public readonly string CallReturnType;
            public readonly string CallInterfaceTypeInstantiation;
            public readonly InterfaceImplementationApproach InterfaceImplementationApproach;

            public override string ToString() => ScenarioName;
            public static IEnumerable<TestScenario> GetScenarios()
            {
                int scenarioIndex = 1;
                int covariantScenarios = 0;
                // Scenario
                // InterfaceReturnType, InterfaceTypeGenericParams, BaseType, BaseTypeGenericParams, BaseTypeReturnType, DerivedType, DerivedTypeGenericParams, DerivedTypeReturnType, DerivedTypeInstantiation, ExactDispatchType, MethodImplOnDerivedType
                foreach (string interfaceTypeGenericParams in new string[] { "", "<U>" })
                {
                    List<string> possibleInterfaceReturnTypes = new List<string>();
                    possibleInterfaceReturnTypes.Add("int32");
                    possibleInterfaceReturnTypes.Add("object");
                    if (interfaceTypeGenericParams == "<U>")
                    {
                        possibleInterfaceReturnTypes.Add("!0");
                    }

                    foreach (string interfaceReturnType in possibleInterfaceReturnTypes)
                    {
                        foreach (string baseTypeGenericParams in new string[] { "", "<T>" })
                        {
                            List<string> possibleInterfaceTypeInstantiationOnBaseType = new List<string>();
                            if (interfaceTypeGenericParams == "")
                            {
                                possibleInterfaceTypeInstantiationOnBaseType.Add("");
                            }
                            else
                            {
                                possibleInterfaceTypeInstantiationOnBaseType.Add("<object>");
                                if (baseTypeGenericParams == "<T>")
                                {
                                    possibleInterfaceTypeInstantiationOnBaseType.Add("<!0>");
                                    possibleInterfaceTypeInstantiationOnBaseType.Add("<class [System.Runtime]System.Action`1<!0>>");
                                }
                            }
                            foreach (string interfaceTypeInstantiationOnBaseType in possibleInterfaceTypeInstantiationOnBaseType)
                            {
                                List<string> possibleBaseTypeReturnTypes = new List<string>();
                                if (!interfaceReturnType.Contains("!0"))
                                {
                                    possibleBaseTypeReturnTypes.Add(interfaceReturnType);
                                    if (interfaceReturnType == "object")
                                    {
                                        possibleBaseTypeReturnTypes.Add("string"); // Covariant return testing
                                    }
                                }
                                else
                                {
                                    possibleBaseTypeReturnTypes.Add(ApplyGenericSubstitution(interfaceReturnType, interfaceTypeInstantiationOnBaseType));
                                }

                                foreach (string baseTypeReturnType in possibleBaseTypeReturnTypes)
                                {
                                    foreach (string derivedTypeGenericParams in new string[] { "", "<V>" })
                                    {
                                        List<string> possibleBaseTypeInstantiationOnDerivedType = new List<string>();
                                        if (baseTypeGenericParams == "")
                                        {
                                            possibleBaseTypeInstantiationOnDerivedType.Add("");
                                        }
                                        else
                                        {
                                            possibleBaseTypeInstantiationOnDerivedType.Add("<string>");
                                            if (derivedTypeGenericParams == "<V>")
                                            {
                                                possibleBaseTypeInstantiationOnDerivedType.Add("<!0>");
                                                possibleBaseTypeInstantiationOnDerivedType.Add("<class [System.Runtime]System.Func`1<!0>>");
                                            }
                                        }

                                        foreach (string baseTypeInstantiationOnDerivedType in possibleBaseTypeInstantiationOnDerivedType)
                                        {
                                            string interfaceTypeInstantiationOnDerivedType = ApplyGenericSubstitution(interfaceTypeInstantiationOnBaseType, baseTypeInstantiationOnDerivedType);
                                            string derivedTypeReturnType = ApplyGenericSubstitution(interfaceReturnType, interfaceTypeInstantiationOnDerivedType);

                                            List<string> possibleDerivedTypeInstantiation = new List<string>();
                                            if (derivedTypeGenericParams == "")
                                            {
                                                possibleDerivedTypeInstantiation.Add("");
                                            }
                                            else
                                            {
                                                possibleDerivedTypeInstantiation.Add("<string>");
                                                possibleDerivedTypeInstantiation.Add("<int32>");
                                            }

                                            foreach (string derivedTypeInstantiation in possibleDerivedTypeInstantiation)
                                            {
                                                string callReturnType = interfaceReturnType;
                                                string callInterfaceTypeInstantiation = ApplyGenericSubstitution(interfaceTypeInstantiationOnDerivedType, derivedTypeInstantiation);

                                                foreach (var interfaceImplementationApproach in (InterfaceImplementationApproach[])Enum.GetValues(typeof(InterfaceImplementationApproach)))
                                                {
                                                    if (baseTypeReturnType == "string")
                                                    {
                                                        covariantScenarios++;
                                                        // We decided covariant scenarios aren't supported
                                                        continue;
                                                    }
                                                    yield return new TestScenario(scenarioIndex++,
                                                                                  interfaceReturnType,
                                                                                  interfaceTypeGenericParams,
                                                                                  baseTypeGenericParams,
                                                                                  baseTypeReturnType,
                                                                                  interfaceTypeInstantiationOnBaseType,
                                                                                  derivedTypeGenericParams,
                                                                                  derivedTypeReturnType,
                                                                                  interfaceTypeInstantiationOnDerivedType,
                                                                                  baseTypeInstantiationOnDerivedType,
                                                                                  derivedTypeInstantiation,
                                                                                  callReturnType,
                                                                                  callInterfaceTypeInstantiation,
                                                                                  interfaceImplementationApproach);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                string ApplyGenericSubstitution(string instantiation, string substitution)
                {
                    if (instantiation == null)
                        return instantiation;

                    if (instantiation.Contains("!0"))
                    {
                        return instantiation.Replace("!0", StripGenericInstantiation(substitution));
                    }
                    return instantiation;
                    string StripGenericInstantiation(string input)
                    {
                        Debug.Assert(input[0] == '<');
                        Debug.Assert(input[input.Length - 1] == '>');
                        return input.Substring(1, input.Length - 2);
                    }
                }
            }
        }

        static void EmitTestGlobalHeader(TextWriter tw)
        {
            tw.WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
            tw.WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
            tw.WriteLine("");
            tw.WriteLine("// THIS FILE IS AUTOGENERATED EDIT Generator/Program.cs instead and rerun the generator");
            tw.WriteLine(".assembly extern System.Console {}");
            tw.WriteLine(".assembly extern mscorlib {}");
            tw.WriteLine(".assembly extern System.Runtime {}");
            tw.WriteLine(".assembly extern TypeHierarchyCommonCs {}");
        }

        static void EmitAssemblyExternRecord(TextWriter tw, string assemblyName)
        {
            tw.WriteLine($".assembly extern {assemblyName} {{}}");
        }
        static void EmitAssemblyRecord(TextWriter tw, string assemblyName)
        {
            tw.WriteLine($".assembly {assemblyName} {{}}");
        }

        public struct ClassDesc
        {
            public string BaseType;
            public string ClassFlags;
            public string GenericParams;
            public string Name;
            public IEnumerable<string> InterfacesImplemented;
        }

        static void EmitClass(TextWriter tw, ClassDesc clz)
        {
            string genericParamString = "";
            if (!String.IsNullOrEmpty(clz.GenericParams))
                genericParamString = clz.GenericParams;
            tw.WriteLine($".class {clz.ClassFlags} {clz.Name}{genericParamString}");
            if (clz.BaseType != null)
            {
                tw.WriteLine($"       extends {clz.BaseType}");
            }

            if (clz.InterfacesImplemented != null)
            {
                bool first = true;
                foreach (string iface in clz.InterfacesImplemented)
                {
                    if (first)
                    {
                        first = false;
                        tw.Write("       implements ");
                    }
                    else
                    {
                        tw.Write("," + Environment.NewLine + "                  ");
                    }
                    tw.Write(iface);
                }

                if (first == true)
                {
                    throw new Exception();
                }
                tw.WriteLine("");
            }
            tw.WriteLine("{");
        }

        static void EmitEndClass(TextWriter tw, ClassDesc clz)
        {
            tw.WriteLine($"}} // end of class {clz.Name}");
        }

        public struct MethodDesc
        {
            public string Name;
            public string Arguments;
            public string ReturnType;
            public bool HasBody;
            public IEnumerable<string> MethodImpls;
            public string MethodFlags;
        }

        static void EmitMethod(TextWriter tw, MethodDesc md)
        {
            tw.WriteLine($"  .method { md.MethodFlags} {md.ReturnType} {md.Name}({md.Arguments}) cil managed noinlining");
            tw.WriteLine("  {");
            if (md.MethodImpls != null)
            {
                foreach (var methodImpl in md.MethodImpls)
                {
                    tw.WriteLine($"    .override {methodImpl}");
                }
            }
        }

        static void EmitEndMethod(TextWriter tw, MethodDesc md)
        {
            tw.WriteLine($"  }} // end of method {md.Name}");
        }

        static string CommonCsAssemblyName = "TypeHierarchyCommonCs";
        static string TestAssemblyName = "TypeHierarchyTest";
        static string CommonCsPrefix = $"[{CommonCsAssemblyName}]";

        static string ToILDasmTypeName(string typeName, string instantiation)
        {
            if (instantiation != "")
            {
                return $"class {typeName}{instantiation}";
            }
            else
            {
                return typeName;
            }
        }
        static void Main(string[] args)
        {
            int maxCases = Int32.MaxValue;
            string rootPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            string scenarioSuffix = "";
            if (args.Length > 0)
                rootPath = args[0];
            if (args.Length > 2)
            {
                maxCases = Int32.Parse(args[1]);
                scenarioSuffix = args[2];
            }
            using StreamWriter twOutputTest = new StreamWriter(Path.Combine(rootPath, @$"{TestAssemblyName}{scenarioSuffix}.il"));

            StringWriter swMainMethodBody = new StringWriter();
            StringWriter swTestClassMethods = new StringWriter();

            EmitTestGlobalHeader(twOutputTest);
            EmitAssemblyRecord(twOutputTest, TestAssemblyName);

            int currentCase = 0;
            foreach (var scenario in TestScenario.GetScenarios())
            {
                if ((++currentCase) > maxCases)
                    break;
                string scenarioName = scenario.ToString();

                // Emit interface
                ClassDesc iface = new ClassDesc();
                iface.ClassFlags = "interface public abstract auto ansi";
                iface.GenericParams = scenario.InterfaceTypeGenericParams;
                iface.Name = "Interface" + scenarioName + GenericTypeSuffix(scenario.InterfaceTypeGenericParams); ;

                EmitClass(twOutputTest, iface);
                MethodDesc ifaceMethod = new MethodDesc();
                ifaceMethod.HasBody = false;
                ifaceMethod.MethodFlags = "public newslot virtual abstract static";
                ifaceMethod.ReturnType = scenario.InterfaceReturnType;
                ifaceMethod.Name = "Method";

                EmitMethod(twOutputTest, ifaceMethod);
                EmitEndMethod(twOutputTest, ifaceMethod);
                EmitEndClass(twOutputTest, iface);

                // Emit base class which implements static method to implement interface. Mark it abstract if we don't put the methodimpl there
                ClassDesc baseType = new ClassDesc();
                baseType.BaseType = "[System.Runtime]System.Object";
                switch (scenario.InterfaceImplementationApproach)
                {
                    case InterfaceImplementationApproach.OnBaseType:
                    case InterfaceImplementationApproach.OnBothBaseAndDerived:
                        baseType.ClassFlags = "public auto ansi";
                        break;

                    case InterfaceImplementationApproach.OnBothBaseAndDerivedBaseIsAbstract:
                        baseType.ClassFlags = "public abstract auto ansi";
                        break;

                    default:
                        throw new Exception("Unknown interface approach");
                }
                baseType.GenericParams = scenario.BaseTypeGenericParams;
                baseType.Name = "Base" + scenarioName + GenericTypeSuffix(scenario.BaseTypeGenericParams);
                if (scenario.InterfaceImplementationApproach.ToString().Contains("Base"))
                {
                    baseType.InterfacesImplemented = new string[] { ToILDasmTypeName(iface.Name, scenario.InterfaceTypeInstantiationOnBaseType) };
                }
                EmitClass(twOutputTest, baseType);
                switch (scenario.InterfaceImplementationApproach)
                {
                    case InterfaceImplementationApproach.OnBaseType:
                    case InterfaceImplementationApproach.OnBothBaseAndDerived:
                        MethodDesc ifaceImplMethod = new MethodDesc();
                        ifaceImplMethod.HasBody = true;
                        ifaceImplMethod.MethodFlags = "public static";
                        ifaceImplMethod.ReturnType = scenario.BaseTypeReturnType;
                        ifaceImplMethod.Name = "Method";
                        EmitMethod(twOutputTest, ifaceImplMethod);
                        twOutputTest.WriteLine($"    .override method {scenario.InterfaceReturnType} {ToILDasmTypeName(iface.Name, scenario.InterfaceTypeInstantiationOnBaseType)}::Method()");
                        twOutputTest.WriteLine($"    .locals init ({scenario.BaseTypeReturnType} V_O)");
                        twOutputTest.WriteLine($"    ldloca.s 0");
                        twOutputTest.WriteLine($"    initobj {scenario.BaseTypeReturnType}");
                        twOutputTest.WriteLine($"    ldloc.0");
                        twOutputTest.WriteLine($"    ret");
                        EmitEndMethod(twOutputTest, ifaceImplMethod);
                        break;

                    case InterfaceImplementationApproach.OnBothBaseAndDerivedBaseIsAbstract:
                        break;

                    default:
                        throw new Exception("Unknown interface approach");
                }
                EmitEndClass(twOutputTest, baseType);

                // Emit derived class.
                ClassDesc derivedType = new ClassDesc();
                derivedType.BaseType = ToILDasmTypeName(baseType.Name, scenario.BaseTypeInstantiationOnDerivedType);
                switch (scenario.InterfaceImplementationApproach)
                {
                    case InterfaceImplementationApproach.OnBaseType:
                    case InterfaceImplementationApproach.OnBothBaseAndDerived:
                    case InterfaceImplementationApproach.OnBothBaseAndDerivedBaseIsAbstract:
                        derivedType.ClassFlags = "public auto ansi";
                        break;

                    default:
                        throw new Exception("Unkonwn interface approach");
                }
                derivedType.Name = "Derived" + scenarioName + GenericTypeSuffix(scenario.DerivedTypeGenericParams);
                derivedType.GenericParams = scenario.DerivedTypeGenericParams;
                if (scenario.InterfaceImplementationApproach.ToString().Contains("Derived"))
                {
                    derivedType.InterfacesImplemented = new string[] { ToILDasmTypeName(iface.Name, scenario.InterfaceTypeInstantiationOnDerivedType) };
                }

                EmitClass(twOutputTest, derivedType);
                switch (scenario.InterfaceImplementationApproach)
                {
                    case InterfaceImplementationApproach.OnBaseType:
                    case InterfaceImplementationApproach.OnBothBaseAndDerived:
                        break;

                    case InterfaceImplementationApproach.OnBothBaseAndDerivedBaseIsAbstract:
                        MethodDesc ifaceImplMethod = new MethodDesc();
                        ifaceImplMethod.HasBody = true;
                        ifaceImplMethod.MethodFlags = "public static";
                        ifaceImplMethod.ReturnType = scenario.DerivedTypeReturnType;
                        ifaceImplMethod.Name = "MethodImplOnDerived";
                        EmitMethod(twOutputTest, ifaceImplMethod);
                        twOutputTest.WriteLine($"    .override method {scenario.InterfaceReturnType} {ToILDasmTypeName(iface.Name, scenario.InterfaceTypeInstantiationOnDerivedType)}::Method()");
                        twOutputTest.WriteLine($"    .locals init ({scenario.DerivedTypeReturnType} V_O)");
                        twOutputTest.WriteLine($"    ldloca.s 0");
                        twOutputTest.WriteLine($"    initobj {scenario.DerivedTypeReturnType}");
                        twOutputTest.WriteLine($"    ldloc.0");
                        twOutputTest.WriteLine($"    ret");
                        EmitEndMethod(twOutputTest, ifaceImplMethod);
                        break;
                    default:
                        throw new Exception("Unknown interface approach");
                }
                EmitEndClass(twOutputTest, derivedType);

                // Emit test method which performs constrained call to hit the method
                MethodDesc mdIndividualTestMethod = new MethodDesc();
                string basicTestMethodName = $"Test_{scenarioName}";
                mdIndividualTestMethod.Name = basicTestMethodName;
                mdIndividualTestMethod.HasBody = true;
                mdIndividualTestMethod.MethodFlags = "public static";
                mdIndividualTestMethod.MethodImpls = null;
                mdIndividualTestMethod.ReturnType = "void";
                mdIndividualTestMethod.Arguments = "";

                EmitMethod(swTestClassMethods, mdIndividualTestMethod);
                swTestClassMethods.WriteLine($"    constrained. {ToILDasmTypeName(derivedType.Name, scenario.DerivedTypeInstantiation)}");
                swTestClassMethods.WriteLine($"    call {scenario.CallReturnType} {ToILDasmTypeName(iface.Name, scenario.CallInterfaceTypeInstantiation)}::Method()");
                if (scenario.CallReturnType != "void")
                {
                    // TODO: should we rather convert the value to string and stsfld Statics.String?
                    swTestClassMethods.WriteLine($"    pop");
                }
                swTestClassMethods.WriteLine($"    ldstr \"{scenarioName}\"");
                swTestClassMethods.WriteLine($"    ldnull");
                swTestClassMethods.WriteLine($"    call void {CommonCsPrefix}Statics::CheckForFailure(string,string)");
                swTestClassMethods.WriteLine($"    ret");
                EmitEndMethod(swTestClassMethods, mdIndividualTestMethod);
                // Call test method from main method
                swMainMethodBody.WriteLine("    .try {");
                swMainMethodBody.WriteLine($"        call void TestEntrypoint::{mdIndividualTestMethod.Name}()");
                swMainMethodBody.WriteLine($"        leave.s {scenarioName}Done");
                swMainMethodBody.WriteLine("    } catch [System.Runtime]System.Exception {");
                swMainMethodBody.WriteLine($"        stloc.0");
                swMainMethodBody.WriteLine($"        ldstr \"{scenarioName}\"");
                swMainMethodBody.WriteLine($"        ldnull");
                swMainMethodBody.WriteLine($"        ldloc.0");
                swMainMethodBody.WriteLine($"        callvirt   instance string [System.Runtime]System.Object::ToString()");
                swMainMethodBody.WriteLine($"        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)");
                swMainMethodBody.WriteLine($"        leave.s {scenarioName}Done");
                swMainMethodBody.WriteLine("    }");
                swMainMethodBody.WriteLine($"{scenarioName}Done: nop");

                string GenericTypeSuffix(string genericParams)
                {
                    if (String.IsNullOrEmpty(genericParams))
                        return "";

                    return $"`{genericParams.Split(',').Length}";
                }
            }

            ClassDesc mainClass = new ClassDesc();
            mainClass.BaseType = "[System.Runtime]System.Object";
            mainClass.ClassFlags = "public auto ansi";
            mainClass.Name = "TestEntrypoint";

            EmitClass(twOutputTest, mainClass);

            twOutputTest.Write(swTestClassMethods.ToString());

            MethodDesc mainMethod = new MethodDesc();
            mainMethod.Name = "Main";
            mainMethod.Arguments = "";
            mainMethod.ReturnType = "int32";
            mainMethod.MethodImpls = null;
            mainMethod.HasBody = true;
            mainMethod.MethodFlags = "public static";

            EmitMethod(twOutputTest, mainMethod);
            twOutputTest.WriteLine("    .entrypoint");
            twOutputTest.WriteLine("    .locals init (class [System.Runtime]System.Exception V_0)");
            twOutputTest.Write(swMainMethodBody.ToString());
            twOutputTest.WriteLine($"    call int32 { CommonCsPrefix}Statics::ReportResults()");
            twOutputTest.WriteLine("    ret");

            EmitEndMethod(twOutputTest, mainMethod);
            EmitEndClass(twOutputTest, mainClass);
        }
    }
}
