﻿using System;
using System.Collections.Generic;
using System.IO;
using BetterPartySort.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using Lumina.Excel.Sheets;

namespace BetterPartySort;

public sealed class Plugin : IDalamudPlugin {
    private const string CommandName = "/pmycommand";
    private const string SortCommandName = "/bsort";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("BetterPartySort");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Dictionary<PartyRole, uint> partyDict;

    public SortManager SortManager;

    public Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Dalamud>();
        Configuration = Dalamud.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(Dalamud.PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        Dalamud.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        Dalamud.CommandManager.AddHandler(SortCommandName, new CommandInfo(OnSortCommand) {
            HelpMessage = "Sort the party list according to the specified configuration"
        });

        Dalamud.CommandManager.AddHandler("/assign", new CommandInfo(OnRoleCommand) {
            HelpMessage = "designate party member role"
        });

        Dalamud.PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        Dalamud.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        Dalamud.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        partyDict = new Dictionary<PartyRole, uint>(8);
        SortManager = new SortManager(this);
        // Dalamud.ClientState.TerritoryChanged += OnTerritoryChange;
    }

    // private unsafe void OnTerritoryChange(ushort territoryId) {
    //     Dalamud.Log(AgentHUD.Instance()->PartyMemberCount.ToString());
    //     if(AgentHUD.Instance()->PartyMembers.Length == 8)
    //         Dalamud.Log(8 + " party members");
    // }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        Dalamud.CommandManager.RemoveHandler(CommandName);
        // Dalamud.ClientState.TerritoryChanged -= OnTerritoryChange;
    }

    private void OnCommand(string command, string args) {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
        Dalamud.Log(Configuration.SomePropertyToBeSavedAndWithADefault.ToString());
    }

    private void OnSortCommand(string command, string args) {
        //SortManager.PopulatePartyMembers();
        SortManager.SortParty(int.Parse(args));

        foreach (var order in Configuration.PartyConfigurations) {
            Dalamud.Log("\n" + order.ConfigurationName);
            for (int i = 0; i < 8; i++) {
                Dalamud.Log(order.GetRoleByIndex(i).ToString());
            }
        }
    }

    private unsafe void OnRoleCommand(string command, string args) {
        Dalamud.Log(AgentHUD.Instance()->CurrentTargetId.ToString());
        Enum.TryParse(args, out PartyRole role);
        partyDict.Add(role, AgentHUD.Instance()->CurrentTargetId);
    }


    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}