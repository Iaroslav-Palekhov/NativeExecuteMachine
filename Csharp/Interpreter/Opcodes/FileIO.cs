//
//  27.02.2026 Добавлено разработчиком Iaroslav-Palekhov
//  Обновлено: AOT-совместимая сериализация (без рефлексии)
//

using static Parser;
using static Executer;
using static Types;
using System.Net;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

struct FileIO{

    public static void ExecuteWrite(bool append){
        string filePath = nameArg1 ?? "";
        string content  = GetVarContent(value);
        try {
            if (append){
                string existing = File.Exists(filePath) ? File.ReadAllText(filePath) : "";
                string separator = existing.Length > 0 ? System.Environment.NewLine : "";
                File.WriteAllText(filePath, existing + separator + content);
            } else {
                File.WriteAllText(filePath, content);
            }
        } catch (Exception ex){
            Console.Error.WriteLine($"[fwrite/fapp] {ex.Message}");
        }
    }

    public static void ExecuteRead(){
        string varName  = nameArg1 ?? "";
        string filePath = value ?? "";
        if (!stringVars.ContainsKey(varName)){
            Console.Error.WriteLine($"[fread] переменная \"{varName}\" не найдена (нужен тип ds).");
            return;
        }
        try {
            string content = File.ReadAllText(filePath);
            Init.RAM -= stringVars[varName].Length;
            stringVars[varName] = content;
            Init.RAM += content.Length;
        } catch (Exception ex){
            Console.Error.WriteLine($"[fread] {ex.Message}");
        }
    }

    public static void ExecuteHttpGet(){
        string varName = nameArg1 ?? "";
        string url     = value ?? "";
        if (!stringVars.ContainsKey(varName)){
            Console.Error.WriteLine($"[httpget] переменная \"{varName}\" не найдена (нужен тип ds).");
            return;
        }
        try {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            string response = client.GetStringAsync(url).GetAwaiter().GetResult();
            Init.RAM -= stringVars[varName].Length;
            stringVars[varName] = response;
            Init.RAM += response.Length;
        } catch (Exception ex){
            Console.Error.WriteLine($"[httpget] {ex.Message}");
        }
    }

    public static void ExecuteHttpPost(){
        string varName = nameArg1 ?? "";
        string url     = value ?? "";
        if (!stringVars.ContainsKey(varName)){
            Console.Error.WriteLine($"[httppost] переменная \"{varName}\" не найдена (нужен тип ds).");
            return;
        }
        try {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            var body = new StringContent(stringVars[varName], Encoding.UTF8, "application/json");
            var resp = client.PostAsync(url, body).GetAwaiter().GetResult();
            string response = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Init.RAM -= stringVars[varName].Length;
            stringVars[varName] = response;
            Init.RAM += response.Length;
        } catch (Exception ex){
            Console.Error.WriteLine($"[httppost] {ex.Message}");
        }
    }

