// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Tests
{
    internal class DescriptorTestComponent : IComponent, ISite
    {
        [DefaultValue(null)]
        public event Action ActionEvent;
        public bool DesignMode { get; set; } = true;
        public const int DefaultPropertyValue = 42;

        [DefaultValue(DefaultPropertyValue)]
        public int Property { get; set; }

        public object PropertyWhichThrows { get { throw new NotImplementedException(); } }

        public string StringProperty { get; private set; }

        public DescriptorTestComponent(string stringPropertyValue = "")
        {
            StringProperty = stringPropertyValue;
        }

        public void RaiseEvent() => ActionEvent();

        public event EventHandler Disposed;

        public void Dispose() => Disposed(new object(), new EventArgs());

        public object GetService(Type serviceType) => null;

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
