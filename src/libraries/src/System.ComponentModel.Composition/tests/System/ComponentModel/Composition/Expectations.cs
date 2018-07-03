// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.AttributedModel;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.UnitTesting;

namespace System.ComponentModel.Composition
{
    internal static class Expectations
    {
        public static IEnumerable<CultureInfo> GetCulturesForFormatting()
        {
            yield return new CultureInfo("en-US");      // English (US)
            yield return new CultureInfo("en-AU");      // English (Australian)
            yield return new CultureInfo("en-CA");      // English (Canada)
            yield return new CultureInfo("en-NZ");      // English (Great Britain)
            yield return new CultureInfo("en-JM");      // English (Jamaica)
            yield return new CultureInfo("pt-BR");      // Portuguese (Brazil)
            yield return new CultureInfo("es-AR");      // Spanish (Argentina)
            yield return new CultureInfo("ja-JP");      // Japanese (Japan)
            yield return new CultureInfo("fr-FR");      // French (France)
            yield return new CultureInfo("it-IT");      // Italian (Italy)
            yield return new CultureInfo("de-DE");      // German (Germany)
            yield return new CultureInfo("es-ES");      // Spanish (Spain)
            yield return new CultureInfo("ko-KR");      // Korean (Korea)
            yield return new CultureInfo("zh-TW");      // Chinese (Taiwan)
            yield return new CultureInfo("zh-CN");      // Chinese (People's Republic of China)            
        }     

        public static IEnumerable<Assembly> GetAssemblies()
        {
            yield return Assembly.GetExecutingAssembly();
            yield return Assembly.Load("mscorlib");
        }

        public static IEnumerable<string> GetDisplayNames()
        {
            yield return " ";
            yield return "  ";
            yield return "DisplayName";
            yield return "displayname";
            yield return "This is the display name.";
        }

        public static IEnumerable<ICompositionElement> GetCompositionElements()
        {
            yield return ElementFactory.CreateChain(1);
            yield return ElementFactory.CreateChain(2);
            yield return ElementFactory.CreateChain(3);
            yield return ElementFactory.CreateChain(5);
            yield return ElementFactory.CreateChain(10);
        }

        public static IEnumerable<ICompositionElement> GetCompositionElementsWithNull()
        {
            foreach (var element in GetCompositionElements())
            {
                yield return element;
            }

            yield return null;
        }

        public static IEnumerable<ComposablePartCatalog> GetCatalogs()
        {
            yield return CatalogFactory.Create();
            yield return CatalogFactory.CreateDefaultAttributed();            
        }

        public static IEnumerable<IEnumerable<CompositionError>> GetCompositionErrors()
        {
            foreach (var value in GetEmptyCollections<CompositionError>())
            {
                yield return value;
            }

            yield return new CompositionError[] { new CompositionError("") };
            yield return new CompositionError[] { new CompositionError(""), new CompositionError("Description") };
            yield return new CompositionError[] { new CompositionError(""), new CompositionError("Description"), ErrorFactory.Create(CompositionErrorId.InvalidExportMetadata, "Description", (Exception)null), ErrorFactory.Create(CompositionErrorId.Unknown, "Description", new Exception()) };
        }

        public static IEnumerable<string> GetContractNames()
        {
            yield return " ";
            yield return "   ";
            yield return "ContractName";
            yield return "contractName";
            yield return "{ContractName}";
            yield return "{ContractName}Name";
            yield return "System.Windows.Forms.Control";
            yield return "{System.Windows.Forms}Control";
            yield return "{9}Control";
        }

        public static IEnumerable<string> GetContractNamesWithEmpty()
        {
            foreach (string contractName in GetContractNames())
            {
                yield return contractName;
            }

            yield return string.Empty;
        }

        public static IEnumerable<object> GetObjectsReferenceTypes()
        {
            yield return "Value";
            yield return new Collection<string>();
            yield return new IEnumerable[0];
        }

        public static IEnumerable<ValueType> GetObjectsValueTypes()
        {
            yield return 10;
            yield return 10.0;
            yield return DayOfWeek.Wednesday;
        }

        public static IEnumerable<string> GetMetadataNames()
        {
            return GetContractNamesWithEmpty();
        }

        public static IEnumerable<object> GetMetadataValues()
        {
            yield return null;
            yield return string.Empty;
            yield return "";
            yield return " ";
            yield return "   ";
            yield return (int)1;
            yield return (byte)1;
            yield return (float)1.1;
            yield return (double)1.1;
            yield return DayOfWeek.Wednesday;
        }

        public static IEnumerable<Dictionary<string, Type>> GetRequiredMetadata()
        {
            yield return new Dictionary<string, Type> { { "", typeof(object) } };
            yield return new Dictionary<string, Type> { { " ", typeof(object) } };
            yield return new Dictionary<string, Type> { { "  ", typeof(object) } };
            yield return new Dictionary<string, Type> { { "   ", typeof(object) } };
            yield return new Dictionary<string, Type> { { "A", typeof(object) } };
            yield return new Dictionary<string, Type> { { "A", typeof(object) }, { "B", typeof(object) } };
            yield return new Dictionary<string, Type> { { "A", typeof(object) }, { "B", typeof(object) }, { "C", typeof(object) } };
            yield return new Dictionary<string, Type> { { "a", typeof(object) } };
            yield return new Dictionary<string, Type> { { "a", typeof(object) }, { "b", typeof(object) } };
            yield return new Dictionary<string, Type> { { "a", typeof(object) }, { "b", typeof(object) }, { "c", typeof(object) } };
            yield return new Dictionary<string, Type> { { "Metadata1", typeof(object) }, { "Metadata2", typeof(object) }, { "Metadata3", typeof(object) } };
        }

