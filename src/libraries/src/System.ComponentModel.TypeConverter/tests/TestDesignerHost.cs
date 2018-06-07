// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design.Tests
{
    public class TestDesignerHost : IDesignerHost
    {
        public IContainer Container => throw new NotImplementedException();
        public bool InTransaction => throw new NotImplementedException();
        public bool Loading => throw new NotImplementedException();
        public IComponent RootComponent => throw new NotImplementedException();
        public string RootComponentClassName => throw new NotImplementedException();
        public string TransactionDescription => throw new NotImplementedException();

#pragma warning disable 0067
        public event EventHandler Activated;
        public event EventHandler Deactivated;
        public event EventHandler LoadComplete;
        public event DesignerTransactionCloseEventHandler TransactionClosed;
        public event DesignerTransactionCloseEventHandler TransactionClosing;
        public event EventHandler TransactionOpened;
        public event EventHandler TransactionOpening;
#pragma warning restore 0067

        public void Activate() => throw new NotImplementedException();
        public void AddService(Type serviceType, ServiceCreatorCallback callback) => throw new NotImplementedException();
        public void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) => throw new NotImplementedException();
        public void AddService(Type serviceType, object serviceInstance) => throw new NotImplementedException();
        public void AddService(Type serviceType, object serviceInstance, bool promote) => throw new NotImplementedException();

        public IComponent CreateComponent(Type componentClass) => throw new NotImplementedException();
        public IComponent CreateComponent(Type componentClass, string name) => throw new NotImplementedException();
        public DesignerTransaction CreateTransaction() => throw new NotImplementedException();
        public DesignerTransaction CreateTransaction(string description) => throw new NotImplementedException();

        public void DestroyComponent(IComponent component) => throw new NotImplementedException();
        public IDesigner GetDesigner(IComponent component) => throw new NotImplementedException();
        public object GetService(Type serviceType) => throw new NotImplementedException();
        public Type GetType(string typeName) => throw new NotImplementedException();
        public void RemoveService(Type serviceType) => throw new NotImplementedException();
        public void RemoveService(Type serviceType, bool promote) => throw new NotImplementedException();
    }
}
