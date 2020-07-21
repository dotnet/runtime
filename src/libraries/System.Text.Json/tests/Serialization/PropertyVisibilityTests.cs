// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Concurrent;
using Xunit;
using System.Numerics;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class PropertyVisibilityTests
    {
        [Fact]
        public static void Serialize_NewSlotPublicField()
        {
            // Serialize
            var obj = new ClassWithNewSlotField();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{""MyString"":""NewDefaultValue""}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithNewSlotField>(json);

            Assert.Equal("NewValue", ((ClassWithNewSlotField)obj).MyString);
            Assert.Equal("DefaultValue", ((ClassWithInternalField)obj).MyString);
        }

        [Fact]
        public static void Serialize_NewSlotPublicProperty()
        {
            // Serialize
            var obj = new ClassWithNewSlotProperty();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{""MyString"":""NewDefaultValue""}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithNewSlotProperty>(json);

            Assert.Equal("NewValue", ((ClassWithNewSlotProperty)obj).MyString);
            Assert.Equal("DefaultValue", ((ClassWithInternalProperty)obj).MyString);
        }

        [Fact]
        public static void Serialize_BasePublicProperty_ConflictWithDerivedPrivate()
        {
            // Serialize
            var obj = new ClassWithNewSlotInternalProperty();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{""MyString"":""DefaultValue""}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithNewSlotInternalProperty>(json);

            Assert.Equal("NewValue", ((ClassWithPublicProperty)obj).MyString);
            Assert.Equal("NewDefaultValue", ((ClassWithNewSlotInternalProperty)obj).MyString);
        }

        [Fact]
        public static void Serialize_PublicProperty_ConflictWithPrivateDueAttributes()
        {
            // Serialize
            var obj = new ClassWithPropertyNamingConflict();

            // Newtonsoft.Json throws JsonSerializationException here because
            // non-public properties are included when [JsonProperty] is placed on them.
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{""MyString"":""DefaultValue""}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";

            // Newtonsoft.Json throws JsonSerializationException here because
            // non-public properties are included when [JsonProperty] is placed on them.
            obj = JsonSerializer.Deserialize<ClassWithPropertyNamingConflict>(json);

            Assert.Equal("NewValue", obj.MyString);
            Assert.Equal("ConflictingValue", obj.ConflictingString);
        }

        [Fact]
        public static void Serialize_PublicProperty_ConflictWithPrivateDuePolicy()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassWithPropertyPolicyConflict();
            string json = JsonSerializer.Serialize(obj, options);

            Assert.Equal(@"{""myString"":""DefaultValue""}", json);

            // Deserialize
            json = @"{""myString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithPropertyPolicyConflict>(json, options);

            Assert.Equal("NewValue", obj.MyString);
            Assert.Equal("ConflictingValue", obj.myString);
        }

        [Fact]
        public static void Serialize_NewSlotPublicProperty_ConflictWithBasePublicProperty()
        {
            // Serialize
            var obj = new ClassWithNewSlotDecimalProperty();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{""MyNumeric"":1.5}", json);

            // Deserialize
            json = @"{""MyNumeric"":2.5}";
            obj = JsonSerializer.Deserialize<ClassWithNewSlotDecimalProperty>(json);

            Assert.Equal(2.5M, obj.MyNumeric);
        }

        [Fact]
        public static void Serialize_NewSlotPublicField_ConflictWithBasePublicProperty()
        {
            // Serialize
            var obj = new ClassWithNewSlotDecimalField();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{""MyNumeric"":1.5}", json);

            // Deserialize
            json = @"{""MyNumeric"":2.5}";
            obj = JsonSerializer.Deserialize<ClassWithNewSlotDecimalField>(json);

            Assert.Equal(2.5M, obj.MyNumeric);
        }

        [Fact]
        public static void Serialize_NewSlotPublicField_SpecifiedJsonPropertyName()
        {
            // Serialize
            var obj = new ClassWithNewSlotAttributedDecimalField();
            string json = JsonSerializer.Serialize(obj);

            Assert.Contains(@"""MyNewNumeric"":1.5", json);
            Assert.Contains(@"""MyNumeric"":1", json);

            // Deserialize
            json = @"{""MyNewNumeric"":2.5,""MyNumeric"":4}";
            obj = JsonSerializer.Deserialize<ClassWithNewSlotAttributedDecimalField>(json);

            Assert.Equal(4, ((ClassWithHiddenByNewSlotIntProperty)obj).MyNumeric);
            Assert.Equal(2.5M, ((ClassWithNewSlotAttributedDecimalField)obj).MyNumeric);
        }

        [Fact]
        public static void Serialize_NewSlotPublicProperty_SpecifiedJsonPropertyName()
        {
            // Serialize
            var obj = new ClassWithNewSlotAttributedDecimalProperty();
            string json = JsonSerializer.Serialize(obj);

            Assert.Contains(@"""MyNewNumeric"":1.5", json);
            Assert.Contains(@"""MyNumeric"":1", json);

            // Deserialize
            json = @"{""MyNewNumeric"":2.5,""MyNumeric"":4}";
            obj = JsonSerializer.Deserialize<ClassWithNewSlotAttributedDecimalProperty>(json);

            Assert.Equal(4, ((ClassWithHiddenByNewSlotIntProperty)obj).MyNumeric);
            Assert.Equal(2.5M, ((ClassWithNewSlotAttributedDecimalProperty)obj).MyNumeric);
        }

        [Fact]
        public static void Ignore_NonPublicProperty()
        {
            // Serialize
            var obj = new ClassWithInternalProperty();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithInternalProperty>(json);

            Assert.Equal("DefaultValue", obj.MyString);
        }

        [Fact]
        public static void Ignore_NewSlotPublicFieldIgnored()
        {
            // Serialize
            var obj = new ClassWithIgnoredNewSlotField();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithIgnoredNewSlotField>(json);

            Assert.Equal("NewDefaultValue", ((ClassWithIgnoredNewSlotField)obj).MyString);
            Assert.Equal("DefaultValue", ((ClassWithInternalField)obj).MyString);
        }

        [Fact]
        public static void Ignore_NewSlotPublicPropertyIgnored()
        {
            // Serialize
            var obj = new ClassWithIgnoredNewSlotProperty();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithIgnoredNewSlotProperty>(json);

            Assert.Equal("NewDefaultValue", ((ClassWithIgnoredNewSlotProperty)obj).MyString);
            Assert.Equal("DefaultValue", ((ClassWithInternalProperty)obj).MyString);
        }

        [Fact]
        public static void Ignore_BasePublicPropertyIgnored_ConflictWithDerivedPrivate()
        {
            // Serialize
            var obj = new ClassWithIgnoredPublicPropertyAndNewSlotPrivate();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithIgnoredPublicPropertyAndNewSlotPrivate>(json);

            Assert.Equal("DefaultValue", ((ClassWithIgnoredPublicProperty)obj).MyString);
            Assert.Equal("NewDefaultValue", ((ClassWithIgnoredPublicPropertyAndNewSlotPrivate)obj).MyString);
        }

        [Fact]
        public static void Ignore_PublicProperty_ConflictWithPrivateDueAttributes()
        {
            // Serialize
            var obj = new ClassWithIgnoredPropertyNamingConflictPrivate();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{}", json);

            // Newtonsoft.Json has the following output because non-public properties are included when [JsonProperty] is placed on them.
            // {"MyString":"ConflictingValue"}

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithIgnoredPropertyNamingConflictPrivate>(json);

            Assert.Equal("DefaultValue", obj.MyString);
            Assert.Equal("ConflictingValue", obj.ConflictingString);

            // The output for Newtonsoft.Json is:
            // obj.ConflictingString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void Ignore_PublicProperty_ConflictWithPrivateDuePolicy()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassWithIgnoredPropertyPolicyConflictPrivate();
            string json = JsonSerializer.Serialize(obj, options);

            Assert.Equal(@"{}", json);

            // Deserialize
            json = @"{""myString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithIgnoredPropertyPolicyConflictPrivate>(json, options);

            Assert.Equal("DefaultValue", obj.MyString);
            Assert.Equal("ConflictingValue", obj.myString);
        }

        [Fact]
        public static void Ignore_PublicProperty_ConflictWithPublicDueAttributes()
        {
            // Serialize
            var obj = new ClassWithIgnoredPropertyNamingConflictPublic();
            string json = JsonSerializer.Serialize(obj);

            Assert.Equal(@"{""MyString"":""ConflictingValue""}", json);

            // Deserialize
            json = @"{""MyString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithIgnoredPropertyNamingConflictPublic>(json);

            Assert.Equal("DefaultValue", obj.MyString);
            Assert.Equal("NewValue", obj.ConflictingString);
        }

        [Fact]
        public static void Ignore_PublicProperty_ConflictWithPublicDuePolicy()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassWithIgnoredPropertyPolicyConflictPublic();
            string json = JsonSerializer.Serialize(obj, options);

            Assert.Equal(@"{""myString"":""ConflictingValue""}", json);

            // Deserialize
            json = @"{""myString"":""NewValue""}";
            obj = JsonSerializer.Deserialize<ClassWithIgnoredPropertyPolicyConflictPublic>(json, options);

            Assert.Equal("DefaultValue", obj.MyString);
            Assert.Equal("NewValue", obj.myString);
        }

        [Fact]
        public static void Throw_PublicProperty_ConflictDueAttributes()
        {
            // Serialize
            var obj = new ClassWithPropertyNamingConflictWhichThrows();
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj));

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassWithPropertyNamingConflictWhichThrows>(json));
        }

        [Fact]
        public static void Throw_PublicPropertyAndField_ConflictDueAttributes()
        {
            // Serialize
            var obj = new ClassWithPropertyFieldNamingConflictWhichThrows();
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj));

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassWithPropertyFieldNamingConflictWhichThrows>(json));
        }

        [Fact]
        public static void Throw_PublicProperty_ConflictDueAttributes_SingleInheritance()
        {
            // Serialize
            var obj = new ClassInheritedWithPropertyNamingConflictWhichThrows();
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj));

            // The output for Newtonsoft.Json is:
            // {"MyString":"ConflictingValue"}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassInheritedWithPropertyNamingConflictWhichThrows>(json));

            // The output for Newtonsoft.Json is:
            // obj.ConflictingString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void Throw_PublicPropertyAndField_ConflictDueAttributes_SingleInheritance()
        {
            // Serialize
            var obj = new ClassInheritedWithPropertyFieldNamingConflictWhichThrows();
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj));

            // The output for Newtonsoft.Json is:
            // {"MyString":"ConflictingValue"}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassInheritedWithPropertyFieldNamingConflictWhichThrows>(json));

            // The output for Newtonsoft.Json is:
            // obj.ConflictingString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void Throw_PublicProperty_ConflictDueAttributes_DoubleInheritance()
        {
            // Serialize
            var obj = new ClassTwiceInheritedWithPropertyNamingConflictWhichThrows();
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj));

            // The output for Newtonsoft.Json is:
            // {"MyString":"ConflictingValue"}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassTwiceInheritedWithPropertyNamingConflictWhichThrows>(json));

            // The output for Newtonsoft.Json is:
            // obj.ConflictingString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void Throw_PublicPropertyAndField_ConflictDueAttributes_DoubleInheritance()
        {
            // Serialize
            var obj = new ClassTwiceInheritedWithPropertyFieldNamingConflictWhichThrows();
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj));

            // The output for Newtonsoft.Json is:
            // {"MyString":"ConflictingValue"}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassTwiceInheritedWithPropertyFieldNamingConflictWhichThrows>(json));

            // The output for Newtonsoft.Json is:
            // obj.ConflictingString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void Throw_PublicProperty_ConflictDuePolicy()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassWithPropertyPolicyConflictWhichThrows();
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj, options));

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassWithPropertyPolicyConflictWhichThrows>(json, options));
        }

        [Fact]
        public static void Throw_PublicPropertyAndField_ConflictDuePolicy()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassWithPropertyFieldPolicyConflictWhichThrows();
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj, options));

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassWithPropertyFieldPolicyConflictWhichThrows>(json, options));
        }

        [Fact]
        public static void Throw_PublicProperty_ConflictDuePolicy_SingleInheritance()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassInheritedWithPropertyPolicyConflictWhichThrows();

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj, options));

            // The output for Newtonsoft.Json is:
            // {"myString":"ConflictingValue"}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassInheritedWithPropertyPolicyConflictWhichThrows>(json, options));

            // The output for Newtonsoft.Json is:
            // obj.myString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void Throw_PublicPropertyAndField_ConflictDuePolicy_SingleInheritance()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassInheritedWithPropertyFieldPolicyConflictWhichThrows();

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj, options));

            // The output for Newtonsoft.Json is:
            // {"myString":"ConflictingValue"}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassInheritedWithPropertyFieldPolicyConflictWhichThrows>(json, options));

            // The output for Newtonsoft.Json is:
            // obj.myString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void Throw_PublicProperty_ConflictDuePolicy_DobuleInheritance()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassTwiceInheritedWithPropertyPolicyConflictWhichThrows();

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj, options));

            // The output for Newtonsoft.Json is:
            // {"myString":"ConflictingValue"}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassTwiceInheritedWithPropertyPolicyConflictWhichThrows>(json, options));

            // The output for Newtonsoft.Json is:
            // obj.myString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void Throw_PublicPropertyAndField_ConflictDuePolicy_DobuleInheritance()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Serialize
            var obj = new ClassTwiceInheritedWithPropertyFieldPolicyConflictWhichThrows();

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(obj, options));

            // The output for Newtonsoft.Json is:
            // {"myString":"ConflictingValue"}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.

            // Deserialize
            string json = @"{""MyString"":""NewValue""}";

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Deserialize<ClassTwiceInheritedWithPropertyFieldPolicyConflictWhichThrows>(json, options));

            // The output for Newtonsoft.Json is:
            // obj.myString = "NewValue"
            // obj.MyString still equals "DefaultValue"
        }

        [Fact]
        public static void HiddenPropertiesIgnored_WhenOverridesIgnored()
        {
            string serialized = JsonSerializer.Serialize(new DerivedClass_With_IgnoredOverride());
            Assert.Equal(@"{}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_WithVisibleProperty_Of_DerivedClass_With_IgnoredOverride());
            Assert.Equal(@"{""MyProp"":false}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_With_IgnoredOverride_And_ConflictingPropertyName());
            Assert.Equal(@"{""MyProp"":null}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_With_Ignored_NewProperty());
            Assert.Equal(@"{""MyProp"":false}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_WithConflictingNewMember());
            Assert.Equal(@"{""MyProp"":false}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_WithConflictingNewMember_Of_DifferentType());
            Assert.Equal(@"{""MyProp"":0}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_With_Ignored_ConflictingNewMember());
            Assert.Equal(@"{""MyProp"":false}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_With_Ignored_ConflictingNewMember_Of_DifferentType());
            Assert.Equal(@"{""MyProp"":false}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_With_NewProperty_And_ConflictingPropertyName());
            Assert.Equal(@"{""MyProp"":null}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_With_Ignored_NewProperty_Of_DifferentType());
            Assert.Equal(@"{""MyProp"":false}", serialized);

            serialized = JsonSerializer.Serialize(new DerivedClass_With_Ignored_NewProperty_Of_DifferentType_And_ConflictingPropertyName());
            Assert.Equal(@"{""MyProp"":null}", serialized);

            serialized = JsonSerializer.Serialize(new FurtherDerivedClass_With_ConflictingPropertyName());
            Assert.Equal(@"{""MyProp"":null}", serialized);

            // Here we differ from Newtonsoft.Json, where the output would be
            // {"MyProp":null}
            // Conflicts at different type-hierarchy levels that are not caused by
            // deriving or the new keyword are allowed. Properties on more derived types win.
            // This is invalid in System.Text.Json.
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new DerivedClass_WithConflictingPropertyName()));

            serialized = JsonSerializer.Serialize(new FurtherDerivedClass_With_IgnoredOverride());
            Assert.Equal(@"{""MyProp"":null}", serialized);
        }

        public class ClassWithInternalField
        {
            internal string MyString = "DefaultValue";
        }

        public class ClassWithNewSlotField : ClassWithInternalField
        {
            [JsonInclude]
            public new string MyString = "NewDefaultValue";
        }

        public class ClassWithInternalProperty
        {
            internal string MyString { get; set; } = "DefaultValue";
        }

        public class ClassWithNewSlotProperty : ClassWithInternalProperty
        {
            public new string MyString { get; set; } = "NewDefaultValue";
        }

        public class ClassWithPublicProperty
        {
            public string MyString { get; set; } = "DefaultValue";
        }

        public class ClassWithNewSlotInternalProperty : ClassWithPublicProperty
        {
            internal new string MyString { get; set; } = "NewDefaultValue";
        }

        public class ClassWithPropertyNamingConflict
        {
            public string MyString { get; set; } = "DefaultValue";

            [JsonPropertyName(nameof(MyString))]
            internal string ConflictingString { get; set; } = "ConflictingValue";
        }

        public class ClassWithPropertyNamingConflictWhichThrows
        {
            public string MyString { get; set; } = "DefaultValue";

            [JsonPropertyName(nameof(MyString))]
            public string ConflictingString { get; set; } = "ConflictingValue";
        }

        public class ClassWithPropertyFieldNamingConflictWhichThrows
        {
            public string MyString { get; set; } = "DefaultValue";

            [JsonInclude]
            [JsonPropertyName(nameof(MyString))]
            public string ConflictingString = "ConflictingValue";
        }

        public class ClassInheritedWithPropertyNamingConflictWhichThrows : ClassWithPublicProperty
        {
            [JsonPropertyName(nameof(MyString))]
            public string ConflictingString { get; set; } = "ConflictingValue";
        }

        public class ClassInheritedWithPropertyFieldNamingConflictWhichThrows : ClassWithPublicProperty
        {
            [JsonInclude]
            [JsonPropertyName(nameof(MyString))]
            public string ConflictingString = "ConflictingValue";
        }

        public class ClassTwiceInheritedWithPropertyNamingConflictWhichThrowsDummy : ClassWithPublicProperty
        {
        }

        public class ClassTwiceInheritedWithPropertyNamingConflictWhichThrows : ClassTwiceInheritedWithPropertyNamingConflictWhichThrowsDummy
        {
            [JsonPropertyName(nameof(MyString))]
            public string ConflictingString { get; set; } = "ConflictingValue";
        }

        public class ClassTwiceInheritedWithPropertyFieldNamingConflictWhichThrows : ClassTwiceInheritedWithPropertyNamingConflictWhichThrowsDummy
        {
            [JsonInclude]
            [JsonPropertyName(nameof(MyString))]
            public string ConflictingString = "ConflictingValue";
        }

        public class ClassWithPropertyPolicyConflict
        {
            public string MyString { get; set; } = "DefaultValue";

            internal string myString { get; set; } = "ConflictingValue";
        }

        public class ClassWithPropertyPolicyConflictWhichThrows
        {
            public string MyString { get; set; } = "DefaultValue";

            public string myString { get; set; } = "ConflictingValue";
        }

        public class ClassWithPropertyFieldPolicyConflictWhichThrows
        {
            public string MyString { get; set; } = "DefaultValue";

            [JsonInclude]
            public string myString = "ConflictingValue";
        }

        public class ClassInheritedWithPropertyPolicyConflictWhichThrows : ClassWithPublicProperty
        {
            public string myString { get; set; } = "ConflictingValue";
        }

        public class ClassInheritedWithPropertyFieldPolicyConflictWhichThrows : ClassWithPublicProperty
        {
            [JsonInclude]
            public string myString = "ConflictingValue";
        }

        public class ClassInheritedWithPropertyPolicyConflictWhichThrowsDummy : ClassWithPublicProperty
        {
        }

        public class ClassTwiceInheritedWithPropertyPolicyConflictWhichThrows : ClassInheritedWithPropertyPolicyConflictWhichThrowsDummy
        {
            public string myString { get; set; } = "ConflictingValue";
        }

        public class ClassTwiceInheritedWithPropertyFieldPolicyConflictWhichThrows : ClassInheritedWithPropertyPolicyConflictWhichThrowsDummy
        {
            [JsonInclude]
            public string myString { get; set; } = "ConflictingValue";
        }

        public class ClassWithIgnoredNewSlotField : ClassWithInternalField
        {
            [JsonIgnore]
            public new string MyString = "NewDefaultValue";
        }

        public class ClassWithIgnoredNewSlotProperty : ClassWithInternalProperty
        {
            [JsonIgnore]
            public new string MyString { get; set; } = "NewDefaultValue";
        }

        public class ClassWithIgnoredPublicProperty
        {
            [JsonIgnore]
            public string MyString { get; set; } = "DefaultValue";
        }

        public class ClassWithIgnoredPublicPropertyAndNewSlotPrivate : ClassWithIgnoredPublicProperty
        {
            internal new string MyString { get; set; } = "NewDefaultValue";
        }

        public class ClassWithIgnoredPropertyNamingConflictPrivate
        {
            [JsonIgnore]
            public string MyString { get; set; } = "DefaultValue";

            [JsonPropertyName(nameof(MyString))]
            internal string ConflictingString { get; set; } = "ConflictingValue";
        }

        public class ClassWithIgnoredPropertyPolicyConflictPrivate
        {
            [JsonIgnore]
            public string MyString { get; set; } = "DefaultValue";

            internal string myString { get; set; } = "ConflictingValue";
        }

        public class ClassWithIgnoredPropertyNamingConflictPublic
        {
            [JsonIgnore]
            public string MyString { get; set; } = "DefaultValue";

            [JsonPropertyName(nameof(MyString))]
            public string ConflictingString { get; set; } = "ConflictingValue";
        }

        public class ClassWithIgnoredPropertyPolicyConflictPublic
        {
            [JsonIgnore]
            public string MyString { get; set; } = "DefaultValue";

            public string myString { get; set; } = "ConflictingValue";
        }

        public class ClassWithHiddenByNewSlotIntProperty
        {
            public int MyNumeric { get; set; } = 1;
        }

        public class ClassWithNewSlotDecimalField : ClassWithHiddenByNewSlotIntProperty
        {
            [JsonInclude]
            public new decimal MyNumeric = 1.5M;
        }

        public class ClassWithNewSlotDecimalProperty : ClassWithHiddenByNewSlotIntProperty
        {
            public new decimal MyNumeric { get; set; } = 1.5M;
        }

        public class ClassWithNewSlotAttributedDecimalField : ClassWithHiddenByNewSlotIntProperty
        {
            [JsonInclude]
            [JsonPropertyName("MyNewNumeric")]
            public new decimal MyNumeric = 1.5M;
        }

        public class ClassWithNewSlotAttributedDecimalProperty : ClassWithHiddenByNewSlotIntProperty
        {
            [JsonPropertyName("MyNewNumeric")]
            public new decimal MyNumeric { get; set; } = 1.5M;
        }

        private class Class_With_VirtualProperty
        {
            public virtual bool MyProp { get; set; }
        }

        private class DerivedClass_With_IgnoredOverride : Class_With_VirtualProperty
        {
            [JsonIgnore]
            public override bool MyProp { get; set; }
        }

        private class DerivedClass_WithVisibleProperty_Of_DerivedClass_With_IgnoredOverride : DerivedClass_With_IgnoredOverride
        {
            public override bool MyProp { get; set; }
        }

        private class DerivedClass_With_IgnoredOverride_And_ConflictingPropertyName : Class_With_VirtualProperty
        {
            [JsonPropertyName("MyProp")]
            public string MyString { get; set; }

            [JsonIgnore]
            public override bool MyProp { get; set; }
        }

        private class Class_With_Property
        {
            public bool MyProp { get; set; }
        }

        private class DerivedClass_With_Ignored_NewProperty : Class_With_Property
        {
            [JsonIgnore]
            public new bool MyProp { get; set; }
        }

        private class DerivedClass_With_NewProperty_And_ConflictingPropertyName : Class_With_Property
        {
            [JsonPropertyName("MyProp")]
            public string MyString { get; set; }

            [JsonIgnore]
            public new bool MyProp { get; set; }
        }

        private class DerivedClass_With_Ignored_NewProperty_Of_DifferentType : Class_With_Property
        {
            [JsonIgnore]
            public new int MyProp { get; set; }
        }

        private class DerivedClass_With_Ignored_NewProperty_Of_DifferentType_And_ConflictingPropertyName : Class_With_Property
        {
            [JsonPropertyName("MyProp")]
            public string MyString { get; set; }

            [JsonIgnore]
            public new int MyProp { get; set; }
        }

        private class DerivedClass_WithIgnoredOverride : Class_With_VirtualProperty
        {
            [JsonIgnore]
            public override bool MyProp { get; set; }
        }

        private class DerivedClass_WithConflictingNewMember : Class_With_VirtualProperty
        {
            public new bool MyProp { get; set; }
        }

        private class DerivedClass_WithConflictingNewMember_Of_DifferentType : Class_With_VirtualProperty
        {
            public new int MyProp { get; set; }
        }

        private class DerivedClass_With_Ignored_ConflictingNewMember : Class_With_VirtualProperty
        {
            [JsonIgnore]
            public new bool MyProp { get; set; }
        }

        private class DerivedClass_With_Ignored_ConflictingNewMember_Of_DifferentType : Class_With_VirtualProperty
        {
            [JsonIgnore]
            public new int MyProp { get; set; }
        }

        private class FurtherDerivedClass_With_ConflictingPropertyName : DerivedClass_WithIgnoredOverride
        {
            [JsonPropertyName("MyProp")]
            public string MyString { get; set; }
        }

        private class DerivedClass_WithConflictingPropertyName : Class_With_VirtualProperty
        {
            [JsonPropertyName("MyProp")]
            public string MyString { get; set; }
        }

        private class FurtherDerivedClass_With_IgnoredOverride : DerivedClass_WithConflictingPropertyName
        {
            [JsonIgnore]
            public override bool MyProp { get; set; }
        }

        [Fact]
        public static void IgnoreReadOnlyProperties()
        {
            var options = new JsonSerializerOptions();
            options.IgnoreReadOnlyProperties = true;

            var obj = new ClassWithNoSetter();

            string json = JsonSerializer.Serialize(obj, options);

            // Collections are always serialized unless they have [JsonIgnore].
            Assert.Equal(@"{""MyInts"":[1,2]}", json);
        }

        [Fact]
        public static void IgnoreReadOnlyFields()
        {
            var options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.IgnoreReadOnlyFields = true;

            var obj = new ClassWithReadOnlyFields();

            string json = JsonSerializer.Serialize(obj, options);

            // Collections are always serialized unless they have [JsonIgnore].
            Assert.Equal(@"{""MyInts"":[1,2]}", json);
        }

        [Fact]
        public static void NoSetter()
        {
            var obj = new ClassWithNoSetter();

            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""MyString"":""DefaultValue""", json);
            Assert.Contains(@"""MyInts"":[1,2]", json);

            obj = JsonSerializer.Deserialize<ClassWithNoSetter>(@"{""MyString"":""IgnoreMe"",""MyInts"":[0]}");
            Assert.Equal("DefaultValue", obj.MyString);
            Assert.Equal(2, obj.MyInts.Length);
        }

        [Fact]
        public static void NoGetter()
        {
            ClassWithNoGetter objWithNoGetter = JsonSerializer.Deserialize<ClassWithNoGetter>(
                @"{""MyString"":""Hello"",""MyIntArray"":[0],""MyIntList"":[0]}");

            Assert.Equal("Hello", objWithNoGetter.GetMyString());

            // Currently we don't support setters without getters.
            Assert.Equal(0, objWithNoGetter.GetMyIntArray().Length);
            Assert.Equal(0, objWithNoGetter.GetMyIntList().Count);
        }

        [Fact]
        public static void PrivateGetter()
        {
            var obj = new ClassWithPrivateSetterAndGetter();
            obj.SetMyString("Hello");

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{}", json);
        }

        [Fact]
        public static void PrivateSetter()
        {
            string json = @"{""MyString"":""Hello""}";

            ClassWithPrivateSetterAndGetter objCopy = JsonSerializer.Deserialize<ClassWithPrivateSetterAndGetter>(json);
            Assert.Null(objCopy.GetMyString());
        }

        [Fact]
        public static void PrivateSetterPublicGetter()
        {
            // https://github.com/dotnet/runtime/issues/29503
            ClassWithPublicGetterAndPrivateSetter obj
                = JsonSerializer.Deserialize<ClassWithPublicGetterAndPrivateSetter>(@"{ ""Class"": {} }");

            Assert.NotNull(obj);
            Assert.Null(obj.Class);
        }

        [Fact]
        public static void MissingObjectProperty()
        {
            ClassWithMissingObjectProperty obj
                = JsonSerializer.Deserialize<ClassWithMissingObjectProperty>(@"{ ""Object"": {} }");

            Assert.Null(obj.Collection);
        }

        [Fact]
        public static void MissingCollectionProperty()
        {
            ClassWithMissingCollectionProperty obj
                = JsonSerializer.Deserialize<ClassWithMissingCollectionProperty>(@"{ ""Collection"": [] }");

            Assert.Null(obj.Object);
        }

        private class ClassWithPublicGetterAndPrivateSetter
        {
            public NestedClass Class { get; private set; }
        }

        private class NestedClass
        {
        }

        [Fact]
        public static void JsonIgnoreAttribute()
        {
            var options = new JsonSerializerOptions { IncludeFields = true };

            // Verify default state.
            var obj = new ClassWithIgnoreAttributeProperty();
            Assert.Equal(@"MyString", obj.MyString);
            Assert.Equal(@"MyStringWithIgnore", obj.MyStringWithIgnore);
            Assert.Equal(2, obj.MyStringsWithIgnore.Length);
            Assert.Equal(1, obj.MyDictionaryWithIgnore["Key"]);
            Assert.Equal(3.14M, obj.MyNumeric);

            // Verify serialize.
            string json = JsonSerializer.Serialize(obj, options);
            Assert.Contains(@"""MyString""", json);
            Assert.DoesNotContain(@"MyStringWithIgnore", json);
            Assert.DoesNotContain(@"MyStringsWithIgnore", json);
            Assert.DoesNotContain(@"MyDictionaryWithIgnore", json);
            Assert.DoesNotContain(@"MyNumeric", json);

            // Verify deserialize default.
            obj = JsonSerializer.Deserialize<ClassWithIgnoreAttributeProperty>(@"{}", options);
            Assert.Equal(@"MyString", obj.MyString);
            Assert.Equal(@"MyStringWithIgnore", obj.MyStringWithIgnore);
            Assert.Equal(2, obj.MyStringsWithIgnore.Length);
            Assert.Equal(1, obj.MyDictionaryWithIgnore["Key"]);
            Assert.Equal(3.14M, obj.MyNumeric);

            // Verify deserialize ignores the json for MyStringWithIgnore and MyStringsWithIgnore.
            obj = JsonSerializer.Deserialize<ClassWithIgnoreAttributeProperty>(
                @"{""MyString"":""Hello"", ""MyStringWithIgnore"":""IgnoreMe"", ""MyStringsWithIgnore"":[""IgnoreMe""], ""MyDictionaryWithIgnore"":{""Key"":9}, ""MyNumeric"": 2.71828}", options);
            Assert.Contains(@"Hello", obj.MyString);
            Assert.Equal(@"MyStringWithIgnore", obj.MyStringWithIgnore);
            Assert.Equal(2, obj.MyStringsWithIgnore.Length);
            Assert.Equal(1, obj.MyDictionaryWithIgnore["Key"]);
            Assert.Equal(3.14M, obj.MyNumeric);
        }

        [Fact]
        public static void JsonIgnoreAttribute_UnsupportedCollection()
        {
            string json =
                    @"{
                        ""MyConcurrentDict"":{
                            ""key"":""value""
                        },
                        ""MyIDict"":{
                            ""key"":""value""
                        },
                        ""MyDict"":{
                            ""key"":""value""
                        }
                    }";
            string wrapperJson =
                    @"{
                        ""MyClass"":{
                            ""MyConcurrentDict"":{
                                ""key"":""value""
                            },
                            ""MyIDict"":{
                                ""key"":""value""
                            },
                            ""MyDict"":{
                                ""key"":""value""
                            }
                        }
                    }";

            // Unsupported collections will throw on deserialize by default.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithUnsupportedDictionary>(json));

            // Using new options instance to prevent using previously cached metadata.
            JsonSerializerOptions options = new JsonSerializerOptions();

            // Unsupported collections will throw on serialize by default.
            // Only when the collection contains elements.

            var dictionary = new Dictionary<object, object>();
            // Uri is an unsupported dictionary key.
            dictionary.Add(new Uri("http://foo"), "bar");

            var concurrentDictionary = new ConcurrentDictionary<object, object>(dictionary);

            var instance = new ClassWithUnsupportedDictionary()
            {
                MyConcurrentDict = concurrentDictionary,
                MyIDict = dictionary
            };

            var instanceWithIgnore = new ClassWithIgnoredUnsupportedDictionary
            {
                MyConcurrentDict = concurrentDictionary,
                MyIDict = dictionary
            };

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(instance, options));

            // Unsupported collections will throw on deserialize by default if they contain elements.
            options = new JsonSerializerOptions();
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<WrapperForClassWithUnsupportedDictionary>(wrapperJson, options));

            options = new JsonSerializerOptions();
            // Unsupported collections will throw on serialize by default if they contain elements.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(instance, options));

            // When ignored, we can serialize and deserialize without exceptions.
            options = new JsonSerializerOptions();

            Assert.NotNull(JsonSerializer.Serialize(instanceWithIgnore, options));

            ClassWithIgnoredUnsupportedDictionary obj = JsonSerializer.Deserialize<ClassWithIgnoredUnsupportedDictionary>(json, options);
            Assert.Null(obj.MyDict);

            options = new JsonSerializerOptions();
            Assert.Equal("{}", JsonSerializer.Serialize(new ClassWithIgnoredUnsupportedDictionary()));

            options = new JsonSerializerOptions();
            WrapperForClassWithIgnoredUnsupportedDictionary wrapperObj = JsonSerializer.Deserialize<WrapperForClassWithIgnoredUnsupportedDictionary>(wrapperJson, options);
            Assert.Null(wrapperObj.MyClass.MyDict);

            options = new JsonSerializerOptions();
            Assert.Equal(@"{""MyClass"":{}}", JsonSerializer.Serialize(new WrapperForClassWithIgnoredUnsupportedDictionary()
            {
                MyClass = new ClassWithIgnoredUnsupportedDictionary(),
            }, options)); ;
        }

        [Fact]
        public static void JsonIgnoreAttribute_UnsupportedBigInteger()
        {
            string json = @"{""MyBigInteger"":1}";
            string wrapperJson = @"{""MyClass"":{""MyBigInteger"":1}}";

            // Unsupported types will throw by default.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithUnsupportedBigInteger>(json));
            // Using new options instance to prevent using previously cached metadata.
            JsonSerializerOptions options = new JsonSerializerOptions();
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WrapperForClassWithUnsupportedBigInteger>(wrapperJson, options));

            // When ignored, we can serialize and deserialize without exceptions.
            options = new JsonSerializerOptions();
            ClassWithIgnoredUnsupportedBigInteger obj = JsonSerializer.Deserialize<ClassWithIgnoredUnsupportedBigInteger>(json, options);
            Assert.Null(obj.MyBigInteger);

            options = new JsonSerializerOptions();
            Assert.Equal("{}", JsonSerializer.Serialize(new ClassWithIgnoredUnsupportedBigInteger()));

            options = new JsonSerializerOptions();
            WrapperForClassWithIgnoredUnsupportedBigInteger wrapperObj = JsonSerializer.Deserialize<WrapperForClassWithIgnoredUnsupportedBigInteger>(wrapperJson, options);
            Assert.Null(wrapperObj.MyClass.MyBigInteger);

            options = new JsonSerializerOptions();
            Assert.Equal(@"{""MyClass"":{}}", JsonSerializer.Serialize(new WrapperForClassWithIgnoredUnsupportedBigInteger()
            {
                MyClass = new ClassWithIgnoredUnsupportedBigInteger(),
            }, options));
        }

        public class ObjectDictWrapper : Dictionary<int, string> { }

        public class ClassWithUnsupportedDictionary
        {
            public ConcurrentDictionary<object, object> MyConcurrentDict { get; set; }
            public IDictionary<object, object> MyIDict { get; set; }
            public ObjectDictWrapper MyDict { get; set; }
        }

        public class WrapperForClassWithUnsupportedDictionary
        {
            public ClassWithUnsupportedDictionary MyClass { get; set; } = new ClassWithUnsupportedDictionary();
        }

        public class ClassWithIgnoredUnsupportedDictionary
        {
            [JsonIgnore]
            public ConcurrentDictionary<object, object> MyConcurrentDict { get; set; }
            [JsonIgnore]
            public IDictionary<object, object> MyIDict { get; set; }
            [JsonIgnore]
            public ObjectDictWrapper MyDict { get; set; }
        }

        public class WrapperForClassWithIgnoredUnsupportedDictionary
        {
            public ClassWithIgnoredUnsupportedDictionary MyClass { get; set; }
        }

        public class ClassWithUnsupportedBigInteger
        {
            public BigInteger? MyBigInteger { get; set; }
        }

        public class WrapperForClassWithUnsupportedBigInteger
        {
            public ClassWithUnsupportedBigInteger MyClass { get; set; } = new ClassWithUnsupportedBigInteger();
        }

        public class ClassWithIgnoredUnsupportedBigInteger
        {
            [JsonIgnore]
            public BigInteger? MyBigInteger { get; set; }
        }

        public class WrapperForClassWithIgnoredUnsupportedBigInteger
        {
            public ClassWithIgnoredUnsupportedBigInteger MyClass { get; set; }
        }

        public class ClassWithMissingObjectProperty
        {
            public object[] Collection { get; set; }
        }

        public class ClassWithMissingCollectionProperty
        {
            public object Object { get; set; }
        }

        public class ClassWithPrivateSetterAndGetter
        {
            private string MyString { get; set; }

            public string GetMyString()
            {
                return MyString;
            }

            public void SetMyString(string value)
            {
                MyString = value;
            }
        }

        public class ClassWithReadOnlyFields
        {
            public ClassWithReadOnlyFields()
            {
                MyString = "DefaultValue";
                MyInts = new int[] { 1, 2 };
            }

            public readonly string MyString;
            public readonly int[] MyInts;
        }

        public class ClassWithNoSetter
        {
            public ClassWithNoSetter()
            {
                MyString = "DefaultValue";
                MyInts = new int[] { 1, 2 };
            }

            public string MyString { get; }
            public int[] MyInts { get; }
        }

        public class ClassWithNoGetter
        {
            string _myString = "";
            int[] _myIntArray = new int[] { };
            List<int> _myIntList = new List<int> { };

            public string MyString
            {
                set
                {
                    _myString = value;
                }
            }

            public int[] MyIntArray
            {
                set
                {
                    _myIntArray = value;
                }
            }

            public List<int> MyList
            {
                set
                {
                    _myIntList = value;
                }
            }

            public string GetMyString()
            {
                return _myString;
            }

            public int[] GetMyIntArray()
            {
                return _myIntArray;
            }

            public List<int> GetMyIntList()
            {
                return _myIntList;
            }
        }

        public class ClassWithIgnoreAttributeProperty
        {
            public ClassWithIgnoreAttributeProperty()
            {
                MyDictionaryWithIgnore = new Dictionary<string, int> { { "Key", 1 } };
                MyString = "MyString";
                MyStringWithIgnore = "MyStringWithIgnore";
                MyStringsWithIgnore = new string[] { "1", "2" };
                MyNumeric = 3.14M;
            }

            [JsonIgnore]
            public Dictionary<string, int> MyDictionaryWithIgnore { get; set; }

            [JsonIgnore]
            public string MyStringWithIgnore { get; set; }

            public string MyString { get; set; }

            [JsonIgnore]
            public string[] MyStringsWithIgnore { get; set; }

            [JsonIgnore]
            public decimal MyNumeric;
        }

        private enum MyEnum
        {
            Case1 = 0,
            Case2 = 1,
        }

        private struct StructWithOverride
        {
            [JsonIgnore]
            public MyEnum EnumValue { get; set; }

            [JsonPropertyName("EnumValue")]
            public string EnumString
            {
                get => EnumValue.ToString();
                set
                {
                    if (value == "Case1")
                    {
                        EnumValue = MyEnum.Case1;
                    }
                    else if (value == "Case2")
                    {
                        EnumValue = MyEnum.Case2;
                    }
                    else
                    {
                        throw new Exception("Unknown value!");
                    }
                }
            }
        }

        [Fact]
        public static void OverrideJsonIgnorePropertyUsingJsonPropertyName()
        {
            const string json = @"{""EnumValue"":""Case2""}";

            StructWithOverride obj = JsonSerializer.Deserialize<StructWithOverride>(json);

            Assert.Equal(MyEnum.Case2, obj.EnumValue);
            Assert.Equal("Case2", obj.EnumString);

            string jsonSerialized = JsonSerializer.Serialize(obj);
            Assert.Equal(json, jsonSerialized);
        }

        private struct ClassWithOverrideReversed
        {
            // Same as ClassWithOverride except the order of the properties is different, which should cause different reflection order.
            [JsonPropertyName("EnumValue")]
            public string EnumString
            {
                get => EnumValue.ToString();
                set
                {
                    if (value == "Case1")
                    {
                        EnumValue = MyEnum.Case1;
                    }
                    if (value == "Case2")
                    {
                        EnumValue = MyEnum.Case2;
                    }
                    else
                    {
                        throw new Exception("Unknown value!");
                    }
                }
            }

            [JsonIgnore]
            public MyEnum EnumValue { get; set; }
        }

        [Fact]
        public static void OverrideJsonIgnorePropertyUsingJsonPropertyNameReversed()
        {
            const string json = @"{""EnumValue"":""Case2""}";

            ClassWithOverrideReversed obj = JsonSerializer.Deserialize<ClassWithOverrideReversed>(json);

            Assert.Equal(MyEnum.Case2, obj.EnumValue);
            Assert.Equal("Case2", obj.EnumString);

            string jsonSerialized = JsonSerializer.Serialize(obj);
            Assert.Equal(json, jsonSerialized);
        }

        [Theory]
        [InlineData(typeof(ClassWithProperty_IgnoreConditionAlways))]
        [InlineData(typeof(ClassWithProperty_IgnoreConditionAlways_Ctor))]
        public static void JsonIgnoreConditionSetToAlwaysWorks(Type type)
        {
            string json = @"{""MyString"":""Random"",""MyDateTime"":""2020-03-23"",""MyInt"":4}";

            object obj = JsonSerializer.Deserialize(json, type);
            Assert.Equal("Random", (string)type.GetProperty("MyString").GetValue(obj));
            Assert.Equal(default, (DateTime)type.GetProperty("MyDateTime").GetValue(obj));
            Assert.Equal(4, (int)type.GetProperty("MyInt").GetValue(obj));

            string serialized = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""MyString"":""Random""", serialized);
            Assert.Contains(@"""MyInt"":4", serialized);
            Assert.DoesNotContain(@"""MyDateTime"":", serialized);
        }

        private class ClassWithProperty_IgnoreConditionAlways
        {
            public string MyString { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
            public DateTime MyDateTime { get; set; }
            public int MyInt { get; set; }
        }

        private class ClassWithProperty_IgnoreConditionAlways_Ctor
        {
            public string MyString { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
            public DateTime MyDateTime { get; }
            public int MyInt { get; }

            public ClassWithProperty_IgnoreConditionAlways_Ctor(DateTime myDateTime, int myInt)
            {
                MyDateTime = myDateTime;
                MyInt = myInt;
            }
        }

        [Theory]
        [MemberData(nameof(JsonIgnoreConditionWhenWritingDefault_ClassProperty_TestData))]
        public static void JsonIgnoreConditionWhenWritingDefault_ClassProperty(Type type, JsonSerializerOptions options)
        {
            // Property shouldn't be ignored if it isn't null.
            string json = @"{""Int1"":1,""MyString"":""Random"",""Int2"":2}";

            object obj = JsonSerializer.Deserialize(json, type, options);
            Assert.Equal(1, (int)type.GetProperty("Int1").GetValue(obj));
            Assert.Equal("Random", (string)type.GetProperty("MyString").GetValue(obj));
            Assert.Equal(2, (int)type.GetProperty("Int2").GetValue(obj));

            string serialized = JsonSerializer.Serialize(obj, options);
            Assert.Contains(@"""Int1"":1", serialized);
            Assert.Contains(@"""MyString"":""Random""", serialized);
            Assert.Contains(@"""Int2"":2", serialized);

            // Property should be ignored when null.
            json = @"{""Int1"":1,""MyString"":null,""Int2"":2}";

            obj = JsonSerializer.Deserialize(json, type, options);
            Assert.Equal(1, (int)type.GetProperty("Int1").GetValue(obj));

            if (options.IgnoreNullValues)
            {
                // Null values can be ignored on deserialization using IgnoreNullValues.
                Assert.Equal("DefaultString", (string)type.GetProperty("MyString").GetValue(obj));
            }
            else
            {
                Assert.Null((string)type.GetProperty("MyString").GetValue(obj));
            }

            Assert.Equal(2, (int)type.GetProperty("Int2").GetValue(obj));

            // Set property to be ignored to null.
            type.GetProperty("MyString").SetValue(obj, null);

            serialized = JsonSerializer.Serialize(obj, options);
            Assert.Contains(@"""Int1"":1", serialized);
            Assert.Contains(@"""Int2"":2", serialized);
            Assert.DoesNotContain(@"""MyString"":", serialized);
        }

        private class ClassWithClassProperty_IgnoreConditionWhenWritingDefault
        {
            public int Int1 { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string MyString { get; set; } = "DefaultString";
            public int Int2 { get; set; }
        }

        private class ClassWithClassProperty_IgnoreConditionWhenWritingDefault_Ctor
        {
            public int Int1 { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string MyString { get; set; } = "DefaultString";
            public int Int2 { get; set; }

            public ClassWithClassProperty_IgnoreConditionWhenWritingDefault_Ctor(string myString)
            {
                if (myString != null)
                {
                    MyString = myString;
                }
            }
        }

        private static IEnumerable<object[]> JsonIgnoreConditionWhenWritingDefault_ClassProperty_TestData()
        {
            yield return new object[] { typeof(ClassWithClassProperty_IgnoreConditionWhenWritingDefault), new JsonSerializerOptions() };
            yield return new object[] { typeof(ClassWithClassProperty_IgnoreConditionWhenWritingDefault_Ctor), new JsonSerializerOptions { IgnoreNullValues = true } };
        }

        [Theory]
        [MemberData(nameof(JsonIgnoreConditionWhenWritingDefault_StructProperty_TestData))]
        public static void JsonIgnoreConditionWhenWritingDefault_StructProperty(Type type, JsonSerializerOptions options)
        {
            // Property shouldn't be ignored if it isn't null.
            string json = @"{""Int1"":1,""MyInt"":3,""Int2"":2}";

            object obj = JsonSerializer.Deserialize(json, type, options);
            Assert.Equal(1, (int)type.GetProperty("Int1").GetValue(obj));
            Assert.Equal(3, (int)type.GetProperty("MyInt").GetValue(obj));
            Assert.Equal(2, (int)type.GetProperty("Int2").GetValue(obj));

            string serialized = JsonSerializer.Serialize(obj, options);
            Assert.Contains(@"""Int1"":1", serialized);
            Assert.Contains(@"""MyInt"":3", serialized);
            Assert.Contains(@"""Int2"":2", serialized);

            // Null being assigned to non-nullable types is invalid.
            json = @"{""Int1"":1,""MyInt"":null,""Int2"":2}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, type, options));
        }

        private class ClassWithStructProperty_IgnoreConditionWhenWritingDefault
        {
            public int Int1 { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int MyInt { get; set; }
            public int Int2 { get; set; }
        }

        private struct StructWithStructProperty_IgnoreConditionWhenWritingDefault_Ctor
        {
            public int Int1 { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int MyInt { get; }
            public int Int2 { get; set; }

            [JsonConstructor]
            public StructWithStructProperty_IgnoreConditionWhenWritingDefault_Ctor(int myInt)
            {
                Int1 = 0;
                MyInt = myInt;
                Int2 = 0;
            }
        }

        private static IEnumerable<object[]> JsonIgnoreConditionWhenWritingDefault_StructProperty_TestData()
        {
            yield return new object[] { typeof(ClassWithStructProperty_IgnoreConditionWhenWritingDefault), new JsonSerializerOptions() };
            yield return new object[] { typeof(StructWithStructProperty_IgnoreConditionWhenWritingDefault_Ctor), new JsonSerializerOptions { IgnoreNullValues = true } };
        }

        [Theory]
        [MemberData(nameof(JsonIgnoreConditionNever_TestData))]
        public static void JsonIgnoreConditionNever(Type type)
        {
            // Property should always be (de)serialized, even when null.
            string json = @"{""Int1"":1,""MyString"":""Random"",""Int2"":2}";

            object obj = JsonSerializer.Deserialize(json, type);
            Assert.Equal(1, (int)type.GetProperty("Int1").GetValue(obj));
            Assert.Equal("Random", (string)type.GetProperty("MyString").GetValue(obj));
            Assert.Equal(2, (int)type.GetProperty("Int2").GetValue(obj));

            string serialized = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""Int1"":1", serialized);
            Assert.Contains(@"""MyString"":""Random""", serialized);
            Assert.Contains(@"""Int2"":2", serialized);

            // Property should always be (de)serialized, even when null.
            json = @"{""Int1"":1,""MyString"":null,""Int2"":2}";

            obj = JsonSerializer.Deserialize(json, type);
            Assert.Equal(1, (int)type.GetProperty("Int1").GetValue(obj));
            Assert.Null((string)type.GetProperty("MyString").GetValue(obj));
            Assert.Equal(2, (int)type.GetProperty("Int2").GetValue(obj));

            serialized = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""Int1"":1", serialized);
            Assert.Contains(@"""MyString"":null", serialized);
            Assert.Contains(@"""Int2"":2", serialized);
        }

        [Theory]
        [MemberData(nameof(JsonIgnoreConditionNever_TestData))]
        public static void JsonIgnoreConditionNever_IgnoreNullValues_True(Type type)
        {
            // Property should always be (de)serialized.
            string json = @"{""Int1"":1,""MyString"":""Random"",""Int2"":2}";
            var options = new JsonSerializerOptions { IgnoreNullValues = true };

            object obj = JsonSerializer.Deserialize(json, type, options);
            Assert.Equal(1, (int)type.GetProperty("Int1").GetValue(obj));
            Assert.Equal("Random", (string)type.GetProperty("MyString").GetValue(obj));
            Assert.Equal(2, (int)type.GetProperty("Int2").GetValue(obj));

            string serialized = JsonSerializer.Serialize(obj, options);
            Assert.Contains(@"""Int1"":1", serialized);
            Assert.Contains(@"""MyString"":""Random""", serialized);
            Assert.Contains(@"""Int2"":2", serialized);

            // Property should always be (de)serialized, even when null.
            json = @"{""Int1"":1,""MyString"":null,""Int2"":2}";

            obj = JsonSerializer.Deserialize(json, type, options);
            Assert.Equal(1, (int)type.GetProperty("Int1").GetValue(obj));
            Assert.Null((string)type.GetProperty("MyString").GetValue(obj));
            Assert.Equal(2, (int)type.GetProperty("Int2").GetValue(obj));

            serialized = JsonSerializer.Serialize(obj, options);
            Assert.Contains(@"""Int1"":1", serialized);
            Assert.Contains(@"""MyString"":null", serialized);
            Assert.Contains(@"""Int2"":2", serialized);
        }

        private class ClassWithStructProperty_IgnoreConditionNever
        {
            public int Int1 { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string MyString { get; set; }
            public int Int2 { get; set; }
        }

        private class ClassWithStructProperty_IgnoreConditionNever_Ctor
        {
            public int Int1 { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string MyString { get; }
            public int Int2 { get; set; }

            public ClassWithStructProperty_IgnoreConditionNever_Ctor(string myString)
            {
                MyString = myString;
            }
        }

        private static IEnumerable<object[]> JsonIgnoreConditionNever_TestData()
        {
            yield return new object[] { typeof(ClassWithStructProperty_IgnoreConditionNever) };
            yield return new object[] { typeof(ClassWithStructProperty_IgnoreConditionNever_Ctor) };
        }

        [Fact]
        public static void JsonIgnoreCondition_LastOneWins()
        {
            string json = @"{""MyString"":""Random"",""MYSTRING"":null}";

            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true
            };
            var obj = JsonSerializer.Deserialize<ClassWithStructProperty_IgnoreConditionNever>(json, options);

            Assert.Null(obj.MyString);
        }

        [Fact]
        public static void ClassWithComplexObjectsUsingIgnoreWhenWritingDefaultAttribute()
        {
            string json = @"{""Class"":{""MyInt16"":18}, ""Dictionary"":null}";

            ClassUsingIgnoreWhenWritingDefaultAttribute obj = JsonSerializer.Deserialize<ClassUsingIgnoreWhenWritingDefaultAttribute>(json);

            // Class is deserialized.
            Assert.NotNull(obj.Class);
            Assert.Equal(18, obj.Class.MyInt16);

            // Dictionary is deserialized as JsonIgnoreCondition.WhenWritingDefault only applies to deserialization.
            Assert.Null(obj.Dictionary);

            obj = new ClassUsingIgnoreWhenWritingDefaultAttribute();
            json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Dictionary"":{""Key"":""Value""}}", json);
        }

        public class ClassUsingIgnoreWhenWritingDefaultAttribute
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public SimpleTestClass Class { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public Dictionary<string, string> Dictionary { get; set; } = new Dictionary<string, string> { ["Key"] = "Value" };
        }

        [Fact]
        public static void ClassWithComplexObjectUsingIgnoreNeverAttribute()
        {
            string json = @"{""Class"":null, ""Dictionary"":null}";
            var options = new JsonSerializerOptions { IgnoreNullValues = true };

            var obj = JsonSerializer.Deserialize<ClassUsingIgnoreNeverAttribute>(json, options);

            // Class is not deserialized because it is null in json.
            Assert.NotNull(obj.Class);
            Assert.Equal(18, obj.Class.MyInt16);

            // Dictionary is deserialized regardless of being null in json.
            Assert.Null(obj.Dictionary);

            // Serialize when values are null.
            obj = new ClassUsingIgnoreNeverAttribute();
            obj.Class = null;
            obj.Dictionary = null;

            json = JsonSerializer.Serialize(obj, options);

            // Class is not included in json because it was null, Dictionary is included regardless of being null.
            Assert.Equal(@"{""Dictionary"":null}", json);
        }

        public class ClassUsingIgnoreNeverAttribute
        {
            public SimpleTestClass Class { get; set; } = new SimpleTestClass { MyInt16 = 18 };

            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public Dictionary<string, string> Dictionary { get; set; } = new Dictionary<string, string> { ["Key"] = "Value" };
        }

        [Fact]
        public static void IgnoreConditionNever_WinsOver_IgnoreReadOnlyProperties()
        {
            var options = new JsonSerializerOptions { IgnoreReadOnlyProperties = true };

            // Baseline
            string json = JsonSerializer.Serialize(new ClassWithReadOnlyStringProperty("Hello"), options);
            Assert.Equal("{}", json);

            // With condition to never ignore
            json = JsonSerializer.Serialize(new ClassWithReadOnlyStringProperty_IgnoreNever("Hello"), options);
            Assert.Equal(@"{""MyString"":""Hello""}", json);

            json = JsonSerializer.Serialize(new ClassWithReadOnlyStringProperty_IgnoreNever(null), options);
            Assert.Equal(@"{""MyString"":null}", json);
        }

        [Fact]
        public static void IgnoreConditionWhenWritingDefault_WinsOver_IgnoreReadOnlyProperties()
        {
            var options = new JsonSerializerOptions { IgnoreReadOnlyProperties = true };

            // Baseline
            string json = JsonSerializer.Serialize(new ClassWithReadOnlyStringProperty("Hello"), options);
            Assert.Equal("{}", json);

            // With condition to ignore when null
            json = JsonSerializer.Serialize(new ClassWithReadOnlyStringProperty_IgnoreWhenWritingDefault("Hello"), options);
            Assert.Equal(@"{""MyString"":""Hello""}", json);

            json = JsonSerializer.Serialize(new ClassWithReadOnlyStringProperty_IgnoreWhenWritingDefault(null), options);
            Assert.Equal(@"{}", json);
        }

        [Fact]
        public static void IgnoreConditionNever_WinsOver_IgnoreReadOnlyFields()
        {
            var options = new JsonSerializerOptions { IgnoreReadOnlyProperties = true };

            // Baseline
            string json = JsonSerializer.Serialize(new ClassWithReadOnlyStringField("Hello"), options);
            Assert.Equal("{}", json);

            // With condition to never ignore
            json = JsonSerializer.Serialize(new ClassWithReadOnlyStringField_IgnoreNever("Hello"), options);
            Assert.Equal(@"{""MyString"":""Hello""}", json);

            json = JsonSerializer.Serialize(new ClassWithReadOnlyStringField_IgnoreNever(null), options);
            Assert.Equal(@"{""MyString"":null}", json);
        }

        [Fact]
        public static void IgnoreConditionWhenWritingDefault_WinsOver_IgnoreReadOnlyFields()
        {
            var options = new JsonSerializerOptions { IgnoreReadOnlyProperties = true };

            // Baseline
            string json = JsonSerializer.Serialize(new ClassWithReadOnlyStringField("Hello"), options);
            Assert.Equal("{}", json);

            // With condition to ignore when null
            json = JsonSerializer.Serialize(new ClassWithReadOnlyStringField_IgnoreWhenWritingDefault("Hello"), options);
            Assert.Equal(@"{""MyString"":""Hello""}", json);

            json = JsonSerializer.Serialize(new ClassWithReadOnlyStringField_IgnoreWhenWritingDefault(null), options);
            Assert.Equal(@"{}", json);
        }

        private class ClassWithReadOnlyStringProperty
        {
            public string MyString { get; }

            public ClassWithReadOnlyStringProperty(string myString) => MyString = myString;
        }

        private class ClassWithReadOnlyStringProperty_IgnoreNever
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string MyString { get; }

            public ClassWithReadOnlyStringProperty_IgnoreNever(string myString) => MyString = myString;
        }

        private class ClassWithReadOnlyStringProperty_IgnoreWhenWritingDefault
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string MyString { get; }

            public ClassWithReadOnlyStringProperty_IgnoreWhenWritingDefault(string myString) => MyString = myString;
        }

        private class ClassWithReadOnlyStringField
        {
            public string MyString { get; }

            public ClassWithReadOnlyStringField(string myString) => MyString = myString;
        }

        private class ClassWithReadOnlyStringField_IgnoreNever
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public string MyString { get; }

            public ClassWithReadOnlyStringField_IgnoreNever(string myString) => MyString = myString;
        }

        private class ClassWithReadOnlyStringField_IgnoreWhenWritingDefault
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string MyString { get; }

            public ClassWithReadOnlyStringField_IgnoreWhenWritingDefault(string myString) => MyString = myString;
        }

        [Fact]
        public static void NonPublicMembersAreNotIncluded()
        {
            Assert.Equal("{}", JsonSerializer.Serialize(new ClassWithNonPublicProperties()));

            string json = @"{""MyInt"":1,""MyString"":""Hello"",""MyFloat"":2,""MyDouble"":3}";
            var obj = JsonSerializer.Deserialize<ClassWithNonPublicProperties>(json);
            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(0, obj.GetMyFloat);
            Assert.Equal(0, obj.GetMyDouble);
        }

        private class ClassWithNonPublicProperties
        {
            internal int MyInt { get; set; }
            internal string MyString { get; private set; }
            internal float MyFloat { private get; set; }
            private double MyDouble { get; set; }

            internal float GetMyFloat => MyFloat;
            internal double GetMyDouble => MyDouble;
        }

        [Fact]
        public static void IgnoreCondition_WhenWritingDefault_Globally_Works()
        {
            // Baseline - default values written.
            string expected = @"{""MyString"":null,""MyInt"":0,""MyPoint"":{""X"":0,""Y"":0}}";
            var obj = new ClassWithProps();
            JsonTestHelper.AssertJsonEqual(expected, JsonSerializer.Serialize(obj));

            // Default values ignored when specified.
            Assert.Equal("{}", JsonSerializer.Serialize(obj, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault }));
        }

        private class ClassWithProps
        {
            public string MyString { get; set; }
            public int MyInt { get; set; }
            public Point_2D_Struct MyPoint { get; set; }
        }

        [Fact]
        public static void IgnoreCondition_WhenWritingDefault_PerProperty_Works()
        {
            // Default values ignored when specified.
            Assert.Equal(@"{""MyInt"":0}", JsonSerializer.Serialize(new ClassWithPropsAndIgnoreAttributes()));
        }

        private class ClassWithPropsAndIgnoreAttributes
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string MyString { get; set; }
            public int MyInt { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public Point_2D_Struct MyPoint { get; set; }
        }

        [Fact]
        public static void IgnoreCondition_WhenWritingDefault_DoesNotApplyToCollections()
        {
            var list = new List<bool> { false, true };

            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
            Assert.Equal("[false,true]", JsonSerializer.Serialize(list, options));
        }

        [Fact]
        public static void IgnoreCondition_WhenWritingDefault_DoesNotApplyToDeserialization()
        {
            // Baseline - null values are ignored on deserialization when using IgnoreNullValues (for compat with initial support).
            string json = @"{""MyString"":null,""MyInt"":0,""MyPoint"":{""X"":0,""Y"":0}}";

            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            ClassWithInitializedProps obj = JsonSerializer.Deserialize<ClassWithInitializedProps>(json, options);

            Assert.Equal("Default", obj.MyString);
            // Value types are not ignored.
            Assert.Equal(0, obj.MyInt);
            Assert.Equal(0, obj.MyPoint.X);
            Assert.Equal(0, obj.MyPoint.X);

            // Test - default values (both null and default for value types) are not ignored when using
            // JsonIgnoreCondition.WhenWritingDefault (as the option name implies)
            options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
            obj = JsonSerializer.Deserialize<ClassWithInitializedProps>(json, options);
            Assert.Null(obj.MyString);
            Assert.Equal(0, obj.MyInt);
            Assert.Equal(0, obj.MyPoint.X);
            Assert.Equal(0, obj.MyPoint.X);
        }

        private class ClassWithInitializedProps
        {
            public string MyString { get; set; } = "Default";
            public int MyInt { get; set; } = -1;
            public Point_2D_Struct MyPoint { get; set; } = new Point_2D_Struct(-1, -1);
        }

        [Fact]
        public static void ValueType_Properties_NotIgnoredWhen_IgnoreNullValues_Active_ClassTest()
        {
            var options = new JsonSerializerOptions { IgnoreNullValues = true };

            // Deserialization.
            string json = @"{""MyString"":null,""MyInt"":0,""MyBool"":null,""MyPointClass"":null,""MyPointStruct"":{""X"":0,""Y"":0}}";

            ClassWithValueAndReferenceTypes obj = JsonSerializer.Deserialize<ClassWithValueAndReferenceTypes>(json, options);

            // Null values ignored for reference types/nullable value types.
            Assert.Equal("Default", obj.MyString);
            Assert.NotNull(obj.MyPointClass);
            Assert.True(obj.MyBool);

            // Default values not ignored for value types.
            Assert.Equal(0, obj.MyInt);
            Assert.Equal(0, obj.MyPointStruct.X);
            Assert.Equal(0, obj.MyPointStruct.Y);

            // Serialization.

            // Make all members their default CLR value.
            obj.MyString = null;
            obj.MyPointClass = null;
            obj.MyBool = null;

            json = JsonSerializer.Serialize(obj, options);

            // Null values not serialized, default values for value types serialized.
            JsonTestHelper.AssertJsonEqual(@"{""MyInt"":0,""MyPointStruct"":{""X"":0,""Y"":0}}", json);
        }

        [Fact]
        public static void ValueType_Properties_NotIgnoredWhen_IgnoreNullValues_Active_LargeStructTest()
        {
            var options = new JsonSerializerOptions { IgnoreNullValues = true };

            // Deserialization.
            string json = @"{""MyString"":null,""MyInt"":0,""MyBool"":null,""MyPointClass"":null,""MyPointStruct"":{""X"":0,""Y"":0}}";

            LargeStructWithValueAndReferenceTypes obj = JsonSerializer.Deserialize<LargeStructWithValueAndReferenceTypes>(json, options);

            // Null values ignored for reference types.

            Assert.Equal("Default", obj.MyString);
            // No way to specify a non-constant default before construction with ctor, so this remains null.
            Assert.Null(obj.MyPointClass);
            Assert.True(obj.MyBool);

            // Default values not ignored for value types.
            Assert.Equal(0, obj.MyInt);
            Assert.Equal(0, obj.MyPointStruct.X);
            Assert.Equal(0, obj.MyPointStruct.Y);

            // Serialization.

            // Make all members their default CLR value.
            obj = new LargeStructWithValueAndReferenceTypes(null, new Point_2D_Struct(0, 0), null, 0, null);

            json = JsonSerializer.Serialize(obj, options);

            // Null values not serialized, default values for value types serialized.
            JsonTestHelper.AssertJsonEqual(@"{""MyInt"":0,""MyPointStruct"":{""X"":0,""Y"":0}}", json);
        }

        [Fact]
        public static void ValueType_Properties_NotIgnoredWhen_IgnoreNullValues_Active_SmallStructTest()
        {
            var options = new JsonSerializerOptions { IgnoreNullValues = true };

            // Deserialization.
            string json = @"{""MyString"":null,""MyInt"":0,""MyBool"":null,""MyPointStruct"":{""X"":0,""Y"":0}}";

            SmallStructWithValueAndReferenceTypes obj = JsonSerializer.Deserialize<SmallStructWithValueAndReferenceTypes>(json, options);

            // Null values ignored for reference types.
            Assert.Equal("Default", obj.MyString);
            Assert.True(obj.MyBool);

            // Default values not ignored for value types.
            Assert.Equal(0, obj.MyInt);
            Assert.Equal(0, obj.MyPointStruct.X);
            Assert.Equal(0, obj.MyPointStruct.Y);

            // Serialization.

            // Make all members their default CLR value.
            obj = new SmallStructWithValueAndReferenceTypes(new Point_2D_Struct(0, 0), null, 0, null);

            json = JsonSerializer.Serialize(obj, options);

            // Null values not serialized, default values for value types serialized.
            JsonTestHelper.AssertJsonEqual(@"{""MyInt"":0,""MyPointStruct"":{""X"":0,""Y"":0}}", json);
        }

        private class ClassWithValueAndReferenceTypes
        {
            public string MyString { get; set; } = "Default";
            public int MyInt { get; set; } = -1;
            public bool? MyBool { get; set; } = true;
            public PointClass MyPointClass { get; set; } = new PointClass();
            public Point_2D_Struct MyPointStruct { get; set; } = new Point_2D_Struct(1, 2);
        }

        private struct LargeStructWithValueAndReferenceTypes
        {
            public string MyString { get; }
            public int MyInt { get; set; }
            public bool? MyBool { get; set; }
            public PointClass MyPointClass { get; set; }
            public Point_2D_Struct MyPointStruct { get; set; }

            [JsonConstructor]
            public LargeStructWithValueAndReferenceTypes(
                PointClass myPointClass,
                Point_2D_Struct myPointStruct,
                string myString = "Default",
                int myInt = -1,
                bool? myBool = true)
            {
                MyString = myString;
                MyInt = myInt;
                MyBool = myBool;
                MyPointClass = myPointClass;
                MyPointStruct = myPointStruct;
            }
        }

        private struct SmallStructWithValueAndReferenceTypes
        {
            public string MyString { get; }
            public int MyInt { get; set; }
            public bool? MyBool { get; set; }
            public Point_2D_Struct MyPointStruct { get; set; }

            [JsonConstructor]
            public SmallStructWithValueAndReferenceTypes(
                Point_2D_Struct myPointStruct,
                string myString = "Default",
                int myInt = -1,
                bool? myBool = true)
            {
                MyString = myString;
                MyInt = myInt;
                MyBool = myBool;
                MyPointStruct = myPointStruct;
            }
        }

        public class PointClass { }

        [Fact]
        public static void Ignore_WhenWritingNull_Globally()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true
            };

            string json = @"{
""MyPointClass2_IgnoredWhenWritingNull"":{},
""MyString1_IgnoredWhenWritingNull"":""Default"",
""MyNullableBool1_IgnoredWhenWritingNull"":null,
""MyInt2"":0,
""MyPointStruct2"":{""X"":1,""Y"":2},
""MyInt1"":1,
""MyString2_IgnoredWhenWritingNull"":null,
""MyPointClass1_IgnoredWhenWritingNull"":null,
""MyNullableBool2_IgnoredWhenWritingNull"":true,
""MyPointStruct1"":{""X"":0,""Y"":0}
}";

            // All members should correspond to JSON contents, as ignore doesn't apply to deserialization.
            ClassWithThingsToIgnore obj = JsonSerializer.Deserialize<ClassWithThingsToIgnore>(json, options);
            Assert.NotNull(obj.MyPointClass2_IgnoredWhenWritingNull);
            Assert.Equal("Default", obj.MyString1_IgnoredWhenWritingNull);
            Assert.Null(obj.MyNullableBool1_IgnoredWhenWritingNull);
            Assert.Equal(0, obj.MyInt2);
            Assert.Equal(1, obj.MyPointStruct2.X);
            Assert.Equal(2, obj.MyPointStruct2.Y);
            Assert.Equal(1, obj.MyInt1);
            Assert.Null(obj.MyString2_IgnoredWhenWritingNull);
            Assert.Null(obj.MyPointClass1_IgnoredWhenWritingNull);
            Assert.True(obj.MyNullableBool2_IgnoredWhenWritingNull);
            Assert.Equal(0, obj.MyPointStruct1.X);
            Assert.Equal(0, obj.MyPointStruct1.Y);

            // Ignore null as appropriate during serialization.
            string expectedJson = @"{
""MyPointClass2_IgnoredWhenWritingNull"":{},
""MyString1_IgnoredWhenWritingNull"":""Default"",
""MyInt2"":0,
""MyPointStruct2"":{""X"":1,""Y"":2},
""MyInt1"":1,
""MyNullableBool2_IgnoredWhenWritingNull"":true,
""MyPointStruct1"":{""X"":0,""Y"":0}
}";
            JsonTestHelper.AssertJsonEqual(expectedJson, JsonSerializer.Serialize(obj, options));
        }

        public class ClassWithThingsToIgnore
        {
            public string MyString1_IgnoredWhenWritingNull { get; set; }

            public string MyString2_IgnoredWhenWritingNull;

            public int MyInt1;

            public int MyInt2 { get; set; }

            public bool? MyNullableBool1_IgnoredWhenWritingNull { get; set; }

            public bool? MyNullableBool2_IgnoredWhenWritingNull;

            public PointClass MyPointClass1_IgnoredWhenWritingNull;

            public PointClass MyPointClass2_IgnoredWhenWritingNull { get; set; }

            public Point_2D_Struct_WithAttribute MyPointStruct1;

            public Point_2D_Struct_WithAttribute MyPointStruct2 { get; set; }
        }

        [Fact]
        public static void Ignore_WhenWritingNull_PerProperty()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };

            string json = @"{
""MyPointClass2_IgnoredWhenWritingNull"":{},
""MyString1_IgnoredWhenWritingNull"":""Default"",
""MyNullableBool1_IgnoredWhenWritingNull"":null,
""MyInt2"":0,
""MyPointStruct2"":{""X"":1,""Y"":2},
""MyInt1"":1,
""MyString2_IgnoredWhenWritingNull"":null,
""MyPointClass1_IgnoredWhenWritingNull"":null,
""MyNullableBool2_IgnoredWhenWritingNull"":true,
""MyPointStruct1"":{""X"":0,""Y"":0}
}";

            // All members should correspond to JSON contents, as ignore doesn't apply to deserialization.
            ClassWithThingsToIgnore_PerProperty obj = JsonSerializer.Deserialize<ClassWithThingsToIgnore_PerProperty>(json, options);
            Assert.NotNull(obj.MyPointClass2_IgnoredWhenWritingNull);
            Assert.Equal("Default", obj.MyString1_IgnoredWhenWritingNull);
            Assert.Null(obj.MyNullableBool1_IgnoredWhenWritingNull);
            Assert.Equal(0, obj.MyInt2);
            Assert.Equal(1, obj.MyPointStruct2.X);
            Assert.Equal(2, obj.MyPointStruct2.Y);
            Assert.Equal(1, obj.MyInt1);
            Assert.Null(obj.MyString2_IgnoredWhenWritingNull);
            Assert.Null(obj.MyPointClass1_IgnoredWhenWritingNull);
            Assert.True(obj.MyNullableBool2_IgnoredWhenWritingNull);
            Assert.Equal(0, obj.MyPointStruct1.X);
            Assert.Equal(0, obj.MyPointStruct1.Y);

            // Ignore null as appropriate during serialization.
            string expectedJson = @"{
""MyPointClass2_IgnoredWhenWritingNull"":{},
""MyString1_IgnoredWhenWritingNull"":""Default"",
""MyInt2"":0,
""MyPointStruct2"":{""X"":1,""Y"":2},
""MyInt1"":1,
""MyNullableBool2_IgnoredWhenWritingNull"":true,
""MyPointStruct1"":{""X"":0,""Y"":0}
}";
            JsonTestHelper.AssertJsonEqual(expectedJson, JsonSerializer.Serialize(obj, options));
        }

        public class ClassWithThingsToIgnore_PerProperty
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string MyString1_IgnoredWhenWritingNull { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string MyString2_IgnoredWhenWritingNull;

            public int MyInt1;

            public int MyInt2 { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public bool? MyNullableBool1_IgnoredWhenWritingNull { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public bool? MyNullableBool2_IgnoredWhenWritingNull;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public PointClass MyPointClass1_IgnoredWhenWritingNull;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public PointClass MyPointClass2_IgnoredWhenWritingNull { get; set; }

            public Point_2D_Struct_WithAttribute MyPointStruct1;

            public Point_2D_Struct_WithAttribute MyPointStruct2 { get; set; }
        }

        [Theory]
        [InlineData(typeof(ClassWithBadIgnoreAttribute))]
        [InlineData(typeof(StructWithBadIgnoreAttribute))]
        public static void JsonIgnoreCondition_WhenWritingNull_OnValueType_Fail(Type type)
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize("", type));
            string exAsStr = ex.ToString();
            Assert.Contains("JsonIgnoreCondition.WhenWritingNull", exAsStr);
            Assert.Contains("MyBadMember", exAsStr);
            Assert.Contains(type.ToString(), exAsStr);
            Assert.Contains("JsonIgnoreCondition.WhenWritingDefault", exAsStr);

            ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(Activator.CreateInstance(type)));
            exAsStr = ex.ToString();
            Assert.Contains("JsonIgnoreCondition.WhenWritingNull", exAsStr);
            Assert.Contains("MyBadMember", exAsStr);
            Assert.Contains(type.ToString(), exAsStr);
            Assert.Contains("JsonIgnoreCondition.WhenWritingDefault", exAsStr);
        }

        private class ClassWithBadIgnoreAttribute
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int MyBadMember { get; set; }
        }

        private struct StructWithBadIgnoreAttribute
        {
            [JsonInclude]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Point_2D_Struct MyBadMember { get; set; }
        }
    }
}
