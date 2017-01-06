using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PDFUnlocker
{
    class Program
    {
        static string inputFile;
        static string password;
        static void Main(string[] args)
        {
            //Check, if we have any parameters passed.
            if(args.Length == 0)
            {
                Console.WriteLine("Enter Input file path: ");
                inputFile = Console.ReadLine();
            }else if(args.Length == 1)
            {
                inputFile = args[0];
            }else if(args.Length == 2)
            {
                inputFile = args[0];
                password = args[1];
            }

            if (File.Exists(inputFile)){                
                if(Path.GetExtension(inputFile) == ".pdf")
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        Console.WriteLine("Enter PDF password:");
                        password = ReadPassword('*');
                    }

                    string newFileName = Path.Combine(Path.GetDirectoryName(inputFile),
                        Path.GetFileNameWithoutExtension(inputFile) + "_unlocked.pdf");
                    GeneratePdf(inputFile, password, newFileName);
                }
                else
                {
                    Error("Only PDF files accepted.");
                }
            }else
            {
                Error("File doesn't exists or invalid path.");
            }
            Console.ReadLine();
        }

        static private void GeneratePdf(string inputFile,string password,string outputFile)
        {
            try
            {
                PdfDocument maindoc = PdfReader.Open(inputFile, password, PdfDocumentOpenMode.Import);
                //Use the property HasOwnerPermissions to decide whether the used password
                // was the user or the owner password.   
                bool hasOwnerAccess = maindoc.SecuritySettings.HasOwnerPermissions;

                PdfDocument outputDoc = new PdfDocument();

                foreach (PdfPage page in maindoc.Pages)
                {
                    outputDoc.AddPage(page);
                }

                outputDoc.Save(outputFile);                
                maindoc.Dispose();
                outputDoc.Dispose();               
                Info("PDF file unlocked.");
            }catch(Exception e)
            {
                Error(e.Message);
            }
        }

        /// <summary>
        /// Like System.Console.ReadLine(), only with a mask.
        /// </summary>
        /// <param name="mask">a <c>char</c> representing your choice of console mask</param>
        /// <returns>the string the user typed in </returns>
        public static string ReadPassword(char mask)
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            var pass = new Stack<char>();
            char chr = (char)0;

            while ((chr = System.Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (chr == BACKSP)
                {
                    if (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == CTRLBACKSP)
                {
                    while (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (FILTERED.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push((char)chr);
                    System.Console.Write(mask);
                }
            }

            System.Console.WriteLine();

            return new string(pass.Reverse().ToArray());
        }

        private static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + msg);
            Console.ResetColor();
        }

        static void Info(string info)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(info);
            Console.ResetColor();
        }
    }
}
