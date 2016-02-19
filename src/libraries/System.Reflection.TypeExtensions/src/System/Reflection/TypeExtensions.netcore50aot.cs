// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Reflection.Core.Execution.Binder;
using Internal.Reflection.Extensions.NonPortable;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Reflection
{
    /// <summary>
    /// Extension methods offering source-code compatibility with certain instance methods of <see cref="System.Type"/> on other platforms.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Searches for a public instance constructor whose parameters match the types in the specified array.
        /// </summary>
        /// <param name="type">Type from which to get constructor</param>
        /// <param name="types">  An array of Type objects representing the number, order, and type of the parameters for the desired constructor.
        /// -or- 
        /// An empty array of Type objects, to get a constructor that takes no parameters. Such an empty array is provided by the Array.Empty method. </param>
        /// <returns>Specific ConstructorInfo for type specified in "type" parameter</returns>
        public static ConstructorInfo GetConstructor(this Type type, Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }
            GetTypeInfoOrThrow(type);

            IEnumerable<ConstructorInfo> constructors = MemberEnumerator.GetMembers<ConstructorInfo>(type, MemberEnumerator.AnyName, Helpers.DefaultLookup);
            return Disambiguate(constructors, types);
        }

        /// <summary>
        /// Returns all the public constructors defined for the current Type.
        /// </summary>
        /// <param name="type">Type to retrieve constructors for</param>
        /// <returns>An array of ConstructorInfo objects representing all the public instance constructors defined for the current Type, but not including the type initializer (static constructor). If no public instance constructors are defined for the current Type, or if the current Type represents a type parameter in the definition of a generic type or generic method, an empty array of type ConstructorInfo is returned.</returns>
        public static ConstructorInfo[] GetConstructors(this Type type)
        {
            return GetConstructors(type, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for the constructors defined for the current Type, using the specified BindingFlags.
        /// </summary>
        /// <param name="type">Type to retrieve constructors for</param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted.
        /// -or- 
        /// Zero, to return null. </param>
        /// <returns>An array of ConstructorInfo objects representing all constructors defined for the current Type that match the specified binding constraints, including the type initializer if it is defined. Returns an empty array of type ConstructorInfo if no constructors are defined for the current Type, if none of the defined constructors match the binding constraints, or if the current Type represents a type parameter in the definition of a generic type or generic method.</returns>
        public static ConstructorInfo[] GetConstructors(this Type type, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<ConstructorInfo> constructors = type.GetMembers<ConstructorInfo>(MemberEnumerator.AnyName, bindingAttr);
            return constructors.ToArray();
        }

        /// <summary>
        /// Searches for the members defined for the current Type whose DefaultMemberAttribute is set.
        /// </summary>
        /// <param name="type">Type to be queried</param>
        /// <returns>An array of MemberInfo objects representing all default members of the current Type.
        /// -or- 
        /// An empty array of type MemberInfo, if the current Type does not have default members.</returns>
        public static MemberInfo[] GetDefaultMembers(this Type type)
        {
            TypeInfo typeInfo = GetTypeInfoOrThrow(type);
            string defaultMemberName = GetDefaultMemberName(typeInfo);

            if (defaultMemberName == null)
            {
                return Helpers.EmptyMemberArray;
            }

            return GetMember(type, defaultMemberName);
        }

        /// <summary>
        /// Returns the EventInfo object representing the specified public event.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of an event that is declared or inherited by the current Type. </param>
        /// <returns>The object representing the specified public event that is declared or inherited by the current Type, if found; otherwise, null.</returns>
        public static EventInfo GetEvent(this Type type, string name)
        {
            return GetEvent(type, name, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Returns the EventInfo object representing the specified event, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of an event which is declared or inherited by the current Type. </param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted.
        /// -or- 
        /// Zero, to return null. </param>
        /// <returns>The object representing the specified event that is declared or inherited by the current Type, if found; otherwise, null.</returns>
        public static EventInfo GetEvent(this Type type, string name, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<EventInfo> events = MemberEnumerator.GetMembers<EventInfo>(type, name, bindingAttr);
            return Disambiguate(events);
        }

        /// <summary>
        /// Returns all the public events that are declared or inherited by the current Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <returns>An array of EventInfo objects representing all the public events which are declared or inherited by the current Type.
        /// -or- 
        /// An empty array of type EventInfo, if the current Type does not have public events.</returns>
        public static EventInfo[] GetEvents(this Type type)
        {
            return GetEvents(type, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for events that are declared or inherited by the current Type, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted.
        /// -or- 
        /// Zero, to return null. </param>
        /// <returns>An array of EventInfo objects representing all events that are declared or inherited by the current Type that match the specified binding constraints.
        /// -or- 
        /// An empty array of type EventInfo, if the current Type does not have events, or if none of the events match the binding constraints.</returns>
        public static EventInfo[] GetEvents(this Type type, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<EventInfo> events = MemberEnumerator.GetMembers<EventInfo>(type, MemberEnumerator.AnyName, bindingAttr);
            return events.ToArray();
        }

        /// <summary>
        /// Searches for the public field with the specified name.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the data field to get. </param>
        /// <returns>An object representing the public field with the specified name, if found; otherwise, null.</returns>
        public static FieldInfo GetField(this Type type, string name)
        {
            return GetField(type, name, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for the specified field, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the data field to get. </param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted.
        /// -or- 
        /// Zero, to return null.</param>
        /// <returns>An object representing the field that matches the specified requirements, if found; otherwise, null.</returns>
        public static FieldInfo GetField(this Type type, string name, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<FieldInfo> fields = MemberEnumerator.GetMembers<FieldInfo>(type, name, bindingAttr);
            return Disambiguate(fields);
        }

        /// <summary>
        /// Returns all the public fields of the current Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <returns>An array of FieldInfo objects representing all the public fields defined for the current Type.
        /// -or- 
        /// An empty array of type FieldInfo, if no public fields are defined for the current Type.</returns>
        public static FieldInfo[] GetFields(this Type type)
        {
            return GetFields(type, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for the fields defined for the current Type, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted
        /// -or- 
        /// Zero, to return an empty array. </param>
        /// <returns>An array of FieldInfo objects representing all fields defined for the current Type that match the specified binding constraints.
        /// -or- 
        /// An empty array of type FieldInfo, if no fields are defined for the current Type, or if none of the defined fields match the binding constraints.</returns>
        public static FieldInfo[] GetFields(this Type type, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            return MemberEnumerator.GetMembers<FieldInfo>(type, MemberEnumerator.AnyName, bindingAttr).ToArray();
        }

        /// <summary>
        /// Returns an array of Type objects that represent the type arguments of a generic type or the type parameters of a generic type definition
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <returns>An array of Type objects that represent the type arguments of a generic type. Returns an empty array if the current type is not a generic type.</returns>
        public static Type[] GetGenericArguments(this Type type)
        {
            TypeInfo typeInfo = GetTypeInfoOrThrow(type);

            if (type.IsConstructedGenericType)
            {
                return type.GenericTypeArguments;
            }

            if (typeInfo.IsGenericTypeDefinition)
            {
                return typeInfo.GenericTypeParameters;
            }

            return Helpers.EmptyTypeArray;
        }

        /// <summary>
        /// Gets all the interfaces implemented or inherited by the current Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <returns>An array of Type objects representing all the interfaces implemented or inherited by the current Type.
        /// -or- 
        /// An empty array of type Type, if no interfaces are implemented or inherited by the current Type.</returns>
        public static Type[] GetInterfaces(this Type type)
        {
            TypeInfo typeInfo = GetTypeInfoOrThrow(type);
            return typeInfo.ImplementedInterfaces.ToArray();
        }

        /// <summary>
        /// Searches for the public members with the specified name.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the public members to get.</param>
        /// <returns>An array of MemberInfo objects representing the public members with the specified name, if found; otherwise, an empty array.</returns>
        public static MemberInfo[] GetMember(this Type type, string name)
        {
            return GetMember(type, name, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for the public members with the specified name.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name"> The string containing the name of the public members to get.  </param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted
        /// -or- 
        /// Zero, to return an empty array. </param>
        /// <returns>An array of MemberInfo objects representing the public members with the specified name, if found; otherwise, an empty array.</returns>
        public static MemberInfo[] GetMember(this Type type, string name, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            LowLevelList<MemberInfo> members = GetMembers(type, name, bindingAttr);
            return members.ToArray();
        }

        /// <summary>
        /// Returns all the public members of the current Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <returns>An array of MemberInfo objects representing all the public members of the current Type
        /// -or- 
        /// An empty array of type MemberInfo, if the current Type does not have public members.</returns>
        public static MemberInfo[] GetMembers(this Type type)
        {
            return GetMembers(type, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for the members defined for the current Type, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted
        /// -or- 
        /// Zero, to return an empty array. </param>
        /// <returns>An array of MemberInfo objects representing all members defined for the current Type that match the specified binding constraints.
        /// -or- 
        /// An empty array of type MemberInfo, if no members are defined for the current Type, or if none of the defined members match the binding constraints.</returns>
        public static MemberInfo[] GetMembers(this Type type, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            LowLevelList<MemberInfo> members = GetMembers(type, MemberEnumerator.AnyName, bindingAttr);
            return members.ToArray();
        }

        /// <summary>
        /// Searches for the public method with the specified name.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the public method to get. </param>
        /// <returns>An object that represents the public method with the specified name, if found; otherwise, null</returns>
        public static MethodInfo GetMethod(this Type type, string name)
        {
            return GetMethod(type, name, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for the specified method, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the method to get.</param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted
        /// -or- 
        /// Zero, to return null. </param>
        /// <returns>An object representing the method that matches the specified requirements, if found; otherwise, null.</returns>
        public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<MethodInfo> methods = MemberEnumerator.GetMembers<MethodInfo>(type, name, bindingAttr);
            return Disambiguate(methods);
        }

        /// <summary>
        /// Searches for the specified public method whose parameters match the specified argument types.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the public method to get. </param>
        /// <param name="types">An array of Type objects representing the number, order, and type of the parameters for the method to get 
        /// -or- An empty array of Type objects (as provided by the Array.Empty method) to get a method that takes no parameters. </param>
        /// <returns>An object representing the public method whose parameters match the specified argument types, if found; otherwise, null.</returns>
        public static MethodInfo GetMethod(this Type type, string name, Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }
            GetTypeInfoOrThrow(type);

            IEnumerable<MethodInfo> methods = MemberEnumerator.GetMembers<MethodInfo>(type, name, Helpers.DefaultLookup);
            return Disambiguate(methods, types);
        }

        /// <summary>
        /// Returns all the public methods of the current Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <returns>An array of MethodInfo objects representing all the public methods defined for the current Type.
        /// -or- 
        /// An empty array of type MethodInfo, if no public methods are defined for the current Type.</returns>
        public static MethodInfo[] GetMethods(this Type type)
        {
            return GetMethods(type, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Returns all the public methods of the current Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted
        /// -or- 
        /// Zero, to return an empty array. </param>
        /// <returns>An array of MethodInfo objects representing all the public methods defined for the current Type
        /// -or- 
        /// An empty array of type MethodInfo, if no public methods are defined for the current Type.</returns>
        public static MethodInfo[] GetMethods(this Type type, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<MethodInfo> methods = MemberEnumerator.GetMembers<MethodInfo>(type, MemberEnumerator.AnyName, bindingAttr);
            return methods.ToArray();
        }

        /// <summary>
        /// Searches for the specified nested type, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the nested type to get. </param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted
        /// -or- 
        /// Zero, to return null. </param>
        /// <returns>An object representing the nested type that matches the specified requirements, if found; otherwise, null.</returns>
        public static Type GetNestedType(this Type type, string name, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<TypeInfo> nestedTypes = MemberEnumerator.GetMembers<TypeInfo>(type, name, bindingAttr);
            TypeInfo nestedType = Disambiguate(nestedTypes);
            return nestedType == null ? null : nestedType.AsType();
        }

        /// <summary>
        /// Searches for the types nested in the current Type, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted -or- Zero, to return null. </param>
        /// <returns>An array of Type objects representing all the types nested in the current Type that match the specified binding constraints (the search is not recursive), or an empty array of type Type, if no nested types are found that match the binding constraints.</returns>
        public static Type[] GetNestedTypes(this Type type, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<TypeInfo> types = MemberEnumerator.GetMembers<TypeInfo>(type, MemberEnumerator.AnyName, bindingAttr);
            return types.Select(t => t.AsType()).ToArray();
        }

        /// <summary>
        /// Returns all the public properties of the current Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <returns>An array of PropertyInfo objects representing all public properties of the current Type -or- An empty array of type PropertyInfo, if the current Type does not have public properties</returns>
        public static PropertyInfo[] GetProperties(this Type type)
        {
            return GetProperties(type, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for the properties of the current Type, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted -or- Zero, to return null. </param>
        /// <returns>An array of PropertyInfo objects representing all properties of the current Type that match the specified binding constraints.
        /// -or- 
        /// An empty array of type PropertyInfo, if the current Type does not have properties, or if none of the properties match the binding constraints.</returns>
        public static PropertyInfo[] GetProperties(this Type type, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<PropertyInfo> properties = MemberEnumerator.GetMembers<PropertyInfo>(type, MemberEnumerator.AnyName, bindingAttr);
            return properties.ToArray();
        }

        /// <summary>
        /// Searches for the public property with the specified name.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the public property to get. </param>
        /// <returns>An object representing the public property with the specified name, if found; otherwise, null.</returns>
        public static PropertyInfo GetProperty(this Type type, string name)
        {
            return GetProperty(type, name, Helpers.DefaultLookup);
        }

        /// <summary>
        /// Searches for the specified property, using the specified binding constraints.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the property to get. </param>
        /// <param name="bindingAttr">A bitmask comprised of one or more BindingFlags that specify how the search is conducted.
        /// -or- 
        /// Zero, to return null. </param>
        /// <returns>A bitmask comprised of one or more BindingFlags that specify how the search is conducted.
        /// -or- 
        /// Zero, to return null. </returns>
        public static PropertyInfo GetProperty(this Type type, string name, BindingFlags bindingAttr)
        {
            GetTypeInfoOrThrow(type);

            IEnumerable<PropertyInfo> properties = MemberEnumerator.GetMembers<PropertyInfo>(type, name, bindingAttr);
            return Disambiguate(properties);
        }

        /// <summary>
        /// Searches for the public property with the specified name and return type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the public property to get. </param>
        /// <param name="returnType">The return type of the property. </param>
        /// <returns>An object representing the public property with the specified name, if found; otherwise, null</returns>
        public static PropertyInfo GetProperty(this Type type, string name, Type returnType)
        {
            return GetProperty(type, name, returnType, Array.Empty<Type>());
        }

        /// <summary>
        /// Searches for the specified public property whose parameters match the specified argument types.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="name">The string containing the name of the public property to get. </param>
        /// <param name="returnType">The return type of the property. </param>
        /// <param name="types">
        /// An array of Type objects representing the number, order, and type of the parameters for the indexed property to get.
        /// -or- 
        /// An empty array of the type Type (that is, Type[] types = new Type[0]) to get a property that is not indexed.
        /// </param>
        /// <returns>
        /// An object representing the public property with the specified name, return type, and indexing types, if found; otherwise, null.
        /// </returns>
        public static PropertyInfo GetProperty(this Type type, string name, Type returnType, Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }
            GetTypeInfoOrThrow(type);

            IEnumerable<PropertyInfo> properties = MemberEnumerator.GetMembers<PropertyInfo>(type, name, Helpers.DefaultLookup);
            return Disambiguate(properties, returnType, types);
        }

        /// <summary>
        /// Determines whether an instance of the current Type can be assigned from an instance of the specified Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="c">The type to compare with the current type. </param>
        /// <returns>true if c and the current Type represent the same type, or if the current Type is in the inheritance hierarchy of c, or if the current Type is an interface that c implements, or if c is a generic type parameter and the current Type represents one of the constraints of c, or if c represents a value type and the current Type represents Nullable&lt;c&gt; (Nullable(Of c) in Visual Basic). false if none of these conditions are true, or if c is null.</returns>
        public static bool IsAssignableFrom(this Type type, Type c)
        {
            TypeInfo typeInfo = GetTypeInfoOrThrow(type);
            return c != null && typeInfo.IsAssignableFrom(GetTypeInfoOrThrow(c, "c"));
        }

        /// <summary>
        /// Determines whether the specified object is an instance of the current Type.
        /// </summary>
        /// <param name="type">Type on which to perform lookup</param>
        /// <param name="o">The object to compare with the current type</param>
        /// <returns>true if the current Type is in the inheritance hierarchy of the object represented by o, or if the current Type is an interface that o supports. false if neither of these conditions is the case, or if o is null, or if the current Type is an open generic type (that is, ContainsGenericParameters returns true).</returns>
        public static bool IsInstanceOfType(this Type type, object o)
        {
            Debug.Assert(o == null || o.GetType().GetTypeInfo() != null, "Type obtained from object instance should implement IReflectableType on all platforms that support reflection.");
            return o != null && IsAssignableFrom(type, o.GetType());
        }

        private static LowLevelList<MemberInfo> GetMembers(Type type, object nameFilterOrAnyName, BindingFlags bindingAttr)
        {
            LowLevelList<MemberInfo> members = new LowLevelList<MemberInfo>();

            members.AddRange(MemberEnumerator.GetMembers<MethodInfo>(type, nameFilterOrAnyName, bindingAttr));
            members.AddRange(MemberEnumerator.GetMembers<ConstructorInfo>(type, nameFilterOrAnyName, bindingAttr));
            members.AddRange(MemberEnumerator.GetMembers<PropertyInfo>(type, nameFilterOrAnyName, bindingAttr));
            members.AddRange(MemberEnumerator.GetMembers<EventInfo>(type, nameFilterOrAnyName, bindingAttr));
            members.AddRange(MemberEnumerator.GetMembers<FieldInfo>(type, nameFilterOrAnyName, bindingAttr));
            members.AddRange(MemberEnumerator.GetMembers<TypeInfo>(type, nameFilterOrAnyName, bindingAttr));

            return members;
        }

        private static string GetDefaultMemberName(TypeInfo typeInfo)
        {
            TypeInfo t = typeInfo;

            while (t != null)
            {
                CustomAttributeData attribute = GetDefaultMemberAttribute(typeInfo);
                if (attribute != null)
                {
                    // NOTE: Neither indexing nor cast can fail here. Any attempt to use fewer than 1 argument
                    // or a non-string argument would correctly trigger MissingMethodException before
                    // we reach here as that would be an attempt to reference a non-existent DefaultMemberAttribute
                    // constructor.
                    Debug.Assert(attribute.ConstructorArguments.Count == 1 && attribute.ConstructorArguments[0].Value is string);
                    return (string)attribute.ConstructorArguments[0].Value;
                }

                Type baseType = t.BaseType;
                if (baseType == null)
                {
                    break;
                }

                t = baseType.GetTypeInfo();
            }

            return null;
        }

        private static CustomAttributeData GetDefaultMemberAttribute(TypeInfo typeInfo)
        {
            foreach (CustomAttributeData attribute in typeInfo.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(DefaultMemberAttribute))
                {
                    return attribute;
                }
            }

            return null;
        }

        private static TypeInfo GetTypeInfoOrThrow(Type type, string parameterName = "type")
        {
            Requires.NotNull(type, parameterName);
            TypeInfo typeInfo = type.GetTypeInfo();

            if (typeInfo == null)
            {
                throw new ArgumentException(SR.TypeIsNotReflectable, parameterName);
            }

            return typeInfo;
        }

        private static T Disambiguate<T>(IEnumerable<T> members) where T : MemberInfo
        {
            IEnumerator<T> enumerator = members.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return null;
            }

            T result = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                return result;
            }

            T anotherResult = enumerator.Current;
            if (anotherResult.DeclaringType.Equals(result.DeclaringType))
            {
                throw new AmbiguousMatchException();
            }

            return result;
        }

        private static T Disambiguate<T>(IEnumerable<T> members, Type[] parameterTypes) where T : MethodBase
        {
            return (T)DefaultBinder.SelectMethod(members.ToArray(), parameterTypes);
        }

        private static PropertyInfo Disambiguate(IEnumerable<PropertyInfo> members, Type returnType, Type[] parameterTypes)
        {
            PropertyInfo[] memberArray = members.ToArray();

            if(memberArray.Length == 0)
            {
                return null;
            }

            return DefaultBinder.SelectProperty(memberArray, returnType, parameterTypes);
        }
    }
}
