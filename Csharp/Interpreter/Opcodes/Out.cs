using static Parser;
using static Executer;
using System.Text;

struct Out{ 

    static int xnum;
    static int ynum;
    public static void Execute(){ 
        
        if (nameVars.Contains(value)){
            ExecuteShowVar();
            return;
        }

        if (!int.TryParse(parts[^1], out xnum)){
            Errors.Print(0x02); 
            return;
        }

        Console.Write(Unescape(value));

        while (xnum > 0){
            Console.Write('\n');
            xnum--;
        }
    }

    public static void ExecuteShowVar(){
        // Строковая переменная (ds) — выводим напрямую
        if (stringVars.ContainsKey(value)){
            Console.Write(value: stringVars[value]);
            return;
        }

        // Числовые переменные — выводим значение
        if (byteVars.ContainsKey(value)){   Console.Write(byteVars[value]);   return; }
        if (shortVars.ContainsKey(value)){  Console.Write(shortVars[value]);  return; }
        if (floatVars.ContainsKey(value)){  Console.Write(floatVars[value]);  return; }
        if (doubleVars.ContainsKey(value)){ Console.Write(doubleVars[value]); return; }

        // Массивы и матрицы
        ExecuteShowArr();
    }

    public static void ExecuteShowArr(){ // если на вывод дается массив или матрица
        StringBuilder txt = new StringBuilder(10000);

        string n1 = parts[^1].Replace('*', ' ').Split()[0];

        if (Matrix2_s.ContainsKey(value) || Matrix2_q.ContainsKey(value)){
            string[] mparts = parts[^1].Replace('*', ' ').Split();
            string n2 = mparts.Length > 1 ? mparts[1] : "0";
            if (!int.TryParse(n2, out ynum)){
                Errors.Print(0x02); 
                return;
            }
        }
        
        if (!int.TryParse(n1, out xnum)){
            Errors.Print(0x02); 
            return;
        }

        if (byteArrs.ContainsKey(value)){
            int tempx = xnum;
            foreach (byte b in byteArrs[value]){
                xnum = tempx;
                txt.Append(b);
                while (xnum > 0){ txt.Append(' '); xnum--; }
            }
        } else if (shortArrs.ContainsKey(value)){
            int tempx = xnum;
            foreach (short s in shortArrs[value]){
                xnum = tempx;
                txt.Append(s);
                while (xnum > 0){ txt.Append(' '); xnum--; }
            }
        } else if (floatArrs.ContainsKey(value)){
            int tempx = xnum;
            foreach (float f in floatArrs[value]){
                xnum = tempx;
                txt.Append(f);
                while (xnum > 0){ txt.Append(' '); xnum--; }
            }
        } else if (doubleArrs.ContainsKey(value)){
            int tempx = xnum;
            foreach (double d in doubleArrs[value]){
                xnum = tempx;
                txt.Append(d);
                while (xnum > 0){ txt.Append(' '); xnum--; }
            }
        } else if (stringArrs.ContainsKey(value)){
            int tempx = xnum;
            foreach (string s in stringArrs[value]){
                xnum = tempx;
                txt.Append(s);
                while (xnum > 0){ txt.Append(' '); xnum--; }
            }
        } else if (Matrix2_q.ContainsKey(value)){
            int savexnum = xnum;
            int saveynum = ynum;
            for (int y = 0; y < Matrix2_q[value].GetLength(0); y++){
                for (int x = 0; x < Matrix2_q[value].GetLength(1); x++){
                    txt.Append(Matrix2_q[value][y, x]);
                    while (xnum > 0){ txt.Append(' '); xnum--; }
                    xnum = savexnum;
                } while (ynum > 0){ txt.Append('\n'); ynum--; } ynum = saveynum; 
            }
        } else if (Matrix2_s.ContainsKey(value)){
            int savexnum = xnum;
            int saveynum = ynum;
            for (int y = 0; y < Matrix2_s[value].GetLength(0); y++){
                for (int x = 0; x < Matrix2_s[value].GetLength(1); x++){
                    txt.Append(Matrix2_s[value][y, x]);
                    while (xnum > 0){ txt.Append(' '); xnum--; }
                    xnum = savexnum;
                } while (ynum > 0){ txt.Append('\n'); ynum--; } ynum = saveynum;
            }
        }

        Console.Write(txt);
    }

    private static string Unescape(string s){
        StringBuilder sb = new StringBuilder(s.Length);
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
