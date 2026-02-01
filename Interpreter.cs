using System;
using System.Buffers.Text;
using System.IO;
using System.Linq.Expressions;
using System.Runtime;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32;


public class Interpreter
{
    private static string? file; // путь к файлу
    private static bool Active = true;
    private static bool IfKey = false; // есть ли ifkey в коде?
    private static string? mode;
    private static string[] allFiles = Directory.GetFiles("."); // получаем имена всех файлов в репозитории

    private static Dictionary<int, string> InstructionAddress = new Dictionary<int, string>();

    private static Dictionary<string, int> PointAddress = new Dictionary<string, int>();

    private static Dictionary<string, string> VarAddress = new Dictionary<string, string>();

    private static Dictionary<string, long> Registers = new Dictionary<string, long>
    {
        {"R1", 0}, {"R2", 0}, {"R3", 0}, {"R4", 0},
        {"R5", 0}, {"R6", 0}, {"R7", 0}, {"R8", 0}
    };

    private static string currentKey = "";

    private static Dictionary<string, List<string>> arrs = new Dictionary<string, List<string>>();
    
    public static async Task Run(string fileName, string Mode)
    {

        file = "./" + fileName + ".bin";
        mode = Mode;
        InstructionAddress.Clear();
        PointAddress.Clear();
        VarAddress.Clear();
        Active = true;
        IfKey = false;
        arrs.Clear();

        if (!allFiles.Contains(file))
        {
            Console.WriteLine($"File {file} is not found");
            return;
        }

        await Interpretation();
    }

    private static async Task CheckKey()
    {
        ConsoleKeyInfo keyInfo;
        while (true)
        {
            if (Console.KeyAvailable)
            {
                keyInfo = Console.ReadKey(true);
                currentKey = keyInfo.Key.ToString();
                await Task.Delay(50);
                currentKey = "";
            }
            await Task.Delay(20);
        }
        
    }

    private static async Task Interpretation()
    {
        string[] FileAllLines = File.ReadAllLines(file);

        int number = 0;
        foreach (string line in FileAllLines){
            InstructionAddress.Add(number, line);
            number++;
        }

        FirstRun();

        if (IfKey) {var checkKeyTask = CheckKey();}

        FinalRun();
        Console.WriteLine();
    }

