public class Grid<CT>
        where CT : Grid<CT>.GPD.GC, new()
{
        public abstract class GPD
        {
                public GPD()
                {
                        ctInst = new CT();
                }

                public readonly CT ctInst;

                public abstract class GC
                {
                }
        }
}

public class H : Grid<H.MyCT>.GPD
{
        public class MyCT : GC
        {
                // When no explicit default constructor is present GMCS fails to compile the file.
                // When it is present the execution crashes on mono.
                public MyCT () {}
        }
}

public class TheTest
{
        public static void Main (string[] args)
        {
                new H();
        }
}