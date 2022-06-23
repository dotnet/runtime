// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.CustomTypeMarshallerAnalyzer;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.Interop.Analyzers.CustomTypeMarshallerAnalyzer,
    Microsoft.Interop.Analyzers.CustomTypeMarshallerFixer>;

namespace LibraryImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class CustomTypeMarshallerFixerTests
    {
        [Fact]
        public async Task NullNativeType_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[{|#0:NativeMarshalling(null)|}]
struct S
{
    public string s;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveCustomTypeMarshallerAttributeRule).WithLocation(0).WithArguments("S"),
                source);
        }

        [Fact]
        public async Task NonNamedNativeType_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[{|#0:NativeMarshalling(typeof(int*))|}]
struct S
{
    public string s;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustHaveCustomTypeMarshallerAttributeRule).WithLocation(0).WithArguments("S"),
                source);
        }

        [Fact]
        public async Task NonBlittableNativeType_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S))]
struct {|#0:Native|}
{
    private string value;

    public Native(S s)
    {
        value = s.s;
    }

    public S ToManaged() => new S { s = value };
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S"),
                source);
        }

        [Fact]
        public async Task ClassNativeType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

[{|CS0592:CustomTypeMarshaller|}(typeof(S))]
class {|#0:Native|}
{
    private IntPtr value;

    public Native(S s)
    {
    }

    public S ToManaged() => new S();
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S"),
                source);
        }

        [Fact]
        public async Task BlittableNativeType_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S))]
struct Native
{
    private IntPtr value;

    public Native(S s)
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task BlittableNativeWithNonBlittableToNativeValue_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct Native
{
    private IntPtr value;

    public Native(S s)
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public string {|#0:ToNativeValue|}() => throw null;
    public void FromNativeValue(string value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("string", "S"),
                source);
        }

        [Fact]
        public async Task NonBlittableNativeTypeWithBlittableToNativeValue_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct Native
{
    private string value;

    public Native(S s)
    {
        value = s.s;
    }

    public S ToManaged() => new S() { s = value };

    public IntPtr ToNativeValue() => throw null;
    public void FromNativeValue(IntPtr value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NonBlittableGetPinnableReferenceReturnType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public string s;

    public ref string {|#0:GetPinnableReference|}() => ref s;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private IntPtr value;

    public Native(S s)
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public IntPtr ToNativeValue() => throw null;
    public void FromNativeValue(IntPtr value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(GetPinnableReferenceReturnTypeBlittableRule).WithLocation(0),
                source);
        }

        [Fact]
        public async Task BlittableGetPinnableReferenceReturnType_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;

    public ref byte GetPinnableReference() => ref c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private IntPtr value;

    public Native(S s) : this()
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public IntPtr ToNativeValue() => throw null;
    public void FromNativeValue(IntPtr value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NonBlittableMarshallerGetPinnableReferenceReturnType_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public char c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private IntPtr value;

    public Native(S s)
    {
        value = IntPtr.Zero;
    }

    public ref char GetPinnableReference() => ref System.Runtime.CompilerServices.Unsafe.NullRef<char>();

    public S ToManaged() => new S();

    public IntPtr ToNativeValue() => throw null;
    public void FromNativeValue(IntPtr value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task BlittableMarshallerGetPinnableReferenceReturnType_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private IntPtr value;

    public Native(S s) : this()
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public ref byte GetPinnableReference() => ref System.Runtime.CompilerServices.Unsafe.NullRef<byte>();

    public IntPtr ToNativeValue() => throw null;
    public void FromNativeValue(IntPtr value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task TypeWithGetPinnableReferenceNonPointerReturnType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;

    public ref byte GetPinnableReference() => ref c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private IntPtr value;

    public Native(S s) : this()
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public int {|#0:ToNativeValue|}() => throw null;
    public void FromNativeValue(int value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBePointerSizedRule).WithLocation(0).WithArguments("int", "S"),
                source);
        }

        [Fact]
        public async Task TypeWithGetPinnableReferencePointerReturnType_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;

    public ref byte GetPinnableReference() => ref c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private IntPtr value;

    public Native(S s) : this()
    {
        value = IntPtr.Zero;
    }

    public S ToManaged() => new S();

    public int* ToNativeValue() => throw null;
    public void FromNativeValue(int* value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task TypeWithGetPinnableReferenceByRefValuePropertyType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;

    public ref byte GetPinnableReference() => ref c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private S value;

    public Native(S s) : this()
    {
        value = s;
    }
    public ref byte {|#0:ToNativeValue|}() => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(RefNativeValueUnsupportedRule).WithLocation(0).WithArguments("Native"),
                source);
        }

        [Fact]
        public async Task NativeTypeWithGetPinnableReferenceByRefValuePropertyType_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private S value;

    public Native(S s) : this()
    {
        value = s;
    }

    public ref byte GetPinnableReference() => ref value.c;

    public ref byte {|#0:ToNativeValue|}() => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(RefNativeValueUnsupportedRule).WithLocation(0).WithArguments("Native"),
                source);
        }

        [Fact]
        public async Task NativeTypeWithGetPinnableReferenceNoValueProperty_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In)]
unsafe struct Native
{
    private byte value;

    public Native(S s) : this()
    {
        value = s.c;
    }

    public ref byte {|#0:GetPinnableReference|}() => ref System.Runtime.CompilerServices.Unsafe.NullRef<byte>();
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(MarshallerGetPinnableReferenceRequiresTwoStageMarshallingRule).WithLocation(0).WithArguments("Native"),
                source);
        }

        [Fact]
        public async Task NativeTypeWithGetPinnableReferenceWithNonPointerValueProperty_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    private byte value;

    public Native(S s) : this()
    {
        value = s.c;
    }

    public ref byte GetPinnableReference() => ref System.Runtime.CompilerServices.Unsafe.NullRef<byte>();

    public int ToNativeValue() => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NativeTypeWithNoMarshallingMethods_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.None)]
struct {|#0:Native|}
{
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(CustomMarshallerTypeMustSupportDirectionRule).WithLocation(0).WithArguments("Native", "S"),
                source);
        }

