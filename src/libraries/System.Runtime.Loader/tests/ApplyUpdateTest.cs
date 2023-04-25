// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Metadata
{
    ///
    /// The general setup for ApplyUpdate tests is:
    ///
    /// Each test Foo has a corresponding assembly under
    /// System.Reflection.Metadata.ApplyUpate.Test.Foo The Foo.csproj has a delta
    /// script that applies one or more updates to Foo.dll The ApplyUpdateTest
    /// testsuite runs each test in sequence, loading the corresponding
    /// assembly, applying an update to it and observing the results.
    [Collection(nameof(DisableParallelization))]
    public class ApplyUpdateTest
    {
        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54617", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        void StaticMethodBodyUpdate()
        {
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof (ApplyUpdate.Test.MethodBody1).Assembly;

                var r = ApplyUpdate.Test.MethodBody1.StaticMethod1();
                Assert.Equal("OLD STRING", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = ApplyUpdate.Test.MethodBody1.StaticMethod1();
                Assert.Equal("NEW STRING", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = ApplyUpdate.Test.MethodBody1.StaticMethod1 ();
                Assert.Equal ("NEWEST STRING", r);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54617", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))] 
        void LambdaBodyChange()
        {
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof (ApplyUpdate.Test.LambdaBodyChange).Assembly;

                var o = new ApplyUpdate.Test.LambdaBodyChange ();
                var r = o.MethodWithLambda();

                Assert.Equal("OLD STRING", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = o.MethodWithLambda();

                Assert.Equal("NEW STRING", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = o.MethodWithLambda();

                Assert.Equal("NEWEST STRING!", r);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54617", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))] 
        void LambdaCapturesThis()
        {
            // Tests that changes to the body of a lambda that captures 'this' is supported.
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof (ApplyUpdate.Test.LambdaCapturesThis).Assembly;

                var o = new ApplyUpdate.Test.LambdaCapturesThis ();
                var r = o.MethodWithLambda();

                Assert.Equal("OLD STRING", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = o.MethodWithLambda();

                Assert.Equal("NEW STRING", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = o.MethodWithLambda();

                Assert.Equal("NEWEST STRING!", r);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54617", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))] 
        void FirstCallAfterUpdate()
        {
            /* Tests that updating a method that has not been called before works correctly and that
             * the JIT/interpreter doesn't have to rely on cached baseline data. */
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof (ApplyUpdate.Test.FirstCallAfterUpdate).Assembly;

                var o = new ApplyUpdate.Test.FirstCallAfterUpdate ();

                ApplyUpdateUtil.ApplyUpdate(assm);
                ApplyUpdateUtil.ApplyUpdate(assm);

                string r = o.Method1("NEW");

                Assert.Equal("NEWEST STRING", r);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        void ClassWithCustomAttributes()
        {
            ApplyUpdateUtil.TestCase(static () =>
            {
                // Get the custom attribtues from a newly-added type and method
                // and check that they are the expected ones.
                var assm = typeof(ApplyUpdate.Test.ClassWithCustomAttributesHelper).Assembly;

                // returns ClassWithCustomAttributes
                var ty = ApplyUpdate.Test.ClassWithCustomAttributesHelper.GetAttributedClass();
                Assert.NotNull (ty);

                ApplyUpdateUtil.ApplyUpdate(assm);
                ApplyUpdateUtil.ClearAllReflectionCaches();

                // returns ClassWithCustomAttributes2
                ty = ApplyUpdate.Test.ClassWithCustomAttributesHelper.GetAttributedClass();
                Assert.NotNull (ty);

                var attrType = typeof(ObsoleteAttribute);

                var cattrs = Attribute.GetCustomAttributes(ty, attrType);

                Assert.NotNull(cattrs);
                Assert.Equal(1, cattrs.Length);
                Assert.NotNull(cattrs[0]);
                Assert.Equal(attrType, cattrs[0].GetType());

                var methodName = "Method2";
                var mi = ty.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

                Assert.NotNull (mi);

                cattrs = Attribute.GetCustomAttributes(mi, attrType);

                Assert.NotNull(cattrs);
                Assert.Equal(1, cattrs.Length);
                Assert.NotNull(cattrs[0]);
                Assert.Equal(attrType, cattrs[0].GetType());
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        public void CustomAttributeUpdates()
        {
            // Test that _modifying_ custom attribute constructor/property argumments works as expected.
            // For this test, we don't change which constructor is called, or how many custom attributes there are.
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.ClassWithCustomAttributeUpdates).Assembly;

                ApplyUpdateUtil.ApplyUpdate(assm);
                ApplyUpdateUtil.ClearAllReflectionCaches();

                // Just check the updated value on one method

                Type attrType = typeof(System.Reflection.Metadata.ApplyUpdate.Test.MyAttribute);
                Type ty = assm.GetType("System.Reflection.Metadata.ApplyUpdate.Test.ClassWithCustomAttributeUpdates");
                Assert.NotNull(ty);
                MethodInfo mi = ty.GetMethod(nameof(System.Reflection.Metadata.ApplyUpdate.Test.ClassWithCustomAttributeUpdates.Method1), BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(mi);
                var cattrs = Attribute.GetCustomAttributes(mi, attrType);
                Assert.NotNull(cattrs);
                Assert.Equal(1, cattrs.Length);
                Assert.NotNull(cattrs[0]);
                Assert.Equal(attrType, cattrs[0].GetType());
                string p = (cattrs[0] as System.Reflection.Metadata.ApplyUpdate.Test.MyAttribute).StringValue;
                Assert.Equal("rstuv", p);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        public void CustomAttributeDelete()
        {
            // Test that deleting custom attribute on constructor/property works as expected.
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.ClassWithCustomAttributeDelete).Assembly;

                ApplyUpdateUtil.ApplyUpdate(assm);
                ApplyUpdateUtil.ClearAllReflectionCaches();

                // Just check the updated value on one method

                Type attrType = typeof(System.Reflection.Metadata.ApplyUpdate.Test.MyDeleteAttribute);
                Type ty = assm.GetType("System.Reflection.Metadata.ApplyUpdate.Test.ClassWithCustomAttributeDelete");
                Assert.NotNull(ty);

                MethodInfo mi1 = ty.GetMethod(nameof(System.Reflection.Metadata.ApplyUpdate.Test.ClassWithCustomAttributeDelete.Method1), BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(mi1);
                Attribute[] cattrs = Attribute.GetCustomAttributes(mi1, attrType);
                Assert.NotNull(cattrs);
                Assert.Equal(0, cattrs.Length);

                MethodInfo mi2 = ty.GetMethod(nameof(System.Reflection.Metadata.ApplyUpdate.Test.ClassWithCustomAttributeDelete.Method2), BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(mi2);
                cattrs = Attribute.GetCustomAttributes(mi2, attrType);
                Assert.NotNull(cattrs);
                Assert.Equal(0, cattrs.Length);

                MethodInfo mi3 = ty.GetMethod(nameof(System.Reflection.Metadata.ApplyUpdate.Test.ClassWithCustomAttributeDelete.Method3), BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(mi3);
                cattrs = Attribute.GetCustomAttributes(mi3, attrType);
                Assert.NotNull(cattrs);
                Assert.Equal(1, cattrs.Length);
                string p = (cattrs[0] as System.Reflection.Metadata.ApplyUpdate.Test.MyDeleteAttribute).StringValue;
                Assert.Equal("Not Deleted", p);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        public void AsyncMethodChanges()
        {
            // Test that changing an async method doesn't cause any type load exceptions
            ApplyUpdateUtil.TestCase(static () =>
            {
                Assembly assembly = typeof(System.Reflection.Metadata.ApplyUpdate.Test.AsyncMethodChange).Assembly;

                ApplyUpdateUtil.ApplyUpdate(assembly);
                ApplyUpdateUtil.ClearAllReflectionCaches();

                Type ty = typeof(System.Reflection.Metadata.ApplyUpdate.Test.AsyncMethodChange);
                Assert.NotNull(ty);

                MethodInfo mi = ty.GetMethod(nameof(System.Reflection.Metadata.ApplyUpdate.Test.AsyncMethodChange.TestTaskMethod), BindingFlags.Public | BindingFlags.Static);
                Assert.NotNull(mi);

                string result = ApplyUpdate.Test.AsyncMethodChange.TestTaskMethod().GetAwaiter().GetResult();
                Assert.Equal("TestTaskMethod v1", result);

                object[] attributes = mi.GetCustomAttributes(true);
                Assert.NotNull(attributes);
                Assert.True(attributes.Length > 0);

                foreach (var attribute in attributes)
                {
                    if (attribute is AsyncStateMachineAttribute asm)
                    {
                        Assert.Contains("<TestTaskMethod>", asm.StateMachineType.Name);
                    }
                }
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.IsSupported))]
        public static void TestAddLambdaCapturingThis()
        {
            // Test that adding a lambda that captures 'this' (to a method that already has a lambda that captures 'this') is supported
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.AddLambdaCapturingThis).Assembly;

                var x = new System.Reflection.Metadata.ApplyUpdate.Test.AddLambdaCapturingThis();

                Assert.Equal("123", x.TestMethod());

                ApplyUpdateUtil.ApplyUpdate(assm);

                string result = x.TestMethod();
                Assert.Equal("42123abcd", result);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.IsSupported))]
        public static void TestAddStaticField()
        {
            // Test that adding a new static field to an existing class is supported
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.AddStaticField).Assembly;

                var x = new System.Reflection.Metadata.ApplyUpdate.Test.AddStaticField();

                x.TestMethod();

                Assert.Equal ("abcd", x.GetField);

                ApplyUpdateUtil.ApplyUpdate(assm);

                x.TestMethod();

                string result = x.GetField;
                Assert.Equal("4567", result);
            });
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/76702", TestRuntimes.CoreCLR)]
        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.IsSupported))]
        public static void TestAddInstanceField()
        {
            // Test that adding a new instance field to an existing class is supported
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.AddInstanceField).Assembly;

                var x1 = new System.Reflection.Metadata.ApplyUpdate.Test.AddInstanceField();

                x1.TestMethod();

                Assert.Equal ("abcd", x1.GetStringField);
                Assert.Equal (3.14159, x1.GetDoubleField);

                ApplyUpdateUtil.ApplyUpdate(assm);

                x1.TestMethod();

                Assert.Equal ("4567", x1.GetStringField);
                Assert.Equal (0.707106, x1.GetDoubleField);

                Assert.Equal (-1, x1.GetIntArrayLength ()); // new field on existing object is initially null

                var x2 = new System.Reflection.Metadata.ApplyUpdate.Test.AddInstanceField();

                Assert.Equal ("New Initial Value", x2.GetStringField);
                Assert.Equal (6.5, x2.GetDoubleField);

                Assert.Equal (6, x2.GetIntArrayLength());
                Assert.Equal (7, x2.GetIntArrayElt (3));
                
                // now check that reflection can get/set the new fields
                var fi = x2.GetType().GetField("NewStructField");

                Assert.NotNull(fi);

                var s = fi.GetValue (x2);

                Assert.NotNull(x2);

                var fid = fi.FieldType.GetField("D");
                Assert.NotNull(fid);
                Assert.Equal(-1984.0, fid.GetValue(s));
                var tr = TypedReference.MakeTypedReference (x2, new FieldInfo[] {fi});
                fid.SetValueDirect(tr, (object)34567.0);
                Assert.Equal (34567.0, fid.GetValueDirect (tr));

                fi = x2.GetType().GetField("_doubleField2", BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.NotNull(fi);

                fi.SetValue(x2, 65535.01);
                Assert.Equal(65535.01, x2.GetDoubleField);

                tr = __makeref(x2);
                fi.SetValueDirect (tr, 32768.2);
                Assert.Equal (32768.2, x2.GetDoubleField);
                Assert.Equal ((object)32768.2, fi.GetValueDirect (tr));

                Assert.Equal("abcd", x2.GetStringProp);

                var propInfo = x2.GetType().GetProperty("AddedStringAutoProp", BindingFlags.Public | BindingFlags.Instance);

                Assert.NotNull(propInfo);
                Assert.Equal("abcd", propInfo.GetMethod.Invoke (x2, new object[] {}));

                x2.TestMethod();

                Assert.Equal("abcdTest", x2.GetStringProp);

                var addedPropToken = propInfo.MetadataToken;

                Assert.True (addedPropToken > 0);

                // we don't know exactly what token Roslyn will assign to the added property, but
                // since the AddInstanceField.dll assembly is relatively small, assume that the
                // total number of properties in the updated generation is less than 64 and the
                // token is in that range.  If more code is added, revise this test.

                Assert.True ((addedPropToken & 0x00ffffff) < 64);


                var accumResult = x2.FireEvents();

                Assert.Equal (246.0, accumResult);

                var eventInfo = x2.GetType().GetEvent("AddedEvent", BindingFlags.Public | BindingFlags.Instance);

                Assert.NotNull (eventInfo);

                var addedEventToken = eventInfo.MetadataToken;

                Assert.True (addedEventToken > 0);

                // we don't know exactly what token Roslyn will assign to the added event, but
                // since the AddInstanceField.dll assembly is relatively small, assume that the
                // total number of events in the updated generation is less than 4 and the
                // token is in that range.  If more code is added, revise this test.

                Assert.True ((addedEventToken & 0x00ffffff) < 4);
                

            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.IsSupported))]
        public static void TestAddNestedClass()
        {
            // Test that adding a new nested class to an existing class is supported
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.AddNestedClass).Assembly;

                var x = new System.Reflection.Metadata.ApplyUpdate.Test.AddNestedClass();

                var r = x.TestMethod();

                Assert.Equal ("123", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = x.TestMethod();

                Assert.Equal("123456789", r);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.IsSupported))]
        public static void TestAddStaticLambda()
        {
            // Test that adding a new static lambda to an existing method body is supported
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.AddStaticLambda).Assembly;

                var x = new System.Reflection.Metadata.ApplyUpdate.Test.AddStaticLambda();

                var r = x.TestMethod();

                Assert.Equal ("abcd", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = x.TestMethod();

                Assert.Equal("abcd1abcd", r);
            });
        }

        class NonRuntimeAssembly : Assembly
        {
        }

        [Fact]
        public static void ApplyUpdateInvalidParameters()
        {
            // Dummy delta arrays
            var metadataDelta = new byte[20];
            var ilDelta = new byte[20];

            // Assembly can't be null
            Assert.Throws<ArgumentNullException>("assembly", () =>
                MetadataUpdater.ApplyUpdate(null, new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));

            // Tests fail on non-runtime assemblies
            Assert.Throws<ArgumentException>(() =>
                MetadataUpdater.ApplyUpdate(new NonRuntimeAssembly(), new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));

            // Tests that this assembly isn't not editable
            Assert.Throws<InvalidOperationException>(() =>
                MetadataUpdater.ApplyUpdate(typeof(AssemblyExtensions).Assembly, new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void GetCapabilities()
        {
            var ty = typeof(System.Reflection.Metadata.MetadataUpdater);
            var mi = ty.GetMethod("GetCapabilities", BindingFlags.NonPublic | BindingFlags.Static, Array.Empty<Type>());

            Assert.NotNull(mi);

            var result = mi.Invoke(null, null);

            Assert.NotNull(result);
            Assert.Equal(typeof(string), result.GetType());
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.TestUsingRemoteExecutor))]
        public static void IsSupported()
        {
            bool result = MetadataUpdater.IsSupported;
            Assert.False(result);
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.IsSupported))]
        public static void TestStaticLambdaRegression()
        {
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.StaticLambdaRegression).Assembly;
                var x = new System.Reflection.Metadata.ApplyUpdate.Test.StaticLambdaRegression();

                Assert.Equal (0, x.count);

                x.TestMethod();
                x.TestMethod();

                Assert.Equal (2, x.count);

                ApplyUpdateUtil.ApplyUpdate(assm, usePDB: false);

                x.TestMethod();
                x.TestMethod();

                Assert.Equal (4, x.count);

                ApplyUpdateUtil.ApplyUpdate(assm, usePDB: false);

                x.TestMethod();
                x.TestMethod();

                Assert.Equal (6, x.count);

            });
        }

        private static bool ContainsTypeWithName(Type[] types, string fullName)
        {
            foreach (var ty in types) {
                if (ty.FullName == fullName)
                    return true;
            }
            return false;
        }

        internal static Type CheckReflectedType(Assembly assm, Type[] allTypes, string nameSpace, string typeName, Action<Type> moreChecks = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var fullName = $"{nameSpace}.{typeName}";
            var ty = assm.GetType(fullName);
            Assert.True(ty != null, $"{callerFilePath}:{callerLineNumber}: expected Assembly.GetType for '{typeName}' to return non-null in {callerMemberName}");
            int nestedIdx = typeName.LastIndexOf('+');
            string comparisonName = typeName;
            if (nestedIdx != -1)
                comparisonName = typeName.Substring(nestedIdx+1);
            Assert.True(comparisonName == ty.Name, $"{callerFilePath}:{callerLineNumber}: returned type has unexpected name '{ty.Name}' (expected: '{comparisonName}') in {callerMemberName}");
            Assert.True(ContainsTypeWithName (allTypes, fullName), $"{callerFilePath}:{callerLineNumber}: expected Assembly.GetTypes to contain '{fullName}', but it didn't in {callerMemberName}");
            if (moreChecks != null)
                moreChecks(ty);
            return ty;
        }


        internal static void CheckCustomNoteAttribute(MemberInfo subject, string expectedAttributeValue, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            var attrData = subject.GetCustomAttributesData();
            CustomAttributeData noteData = null;
            foreach (var cad in attrData)
            {
                if (cad.AttributeType.FullName.Contains("CustomNoteAttribute"))
                    noteData = cad;
            }
            Assert.True(noteData != null, $"{callerFilePath}:{callerLineNumber}: expected a CustomNoteAttribute attributes on '{subject.Name}', but got null, in {callerMemberName}");
            Assert.True(1 == noteData.ConstructorArguments.Count, $"{callerFilePath}:{callerLineNumber}: expected exactly 1 constructor argument on CustomNoteAttribute, got {noteData.ConstructorArguments.Count}, in {callerMemberName}");
            object argVal = noteData.ConstructorArguments[0].Value;
            Assert.True(expectedAttributeValue.Equals(argVal), $"{callerFilePath}:{callerLineNumber}: expected '{expectedAttributeValue}' as CustomNoteAttribute argument, got '{argVal}', in {callerMemberName}");

            var attrs = subject.GetCustomAttributes(false);
            object note = null;
            foreach (var attr in attrs)
            {
                if (attr.GetType().FullName.Contains("CustomNoteAttribute"))
                    note = attr;
            }
            Assert.True(note != null, $"{callerFilePath}:{callerLineNumber}: expected a CustomNoteAttribute object on '{subject.Name}', but got null, in {callerMemberName}");
            object v = note.GetType().GetField("Note").GetValue(note);
            Assert.True(expectedAttributeValue.Equals(v), $"{callerFilePath}:{callerLineNumber}: expected '{expectedAttributeValue}' in CustomNoteAttribute Note field, but got '{v}', in {callerMemberName}");
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.IsSupported))]
        public static void TestReflectionAddNewType()
        {
            ApplyUpdateUtil.TestCase(static () =>
            {
                const string ns = "System.Reflection.Metadata.ApplyUpdate.Test.ReflectionAddNewType";
                var assm = typeof(System.Reflection.Metadata.ApplyUpdate.Test.ReflectionAddNewType.ZExistingClass).Assembly;

                var allTypes = assm.GetTypes();

                CheckReflectedType(assm, allTypes, ns, "ZExistingClass");
                CheckReflectedType(assm, allTypes, ns, "ZExistingClass+PreviousNestedClass");

                ApplyUpdateUtil.ApplyUpdate(assm);

                allTypes = assm.GetTypes();

                CheckReflectedType(assm, allTypes, ns, "ZExistingClass", static (ty) =>
                {
                    var allMethods = ty.GetMethods();

                    MethodInfo newMethod = null;
                    foreach (var meth in allMethods)
                    {
                        if (meth.Name == "NewMethod")
                            newMethod = meth;
                    }
                    Assert.NotNull (newMethod);

                    Assert.Equal (newMethod, ty.GetMethod ("NewMethod"));

                    var allFields = ty.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                    // Mono doesn't do instance fields yet
#if false
                    FieldInfo newField = null;
#endif
                    FieldInfo newStaticField = null;
                    foreach (var fld in allFields)
                    {
#if false
                        if (fld.Name == "NewField")
                            newField = fld;
#endif
                        if (fld.Name == "NewStaticField")
                            newStaticField = fld;
                    }
#if false
                    Assert.NotNull(newField);
                    Assert.Equal(newField, ty.GetField("NewField"));
#endif

                    Assert.NotNull(newStaticField);
                    Assert.Equal(newStaticField, ty.GetField("NewStaticField", BindingFlags.Static | BindingFlags.Public));

                });
                CheckReflectedType(assm, allTypes, ns, "ZExistingClass+PreviousNestedClass");
                CheckReflectedType(assm, allTypes, ns, "IExistingInterface");

                CheckReflectedType(assm, allTypes, ns, "ZExistingClass+NewNestedClass");

                var newTy = CheckReflectedType(assm, allTypes, ns, "NewToplevelClass", static (ty) =>
                {
                    CheckCustomNoteAttribute(ty, "123");

                    var nested = ty.GetNestedType("AlsoNested");
                    var allNested = ty.GetNestedTypes();

                    Assert.Equal("AlsoNested", nested.Name);
                    Assert.Same(ty, nested.DeclaringType);

                    Assert.Equal(1, allNested.Length);
                    Assert.Same(nested, allNested[0]);

                    var allInterfaces = ty.GetInterfaces();

                    Assert.Equal (2, allInterfaces.Length);
                    bool hasICloneable = false, hasINewInterface = false;
                    for (int i = 0; i < allInterfaces.Length; ++i) {
                        var itf = allInterfaces[i];
                        if (itf.Name == "ICloneable")
                            hasICloneable = true;
                        if (itf.Name == "IExistingInterface")
                            hasINewInterface = true;
                    }
                    Assert.True(hasICloneable);
                    Assert.True(hasINewInterface);

                    var allProperties = ty.GetProperties();

                    PropertyInfo newProp = null;
                    foreach (var prop in allProperties)
                    {
                        if (prop.Name == "NewProp")
                            newProp = prop;
                    }
                    Assert.NotNull(newProp);

                    Assert.Equal(newProp, ty.GetProperty("NewProp"));
                    MethodInfo newPropGet = newProp.GetGetMethod();
                    Assert.NotNull(newPropGet);
                    MethodInfo newPropSet = newProp.GetSetMethod();
                    Assert.NotNull(newPropSet);

                    Assert.Equal("get_NewProp", newPropGet.Name);

                    CheckCustomNoteAttribute (newProp, "hijkl");

                    var allEvents = ty.GetEvents();

                    EventInfo newEvt = null;
                    foreach (var evt in allEvents)
                    {
                        if (evt.Name == "NewEvent")
                            newEvt = evt;
                    }
                    Assert.NotNull(newEvt);

                    Assert.Equal(newEvt, ty.GetEvent("NewEvent"));
                    MethodInfo newEvtAdd = newEvt.GetAddMethod();
                    Assert.NotNull(newEvtAdd);
                    MethodInfo newEvtRemove = newEvt.GetRemoveMethod();
                    Assert.NotNull(newEvtRemove);

                    Assert.Equal("add_NewEvent", newEvtAdd.Name);
                });
                CheckReflectedType(assm, allTypes, ns, "NewGenericClass`1");
                CheckReflectedType(assm, allTypes, ns, "NewToplevelStruct");
                CheckReflectedType(assm, allTypes, ns, "INewInterface");
                CheckReflectedType(assm, allTypes, ns, "NewEnum", static (ty) => {
                    var names = Enum.GetNames (ty);
                    Assert.Equal(3, names.Length);
                    var vals = Enum.GetValues (ty);
                    Assert.Equal(3, vals.Length);

                    Assert.NotNull(Enum.Parse (ty, "Red"));
                    Assert.NotNull(Enum.Parse (ty, "Yellow"));
                });

                // make some instances using reflection and use them through known interfaces
                var o = Activator.CreateInstance(newTy);

                var i = (System.Reflection.Metadata.ApplyUpdate.Test.ReflectionAddNewType.IExistingInterface)o;

                Assert.Equal("123", i.ItfMethod(123));

                System.Reflection.Metadata.ApplyUpdate.Test.ReflectionAddNewType.ZExistingClass.ExistingMethod ();
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.IsSupported))]
        public static void TestReflectionAddNewMethod()
        {
            ApplyUpdateUtil.TestCase(static () =>
            {
                var ty = typeof(System.Reflection.Metadata.ApplyUpdate.Test.ReflectionAddNewMethod);
                var assm = ty.Assembly;

		var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
                var allMethods = ty.GetMethods(bindingFlags);

                int objectMethods = typeof(object).GetMethods(bindingFlags).Length;
                Assert.Equal (objectMethods + 1, allMethods.Length);

                ApplyUpdateUtil.ApplyUpdate(assm);
                ApplyUpdateUtil.ClearAllReflectionCaches();

                ty = typeof(System.Reflection.Metadata.ApplyUpdate.Test.ReflectionAddNewMethod);

                allMethods = ty.GetMethods(bindingFlags);
                Assert.Equal (objectMethods + 2, allMethods.Length);

                var mi = ty.GetMethod ("AddedNewMethod");

                Assert.NotNull (mi);

                var retParm = mi.ReturnParameter;
                Assert.NotNull (retParm);
                Assert.NotNull (retParm.ParameterType);
                Assert.Equal (-1, retParm.Position);

                var retCas = retParm.GetCustomAttributes(false);
                Assert.NotNull(retCas);
                Assert.Equal(0, retCas.Length);

                var parms = mi.GetParameters();
                Assert.Equal (5, parms.Length);

                int parmPos = 0;
                foreach (var parm in parms)
                {
                    Assert.NotNull(parm);
                    Assert.NotNull(parm.ParameterType);
                    Assert.Equal(parmPos, parm.Position);
                    Assert.NotNull(parm.Name);
                    
                    var cas = parm.GetCustomAttributes(false);
                    foreach (var ca in cas) {
                        Assert.NotNull (ca);
                    }

                    parmPos++;
                }

		var parmAttrs = parms[4].GetCustomAttributes(false);
                Assert.Equal (2, parmAttrs.Length);
		bool foundCallerMemberName = false;
		bool foundOptional = false;
		foreach (var pa in parmAttrs) {
		    if (typeof (CallerMemberNameAttribute).Equals(pa.GetType()))
		    {
			foundCallerMemberName = true;
		    }
		    if (typeof (OptionalAttribute).Equals(pa.GetType()))
		    {
			foundOptional = true;
		    }
		}
		Assert.True(foundCallerMemberName);
		Assert.True(foundOptional);

		// n.b. this typeof() also makes the rest of the test work on Wasm with aggressive trimming.
		Assert.Equal (typeof(System.Threading.CancellationToken), parms[3].ParameterType);

                Assert.True(parms[3].HasDefaultValue);
		Assert.True(parms[4].HasDefaultValue);

		Assert.Null(parms[3].DefaultValue);
		Assert.Equal(string.Empty, parms[4].DefaultValue);
            });
	} 
    }
}
