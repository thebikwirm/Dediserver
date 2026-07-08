# Railroader Dedicated Server Mod

Dedicated server helper mod for Railroader.

## RCON / remote-control security

RCON gives remote control over the running server. An authenticated RCON user can request status, save the world, restart the server, stop the server, and send supported game/server commands.

For safety, RCON is disabled by default.

Default RCON settings are intentionally safe:

```json
{
  "EnableRcon": false,
  "RconBindAddress": "127.0.0.1",
  "RconPort": 28016,
  "RconPassword": ""
}
```

Before enabling RCON, edit `dedicated_host.json` and set your own unique strong password. The mod refuses to start RCON with an empty, weak, or shared/default-style password.

Recommended password rules:

- Use 16 or more characters where possible.
- Use a unique password that you do not use anywhere else.
- Include a mix of upper-case letters, lower-case letters, numbers, and symbols.
- Do not use examples such as `changeme`, `password`, `admin`, `test123`, `railroader`, `dediserver`, or similar values.

Keep `RconBindAddress` as `127.0.0.1` unless you specifically need external RCON access. Binding RCON to `0.0.0.0`, a public IP, or a LAN IP can expose remote server control to other machines. Anyone who can reach the RCON port can attempt to authenticate.

If you must expose RCON outside the local machine:

- Use firewall rules to restrict the source IPs that can connect.
- Prefer VPN/private networks instead of public internet exposure.
- Use a long unique password.
- Do not reuse the password for your game server, AMP, Discord bot, or any other service.
- Watch the server log for unexpected RCON connection attempts.

When RCON is enabled, the mod writes a runtime security warning to the UMM/log output and dedicated terminal. If RCON is bound to anything other than a loopback address, the warning is stronger.

## Debug/test file writes

The mod should only write normal config/log/restart files inside the mod folder or game folder. Old debug/test writes such as `D:\deditest.txt` have been removed.
