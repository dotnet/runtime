using System;
using System.Xml;
using System.Xml.Serialization;

class Write {

        static void Main ()
        {
                Contributor c = new Contributor ();
                Console.Write ("What is your name (first-name last-name)?  ");
                c.name = new Name (Console.ReadLine ());

                Console.Write ("What is your e-mail address?  ");
                c.email = Console.ReadLine ();

                Console.Write ("What the filename for your image file? ");
                string s = Console.ReadLine ();
                if (s == null || s == String.Empty)
                        c.image = "none.png";
                else
                        c.image = s;

                Console.Write ("Where are you located?  ");
                c.location = Console.ReadLine ();

                Console.Write ("Who do you work for?  (Optional) ");
                c.organization = Console.ReadLine ();

                Console.Write ("Please give a short decription of who you are: ");
                c.description = Console.ReadLine ();

                Console.Write ("Please list the tasks you're working on, separated by commas: ");
                string t = Console.ReadLine ();

                c.tasks = Cleanup (t.Split (','));

                XmlSerializer ser = new XmlSerializer (typeof (Contributor));
                ser.Serialize (Console.Out, c);

                Console.WriteLine ("\n\nDone.");
        }


        static string [] Cleanup (string [] n)
        {
                string [] result = new string [n.Length];

                int i = 0;
                foreach (string s in n) {
                        string t = s.Trim ();
                        result [i] = t;
                        i++;
                }

                return result;
        }
}
