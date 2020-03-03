using System;

public class Test
{
        public static void Main ()
        {
                var vtib = new VTI_C<int> ();
                var result = vtib.GRAF<int> ();
                if (result) {
                        Console.WriteLine ("It works");
                }
        }
}

public abstract class VTIB
{
        public abstract bool GRAF<K>();
}


public class VTI<T> : VTIB
{
        public override bool GRAF<K>() {
                return true;
        }
}

public class VTI_C<T> : VTI<T>
{
}
