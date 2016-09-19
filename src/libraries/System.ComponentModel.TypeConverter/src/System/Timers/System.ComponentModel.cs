// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Stub to unblock explosing System.Timers Issue dotnet/corefx#11774

namespace System.ComponentModel.Design
{
    public partial interface IDesignerHost
    {
        IComponent RootComponent { get; }
    }
}

namespace System.ComponentModel
{
    public partial interface ISynchronizeInvoke
    {
        IAsyncResult BeginInvoke(Delegate method, object[] args);
        object EndInvoke(IAsyncResult result);
        object Invoke(Delegate method, object[] args);
        bool InvokeRequired { get; }
    }

    public partial interface ISupportInitialize
    {
        void BeginInit();
        void EndInit();
    }

    public partial class Component : IDisposable
    {
        public Component() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~Component() { }
        protected virtual object GetService(Type service) { return default(object); }
        public override string ToString() { return default(string); }
        protected virtual bool CanRaiseEvents { get { return default(bool); } }
        protected bool DesignMode { get { return default(bool); } }
        public virtual ISite Site { get { return default(ISite); } set { } }

    }
}
