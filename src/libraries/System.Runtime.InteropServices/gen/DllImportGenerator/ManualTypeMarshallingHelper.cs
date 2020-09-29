#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    static class ManualTypeMarshallingHelper
    {
        public const string ValuePropertyName = "Value";
        public const string StackBufferSizeFieldName = "StackBufferSize";
        public const string ToManagedMethodName = "ToManaged";
        public const string FreeNativeMethodName = "FreeNative";

        public static bool HasToManagedMethod(ITypeSymbol nativeType, ITypeSymbol managedType)
        {
            return nativeType.GetMembers(ToManagedMethodName)
                    .OfType<IMethodSymbol>()
                    .Any(m => m.Parameters.IsEmpty
                        && !m.ReturnsByRef
                        && !m.ReturnsByRefReadonly
                        && SymbolEqualityComparer.Default.Equals(m.ReturnType, managedType)
                        && !m.IsStatic);
        }

        public static bool IsManagedToNativeConstructor(IMethodSymbol ctor, ITypeSymbol managedType)
        {
            return ctor.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(managedType, ctor.Parameters[0].Type);
        }

        public static bool IsStackallocConstructor(
            IMethodSymbol ctor,
            ITypeSymbol managedType,
            ITypeSymbol spanOfByte)
        {
            return ctor.Parameters.Length == 2
                && SymbolEqualityComparer.Default.Equals(managedType, ctor.Parameters[0].Type)
                && SymbolEqualityComparer.Default.Equals(spanOfByte, ctor.Parameters[1].Type);
        }

        public static IMethodSymbol? FindGetPinnableReference(ITypeSymbol type)
        {
            // Lookup a GetPinnableReference method based on the spec for the pattern-based
            // fixed statement. We aren't supporting a GetPinnableReference extension method
            // (which is apparently supported in the compiler).
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-7.3/pattern-based-fixed
            return type.GetMembers("GetPinnableReference")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { Parameters: { Length: 0 } } and
                    ({ ReturnsByRef: true } or { ReturnsByRefReadonly: true }));
        }

        public static IPropertySymbol? FindValueProperty(ITypeSymbol type)
        {
            return type.GetMembers(ValuePropertyName)
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => !p.IsStatic);
        }

        public static bool HasFreeNativeMethod(ITypeSymbol type)
        {
            return type.GetMembers(FreeNativeMethodName)
                .OfType<IMethodSymbol>()
                .Any(m => m is { Parameters: { Length: 0 } } and
                    ({ ReturnType: { SpecialType: SpecialType.System_Void } }));
        }
    }
}