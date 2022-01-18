// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

Console.WriteLine("Hello, World!");

var type = Type.GetType("AotProblem." + "_测试数据记录仪_Iiİı_åäö_Controller_DataLogger1_log_all_", false);

Console.WriteLine(type?.FullName??"null");

var obj = Activator.CreateInstance(type!);

Console.WriteLine(obj?.GetType().Name);

return 100;

namespace AotProblem
{
    public class _测试数据记录仪_Iiİı_åäö_Controller_DataLogger1_log_all_
    {

    }
}
