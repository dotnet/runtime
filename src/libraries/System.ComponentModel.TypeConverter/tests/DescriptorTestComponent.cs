// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.ComponentModel.Tests
{
    internal enum DescriptorTestEnum
    {
        Value0 = 0,
        Value1 = 1
    }

    internal class DescriptorTestComponent : IComponent, ISite
    {
        private Dictionary<Type, object> _services;

        [DefaultValue(null)]
        public event Action ActionEvent;
        public bool DesignMode { get; set; } = true;
        public const int DefaultPropertyValue = 42;

        [DefaultValue(DefaultPropertyValue)]
        public int Property { get; set; }

        [DefaultValue(0)]
        public DescriptorTestEnum EnumProperty { get; set; }

        public object PropertyWhichThrows { get { throw new NotImplementedException(); } }

        public string StringProperty { get; private set; }

        public const string ProtectedStringPropertyName = nameof(ProtectedStringProperty);
        protected string ProtectedStringProperty { get; private set; }

        public string ProtectedStringPropertyValue => ProtectedStringProperty;

        public DescriptorTestComponent(string stringPropertyValue = "")
        {
            StringProperty = stringPropertyValue;
        }

        public void AddService(Type serviceType, object service)
        {
            if (_services == null)
            {
                _services = new Dictionary<Type, object>();
            }

            _services.Add(serviceType, service);
        }

        public void RaiseEvent() => ActionEvent();

        public event EventHandler Disposed;

        public void Dispose() => Disposed(new object(), new EventArgs());

        public object GetService(Type serviceType)
        {
            if (_services == null)
            {
                return null;
            }

            return _services.TryGetValue(serviceType, out var service)
                ? service
                : null;
        }

        public IComponent Component => this;

        public IContainer Container => null;

        public string Name
        {
            get
            {
                return nameof(DescriptorTestComponent);
            }
            set
            { }
        }

        public ISite Site
        {
            get
            {
                return this;
            }
            set
            { }
        }
    }
}
