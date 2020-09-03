using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.ManualTypeMarshallingAnalyzer;

using VerifyCS = DllImportGenerator.Test.Verifiers.CSharpAnalyzerVerifier<Microsoft.Interop.ManualTypeMarshallingAnalyzer>;

namespace DllImportGenerator.Test
{
    public class ManualTypeMarshallingAnalyzerTests
    {
        public static IEnumerable<object[]> NonBlittableTypeMarkedBlittable_ReportsDiagnostic_TestData {
            get
            {
                yield return new object[]
                {
                    @"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public bool field;
}
"
                };
                yield return new object[]
                {
                    @"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public char field;
}
"
                };
                yield return new object[]
                {
                    
@"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public string field;
}
"
                };
            }
        }

        [MemberData(nameof(NonBlittableTypeMarkedBlittable_ReportsDiagnostic_TestData))]
        [Theory]
        public async Task NonBlittableTypeMarkedBlittable_ReportsDiagnostic(string source)
        {
            var diagnostic = VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithSpan(4, 2, 4, 15).WithArguments("S");
            await VerifyCS.VerifyAnalyzerAsync(source, diagnostic);
        }

        [Fact]
        public async Task TypeWithBlittablePrimitiveFieldsMarkedBlittableNoDiagnostic()
        {

            string source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public int field;
}
";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task TypeWithBlittableStructFieldsMarkedBlittableNoDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public T field;
}

[BlittableType]
struct T
{
    public int field;
}
";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task TypeMarkedBlittableWithNonBlittableFieldsMarkedBlittableReportDiagnosticOnFieldTypeDefinition()
        {
            string source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public T field;
}

[BlittableType]
struct T
{
    public bool field;
}
";
            var diagnostic = VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithSpan(10, 2, 10, 15).WithArguments("T");
            await VerifyCS.VerifyAnalyzerAsync(source, diagnostic);
        }

        [Fact]
        public async Task NonUnmanagedTypeMarkedBlittable_ReportsDiagnosticOnStructType()
        {
            string source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public T field;
}

[BlittableType]
struct T
{
    public string field;
}
";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithSpan(10, 2, 10, 15).WithArguments("T"));
        }

        [Fact]
        public async Task BlittableTypeWithNonBlittableStaticField_DoesNotReportDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public static string Static;
    public int instance;
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NullNativeType_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[NativeMarshalling(null)]
struct S
{
    public string s;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeNonNullRule).WithSpan(4, 2, 4, 25).WithArguments("S"));
        }

        [Fact]
        public async Task NonNamedNativeType_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(int*))]
struct S
{
    public string s;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveRequiredShapeRule).WithSpan(4, 2, 4, 33).WithArguments("int*", "S"));
        }

        [Fact]
        public async Task NonBlittableNativeType_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

struct Native
{
    private string value;

    public Native(S s)
    {
        value = s.s;
    }

    public S ToManaged() => new S { s = value };
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithSpan(10, 1, 20, 2).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task ClassNativeType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

class Native
{
    private IntPtr value;

    public Native(S s)
    {
    }

    public S ToManaged() => new S();
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveRequiredShapeRule).WithSpan(11, 1, 20, 2).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task BlittableNativeType_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

[BlittableType]
struct Native
{
    private IntPtr value;

    public Native(S s)
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BlittableNativeWithNonBlittableValueProperty_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

[BlittableType]
struct Native
{
    private IntPtr value;

    public Native(S s)
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public string Value { get => null; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithSpan(23, 5, 23, 48).WithArguments("string", "S"));
        }

        [Fact]
        public async Task NonBlittableNativeTypeWithBlittableValueProperty_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

struct Native
{
    private string value;

    public Native(S s)
    {
        value = s.s;
    }

    public S ToManaged() => new S() { s = value };

