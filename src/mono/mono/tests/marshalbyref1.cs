class Base : System.MarshalByRefObject {
        public virtual void method () {
        }
}

class Derived : Base {
        public override void method () {
                base.method ();
        }
        static void Main() {
                Derived d = new Derived ();
                d.method ();
        }
}
