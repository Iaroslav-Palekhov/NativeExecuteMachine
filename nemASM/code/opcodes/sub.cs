using static PC.Computer;
using static Interpreter;
#pragma warning disable CS8981
struct sub{

    public static void run(){

        if(!CheckArgument.Check(2)){
            Errors.Print(0x02);
            return;
        }

        try {
        if (registres.Keys.Contains(parts[1])){ // если первый аргумент это регистр
            if (registres.Keys.Contains(parts[2])){ // если второй аргумент регистр
                registres[parts[1]] -= registres[parts[2]];
                num++;
                return;
            } else { // если второй аргумент это переменнная
                switch (CheckVarName(parts[2])){
                    case "string":{
                        isWarn = true;
                        Console.Write($"\nLine {num + 1} Error - String and registres?");
                        return;   
                    }
                    case "byte":{
                        registres[parts[1]] -= Convert.ToDouble(varsByte[parts[2]]);
                        num++;
                        return;
                    }
                    case "short":{
                        registres[parts[1]] -= Convert.ToDouble(varsShort[parts[2]]);
                        num++;
                        return;
                    }
                    case "float":{
                        registres[parts[1]] -= Convert.ToDouble(varsFloat[parts[2]]);
                        num++;
                        return;
                    }
                    case "double":{
                        registres[parts[1]] -= varsDouble[parts[2]];
                        num++;
                        return;
                    }
                }
            }
        } else if (CheckVarContain(parts[1])){ // если первый аргумент это переменная
            switch (CheckVarName(parts[1])){
                case "string":{ // если первая переменная стринг
                    Console.Write($"\nLine {num + 1} Error - String and registres?");
                    isWarn = true;
                    return;
                }
                case "byte":{
                    if (registres.Keys.Contains(parts[2])){ // если второй аргумент регистр  
                        varsByte[parts[1]] -= Convert.ToByte(registres[parts[2]]);
                        num++;
                        return;
                    } else if (CheckVarContain(parts[2])){ // если второй аргумент переменная
                        varsByte[parts[1]] -= varsByte[parts[2]];
                        num++;
                        return;
                    } else { // если второй аргумент это готовое значение
                        varsByte[parts[1]] -= Convert.ToByte(parts[2]);
                        num++;
                        return;
                    }
                }
                case "short":{
                    if (registres.Keys.Contains(parts[2])){ // если второй аргумент регистр  
                        varsShort[parts[1]] -= Convert.ToInt16(registres[parts[2]]);
                        num++;
                        return;
                    } else if (CheckVarContain(parts[2])){ // если второй аргумент переменная
                        varsShort[parts[1]] -= varsShort[parts[2]];
                        num++;
                        return;
                    } else { // если второй аргумент это готовое значение
                        varsShort[parts[1]] -= Convert.ToInt16(parts[2]);
                        num++;
                        return;
                    }
                }
                case "float":{
                    if (registres.Keys.Contains(parts[2])){ // если второй аргумент регистр  
                        varsFloat[parts[1]] -= Convert.ToSingle(registres[parts[2]]);
                        num++;
                        return;
                    } else if (CheckVarContain(parts[2])){ // если второй аргумент переменная
                        varsFloat[parts[1]] -= varsFloat[parts[2]];
                        num++;
                        return;
                    } else { // если второй аргумент это готовое значение
                        varsFloat[parts[1]] -= Convert.ToSingle(parts[2]);
                        num++;
                        return;
                    }
                }
                case "double":{
                    if (registres.Keys.Contains(parts[2])){ // если второй аргумент регистр  
                        varsDouble[parts[1]] -= Convert.ToDouble(registres[parts[2]]);
                        num++;
                        return;
                    } else if (CheckVarContain(parts[2])){ // если второй аргумент переменная
                        varsDouble[parts[1]] -= varsDouble[parts[2]];
                        num++;
                        return;
                    } else { // если второй аргумент это готовое значение
                        varsDouble[parts[1]] -= Convert.ToDouble(parts[2]);
                        num++;
                        return;
                    }
                }
            }
        }
        
        registres[parts[1]] -= double.Parse(parts[2]);
        num++;
        return;

        } catch {
            isWarn = true;
            Console.Write($"\nLine {num + 1} Error - Incorrect address!"); 
            return;  
        }
    }
}