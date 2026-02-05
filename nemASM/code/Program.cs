using System.Runtime.CompilerServices;
using PC;

struct Program
{

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static void Main(){
        Computer.Start();
        Terminal.Start();
    }

}