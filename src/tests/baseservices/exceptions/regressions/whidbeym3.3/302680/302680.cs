// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

public class xa {

  public static int Main(string[] args) {

    try {

      x.doX(null);

    } catch(NullReferenceException) {
	//Expected
        return 100;
    }
    return 0;
  }

}

  
