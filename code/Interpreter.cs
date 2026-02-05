using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualBasic;
using PC;
using static PC.Computer;

public class Interpreter
{
    public static bool isWarn = false; // если true то инструкция завершилась с ошибкой.
    public static string[] systemArguments = ["0", "0"]; // one, two.
    public static string[] parts = []; // список в котором будут части линии инструкций
    public static int num = 0; // номер текущей строки
    public static int stackAddress = -1; // стэк для запоминания адреса
    public static bool isStop = false; // мы находимся в блоке стоп?
    public static StringBuilder txt = new StringBuilder();
    // строка строитель, в которой мы будем хранить текстовые данные на время выполнения кода.

    public static Dictionary<int, string> codeParts = new Dictionary<int, string>();
    // библиотека в которой хранится ключ: адрес и значение это линия кода.

    public static Dictionary<string, int> blocks = new Dictionary<string, int>();
    // библиотека в которой хранятся адреса блоков

    public static HashSet<string> varsNames = new HashSet<string>(); // список с названиями переменных в адресе
    public static Dictionary<string, byte> varsByte = new Dictionary<string, byte>(); // библиотека с переменными байт
    public static Dictionary<string, short> varsShort = new Dictionary<string, short>(); // библиотека с переменными 2 байт
    public static Dictionary<string, float> varsFloat = new Dictionary<string, float>(); // библиотека с переменными 4 байт
    public static Dictionary<string, double> varsDouble = new Dictionary<string, double>(); // библиотека с переменными 8 байт
    public static Dictionary<string, string> varsString = new Dictionary<string, string>(); // библиотека с переменными стринг

    public static void Run(){
        Clear(); // очищаем мусор
        FillCodeParts(); // заполняем список с кодом
        Interpetation(); // интерпретация

        Console.WriteLine();
    }

    public static void Clear(){
        codeParts.Clear();
        txt.Clear();
        blocks.Clear();
        isStop = false;
        num = 0;
        parts = [];
        isWarn = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void FillCodeParts(){
        string[] lines = File.ReadAllLines(Terminal.path);
        foreach (string line in lines){

            switch (line.Trim().Split()[0]){
                case ".block":{
                    blocks.Add(line.Trim().Split()[1], num + 1);
                    break;
                }
                case "stop:":{
                    blocks.Add("stop:", num);
                    break;
                }
                case "start:":{
                    blocks.Add("start:", num);
                    break;
                }
            }

            codeParts.Add(num, line.Trim());
            num++; 
        }

        num = blocks["start:"];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Interpetation(){

        while (true){
            if (isWarn){
                num = blocks["stop:"];
                isWarn = false;
            } 

            parts = codeParts[num].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            try {
                parts[0] = parts[0];
            } catch {
                num++;
                continue;
            }

            switch (parts[0]){
                case "start:":{
                    start.run();
                    break;
                }
                case "stop:":{
                    stop.run();
                    break;
                }
                case "end":{
                    if (isStop) return; // если сейчас блок stop
                    end.run();
                    break;
                }
                case "mov":{ // вставить
                    mov.run();
                    break;
                }
                case "go":{ // перейти
                    go.run();
                    continue;
                }
                case "out":{ // вывести
                    _out.run();
                    break;            
                }

                case "next":{ // переход на следующую строку
                    next.run();
                    break;
                }

                case "clear":{ // инструкция для очистки ячейки или адреса
                    clear.run();
                    break;
                }

                case "inp":{ // запрашиваем ввод
                    inp.run();
                    break;
                }

                case "wait":{ // ожидание
                    wait.run();
                    break;
                }

                case "add":{ // прибавить
                    add.run();
                    break; 
                }

                case "sub":{ // убавить
                    sub.run();
                    break;
                }

                case "mul":{ // умножить
                    mul.run();
                    break;
                }

                case "div":{ // поделить
                    div.run();
                    break;
                }
                
                case "call":{ // вызвать
                    call.run();
                    break;
                }

                case "ret":{ // вернуться
                    ret.run();
                    break;
                } 

                case "db":{ // создать ячейку для байт
                    db.run();
                    break;
                }

                case "dw":{ // создать ячейку для шортс
                    dw.run();
                    break;
                }

                case "dd":{ // создать ячейку для флот
                    dd.run();
                    break;
                }

                case "dq":{ // создать ячейку для дабл
                    dq.run();
                    break;
                }

                case "ds":{ // создать ячейку для строк
                    ds.run();
                    break;
                }

                case "cmp":{ // сравнить два значения
                    cmp.run();
                    break;
                } 

                case "ife":{ // если равны ==
                    ife.run();
                    break;
                }

                case "ifn":{ // если не равны !=
                    ifn.run();
                    break;
                }

                case "ifg":{ // если больше >
                    ifg.run();
                    break;
                }

                case "ifl":{ // если меньше <
                    ifl.run();
                    break;
                }

                default:{
                    Errors.Print(0x00); 
                    isWarn = true;
                    break;
                }
            } 
        }
    }

    public static string CheckVarName(string part){ // Проверить к какому типу относится переменная
        if (varsString.Keys.Contains(part)){
            return "string";
        } else if (varsShort.Keys.Contains(part)){
            return "short";
        } else if (varsFloat.Keys.Contains(part)){          
            return "float";
        } else if (varsDouble.Keys.Contains(part)){
            return "double";
        } else if (varsByte.Keys.Contains(part)){
            return "byte";
        } else {
            return "none";
        }
    }

    public static bool CheckVarContain(string part){ // Проверить к какому типу относится переменная
        if (!varsString.Keys.Contains(part) && !varsShort.Keys.Contains(part) && !varsFloat.Keys.Contains(part) && !varsDouble.Keys.Contains(part) && !varsByte.Keys.Contains(part)){
            return false;
        }
        return true;
    }
}