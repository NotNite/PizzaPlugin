using System;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PizzaPlugin.API;

namespace PizzaPlugin.API;

public class Order {
    [Serializable]
    private class InvalidItemCodeException : Exception {
        public InvalidItemCodeException() { }
        public InvalidItemCodeException(string message) : base(message) { }
        public InvalidItemCodeException(string message, Exception inner) : base(message, inner) { }

        protected InvalidItemCodeException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public JObject Data;
    public JObject MenuJSON;
    public API.Customer Customer;
    public API.Address Address;
    public Country Country;
    public Store Store;

    public Order(Store store, Customer customer, Address address, Country country) {
        Country = country;
        MenuJSON = store.GetMenu().MenuJSON;
        Store = store;
        Customer = customer;
        Address = address;
        Data = JObject.Parse(@"
            {
    ""Address"": {
        ""Street"": """",
        ""City"": """",
        ""Region"": """",
        ""PostalCode"": """",
        ""Type"": """"
    },
    ""Coupons"": [],
    ""CustomerID"": """",
    ""Extension"": """",
    ""OrderChannel"": ""OLO"",
    ""OrderID"": """",
    ""NoCombine"": ""true"",
    ""OrderMethod"": ""Web"",
    ""OrderTaker"": ""null"",
    ""Payments"": [],
    ""Products"": [],
    ""Market"": """",
    ""Currency"": """",
    ""ServiceMethod"": ""Delivery"",
    ""Tags"": {},
    ""Version"": ""1.0"",
    ""SourceOrganizationURI"": ""order.dominos.com"",
    ""LanguageCode"": ""en"",
    ""Partners"": {},
    ""NewUser"": true,
    ""metaData"": {},
    ""Amounts"": {},
    ""BusinessDate"": """",
    ""EstimatedWaitMinutes"": """",
    ""PriceOrderTime"": """",
    ""AmountsBreakdown"": {}
}
                                ");
        JObject addressData = (JObject)Data["Address"];
        Data["ServiceMethod"] = this.Address.ServiceType.ToString();
        addressData["Street"] = ((string)address.Street);
        addressData["City"] = ((string)address.City);
        addressData["Region"] = ((string)address.Region);
        addressData["PostalCode"] = ((string)address.Zip);
        if (address.Street.ToLower().Contains("apartment") || address.Street.ToLower().Contains("apt") ||
            address.Street.ToLower().Contains("#")) {
            addressData["Type"] = ((string)"Apartment");
        } else {
            addressData["Type"] = ((string)"House");
        }
    }

    public void add_item(int quantity, string itemCode) {
        if ((JObject)MenuJSON["Variants"][itemCode] == null) {
            throw new InvalidItemCodeException(
                "Invalid item code, please make sure you are using the item code, not the item name. e.g (use 500DIETC instead of Diet Coke 500ml).");
        }

        for (int i = 0; i < quantity; i++) {
            JObject item = (JObject)MenuJSON["Variants"][itemCode];
            JArray a = (JArray)Data["Products"];
            a.Add((JToken)item);
            item["Price"].ToObject<double>();
        }
    }

    public void remove_item(int quantityToRemove, string itemCode) {
        if ((JObject)MenuJSON["Variants"][itemCode] == null) {
            throw new InvalidItemCodeException(
                "Invalid item code, please make sure you are using the item code, not the item name. e.g (use 500DIETC instead of Diet Coke 500ml).");
        }

        for (int i = 0; i < quantityToRemove; i++) {
            JObject item = (JObject)MenuJSON["Coupons"][itemCode];
            JArray a = (JArray)Data["Products"];
            a.Remove((JToken)item);
            item["Price"].ToObject<double>();
        }
    }

    public void add_coupon(string couponCode) {
        bool isAcceptableOrderType = false;

        if ((JObject)MenuJSON["Coupons"][couponCode] == null) {
            throw new InvalidItemCodeException("Invalid coupon code.");
        }

        JObject item = (JObject)MenuJSON["Coupons"][couponCode];
        JArray a = (JArray)Data["Coupons"];


        foreach (var vsm in JArray.Parse(item["Tags"]["ValidServiceMethods"].ToString()).Children()) {
            if (Address.ServiceType.ToString() == vsm.ToString()) {
                isAcceptableOrderType = true;
                break;
            }
        }


        if (!isAcceptableOrderType) {
            throw new Exception("Coupon does not support your service type.");
        }

        foreach (var coupon in a.Children()) {
            JObject couponO = (JObject)coupon;
            if (couponO["Code"].ToString() == couponCode) {
                Console.WriteLine("Coupon already exists!");
                return;
            }
        }

        a.Add((JToken)item);
    }

    public void remove_coupon(string couponCode) {
        JArray a = JArray.Parse(Data["Coupons"].ToString());


        for (int i = 0; i < a.Count; i++) {
            JObject coupon = (JObject)a[i];
            if (JObject.Parse(coupon.ToString())["Code"].ToString() == couponCode) {
                Console.WriteLine(a.Remove((JToken)coupon));
                Data["Coupons"] = JArray.Parse(a.ToString());
            }
        }
    }

    private JObject send(string URL, bool Merge, string content) {
        Data["StoreID"] = Store.id;
        Data["Email"] = Customer.email;
        Data["FirstName"] = Customer.first_name;
        Data["LastName"] = Customer.last_name;
        Data["Phone"] = Customer.phone_number;
        HttpClient c = new HttpClient();
        Console.WriteLine(Data.ToString());
        StringContent stringContent = null;
        if (content == null) {
            stringContent = new StringContent(" { " + @"""Order"" : " + Data.ToString() + " } ", Encoding.UTF8,
                "application/json");
        } else {
            stringContent = new StringContent(content, Encoding.UTF8, "application/json");
        }

        c.DefaultRequestHeaders.Add("Referer", "https://order.dominos.com/en/pages/order/");

        Task<HttpResponseMessage> m = c.PostAsync(URL, stringContent);


        JObject jsonResponse = JObject.Parse(m.Result.Content.ReadAsStringAsync().Result.Replace(@"^""}]}", ""));

        if (Merge) {
            foreach (var keyValuePair in jsonResponse) {
                Data[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        return jsonResponse;
    }

    public void place(string type) {
        bool isAcceptable = false;
        JArray acceptablePaymentTypes = (JArray)Store.Data["AcceptablePaymentTypes"];
        foreach (var v in acceptablePaymentTypes.Children()) {
            if (v.ToString() == type) {
                isAcceptable = true;
            }
        }

        if (!isAcceptable) {
            throw new Exception("Store does not support type " + type + ".");
        }

        JArray paymentArray = JArray.Parse(Data["Payments"].ToString());
        JObject typeObj = new JObject { { "Type", type } };
        paymentArray.Add(typeObj);
        send(Endpoints.Canada["place_url"], false,
            Country == Country.Canada
                ? send(Endpoints.Canada["price_url"], true, null).ToString()
                : send(Endpoints.UnitedStates["price_url"], true, null).ToString());
    }

    public void place(PaymentObject o) {
        bool isAcceptableCard = false;
        bool canPayWithCard = false;

        JArray acceptableCards = (JArray)Store.Data["AcceptableCreditCards"];
        JArray acceptablePaymentTypes = (JArray)Store.Data["AcceptablePaymentTypes"];
        foreach (var v in acceptableCards.Children()) {
            if (v.ToString() == o.type.ToString()) {
                isAcceptableCard = true;
            }
        }

        foreach (var v in acceptablePaymentTypes.Children()) {
            if (v.ToString() == "CreditCard") {
                isAcceptableCard = true;
            }
        }

        if (canPayWithCard == false) {
            throw new Exception("Store does not support credit cards.");
        }

        if (isAcceptableCard) {
            JArray paymentArray = JArray.Parse(Data["Payments"].ToString());
            JObject typeObj = new JObject();
            typeObj.Add("Type", "CreditCard");
            typeObj.Add("Expiration", o.expiration);
            typeObj.Add("Amount", 0);
            typeObj.Add("CardType", o.type.ToString());
            typeObj.Add("Number", int.Parse(o.number));
            typeObj.Add("SecurityCode", int.Parse(o.cvv));
            typeObj.Add("PostalCode", int.Parse(o.zip));
            paymentArray.Add(typeObj);
        } else {
            throw new Exception("Card unsupported.");
        }

        if (Country == Country.Canada) {
            send(Endpoints.Canada["place_url"], false, send(Endpoints.Canada["price_url"], true, null).ToString());
        } else {
            send(Endpoints.Canada["place_url"], false,
                send(Endpoints.UnitedStates["price_url"], true, null).ToString());
        }
    }
}