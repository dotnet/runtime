using System;
using System.Collections.Generic;

namespace Program {
    public class AxControlEventArgs<T> : EventArgs where T : WindowlessControl
{
        public T Control { get; private set; }

        public AxControlEventArgs(T control) {
            Control = control;
        }
    }
    public class AxControlEventArgs : AxControlEventArgs<WindowlessControl> {
        public AxControlEventArgs(WindowlessControl c) : base(c) { }
    }

    public class AlignPadLayoutPanel : AlignPadLayoutPanel<WindowlessControl> {
}

    public class AlignPadLayoutPanel<T> : WindowlessControl where T :
WindowlessControl {
        protected override void OnControlAdded(AxControlEventArgs e) {
            e.Control.Resize += Content_Resize; //OK if removed
            base.OnControlAdded(e);
        }

        private void Content_Resize(object sender, EventArgs e) { }
    }

    public class GroupBox : AlignPadLayoutPanel { } //NOT OK
    //public class GroupBox : AlignPadLayoutPanel<WindowlessControl> {} //OK

    public class Program {
        public static readonly AttachedProperty<string> EDITING_TEXT = new
AttachedProperty<string>(null);

        static void Main(string[] args) {
            Console.WriteLine("Program 6");

            var gr = new GroupBox();

            //gr.InsertControl(0, new WindowlessControl()); //OK

            /* need this block(A) this to crash */
            var item = new WindowlessControl();

            new AlignPadLayoutPanel<WindowlessControl>().InsertControl(0,
item); //OK if removed 

            item.SetAttachedProperty(EDITING_TEXT, "label.Text");
            /*end block(A)*/

            gr.InsertControl(0, new WindowlessControl()); //NOT OK            

            var wc = new WindowlessControl();
            wc.SetAttachedProperty(EDITING_TEXT, "label.Text");
            var str = wc.GetAttachedProperty(EDITING_TEXT);

            Console.WriteLine("DONE");
        }
    }

    public class WindowlessControl {
        internal readonly Dictionary<object, object> _AttachedProperties = new
Dictionary<object, object>();

        public void SetAttachedProperty<T>(AttachedProperty<T> prop, T val) {
            Console.WriteLine("SetAttachedProperty 1");
            _AttachedProperties[prop] = val;
            Console.WriteLine("SetAttachedProperty 2");
        }

        public T GetAttachedProperty<T>(AttachedProperty<T> prop) {
            Console.WriteLine("GET AttachedProperty");
            return (T)(_AttachedProperties[prop] ?? prop.DefaultValue);
        }

        public event EventHandler Resize;

        public void InsertControl(int index, WindowlessControl control) {
            OnControlAdded(new AxControlEventArgs(control));
        }

        protected virtual void OnControlAdded(AxControlEventArgs e) { }
    }

    public struct AttachedProperty<T> {
        public T DefaultValue { get; private set; }

        public AttachedProperty(T defaultValue) : this() {
            DefaultValue = defaultValue;
        }
    }
}
