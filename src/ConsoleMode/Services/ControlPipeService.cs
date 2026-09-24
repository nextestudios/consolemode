using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using ConsoleMode.ViewModels;
using Microsoft.UI.Dispatching;

namespace ConsoleMode.Services;

/// <summary>
/// Local control API for other tools on this PC (remote-control agents running as a Windows
/// service, scripts, stream decks): the named pipe <c>\\.\pipe\ConsoleMode.Control</c> takes one
/// JSON line per connection and answers one JSON line.
/// <code>
/// → {"cmd":"status"}   (or "start", "stop", "show")
/// ← {"ok":true,"active":true,"restoring":false,"mode":"xboxMode","version":"1.4.0"}
/// </code>
/// "start" is the same as the desktop shortcut (saved setup), "stop" the same as the tray's
/// Restore, so every fullscreen mode (Big Picture, Playnite, Xbox) is restored the app's own way.
/// Only the signed-in user and LocalSystem may connect; nothing is exposed to the network.
/// </summary>
public sealed class ControlPipeService : IDisposable
{
    public const string PipeName = "ConsoleMode.Control";

    private readonly MainViewModel _vm;
    private readonly DispatcherQueue _dispatcher;
    private readonly Action _showWindow;
    private readonly CancellationTokenSource _cts = new();

    public ControlPipeService(MainViewModel vm, DispatcherQueue dispatcher, Action showWindow)
    {
        _vm = vm;
        _dispatcher = dispatcher;
        _showWindow = showWindow;
        _ = Task.Run(ListenAsync);
    }

    public void Dispose() => _cts.Cancel();

    private async Task ListenAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = CreatePipe();
                await pipe.WaitForConnectionAsync(_cts.Token);
                var client = pipe;
                pipe = null;
                _ = Task.Run(() => HandleAsync(client));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AppLog.Write($"Controle: {ex.Message}");
                try { await Task.Delay(2000, _cts.Token); }
                catch (OperationCanceledException) { break; }
            }
            finally
            {
                pipe?.Dispose();
            }
        }
    }

    private static NamedPipeServerStream CreatePipe()
    {
        var security = new PipeSecurity();
        var user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("no user SID");
        security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        return NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, security);
    }

    private async Task HandleAsync(NamedPipeServerStream pipe)
    {
        await using (pipe)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(90));
                using var reader = new StreamReader(pipe, new UTF8Encoding(false), false, 1024, leaveOpen: true);
                var line = await reader.ReadLineAsync(timeout.Token);
                var reply = await RunOnUiAsync(() => ExecuteAsync(ParseCommand(line)));
                await pipe.WriteAsync(Encoding.UTF8.GetBytes(reply + "\n"), timeout.Token);
                await pipe.FlushAsync(timeout.Token);
            }
            catch (Exception ex)
            {
                AppLog.Write($"Controle: {ex.Message}");
            }
        }
    }

    private static string ParseCommand(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return "";
        try
        {
            using var doc = JsonDocument.Parse(line);
            return doc.RootElement.TryGetProperty("cmd", out var cmd) ? (cmd.GetString() ?? "").Trim().ToLowerInvariant() : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private async Task<string> ExecuteAsync(string cmd)
    {
        AppLog.Write($"Controle: {cmd}");
        switch (cmd)
        {
            case "status":
                return Reply(true);
            case "start":
                if (!_vm.IsConsoleActive && !await _vm.TryAutoStartAsync())
                    return Reply(false, "no game display configured");
                return Reply(true);
            case "stop":
                await _vm.StopConsoleAsync();
                return Reply(!_vm.IsConsoleActive, _vm.IsConsoleActive ? "restore failed" : null);
            case "show":
                _showWindow();
                return Reply(true);
            default:
                return Reply(false, "unknown command");
        }
    }

    private string Reply(bool ok, string? error = null)
    {
        var mode = _vm.IsConsoleActive ? _vm.Engine.State.FullscreenMode : ConfigService.Load().FullscreenMode;
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteBoolean("ok", ok);
            if (error is not null) json.WriteString("error", error);
            json.WriteBoolean("active", _vm.IsConsoleActive);
            json.WriteBoolean("restoring", _vm.IsRestoring);
            json.WriteString("mode", mode);
            json.WriteString("version", typeof(ControlPipeService).Assembly.GetName().Version?.ToString(3) ?? "");
            json.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private Task<string> RunOnUiAsync(Func<Task<string>> work)
    {
        var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queued = _dispatcher.TryEnqueue(async () =>
        {
            try { done.SetResult(await work()); }
            catch (Exception ex) { done.SetException(ex); }
        });
        if (!queued) done.SetException(new InvalidOperationException("dispatcher unavailable"));
        return done.Task;
    }
}
