using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.ManualTypeMarshallingAnalyzer;

using VerifyCS = DllImportGenerator.UnitTests.Verifiers.CSharpAnalyzerVerifier<Microsoft.Interop.Analyzers.ManualTypeMarshallingAnalyzer>;

namespace DllImportGenerator.UnitTests
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

[{|#0:BlittableType|}]
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

[{|#0:BlittableType|}]
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

[{|#0:BlittableType|}]
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
            var diagnostic = VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithLocation(0).WithArguments("S");
            await VerifyCS.VerifyAnalyzerAsync(source, diagnostic);
        }

        [Fact]
        public async Task BlittablePrimitiveFields_MarkedBlittable_NoDiagnostic()
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
        public async Task BlittableEnumFields_MarkedBlittable_NoDiagnostic()
        {

            string source = @"
using System.Runtime.InteropServices;

enum E { Zero, One, Two }

[BlittableType]
struct S
{
    public E field;
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BlittableStructFields_MarkedBlittable_NoDiagnostic()
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
        public async Task NonBlittableFields_MarkedBlittable_ReportDiagnosticOnFieldTypeDefinition()
        {
            string source = @"
using System.Runtime.InteropServices;

[BlittableType]
struct S
{
    public T field;
}

[{|#0:BlittableType|}]
struct T
{
    public bool field;
}
";
            var diagnostic = VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithLocation(0).WithArguments("T");
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

[{|#0:BlittableType|}]
struct T
{
    public string field;
}
";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithLocation(0).WithArguments("T"));
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

[{|#0:NativeMarshalling(null)|}]
struct S
{
    public string s;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeNonNullRule).WithLocation(0).WithArguments("S"));
        }

        [Fact]
        public async Task NonNamedNativeType_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[{|#0:NativeMarshalling(typeof(int*))|}]
struct S
{
    public string s;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("int*", "S"));
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

struct {|#0:Native|}
{
    private string value;

    public Native(S s)
    {
        value = s.s;
    }

    public S ToManaged() => new S { s = value };
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S"));
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

class {|#0:Native|}
{
    private IntPtr value;

    public Native(S s)
    {
    }

    public S ToManaged() => new S();
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("Native", "S"));
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

    public string {|#0:Value|} { get => null; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("string", "S"));
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

class {|#0:Native|}
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
                VerifyCS.Diagnostic(NativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("Native", "S"));
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

    public ref char {|#0:GetPinnableReference|}() => ref c;
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
                VerifyCS.Diagnostic(GetPinnableReferenceReturnTypeBlittableRule).WithLocation(0));
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

    public int {|#0:Value|} { get => 0; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBePointerSizedRule).WithLocation(0).WithArguments("int", "S"));
        }

        [Fact]
        public async Task TypeWithGetPinnableReferencePointerReturnType_DoesNotReportDiagnostic()
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

    public int* Value { get => null; set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    
        [Fact]
        public async Task TypeWithGetPinnableReferenceByRefReturnType_ReportsDiagnostic()
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
    private S value;

    public Native(S s) : this()
    {
        value = s;
    }

    public ref byte {|#0:Value|} { get => ref value.GetPinnableReference(); }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(RefValuePropertyUnsupportedRule).WithLocation(0).WithArguments("Native"));
        }

        [Fact]
        public async Task NativeTypeWithGetPinnableReferenceByRefReturnType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

unsafe struct Native
{
    private S value;

    public Native(S s) : this()
    {
        value = s;
    }

    public ref byte GetPinnableReference() => ref value.c;

    public ref byte {|#0:Value|} { get => ref GetPinnableReference(); }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(RefValuePropertyUnsupportedRule).WithLocation(0).WithArguments("Native"));
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
struct {|#0:Native|}
{
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task CollectionNativeTypeWithNoMarshallingMethods_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[GenericContiguousCollectionMarshaller]
struct {|#0:Native|}
{
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(CollectionNativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task CollectionNativeTypeWithWrongConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[GenericContiguousCollectionMarshaller]
ref struct {|#0:Native|}
{
    public Native(S s) : this() {}

    public Span<int> ManagedValues { get; set; }
    public Span<byte> NativeValueStorage { get; set; }

    public IntPtr Value { get; }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(CollectionNativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task CollectionNativeTypeWithCorrectConstructor_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[GenericContiguousCollectionMarshaller]
ref struct Native
{
    public Native(S s, int nativeElementSize) : this() {}

    public Span<int> ManagedValues { get; set; }
    public Span<byte> NativeValueStorage { get; set; }

    public IntPtr Value { get; }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task CollectionNativeTypeWithIncorrectStackallocConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[GenericContiguousCollectionMarshaller]
ref struct {|#0:Native|}
{
    public Native(S s, Span<byte> stackSpace) : this() {}

    public const int StackBufferSize = 1;

    public Span<int> ManagedValues { get; set; }
    public Span<byte> NativeValueStorage { get; set; }

    public IntPtr Value { get; }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(CollectionNativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task CollectionNativeTypeWithOnlyStackallocConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[GenericContiguousCollectionMarshaller]
ref struct {|#0:Native|}
{
    public Native(S s, Span<byte> stackSpace, int nativeElementSize) : this() {}

    public const int StackBufferSize = 1;

    public Span<int> ManagedValues { get; set; }
    public Span<byte> NativeValueStorage { get; set; }

    public IntPtr Value { get; }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task CollectionNativeTypeWithMissingManagedValuesProperty_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[GenericContiguousCollectionMarshaller]
ref struct {|#0:Native|}
{
    public Native(S s, int nativeElementSize) : this() {}

    public Span<byte> NativeValueStorage { get; set; }

    public IntPtr Value { get; }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(CollectionNativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task CollectionNativeTypeWithMissingNativeValueStorageProperty_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[GenericContiguousCollectionMarshaller]
ref struct {|#0:Native|}
{
    public Native(S s, int nativeElementSize) : this() {}

    public Span<int> ManagedValues { get; set; }

    public IntPtr Value { get; }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(CollectionNativeTypeMustHaveRequiredShapeRule).WithLocation(0).WithArguments("Native", "S"));
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
struct {|#0:Native|}
{
    public Native(S s, Span<byte> buffer) {}

    public const int StackBufferSize = 0x100;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule).WithLocation(0).WithArguments("Native"));
        }

        [Fact]
        public async Task TypeWithOnlyNativeStackallocConstructorAndGetPinnableReference_ReportsDiagnostics()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[{|#0:NativeMarshalling(typeof(Native))|}]
class S
{
    public byte c;
    public ref byte GetPinnableReference() => ref c;
}

struct {|#1:Native|}
{
    public Native(S s, Span<byte> buffer) {}

    public IntPtr Value => IntPtr.Zero;
    
    public const int StackBufferSize = 0x100;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule).WithLocation(1).WithArguments("Native"),
                VerifyCS.Diagnostic(GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule).WithLocation(0).WithArguments("S", "Native"));
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

    public IntPtr {|#0:Value|} { set {} }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ValuePropertyMustHaveGetterRule).WithLocation(0).WithArguments("Native"));
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

    public IntPtr {|#0:Value|} => IntPtr.Zero;
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ValuePropertyMustHaveSetterRule).WithLocation(0).WithArguments("Native"));
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

struct {|#0:Native|}
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
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task NonBlittableNativeTypeOnMarshalUsingParameter_MultipleCompilations_ReportsDiagnostic_WithLocation()
        {
            string source1 = @"
using System;
using System.Runtime.InteropServices;

public struct S
{
    public string s;
}

public struct Native
{
    private string value;

    public Native(S s) : this()
    {
    }

    public S ToManaged() => new S();
}
";
            Compilation compilation1 = await TestUtils.CreateCompilation(source1);

            string source2 = @"
using System;
using System.Runtime.InteropServices;

static class Test
{
    static void Foo([{|#0:MarshalUsing(typeof(Native))|}] S s)
    {}
}
";
            var test = new Verifiers.CSharpCodeFixVerifier<Microsoft.Interop.Analyzers.ManualTypeMarshallingAnalyzer, EmptyCodeFixProvider>.Test
            {
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S")
                },
                SolutionTransforms =
                {
                    (solution, projectId) => solution.AddMetadataReference(projectId, compilation1.ToMetadataReference())
                },
                TestCode = source2
            };

            await test.RunAsync();
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

struct {|#0:Native|}
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
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S"));
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

struct {|#0:Native|}
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
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S"));
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
        public async Task UninstantiatedGenericNativeTypeOnNonGeneric_ReportsDiagnostic()
        {

            string source = @"
using System.Runtime.InteropServices;

[{|#0:NativeMarshalling(typeof(Native<>))|}]
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
            await VerifyCS.VerifyAnalyzerAsync(source, VerifyCS.Diagnostic(NativeGenericTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("Native<>", "S"));
        }

        [Fact]
        public async Task UninstantiatedGenericNativeTypeOnGenericWithArityMismatch_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[{|#0:NativeMarshalling(typeof(Native<,>))|}]
struct S<T>
{
    public string s;
}

struct Native<T, U>
    where T : new()
{
    public Native(S<T> s)
    {
        Value = 0;
    }

    public S<T> ToManaged() => new S<T>();

    public int Value { get; set; }
}";
            await VerifyCS.VerifyAnalyzerAsync(source, VerifyCS.Diagnostic(NativeGenericTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("Native<,>", "S<T>"));
        }

        [Fact]
        public async Task UninstantiatedGenericNativeTypeOnGenericWithArityMatch_DoesNotReportDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;

[NativeMarshalling(typeof(Native<>))]
struct S<T>
{
    public T t;
}

struct Native<T>
    where T : new()
{
    public Native(S<T> s)
    {
        Value = 0;
    }

    public S<T> ToManaged() => new S<T>();

    public int Value { get; set; }
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

[{|#0:BlittableType|}]
unsafe struct S
{
    private bool* ptr;
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithLocation(0).WithArguments("S"));
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

[{|#0:BlittableType|}]
unsafe struct S
{
    private bool fld;
    private S* ptr;
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithLocation(0).WithArguments("S"));
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

[{|#0:BlittableType|}]
unsafe struct S
{
    private G<string> field;
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithLocation(0).WithArguments("S"));
        }

        [Fact]
        public async Task BlittableGenericTypeTypeParameterReferenceType_ReportsDiagnostic()
        {
            var source = @"
using System.Runtime.InteropServices;

[{|#0:BlittableType|}]
struct G<T> where T : class
{
    T fld;
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithLocation(0).WithArguments("G<T>"));
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
    [{|#0:BlittableType|}]
    struct G
    {
        T fld;
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(BlittableTypeMustBeBlittableRule).WithLocation(0).WithArguments("C<T>.G"));
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

        [Fact]
        public async Task NativeTypeWithStackallocConstructorWithoutBufferSize_ReportsDiagnostic()
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
    public {|#0:Native|}(S s, Span<byte> buffer) {}
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(StackallocConstructorMustHaveStackBufferSizeConstantRule).WithLocation(0).WithArguments("Native"));
        }
    }
}