// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace StaticTestGenerator
{
    /// <summary>
    /// Utility that reflects over an xunit test assembly and generates a static .cs/.csproj containing invocations
    /// of all of the tests, with minimal additional ceremony and no use of reflection.
    /// </summary>
    public static class Program
    {
        /// <summary>Entrypoint to the utility.</summary>
        /// <param name="args">Command-line arguments.</param>
        public static void Main(string[] args)
        {
            // Validate the command line and parse out the relevant pieces.
            if (!TryParseCommandLine(args, out string testAssemblyPath, out string runtimeAssembliesPath, out string outputPath, out Xunit.ConsoleClient.CommandLine? xunitCommandLine))
            {
                return;
            }

            // Set up an assembly resolving event handler to help locate helper assemblies that are needed
            // to process the test assembly, such as xunit assemblies and corefx test helpers.
            string[] probingPaths = new[] { Path.GetDirectoryName(testAssemblyPath), runtimeAssembliesPath, outputPath, AppContext.BaseDirectory };
            AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
            {
                string name = args.Name;
                int comma = name.IndexOf(',');
                if (comma > 0)
                {
                    name = name.Substring(0, comma);
                }
                if (!name.EndsWith(".dll"))
                {
                    name += ".dll";
                }

                foreach (string probingPath in probingPaths)
                {
                    try
                    {
                        Assembly a = Assembly.LoadFrom(Path.Combine(probingPath, name));
                        Log($"Loaded {a} from {a.Location}");
                        return a;
                    }
                    catch { }
                }

                return null;
            };

            // Discover all of the test methods in the test assembly, and find all theory inputs.
            DiscoverTestMethods(testAssemblyPath, out Assembly testAssembly, out TestDiscoverySink sink);
            Dictionary<IXunitTestCase, List<TestCase>> testCaseData = ComputeTestMethodTestCases(sink);

            int numUnsupported = 0, numCalls = 0;
            XunitFilters xunitFilters = xunitCommandLine!.Project.Filters;
            var sb = new StringBuilder();

            // Output the beginning of the program.
            Log("");
            sb.AppendLine(CodeTemplateStart);

            // Output calls for each test case.
            foreach (IXunitTestCase testCase in sink.TestCases)
            {
                // Skip test cases that aren't relevant to the current platform, OS, etc.  This is based
                // primarily on the traits applied earlier.
                if (!xunitFilters.Filter(testCase))
                {
                    continue;
                }

                MethodInfo m = ((ReflectionMethodInfo)testCase.Method).MethodInfo;
                Type t = m.ReflectedType;

                // Skip test cases we can't support.  Some of these xunit doesn't support
                // either, so it's just for good measure; in other cases, xunit can support
                // them but with a lot of work at run time, often involving complicated reflection.
                if (t.IsGenericType)
                {
                    Log($"Unsupported {t.Name}.{testCase.Method.Name}. Generic type.");
                    numUnsupported++;
                    continue;
                }

                if (!IsPublic(t))
                {
                    Log($"Unsupported {t.Name}.{testCase.Method.Name}. Non-public type.");
                    numUnsupported++;
                    continue;
                }

                if (!m.IsPublic)
                {
                    Log($"Unsupported {t.Name}.{testCase.Method.Name}. Non-public method.");
                    numUnsupported++;
                    continue;
                }

                if (!m.IsStatic && !HasSupportedConstructor(t))
                {
                    Log($"Unsupported {t.Name}.{testCase.Method.Name}. Unsupported ctor.");
                    numUnsupported++;
                    continue;
                }

                // Output a call per theory data for this test case.
                sb.AppendLine("{");
                List<TestCase> testCases = testCaseData[testCase];
                for (int i = 0; i < testCases.Count; i++)
                {
                    TestCase test = testCases[i];
                    MethodInfo? mi = test.MemberDataMember as MethodInfo ?? (test.MemberDataMember as PropertyInfo)?.GetGetMethod();

                    // Skip theory data we can't support.

                    if (mi != null)
                    {
                        if (!mi.IsPublic || !mi.IsStatic)
                        {
                            Log($"Unsupported {t.Name}.{testCase.Method.Name}. Non-public MemberData {mi.Name}.");
                            numUnsupported++;
                            continue;
                        }

                        if (m.IsGenericMethod)
                        {
                            Log($"Unsupported {t.Name}.{testCase.Method.Name}. Generic method requires reflection invoke.");
                            numUnsupported++;
                            continue;
                        }
                    }

                    if (test.MemberDataMember is FieldInfo fi && (!fi.IsPublic || !fi.IsStatic))
                    {
                        Log($"Unsupported {t.Name}.{testCase.Method.Name}. Non-public MemberData field {fi.Name}.");
                        numUnsupported++;
                        continue;
                    }

                    if (test.Values != null)
                    {
                        if (!test.Values.All(v => v == null || (v is Type t && IsPublic(t)) || IsPublic(v.GetType())))
                        {
                            Log($"Unsupported {t.Name}.{testCase.Method.Name}. Non-public theory argument.");
                            numUnsupported++;
                            continue;
                        }
                    }

                    // Compute a display name to render.  This will be displayed in an error message, and
                    // can be used to then correlate back to the offending line in the generated .cs file.
                    string displayName = testCase.DisplayName;
                    if (testCases.Count > 1)
                    {
                        displayName += "{" + i + "}";
                    }

                    // Write out the call.
                    GenerateTestCaseCode(sb, displayName, t, m, test);
                    numCalls++;
                }
                sb.AppendLine("}");
            }

            // Output the end of the program.
            sb.AppendLine(CodeTemplateEnd);

            Log("");
            Log($"Num unsupported: {numUnsupported}");
            Log($"Num calls written: {numCalls}");
            Log("");

            // Make sure our output directory exists
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Write out the .cs file
            string csPath = Path.Combine(outputPath, "Program.cs");
            File.WriteAllText(csPath, CSharpSyntaxTree.ParseText(sb.ToString()).GetRoot().NormalizeWhitespace().ToString());
            Log($"Wrote {csPath}");

            // Write out the associated .csproj
            string csprojPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(testAssemblyPath) + "-runner.csproj");
            File.WriteAllText(
                csprojPath,
                CSProjTemplate
                .Replace("#HelperAssemblyLocation#", runtimeAssembliesPath)
                .Replace("#TestAssembly#", Path.GetFullPath(testAssemblyPath))
                .Replace("#TestAssemblyLocation#", testAssemblyPath));
            Log($"Wrote {csprojPath}");
        }

        /// <summary>Parse the command-line.</summary>
        /// <param name="args">The arguments passed to Main.</param>
        /// <param name="testAssemblyPath">The location of the xunit test assembly to be analyzed. The resulting .cs file will call into this assembly.</param>
        /// <param name="runtimeAssembliesPath">The directory containing all of the helper assemblies needed, e.g. xunit's assemblies, corefx utility assemblies, etc.</param>
        /// <param name="outputPath">The directory into which the resulting project should be written.</param>
        /// <param name="xunitCommandLine">The xunit command-line object to pass to xunit test discovery.</param>
        /// <returns></returns>
        private static bool TryParseCommandLine(
            string[] args,
            out string testAssemblyPath,
            out string runtimeAssembliesPath,
            out string outputPath,
            out Xunit.ConsoleClient.CommandLine? xunitCommandLine)
        {
            if (args.Length >= 3)
            {
                static string EnsureEndsWithSeparator(string path) =>
                    !path.EndsWith(Path.DirectorySeparatorChar) && !path.EndsWith(Path.AltDirectorySeparatorChar) ?
                    path + Path.DirectorySeparatorChar :
                    path;

                runtimeAssembliesPath = EnsureEndsWithSeparator(args[1]);
                testAssemblyPath = Path.GetFullPath(args[2]);
                outputPath = EnsureEndsWithSeparator(Path.Combine(args[0], Path.GetFileNameWithoutExtension(testAssemblyPath)));

                // Gather arguments for xunit.
                var argsForXunit = new List<string>();
                argsForXunit.Add(testAssemblyPath); // first argument is the test assembly
                foreach (string extraArg in args.Skip(3))
                {
                    // If an argument is a response file, load its contents and add that instead.
                    if (extraArg.StartsWith("@"))
                    {
                        argsForXunit.AddRange(from line in File.ReadAllLines(extraArg.Substring(1))
                                              where line.Length > 0 && line[0] != '#'
                                              from part in line.Split(' ')
                                              select part);
                    }
                    else
                    {
                        // Otherwise, add the argument as-is.
                        argsForXunit.Add(extraArg);
                    }
                }

                // If the only argument added was the test assembly path, use default arguments.
                if (argsForXunit.Count == 1)
                {
                    argsForXunit.AddRange(s_defaultXunitOptions);
                }

                // Finally, hand off these arguments to xunit.
                xunitCommandLine = Xunit.ConsoleClient.CommandLine.Parse(argsForXunit.ToArray());

                Log($"Test assembly path    : {testAssemblyPath}");
                Log($"Helper assemblies path: {runtimeAssembliesPath}");
                Log($"Output path           : {outputPath}");
                Log($"Xunit arguments       : {string.Join(" ", argsForXunit)}");
                Log("");
                return true;
            }

            // Invalid command line arguments.
            Console.WriteLine("Usage: <output_directory> <helper_assemblies_directory> <test_assembly_path> <xunit_console_options>");
            Console.WriteLine("    Example:");
            Console.WriteLine(@"   dotnet run d:\tmpoutput d:\repos\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Debug-x64\shared\Microsoft.NETCore.App\9.9.9 d:\repos\corefx\artifacts\bin\System.Runtime.Tests\netcoreapp-Windows_NT-Debug\System.Runtime.Tests.dll");
            testAssemblyPath = string.Empty;
            runtimeAssembliesPath = string.Empty;
            outputPath = string.Empty;
            xunitCommandLine = null;
            return false;
        }

        /// <summary>Find all test methods in the test assembly.  The resulting <paramref name="sink"/> will contain the found tests.</summary>
        /// <param name="testAssemblyPath">The path to the test assembly.</param>
        /// <param name="testAssembly">The loaded test assembly.</param>
        /// <param name="sink">The discovered tests.</param>
        private static void DiscoverTestMethods(string testAssemblyPath, out Assembly testAssembly, out TestDiscoverySink sink)
        {
            // Load the test assembly.
            testAssembly = Assembly.LoadFrom(testAssemblyPath);
            Log($"Loaded {testAssembly.GetName().Name} from {testAssembly.Location}");

            // Find all tests.
            var discoverer = new Xunit2Discoverer(
                AppDomainSupport.Denied,
                new NullSourceInformationProvider(),
                new ReflectionAssemblyInfo(testAssembly),
                xunitExecutionAssemblyPath: null,
                shadowCopyFolder: null,
                new Xunit.NullMessageSink());

            sink = new TestDiscoverySink();

            discoverer.Find(includeSourceInformation: false, sink, TestFrameworkOptions.ForDiscovery(new TestAssemblyConfiguration()
            {
                DiagnosticMessages = true,
                InternalDiagnosticMessages = true,
                PreEnumerateTheories = false,
                StopOnFail = false
            }));

            // Wait for the find to complete.
            sink.Finished.WaitOne();
            Log($"Found {sink.TestCases.Count} test methods.");
        }

        /// <summary>Find all test cases associated with the found tests (e.g. one test case per theory input to each test).</summary>
        /// <param name="sink">The sink containing the discovered tests.</param>
        /// <returns>A dictionary of all tests and their associated test cases.</returns>
        private static Dictionary<IXunitTestCase, List<TestCase>> ComputeTestMethodTestCases(TestDiscoverySink sink)
        {
            int inlineDatas = 0, methodDatas = 0;

            // Create the dictionary containing all tests and associated test cases.
            Dictionary<IXunitTestCase, List<TestCase>> testCases = sink
                .TestCases
                .Cast<IXunitTestCase>()
                .Select(tc =>
                {
                    MethodInfo testMethod = ((ReflectionMethodInfo)tc.Method).MethodInfo;
                    Type testMethodType = testMethod.ReflectedType;

                    var cases = new List<TestCase>();

                    if (testMethod.GetParameters().Length > 0)
                    {
                        // The test method has arguments, so look for all of the data attributes we can use to invoke the theory.
                        foreach (DataAttribute attr in testMethod.GetCustomAttributes<DataAttribute>(inherit: true))
                        {
                            // DataAttributes can themselves be marked to be skipped.  Ignore the attribute if it is.
                            if (!string.IsNullOrWhiteSpace(attr.Skip))
                            {
                                continue;
                            }

                            switch (attr)
                            {
                                case InlineDataAttribute inlineData:
                                    // For an [InlineData(...)], just add a single test case containing the values.
                                    cases.Add(new TestCase() { Values = inlineData.GetData(null).First() });
                                    inlineDatas++;
                                    break;

                                case MemberDataAttribute memberData:
                                    // For a [MemberData(...)], it might point to a method, property, or field; get the right
                                    // piece of metadata.  Also, for methods, there might be data to pass to the method
                                    // when invoking it; store that as well.
                                    Type memberDataType = memberData.MemberType ?? testMethod.DeclaringType;

                                    MethodInfo testDataMethod = memberDataType.GetMethod(memberData.MemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                                    if (testDataMethod != null)
                                    {
                                        methodDatas++;
                                        cases.Add(new TestCase { MemberDataMember = testDataMethod, Values = memberData.Parameters });
                                        break;
                                    }

                                    PropertyInfo testDataProperty = memberDataType.GetProperty(memberData.MemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                                    if (testDataProperty != null)
                                    {
                                        methodDatas++;
                                        cases.Add(new TestCase { MemberDataMember = testDataProperty });
                                        break;
                                    }

                                    FieldInfo testDataField = memberDataType.GetField(memberData.MemberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                                    if (testDataField != null)
                                    {
                                        cases.Add(new TestCase { MemberDataMember = testDataField });
                                        break;
                                    }

                                    Log($"Could not find {memberData.MemberName} in MemberData on {testMethod}");
                                    break;

                                case DataAttribute dataAttribute:
                                    // Some other DataAttribute we don't understand. Skip it.
                                    Log($"Unknown data attribute {dataAttribute} on {testMethod}");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // There are no arguments to the method, so we just add a single test case to represent invoking the method.
                        cases.Add(new TestCase());
                    }

                    return KeyValuePair.Create(tc, cases);
                }).ToDictionary(k => k.Key, k => k.Value);

            Log($"Found {inlineDatas} InlineDatas / {methodDatas} MethodDatas across {testCases.Count} test methods.");
            return testCases;
        }

        /// <summary>Writes the code for invoking the test case into the <see cref="StringBuilder"/>.</summary>
        /// <param name="sb">The destination StringBuilder.</param>
        /// <param name="testCaseDisplayName">The display name of the test case.</param>
        /// <param name="testMethodType">The type on which the test method lives.</param>
        /// <param name="testMethod">The test method.</param>
        /// <param name="testCase">The test case.</param>
        private static void GenerateTestCaseCode(
            StringBuilder sb, string testCaseDisplayName, Type testMethodType, MethodInfo testMethod, TestCase testCase)
        {
            // Get the parameters for the test method.
            ParameterInfo[] parameters = testMethod.GetParameters();

            // Writes out ".MethodName(arg1, arg2, ...)" when all arguments are statically available.
            // The arguments are written as literals.
            void WriteMethodNameAndArgumentsStatic(object[]? arguments)
            {
                sb.Append("." + testMethod.Name + "(");
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(EncodeLiteral(arguments?[i], parameters[i].ParameterType));
                }
                sb.Append(")");
            }

            // Writes out ".MethodName(Cast<T0>(args[0]), Cast<T1>(args[1]), ...)" when all arguments are only
            // known at execution time from invoking a theory member data.
            void WriteMethodNameAndArgumentsDynamic(string argumentsName)
            {
                sb.Append("." + testMethod.Name + "(");
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append($"Cast<{GetTypeName(parameters[i].ParameterType)}>({argumentsName}[{i}])");
                }
                sb.Append(")");
            }

            // TODO: Support IDisposable on these objects passed to the ctors, and create one that's shared/reused
            // across every method on the same test class.

            // Gets the argument string to pass to the test class.  This may include instantiating
            // a class fixture to pass into the test class' ctor.
            string GetConstructorArgs()
            {
                ConstructorInfo[] ctors = testMethodType.GetConstructors();
                foreach (ConstructorInfo ctor in ctors)
                {
                    ParameterInfo[] parameters = ctor.GetParameters();
                    switch (parameters.Length)
                    {
                        case 0:
                            return string.Empty;

                        case 1:
                            if (parameters[0].GetType() == typeof(ITestOutputHelper))
                            {
                                return typeof(TestOutputHelper).FullName;
                            }

                            return $"new {GetTypeName(parameters[0].GetType())}()";
                    }
                }

                Log($"ERROR: Unexpected constructor found.");
                return string.Empty;
            }

            // Write out the method call to invoke the test case with all arguments known at compile time.
            void WriteInvocationStatic(object[]? arguments)
            {
                if (testMethod.IsStatic)
                {
                    sb.Append($"Execute(\"{testCaseDisplayName}\", () => {GetTypeName(testMethodType)}");
                    WriteMethodNameAndArgumentsStatic(arguments);
                    sb.AppendLine(", ref succeeded, ref failed);");
                }
                else if (testMethodType.GetInterface("IDisposable") != null)
                {
                    sb.AppendLine($"using (var inst = new {GetTypeName(testMethodType)}({GetConstructorArgs()}))");
                    sb.Append($"Execute(\"{testCaseDisplayName}\", () => inst");
                    WriteMethodNameAndArgumentsStatic(arguments);
                    sb.AppendLine(", ref succeeded, ref failed);");
                }
                else
                {
                    sb.Append($"Execute(\"{testCaseDisplayName}\", () => new {GetTypeName(testMethodType)}({GetConstructorArgs()})");
                    WriteMethodNameAndArgumentsStatic(arguments);
                    sb.AppendLine(", ref succeeded, ref failed);");
                }
            }

            // Write out the method call to invoke the test case with all arguments known at run time.
            void WriteInvocationDynamic(string argumentsName)
            {
                if (testMethod.IsStatic)
                {
                    sb.Append($"Execute(\"{testCaseDisplayName}\", () => {GetTypeName(testMethodType)}");
                    WriteMethodNameAndArgumentsDynamic(argumentsName);
                    sb.AppendLine(", ref succeeded, ref failed);");
                }
                else if (testMethodType.GetInterface("IDisposable") != null)
                {
                    sb.AppendLine($"using (var inst = new {GetTypeName(testMethodType)}({GetConstructorArgs()}))");
                    sb.Append($"Execute(\"{testCaseDisplayName}\", () => inst");
                    WriteMethodNameAndArgumentsDynamic(argumentsName);
                    sb.AppendLine(", ref succeeded, ref failed);");
                }
                else
                {
                    sb.Append($"Execute(\"{testCaseDisplayName}\", () => new {GetTypeName(testMethodType)}({GetConstructorArgs()})");
                    WriteMethodNameAndArgumentsDynamic(argumentsName);
                    sb.AppendLine(", ref succeeded, ref failed);");
                }
            }

            // Write out the invocation, with input coming from a theory data attribute if relevant.
            switch (testCase.MemberDataMember)
            {
                case MethodInfo mi:
                    // This is a theory with data coming from a MemberData method.
                    string memberDataArgs = string.Empty;
                    if (testCase.Values != null)
                    {
                        // There are arguments to the member data; serialize them to be used in the call to it.
                        ParameterInfo[] memberDataParameters = mi.GetParameters();
                        var argsSb = new StringBuilder();
                        for (int i = 0; i < testCase.Values.Length; i++)
                        {
                            if (i != 0)
                            {
                                argsSb.Append(", ");
                            }
                            argsSb.Append(EncodeLiteral(testCase.Values[i], memberDataParameters[i].ParameterType));
                        }
                        memberDataArgs = argsSb.ToString();
                    }
                    sb.AppendLine($"foreach (object[] row in {GetTypeName(mi.ReflectedType)}.{mi.Name}({memberDataArgs}))");
                    WriteInvocationDynamic("row");
                    break;

                case PropertyInfo pi:
                    // This is a theory with data coming from a MemberData property.
                    sb.AppendLine($"foreach (object[] row in {GetTypeName(pi.ReflectedType)}.{pi.Name})");
                    WriteInvocationDynamic("row");
                    break;

                case FieldInfo fi:
                    // This is a theory with data coming from a MemberData field.
                    sb.AppendLine($"foreach (object[] row in {GetTypeName(fi.ReflectedType)}.{fi.Name})");
                    WriteInvocationDynamic("row");
                    break;

                default:
                    // This is either a method with no arguments, or it's a theory with theory data
                    // coming from an InlineData or some other means where we know all of the values
                    // at compile time.
                    WriteInvocationStatic(testCase.Values);
                    break;
            }
        }

        /// <summary>Encodes the provided object as a literal.</summary>
        /// <param name="literal">The literal to encode.</param>
        /// <param name="expectedType">The type that's expected at the usage location.</param>
        /// <returns>A string representing the encoded literal.</returns>
        private static string EncodeLiteral(object? literal, Type expectedType)
        {
            if (literal == null)
            {
                return "null";
            }

            if (literal is Type t)
            {
                return $"typeof({GetTypeName(t)})";
            }

            if (literal is Array arr)
            {
                Type elementType = literal.GetType().GetElementType();
                return
                    $"new {GetTypeName(elementType)}[]" +
                    "{" +
                    string.Join(",", arr.Cast<object>().Select(o => EncodeLiteral(o, elementType))) +
                    "}";
            }

            if (literal is Guid guid)
            {
                return $"Guid.Parse(\"{guid}\")";
            }

            if (literal is IntPtr ptr)
            {
                return $"new IntPtr(0x{((long)ptr).ToString("X")})";
            }

            if (literal is UIntPtr uptr)
            {
                return $"new UIntPtr(0x{((ulong)uptr).ToString("X")})";
            }

            string? result = null;

            if (literal is Enum e)
            {
                result = $"({GetTypeName(e.GetType())})({e.ToString("D")}{(Convert.GetTypeCode(literal) == TypeCode.UInt64 ? "UL" : "L")})";
            }
            else
            {
                switch (Type.GetTypeCode(literal.GetType()))
                {
                    case TypeCode.Boolean:
                        result = ((bool)literal).ToString().ToLowerInvariant();
                        break;
                    case TypeCode.Char:
                        result = $"'\\u{((int)(char)literal).ToString("X4")}'";
                        break;
                    case TypeCode.SByte:
                        result = $"(sbyte)({literal.ToString()})";
                        break;
                    case TypeCode.Byte:
                        result = $"(byte){literal.ToString()}";
                        break;
                    case TypeCode.Int16:
                        result = $"(short)({literal.ToString()})";
                        break;
                    case TypeCode.UInt16:
                        result = $"(ushort){literal.ToString()}";
                        break;
                    case TypeCode.Int32:
                        result = $"({literal.ToString()})";
                        break;
                    case TypeCode.UInt32:
                        result = $"{literal.ToString()}U";
                        break;
                    case TypeCode.Int64:
                        result = $"({literal.ToString()}L)";
                        break;
                    case TypeCode.UInt64:
                        result = $"{literal.ToString()}UL";
                        break;
                    case TypeCode.Decimal:
                        result = $"({literal.ToString()}M)";
                        break;
                    case TypeCode.Single:
                        result =
                            float.IsNegativeInfinity((float)literal) ? "float.NegativeInfinity" :
                            float.IsInfinity((float)literal) ? "float.PositiveInfinity" :
                            float.IsNaN((float)literal) ? "float.NaN" :
                            $"(float)({((float)literal).ToString("R")}F)";
                        break;
                    case TypeCode.Double:
                        result =
                            double.IsNegativeInfinity((double)literal) ? "double.NegativeInfinity" :
                            double.IsInfinity((double)literal) ? "double.PositiveInfinity" :
                            double.IsNaN((double)literal) ? "double.NaN" :
                            $"(double)({((double)literal).ToString("R")}D)";
                        break;
                    case TypeCode.String:
                        var sb = new StringBuilder();
                        sb.Append('"');
                        foreach (char c in literal.ToString())
                        {
                            if (c == '\\')
                            {
                                sb.Append("\\\\");
                            }
                            else if (c >= 32 && c < 127)
                            {
                                sb.Append(c);
                            }
                            else
                            {
                                sb.Append($"\\u{((int)c).ToString("X4")}");
                            }
                        }
                        sb.Append('"');
                        result = sb.ToString();
                        break;
                    default:
                        Log($"Unhandled value: {literal} ({literal?.GetType()})");
                        return string.Empty;
                }
            }

            if (expectedType != null &&
                Nullable.GetUnderlyingType(expectedType) == null &&
                literal.GetType() != expectedType &&
                !expectedType.IsGenericParameter)
            {
                result = $"({GetTypeName(expectedType)})({result})";
            }

            return result;
        }

        /// <summary>Gets the full type name that can be written into the source, e.g. in a typeof or in a method invocation.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The rendered type name.</returns>
        private static string GetTypeName(Type type)
        {
            if (type == typeof(void))
            {
                return "void";
            }

            if (type == typeof(object))
            {
                return "object";
            }

            if (!type.IsEnum)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean: return "bool";
                    case TypeCode.Byte: return "byte";
                    case TypeCode.Char: return "char";
                    case TypeCode.Decimal: return "decimal";
                    case TypeCode.Double: return "double";
                    case TypeCode.Int16: return "short";
                    case TypeCode.Int32: return "int";
                    case TypeCode.Int64: return "long";
                    case TypeCode.SByte: return "sbyte";
                    case TypeCode.Single: return "float";
                    case TypeCode.String: return "string";
                    case TypeCode.UInt16: return "ushort";
                    case TypeCode.UInt32: return "uint";
                    case TypeCode.UInt64: return "ulong";
                }
            }

            if (type.IsArray)
            {
                return GetTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (type.IsPointer)
            {
                return GetTypeName(type.GetElementType()) + "*";
            }

            string name =
                type.IsNested ? GetTypeName(type.DeclaringType) + "." + type.Name :
                !string.IsNullOrEmpty(type.Namespace) ? type.Namespace + "." + type.Name :
                type.Name;

            if (!type.IsGenericType)
            {
                return name;
            }

            name = name.Substring(0, name.IndexOf("`"));

            Type[] genericArgs = type.GetGenericArguments();
            if (type.IsNested && type.DeclaringType.IsGenericType)
            {
                genericArgs = genericArgs.Skip(type.DeclaringType.GetGenericArguments().Length).ToArray();
            }

            return
                name + 
                "<" +
                (type.IsConstructedGenericType ? string.Join(", ", genericArgs.Select(g => GetTypeName(g))) : new string(',', genericArgs.Length - 1)) +
                ">";
        }

        /// <summary>Determines whether the type has public visibility such that we can emit calls into it.</summary>
        /// <param name="type">The type.</param>
        /// <returns>true if we can make calls to the type; otherwise, false.</returns>
        private static bool IsPublic(Type type)
        {
            if (type.IsArray || type.IsPointer)
            {
                return IsPublic(type.GetElementType());
            }

            if (type.IsNested)
            {
                return type.IsNestedPublic && IsPublic(type.DeclaringType);
            }

            return type.IsPublic;
        }

        /// <summary>Determines whether the test class has a ctor we can use to instantiate it.</summary>
        /// <param name="testClassType">The type.</param>
        /// <returns>true if we can instantiate the test class; otherwise, false.</returns>
        private static bool HasSupportedConstructor(Type testClassType)
        {
            foreach (ConstructorInfo ci in testClassType.GetConstructors())
            {
                ParameterInfo[] parameters = ci.GetParameters();
                switch (parameters.Length)
                {
                    case 0:
                        // If there's a default ctor, we're good to go.
                        return true;

                    case 1:
                        // If the test class takes an ITestOutputHelper, we can manufacture
                        // a TestOutputHelper.
                        if (parameters[0].GetType() == typeof(ITestOutputHelper))
                        {
                            return true;
                        }

                        // If the test class takes a type that has a public ctor we can
                        // use to create it, then we're also fine.
                        Type ctorArgType = parameters[0].GetType();
                        if (IsPublic(ctorArgType) && ctorArgType.GetConstructor(Type.EmptyTypes) != null)
                        {
                            return true;
                        }
                        break;
                }
            }

            // We don't know how to instantiate this test class.
            return false;
        }

        /// <summary>Log a message to the console.</summary>
        /// <param name="message">The message to log.</param>
        private static void Log(string message)
        {
            message ??= string.Empty;

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(DateTime.Now + " | ");
            Console.ResetColor();

            const string ToHighlight = "Unsupported";
            if (message.StartsWith(ToHighlight))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(ToHighlight);
                Console.ResetColor();
                message = message.Substring(ToHighlight.Length);
            }

            Console.WriteLine(message);
        }

        /// <summary>Represents a test case for a test method.</summary>
        private sealed class TestCase
        {
            /// <summary>The method, property, or field to invoke or access to retrieve theory data.</summary>
            public MemberInfo? MemberDataMember;
            /// <summary>
            /// If <see cref="MemberDataMember"/> is a MethodInfo, the arguments to the test method, if there are any.
            /// Otherwise, the arguments to the member data method, or null if there aren't any.
            /// </summary>
            public object[]? Values;
        }

        /// <summary>Default options to use when constructing xunit options if no additional options are provided.</summary>
        private static readonly string[] s_defaultXunitOptions = new string[]
        {
            "-notrait", "category=nonnetcoreapptests",
            "-notrait", "category=nonwindowstests",
            "-notrait", "category=IgnoreForCI",
            "-notrait", "category=failing",
            "-notrait", "category=OuterLoop"
        };

        /// <summary>The code to write out to the output file before all of the test cases.</summary>
        private const string CodeTemplateStart =
@"using System;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;

public static class Test
{
    public static void Main()
    {
        int succeeded = 0, failed = 0;

";
        /// <summary>The code to write out to the output file after all of the test cases.</summary>
        private const string CodeTemplateEnd =
@"
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        int total = succeeded + failed;
        Console.WriteLine($""Total : {total}"");
        if (total > 0)
        {
            Console.WriteLine($""Passed: {succeeded} ({(succeeded * 100.0 / total).ToString(""N"")}%)"");
            Console.WriteLine($""Failed: {failed} ({(failed * 100.0 / total).ToString(""N"")}%)"");
        }
        Console.ResetColor();
    }

    private static void Execute(string name, Action action, ref int succeeded, ref int failed)
    {
        try
        {
            action();
            succeeded++;
        }
        catch (SkipTestException)
        {
            // Test is being skipped.
        }
        catch (Exception e) when (!(e is TypeLoadException))
        {
            failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(name + "" [FAIL]"");
            Console.ResetColor();
            Console.Error.WriteLine(e);
        }
    }

    private static void Execute(string name, Func<Task> action, ref int succeeded, ref int failed)
    {
        try
        {
            action().GetAwaiter().GetResult();
            succeeded++;
        }
        catch (SkipTestException)
        {
            // Test is being skipped.
        }
        catch (Exception e) when (!(e is TypeLoadException))
        {
            failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(name + "" [FAIL]"");
            Console.ResetColor();
            Console.Error.WriteLine(e);
        }
    }

    private static T Cast<T>(object obj) =>
        obj is null ? default :
        obj is T t ? t :
        (T)Convert.ChangeType(obj, typeof(T));
}
";

        /// <summary>The template for the .csproj.</summary>
        private const string CSProjTemplate =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>IDE0049</NoWarn> <!-- names can be simplified -->
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""xunit.core""><HintPath>#HelperAssemblyLocation#xunit.core.dll</HintPath></Reference>
    <Reference Include=""xunit.assert""><HintPath>#HelperAssemblyLocation#xunit.assert.dll</HintPath></Reference>
    <Reference Include=""CoreFx.Private.TestUtilities""><HintPath>#HelperAssemblyLocation#CoreFx.Private.TestUtilities.dll</HintPath></Reference>
    <Reference Include=""System.Runtime.CompilerServices.Unsafe""><HintPath>#HelperAssemblyLocation#System.Runtime.CompilerServices.Unsafe.dll</HintPath></Reference>
    <Reference Include=""Microsoft.DotNet.XUnitExtensions""><HintPath>#HelperAssemblyLocation#Microsoft.DotNet.XUnitExtensions.dll</HintPath></Reference>
    <Reference Include=""#TestAssembly#""><HintPath>#TestAssemblyLocation#</HintPath></Reference>
  </ItemGroup>
</Project>";
    }
}
