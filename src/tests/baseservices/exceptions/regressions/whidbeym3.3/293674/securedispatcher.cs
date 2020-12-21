// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Avalon.Secure
{

    public class Foo
    {

        static public void FooMethod(object sender, EventArgs e)
        {
		bar.BarEvent -= new EventHandler(Foo.FooMethod);
		bar.OnBarEvent();
        }


        ///<summary>
        ///</summary>
        static public Bar bar = null;

    }




    public class Bar
    {
	public Bar(){}

	public event EventHandler BarEvent;

	public void OnBarEvent()
	{
		if (BarEvent != null)
			BarEvent(this, EventArgs.Empty);
	}

	
    }

    
}

