using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace PizzaPlugin.API;

public class MenuItem {
    public string Name;
    public string Code;
    public string Price;
}

public class Menu {
    public JObject MenuJSON;
    public List<MenuItem> Items = new();

    public Menu(JObject menu) {
        MenuJSON = menu;
        //PluginLog.Log(MenuJSON.ToString());

        var products = MenuJSON["Variants"] as JObject;
        foreach (var product in products) {
            var obj = product.Value.ToObject<MenuItem>();
            Items.Add(obj);
        }
    }
}