// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    public partial interface IServiceProvider
    {
        object GetService(System.Type serviceType);
    }
}
namespace System.ComponentModel
{
    public partial class CancelEventArgs : System.EventArgs
    {
        public CancelEventArgs() { }
        public CancelEventArgs(bool cancel) { }
        public bool Cancel { get { return default(bool); } set { } }
    }
    public partial interface IChangeTracking
    {
        bool IsChanged { get; }
        void AcceptChanges();
    }
    public partial interface IEditableObject
    {
        void BeginEdit();
        void CancelEdit();
        void EndEdit();
    }
    public partial interface IRevertibleChangeTracking : System.ComponentModel.IChangeTracking
    {
        void RejectChanges();
    }
}
