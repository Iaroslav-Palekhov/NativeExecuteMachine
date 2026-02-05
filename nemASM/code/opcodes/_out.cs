using static PC.Computer;
using static Interpreter;
struct _out{
    public static void run(){

        if(!CheckArgument.Check(1)){
            Errors.Print(0x02);
            return;
        }
        

        if (registres.Keys.Contains(parts[1])){ // если на вывод дается регистр
            Console.Write(registres[parts[1]]);
            num++;
            return;
        } 

        switch (CheckVarName(parts[1])){
            case "string":{
                Console.Write(varsString[parts[1]]);
                num++;
                return;
            }
            case "byte":{
                Console.Write(varsByte[parts[1]]);
                num++;
                return;
            }
            case "short":{
                Console.Write(varsShort[parts[1]]);
                num++;
                return;
            }
            case "float":{
                Console.Write(varsFloat[parts[1]]);
                num++;
                return;
            }
            case "double":{
                Console.Write(varsDouble[parts[1]]);
                num++;
                return;    
            }
            case "none":{
                break;
            }
        }

        if (parts[1][0] == '"'){ // если на вывод дается готовый текст
            int num2 = 0;
            try {
                txt.Clear();
                while (codeParts[num][num2] != '"'){
                    num2++;
                }
                num2++;
                while (codeParts[num][num2] != '"'){
                    txt.Append(codeParts[num][num2]);
                    num2++;
                }
            Console.Write(txt);
            txt.Clear();
            } catch {
                Errors.Print(0x01);
                return;
            }

            Console.Write(txt);
            txt.Clear();
            num++;
            return;    
        } else {
            isWarn = true;
            Errors.Print(0x02);
            return;    
        }        
    }
}