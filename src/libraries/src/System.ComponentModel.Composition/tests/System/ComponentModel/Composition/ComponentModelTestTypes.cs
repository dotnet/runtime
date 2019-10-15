// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    public class CallbackExecuteCodeDuringCompose
    {
        public CallbackExecuteCodeDuringCompose(Action callback)
        {
            this.callback = callback;
        }

        [Export("MyOwnCallbackContract")]
        public string ExportValue
        {
            get
            {
                callback();
                return string.Empty;
            }
        }

        [Import("MyOwnCallbackContract")]
        public string ImportValue { get; set; }
        private Action callback;
    }

    public class CallbackImportNotify : IPartImportsSatisfiedNotification
    {
        private Action callback;
        public CallbackImportNotify(Action callback)
        {
            this.callback = callback;
        }

        [Import(AllowDefault = true)]
        public ICompositionService ImportSomethingSoIGetImportCompletedCalled { get; set; }

        public void OnImportsSatisfied()
        {
            this.callback();
        }
    }

    public class ExportValueTypeFactory
    {
        [Export("{AssemblyCatalogResolver}FactoryValueType")]
        public int Value
        {
            get
            {
                return 18;
            }
        }
    }

    public class ExportValueTypeSingleton
    {
        [Export("{AssemblyCatalogResolver}SingletonValueType")]
        public int Value
        {
            get
            {
                return 17;
            }
        }
    }
}
