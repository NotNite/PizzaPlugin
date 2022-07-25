using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PizzaPlugin.API;

public class Store {
    public static Country Country;
    public JObject Data;
    public string id;

    private async Task<string> GetMenuJSONString() {
        if (Country == Country.Canada) {
            var httpClient = new HttpClient();
            var url = Endpoints.Canada["menu_url"].Replace("{store_id}", id).Replace("{lang}", "en");

            var content = await httpClient.GetStringAsync(url);
            return content;
        } else {
            var httpClient = new HttpClient();
            var url = Endpoints.UnitedStates["menu_url"].Replace("{store_id}", id).Replace("{lang}", "en");

            var content = await httpClient.GetStringAsync(url);
            return content;
        }
    }


    public Menu GetMenu() {
        JObject MenuJSON = JObject.Parse(GetMenuJSONString().Result);
        return new Menu(MenuJSON);
    }

    public Store(JObject data, Country country, string storeID) {
        Country = country;
        Data = data;
        id = storeID;
    }
}