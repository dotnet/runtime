// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Net.Http.Json.Functional.Tests
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<Person> _Person;
        public JsonTypeInfo<Person> Person
        {
            get
            {
                if (_Person == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(Person))) != null)
                    {
                        _Person = JsonMetadataServices.CreateValueInfo<Person>(Options, customConverter);
                    }
                    else
                    {
                        JsonTypeInfo<Person> objectInfo = JsonMetadataServices.CreateObjectInfo<Person>(
                            Options,
                            createObjectFunc: static () => new Person(),
                            PersonPropInitFunc,
                            default,
                            serializeFunc: null);

                        _Person = objectInfo;
                    }
                }

                return _Person;
            }
        }
        private static JsonPropertyInfo[] PersonPropInitFunc(JsonSerializerContext context)
        {
            JsonContext jsonContext = (JsonContext)context;
            JsonSerializerOptions options = context.Options;

            JsonPropertyInfo[] properties = new JsonPropertyInfo[4];

            properties[0] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(Person),
                propertyTypeInfo: jsonContext.Int32,
                converter: null,
                getter: static (obj) => { return ((Person)obj).Age; },
                setter: static (obj, value) => { ((Person)obj).Age = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Tests.Person.Age),
                jsonPropertyName: null);

            properties[1] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(Person),
                propertyTypeInfo: jsonContext.String,
                converter: null,
                getter: static (obj) => { return ((Person)obj).Name; },
                setter: static (obj, value) => { ((Person)obj).Name = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Tests.Person.Name),
                jsonPropertyName: null);

            properties[2] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(Person),
                propertyTypeInfo: jsonContext.Person,
                converter: null,
                getter: static (obj) => { return ((Person)obj).Parent; },
                setter: static (obj, value) => { ((Person)obj).Parent = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Tests.Person.Parent),
                jsonPropertyName: null);

            properties[3] = JsonMetadataServices.CreatePropertyInfo(
                options,
                isProperty: true,
                declaringType: typeof(Person),
                propertyTypeInfo: jsonContext.String,
                converter: null,
                getter: static (obj) => { return ((Person)obj).PlaceOfBirth; },
                setter: static (obj, value) => { ((Person)obj).PlaceOfBirth = value; },
                ignoreCondition: default,
                numberHandling: default,
                propertyName: nameof(Tests.Person.PlaceOfBirth),
                jsonPropertyName: null);

            return properties;
        }
    }
}
