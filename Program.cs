using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;


class Program
{
    public static string? ModelPC;
    public static bool Started = true;

    static void Main()
    {
        Random rnd = new Random();

        ModelPC = "SC-13022";

        Console.Clear();
        Console.WriteLine("System Control : NEM Assembly Terminal\n");

        while (Started)
        {
            WaitInput();    
        }
    }

    static void WaitInput()
    {
        Console.Write($"{ModelPC}> ");

        string Answer = Console.ReadLine() ?? ""; 
        
        switch (Answer.ToLower())
        {
            case "end": Started = false; return;
            case "clear": Console.Clear(); Console.WriteLine("System Control : NEM Assembly Terminal\n"); return;
        } // быстрая проверка коротких команд


        string[] AnswerWords = Answer.Split(' ', StringSplitOptions.RemoveEmptyEntries); 

        try
        {
            RunCommand(AnswerWords[0], AnswerWords[1], AnswerWords[2]);
        }
        catch
        {
            RunCommand(AnswerWords[0], AnswerWords[1], "");
        }

    }

    static void RunCommand(string Command, string File, string Mode)
    {
        switch (Command)
        {
            case "compile": Compilier.Run(File, Mode); break;
            case "run": Interpreter.Run(File, Mode); break;
            case "hex": PrintHexDump(File + ".bin"); break;
            default: Console.WriteLine($"ERROR: Command {Command} is not found"); break;
        }
    }
    
    public static void PrintHexDump(string path)
    {
        string fileText = File.ReadAllText(path);

        string[] fileList = fileText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        byte temp = 0;
        foreach (string part in fileList)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(part);

            string byteCodeText = BitConverter.ToString(bytes).Replace("-", " ");

            string[] byteCodeStringText = byteCodeText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string b in byteCodeStringText)
            {
                if (temp > 16)
                {
                    Console.WriteLine();
                    temp = 0;
                }

                Console.Write(b + " ");
                temp++;
            }
        }
        Console.WriteLine();
    }

}
