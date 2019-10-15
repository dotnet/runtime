// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design;

namespace System.ComponentModel.Tests
{
    internal class MockDesignerHost : IDesignerHost
    {
        private Dictionary<IComponent, IDesigner> _designers;

        public void AddDesigner(IComponent component, IDesigner designer)
        {
            if (_designers == null)
            {
                _designers = new Dictionary<IComponent, IDesigner>();
            }

            _designers.Add(component, designer);
        }

        public IContainer Container => throw new NotImplementedException();

        public bool InTransaction => throw new NotImplementedException();

        public bool Loading => throw new NotImplementedException();

        public IComponent RootComponent => throw new NotImplementedException();

        public string RootComponentClassName => throw new NotImplementedException();

        public string TransactionDescription => throw new NotImplementedException();

        public event EventHandler Activated
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler Deactivated
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler LoadComplete
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event DesignerTransactionCloseEventHandler TransactionClosed
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event DesignerTransactionCloseEventHandler TransactionClosing
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler TransactionOpened
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public event EventHandler TransactionOpening
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void AddService(Type serviceType, ServiceCreatorCallback callback)
        {
            throw new NotImplementedException();
        }

        public void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
        {
            throw new NotImplementedException();
        }

        public void AddService(Type serviceType, object serviceInstance)
        {
            throw new NotImplementedException();
        }

        public void AddService(Type serviceType, object serviceInstance, bool promote)
        {
            throw new NotImplementedException();
        }

        public IComponent CreateComponent(Type componentClass)
        {
            throw new NotImplementedException();
        }

        public IComponent CreateComponent(Type componentClass, string name)
        {
            throw new NotImplementedException();
        }

        public DesignerTransaction CreateTransaction()
        {
            throw new NotImplementedException();
        }

        public DesignerTransaction CreateTransaction(string description)
        {
            throw new NotImplementedException();
        }

        public void DestroyComponent(IComponent component)
        {
            throw new NotImplementedException();
        }

        public IDesigner GetDesigner(IComponent component)
        {
            if (_designers == null)
            {
                return null;
            }

            return _designers.TryGetValue(component, out var designer)
                ? designer
                : null;
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public Type GetType(string typeName)
        {
            throw new NotImplementedException();
        }

        public void RemoveService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public void RemoveService(Type serviceType, bool promote)
        {
            throw new NotImplementedException();
        }
    }
}
