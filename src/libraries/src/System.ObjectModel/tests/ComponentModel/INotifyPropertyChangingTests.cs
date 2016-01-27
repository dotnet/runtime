// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.ComponentModel;

public class INotifyPropertyChangingTests
{
    [Fact]
    public static void CanImplementEndToEnd()
    {   
        // Integration test that ensures that we have the right types & infrastructure to implement INotifyPropertyChanging end-to-end
        string result = null;
        int callCount = 0;

        Model model = new Model();
        model.PropertyChanging += (sender, e) => { result = e.PropertyName; callCount++; };

        model.Property = "Value";

        Assert.Equal("Property", result);
        Assert.Equal(callCount, 1);

        model.Property = null;

        Assert.Equal("Property", result);
        Assert.Equal(callCount, 2);

        model.Property = "NewValue";

        Assert.Equal("Property", result);
        Assert.Equal(callCount, 3);
    }

    private class Model : INotifyPropertyChanging
    {
        private string _property;

        public event PropertyChangingEventHandler PropertyChanging;

        public string Property
        {
            set
            {
                if (_property != value)
                {
                    _property = value;

                    OnPropertyChanging("Property");
                }
            }

        }

        protected void OnPropertyChanging(string propertyName)
        {
            var handler = PropertyChanging;

            if (handler != null)
            {
                handler(this, new PropertyChangingEventArgs(propertyName));
            }
        }
    }
}
