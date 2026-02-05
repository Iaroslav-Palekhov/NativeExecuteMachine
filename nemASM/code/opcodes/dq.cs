using static PC.Computer;
using static Interpreter;
#pragma warning disable CS8981
struct dq{
    public static void run(){
        
        if(!CheckArgument.Check(2)){
            Errors.Print(0x02);
            return;
        }
                    
        RAM += 8;
        if (RAM >= maxRAM)
            KillProcessRAM();

        if (varsNames.Contains(parts[1])){
            isWarn = true;
            Console.WriteLine($"\nLine {num + 1} Error: Redefinition of symbol");
            return;
        }

        try {
            varsDouble.Add(parts[1], Convert.ToDouble(parts[2]));
        } catch {
            Console.WriteLine($"\nLine {num + 1} Error: Segmentation fault");
            isWarn = true;
            return;
        }

        varsNames.Add(parts[1]);
        num++;
    }
}