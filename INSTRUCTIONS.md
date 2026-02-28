# NativeExecuteMachine — Справочник инструкций

## Типы данных

| Инструкция | Тип       | Размер | Пример                        |
|------------|-----------|--------|-------------------------------|
| `db`       | byte      | 1 байт | `db x, 255`                   |
| `dw`       | short     | 2 байта| `dw x, 1000`                  |
| `dd`       | float     | 4 байта| `dd x, 3.14`                  |
| `dq`       | double    | 8 байт | `dq x, 3.14159`               |
| `ds`       | string    | —      | `ds x, "hello"`               |
| `arrb`     | byte[]    | —      | `arrb x, 10`                  |
| `arrw`     | short[]   | —      | `arrw x, 10`                  |
| `arrd`     | float[]   | —      | `arrd x, 10`                  |
| `arrq`     | double[]  | —      | `arrq x, 10`                  |
| `arrs`     | string[]  | —      | `arrs x, 10`                  |
| `mx2_s`    | string[,] | —      | `mx2_s x, 4*4`                |
| `mx2_q`    | double[,] | —      | `mx2_q x, 4*4`                |
| `vec2`     | Vector2   | —      | `vec2 v, 1.0 2.0`             |
| `vec3`     | Vector3   | —      | `vec3 v, 1.0 2.0 3.0`         |
| `vec4`     | Vector4   | —      | `vec4 v, 1.0 2.0 3.0 4.0`     |

---

## Арифметика и присваивание

| Инструкция           | Описание                                         |
|----------------------|--------------------------------------------------|
| `mov arg1, arg2`     | Записать значение arg2 в arg1                    |
| `add arg1, arg2`     | arg1 = arg1 + arg2                               |
| `sub arg1, arg2`     | arg1 = arg1 - arg2                               |
| `mul arg1, arg2`     | arg1 = arg1 * arg2                               |
| `div arg1, arg2`     | arg1 = arg1 / arg2                               |
| `sin arg1, arg2`     | arg1 = sin(arg2)                                 |
| `cos arg1, arg2`     | arg1 = cos(arg2)                                 |
| `tan arg1, arg2`     | arg1 = tan(arg2)                                 |
| `rand arg1, arg2`    | arg1 = случайное число от 0 до arg2              |
| `max arg1, arg2`     | arg1 = максимальный элемент массива arg2         |
| `min arg1, arg2`     | arg1 = минимальный элемент массива arg2          |
| `srt arg1`           | Сортировать массив arg1                          |
| `rss arg1`           | Перемешать массив arg1 случайно                  |
| `len arg1, arg2`     | arg1 = длина массива arg2                        |
| `dst arg1, arg2`     | arg1 = расстояние между двумя векторами          |
| `lng arg1, arg2`     | arg1 = длина вектора arg2                        |
| `lngsq arg1, arg2`   | arg1 = длина вектора arg2 в квадрате             |

---

## Ввод / Вывод

| Инструкция           | Описание                                                  |
|----------------------|-----------------------------------------------------------|
| `out "текст", N`     | Вывести текст и N переносов строки                        |
| `out var N`          | Вывести переменную/массив и N переносов строки            |
| `inp var`            | Прочитать ввод с клавиатуры в переменную                  |
| `clear var`          | Очистить переменную / массив / регистры                   |

Escape-последовательности в строках: `\n` (перенос), `\t` (табуляция), `\r`, `\\`, `\'`

---

## Управление потоком

| Инструкция           | Описание                                                  |
|----------------------|-----------------------------------------------------------|
| `go block`           | Перейти к блоку `.p block:`                               |
| `call block`         | Вызвать блок как функцию (с возвратом через `ret`)        |
| `ret`                | Вернуться из вызванного блока                             |
| `hlt`                | Перейти к `__stop:`                                       |
| `cmp arg1, arg2`     | Сравнить arg1 и arg2 (результат в флагах isHigh/isEqual)  |
| `ife block`          | Перейти к block если последнее сравнение: равно           |
| `ifn block`          | Перейти к block если последнее сравнение: не равно        |
| `ifh block`          | Перейти к block если последнее сравнение: больше          |
| `ifl block`          | Перейти к block если последнее сравнение: меньше          |

---

## Стек

| Инструкция   | Описание                              |
|--------------|---------------------------------------|
| `push var`   | Положить переменную на стек           |
| `pop var`    | Снять значение со стека в переменную  |
| `pusha`      | Сохранить все регистры на стек        |
| `popa`       | Восстановить все регистры со стека    |

---

## Файловые инструкции *(новые)*

| Инструкция              | Описание                                                        |
|-------------------------|-----------------------------------------------------------------|
| `fwrite "path" var`     | Записать переменную в файл (перезапись)                        |
| `fapp "path" var`       | Дописать переменную в конец файла на новой строке              |
| `fread var "path"`      | Прочитать файл целиком в строковую переменную (`ds`)           |

```asm
ds line1 "Hello, world!"
ds line2 "Second line"
fwrite "output.txt" line1   ; файл: Hello, world!
fapp   "output.txt" line2   ; файл: Hello, world!\nSecond line

ds content "                    "
fread content "output.txt"
out content 1
```

---

## Сетевые инструкции *(новые)*

| Инструкция                  | Описание                                                             |
|-----------------------------|----------------------------------------------------------------------|
| `httpget var "url"`         | GET запрос по url, ответ записывается в переменную (`ds`)           |
| `httppost var "url"`        | POST запрос с телом из var (JSON), ответ записывается обратно в var |
| `httpserve "port" "folder"` | Запустить HTTP сервер на порту, отдавать файлы из папки             |

### httpserve — маршрутизация

`httpserve` читает HTML/CSS/JS файлы из указанной папки:

| URL запроса   | Файл                  |
|---------------|-----------------------|
| `/`           | `folder/index.html`   |
| `/about`      | `folder/about.html`   |
| `/blog/post`  | `folder/blog/post.html` |
| `/style.css`  | `folder/style.css`    |
| `/logo.png`   | `folder/logo.png`     |

Поддерживаются: `.html`, `.css`, `.js`, `.json`, `.png`, `.jpg`, `.gif`, `.svg`, `.ico`, `.txt`

```asm
; Структура папки www/:
;   www/index.html
;   www/about.html
;   www/style.css

__start:
    go main
.p main:
    httpserve "8080" "www"
__stop:
    clear registres
```

```asm
; GET запрос
ds resp "                    "
httpget resp "https://api.example.com/data"
out resp 1

; POST запрос
ds body "{\"key\":\"value\"}"
httppost body "https://api.example.com/post"
out body 1
```

---

## Структура программы

```asm
__start:
    go main          ; точка входа

.p main:             ; блок main
    ; ... код ...
    call myFunc      ; вызов функции

.p myFunc:
    ; ... код ...
    ret              ; возврат

__stop:              ; блок завершения
    clear registres
```
