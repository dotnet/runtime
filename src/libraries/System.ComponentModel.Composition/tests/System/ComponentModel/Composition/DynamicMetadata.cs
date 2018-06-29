// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class DynamicMetadata : IDisposable
    {
        [Fact]
        public void SimpleAttachment()
        {
            MetadataStore.Container = new CompositionContainer();
            DynamicMetadataTestClass val = DynamicMetadataTestClass.Get("42");

            var notYetAttached = TypeDescriptor.GetConverter(val);
            Assert.False(notYetAttached.CanConvertFrom(typeof(string)), "The default type converter for DynamicMetadataTestClass shouldn't support round tripping");

            MetadataStore.AddAttribute(
                typeof(DynamicMetadataTestClass),
                (type, attributes) =>
                    Enumerable.Concat(
                        attributes,
                        new Attribute[] { new TypeConverterAttribute(typeof(DynamicMetadataTestClassConverter)) }
                    )
            );
            var attached = TypeDescriptor.GetConverter(val);
            Assert.True(attached.CanConvertFrom(typeof(string)), "The new type converter for DynamicMetadataTestClass should support round tripping");
        }

        [Fact]
        public void LocalContainer()
        {
            var container1 = new CompositionContainer();
            TypeDescriptorServices dat = new TypeDescriptorServices();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(dat);
            container1.Compose(batch);
            MetadataStore.AddAttribute(
                typeof(DynamicMetadataTestClass),
                (type, attributes) =>
                    Enumerable.Concat(
                        attributes,
                        new Attribute[] { new TypeConverterAttribute(typeof(DynamicMetadataTestClassConverter)) }
                    ),
                container1
            );
            DynamicMetadataTestClass val = DynamicMetadataTestClass.Get("42");

            var notYetAttached = TypeDescriptor.GetConverter(val.GetType());
            Assert.False(notYetAttached.CanConvertFrom(typeof(string)), "The default type converter for DynamicMetadataTestClass shouldn't support round tripping");

            var attached = dat.GetConverter(val.GetType());
            Assert.True(attached.CanConvertFrom(typeof(string)), "The new type converter for DynamicMetadataTestClass should support round tripping");
        }

        [Fact]
        public void DualContainers()
        {
            var container1 = new CompositionContainer();
            TypeDescriptorServices dat1 = new TypeDescriptorServices();
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(dat1);
            container1.Compose(batch);
            MetadataStore.AddAttribute(
                typeof(DynamicMetadataTestClass),
                (type, attributes) =>
                    Enumerable.Concat(
                        attributes,
                        new Attribute[] { new TypeConverterAttribute(typeof(DynamicMetadataTestClassConverter)) }
                    ),
                container1
            );

            var container2 = new CompositionContainer();
            CompositionBatch batch2 = new CompositionBatch();
            TypeDescriptorServices dat2 = new TypeDescriptorServices();
            batch2.AddPart(dat2);
            container2.Compose(batch2);

            DynamicMetadataTestClass val = DynamicMetadataTestClass.Get("42");

            var attached1 = dat1.GetConverter(val.GetType());
            Assert.True(attached1.CanConvertFrom(typeof(string)), "The new type converter for DynamicMetadataTestClass should support round tripping");

            var attached2 = dat2.GetConverter(val.GetType());
            Assert.False(attached2.CanConvertFrom(typeof(string)), "The default type converter for DynamicMetadataTestClass shouldn't support round tripping");
        }

        public void Dispose()
        {
            MetadataStore.Container = null;
        }
    }

    [Export]
    public class TypeDescriptorServices
    {
        Dictionary<Type, TypeDescriptionProvider> providers = new Dictionary<Type, TypeDescriptionProvider>();

        internal Dictionary<Type, TypeDescriptionProvider> Providers
        {
            get { return providers; }
            set { providers = value; }
        }

        public ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            if (Providers.ContainsKey(objectType))
            {
                return Providers[objectType].GetTypeDescriptor(objectType);
            }
            else
            {
                return null;
            }
        }
        public void AddProvider(TypeDescriptionProvider provider, Type type)
        {
            Providers[type] = provider;
        }
        public TypeConverter GetConverter(Type type)
        {
            var ictd = GetTypeDescriptor(type, null);
            if (ictd != null)
            {
                return ictd.GetConverter();
            }
            else
            {
                return TypeDescriptor.GetConverter(type);
            }
        }
    }

    public static class MetadataStore
    {
        public static CompositionContainer Container { get; set; }
        static Dictionary<Type, TypeDescriptionProvider> registeredRedirect = new Dictionary<Type, TypeDescriptionProvider>();

        public static void AddAttribute(Type target, Func<MemberInfo, IEnumerable<Attribute>, IEnumerable<Attribute>> provider)
        {
            AddAttribute(target, provider, MetadataStore.Container);
        }
        public static void AddAttribute(Type target, Func<MemberInfo, IEnumerable<Attribute>, IEnumerable<Attribute>> provider, CompositionContainer container)
        {
            ContainerUnawareProviderRedirect.GetRedirect(container)[target] = new MetadataStoreProvider(target, provider);
            RegisterTypeDescriptorInterop(target);
        }
        private static void RegisterTypeDescriptorInterop(Type target)
        {
            if (!registeredRedirect.ContainsKey(target))
            {
                var r = new ContainerUnawareProviderRedirect(target);
                TypeDescriptor.AddProvider(r, target);
                registeredRedirect[target] = r;
            }
            else
            {
                // force a uncache of the information from TypeDescriptor
                //
                TypeDescriptor.RemoveProvider(registeredRedirect[target], target);
                TypeDescriptor.AddProvider(registeredRedirect[target], target);
            }
        }
        public static TypeDescriptorServices GetTypeDescriptorServicesForContainer(CompositionContainer container)
        {
            if (container != null)
            {
                var result = container.GetExportedValueOrDefault<TypeDescriptorServices>();
                if (result == null)
                {
                    var v = new TypeDescriptorServices();
                    CompositionBatch batch = new CompositionBatch();
                    batch.AddPart(v);
                    container.Compose(batch);
                    return v;
                }

                return result;
            }
            return null;
        }

        private class ContainerUnawareProviderRedirect : TypeDescriptionProvider
        {
            public ContainerUnawareProviderRedirect(Type forType)
                : base(TypeDescriptor.GetProvider(forType))
            {
            }
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            {
                var datd = GetTypeDescriptorServicesForContainer(MetadataStore.Container);
                if (datd == null || !datd.Providers.ContainsKey(objectType))
                {
                    return base.GetTypeDescriptor(objectType, instance);
                }
                else
                {
                    return datd.GetTypeDescriptor(objectType, instance);
                }
            }

            internal static Dictionary<Type, TypeDescriptionProvider> GetRedirect(CompositionContainer container)
            {
                TypeDescriptorServices v = GetTypeDescriptorServicesForContainer(container);
                return v != null ? v.Providers : null;
            }
        }

        private class MetadataStoreProvider : TypeDescriptionProvider
        {
            Func<MemberInfo, IEnumerable<Attribute>, IEnumerable<Attribute>> provider;
            public MetadataStoreProvider(Type forType, Func<MemberInfo, IEnumerable<Attribute>, IEnumerable<Attribute>> provider)
                : base(TypeDescriptor.GetProvider(forType))
            {
                this.provider = provider;
            }
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            {
                ICustomTypeDescriptor descriptor = base.GetTypeDescriptor(objectType, instance);
                descriptor = new MetadataStoreTypeDescriptor(objectType, descriptor, provider);
                return descriptor;
            }

        }

        private class MetadataStoreTypeDescriptor : CustomTypeDescriptor
        {
            Type targetType;
            Func<MemberInfo, IEnumerable<Attribute>, IEnumerable<Attribute>> provider;
            public MetadataStoreTypeDescriptor(Type targetType, ICustomTypeDescriptor parent, Func<MemberInfo, IEnumerable<Attribute>, IEnumerable<Attribute>> provider)
                : base(parent)
            {
                this.targetType = targetType;
                this.provider = provider;
            }
            public override TypeConverter GetConverter()
            {
                TypeConverterAttribute attribute = (TypeConverterAttribute)GetAttributes()[typeof(TypeConverterAttribute)];
                if (attribute != null)
                {
                    Type c = this.GetTypeFromName(attribute.ConverterTypeName);
                    if ((c != null) && typeof(TypeConverter).IsAssignableFrom(c))
                    {
                        return (TypeConverter)Activator.CreateInstance(c);
                    }
                }
                return base.GetConverter();
            }
            private Type GetTypeFromName(string typeName)
            {
                if ((typeName == null) || (typeName.Length == 0))
                {
                    return null;
                }
                int length = typeName.IndexOf(',');
                Type type = null;
                if (length == -1)
                {
                    type = targetType.Assembly.GetType(typeName);
                }
                if (type == null)
                {
                    type = Type.GetType(typeName);
                }
                if ((type == null) && (length != -1))
                {
                    type = Type.GetType(typeName.Substring(0, length));
                }
                return type;
            }
            public override AttributeCollection GetAttributes()
            {
                var n = new List<Attribute>();
                foreach (var attr in provider(targetType, base.GetAttributes().OfType<Attribute>()))
                {
                    n.Add(attr);
                }
                return new AttributeCollection(n.ToArray());
            }
        }
    }

    public class DynamicMetadataTestClass
    {
        int i;

        private DynamicMetadataTestClass(int i)
        {
            this.i = i;
        }

        public override string ToString()
        {
            return i.ToString();
        }

        public static DynamicMetadataTestClass Get(string s)
        {
            return new DynamicMetadataTestClass(int.Parse(s));
        }
    }

    public class DynamicMetadataTestClassConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            return ((DynamicMetadataTestClass)value).ToString();
        }
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            return DynamicMetadataTestClass.Get((string)value);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }
    }
}
