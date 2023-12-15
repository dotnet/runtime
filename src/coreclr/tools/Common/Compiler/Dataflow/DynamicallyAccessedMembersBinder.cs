// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Dataflow
{
    internal static class DynamicallyAccessedMembersBinder
    {
        // Returns the members of the type bound by memberTypes. For DynamicallyAccessedMemberTypes.All, this returns all members of the type and its
        // nested types, including interface implementations, plus the same or any base types or implemented interfaces.
        // DynamicallyAccessedMemberTypes.PublicNestedTypes and NonPublicNestedTypes do the same for members of the selected nested types.
        public static IEnumerable<TypeSystemEntity> GetDynamicallyAccessedMembers(this TypeDesc typeDefinition, DynamicallyAccessedMemberTypes memberTypes, bool declaredOnly = false)
        {
            if (memberTypes == DynamicallyAccessedMemberTypes.None)
                yield break;

            if (memberTypes == DynamicallyAccessedMemberTypes.All)
            {
                var members = new List<TypeSystemEntity>();
                typeDefinition.GetAllOnType(declaredOnly, members);
                foreach (var m in members)
                    yield return m;
                yield break;
            }

            var declaredOnlyFlags = declaredOnly ? BindingFlags.DeclaredOnly : BindingFlags.Default;

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.NonPublicConstructors))
            {
                foreach (var c in typeDefinition.GetConstructorsOnType(filter: null, bindingFlags: BindingFlags.NonPublic))
                    yield return c;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicConstructors))
            {
                foreach (var c in typeDefinition.GetConstructorsOnType(filter: null, bindingFlags: BindingFlags.Public))
                    yield return c;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor))
            {
                foreach (var c in typeDefinition.GetConstructorsOnType(filter: m => m.IsPublic() && !m.HasMetadataParameters()))
                    yield return c;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.NonPublicMethods))
            {
                foreach (var m in typeDefinition.GetMethodsOnTypeHierarchy(filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
                    yield return m;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicMethods))
            {
                foreach (var m in typeDefinition.GetMethodsOnTypeHierarchy(filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
                    yield return m;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.NonPublicFields))
            {
                foreach (var f in typeDefinition.GetFieldsOnTypeHierarchy(filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
                    yield return f;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicFields))
            {
                foreach (var f in typeDefinition.GetFieldsOnTypeHierarchy(filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
                    yield return f;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.NonPublicNestedTypes))
            {
                foreach (var t in typeDefinition.GetNestedTypesOnType(filter: null, bindingFlags: BindingFlags.NonPublic))
                {
                    yield return t;
                    var members = new List<TypeSystemEntity>();
                    t.GetAllOnType(declaredOnly: false, members);
                    foreach (var m in members)
                        yield return m;
                }
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicNestedTypes))
            {
                foreach (var t in typeDefinition.GetNestedTypesOnType(filter: null, bindingFlags: BindingFlags.Public))
                {
                    yield return t;
                    var members = new List<TypeSystemEntity>();
                    t.GetAllOnType(declaredOnly: false, members);
                    foreach (var m in members)
                        yield return m;
                }
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.NonPublicProperties))
            {
                foreach (var p in typeDefinition.GetPropertiesOnTypeHierarchy(filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
                    yield return p;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicProperties))
            {
                foreach (var p in typeDefinition.GetPropertiesOnTypeHierarchy(filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
                    yield return p;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.NonPublicEvents))
            {
                foreach (var e in typeDefinition.GetEventsOnTypeHierarchy(filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
                    yield return e;
            }

            if (memberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicEvents))
            {
                foreach (var e in typeDefinition.GetEventsOnTypeHierarchy(filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
                    yield return e;
            }
        }

        public static IEnumerable<MethodDesc> GetConstructorsOnType(this TypeDesc type, Func<MethodDesc, bool> filter, BindingFlags? bindingFlags = null)
        {
            if (type.IsArray)
            {
                // Constructors on arrays are special magic that the reflection stack special cases at runtime anyway.
                yield break;
            }

            foreach (var method in type.GetMethods())
            {
                if (!method.IsConstructor && !method.IsStaticConstructor)
                    continue;

                if (filter != null && !filter(method))
                    continue;

                if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.Signature.IsStatic)
                    continue;

                if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.Signature.IsStatic)
                    continue;

                if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic())
                    continue;

                if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic())
                    continue;

                yield return method;
            }
        }

        public static IEnumerable<MethodDesc> GetMethodsOnTypeHierarchy(this TypeDesc type, Func<MethodDesc, bool> filter, BindingFlags? bindingFlags = null)
        {
            bool onBaseType = false;

            if (type.IsArray)
            {
                // Methods on arrays are special magic that the reflection stack special cases at runtime anyway.
                type = type.BaseType;
                onBaseType = true;
            }

            while (type != null)
            {
                foreach (var method in type.GetMethods())
                {
                    // Ignore constructors as those are not considered methods from a reflection's point of view
                    if (method.IsConstructor)
                        continue;

                    // Ignore private methods on a base type - those are completely ignored by reflection
                    // (anything private on the base type is not visible via the derived type)
                    if (onBaseType && method.IsPrivate())
                        continue;

                    // Note that special methods like property getter/setter, event adder/remover will still get through and will be marked.
                    // This is intentional as reflection treats these as methods as well.

                    if (filter != null && !filter(method))
                        continue;

                    if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.Signature.IsStatic)
                        continue;

                    if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.Signature.IsStatic)
                        continue;

                    if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic())
                        continue;

                    if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic())
                        continue;

                    yield return method;
                }

                if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
                    yield break;

                type = type.TryGetBaseType();
                onBaseType = true;
            }
        }

        public static IEnumerable<FieldDesc> GetFieldsOnTypeHierarchy(this TypeDesc type, Func<FieldDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            bool onBaseType = false;
            while (type != null)
            {
                foreach (var field in type.GetFields())
                {
                    // Ignore private fields on a base type - those are completely ignored by reflection
                    // (anything private on the base type is not visible via the derived type)
                    if (onBaseType && field.IsPrivate())
                        continue;

                    // Note that compiler generated fields backing some properties and events will get through here.
                    // This is intentional as reflection treats these as fields as well.

                    if (filter != null && !filter(field))
                        continue;

                    if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !field.IsStatic)
                        continue;

                    if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && field.IsStatic)
                        continue;

                    if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !field.IsPublic())
                        continue;

                    if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && field.IsPublic())
                        continue;

                    yield return field;
                }

                if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
                    yield break;

                type = type.TryGetBaseType();
                onBaseType = true;
            }
        }

        public static IEnumerable<MetadataType> GetNestedTypesOnType(this TypeDesc type, Func<MetadataType, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            if (type is not MetadataType mdType)
                yield break;

            foreach (var nestedType in mdType.GetNestedTypes())
            {
                if (filter != null && !filter(nestedType))
                    continue;

                if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public)
                {
                    if (!nestedType.IsNestedPublic())
                        continue;
                }

                if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic)
                {
                    if (nestedType.IsNestedPublic())
                        continue;
                }

                yield return nestedType;
            }
        }

        public static IEnumerable<PropertyPseudoDesc> GetPropertiesOnTypeHierarchy(this TypeDesc type, Func<PropertyPseudoDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            bool onBaseType = false;

            if (type.IsArray)
            {
                type = type.BaseType;
                onBaseType = true;
            }

            while (type != null)
            {
                if (type.GetTypeDefinition() is not EcmaType ecmaType)
                {
                    yield break;
                }

                foreach (var propertyHandle in ecmaType.MetadataReader.GetTypeDefinition(ecmaType.Handle).GetProperties())
                {
                    var property = new PropertyPseudoDesc(ecmaType, propertyHandle);

                    // Ignore private properties on a base type - those are completely ignored by reflection
                    // (anything private on the base type is not visible via the derived type)
                    // Note that properties themselves are not actually private, their accessors are
                    if (onBaseType &&
                        (property.GetMethod == null || property.GetMethod.IsPrivate()) &&
                        (property.SetMethod == null || property.SetMethod.IsPrivate()))
                        continue;

                    if (filter != null && !filter(property))
                        continue;

                    if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static)
                    {
                        if ((property.GetMethod != null) && !property.GetMethod.Signature.IsStatic) continue;
                        if ((property.SetMethod != null) && !property.SetMethod.Signature.IsStatic) continue;
                    }

                    if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance)
                    {
                        if ((property.GetMethod != null) && property.GetMethod.Signature.IsStatic) continue;
                        if ((property.SetMethod != null) && property.SetMethod.Signature.IsStatic) continue;
                    }

                    if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public)
                    {
                        if ((property.GetMethod == null || !property.GetMethod.IsPublic())
                            && (property.SetMethod == null || !property.SetMethod.IsPublic()))
                            continue;
                    }

                    if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic)
                    {
                        if ((property.GetMethod != null) && property.GetMethod.IsPublic()) continue;
                        if ((property.SetMethod != null) && property.SetMethod.IsPublic()) continue;
                    }

                    yield return property;
                }

                if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
                    yield break;

                type = type.TryGetBaseType();
                onBaseType = true;
            }
        }

        public static IEnumerable<EventPseudoDesc> GetEventsOnTypeHierarchy(this TypeDesc type, Func<EventPseudoDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            bool onBaseType = false;

            if (type.IsArray)
            {
                type = type.BaseType;
                onBaseType = true;
            }

            while (type != null)
            {
                if (type.GetTypeDefinition() is not EcmaType ecmaType)
                {
                    yield break;
                }

                foreach (var eventHandle in ecmaType.MetadataReader.GetTypeDefinition(ecmaType.Handle).GetEvents())
                {
                    var @event = new EventPseudoDesc(ecmaType, eventHandle);

                    // Ignore private properties on a base type - those are completely ignored by reflection
                    // (anything private on the base type is not visible via the derived type)
                    // Note that properties themselves are not actually private, their accessors are
                    if (onBaseType &&
                        (@event.AddMethod == null || @event.AddMethod.IsPrivate()) &&
                        (@event.RemoveMethod == null || @event.RemoveMethod.IsPrivate()))
                        continue;

                    if (filter != null && !filter(@event))
                        continue;

                    if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static)
                    {
                        if ((@event.AddMethod != null) && !@event.AddMethod.Signature.IsStatic) continue;
                        if ((@event.RemoveMethod != null) && !@event.RemoveMethod.Signature.IsStatic) continue;
                    }

                    if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance)
                    {
                        if ((@event.AddMethod != null) && @event.AddMethod.Signature.IsStatic) continue;
                        if ((@event.RemoveMethod != null) && @event.RemoveMethod.Signature.IsStatic) continue;
                    }

                    if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public)
                    {
                        if ((@event.AddMethod == null || !@event.AddMethod.IsPublic())
                            && (@event.RemoveMethod == null || !@event.RemoveMethod.IsPublic()))
                            continue;
                    }

                    if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic)
                    {
                        if ((@event.AddMethod != null) && @event.AddMethod.IsPublic()) continue;
                        if ((@event.RemoveMethod != null) && @event.RemoveMethod.IsPublic()) continue;
                    }

                    yield return @event;
                }

                if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
                    yield break;

                type = type.TryGetBaseType();
                onBaseType = true;
            }
        }

        // declaredOnly will cause this to retrieve interfaces recursively required by the type, but doesn't necessarily
        // include interfaces required by any base types.
        public static IEnumerable<DefType> GetAllInterfaceImplementations(this TypeDesc type, bool declaredOnly)
        {
            while (type != null)
            {
                foreach (DefType i in type.TryGetExplicitlyImplementedInterfaces())
                {
                    yield return i;

                    // declaredOnly here doesn't matter since interfaces don't have base types
                    foreach (DefType innerInterface in i.GetAllInterfaceImplementations(declaredOnly: true))
                        yield return innerInterface;
                }

                if (declaredOnly)
                    yield break;

                type = type.TryGetBaseType();
            }
        }

        // declaredOnly will cause this to retrieve only members of the type, not of its base types. This includes interfaces recursively
        // required by this type (but not members of these interfaces, or interfaces required only by base types).
        public static void GetAllOnType(this TypeDesc type, bool declaredOnly, List<TypeSystemEntity> members) => GetAllOnType(type, declaredOnly, members, new HashSet<TypeDesc>());

        private static void GetAllOnType(TypeDesc type, bool declaredOnly, List<TypeSystemEntity> members, HashSet<TypeDesc> types)
        {
            if (!types.Add(type))
                return;

            if (type is MetadataType mdType)
            {
                foreach (MetadataType nested in mdType.GetNestedTypes())
                {
                    members.Add(nested);
                    // Base types and interfaces of nested types are always included.
                    GetAllOnType(nested, declaredOnly: false, members, types);
                }
            }

            if (!declaredOnly)
            {
                DefType baseType = type.TryGetBaseType();
                if (baseType != null)
                    GetAllOnType(baseType, declaredOnly: false, members, types);
            }

            if (declaredOnly)
            {
                foreach (DefType iface in type.GetAllInterfaceImplementations(declaredOnly: true))
                    members.Add(iface);
            }
            else
            {
                foreach (DefType interfaceType in type.TryGetExplicitlyImplementedInterfaces())
                {
                    members.Add(interfaceType);
                    GetAllOnType(interfaceType, declaredOnly: false, members, types);
                }
            }

            foreach (var f in type.GetFields())
                members.Add(f);

            foreach (var m in type.GetMethods())
                members.Add(m);

            foreach (var p in type.GetPropertiesOnTypeHierarchy(filter: null, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                members.Add(p);

            foreach (var e in type.GetEventsOnTypeHierarchy(filter: null, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                members.Add(e);
        }

        private static DefType TryGetBaseType(this TypeDesc type)
        {
            try
            {
                return type.BaseType;
            }
            catch (TypeSystemException)
            {
                return null;
            }
        }

        private static DefType[] TryGetExplicitlyImplementedInterfaces(this TypeDesc type)
        {
            if (type is MetadataType mdType)
            {
                try
                {
                    return mdType.ExplicitlyImplementedInterfaces;
                }
                catch (TypeSystemException)
                {
                }
            }
            return Array.Empty<DefType>();
        }
    }
}
