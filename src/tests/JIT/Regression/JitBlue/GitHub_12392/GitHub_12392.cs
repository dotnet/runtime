// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

namespace Test
{
    // This is a regression test for a bug in fgMorphRecognizeBoxNullable.
    // See the comment in Setting<T>.HasValue for details.
    public class Program
    {

        [Fact]
        public static int TestEntryPoint()
        {
            Test t = new Test();
            if (!t.TestMethod())
            {
                Console.WriteLine("SUCCESS");
                return 100;
            }
            else
            {
                Console.WriteLine("FAILURE");
                return 0;
            }
        }
    }

    class Test
    {
        Setting<bool?> supportInteractive = Setting.ForBool(null);

        public bool TestMethod()
        {
            return this.supportInteractive.HasValue;
        }
    }

    public class Setting<T>
    {
        Setting()
        {
        }

        public Setting(T value)
        {
            this.value = value;
        }

        public bool HasValue
        {
            get
            {
                if (this.value != null)
                {
                    Type t = this.value.GetType();
                    if (t.IsGenericType)
                    {
                        if (t.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            PropertyInfo hasValueProperty = t.GetProperty("HasValue");
                            bool result = (bool)hasValueProperty.GetValue(this.value, null);
                            return result;
                        }
                    }
                }

                // The bug reproduces when the C# compiler generates
                //   ldnull
                //   cgt.un
                //   ret
                // for this statement.
                // The code above this statement is necessary so that some assertions are
                // propagated and the statement gets re-morphed.
                // The bug in fgMorphRecognizeBoxNullable was that it couldn't deal
                // with a morphed helper call correctly.
                return null != this.value;
            }
        }

        T value;
        public T Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
    }

    public class Setting
    {
        Setting()
        {
            ;
        }

        public static Setting<bool?> ForBool(string parameter)
        {
            if (null == parameter)
            {
                return new Setting<bool?>(null);
            }
            Setting<bool?> setting = new Setting<bool?>(bool.Parse(parameter));
            return setting;
        }
    }
}
