; ===== ФАЙЛОВЫЕ ИНСТРУКЦИИ =====
; fwrite "path" var   — записать в файл (перезапись)
; fapp   "path" var   — дописать на новой строке в конец файла
; fread  var "path"   — прочитать файл в переменную (ds)

__start:
    go main

.p main:
    ds line1 "Hello, world!"
    ds line2 "Second line"
    fwrite "output.txt" line1
    fapp   "output.txt" line2
    ds content "                    "
    fread content "output.txt"
    out content 1

__stop:
    clear registres