        public static IEnumerable<Dictionary<string, Type>> GetRequiredMetadataWithEmpty()
        {
            foreach (var requiredMetadata in GetRequiredMetadata())
            {
                yield return requiredMetadata;
            }
            
            yield return new Dictionary<string, Type>();
        }

        public static IEnumerable<IDictionary<string, object>> GetMetadata()
        {
            yield return new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(0));
            yield return new Dictionary<string, object>();
            yield return new SortedDictionary<string, object>();
            yield return new SortedList<string, object>();
            var metadata = new Dictionary<string, object>();
            metadata.Add("One", "Value");
            metadata.Add("Two", true);
            metadata.Add("Three", 10);
            metadata.Add("Four", 1.0);
            metadata.Add("Five", null);
            
            yield return metadata;
        }

        public static IEnumerable<string> GetExceptionMessages()
        {
            yield return "";
            yield return " ";
            yield return "  ";
            yield return "message";
            yield return "This is an error message.";
            yield return "Line One." + Environment.NewLine + "Line Two.";
        }

        public static IEnumerable<Exception> GetInnerExceptions()
        {
            yield return new Exception();
            yield return new ArgumentException();
            yield return new SystemException();
            yield return new CompositionException();
            yield return new ImportCardinalityMismatchException();
        }

        public static IEnumerable<Exception> GetInnerExceptionsWithNull()
        {
            foreach (var exception in GetInnerExceptions())
            {
                yield return exception;
            }
            
            yield return null;
        }

        public static IEnumerable<bool> GetBooleans()
        {
            yield return false;
            yield return true;
            yield return false;
            yield return true;
        }

        public static IEnumerable<ReflectionComposablePartDefinition> GetAttributedDefinitions()
        {
            foreach (var type in GetAttributedTypes())
            {
                yield return PartDefinitionFactory.CreateAttributed(type);
            }
        }

        public static IEnumerable<Type> GetTypes()
        {
            yield return typeof(void);
            yield return typeof(Type);
            yield return typeof(double);
            yield return typeof(string);
            yield return typeof(int);
            yield return typeof(CompositionServices);
            yield return typeof(ICompositionService);
        }

        public static IEnumerable<Type> GetAttributedTypes()
        {
            foreach (Type type in typeof(Expectations).Assembly.GetTypes())
            {
                var definition = AttributedModelDiscovery.CreatePartDefinitionIfDiscoverable(type, (ICompositionElement)null);
                if (definition != null)
                {
                    yield return type;
                }

            }
        }

        public static IEnumerable<MemberInfo> GetMembers()
        {
            yield return typeof(String).GetSingleMember("Length");
            yield return typeof(Int32).GetSingleMember("MaxValue");
        }

        public static IEnumerable<TEnum> GetEnumValues<TEnum>() where TEnum : struct
        {
            var values = TestServices.GetEnumValues<TEnum>();

            foreach (TEnum value in values)
            {
                yield return value;
            }
        }

        public static IEnumerable<TEnum> GetInvalidEnumValues<TEnum>() where TEnum : struct
        {
            var bounds = GetEnumBounds<TEnum>();

            yield return AddEnum(bounds.Item1, -3);
            yield return AddEnum(bounds.Item1, -2);
            yield return AddEnum(bounds.Item1, -1);
            yield return AddEnum(bounds.Item2, 1);
            yield return AddEnum(bounds.Item2, 2);
            yield return AddEnum(bounds.Item2, 3);
            yield return (TEnum)(object)(int.MinValue + 1);
            yield return (TEnum)(object)int.MinValue;  
            yield return (TEnum)(object)int.MaxValue;
            yield return (TEnum)(object)(int.MaxValue - 1);
        }

        public static IEnumerable<object[]> GetObjectArraysWithNull()
        {
            yield return null;
            yield return new object[0];
            yield return new object[] { null };
            yield return new string[] { null };
            yield return new object[] { 1, "2", 3.0 };
            yield return new string[] { "1" };
            yield return new string[] { "1", "2" };
            yield return new string[] { "1", "2", "3" };
        }

        private static TEnum AddEnum<TEnum>(TEnum left, int right) where TEnum : struct
        {
            int intRight = (int)(object)left;

            return (TEnum)(object)(intRight + right);
        }

        private static Tuple<TEnum, TEnum> GetEnumBounds<TEnum>() where TEnum : struct
        {
            var values = TestServices.GetEnumValues<TEnum>();

            return new Tuple<TEnum, TEnum>(values.First(), values.Last());
        }

        private static IEnumerable<IEnumerable<T>> GetEmptyCollections<T>()
        {
            yield return new T[0];
            yield return Enumerable.Empty<T>();
            yield return new List<T>();
            yield return new Collection<T>();
            yield return new Dictionary<T, object>().Keys;
        }
    }
}
