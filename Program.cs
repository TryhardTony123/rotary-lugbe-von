using DogMeet.Components;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();

var clubAccessCode = builder.Configuration["ClubAccessCode"] ?? "LUGBE-VON-2026";

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isLoginRequest = path.StartsWithSegments("/club-login");
    var hasAccess = context.Request.Cookies.TryGetValue("rotary_club_access", out var code)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(code.PadRight(clubAccessCode.Length)),
            Encoding.UTF8.GetBytes(clubAccessCode.PadRight(code.Length)));

    if (!isLoginRequest && !hasAccess)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(LoginPage());
        return;
    }

    await next();
});

app.UseAntiforgery();

app.MapPost("/club-login", async context =>
{
    var form = await context.Request.ReadFormAsync();
    var submittedCode = form["accessCode"].ToString();

    if (submittedCode == clubAccessCode)
    {
        context.Response.Cookies.Append("rotary_club_access", submittedCode, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30)
        });
        context.Response.Redirect("/");
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(LoginPage("That club access code is incorrect."));
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();

static string LoginPage(string error = "") => $$"""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width,initial-scale=1">
    <title>Members Access - Rotary Club of Lugbe VON</title>
    <style>
        *{box-sizing:border-box}body{margin:0;min-height:100vh;display:grid;place-items:center;padding:24px;background:#0f2f55;font-family:Arial,sans-serif;color:#1d3048}
        .card{width:min(430px,100%);background:#fff;border-radius:18px;padding:34px;box-shadow:0 25px 70px #06182e80}.brand{display:flex;align-items:center;gap:12px;margin-bottom:32px}.wheel{width:46px;height:46px;border:4px dotted #f7a81b;border-radius:50%;display:grid;place-items:center;color:#f7a81b;font-size:22px}.brand b{display:block;font-size:18px}.brand small{display:block;color:#78889b;margin-top:3px}h1{font-size:27px;margin:0 0 8px}p{color:#748397;line-height:1.55;font-size:14px;margin:0 0 24px}label{font-weight:700;font-size:12px}input{display:block;width:100%;margin:8px 0 17px;padding:13px;border:1px solid #d6dde5;border-radius:8px;font-size:15px;outline:0}input:focus{border-color:#27659e;box-shadow:0 0 0 3px #e2eef9}button{width:100%;border:0;border-radius:8px;background:#174f8b;color:#fff;padding:13px;font-weight:700;cursor:pointer}.error{background:#fff0ec;color:#a34f3f;padding:10px;border-radius:7px;font-size:12px;margin-bottom:15px}.note{text-align:center;font-size:10px;color:#94a0ad;margin-top:17px}
    </style>
</head>
<body>
    <main class="card">
        <div class="brand"><span class="wheel">✦</span><div><b>Rotary Club of Lugbe VON</b><small>Members portal</small></div></div>
        <h1>Welcome, Rotarian.</h1>
        <p>Enter the private club access code shared in the official WhatsApp group.</p>
        {{(string.IsNullOrEmpty(error) ? "" : $"<div class=\"error\">{error}</div>")}}
        <form method="post" action="/club-login">
            <label>Club access code<input name="accessCode" type="password" required autocomplete="current-password" placeholder="Enter access code"></label>
            <button type="submit">Open club dashboard</button>
        </form>
        <div class="note">For Rotary Club of Abuja Lugbe VON members only.</div>
    </main>
</body>
</html>
""";
