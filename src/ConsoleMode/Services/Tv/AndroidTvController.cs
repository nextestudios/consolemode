using System.Net.Sockets;
using ConsoleMode.Models;

namespace ConsoleMode.Services.Tv;

/// <summary>
/// Android TV / Google TV (TCL, Sony, Hisense, Philips…) over the network with ADB:
/// wake up, then the "HDMI n" key (or a custom command). Needs Developer options →
/// "USB debugging" / "Network debugging" on the TV; nothing to install on the PC.
/// </summary>
public sealed class AndroidTvController : ITvController
{
    /// <summary>A TV woken by Wake-on-LAN takes a while to bring its network (and adbd) up.</summary>
    private static readonly TimeSpan WakeWait = TimeSpan.FromSeconds(20);

    public async Task TurnOnAsync(TvControlConfig config, TimeSpan approvalTimeout, CancellationToken ct)
    {
        await using var adb = await ConnectAsync(config, approvalTimeout, wakeOnLan: true, ct);
        await adb.ShellAsync($"input keyevent {AdbProtocol.KeyWakeUp}", ct);
        // Right after waking, some TVs drop the input key while the launcher loads.
        await Task.Delay(1500, ct);
        var inputCommand = string.IsNullOrWhiteSpace(config.InputCommand)
            ? $"input keyevent {AdbProtocol.HdmiKey(config.HdmiInput)}"
            : config.InputCommand.Trim();
        await adb.ShellAsync(inputCommand, ct);
    }

    public async Task TurnOffAsync(TvControlConfig config, CancellationToken ct)
    {
        await using var adb = await ConnectAsync(config, TimeSpan.FromSeconds(5), wakeOnLan: false, ct);
        await adb.ShellAsync($"input keyevent {AdbProtocol.KeySleep}", ct);
    }

    private static async Task<AdbClient> ConnectAsync(TvControlConfig config, TimeSpan approvalTimeout, bool wakeOnLan, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.Host))
            throw new TvControlException(LocalizationService.Get("TvHostMissing"));
        var (host, port) = AdbProtocol.ParseEndpoint(config.Host);

        try
        {
            return await AdbClient.ConnectAsync(host, port, approvalTimeout, ct);
        }
        catch (Exception ex) when (IsUnreachable(ex) && wakeOnLan && WakeOnLan.TryParseMac(config.MacAddress, out var mac))
        {
            // Deep standby: no network until the magic packet wakes the TV.
            AppLog.Write($"TV: {host}:{port} sem resposta; enviando Wake-on-LAN");
            await WakeOnLan.SendAsync(mac, ct);
        }
        catch (Exception ex) when (IsUnreachable(ex))
        {
            throw new TvControlException(LocalizationService.Get("TvUnreachable", host));
        }

        var deadline = DateTime.UtcNow + WakeWait;
        while (true)
        {
            await Task.Delay(2000, ct);
            try
            {
                return await AdbClient.ConnectAsync(host, port, approvalTimeout, ct);
            }
            catch (Exception ex) when (IsUnreachable(ex) && DateTime.UtcNow < deadline)
            {
                // Still booting; try again.
            }
            catch (Exception ex) when (IsUnreachable(ex))
            {
                throw new TvControlException(LocalizationService.Get("TvUnreachable", host));
            }
        }
    }

    /// <summary>Nothing listening, no route, connect timeout or a TV that hung up mid-handshake.</summary>
    private static bool IsUnreachable(Exception ex) => ex is SocketException or IOException;
}