    private static void FirstRun()
    {
        foreach (int key in InstructionAddress.Keys)
        {
            string line = InstructionAddress[key];
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0])
            {
                case "B2":
                {
                    PointAddress.Add(parts[1], key);
                    break;
                }

                case "I8":
                {
                    IfKey = true;
                    break;
                }
            }
        }
    }


    private static void FinalRun()
    {
        int number = 0;

        while (number < InstructionAddress.Count)
        {
            string[] parts = InstructionAddress[number].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (Active == false) return;

            switch(parts[0])
            {

                case "M0": // MOV
                {
                    if (Registers.Keys.Contains(parts[1]))
                    {
                        if (parts[2][0] == 'R')
                        {
                            Registers[parts[1]] = Registers[parts[2]];
                            break;
                        }

                        if (parts[2].Length == 7 && parts[2][0] == '0' && parts[2][1] == 'x')
                        {
                            if (!VarAddress.Keys.Contains(parts[2]))
                            {
                                VarAddress.Add(parts[2], "0");
                            }
                            Registers[parts[1]] = Convert.ToInt64(VarAddress[parts[2]]);
                            break;
                        }

                        if (arrs.Keys.Contains(parts[2]))
                        {
                            if (Registers.Keys.Contains(parts[3]))
                            {
                                Registers[parts[1]] = Convert.ToInt64(arrs[parts[2]][Convert.ToInt32(Registers[parts[3]])]);
                                break;
                            }

                            if (VarAddress.Keys.Contains(parts[3]))
                            {
                                Registers[parts[1]] = Convert.ToInt64(arrs[parts[2]][Convert.ToInt32(VarAddress[parts[3]])]);
                                break;
                            }
                            Registers[parts[1]] = Convert.ToInt64(arrs[parts[2]][Convert.ToInt32(parts[3])]);
                            break;
                        }

                        Registers[parts[1]] = Convert.ToInt64(parts[2]);
                        break;
                    }
                    else if (arrs.Keys.Contains(parts[2]) && arrs.Keys.Contains(parts[1]))
                    {
                        arrs[parts[1]] = arrs[parts[2]];
                        break;
                    }
                    {
                        if (!(parts[1].Length == 7 && parts[1][0] == '0' && parts[1][1] == 'x'))
                        {
                            STOP();
                            Console.WriteLine($"ERROR: Invalid Adress");
                        }

                        if (!VarAddress.Keys.Contains(parts[1])){ // проверяем, есть ли первый адрес
                            VarAddress.Add(parts[1], "0");
                        }

                        if (arrs.Keys.Contains(parts[2]))
                        {
                            VarAddress[parts[1]] = arrs[parts[2]][Convert.ToInt32(parts[3])];
                            break;
                        }

                        if (parts[2].Length == 7 && parts[2][0] == '0' && parts[2][1] == 'x')
                        {
                            if (!VarAddress.Keys.Contains(parts[2])){ // проверяем, есть ли второй адрес
                                VarAddress.Add(parts[2], "0");
                            }

                            VarAddress[parts[1]] = VarAddress[parts[2]];
                            break;
                        }

                        if (parts[2][0] == 'R')
                        {
                            VarAddress[parts[1]] = Registers[parts[2]].ToString();
                            break;
                        }

                        string answer = "";
                        foreach (string part in parts)
                        {
                            if (part == parts[0] || part == parts[1]) continue;

                            answer += part;
                            answer += " ";
                        }
                        VarAddress[parts[1]] = answer;
                        break;
                    }
                }

                case "A2": // ADD
                {
                    if (Registers.Keys.Contains(parts[1]))
                    {
                        if (Registers.Keys.Contains(parts[2]))
                        {
                            Registers[parts[1]] += Registers[parts[2]];
                            break; 
                        }
                        else
                        {
                            Registers[parts[1]] += Convert.ToInt64(parts[2]);
                            break;
                        }
                    }

                    if (arrs.Keys.Contains(parts[1]))
                    {
                        arrs[parts[1]].Add(parts[2]);
                        break;
                    }
                    break;
                    
                }

                case "S4": // SUB
                {
                    if (parts[2][0] == 'R')
                    {
                        Registers[parts[1]] -= Registers[parts[2]];
                        break; 
                    }
                    else
                    {
                        Registers[parts[1]] -= Convert.ToInt64(parts[2]);
                        break;
                    }
                }

                case "D6": // DIV
                {
                    if (parts[2][0] == 'R')
                    {
                        Registers[parts[1]] /= Registers[parts[2]];
                        break; 
                    }
                    else
                    {
                        Registers[parts[1]] /= Convert.ToInt64(parts[2]);
                        break;
                    }
                }

                case "M8": // MUL
                {
                    if (parts[2][0] == 'R')
                    {
                        Registers[parts[1]] *= Registers[parts[2]];
                        break; 
                    }
                    else
                    {
                        Registers[parts[1]] *= Convert.ToInt64(parts[2]);
                        break;
                    }
                }
                
                case "P2": // PRT
                {
                    if (parts[1][0] == 'R')
                    {
                        Console.Write(Convert.ToInt64(Registers[parts[1]]));
                        break;
                    }
                    if (VarAddress.Keys.Contains(parts[1]))
                    {
                        Console.Write(VarAddress[parts[1]]);
                        break;
                    }
                    if (arrs.Keys.Contains(parts[1]))
                    {
                        if (parts[2][0] != '[')
                        {
                            string temp = parts[2].Replace('[', ' ').Replace(']', ' ');
                            temp = temp.Replace(" ", "");

                            if (Registers.Keys.Contains(temp))
                            {
                                foreach (string t in arrs[parts[1]])
                                {
                                    if (t == Convert.ToString(Registers[temp]))
                                    {
                                        Console.Write(t);
                                    }
                                }
                                break;
                            }
                            if (temp.Length == 7)
                            {
                                if ((temp[0] == '0' && temp[1] == 'x') == false)
                                {
                                    break;
                                }
                                if (VarAddress.Keys.Contains(temp))
                                {
                                    foreach (string t in arrs[parts[1]])
                                    {
                                        if (VarAddress[temp] == t + " ")
                                        {
                                            Console.Write(t);
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    VarAddress.Add(parts[2], "0");
                                    foreach (string t in arrs[parts[1]])
                                    {
                                        if (t == VarAddress[temp])
                                        {
                                            Console.Write(t);
                                        }
                                    }
                                    break;
                                }
                            }
                            foreach (string t in arrs[parts[1]])
                            {
                                if (t == temp)
                                {
                                    Console.Write(t);
                                }
                            }
                            break;
                        }
                        else
                        {
                            if (Registers.Keys.Contains(parts[2]))
                            {
                                Console.Write(arrs[parts[1]][Convert.ToInt32(Registers[parts[2]])]);
                                break;
                            }
                            try
                            {
                                if (parts[2][0] == '0' && parts[2][1] == 'x' && parts[2].Length == 7)
                                {
                                    if (VarAddress.Keys.Contains(parts[2]))
                                    {
                                        Console.Write(arrs[parts[1]][Convert.ToInt32(VarAddress[parts[2]])]);
                                        break;
                                    }
                                    else
                                    {
                                        VarAddress.Add(parts[2], "0");
                                        Console.Write(arrs[parts[1]][Convert.ToInt32(VarAddress[parts[2]])]);
                                        break;
                                    }
                                }
                            }
                            catch{}
                            Console.Write(arrs[parts[1]][Convert.ToInt32(parts[2])]);
                            break;
                        }
                        
                    }
                    break;
                }


                case "J4": // JMP
                {
                    number = PointAddress[parts[1]];
                    continue;
                }


                case "I6": // IFZ
                {
                    if (Registers[parts[1]] != 0) break;

                    if (parts[2] == "H8")
                    {
                        STOP();
                        return;
                    }

                    number = PointAddress[parts[2]];
                    continue;
                }

                
                case "H8": // HLT
                {
                    STOP();
                    return;
                }


                case "S0": // SWP
                {
                    long temp = Registers[parts[1]];
                    Registers[parts[1]] = Registers[parts[2]];
                    Registers[parts[2]] = temp;
                    break;
                }

                case "W6": // WAIT
                {
                    try
                    {
                        parts[1] = parts[1];
                    }
                    catch
                    {
                        Thread.Sleep(1000);
                        break;
                    }

                    if (Registers.Keys.Contains(parts[1]))
                    {
                        Thread.Sleep((int)Registers[parts[1]]);
                        break;
                    }

                    Thread.Sleep(Convert.ToInt32(parts[1]));
                    break;
                }

                case "N8": // NXT
                {
                    Console.WriteLine();
                    break;
                }

                case "I0": // INP
                {
                    if (parts[1][0] == 'R')
                    {
                        Registers[parts[1]] = Convert.ToInt64(Console.ReadLine());
                        break;
                    }
                    break;
                }

                case "04": // REM
                {
                    if (parts[1] == "ALL")
                    {
                        VarAddress.Clear();
                        break;
                    }

                    if (arrs.Keys.Contains(parts[1]))
                    {
                        if (parts[2][0] == '[')
                        {
                            string num = parts[2].Replace('[', ' ').Replace(']', ' ');
                            num = num.Replace(" ", "");

                            if (num[0] == 'R')
                            {
                                arrs[parts[1]].RemoveAt(Convert.ToInt32(Registers[num]));
                                break;
                            }
                            try
                            {
                                if (num[0] == '0' && num[1] == 'x' && num.Length == 7)
                                {
                                    if (VarAddress.Keys.Contains(num))
                                    {
                                        arrs[parts[1]].RemoveAt(Convert.ToInt32(VarAddress[num]));
                                        break;
                                    }
                                    else
                                    {
                                        VarAddress.Add(num, "0");
                                        arrs[parts[1]].RemoveAt(Convert.ToInt32(VarAddress[num]));
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                arrs[parts[1]].RemoveAt(Convert.ToInt32(num));
                                break;
                            }
                            arrs[parts[1]].RemoveAt(Convert.ToInt32(num));
                            break;
                        }

                        else
                        {
                            if (parts[2][0] == 'R')
                            {
                                arrs[parts[1]].Remove(Registers[parts[2]].ToString());
                                break;
                            }
                            try
                            {
                                if (parts[2][0] == '0' && parts[2][1] == 'x' && parts[2].Length == 7)
                                {
                                    if (VarAddress.Keys.Contains(parts[2]))
                                    {
                                        arrs[parts[1]].Remove(VarAddress[parts[2]]);
                                        break;
                                    }
                                    else
                                    {
                                        VarAddress.Add(parts[2], "0");
                                        arrs[parts[1]].Remove(VarAddress[parts[2]]);
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                arrs[parts[1]].Remove(parts[2]);
                                break;
                            }
                            arrs[parts[1]].Remove(parts[2]);
                            break;
                        }
                    }

                    VarAddress.Remove(parts[1]);
                    break;
                }

                case "C6": // CLS
                {
                    Console.Clear();
                    break;
                }

                case "I8": // IFKEY
                {
                    if (currentKey == parts[1])
                    {
                        number = PointAddress[parts[2]];
                    }

                    break;
                }

                case "A8": // LIST
                {
                    try
                    {
                        arrs.Add(parts[1], []);
                        break;
                    }
                    catch
                    {
                        STOP();
                        Console.WriteLine($"ERROR Line {number + 1}: Massive {parts[1]} is already exist");
                        break;
                    }
                }

                case "G0": // LEN
                {
                    if (Registers.Keys.Contains(parts[1]))
                    {
                        Registers[parts[1]] = arrs[parts[2]].Count();
                        break;    
                    }
                    if (VarAddress.Keys.Contains(parts[1]))
                    {
                        VarAddress[parts[1]] = Convert.ToString(arrs[parts[2]].Count());
                        break;
                    }
                    else
                    {
                        if (parts[1].Length == 7 && parts[1][1] == 'x' && parts[1][0] == '0')
                        {
                            VarAddress.Add(parts[1], Convert.ToString(arrs[parts[2]].Count()));
                            break;
                        }
                    }
                    
                    break;
                }

            }
        number++;
        }
    }

    private static void STOP()
    {
        Active = false;
    }

}