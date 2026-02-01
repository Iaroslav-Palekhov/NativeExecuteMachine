using System;
using System.Buffers.Text;
using System.ComponentModel;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Microsoft.Win32;

public class Compilier
{
    private static string? file; // путь к файлу
    public static readonly Dictionary<string, string> Opcodes = new Dictionary<string, string> // инструкции
    {
        {"ADD", "A2"}, {"IFKEY", "I8"},
        {"SUB", "S4"}, {"DIV", "D6"}, 
        {"MUL", "M8"}, {"MOV", "M0"}, 
        {"PRT", "P2"}, {"JMP", "J4"}, 
        {"IFZ", "I6"}, {"HLT", "H8"}, 
        {"SWP", "S0"}, {"BEGIN", "B2"}, 
        {"END", "E4"}, {"WAIT", "W6"},
        {"NXT", "N8"}, {"INP", "I0"},
        {"REM", "04"}, {"CLS", "C6"}, 
        {"LIST", "A8"},{"LEN", "G0"},
        {"INT", "I2"}, {"STR", "S8"}
    };


    private static string[] allFiles = Directory.GetFiles("."); // получаем имена всех файлов в репозитории

    private static Dictionary<string, string> AddressConst = new Dictionary<string, string>();

    private static Dictionary<string, string> TempAddressConst = new Dictionary<string, string>(); // временное решение проблемы

    public static void Run(string fileName, string Mode)
    {
        AddressConst.Clear();
        TempAddressConst.Clear();
        file = "./" + fileName;

        if (!allFiles.Contains(file))
        {
            Console.WriteLine($"File {file} is not found");
            return;
        }

        Compile(file);

        if (Mode == "-h")
        {
            Program.PrintHexDump(file + ".bin");
        }
    }

    private static void Compile(string file)
    {
        string[] FileLines = File.ReadAllLines(file);
        string BinPath = file + ".bin"; 

        File.WriteAllText(BinPath, "");

        foreach (string line in FileLines)
        {
            CompileLine(line, BinPath);
        }

        FileLines = File.ReadAllLines(BinPath);
        File.WriteAllText(BinPath, "");

        foreach (string line in FileLines)
        {
            CompileLine(line, BinPath);
        }
    }

    private static void CompileLine(string line, string binpath)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        try {string a = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];} catch {return;}

        foreach (string part in parts)
        {
            if (part == "CONST")
            {
                AddressConst.Add("[" + parts[1] + "]", parts[3].Replace("[", "").Replace("]", ""));
                TempAddressConst.Add("[[" + parts[1] + "]]", parts[3].Replace("[", "").Replace("]", ""));
                return;
            }

            if (part == "END") return;

            if (part == ";") {   
                File.AppendAllText(binpath, "\n");
                return;
            }

            if (TempAddressConst.Keys.Contains(part))
            {
                File.AppendAllText(binpath, '[' + TempAddressConst[part] + ']' + ' ');
                File.AppendAllText(binpath, "\n");
                return;
            }

            try
            {
                File.AppendAllText(binpath, Opcodes[part] + ' ');
            }
            catch
            {
                try
                {
                    File.AppendAllText(binpath, AddressConst[part] + ' ');
                }
                catch
                {
                    File.AppendAllText(binpath, part + ' ');
                }
            }
            
        }
        File.AppendAllText(binpath, "\n");
    }
}