    // httpserve "port" "webroot_dir"
    public static void ExecuteHttpServe(){
        string portStr = nameArg1 ?? "8080";
        string webRoot = GetVarContent(value).Trim();
        if (webRoot == "") webRoot = ".";

        string dbPath = Path.Combine(webRoot, "db.json");

        if (!File.Exists(dbPath)){
            File.WriteAllText(dbPath, "{\"users\":[]}");
            Console.WriteLine($"[httpserve] Создан db.json: {dbPath}");
        }

        try {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{portStr}/");
            listener.Start();
            Console.WriteLine($"[httpserve] Сервер запущен на порту {portStr}, папка: {webRoot}");
            Console.WriteLine($"[httpserve] Открой http://localhost:{portStr}/  (Ctrl+C для остановки)");

            while (true){
                var ctx = listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx, webRoot, dbPath));
            }
        } catch (Exception ex){
            Console.Error.WriteLine($"[httpserve] {ex.Message}");
        }
    }

    private static void HandleRequest(HttpListenerContext ctx, string webRoot, string dbPath){
        var req = ctx.Request;
        var res = ctx.Response;

        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        string urlPath = req.Url?.AbsolutePath ?? "/";
        string method  = req.HttpMethod.ToUpper();

        Console.WriteLine($"[httpserve] {method} {urlPath}");

        if (method == "OPTIONS"){
            res.StatusCode = 204;
            res.OutputStream.Close();
            return;
        }

        if (method == "POST" && urlPath == "/api/register"){ HandleRegister(req, res, dbPath); return; }
        if (method == "POST" && urlPath == "/api/login")   { HandleLogin(req, res, dbPath);    return; }
        if (method == "GET"  && urlPath == "/api/users")   { HandleGetUsers(res, dbPath);       return; }

        // Decode URL
        string decodedPath = Uri.UnescapeDataString(urlPath);
        string relativePath = decodedPath.TrimStart('/');
        string fsPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));

        // Security: prevent path traversal
        string rootFull = Path.GetFullPath(webRoot);
        if (!fsPath.StartsWith(rootFull)){
            byte[] buf = Encoding.UTF8.GetBytes("<html><body><h1>403 Forbidden</h1></body></html>");
            res.StatusCode = 403; res.ContentType = "text/html; charset=utf-8";
            res.ContentLength64 = buf.Length; res.OutputStream.Write(buf, 0, buf.Length);
            res.OutputStream.Close(); return;
        }

        // Directory listing
        if (Directory.Exists(fsPath)){
            // Try index.html first
            string indexFile = Path.Combine(fsPath, "index.html");
            if (File.Exists(indexFile) && decodedPath == "/"){
                ServeFile(res, indexFile, "index.html", req); return;
            }
            ServeDirectoryListing(res, fsPath, rootFull, decodedPath);
            return;
        }

        // Static file
        if (File.Exists(fsPath)){
            ServeFile(res, fsPath, Path.GetFileName(fsPath), req);
            return;
        }

        // Try adding .html
        string htmlPath = fsPath + ".html";
        if (File.Exists(htmlPath)){
            ServeFile(res, htmlPath, Path.GetFileName(htmlPath), req);
            return;
        }

        byte[] notFound = Encoding.UTF8.GetBytes($"<html><body><h1>404 Not Found</h1><p>{decodedPath}</p></body></html>");
        res.StatusCode = 404; res.ContentType = "text/html; charset=utf-8";
        res.ContentLength64 = notFound.Length; res.OutputStream.Write(notFound, 0, notFound.Length);
        res.OutputStream.Close();
        Console.WriteLine($"[httpserve] 404 {urlPath}");
    }

    private static void ServeFile(HttpListenerResponse res, string filePath, string fileName, HttpListenerRequest? req = null){
        try {
            long fileSize   = new FileInfo(filePath).Length;
            string ctype    = GetContentType(fileName);
            const int CHUNK = 262144; // 256 KB

            // Parse Range header (needed for video/audio seeking in browser)
            string? rangeHeader = req?.Headers["Range"];
            long rangeStart = 0;
            long rangeEnd   = fileSize - 1;
            bool isRange    = false;

            if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes=")){
                string rv       = rangeHeader.Substring(6);
                string[] parts  = rv.Split('-');
                if (parts.Length == 2){
                    if (long.TryParse(parts[0], out long rs)) rangeStart = rs;
                    if (!string.IsNullOrEmpty(parts[1]) && long.TryParse(parts[1], out long re)) rangeEnd = re;
                    isRange = true;
                }
            }

            long sendLength = rangeEnd - rangeStart + 1;

            res.ContentType = ctype;
            res.Headers.Add("Accept-Ranges", "bytes");
            res.Headers.Add("Cache-Control", "public, max-age=3600");

            if (isRange){
                res.StatusCode      = 206;
                res.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/{fileSize}");
                res.ContentLength64 = sendLength;
                Console.WriteLine($"[httpserve] 206 {rangeStart}-{rangeEnd}/{fileSize} {fileName}");
            } else {
                res.StatusCode      = 200;
                res.ContentLength64 = fileSize;
                Console.WriteLine($"[httpserve] 200 {fileName} ({FormatSize(fileSize)})");
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, CHUNK, FileOptions.SequentialScan);
            fs.Seek(rangeStart, SeekOrigin.Begin);

            byte[] buf       = new byte[CHUNK];
            long   remaining = sendLength;
            while (remaining > 0){
                int toRead = (int)Math.Min(CHUNK, remaining);
                int read   = fs.Read(buf, 0, toRead);
                if (read <= 0) break;
                res.OutputStream.Write(buf, 0, read);
                remaining -= read;
            }
        } catch (HttpListenerException){
            // Client disconnected — ignore
        } catch (Exception ex){
            Console.Error.WriteLine($"[httpserve] ServeFile error: {ex.Message}");
            try {
                byte[] err = Encoding.UTF8.GetBytes($"<html><body><h1>500 Error</h1><p>{HtmlEnc(ex.Message)}</p></body></html>");
                if (res.StatusCode == 200){ res.StatusCode = 500; }
                res.ContentLength64 = err.Length;
                res.OutputStream.Write(err, 0, err.Length);
            } catch { }
        } finally {
            try { res.OutputStream.Close(); } catch { }
        }
    }

    private static void ServeDirectoryListing(HttpListenerResponse res, string dirPath, string webRoot, string urlPath){
        var sb = new System.Text.StringBuilder();
        string displayPath = urlPath == "" ? "/" : urlPath;
        if (!displayPath.EndsWith("/")) displayPath += "/";

        sb.AppendLine("<!DOCTYPE html><html lang='ru'><head><meta charset='utf-8'>");
        sb.AppendLine($"<title>📁 {HtmlEnc(displayPath)}</title>");
        sb.AppendLine("<meta name='viewport' content='width=device-width,initial-scale=1'>");
        sb.AppendLine("<style>");
        sb.AppendLine("*{box-sizing:border-box;margin:0;padding:0}");
        sb.AppendLine("body{font-family:'Segoe UI',sans-serif;background:#0f0f13;color:#e0e0e0;min-height:100vh}");
        sb.AppendLine(".header{background:linear-gradient(135deg,#1a1a2e,#16213e);padding:24px 32px;border-bottom:1px solid #2a2a4a;position:sticky;top:0;z-index:10}");
        sb.AppendLine(".header h1{font-size:1.2rem;font-weight:600;color:#a0a8ff;display:flex;align-items:center;gap:10px}");
        sb.AppendLine(".breadcrumb{margin-top:8px;font-size:0.85rem;color:#888}");
        sb.AppendLine(".breadcrumb a{color:#7a8fff;text-decoration:none}.breadcrumb a:hover{color:#fff}");
        sb.AppendLine(".container{padding:20px 32px;max-width:1200px;margin:0 auto}");
        sb.AppendLine(".stats{color:#666;font-size:0.8rem;margin-bottom:16px}");
        sb.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:12px}");
        sb.AppendLine(".card{background:#1a1a2a;border:1px solid #2a2a3a;border-radius:12px;overflow:hidden;transition:all .2s;cursor:pointer;text-decoration:none;color:inherit;display:block}");
        sb.AppendLine(".card:hover{border-color:#5a5aff;transform:translateY(-2px);box-shadow:0 8px 24px rgba(90,90,255,.2)}");
        sb.AppendLine(".card-thumb{width:100%;height:120px;display:flex;align-items:center;justify-content:center;background:#111118;overflow:hidden;position:relative}");
        sb.AppendLine(".card-thumb img{width:100%;height:100%;object-fit:cover}");
        sb.AppendLine(".card-thumb video{width:100%;height:100%;object-fit:cover}");
        sb.AppendLine(".card-thumb .icon{font-size:3rem;line-height:1}");
        sb.AppendLine(".card-info{padding:10px 12px}");
        sb.AppendLine(".card-name{font-size:0.8rem;font-weight:500;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;color:#ddd}");
        sb.AppendLine(".card-meta{font-size:0.7rem;color:#666;margin-top:3px}");
        sb.AppendLine(".back-btn{display:inline-flex;align-items:center;gap:6px;color:#7a8fff;text-decoration:none;font-size:0.9rem;margin-bottom:20px;padding:8px 14px;background:#1a1a2a;border:1px solid #2a2a3a;border-radius:8px;transition:.2s}");
        sb.AppendLine(".back-btn:hover{border-color:#5a5aff;color:#fff}");
        sb.AppendLine(".section-title{font-size:0.75rem;text-transform:uppercase;letter-spacing:.1em;color:#555;margin:20px 0 10px}");
        sb.AppendLine(".list-view{display:flex;flex-direction:column;gap:4px}");
        sb.AppendLine(".list-item{display:flex;align-items:center;gap:12px;padding:10px 14px;background:#1a1a2a;border:1px solid #2a2a3a;border-radius:8px;text-decoration:none;color:#ddd;transition:.15s;font-size:0.85rem}");
        sb.AppendLine(".list-item:hover{border-color:#5a5aff;background:#1e1e30}");
        sb.AppendLine(".list-item .ico{font-size:1.4rem;flex-shrink:0}");
        sb.AppendLine(".list-item .name{flex:1;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}");
        sb.AppendLine(".list-item .size{color:#555;font-size:0.75rem;flex-shrink:0}");
        sb.AppendLine("</style></head><body>");

        // Header
        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>📁 Файловый менеджер</h1>");
        // Breadcrumb
        sb.Append("<div class='breadcrumb'>");
        var parts = displayPath.Trim('/').Split('/');
        sb.Append("<a href='/'>🏠 root</a>");
        string accumulated = "";
        foreach (var p in parts){
            if (string.IsNullOrEmpty(p)) continue;
            accumulated += "/" + p;
            sb.Append($" / <a href='{accumulated}/'>{HtmlEnc(p)}</a>");
        }
        sb.AppendLine("</div></div>");

        sb.AppendLine("<div class='container'>");

        // Back button
        if (displayPath != "/"){
            string parent = displayPath.TrimEnd('/');
            parent = parent.Contains('/') ? parent.Substring(0, parent.LastIndexOf('/') + 1) : "/";
            if (parent == "") parent = "/";
            sb.AppendLine($"<a href='{parent}' class='back-btn'>⬅ Назад</a>");
        }

        var dirs  = Directory.GetDirectories(dirPath).OrderBy(d => Path.GetFileName(d)).ToArray();
        var files = Directory.GetFiles(dirPath).OrderBy(f => Path.GetFileName(f)).ToArray();

        int totalCount = dirs.Length + files.Length;
        sb.AppendLine($"<div class='stats'>{totalCount} элементов</div>");

        // Separate by type
        var imageExts  = new HashSet<string>{".jpg",".jpeg",".png",".gif",".webp",".bmp",".svg",".ico",".tiff",".avif"};
        var videoExts  = new HashSet<string>{".mp4",".webm",".ogg",".mkv",".avi",".mov",".wmv",".flv",".m4v"};
        var audioExts  = new HashSet<string>{".mp3",".wav",".flac",".aac",".ogg",".m4a",".opus",".wma"};

        var imageFiles = files.Where(f => imageExts.Contains(Path.GetExtension(f).ToLower())).ToArray();
        var videoFiles = files.Where(f => videoExts.Contains(Path.GetExtension(f).ToLower())).ToArray();
        var audioFiles = files.Where(f => audioExts.Contains(Path.GetExtension(f).ToLower())).ToArray();
        var otherFiles = files.Where(f => !imageExts.Contains(Path.GetExtension(f).ToLower())
                                       && !videoExts.Contains(Path.GetExtension(f).ToLower())
                                       && !audioExts.Contains(Path.GetExtension(f).ToLower())).ToArray();

        // FOLDERS
        if (dirs.Length > 0){
            sb.AppendLine("<div class='section-title'>📁 Папки</div>");
            sb.AppendLine("<div class='list-view'>");
            foreach (var d in dirs){
                string dName = Path.GetFileName(d);
                int itemCount = Directory.GetFileSystemEntries(d).Length;
                string href = displayPath.TrimEnd('/') + "/" + Uri.EscapeDataString(dName) + "/";
                sb.AppendLine($"<a href='{href}' class='list-item'><span class='ico'>📁</span><span class='name'>{HtmlEnc(dName)}</span><span class='size'>{itemCount} эл.</span></a>");
            }
            sb.AppendLine("</div>");
        }

        // IMAGES
        if (imageFiles.Length > 0){
            sb.AppendLine("<div class='section-title'>🖼 Изображения</div>");
            sb.AppendLine("<div class='grid'>");
            foreach (var f in imageFiles){
                string fName = Path.GetFileName(f);
                string href = displayPath.TrimEnd('/') + "/" + Uri.EscapeDataString(fName);
                long sz = new FileInfo(f).Length;
                sb.AppendLine($"<a href='{href}' class='card'>");
                sb.AppendLine($"<div class='card-thumb'><img src='{href}' loading='lazy' alt='{HtmlEnc(fName)}'></div>");
                sb.AppendLine($"<div class='card-info'><div class='card-name'>{HtmlEnc(fName)}</div><div class='card-meta'>{FormatSize(sz)}</div></div>");
                sb.AppendLine("</a>");
            }
            sb.AppendLine("</div>");
        }

        // VIDEOS
        if (videoFiles.Length > 0){
            sb.AppendLine("<div class='section-title'>🎬 Видео</div>");
            sb.AppendLine("<div class='grid'>");
            foreach (var f in videoFiles){
                string fName = Path.GetFileName(f);
                string href = displayPath.TrimEnd('/') + "/" + Uri.EscapeDataString(fName);
                long sz = new FileInfo(f).Length;
                string ct = GetContentType(fName);
                sb.AppendLine($"<a href='{href}' class='card'>");
                sb.AppendLine($"<div class='card-thumb'><video muted preload='metadata'><source src='{href}' type='{ct}'></video><span style='position:absolute;font-size:2.5rem'>▶️</span></div>");
                sb.AppendLine($"<div class='card-info'><div class='card-name'>{HtmlEnc(fName)}</div><div class='card-meta'>{FormatSize(sz)}</div></div>");
                sb.AppendLine("</a>");
            }
            sb.AppendLine("</div>");
        }

        // AUDIO
        if (audioFiles.Length > 0){
            sb.AppendLine("<div class='section-title'>🎵 Аудио</div>");
            sb.AppendLine("<div class='list-view'>");
            foreach (var f in audioFiles){
                string fName = Path.GetFileName(f);
                string href = displayPath.TrimEnd('/') + "/" + Uri.EscapeDataString(fName);
                long sz = new FileInfo(f).Length;
                sb.AppendLine($"<a href='{href}' class='list-item'><span class='ico'>🎵</span><span class='name'>{HtmlEnc(fName)}</span><span class='size'>{FormatSize(sz)}</span></a>");
            }
            sb.AppendLine("</div>");
        }

        // OTHER FILES
        if (otherFiles.Length > 0){
            sb.AppendLine("<div class='section-title'>📄 Файлы</div>");
            sb.AppendLine("<div class='list-view'>");
            foreach (var f in otherFiles){
                string fName = Path.GetFileName(f);
                string href = displayPath.TrimEnd('/') + "/" + Uri.EscapeDataString(fName);
                long sz = new FileInfo(f).Length;
                string ico = GetFileIcon(fName);
                sb.AppendLine($"<a href='{href}' class='list-item'><span class='ico'>{ico}</span><span class='name'>{HtmlEnc(fName)}</span><span class='size'>{FormatSize(sz)}</span></a>");
            }
            sb.AppendLine("</div>");
        }

        if (totalCount == 0){
            sb.AppendLine("<div style='text-align:center;padding:60px;color:#444'>Папка пустая</div>");
        }

        sb.AppendLine("</div></body></html>");

        byte[] html = Encoding.UTF8.GetBytes(sb.ToString());
        res.StatusCode = 200; res.ContentType = "text/html; charset=utf-8";
        res.ContentLength64 = html.Length; res.OutputStream.Write(html, 0, html.Length);
        res.OutputStream.Close();
        Console.WriteLine($"[httpserve] 200 DIR {displayPath} ({totalCount} items)");
    }

    private static string HtmlEnc(string s) =>
        s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;").Replace("'","&#39;");

    private static string FormatSize(long bytes){
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024*1024) return $"{bytes/1024.0:F1} KB";
        if (bytes < 1024*1024*1024) return $"{bytes/1024.0/1024:F1} MB";
        return $"{bytes/1024.0/1024/1024:F2} GB";
    }

    private static string GetFileIcon(string fileName){
        return Path.GetExtension(fileName).ToLower() switch {
            ".html" or ".htm" => "🌐",
            ".css"  => "🎨",
            ".js"   => "⚡",
            ".json" => "📋",
            ".txt"  => "📝",
            ".md"   => "📝",
            ".pdf"  => "📕",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
            ".exe" or ".msi" => "⚙️",
            ".cs" or ".py" or ".cpp" or ".c" or ".java" or ".rs" or ".go" or ".ts" => "💻",
            ".asm" => "🔧",
            ".xml" => "📄",
            ".csv" or ".xlsx" or ".xls" => "📊",
            ".doc" or ".docx" => "📘",
            ".ppt" or ".pptx" => "📙",
            ".db" or ".sql" or ".sqlite" => "🗄️",
            ".sh" or ".bat" or ".ps1" => "🖥️",
            _ => "📄"
        };
    }

    // POST /api/register
    private static void HandleRegister(HttpListenerRequest req, HttpListenerResponse res, string dbPath){
        try {
            string body     = ReadBody(req);
            var data        = JsonNode.Parse(body);
            string username = data?["username"]?.GetValue<string>()?.Trim() ?? "";
            string password = data?["password"]?.GetValue<string>() ?? "";
            string email    = data?["email"]?.GetValue<string>()?.Trim() ?? "";

            if (username.Length < 3 || password.Length < 6){
                SendJson(res, 400, J(false, "Логин мин. 3 символа, пароль мин. 6"));
                return;
            }

            var db    = LoadDb(dbPath);
            var users = db["users"]!.AsArray();

            foreach (var u in users){
                if (u?["username"]?.GetValue<string>() == username){
                    SendJson(res, 409, J(false, "Логин уже занят"));
                    return;
                }
                if (email != "" && u?["email"]?.GetValue<string>() == email){
                    SendJson(res, 409, J(false, "Email уже используется"));
                    return;
                }
            }

            var newUser = new JsonObject{
                ["id"]        = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["username"]  = username,
                ["email"]     = email,
                ["password"]  = Convert.ToBase64String(Encoding.UTF8.GetBytes(password)),
                ["createdAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            users.Add(newUser);
            SaveDb(dbPath, db);
            Console.WriteLine($"[db.json] Зарегистрирован: {username}");

            SendJson(res, 200, J(true, "Аккаунт создан!"));
        } catch (Exception ex){
            SendJson(res, 500, J(false, ex.Message));
        }
    }

    // POST /api/login
    private static void HandleLogin(HttpListenerRequest req, HttpListenerResponse res, string dbPath){
        try {
            string body     = ReadBody(req);
            var data        = JsonNode.Parse(body);
            string username = data?["username"]?.GetValue<string>()?.Trim() ?? "";
            string password = data?["password"]?.GetValue<string>() ?? "";
            string encoded  = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));

            var db    = LoadDb(dbPath);
            var users = db["users"]!.AsArray();

            JsonNode? found = null;
            foreach (var u in users){
                if (u?["username"]?.GetValue<string>() == username &&
                    u?["password"]?.GetValue<string>() == encoded){
                    found = u; break;
                }
            }

            if (found == null){
                SendJson(res, 401, J(false, "Неверный логин или пароль"));
                return;
            }

            string uname = found["username"]?.GetValue<string>() ?? "";
            string uemail = found["email"]?.GetValue<string>() ?? "";
            Console.WriteLine($"[db.json] Вошёл: {uname}");

            // Строим JSON вручную — без рефлексии (AOT-совместимо)
            string json = $"{{\"ok\":true,\"message\":\"Добро пожаловать!\",\"username\":{JStr(uname)},\"email\":{JStr(uemail)}}}";
            SendRawJson(res, 200, json);
        } catch (Exception ex){
            SendJson(res, 500, J(false, ex.Message));
        }
    }

    // GET /api/users
    private static void HandleGetUsers(HttpListenerResponse res, string dbPath){
        try {
            var db    = LoadDb(dbPath);
            var users = db["users"]!.AsArray();

            var sb = new StringBuilder();
            sb.Append("{\"ok\":true,\"count\":");
            sb.Append(users.Count);
            sb.Append(",\"users\":[");
            bool first = true;
            foreach (var u in users){
                if (!first) sb.Append(',');
                first = false;
                string uname  = u?["username"]?.GetValue<string>() ?? "";
                string uemail = u?["email"]?.GetValue<string>() ?? "";
                string uid    = u?["id"]?.GetValue<long>().ToString() ?? "0";
                string udate  = u?["createdAt"]?.GetValue<string>() ?? "";
                sb.Append($"{{\"id\":{uid},\"username\":{JStr(uname)},\"email\":{JStr(uemail)},\"createdAt\":{JStr(udate)}}}");
            }
            sb.Append("]}");
            SendRawJson(res, 200, sb.ToString());
        } catch (Exception ex){
            SendJson(res, 500, J(false, ex.Message));
        }
    }

    // ── JSON helpers (AOT-safe, без рефлексии) ───────────────────────

    // Простой {"ok":bool,"error/message":"text"}
    private static string J(bool ok, string text){
        string key = ok ? "message" : "error";
        return $"{{\"ok\":{(ok ? "true" : "false")},\"{key}\":{JStr(text)}}}";
    }

    // Экранируем строку для JSON вручную
    private static string JStr(string s){
        var sb = new StringBuilder("\"");
        foreach (char c in s){
            switch (c){
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:   sb.Append(c);      break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static void SendJson(HttpListenerResponse res, int status, string json){
        SendRawJson(res, status, json);
    }

    private static void SendRawJson(HttpListenerResponse res, int status, string json){
        byte[] buf = Encoding.UTF8.GetBytes(json);
        res.StatusCode      = status;
        res.ContentType     = "application/json; charset=utf-8";
        res.ContentLength64 = buf.Length;
        res.OutputStream.Write(buf, 0, buf.Length);
        res.OutputStream.Close();
    }

    private static string ReadBody(HttpListenerRequest req){
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        return reader.ReadToEnd();
    }

    private static readonly object _dbLock = new();

    private static JsonObject LoadDb(string dbPath){
        lock (_dbLock){
            return JsonNode.Parse(File.ReadAllText(dbPath))!.AsObject();
        }
    }

    private static void SaveDb(string dbPath, JsonObject db){
        lock (_dbLock){
            // JsonNode.ToJsonString() работает в AOT
            File.WriteAllText(dbPath, db.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static string GetContentType(string fileName){
        return Path.GetExtension(fileName).ToLower() switch {
            // Text / Web
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".css"            => "text/css; charset=utf-8",
            ".js"             => "application/javascript",
            ".json"           => "application/json",
            ".xml"            => "application/xml",
            ".txt"            => "text/plain; charset=utf-8",
            ".md"             => "text/plain; charset=utf-8",
            ".csv"            => "text/csv; charset=utf-8",
            // Images
            ".png"            => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif"            => "image/gif",
            ".svg"            => "image/svg+xml",
            ".ico"            => "image/x-icon",
            ".webp"           => "image/webp",
            ".bmp"            => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            ".avif"           => "image/avif",
            // Video
            ".mp4"            => "video/mp4",
            ".webm"           => "video/webm",
            ".ogg"            => "video/ogg",
            ".mkv"            => "video/x-matroska",
            ".avi"            => "video/x-msvideo",
            ".mov"            => "video/quicktime",
            ".wmv"            => "video/x-ms-wmv",
            ".flv"            => "video/x-flv",
            ".m4v"            => "video/mp4",
            // Audio
            ".mp3"            => "audio/mpeg",
            ".wav"            => "audio/wav",
            ".flac"           => "audio/flac",
            ".aac"            => "audio/aac",
            ".m4a"            => "audio/mp4",
            ".opus"           => "audio/ogg",
            ".wma"            => "audio/x-ms-wma",
            // Documents
            ".pdf"            => "application/pdf",
            ".doc"            => "application/msword",
            ".docx"           => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls"            => "application/vnd.ms-excel",
            ".xlsx"           => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt"            => "application/vnd.ms-powerpoint",
            ".pptx"           => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            // Archives
            ".zip"            => "application/zip",
            ".rar"            => "application/x-rar-compressed",
            ".7z"             => "application/x-7z-compressed",
            ".tar"            => "application/x-tar",
            ".gz"             => "application/gzip",
            // Fonts
            ".woff"           => "font/woff",
            ".woff2"          => "font/woff2",
            ".ttf"            => "font/ttf",
            ".otf"            => "font/otf",
            _                 => "application/octet-stream",
        };
    }

    private static string GetVarContent(string val){
        if (stringVars.ContainsKey(val))   return stringVars[val];
        if (byteVars.ContainsKey(val))     return byteVars[val].ToString();
        if (shortVars.ContainsKey(val))    return shortVars[val].ToString();
        if (floatVars.ContainsKey(val))    return floatVars[val].ToString();
        if (doubleVars.ContainsKey(val))   return doubleVars[val].ToString();
        if (byteArrs.ContainsKey(val))     return string.Join(" ", byteArrs[val]);
        if (shortArrs.ContainsKey(val))    return string.Join(" ", shortArrs[val]);
        if (floatArrs.ContainsKey(val))    return string.Join(" ", floatArrs[val]);
        if (doubleArrs.ContainsKey(val))   return string.Join(" ", doubleArrs[val]);
        if (stringArrs.ContainsKey(val))   return string.Join(" ", stringArrs[val]);
        return val;
    }
}