    public IntPtr Value { get => IntPtr.Zero; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task ClassNativeTypeWithValueProperty_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

class Native
{
    private string value;

    public Native(S s)
    {
        value = s.s;
    }

    public S ToManaged() => new S() { s = value };

    public IntPtr Value { get => IntPtr.Zero; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveRequiredShapeRule).WithSpan(11, 1, 23, 2).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task NonBlittableGetPinnableReferenceReturnType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public char c;

    public ref char GetPinnableReference() => ref c;
}

unsafe struct Native
{
    private IntPtr value;

    public Native(S s)
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public IntPtr Value { get => IntPtr.Zero; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(GetPinnableReferenceReturnTypeBlittableRule).WithSpan(10, 5, 10, 53));
        }

        
        [Fact]
        public async Task BlittableGetPinnableReferenceReturnType_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;

    public ref byte GetPinnableReference() => ref c;
}

unsafe struct Native
{
    private IntPtr value;

    public Native(S s) : this()
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public IntPtr Value { get => IntPtr.Zero; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }
        
        [Fact]
        public async Task TypeWithGetPinnableReferenceNonPointerReturnType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;

    public ref byte GetPinnableReference() => ref c;
}

unsafe struct Native
{
    private IntPtr value;

    public Native(S s) : this()
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public int Value { get => 0; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBePointerSizedRule).WithSpan(24, 5, 24, 42).WithArguments("int", "S"));
        }

        [Fact]
        public async Task BlittableValueTypeWithNoFields_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NativeTypeWithNoMarshallingMethods_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[BlittableType]
struct Native
{
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveRequiredShapeRule).WithSpan(11, 1, 14, 2).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task NativeTypeWithOnlyConstructor_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[BlittableType]
struct Native
{
    public Native(S s) {}
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NativeTypeWithOnlyToManagedMethod_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[BlittableType]
struct Native
{
    public S ToManaged() => new S();
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }
        
        [Fact]
        public async Task NativeTypeWithOnlyStackallocConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[BlittableType]
struct Native
{
    public Native(S s, Span<byte> buffer) {}
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule).WithSpan(11, 1, 15, 2).WithArguments("Native"));
        }

        [Fact]
        public async Task TypeWithOnlyNativeStackallocConstructorAndGetPinnableReference_ReportsDiagnostics()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
    public ref byte GetPinnableReference() => ref c;
}

struct Native
{
    public Native(S s, Span<byte> buffer) {}

    public IntPtr Value => IntPtr.Zero;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule).WithSpan(12, 1, 17, 2).WithArguments("Native"),
                VerifyCS.Diagnostic(GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule).WithSpan(5, 2, 5, 35).WithArguments("S", "Native"));
        }

        [Fact]
        public async Task NativeTypeWithConstructorAndSetOnlyValueProperty_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

struct Native
{
    public Native(S s) {}

    public IntPtr Value { set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ValuePropertyMustHaveGetterRule).WithSpan(15, 5, 15, 35).WithArguments("Native"));
        }

        [Fact]
        public async Task NativeTypeWithToManagedAndGetOnlyValueProperty_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

struct Native
{
    public S ToManaged() => new S();

    public IntPtr Value => IntPtr.Zero;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ValuePropertyMustHaveSetterRule).WithSpan(15, 5, 15, 40).WithArguments("Native"));
        }
        
        [Fact]
        public async Task BlittableNativeTypeOnMarshalUsingParameter_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

struct S
{
    public string s;
}

[BlittableType]
struct Native
{
    private IntPtr value;

    public Native(S s)
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();
}


static class Test
{
    static void Foo([MarshalUsing(typeof(Native))] S s)
    {}
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NonBlittableNativeTypeOnMarshalUsingParameter_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

struct S
{
    public string s;
}

struct Native
{
    private string value;

    public Native(S s) : this()
    {
    }

    public S ToManaged() => new S();
}


static class Test
{
    static void Foo([MarshalUsing(typeof(Native))] S s)
    {}
}
";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithSpan(10, 1, 19, 2).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task NonBlittableNativeTypeOnMarshalUsingReturn_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

struct S
{
    public string s;
}

struct Native
{
    private string value;

