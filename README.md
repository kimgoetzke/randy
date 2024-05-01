<p align="center">
  <img src="./assets/randy_logo.png" width="200" height="200" alt="Randy"/>
</p>

# Meet Randy

A Windows application that only has one purpose: near-maximising the active window, i.e. maximising minus 20
px on each side, using a hotkey.

Hit `Windows` + `\` to use the hotkey. This requires only one hand and is next to `Windows` + `z` which is a default
Windows hotkey to creates window groups

The application is written in C#, using .NET 8 and WinForms. The latter may be an awful framework but was the fastest
way to get this done.

## Installation

1. Clone the repo and run `dotnet restore`
2. Run `dotnet publish -c Release -r win-x64 --self-contained`
3. You can then find `Randy.exe` in: `randy\Randy\bin\Release\net8.0-windows\win-x64\publish`
4. Run `Randy.exe` and select `Start with Windows`