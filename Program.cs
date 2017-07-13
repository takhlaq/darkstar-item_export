using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace darkstar_item_export
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(args);
            Console.WriteLine("Press return key to exit.");
            Console.ReadLine();
        }
    }
}