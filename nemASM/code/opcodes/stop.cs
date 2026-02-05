using static PC.Computer;
using static Interpreter;
#pragma warning disable CS8981
struct stop{
    public static void run(){
        isStop = true;
        num++;
    }
}