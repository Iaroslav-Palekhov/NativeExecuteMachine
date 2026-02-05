using static PC.Computer;
using static Interpreter;
#pragma warning disable CS8981
struct call{
    public static void run(){

        if(!CheckArgument.Check(1)){
            Errors.Print(0x02);
            return;
        }

        try {
            stackAddress = num + 1;
            num = blocks[parts[1] + ":"];
        } catch {
            Errors.Print(0x03);
            return;
        }
    }
}