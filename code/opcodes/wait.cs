using static PC.Computer;
using static Interpreter;
#pragma warning disable CS8981
struct wait{
    public static void run(){

        if(!CheckArgument.Check(1)){
            Errors.Print(0x02);
            return;
        }

        Thread.Sleep(int.Parse(parts[1]));
        num++;
    }
}