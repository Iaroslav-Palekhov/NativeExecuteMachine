using static Parser;
using static Init;
using static System.Convert;
using static Types;
using static Executer;

struct CreateVar{ 
    public static void Execute(Types t_var){ // создание переменной

        nameVars.Add(nameArg1); // добавляем в список имен переменных

        switch (t_var){ // проверяем тип переменной и меняем
            case _byte:{ 
                byteVars.Add(nameArg1, ToByte(value)); 
                RAM++; 
                return;
            }
            case _short:{ 
                shortVars.Add(nameArg1, ToInt16(value)); 
                RAM += 2; 
                return;
            }
            case _float:{ 
                floatVars.Add(nameArg1, ToSingle(value)); 
                RAM += 4; 
                return;
            }
            case _double:{ 
                doubleVars.Add(nameArg1, ToDouble(value)); 
                RAM += 8; 
                return;
            }
            case _string:{
                string s = Unescape(value);
                stringVars.Add(nameArg1, s); 
                RAM += s.Length; 
                return;
            }
        }
    }

    // Конвертируем \n \t \r \\ в реальные символы
    private static string Unescape(string s){
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length){
            if (s[i] == '\\' && i + 1 < s.Length){
                switch (s[i + 1]){
                    case 'n':  sb.Append('\n'); i += 2; continue;
                    case 't':  sb.Append('\t'); i += 2; continue;
                    case 'r':  sb.Append('\r'); i += 2; continue;
                    case '\\': sb.Append('\\'); i += 2; continue;
                    case '\'': sb.Append('\''); i += 2; continue;
                    case '0':  sb.Append('\0'); i += 2; continue;
                }
            }
            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }
}
