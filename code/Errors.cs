using static Interpreter;
struct Errors{
    
    public static void Print(byte errcode){

        isWarn = true;

        switch (errcode){
            case 0x00:{ // instruction not found
                Console.WriteLine($"Line {num + 1}. Error 0x00: Instruction <{parts[0]}> not found");
                break;
            }
            case 0x01:{
                Console.Write($"\nLine {num + 1}. Error 0x01: Address {parts[1]} is not exist");
                break;
            }
            case 0x02:{
                Console.Write($"\nLine {num + 1}. Error 0x02: Incorrect number of arguments"); 
                break;
            }
            case 0x03:{
                Console.Write($"\nLine {num + 1} Error 0x03: Incorrect block name");
                break;
            }
            case 0x04:{
                Console.WriteLine($"\nLine {num + 1} Error 0x04: Icorrect arguments");
                break;
            }
            case 0x05:{
                Console.WriteLine($"\nLine {num + 1} Error 0x05: Redefinition of symbol");
                break;
            }
            case 0x06:{
                Console.WriteLine($"\nSegmentation fault");
                break;
            }
            case 0x07:{
                Console.WriteLine($"\nLine {num + 1} Error 0x07: Typing error");
                break;
            }
            case 0x08:{
                Console.Write($"\nLine {num + 1} Error 0x08: Incorrect name address!");
                break;
            }
        }
    }
}