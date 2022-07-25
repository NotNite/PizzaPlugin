using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using PizzaPlugin.Windows;

namespace PizzaPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Pizza Plugin";
        private const string CommandName = "/ppizza";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        
        private WindowSystem windowSystem = new("PizzaPlugin");

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
            
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });
            
            windowSystem.AddWindow(new OrderWindow());
            
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            windowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            windowSystem.GetWindow("OrderWindow").IsOpen = true;
        }

        private void DrawUI() {
            windowSystem.Draw();
        }

        private void DrawConfigUI() {
            windowSystem.GetWindow("ConfigWindow").IsOpen = true;
        }
    }
}
