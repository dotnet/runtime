// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGenerators
{
    /// <summary>
    /// Emits member and constructor accessors for a source generator: <c>[UnsafeAccessor]</c> externs on frameworks that
    /// support them, and a reflection-based fallback (cached delegates / <see cref="System.Reflection.FieldInfo"/> /
    /// <see cref="System.Reflection.ConstructorInfo"/>) downlevel. Callers describe the members to emit with the neutral,
    /// primitive-only spec types in this file; the emitter owns accessor naming and the generic wrapper-class machinery.
    /// </summary>
    /// <remarks>
    /// The reflection fallback emitted for members references an <c>InstanceMemberBindingFlags</c> constant (of type
    /// <see cref="System.Reflection.BindingFlags"/>) that the consuming generator must emit into the same scope by
    /// writing <see cref="InstanceMemberBindingFlagsDeclaration"/>, and a <c>ValueTypeSetter&lt;TDeclaringType, TValue&gt;</c>
    /// delegate (written via <see cref="ValueTypeSetterDelegateDeclaration"/>) when <see cref="EmitMemberAccessors"/>
    /// returns <see langword="true"/> for a value-type setter.
    /// </remarks>
    internal static class UnsafeAccessorEmitter
    {
        private const string UnsafeAccessorAttributeTypeRef = "global::System.Runtime.CompilerServices.UnsafeAccessorAttribute";
        private const string UnsafeAccessorKindTypeRef = "global::System.Runtime.CompilerServices.UnsafeAccessorKind";
        private const string EmptyTypeArray = "global::System.Array.Empty<global::System.Type>()";

        /// <summary>
        /// The declaration of the <c>InstanceMemberBindingFlags</c> constant that the reflection fallback (emitted by
        /// <see cref="EmitMemberAccessors"/> and <see cref="EmitConstructorAccessor"/>) references. A consuming generator
        /// must write this once into the scope containing the emitted accessors.
        /// </summary>
        public const string InstanceMemberBindingFlagsDeclaration = """
            private const global::System.Reflection.BindingFlags InstanceMemberBindingFlags =
                global::System.Reflection.BindingFlags.Instance |
                global::System.Reflection.BindingFlags.Public |
                global::System.Reflection.BindingFlags.NonPublic;
            """;

        /// <summary>
        /// The declaration of the <c>ValueTypeSetter&lt;TDeclaringType, TValue&gt;</c> delegate that the value-type
        /// setter reflection fallback references. A consuming generator must write this once when
        /// <see cref="EmitMemberAccessors"/> returns <see langword="true"/>.
        /// </summary>
        public const string ValueTypeSetterDelegateDeclaration = "private delegate void ValueTypeSetter<TDeclaringType, TValue>(ref TDeclaringType obj, TValue value);";

        internal enum AccessorMemberKind
        {
            Property,
            Field,
        }

        /// <summary>
        /// Describes a single member (property or field) an accessor may be emitted for. All type names are neutral
        /// fully-qualified strings so the spec carries no Roslyn symbols and remains incremental-pipeline safe.
        /// </summary>
        internal sealed record UnsafeAccessorMemberSpec
        {
            public required AccessorMemberKind Kind { get; init; }
            public required string MemberName { get; init; }
            public required bool NeedsGetter { get; init; }
            public required bool NeedsSetter { get; init; }
            public required bool CanUseUnsafeAccessors { get; init; }
            public required string DeclaringTypeFQN { get; init; }
            public required string MemberTypeFQN { get; init; }

            /// <summary>Type-parameter names of the declaring type when it is generic (.NET 9+ wrapper class), otherwise <see langword="null"/>.</summary>
            public ImmutableEquatableArray<string>? DeclaringTypeParameterNames { get; init; }
            public string? OpenDeclaringTypeFQN { get; init; }
            public string? OpenMemberTypeFQN { get; init; }
            public string? DeclaringTypeParameterConstraintClauses { get; init; }

            public bool IsProperty => Kind is AccessorMemberKind.Property;
        }

        /// <summary>Describes a constructor accessor to emit.</summary>
        internal sealed record UnsafeAccessorConstructorSpec
        {
            public required string TypeFriendlyName { get; init; }
            public required string TypeFQN { get; init; }
            public required bool CanUseUnsafeAccessor { get; init; }
            public required ImmutableEquatableArray<UnsafeAccessorParameterSpec> Parameters { get; init; }
        }

        /// <summary>A single constructor parameter of a <see cref="UnsafeAccessorConstructorSpec"/>.</summary>
        internal sealed record UnsafeAccessorParameterSpec
        {
            public required string TypeFQN { get; init; }
            public required int Index { get; init; }
        }

        /// <summary>
        /// Gets the accessor name for a property or field. For UnsafeAccessor this is the extern method name;
        /// for reflection fallback this is the strongly typed wrapper method name.
        /// Use kind "get"/"set" for property getters/setters, or "field" for field UnsafeAccessor externs.
        /// The property index suffix is only appended when needed to disambiguate shadowed members.
        /// </summary>
        public static string GetAccessorName(string typeFriendlyName, string accessorKind, string memberName, int propertyIndex, bool needsDisambiguation)
            => needsDisambiguation
                ? $"__{accessorKind}_{typeFriendlyName}_{memberName}_{propertyIndex}"
                : $"__{accessorKind}_{typeFriendlyName}_{memberName}";

        /// <summary>
        /// For properties on generic types using wrapper-class UnsafeAccessors (.NET 9+), returns the
        /// fully qualified accessor reference including the generic wrapper class prefix, e.g.
        /// <c>__GenericAccessors_MyType&lt;int&gt;.__get_MyType_Name</c>.
        /// For non-generic types, returns the plain accessor name.
        /// </summary>
        public static string GetQualifiedAccessorName(
            ImmutableEquatableArray<string>? declaringTypeParameterNames,
            string declaringTypeFQN,
            string typeFriendlyName,
            string accessorKind,
            string memberName,
            int propertyIndex,
            bool needsDisambiguation)
        {
            string accessorName = GetAccessorName(typeFriendlyName, accessorKind, memberName, propertyIndex, needsDisambiguation);
            if (declaringTypeParameterNames is null)
            {
                return accessorName;
            }

            int openAngle = declaringTypeFQN.IndexOf('<');
            string typeArgsList = declaringTypeFQN.Substring(openAngle);
            return $"__GenericAccessors_{typeFriendlyName}{typeArgsList}.{accessorName}";
        }

        public static string GetReflectionCacheName(string typeFriendlyName, string accessorKind, string memberName, int propertyIndex, bool needsDisambiguation)
            => needsDisambiguation
                ? $"s_{accessorKind}_{typeFriendlyName}_{memberName}_{propertyIndex}"
                : $"s_{accessorKind}_{typeFriendlyName}_{memberName}";

        /// <summary>
        /// Gets the unified constructor accessor name. The wrapper has the same
        /// signature for both UnsafeAccessor and reflection fallback:
        /// <c>static TypeName __ctor_TypeName(params)</c>
        /// </summary>
        public static string GetConstructorAccessorName(string typeFriendlyName)
            => $"__ctor_{typeFriendlyName}";

        public static string GetConstructorReflectionCacheName(string typeFriendlyName)
            => $"s_ctor_{typeFriendlyName}";

        /// <summary>
        /// Returns the set of member names that appear more than once in the property list.
        /// This occurs when derived types shadow base members via the <c>new</c> keyword.
        /// </summary>
        public static HashSet<string> GetDuplicateMemberNames(IEnumerable<string> memberNames)
        {
            HashSet<string> seen = new();
            HashSet<string> duplicates = new();
            foreach (string memberName in memberNames)
            {
                if (!seen.Add(memberName))
                {
                    duplicates.Add(memberName);
                }
            }

            return duplicates;
        }

        /// <summary>
        /// Emits the member accessors for the given ordered member list. Members not requiring a getter or setter
        /// accessor are skipped. Returns <see langword="true"/> if a value-type setter reflection fallback was emitted,
        /// requiring the consuming generator to emit the <c>ValueTypeSetter</c> delegate type.
        /// </summary>
        public static bool EmitMemberAccessors(
            SourceWriter writer,
            string typeFriendlyName,
            bool declaringTypeIsValueType,
            IReadOnlyList<UnsafeAccessorMemberSpec> members)
        {
            HashSet<string> duplicateMemberNames = GetDuplicateMemberNames(members.Select(static m => m.MemberName));
            bool needsAccessors = false;
            bool needsValueTypeSetterDelegate = false;
            Dictionary<string, List<(UnsafeAccessorMemberSpec Member, int Index, bool Disambiguate)>>? genericAccessorEntries = null;

            for (int i = 0; i < members.Count; i++)
            {
                UnsafeAccessorMemberSpec member = members[i];
                bool needsGetterAccessor = member.NeedsGetter;
                bool needsSetterAccessor = member.NeedsSetter;

                if (!needsGetterAccessor && !needsSetterAccessor)
                {
                    continue;
                }

                if (!needsAccessors)
                {
                    writer.WriteLine();
                    needsAccessors = true;
                }

                string declaringTypeFQN = member.DeclaringTypeFQN;
                string propertyTypeFQN = member.MemberTypeFQN;
                bool disambiguate = duplicateMemberNames.Contains(member.MemberName);

                if (member.CanUseUnsafeAccessors)
                {
                    if (member.DeclaringTypeParameterNames is not null)
                    {
                        // Generic types need a wrapper class for UnsafeAccessor (.NET 9+).
                        // Collect the accessor and emit the wrapper class after the loop.
                        string key = member.DeclaringTypeFQN;
                        genericAccessorEntries ??= new();
                        if (!genericAccessorEntries.TryGetValue(key, out List<(UnsafeAccessorMemberSpec Member, int Index, bool Disambiguate)>? entries))
                        {
                            entries = new();
                            genericAccessorEntries[key] = entries;
                        }

                        entries.Add((member, i, disambiguate));
                    }
                    else
                    {
                        string refPrefix = declaringTypeIsValueType ? "ref " : "";

                        if (member.IsProperty)
                        {
                            if (needsGetterAccessor)
                            {
                                string accessorName = GetAccessorName(typeFriendlyName, "get", member.MemberName, i, disambiguate);
                                writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Method, Name = "get_{member.MemberName}")]""");
                                writer.WriteLine($"private static extern {propertyTypeFQN} {accessorName}({refPrefix}{declaringTypeFQN} obj);");
                            }

                            if (needsSetterAccessor)
                            {
                                string accessorName = GetAccessorName(typeFriendlyName, "set", member.MemberName, i, disambiguate);
                                writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Method, Name = "set_{member.MemberName}")]""");
                                writer.WriteLine($"private static extern void {accessorName}({refPrefix}{declaringTypeFQN} obj, {propertyTypeFQN} value);");
                            }
                        }
                        else
                        {
                            // Field: single UnsafeAccessor that returns ref T, used for both get and set.
                            string fieldAccessorName = GetAccessorName(typeFriendlyName, "field", member.MemberName, i, disambiguate);
                            writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Field, Name = "{member.MemberName}")]""");
                            writer.WriteLine($"private static extern ref {propertyTypeFQN} {fieldAccessorName}({refPrefix}{declaringTypeFQN} obj);");
                        }
                    }
                }
                else if (member.IsProperty)
                {
                    // Reflection fallback for properties: use Delegate.CreateDelegate on the MethodInfo for efficient invocation.
                    // Wrapper methods are strongly typed to match UnsafeAccessor signatures.
                    string propertyExpr = $"typeof({declaringTypeFQN}).GetProperty({FormatStringLiteral(member.MemberName)}, InstanceMemberBindingFlags, null, typeof({propertyTypeFQN}), {EmptyTypeArray}, null)!";

                    if (needsGetterAccessor)
                    {
                        string cacheName = GetReflectionCacheName(typeFriendlyName, "get", member.MemberName, i, disambiguate);
                        string wrapperName = GetAccessorName(typeFriendlyName, "get", member.MemberName, i, disambiguate);

                        if (declaringTypeIsValueType)
                        {
                            // For value types, Delegate.CreateDelegate doesn't work with struct instance getters
                            // on .NET Framework (the this parameter is passed by-ref internally).
                            // Cache the MethodInfo and use Invoke instead.
                            string methodCacheType = "global::System.Reflection.MethodInfo";
                            writer.WriteLine($"private static {methodCacheType}? {cacheName};");
                            writer.WriteLine($"private static {propertyTypeFQN} {wrapperName}({declaringTypeFQN} obj) => ({propertyTypeFQN})({cacheName} ??= {propertyExpr}.GetGetMethod(true)!).Invoke(obj, null)!;");
                        }
                        else
                        {
                            string delegateType = $"global::System.Func<{declaringTypeFQN}, {propertyTypeFQN}>";
                            writer.WriteLine($"private static {delegateType}? {cacheName};");
                            writer.WriteLine($"private static {propertyTypeFQN} {wrapperName}({declaringTypeFQN} obj) => ({cacheName} ??= ({delegateType})global::System.Delegate.CreateDelegate(typeof({delegateType}), {propertyExpr}.GetGetMethod(true)!))(obj);");
                        }
                    }

                    if (needsSetterAccessor)
                    {
                        string cacheName = GetReflectionCacheName(typeFriendlyName, "set", member.MemberName, i, disambiguate);
                        string wrapperName = GetAccessorName(typeFriendlyName, "set", member.MemberName, i, disambiguate);

                        if (declaringTypeIsValueType)
                        {
                            // For value types, use a ref-parameter delegate to mutate the unboxed value in-place.
                            needsValueTypeSetterDelegate = true;
                            string delegateType = $"ValueTypeSetter<{declaringTypeFQN}, {propertyTypeFQN}>";
                            writer.WriteLine($"private static {delegateType}? {cacheName};");
                            writer.WriteLine($"private static void {wrapperName}(ref {declaringTypeFQN} obj, {propertyTypeFQN} value) => ({cacheName} ??= ({delegateType})global::System.Delegate.CreateDelegate(typeof({delegateType}), {propertyExpr}.GetSetMethod(true)!))(ref obj, value);");
                        }
                        else
                        {
                            string delegateType = $"global::System.Action<{declaringTypeFQN}, {propertyTypeFQN}>";
                            writer.WriteLine($"private static {delegateType}? {cacheName};");
                            writer.WriteLine($"private static void {wrapperName}({declaringTypeFQN} obj, {propertyTypeFQN} value) => ({cacheName} ??= ({delegateType})global::System.Delegate.CreateDelegate(typeof({delegateType}), {propertyExpr}.GetSetMethod(true)!))(obj, value);");
                        }
                    }
                }
                else
                {
                    // Reflection fallback for fields: cache the FieldInfo and use GetValue/SetValue.
                    // Fields don't have MethodInfo, so Delegate.CreateDelegate can't be used.
                    string fieldExpr = $"typeof({declaringTypeFQN}).GetField({FormatStringLiteral(member.MemberName)}, InstanceMemberBindingFlags)!";
                    string fieldCacheName = GetReflectionCacheName(typeFriendlyName, "field", member.MemberName, i, disambiguate);
                    writer.WriteLine($"private static global::System.Reflection.FieldInfo? {fieldCacheName};");

                    if (needsGetterAccessor)
                    {
                        string wrapperName = GetAccessorName(typeFriendlyName, "get", member.MemberName, i, disambiguate);
                        writer.WriteLine($"private static {propertyTypeFQN} {wrapperName}(object obj) => ({propertyTypeFQN})({fieldCacheName} ??= {fieldExpr}).GetValue(obj)!;");
                    }

                    if (needsSetterAccessor)
                    {
                        string wrapperName = GetAccessorName(typeFriendlyName, "set", member.MemberName, i, disambiguate);
                        writer.WriteLine($"private static void {wrapperName}(object obj, {propertyTypeFQN} value) => ({fieldCacheName} ??= {fieldExpr}).SetValue(obj, value);");
                    }
                }
            }

            // Emit generic wrapper classes for UnsafeAccessors on generic types (.NET 9+).
            if (genericAccessorEntries is not null)
            {
                foreach (KeyValuePair<string, List<(UnsafeAccessorMemberSpec Member, int Index, bool Disambiguate)>> kvp in genericAccessorEntries)
                {
                    List<(UnsafeAccessorMemberSpec Member, int Index, bool Disambiguate)> entries = kvp.Value;
                    UnsafeAccessorMemberSpec firstMember = entries[0].Member;
                    ImmutableEquatableArray<string> typeParams = firstMember.DeclaringTypeParameterNames!;
                    string openDeclaringTypeFQN = firstMember.OpenDeclaringTypeFQN!;
                    string refPrefix = declaringTypeIsValueType ? "ref " : "";
                    string typeParamList = string.Join(", ", typeParams);
                    string constraintClauses = firstMember.DeclaringTypeParameterConstraintClauses is { } c ? $" {c}" : "";

                    writer.WriteLine();
                    writer.WriteLine($"private static class __GenericAccessors_{typeFriendlyName}<{typeParamList}>{constraintClauses}");
                    writer.WriteLine('{');
                    writer.Indentation++;

                    foreach ((UnsafeAccessorMemberSpec member, int index, bool disambiguate) in entries)
                    {
                        bool needsGetter = member.NeedsGetter;
                        bool needsSetter = member.NeedsSetter;
                        string openPropertyTypeFQN = member.OpenMemberTypeFQN!;

                        if (member.IsProperty)
                        {
                            if (needsGetter)
                            {
                                string accessorName = GetAccessorName(typeFriendlyName, "get", member.MemberName, index, disambiguate);
                                writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Method, Name = "get_{member.MemberName}")]""");
                                writer.WriteLine($"public static extern {openPropertyTypeFQN} {accessorName}({refPrefix}{openDeclaringTypeFQN} obj);");
                            }

                            if (needsSetter)
                            {
                                string accessorName = GetAccessorName(typeFriendlyName, "set", member.MemberName, index, disambiguate);
                                writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Method, Name = "set_{member.MemberName}")]""");
                                writer.WriteLine($"public static extern void {accessorName}({refPrefix}{openDeclaringTypeFQN} obj, {openPropertyTypeFQN} value);");
                            }
                        }
                        else
                        {
                            string fieldAccessorName = GetAccessorName(typeFriendlyName, "field", member.MemberName, index, disambiguate);
                            writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Field, Name = "{member.MemberName}")]""");
                            writer.WriteLine($"public static extern ref {openPropertyTypeFQN} {fieldAccessorName}({refPrefix}{openDeclaringTypeFQN} obj);");
                        }
                    }

                    writer.Indentation--;
                    writer.WriteLine('}');
                }
            }

            return needsValueTypeSetterDelegate;
        }

        /// <summary>
        /// Emits a constructor accessor: a <c>[UnsafeAccessor(Constructor)]</c> extern where supported, otherwise a
        /// cached <see cref="System.Reflection.ConstructorInfo"/> wrapper. The wrapper has the same signature in both
        /// cases: <c>static TypeName __ctor_TypeName(params)</c>.
        /// </summary>
        public static void EmitConstructorAccessor(SourceWriter writer, UnsafeAccessorConstructorSpec spec)
        {
            string typeFQN = spec.TypeFQN;
            string wrapperName = GetConstructorAccessorName(spec.TypeFriendlyName);
            ImmutableEquatableArray<UnsafeAccessorParameterSpec> parameters = spec.Parameters;

            // Build the parameter list for the wrapper method.
            var wrapperParams = new StringBuilder();

            foreach (UnsafeAccessorParameterSpec param in parameters)
            {
                if (wrapperParams.Length > 0)
                {
                    wrapperParams.Append(", ");
                }

                wrapperParams.Append($"{param.TypeFQN} p{param.Index}");
            }

            if (spec.CanUseUnsafeAccessor)
            {
                writer.WriteLine($"[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Constructor)]");
                writer.WriteLine($"private static extern {typeFQN} {wrapperName}({wrapperParams});");
            }
            else
            {
                // Reflection fallback: cached ConstructorInfo + Invoke.
                // Note: ConstructorInfo cannot be wrapped in a delegate, so we cache the ConstructorInfo directly.
                string cacheName = GetConstructorReflectionCacheName(spec.TypeFriendlyName);

                string argTypes = parameters.Count == 0
                    ? EmptyTypeArray
                    : $"new global::System.Type[] {{{string.Join(", ", parameters.Select(p => $"typeof({p.TypeFQN})"))}}}";

                writer.WriteLine($"private static global::System.Reflection.ConstructorInfo? {cacheName};");

                string invokeArgs = parameters.Count == 0
                    ? "null"
                    : $"new object?[] {{{string.Join(", ", parameters.Select(p => $"p{p.Index}"))}}}";

                writer.WriteLine($"private static {typeFQN} {wrapperName}({wrapperParams}) => ({typeFQN})({cacheName} ??= typeof({typeFQN}).GetConstructor(InstanceMemberBindingFlags, binder: null, {argTypes}, modifiers: null)!).Invoke({invokeArgs});");
            }
        }

        private static string FormatStringLiteral(string? value) => value is null ? "null" : SymbolDisplay.FormatLiteral(value, quote: true);
    }
}
