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

  