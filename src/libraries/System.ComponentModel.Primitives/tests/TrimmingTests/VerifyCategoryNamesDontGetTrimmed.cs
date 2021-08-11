// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace Test
{
    /// <summary>
    /// When UseSystemResourceStrings feature switch is on, we want to validate that getting the resource string of
    /// a category attribute won't result in having "PropertyCategory" appended to the beginning of the resulting string.
    /// This test ensures that both built-in categories as well as custom categories get the right Category when the
    /// feature switch is on.
    /// </summary>
    public class Program
    {
        public static int Main()
        {
            if (GetEnumCategory(AnEnum.Action) == "Action" && GetEnumCategory(AnEnum.Something) == "Something" && GetEnumCategory(AnEnum.WindowStyle) == "Window Style")
            {
                return 100;
            }
            return -1;
        }

        public static string GetEnumCategory<T>(T enumValue)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                return null;
            var enumCategory = enumValue.ToString();
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            if (fieldInfo != null)
            {
                var attrs = fieldInfo.GetCustomAttributes(typeof(CategoryAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    enumCategory = ((CategoryAttribute)attrs[0]).Category;
                }
            }
            return enumCategory;
        }
    }

    public enum AnEnum
    {
        [Category("Action")] // Built-in category
        Action = 1,

        [Category("Something")] // Custom category
        Something = 2,

        [Category("WindowStyle")] // Built-in category with localized string different than category name.
        WindowStyle = 3,
    }
}