// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using ILVerify;
using Internal.TypeSystem.Ecma;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace ILVerification.Tests
{
    /// <summary>
    /// Parses the methods in the test assemblies.
    /// It loads all assemblies from the test folder defined in <code>TestDataLoader.TestAssemblyPath</code>
    /// This class feeds the xunit Theories
    /// </summary>
    class TestDataLoader
    {
        /// <summary>
        /// The folder with the test binaries
        /// </summary>
        private const string TestAssemblyPath = @"Tests";

        private const string SpecialTestPrefix = "special.";

        /// <summary>
        ///  Returns all class correctly implement based on following naming convention
        ///  [FriendlyName]_ValidType_Valid
        /// </summary>
        /// <returns></returns>
        public static TheoryData<TestCase> GetTypesWithValidType()
        {
            var typeSelector = new Func<string[], TypeDefinitionHandle, TestCase>((mparams, typeDefinitionHandle) =>
            {
                if (mparams[1] == "ValidType")
                {
                    return new ValidTypeTestCase { MetadataToken = MetadataTokens.GetToken(typeDefinitionHandle) };
                }
                return null;
            });
            return GetTestTypeFromDll(typeSelector);
        }

        /// <summary>
        ///  Returns all class doesn't correctly implement based on following naming convention
        ///  [FriendlyName]_InvalidType_[ExpectedVerifierError1]@[ExpectedVerifierError2]....[ExpectedVerifierErrorN]
        /// </summary>
        /// <returns></returns>
        public static TheoryData<TestCase> GetTypesWithInvalidType()
        {
            var typeSelector = new Func<string[], TypeDefinitionHandle, TestCase>((mparams, typeDefinitionHandle) =>
            {
                if (mparams[1] == "InvalidType")
                {
                    var verificationErrors = new List<VerifierError>();
                    foreach (var expectedError in mparams[2].Split('@'))
                    {
                        verificationErrors.Add((VerifierError)Enum.Parse(typeof(VerifierError), expectedError));
                    }
                    var newItem = new InvalidTypeTestCase { MetadataToken = MetadataTokens.GetToken(typeDefinitionHandle) };
                    newItem.ExpectedVerifierErrors = verificationErrors;
                    return newItem;
                }
                return null;
            });
            return GetTestTypeFromDll(typeSelector);
        }

        private static TheoryData<TestCase> GetTestTypeFromDll(Func<string[], TypeDefinitionHandle, TestCase> typeSelector)
        {
            var retVal = new TheoryData<TestCase>();

            foreach (var testDllName in GetAllTestDlls())
            {
                EcmaModule testModule = GetModuleForTestAssembly(testDllName);
                MetadataReader metadataReader = testModule.PEReader.GetMetadataReader();
                foreach (TypeDefinitionHandle typeHandle in metadataReader.TypeDefinitions)
                {
                    var typeDef = metadataReader.GetTypeDefinition(typeHandle);
                    var typeName = metadataReader.GetString(typeDef.Name);
                    if (!string.IsNullOrEmpty(typeName) && typeName.Contains("_"))
                    {
                        var mparams = typeName.Split('_');
                        TestCase newItem = typeSelector(mparams, typeHandle);
                        if (newItem != null)
                        {
                            newItem.TestName = mparams[0];
                            newItem.TypeName = typeName;
                            newItem.ModuleName = testDllName;
                            retVal.Add(newItem);
                        }
                    }
                }
            }
            return retVal;
        }

        /// <summary>
        /// Returns all methods that contain valid IL code based on the following naming convention:
        /// [FriendlyName]_Valid
        /// The method must contain 1 '_'. The part before the '_' is a friendly name describing what the method does.
        /// The word after the '_' has to be 'Valid' (Case sensitive)
        /// E.g.: 'SimpleAdd_Valid'
        /// </summary>
        public static TheoryData<TestCase> GetMethodsWithValidIL()
        {
            var methodSelector = new Func<string[], MethodDefinitionHandle, TestCase>((mparams, methodHandle) =>
            {
                if (mparams.Length == 2 && mparams[1] == "Valid")
                {
                    return new ValidILTestCase { MetadataToken = MetadataTokens.GetToken(methodHandle) };
                }
                return null;
            });
            return GetTestMethodsFromDll(methodSelector);
        }

        /// <summary>
        /// Returns all methods that contain valid IL code based on the following naming convention:
        /// [FriendlyName]_Invalid_[ExpectedVerifierError1].[ExpectedVerifierError2]....[ExpectedVerifierErrorN]
        /// The method name must contain 2 '_' characters.
        /// 1. part: a friendly name
        /// 2. part: must be the word 'Invalid' (Case sensitive)
        /// 3. part: the expected VerifierErrors as string separated by '.'.
        /// E.g.: SimpleAdd_Invalid_ExpectedNumericType
        /// </summary>
        public static TheoryData<TestCase> GetMethodsWithInvalidIL()
        {
            var methodSelector = new Func<string[], MethodDefinitionHandle, TestCase>((mparams, methodHandle) =>
            {
                if (mparams.Length == 3 && mparams[1] == "Invalid")
                {
                    var expectedErrors = mparams[2].Split('.');
                    var verificationErrors = new List<VerifierError>();

                    foreach (var item in expectedErrors)
                    {
                        if (Enum.TryParse(item, out VerifierError expectedError))
                        {
                            verificationErrors.Add(expectedError);
                        }
                    }

                    var newItem = new InvalidILTestCase { MetadataToken = MetadataTokens.GetToken(methodHandle) };

                    if (expectedErrors.Length > 0)
                    {
                        newItem.ExpectedVerifierErrors = verificationErrors;
                    }

                    return newItem;
                }
                return null;
            });
            return GetTestMethodsFromDll(methodSelector);
        }

        private static TheoryData<TestCase> GetTestMethodsFromDll(Func<string[], MethodDefinitionHandle, TestCase> methodSelector)
        {
            var retVal = new TheoryData<TestCase>();

            foreach (var testDllName in GetAllTestDlls())
            {
                var testModule = GetModuleForTestAssembly(testDllName);

                foreach (var methodHandle in testModule.MetadataReader.MethodDefinitions)
                {
                    var method = (EcmaMethod)testModule.GetMethod(methodHandle);
                    var methodName = method.Name;

                    if (!methodName.Contains('_', StringComparison.Ordinal))
                        continue;

                    var index = methodName.LastIndexOf("_Valid", StringComparison.Ordinal);
                    if (index < 0)
                        index = methodName.LastIndexOf("_Invalid", StringComparison.Ordinal);
                    if (index < 0)
                        continue;

                    var substring = methodName.Substring(index + 1);
                    var split = substring.Split('_');
                    string[] mparams = new string[split.Length + 1];
                    split.CopyTo(mparams, 1);
                    mparams[0] = methodName.Substring(0, index);
                    // examples of methodName to mparams transformation:
                    //   * `get_Property` -> [ 'get_Property' ]
                    //   * `CheckSomething_Valid` -> [ 'CheckSomething', 'Valid' ]
                    //   * 'WrongMethod_Invalid_BranchOutOfTry' -> [ 'WrongMethod', 'Invalid', 'BranchOutOfTry' ]
                    //   * 'MoreWrongMethod_Invalid_TypeAccess.InitOnly' -> [ 'MoreWrongMethod', 'Invalid', 'TypeAccess', 'InitOnly' ]
                    //   * 'special.set_MyProperty.set_MyProperty_Invalid_InitOnly' -> [ 'special.set_MyProperty.set_MyProperty', 'Invalid', 'InitOnly' ]

                    var specialMethodHandle = HandleSpecialTests(mparams, method);
                    var newItem = methodSelector(mparams, specialMethodHandle);

                    if (newItem != null)
                    {
                        newItem.TestName = mparams[0];
                        newItem.MethodName = methodName;
                        newItem.ModuleName = testDllName;

                        retVal.Add(newItem);
                    }
                }
            }
            return retVal;
        }

        private static MethodDefinitionHandle HandleSpecialTests(string[] methodParams, EcmaMethod method)
        {
            if (!methodParams[0].StartsWith(SpecialTestPrefix))
                return method.Handle;

            // Cut off special prefix
            var specialParams = methodParams[0].Substring(SpecialTestPrefix.Length);

            // Get friendly name / special name
            int delimiter = specialParams.IndexOf('.');
            if (delimiter < 0)
                return method.Handle;

            var friendlyName = specialParams.Substring(0, delimiter);
            var specialName = specialParams.Substring(delimiter + 1);

            // Substitute method parameters with friendly name
            methodParams[0] = friendlyName;

            var specialMethodHandle = (EcmaMethod)method.OwningType.GetMethod(specialName, method.Signature);
            return specialMethodHandle == null ? method.Handle : specialMethodHandle.Handle;
        }

        private static IEnumerable<string> GetAllTestDlls()
        {
            foreach (var item in Directory.GetFiles(TestAssemblyPath))
            {
                if (item.ToLower().EndsWith(".dll"))
                {
                    yield return Path.GetFileName(item);
                }
            }
        }

        public static EcmaModule GetModuleForTestAssembly(string assemblyName)
        {
            var simpleNameToPathMap = new Dictionary<string, string>();

            foreach (var fileName in GetAllTestDlls())
            {
                simpleNameToPathMap.Add(Path.GetFileNameWithoutExtension(fileName), Path.Combine(TestAssemblyPath, fileName));
            }

            Assembly coreAssembly = typeof(object).GetTypeInfo().Assembly;
            simpleNameToPathMap.Add(coreAssembly.GetName().Name, coreAssembly.Location);

            Assembly systemRuntime = Assembly.Load(new AssemblyName("System.Runtime"));
            simpleNameToPathMap.Add(systemRuntime.GetName().Name, systemRuntime.Location);

            var resolver = new TestResolver(simpleNameToPathMap);
            var typeSystemContext = new ILVerifyTypeSystemContext(resolver);
            typeSystemContext.SetSystemModule(typeSystemContext.GetModule(resolver.Resolve(coreAssembly.GetName().Name)));

            return typeSystemContext.GetModule(resolver.Resolve(new AssemblyName(Path.GetFileNameWithoutExtension(assemblyName)).Name));
        }

        private sealed class TestResolver : IResolver
        {
            Dictionary<string, PEReader> _resolverCache = new Dictionary<string, PEReader>();
            Dictionary<string, string> _simpleNameToPathMap;

            public TestResolver(Dictionary<string, string> simpleNameToPathMap)
            {
                _simpleNameToPathMap = simpleNameToPathMap;
            }

            PEReader IResolver.ResolveAssembly(AssemblyName assemblyName)
                => Resolve(assemblyName.Name);

            PEReader IResolver.ResolveModule(AssemblyName referencingModule, string fileName)
                => Resolve(Path.GetFileNameWithoutExtension(fileName));

            public PEReader Resolve(string simpleName)
            {
                if (_resolverCache.TryGetValue(simpleName, out PEReader peReader))
                {
                    return peReader;
                }

                if (_simpleNameToPathMap.TryGetValue(simpleName, out string path))
                {
                    var result = new PEReader(File.OpenRead(path));
                    _resolverCache.Add(simpleName, result);
                    return result;
                }

                return null;
            }
        }
    }

    public abstract class TestCase : IXunitSerializable
    {
        public string TestName { get; set; }
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public int MetadataToken { get; set; }
        public string ModuleName { get; set; }

        public virtual void Deserialize(IXunitSerializationInfo info)
        {
            TestName = info.GetValue<string>(nameof(TestName));
            TypeName = info.GetValue<string>(nameof(TypeName));
            MethodName = info.GetValue<string>(nameof(MethodName));
            MetadataToken = info.GetValue<int>(nameof(MetadataToken));
            ModuleName = info.GetValue<string>(nameof(ModuleName));
        }

        public virtual void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(TestName), TestName);
            info.AddValue(nameof(TypeName), TypeName);
            info.AddValue(nameof(MethodName), MethodName);
            info.AddValue(nameof(MetadataToken), MetadataToken);
            info.AddValue(nameof(ModuleName), ModuleName);
        }

        public override string ToString()
        {
            return $"[{Path.GetFileNameWithoutExtension(ModuleName)}] {TestName}";
        }
    }

    /// <summary>
    /// Describes a test case with a method that contains valid IL
    /// </summary>
    public class ValidILTestCase : TestCase { }

    /// <summary>
    /// Describes a test case with a method that contains invalid IL with the expected VerifierErrors
    /// </summary>
    public class InvalidILTestCase : TestCase
    {
        public List<VerifierError> ExpectedVerifierErrors { get; set; }

        public override void Serialize(IXunitSerializationInfo info)
        {
            base.Serialize(info);
            var serializedExpectedErrors = JsonConvert.SerializeObject(ExpectedVerifierErrors);
            info.AddValue(nameof(ExpectedVerifierErrors), serializedExpectedErrors);
        }

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);
            var serializedExpectedErrors = info.GetValue<string>(nameof(ExpectedVerifierErrors));
            ExpectedVerifierErrors = JsonConvert.DeserializeObject<List<VerifierError>>(serializedExpectedErrors);
        }

        public override string ToString()
        {
            return base.ToString() + GetErrorsString(ExpectedVerifierErrors);
        }

        private static string GetErrorsString(List<VerifierError> errors)
        {
            if (errors == null || errors.Count <= 0)
                return String.Empty;

            var errorsString = new StringBuilder(" (");

            for (int i = 0; i < errors.Count - 1; ++i)
                errorsString.Append(errors[i]).Append(", ");

            errorsString.Append(errors[errors.Count - 1]);
            errorsString.Append(")");

            return errorsString.ToString();
        }
    }

    public class ValidTypeTestCase : TestCase { }

    public class InvalidTypeTestCase : TestCase
    {
        public List<VerifierError> ExpectedVerifierErrors { get; set; }

        public override void Serialize(IXunitSerializationInfo info)
        {
            base.Serialize(info);
            var serializedExpectedErrors = JsonConvert.SerializeObject(ExpectedVerifierErrors);
            info.AddValue(nameof(ExpectedVerifierErrors), serializedExpectedErrors);
        }

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);
            var serializedExpectedErrors = info.GetValue<string>(nameof(ExpectedVerifierErrors));
            ExpectedVerifierErrors = JsonConvert.DeserializeObject<List<VerifierError>>(serializedExpectedErrors);
        }

        public override string ToString()
        {
            return base.ToString() + GetErrorsString(ExpectedVerifierErrors);
        }

        private static string GetErrorsString(List<VerifierError> errors)
        {
            if (errors == null || errors.Count <= 0)
                return String.Empty;

            var errorsString = new StringBuilder(" (");

            for (int i = 0; i < errors.Count - 1; ++i)
                errorsString.Append(errors[i]).Append(", ");

            errorsString.Append(errors[errors.Count - 1]);
            errorsString.Append(")");

            return errorsString.ToString();
        }
    }
}
