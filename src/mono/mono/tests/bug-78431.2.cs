  public class Pair <T> { 
    public T fst; 
    public T snd; 
  }
  
  public class RList <T>  {
    public class Nil : RList <T> {}
    public class Zero : RList <T> { 
      public RList <Pair <T> > arg;
    }

    static int _Length (RList <T> xs) {
      if (xs is Zero)
        return RList <Pair <T> >._Length (((Zero)xs).arg);
      else
        return 0;
    }
    public int Length  {
      get { 
        return _Length (this);
      }
    }    
  }


class M { 
  public static void Main() {
    int x = (new RList<object>.Nil()).Length;
  }
}


