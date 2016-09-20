// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace System
{
    public partial class AppDomain
    {
        private static AppDomain s_domain = new AppDomain();
        private AppDomain() {}
        public static AppDomain CurrentDomain => s_domain;
        public string BaseDirectory => AppContext.BaseDirectory;
        public string RelativeSearchPath => null;
        public event UnhandledExceptionEventHandler UnhandledException  
        {  
            add  
            {  
                AppContext.UnhandledException += value;  
            }  
  
            remove  
            {  
                AppContext.UnhandledException -= value;  
            }  
        }  
    }
}