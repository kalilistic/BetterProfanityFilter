// ReSharper disable UnusedMember.Global
// ReSharper disable DelegateSubtraction
// ReSharper disable UnusedType.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Plugin;
using ImGuiNET;

namespace BetterProfanityFilter
{
    public class BetterProfanityFilterPlugin : IDalamudPlugin
    {
        private DalamudPluginInterface _pluginInterface;
        public string Name => "BetterProfanityFilter";
        private ProfanityFilter.ProfanityFilter _profanityFilter;
        private Config _configuration;
        private bool _showSettings;
        private string _wordInput = string.Empty;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
            _configuration = _pluginInterface.GetPluginConfig() as Config ?? new Config();
            InitProfanityFilter();
            pluginInterface.Framework.Gui.Chat.OnChatMessage += ChatOnOnChatMessage;
            _pluginInterface.CommandManager.AddHandler("/profanity", new Dalamud.Game.Command.CommandInfo(ToggleFilter)
            {
                HelpMessage = "Toggle profanity filter."
            });
            _pluginInterface.CommandManager.AddHandler("/profanityconfig", new Dalamud.Game.Command.CommandInfo(OpenConfig)
            {
                HelpMessage = "Open profanity filter config."
            });
            _pluginInterface.UiBuilder.OnBuildUi += DrawWindow;
            _pluginInterface.UiBuilder.OnOpenConfigUi += ToggleConfig;
        }

        private void InitProfanityFilter()
        {
            if (!_configuration.UseDefaults)
            {
                _profanityFilter = new ProfanityFilter.ProfanityFilter(_configuration.BlockedWords);
            }
            else
            {
                _profanityFilter = new ProfanityFilter.ProfanityFilter(); 
                foreach (var word in _configuration.AllowedWords)
                {
                    _profanityFilter.RemoveProfanity(word);
                }
                foreach (var word in _configuration.BlockedWords)
                {
                    _profanityFilter.AddProfanity(word);
                }
            }

        }
        private void ToggleConfig(object sender, EventArgs e)
        {
            _showSettings = !_showSettings;
        }

        private void DrawWindow()
        {
            if (!_showSettings) return;
            var scale = ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextWindowSize(new Vector2(325 * scale, 200 * scale), ImGuiCond.Appearing);
            if (ImGui.Begin("BetterProfanityFilter", ref _showSettings, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar))
            {
                
                // enabled toggle
                var enabled = _configuration.Enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    _configuration.Enabled = enabled;
                    _pluginInterface.SavePluginConfig(_configuration);
                }
                
                // use defaults
                var useDefaults = _configuration.UseDefaults;
                if (ImGui.Checkbox("Use Default Words", ref useDefaults))
                {
                    _configuration.UseDefaults = useDefaults;
                    _pluginInterface.SavePluginConfig(_configuration);
                    InitProfanityFilter();
                }
                
                // add word
                ImGui.Spacing();
                //ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 2 * scale);
                ImGui.InputTextWithHint("###BetterProfanityFilter_AddWord_Input",
                    "Add word", ref _wordInput, 30);
                ImGui.SameLine();
                if (ImGui.Button("Allow") && !string.IsNullOrEmpty(_wordInput))
                {
                    _configuration.AllowedWords.Add(_wordInput);
                    _wordInput = string.Empty;
                    _pluginInterface.SavePluginConfig(_configuration);
                    InitProfanityFilter();
                }
                ImGui.SameLine();
                if (ImGui.Button("Block") && !string.IsNullOrEmpty(_wordInput))
                {
                    _configuration.BlockedWords.Add(_wordInput);
                    _wordInput = string.Empty;
                    _pluginInterface.SavePluginConfig(_configuration);
                    InitProfanityFilter();
                }
                ImGui.Spacing();
                
                // reset
                if (ImGui.Button("Reset To Defaults"))
                {
                    _configuration = new Config();
                    _wordInput = string.Empty;
                    _pluginInterface.SavePluginConfig(_configuration);
                    InitProfanityFilter();
                }
                ImGui.Spacing();

            }
            ImGui.End();
        }

        private void OpenConfig(string command, string arguments)
        {
            _showSettings = true;
        }

        private void ToggleFilter(string command, string arguments)
        {
            _configuration.Enabled = !_configuration.Enabled;
            _pluginInterface.SavePluginConfig(_configuration);
        }

        public void Dispose()
        {
            _pluginInterface.CommandManager.RemoveHandler("/profanity");
            _pluginInterface.CommandManager.RemoveHandler("/profanityconfig");
            _pluginInterface.UiBuilder.OnBuildUi -= DrawWindow;
            _pluginInterface.UiBuilder.OnOpenConfigUi -= ToggleConfig;
            _pluginInterface.Framework.Gui.Chat.OnChatMessage -= ChatOnOnChatMessage;
            _pluginInterface.Dispose();
        }

        private void ChatOnOnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message,
            ref bool isHandled)
        {
            if (!_configuration.Enabled) return;
            foreach (var payload in message.Payloads)
                if (payload is TextPayload textPayload)
                    textPayload.Text = _profanityFilter.CensorString(textPayload.Text);
        }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public bool UseDefaults { get; set; } = true;
        public readonly List<string> AllowedWords = new List<string>();
        public readonly List<string> BlockedWords = new List<string>();
    }
}