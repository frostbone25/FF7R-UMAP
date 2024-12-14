using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UMapExporter
{
    //Sloppy pretty console writer
    public static class ConsoleWriter
    {
        public static void WriteLine()
        {
            ///*
            Console.ResetColor();
            Console.WriteLine();
            //*/
        }

        public static void WriteLine(string line)
        {
            ///*
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(line);
            Console.ResetColor();
            //*/
        }

        public static void WriteInfoLine(string line)
        {
            ///*
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write("{0, -9}", "[INFO]");
            Console.ResetColor();
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(line);
            Console.ResetColor();
            //*/
        }

        public static void WriteInfoLineAlternate(string line)
        {
            ///*
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write("{0, -9}", "[INFO]");
            Console.ResetColor();
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(line);
            Console.ResetColor();
            //*/
        }

        public static void WriteSuccessLine(string line)
        {
            ///*
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.Write("{0, -9}", "[SUCCESS]");
            Console.ResetColor();
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(line);
            Console.ResetColor();
            //*/
        }

        public static void WriteErrorLine(string line)
        {
            ///*
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.Write("{0, -9}", "[ERROR]");
            Console.ResetColor();
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(line);
            Console.ResetColor();
            //*/
        }

        public static void WriteWarningLine(string line)
        {
            ///*
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.Write("{0, -9}", "[WARNING]");
            Console.ResetColor();
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(line);
            Console.ResetColor();
            //*/
        }
    }
}
