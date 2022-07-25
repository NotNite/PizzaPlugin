using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PizzaPlugin.API;

public enum ServiceType {
    Delivery,
    Carryout,
    DriveUpCarryout
}

public class Address {
    [Serializable]
    private class StoreNotFoundException : Exception {
        public StoreNotFoundException() { }
        public StoreNotFoundException(string message) : base(message) { }
        public StoreNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected StoreNotFoundException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public string Street;
    public string City;
    public string Region;
    public string Zip;
    public Country Country;
    public ServiceType ServiceType;

    public Address(string street, string city, string region, string zip, Country country, ServiceType serviceType) {
        Street = street;
        City = city;
        Region = region;
        Zip = zip;
        Country = country;
        ServiceType = serviceType;
    }

    public Store GetClosestStore() {
        Store closestStore = null;

        async Task<String> GetJSON() {
            if (Country == Country.Canada) {
                var httpClient = new HttpClient();
                string URL = Endpoints.Canada["find_url"].Replace("{line1}", Street)
                    .Replace("{line2}", City + ", " + Region + ", " + Zip)
                    .Replace("{type}", ServiceType.ToString());

                var content = await httpClient.GetStringAsync(URL);
                return content;
            } else {
                var httpClient = new HttpClient();
                string URL = Endpoints.UnitedStates["find_url"].Replace("{line1}", Street)
                    .Replace("{line2}", City + ", " + Region + ", " + Zip)
                    .Replace("{type}", ServiceType.ToString());


                var content = await httpClient.GetStringAsync(URL);

                return content;
            }
        }

        async Task<String> GetStoreInfo(string storeId) {
            if (Country == Country.Canada) {
                var httpClient = new HttpClient();
                string URL = Endpoints.Canada["info_url"].Replace("{store_id}", storeId);

                var content = await httpClient.GetStringAsync(URL);
                return content;
            } else {
                var httpClient = new HttpClient();
                string URL = Endpoints.UnitedStates["info_url"].Replace("{store_id}", storeId);


                var content = await httpClient.GetStringAsync(URL);

                return content;
            }
        }

        void SetStoreClass() {
            JObject json = JObject.Parse(GetJSON().Result);
            JArray stores = JArray.Parse(json["Stores"].ToString());

            foreach (JObject store in stores.Children()) {
                if (store["IsOnlineNow"].ToObject<bool>() &&
                    store["ServiceIsOpen"][ServiceType.ToString()].ToObject<bool>()) {
                    closestStore = new Store(JObject.Parse(GetStoreInfo(store["StoreID"].ToString()).Result),
                        Country, store["StoreID"].ToString());
                    break;
                }
            }
        }

        SetStoreClass();
        if (closestStore == null) {
            throw new StoreNotFoundException(
                "Error: No stores nearby are currently open. Try using another service method (e.g ServiceType.Carryout instead of ServiceType.Delivery).");
        }

        return closestStore;
    }
}