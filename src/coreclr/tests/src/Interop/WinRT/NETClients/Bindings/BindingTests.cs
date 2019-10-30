// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using TestLibrary;
using Component.Contracts;

namespace NetClient
{
    public class BindingTests
    {
        public static void RunTest()
        {
            IBindingProjectionsTesting target = (IBindingProjectionsTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.BindingProjectionsTesting");
            using (target.InitializeXamlFrameworkForCurrentThread())
            {       
                IBindingViewModel vm = target.CreateViewModel();
                RunINotifyPropertyChangedTest(vm);
                RunINotifyCollectionChangedTest(vm);
            }
        }

        private static void RunINotifyPropertyChangedTest(IBindingViewModel viewModel)
        {
            bool propertyChangedEventFired = false;
            INotifyPropertyChanged notifyPropertyChanged = (INotifyPropertyChanged)viewModel;
            PropertyChangedEventHandler handler = (o, e) => propertyChangedEventFired = (e.PropertyName == nameof(viewModel.Name));
            notifyPropertyChanged.PropertyChanged += handler;
            viewModel.Name = "New Name";
            Assert.IsTrue(propertyChangedEventFired);
            notifyPropertyChanged.PropertyChanged -= handler;
            propertyChangedEventFired = false;
            viewModel.Name = "Old Name";
            Assert.IsFalse(propertyChangedEventFired);
        }

        private static void RunINotifyCollectionChangedTest(IBindingViewModel viewModel)
        {
            bool notifyCollectionChangedEventFired = false;
            int addedElement = 42;
            viewModel.Collection.CollectionChanged += (o, e) => 
            {
                notifyCollectionChangedEventFired = 
                    e.Action == NotifyCollectionChangedAction.Add
                    && e.NewItems.Count == 1
                    && e.NewItems[0] is int i
                    && i == addedElement;
            };

            viewModel.AddElement(addedElement);

            Assert.IsTrue(notifyCollectionChangedEventFired);
        }
    }
}
