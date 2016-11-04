// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: These are extension methods in the contract, but plain static methods
// in this implementation. This is done to avoid confusion around what would 
// look like infinite recursion in the implementation. Callers compiled against
// the contract will still be able to invoke them as extension methods and get
// source compatibility with classic reflection code.
//
// However, this does not apply if there is no 1:1 correspondence with an instance
// in mscorlib. New extension methods should be marked with 'this'.

namespace System.Reflection
{
    public static class TypeExtensions
    {
        public static ConstructorInfo GetConstructor(Type type, Type[] types)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetConstructor(types);
        }

        public static ConstructorInfo[] GetConstructors(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetConstructors();
        }

        public static ConstructorInfo[] GetConstructors(Type type, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetConstructors(bindingAttr);
        }

        public static MemberInfo[] GetDefaultMembers(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetDefaultMembers();
        }

        public static EventInfo GetEvent(Type type, string name)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetEvent(name);
        }

        public static EventInfo GetEvent(Type type, string name, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetEvent(name, bindingAttr);
        }

        public static EventInfo[] GetEvents(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetEvents();
        }

        public static EventInfo[] GetEvents(Type type, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetEvents(bindingAttr);
        }

        public static FieldInfo GetField(Type type, string name)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetField(name);
        }

        public static FieldInfo GetField(Type type, string name, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetField(name, bindingAttr);
        }

        public static FieldInfo[] GetFields(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetFields();
        }

        public static FieldInfo[] GetFields(Type type, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetFields(bindingAttr);
        }

        public static Type[] GetGenericArguments(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetGenericArguments();
        }

        public static Type[] GetInterfaces(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetInterfaces();
        }

        public static MemberInfo[] GetMember(Type type, string name)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMember(name);
        }

        public static MemberInfo[] GetMember(Type type, string name, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMember(name, bindingAttr);
        }

        public static MemberInfo[] GetMembers(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMembers();
        }

        public static MemberInfo[] GetMembers(Type type, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMembers(bindingAttr);
        }

        public static MethodInfo GetMethod(Type type, string name)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMethod(name);
        }

        public static MethodInfo GetMethod(Type type, string name, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMethod(name, bindingAttr);
        }

        public static MethodInfo GetMethod(Type type, string name, Type[] types)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMethod(name, types);
        }

        public static MethodInfo[] GetMethods(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMethods();
        }

        public static MethodInfo[] GetMethods(Type type, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetMethods(bindingAttr);
        }

        public static Type GetNestedType(Type type, string name, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetNestedType(name, bindingAttr);
        }

        public static Type[] GetNestedTypes(Type type, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetNestedTypes(bindingAttr);
        }

        public static PropertyInfo[] GetProperties(Type type)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetProperties();
        }

        public static PropertyInfo[] GetProperties(Type type, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetProperties(bindingAttr);
        }

        public static PropertyInfo GetProperty(Type type, string name)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetProperty(name);
        }

        public static PropertyInfo GetProperty(Type type, string name, BindingFlags bindingAttr)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetProperty(name, bindingAttr);
        }

        public static PropertyInfo GetProperty(Type type, string name, Type returnType)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetProperty(name, returnType);
        }

        public static PropertyInfo GetProperty(Type type, string name, Type returnType, Type[] types)
        {
            Requires.NotNull(type, nameof(type));
            return type.GetProperty(name, returnType, types);
        }

        public static bool IsAssignableFrom(Type type, Type c)
        {
            Requires.NotNull(type, nameof(type));
            return type.IsAssignableFrom(c);
        }

        public static bool IsInstanceOfType(Type type, object o)
        {
            Requires.NotNull(type, nameof(type));
            return type.IsInstanceOfType(o);
        }
    }

    public static class AssemblyExtensions
    {
        public static Type[] GetExportedTypes(Assembly assembly)
        {
            Requires.NotNull(assembly, nameof(assembly));
            return assembly.GetExportedTypes();
        }

        public static Module[] GetModules(Assembly assembly)
        {
            Requires.NotNull(assembly, nameof(assembly));
            return assembly.GetModules();
        }

        public static Type[] GetTypes(Assembly assembly)
        {
            Requires.NotNull(assembly, nameof(assembly));
            return assembly.GetTypes();
        }
    }

    public static class EventInfoExtensions
    {
        public static MethodInfo GetAddMethod(EventInfo eventInfo)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return eventInfo.GetAddMethod();
        }

        public static MethodInfo GetAddMethod(EventInfo eventInfo, bool nonPublic)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return eventInfo.GetAddMethod(nonPublic);
        }

        public static MethodInfo GetRaiseMethod(EventInfo eventInfo)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return eventInfo.GetRaiseMethod();
        }

        public static MethodInfo GetRaiseMethod(EventInfo eventInfo, bool nonPublic)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return eventInfo.GetRaiseMethod(nonPublic);
        }

        public static MethodInfo GetRemoveMethod(EventInfo eventInfo)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return eventInfo.GetRemoveMethod();
        }

        public static MethodInfo GetRemoveMethod(EventInfo eventInfo, bool nonPublic)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return eventInfo.GetRemoveMethod(nonPublic);
        }
    }

    public static class MemberInfoExtensions
    {

        /// <summary>
        /// Determines if there is a metadata token available for the given member.
        /// <see cref="GetMetadataToken(MemberInfo)"/> throws <see cref="InvalidOperationException"/> otherwise.
        /// </summary>
        /// <remarks>This maybe</remarks>
        public static bool HasMetadataToken(this MemberInfo member)
        {
            Requires.NotNull(member, nameof(member));

            try
            {
                return GetMetadataTokenOrZeroOrThrow(member) != 0;
            }
            catch (InvalidOperationException)
            {
                // Thrown for unbaked ref-emit members/types. 
                // Other cases such as typeof(byte[]).MetadataToken will be handled by comparison to zero above.
                return false;
            }
        }

        /// <summary>
        /// Gets a metadata token for the given member if available. The returned token is never nil.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// There is no metadata token available. <see cref="HasMetadataToken(MemberInfo)"/> returns false in this case.
        /// </exception>
        public static int GetMetadataToken(this MemberInfo member)
        {
            Requires.NotNull(member, nameof(member));

            int token = GetMetadataTokenOrZeroOrThrow(member); 

            if (token == 0)
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }

            return token;
        }

        private static int GetMetadataTokenOrZeroOrThrow(MemberInfo member)
        {
            int token = member.MetadataToken;

            // Tokens have MSB = table index, 3 LSBs = row index
            // row index of 0 is a nil token 
            const int rowMask = 0x00FFFFFF;
            if ((token & rowMask) == 0)
            {
                // Nil token is returned for edge cases like typeof(byte[]).MetadataToken.
                return 0;
            }

            return token;
         }
    }

    public static class MethodInfoExtensions
    {
        public static MethodInfo GetBaseDefinition(MethodInfo method)
        {
            Requires.NotNull(method, nameof(method));
            return method.GetBaseDefinition();
        }
    }

    public static class ModuleExtensions
    {
        public static bool HasModuleVersionId(this Module module)
        {
            Requires.NotNull(module, nameof(module));
            return true; // not expected to fail on platforms with Module.ModuleVersionId built-in.
        }

        public static Guid GetModuleVersionId(this Module module)
        {
            Requires.NotNull(module, nameof(module));
            return module.ModuleVersionId;
        }
    }

    public static class PropertyInfoExtensions
    {
        public static MethodInfo[] GetAccessors(PropertyInfo property)
        {
            Requires.NotNull(property, nameof(property));
            return property.GetAccessors();
        }

        public static MethodInfo[] GetAccessors(PropertyInfo property, bool nonPublic)
        {
            Requires.NotNull(property, nameof(property));
            return property.GetAccessors(nonPublic);
        }

        public static MethodInfo GetGetMethod(PropertyInfo property)
        {
            Requires.NotNull(property, nameof(property));
            return property.GetGetMethod();
        }

        public static MethodInfo GetGetMethod(PropertyInfo property, bool nonPublic)
        {
            Requires.NotNull(property, nameof(property));
            return property.GetGetMethod(nonPublic);
        }

        public static MethodInfo GetSetMethod(PropertyInfo property)
        {
            Requires.NotNull(property, nameof(property));
            return property.GetSetMethod();
        }

        public static MethodInfo GetSetMethod(PropertyInfo property, bool nonPublic)
        {
            Requires.NotNull(property, nameof(property));
            return property.GetSetMethod(nonPublic);
        }
    }
}