        [Fact]
        public async Task CollectionNativeTypeWithNoMarshallingMethods_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.None)]
struct {|#0:Native|}
{
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(CustomMarshallerTypeMustSupportDirectionRule).WithLocation(0).WithArguments("Native", "S"),
                source);
        }

        [Fact]
        public async Task CollectionNativeTypeWithWrongConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct {|#0:Native|}
{
    public Native(S s) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct Native
{
    public Native(S s) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;

    public Native(S managed, int nativeElementSize)
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(LinearCollectionInRequiresTwoParameterConstructorRule).WithLocation(0).WithArguments("Native", "S"),
                fixedSource);
        }

        [Fact]
        public async Task CollectionNativeTypeWithCorrectConstructor_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct Native
{
    public Native(S s, int nativeElementSize) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task CollectionNativeTypeWithIncorrectStackallocConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 1)]
ref struct {|#0:Native|}
{
    public Native(S s, Span<byte> stackSpace) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 1)]
ref struct Native
{
    public Native(S s, Span<byte> stackSpace) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;

    public Native(S managed, int nativeElementSize)
    {
        throw new NotImplementedException();
    }

    public Native(S managed, Span<byte> buffer, int nativeElementSize)
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                fixedSource,
                    VerifyCS.Diagnostic(LinearCollectionInRequiresTwoParameterConstructorRule).WithLocation(0).WithArguments("Native", "S"),
                    VerifyCS.Diagnostic(LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task CollectionNativeTypeWithOnlyStackallocConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 1)]
ref struct {|#0:Native|}
{
    public Native(S s, Span<byte> stackSpace, int nativeElementSize) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 1)]
