using System;

class ArrayBase <T>
{
        protected T[] arr;
}

class ArrayList<T> : ArrayBase <T>
{
        public void map<V> (ArrayList<V> list) {
                list.arr = null;
        }
}

class Tests
{
        public static void Main () {
                ArrayList <int> arr = new ArrayList <int> ();
                arr.map<string> (new ArrayList <string> ());
        }
}
