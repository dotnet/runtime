// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SampleMetadata;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Reflection.Tests
{
    internal static partial class TestUtils
    {
        public static int GetMark(this MemberInfo m)
        {
            Type markAttributeType = typeof(MarkAttribute).Project();
            CustomAttributeData cad = m.CustomAttributes.Single(ca => ca.AttributeType == markAttributeType);
            return (int)(cad.ConstructorArguments[0].Value);
        }

        private sealed class TestCustomAttributeData : CustomAttributeData
        {
            private readonly ConstructorInfo _constructor;
            private readonly IList<CustomAttributeTypedArgument> _cats;
            private readonly IList<CustomAttributeNamedArgument> _cans;

            public TestCustomAttributeData(ConstructorInfo constructor, IList<CustomAttributeTypedArgument> cats, IList<CustomAttributeNamedArgument> cans)
            {
                _constructor = constructor;
                _cats = cats;
                _cans = cans;
            }

            public sealed override ConstructorInfo Constructor => _constructor;
            public sealed override IList<CustomAttributeTypedArgument> ConstructorArguments => _cats;
            public sealed override IList<CustomAttributeNamedArgument> NamedArguments => _cans;
        }

        public static T UnprojectAndInstantiate<T>(this CustomAttributeData cad) where T : Attribute
        {
            CustomAttributeData runtimeCad = cad.ProjectBackToRuntime();
            return runtimeCad.Instantiate<T>();
        }

        private static CustomAttributeData ProjectBackToRuntime(this CustomAttributeData cad)
        {
            if (cad.AttributeType.IsImplementedByRuntime())
                return cad;

            Type runtimeAttributeType = cad.AttributeType.ProjectBackToRuntime();

            ConstructorInfo ecmaConstructor = cad.Constructor;
            Type[] parameterTypes = ecmaConstructor.GetParameters().Select(p => p.ParameterType.ProjectBackToRuntime()).ToArray();
            const BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.ExactBinding;
            ConstructorInfo runtimeConstructor = runtimeAttributeType.GetConstructor(bf, null, parameterTypes, null);
            Assert.NotNull(runtimeConstructor);

            IList<CustomAttributeTypedArgument> runtimeCats = cad.ConstructorArguments.Select(ct => ct.ProjectBackToRuntime()).ToArray();
            IList<CustomAttributeNamedArgument> runtimeCans = cad.NamedArguments.Select(cn => cn.ProjectBackToRuntime()).ToArray();

            return new TestCustomAttributeData(runtimeConstructor, runtimeCats, runtimeCans);
        }

        private static CustomAttributeTypedArgument ProjectBackToRuntime(this CustomAttributeTypedArgument cat)
        {
            Type ecmaArgumentType = cat.ArgumentType;
            if (ecmaArgumentType.IsImplementedByRuntime())
                return cat;

            Type runtimeArgumentType = ecmaArgumentType.ProjectBackToRuntime();
            if (runtimeArgumentType == typeof(Type))
                return new CustomAttributeTypedArgument(typeof(Type), cat.Value == null ? null : ((Type)cat.Value).ProjectBackToRuntime());

            if (runtimeArgumentType.IsArray)
                throw new Exception("Arrays not supported."); // Add if it's useful.

            return new CustomAttributeTypedArgument(runtimeArgumentType, cat.Value);
        }

        private static CustomAttributeNamedArgument ProjectBackToRuntime(this CustomAttributeNamedArgument can)
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            Type runtimeDeclaringType = can.MemberInfo.DeclaringType.ProjectBackToRuntime();
            MemberInfo runtimeMember = can.IsField ?
                (MemberInfo)runtimeDeclaringType.GetField(can.MemberName, bf) :
                (MemberInfo)runtimeDeclaringType.GetProperty(can.MemberName, bf);
            return new CustomAttributeNamedArgument(runtimeMember, can.TypedValue.ProjectBackToRuntime());
        }

        private static Type ProjectBackToRuntime(this Type type)
        {
            if (type.FullName.StartsWith("System."))
            {
                // Assume it's from the core assembly.
                return typeof(object).Assembly.GetType(type.FullName, throwOnError: true);
            }
            else
            {
                // Assume it's from SampleMetadata.
                return typeof(SampleMetadata.Base1).Assembly.GetType(type.FullName, throwOnError: true);
            }
        }

        internal static byte[] HexToByteArray(this string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string s = hexString.Substring(i, 2);
                bytes[i / 2] = byte.Parse(s, NumberStyles.HexNumber, null);
            }

            return bytes;
        }

        internal static string ByteArrayToHex(this byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append($"{bytes[i]:X2}");
            }

            return builder.ToString();
        }

        internal static byte[] ToArray(this Stream s)
        {
            int count = checked((int)(s.Length));
            byte[] buffer = new byte[count];
            int index = 0;
            while (index < count)
            {
                int numRead = s.Read(buffer, index, count - index);
                index += numRead;
            }
            return buffer;
        }

        // On netstandard builds, can't use t.GetTypeInfo().GenericTypeParameters but I don't want
        // to spread GetGenericArguments() calls all over so we do it once in this helper.
        public static Type[] GetGenericTypeParameters(this Type t)
        {
            return t.GetGenericArguments();
        }

        public static bool IsImplementedByRuntime(this MemberInfo m) => m.GetType().Assembly == typeof(object).Assembly;

        public static void AssertNewObjectReturnedEachTime<T>(Func<IEnumerable<T>> action) where T : class
        {
            IEnumerable<T> r1 = action();
            IEnumerable<T> r2 = action();

            if (r1 == null && r2 == null)
                return;

            if (r1.Any())
            {
                Assert.NotSame(r1, r2);
            }

            // Don't use xunit's sequence Equality assert here - we want to exercise our object's Equals() methods and not bypass because
            // of a passing reference equality check.
            T[] a1 = r1.ToArray();
            T[] a2 = r2.ToArray();
            Assert.Equal(a1.Length, a2.Length);
            for (int i = 0; i < a1.Length; i++)
            {
                T t1 = a1[i];
                T t2 = a2[i];

                if (t1 == null && t2 == null)
                    continue;

                if (t1.Equals(t2))
                    continue;

                if (t2.Equals(t1))
                    continue;

                Assert.Fail("Expected Equals() to return true: " + t1 + ", " + t2);
            }
        }

        public static void Dump(this IEnumerable<CustomAttributeData> cads, int indent = 0)
        {
            cads = cads.OrderBy(cad => cad.AttributeType.FullName).ToArray();
            foreach (CustomAttributeData cad in cads)
            {
                cad.Dump(indent);
            }
        }

        public static void Dump(this CustomAttributeData cad, int indent = 0)
        {
            string si = new string(' ', indent);
            Console.WriteLine(si + "AttributeType...: " + cad.AttributeType.FullName);
            Console.WriteLine(si + "Constructor.....: " + cad.Constructor.ToString() + " (" + cad.Constructor.DeclaringType.Name + ")");
            Console.WriteLine(si + "Constructor Arguments...:");
            foreach (CustomAttributeTypedArgument cta in cad.ConstructorArguments)
            {
                cta.Dump(indent + 4);
            }
            Console.WriteLine(si + "Named Arguments.........:");
            foreach (CustomAttributeNamedArgument cna in cad.NamedArguments.OrderBy(can => can.MemberName))
            {
                Console.WriteLine(si + "   MemberName....: " + cna.MemberName);
                Console.WriteLine(si + "    IsField.......: " + cna.IsField);
                cna.TypedValue.Dump(indent + 4);
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public static void Dump(this CustomAttributeTypedArgument cta, int indent = 0)
        {
            string si = new string(' ', indent);
            Console.WriteLine(si + "ArgumentType..: " + cta.ArgumentType.Name);
            if (!(cta.Value is IList<CustomAttributeTypedArgument> ctas))
            {
                Console.Write(si + "Value.........: " + cta.Value);
                if (cta.Value != null)
                    Console.Write(" (" + cta.Value.GetType().Name + ")");
                Console.WriteLine();
            }
            else
            {
                for (int i = 0; i < ctas.Count; i++)
                {
                    ctas[i].Dump(indent + 4);
                }
            }
        }

        // For the tests that exercise Type objects and friends, we'll use a single MetadataLoadContext to keep the test methods from being
        // bound too tightly to the fact that the Types are coming from a MetadataLoadContext.
        //
        // This is a simple MetadataLoadContext designed to do the minimal work to make test assembly work across various runtime scenarios.
        // The assumptions are that our test metadata only references the basic System.Runtime types. To keep test environment issues
        // from being a road block, we'll redirect any reference to "mscorlib", "System.Runtime", etc. to our designed core assembly.
        //
        private static MetadataLoadContext TestMetadataLoadContext => s_lazyTestMetadataLoadContext.Value;
        private static readonly Lazy<MetadataLoadContext> s_lazyTestMetadataLoadContext = new Lazy<MetadataLoadContext>(() =>
        {
            return new MetadataLoadContext(new SimpleAssemblyResolver());
        });

        private static readonly Lazy<bool> s_useRuntimeTypesForTests = new Lazy<bool>(() =>
        {
            if (PlatformDetection.IsBrowser)
                return false;

            var loc = AssemblyPathHelper.GetAssemblyLocation(typeof(TestUtils).Assembly);

            if (File.Exists(Path.Combine(loc, "UseRuntimeTypes.txt")))
            {
                // Disable projection so that are Reflection tests run against the runtime types. This is used primarily to verify
                // the *test* code for correctness.
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("! Running Reflection tests on runtime types. They should pass but the results say nothing about MetadataLoadContext correctness.");
                Console.ResetColor();
                return true;
            }
            return false;
        });
    }
}