ref struct Native
{
    public Native(S s, Span<byte> stackSpace, int nativeElementSize) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;

    public Native(S managed, int nativeElementSize)
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                fixedSource,
                VerifyCS.Diagnostic(CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule).WithLocation(0).WithArguments("Native", "S"),
                VerifyCS.Diagnostic(LinearCollectionInRequiresTwoParameterConstructorRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task CollectionNativeTypeWithMissingManagedValuesSource_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct {|#0:Native|}
{
    public Native(S s, int nativeElementSize) : this() {}

    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct Native
{
    public Native(S s, int nativeElementSize) : this() {}

    public System.Span<byte> GetNativeValuesDestination() => throw null;
    public System.IntPtr ToNativeValue() => throw null;

    public ReadOnlySpan<object> GetManagedValuesSource()
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(LinearCollectionInRequiresCollectionMethodsRule).WithLocation(0).WithArguments("Native", "S"),
                fixedSource);
        }

        [Fact]
        public async Task CollectionNativeTypeWithMissingNativeValuesDestination_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct {|#0:Native|}
{
    public Native(S s, int nativeElementSize) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.IntPtr ToNativeValue() => throw null;
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct Native
{
    public Native(S s, int nativeElementSize) : this() {}

    public System.ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public System.IntPtr ToNativeValue() => throw null;

    public Span<byte> GetNativeValuesDestination()
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(LinearCollectionInRequiresCollectionMethodsRule).WithLocation(0).WithArguments("Native", "S"),
                fixedSource);
        }

        [Fact]
        public async Task CollectionNativeTypeWithCorrectRefShape_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct Native
{
    public Native(int nativeElementSize) : this() {}
    public Native(S s, int nativeElementSize) : this() {}

    public ReadOnlySpan<int> GetManagedValuesSource() => throw null;
    public Span<byte> GetNativeValuesDestination() => throw null;
    public ReadOnlySpan<byte> GetNativeValuesSource(int length) => throw null;
    public Span<int> GetManagedValuesDestination(int length) => throw null;
    public IntPtr ToNativeValue() => throw null;
    public void FromNativeValue(IntPtr value) => throw null;
    public S ToManaged() => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task CollectionNativeTypeWithMismatched_Element_Type_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
ref struct Native
{
    public Native(int nativeElementSize) : this() {}
    public Native(S s, int nativeElementSize) : this() {}

    public ReadOnlySpan<int> {|#0:GetManagedValuesSource|}() => throw null;
    public Span<byte> GetNativeValuesDestination() => throw null;
    public ReadOnlySpan<byte> GetNativeValuesSource(int length) => throw null;
    public Span<long> GetManagedValuesDestination(int length) => throw null;
    public IntPtr ToNativeValue() => throw null;
    public void FromNativeValue(IntPtr value) => throw null;
    public S ToManaged() => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                source,
                VerifyCS.Diagnostic(LinearCollectionElementTypesMustMatchRule)
                    .WithLocation(0));
        }

        [Fact]
        public async Task NativeTypeWithOnlyConstructor_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In)]
struct Native
{
    public Native(S s) {}
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NativeTypeWithOnlyToManagedMethod_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.Out)]
struct Native
{
    public S ToManaged() => new S();
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NativeTypeWithIncorrectStackallocConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer, BufferSize = 0x100)]
struct {|#0:Native|}
{
    public Native(S managed) {}
    public Native(S s, int i) {}
    public Native(S s, Span<object> buffer) {}
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer, BufferSize = 0x100)]
struct Native
{
    public Native(S managed) {}
    public Native(S s, int i) {}
    public Native(S s, Span<object> buffer) {}

    public Native(S managed, Span<byte> buffer)
    {
        throw new NotImplementedException();
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                fixedSource,
                VerifyCS.Diagnostic(ValueInCallerAllocatedBufferRequiresSpanConstructorRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task NativeTypeWithOnlyStackallocConstructor_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer, BufferSize = 0x100)]
struct {|#0:Native|}
{
    public Native(S s, Span<byte> buffer) {}
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer, BufferSize = 0x100)]
struct Native
{
    public Native(S s, Span<byte> buffer) {}

    public Native(S managed)
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                fixedSource,
                VerifyCS.Diagnostic(CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule).WithLocation(0).WithArguments("Native", "S"),
                VerifyCS.Diagnostic(ValueInRequiresOneParameterConstructorRule).WithLocation(0).WithArguments("Native", "S"));
        }

        [Fact]
        public async Task TypeWithOnlyGetPinnableReference_AndInSupport_ReportsDiagnostics()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class {|#0:S|}
{
    public byte c;
    public ref byte GetPinnableReference() => ref c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.Out)]
struct {|#1:Native|}
{
    public S ToManaged() => default;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule).WithLocation(0).WithArguments("S", "Native"),
                source);
        }

        [Fact]
        public async Task NativeTypeWithConstructorAndFromNativeValueMethod_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct {|#0:Native|}
{
    public Native(S s) {}

    public void FromNativeValue(IntPtr value) => throw null;

    public S ToManaged() => new S();
}";

            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct Native
{
    public Native(S s) {}

    public void FromNativeValue(IntPtr value) => throw null;

    public S ToManaged() => new S();

    public IntPtr ToNativeValue()
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(InTwoStageMarshallingRequiresToNativeValueRule).WithLocation(0).WithArguments("Native"),
                fixedSource);
        }

        [Fact]
        public async Task NativeTypeWithToManagedAndToNativeValueMethod_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct {|#0:Native|}
{
    public Native(S managed) {}

    public S ToManaged() => new S();

    public IntPtr ToNativeValue() => IntPtr.Zero;
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct Native
{
    public Native(S managed) {}

    public S ToManaged() => new S();

    public IntPtr ToNativeValue() => IntPtr.Zero;

    public void FromNativeValue(IntPtr value)
    {
        throw new NotImplementedException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(OutTwoStageMarshallingRequiresFromNativeValueRule).WithLocation(0).WithArguments("Native"),
                fixedSource);
        }

        [Fact]
        public async Task BlittableNativeTypeOnMarshalUsingParameter_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S))]
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
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NonBlittableNativeTypeOnMarshalUsingParameter_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S))]
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
            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S"),
                source);
        }

        [Fact]
        public async Task NonBlittableNativeTypeOnMarshalUsingReturn_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S))]
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
            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeTypeMustBeBlittableRule).WithLocation(0).WithArguments("Native", "S"),
                source);
        }

        [Fact]
        public async Task GenericNativeTypeWithGenericMemberInstantiatedWithBlittable_DoesNotReportDiagnostic()
        {

            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native<int>))]
struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct Native<T>
    where T : unmanaged
{
    public Native(S s)
    {
    }

    public S ToManaged() => new S();

    public T ToNativeValue() => throw null;
    public void FromNativeValue(T value) => throw null;
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task UninstantiatedGenericNativeTypeOnNonGeneric_ReportsDiagnostic()
        {

            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[{|#0:NativeMarshalling(typeof(Native<>))|}]
struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct Native<T>
    where T : unmanaged
{
    public Native(S s)
    {
    }

    public S ToManaged() => new S();

    public T ToNativeValue() => throw null;
    public void FromNativeValue(T value) => throw null;
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeGenericTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("Native<>", "S"),
                source);
        }

        [Fact]
        public async Task MarshalUsingUninstantiatedGenericNativeType_ReportsDiagnostic()
        {

            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S
{
    public string s;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct Native<T>
    where T : unmanaged
{
    public Native(S s)
    {
    }

    public S ToManaged() => new S();

    public T ToNativeValue() => throw null;
    public void FromNativeValue(T value) => throw null;
}

static class Test
{
    static void Foo([{|#0:MarshalUsing(typeof(Native<>))|}] S s)
    {}
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(NativeGenericTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("Native<>", "S"),
                source);
        }

        [Fact]
        public async Task UninstantiatedGenericNativeTypeOnGenericWithArityMismatch_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[{|#0:NativeMarshalling(typeof(Native<,>))|}]
struct S<T>
{
    public string s;
}

[CustomTypeMarshaller(typeof(S<>), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct {|#1:Native|}<T, U>
    where T : new()
{
    public Native(S<T> s)
    {
    }

    public S<T> ToManaged() => new S<T>();

    public T ToNativeValue() => throw null;
    public void FromNativeValue(T value) => throw null;
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                source,
                VerifyCS.Diagnostic(NativeTypeMustHaveCustomTypeMarshallerAttributeRule).WithLocation(0).WithArguments("S<T>"),
                VerifyCS.Diagnostic(NativeGenericTypeMustBeClosedOrMatchArityRule).WithLocation(1).WithArguments("Native<T, U>", "S<>"));
        }

        [Fact]
        public async Task UninstantiatedGenericNativeTypeOnGenericWithArityMatch_DoesNotReportDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native<>))]
struct S<T>
{
    public T t;
}

[CustomTypeMarshaller(typeof(S<>), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
struct Native<T>
    where T : new()
{
    public Native(S<T> s)
    {
    }

    public S<T> ToManaged() => new S<T>();

    public T ToNativeValue() => throw null;
    public void FromNativeValue(T value) => throw null;
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NativeTypeWithStackallocConstructorWithoutBufferSize_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer)]
struct Native
{
    public Native(S s) {}
    public {|#0:Native|}(S s, Span<byte> buffer) {}
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                VerifyCS.Diagnostic(CallerAllocConstructorMustHaveBufferSizeRule).WithLocation(0).WithArguments("Native"),
                source);
        }

        [Fact]
        public async Task CustomTypeMarshallerForTypeWithPlaceholder_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[CustomTypeMarshaller(typeof(CustomTypeMarshallerAttribute.GenericPlaceholder), Direction = CustomTypeMarshallerDirection.In)]
struct Native<T>
{
    public Native(T a) {}
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task CustomTypeMarshallerForArrayTypeWithPlaceholder_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[CustomTypeMarshaller(typeof(CustomTypeMarshallerAttribute.GenericPlaceholder[]), Direction = CustomTypeMarshallerDirection.In)]
struct Native<T>
{
    public Native(T[] a) {}
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task CustomTypeMarshallerForPointerTypeWithPlaceholder_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[CustomTypeMarshaller(typeof(CustomTypeMarshallerAttribute.GenericPlaceholder*), Direction = CustomTypeMarshallerDirection.In)]
unsafe struct Native<T> where T : unmanaged
{
    public Native(T* a) {}
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task CustomTypeMarshallerForArrayOfPointerTypeWithPlaceholder_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[CustomTypeMarshaller(typeof(CustomTypeMarshallerAttribute.GenericPlaceholder*[]), Direction = CustomTypeMarshallerDirection.In)]
unsafe struct Native<T> where T : unmanaged
{
    public Native(T*[] a) {}
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task CustomTypeMarshallerWithFreeNativeMethod_NoUnmanagedResourcesFeatures_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S { }

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In)]
unsafe struct {|#0:Native|}
{
    public Native(S s){}

    public void FreeNative() { }
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S { }

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.Value, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
unsafe struct Native
{
    public Native(S s){}

    public void FreeNative() { }
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                fixedSource,
                VerifyCS.Diagnostic(FreeNativeMethodProvidedShouldSpecifyUnmanagedResourcesFeatureRule)
                    .WithArguments("Native")
                    .WithLocation(0));
        }
        [Fact]
        public async Task CustomTypeMarshallerWithCallerAllocatedBufferConstructor_NoFeature_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S { }

[CustomTypeMarshaller(typeof(S), Direction = CustomTypeMarshallerDirection.In, BufferSize = 0x100)]
unsafe struct {|#0:Native|}
{
    public Native(S s){}

    public Native(S s, Span<byte> buffer) { }
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S { }

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.Value, Direction = CustomTypeMarshallerDirection.In, BufferSize = 256, Features = CustomTypeMarshallerFeatures.CallerAllocatedBuffer)]
unsafe struct Native
{
    public Native(S s){}

    public Native(S s, Span<byte> buffer) { }
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                fixedSource,
                VerifyCS.Diagnostic(CallerAllocatedBufferConstructorProvidedShouldSpecifyFeatureRule)
                    .WithArguments("Native")
                    .WithLocation(0));
        }

        [Fact]
        public async Task Add_Feature_Declaration_Preserves_Attribute_Argument_Location()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S { }

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.None, Direction = CustomTypeMarshallerDirection.In)]
unsafe struct {|#0:Native|}
{
    public Native(S s){}

    public void FreeNative() { }
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S { }

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.Value, Features = CustomTypeMarshallerFeatures.UnmanagedResources, Direction = CustomTypeMarshallerDirection.In)]
unsafe struct Native
{
    public Native(S s){}

    public void FreeNative() { }
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                fixedSource,
                VerifyCS.Diagnostic(FreeNativeMethodProvidedShouldSpecifyUnmanagedResourcesFeatureRule)
                    .WithArguments("Native")
                    .WithLocation(0));
        }

        [Fact]
        public async Task CustomTypeMarshallerWithTwoStageMarshallingMethod_NoFeature_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S { }

[CustomTypeMarshaller(typeof(S))]
unsafe struct {|#0:Native|}
{
    public Native(S s){}

    public int ToNativeValue() => throw null;

    public S ToManaged() => throw null;
}

[CustomTypeMarshaller(typeof(S))]
unsafe struct {|#1:Native2|}
{
    public Native2(S s){}

    public void FromNativeValue(int value) { }

    public S ToManaged() => throw null;
}";
            string fixedSource = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

struct S { }

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.Value, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    public Native(S s){}

    public int ToNativeValue() => throw null;

    public S ToManaged() => throw null;

    public void FromNativeValue(int value)
    {
        throw new NotImplementedException();
    }
}

[CustomTypeMarshaller(typeof(S), CustomTypeMarshallerKind.Value, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native2
{
    public Native2(S s){}

    public void FromNativeValue(int value) { }

    public S ToManaged() => throw null;

    public int ToNativeValue()
    {
        throw new NotImplementedException();
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source,
                new[]
                {
                    VerifyCS.Diagnostic(ToNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureRule)
                        .WithArguments("Native")
                        .WithLocation(0),
                    VerifyCS.Diagnostic(FromNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureRule)
                        .WithArguments("Native2")
                        .WithLocation(1)
                },
                fixedSource,
                // One code-fix run is expected for each of the two diagnostics.
                // Each fix of the "specifiy the feature" diagnostic will result in code that reports another diagnostic
                // for the missing other member.
                // The second two code-fix runs are the fixes for those diagnostics.
                numIncrementalIterations: 4,
                // The first run adds the feature flag and the second adds the missing members for the feature.
                numFixAllIterations: 2);
        }

        [Fact]
        public async Task Mismatched_NativeValue_Type_ReportsDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    public Native(S s) { }

    public S ToManaged() => new S();

    public int {|#0:ToNativeValue|}() => throw null;
    public void FromNativeValue(long value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                source,
                VerifyCS.Diagnostic(TwoStageMarshallingNativeTypesMustMatchRule)
                    .WithLocation(0));
        }

        [Fact]
        public async Task Same_NativeValue_Type_DifferentName_DoesNotReportDiagnostic()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Value2 = N.Value;

namespace N
{
    struct Value
    {
        private int i;
    }
}

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

[CustomTypeMarshaller(typeof(S), Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
unsafe struct Native
{
    public Native(S s) { }

    public S ToManaged() => new S();

    public N.Value ToNativeValue() => throw null;
    public void FromNativeValue(Value2 value) => throw null;
}";

            await VerifyCS.VerifyCodeFixAsync(source,
                source);
        }
    }
}
