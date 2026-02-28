; Пример веб-сервера с маршрутизацией из файлов
; Структура папки:
;   www/
;     index.html   — открывается при GET /
;     about.html   — открывается при GET /about
;     style.css    — открывается при GET /style.css

__start:
    go main

.p main:
    ; httpserve "port" "webroot_folder"
    httpserve "8080" "www"

__stop:
    clear registres
