// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// customCultureInfo is derived from the System.Globalization.CultureInfo
/// in order to provide support for a custom culture that is closely related
/// to its parent culture. 
/// In this sample, the custom culture's role is limited to providing access 
/// to custom culture specific resources and providing a date format but the 
/// role of the custom culture could easily be expanded to provide additional 
/// differentiation with its parent culture. To do so, it is necessary to 
/// implement support for the other properties of the base CultureInfo class 
/// by overriding them in the derived class
/// </summary>

namespace CustomCulture
{
	public class CustomCultureInfo : CultureInfo
	{
    private string myDescription;
    private string myName;
    private string myParent;

    // the constructor takes two parameters: the parent name and the custom name
		public CustomCultureInfo(string parent, string customName) : base(parent)
		{
      myParent      = parent;
      myName        = String.Format("{0}-{1}", parent, customName);
      myDescription = String.Format("Custom Culture ({0})", myName);

      // set formatting for date time
      NumberFormatInfo nfi = (NumberFormatInfo)(new CultureInfo("en-US")).NumberFormat.Clone();
      nfi.CurrencyPositivePattern = 3;
      nfi.CurrencyGroupSeparator = "'";
      nfi.CurrencySymbol = "$";
      nfi.CurrencyDecimalDigits = 0;
      base.NumberFormat = nfi;
		}

    public override String Name
    {
      get { return myName; }
    }

    public override CultureInfo Parent
    {
      get { return new CultureInfo(myParent); }
    }

    public override String EnglishName
    {
      get { return myDescription; }
    }

    public override String NativeName
    {
      get { return myDescription; }
    }

  }
}