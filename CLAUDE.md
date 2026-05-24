# MyZabbix

Monitoring nastroj pro zobrazeni dat ze Zabbix API — .NET 10 Blazor Server + MAUI aplikace.

## Technologie

- .NET 10, Blazor Server (bez `[Authorize]` — pouziva vlastni stav pripojeni)
- `MyZabbix.Core` — izolovaná vrstva pro Zabbix API (`ZabbixApiService`, `ZabbixModels`)
- `Blazored.LocalStorage` — ulozeni URL, username (heslo se neukla da)
- Bootstrap 5, Bootstrap Icons (`bi bi-*`)
- `SharedServices` — git submodule
- Auto-refresh: periodicke volani kazdeych 60s pres timer

## Struktura projektu

```
src/
  MyZabbix.Web/
    Components/
      Pages/
        Admin/
          AdminDashboard.razor   # /admin (Authorize Roles=Admin)
        Home.razor               # / (dashboard: summary Zabbix dat)
        Hosts.razor              # /hosts (seznam hostu, filter + paginace)
        HostDetail.razor         # /hosts/{id} (detail hosta)
        Problems.razor           # /problems (aktivni problemy, filter + severity)
        Triggers.razor           # /triggers (aktivni triggery v PROBLEM stavu, filter)
        Settings.razor           # /settings (konfigurace Zabbix pripojeni)
  MyZabbix.Core/
    Models/
      ZabbixModels.cs            # ZabbixHost, ZabbixProblem, ZabbixTrigger, ...
    Services/
      ZabbixApiService.cs        # HTTP klient pro Zabbix JSON-RPC API
  MyZabbix.Tests/
  MyZabbix.Mobile/               # MAUI
  SharedServices/                # git submodule
```

## Klicove modely (MyZabbix.Core.Models)

- `ZabbixHost` — `HostId`, `Host`, `Name`, `Status`, `IsEnabled`, `StatusLabel`, `StatusBadge`
- `ZabbixProblem` — `Name`, `HostName`, `Severity`, `OccurredAt`, `IsAcknowledged`, `SeverityLabel`, `SeverityBadge`
- `ZabbixTrigger` — priorita, host, popis

## Auth a pripojeni

- `Zabbix.IsAuthenticated` — kontrola stavu pred zobrazenim dat
- Neni-li pripojeno, zobrazit `<div class="alert alert-warning">Not connected. Go to <a href="/settings">Settings</a>.</div>`
- Settings strana ulozi URL + username do LocalStorage, heslo se nepersistuje

## Konvence

- Filter pattern: `_filter` string + `@bind:event="oninput"` + `OnFilterChanged()`
- Problems ma navic `_minSeverity` (int) pro filtrovani podle zavaznosti
- Refresh button: `@onclick="LoadAsync"` + auto-refresh timer v `OnInitializedAsync`
- `@implements IDisposable` — timer se disposes v `Dispose()`
- `ZabbixApiService` komunikuje pres JSON-RPC (Zabbix API v5+)
