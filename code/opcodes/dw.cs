using static PC.Computer;
using static Interpreter;
#pragma warning disable CS8981
struct dw{
    public static void run(){
        
        if(!CheckArgument.Check(2)){
            Errors.Print(0x02);
            return;
        }
                    
        RAM += 2;
        if (RAM >= maxRAM)
            KillProcessRAM();

        if (varsNames.Contains(parts[1])){
            isWarn = true;
            Console.WriteLine($"\nLine {num + 1} Error: Redefinition of symbol");
            return;
        }

        try {
            varsShort.Add(parts[1], Convert.ToInt16(parts[2]));
        } catch {
            Console.WriteLine($"\nLine {num + 1} Error: Segmentation fault");
            isWarn = true;
            return;
        }

        varsNames.Add(parts[1]);
        num++;
    }
}