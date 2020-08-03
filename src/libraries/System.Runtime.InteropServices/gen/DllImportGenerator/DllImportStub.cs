using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal class DllImportStub
    {
        private TypePositionInfo returnTypeInfo;
        private IEnumerable<TypePositionInfo> paramsTypeInfo;

        private DllImportStub()
        {
        }

        public string StubTypeNamespace { get; private set; }

        public IEnumerable<string> StubContainingTypesDecl { get; private set; }

        public string StubReturnType { get => this.returnTypeInfo.ManagedTypeDecl; }

        public IEnumerable<(string Type, string Name)> StubParameters
        {
            get
            {
                foreach (var typeinfo in paramsTypeInfo)
                {
                    //if (typeinfo.ManagedIndex != TypePositionInfo.UnsetIndex)
                    {
                        yield return (typeinfo.ManagedTypeDecl, typeinfo.InstanceIdentifier);
                    }
                }
            }
        }

        public IEnumerable<string> StubCode { get; private set; }

        public string DllImportReturnType { get => this.returnTypeInfo.UnmanagedTypeDecl; }

        public string DllImportMethodName { get; private set; }

        public IEnumerable<(string Type, string Name)> DllImportParameters
        {
            get
            {
                foreach (var typeinfo in paramsTypeInfo)
                {
                    //if (typeinfo.UnmanagedIndex != TypePositionInfo.UnsetIndex)
                    {
                        yield return (typeinfo.UnmanagedTypeDecl, typeinfo.InstanceIdentifier);
                    }
                }
            }
        }

        public IEnumerable<Diagnostic> Diagnostics { get; private set; }

        /// <summary>
        /// Flags used to indicate members on GeneratedDllImport attribute.
        /// </summary>
        [Flags]
        public enum DllImportMember
        {
            None = 0,
            BestFitMapping = 1 << 0,
            CallingConvention = 1 << 1,
            CharSet = 1 << 2,
            EntryPoint = 1 << 3,
            ExactSpelling = 1 << 4,
            PreserveSig = 1 << 5,
            SetLastError = 1 << 6,
            ThrowOnUnmappableChar = 1 << 7,
        }

        /// <summary>
        /// GeneratedDllImportAttribute data
        /// </summary>
        /// <remarks>
        /// The names of these members map directly to those on the
        /// DllImportAttribute and should not be changed.
        /// </remarks>
        public class GeneratedDllImportData
        {
            public string ModuleName { get; set; }

            /// <summary>
            /// Value set by the user on the original declaration.
            /// </summary>
            public DllImportMember IsUserDefined = DllImportMember.None;

            // Default values for the below fields are based on the
            // documented semanatics of DllImportAttribute:
            //   - https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute
            public bool BestFitMapping { get; set; } = true;
            public CallingConvention CallingConvention { get; set; } = CallingConvention.Winapi;
            public CharSet CharSet { get; set; } = CharSet.Ansi;
            public string EntryPoint { get; set; } = null;
            public bool ExactSpelling { get; set; } = false; // VB has different and unusual default behavior here.
            public bool PreserveSig { get; set; } = true;
            public bool SetLastError { get; set; } = false;
            public bool ThrowOnUnmappableChar { get; set; } = false;
        }

        public static DllImportStub Create(
            IMethodSymbol method,
            GeneratedDllImportData dllImportData,
            CancellationToken token = default)
        {
            // Cancel early if requested
            token.ThrowIfCancellationRequested();

            // Determine the namespace
            string stubTypeNamespace = null;
            if (!(method.ContainingNamespace is null)
                && !method.ContainingNamespace.IsGlobalNamespace)
            {
                stubTypeNamespace = method.ContainingNamespace.ToString();
            }

            // Determine type
            var stubContainingTypes = new List<string>();
            INamedTypeSymbol currType = method.ContainingType;
            while (!(currType is null))
            {
                var visibility = currType.DeclaredAccessibility switch
                {
                    Accessibility.Public => "public",
                    Accessibility.Private => "private",
                    Accessibility.Protected => "protected",
                    Accessibility.Internal => "internal",
                    _ => throw new NotSupportedException(), // [TODO] Proper error message
                };

                var typeKeyword = currType.TypeKind switch
                {
                    TypeKind.Class => "class",
                    TypeKind.Struct => "struct",
                    _ => throw new NotSupportedException(), // [TODO] Proper error message
                };

                stubContainingTypes.Add($"{visibility} partial {typeKeyword} {currType.Name}");
                currType = currType.ContainingType;
            }

            // Flip the order to that of how to declare the types
            stubContainingTypes.Reverse();

            // Determine parameter types
            var paramsTypeInfo = new List<TypePositionInfo>();
            foreach (var paramSymbol in method.Parameters)
            {
                paramsTypeInfo.Add(TypePositionInfo.CreateForParameter(paramSymbol));
            }

            var retTypeInfo = TypePositionInfo.CreateForType(method.ReturnType, method.GetReturnTypeAttributes());

            string dllImportName = method.Name + "__PInvoke__";

#if !GENERATE_FORWARDER
            var dispatchCall = new StringBuilder($"throw new System.{nameof(NotSupportedException)}();");
#else
            // Forward call to generated P/Invoke
            var returnMaybe = method.ReturnsVoid ? string.Empty : "return ";

            var dispatchCall = new StringBuilder($"{returnMaybe}{dllImportName}");
            if (!paramsTypeInfo.Any())
            {
                dispatchCall.Append("();");
            }
            else
            {
                char delim = '(';
                foreach (var param in paramsTypeInfo)
                {
                    dispatchCall.Append($"{delim}{param.RefKindDecl}{param.InstanceIdentifier}");
                    delim = ',';
                }
                dispatchCall.Append(");");
            }
#endif

            return new DllImportStub()
            {
                returnTypeInfo = retTypeInfo,
                paramsTypeInfo = paramsTypeInfo,
                StubTypeNamespace = stubTypeNamespace,
                StubContainingTypesDecl = stubContainingTypes,
                StubCode = new[] { dispatchCall.ToString() },
                DllImportMethodName = dllImportName,
                Diagnostics = Enumerable.Empty<Diagnostic>(),
            };
        }
    }
}
