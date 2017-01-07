using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PDFUnlocker
{
    class Program
    {
        static List<string> wrongPasswordFiles = new List<string>();
        static void Main(string[] args)
        {
            string inputPath = null;
            string password = null;            
            Console.Title = "PDF Unlocker";

            WriteBranding();            

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            //Check, if we have any parameters passed.
            if(args.Length == 1)
            {
                inputPath = args[0];
            }else if(args.Length == 2)
            {
                inputPath = args[0];
                password = args[1];
            }

            while (true)
            {
                try
                {
                    if (string.IsNullOrEmpty(inputPath))
                    {
                        Console.WriteLine(new String('-',Console.BufferWidth-1));
                        Console.Write("Enter Input file or folder path: ");
                        inputPath = Console.ReadLine();

                        if (string.IsNullOrEmpty(inputPath))
                        {
                            Error("Path cannot be empty. Type \"exit\" to exit application.");
                            continue;
                        }
                        else if (inputPath == "exit")
                        {
                            break;                            
                        }
                    }


                    FileAttributes attr = File.GetAttributes(inputPath);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        if (Directory.Exists(inputPath))
                        {
                            var files = GetFiles(inputPath, d => !d.Contains("unlocked"), "*.pdf");
                            if (files.Count() > 0)
                            {
                                if (string.IsNullOrEmpty(password))
                                {
                                    password = GetPassword();
                                }

                                foreach (string file in files)
                                {                                    
                                    GeneratePdf(file, password, GetOutputFile(file));
                                }
                            }
                        }
                        else
                        {
                            Error($"Invalid path - {inputPath}");
                        }
                    }
                    else
                    {
                        if (File.Exists(inputPath))
                        {
                            if (string.IsNullOrEmpty(password))
                            {
                                password = GetPassword();
                            }
                            
                            GeneratePdf(inputPath, password, GetOutputFile(inputPath));
                        }
                        else
                        {
                            Error($"Invalid path - {inputPath}");
                        }
                    }
                   
                    if(wrongPasswordFiles.Count > 0)
                    {
                        Console.Write($"There are {wrongPasswordFiles.Count} files with wrong password, do you want to enter password for each file manually.?Enter y/n: ");
                        var feedback = Console.ReadLine();
                        if(!string.IsNullOrEmpty(feedback) && feedback.ToLower() == "y")
                        {
                            foreach(var file in wrongPasswordFiles)
                            {
                                Console.Write($"Enter password for {file}:");
                                password = ReadPassword('*');

                                GeneratePdf(file, password, GetOutputFile(file));
                            }
                        }
                    }
                    //Reset wrong password list.
                    wrongPasswordFiles = new List<string>();
                }catch(Exception e)
                {
                    Error(e.Message);
                }                              

                inputPath = null;
                password = null;
            }            
        }

        public static IEnumerable<string> GetFiles(string rootDirectory,Func<string, bool> directoryFilter,string filePattern)
        {
            foreach (string matchedFile in Directory.GetFiles(rootDirectory, filePattern, SearchOption.TopDirectoryOnly))
            {
                yield return matchedFile;
            }

            var matchedDirectories = Directory.GetDirectories(rootDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(directoryFilter);

            foreach (var dir in matchedDirectories)
            {
                foreach (var file in GetFiles(dir, directoryFilter, filePattern))
                {
                    yield return file;
                }
            }
        }

        static private string GetOutputFile(string inputFile)
        {
            var unlockedDir = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(inputFile), "unlocked"));
            return Path.Combine(unlockedDir.FullName, Path.GetFileName(inputFile));
        }

        static private string GetPassword()
        {
            Console.Write("Enter PDF password:");
            return ReadPassword('*');        
        }

        static private void GeneratePdf(string inputFile,string password,string outputFile)
        {
            try
            {
                using (PdfDocument maindoc = PdfReader.Open(inputFile, password, PdfDocumentOpenMode.Import))
                {
                    using (PdfDocument outputDoc = new PdfDocument())
                    {
                        foreach (PdfPage page in maindoc.Pages)
                        {
                            outputDoc.AddPage(page);
                        }

                        outputDoc.Save(outputFile);
                    }                    
                }

                Success($"PDF file \"{inputFile}\" unlocked.");
            }catch(PdfReaderException pre)
            {
                Error(pre.Message);
                if(pre.Message == "The specified password is invalid.")
                {
                    wrongPasswordFiles.Add(inputFile);
                }
            }
            catch (Exception e)
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

        static void Info(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        static void Success(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Error(((Exception)e.ExceptionObject).Message);            
            Console.ReadLine();
            Environment.Exit(1);
        }

        static void WriteBranding()
        {
            Info(new string('*', Console.BufferWidth-1));

            var line1 = "This app unprotects any PDF file with a password.";
            var line1stars = (Console.BufferWidth-1 - line1.Length - 20) / 2;
            Info($"{new string('*', 10)}{new string(' ', line1stars)}{line1}{new string(' ', (Console.BufferWidth-1-20-line1.Length-line1stars))}{new string('*', 10)}");

            var line2 = "Developed by: Kishore Jangid";
            var line2stars = (Console.BufferWidth -1 - line2.Length - 20) / 2 ;
            Info($"{new string('*', 10)}{new string(' ', line2stars)}{line2}{new string(' ', (Console.BufferWidth - 1 - 20 - line2.Length - line2stars))}{new string('*', 10)}");

            var line3 = "This app is free to use";
            var line3stars = (Console.BufferWidth - 1 - line3.Length - 20) / 2;
            Info($"{new string('*', 10)}{new string(' ', line3stars)}{line3}{new string(' ', (Console.BufferWidth - 1 - 20 - line3.Length - line3stars))}{new string('*', 10)}");

            Info(new string('*', Console.BufferWidth - 1));            
        }
    }
}
