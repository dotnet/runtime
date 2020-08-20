// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.SourceGeneration
{
    internal sealed partial class JsonSourceGeneratorHelper
    {
        // Simple handled types with typeinfo.
        private static readonly HashSet<Type> s_simpleTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(int),
            typeof(double),
            typeof(long),
            typeof(string),
            typeof(char),
            typeof(DateTime),
            typeof(DateTimeOffset),
        };

        // Generation namespace for source generation code.
        const string GenerationNamespace = "JsonCodeGeneration";

        // Type for key and a generated-source for value.
        public Dictionary<Type, string> Types { get; }

        // Contains types that failed to be generated.
        private HashSet<Type> _failedTypes = new HashSet<Type>();

        // Contains list of diagnostics for the code generator.
        public List<Diagnostic> Diagnostics { get; }

        public JsonSourceGeneratorHelper()
        {
            // Initialize auto properties.
            Types = new Dictionary<Type, string>();
            Diagnostics = new List<Diagnostic>();

            // Initiate diagnostic descriptors.
            InitializeDiagnosticDescriptors();
        }

        public struct GenerationClassFrame
        {
            public Type RootType;
            public Type CurrentType;
            public string ClassName;
            public StringBuilder Source;

            public PropertyInfo[] Properties;
            public FieldInfo[] Fields;

            public bool IsSuccessful; 

            public GenerationClassFrame(Type rootType, Type currentType)
            {
                RootType = rootType;
                CurrentType = currentType;
                ClassName = currentType.GetCompilableUniqueName();
                Source = new StringBuilder();
                Properties = CurrentType.GetProperties();
                Fields = CurrentType.GetFields();
                IsSuccessful = true;
            }
        }

        // Base source generation context partial class.
        public string GenerateHelperContextInfo()
        {
            return @$"
using System.Text.Json.Serialization;

namespace {GenerationNamespace}
{{
    public partial class JsonContext : JsonSerializerContext
    {{
        private static JsonContext s_instance;
        public static JsonContext Instance
        {{
            get
            {{
                if (s_instance == null)
                {{
                    s_instance = new JsonContext();
                }}

                return s_instance;
            }}
        }}
    }}
}}
            ";
        }

        // Generates metadata for type and returns if it was successful.
        private bool GenerateClassInfo(GenerationClassFrame currentFrame, HashSet<Type> seenTypes)
        {
            // Add current type to generated types.
            seenTypes.Add(currentFrame.CurrentType);

            // Try to recursively generate necessary field and property types.
            foreach (FieldInfo field in currentFrame.Fields)
            {
                if (!IsSupportedType(field.FieldType))
                {
                    Diagnostics.Add(Diagnostic.Create(_notSupported, Location.None, new string[] { currentFrame.RootType.Name, field.FieldType.Name }));
                    return false;
                }
                foreach (Type handlingType in GetTypesToGenerate(field.FieldType))
                {
                    GenerateForMembers(currentFrame, handlingType, seenTypes);
                }
            }

            foreach (PropertyInfo property in currentFrame.Properties)
            {
                if (!IsSupportedType(property.PropertyType))
                {
                    Diagnostics.Add(Diagnostic.Create(_notSupported, Location.None, new string[] { currentFrame.RootType.Name, property.PropertyType.Name }));
                    return false;
                }
                foreach (Type handlingType in GetTypesToGenerate(property.PropertyType))
                {
                    GenerateForMembers(currentFrame, handlingType, seenTypes);
                }
            }

            // Try to generate current type info now that fields and property types have been resolved.
            AddImportsToTypeClass(currentFrame);
            InitializeContextClass(currentFrame);
            InitializeTypeClass(currentFrame);
            TypeInfoGetterSetter(currentFrame);
            currentFrame.IsSuccessful &= InitializeTypeInfoProperties(currentFrame);
            currentFrame.IsSuccessful &= GenerateTypeInfoConstructor(currentFrame);
            GenerateCreateObject(currentFrame);
            GenerateSerialize(currentFrame);
            GenerateDeserialize(currentFrame);
            FinalizeTypeAndContextClasses(currentFrame);

            if (currentFrame.IsSuccessful)
            {
                Diagnostics.Add(Diagnostic.Create(_generatedTypeClass, Location.None, new string[] { currentFrame.RootType.Name, currentFrame.ClassName }));

                // Add generated typeinfo for current traversal.
                Types.Add(currentFrame.CurrentType, currentFrame.Source.ToString());
            }
            else
            {
                Diagnostics.Add(Diagnostic.Create(_failedToGenerateTypeClass, Location.None, new string[] { currentFrame.RootType.Name, currentFrame.ClassName }));

                // If not successful remove it from found types hashset and add to failed types list.
                seenTypes.Remove(currentFrame.CurrentType);
                _failedTypes.Add(currentFrame.CurrentType);
            }

            return currentFrame.IsSuccessful;
        }

        // Call recursive type generation if unseen type and check for success and cycles.
        void GenerateForMembers(GenerationClassFrame currentFrame, Type newType, HashSet<Type> seenTypes)
        {
            // If new type, recurse.
            if (IsNewType(newType, seenTypes))
            {
                bool isMemberSuccessful = GenerateClassInfo(new GenerationClassFrame(currentFrame.RootType, newType), seenTypes);
                currentFrame.IsSuccessful &= isMemberSuccessful;

                if (!isMemberSuccessful)
                {
                    Diagnostics.Add(Diagnostic.Create(_failedToAddNewTypesFromMembers, Location.None, new string[] { currentFrame.RootType.Name, currentFrame.CurrentType.Name }));
                }
            }
        }

        // Check if current type is supported to be iterated over.
        private static bool IsSupportedType(Type type)
        {
            if (type.IsIEnumerable())
            {
                // todo: Add more support to collections.
                if (!type.IsIList())
                {
                    return false;
                }
            }

            return true;
        }

        // Returns name of types traversed that can be looked up in the dictionary.
        public void GenerateClassInfo(Type type)
        {
            HashSet<Type> foundTypes = new HashSet<Type>();
            GenerateClassInfo(new GenerationClassFrame(rootType: type, currentType: type), foundTypes);
        }

        private Type[] GetTypesToGenerate(Type type)
        {
            if (type.IsArray)
            {
                return new Type[] { type.GetElementType() };
            }
            if (type.IsGenericType)
            {
                return type.GetGenericArguments();
            }

            return new Type[] { type };
        }

        private bool IsNewType(Type type, HashSet<Type> foundTypes) => (
            !Types.ContainsKey(type) &&
            !foundTypes.Contains(type) &&
            !s_simpleTypes.Contains(type));

        private void AddImportsToTypeClass(GenerationClassFrame currentFrame)
        {
            HashSet<string> imports = new HashSet<string>();

            // Add base imports.
            imports.Add("System");
            imports.Add("System.Text.Json");
            imports.Add("System.Text.Json.Serialization");
            imports.Add("System.Text.Json.Serialization.Metadata");

            // Add imports to root type.
            imports.Add(currentFrame.CurrentType.GetFullNamespace());

            foreach (PropertyInfo property in currentFrame.Properties)
            {
                foreach (Type handlingType in GetTypesToGenerate(property.PropertyType))
                {
                    imports.Add(property.PropertyType.GetFullNamespace());
                    imports.Add(handlingType.GetFullNamespace());
                }
            }
            foreach (FieldInfo field in currentFrame.Fields)
            {
                foreach (Type handlingType in GetTypesToGenerate(field.FieldType))
                {
                    imports.Add(field.FieldType.GetFullNamespace());
                    imports.Add(handlingType.GetFullNamespace());
                }
            }

            foreach (string import in imports)
            {
                currentFrame.Source.Append($@"
using {import};");
            }
        }

        // Includes necessary imports, namespace decl and initializes class.
        private void InitializeContextClass(GenerationClassFrame currentFrame)
        {
            currentFrame.Source.Append($@"

namespace {GenerationNamespace}
{{
    public partial class JsonContext : JsonSerializerContext
    {{
        private {currentFrame.ClassName}TypeInfo _{currentFrame.ClassName};
        public JsonTypeInfo<{currentFrame.CurrentType.FullName}> {currentFrame.ClassName}
        {{
            get
            {{
                if (_{currentFrame.ClassName} == null)
                {{
                    _{currentFrame.ClassName} = new {currentFrame.ClassName}TypeInfo(this);
                }}

                return _{currentFrame.ClassName}.TypeInfo;
            }}
        }}
        ");
        }

        private void InitializeTypeClass(GenerationClassFrame currentFrame) {
            currentFrame.Source.Append($@"
        private class {currentFrame.ClassName}TypeInfo 
        {{
        ");
        }

        private void TypeInfoGetterSetter(GenerationClassFrame currentFrame)
        {
            currentFrame.Source.Append($@"
            public JsonTypeInfo<{currentFrame.CurrentType.FullName}> TypeInfo {{ get; private set; }}
            ");
        }

        private bool InitializeTypeInfoProperties(GenerationClassFrame currentFrame)
        {
            Type propertyType;
            Type genericType;
            string typeName;
            string propertyName;

            foreach (PropertyInfo property in currentFrame.Properties)
            {
                // Find type and property name to use for property definition.
                propertyType = property.PropertyType;
                propertyName = property.Name;
                typeName = propertyType.FullName;

                // Check if IEnumerable.
                if (propertyType.IsIEnumerable())
                {
                    genericType = GetTypesToGenerate(propertyType).First();
                    if (propertyType.IsIList())
                    {
                        typeName = $"List<{genericType.FullName}>";
                    }
                    else
                    {
                        // todo: Add support for rest of the IEnumerables.
                        return false;
                    }
                }

                currentFrame.Source.Append($@"
            private JsonPropertyInfo<{typeName}> _property_{propertyName};
                ");
            }

            return true;
        }

        private bool GenerateTypeInfoConstructor(GenerationClassFrame currentFrame)
        {
            Type currentType = currentFrame.CurrentType;

            currentFrame.Source.Append($@"
            public {currentFrame.ClassName}TypeInfo(JsonContext context)
            {{
                var typeInfo = new JsonObjectInfo<{currentType.FullName}>(CreateObjectFunc, SerializeFunc, DeserializeFunc, context.GetOptions());
            ");

            Type propertyType;
            Type genericType;
            string typeClassInfoCall;
            foreach (PropertyInfo property in currentFrame.Properties)
            {
                propertyType = property.PropertyType;
                // Default classtype for values.
                typeClassInfoCall = $"context.{propertyType.GetCompilableUniqueName()}";

                // Check if IEnumerable.
                if (propertyType.IsIEnumerable())
                {
                    genericType = GetTypesToGenerate(propertyType).First();

                    if (propertyType.IsIList())
                    {
                        typeClassInfoCall = $"KnownCollectionTypeInfos<{genericType.FullName}>.GetList(context.{genericType.GetCompilableUniqueName()}, context)";
                    }
                    else
                    {
                        // todo: Add support for rest of the IEnumerables.
                        return false;
                    }
                }

                currentFrame.Source.Append($@"
                _property_{property.Name} = typeInfo.AddProperty(nameof({currentType.FullName}.{property.Name}),
                    (obj) => {{ return (({currentType.FullName})obj).{property.Name}; }},
                    (obj, value) => {{ (({currentType.FullName})obj).{property.Name} = value; }},
                    {typeClassInfoCall});
                ");
            }

            // Finalize constructor.
            currentFrame.Source.Append($@"
                typeInfo.CompleteInitialization();
                TypeInfo = typeInfo;
            }}
            ");

            return true;
        }

        private void GenerateCreateObject(GenerationClassFrame currentFrame)
        {
            currentFrame.Source.Append($@"
            private object CreateObjectFunc()
            {{
                return new {currentFrame.CurrentType.FullName}();
            }}
            ");
        }

        private void GenerateSerialize(GenerationClassFrame currentFrame)
        {
            // Start function.
            currentFrame.Source.Append($@"
            private void SerializeFunc(Utf8JsonWriter writer, object value, ref WriteStack writeStack, JsonSerializerOptions options)
            {{");

            // Create base object.
            currentFrame.Source.Append($@"
                {currentFrame.CurrentType.FullName} obj = ({currentFrame.CurrentType.FullName})value;
            ");

            foreach (PropertyInfo property in currentFrame.Properties)
            {
                currentFrame.Source.Append($@"
                _property_{property.Name}.WriteValue(obj.{property.Name}, ref writeStack, writer);");
            }

            // End function.
            currentFrame.Source.Append($@"
            }}
            ");
        }

        private void GenerateDeserialize(GenerationClassFrame currentFrame)
        {
            // Start deserialize function.
            currentFrame.Source.Append($@"
            private {currentFrame.CurrentType.FullName} DeserializeFunc(ref Utf8JsonReader reader, ref ReadStack readStack, JsonSerializerOptions options)
            {{
            ");

            // Create helper function to check for property name.
            currentFrame.Source.Append($@"
                bool ReadPropertyName(ref Utf8JsonReader reader)
                {{
                    return reader.Read() && reader.TokenType == JsonTokenType.PropertyName;
                }}
            ");

            // Start loop to read properties.
            currentFrame.Source.Append($@"
                ReadOnlySpan<byte> propertyName;
                {currentFrame.CurrentType.FullName} obj = new {currentFrame.CurrentType.FullName}();

                while(ReadPropertyName(ref reader))
                {{
                    propertyName = reader.ValueSpan;
            ");

            // Read and set each property.
            foreach ((PropertyInfo property, int i) in currentFrame.Properties.Select((p, i) => (p, i)))
            {
                currentFrame.Source.Append($@"
                    {((i == 0) ? "" : "else ")}if (propertyName.SequenceEqual(_property_{property.Name}.NameAsUtf8Bytes))
                    {{
                        reader.Read();
                        _property_{property.Name}.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    }}");
            }

            // Base condition for unhandled properties.
            if (currentFrame.Properties.Length > 0)
            {
                currentFrame.Source.Append($@"
                    else
                    {{
                        reader.Read();
                    }}");
            }
            else
            {
                currentFrame.Source.Append($@"
                    reader.Read();");
            }

            // Finish property reading loops.
            currentFrame.Source.Append($@"
                }}
            ");

            // Verify the final received token and return object.
            currentFrame.Source.Append($@"
                if (reader.TokenType != JsonTokenType.EndObject)
                {{
                    throw new JsonException(""todo"");
                }}
                return obj;
            ");

            // End deserialize function.
            currentFrame.Source.Append($@"
            }}
            ");
        }

        private void FinalizeTypeAndContextClasses(GenerationClassFrame currentFrame)
        {
            currentFrame.Source.Append($@"
        }} // End of typeinfo class.
    }} // End of context class.
}} // End of namespace.
            ");
        }
    }
}
