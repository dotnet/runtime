// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Tests;
using Microsoft.CSharp;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CSharpCodeGenerationTests : CodeGenerationTests
    {
        protected override CodeDomProvider GetProvider() => CodeDomProvider.CreateProvider("cs");

        protected override string GetEmptyProgramSource() => "class Program { static void Main() { } }";

        [Fact]
        public void Provider_Ctor()
        {
            Assert.Equal("cs", new CSharpCodeProvider().FileExtension);
            Assert.Equal("cs", new CSharpCodeProvider(new Dictionary<string, string>()).FileExtension);
            AssertExtensions.Throws<ArgumentNullException>("providerOptions", () => new CSharpCodeProvider(null));
        }

        [Fact]
        public void ClassWithInstanceFields()
        {
            var cd = new CodeTypeDeclaration("SomeClass") { IsClass = true };
            cd.Members.Add(new CodeMemberField(typeof(int), "_privateNumber") { Attributes = MemberAttributes.Private });
            cd.Members.Add(new CodeMemberField(typeof(string), "_internalString") { Attributes = MemberAttributes.Assembly });
            cd.Members.Add(new CodeMemberField(typeof(DateTime), "_protectedDateTime") { Attributes = MemberAttributes.Family });
            cd.Members.Add(new CodeMemberField(typeof(TimeSpan), "PublicTimeSpan") { Attributes = MemberAttributes.Public });
            cd.Members.Add(new CodeMemberField(typeof(Guid), "_protectedInternalGuid") { Attributes = MemberAttributes.FamilyOrAssembly });

            AssertEqual(cd,
                @"public class SomeClass {
                      private int _privateNumber;
                      internal string _internalString;
                      protected System.DateTime _protectedDateTime;
                      public System.TimeSpan PublicTimeSpan;
                      protected internal System.Guid _protectedInternalGuid;
                  }");
        }

        [Fact]
        public void CodeMemberField_OpenGenericType_Works()
        {
            var cd = new CodeTypeDeclaration("SomeClass") { IsClass = true };
            cd.Members.Add(new CodeMemberField(typeof(List<>), "_field"));

            AssertEqual(cd,
                @"public class SomeClass {
                    private System.Collections.Generic.List<> _field;
                }");
        }

        [Fact]
        public void CodeMemberField_PointerType_Works()
        {
            var cd = new CodeTypeDeclaration("SomeClass") { IsClass = true };
            cd.Members.Add(new CodeMemberField(typeof(int*), "_field"));

            AssertEqual(cd,
                @"public class SomeClass {
                    private System.Int32* _field;
                }");
        }

        [Fact]
        public void CodeMemberField_ByRefType_Works()
        {
            var cd = new CodeTypeDeclaration("SomeClass") { IsClass = true };
            cd.Members.Add(new CodeMemberField(typeof(int).MakeByRefType(), "_field"));

            AssertEqual(cd,
                @"public class SomeClass {
                    private System.Int32& _field;
                }");
        }

        [Fact]
        public void ClassWithStaticFields()
        {
            var cd = new CodeTypeDeclaration("SomeClass") { IsClass = true };
            cd.Members.Add(new CodeMemberField(typeof(int), "s_privateNumber") { Attributes = MemberAttributes.Private | MemberAttributes.Static });
            cd.Members.Add(new CodeMemberField(typeof(string), "s_internalString") { Attributes = MemberAttributes.Assembly | MemberAttributes.Static });
            cd.Members.Add(new CodeMemberField(typeof(DateTime), "s_protectedDateTime") { Attributes = MemberAttributes.Family | MemberAttributes.Static });
            cd.Members.Add(new CodeMemberField(typeof(TimeSpan), "PublicTimeSpan") { Attributes = MemberAttributes.Public | MemberAttributes.Static });
            cd.Members.Add(new CodeMemberField(typeof(Guid), "s_protectedInternalGuid") { Attributes = MemberAttributes.FamilyOrAssembly | MemberAttributes.Static });

            AssertEqual(cd,
                @"public class SomeClass {
                      private static int s_privateNumber;
                      internal static string s_internalString;
                      protected static System.DateTime s_protectedDateTime;
                      public static System.TimeSpan PublicTimeSpan;
                      protected internal static System.Guid s_protectedInternalGuid;
                  }");
        }

        [Fact]
        public void AccessingFields()
        {
            var cd = new CodeTypeDeclaration("ClassWithFields") { IsClass = true };

            var field = new CodeMemberField("System.String", "Microsoft");
            field.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            field.InitExpression = new CodePrimitiveExpression("hi");
            cd.Members.Add(field);

            field = new CodeMemberField();
            field.Name = "StaticPublicField";
            field.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            field.Type = new CodeTypeReference(typeof(int));
            field.InitExpression = new CodePrimitiveExpression(5);
            cd.Members.Add(field);

            field = new CodeMemberField();
            field.Name = "NonStaticPublicField";
            field.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            field.Type = new CodeTypeReference(typeof(int));
            field.InitExpression = new CodePrimitiveExpression(6);
            cd.Members.Add(field);

            field = new CodeMemberField();
            field.Name = "PrivateField";
            field.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            field.Type = new CodeTypeReference(typeof(int));
            field.InitExpression = new CodePrimitiveExpression(7);
            cd.Members.Add(field);

            var cmm = new CodeMemberMethod();
            cmm.Name = "UsePrivateField";
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "i"));
            cmm.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "PrivateField"), new CodeVariableReferenceExpression("i")));
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "PrivateField")));
            cd.Members.Add(cmm);

            AssertEqual(cd,
                @"public class ClassWithFields {
                      public static string Microsoft = ""hi"";
                      public static int StaticPublicField = 5;
                      public int NonStaticPublicField = 6;
                      private int PrivateField = 7;
                      public int UsePrivateField(int i) {
                          this.PrivateField = i;
                          return this.PrivateField;
                      }
                  }");
        }

        [Fact]
        public void OperatingOnFields()
        {
            var cd = new CodeTypeDeclaration("TestFields") { IsClass = true };

            var cmm = new CodeMemberMethod();
            cmm.Name = "UseFields";
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "i"));
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("ClassWithFields"), "number", new CodeObjectCreateExpression("ClassWithFields")));
            var binaryOpExpression = new CodeBinaryOperatorExpression(
                new CodeFieldReferenceExpression(
                    new CodeVariableReferenceExpression("number"), "NonStaticPublicField"), CodeBinaryOperatorType.Add,
                    new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("number"), "UsePrivateField", new CodeVariableReferenceExpression("i")));
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                binaryOpExpression, CodeBinaryOperatorType.Add, new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("ClassWithFields"), "StaticPublicField"))));
            cd.Members.Add(cmm);

            AssertEqual(cd,
                @"public class TestFields
                  {
                      public static int UseFields(int i)
                      {
                          ClassWithFields number = new ClassWithFields();
                          return ((number.NonStaticPublicField + number.UsePrivateField(i)) + ClassWithFields.StaticPublicField);
                      }
                  }");
        }

        [Fact]
        public void CallingMethods()
        {
            var ns = new CodeNamespace("MyNamespace");
            var cd = new CodeTypeDeclaration("TEST") { IsClass = true };

            var cmm = new CodeMemberMethod();
            cmm.Name = "CallingOverrideScenario";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "i"));
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeVariableDeclarationStatement("ClassWVirtualMethod", "t", new CodeObjectCreateExpression("ClassWOverrideMethod")));
            CodeMethodInvokeExpression methodinvoke = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("t"), "VirtualMethod");
            methodinvoke.Parameters.Add(new CodeVariableReferenceExpression("i"));
            cmm.Statements.Add(new CodeMethodReturnStatement(methodinvoke));
            cd.Members.Add(cmm);

            // declare a method without parameters
            cmm = new CodeMemberMethod();
            cmm.Name = "NoParamsMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(16)));
            cd.Members.Add(cmm);

            // declare a method with multiple parameters
            cmm = new CodeMemberMethod();
            cmm.Name = "MultipleParamsMethod";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "b"));
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(new
                CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.Add,
                new CodeVariableReferenceExpression("b"))));
            cd.Members.Add(cmm);

            // call method with no parameters, call a method with multiple parameters,
            // and call a method from a method call
            cmm = new CodeMemberMethod();
            cmm.Name = "CallParamsMethods";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("TEST"), "t", new CodeObjectCreateExpression("TEST")));
            CodeVariableReferenceExpression cvre = new CodeVariableReferenceExpression(); //To increase code coverage
            cvre.VariableName = "t";
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(cvre,
                "MultipleParamsMethod", new CodePrimitiveExpression(78),
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("t"), "NoParamsMethod"))));
            cd.Members.Add(cmm);

            // method to test the 'new' scenario by calling the 'new' method
            cmm = new CodeMemberMethod();
            cmm.Name = "CallingNewScenario";
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "i"));
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeVariableDeclarationStatement("ClassWVirtualMethod", "t", new CodeObjectCreateExpression("ClassWNewMethod")));
            methodinvoke = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("t"), "VirtualMethod");
            methodinvoke.Parameters.Add(new CodeVariableReferenceExpression("i"));
            CodeMethodInvokeExpression methodinvoke2 = new CodeMethodInvokeExpression(new CodeCastExpression("ClassWNewMethod", new
                CodeVariableReferenceExpression("t")), "VirtualMethod");
            methodinvoke2.Parameters.Add(new CodeVariableReferenceExpression("i"));
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                methodinvoke2, CodeBinaryOperatorType.Subtract, methodinvoke)));
            cd.Members.Add(cmm);

            // first declare a class with a virtual method in it
            cd = new CodeTypeDeclaration("ClassWVirtualMethod");
            cd.IsClass = true;
            ns.Types.Add(cd);
            cmm = new CodeMemberMethod();
            cmm.Name = "VirtualMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
            cmm.Attributes = MemberAttributes.Public;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
            cd.Members.Add(cmm);

            // now declare a class that inherits from the previous class and has a 'new' method with the
            cd = new CodeTypeDeclaration("ClassWNewMethod");
            cd.BaseTypes.Add(new CodeTypeReference("ClassWVirtualMethod"));
            cd.IsClass = true;
            ns.Types.Add(cd);
            cmm = new CodeMemberMethod();
            cmm.Name = "VirtualMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.New;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                new CodePrimitiveExpression(2), CodeBinaryOperatorType.Multiply, new CodeVariableReferenceExpression("a"))));
            cd.Members.Add(cmm);

            // now declare a class that inherits from the previous class and has a 'new' method with the
            cd = new CodeTypeDeclaration("ClassWOverrideMethod");
            cd.BaseTypes.Add(new CodeTypeReference("ClassWVirtualMethod"));
            cd.IsClass = true;
            ns.Types.Add(cd);
            cmm = new CodeMemberMethod();
            cmm.Name = "VirtualMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                new CodePrimitiveExpression(2), CodeBinaryOperatorType.Multiply, new CodeVariableReferenceExpression("a"))));
            cd.Members.Add(cmm);

            // new class which will include both functions
            cd = new CodeTypeDeclaration("TEST7");
            cd.IsClass = true;
            ns.Types.Add(cd);
            cmm = new CodeMemberMethod();
            cmm.Name = "OverloadedMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
            cd.Members.Add(cmm);
            cmm = new CodeMemberMethod();
            cmm.Name = "OverloadedMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "b"));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("b"), CodeBinaryOperatorType.Add,
                new CodeVariableReferenceExpression("a"))));
            cd.Members.Add(cmm);

            // declare a method that will call both OverloadedMethod functions
            cmm = new CodeMemberMethod();
            cmm.Name = "CallingOverloadedMethods";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "i"));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeMethodReferenceExpression methodref = new CodeMethodReferenceExpression();
            methodref.MethodName = "OverloadedMethod";
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                new CodeMethodInvokeExpression(methodref, new
                CodeVariableReferenceExpression("i"), new CodeVariableReferenceExpression("i"))
                , CodeBinaryOperatorType.Subtract, new CodeMethodInvokeExpression(methodref, new
                CodeVariableReferenceExpression("i")))));
            cd.Members.Add(cmm);

            AssertEqual(ns,
                @"namespace MyNamespace {
                      public class ClassWVirtualMethod {
                          public virtual int VirtualMethod(int a) {
                              return a;
                          }
                      }

                      public class ClassWNewMethod : ClassWVirtualMethod {
                          public new virtual int VirtualMethod(int a) {
                              return (2 * a);
                          }
                      }

                      public class ClassWOverrideMethod : ClassWVirtualMethod {
                          public override int VirtualMethod(int a) {
                              return (2 * a);
                          }
                      }

                      public class TEST7 {
                          public static int OverloadedMethod(int a) {
                              return a;
                          }

                          public static int OverloadedMethod(int a, int b) {
                              return (b + a);
                          }

                          public static int CallingOverloadedMethods(int i) {
                              return (OverloadedMethod(i, i) - OverloadedMethod(i));
                          }
                      }
                  }");
        }

        [Fact]
        public void MethodWithRefParameter()
        {
            var cd = new CodeTypeDeclaration("TEST");
            cd.IsClass = true;

            var cmm = new CodeMemberMethod();
            cmm.Name = "Work";
            cmm.ReturnType = new CodeTypeReference("System.void");
            cmm.Attributes = MemberAttributes.Static;
            // add parameter with ref direction
            CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "i");
            param.Direction = FieldDirection.Ref;
            cmm.Parameters.Add(param);
            // add parameter with out direction
            param = new CodeParameterDeclarationExpression(typeof(int), "j");
            param.Direction = FieldDirection.Out;
            cmm.Parameters.Add(param);
            cmm.Statements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression("i"),
                new CodeBinaryOperatorExpression(new CodeArgumentReferenceExpression("i"),
                CodeBinaryOperatorType.Add, new CodePrimitiveExpression(4))));
            cmm.Statements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression("j"),
                new CodePrimitiveExpression(5)));
            cd.Members.Add(cmm);

            cmm = new CodeMemberMethod();
            cmm.Name = "CallingWork";
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeParameterDeclarationExpression parames = new CodeParameterDeclarationExpression(typeof(int), "a");
            cmm.Parameters.Add(parames);
            cmm.ReturnType = new CodeTypeReference("System.int32");
            cmm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("a"),
                new CodePrimitiveExpression(10)));
            cmm.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "b"));
            // invoke the method called "work"
            CodeMethodInvokeExpression methodinvoked = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression
                (new CodeTypeReferenceExpression("TEST"), "Work"));
            // add parameter with ref direction
            CodeDirectionExpression parameter = new CodeDirectionExpression(FieldDirection.Ref,
                new CodeVariableReferenceExpression("a"));
            methodinvoked.Parameters.Add(parameter);
            // add parameter with out direction
            parameter = new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression("b"));
            methodinvoked.Parameters.Add(parameter);
            cmm.Statements.Add(methodinvoked);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression
                (new CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.Add, new CodeVariableReferenceExpression("b"))));
            cd.Members.Add(cmm);

            AssertEqual(cd,
                @"public class TEST {
                      static void Work(ref int i, out int j) {
                          i = (i + 4);
                          j = 5;
                      }

                      public static int CallingWork(int a) {
                          a = 10;
                          int b;
                          TEST.Work(ref a, out b);
                          return (a + b);
                      }
                  }");
        }

        [Fact]
        public void NamespaceWithMultipleClasses()
        {
            var ns = new CodeNamespace("SomeNamespace");
            ns.Types.Add(new CodeTypeDeclaration("PublicClass") { IsClass = true, TypeAttributes = TypeAttributes.Public });
            ns.Types.Add(new CodeTypeDeclaration("PrivateClass") { IsClass = true, TypeAttributes = TypeAttributes.NotPublic });
            ns.Types.Add(new CodeTypeDeclaration("SealedClass") { IsClass = true, TypeAttributes = TypeAttributes.Sealed });
            ns.Types.Add(new CodeTypeDeclaration("PartialClass") { IsClass = true, IsPartial = true });
            ns.Types.Add(new CodeTypeDeclaration("PartialClass") { IsClass = true, IsPartial = true });

            AssertEqual(ns,
                @"namespace SomeNamespace {
                      public class PublicClass { }
                      internal class PrivateClass { }
                      internal sealed class SealedClass { }
                      public partial class PartialClass { }
                      public partial class PartialClass { }
                  }");
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework outputs C# keywords rather than type names")]
        [Theory]
        [InlineData(typeof(byte), "void System.Byte.MyMethod() { }")]
        [InlineData(typeof(short), "void System.Int16.MyMethod() { }")]
        [InlineData(typeof(ushort), "void System.UInt16.MyMethod() { }")]
        [InlineData(typeof(int), "void System.Int32.MyMethod() { }")]
        [InlineData(typeof(uint), "void System.UInt32.MyMethod() { }")]
        [InlineData(typeof(long), "void System.Int64.MyMethod() { }")]
        [InlineData(typeof(ulong), "void System.UInt64.MyMethod() { }")]
        [InlineData(typeof(string), "void System.String.MyMethod() { }")]
        [InlineData(typeof(object), "void System.Object.MyMethod() { }")]
        [InlineData(typeof(bool), "void System.Boolean.MyMethod() { }")]
        [InlineData(typeof(void), "void System.Void.MyMethod() { }")]
        [InlineData(typeof(char), "void System.Char.MyMethod() { }")]
        [InlineData(typeof(float), "void System.Single.MyMethod() { }")]
        [InlineData(typeof(double), "void System.Double.MyMethod() { }")]
        [InlineData(typeof(decimal), "void System.Decimal.MyMethod() { }")]
        public void ExplicitImplementation(Type type, string expectedResult)
        {
            AssertEqual(new CodeMemberMethod()
            {
                Name = "MyMethod",
                PrivateImplementationType = new CodeTypeReference(type)
            }, expectedResult);
        }

        [Fact]
        public void Arrays_SingleDimensional_PrimitiveTypes()
        {
            var arrayMethod = new CodeMemberMethod();
            arrayMethod.Name = "ArrayMethod";
            arrayMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "parameter"));
            arrayMethod.Attributes = (arrayMethod.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            arrayMethod.ReturnType = new CodeTypeReference(typeof(long));
            arrayMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "arraySize", new CodePrimitiveExpression(3)));
            arrayMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(int[]), "array1"));
            arrayMethod.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int32", 1), "array2", new CodeArrayCreateExpression(typeof(int[]), new CodePrimitiveExpression(3))));
            arrayMethod.Statements.Add(
                new CodeVariableDeclarationStatement(new CodeTypeReference("System.Int16", 1), "array3", new CodeArrayCreateExpression(new CodeTypeReference("System.Int16", 1),
                new CodeExpression[] {
                                         new CodePrimitiveExpression(1),
                                         new CodePrimitiveExpression(4),
                                         new CodePrimitiveExpression(9),})));
            arrayMethod.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("array1"), new CodeArrayCreateExpression(typeof(int[]), new CodeVariableReferenceExpression("arraySize"))));
            arrayMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(long), "retValue", new CodePrimitiveExpression(0)));
            arrayMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "i"));
            arrayMethod.Statements.Add(
                new CodeIterationStatement(
                new CodeAssignStatement(new CodeVariableReferenceExpression("i"), new CodePrimitiveExpression(0)),
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("i"),
                CodeBinaryOperatorType.LessThan,
                new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression("array1"), "Length")),
                new CodeAssignStatement(
                new CodeVariableReferenceExpression("i"),
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("i"),
                CodeBinaryOperatorType.Add,
                new CodePrimitiveExpression(1))),
                new CodeAssignStatement(
                new CodeArrayIndexerExpression(
                new CodeVariableReferenceExpression("array1"),
                new CodeVariableReferenceExpression("i")),
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("i"),
                CodeBinaryOperatorType.Multiply,
                new CodeVariableReferenceExpression("i"))),
                new CodeAssignStatement(
                new CodeArrayIndexerExpression(
                new CodeVariableReferenceExpression("array2"),
                new CodeVariableReferenceExpression("i")),
                new CodeBinaryOperatorExpression(
                new CodeArrayIndexerExpression(
                new CodeVariableReferenceExpression("array1"),
                new CodeVariableReferenceExpression("i")),
                CodeBinaryOperatorType.Subtract,
                new CodeVariableReferenceExpression("i"))),
                new CodeAssignStatement(
                new CodeVariableReferenceExpression("retValue"),
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("retValue"),
                CodeBinaryOperatorType.Add,
                new CodeBinaryOperatorExpression(
                new CodeArrayIndexerExpression(
                new CodeVariableReferenceExpression("array1"),
                new CodeVariableReferenceExpression("i")),
                CodeBinaryOperatorType.Add,
                new CodeBinaryOperatorExpression(
                new CodeArrayIndexerExpression(
                new CodeVariableReferenceExpression("array2"),
                new CodeVariableReferenceExpression("i")),
                CodeBinaryOperatorType.Add,
                new CodeArrayIndexerExpression(
                new CodeVariableReferenceExpression("array3"),
                new CodeVariableReferenceExpression("i"))))))));
            arrayMethod.Statements.Add(
                new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retValue")));

            AssertEqual(arrayMethod,
                @"public long ArrayMethod(int parameter)
                  {
                      int arraySize = 3;
                      int[] array1;
                      int[] array2 = new int[3];
                      short[] array3 = new short[] { 1, 4, 9};
                      array1 = new int[arraySize];
                      long retValue = 0;
                      int i;
                      for (i = 0; (i < array1.Length); i = (i + 1))
                      {
                          array1[i] = (i * i);
                          array2[i] = (array1[i] - i);
                          retValue = (retValue + (array1[i] + (array2[i] + array3[i])));
                      }
                      return retValue;
                  }");
        }

        [Fact]
        public void Arrays_WithinArrays_Empty_NonPrimitiveTypes()
        {
            var arrayMethod = new CodeMemberMethod();
            arrayMethod.Name = "MoreArrayTests";
            arrayMethod.Attributes = (arrayMethod.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            arrayMethod.ReturnType = new CodeTypeReference(typeof(int));
            arrayMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "i"));
                arrayMethod.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int[][])),
                            "arrayOfArrays", new CodeArrayCreateExpression(typeof(int[][]),
                            new CodeArrayCreateExpression(typeof(int[]), new CodePrimitiveExpression(3), new CodePrimitiveExpression(4)),
                            new CodeArrayCreateExpression(typeof(int[]), new CodePrimitiveExpression(1)), new CodeArrayCreateExpression(typeof(int[])))));
            arrayMethod.Statements.Add(new CodeVariableDeclarationStatement(
                new CodeTypeReference("System.Int32", 1), "array2",
                new CodeArrayCreateExpression(typeof(int[]), new CodePrimitiveExpression(0))));
            arrayMethod.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("Class2", 1),
                "arrayType", new CodeArrayCreateExpression(new CodeTypeReference("Class2", 1), new CodePrimitiveExpression(2))));
            arrayMethod.Statements.Add(new CodeAssignStatement(new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("arrayType"),
                        new CodePrimitiveExpression(1)), new CodeObjectCreateExpression(new CodeTypeReference("Class2"))));
            arrayMethod.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression
                        (new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("arrayType"),
                        new CodePrimitiveExpression(1)), "number"),
                        new CodeBinaryOperatorExpression(new CodeArrayIndexerExpression(
                        new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("arrayOfArrays"), new CodePrimitiveExpression(0)), new CodePrimitiveExpression(1)),
                        CodeBinaryOperatorType.Add, new CodeVariableReferenceExpression("i"))));
            arrayMethod.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression
                        (new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("arrayType"),
                        new CodePrimitiveExpression(1)), "number")));

            AssertEqual(arrayMethod,
                @"public int MoreArrayTests(int i)
                  {
                      int[][] arrayOfArrays = new int[][] { new int[] { 3,
                                                      4},
                                              new int[1],
                                              new int[0]};
                      int[] array2 = new int[0];
                      Class2[] arrayType = new Class2[2];
                      arrayType[1] = new Class2();
                      arrayType[1].number = (arrayOfArrays[0][1] + i);
                      return arrayType[1].number;
                  }");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void MetadataAttributes()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                var cu = new CodeCompileUnit();

                var ns = new CodeNamespace();
                ns.Name = "MyNamespace";
                ns.Imports.Add(new CodeNamespaceImport("System"));
                ns.Imports.Add(new CodeNamespaceImport("System.Drawing"));
                ns.Imports.Add(new CodeNamespaceImport("System.Windows.Forms"));
                ns.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
                cu.Namespaces.Add(ns);

                var attrs = cu.AssemblyCustomAttributes;
                attrs.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyTitle", new CodeAttributeArgument(new CodePrimitiveExpression("MyAssembly"))));
                attrs.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyVersion", new CodeAttributeArgument(new CodePrimitiveExpression("1.0.6.2"))));
                attrs.Add(new CodeAttributeDeclaration("System.CLSCompliantAttribute", new CodeAttributeArgument(new CodePrimitiveExpression(false))));

                var class1 = new CodeTypeDeclaration() { Name = "MyClass" };
                class1.CustomAttributes.Add(new CodeAttributeDeclaration("System.Serializable"));
                class1.CustomAttributes.Add(new CodeAttributeDeclaration("System.Obsolete", new CodeAttributeArgument(new CodePrimitiveExpression("Don't use this Class"))));
                ns.Types.Add(class1);

                var nestedClass = new CodeTypeDeclaration("NestedClass") { IsClass = true, TypeAttributes = TypeAttributes.NestedPublic };
                nestedClass.CustomAttributes.Add(new CodeAttributeDeclaration("System.Serializable"));
                class1.Members.Add(nestedClass);

                var method1 = new CodeMemberMethod() { Name = "MyMethod" };
                method1.CustomAttributes.Add(new CodeAttributeDeclaration("System.Obsolete", new CodeAttributeArgument(new CodePrimitiveExpression("Don't use this Method"))));
                method1.CustomAttributes.Add(new CodeAttributeDeclaration("System.ComponentModel.Editor", new CodeAttributeArgument(new CodePrimitiveExpression("This")), new CodeAttributeArgument(new CodePrimitiveExpression("That"))));
                var param1 = new CodeParameterDeclarationExpression(typeof(string), "blah");
                param1.CustomAttributes.Add(new CodeAttributeDeclaration("System.Xml.Serialization.XmlElementAttribute",
                                new CodeAttributeArgument("Form", new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.Xml.Schema.XmlSchemaForm"), "Unqualified")),
                                new CodeAttributeArgument("IsNullable", new CodePrimitiveExpression(false))));
                method1.Parameters.Add(param1);
                var param2 = new CodeParameterDeclarationExpression(typeof(int[]), "arrayit");
                param2.CustomAttributes.Add(
                            new CodeAttributeDeclaration("System.Xml.Serialization.XmlElementAttribute",
                                new CodeAttributeArgument("Form", new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.Xml.Schema.XmlSchemaForm"), "Unqualified")),
                                new CodeAttributeArgument("IsNullable", new CodePrimitiveExpression(false))));
                method1.Parameters.Add(param2);
                class1.Members.Add(method1);

                var function1 = new CodeMemberMethod();
                function1.Name = "MyFunction";
                function1.ReturnType = new CodeTypeReference(typeof(string));
                function1.CustomAttributes.Add(new CodeAttributeDeclaration("System.Obsolete", new CodeAttributeArgument(new CodePrimitiveExpression("Don't use this Function"))));
                function1.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("System.Xml.Serialization.XmlIgnoreAttribute"));
                function1.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("System.Xml.Serialization.XmlRootAttribute", new
                    CodeAttributeArgument("Namespace", new CodePrimitiveExpression("Namespace Value")), new
                    CodeAttributeArgument("ElementName", new CodePrimitiveExpression("Root, hehehe"))));
                function1.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression("Return")));
                class1.Members.Add(function1);

                CodeMemberMethod function2 = new CodeMemberMethod();
                function2.Name = "GlobalKeywordFunction";
                function2.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(ObsoleteAttribute), CodeTypeReferenceOptions.GlobalReference), new
                    CodeAttributeArgument(new CodePrimitiveExpression("Don't use this Function"))));
                CodeTypeReference typeRef = new CodeTypeReference("System.Xml.Serialization.XmlIgnoreAttribute", CodeTypeReferenceOptions.GlobalReference);
                CodeAttributeDeclaration codeAttrib = new CodeAttributeDeclaration(typeRef);
                function2.ReturnTypeCustomAttributes.Add(codeAttrib);
                class1.Members.Add(function2);

                CodeMemberField field1 = new CodeMemberField();
                field1.Name = "myField";
                field1.Type = new CodeTypeReference(typeof(string));
                field1.CustomAttributes.Add(new CodeAttributeDeclaration("System.Xml.Serialization.XmlElementAttribute"));
                field1.InitExpression = new CodePrimitiveExpression("hi!");
                class1.Members.Add(field1);

                CodeMemberProperty prop1 = new CodeMemberProperty();
                prop1.Name = "MyProperty";
                prop1.Type = new CodeTypeReference(typeof(string));
                prop1.CustomAttributes.Add(new CodeAttributeDeclaration("System.Obsolete", new CodeAttributeArgument(new CodePrimitiveExpression("Don't use this Property"))));
                prop1.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "myField")));
                class1.Members.Add(prop1);

                CodeConstructor const1 = new CodeConstructor();
                const1.CustomAttributes.Add(new CodeAttributeDeclaration("System.Obsolete", new CodeAttributeArgument(new CodePrimitiveExpression("Don't use this Constructor"))));
                class1.Members.Add(const1);

                class1 = new CodeTypeDeclaration("Test");
                class1.IsClass = true;
                class1.BaseTypes.Add(new CodeTypeReference("Form"));
                ns.Types.Add(class1);

                CodeMemberField mfield = new CodeMemberField(new CodeTypeReference("Button"), "b");
                mfield.InitExpression = new CodeObjectCreateExpression(new CodeTypeReference("Button"));
                class1.Members.Add(mfield);

                CodeConstructor ctor = new CodeConstructor();
                ctor.Attributes = MemberAttributes.Public;
                ctor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                    "Size"), new CodeObjectCreateExpression(new CodeTypeReference("Size"),
                                    new CodePrimitiveExpression(600), new CodePrimitiveExpression(600))));
                ctor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("b"),
                                    "Text"), new CodePrimitiveExpression("Test")));
                ctor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("b"),
                                    "TabIndex"), new CodePrimitiveExpression(0)));
                ctor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("b"),
                                    "Location"), new CodeObjectCreateExpression(new CodeTypeReference("Point"),
                                    new CodePrimitiveExpression(400), new CodePrimitiveExpression(525))));
                ctor.Statements.Add(new CodeAttachEventStatement(new CodeEventReferenceExpression(new
                    CodeThisReferenceExpression(), "MyEvent"), new CodeDelegateCreateExpression(new CodeTypeReference("EventHandler")
                    , new CodeThisReferenceExpression(), "b_Click")));
                class1.Members.Add(ctor);

                CodeMemberEvent evt = new CodeMemberEvent();
                evt.Name = "MyEvent";
                evt.Type = new CodeTypeReference("System.EventHandler");
                evt.Attributes = MemberAttributes.Public;
                evt.CustomAttributes.Add(new CodeAttributeDeclaration("System.CLSCompliantAttribute", new CodeAttributeArgument(new CodePrimitiveExpression(false))));
                class1.Members.Add(evt);

                var cmm = new CodeMemberMethod();
                cmm.Name = "b_Click";
                cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "sender"));
                cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(EventArgs), "e"));
                class1.Members.Add(cmm);

                AssertEqual(cu,
                    @"//------------------------------------------------------------------------------
                    // <auto-generated>
                    //     This code was generated by a tool.
                    //
                    //     Changes to this file may cause incorrect behavior and will be lost if
                    //     the code is regenerated.
                    // </auto-generated>
                    //------------------------------------------------------------------------------

                    [assembly: System.Reflection.AssemblyTitle(""MyAssembly"")]
                    [assembly: System.Reflection.AssemblyVersion(""1.0.6.2"")]
                    [assembly: System.CLSCompliantAttribute(false)]

                    namespace MyNamespace
                    {
                        using System;
                        using System.Drawing;
                        using System.Windows.Forms;
                        using System.ComponentModel;

                        [System.Serializable()]
                        [System.Obsolete(""Don\'t use this Class"")]
                        public class MyClass
                        {
                            [System.Xml.Serialization.XmlElementAttribute()]
                            private string myField = ""hi!"";

                            [System.Obsolete(""Don\'t use this Constructor"")]
                            private MyClass()
                            {
                            }

                            [System.Obsolete(""Don\'t use this Property"")]
                            private string MyProperty
                            {
                                get
                                {
                                    return this.myField;
                                }
                            }

                            [System.Obsolete(""Don\'t use this Method"")]
                            [System.ComponentModel.Editor(""This"", ""That"")]
                            private void MyMethod([System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable=false)] string blah, [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable=false)] int[] arrayit)
                            {
                            }

                            [System.Obsolete(""Don\'t use this Function"")]
                            [return: System.Xml.Serialization.XmlIgnoreAttribute()]
                            [return: System.Xml.Serialization.XmlRootAttribute(Namespace=""Namespace Value"", ElementName=""Root, hehehe"")]
                            private string MyFunction()
                            {
                                return ""Return"";
                            }

                            [global::System.ObsoleteAttribute(""Don\'t use this Function"")]
                            [return: global::System.Xml.Serialization.XmlIgnoreAttribute()]
                            private void GlobalKeywordFunction()
                            {
                            }

                            [System.Serializable()]
                            public class NestedClass
                            {
                            }
                        }

                        public class Test : Form
                        {
                            private Button b = new Button();

                            public Test()
                            {
                                this.Size = new Size(600, 600);
                                b.Text = ""Test"";
                                b.TabIndex = 0;
                                b.Location = new Point(400, 525);
                                this.MyEvent += new EventHandler(this.b_Click);
                            }

                            [System.CLSCompliantAttribute(false)]
                            public event System.EventHandler MyEvent;

                            private void b_Click(object sender, System.EventArgs e)
                            {
                            }
                        }
                    }");
            }
        }

        [Fact]
        public void CastingOperations()
        {
            var cd = new CodeTypeDeclaration();
            cd.Name = "Test";
            cd.IsClass = true;

            // create method to test casting float to int
            CodeMemberMethod castReturnValue = new CodeMemberMethod();
            castReturnValue.Name = "CastReturnValue";
            castReturnValue.ReturnType = new CodeTypeReference(typeof(int));
            castReturnValue.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeParameterDeclarationExpression strParam = new CodeParameterDeclarationExpression(typeof(string), "value");
            castReturnValue.Parameters.Add(strParam);
            castReturnValue.Statements.Add(new CodeMethodReturnStatement(new CodeCastExpression(typeof(int), new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("System.Single"), "Parse", new CodeExpression[] { new CodeVariableReferenceExpression("value"), new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("System.Globalization.CultureInfo"), "InvariantCulture") }))));
            cd.Members.Add(castReturnValue);

            // create method to test casting interface -> class
            CodeMemberMethod castInterface = new CodeMemberMethod();
            castInterface.Name = "CastInterface";
            castInterface.ReturnType = new CodeTypeReference(typeof(string));
            castInterface.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeParameterDeclarationExpression interfaceParam = new CodeParameterDeclarationExpression(typeof(System.ICloneable), "value");
            castInterface.Parameters.Add(interfaceParam);
            castInterface.Statements.Add(new CodeMethodReturnStatement(new CodeCastExpression(typeof(string), new CodeVariableReferenceExpression("value"))));
            cd.Members.Add(castInterface);

            // create method to test casting value type -> reference type
            CodeMemberMethod valueToReference = new CodeMemberMethod();
            valueToReference.Name = "ValueToReference";
            valueToReference.ReturnType = new CodeTypeReference(typeof(object));
            valueToReference.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeParameterDeclarationExpression valueParam = new CodeParameterDeclarationExpression(typeof(int), "value");
            valueToReference.Parameters.Add(valueParam);
            valueToReference.Statements.Add(new CodeMethodReturnStatement(new CodeCastExpression(typeof(object), new CodeVariableReferenceExpression("value"))));
            cd.Members.Add(valueToReference);

            AssertEqual(cd,
                @"public class Test {
                     public static int CastReturnValue(string value) {
                         return ((int)(float.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));
                     }
                     public static string CastInterface(System.ICloneable value) {
                         return ((string)(value));
                     }
                     public static object ValueToReference(int value) {
                         return ((object)(value));
                     }
                 }");
        }

        [Fact]
        public void BinaryOperators()
        {
            CodeNamespace ns = new CodeNamespace("Namespace1");
            ns.Imports.Add(new CodeNamespaceImport("System"));

            CodeTypeDeclaration class1 = new CodeTypeDeclaration();
            class1.Name = "Class1";
            class1.BaseTypes.Add(new CodeTypeReference(typeof(object)));
            ns.Types.Add(class1);

            CodeMemberMethod retMethod = new CodeMemberMethod();
            retMethod.Name = "ReturnMethod";
            retMethod.Attributes = (retMethod.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            retMethod.ReturnType = new CodeTypeReference(typeof(int));
            retMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "intInput"));

            CodeBinaryOperatorExpression cboExpression = new CodeBinaryOperatorExpression(
                new CodeBinaryOperatorExpression(
                new CodePrimitiveExpression(18),
                CodeBinaryOperatorType.Divide,
                new CodeBinaryOperatorExpression(
                new CodePrimitiveExpression(6),
                CodeBinaryOperatorType.Subtract,
                new CodePrimitiveExpression(4))),
                CodeBinaryOperatorType.Multiply,
                new CodeArgumentReferenceExpression("intInput"));

            CodeVariableDeclarationStatement variableDeclaration = null;
            variableDeclaration = new CodeVariableDeclarationStatement(typeof(int), "x1", cboExpression);

            retMethod.Statements.Add(variableDeclaration);
            retMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                typeof(int),
                "x2",
                new CodeBinaryOperatorExpression(
                new CodePrimitiveExpression(19),
                CodeBinaryOperatorType.Modulus,
                new CodePrimitiveExpression(8))));
            retMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                typeof(int),
                "x3",
                new CodeBinaryOperatorExpression(
                new CodeBinaryOperatorExpression(
                new CodePrimitiveExpression(15),
                CodeBinaryOperatorType.BitwiseAnd,
                new CodePrimitiveExpression(35)),
                CodeBinaryOperatorType.BitwiseOr,
                new CodePrimitiveExpression(129))));
            retMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                typeof(int),
                "x4",
                new CodePrimitiveExpression(0)));
            retMethod.Statements.Add(
                new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x2"),
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(3)),
                CodeBinaryOperatorType.BooleanOr,
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x3"),
                CodeBinaryOperatorType.LessThan,
                new CodePrimitiveExpression(129))),
                new CodeStatement[] { CreateVariableIncrementExpression("x4", 1) },
                new CodeStatement[] { CreateVariableIncrementExpression("x4", 2) }));
            retMethod.Statements.Add(
                new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x2"),
                CodeBinaryOperatorType.GreaterThan,
                new CodePrimitiveExpression(-1)),
                CodeBinaryOperatorType.BooleanAnd,
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x3"),
                CodeBinaryOperatorType.GreaterThanOrEqual,
                new CodePrimitiveExpression(5000))),
                new CodeStatement[] { CreateVariableIncrementExpression("x4", 4) },
                new CodeStatement[] { CreateVariableIncrementExpression("x4", 8) }));
            retMethod.Statements.Add(
                new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x2"),
                CodeBinaryOperatorType.LessThanOrEqual,
                new CodePrimitiveExpression(3)),
                CodeBinaryOperatorType.BooleanAnd,
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x3"),
                CodeBinaryOperatorType.IdentityInequality,
                new CodePrimitiveExpression(1))),
                new CodeStatement[] { CreateVariableIncrementExpression("x4", 16) },
                new CodeStatement[] { CreateVariableIncrementExpression("x4", 32) }));
            retMethod.Statements.Add(
                new CodeMethodReturnStatement(
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x1"),
                CodeBinaryOperatorType.Add,
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x2"),
                CodeBinaryOperatorType.Add,
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("x3"),
                CodeBinaryOperatorType.Add,
                new CodeVariableReferenceExpression("x4"))))));
            class1.Members.Add(retMethod);

            retMethod = new CodeMemberMethod();
            retMethod.Name = "SecondReturnMethod";
            retMethod.Attributes = (retMethod.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            retMethod.ReturnType = new CodeTypeReference(typeof(int));
            retMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "intInput"));

            retMethod.Statements.Add(new CodeCommentStatement("To test CodeBinaryOperatorType.IdentiEquality operator"));
            retMethod.Statements.Add(
                new CodeConditionStatement(
                new CodeBinaryOperatorExpression(new CodeCastExpression("Object",
                new CodeVariableReferenceExpression("intInput")),
                CodeBinaryOperatorType.IdentityEquality, new CodeCastExpression("Object",
                new CodePrimitiveExpression(5))),
                new CodeStatement[] { new CodeMethodReturnStatement(new
                CodePrimitiveExpression(5)) }, new CodeStatement[] { new
                CodeMethodReturnStatement(new CodePrimitiveExpression(4))}));
            class1.Members.Add(retMethod);

            AssertEqual(ns,
                @"namespace Namespace1 {
                      using System;

                      public class Class1 : object {
                          public int ReturnMethod(int intInput) {
                              int x1 = ((18 / (6 - 4)) * intInput);
                              int x2 = (19 % 8);
                              int x3 = ((15 & 35) | 129);
                              int x4 = 0;
                              if (((x2 == 3) || (x3 < 129))) {
                                  x4 = (x4 + 1);
                              }
                              else {
                                  x4 = (x4 + 2);
                              }
                              if (((x2 > -1) && (x3 >= 5000))) {
                                  x4 = (x4 + 4);
                              }
                              else {
                                  x4 = (x4 + 8);
                              }
                              if (((x2 <= 3) && (x3 != 1))) {
                                  x4 = (x4 + 16);
                              }
                              else {
                                  x4 = (x4 + 32);
                              }
                              return (x1 + (x2 + (x3 + x4)));
                          }

                          public int SecondReturnMethod(int intInput) {
                              // To test CodeBinaryOperatorType.IdentiEquality operator
                              if ((((Object)(intInput)) == ((Object)(5)))) {
                                  return 5;
                              }
                              else {
                                  return 4;
                              }
                          }
                      }
                  }");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/1913", ~TargetFrameworkMonikers.NetFramework)]
        public void CharEncodingNetFx()
        {
            string chars = "\u1234 \u4567 \uABCD \r \n \t \\ \" \' \0 \u2028 \u2029 \u0084 \u0085 \U00010F00";

            var main = new CodeEntryPointMethod();
            main.Statements.Add(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Console)), "WriteLine"),
                    new CodeExpression[] { new CodePrimitiveExpression(chars) }));

            AssertEqual(main,
                 "public static void Main() { " +
                 "     System.Console.WriteLine(\"\u1234 \u4567 \uABCD \\r \\n \\t \\\\ \\\" \\' \\0 \\u2028 \\u2029 \u0084 \u0085 \U00010F00\"); " +
                 "}");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/1913", TargetFrameworkMonikers.NetFramework)]
        public void CharEncoding()
        {
            string chars = "\u1234 \u4567 \uABCD \r \n \t \\ \" \' \0 \u2028 \u2029 \u0084 \u0085 \U00010F00";

            var main = new CodeEntryPointMethod();
            main.Statements.Add(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Console)), "WriteLine"),
                    new CodeExpression[] { new CodePrimitiveExpression(chars) }));

            AssertEqual(main,
                 "public static void Main() { " +
                 "     System.Console.WriteLine(\"\u1234 \u4567 \uABCD \\r \\n \\t \\\\ \\\" \\' \\0 \\u2028 \\u2029 \u0084 \\u0085 \U00010F00\"); " +
                 "}");
        }

        [Fact]
        public void DefaultValues()
        {
            var main = new CodeEntryPointMethod();
            foreach (Type t in new[] { typeof(int), typeof(object), typeof(DateTime), typeof(string) })
            {
                main.Statements.Add(
                    new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Console)), "WriteLine"),
                        new CodeExpression[] { new CodeDefaultValueExpression(new CodeTypeReference(t)) }));
            }

            AssertEqual(main,
                 "public static void Main() { " +
                 "     System.Console.WriteLine(default(int)); " +
                 "     System.Console.WriteLine(default(object)); " +
                 "     System.Console.WriteLine(default(System.DateTime)); " +
                 "     System.Console.WriteLine(default(string)); " +
                 "}");

            AssertEqual(
                new CodeDefaultValueExpression(new CodeTypeReference(typeof(Guid))),
                "default(System.Guid)");
            AssertEqual(
                new CodeDefaultValueExpression(new CodeTypeReference("Some.Type.Name")),
                "default(Some.Type.Name)");
        }

        [Fact]
        public void TypeOf()
        {
            var nspace = new CodeNamespace("System.Something");
            var class1 = new CodeTypeDeclaration("ClassToTest") { IsClass = true };
            nspace.Types.Add(class1);

            var cmm = new CodeMemberMethod();
            cmm.Name = "Primitives";
            cmm.ReturnType = new CodeTypeReference(typeof(string));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeOfExpression(typeof(int)), "ToString")));
            class1.Members.Add(cmm);

            cmm = new CodeMemberMethod();
            cmm.Name = "ArraysOfPrimitives";
            cmm.ReturnType = new CodeTypeReference(typeof(string));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeOfExpression(typeof(int[])), "ToString")));
            class1.Members.Add(cmm);

            cmm = new CodeMemberMethod();
            cmm.Name = "NonPrimitives";
            cmm.ReturnType = new CodeTypeReference(typeof(string));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeOfExpression(typeof(System.ICloneable)), "ToString")));
            class1.Members.Add(cmm);

            cmm = new CodeMemberMethod();
            cmm.Name = "ArraysOfNonPrimitives";
            cmm.ReturnType = new CodeTypeReference(typeof(string));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeOfExpression(typeof(System.ICloneable[])), "ToString")));
            class1.Members.Add(cmm);

            cmm = new CodeMemberMethod();
            cmm.Name = "Enumerations";
            cmm.ReturnType = new CodeTypeReference(typeof(string));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeOfExpression("DecimalEnum"), "ToString")));
            class1.Members.Add(cmm);

            var ce = new CodeTypeDeclaration("DecimalEnum") { IsEnum = true };
            ce.IsEnum = true;
            nspace.Types.Add(ce);
            for (int k = 0; k < 5; k++)
            {
                CodeMemberField Field = new CodeMemberField("System.Int32", "Num" + (k).ToString());
                Field.InitExpression = new CodePrimitiveExpression(k);
                ce.Members.Add(Field);
            }

            AssertEqual(nspace,
                @"namespace System.Something {
                      public class ClassToTest {
                          public static string Primitives() {
                              return typeof(int).ToString();
                          }
                          public static string ArraysOfPrimitives() {
                              return typeof(int[]).ToString();
                          }
                          public static string NonPrimitives() {
                              return typeof(System.ICloneable).ToString();
                          }
                          public static string ArraysOfNonPrimitives() {
                              return typeof(System.ICloneable[]).ToString();
                          }
                          public static string Enumerations() {
                              return typeof(DecimalEnum).ToString();
                          }
                      }

                      public enum DecimalEnum {
                          Num0 = 0,
                          Num1 = 1,
                          Num2 = 2,
                          Num3 = 3,
                          Num4 = 4,
                      }
                  }");
        }

        [Fact]
        public void TryCatchThrow()
        {
            var cd = new CodeTypeDeclaration();
            cd.Name = "Test";
            cd.IsClass = true;

            // try catch statement with just finally
            var cmm = new CodeMemberMethod();
            cmm.Name = "FirstScenario";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "a");
            cmm.Parameters.Add(param);

            CodeTryCatchFinallyStatement tcfstmt = new CodeTryCatchFinallyStatement();
            tcfstmt.FinallyStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("a"), new
                                                                  CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.Add,
                                                                                               new CodePrimitiveExpression(5))));
            cmm.Statements.Add(tcfstmt);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
            cd.Members.Add(cmm);

            CodeBinaryOperatorExpression cboExpression = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.Divide, new CodeVariableReferenceExpression("a"));
            CodeAssignStatement assignStatement = new CodeAssignStatement(new CodeVariableReferenceExpression("a"), cboExpression);

            // try catch statement with just catch
            cmm = new CodeMemberMethod();
            cmm.Name = "SecondScenario";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            param = new CodeParameterDeclarationExpression(typeof(int), "a");
            cmm.Parameters.Add(param);
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "exceptionMessage"));

            tcfstmt = new CodeTryCatchFinallyStatement();
            CodeCatchClause catchClause = new CodeCatchClause("e");
            tcfstmt.TryStatements.Add(assignStatement);
            catchClause.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("a"),
                                                               new CodePrimitiveExpression(3)));
            catchClause.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("exceptionMessage"),
                                                               new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("e"), "ToString")));
            tcfstmt.CatchClauses.Add(catchClause);
            tcfstmt.FinallyStatements.Add(CreateVariableIncrementExpression("a", 1));

            cmm.Statements.Add(tcfstmt);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));

            cd.Members.Add(cmm);

            // try catch statement with multiple catches
            cmm = new CodeMemberMethod();
            cmm.Name = "ThirdScenario";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            param = new CodeParameterDeclarationExpression(typeof(int), "a");
            cmm.Parameters.Add(param);
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "exceptionMessage"));

            tcfstmt = new CodeTryCatchFinallyStatement();
            catchClause = new CodeCatchClause("e", new CodeTypeReference(typeof(ArgumentNullException)));
            tcfstmt.TryStatements.Add(assignStatement);
            catchClause.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("a"),
                                                               new CodePrimitiveExpression(9)));
            catchClause.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("exceptionMessage"),
                                                               new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("e"), "ToString")));
            tcfstmt.CatchClauses.Add(catchClause);

            // add a second catch clause
            catchClause = new CodeCatchClause("f", new CodeTypeReference(typeof(Exception)));
            catchClause.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("exceptionMessage"),
                                                               new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("f"), "ToString")));
            catchClause.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("a"),
                                                               new CodePrimitiveExpression(9)));
            tcfstmt.CatchClauses.Add(catchClause);

            cmm.Statements.Add(tcfstmt);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
            cd.Members.Add(cmm);

            // catch throws exception
            cmm = new CodeMemberMethod();
            cmm.Name = "FourthScenario";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            param = new CodeParameterDeclarationExpression(typeof(int), "a");
            cmm.Parameters.Add(param);

            tcfstmt = new CodeTryCatchFinallyStatement();
            catchClause = new CodeCatchClause("e");
            tcfstmt.TryStatements.Add(assignStatement);
            catchClause.Statements.Add(new CodeCommentStatement("Error handling"));
            catchClause.Statements.Add(new CodeThrowExceptionStatement(new CodeArgumentReferenceExpression("e")));
            tcfstmt.CatchClauses.Add(catchClause);
            cmm.Statements.Add(tcfstmt);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
            cd.Members.Add(cmm);

            AssertEqual(cd,
                @"public class Test {
                      public static int FirstScenario(int a) {
                          try {
                          }
                          finally {
                              a = (a + 5);
                          }
                          return a;
                      }
                      public static int SecondScenario(int a, string exceptionMessage) {
                          try {
                              a = (a / a);
                          }
                          catch (System.Exception e) {
                              a = 3;
                              exceptionMessage = e.ToString();
                          }
                          finally {
                              a = (a + 1);
                          }
                          return a;
                      }
                      public static int ThirdScenario(int a, string exceptionMessage) {
                          try {
                              a = (a / a);
                          }
                          catch (System.ArgumentNullException e) {
                              a = 9;
                              exceptionMessage = e.ToString();
                          }
                          catch (System.Exception f) {
                              exceptionMessage = f.ToString();
                              a = 9;
                          }
                          return a;
                      }
                      public static int FourthScenario(int a) {
                          try {
                              a = (a / a);
                          }
                          catch (System.Exception e) {
                              // Error handling
                              throw e;
                          }
                          return a;
                      }
                  }");
        }

        [Fact]
        public void ValueTypes()
        {
            // create a namespace
            CodeNamespace ns = new CodeNamespace("NS");
            ns.Imports.Add(new CodeNamespaceImport("System"));

            // create a class
            CodeTypeDeclaration class1 = new CodeTypeDeclaration();
            class1.Name = "Test";
            class1.IsClass = true;
            ns.Types.Add(class1);

            // create first struct to test nested structs
            CodeTypeDeclaration structA = new CodeTypeDeclaration("structA");
            structA.IsStruct = true;

            CodeTypeDeclaration structB = new CodeTypeDeclaration("structB");
            structB.Attributes = MemberAttributes.Public;
            structB.IsStruct = true;

            CodeMemberField firstInt = new CodeMemberField(typeof(int), "int1");
            firstInt.Attributes = MemberAttributes.Public;
            structB.Members.Add(firstInt);

            CodeMemberField innerStruct = new CodeMemberField("structB", "innerStruct");
            innerStruct.Attributes = MemberAttributes.Public;

            structA.Members.Add(structB);
            structA.Members.Add(innerStruct);
            class1.Members.Add(structA);

            // create second struct to test tructs of non-primitive types
            CodeTypeDeclaration structC = new CodeTypeDeclaration("structC");
            structC.IsStruct = true;

            CodeMemberField firstPt = new CodeMemberField("Point", "pt1");
            firstPt.Attributes = MemberAttributes.Public;
            structC.Members.Add(firstPt);

            CodeMemberField secondPt = new CodeMemberField("Point", "pt2");
            secondPt.Attributes = MemberAttributes.Public;
            structC.Members.Add(secondPt);
            class1.Members.Add(structC);

            // create method to test nested struct
            CodeMemberMethod nestedStructMethod = new CodeMemberMethod();
            nestedStructMethod.Name = "NestedStructMethod";
            nestedStructMethod.ReturnType = new CodeTypeReference(typeof(int));
            nestedStructMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeVariableDeclarationStatement varStructA = new CodeVariableDeclarationStatement("structA", "varStructA");
            nestedStructMethod.Statements.Add(varStructA);
            nestedStructMethod.Statements.Add
                (
                new CodeAssignStatement
                (
                /* Expression1 */ new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("varStructA"), "innerStruct"), "int1"),
                /* Expression1 */ new CodePrimitiveExpression(3)
                )
                );
            nestedStructMethod.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("varStructA"), "innerStruct"), "int1")));
            class1.Members.Add(nestedStructMethod);

            // create method to test nested non primitive struct member
            CodeMemberMethod nonPrimitiveStructMethod = new CodeMemberMethod();
            nonPrimitiveStructMethod.Name = "NonPrimitiveStructMethod";
            nonPrimitiveStructMethod.ReturnType = new CodeTypeReference(typeof(DateTime));
            nonPrimitiveStructMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeVariableDeclarationStatement varStructC = new CodeVariableDeclarationStatement("structC", "varStructC");
            nonPrimitiveStructMethod.Statements.Add(varStructC);
            nonPrimitiveStructMethod.Statements.Add
                (
                new CodeAssignStatement
                (
                /* Expression1 */ new CodeFieldReferenceExpression(
                new CodeVariableReferenceExpression("varStructC"),
                "pt1"),
                /* Expression2 */ new CodeObjectCreateExpression("DateTime", new CodeExpression[] { new CodePrimitiveExpression(1), new CodePrimitiveExpression(-1) })
                )
                );
            nonPrimitiveStructMethod.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("varStructC"), "pt1")));
            class1.Members.Add(nonPrimitiveStructMethod);

            AssertEqual(ns,
                @"namespace NS {
                      using System;

                      public class Test {
                          public static int NestedStructMethod() {
                              structA varStructA;
                              varStructA.innerStruct.int1 = 3;
                              return varStructA.innerStruct.int1;
                          }

                          public static System.DateTime NonPrimitiveStructMethod() {
                              structC varStructC;
                              varStructC.pt1 = new DateTime(1, -1);
                              return varStructC.pt1;
                          }

                          public struct structA {

                              public structB innerStruct;

                              public struct structB {

                                  public int int1;
                              }
                          }

                          public struct structC {

                              public Point pt1;

                              public Point pt2;
                          }
                      }
                  }");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void RegionsSnippetsAndLinePragmas()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                var cu = new CodeCompileUnit();
                CodeNamespace ns = new CodeNamespace("Namespace1");

                cu.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Compile Unit Region"));
                cu.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                cu.Namespaces.Add(ns);

                var cd = new CodeTypeDeclaration("Class1");
                ns.Types.Add(cd);

                cd.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Outer Type Region"));
                cd.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                cd.Comments.Add(new CodeCommentStatement("Outer Type Comment"));

                CodeMemberField field1 = new CodeMemberField(typeof(String), "field1");
                CodeMemberField field2 = new CodeMemberField(typeof(String), "field2");
                field1.Comments.Add(new CodeCommentStatement("Field 1 Comment"));
                field2.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Field Region"));
                field2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                CodeMemberEvent evt1 = new CodeMemberEvent();
                evt1.Name = "Event1";
                evt1.Type = new CodeTypeReference(typeof(System.EventHandler));
                evt1.Attributes = (evt1.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;

                CodeMemberEvent evt2 = new CodeMemberEvent();
                evt2.Name = "Event2";
                evt2.Type = new CodeTypeReference(typeof(System.EventHandler));
                evt2.Attributes = (evt2.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;

                evt2.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Event Region"));
                evt2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                CodeMemberMethod method1 = new CodeMemberMethod();
                method1.Name = "Method1";
                method1.Attributes = (method1.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                method1.Statements.Add(
                    new CodeDelegateInvokeExpression(
                        new CodeEventReferenceExpression(new CodeThisReferenceExpression(), "Event1"),
                        new CodeExpression[] {
                        new CodeThisReferenceExpression(),
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.EventArgs"), "Empty")
                        }));

                CodeMemberMethod method2 = new CodeMemberMethod();
                method2.Name = "Method2";
                method2.Attributes = (method2.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                method2.Statements.Add(
                    new CodeDelegateInvokeExpression(
                        new CodeEventReferenceExpression(new CodeThisReferenceExpression(), "Event2"),
                        new CodeExpression[] {
                    new CodeThisReferenceExpression(),
                    new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.EventArgs"), "Empty")
                        }));
                method2.LinePragma = new CodeLinePragma("MethodLinePragma.txt", 500);
                method2.Comments.Add(new CodeCommentStatement("Method 2 Comment"));

                method2.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Method Region"));
                method2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                CodeMemberProperty property1 = new CodeMemberProperty();
                property1.Name = "Property1";
                property1.Type = new CodeTypeReference(typeof(string));
                property1.Attributes = (property1.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                property1.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "field1")));

                CodeMemberProperty property2 = new CodeMemberProperty();
                property2.Name = "Property2";
                property2.Type = new CodeTypeReference(typeof(string));
                property2.Attributes = (property2.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                property2.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "field2")));

                property2.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Property Region"));
                property2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                CodeConstructor constructor1 = new CodeConstructor();
                constructor1.Attributes = (constructor1.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                CodeStatement conState1 = new CodeAssignStatement(
                                            new CodeFieldReferenceExpression(
                                                new CodeThisReferenceExpression(),
                                                "field1"),
                                            new CodePrimitiveExpression("value1"));
                conState1.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Statements Region"));
                constructor1.Statements.Add(conState1);
                CodeStatement conState2 = new CodeAssignStatement(
                                            new CodeFieldReferenceExpression(
                                                new CodeThisReferenceExpression(),
                                                "field2"),
                                            new CodePrimitiveExpression("value2"));
                conState2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));
                constructor1.Statements.Add(conState2);

                constructor1.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Constructor Region"));
                constructor1.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                CodeConstructor constructor2 = new CodeConstructor();
                constructor2.Attributes = (constructor2.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                constructor2.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value1"));
                constructor2.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value2"));

                CodeTypeConstructor typeConstructor2 = new CodeTypeConstructor();

                typeConstructor2.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Type Constructor Region"));
                typeConstructor2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                CodeEntryPointMethod methodMain = new CodeEntryPointMethod();

                CodeTypeDeclaration nestedClass1 = new CodeTypeDeclaration("NestedClass1");
                CodeTypeDeclaration nestedClass2 = new CodeTypeDeclaration("NestedClass2");
                nestedClass2.LinePragma = new CodeLinePragma("NestedTypeLinePragma.txt", 400);
                nestedClass2.Comments.Add(new CodeCommentStatement("Nested Type Comment"));

                nestedClass2.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Nested Type Region"));
                nestedClass2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                CodeTypeDelegate delegate1 = new CodeTypeDelegate();
                delegate1.Name = "nestedDelegate1";
                delegate1.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.Object"), "sender"));
                delegate1.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.EventArgs"), "e"));

                CodeTypeDelegate delegate2 = new CodeTypeDelegate();
                delegate2.Name = "nestedDelegate2";
                delegate2.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.Object"), "sender"));
                delegate2.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("System.EventArgs"), "e"));

                delegate2.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "Delegate Region"));
                delegate2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                var snippet1 = new CodeSnippetTypeMember();
                var snippet2 = new CodeSnippetTypeMember();

                CodeRegionDirective regionStart = new CodeRegionDirective(CodeRegionMode.End, "");
                regionStart.RegionText = "Snippet Region";
                regionStart.RegionMode = CodeRegionMode.Start;
                snippet2.StartDirectives.Add(regionStart);
                snippet2.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));

                cd.Members.Add(field1);
                cd.Members.Add(method1);
                cd.Members.Add(constructor1);
                cd.Members.Add(property1);
                cd.Members.Add(methodMain);

                cd.Members.Add(evt1);
                cd.Members.Add(nestedClass1);
                cd.Members.Add(delegate1);

                cd.Members.Add(snippet1);

                cd.Members.Add(field2);
                cd.Members.Add(method2);
                cd.Members.Add(constructor2);
                cd.Members.Add(property2);

                cd.Members.Add(typeConstructor2);
                cd.Members.Add(evt2);
                cd.Members.Add(nestedClass2);
                cd.Members.Add(delegate2);
                cd.Members.Add(snippet2);

                AssertEqual(cu,
                    @"#region Compile Unit Region
                    //------------------------------------------------------------------------------
                    // <auto-generated>
                    //     This code was generated by a tool.
                    //
                    //     Changes to this file may cause incorrect behavior and will be lost if
                    //     the code is regenerated.
                    // </auto-generated>
                    //------------------------------------------------------------------------------

                    namespace Namespace1 {


                        #region Outer Type Region
                        // Outer Type Comment
                        public class Class1 {

                            // Field 1 Comment
                            private string field1;

                            #region Field Region
                            private string field2;
                            #endregion


                            #region Snippet Region
                    #endregion


                            #region Type Constructor Region
                            static Class1() {
                            }
                            #endregion

                            #region Constructor Region
                            public Class1() {
                                #region Statements Region
                                this.field1 = ""value1"";
                                this.field2 = ""value2"";
                #endregion
                        }
                #endregion

                        public Class1(string value1, string value2)
                        {
                        }

                        public string Property1
                        {
                            get
                            {
                                return this.field1;
                            }
                        }

                        #region Property Region
                        public string Property2
                        {
                            get
                            {
                                return this.field2;
                            }
                        }
                        #endregion

                        public event System.EventHandler Event1;

                        #region Event Region
                        public event System.EventHandler Event2;
                        #endregion

                        public void Method1()
                        {
                            this.Event1(this, System.EventArgs.Empty);
                        }

                        public static void Main()
                        {
                        }

                        #region Method Region
                        // Method 2 Comment

                #line 500 ""MethodLinePragma.txt""
                        public void Method2()
                        {
                            this.Event2(this, System.EventArgs.Empty);
                        }

                #line default
                #line hidden
                        #endregion

                        public class NestedClass1
                        {
                        }

                        public delegate void nestedDelegate1(object sender, System.EventArgs e);

                        #region Nested Type Region
                        // Nested Type Comment

                #line 400 ""NestedTypeLinePragma.txt""
                        public class NestedClass2
                        {
                        }

                #line default
                #line hidden
                        #endregion

                        #region Delegate Region
                        public delegate void nestedDelegate2(object sender, System.EventArgs e);
                        #endregion
                    }
                #endregion
                }
                    #endregion");
            }
        }

        [Fact]
        public void Properties()
        {
            CodeNamespace ns = new CodeNamespace("NS");
            ns.Imports.Add(new CodeNamespaceImport("System"));

            // create a class
            CodeTypeDeclaration class1 = new CodeTypeDeclaration();
            class1.Name = "Test";
            class1.IsClass = true;
            class1.BaseTypes.Add(new CodeTypeReference(typeof(Exception)));
            ns.Types.Add(class1);

            CodeMemberField int1 = new CodeMemberField(typeof(int), "int1");
            class1.Members.Add(int1);

            CodeMemberField tempString = new CodeMemberField(typeof(string), "tempString");
            class1.Members.Add(tempString);

            // basic property with get/set
            CodeMemberProperty prop1 = new CodeMemberProperty();
            prop1.Name = "prop1";
            prop1.Type = new CodeTypeReference(typeof(int));
            prop1.Attributes = MemberAttributes.Public;
            prop1.HasGet = true;
            prop1.HasSet = true;
            prop1.GetStatements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("int1"), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1))));
            prop1.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("int1"), new CodeVariableReferenceExpression("value")));
            class1.Members.Add(prop1);

            // override Property
            CodeMemberProperty overrideProp = new CodeMemberProperty();
            overrideProp.Name = "Text";
            overrideProp.Type = new CodeTypeReference(typeof(string));
            overrideProp.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            overrideProp.HasGet = true;
            overrideProp.HasSet = true;
            overrideProp.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("tempString"), new CodeVariableReferenceExpression("value")));
            overrideProp.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression("Hello World")));

            class1.Members.Add(overrideProp);

            foreach (MemberAttributes attrs in new[] { MemberAttributes.Private, MemberAttributes.Family, MemberAttributes.Assembly })
            {
                CodeMemberProperty configuredProp = new CodeMemberProperty();
                configuredProp.Name = attrs.ToString() + "Prop";
                configuredProp.Type = new CodeTypeReference(typeof(int));
                configuredProp.Attributes = attrs;
                configuredProp.HasGet = true;
                configuredProp.HasSet = true;
                configuredProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("int1"), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1))));
                configuredProp.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("int1"), new CodeVariableReferenceExpression("value")));
                class1.Members.Add(configuredProp);
            }

            // Static property
            CodeMemberProperty staticProp = new CodeMemberProperty();
            staticProp.Name = "staticProp";
            staticProp.Type = new CodeTypeReference(typeof(int));
            staticProp.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            staticProp.HasGet = true;
            staticProp.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(99)));
            class1.Members.Add(staticProp);

            // this reference
            CodeMemberMethod thisRef = new CodeMemberMethod();
            thisRef.Name = "thisRef";
            thisRef.ReturnType = new CodeTypeReference(typeof(int));
            thisRef.Attributes = MemberAttributes.Public;
            CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "value");
            thisRef.Parameters.Add(param);

            thisRef.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "privProp1"), new CodeVariableReferenceExpression("value")));
            thisRef.Statements.Add(new CodeMethodReturnStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "privProp1")));
            class1.Members.Add(thisRef);

            // set value
            CodeMemberMethod setProp = new CodeMemberMethod();
            setProp.Name = "setProp";
            setProp.ReturnType = new CodeTypeReference(typeof(int));
            setProp.Attributes = MemberAttributes.Public;
            CodeParameterDeclarationExpression intParam = new CodeParameterDeclarationExpression(typeof(int), "value");
            setProp.Parameters.Add(intParam);

            setProp.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "prop1"), new CodeVariableReferenceExpression("value")));
            setProp.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("int1")));
            class1.Members.Add(setProp);

            AssertEqual(ns,
                @"namespace NS {
                      using System;


                      public class Test : System.Exception {

                          private int int1;

                          private string tempString;

                          public virtual int prop1 {
                              get {
                                  return (int1 + 1);
                              }
                              set {
                                  int1 = value;
                              }
                          }

                          public override string Text {
                              get {
                                  return ""Hello World"";
                              }
                              set {
                                  tempString = value;
                              }
                          }

                          private int PrivateProp
                          {
                              get
                              {
                                  return (int1 + 1);
                              }
                              set
                              {
                                  int1 = value;
                              }
                          }

                          protected virtual int FamilyProp
                          {
                              get
                              {
                                  return (int1 + 1);
                              }
                              set
                              {
                                  int1 = value;
                              }
                          }

                          internal virtual int AssemblyProp
                          {
                              get
                              {
                                  return (int1 + 1);
                              }
                              set
                              {
                                  int1 = value;
                              }
                          }

                          public static int staticProp
                          {
                              get
                              {
                                  return 99;
                              }
                          }

                          public virtual int thisRef(int value)
                          {
                              this.privProp1 = value;
                              return this.privProp1;
                          }

                          public virtual int setProp(int value)
                          {
                              this.prop1 = value;
                              return int1;
                          }
                      }
                  }");
        }

        [Fact]
        public void Params()
        {
            static CodeStatement CreateStatement(string objName, int iNum)
            {
                CodeAssignStatement statement = new CodeAssignStatement(new CodeVariableReferenceExpression("str"),
                                    new CodeMethodInvokeExpression(
                                    new CodeMethodReferenceExpression(
                                    new CodeTypeReferenceExpression(new CodeTypeReference(objName)), "Replace"),
                                    new CodeExpression[]{
                                        new CodePrimitiveExpression("{" + iNum + "}"),
                                        new CodeMethodInvokeExpression(
                                            new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("array"), new CodePrimitiveExpression(iNum)),
                                            "ToString")}));
                return statement;
            }

            CodeNamespace ns = new CodeNamespace("Namespace1");
            ns.Imports.Add(new CodeNamespaceImport("System"));

            CodeTypeDeclaration class1 = new CodeTypeDeclaration();
            class1.Name = "Class1";
            ns.Types.Add(class1);

            CodeMemberMethod fooMethod1 = new CodeMemberMethod();
            fooMethod1.Name = "Foo1";
            fooMethod1.Attributes = MemberAttributes.Public;
            fooMethod1.ReturnType = new CodeTypeReference(typeof(string));

            CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression();
            parameter1.Name = "format";
            parameter1.Type = new CodeTypeReference(typeof(string));
            fooMethod1.Parameters.Add(parameter1);

            CodeParameterDeclarationExpression parameter2 = new CodeParameterDeclarationExpression();
            parameter2.Name = "array";
            parameter2.Type = new CodeTypeReference(typeof(object[]));
            parameter2.CustomAttributes.Add(new CodeAttributeDeclaration("System.ParamArrayAttribute"));
            parameter2.CustomAttributes.Add(new CodeAttributeDeclaration("System.Runtime.InteropServices.OptionalAttribute"));
            fooMethod1.Parameters.Add(parameter2);
            class1.Members.Add(fooMethod1);

            fooMethod1.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), "str"));

            fooMethod1.Statements.Add(CreateStatement("format", 0));
            fooMethod1.Statements.Add(CreateStatement("str", 1));
            fooMethod1.Statements.Add(CreateStatement("str", 2));

            fooMethod1.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("str")));

            CodeEntryPointMethod methodMain = new CodeEntryPointMethod();
            methodMain.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("Class1"), "test1", new CodeObjectCreateExpression(new CodeTypeReference("Class1"))));

            methodMain.Statements.Add(new CodeExpressionStatement(
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(new CodeTypeReference("test1")), "Foo1"),
                    new CodeExpression[] {
                        new CodePrimitiveExpression("{0} + {1} = {2}"),
                        new CodePrimitiveExpression(1),
                        new CodePrimitiveExpression(2),
                        new CodePrimitiveExpression(3)
                    })));

            class1.Members.Add(methodMain);

            AssertEqual(ns,
                @"namespace Namespace1 {
                      using System;
                      public class Class1 {
                          public virtual string Foo1(string format, [System.Runtime.InteropServices.OptionalAttribute()] params object[] array) {
                              string str;
                              str = format.Replace(""{0}"", array[0].ToString());
                              str = str.Replace(""{1}"", array[1].ToString());
                              str = str.Replace(""{2}"", array[2].ToString());
                              return str;
                          }
                          public static void Main() {
                              Class1 test1 = new Class1();
                              test1.Foo1(""{0} + {1} = {2}"", 1, 2, 3);
                          }
                      }
                  }");
        }

        [Fact]
        public void DocComments()
        {
            CodeNamespace ns = new CodeNamespace("System");
            ns.Comments.Add(new CodeCommentStatement("Some comment on a namespace"));

            var cd = new CodeTypeDeclaration("MyType");
            cd.Comments.Add(new CodeCommentStatement("<summary>Insightful comment</summary>", docComment: true));
            ns.Types.Add(cd);

            var cmm = new CodeMemberMethod() { Name = "SomeMethod" };
            cmm.Comments.Add(new CodeCommentStatement("<summary>Another insightful comment</summary>", docComment: true));
            cd.Members.Add(cmm);

            AssertEqual(ns,
                @"// Some comment on a namespace
                  namespace System {
                      /// <summary>Insightful comment</summary>
                      public class MyType {
                          /// <summary>Another insightful comment</summary>
                          private void SomeMethod() { }
                      }
                  }");
        }

        [Fact]
        public void MaskingVariables()
        {
            CodeNamespace ns = new CodeNamespace("ns");
            CodeTypeDeclaration class1 = new CodeTypeDeclaration();
            class1.Name = "Class1";
            ns.Types.Add(class1);

            var cmm = new CodeMemberMethod();
            cmm.Name = "for";
            class1.Members.Add(cmm);

            cmm = new CodeMemberMethod();
            cmm.Name = "while";
            cmm.Attributes = MemberAttributes.Final | MemberAttributes.Public;
            cmm.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "for")));
            class1.Members.Add(cmm);

            class1 = new CodeTypeDeclaration();
            class1.Name = "TestMasking";
            ns.Types.Add(class1);

            cmm = new CodeMemberMethod();
            cmm.Name = "TestMasks";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "i"));
            cmm.Attributes = MemberAttributes.Static | MemberAttributes.Public;
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("Class1"), "temp", new CodeObjectCreateExpression(new CodeTypeReference("Class1"))));
            cmm.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("temp"), "while")));
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("i")));
            class1.Members.Add(cmm);

            AssertEqual(ns,
                @"namespace ns {
                  public class Class1 {
                      private void @for() { }

                      public void @while() {
                          this.@for();
                      }
                  }

                  public class TestMasking {
                      public static int TestMasks(int i) {
                          Class1 temp = new Class1();
                          temp.@while();
                          return i;
                      }
                  }
              }");
        }

        [Fact]
        public void ForLoops()
        {
            var nspace = new CodeNamespace("NSPC");

            CodeTypeDeclaration class1 = new CodeTypeDeclaration("ClassWithMethod");
            class1.IsClass = true;
            nspace.Types.Add(class1);

            var cmm = new CodeMemberMethod();
            cmm.Name = "TestBasicIterationStatement";
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "i"));
            cmm.Statements.Add(new CodeIterationStatement(new CodeAssignStatement(new
                CodeVariableReferenceExpression("i"), new CodePrimitiveExpression(1)),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(8)),
                new CodeAssignStatement(new CodeVariableReferenceExpression("i"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.Multiply,
                new CodePrimitiveExpression(2)))));
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("i")));
            class1.Members.Add(cmm);

            cmm = new CodeMemberMethod();
            cmm.Name = "TestComplexIterationStatement";
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "i"));
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "a", new CodePrimitiveExpression(7)));
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "b"));
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "c", new CodePrimitiveExpression(9)));
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int)), "d", new CodePrimitiveExpression(2)));
            CodeIterationStatement iteration = new CodeIterationStatement();
            iteration.IncrementStatement = new CodeAssignStatement(new CodeVariableReferenceExpression("i"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.Add,
                new CodePrimitiveExpression(1)));
            iteration.InitStatement = new CodeAssignStatement(new CodeVariableReferenceExpression("i"), new
                CodePrimitiveExpression(0));
            iteration.TestExpression = (new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(2)));
            CodeConditionStatement secondIf = new CodeConditionStatement(new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("c"), CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(10)),
                new CodeAssignStatement(new CodeVariableReferenceExpression("d"), new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("d"), CodeBinaryOperatorType.Subtract, new CodePrimitiveExpression(1))));

            CodeIterationStatement secondFor = new CodeIterationStatement();
            secondFor.Statements.Add(secondIf);
            secondFor.IncrementStatement = new CodeAssignStatement(new CodeVariableReferenceExpression("b"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("b"), CodeBinaryOperatorType.Add,
                new CodePrimitiveExpression(1)));
            secondFor.InitStatement = new CodeAssignStatement(new CodeVariableReferenceExpression("b"), new
                CodePrimitiveExpression(0));
            secondFor.TestExpression = (new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("b"),
                CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(2)));
            secondFor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("d"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("d"), CodeBinaryOperatorType.Multiply,
                new CodePrimitiveExpression(2))));

            CodeConditionStatement firstIf = new CodeConditionStatement();
            firstIf.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.LessThan,
                new CodePrimitiveExpression(16));
            firstIf.TrueStatements.Add(secondFor);

            iteration.Statements.Add(firstIf);
            cmm.Statements.Add(iteration);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("d")));
            class1.Members.Add(cmm);

            AssertEqual(nspace,
                @"namespace NSPC {
                      public class ClassWithMethod {
                          public static int TestBasicIterationStatement() {
                              int i;
                              for (i = 1; (i < 8); i = (i * 2)) {
                              }
                              return i;
                          }
                          public static int TestComplexIterationStatement() {
                              int i;
                              int a = 7;
                              int b;
                              int c = 9;
                              int d = 2;
                              for (i = 0; (i < 2); i = (i + 1)) {
                                  if ((a < 16)) {
                                      for (b = 0; (b < 2); b = (b + 1)) {
                                          if ((c < 10)) {
                                              d = (d - 1);
                                          }
                                          d = (d * 2);
                                      }
                                  }
                              }
                              return d;
                          }
                      }
                  }");
        }

        [Fact]
        public void Indexers()
        {
            var nspace = new CodeNamespace("NSPC");

            var cd = new CodeTypeDeclaration("TEST");
            cd.IsClass = true;
            nspace.Types.Add(cd);

            CodeMemberField field = new CodeMemberField();
            field.Name = "PublicField";
            field.InitExpression = new CodeArrayCreateExpression(new CodeTypeReference(typeof(int)), new CodeExpression[]{
                new CodePrimitiveExpression(0), new CodePrimitiveExpression(0),new CodePrimitiveExpression(0),new CodePrimitiveExpression(0),
                new CodePrimitiveExpression(0),new CodePrimitiveExpression(0), new CodePrimitiveExpression(0)});
            field.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            field.Type = new CodeTypeReference(typeof(int[]));
            cd.Members.Add(field);

            // nonarray indexers
            CodeMemberProperty indexerProperty = new CodeMemberProperty();
            indexerProperty.Name = "Item";
            indexerProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            indexerProperty.Type = new CodeTypeReference(typeof(int));
            indexerProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "i"));
            // uses array indexer
            indexerProperty.SetStatements.Add(new CodeAssignStatement(new CodeArrayIndexerExpression(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression()
                , "PublicField"), new CodeExpression[] { new CodeVariableReferenceExpression("i") }),
                new CodeVariableReferenceExpression("value")));
            indexerProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeArrayIndexerExpression(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "PublicField"),
                new CodeVariableReferenceExpression("i"))));
            cd.Members.Add(indexerProperty);

            // nonarray indexers
            indexerProperty = new CodeMemberProperty();
            indexerProperty.Name = "Item";
            indexerProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            indexerProperty.Type = new CodeTypeReference(typeof(int));
            indexerProperty.SetStatements.Add(new CodeAssignStatement(new CodeArrayIndexerExpression(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression()
                , "PublicField"), new CodeExpression[] { new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.Add,
                                                            new CodeVariableReferenceExpression("b"))}),
                new CodeVariableReferenceExpression("value")));
            indexerProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeArrayIndexerExpression(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "PublicField"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.Add, new CodeVariableReferenceExpression("b")))));
            indexerProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "a"));
            indexerProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "b"));
            // uses array indexer

            cd.Members.Add(indexerProperty);

            cd = new CodeTypeDeclaration("UseTEST");
            cd.IsClass = true;
            nspace.Types.Add(cd);

            var cmm = new CodeMemberMethod();
            cmm.Name = "TestMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "i"));
            cmm.Attributes = MemberAttributes.Final | MemberAttributes.Public;
            cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("TEST"),
                "temp", new CodeObjectCreateExpression("TEST")));
            cmm.Statements.Add(new CodeAssignStatement(new CodeIndexerExpression(
                new CodeVariableReferenceExpression("temp"), new CodeExpression[] { new CodePrimitiveExpression(1) }),
                new CodeVariableReferenceExpression("i")));
            cmm.Statements.Add(new CodeAssignStatement(new CodeIndexerExpression(
                new CodeVariableReferenceExpression("temp"), new CodeExpression[]{new CodePrimitiveExpression(2),
                new CodePrimitiveExpression(4)}),
                new CodePrimitiveExpression(83)));
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                new CodeIndexerExpression(new CodeVariableReferenceExpression("temp"), new CodeExpression[] { new CodePrimitiveExpression(1) }),
                CodeBinaryOperatorType.Add,
                new CodeIndexerExpression(new CodeVariableReferenceExpression("temp"), new CodeExpression[] { new CodePrimitiveExpression(2), new CodePrimitiveExpression(4) }))));

            cd.Members.Add(cmm);

            AssertEqual(nspace,
                @"namespace NSPC {
                      public class TEST {
                          public int[] PublicField = new int[] { 0, 0, 0, 0, 0, 0, 0};

                          public int this[int i] {
                              get {
                                  return this.PublicField[i];
                              }
                              set {
                                  this.PublicField[i] = value;
                              }
                          }

                          public int this[int a, int b] {
                              get {
                                  return this.PublicField[(a + b)];
                              }
                              set {
                                  this.PublicField[(a + b)] = value;
                              }
                          }
                      }

                      public class UseTEST {
                          public int TestMethod(int i) {
                              TEST temp = new TEST();
                              temp[1] = i;
                              temp[2, 4] = 83;
                              return (temp[1] + temp[2, 4]);
                          }
                      }
                  }");
        }

        [Fact]
        public void Goto()
        {
            CodeNamespace ns = new CodeNamespace("NS");
            ns.Imports.Add(new CodeNamespaceImport("System"));

            CodeTypeDeclaration class1 = new CodeTypeDeclaration();
            class1.Name = "Test";
            class1.IsClass = true;
            ns.Types.Add(class1);

            // create first method to test gotos that jump ahead to a defined label with statement
            var cmm = new CodeMemberMethod();
            cmm.Name = "FirstMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "i");
            cmm.Parameters.Add(param);
            CodeConditionStatement condstmt = new CodeConditionStatement(new CodeBinaryOperatorExpression(
                                new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(1)),
                                new CodeGotoStatement("comehere"));
            cmm.Statements.Add(condstmt);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(6)));
            cmm.Statements.Add(new CodeLabeledStatement("comehere",
                                new CodeMethodReturnStatement(new CodePrimitiveExpression(7))));
            class1.Members.Add(cmm);

            // create second method to test gotos that jump ahead to a defined label without a statement attached to it
            cmm = new CodeMemberMethod();
            cmm.Name = "SecondMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            param = new CodeParameterDeclarationExpression(typeof(int), "i");
            cmm.Parameters.Add(param);
            condstmt = new CodeConditionStatement(new CodeBinaryOperatorExpression(
                                new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(1)),
                                new CodeGotoStatement("comehere"));
            cmm.Statements.Add(condstmt);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(6)));
            cmm.Statements.Add(new CodeLabeledStatement("comehere"));
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(7)));
            class1.Members.Add(cmm);

            // create third method to test gotos that jump to a previously defined label
            cmm = new CodeMemberMethod();
            cmm.Name = "ThirdMethod";
            cmm.ReturnType = new CodeTypeReference(typeof(int));
            cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            param = new CodeParameterDeclarationExpression(typeof(int), "i");
            cmm.Parameters.Add(param);
            CodeAssignStatement assignmt = new CodeAssignStatement(new CodeVariableReferenceExpression("i"),
                                new CodeBinaryOperatorExpression
                                (new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.Add,
                                new CodePrimitiveExpression(5)));
            cmm.Statements.Add(new CodeLabeledStatement("label", assignmt));
            condstmt = new CodeConditionStatement(new CodeBinaryOperatorExpression(
                                new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(1)),
                                new CodeGotoStatement("label"));
            cmm.Statements.Add(condstmt);
            cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("i")));
            class1.Members.Add(cmm);

            AssertEqual(ns,
                @"namespace NS {
                      using System;

                      public class Test {
                          public static int FirstMethod(int i) {
                              if ((i < 1)) {
                                  goto comehere;
                              }
                              return 6;
                          comehere:
                              return 7;
                          }

                          public static int SecondMethod(int i) {
                              if ((i < 1)) {
                                  goto comehere;
                              }
                              return 6;
                          comehere:
                              return 7;
                          }

                          public static int ThirdMethod(int i) {
                          label:
                              i = (i + 5);
                              if ((i < 1)) {
                                  goto label;
                              }
                              return i;
                          }
                      }
                  }");
        }

        [Fact]
        public void Conditionals()
        {
            CodeTypeDeclaration class1 = new CodeTypeDeclaration("ClassWithMethod");
            class1.IsClass = true;

            CodeMemberMethod retMethod = new CodeMemberMethod();
            retMethod.Name = "ReturnMethod";
            retMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            retMethod.ReturnType = new CodeTypeReference(typeof(int));
            retMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "intInput"));
            retMethod.Statements.Add(
                new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("intInput"),
                CodeBinaryOperatorType.LessThanOrEqual,
                new CodePrimitiveExpression(3)),
                CodeBinaryOperatorType.BooleanAnd,
                new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("intInput"),
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(2))),
                new CodeStatement[] { CreateVariableIncrementExpression("intInput", 16) },
                new CodeStatement[] { CreateVariableIncrementExpression("intInput", 1) }));
            retMethod.Statements.Add(new CodeConditionStatement(
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("intInput"), CodeBinaryOperatorType.LessThanOrEqual,
                new CodePrimitiveExpression(10)),
                new CodeAssignStatement(new CodeVariableReferenceExpression("intInput"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("intInput"),
                CodeBinaryOperatorType.Add, new CodePrimitiveExpression(11)))));
            retMethod.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("intInput")));
            class1.Members.Add(retMethod);

            AssertEqual(class1,
                @"public class ClassWithMethod {
                      public static int ReturnMethod(int intInput) {
                          if (((intInput <= 3)
                                      && (intInput == 2))) {
                              intInput = (intInput + 16);
                          }
                          else {
                              intInput = (intInput + 1);
                          }
                          if ((intInput <= 10)) {
                              intInput = (intInput + 11);
                          }
                          return intInput;
                      }
                  }");
        }

        [Fact]
        public void GlobalKeyword()
        {
            CodeNamespace ns = new CodeNamespace("Foo");
            ns.Comments.Add(new CodeCommentStatement("Foo namespace"));

            var cd = new CodeTypeDeclaration("Foo");
            ns.Types.Add(cd);

            string fieldName1 = "_verifyGlobalGeneration1";
            CodeMemberField field = new CodeMemberField();
            field.Name = fieldName1;
            field.Type = new CodeTypeReference(typeof(int), CodeTypeReferenceOptions.GlobalReference);
            field.Attributes = MemberAttributes.Public;
            field.InitExpression = new CodePrimitiveExpression(int.MaxValue);
            cd.Members.Add(field);

            string fieldName2 = "_verifyGlobalGeneration2";
            CodeMemberField field2 = new CodeMemberField();
            field2.Name = fieldName2;
            CodeTypeReference typeRef = new CodeTypeReference("System.Nullable", CodeTypeReferenceOptions.GlobalReference);
            typeRef.TypeArguments.Add(new CodeTypeReference(typeof(int), CodeTypeReferenceOptions.GlobalReference));
            field2.Type = typeRef;
            field2.InitExpression = new CodePrimitiveExpression(0);
            cd.Members.Add(field2);

            CodeMemberMethod method1 = new CodeMemberMethod();
            method1.Name = "TestMethod01";
            method1.Attributes = (method1.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public | MemberAttributes.Static;
            method1.ReturnType = new CodeTypeReference(typeof(int));
            method1.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(int.MaxValue)));
            cd.Members.Add(method1);

            CodeMemberMethod method2 = new CodeMemberMethod();
            method2.Name = "TestMethod02";
            method2.Attributes = (method2.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            method2.ReturnType = new CodeTypeReference(typeof(int));
            method2.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "iReturn"));

            CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression(
                                              new CodeMethodReferenceExpression(
                                              new CodeTypeReferenceExpression(new CodeTypeReference("Foo.Foo", CodeTypeReferenceOptions.GlobalReference)), "TestMethod01"));
            CodeAssignStatement cas = new CodeAssignStatement(new CodeVariableReferenceExpression("iReturn"), cmie);
            method2.Statements.Add(cas);
            method2.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("iReturn")));
            cd.Members.Add(method2);

            CodeMemberMethod method3 = new CodeMemberMethod();
            method3.Name = "TestMethod03";
            method3.Attributes = (method3.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            method3.ReturnType = new CodeTypeReference(typeof(int));
            method3.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "iReturn"));
            CodeTypeReferenceOptions ctro = CodeTypeReferenceOptions.GlobalReference;
            CodeTypeReference ctr = new CodeTypeReference(typeof(Math), ctro);
            cmie = new CodeMethodInvokeExpression(
                                              new CodeMethodReferenceExpression(
                                              new CodeTypeReferenceExpression(ctr), "Abs"), new CodeExpression[] { new CodePrimitiveExpression(-1) });
            cas = new CodeAssignStatement(new CodeVariableReferenceExpression("iReturn"), cmie);
            method3.Statements.Add(cas);
            method3.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("iReturn")));
            cd.Members.Add(method3);

            CodeMemberProperty property = new CodeMemberProperty();
            property.Name = "GlobalTestProperty1";
            property.Type = new CodeTypeReference(typeof(int));
            property.Attributes = (property.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            property.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(fieldName1)));
            property.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(fieldName1), new CodeVariableReferenceExpression("value")));
            cd.Members.Add(property);

            CodeMemberProperty property2 = new CodeMemberProperty();
            property2.Name = "GlobalTestProperty2";
            property2.Type = typeRef;
            property2.Attributes = (property.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            property2.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(fieldName2)));
            property2.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(fieldName2), new CodeVariableReferenceExpression("value")));
            cd.Members.Add(property2);

            AssertEqual(ns,
                @"// Foo namespace
                  namespace Foo {
                      public class Foo {
                          public int _verifyGlobalGeneration1 = 2147483647;

                          private global::System.Nullable<int> _verifyGlobalGeneration2 = 0;

                          public int GlobalTestProperty1 {
                              get {
                                  return _verifyGlobalGeneration1;
                              }
                              set {
                                  _verifyGlobalGeneration1 = value;
                              }
                          }

                          public global::System.Nullable<int> GlobalTestProperty2 {
                              get {
                                  return _verifyGlobalGeneration2;
                              }
                              set {
                                  _verifyGlobalGeneration2 = value;
                              }
                          }

                          public static int TestMethod01() {
                              return 2147483647;
                          }

                          public int TestMethod02() {
                              int iReturn;
                              iReturn = global::Foo.Foo.TestMethod01();
                              return iReturn;
                          }

                          public int TestMethod03() {
                              int iReturn;
                              iReturn = global::System.Math.Abs(-1);
                              return iReturn;
                          }
                      }
                  }");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void ProviderSupports()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                CodeDomProvider provider = GetProvider();

                var cu = new CodeCompileUnit();
                var nspace = new CodeNamespace("NSPC");
                nspace.Imports.Add(new CodeNamespaceImport("System"));
                nspace.Imports.Add(new CodeNamespaceImport("System.Drawing"));
                nspace.Imports.Add(new CodeNamespaceImport("System.Windows.Forms"));
                nspace.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
                cu.Namespaces.Add(nspace);

                var cd = new CodeTypeDeclaration("TEST");
                cd.IsClass = true;
                nspace.Types.Add(cd);

                // Arrays of Arrays
                var cmm = new CodeMemberMethod();
                cmm.Name = "ArraysOfArrays";
                cmm.ReturnType = new CodeTypeReference(typeof(int));
                cmm.Attributes = MemberAttributes.Final | MemberAttributes.Public;
                if (provider.Supports(GeneratorSupport.ArraysOfArrays))
                {
                    cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(int[][])),
                        "arrayOfArrays", new CodeArrayCreateExpression(typeof(int[][]),
                        new CodeArrayCreateExpression(typeof(int[]), new CodePrimitiveExpression(3), new CodePrimitiveExpression(4)),
                        new CodeArrayCreateExpression(typeof(int[]), new CodeExpression[] { new CodePrimitiveExpression(1) }))));
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeArrayIndexerExpression(
                        new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("arrayOfArrays"), new CodePrimitiveExpression(0))
                        , new CodePrimitiveExpression(1))));
                }
                else
                {
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(0)));
                }
                cd.Members.Add(cmm);

                // assembly attributes
                if (provider.Supports(GeneratorSupport.AssemblyAttributes))
                {
                    CodeAttributeDeclarationCollection attrs = cu.AssemblyCustomAttributes;
                    attrs.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyTitle", new
                        CodeAttributeArgument(new CodePrimitiveExpression("MyAssembly"))));
                    attrs.Add(new CodeAttributeDeclaration("System.Reflection.AssemblyVersion", new
                        CodeAttributeArgument(new CodePrimitiveExpression("1.0.6.2"))));
                }

                CodeTypeDeclaration class1 = new CodeTypeDeclaration();
                if (provider.Supports(GeneratorSupport.ChainedConstructorArguments))
                {
                    class1.Name = "Test2";
                    class1.IsClass = true;
                    nspace.Types.Add(class1);

                    class1.Members.Add(new CodeMemberField(new CodeTypeReference(typeof(String)), "stringField"));
                    CodeMemberProperty prop = new CodeMemberProperty();
                    prop.Name = "accessStringField";
                    prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    prop.Type = new CodeTypeReference(typeof(String));
                    prop.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                        "stringField")));
                    prop.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new
                        CodeThisReferenceExpression(), "stringField"),
                        new CodePropertySetValueReferenceExpression()));
                    class1.Members.Add(prop);

                    CodeConstructor cctor = new CodeConstructor();
                    cctor.Attributes = MemberAttributes.Public;
                    cctor.ChainedConstructorArgs.Add(new CodePrimitiveExpression("testingString"));
                    cctor.ChainedConstructorArgs.Add(new CodePrimitiveExpression(null));
                    cctor.ChainedConstructorArgs.Add(new CodePrimitiveExpression(null));
                    class1.Members.Add(cctor);

                    CodeConstructor cc = new CodeConstructor();
                    cc.Attributes = MemberAttributes.Public | MemberAttributes.Overloaded;
                    cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "p1"));
                    cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "p2"));
                    cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "p3"));
                    cc.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression()
                        , "stringField"), new CodeVariableReferenceExpression("p1")));
                    class1.Members.Add(cc);
                    // verify chained constructors work
                    cmm = new CodeMemberMethod();
                    cmm.Name = "ChainedConstructorUse";
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    cmm.ReturnType = new CodeTypeReference(typeof(String));
                    // utilize constructor
                    cmm.Statements.Add(new CodeVariableDeclarationStatement("Test2", "t", new CodeObjectCreateExpression("Test2")));
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodReferenceExpression(
                        new CodeVariableReferenceExpression("t"), "accessStringField")));
                    cd.Members.Add(cmm);
                }

                // complex expressions
                if (provider.Supports(GeneratorSupport.ComplexExpressions))
                {
                    cmm = new CodeMemberMethod();
                    cmm.Name = "ComplexExpressions";
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Attributes = MemberAttributes.Final | MemberAttributes.Public;
                    cmm.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "i"));
                    cmm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("i"),
                        new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.Multiply,
                        new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.Add,
                        new CodePrimitiveExpression(3)))));
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("i")));
                    cd.Members.Add(cmm);
                }

                if (provider.Supports(GeneratorSupport.DeclareEnums))
                {
                    CodeTypeDeclaration ce = new CodeTypeDeclaration("DecimalEnum");
                    ce.IsEnum = true;
                    nspace.Types.Add(ce);

                    // things to enumerate
                    for (int k = 0; k < 5; k++)
                    {
                        CodeMemberField Field = new CodeMemberField("System.Int32", "Num" + (k).ToString());
                        Field.InitExpression = new CodePrimitiveExpression(k);
                        ce.Members.Add(Field);
                    }
                    cmm = new CodeMemberMethod();
                    cmm.Name = "OutputDecimalEnumVal";
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "i");
                    cmm.Parameters.Add(param);
                    CodeBinaryOperatorExpression eq = new CodeBinaryOperatorExpression(
                        new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.ValueEquality,
                        new CodePrimitiveExpression(3));
                    CodeMethodReturnStatement truestmt = new CodeMethodReturnStatement(
                        new CodeCastExpression(typeof(int),
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DecimalEnum"), "Num3")));
                    CodeConditionStatement condstmt = new CodeConditionStatement(eq, truestmt);
                    cmm.Statements.Add(condstmt);

                    eq = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                        CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(4));
                    truestmt = new CodeMethodReturnStatement(new CodeCastExpression(typeof(int),
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DecimalEnum"), "Num4")));
                    condstmt = new CodeConditionStatement(eq, truestmt);
                    cmm.Statements.Add(condstmt);
                    eq = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                        CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(2));
                    truestmt = new CodeMethodReturnStatement(new CodeCastExpression(typeof(int),
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DecimalEnum"), "Num2")));
                    condstmt = new CodeConditionStatement(eq, truestmt);
                    cmm.Statements.Add(condstmt);

                    eq = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                        CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(1));
                    truestmt = new CodeMethodReturnStatement(new CodeCastExpression(typeof(int),
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DecimalEnum"), "Num1")));
                    condstmt = new CodeConditionStatement(eq, truestmt);
                    cmm.Statements.Add(condstmt);

                    eq = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("i"),
                        CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(0));
                    truestmt = new CodeMethodReturnStatement(new CodeCastExpression(typeof(int),
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DecimalEnum"), "Num0")));
                    condstmt = new CodeConditionStatement(eq, truestmt);
                    cmm.Statements.Add(condstmt);

                    cmm.ReturnType = new CodeTypeReference("System.int32");

                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                        new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(10))));
                    cd.Members.Add(cmm);
                }

                if (provider.Supports(GeneratorSupport.DeclareInterfaces))
                {
                    cmm = new CodeMemberMethod();
                    cmm.Name = "TestSingleInterface";
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "i"));
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    cmm.Statements.Add(new CodeVariableDeclarationStatement("TestSingleInterfaceImp", "t", new CodeObjectCreateExpression("TestSingleInterfaceImp")));
                    CodeMethodInvokeExpression methodinvoke = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("t")
                        , "InterfaceMethod");
                    methodinvoke.Parameters.Add(new CodeVariableReferenceExpression("i"));
                    cmm.Statements.Add(new CodeMethodReturnStatement(methodinvoke));
                    cd.Members.Add(cmm);

                    class1 = new CodeTypeDeclaration("InterfaceA");
                    class1.IsInterface = true;
                    nspace.Types.Add(class1);
                    cmm = new CodeMemberMethod();
                    cmm.Attributes = MemberAttributes.Public;
                    cmm.Name = "InterfaceMethod";
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
                    class1.Members.Add(cmm);

                    if (provider.Supports(GeneratorSupport.MultipleInterfaceMembers))
                    {
                        CodeTypeDeclaration classDecl = new CodeTypeDeclaration("InterfaceB");
                        classDecl.IsInterface = true;
                        nspace.Types.Add(classDecl);
                        cmm = new CodeMemberMethod();
                        cmm.Name = "InterfaceMethod";
                        cmm.Attributes = MemberAttributes.Public;
                        cmm.ReturnType = new CodeTypeReference(typeof(int));
                        cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
                        classDecl.Members.Add(cmm);

                        CodeTypeDeclaration class2 = new CodeTypeDeclaration("TestMultipleInterfaceImp");
                        class2.BaseTypes.Add(new CodeTypeReference("System.Object"));
                        class2.BaseTypes.Add(new CodeTypeReference("InterfaceB"));
                        class2.BaseTypes.Add(new CodeTypeReference("InterfaceA"));
                        class2.IsClass = true;
                        nspace.Types.Add(class2);
                        cmm = new CodeMemberMethod();
                        cmm.ImplementationTypes.Add(new CodeTypeReference("InterfaceA"));
                        cmm.ImplementationTypes.Add(new CodeTypeReference("InterfaceB"));
                        cmm.Name = "InterfaceMethod";
                        cmm.ReturnType = new CodeTypeReference(typeof(int));
                        cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
                        cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                        cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
                        class2.Members.Add(cmm);

                        cmm = new CodeMemberMethod();
                        cmm.Name = "TestMultipleInterfaces";
                        cmm.ReturnType = new CodeTypeReference(typeof(int));
                        cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "i"));
                        cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                        cmm.Statements.Add(new CodeVariableDeclarationStatement("TestMultipleInterfaceImp", "t", new CodeObjectCreateExpression("TestMultipleInterfaceImp")));
                        cmm.Statements.Add(new CodeVariableDeclarationStatement("InterfaceA", "interfaceAobject", new CodeCastExpression("InterfaceA",
                            new CodeVariableReferenceExpression("t"))));
                        cmm.Statements.Add(new CodeVariableDeclarationStatement("InterfaceB", "interfaceBobject", new CodeCastExpression("InterfaceB",
                            new CodeVariableReferenceExpression("t"))));
                        methodinvoke = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("interfaceAobject")
                            , "InterfaceMethod");
                        methodinvoke.Parameters.Add(new CodeVariableReferenceExpression("i"));
                        CodeMethodInvokeExpression methodinvoke2 = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("interfaceBobject")
                            , "InterfaceMethod");
                        methodinvoke2.Parameters.Add(new CodeVariableReferenceExpression("i"));
                        cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression(
                            methodinvoke,
                            CodeBinaryOperatorType.Subtract, methodinvoke2)));
                        cd.Members.Add(cmm);
                    }

                    class1 = new CodeTypeDeclaration("TestSingleInterfaceImp");
                    class1.BaseTypes.Add(new CodeTypeReference("System.Object"));
                    class1.BaseTypes.Add(new CodeTypeReference("InterfaceA"));
                    class1.IsClass = true;
                    nspace.Types.Add(class1);
                    cmm = new CodeMemberMethod();
                    cmm.ImplementationTypes.Add(new CodeTypeReference("InterfaceA"));
                    cmm.Name = "InterfaceMethod";
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
                    cmm.Attributes = MemberAttributes.Public;
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
                    class1.Members.Add(cmm);
                }

                if (provider.Supports(GeneratorSupport.DeclareValueTypes))
                {
                    CodeTypeDeclaration structA = new CodeTypeDeclaration("structA");
                    structA.IsStruct = true;

                    CodeTypeDeclaration structB = new CodeTypeDeclaration("structB");
                    structB.Attributes = MemberAttributes.Public;
                    structB.IsStruct = true;

                    CodeMemberField firstInt = new CodeMemberField(typeof(int), "int1");
                    firstInt.Attributes = MemberAttributes.Public;
                    structB.Members.Add(firstInt);

                    CodeMemberField innerStruct = new CodeMemberField("structB", "innerStruct");
                    innerStruct.Attributes = MemberAttributes.Public;

                    structA.Members.Add(structB);
                    structA.Members.Add(innerStruct);
                    nspace.Types.Add(structA);

                    CodeMemberMethod nestedStructMethod = new CodeMemberMethod();
                    nestedStructMethod.Name = "NestedStructMethod";
                    nestedStructMethod.ReturnType = new CodeTypeReference(typeof(int));
                    nestedStructMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    CodeVariableDeclarationStatement varStructA = new CodeVariableDeclarationStatement("structA", "varStructA");
                    nestedStructMethod.Statements.Add(varStructA);
                    nestedStructMethod.Statements.Add
                        (
                        new CodeAssignStatement
                        (
                        /* Expression1 */ new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("varStructA"), "innerStruct"), "int1"),
                        /* Expression1 */ new CodePrimitiveExpression(3)
                        )
                        );
                    nestedStructMethod.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("varStructA"), "innerStruct"), "int1")));
                    cd.Members.Add(nestedStructMethod);
                }
                if (provider.Supports(GeneratorSupport.EntryPointMethod))
                {
                    CodeEntryPointMethod cep = new CodeEntryPointMethod();
                    cd.Members.Add(cep);
                }
                // goto statements
                if (provider.Supports(GeneratorSupport.GotoStatements))
                {
                    cmm = new CodeMemberMethod();
                    cmm.Name = "GoToMethod";
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "i");
                    cmm.Parameters.Add(param);
                    CodeConditionStatement condstmt = new CodeConditionStatement(new CodeBinaryOperatorExpression(
                        new CodeVariableReferenceExpression("i"), CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(1)),
                        new CodeGotoStatement("comehere"));
                    cmm.Statements.Add(condstmt);
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(6)));
                    cmm.Statements.Add(new CodeLabeledStatement("comehere",
                        new CodeMethodReturnStatement(new CodePrimitiveExpression(7))));
                    cd.Members.Add(cmm);
                }
                if (provider.Supports(GeneratorSupport.NestedTypes))
                {
                    cmm = new CodeMemberMethod();
                    cmm.Name = "CallingPublicNestedScenario";
                    cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "i"));
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference
                        ("PublicNestedClassA+PublicNestedClassB2+PublicNestedClassC"), "t",
                        new CodeObjectCreateExpression(new CodeTypeReference
                        ("PublicNestedClassA+PublicNestedClassB2+PublicNestedClassC"))));
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("t"),
                        "publicNestedClassesMethod",
                        new CodeVariableReferenceExpression("i"))));
                    cd.Members.Add(cmm);

                    class1 = new CodeTypeDeclaration("PublicNestedClassA");
                    class1.IsClass = true;
                    nspace.Types.Add(class1);
                    CodeTypeDeclaration nestedClass = new CodeTypeDeclaration("PublicNestedClassB1");
                    nestedClass.IsClass = true;
                    nestedClass.TypeAttributes = TypeAttributes.NestedPublic;
                    class1.Members.Add(nestedClass);
                    nestedClass = new CodeTypeDeclaration("PublicNestedClassB2");
                    nestedClass.TypeAttributes = TypeAttributes.NestedPublic;
                    nestedClass.IsClass = true;
                    class1.Members.Add(nestedClass);
                    CodeTypeDeclaration innerNestedClass = new CodeTypeDeclaration("PublicNestedClassC");
                    innerNestedClass.TypeAttributes = TypeAttributes.NestedPublic;
                    innerNestedClass.IsClass = true;
                    nestedClass.Members.Add(innerNestedClass);
                    cmm = new CodeMemberMethod();
                    cmm.Name = "publicNestedClassesMethod";
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "a"));
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
                    innerNestedClass.Members.Add(cmm);
                }
                // Parameter Attributes
                if (provider.Supports(GeneratorSupport.ParameterAttributes))
                {
                    CodeMemberMethod method1 = new CodeMemberMethod();
                    method1.Name = "MyMethod";
                    method1.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    CodeParameterDeclarationExpression param1 = new CodeParameterDeclarationExpression(typeof(string), "blah");
                    param1.CustomAttributes.Add(
                        new CodeAttributeDeclaration(
                        "System.Xml.Serialization.XmlElementAttribute",
                        new CodeAttributeArgument(
                        "Form",
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("System.Xml.Schema.XmlSchemaForm"), "Unqualified")),
                        new CodeAttributeArgument(
                        "IsNullable",
                        new CodePrimitiveExpression(false))));
                    method1.Parameters.Add(param1);
                    cd.Members.Add(method1);
                }
                // public static members
                if (provider.Supports(GeneratorSupport.PublicStaticMembers))
                {
                    cmm = new CodeMemberMethod();
                    cmm.Name = "PublicStaticMethod";
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(16)));
                    cd.Members.Add(cmm);
                }
                // reference parameters
                if (provider.Supports(GeneratorSupport.ReferenceParameters))
                {
                    cmm = new CodeMemberMethod();
                    cmm.Name = "Work";
                    cmm.ReturnType = new CodeTypeReference("System.void");
                    cmm.Attributes = MemberAttributes.Static;
                    // add parameter with ref direction
                    CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "i");
                    param.Direction = FieldDirection.Ref;
                    cmm.Parameters.Add(param);
                    // add parameter with out direction
                    param = new CodeParameterDeclarationExpression(typeof(int), "j");
                    param.Direction = FieldDirection.Out;
                    cmm.Parameters.Add(param);
                    cmm.Statements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression("i"),
                        new CodeBinaryOperatorExpression(new CodeArgumentReferenceExpression("i"),
                        CodeBinaryOperatorType.Add, new CodePrimitiveExpression(4))));
                    cmm.Statements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression("j"),
                        new CodePrimitiveExpression(5)));
                    cd.Members.Add(cmm);

                    cmm = new CodeMemberMethod();
                    cmm.Name = "CallingWork";
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    CodeParameterDeclarationExpression parames = new CodeParameterDeclarationExpression(typeof(int), "a");
                    cmm.Parameters.Add(parames);
                    cmm.ReturnType = new CodeTypeReference("System.int32");
                    cmm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("a"),
                        new CodePrimitiveExpression(10)));
                    cmm.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "b"));
                    // invoke the method called "work"
                    CodeMethodInvokeExpression methodinvoked = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression
                        (new CodeTypeReferenceExpression("TEST"), "Work"));
                    // add parameter with ref direction
                    CodeDirectionExpression parameter = new CodeDirectionExpression(FieldDirection.Ref,
                        new CodeVariableReferenceExpression("a"));
                    methodinvoked.Parameters.Add(parameter);
                    // add parameter with out direction
                    parameter = new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression("b"));
                    methodinvoked.Parameters.Add(parameter);
                    cmm.Statements.Add(methodinvoked);
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeBinaryOperatorExpression
                        (new CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.Add, new CodeVariableReferenceExpression("b"))));
                    cd.Members.Add(cmm);
                }
                if (provider.Supports(GeneratorSupport.ReturnTypeAttributes))
                {
                    CodeMemberMethod function1 = new CodeMemberMethod();
                    function1.Name = "MyFunction";
                    function1.ReturnType = new CodeTypeReference(typeof(string));
                    function1.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    function1.ReturnTypeCustomAttributes.Add(new
                        CodeAttributeDeclaration("System.Xml.Serialization.XmlIgnoreAttribute"));
                    function1.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("System.Xml.Serialization.XmlRootAttribute", new
                        CodeAttributeArgument("Namespace", new CodePrimitiveExpression("Namespace Value")), new
                        CodeAttributeArgument("ElementName", new CodePrimitiveExpression("Root, hehehe"))));
                    function1.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression("Return")));
                    cd.Members.Add(function1);
                }
                if (provider.Supports(GeneratorSupport.StaticConstructors))
                {
                    cmm = new CodeMemberMethod();
                    cmm.Name = "TestStaticConstructor";
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "a");
                    cmm.Parameters.Add(param);
                    // utilize constructor
                    cmm.Statements.Add(new CodeVariableDeclarationStatement("Test4", "t", new CodeObjectCreateExpression("Test4")));
                    // set then get number
                    cmm.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("t"), "i")
                        , new CodeVariableReferenceExpression("a")));
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeMethodReferenceExpression(
                        new CodeVariableReferenceExpression("t"), "i")));
                    cd.Members.Add(cmm);

                    class1 = new CodeTypeDeclaration();
                    class1.Name = "Test4";
                    class1.IsClass = true;
                    nspace.Types.Add(class1);

                    class1.Members.Add(new CodeMemberField(new CodeTypeReference(typeof(int)), "number"));
                    CodeMemberProperty prop = new CodeMemberProperty();
                    prop.Name = "i";
                    prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    prop.Type = new CodeTypeReference(typeof(int));
                    prop.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("number")));
                    prop.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("number"),
                        new CodePropertySetValueReferenceExpression()));
                    class1.Members.Add(prop);
                    CodeTypeConstructor ctc = new CodeTypeConstructor();
                    class1.Members.Add(ctc);
                }
                if (provider.Supports(GeneratorSupport.TryCatchStatements))
                {
                    cmm = new CodeMemberMethod();
                    cmm.Name = "TryCatchMethod";
                    cmm.ReturnType = new CodeTypeReference(typeof(int));
                    cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                    CodeParameterDeclarationExpression param = new CodeParameterDeclarationExpression(typeof(int), "a");
                    cmm.Parameters.Add(param);

                    CodeTryCatchFinallyStatement tcfstmt = new CodeTryCatchFinallyStatement();
                    tcfstmt.FinallyStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("a"), new
                        CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("a"), CodeBinaryOperatorType.Add,
                        new CodePrimitiveExpression(5))));
                    cmm.Statements.Add(tcfstmt);
                    cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("a")));
                    cd.Members.Add(cmm);
                }
                if (provider.Supports(GeneratorSupport.DeclareEvents))
                {
                    CodeNamespace ns = new CodeNamespace();
                    ns.Name = "MyNamespace";
                    ns.Imports.Add(new CodeNamespaceImport("System"));
                    ns.Imports.Add(new CodeNamespaceImport("System.Drawing"));
                    ns.Imports.Add(new CodeNamespaceImport("System.Windows.Forms"));
                    ns.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
                    cu.Namespaces.Add(ns);
                    class1 = new CodeTypeDeclaration("Test");
                    class1.IsClass = true;
                    class1.BaseTypes.Add(new CodeTypeReference("Form"));
                    ns.Types.Add(class1);

                    CodeMemberField mfield = new CodeMemberField(new CodeTypeReference("Button"), "b");
                    mfield.InitExpression = new CodeObjectCreateExpression(new CodeTypeReference("Button"));
                    class1.Members.Add(mfield);

                    CodeConstructor ctor = new CodeConstructor();
                    ctor.Attributes = MemberAttributes.Public;
                    ctor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                        "Size"), new CodeObjectCreateExpression(new CodeTypeReference("Size"),
                        new CodePrimitiveExpression(600), new CodePrimitiveExpression(600))));
                    ctor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("b"),
                        "Text"), new CodePrimitiveExpression("Test")));
                    ctor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("b"),
                        "TabIndex"), new CodePrimitiveExpression(0)));
                    ctor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("b"),
                        "Location"), new CodeObjectCreateExpression(new CodeTypeReference("Point"),
                        new CodePrimitiveExpression(400), new CodePrimitiveExpression(525))));
                    ctor.Statements.Add(new CodeAttachEventStatement(new CodeEventReferenceExpression(new
                        CodeThisReferenceExpression(), "MyEvent"), new CodeDelegateCreateExpression(new CodeTypeReference("EventHandler")
                        , new CodeThisReferenceExpression(), "b_Click")));
                    class1.Members.Add(ctor);

                    CodeMemberEvent evt = new CodeMemberEvent();
                    evt.Name = "MyEvent";
                    evt.Type = new CodeTypeReference("System.EventHandler");
                    evt.Attributes = MemberAttributes.Public;
                    class1.Members.Add(evt);

                    cmm = new CodeMemberMethod();
                    cmm.Name = "b_Click";
                    cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "sender"));
                    cmm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(EventArgs), "e"));
                    class1.Members.Add(cmm);
                }

                AssertEqual(cu,
                    @"//------------------------------------------------------------------------------
                    // <auto-generated>
                    //     This code was generated by a tool.
                    //
                    //     Changes to this file may cause incorrect behavior and will be lost if
                    //     the code is regenerated.
                    // </auto-generated>
                    //------------------------------------------------------------------------------

                    [assembly: System.Reflection.AssemblyTitle(""MyAssembly"")]
                    [assembly: System.Reflection.AssemblyVersion(""1.0.6.2"")]

                    namespace NSPC {
                        using System;
                        using System.Drawing;
                        using System.Windows.Forms;
                        using System.ComponentModel;

                        public class TEST {

                            public int ArraysOfArrays() {
                                int[][] arrayOfArrays = new int[][] {
                                        new int[] { 3, 4},
                                        new int[] { 1}};
                                return arrayOfArrays[0][1];
                            }

                            public static string ChainedConstructorUse() {
                                Test2 t = new Test2();
                                return t.accessStringField;
                            }

                            public int ComplexExpressions(int i) {
                                i = (i * (i + 3));
                                return i;
                            }

                            public static int OutputDecimalEnumVal(int i) {
                                if ((i == 3)) {
                                    return ((int)(DecimalEnum.Num3));
                                }
                                if ((i == 4)) {
                                    return ((int)(DecimalEnum.Num4));
                                }
                                if ((i == 2)) {
                                    return ((int)(DecimalEnum.Num2));
                                }
                                if ((i == 1)) {
                                    return ((int)(DecimalEnum.Num1));
                                }
                                if ((i == 0)) {
                                    return ((int)(DecimalEnum.Num0));
                                }
                                return (i + 10);
                            }

                            public static int TestSingleInterface(int i) {
                                TestSingleInterfaceImp t = new TestSingleInterfaceImp();
                                return t.InterfaceMethod(i);
                            }

                            public static int TestMultipleInterfaces(int i) {
                                TestMultipleInterfaceImp t = new TestMultipleInterfaceImp();
                                InterfaceA interfaceAobject = ((InterfaceA)(t));
                                InterfaceB interfaceBobject = ((InterfaceB)(t));
                                return (interfaceAobject.InterfaceMethod(i) - interfaceBobject.InterfaceMethod(i));
                            }

                            public static int NestedStructMethod() {
                                structA varStructA;
                                varStructA.innerStruct.int1 = 3;
                                return varStructA.innerStruct.int1;
                            }

                            public static void Main() { }

                            public int GoToMethod(int i) {
                                if ((i < 1)) {
                                    goto comehere;
                                }
                                return 6;
                            comehere:
                                return 7;
                            }

                            public static int CallingPublicNestedScenario(int i) {
                                PublicNestedClassA.PublicNestedClassB2.PublicNestedClassC t = new PublicNestedClassA.PublicNestedClassB2.PublicNestedClassC();
                                return t.publicNestedClassesMethod(i);
                            }

                            public void MyMethod([System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable=false)] string blah) {
                            }

                            public static int PublicStaticMethod() {
                                return 16;
                            }

                            static void Work(ref int i, out int j) {
                                i = (i + 4);
                                j = 5;
                            }

                            public static int CallingWork(int a) {
                                a = 10;
                                int b;
                                TEST.Work(ref a, out b);
                                return (a + b);
                            }

                            [return: System.Xml.Serialization.XmlIgnoreAttribute()]
                            [return: System.Xml.Serialization.XmlRootAttribute(Namespace=""Namespace Value"", ElementName=""Root, hehehe"")]
                            public string MyFunction() {
                                return ""Return"";
                            }

                            public static int TestStaticConstructor(int a) {
                                Test4 t = new Test4();
                                t.i = a;
                                return t.i;
                            }

                            public static int TryCatchMethod(int a) {
                                try {
                                }
                                finally {
                                    a = (a + 5);
                                }
                                return a;
                            }
                        }

                        public class Test2 {

                            private string stringField;

                            public Test2() :
                                    this(""testingString"", null, null) {
                            }

                            public Test2(string p1, string p2, string p3) {
                                this.stringField = p1;
                            }

                            public string accessStringField {
                                get {
                                    return this.stringField;
                                }
                                set {
                                    this.stringField = value;
                                }
                            }
                        }

                        public enum DecimalEnum {
                            Num0 = 0,
                            Num1 = 1,
                            Num2 = 2,
                            Num3 = 3,
                            Num4 = 4,
                        }

                        public interface InterfaceA {
                            int InterfaceMethod(int a);
                        }

                        public interface InterfaceB {
                            int InterfaceMethod(int a);
                        }

                        public class TestMultipleInterfaceImp : object, InterfaceB, InterfaceA {
                            public int InterfaceMethod(int a) {
                                return a;
                            }
                        }

                        public class TestSingleInterfaceImp : object, InterfaceA {
                            public virtual int InterfaceMethod(int a) {
                                return a;
                            }
                        }

                        public struct structA {
                            public structB innerStruct;

                            public struct structB {
                                public int int1;
                            }
                        }

                        public class PublicNestedClassA {

                            public class PublicNestedClassB1 { }

                            public class PublicNestedClassB2 {
                                public class PublicNestedClassC {
                                    public int publicNestedClassesMethod(int a) {
                                        return a;
                                    }
                                }
                            }
                        }

                        public class Test4 {

                            private int number;

                            static Test4() {
                            }

                            public int i {
                                get {
                                    return number;
                                }
                                set {
                                    number = value;
                                }
                            }
                        }
                    }
                    namespace MyNamespace {
                        using System;
                        using System.Drawing;
                        using System.Windows.Forms;
                        using System.ComponentModel;

                        public class Test : Form {
                            private Button b = new Button();

                            public Test() {
                                this.Size = new Size(600, 600);
                                b.Text = ""Test"";
                                b.TabIndex = 0;
                                b.Location = new Point(400, 525);
                                this.MyEvent += new EventHandler(this.b_Click);
                            }

                            public event System.EventHandler MyEvent;

                            private void b_Click(object sender, System.EventArgs e) {
                            }
                        }
                    }");
            }
        }

        [Fact]
        public void GenericTypesAndConstraints()
        {
            CodeNamespace ns = new CodeNamespace("NS");
            ns.Imports.Add(new CodeNamespaceImport("System"));
            ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));

            CodeTypeDeclaration class1 = new CodeTypeDeclaration();
            class1.Name = "MyDictionary";
            class1.BaseTypes.Add(new CodeTypeReference("Dictionary", new CodeTypeReference[] { new CodeTypeReference("TKey"), new CodeTypeReference("TValue"), }));
            CodeTypeParameter kType = new CodeTypeParameter("TKey");
            kType.HasConstructorConstraint = true;
            kType.Constraints.Add(new CodeTypeReference(typeof(IComparable)));
            kType.CustomAttributes.Add(new CodeAttributeDeclaration(
                "System.ComponentModel.DescriptionAttribute", new CodeAttributeArgument(new CodePrimitiveExpression("KeyType"))));

            CodeTypeReference iComparableT = new CodeTypeReference("IComparable");
            iComparableT.TypeArguments.Add(new CodeTypeReference(kType));
            kType.Constraints.Add(iComparableT);

            CodeTypeParameter vType = new CodeTypeParameter("TValue");
            vType.Constraints.Add(new CodeTypeReference(typeof(IList<string>)));
            vType.CustomAttributes.Add(new CodeAttributeDeclaration(
                "System.ComponentModel.DescriptionAttribute", new CodeAttributeArgument(new CodePrimitiveExpression("ValueType"))));

            class1.TypeParameters.Add(kType);
            class1.TypeParameters.Add(vType);
            ns.Types.Add(class1);

            // Declare a generic method.
            CodeMemberMethod printMethod = new CodeMemberMethod();
            CodeTypeParameter sType = new CodeTypeParameter("S");
            sType.HasConstructorConstraint = true;
            CodeTypeParameter tType = new CodeTypeParameter("T");
            sType.HasConstructorConstraint = true;

            printMethod.Name = "Nop";
            printMethod.TypeParameters.Add(sType);
            printMethod.TypeParameters.Add(tType);
            printMethod.Attributes = MemberAttributes.Public;
            class1.Members.Add(printMethod);

            var class2 = new CodeTypeDeclaration();
            class2.Name = "Demo";

            var methodMain = new CodeEntryPointMethod();
            var myClass = new CodeTypeReference(
                "MyDictionary",
                new CodeTypeReference[] {
                    new CodeTypeReference(typeof(int)),
                    new CodeTypeReference("List",
                       new CodeTypeReference[]
                            {new CodeTypeReference("System.String") })});
            methodMain.Statements.Add(new CodeVariableDeclarationStatement(myClass, "dict", new CodeObjectCreateExpression(myClass)));
            string dictionaryTypeName = typeof(System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>[]).FullName;

            var dictionaryType = new CodeTypeReference(dictionaryTypeName);
            methodMain.Statements.Add(
                  new CodeVariableDeclarationStatement(dictionaryType, "dict2",
                     new CodeArrayCreateExpression(dictionaryType, new CodeExpression[1] { new CodePrimitiveExpression(null) })));

            class2.Members.Add(methodMain);
            ns.Types.Add(class2);

            AssertEqual(ns,
                @"namespace NS {
                      using System;
                      using System.Collections.Generic;
                      public class MyDictionary<[System.ComponentModel.DescriptionAttribute(""KeyType"")] TKey, [System.ComponentModel.DescriptionAttribute(""ValueType"")]  TValue> : Dictionary<TKey, TValue>
                          where TKey : System.IComparable, IComparable<TKey>, new ()
                          where TValue : System.Collections.Generic.IList<string>
                      {
                          public virtual void Nop<S, T>() where S : new() { }
                      }

                      public class Demo
                      {
                          public static void Main()
                          {
                              MyDictionary<int, List<string>> dict = new MyDictionary<int, List<string>>();
                              System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>[] dict2 = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>[] { null};
                          }
                      }
                  }");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/1913", TargetFrameworkMonikers.NetFramework)]
        public void SpecialNewLineCharactersInStringsDoneCorrectly()
        {
            var cd = new CodeTypeDeclaration("ClassWithStringFields") { IsClass = true };

            var field = new CodeMemberField("System.String", "StringWithSpecialNewLines");
            field.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            field.InitExpression = new CodePrimitiveExpression("\u0085\u2028\u2029");
            cd.Members.Add(field);

            AssertEqual(cd,
                @"public class ClassWithStringFields {
                      public static string StringWithSpecialNewLines = ""\u0085\u2028\u2029"";
                  }");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void MethodTypeParameterConstraintLineOnlyAddedForTypesThatHaveConstraints()
        {
            var codeTypeDeclaration = new CodeTypeDeclaration("ClassWithGenericMethod") { IsClass = true };

            var method = new CodeMemberMethod();
            method.Name = "Test";
            var t1 = new CodeTypeParameter("T1");
            var t2 = new CodeTypeParameter("T2");
            t2.Constraints.Add("MyBaseClass");
            var t3 = new CodeTypeParameter("T3");
            var t4 = new CodeTypeParameter("T4");

            method.TypeParameters.Add(t1);
            method.TypeParameters.Add(t2);
            method.TypeParameters.Add(t3);
            method.TypeParameters.Add(t4);

            codeTypeDeclaration.Members.Add(method);

            AssertEqualPreserveLineBreaks(codeTypeDeclaration,
                @"
                  public class ClassWithGenericMethod {

                      private void Test<T1, T2, T3, T4>()
                          where T2 : MyBaseClass {
                      }
                  }
                ");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The bug was present on .NET Framework: https://github.com/dotnet/runtime/issues/56267")]
        public void OrdinaryCommentsDoNotAccidentallyBecomeDocumentationComments()
        {
            var codeTypeDeclaration = new CodeTypeDeclaration("ClassWithComment")
            {
                IsClass = true,
                Comments =
                {
                    new CodeCommentStatement(
                        "/ Lines starting with exactly one slash" + Environment.NewLine +
                        "/ each get a separating space," + Environment.NewLine +
                        "but other lines do not get a space. This way generated files only change on tool upgrade where there were generation bugs."+ Environment.NewLine +
                        "// This includes lines starting with more than one slash.",
                        docComment: false),
                },
            };

            AssertEqualPreserveLineBreaks(codeTypeDeclaration,
                @"
                  // / Lines starting with exactly one slash
                  // / each get a separating space,
                  //but other lines do not get a space. This way generated files only change on tool upgrade where there were generation bugs.
                  //// This includes lines starting with more than one slash.
                  public class ClassWithComment {
                  }
                ");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The bug was present on .NET Framework: https://github.com/dotnet/runtime/issues/56267")]
        public void DocumentationCommentsDoNotAccidentallyBecomeOrdinaryComments()
        {
            var codeTypeDeclaration = new CodeTypeDeclaration("ClassWithComment")
            {
                IsClass = true,
                Comments =
                {
                    new CodeCommentStatement(
                        "/ Lines starting with a slash each get a separating space," + Environment.NewLine +
                        "// including lines starting with more than one slash," + Environment.NewLine +
                        "but other lines do not get a space. This way generated files only change on tool upgrade where there were generation bugs.",
                        docComment: true),
                },
            };

            AssertEqualPreserveLineBreaks(codeTypeDeclaration,
                @"
                  /// / Lines starting with a slash each get a separating space,
                  /// // including lines starting with more than one slash,
                  ///but other lines do not get a space. This way generated files only change on tool upgrade where there were generation bugs.
                  public class ClassWithComment {
                  }
                ");
        }
    }
}