    public Native(S s) : this()
    {
    }

    public S ToManaged() => new S();
}


static class Test
{
    [return: MarshalUsing(typeof(Native))]
    static S Foo() => new S();
}
";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithSpan(10, 1, 19, 2).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task NonBlittableNativeTypeOnMarshalUsingField_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

struct S
{
    public string s;
}

struct Native
{
    private string value;

    public Native(S s) : this()
    {
    }

    public S ToManaged() => new S();
}


struct Test
{
    [MarshalUsing(typeof(Native))]
    S s;
}
";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithSpan(10, 1, 19, 2).WithArguments("Native", "S"));
        }

        
        [Fact]
        public async Task GenericNativeTypeWithValueTypeValueProperty_DoesNotReportDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native<S>))]
struct S
{
    public string s;
}

struct Native<T>
    where T : new()
{
    public Native(T s)
    {
        Value = 0;
    }

    public T ToManaged() => new T();

    public int Value { get; set; }
}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task GenericNativeTypeWithGenericMemberInstantiatedWithBlittable_DoesNotReportDiagnostic()
        {
            
            string source = @"
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native<int>))]
struct S
{
    public string s;
}

struct Native<T>
    where T : new()
{
    public Native(S s)
    {
        Value = new T();
    }

    public S ToManaged() => new S();

    public T Value { get; set; }
}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task ValueTypeContainingPointerBlittableType_DoesNotReportDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
unsafe struct S
{
    private int* ptr;
}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task ValueTypeContainingPointerToNonBlittableType_ReportsDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
unsafe struct S
{
    private bool* ptr;
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithSpan(4, 2, 4, 15).WithArguments("S"));
        }

        [Fact]
        public async Task BlittableValueTypeContainingPointerToSelf_DoesNotReportDiagnostic()
        {

            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
unsafe struct S
{
    private int fld;
    private S* ptr;
}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NonBlittableValueTypeContainingPointerToSelf_ReportsDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
unsafe struct S
{
    private bool fld;
    private S* ptr;
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithSpan(4, 2, 4, 15).WithArguments("S"));
        }

        [Fact]
        public async Task BlittableTypeContainingFunctionPointer_DoesNotReportDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
unsafe struct S
{
    private delegate*<int> ptr;
}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BlittableGenericTypeInBlittableType_DoesNotReportDiagnostic()
        {
            
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct G<T>
{
    T fld;
}

[BlittableType]
unsafe struct S
{
    private G<int> field;
}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NonBlittableGenericTypeInBlittableType_ReportsDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct G<T>
{
    T fld;
}

[BlittableType]
unsafe struct S
{
    private G<string> field;
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithSpan(10, 2, 10, 15).WithArguments("S"));
        }

        [Fact]
        public async Task BlittableGenericTypeTypeParameterReferenceType_ReportsDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct G<T> where T : class
{
    T fld;
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithSpan(4, 2, 4, 15).WithArguments("G<T>"));
        }

        [Fact]
        public async Task BlittableGenericTypeContainingGenericType_DoesNotReportDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct G<T>
{
    T fld;
}

[BlittableType]
struct F<T>
{
    G<T> fld;
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BlittableNestedGenericType_DoesNotReportDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

struct C<T>
{
    [BlittableType]
    public struct G
    {
        T fld;
    }
}

[BlittableType]
struct S
{
    C<int>.G g;
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BlittableNestedGenericTypeWithReferenceTypeGenericParameter_DoesNotReportDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

struct C<T> where T : class
{
    [BlittableType]
    struct G
    {
        T fld;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithSpan(6, 6, 6, 19).WithArguments("C<T>.G"));
        }

        [Fact]
        public async Task BlittableGenericTypeWithReferenceTypeParameterNotUsedInFieldType_DoesNotReportDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct G<T, U> where U : class
{
    T fld;
}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }
}